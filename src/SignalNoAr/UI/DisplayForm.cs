using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using SignalNoAr.Models;
using SignalNoAr.Services;

namespace SignalNoAr.UI
{
    public class DisplayForm : Form
    {
        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_TOOLWINDOW = 0x00000080;
        private const int WS_EX_APPWINDOW = 0x00040000;

        [DllImport("user32.dll", EntryPoint = "GetWindowLong")]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", EntryPoint = "SetWindowLong")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        private readonly AppConfig _config;
        private readonly AppContext _appContext;
        private readonly string _targetUrl;
        private readonly string _displayRole;

        private WebView2? _webView;
        private Panel? _loadingPanel;
        private Panel? _topControlBar;
        private Panel? _topHoverTrigger;
        private Label? _lblStatus;
        private Label? _lblSubStatus;
        private Label? _lblLocalEquipmentStatus;
        private PictureBox? _picLogo;
        private Button? _btnManualRetry;
        private Button? _btnInstallWebView2;
        private System.Windows.Forms.Timer? _retryTimer;
        private System.Windows.Forms.Timer? _watchdogTimer;
        private Screen _targetScreen;
        private bool _isNavigating = false;
        private int _retryCount = 0;
        private int _countdown = 3;
        private bool _isWindowed = false;

        private bool _lastVmixStreaming = false;
        private bool _lastVmixRecording = false;
        private bool _lastHolyricsConnected = false;

        public string DisplayRole => _displayRole;

        public DisplayForm(
            AppConfig config,
            AppContext appContext,
            Screen targetScreen,
            bool runWindowed = false,
            string? customUrl = null,
            string displayRole = "No Ar")
        {
            _config = config;
            _appContext = appContext;
            _targetScreen = targetScreen;
            _isWindowed = runWindowed;
            _displayRole = string.IsNullOrWhiteSpace(displayRole) ? "No Ar" : displayRole;
            _targetUrl = string.IsNullOrWhiteSpace(customUrl) ? config.Url : customUrl.Trim();

            InitializeForm();
            InitializeControls();
            InitializeTopControlBar();
            InitializeWebViewAsync();
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                if (!_isWindowed)
                {
                    // WS_EX_TOOLWINDOW (0x80) removes window completely from Alt+Tab switcher
                    cp.ExStyle |= WS_EX_TOOLWINDOW;
                    // Ensure WS_EX_APPWINDOW (0x40000) is cleared
                    cp.ExStyle &= ~WS_EX_APPWINDOW;
                }
                return cp;
            }
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            ApplyAltTabVisibility();
        }

        private void ApplyAltTabVisibility()
        {
            try
            {
                if (IsHandleCreated)
                {
                    int exStyle = GetWindowLong(Handle, GWL_EXSTYLE);
                    if (!_isWindowed)
                    {
                        exStyle |= WS_EX_TOOLWINDOW;
                        exStyle &= ~WS_EX_APPWINDOW;
                    }
                    else
                    {
                        exStyle &= ~WS_EX_TOOLWINDOW;
                        exStyle |= WS_EX_APPWINDOW;
                    }
                    SetWindowLong(Handle, GWL_EXSTYLE, exStyle);
                }
            }
            catch { }
        }

        private void InitializeForm()
        {
            if (_isWindowed)
            {
                FormBorderStyle = FormBorderStyle.Sizable;
                StartPosition = FormStartPosition.CenterScreen;
                Size = new Size(1040, 620);
                ShowInTaskbar = true;
                Text = $"Signal No Ar — Prévia ({_displayRole})";
            }
            else
            {
                FormBorderStyle = FormBorderStyle.None;
                StartPosition = FormStartPosition.Manual;
                Bounds = _targetScreen.Bounds;
                ShowInTaskbar = false;
                Text = $"Signal No Ar — {_displayRole}";
            }

            TopMost = false;
            BackColor = Color.FromArgb(14, 14, 20); // Signal Dark Slate
            KeyPreview = true;

            KeyDown += DisplayForm_KeyDown;
        }

        private void InitializeControls()
        {
            _loadingPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(14, 14, 20),
                Visible = true
            };

            var centerBox = new Panel
            {
                Size = new Size(540, 310),
                BackColor = Color.Transparent
            };

            _loadingPanel.Resize += (s, e) =>
            {
                centerBox.Location = new Point(
                    (_loadingPanel.Width - centerBox.Width) / 2,
                    (_loadingPanel.Height - centerBox.Height) / 2
                );
            };

            // Signal Logo
            _picLogo = new PictureBox
            {
                Dock = DockStyle.Top,
                Height = 65,
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.Transparent
            };

            try
            {
                using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("SignalNoAr.logo.png");
                if (stream != null)
                {
                    _picLogo.Image = Image.FromStream(stream);
                }
            }
            catch { }

            var pnlBadgeContainer = new Panel
            {
                Dock = DockStyle.Top,
                Height = 32,
                BackColor = Color.Transparent
            };

            string badgeText = _displayRole == "Palco" ? "🎤 MONITOR DE PALCO" : "🔴 NO AR";
            var lblBadge = new Label
            {
                Text = badgeText,
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                ForeColor = Color.FromArgb(255, 120, 0),
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill
            };
            pnlBadgeContainer.Controls.Add(lblBadge);

            _lblStatus = new Label
            {
                Text = "Conectando ao sinal...",
                Font = new Font("Segoe UI", 13, FontStyle.Bold),
                ForeColor = Color.White,
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Top,
                Height = 36
            };

            _lblSubStatus = new Label
            {
                Text = "Carregando sinal em tela cheia...",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Regular),
                ForeColor = Color.FromArgb(180, 175, 200),
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Top,
                Height = 28
            };

            _lblLocalEquipmentStatus = new Label
            {
                Text = "Rede Local: Aguardando verificação...",
                Font = new Font("Segoe UI", 9f, FontStyle.Regular),
                ForeColor = Color.FromArgb(145, 138, 175),
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Top,
                Height = 26
            };

            _btnManualRetry = new Button
            {
                Text = "Reconectar Agora",
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Size = new Size(160, 32),
                Location = new Point((centerBox.Width - 160) / 2, 196),
                BackColor = Color.FromArgb(255, 110, 0),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Visible = false
            };
            _btnManualRetry.FlatAppearance.BorderSize = 0;
            _btnManualRetry.Click += (s, e) => NavigateToUrl();
            centerBox.Controls.Add(_btnManualRetry);

            _btnInstallWebView2 = new Button
            {
                Text = "📦 Baixar e Instalar WebView2 Runtime",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Size = new Size(290, 36),
                Location = new Point((centerBox.Width - 290) / 2, 194),
                BackColor = Color.FromArgb(40, 160, 80),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Visible = false
            };
            _btnInstallWebView2.FlatAppearance.BorderSize = 0;
            _btnInstallWebView2.Click += async (s, e) => await InstallWebView2RuntimeAsync();
            centerBox.Controls.Add(_btnInstallWebView2);

            var lblEscHint = new Label
            {
                Text = "💡 [ESC] Sair da tela cheia • [F11] Janela • [F5] Recarregar",
                Font = new Font("Segoe UI", 9.2f, FontStyle.Regular),
                ForeColor = Color.FromArgb(255, 195, 0),
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Bottom,
                Height = 32
            };

            var lblMonitorInfo = new Label
            {
                Text = $"Monitor: {_targetScreen.DeviceName} ({_targetScreen.Bounds.Width}x{_targetScreen.Bounds.Height})",
                Font = new Font("Segoe UI", 8.5f, FontStyle.Italic),
                ForeColor = Color.FromArgb(140, 130, 160),
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Bottom,
                Height = 24
            };

            centerBox.Controls.Add(lblMonitorInfo);
            centerBox.Controls.Add(lblEscHint);
            centerBox.Controls.Add(_lblLocalEquipmentStatus);
            centerBox.Controls.Add(_lblSubStatus);
            centerBox.Controls.Add(_lblStatus);
            centerBox.Controls.Add(pnlBadgeContainer);
            centerBox.Controls.Add(_picLogo);

            _loadingPanel.Controls.Add(centerBox);
            Controls.Add(_loadingPanel);

            // Retry timer (runs every 1s for countdown)
            _retryTimer = new System.Windows.Forms.Timer { Interval = 1000 };
            _retryTimer.Tick += RetryTimer_Tick;

            // Watchdog timer (every 10s checks bounds)
            _watchdogTimer = new System.Windows.Forms.Timer { Interval = 10000 };
            _watchdogTimer.Tick += WatchdogTimer_Tick;
            _watchdogTimer.Start();
        }

        private void InitializeTopControlBar()
        {
            _topHoverTrigger = new Panel
            {
                Dock = DockStyle.Top,
                Height = 4,
                BackColor = Color.Transparent
            };
            _topHoverTrigger.MouseEnter += (s, e) => ShowTopControlBar();
            Controls.Add(_topHoverTrigger);
            _topHoverTrigger.BringToFront();

            _topControlBar = new Panel
            {
                Height = 42,
                Width = 430,
                BackColor = Color.FromArgb(235, 22, 20, 32),
                Visible = false
            };

            var btnExitFs = new Button
            {
                Text = "🚪 Sair da Tela Cheia [ESC]",
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                BackColor = Color.FromArgb(45, 38, 60),
                ForeColor = Color.FromArgb(255, 200, 100),
                FlatStyle = FlatStyle.Flat,
                Size = new Size(180, 30),
                Location = new Point(6, 6),
                Cursor = Cursors.Hand
            };
            btnExitFs.FlatAppearance.BorderColor = Color.FromArgb(100, 70, 140);
            btnExitFs.Click += (s, e) => SafeExitFullscreen();

            var btnOpenSettings = new Button
            {
                Text = "⚙️ Painel",
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                BackColor = Color.FromArgb(255, 110, 0),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(110, 30),
                Location = new Point(192, 6),
                Cursor = Cursors.Hand
            };
            btnOpenSettings.FlatAppearance.BorderSize = 0;
            btnOpenSettings.Click += (s, e) =>
            {
                _topControlBar.Visible = false;
                _appContext.ShowSettings();
            };

            bool isPalco = _displayRole.Equals("Palco", StringComparison.OrdinalIgnoreCase);
            var btnToggleMode = new Button
            {
                Text = isPalco ? "🔴 Ir para No Ar" : "🎤 Ir para Palco",
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                BackColor = Color.FromArgb(46, 38, 64),
                ForeColor = Color.FromArgb(255, 195, 100),
                FlatStyle = FlatStyle.Flat,
                Size = new Size(140, 30),
                Location = new Point(310, 6),
                Cursor = Cursors.Hand
            };
            btnToggleMode.FlatAppearance.BorderColor = Color.FromArgb(80, 60, 110);
            btnToggleMode.Click += (s, e) =>
            {
                _topControlBar.Visible = false;
                ToggleNoArPalcoMode();
            };

            var lblRoleBadge = new Label
            {
                Text = $"[{_displayRole.ToUpper()}]",
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = Color.FromArgb(170, 160, 210),
                Location = new Point(460, 11),
                AutoSize = true
            };

            _topControlBar.Width = 560;
            _topControlBar.Controls.Add(btnExitFs);
            _topControlBar.Controls.Add(btnOpenSettings);
            _topControlBar.Controls.Add(btnToggleMode);
            _topControlBar.Controls.Add(lblRoleBadge);

            _topControlBar.MouseLeave += (s, e) =>
            {
                Point clientPos = PointToClient(Cursor.Position);
                if (clientPos.Y > _topControlBar.Height + 5 || clientPos.Y < 0)
                {
                    _topControlBar.Visible = false;
                }
            };

            Controls.Add(_topControlBar);
        }

        private void ShowTopControlBar()
        {
            if (!_isWindowed && _topControlBar != null)
            {
                _topControlBar.Location = new Point((Width - _topControlBar.Width) / 2, 0);
                _topControlBar.Visible = true;
                _topControlBar.BringToFront();
            }
        }

        private async void InitializeWebViewAsync()
        {
            try
            {
                UpdateStatus("Iniciando navegador...", "Configurando ambiente seguro em tela cheia...", isError: false);

                if (_webView == null)
                {
                    _webView = new WebView2
                    {
                        Dock = DockStyle.Fill,
                        Visible = false,
                        DefaultBackgroundColor = Color.FromArgb(14, 14, 20)
                    };
                    _webView.MouseMove += (s, e) =>
                    {
                        if (!_isWindowed && e.Y <= 15)
                        {
                            ShowTopControlBar();
                        }
                        else if (!_isWindowed && e.Y > 60 && _topControlBar != null && _topControlBar.Visible)
                        {
                            _topControlBar.Visible = false;
                        }
                    };

                    Controls.Add(_webView);
                    _loadingPanel?.BringToFront();
                    _topHoverTrigger?.BringToFront();
                }

                string profileFolder = _displayRole == "Palco" ? "WebViewProfile_Palco" : "WebViewProfile";
                string profilePath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "SignalNoAr",
                    profileFolder
                );

                CoreWebView2Environment? env = null;

                try
                {
                    var options = new CoreWebView2EnvironmentOptions(
                        "--disable-background-networking --disable-sync --disable-features=TranslateUI,InterestFeedContentSuggestions,BlockInsecurePrivateNetworkRequests --disable-extensions --allow-running-insecure-content --disable-web-security"
                    );
                    env = await CoreWebView2Environment.CreateAsync(null, profilePath, options);
                }
                catch
                {
                    try
                    {
                        env = await CoreWebView2Environment.CreateAsync(null, profilePath, null);
                    }
                    catch
                    {
                        env = null;
                    }
                }

                await _webView.EnsureCoreWebView2Async(env);

                if (_webView.CoreWebView2 != null)
                {
                    _webView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
                    _webView.CoreWebView2.Settings.AreDevToolsEnabled = false;
                    _webView.CoreWebView2.Settings.IsStatusBarEnabled = false;
                    _webView.CoreWebView2.Settings.IsZoomControlEnabled = false;
                    _webView.DefaultBackgroundColor = Color.Black;

                    try
                    {
                        await _webView.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(
                            "window.__SIGNAL_DESKTOP_APP__ = true; window.__SIGNAL_VERSION__ = '" + UpdateService.CurrentVersion + "';"
                        );
                    }
                    catch { }

                    _webView.CoreWebView2.NavigationStarting += CoreWebView2_NavigationStarting;
                    _webView.CoreWebView2.NavigationCompleted += CoreWebView2_NavigationCompleted;
                    _webView.CoreWebView2.ProcessFailed += CoreWebView2_ProcessFailed;

                    if (_btnInstallWebView2 != null) _btnInstallWebView2.Visible = false;
                    NavigateToUrl();
                }
            }
            catch (Exception ex)
            {
                bool isMissingRuntime = ex is FileNotFoundException ||
                                        ex.GetType().Name.Contains("WebView2RuntimeNotFound") ||
                                        ex.Message.Contains("WebView2") ||
                                        ex.Message.Contains("0x80070002") ||
                                        ex.Message.Contains("não encontrado") ||
                                        ex.Message.Contains("not found");

                if (isMissingRuntime)
                {
                    UpdateStatus(
                        "Componente WebView2 Necessário",
                        "O computador precisa do Microsoft Edge WebView2 Runtime para exibir a página.",
                        isError: true
                    );

                    if (_btnInstallWebView2 != null)
                    {
                        _btnInstallWebView2.Visible = true;
                    }
                }
                else
                {
                    UpdateStatus("Erro ao iniciar visualizador", ex.Message, isError: true);
                }

                _retryCount++;
                _countdown = 5;
                if (_btnManualRetry != null) _btnManualRetry.Visible = true;
                _retryTimer?.Start();
            }
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            // ESC or Ctrl+Q -> Safe exit fullscreen
            if (keyData == Keys.Escape || keyData == (Keys.Control | Keys.Q))
            {
                SafeExitFullscreen();
                return true;
            }

            // F11 -> Toggle windowed mode
            if (keyData == Keys.F11)
            {
                ToggleWindowedMode();
                return true;
            }

            // Ctrl+Shift+S -> Open Settings
            if (keyData == (Keys.Control | Keys.Shift | Keys.S))
            {
                _appContext.ShowSettings();
                return true;
            }

            // Block Alt+F4 and Ctrl+W
            if (keyData == (Keys.Alt | Keys.F4) || keyData == (Keys.Control | Keys.W) || keyData == (Keys.Control | Keys.F4))
            {
                return true;
            }

            // F5 or Ctrl+R -> Reload
            if (keyData == Keys.F5 || keyData == (Keys.Control | Keys.R))
            {
                Reload();
                return true;
            }

            return base.ProcessCmdKey(ref msg, keyData);
        }

        private async Task InstallWebView2RuntimeAsync()
        {
            if (_btnInstallWebView2 != null)
            {
                _btnInstallWebView2.Enabled = false;
                _btnInstallWebView2.Text = "⏳ Baixando instalador da Microsoft...";
            }

            UpdateStatus("Baixando WebView2 Runtime...", "Aguarde o download do instalador oficial da Microsoft...", isError: false);

            try
            {
                string tempInstaller = Path.Combine(Path.GetTempPath(), "MicrosoftEdgeWebview2Setup.exe");
                using (var client = new System.Net.Http.HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(60);
                    client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) SignalNoAr");
                    var bytes = await client.GetByteArrayAsync("https://go.microsoft.com/fwlink/p/?LinkId=2124703");
                    await File.WriteAllBytesAsync(tempInstaller, bytes);
                }

                UpdateStatus("Instalando WebView2 Runtime...", "Executando instalação silenciosa da Microsoft...", isError: false);

                var psi = new ProcessStartInfo
                {
                    FileName = tempInstaller,
                    Arguments = "/silent /install",
                    UseShellExecute = true
                };
                var proc = Process.Start(psi);
                if (proc != null)
                {
                    await proc.WaitForExitAsync();
                }

                UpdateStatus("Concluído!", "Reiniciando navegador do sinal...", isError: false);
                await Task.Delay(1000);
                InitializeWebViewAsync();
            }
            catch (Exception ex)
            {
                UpdateStatus("Falha na instalação automática", ex.Message, isError: true);
                if (_btnInstallWebView2 != null)
                {
                    _btnInstallWebView2.Enabled = true;
                    _btnInstallWebView2.Text = "Tentar Instalar Novamente";
                }
            }
        }

        public void NavigateToUrl()
        {
            if (_webView?.CoreWebView2 != null)
            {
                UpdateStatus("Carregando sinal...", _targetUrl, isError: false);
                if (_loadingPanel != null) _loadingPanel.Visible = true;
                _webView.CoreWebView2.Navigate(_targetUrl);
            }
        }

        public void Reload()
        {
            if (_webView?.CoreWebView2 != null)
            {
                _webView.CoreWebView2.Reload();
            }
            else
            {
                NavigateToUrl();
            }
        }

        public void ToggleWindowedMode()
        {
            _isWindowed = !_isWindowed;
            if (_isWindowed)
            {
                FormBorderStyle = FormBorderStyle.Sizable;
                WindowState = FormWindowState.Normal;
                Size = new Size(1040, 620);
                CenterToScreen();
                ShowInTaskbar = true;
                if (_topControlBar != null) _topControlBar.Visible = false;
            }
            else
            {
                FormBorderStyle = FormBorderStyle.None;
                Bounds = _targetScreen.Bounds;
                ShowInTaskbar = false;
            }
            ApplyAltTabVisibility();
        }

        public void ToggleNoArPalcoMode()
        {
            if (_webView?.CoreWebView2 != null)
            {
                string currentUri = _webView.Source?.ToString() ?? _targetUrl;
                bool isCurrentlyPalco = currentUri.Contains("/palco", StringComparison.OrdinalIgnoreCase);
                string target = isCurrentlyPalco ? _config.Url : _config.SecondDisplayUrl;
                UpdateStatus("Alternando modo...", target, isError: false);
                _webView.CoreWebView2.Navigate(target);
            }
        }

        public void RelocateToScreen(Screen newScreen)
        {
            _targetScreen = newScreen;
            if (!_isWindowed)
            {
                Bounds = _targetScreen.Bounds;
            }
        }

        public void SafeExitFullscreen()
        {
            _appContext.DeactivateTemporarily();
        }

        private void CoreWebView2_NavigationStarting(object? sender, CoreWebView2NavigationStartingEventArgs e)
        {
            _isNavigating = true;
        }

        private void CoreWebView2_NavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            _isNavigating = false;
            if (e.IsSuccess)
            {
                _retryCount = 0;
                _retryTimer?.Stop();
                if (_btnManualRetry != null) _btnManualRetry.Visible = false;
                if (_loadingPanel != null) _loadingPanel.Visible = false;
                if (_webView != null) _webView.Visible = true;

                // Push current vMix and Holyrics status immediately to newly loaded page
                var vmixState = _appContext.VmixWatcher.CurrentState;
                NotifyVmixStatus(vmixState.IsStreaming, vmixState.IsRecording, vmixState.ActiveInput);

                var holyricsState = _appContext.HolyricsWatcher.CurrentState;
                NotifyHolyricsStatus(
                    holyricsState.IsConnected,
                    holyricsState.ServerUrl,
                    holyricsState.CurrentSongTitle,
                    holyricsState.CurrentLyrics,
                    holyricsState.CurrentSlideIndex,
                    holyricsState.RawDataJson
                );
            }
            else
            {
                _retryCount++;
                _countdown = 3;
                if (_btnManualRetry != null) _btnManualRetry.Visible = true;
                UpdateStatus("Aguardando conexão com o servidor...", $"Tentativa #{_retryCount} • Próxima tentativa em {_countdown}s...", isError: true);
                if (_loadingPanel != null) _loadingPanel.Visible = true;
                if (_webView != null) _webView.Visible = false;
                _retryTimer?.Start();
            }
        }

        private void CoreWebView2_ProcessFailed(object? sender, CoreWebView2ProcessFailedEventArgs e)
        {
            UpdateStatus("Recuperando sinal...", "O processo do navegador foi reiniciado automaticamente.", isError: true);
            _webView?.Reload();
        }

        private void RetryTimer_Tick(object? sender, EventArgs e)
        {
            if (!_isNavigating)
            {
                _countdown--;
                if (_lblSubStatus != null)
                {
                    _lblSubStatus.Text = $"Tentativa #{_retryCount} • Próxima tentativa em {_countdown}s...";
                }

                if (_countdown <= 0)
                {
                    _countdown = 3;
                    if (_webView?.CoreWebView2 != null)
                    {
                        NavigateToUrl();
                    }
                    else
                    {
                        InitializeWebViewAsync();
                    }
                }
            }
        }

        private void WatchdogTimer_Tick(object? sender, EventArgs e)
        {
            if (!_isWindowed && Bounds != _targetScreen.Bounds)
            {
                Bounds = _targetScreen.Bounds;
            }
        }

        private void UpdateStatus(string title, string sub, bool isError)
        {
            if (_lblStatus != null)
            {
                _lblStatus.Text = title;
                _lblStatus.ForeColor = isError ? Color.FromArgb(255, 100, 100) : Color.White;
            }
            if (_lblSubStatus != null)
            {
                _lblSubStatus.Text = sub;
            }
        }

        private void UpdateEquipmentStatusPill()
        {
            if (_lblLocalEquipmentStatus == null || _lblLocalEquipmentStatus.IsDisposed) return;

            string vmixText = _lastVmixStreaming ? "🟢 vMix: No Ar" : (_lastVmixRecording ? "🔴 vMix: Gravando" : "⚪ vMix: Pronto");
            string holyricsText = _lastHolyricsConnected ? "🟢 Holyrics: Conectado" : "⚪ Holyrics: Offline";

            if (InvokeRequired)
            {
                try
                {
                    BeginInvoke(new Action(() =>
                    {
                        if (_lblLocalEquipmentStatus != null && !_lblLocalEquipmentStatus.IsDisposed)
                        {
                            _lblLocalEquipmentStatus.Text = $"Rede Local: {vmixText}   |   {holyricsText}";
                        }
                    }));
                }
                catch { }
            }
            else
            {
                _lblLocalEquipmentStatus.Text = $"Rede Local: {vmixText}   |   {holyricsText}";
            }
        }

        private void DisplayForm_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape || (e.Control && e.KeyCode == Keys.Q))
            {
                e.Handled = true;
                SafeExitFullscreen();
                return;
            }

            if (e.KeyCode == Keys.F11)
            {
                e.Handled = true;
                ToggleWindowedMode();
                return;
            }

            if (e.Control && e.Shift && e.KeyCode == Keys.S)
            {
                e.Handled = true;
                _appContext.ShowSettings();
                return;
            }

            // Block Alt+F4 and Ctrl+W
            if (e.Alt && e.KeyCode == Keys.F4)
            {
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
            else if (e.Control && (e.KeyCode == Keys.W || e.KeyCode == Keys.F4))
            {
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
            else if (e.KeyCode == Keys.F5 || (e.Control && e.KeyCode == Keys.R))
            {
                Reload();
            }
        }

        public void NotifyVmixStatus(bool isStreaming, bool isRecording, string activeInput)
        {
            _lastVmixStreaming = isStreaming;
            _lastVmixRecording = isRecording;
            UpdateEquipmentStatusPill();

            if (InvokeRequired)
            {
                try
                {
                    Invoke(new Action(() => NotifyVmixStatus(isStreaming, isRecording, activeInput)));
                }
                catch { }
                return;
            }

            if (_webView?.CoreWebView2 != null)
            {
                try
                {
                    var payload = JsonSerializer.Serialize(new
                    {
                        type = "VMIX_STATUS",
                        isStreaming = isStreaming,
                        isRecording = isRecording,
                        activeInput = activeInput ?? ""
                    });
                    _webView.CoreWebView2.ExecuteScriptAsync($"window.postMessage({payload}, '*');");
                }
                catch { }
            }
        }

        public void NotifyHolyricsStatus(
            bool isConnected,
            string url,
            string? title = null,
            string? lyrics = null,
            int slideIndex = 0,
            string? rawJson = null)
        {
            _lastHolyricsConnected = isConnected;
            UpdateEquipmentStatusPill();

            if (InvokeRequired)
            {
                try
                {
                    Invoke(new Action(() => NotifyHolyricsStatus(isConnected, url, title, lyrics, slideIndex, rawJson)));
                }
                catch { }
                return;
            }

            if (_webView?.CoreWebView2 != null)
            {
                try
                {
                    var payload = JsonSerializer.Serialize(new
                    {
                        type = "HOLYRICS_STATUS",
                        isConnected = isConnected,
                        url = url ?? "",
                        title = title ?? "",
                        lyrics = lyrics ?? "",
                        slideIndex = slideIndex
                    });
                    _webView.CoreWebView2.ExecuteScriptAsync($"window.postMessage({payload}, '*');");
                }
                catch { }
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (!Program.IsExplicitExit)
            {
                e.Cancel = true;
                SafeExitFullscreen();
            }
            else
            {
                _retryTimer?.Stop();
                _watchdogTimer?.Stop();
                base.OnFormClosing(e);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _retryTimer?.Dispose();
                _watchdogTimer?.Dispose();
                _webView?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
