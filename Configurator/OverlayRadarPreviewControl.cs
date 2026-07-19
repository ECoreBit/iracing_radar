using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.IO.Compression;
using System.Windows.Forms;

namespace IRacingRadarConfigurator
{
    public sealed class OverlayRadarPreviewControl : Control
    {
        private const float OverlayWidth = 420f;
        private const float OverlayHeight = 260f;
        private readonly Dictionary<string, Bitmap> resources = new Dictionary<string, Bitmap>(StringComparer.OrdinalIgnoreCase);
        private double frontNearProgress;
        private double frontFarProgress;
        private double frontNearBlend;
        private double rearNearProgress;
        private double rearFarProgress;
        private double rearNearBlend;
        private double leftVisualOpacity;
        private double rightVisualOpacity;

        public RadarConfiguratorSettings Settings { get; set; }
        public PreviewScenario Scenario { get; set; }
        public double DistanceMeters { get; set; }
        public double TimeSeconds { get; set; }
        public bool Closing { get; set; }
        public bool English { get; set; }
        public bool DayMode { get; set; }
        public bool AnimateTransitions { get; set; }
        public double TransitionElapsedSeconds { get; set; }
        public string ResourceStatus { get; private set; }

        public OverlayRadarPreviewControl()
        {
            DoubleBuffered = true;
            ResizeRedraw = true;
            BackColor = Color.FromArgb(78, 96, 119);
            Settings = RadarConfiguratorSettings.Defaults();
            Scenario = PreviewScenario.Front;
            DistanceMeters = 35;
            TimeSeconds = 0.5;
            Closing = true;
            TransitionElapsedSeconds = 0.033;
            LoadResources();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                foreach (Bitmap bitmap in resources.Values) bitmap.Dispose();
                resources.Clear();
            }
            base.Dispose(disposing);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
            Color surface = DayMode ? Color.White : Color.FromArgb(24, 29, 38);
            g.Clear(surface);
            if (ClientSize.Width < 4 || ClientSize.Height < 4) return;
            GraphicsState frameState = g.Save();
            using (GraphicsPath frame = RoundedRectanglePath(
                new RectangleF(1, 1, ClientSize.Width - 2, ClientSize.Height - 2), 16))
                g.SetClip(frame);
            DrawBackdrop(g);

            float availableWidth = Math.Max(1, ClientSize.Width - 36);
            float availableHeight = Math.Max(1, ClientSize.Height - 36);
            float scale = Math.Min(availableWidth / OverlayWidth, availableHeight / OverlayHeight);
            float originX = (ClientSize.Width - OverlayWidth * scale) / 2f;
            float originY = (ClientSize.Height - OverlayHeight * scale) / 2f;
            GraphicsState state = g.Save();
            g.TranslateTransform(originX, originY);
            g.ScaleTransform(scale, scale);
            DrawOverlay(g);
            g.Restore(state);

            if (resources.Count < 240)
            {
                using (Font font = new Font("Segoe UI", 9))
                using (Brush brush = new SolidBrush(Color.FromArgb(190, 255, 214, 105)))
                {
                    string status = English ? "Preview resources missing" : "预览资源未加载";
                    SizeF size = g.MeasureString(status, font);
                    g.DrawString(status, font, brush, (ClientSize.Width - size.Width) / 2, ClientSize.Height - 25);
                }
            }
            g.Restore(frameState);
            using (GraphicsPath frame = RoundedRectanglePath(
                new RectangleF(1, 1, ClientSize.Width - 2, ClientSize.Height - 2), 16))
            using (Pen edge = new Pen(surface, 2))
                g.DrawPath(edge, frame);
        }

        private void DrawOverlay(Graphics g)
        {
            bool side = Scenario >= PreviewScenario.LeftBehind;
            if (Scenario == PreviewScenario.NoCars)
                return;

            double proximity = side ? 100 : RadarPreviewMath.Opacity(Settings, DistanceMeters, TimeSeconds);
            double alert = side ? 0 : RadarOverlayMath.AlertProgress(Settings, DistanceMeters, TimeSeconds);
            double nearStart = RadarOverlayMath.NearStart(Settings);
            double nearProgressTarget = side ? 0 : RadarOverlayMath.NearProgress(alert, nearStart);
            double farProgressTarget = side ? 0 : RadarOverlayMath.FarProgress(alert, nearStart);
            double blendTarget = side ? 0 : RadarOverlayMath.NearBlend(Settings, alert);
            bool front = Scenario == PreviewScenario.Front;

            UpdateTransitionState(front, nearProgressTarget, farProgressTarget, blendTarget, side);
            double nearProgress = front ? frontNearProgress : rearNearProgress;
            double farProgress = front ? frontFarProgress : rearFarProgress;
            double blend = front ? frontNearBlend : rearNearBlend;
            if (!AnimateTransitions)
            {
                nearProgress = nearProgressTarget;
                farProgress = farProgressTarget;
                blend = blendTarget;
            }

            bool greenEnabled = front ? Settings.FrontGreenArcEnabled : Settings.RearGreenArcEnabled;
            bool nearVisible = !side && blendTarget > 0.5;
            bool farVisible = !side && blendTarget < 99.5 && greenEnabled;
            double radarOpacity = side ? 100 : greenEnabled
                ? proximity
                : nearVisible ? proximity * blendTarget / 100.0 : 0;
            if (radarOpacity <= 0.1)
                return;

            DrawRoundedRectangle(g, new RectangleF(81, 1, 258, 258), "#52101620",
                62 * Settings.OverlayOpacity / 100.0 * radarOpacity / 100.0,
                129, "#88DDE6EE", 2);

            if (farVisible)
            {
                int frame = RadarOverlayMath.FrameIndex(farProgress) + 1;
                string image = (front ? "FrontGreenArc" : "RearGreenArc") + frame;
                double opacity = (100 - blend) * (50 + proximity * 0.5) / 100.0 *
                    Settings.OverlayOpacity / 100.0 * radarOpacity / 100.0;
                DrawResource(g, image, new RectangleF(80, front ? 0 : 130, 260, 130), opacity);
            }
            if (nearVisible)
            {
                int frame = RadarOverlayMath.FrameIndex(nearProgress) + 1;
                string image = (front ? "FrontFan" : "RearFan") + frame;
                double opacity = (40 + nearProgress * 0.6) * blend / 100.0 *
                    Settings.OverlayOpacity / 100.0 * radarOpacity / 100.0;
                DrawResource(g, image, new RectangleF(80, front ? 0 : 130, 260, 130), opacity);
            }

            if (!side && Math.Abs(DistanceMeters) >= 2.5)
            {
                string text = RadarPreviewMath.DisplayText(Settings, DistanceMeters, TimeSeconds);
                text = RadarPreviewMath.AppendCatchEstimate(Settings, text, front, Closing, DistanceMeters, 8.0);
                if (text.Length > 0 && farVisible)
                {
                    double farTextOpacity = (100 - blend) * proximity / 100.0 *
                        Settings.OverlayOpacity / 100.0 * radarOpacity / 100.0;
                    DrawOverlayText(g, text, new RectangleF(110, front ? 20 : 178, 200, front ? 84 : 40), farTextOpacity);
                }
            }

            FillRectangle(g, new RectangleF(209, 8, 2, 244), "#66D8E1E9",
                40 * Settings.OverlayOpacity / 100.0 * radarOpacity / 100.0);
            FillRectangle(g, new RectangleF(201, 28, 18, 2), "#5CD8E1E9",
                36 * Settings.OverlayOpacity / 100.0 * radarOpacity / 100.0);
            FillRectangle(g, new RectangleF(199, 129, 22, 2), "#78E4EBF2",
                46 * Settings.OverlayOpacity / 100.0 * radarOpacity / 100.0);
            FillRectangle(g, new RectangleF(201, 230, 18, 2), "#5CD8E1E9",
                36 * Settings.OverlayOpacity / 100.0 * radarOpacity / 100.0);

            if (side) DrawSide(g, radarOpacity, SideVisualOpacity());

            if (!side && Math.Abs(DistanceMeters) >= 2.5)
            {
                string text = RadarPreviewMath.DisplayText(Settings, DistanceMeters, TimeSeconds);
                text = RadarPreviewMath.AppendCatchEstimate(Settings, text, front, Closing, DistanceMeters, 8.0);
                if (text.Length > 0 && nearVisible)
                {
                    double textOpacity = blend *
                        Settings.OverlayOpacity / 100.0 * radarOpacity / 100.0;
                    DrawOverlayText(g, text, new RectangleF(110, front ? 20 : 178, 200, front ? 84 : 40), textOpacity);
                }
            }

            DrawRoundedRectangle(g, new RectangleF(201, 109, 18, 42), "#FF727E8A",
                100 * Settings.OverlayOpacity / 100.0 * radarOpacity / 100.0,
                7, "#B8E8EDF2", 1);
        }

        private void DrawSide(Graphics g, double radarOpacity, double sideOpacity)
        {
            bool left = Scenario == PreviewScenario.LeftBehind ||
                Scenario == PreviewScenario.LeftParallel || Scenario == PreviewScenario.LeftAhead;
            double relative = Scenario == PreviewScenario.LeftAhead || Scenario == PreviewScenario.RightAhead
                ? -Math.Abs(DistanceMeters)
                : Scenario == PreviewScenario.LeftBehind || Scenario == PreviewScenario.RightBehind
                    ? Math.Abs(DistanceMeters) : 0;
            float top = (float)RadarOverlayMath.SideTop(relative);
            float railX = left ? 175 : 243;
            float markerX = left ? 167 : 235;
            FillRectangle(g, new RectangleF(railX, 34, 2, 192), "#80D51B2A",
                30 * sideOpacity / 100.0 * Settings.OverlayOpacity / 100.0 * radarOpacity / 100.0);
            DrawRoundedRectangle(g, new RectangleF(markerX, top, 18, 42), "#F0E31B2C",
                92 * sideOpacity / 100.0 * Settings.OverlayOpacity / 100.0 * radarOpacity / 100.0,
                7, "#B8FF7A82", 1);
        }

        private void UpdateTransitionState(bool front, double nearTarget, double farTarget,
            double blendTarget, bool side)
        {
            if (!AnimateTransitions)
            {
                frontNearProgress = front ? nearTarget : 0;
                frontFarProgress = front ? farTarget : 0;
                frontNearBlend = front ? blendTarget : 0;
                rearNearProgress = !front && !side ? nearTarget : 0;
                rearFarProgress = !front && !side ? farTarget : 0;
                rearNearBlend = !front && !side ? blendTarget : 0;
                leftVisualOpacity = IsLeftScenario() ? 100 : 0;
                rightVisualOpacity = IsRightScenario() ? 100 : 0;
                return;
            }

            double elapsed = Math.Max(0, Math.Min(0.25, TransitionElapsedSeconds));
            frontNearProgress = SmoothValue(frontNearProgress, front && !side ? nearTarget : 0, elapsed, 0.12);
            frontFarProgress = SmoothValue(frontFarProgress, front && !side ? farTarget : 0, elapsed, 0.12);
            frontNearBlend = SmoothValue(frontNearBlend, front && !side ? blendTarget : 0, elapsed, 0.12);
            rearNearProgress = SmoothValue(rearNearProgress, !front && !side ? nearTarget : 0, elapsed, 0.12);
            rearFarProgress = SmoothValue(rearFarProgress, !front && !side ? farTarget : 0, elapsed, 0.12);
            rearNearBlend = SmoothValue(rearNearBlend, !front && !side ? blendTarget : 0, elapsed, 0.12);
            leftVisualOpacity = SmoothSide(leftVisualOpacity, IsLeftScenario() ? 100 : 0, elapsed);
            rightVisualOpacity = SmoothSide(rightVisualOpacity, IsRightScenario() ? 100 : 0, elapsed);
        }

        private bool IsLeftScenario()
        {
            return Scenario == PreviewScenario.LeftBehind || Scenario == PreviewScenario.LeftParallel ||
                Scenario == PreviewScenario.LeftAhead;
        }

        private bool IsRightScenario()
        {
            return Scenario == PreviewScenario.RightBehind || Scenario == PreviewScenario.RightParallel ||
                Scenario == PreviewScenario.RightAhead;
        }

        private double SideVisualOpacity()
        {
            return IsLeftScenario() ? leftVisualOpacity : rightVisualOpacity;
        }

        private static double SmoothSide(double current, double target, double elapsed)
        {
            return SmoothValue(current, target, elapsed, target > current ? 0.07 : 0.18);
        }

        private static double SmoothValue(double current, double target, double elapsed, double timeConstant)
        {
            double alpha = 1.0 - Math.Exp(-elapsed / timeConstant);
            return current + (target - current) * alpha;
        }
        private void DrawOverlayText(Graphics g, string text, RectangleF area, double opacity)
        {
            Color baseColor = Closing ? Color.FromArgb(255, 255, 52, 69) : Color.FromArgb(255, 80, 233, 135);
            Color color = Color.FromArgb(ScaleAlpha(baseColor.A, opacity), baseColor.R, baseColor.G, baseColor.B);
            using (Font font = new Font("Segoe UI", (float)Settings.LabelFontSize, FontStyle.Bold, GraphicsUnit.Pixel))
            using (Brush brush = new SolidBrush(color))
            using (StringFormat format = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                g.DrawString(text, font, brush, area, format);
        }

        private void DrawRoundedRectangle(Graphics g, RectangleF rectangle, string backgroundHex,
            double opacity, float radius, string borderHex, float borderWidth)
        {
            using (GraphicsPath path = RoundedRectanglePath(rectangle, radius))
            {
                Color background = ParseColor(backgroundHex);
                using (Brush brush = new SolidBrush(Color.FromArgb(
                    ScaleAlpha(background.A, opacity), background.R, background.G, background.B)))
                    g.FillPath(brush, path);

                if (borderWidth > 0)
                {
                    Color border = ParseColor(borderHex);
                    using (Pen pen = new Pen(Color.FromArgb(
                        ScaleAlpha(border.A, opacity), border.R, border.G, border.B), borderWidth))
                        g.DrawPath(pen, path);
                }
            }
        }

        private static GraphicsPath RoundedRectanglePath(RectangleF rectangle, float radius)
        {
            GraphicsPath path = new GraphicsPath();
            float diameter = Math.Min(Math.Min(radius * 2, rectangle.Width), rectangle.Height);
            if (diameter <= 0)
            {
                path.AddRectangle(rectangle);
                path.CloseFigure();
                return path;
            }
            RectangleF arc = new RectangleF(rectangle.X, rectangle.Y, diameter, diameter);
            path.AddArc(arc, 180, 90);
            arc.X = rectangle.Right - diameter;
            path.AddArc(arc, 270, 90);
            arc.Y = rectangle.Bottom - diameter;
            path.AddArc(arc, 0, 90);
            arc.X = rectangle.X;
            path.AddArc(arc, 90, 90);
            path.CloseFigure();
            return path;
        }
        private void FillRectangle(Graphics g, RectangleF rectangle, string hex, double opacity)
        {
            Color color = ParseColor(hex);
            using (Brush brush = new SolidBrush(Color.FromArgb(ScaleAlpha(color.A, opacity), color.R, color.G, color.B)))
                g.FillRectangle(brush, rectangle);
        }

        private void DrawResource(Graphics g, string name, RectangleF destination, double opacity)
        {
            Bitmap bitmap;
            if (!resources.TryGetValue(name, out bitmap)) return;
            float alpha = (float)Math.Max(0, Math.Min(1, opacity / 100.0));
            using (ImageAttributes attributes = new ImageAttributes())
            {
                ColorMatrix matrix = new ColorMatrix();
                matrix.Matrix33 = alpha;
                attributes.SetColorMatrix(matrix, ColorMatrixFlag.Default, ColorAdjustType.Bitmap);
                g.DrawImage(bitmap, Rectangle.Round(destination), 0, 0, bitmap.Width, bitmap.Height,
                    GraphicsUnit.Pixel, attributes);
            }
        }

        private void DrawBackdrop(Graphics g)
        {
            Color top = DayMode ? Color.FromArgb(226, 234, 243) : Color.FromArgb(126, 145, 168);
            Color bottom = DayMode ? Color.FromArgb(174, 190, 208) : Color.FromArgb(70, 84, 103);
            using (LinearGradientBrush background = new LinearGradientBrush(ClientRectangle,
                top, bottom, LinearGradientMode.Vertical))
                g.FillRectangle(background, ClientRectangle);

        }
        private void LoadResources()
        {
            string archivePath = FindArchive();
            if (archivePath == null)
            {
                ResourceStatus = "resource archive not found";
                return;
            }
            try
            {
                using (ZipArchive archive = ZipFile.OpenRead(archivePath))
                {
                    foreach (ZipArchiveEntry entry in archive.Entries)
                    {
                        if (!entry.Name.EndsWith(".png", StringComparison.OrdinalIgnoreCase)) continue;
                        using (Stream stream = entry.Open())
                        using (Image image = Image.FromStream(stream))
                            resources[Path.GetFileNameWithoutExtension(entry.Name)] = new Bitmap(image);
                    }
                }
                ResourceStatus = archivePath;
            }
            catch (Exception ex)
            {
                ResourceStatus = ex.GetType().Name;
            }
        }

        private static string FindArchive()
        {
            DirectoryInfo directory = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
            for (int i = 0; i < 7 && directory != null; i++, directory = directory.Parent)
            {
                string[] candidates =
                {
                    Path.Combine(directory.FullName, "DashTemplates", "iRacing Radar", "iRacing Radar.djson.ressources"),
                    Path.Combine(directory.FullName, "SimHubPlugin", "Overlay", "iRacing Radar.djson.ressources"),
                    Path.Combine(directory.FullName, "iRacing Radar.djson.ressources")
                };
                foreach (string candidate in candidates) if (File.Exists(candidate)) return candidate;
            }
            return null;
        }

        private static int ScaleAlpha(int alpha, double opacityPercent)
        {
            return Math.Max(0, Math.Min(255, (int)Math.Round(alpha * Math.Max(0, Math.Min(100, opacityPercent)) / 100.0)));
        }

        private static Color ParseColor(string value)
        {
            string hex = value.TrimStart('#');
            if (hex.Length != 8) return Color.Transparent;
            return Color.FromArgb(Convert.ToInt32(hex.Substring(0, 2), 16),
                Convert.ToInt32(hex.Substring(2, 2), 16), Convert.ToInt32(hex.Substring(4, 2), 16),
                Convert.ToInt32(hex.Substring(6, 2), 16));
        }
    }
}
