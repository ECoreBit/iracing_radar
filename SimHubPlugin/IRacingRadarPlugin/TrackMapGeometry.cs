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
        internal const double CircleRadiusPixels = 125.0;
        internal const double PlayerWidthMeters = 1.9;
        internal const double PlayerLengthMeters = 4.8;
        internal const double ContactCenterDistanceMeters = 4.5;

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
            double pixelsPerMeter, double predictedOvertakeDistanceMeters,
            double frontOpponentRelativeMeters, double rearOpponentRelativeMeters,
            double markerScalePercent)
        {
            EnsureLoaded(telemetry);
            TrackVisualFrame frame = TrackVisualFrame.Empty(roadWidthMeters, pixelsPerMeter, markerScalePercent);
            if (!Available || telemetry == null) return frame;

            double progress = NormalizeProgress(telemetry.TrackPositionPercent);
            if (double.IsNaN(progress)) return frame;

            // A map record may come from another driving session whose coordinate origin
            // differs. Its recorded lap-progress value is the stable link to live telemetry.
            double playerDistance = DistanceAtProgress(progress);
            WorldPoint mapCenter = SampleAtDistance(playerDistance);
            WorldPoint player = mapCenter;
            frame.ProgressPercent = progress * 100.0;
            WorldPoint behind = SampleAtDistance(playerDistance - 5.0);
            WorldPoint ahead = SampleAtDistance(playerDistance + 5.0);
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
                WorldPoint sample = SampleAtDistance(playerDistance + offset);
                double dx = sample.X - player.X;
                double dz = sample.Z - player.Z;
                double x = CenterX + (dx * rightX + dz * rightZ) * pixelsPerMeter;
                double y = CenterY - (dx * forwardX + dz * forwardZ) * pixelsPerMeter;
                double radius = Math.Sqrt((x - CenterX) * (x - CenterX) + (y - CenterY) * (y - CenterY));
                frame.Points[i] = new TrackVisualPoint(x, y,
                    radius <= CircleRadiusPixels - frame.RoadWidthPixels * 0.52);
            }
            if (IsFinite(predictedOvertakeDistanceMeters) && predictedOvertakeDistanceMeters > 0.0)
            {
                double predictionDistance = playerDistance + predictedOvertakeDistanceMeters;
                WorldPoint prediction = SampleAtDistance(predictionDistance);
                double dx = prediction.X - player.X;
                double dz = prediction.Z - player.Z;
                double x = CenterX + (dx * rightX + dz * rightZ) * pixelsPerMeter;
                double y = CenterY - (dx * forwardX + dz * forwardZ) * pixelsPerMeter;
                double radius = Math.Sqrt((x - CenterX) * (x - CenterX) + (y - CenterY) * (y - CenterY));
                frame.PredictedOvertakeX = x;
                frame.PredictedOvertakeY = y;
                frame.PredictedOvertakeRotation = LocalRotationDegrees(predictionDistance,
                    rightX, rightZ, forwardX, forwardZ);
                frame.PredictedOvertakeVisible = radius <= CircleRadiusPixels - frame.RoadWidthPixels * 0.60;
            }
            ProjectOpponent(frame, playerDistance, player, rightX, rightZ, forwardX, forwardZ,
                pixelsPerMeter, frontOpponentRelativeMeters, true);
            ProjectOpponent(frame, playerDistance, player, rightX, rightZ, forwardX, forwardZ,
                pixelsPerMeter, rearOpponentRelativeMeters, false);
            return frame;
        }

        private void ProjectOpponent(TrackVisualFrame frame, double playerDistance, WorldPoint player,
            double rightX, double rightZ, double forwardX, double forwardZ, double pixelsPerMeter,
            double relativeMeters, bool front)
        {
            if (!IsFinite(relativeMeters) || Math.Abs(relativeMeters) < 0.25) return;
            double displayedRelativeMeters = CalculateDisplayedOpponentDistance(relativeMeters,
                pixelsPerMeter, frame.PlayerLengthPixels);
            double opponentDistance = playerDistance - displayedRelativeMeters;
            WorldPoint opponent = SampleAtDistance(opponentDistance);
            double dx = opponent.X - player.X;
            double dz = opponent.Z - player.Z;
            double x = CenterX + (dx * rightX + dz * rightZ) * pixelsPerMeter;
            double y = CenterY - (dx * forwardX + dz * forwardZ) * pixelsPerMeter;
            double radius = Math.Sqrt((x - CenterX) * (x - CenterX) + (y - CenterY) * (y - CenterY));
            bool visible = radius <= CircleRadiusPixels - frame.RoadWidthPixels * 0.68;
            double opacity = TrackVisualFrame.OpponentMarkerOpacity(radius,
                frame.PlayerLengthPixels);
            double rotation = LocalRotationDegrees(opponentDistance,
                rightX, rightZ, forwardX, forwardZ);
            if (front)
            {
                frame.FrontOpponentX = x;
                frame.FrontOpponentY = y;
                frame.FrontOpponentVisible = visible;
                frame.FrontOpponentRotation = rotation;
                frame.FrontOpponentOpacity = opacity;
            }
            else
            {
                frame.RearOpponentX = x;
                frame.RearOpponentY = y;
                frame.RearOpponentVisible = visible;
                frame.RearOpponentRotation = rotation;
                frame.RearOpponentOpacity = opacity;
            }
        }

        internal static double CalculateDisplayedOpponentDistance(double relativeMeters,
            double pixelsPerMeter, double markerLengthPixels)
        {
            if (!IsFinite(relativeMeters) || pixelsPerMeter <= 0.0 || markerLengthPixels <= 0.0)
                return relativeMeters;
            double physicalGapMeters = Math.Max(0.0,
                Math.Abs(relativeMeters) - ContactCenterDistanceMeters);
            double displayedMagnitude = markerLengthPixels / pixelsPerMeter + physicalGapMeters;
            return relativeMeters < 0.0 ? -displayedMagnitude : displayedMagnitude;
        }

        private double LocalRotationDegrees(double distance, double rightX, double rightZ,
            double forwardX, double forwardZ)
        {
            WorldPoint behind = SampleAtDistance(distance - 5.0);
            WorldPoint ahead = SampleAtDistance(distance + 5.0);
            double dx = ahead.X - behind.X;
            double dz = ahead.Z - behind.Z;
            double screenX = dx * rightX + dz * rightZ;
            double screenY = -(dx * forwardX + dz * forwardZ);
            return Math.Atan2(screenY, screenX) * 180.0 / Math.PI + 90.0;
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
            return SampleAtDistance(DistanceAtProgress(progress));
        }

        private double DistanceAtProgress(double progress)
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
            double startDistance = cumulative[low];
            double endDistance = next == 0 ? totalLength : cumulative[next];
            return startDistance + (endDistance - startDistance) * amount;
        }

        private WorldPoint SampleAtDistance(double distance)
        {
            if (!IsFinite(distance) || totalLength <= 0.0 || points.Count == 0)
                return new WorldPoint(0.0, 0.0);
            distance %= totalLength;
            if (distance < 0.0) distance += totalLength;

            int low = 0;
            int high = cumulative.Count - 1;
            while (low < high)
            {
                int middle = (low + high + 1) / 2;
                if (cumulative[middle] <= distance) low = middle;
                else high = middle - 1;
            }
            int next = low + 1;
            double startDistance = cumulative[low];
            double endDistance;
            if (next >= points.Count)
            {
                next = 0;
                endDistance = totalLength;
            }
            else
                endDistance = cumulative[next];
            double segment = endDistance - startDistance;
            double amount = segment > 0.0000001 ? (distance - startDistance) / segment : 0.0;
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
        internal bool PredictedOvertakeVisible;
        internal double PredictedOvertakeX = TrackMapGeometry.CenterX;
        internal double PredictedOvertakeY = TrackMapGeometry.CenterY;
        internal double PredictedOvertakeRotation;
        internal bool FrontOpponentVisible;
        internal double FrontOpponentX = TrackMapGeometry.CenterX;
        internal double FrontOpponentY = TrackMapGeometry.CenterY;
        internal double FrontOpponentRotation;
        internal double FrontOpponentOpacity = 100.0;
        internal bool RearOpponentVisible;
        internal double RearOpponentX = TrackMapGeometry.CenterX;
        internal double RearOpponentY = TrackMapGeometry.CenterY;
        internal double RearOpponentRotation;
        internal double RearOpponentOpacity = 100.0;
        internal readonly TrackVisualPoint[] Points = new TrackVisualPoint[TrackMapGeometry.PointCount];

        internal const double DefaultPixelsPerMeter = 3.5;
        internal const double ReferencePlayerWidthPixels = 13.44;
        internal const double ReferencePlayerLengthPixels = 28.0;

        internal static TrackVisualFrame Empty(double roadWidthMeters, double pixelsPerMeter,
            double markerScalePercent = 100.0)
        {
            pixelsPerMeter = Math.Max(1.0, Math.Min(50.0, pixelsPerMeter));
            double zoom = Math.Max(0.0, Math.Min(1.0,
                (pixelsPerMeter - 1.75) / 1.75));
            double markerScale = Math.Max(0.5, Math.Min(2.0, markerScalePercent / 100.0));
            double markerLength = (27.0 + 7.0 * zoom) * markerScale;
            double markerWidth = markerLength * 0.48;
            return new TrackVisualFrame
            {
                PixelsPerMeter = pixelsPerMeter,
                RoadWidthPixels = Math.Max(roadWidthMeters * pixelsPerMeter,
                    roadWidthMeters * DefaultPixelsPerMeter * 0.90),
                PlayerWidthPixels = markerWidth,
                PlayerLengthPixels = markerLength
            };
        }

        internal static double OpponentMarkerOpacity(double centerDistancePixels,
            double markerLengthPixels)
        {
            double progress = markerLengthPixels <= 0.0 ? 1.0 :
                Math.Max(0.0, Math.Min(1.0, centerDistancePixels / markerLengthPixels));
            return 45.0 + 55.0 * progress;
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
