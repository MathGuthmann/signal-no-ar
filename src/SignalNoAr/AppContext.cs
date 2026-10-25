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
        private DisplayForm? _displayForm;
        private SettingsForm? _settingsForm;
        private ToolStripMenuItem _menuItemToggle = null!;
        private CancellationTokenSource? _pipeCts;
        private int _lastScreenCount = 0;

        public VmixWatcher VmixWatcher { get; private set; } = null!;
        public bool IsTemporarilyDisabled { get; private set; } = false;

        public AppContext(AppConfig config)
        {
            _config = config;
            _syncContext = SynchronizationContext.Current ?? new WindowsFormsSynchronizationContext();
            _lastScreenCount = Screen.AllScreens.Length;

            InitializeTrayIcon();
            StartIpcServer();

            // Initialize vMix Monitor
            VmixWatcher = new VmixWatcher(
                () => _config.VmixEnabled,
                () => _config.VmixUrl,
                () => _config.VmixPollIntervalSeconds
            );
            VmixWatcher.StateChanged += OnVmixStateChanged;
            VmixWatcher.Start();

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

            // Automatic background update check on startup
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

            var menuReload = new ToolStripMenuItem("🔄 Recarregar Sinal", null, (s, e) => ReloadSignal());

            var menuUpdates = new ToolStripMenuItem("✨ Procurar Atualizações...", null, async (s, e) => await CheckForUpdatesUserInitiatedAsync());

            var menuExit = new ToolStripMenuItem("❌ Sair do Aplicativo", null, (s, e) => ExitApplication());

            menu.Items.Add(menuSettings);
            menu.Items.Add(menuIdentify);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(_menuItemToggle);
            menu.Items.Add(menuReload);
            menu.Items.Add(menuUpdates);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(menuExit);

            Icon icon = SystemIcons.Application;
            try
            {
                if (File.Exists(Program.IconPath))
                {
                    icon = new Icon(Program.IconPath);
                }
            }
            catch { }

            _notifyIcon = new NotifyIcon
            {
                Icon = icon,
                ContextMenuStrip = menu,
                Text = "Signal No Ar - Ativo",
                Visible = true
            };

            _notifyIcon.DoubleClick += (s, e) => ShowSettings();
        }

        private void OnDisplaySettingsChanged(object? sender, EventArgs e)
        {
            _syncContext.Post(_ =>
            {
                int currentCount = Screen.AllScreens.Length;

                if (_settingsForm != null && !_settingsForm.IsDisposed)
                {
                    _settingsForm.ReloadMonitorsList();
                }

                if (_displayForm != null && !_displayForm.IsDisposed && !IsTemporarilyDisabled)
                {
                    Screen targetScreen = _config.ResolveScreen();
                    bool found = false;
                    foreach (var sc in Screen.AllScreens)
                    {
                        if (string.Equals(sc.DeviceName, targetScreen.DeviceName, StringComparison.OrdinalIgnoreCase))
                        {
                            found = true;
                            _displayForm.RelocateToScreen(sc);
                            break;
                        }
                    }

                    if (!found)
                    {
                        DeactivateTemporarily();
                        _notifyIcon.ShowBalloonTip(
                            4000,
                            "Monitor Desconectado",
                            "O monitor configurado para o Signal foi desconectado. O sinal foi pausado por segurança.",
                            ToolTipIcon.Warning
                        );
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
                Screen targetScreen = _config.ResolveScreen();
                bool runWindowed = _config.WindowedPreviewOnSingleMonitor && Screen.AllScreens.Length <= 1;

                if (_displayForm == null || _displayForm.IsDisposed)
                {
                    _displayForm = new DisplayForm(_config, this, targetScreen, runWindowed);
                }
                else
                {
                    _displayForm.RelocateToScreen(targetScreen);
                }

                _displayForm.Show();
                UpdateTrayStatus();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao iniciar tela cheia: {ex.Message}", "Signal No Ar", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public void DeactivateTemporarily()
        {
            IsTemporarilyDisabled = true;
            if (_displayForm != null && !_displayForm.IsDisposed)
            {
                _displayForm.Hide();
            }
            UpdateTrayStatus();

            _notifyIcon.ShowBalloonTip(
                4000,
                "Signal No Ar Pausado",
                "O sinal foi ocultado temporariamente. Ele voltará a abrir automaticamente quando o computador reiniciar.",
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
            if (_displayForm != null && !_displayForm.IsDisposed)
            {
                _displayForm.Reload();
            }
        }

        public void ApplyConfigurationChanges()
        {
            if (!IsTemporarilyDisabled)
            {
                Screen targetScreen = _config.ResolveScreen();
                if (_displayForm != null && !_displayForm.IsDisposed)
                {
                    _displayForm.RelocateToScreen(targetScreen);
                    _displayForm.NavigateToUrl();
                }
                else
                {
                    LaunchDisplay();
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
                _notifyIcon.Text = "Signal No Ar - (Pausado nesta sessão)";
                _menuItemToggle.Text = "▶️ Reativar Agora";
            }
            else
            {
                string dev = string.IsNullOrEmpty(_config.SelectedMonitorDeviceName) ? "Display" : _config.SelectedMonitorDeviceName;
                _notifyIcon.Text = $"Signal No Ar - Ativo ({dev})";
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

                        if (message == "SHOW_SETTINGS" || message == "SETUP")
                        {
                            _syncContext.Post(_ =>
                            {
                                if (message == "SETUP")
                                {
                                    ShowSetupWizard();
                                }
                                else
                                {
                                    ShowSettings();
                                }
                            }, null);
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
                    _notifyIcon.Text = "Signal No Ar — 🔴 AO VIVO (vMix)";
                }
                else if (state.IsConnected)
                {
                    _notifyIcon.Text = "Signal No Ar — 🟡 Standby (vMix)";
                }
                else
                {
                    _notifyIcon.Text = "Signal No Ar — Ativo";
                }

                if (_displayForm != null && !_displayForm.IsDisposed)
                {
                    _displayForm.NotifyVmixStatus(state.IsStreaming, state.IsRecording, state.ActiveInput);
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
            SystemEvents.DisplaySettingsChanged -= OnDisplaySettingsChanged;
            _pipeCts?.Cancel();
            VmixWatcher.Dispose();

            if (_displayForm != null && !_displayForm.IsDisposed)
            {
                _displayForm.Close();
                _displayForm.Dispose();
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
