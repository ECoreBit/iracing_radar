using System;
using System.Net;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace IRacingRadarConfigurator
{
    internal sealed class AvailableRelease
    {
        public Version Version { get; set; }
        public string Tag { get; set; }
        public string Url { get; set; }
        public string DownloadUrl { get; set; }
    }

    internal static class UpdateChecker
    {
        internal const string LatestReleaseApi =
            "https://api.github.com/repos/ECoreBit/iracing_radar/releases/latest";
        internal const string ReleasesPage =
            "https://github.com/ECoreBit/iracing_radar/releases/latest";

        internal static Version CurrentVersion
        {
            get { return Assembly.GetExecutingAssembly().GetName().Version ?? new Version(0, 0); }
        }

        internal static async Task<AvailableRelease> CheckAsync()
        {
            try
            {
                ServicePointManager.SecurityProtocol |= (SecurityProtocolType)3072;
                using (TimeoutWebClient client = new TimeoutWebClient())
                {
                    client.Headers[HttpRequestHeader.UserAgent] = "iRacing-Radar-Configurator";
                    client.Headers[HttpRequestHeader.Accept] = "application/vnd.github+json";
                    string json = await client.DownloadStringTaskAsync(new Uri(LatestReleaseApi));
                    AvailableRelease release = ParseLatestRelease(json);
                    return release != null && release.Version > CurrentVersion ? release : null;
                }
            }
            catch
            {
                return null;
            }
        }

        internal static AvailableRelease ParseLatestRelease(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return null;
            Match tagMatch = Regex.Match(json, "\\\"tag_name\\\"\\s*:\\s*\\\"([^\\\"]+)\\\"", RegexOptions.IgnoreCase);
            if (!tagMatch.Success) return null;
            Version version;
            if (!TryParseVersionTag(tagMatch.Groups[1].Value, out version)) return null;

            Match urlMatch = Regex.Match(json, "\\\"html_url\\\"\\s*:\\s*\\\"([^\\\"]+)\\\"", RegexOptions.IgnoreCase);
            MatchCollection downloadMatches = Regex.Matches(json,
                "\\\"browser_download_url\\\"\\s*:\\s*\\\"([^\\\"]+\\.zip)\\\"", RegexOptions.IgnoreCase);
            string downloadUrl = null;
            foreach (Match downloadMatch in downloadMatches)
            {
                string candidate = downloadMatch.Groups[1].Value.Replace("\\/", "/");
                if (IsTrustedDownloadUrl(candidate))
                {
                    downloadUrl = candidate;
                    break;
                }
            }
            return new AvailableRelease
            {
                Version = version,
                Tag = tagMatch.Groups[1].Value,
                Url = urlMatch.Success ? urlMatch.Groups[1].Value.Replace("\\/", "/") : ReleasesPage,
                DownloadUrl = downloadUrl
            };
        }

        internal static bool IsTrustedDownloadUrl(string value)
        {
            Uri uri;
            return Uri.TryCreate(value, UriKind.Absolute, out uri) &&
                uri.Scheme == Uri.UriSchemeHttps &&
                uri.Host.Equals("github.com", StringComparison.OrdinalIgnoreCase) &&
                uri.AbsolutePath.StartsWith("/ECoreBit/iracing_radar/releases/download/",
                    StringComparison.OrdinalIgnoreCase);
        }

        internal static bool TryParseVersionTag(string tag, out Version version)
        {
            version = null;
            if (string.IsNullOrWhiteSpace(tag)) return false;
            string value = tag.Trim();
            if (value.StartsWith("v", StringComparison.OrdinalIgnoreCase)) value = value.Substring(1);
            int suffix = value.IndexOf('-');
            if (suffix >= 0) value = value.Substring(0, suffix);
            Version parsed;
            if (!Version.TryParse(value, out parsed)) return false;
            version = parsed;
            return true;
        }

        private sealed class TimeoutWebClient : WebClient
        {
            protected override WebRequest GetWebRequest(Uri address)
            {
                WebRequest request = base.GetWebRequest(address);
                request.Timeout = 5000;
                HttpWebRequest http = request as HttpWebRequest;
                if (http != null) http.ReadWriteTimeout = 5000;
                return request;
            }
        }
    }
}