using System;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using SignalNoAr.Models;

namespace SignalNoAr.UI
{
    public class SettingsForm : Form
    {
        private readonly AppConfig _config;
        private readonly AppContext _appContext;
        private readonly bool _isWizardMode;

        private Panel _centerContainer = null!;
        private ComboBox _cbMonitors = null!;
        private TextBox _txtUrl = null!;
        private CheckBox _chkAutoStart = null!;
        private CheckBox _chkLaunchImmediately = null!;
        private CheckBox _chkWindowedOnSingle = null!;
        private Button _btnTemporaryToggle = null!;
        private Label _lblStatusBadge = null!;
        private Label _lblStatusDesc = null!;
        private Button _btnApply = null!;
        private Button _btnIdentify = null!;
        private Panel _pnlSingleMonitorWarning = null!;
        private Label _lblSelectedMonitorDetails = null!;

        public SettingsForm(AppConfig config, AppContext appContext, bool isWizardMode = false)
        {
            _config = config;
            _appContext = appContext;
            _isWizardMode = isWizardMode;

            InitializeForm();
            BuildUI();
            LoadConfigData();
        }

        private void InitializeForm()
        {
            Text = _isWizardMode ? "Signal No Ar — Configuração do Monitor" : "Signal No Ar — Painel de Controle";
            Size = new Size(740, 750);
            MinimumSize = new Size(650, 640);
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.Sizable; // Resizable for any screen
            MaximizeBox = true;
            MinimizeBox = !_isWizardMode;
            BackColor = Color.FromArgb(14, 13, 20); // Signal Deep Dark Slate
            ForeColor = Color.White;
            ShowInTaskbar = true;
            AutoScaleMode = AutoScaleMode.Dpi;
            KeyPreview = true;

            KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Escape && !_isWizardMode)
                {
                    Close();
                }
            };

            try
            {
                if (File.Exists(Program.IconPath))
                {
                    Icon = new Icon(Program.IconPath);
                }
            }
            catch { }
        }

        private void BuildUI()
        {
            // Top Header Banner
            var headerPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 125,
                BackColor = Color.FromArgb(22, 20, 32),
                Padding = new Padding(24, 14, 24, 10)
            };

            var picHeaderLogo = new PictureBox
            {
                Dock = DockStyle.Top,
                Height = 52,
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.Transparent
            };

            try
            {
                using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("SignalNoAr.logo.png");
                if (stream != null)
                {
                    picHeaderLogo.Image = Image.FromStream(stream);
                }
            }
            catch { }

            var lblHeaderTitle = new Label
            {
                Text = _isWizardMode ? "CONFIGURAÇÃO INICIAL DO DISPLAY" : "PAINEL DE CONTROLE DO SINAL",
                Font = new Font("Segoe UI", 11.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(255, 120, 0), // Signal Orange
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Top,
                Height = 26
            };

            var lblHeaderSub = new Label
            {
                Text = _isWizardMode
                    ? "Escolha o monitor onde o sinal 'NO AR' será exibido em tela cheia."
                    : "Gerencie a exibição em tela cheia, selecione monitores ou pause temporariamente.",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Regular),
                ForeColor = Color.FromArgb(185, 180, 205),
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Top,
                Height = 22
            };

            headerPanel.Controls.Add(lblHeaderSub);
            headerPanel.Controls.Add(lblHeaderTitle);
            headerPanel.Controls.Add(picHeaderLogo);
            Controls.Add(headerPanel);

            // Bottom Actions Bar
            var bottomPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 72,
                BackColor = Color.FromArgb(18, 17, 26),
                Padding = new Padding(24, 14, 24, 14)
            };

            _btnApply = new Button
            {
                Text = _isWizardMode ? "Salvar e Concluir" : "Salvar e Aplicar",
                Dock = DockStyle.Right,
                Width = _isWizardMode ? 220 : 185,
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                BackColor = Color.FromArgb(255, 110, 0), // Signal Orange
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            _btnApply.FlatAppearance.BorderSize = 0;
            _btnApply.Click += BtnApply_Click;
            bottomPanel.Controls.Add(_btnApply);

            var btnCancel = new Button
            {
                Text = _isWizardMode ? "Cancelar" : "Fechar Painel",
                Dock = DockStyle.Left,
                Width = 125,
                Font = new Font("Segoe UI", 10f, FontStyle.Regular),
                BackColor = Color.FromArgb(36, 33, 48),
                ForeColor = Color.FromArgb(215, 210, 230),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnCancel.FlatAppearance.BorderColor = Color.FromArgb(60, 55, 80);
            btnCancel.Click += (s, e) => Close();
            bottomPanel.Controls.Add(btnCancel);

            Controls.Add(bottomPanel);

            // Main Scrollable Panel
            var scrollPanel = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = Color.FromArgb(14, 13, 20),
                Padding = new Padding(20, 16, 20, 16)
            };
            Controls.Add(scrollPanel);

            // Centered Card Container (auto-centers on large/wide screens)
            _centerContainer = new Panel
            {
                Location = new Point(20, 10),
                Width = Math.Max(580, scrollPanel.ClientSize.Width - 40),
                BackColor = Color.Transparent
            };
            scrollPanel.Controls.Add(_centerContainer);

            scrollPanel.Resize += (s, e) =>
            {
                int availableWidth = scrollPanel.ClientSize.Width - 40;
                int targetWidth = Math.Min(820, Math.Max(580, availableWidth));
                int targetX = Math.Max(10, (scrollPanel.ClientSize.Width - targetWidth) / 2);
                _centerContainer.Width = targetWidth;
                _centerContainer.Left = targetX;
            };

            int currentY = 0;

            // 1. Single Monitor Warning Card
            _pnlSingleMonitorWarning = new Panel
            {
                Location = new Point(0, currentY),
                Width = _centerContainer.Width,
                Height = 88,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                BackColor = Color.FromArgb(44, 30, 18), // Dark Amber Alert
                Padding = new Padding(16, 10, 16, 10),
                Visible = false
            };

            var lblWarnTitle = new Label
            {
                Text = "⚠️ Atenção: Apenas 1 monitor detectado",
                Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(255, 190, 45),
                Dock = DockStyle.Top,
                Height = 24
            };

            var lblWarnDesc = new Label
            {
                Text = "Como você possui apenas 1 monitor, abrir em tela cheia cobrirá toda a sua área de trabalho. " +
                       "Dica de segurança: Você pode pressionar a tecla [ESC] a qualquer momento para sair da tela cheia.",
                Font = new Font("Segoe UI", 9f, FontStyle.Regular),
                ForeColor = Color.FromArgb(250, 230, 180),
                Dock = DockStyle.Fill
            };

            _pnlSingleMonitorWarning.Controls.Add(lblWarnDesc);
            _pnlSingleMonitorWarning.Controls.Add(lblWarnTitle);
            _centerContainer.Controls.Add(_pnlSingleMonitorWarning);

            // 2. Status Card (normal settings mode)
            if (!_isWizardMode)
            {
                var pnlStatusCard = new Panel
                {
                    Location = new Point(0, currentY),
                    Width = _centerContainer.Width,
                    Height = 84,
                    Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                    BackColor = Color.FromArgb(26, 23, 36),
                    Padding = new Padding(18, 12, 18, 12)
                };

                _lblStatusBadge = new Label
                {
                    Text = "🟢 SINAL ATIVO",
                    Font = new Font("Segoe UI", 11.5f, FontStyle.Bold),
                    ForeColor = Color.FromArgb(70, 220, 130),
                    Location = new Point(14, 12),
                    Size = new Size(260, 24)
                };

                _lblStatusDesc = new Label
                {
                    Text = "Exibindo em tela cheia no monitor selecionado.",
                    Font = new Font("Segoe UI", 9.2f, FontStyle.Regular),
                    ForeColor = Color.FromArgb(190, 185, 210),
                    Location = new Point(14, 38),
                    Size = new Size(360, 28)
                };

                _btnTemporaryToggle = new Button
                {
                    Text = "Pausar nesta sessão",
                    Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                    Size = new Size(185, 38),
                    Anchor = AnchorStyles.Top | AnchorStyles.Right,
                    Location = new Point(pnlStatusCard.Width - 195, 20),
                    BackColor = Color.FromArgb(46, 38, 64),
                    ForeColor = Color.FromArgb(255, 195, 100),
                    FlatStyle = FlatStyle.Flat,
                    Cursor = Cursors.Hand
                };
                _btnTemporaryToggle.FlatAppearance.BorderColor = Color.FromArgb(110, 75, 150);
                _btnTemporaryToggle.Click += BtnTemporaryToggle_Click;

                pnlStatusCard.Controls.Add(_btnTemporaryToggle);
                pnlStatusCard.Controls.Add(_lblStatusDesc);
                pnlStatusCard.Controls.Add(_lblStatusBadge);
                _centerContainer.Controls.Add(pnlStatusCard);

                currentY += 96;
            }

            // 3. Monitor Section Card
            var pnlMonitorCard = new Panel
            {
                Location = new Point(0, currentY),
                Width = _centerContainer.Width,
                Height = 155,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                BackColor = Color.FromArgb(24, 22, 34),
                Padding = new Padding(18, 14, 18, 14)
            };

            var lblMonitorSection = new Label
            {
                Text = "📺 MONITOR DE EXIBIÇÃO:",
                Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(240, 235, 255),
                Dock = DockStyle.Top,
                Height = 25
            };
            pnlMonitorCard.Controls.Add(lblMonitorSection);

            var pnlMonitorControls = new Panel
            {
                Dock = DockStyle.Top,
                Height = 36,
                BackColor = Color.Transparent,
                Padding = new Padding(0, 2, 0, 0)
            };

            _btnIdentify = new Button
            {
                Text = "Identificar Telas",
                Dock = DockStyle.Right,
                Width = 160,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                BackColor = Color.FromArgb(92, 42, 145),
                ForeColor = Color.FromArgb(240, 220, 255),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            _btnIdentify.FlatAppearance.BorderColor = Color.FromArgb(135, 75, 195);
            _btnIdentify.Click += (s, e) => MonitorIdentifyForm.IdentifyAllScreens();
            pnlMonitorControls.Controls.Add(_btnIdentify);

            _cbMonitors = new ComboBox
            {
                Dock = DockStyle.Fill,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Color.FromArgb(34, 30, 48),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10.5f)
            };
            _cbMonitors.SelectedIndexChanged += (s, e) => UpdateSelectedMonitorDetails();
            pnlMonitorControls.Controls.Add(_cbMonitors);
            pnlMonitorCard.Controls.Add(pnlMonitorControls);

            _lblSelectedMonitorDetails = new Label
            {
                Text = "Resolução: -- x -- | Monitor Principal",
                Font = new Font("Segoe UI", 9f, FontStyle.Regular),
                ForeColor = Color.FromArgb(255, 170, 70), // Signal Gold/Amber
                Dock = DockStyle.Top,
                Height = 26
            };
            pnlMonitorCard.Controls.Add(_lblSelectedMonitorDetails);

            var lblMonitorHelp = new Label
            {
                Text = "💡 O Signal No Ar memoriza este monitor e abre diretamente nele toda vez que o PC ligar.",
                Font = new Font("Segoe UI", 8.8f, FontStyle.Italic),
                ForeColor = Color.FromArgb(160, 155, 180),
                Dock = DockStyle.Top,
                Height = 22
            };
            pnlMonitorCard.Controls.Add(lblMonitorHelp);

            _centerContainer.Controls.Add(pnlMonitorCard);
            currentY += 166;

            // 4. URL Section Card
            var pnlUrlCard = new Panel
            {
                Location = new Point(0, currentY),
                Width = _centerContainer.Width,
                Height = 100,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                BackColor = Color.FromArgb(24, 22, 34),
                Padding = new Padding(18, 14, 18, 14)
            };

            var lblUrlSection = new Label
            {
                Text = "🔗 ENDEREÇO DO SINAL (URL):",
                Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(240, 235, 255),
                Dock = DockStyle.Top,
                Height = 25
            };
            pnlUrlCard.Controls.Add(lblUrlSection);

            _txtUrl = new TextBox
            {
                Dock = DockStyle.Top,
                Height = 32,
                BackColor = Color.FromArgb(34, 30, 48),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10.5f),
                BorderStyle = BorderStyle.FixedSingle
            };
            pnlUrlCard.Controls.Add(_txtUrl);

            _centerContainer.Controls.Add(pnlUrlCard);
            currentY += 112;

            // 5. Options & Behavior Card
            var pnlOptionsCard = new Panel
            {
                Location = new Point(0, currentY),
                Width = _centerContainer.Width,
                Height = _isWizardMode ? 145 : 105,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                BackColor = Color.FromArgb(24, 22, 34),
                Padding = new Padding(18, 14, 18, 14)
            };

            var lblOptionsSection = new Label
            {
                Text = "⚙️ OPÇÕES DE INICIALIZAÇÃO E EXIBIÇÃO:",
                Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(240, 235, 255),
                Dock = DockStyle.Top,
                Height = 25
            };
            pnlOptionsCard.Controls.Add(lblOptionsSection);

            _chkAutoStart = new CheckBox
            {
                Text = "Iniciar automaticamente quando o computador ligar (Recomendado)",
                Font = new Font("Segoe UI", 9.8f, FontStyle.Regular),
                ForeColor = Color.FromArgb(235, 230, 250),
                Dock = DockStyle.Top,
                Height = 28,
                Checked = true
            };
            pnlOptionsCard.Controls.Add(_chkAutoStart);

            _chkWindowedOnSingle = new CheckBox
            {
                Text = "Permitir teste em janela flutuante quando houver apenas 1 monitor",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Regular),
                ForeColor = Color.FromArgb(200, 195, 220),
                Dock = DockStyle.Top,
                Height = 28,
                Checked = _config.WindowedPreviewOnSingleMonitor
            };
            pnlOptionsCard.Controls.Add(_chkWindowedOnSingle);

            if (_isWizardMode)
            {
                _chkLaunchImmediately = new CheckBox
                {
                    Text = "Abrir o sinal em tela cheia agora ao salvar",
                    Font = new Font("Segoe UI", 9.8f, FontStyle.Bold),
                    ForeColor = Color.FromArgb(255, 180, 70),
                    Dock = DockStyle.Top,
                    Height = 28,
                    Checked = Screen.AllScreens.Length > 1
                };
                pnlOptionsCard.Controls.Add(_chkLaunchImmediately);
            }

            _centerContainer.Controls.Add(pnlOptionsCard);
            currentY += pnlOptionsCard.Height + 14;

            // 6. Temporary Pause Notice Card
            var pnlNotice = new Panel
            {
                Location = new Point(0, currentY),
                Width = _centerContainer.Width,
                Height = 55,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                BackColor = Color.FromArgb(22, 20, 32),
                Padding = new Padding(14, 10, 14, 10)
            };

            var lblNotice = new Label
            {
                Text = "ℹ️ Se desativar temporariamente, o sinal fecha nesta sessão e reabre sozinho quando o computador reiniciar.",
                Font = new Font("Segoe UI", 9f, FontStyle.Regular),
                ForeColor = Color.FromArgb(190, 185, 220),
                Dock = DockStyle.Fill
            };
            pnlNotice.Controls.Add(lblNotice);
            _centerContainer.Controls.Add(pnlNotice);
            currentY += 65;

            _centerContainer.Height = currentY + 20;
        }

        public void ReloadMonitorsList()
        {
            if (InvokeRequired)
            {
                Invoke(new Action(ReloadMonitorsList));
                return;
            }

            LoadConfigData();
        }

        private void LoadConfigData()
        {
            _cbMonitors.Items.Clear();
            Screen[] screens = Screen.AllScreens;
            int defaultIndex = 0;

            if (screens.Length <= 1)
            {
                _pnlSingleMonitorWarning.Visible = true;
                _pnlSingleMonitorWarning.BringToFront();
            }
            else
            {
                _pnlSingleMonitorWarning.Visible = false;
                // Auto-select secondary screen if available
                for (int i = 0; i < screens.Length; i++)
                {
                    if (!screens[i].Primary)
                    {
                        defaultIndex = i;
                        break;
                    }
                }
            }

            for (int i = 0; i < screens.Length; i++)
            {
                var screen = screens[i];
                string label = $"Monitor {i + 1}: {screen.Bounds.Width} × {screen.Bounds.Height} " +
                               $"({(screen.Primary ? "Principal" : "Secundário")}) - {screen.DeviceName}";
                _cbMonitors.Items.Add(new MonitorItem(i, screen, label));

                if (!string.IsNullOrEmpty(_config.SelectedMonitorDeviceName) &&
                    string.Equals(screen.DeviceName, _config.SelectedMonitorDeviceName, StringComparison.OrdinalIgnoreCase))
                {
                    defaultIndex = i;
                }
                else if (string.IsNullOrEmpty(_config.SelectedMonitorDeviceName) && _config.SelectedMonitorIndex == i)
                {
                    defaultIndex = i;
                }
            }

            if (_cbMonitors.Items.Count > 0)
            {
                _cbMonitors.SelectedIndex = Math.Clamp(defaultIndex, 0, _cbMonitors.Items.Count - 1);
            }

            _txtUrl.Text = string.IsNullOrEmpty(_config.Url) ? "https://signal.mathguthmann.com/no-ar" : _config.Url;
            _chkAutoStart.Checked = _config.AutoStart;
            if (_chkWindowedOnSingle != null)
            {
                _chkWindowedOnSingle.Checked = _config.WindowedPreviewOnSingleMonitor;
            }

            UpdateSelectedMonitorDetails();
            UpdateStatusUI();
        }

        private void UpdateSelectedMonitorDetails()
        {
            if (_cbMonitors.SelectedItem is MonitorItem item && _lblSelectedMonitorDetails != null)
            {
                string type = item.Screen.Primary ? "Monitor Principal do Windows" : "Monitor Secundário (Recomendado para NO AR)";
                _lblSelectedMonitorDetails.Text = $"Resolução: {item.Screen.Bounds.Width} × {item.Screen.Bounds.Height} px | {type}";
            }
        }

        public void UpdateStatusUI()
        {
            if (_isWizardMode || _lblStatusBadge == null) return;

            bool isTemporarilyDisabled = _appContext.IsTemporarilyDisabled;
            if (isTemporarilyDisabled)
            {
                _lblStatusBadge.Text = "🟠 DESATIVADO (TEMPORÁRIO)";
                _lblStatusBadge.ForeColor = Color.FromArgb(255, 160, 40);
                _lblStatusDesc.Text = "Pausado nesta sessão. Voltará a abrir ao reiniciar o computador.";
                _btnTemporaryToggle.Text = "Reativar Agora";
                _btnTemporaryToggle.BackColor = Color.FromArgb(32, 85, 52);
                _btnTemporaryToggle.ForeColor = Color.White;
            }
            else
            {
                _lblStatusBadge.Text = "🟢 SINAL ATIVO";
                _lblStatusBadge.ForeColor = Color.FromArgb(70, 220, 130);
                _lblStatusDesc.Text = "Exibindo em tela cheia no monitor selecionado.";
                _btnTemporaryToggle.Text = "Pausar nesta sessão";
                _btnTemporaryToggle.BackColor = Color.FromArgb(46, 38, 64);
                _btnTemporaryToggle.ForeColor = Color.FromArgb(255, 195, 100);
            }
        }

        private void BtnTemporaryToggle_Click(object? sender, EventArgs e)
        {
            if (_appContext.IsTemporarilyDisabled)
            {
                _appContext.Reactivate();
            }
            else
            {
                _appContext.DeactivateTemporarily();
            }
            UpdateStatusUI();
        }

        private void BtnApply_Click(object? sender, EventArgs e)
        {
            if (_cbMonitors.SelectedItem is MonitorItem selectedItem)
            {
                _config.SelectedMonitorIndex = selectedItem.Index;
                _config.SelectedMonitorDeviceName = selectedItem.Screen.DeviceName;
                _config.SelectedMonitorWidth = selectedItem.Screen.Bounds.Width;
                _config.SelectedMonitorHeight = selectedItem.Screen.Bounds.Height;
            }

            string url = _txtUrl.Text.Trim();
            if (!string.IsNullOrEmpty(url))
            {
                if (!url.StartsWith("http://") && !url.StartsWith("https://"))
                {
                    url = "https://" + url;
                }
                _config.Url = url;
            }

            _config.AutoStart = _chkAutoStart.Checked;
            if (_chkWindowedOnSingle != null)
            {
                _config.WindowedPreviewOnSingleMonitor = _chkWindowedOnSingle.Checked;
            }
            _config.IsConfigured = true;

            _config.Save();
            AppConfig.SetStartupRegistration(_config.AutoStart);

            if (_isWizardMode)
            {
                bool launchNow = _chkLaunchImmediately?.Checked ?? true;
                if (launchNow)
                {
                    DialogResult = DialogResult.OK;
                }
                else
                {
                    _appContext.DeactivateTemporarily();
                    DialogResult = DialogResult.OK;
                }
                Close();
            }
            else
            {
                _appContext.ApplyConfigurationChanges();
                UpdateStatusUI();
                MessageBox.Show(
                    "Configurações salvas e aplicadas com sucesso!",
                    "Signal No Ar",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information
                );
            }
        }

        private class MonitorItem
        {
            public int Index { get; }
            public Screen Screen { get; }
            public string DisplayText { get; }

            public MonitorItem(int index, Screen screen, string displayText)
            {
                Index = index;
                Screen = screen;
                DisplayText = displayText;
            }

            public override string ToString() => DisplayText;
        }
    }
}
