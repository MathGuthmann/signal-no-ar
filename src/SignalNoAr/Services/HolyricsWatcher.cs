using System;
using System.Diagnostics;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace SignalNoAr.Services
{
    public class HolyricsState
    {
        public bool IsConnected { get; set; }
        public string ServerUrl { get; set; } = "";
        public string ErrorMessage { get; set; } = "";
        public int StatusCode { get; set; } = 0;
        public DateTime? LastChecked { get; set; }

        public string CurrentSongTitle { get; set; } = "";
        public string CurrentLyrics { get; set; } = "";
        public int CurrentSlideIndex { get; set; } = 0;
        public int TotalSlides { get; set; } = 0;
        public string RawDataJson { get; set; } = "";
    }

    public class HolyricsWatcher : IDisposable
    {
        private static readonly HttpClient _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(2.5)
        };

        private CancellationTokenSource? _cts;
        private Task? _pollTask;
        private readonly Func<bool> _isEnabled;
        private readonly Func<string> _getUrl;
        private readonly Func<string> _getToken;
        private readonly Func<int> _getIntervalSec;

        public HolyricsState CurrentState { get; private set; } = new HolyricsState();
        public event Action<HolyricsState>? StateChanged;

        public HolyricsWatcher(
            Func<bool> isEnabled,
            Func<string> getUrl,
            Func<int> getIntervalSec,
            Func<string>? getToken = null)
        {
            _isEnabled = isEnabled;
            _getUrl = getUrl;
            _getIntervalSec = getIntervalSec;
            _getToken = getToken ?? (() => "");
        }

        public void Start()
        {
            Stop();
            _cts = new CancellationTokenSource();
            _pollTask = Task.Run(() => PollLoopAsync(_cts.Token));
        }

        public void Stop()
        {
            if (_cts != null)
            {
                try
                {
                    _cts.Cancel();
                    _cts.Dispose();
                }
                catch { }
                _cts = null;
            }
        }

        public async Task<HolyricsState> CheckNowAsync(string? overrideUrl = null, string? overrideToken = null)
        {
            string url = overrideUrl ?? _getUrl();
            string token = overrideToken ?? _getToken();
            return await QueryHolyricsAsync(url, token, CancellationToken.None);
        }

        private async Task PollLoopAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                if (_isEnabled())
                {
                    string url = _getUrl();
                    string authToken = _getToken();
                    var newState = await QueryHolyricsAsync(url, authToken, token);

                    bool changed = (newState.IsConnected != CurrentState.IsConnected) ||
                                   (newState.ServerUrl != CurrentState.ServerUrl) ||
                                   (newState.CurrentSongTitle != CurrentState.CurrentSongTitle) ||
                                   (newState.CurrentLyrics != CurrentState.CurrentLyrics) ||
                                   (newState.CurrentSlideIndex != CurrentState.CurrentSlideIndex);

                    CurrentState = newState;

                    if (changed)
                    {
                        try
                        {
                            StateChanged?.Invoke(CurrentState);
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"[HolyricsWatcher] Error invoking StateChanged: {ex.Message}");
                        }
                    }
                }
                else
                {
                    if (CurrentState.IsConnected)
                    {
                        CurrentState = new HolyricsState();
                        StateChanged?.Invoke(CurrentState);
                    }
                }

                int delaySec = Math.Max(1, _getIntervalSec());
                try
                {
                    await Task.Delay(delaySec * 1000, token);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }

        public static async Task<HolyricsState> QueryHolyricsAsync(string url, string token, CancellationToken cancellationToken)
        {
            var state = new HolyricsState
            {
                ServerUrl = url,
                LastChecked = DateTime.Now
            };

            if (string.IsNullOrWhiteSpace(url))
            {
                state.ErrorMessage = "URL do Holyrics não informada";
                return state;
            }

            try
            {
                string targetUrl = url.Trim();
                if (!targetUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
                    !targetUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                {
                    targetUrl = "http://" + targetUrl;
                }

                // If user only provided host/port, default to stage-view or root status check
                Uri baseUri = new Uri(targetUrl);

                using var request = new HttpRequestMessage(HttpMethod.Get, targetUrl);

                // Add token headers if provided
                if (!string.IsNullOrWhiteSpace(token))
                {
                    request.Headers.TryAddWithoutValidation("token", token.Trim());
                    request.Headers.TryAddWithoutValidation("Authorization", "Bearer " + token.Trim());
                }

                using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseContentRead, cancellationToken);

                state.StatusCode = (int)response.StatusCode;
                // If it answers HTTP 200-299 or 401/403/404, server is physically alive
                state.IsConnected = response.IsSuccessStatusCode || state.StatusCode == 401 || state.StatusCode == 403;

                if (response.IsSuccessStatusCode)
                {
                    string body = await response.Content.ReadAsStringAsync(cancellationToken);
                    state.RawDataJson = body;

                    // Try parsing JSON if available
                    if (body.TrimStart().StartsWith("{") || body.TrimStart().StartsWith("["))
                    {
                        try
                        {
                            using var doc = JsonDocument.Parse(body);
                            var root = doc.RootElement;

                            if (root.ValueKind == JsonValueKind.Object)
                            {
                                if (root.TryGetProperty("title", out var titleProp))
                                    state.CurrentSongTitle = titleProp.GetString() ?? "";
                                else if (root.TryGetProperty("song", out var songProp) && songProp.TryGetProperty("title", out var sTitle))
                                    state.CurrentSongTitle = sTitle.GetString() ?? "";

                                if (root.TryGetProperty("lyrics", out var lyricsProp))
                                    state.CurrentLyrics = lyricsProp.GetString() ?? "";
                                else if (root.TryGetProperty("text", out var textProp))
                                    state.CurrentLyrics = textProp.GetString() ?? "";

                                if (root.TryGetProperty("slide", out var slideProp) && slideProp.TryGetInt32(out int sIdx))
                                    state.CurrentSlideIndex = sIdx;
                            }
                        }
                        catch { }
                    }
                }
                else if (state.StatusCode == 401 || state.StatusCode == 403)
                {
                    state.ErrorMessage = "Servidor Holyrics encontrado, mas requer permissão de Token";
                }
            }
            catch (Exception ex)
            {
                state.IsConnected = false;
                state.ErrorMessage = ex.Message;
            }

            return state;
        }

        public void Dispose()
        {
            Stop();
        }
    }
}
