using System;
using System.IO;
using System.IO.Compression;

namespace IRacingRadarUpdater
{
    internal static class RadarUpdaterSyntheticTest
    {
        private static int Main()
        {
            string root = Path.Combine(Path.GetTempPath(), "IRacingRadar-updater-test-" + Guid.NewGuid().ToString("N"));
            string source = Path.Combine(root, "source");
            string target = Path.Combine(root, "target");
            string zip = Path.Combine(root, "update.zip");
            try
            {
                Directory.CreateDirectory(Path.Combine(source, "DashTemplates", "iRacing Radar"));
                Directory.CreateDirectory(target);
                File.WriteAllText(Path.Combine(source, "User.IRacingRadarPlugin.dll"), "new plugin");
                File.WriteAllText(Path.Combine(source, "IRacingRadar.Configurator.exe"), "new configurator");
                File.WriteAllText(Path.Combine(source, "IRacingRadar.Updater.exe"), "new updater");
                File.WriteAllText(Path.Combine(source, "IRacingRadar.settings.ini"), "default settings");
                File.WriteAllText(Path.Combine(source, "DashTemplates", "iRacing Radar", "iRacing Radar.djson"), "new overlay");
                File.WriteAllText(Path.Combine(source, "DashTemplates", "iRacing Radar", "iRacing Radar.djson.ressources"), "new resources");
                ZipFile.CreateFromDirectory(source, zip);

                File.WriteAllText(Path.Combine(target, "User.IRacingRadarPlugin.dll"), "old plugin");
                File.WriteAllText(Path.Combine(target, "IRacingRadar.settings.ini"), "user settings");
                Installer.InstallPackage(zip, target);

                if (File.ReadAllText(Path.Combine(target, "User.IRacingRadarPlugin.dll")) != "new plugin")
                    throw new InvalidOperationException("Plugin was not updated.");
                if (File.ReadAllText(Path.Combine(target, "IRacingRadar.settings.ini")) != "user settings")
                    throw new InvalidOperationException("User settings were overwritten.");
                if (!File.Exists(Path.Combine(target, "DashTemplates", "iRacing Radar", "iRacing Radar.djson.ressources")))
                    throw new InvalidOperationException("Overlay resources were not installed.");

                Console.WriteLine("PASS automatic updater replacement and settings preservation");
                return 0;
            }
            finally
            {
                try { Directory.Delete(root, true); } catch { }
            }
        }
    }
}
