using System;
using System.Diagnostics;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace SignalNoAr.Services
{
    public class VmixState
    {
        public bool IsConnected { get; set; }
        public bool IsStreaming { get; set; }
        public bool IsRecording { get; set; }
        public string ActiveInput { get; set; } = "";
        public string PreviewInput { get; set; } = "";
        public string Version { get; set; } = "";
        public string ErrorMessage { get; set; } = "";
        public DateTime? LastChecked { get; set; }
    }

    public class VmixWatcher : IDisposable
    {
        private static readonly HttpClient _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(2)
        };

        private CancellationTokenSource? _cts;
        private Task? _pollTask;
        private readonly Func<bool> _isEnabled;
        private readonly Func<string> _getUrl;
        private readonly Func<int> _getIntervalSec;

        public VmixState CurrentState { get; private set; } = new VmixState();
        public event Action<VmixState>? StateChanged;

        public VmixWatcher(Func<bool> isEnabled, Func<string> getUrl, Func<int> getIntervalSec)
        {
            _isEnabled = isEnabled;
            _getUrl = getUrl;
            _getIntervalSec = getIntervalSec;
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

        public async Task<VmixState> CheckNowAsync(string? overrideUrl = null)
        {
            string url = overrideUrl ?? _getUrl();
            return await QueryVmixAsync(url, CancellationToken.None);
        }

        private async Task PollLoopAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                if (_isEnabled())
                {
                    string url = _getUrl();
                    var newState = await QueryVmixAsync(url, token);

                    bool changed = (newState.IsConnected != CurrentState.IsConnected) ||
                                   (newState.IsStreaming != CurrentState.IsStreaming) ||
                                   (newState.IsRecording != CurrentState.IsRecording) ||
                                   (newState.ActiveInput != CurrentState.ActiveInput);

                    CurrentState = newState;

                    if (changed)
                    {
                        try
                        {
                            StateChanged?.Invoke(CurrentState);
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"[VmixWatcher] Error invoking StateChanged: {ex.Message}");
                        }
                    }
                }
                else
                {
                    if (CurrentState.IsConnected)
                    {
                        CurrentState = new VmixState();
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

        public static async Task<VmixState> QueryVmixAsync(string url, CancellationToken token)
        {
            var state = new VmixState
            {
                LastChecked = DateTime.Now
            };

            if (string.IsNullOrWhiteSpace(url))
            {
                state.ErrorMessage = "URL do vMix não informada";
                return state;
            }

            try
            {
                string xmlContent = await _httpClient.GetStringAsync(url, token);
                var doc = XDocument.Parse(xmlContent);
                var root = doc.Root;

                if (root != null && root.Name.LocalName.Equals("vmix", StringComparison.OrdinalIgnoreCase))
                {
                    state.IsConnected = true;
                    state.Version = root.Element("version")?.Value ?? "";
                    
                    string? strStream = root.Element("streaming")?.Value;
                    state.IsStreaming = string.Equals(strStream, "True", StringComparison.OrdinalIgnoreCase);

                    string? strRec = root.Element("recording")?.Value;
                    state.IsRecording = string.Equals(strRec, "True", StringComparison.OrdinalIgnoreCase);

                    state.ActiveInput = root.Element("active")?.Value ?? "";
                    state.PreviewInput = root.Element("preview")?.Value ?? "";
                }
                else
                {
                    state.ErrorMessage = "Resposta inválida: Tag raiz <vmix> não encontrada";
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
