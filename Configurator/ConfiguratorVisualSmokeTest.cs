using System;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;

namespace IRacingRadarConfigurator
{
    internal static class ConfiguratorVisualSmokeTest
    {
        [STAThread]
        private static int Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            string root = args.Length > 0 ? args[0] : AppDomain.CurrentDomain.BaseDirectory;
            string output = args.Length > 1 ? args[1] : Path.Combine(root, "configurator-preview-qa.png");
            string preferences = Path.Combine(Path.GetTempPath(),
                "IRacingRadar-visual-" + Guid.NewGuid().ToString("N") + ".ini");
            using (ConfiguratorForm form = new ConfiguratorForm(Path.Combine(root, "IRacingRadar.settings.ini"), preferences))
            {
                form.Show();
                form.Size = new Size(1240, 820);
                Application.DoEvents();
                if (args.Length > 2 && args[2].Equals("day", StringComparison.OrdinalIgnoreCase))
                {
                    MethodInfo setTheme = typeof(ConfiguratorForm).GetMethod("SetTheme",
                        BindingFlags.Instance | BindingFlags.NonPublic);
                    setTheme.Invoke(form, new object[] { true });
                }
                if (args.Length > 3 && args[3].Equals("front-off", StringComparison.OrdinalIgnoreCase))
                {
                    CheckBox frontArc = (CheckBox)typeof(ConfiguratorForm).GetField("frontArc",
                        BindingFlags.Instance | BindingFlags.NonPublic).GetValue(form);
                    frontArc.Checked = false;
                    Application.DoEvents();
                }
                form.PerformLayout();
                using (Bitmap bitmap = new Bitmap(form.ClientSize.Width, form.ClientSize.Height))
                {
                    form.DrawToBitmap(bitmap, new Rectangle(Point.Empty, bitmap.Size));
                    Color previewBackground = bitmap.GetPixel(bitmap.Width - 80, bitmap.Height - 80);
                    if (previewBackground.R < 25 && previewBackground.G < 25 && previewBackground.B < 25)
                        throw new InvalidOperationException("Preview background is still effectively black.");
                    bitmap.Save(output);
                }
                OverlayRadarPreviewControl preview = (OverlayRadarPreviewControl)typeof(ConfiguratorForm)
                    .GetField("preview", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(form);
                using (Bitmap previewBitmap = new Bitmap(preview.Width, preview.Height))
                {
                    preview.DrawToBitmap(previewBitmap, new Rectangle(Point.Empty, previewBitmap.Size));
                    Color expected = preview.DayMode ? Color.White : Color.FromArgb(24, 29, 38);
                    AssertEdge(previewBitmap, expected);
                }
                form.Close();
            }
            if (File.Exists(preferences)) File.Delete(preferences);
            Console.WriteLine("PASS configurator visual smoke test and preview edge pixels");
            Console.WriteLine(output);
            return 0;
        }
        private static void AssertEdge(Bitmap bitmap, Color expected)
        {
            for (int x = 0; x < bitmap.Width; x++)
            {
                AssertColor(bitmap.GetPixel(x, 0), expected);
                AssertColor(bitmap.GetPixel(x, bitmap.Height - 1), expected);
            }
            for (int y = 0; y < bitmap.Height; y++)
            {
                AssertColor(bitmap.GetPixel(0, y), expected);
                AssertColor(bitmap.GetPixel(bitmap.Width - 1, y), expected);
            }
        }

        private static void AssertColor(Color actual, Color expected)
        {
            if (Math.Abs(actual.R - expected.R) > 4 || Math.Abs(actual.G - expected.G) > 4 ||
                Math.Abs(actual.B - expected.B) > 4)
                throw new InvalidOperationException("Preview outer edge contains a mismatched dark pixel.");
        }
    }
}
