using GameReaderCommon;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;

namespace User.IRacingRadarPlugin
{
    internal sealed class TrackMapGeometry
    {
        internal const int PointCount = 48;
        internal const double CenterX = 210.0;
        internal const double CenterY = 130.0;
        internal const double CircleRadiusPixels = 129.0;
        internal const double PlayerWidthMeters = 1.9;
        internal const double PlayerLengthMeters = 4.8;

        private static readonly Regex CoordinatePattern = new Regex(
            "\"Value\"\\s*:\\s*\\[\\s*(?<x>[-+0-9.eE]+)\\s*,\\s*(?<y>[-+0-9.eE]+)\\s*,\\s*(?<z>[-+0-9.eE]+)\\s*\\]",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);
        private static readonly Regex ProgressPattern = new Regex(
            "\"p\"\\s*:\\s*(?<p>[-+0-9.eE]+)",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);
        private readonly List<WorldPoint> points = new List<WorldPoint>();
        private readonly List<double> recordedProgress = new List<double>();
        private readonly List<double> cumulative = new List<double>();
        private string loadedTrack = string.Empty;
        private string failedTrack = string.Empty;
        private double totalLength;
        internal string SourceName { get; private set; }
        internal bool Available { get { return points.Count >= 8 && totalLength > 100.0; } }

        internal TrackVisualFrame Build(StatusDataBase telemetry, double roadWidthMeters,
            double pixelsPerMeter)
        {
            EnsureLoaded(telemetry);
            TrackVisualFrame frame = TrackVisualFrame.Empty(roadWidthMeters, pixelsPerMeter);
            if (!Available || telemetry == null) return frame;

            double progress = NormalizeProgress(telemetry.TrackPositionPercent);
            if (double.IsNaN(progress)) return frame;

            // A map record may come from another driving session whose coordinate origin
            // differs. Its recorded lap-progress value is the stable link to live telemetry.
            WorldPoint mapCenter = SampleAtProgress(progress);
            WorldPoint player = mapCenter;
            frame.ProgressPercent = progress * 100.0;
            double fiveMetersOfLap = 5.0 / totalLength;
            WorldPoint behind = SampleAtProgress(progress - fiveMetersOfLap);
            WorldPoint ahead = SampleAtProgress(progress + fiveMetersOfLap);
            double forwardX = ahead.X - behind.X;
            double forwardZ = ahead.Z - behind.Z;
            double forwardLength = Math.Sqrt(forwardX * forwardX + forwardZ * forwardZ);
            if (forwardLength < 0.01) return frame;
            forwardX /= forwardLength;
            forwardZ /= forwardLength;
            double rightX = -forwardZ;
            double rightZ = forwardX;



            double viewRangeMeters = (CircleRadiusPixels + frame.RoadWidthPixels * 0.55) / pixelsPerMeter;

            frame.Available = true;
            for (int i = 0; i < PointCount; i++)
            {
                double offset = -viewRangeMeters + 2.0 * viewRangeMeters * i / (PointCount - 1);
                WorldPoint sample = SampleAtProgress(progress + offset / totalLength);
                double dx = sample.X - player.X;
                double dz = sample.Z - player.Z;
                double x = CenterX + (dx * rightX + dz * rightZ) * pixelsPerMeter;
                double y = CenterY - (dx * forwardX + dz * forwardZ) * pixelsPerMeter;
                double radius = Math.Sqrt((x - CenterX) * (x - CenterX) + (y - CenterY) * (y - CenterY));
                frame.Points[i] = new TrackVisualPoint(x, y,
                    radius <= CircleRadiusPixels - frame.RoadWidthPixels * 0.52);
            }
            return frame;
        }
        private static double NormalizeProgress(double progress)
        {
            if (!IsFinite(progress)) return double.NaN;
            if (Math.Abs(progress) > 1.5) progress /= 100.0;
            progress %= 1.0;
            return progress < 0.0 ? progress + 1.0 : progress;
        }

        private static bool IsFinite(double value)
        {
            return !double.IsNaN(value) && !double.IsInfinity(value);
        }
        private void EnsureLoaded(StatusDataBase telemetry)
        {
            string track = ReadTrackKey(telemetry);
            if (track.Length == 0 || track.Equals(loadedTrack, StringComparison.OrdinalIgnoreCase) ||
                track.Equals(failedTrack, StringComparison.OrdinalIgnoreCase)) return;
            points.Clear();
            recordedProgress.Clear();
            cumulative.Clear();
            totalLength = 0.0;
            SourceName = null;
            string path = FindTrackRecord(track);
            if (path == null || !TryLoad(path))
            {
                failedTrack = track;
                loadedTrack = string.Empty;
                return;
            }
            loadedTrack = track;
            failedTrack = string.Empty;
            SourceName = Path.GetFileName(path);
        }

        private bool TryLoad(string path)
        {
            try
            {
                string json;
                using (FileStream input = File.OpenRead(path))
                using (GZipStream gzip = new GZipStream(input, CompressionMode.Decompress))
                using (StreamReader reader = new StreamReader(gzip, Encoding.UTF8))
                    json = reader.ReadToEnd();
                int start = json.IndexOf("\"CarCoordinates\"", StringComparison.Ordinal);
                int end = json.IndexOf("\"LapId\"", Math.Max(0, start), StringComparison.Ordinal);
                if (start < 0 || end <= start) return false;
                string coordinateSection = json.Substring(start, end - start);
                MatchCollection coordinateMatches = CoordinatePattern.Matches(coordinateSection);
                MatchCollection progressMatches = ProgressPattern.Matches(coordinateSection);
                for (int matchIndex = 0; matchIndex < coordinateMatches.Count; matchIndex++)
                {
                    Match match = coordinateMatches[matchIndex];
                    double x;
                    double z;
                    double p;
                    if (!double.TryParse(match.Groups["x"].Value, NumberStyles.Float,
                        CultureInfo.InvariantCulture, out x) ||
                        !double.TryParse(match.Groups["z"].Value, NumberStyles.Float,
                        CultureInfo.InvariantCulture, out z) ||
                        matchIndex >= progressMatches.Count ||
                        !double.TryParse(progressMatches[matchIndex].Groups["p"].Value,
                        NumberStyles.Float, CultureInfo.InvariantCulture, out p)) continue;
                    WorldPoint current = new WorldPoint(x, z);
                    if (points.Count > 0)
                    {
                        double gap = Distance(points[points.Count - 1], current);
                        if (gap < 0.15) continue;
                        if (gap > 40.0) break;
                    }
                    points.Add(current);
                    recordedProgress.Add(NormalizeProgress(p));
                }
                if (points.Count < 8 || recordedProgress.Count != points.Count) return false;
                for (int i = 1; i < recordedProgress.Count; i++)
                    if (recordedProgress[i] <= recordedProgress[i - 1]) return false;
                cumulative.Add(0.0);
                for (int i = 1; i < points.Count; i++)
                    cumulative.Add(cumulative[i - 1] + Distance(points[i - 1], points[i]));
                totalLength = cumulative[cumulative.Count - 1] +
                    Distance(points[points.Count - 1], points[0]);
                return totalLength > 100.0;
            }
            catch
            {
                points.Clear();
                recordedProgress.Clear();
                cumulative.Clear();
                totalLength = 0.0;
                return false;
            }
        }

        private static string ReadTrackKey(StatusDataBase telemetry)
        {
            if (telemetry == null) return string.Empty;
            if (!string.IsNullOrWhiteSpace(telemetry.TrackNameWithConfig))
                return telemetry.TrackNameWithConfig.Trim();
            return ((telemetry.TrackName ?? string.Empty) + "-" +
                (telemetry.TrackConfig ?? string.Empty)).Trim(' ', '-');
        }

        private static string FindTrackRecord(string track)
        {
            string simHub = Path.GetDirectoryName(typeof(TrackMapGeometry).Assembly.Location) ?? string.Empty;
            string[] folders =
            {
                Path.Combine(simHub, "PluginsData", "IRacing", "MapRecords"),
                Path.Combine(simHub, "PluginsData", "IRacing", "MapRecordsCloud")
            };
            string normalizedTrack = Normalize(track);
            string best = null;
            int bestScore = -1;
            foreach (string folder in folders)
            {
                if (!Directory.Exists(folder)) continue;
                foreach (string file in Directory.GetFiles(folder, "*.shtl"))
                {
                    string normalizedFile = Normalize(Path.GetFileNameWithoutExtension(file));
                    int score = normalizedFile == normalizedTrack ? 100000 :
                        normalizedFile.Contains(normalizedTrack) || normalizedTrack.Contains(normalizedFile)
                            ? Math.Min(normalizedFile.Length, normalizedTrack.Length) : -1;
                    if (score > bestScore)
                    {
                        best = file;
                        bestScore = score;
                    }
                }
            }
            return best;
        }

        private static string Normalize(string value)
        {
            StringBuilder result = new StringBuilder();
            foreach (char c in (value ?? string.Empty).ToLowerInvariant())
                if (char.IsLetterOrDigit(c)) result.Append(c);
            return result.ToString();
        }

        private WorldPoint SampleAtProgress(double progress)
        {
            progress = NormalizeProgress(progress);
            int low;
            int next;
            double target = progress;
            double start;
            double end;
            if (progress < recordedProgress[0] || progress >= recordedProgress[recordedProgress.Count - 1])
            {
                low = recordedProgress.Count - 1;
                next = 0;
                start = recordedProgress[low];
                end = recordedProgress[0] + 1.0;
                if (target < recordedProgress[0]) target += 1.0;
            }
            else
            {
                low = 0;
                int high = recordedProgress.Count - 1;
                while (low < high)
                {
                    int middle = (low + high + 1) / 2;
                    if (recordedProgress[middle] <= target) low = middle;
                    else high = middle - 1;
                }
                next = low + 1;
                start = recordedProgress[low];
                end = recordedProgress[next];
            }
            double segment = end - start;
            double amount = segment > 0.0000001 ? (target - start) / segment : 0.0;
            return new WorldPoint(
                points[low].X + (points[next].X - points[low].X) * amount,
                points[low].Z + (points[next].Z - points[low].Z) * amount);
        }

        private static double Distance(WorldPoint a, WorldPoint b)
        {
            double dx = b.X - a.X;
            double dz = b.Z - a.Z;
            return Math.Sqrt(dx * dx + dz * dz);
        }

        private struct WorldPoint
        {
            internal readonly double X;
            internal readonly double Z;
            internal WorldPoint(double x, double z) { X = x; Z = z; }
        }
    }

    internal sealed class TrackVisualFrame
    {
        internal bool Available;
        internal double PixelsPerMeter;
        internal double ProgressPercent;
        internal double RoadWidthPixels;
        internal double PlayerWidthPixels;
        internal double PlayerLengthPixels;
        internal readonly TrackVisualPoint[] Points = new TrackVisualPoint[TrackMapGeometry.PointCount];

        internal const double DefaultPixelsPerMeter = 3.5;
        internal const double ReferencePlayerWidthPixels = 12.0;
        internal const double ReferencePlayerLengthPixels = 30.0;

        internal static TrackVisualFrame Empty(double roadWidthMeters, double pixelsPerMeter)
        {
            pixelsPerMeter = Math.Max(2.0, Math.Min(12.0, pixelsPerMeter));
            return new TrackVisualFrame
            {
                PixelsPerMeter = pixelsPerMeter,
                RoadWidthPixels = roadWidthMeters * pixelsPerMeter,
                PlayerWidthPixels = ReferencePlayerWidthPixels,
                PlayerLengthPixels = ReferencePlayerLengthPixels
            };
        }
    }

    internal struct TrackVisualPoint
    {
        internal readonly double X;
        internal readonly double Y;
        internal readonly bool Visible;
        internal TrackVisualPoint(double x, double y, bool visible)
        {
            X = x;
            Y = y;
            Visible = visible;
        }
    }
}
