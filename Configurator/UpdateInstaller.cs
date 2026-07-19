using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Threading.Tasks;

namespace IRacingRadarConfigurator
{
    internal static class UpdateInstaller
    {
        internal static async Task BeginAsync(AvailableRelease release, string settingsPath)
        {
            if (release == null || string.IsNullOrEmpty(release.DownloadUrl))
                throw new InvalidOperationException("The release does not contain a downloadable ZIP package.");

            string targetRoot = Path.GetFullPath(AppDomain.CurrentDomain.BaseDirectory);
            string updaterSource = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "IRacingRadar.Updater.exe");
            if (!File.Exists(updaterSource))
                throw new FileNotFoundException("IRacingRadar.Updater.exe was not found.", updaterSource);

            string work = Path.Combine(Path.GetTempPath(), "iRacingRadarUpdate-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(work);
            string package = Path.Combine(work, "update.zip");
            string updater = Path.Combine(work, "IRacingRadar.Updater.exe");
            File.Copy(updaterSource, updater, true);

            ServicePointManager.SecurityProtocol |= (SecurityProtocolType)3072;
            using (UpdateWebClient client = new UpdateWebClient())
            {
                client.Headers[HttpRequestHeader.UserAgent] = "iRacing-Radar-Configurator";
                await client.DownloadFileTaskAsync(new Uri(release.DownloadUrl), package);
            }
            ValidatePackage(package);

            bool restartSimHub = SimHubRestartService.IsRunning();
            string simHubPath = restartSimHub ? SimHubRestartService.FindExecutable(settingsPath) : string.Empty;
            string configuratorPath = Path.Combine(targetRoot, "IRacingRadar.Configurator.exe");
            ProcessStartInfo start = new ProcessStartInfo
            {
                FileName = updater,
                WorkingDirectory = work,
                UseShellExecute = true,
                Arguments = JoinArguments(package, targetRoot, Process.GetCurrentProcess().Id,
                    configuratorPath, restartSimHub, simHubPath, release.Tag)
            };
            Process process = Process.Start(start);
            if (process == null) throw new InvalidOperationException("The updater could not be started.");
            process.Dispose();
        }

        internal static void ValidatePackage(string package)
        {
            string[] required =
            {
                "User.IRacingRadarPlugin.dll",
                "IRacingRadar.Configurator.exe",
                "IRacingRadar.Updater.exe",
                "DashTemplates/iRacing Radar/iRacing Radar.djson",
                "DashTemplates/iRacing Radar/iRacing Radar.djson.ressources"
            };
            using (ZipArchive archive = ZipFile.OpenRead(package))
            {
                foreach (string requiredName in required)
                {
                    bool found = false;
                    foreach (ZipArchiveEntry entry in archive.Entries)
                    {
                        if (Normalize(entry.FullName).Equals(requiredName, StringComparison.OrdinalIgnoreCase))
                        {
                            found = true;
                            break;
                        }
                    }
                    if (!found) throw new InvalidDataException("The update package is missing: " + requiredName);
                }
            }
        }

        private static string JoinArguments(string package, string targetRoot, int processId,
            string configuratorPath, bool restartSimHub, string simHubPath, string tag)
        {
            return Quote(package) + " " + Quote(targetRoot) + " " + processId + " " +
                Quote(configuratorPath) + " " + (restartSimHub ? "1" : "0") + " " +
                Quote(simHubPath ?? string.Empty) + " " + Quote(tag ?? string.Empty);
        }

        private static string Quote(string value)
        {
            return "\"" + (value ?? string.Empty).Replace("\"", "\\\"") + "\"";
        }

        private static string Normalize(string value)
        {
            return (value ?? string.Empty).Replace('\\', '/').TrimStart('/');
        }

        private sealed class UpdateWebClient : WebClient
        {
            protected override WebRequest GetWebRequest(Uri address)
            {
                WebRequest request = base.GetWebRequest(address);
                request.Timeout = 60000;
                HttpWebRequest http = request as HttpWebRequest;
                if (http != null) http.ReadWriteTimeout = 60000;
                return request;
            }
        }
    }
}
