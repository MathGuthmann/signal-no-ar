using System;
using System.Drawing;
using System.IO;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Win32;
using SignalNoAr.Models;
using SignalNoAr.Services;
using SignalNoAr.UI;

namespace SignalNoAr
{
    public class AppContext : ApplicationContext
    {
        private readonly AppConfig _config;
        private readonly SynchronizationContext _syncContext;
        private NotifyIcon _notifyIcon = null!;
        private DisplayForm? _primaryDisplayForm;
        private DisplayForm? _secondDisplayForm;
        private SettingsForm? _settingsForm;
        private ToolStripMenuItem _menuItemToggle = null!;
        private CancellationTokenSource? _pipeCts;
        private int _lastScreenCount = 0;

        public VmixWatcher VmixWatcher { get; private set; } = null!;
        public HolyricsWatcher HolyricsWatcher { get; private set; } = null!;
        public bool IsTemporarilyDisabled { get; private set; } = false;

        public AppContext(AppConfig config)
        {
            _config = config;
            _syncContext = SynchronizationContext.Current ?? new WindowsFormsSynchronizationContext();
            _lastScreenCount = Screen.AllScreens.Length;

            InitializeTrayIcon();
            StartIpcServer();

            // Hook clean exit on update restart
            UpdateService.OnBeforeRestart = () => ExitApplication();

            // Initialize vMix Monitor
            VmixWatcher = new VmixWatcher(
                () => _config.VmixEnabled,
                () => _config.VmixUrl,
                () => _config.VmixPollIntervalSeconds
            );
            VmixWatcher.StateChanged += OnVmixStateChanged;
            VmixWatcher.Start();

            // Initialize Holyrics Monitor
            HolyricsWatcher = new HolyricsWatcher(
                () => _config.HolyricsEnabled,
                () => _config.HolyricsUrl,
                () => _config.HolyricsPollIntervalSeconds,
                () => _config.HolyricsToken
            );
            HolyricsWatcher.StateChanged += OnHolyricsStateChanged;
            HolyricsWatcher.Start();

            // Listen to monitor plug/unplug events
            SystemEvents.DisplaySettingsChanged += OnDisplaySettingsChanged;

            _syncContext.Post(_ =>
            {
                if (!_config.IsConfigured)
                {
                    ShowSetupWizard();
                }
                else
                {
                    LaunchDisplay();
                }
            }, null);

            // Automatic background update check on startup (waits 6s)
            _ = Task.Run(async () =>
            {
                await Task.Delay(6000);
                if (_config.AutoCheckUpdates)
                {
                    await CheckForUpdatesSilentlyAsync();
                }
            });
        }

        private void InitializeTrayIcon()
        {
            var menu = new ContextMenuStrip();

            var menuSettings = new ToolStripMenuItem("⚙️ Painel de Configurações...", null, (s, e) => ShowSettings());
            menuSettings.Font = new Font(menuSettings.Font, FontStyle.Bold);

            var menuIdentify = new ToolStripMenuItem("🖥️ Identificar Monitores", null, (s, e) => MonitorIdentifyForm.IdentifyAllScreens());

            _menuItemToggle = new ToolStripMenuItem("⏸️ Pausar nesta sessão", null, (s, e) => ToggleTemporaryState());

            var menuReload = new ToolStripMenuItem("🔄 Recarregar Telas (F5)", null, (s, e) => ReloadSignal());

            var menuUpdates = new ToolStripMenuItem("✨ Procurar Atualizações...", null, async (s, e) => await CheckForUpdatesUserInitiatedAsync());

            var menuExit = new ToolStripMenuItem("❌ Fechar Signal No Ar", null, (s, e) => ExitApplication());

            menu.Items.Add(menuSettings);
            menu.Items.Add(menuIdentify);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(_menuItemToggle);
            menu.Items.Add(menuReload);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(menuUpdates);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(menuExit);

            _notifyIcon = new NotifyIcon
            {
                ContextMenuStrip = menu,
                Visible = true
            };

            // Set tray icon safely
            try
            {
                if (File.Exists(Program.IconPath))
                {
                    _notifyIcon.Icon = new Icon(Program.IconPath);
                }
                else
                {
                    _notifyIcon.Icon = SystemIcons.Application;
                }
            }
            catch
            {
                _notifyIcon.Icon = SystemIcons.Application;
            }

            SetTrayText("Signal No Ar — Inicializando...");
            _notifyIcon.DoubleClick += (s, e) => ShowSettings();
        }

        private void SetTrayText(string text)
        {
            if (_notifyIcon == null) return;
            if (string.IsNullOrEmpty(text))
            {
                _notifyIcon.Text = "Signal No Ar";
                return;
            }

            // Windows has a strict 63-character limit for NotifyIcon.Text
            if (text.Length > 63)
            {
                _notifyIcon.Text = text.Substring(0, 60) + "...";
            }
            else
            {
                _notifyIcon.Text = text;
            }
        }

        private void OnDisplaySettingsChanged(object? sender, EventArgs e)
        {
            _syncContext.Post(_ =>
            {
                int currentCount = Screen.AllScreens.Length;
                if (currentCount != _lastScreenCount)
                {
                    bool foundPrimary = false;
                    foreach (var s in Screen.AllScreens)
                    {
                        if (string.Equals(s.DeviceName, _config.SelectedMonitorDeviceName, StringComparison.OrdinalIgnoreCase))
                        {
                            foundPrimary = true;
                            break;
                        }
                    }

                    if (!foundPrimary && _config.SelectedMonitorIndex >= Screen.AllScreens.Length)
                    {
                        DeactivateTemporarily();
                        _notifyIcon.ShowBalloonTip(
                            4000,
                            "Monitor Desconectado",
                            "O monitor configurado para o Signal foi desconectado. O sinal foi pausado por segurança.",
                            ToolTipIcon.Warning
                        );
                    }
                    else
                    {
                        ApplyConfigurationChanges();
                    }
                }

                _lastScreenCount = currentCount;
            }, null);
        }

        public void ShowSetupWizard()
        {
            if (_settingsForm != null && !_settingsForm.IsDisposed)
            {
                _settingsForm.BringToFront();
                return;
            }

            var wizard = new SettingsForm(_config, this, isWizardMode: true);
            var result = wizard.ShowDialog();
            if (result == DialogResult.OK)
            {
                if (!IsTemporarilyDisabled)
                {
                    LaunchDisplay();
                }
            }
        }

        public void LaunchDisplay()
        {
            if (IsTemporarilyDisabled) return;

            try
            {
                // 1. Primary Display (No Ar)
                Screen primaryScreen = _config.ResolveScreen();
                bool runWindowed = _config.WindowedPreviewOnSingleMonitor && Screen.AllScreens.Length <= 1;

                if (_primaryDisplayForm == null || _primaryDisplayForm.IsDisposed)
                {
                    _primaryDisplayForm = new DisplayForm(_config, this, primaryScreen, runWindowed, _config.Url, "No Ar");
                }
                else
                {
                    _primaryDisplayForm.RelocateToScreen(primaryScreen);
                }
                _primaryDisplayForm.Show();

                // 2. Secondary Display (Palco / Retorno)
                if (_config.SecondDisplayEnabled)
                {
                    Screen? secondScreen = _config.ResolveSecondScreen();
                    if (secondScreen != null)
                    {
                        if (_secondDisplayForm == null || _secondDisplayForm.IsDisposed)
                        {
                            _secondDisplayForm = new DisplayForm(_config, this, secondScreen, false, _config.SecondDisplayUrl, "Palco");
                        }
                        else
                        {
                            _secondDisplayForm.RelocateToScreen(secondScreen);
                        }
                        _secondDisplayForm.Show();
                    }
                }
                else
                {
                    if (_secondDisplayForm != null && !_secondDisplayForm.IsDisposed)
                    {
                        _secondDisplayForm.Close();
                        _secondDisplayForm.Dispose();
                        _secondDisplayForm = null;
                    }
                }

                // 3. Sleep Blocker
                if (_config.PreventSleep)
                {
                    SleepBlocker.Enable();
                }
                else
                {
                    SleepBlocker.Disable();
                }

                UpdateTrayStatus();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao iniciar telas: {ex.Message}", "Signal No Ar", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public void DeactivateTemporarily()
        {
            IsTemporarilyDisabled = true;
            if (_primaryDisplayForm != null && !_primaryDisplayForm.IsDisposed)
            {
                _primaryDisplayForm.Hide();
            }
            if (_secondDisplayForm != null && !_secondDisplayForm.IsDisposed)
            {
                _secondDisplayForm.Hide();
            }

            SleepBlocker.Disable();
            UpdateTrayStatus();

            _notifyIcon.ShowBalloonTip(
                4000,
                "Signal No Ar Pausado",
                "As telas foram ocultadas temporariamente. Elas voltarão a abrir automaticamente quando o computador reiniciar ou pelo menu da bandeja.",
                ToolTipIcon.Info
            );
        }

        public void Reactivate()
        {
            IsTemporarilyDisabled = false;
            LaunchDisplay();
            UpdateTrayStatus();

            _notifyIcon.ShowBalloonTip(
                3000,
                "Signal No Ar Reativado",
                "O sinal em tela cheia foi restaurado.",
                ToolTipIcon.Info
            );
        }

        public void ToggleTemporaryState()
        {
            if (IsTemporarilyDisabled)
            {
                Reactivate();
            }
            else
            {
                DeactivateTemporarily();
            }
            _settingsForm?.UpdateStatusUI();
        }

        public void ReloadSignal()
        {
            if (_primaryDisplayForm != null && !_primaryDisplayForm.IsDisposed)
            {
                _primaryDisplayForm.Reload();
            }
            if (_secondDisplayForm != null && !_secondDisplayForm.IsDisposed)
            {
                _secondDisplayForm.Reload();
            }
        }

        public void ApplyConfigurationChanges()
        {
            if (!IsTemporarilyDisabled)
            {
                // Primary display
                Screen primaryScreen = _config.ResolveScreen();
                if (_primaryDisplayForm != null && !_primaryDisplayForm.IsDisposed)
                {
                    _primaryDisplayForm.RelocateToScreen(primaryScreen);
                    _primaryDisplayForm.NavigateToUrl();
                }
                else
                {
                    LaunchDisplay();
                }

                // Secondary display
                if (_config.SecondDisplayEnabled)
                {
                    Screen? secondScreen = _config.ResolveSecondScreen();
                    if (secondScreen != null)
                    {
                        if (_secondDisplayForm != null && !_secondDisplayForm.IsDisposed)
                        {
                            _secondDisplayForm.RelocateToScreen(secondScreen);
                            _secondDisplayForm.NavigateToUrl();
                        }
                        else
                        {
                            _secondDisplayForm = new DisplayForm(_config, this, secondScreen, false, _config.SecondDisplayUrl, "Palco");
                            _secondDisplayForm.Show();
                        }
                    }
                }
                else
                {
                    if (_secondDisplayForm != null && !_secondDisplayForm.IsDisposed)
                    {
                        _secondDisplayForm.Close();
                        _secondDisplayForm.Dispose();
                        _secondDisplayForm = null;
                    }
                }

                if (_config.PreventSleep)
                {
                    SleepBlocker.Enable();
                }
                else
                {
                    SleepBlocker.Disable();
                }
            }
            UpdateTrayStatus();
        }

        public void ShowSettings()
        {
            if (_settingsForm == null || _settingsForm.IsDisposed)
            {
                _settingsForm = new SettingsForm(_config, this, isWizardMode: false);
            }

            _settingsForm.Show();
            _settingsForm.WindowState = FormWindowState.Normal;
            _settingsForm.BringToFront();
            _settingsForm.Activate();
        }

        private void UpdateTrayStatus()
        {
            if (IsTemporarilyDisabled)
            {
                SetTrayText("Signal No Ar - (Pausado nesta sessão)");
                _menuItemToggle.Text = "▶️ Reativar Agora";
            }
            else
            {
                string info = _config.SecondDisplayEnabled ? "Multi-Telas (No Ar + Palco)" : "Ativo";
                SetTrayText($"Signal No Ar — {info}");
                _menuItemToggle.Text = "⏸️ Pausar nesta sessão";
            }
        }

        private void StartIpcServer()
        {
            _pipeCts = new CancellationTokenSource();
            Task.Run(async () =>
            {
                while (!_pipeCts.Token.IsCancellationRequested)
                {
                    try
                    {
                        using var server = new NamedPipeServerStream(
                            "SignalNoAr_Ipc_Pipe",
                            PipeDirection.In,
                            1,
                            PipeTransmissionMode.Byte,
                            PipeOptions.Asynchronous
                        );

                        await server.WaitForConnectionAsync(_pipeCts.Token);
                        using var reader = new StreamReader(server);
                        string? message = await reader.ReadLineAsync();

                        if (message == "SHOW_SETTINGS")
                        {
                            _syncContext.Post(_ => ShowSettings(), null);
                        }
                        else if (message == "SETUP")
                        {
                            _syncContext.Post(_ => ShowSetupWizard(), null);
                        }
                        else if (message == "RELOAD")
                        {
                            _syncContext.Post(_ => ReloadSignal(), null);
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"IPC Error: {ex.Message}");
                        await Task.Delay(500);
                    }
                }
            });
        }

        private void OnVmixStateChanged(VmixState state)
        {
            _syncContext.Post(_ =>
            {
                if (state.IsStreaming)
                {
                    SetTrayText("Signal No Ar — 🔴 AO VIVO (vMix)");
                }
                else if (state.IsConnected)
                {
                    SetTrayText("Signal No Ar — 🟡 Standby (vMix)");
                }
                else
                {
                    UpdateTrayStatus();
                }

                if (_primaryDisplayForm != null && !_primaryDisplayForm.IsDisposed)
                {
                    _primaryDisplayForm.NotifyVmixStatus(state.IsStreaming, state.IsRecording, state.ActiveInput);
                }

                if (_secondDisplayForm != null && !_secondDisplayForm.IsDisposed)
                {
                    _secondDisplayForm.NotifyVmixStatus(state.IsStreaming, state.IsRecording, state.ActiveInput);
                }
            }, null);
        }

        private void OnHolyricsStateChanged(HolyricsState state)
        {
            _syncContext.Post(_ =>
            {
                if (_primaryDisplayForm != null && !_primaryDisplayForm.IsDisposed)
                {
                    _primaryDisplayForm.NotifyHolyricsStatus(
                        state.IsConnected,
                        state.ServerUrl,
                        state.CurrentSongTitle,
                        state.CurrentLyrics,
                        state.CurrentSlideIndex,
                        state.RawDataJson
                    );
                }

                if (_secondDisplayForm != null && !_secondDisplayForm.IsDisposed)
                {
                    _secondDisplayForm.NotifyHolyricsStatus(
                        state.IsConnected,
                        state.ServerUrl,
                        state.CurrentSongTitle,
                        state.CurrentLyrics,
                        state.CurrentSlideIndex,
                        state.RawDataJson
                    );
                }
            }, null);
        }

        private async Task CheckForUpdatesSilentlyAsync()
        {
            try
            {
                var info = await UpdateService.CheckForUpdatesAsync();
                if (info.HasUpdate && !string.IsNullOrEmpty(info.DownloadUrl))
                {
                    _syncContext.Post(_ =>
                    {
                        _notifyIcon.ShowBalloonTip(
                            5000,
                            "Atualização do Signal No Ar",
                            $"Nova versão {info.LatestVersion} detectada! Atualizando o Signal No Ar automaticamente...",
                            ToolTipIcon.Info
                        );
                    }, null);

                    await UpdateService.DownloadAndInstallUpdateAsync(info.DownloadUrl, autoMode: true);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Silent update check error: {ex.Message}");
            }
        }

        public async Task CheckForUpdatesUserInitiatedAsync()
        {
            _notifyIcon.ShowBalloonTip(
                3000,
                "Verificando Atualizações",
                "Conectando ao GitHub para verificar novidades...",
                ToolTipIcon.Info
            );

            var info = await UpdateService.CheckForUpdatesAsync();

            _syncContext.Post(_ =>
            {
                if (!string.IsNullOrEmpty(info.ErrorMessage))
                {
                    MessageBox.Show(
                        $"Não foi possível verificar atualizações no momento:\n{info.ErrorMessage}\n\nVerifique sua conexão com a internet.",
                        "Verificação de Atualização",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning
                    );
                    return;
                }

                if (!info.HasUpdate)
                {
                    MessageBox.Show(
                        $"O Signal No Ar já está na versão mais recente!\nVersão instalada: v{info.CurrentVersion}",
                        "Signal No Ar Atualizado",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information
                    );
                    return;
                }

                string msg = $"Uma nova versão do Signal No Ar está disponível!\n\n" +
                             $"Versão Instalada: v{info.CurrentVersion}\n" +
                             $"Nova Versão: v{info.LatestVersion}\n\n" +
                             $"Novidades:\n{info.ReleaseNotes}\n\n" +
                             $"Deseja baixar e atualizar agora?";

                var result = MessageBox.Show(msg, "Atualização Disponível", MessageBoxButtons.YesNo, MessageBoxIcon.Information);
                if (result == DialogResult.Yes)
                {
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            _syncContext.Post(__ =>
                            {
                                _notifyIcon.ShowBalloonTip(
                                    5000,
                                    "Baixando Atualização",
                                    "O instalador está sendo baixado e iniciará em instantes...",
                                    ToolTipIcon.Info
                                );
                            }, null);

                            await UpdateService.DownloadAndInstallUpdateAsync(info.DownloadUrl, autoMode: false);
                        }
                        catch (Exception ex)
                        {
                            _syncContext.Post(__ =>
                            {
                                MessageBox.Show(
                                    $"Erro ao baixar atualização:\n{ex.Message}",
                                    "Erro de Download",
                                    MessageBoxButtons.OK,
                                    MessageBoxIcon.Error
                                );
                            }, null);
                        }
                    });
                }
            }, null);
        }

        public void ExitApplication()
        {
            Program.IsExplicitExit = true;
            SleepBlocker.Disable();
            SystemEvents.DisplaySettingsChanged -= OnDisplaySettingsChanged;
            _pipeCts?.Cancel();
            VmixWatcher.Dispose();
            HolyricsWatcher.Dispose();

            if (_primaryDisplayForm != null && !_primaryDisplayForm.IsDisposed)
            {
                _primaryDisplayForm.Close();
                _primaryDisplayForm.Dispose();
            }

            if (_secondDisplayForm != null && !_secondDisplayForm.IsDisposed)
            {
                _secondDisplayForm.Close();
                _secondDisplayForm.Dispose();
            }

            if (_settingsForm != null && !_settingsForm.IsDisposed)
            {
                _settingsForm.Close();
                _settingsForm.Dispose();
            }

            _notifyIcon.Visible = false;
            _notifyIcon.Dispose();

            ExitThread();
        }
    }
}
