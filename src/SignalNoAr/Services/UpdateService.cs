using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SignalNoAr.Services
{
    public class UpdateInfo
    {
        public bool HasUpdate { get; set; }
        public string LatestVersion { get; set; } = "";
        public string CurrentVersion { get; set; } = UpdateService.AppVersion;
        public string ReleaseNotes { get; set; } = "";
        public string DownloadUrl { get; set; } = "";
        public string ErrorMessage { get; set; } = "";
    }

    public static class UpdateService
    {
        public const string AppVersion = "1.2.2";
        private const string VersionFeedUrl = "https://raw.githubusercontent.com/MathGuthmann/signal-no-ar/main/version.json";
        private static readonly HttpClient _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(20)
        };

        public static Action? OnBeforeRestart { get; set; }

        public static async Task<UpdateInfo> CheckForUpdatesAsync()
        {
            var info = new UpdateInfo();

            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, VersionFeedUrl + $"?nocache={DateTime.UtcNow.Ticks}");
                request.Headers.Add("User-Agent", "SignalNoAr-App-UpdateChecker");

                using var response = await _httpClient.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                {
                    info.ErrorMessage = $"Servidor retornou status HTTP {(int)response.StatusCode}";
                    return info;
                }

                string json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                string remoteVersionStr = root.TryGetProperty("version", out var verProp) ? verProp.GetString() ?? "" : "";
                info.LatestVersion = remoteVersionStr;
                info.ReleaseNotes = root.TryGetProperty("releaseNotes", out var notesProp) ? notesProp.GetString() ?? "" : "";
                info.DownloadUrl = root.TryGetProperty("downloadUrl", out var urlProp) ? urlProp.GetString() ?? "" : "";

                string cleanRemote = remoteVersionStr.TrimStart('v', 'V').Trim();
                string cleanCurrent = AppVersion.TrimStart('v', 'V').Trim();

                if (Version.TryParse(cleanRemote, out var remoteVer) &&
                    Version.TryParse(cleanCurrent, out var currentVer))
                {
                    info.HasUpdate = remoteVer > currentVer;
                }
                else
                {
                    info.HasUpdate = !string.IsNullOrEmpty(cleanRemote) && !cleanRemote.Equals(cleanCurrent, StringComparison.OrdinalIgnoreCase);
                }
            }
            catch (Exception ex)
            {
                info.ErrorMessage = ex.Message;
            }

            return info;
        }

        public static async Task DownloadAndInstallUpdateAsync(string downloadUrl, bool autoMode = false, Action<int>? progressCallback = null)
        {
            string tempInstaller = Path.Combine(Path.GetTempPath(), "Instalador_Signal_No_Ar_Update.exe");

            using (var request = new HttpRequestMessage(HttpMethod.Get, downloadUrl))
            {
                request.Headers.Add("User-Agent", "SignalNoAr-App-Updater");

                using (var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead))
                {
                    response.EnsureSuccessStatusCode();

                    long? totalBytes = response.Content.Headers.ContentLength;

                    using (var contentStream = await response.Content.ReadAsStreamAsync())
                    using (var fileStream = new FileStream(tempInstaller, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true))
                    {
                        var buffer = new byte[16384];
                        long totalRead = 0;
                        int bytesRead;

                        while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                        {
                            await fileStream.WriteAsync(buffer, 0, bytesRead);
                            totalRead += bytesRead;

                            if (totalBytes.HasValue && totalBytes.Value > 0)
                            {
                                int progress = (int)((totalRead * 100) / totalBytes.Value);
                                progressCallback?.Invoke(progress);
                            }
                        }
                    }
                }
            }

            // Launch the installer
            string args = autoMode ? "--auto" : "";
            Process.Start(new ProcessStartInfo
            {
                FileName = tempInstaller,
                Arguments = args,
                UseShellExecute = true
            });

            // Shutdown the current instance cleanly
            Program.IsExplicitExit = true;
            if (OnBeforeRestart != null)
            {
                OnBeforeRestart.Invoke();
            }
            else
            {
                Application.Exit();
            }
        }
    }
}
