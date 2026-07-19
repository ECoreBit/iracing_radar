using System;
using System.IO;

namespace IRacingRadarConfigurator
{
    internal static class RadarConfiguratorSyntheticTest
    {
        private static int Main()
        {
            Version parsedVersion;
            if (!UpdateChecker.TryParseVersionTag("v1.4.2", out parsedVersion) || parsedVersion != new Version(1, 4, 2))
                throw new InvalidOperationException("GitHub release version parsing failed.");
            AvailableRelease parsedRelease = UpdateChecker.ParseLatestRelease(
                "{\"tag_name\":\"v1.4.0\",\"html_url\":\"https://github.com/ECoreBit/iracing_radar/releases/tag/v1.4.0\",\"assets\":[{\"browser_download_url\":\"https://github.com/ECoreBit/iracing_radar/releases/download/v1.4.0/iracing-radar-v1.4.0.zip\"}]}");
            if (parsedRelease == null || parsedRelease.Version != new Version(1, 4, 0) ||
                !parsedRelease.Url.EndsWith("/v1.4.0", StringComparison.Ordinal) ||
                !parsedRelease.DownloadUrl.EndsWith("iracing-radar-v1.4.0.zip", StringComparison.Ordinal))
                throw new InvalidOperationException("GitHub release response parsing failed.");
            if (UpdateChecker.IsTrustedDownloadUrl("https://example.com/fake.zip"))
                throw new InvalidOperationException("Untrusted update URL was accepted.");
            RadarConfiguratorSettings defaults = RadarConfiguratorSettings.Defaults();
            bool defaultsPass = defaults.DisplayMode == "Both" && defaults.RadarRangeMeters == 70 &&
                defaults.NearDistanceMeters == 20 && defaults.TimeAlertSeconds == 0.7 &&
                defaults.FrontGreenArcEnabled && defaults.RearGreenArcEnabled && defaults.CatchEstimateEnabled &&
                defaults.RadarFadeBandPercent == 15 && defaults.LabelFontSize == 22 && defaults.OverlayOpacity == 92;

            string source = "# keep this comment\nDisplayMode=Time\nRadarRangeMeters=500\n" +
                "NearDistanceMeters=90\nFrontGreenArcEnabled=false\nRearGreenArcEnabled=yes\nCatchEstimateEnabled=false\n" +
                "TimeAlertSeconds=0.4\nRadarFadeBandPercent=80\nLabelFontSize=8\nOverlayOpacity=110\n";
            RadarConfiguratorSettings parsed = RadarConfiguratorSettings.Parse(source);
            bool parsePass = parsed.DisplayMode == "Time" && parsed.RadarRangeMeters == 200 &&
                parsed.NearDistanceMeters == 90 && !parsed.FrontGreenArcEnabled && parsed.RearGreenArcEnabled && !parsed.CatchEstimateEnabled &&
                parsed.RadarFadeBandPercent == 50 && parsed.LabelFontSize == 10 && parsed.OverlayOpacity == 100;

            parsed.RadarRangeMeters = 70;
            parsed.NearDistanceMeters = 20;
            string updated = parsed.UpdateDocument(source);
            bool updatePass = updated.Contains("# keep this comment") && updated.Contains("RadarRangeMeters=70") &&
                updated.Contains("NearDistanceMeters=20") && !updated.Contains("RadarRangeMeters=500");

            string path = Path.Combine(Path.GetTempPath(), "IRacingRadar-configurator-test-" + Guid.NewGuid().ToString("N") + ".ini");
            bool savePass = false;
            bool preferencesPass = false;
            try
            {
                File.WriteAllText(path, source);
                parsed.Save(path);
                RadarConfiguratorSettings saved = RadarConfiguratorSettings.Load(path);
                savePass = saved.RadarRangeMeters == 70 && saved.NearDistanceMeters == 20 &&
                    File.ReadAllText(path).Contains("# keep this comment");
                string preferencesPath = path + ".ui";
                new ConfiguratorPreferences { English = true, DayMode = true }.Save(preferencesPath);
                ConfiguratorPreferences preferences = ConfiguratorPreferences.Load(preferencesPath);
                preferencesPass = preferences.English && preferences.DayMode;
                File.Delete(preferencesPath);
            }
            finally
            {
                if (File.Exists(path)) File.Delete(path);
                if (File.Exists(path + ".tmp")) File.Delete(path + ".tmp");
            }

            double nearStart = RadarOverlayMath.NearStart(defaults);
            bool mathPass = RadarPreviewMath.IsTriggered(defaults, 50, 2) &&
                !RadarPreviewMath.IsTriggered(defaults, 80, 2) &&
                RadarPreviewMath.Opacity(defaults, 70, 2) == 0 &&
                Math.Abs(RadarPreviewMath.CatchSeconds(-20, 5) - 4.0) < 0.001 &&
                double.IsNaN(RadarPreviewMath.CatchSeconds(-20, 1)) &&
                RadarPreviewMath.AppendCatchEstimate(defaults, "20m / 0.4s", true, true, -20, 5).Contains("Catch 4.0s") &&
                RadarOverlayMath.NearProgress(nearStart, nearStart) == 0 &&
                RadarOverlayMath.FrameIndex(100) == 59 &&
                Math.Abs(RadarOverlayMath.SideTop(-6) - 80.2) < 0.01 &&
                Math.Abs(RadarOverlayMath.OvertakeRelative(0) - 9) < 0.01 &&
                Math.Abs(RadarOverlayMath.OvertakeRelative(0.5)) < 0.01 &&
                Math.Abs(RadarOverlayMath.OvertakeRelative(1) + 9) < 0.01;

            Console.WriteLine(defaultsPass ? "PASS configurator defaults" : "FAIL configurator defaults");
            Console.WriteLine(parsePass ? "PASS configurator parsing" : "FAIL configurator parsing");
            Console.WriteLine(updatePass ? "PASS comment-preserving update" : "FAIL comment-preserving update");
            Console.WriteLine(savePass ? "PASS configurator save/reload" : "FAIL configurator save/reload");
            Console.WriteLine(preferencesPass ? "PASS interface preferences save/reload" : "FAIL interface preferences save/reload");
            Console.WriteLine(mathPass ? "PASS overlay preview math and side overtake path" : "FAIL overlay preview math and side overtake path");
            return defaultsPass && parsePass && updatePass && savePass && preferencesPass && mathPass ? 0 : 2;
        }
    }
}
