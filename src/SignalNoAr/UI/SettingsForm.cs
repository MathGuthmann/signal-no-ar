using System;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Forms;
using SignalNoAr.Models;
using SignalNoAr.Services;

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
        private CheckBox _chkVmixEnabled = null!;
        private TextBox _txtVmixUrl = null!;
        private Label _lblVmixStatusBadge = null!;
        private Button _btnTestVmix = null!;
        private CheckBox _chkAutoCheckUpdates = null!;
        private Button _btnCheckUpdates = null!;
        private Label _lblUpdateStatus = null!;
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
            Size = new Size(740, 800);
            MinimumSize = new Size(650, 640);
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.Sizable;
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

                var pnlStatusHeader = new Panel { Dock = DockStyle.Top, Height = 28 };

                _lblStatusBadge = new Label
                {
                    Text = "🟢 SINAL ATIVO",
                    Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                    ForeColor = Color.FromArgb(70, 220, 130),
                    Dock = DockStyle.Left,
                    AutoSize = true
                };

                _btnTemporaryToggle = new Button
                {
                    Text = "Pausar nesta sessão",
                    Dock = DockStyle.Right,
                    Width = 175,
                    Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                    BackColor = Color.FromArgb(46, 38, 64),
                    ForeColor = Color.FromArgb(255, 195, 100),
                    FlatStyle = FlatStyle.Flat,
                    Cursor = Cursors.Hand
                };
                _btnTemporaryToggle.FlatAppearance.BorderColor = Color.FromArgb(70, 60, 95);
                _btnTemporaryToggle.Click += BtnTemporaryToggle_Click;

                pnlStatusHeader.Controls.Add(_btnTemporaryToggle);
                pnlStatusHeader.Controls.Add(_lblStatusBadge);
                pnlStatusCard.Controls.Add(pnlStatusHeader);

                _lblStatusDesc = new Label
                {
                    Text = "Exibindo sinal contínuo no monitor escolhido.",
                    Font = new Font("Segoe UI", 9f, FontStyle.Regular),
                    ForeColor = Color.FromArgb(180, 175, 205),
                    Dock = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleLeft
                };
                pnlStatusCard.Controls.Add(_lblStatusDesc);
                _centerContainer.Controls.Add(pnlStatusCard);

                currentY += 96;
            }

            // 3. Monitor Selection Card
            var pnlMonitorCard = new Panel
            {
                Location = new Point(0, currentY),
                Width = _centerContainer.Width,
                Height = 154,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                BackColor = Color.FromArgb(24, 22, 34),
                Padding = new Padding(16, 12, 16, 12)
            };

            var pnlMonitorHeader = new Panel { Dock = DockStyle.Top, Height = 28 };
            var lblMonitorTitle = new Label
            {
                Text = "🖥️ MONITOR DE EXIBIÇÃO (TELA CHEIA)",
                Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(245, 240, 255),
                Dock = DockStyle.Left,
                AutoSize = true
            };

            _btnIdentify = new Button
            {
                Text = "🔍 Identificar Telas",
                Dock = DockStyle.Right,
                Width = 150,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                BackColor = Color.FromArgb(46, 38, 64),
                ForeColor = Color.FromArgb(255, 190, 80),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            _btnIdentify.FlatAppearance.BorderColor = Color.FromArgb(70, 60, 95);
            _btnIdentify.Click += (s, e) => MonitorIdentifyForm.IdentifyAllScreens();

            pnlMonitorHeader.Controls.Add(_btnIdentify);
            pnlMonitorHeader.Controls.Add(lblMonitorTitle);
            pnlMonitorCard.Controls.Add(pnlMonitorHeader);

            var lblMonitorSub = new Label
            {
                Text = "Selecione a tela onde a página 'NO AR' ficará aberta permanentemente:",
                Font = new Font("Segoe UI", 9f, FontStyle.Regular),
                ForeColor = Color.FromArgb(170, 165, 195),
                Dock = DockStyle.Top,
                Height = 24
            };
            pnlMonitorCard.Controls.Add(lblMonitorSub);

            _cbMonitors = new ComboBox
            {
                Dock = DockStyle.Top,
                Height = 32,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Color.FromArgb(34, 30, 48),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 11f),
                FlatStyle = FlatStyle.Flat
            };
            _cbMonitors.SelectedIndexChanged += (s, e) => UpdateSelectedMonitorDetails();
            pnlMonitorCard.Controls.Add(_cbMonitors);

            _lblSelectedMonitorDetails = new Label
            {
                Text = "Resolução: Carregando...",
                Font = new Font("Segoe UI", 9f, FontStyle.Italic),
                ForeColor = Color.FromArgb(190, 185, 220),
                Dock = DockStyle.Top,
                Height = 26,
                Padding = new Padding(0, 4, 0, 0)
            };
            pnlMonitorCard.Controls.Add(_lblSelectedMonitorDetails);

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
                Padding = new Padding(16, 12, 16, 12)
            };

            var lblUrlTitle = new Label
            {
                Text = "🌐 ENDEREÇO DO SINAL (URL DA PÁGINA)",
                Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(245, 240, 255),
                Dock = DockStyle.Top,
                Height = 26
            };
            pnlUrlCard.Controls.Add(lblUrlTitle);

            _txtUrl = new TextBox
            {
                Dock = DockStyle.Top,
                Height = 32,
                BackColor = Color.FromArgb(34, 30, 48),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10.5f),
                BorderStyle = BorderStyle.FixedSingle,
                Text = _config.Url
            };
            pnlUrlCard.Controls.Add(_txtUrl);

            _centerContainer.Controls.Add(pnlUrlCard);
            currentY += 112;

            // 5. Options & Behavior Card
            var pnlOptionsCard = new Panel
            {
                Location = new Point(0, currentY),
                Width = _centerContainer.Width,
                Height = _isWizardMode ? 100 : 130,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                BackColor = Color.FromArgb(24, 22, 34),
                Padding = new Padding(16, 12, 16, 12)
            };

            var lblOptionsTitle = new Label
            {
                Text = "⚙️ COMPORTAMENTO DO SISTEMA",
                Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(245, 240, 255),
                Dock = DockStyle.Top,
                Height = 26
            };
            pnlOptionsCard.Controls.Add(lblOptionsTitle);

            _chkAutoStart = new CheckBox
            {
                Text = "Iniciar o Signal No Ar automaticamente ao ligar o computador (Recomendado)",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Regular),
                ForeColor = Color.FromArgb(230, 225, 245),
                Dock = DockStyle.Top,
                Height = 28,
                Checked = _config.AutoStart
            };
            pnlOptionsCard.Controls.Add(_chkAutoStart);

            if (_isWizardMode)
            {
                _chkLaunchImmediately = new CheckBox
                {
                    Text = "Abrir em tela cheia imediatamente após salvar as configurações",
                    Font = new Font("Segoe UI", 9.5f, FontStyle.Regular),
                    ForeColor = Color.FromArgb(230, 225, 245),
                    Dock = DockStyle.Top,
                    Height = 28,
                    Checked = true
                };
                pnlOptionsCard.Controls.Add(_chkLaunchImmediately);
            }
            else
            {
                _chkWindowedOnSingle = new CheckBox
                {
                    Text = "Se houver apenas 1 monitor conectado, abrir como janela em vez de cobrir toda a tela",
                    Font = new Font("Segoe UI", 9.5f, FontStyle.Regular),
                    ForeColor = Color.FromArgb(230, 225, 245),
                    Dock = DockStyle.Top,
                    Height = 28,
                    Checked = _config.WindowedPreviewOnSingleMonitor
                };
                pnlOptionsCard.Controls.Add(_chkWindowedOnSingle);
            }

            _centerContainer.Controls.Add(pnlOptionsCard);
            currentY += pnlOptionsCard.Height + 14;

            // 6. vMix Integration Card
            var pnlVmixCard = new Panel
            {
                Location = new Point(0, currentY),
                Width = _centerContainer.Width,
                Height = 150,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                BackColor = Color.FromArgb(24, 22, 34),
                Padding = new Padding(16, 12, 16, 12)
            };

            var pnlVmixHeader = new Panel { Dock = DockStyle.Top, Height = 28 };
            var lblVmixTitle = new Label
            {
                Text = "📡 LEITOR AUTOMÁTICO DO VMIX (AO VIVO / STANDBY)",
                Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(245, 240, 255),
                Dock = DockStyle.Left,
                AutoSize = true
            };

            _btnTestVmix = new Button
            {
                Text = "⚡ Testar Conexão vMix",
                Dock = DockStyle.Right,
                Width = 175,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                BackColor = Color.FromArgb(46, 38, 64),
                ForeColor = Color.FromArgb(120, 220, 255),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            _btnTestVmix.FlatAppearance.BorderColor = Color.FromArgb(60, 80, 120);
            _btnTestVmix.Click += async (s, e) => await TestVmixConnectionAsync();

            pnlVmixHeader.Controls.Add(_btnTestVmix);
            pnlVmixHeader.Controls.Add(lblVmixTitle);
            pnlVmixCard.Controls.Add(pnlVmixHeader);

            _chkVmixEnabled = new CheckBox
            {
                Text = "Ativar monitoramento automático do vMix neste computador",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Regular),
                ForeColor = Color.FromArgb(230, 225, 245),
                Dock = DockStyle.Top,
                Height = 28,
                Checked = _config.VmixEnabled
            };
            pnlVmixCard.Controls.Add(_chkVmixEnabled);

            var pnlVmixUrlRow = new Panel { Dock = DockStyle.Top, Height = 34 };
            var lblVmixUrlPrefix = new Label
            {
                Text = "URL API vMix:",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(190, 185, 215),
                Dock = DockStyle.Left,
                Width = 110,
                TextAlign = ContentAlignment.MiddleLeft
            };
            pnlVmixUrlRow.Controls.Add(lblVmixUrlPrefix);

            _txtVmixUrl = new TextBox
            {
                Dock = DockStyle.Fill,
                Height = 32,
                BackColor = Color.FromArgb(34, 30, 48),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10.5f),
                BorderStyle = BorderStyle.FixedSingle,
                Text = _config.VmixUrl
            };
            pnlVmixUrlRow.Controls.Add(_txtVmixUrl);
            pnlVmixCard.Controls.Add(pnlVmixUrlRow);

            _lblVmixStatusBadge = new Label
            {
                Text = "Status: Aguardando verificação...",
                Font = new Font("Segoe UI", 9f, FontStyle.Regular),
                ForeColor = Color.FromArgb(170, 165, 195),
                Dock = DockStyle.Top,
                Height = 26
            };
            pnlVmixCard.Controls.Add(_lblVmixStatusBadge);

            _centerContainer.Controls.Add(pnlVmixCard);
            currentY += pnlVmixCard.Height + 14;

            // 7. Updates Section Card
            var pnlUpdatesCard = new Panel
            {
                Location = new Point(0, currentY),
                Width = _centerContainer.Width,
                Height = 145,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                BackColor = Color.FromArgb(24, 22, 34),
                Padding = new Padding(16, 12, 16, 12)
            };

            var pnlUpdatesHeader = new Panel { Dock = DockStyle.Top, Height = 28 };
            var lblUpdatesTitle = new Label
            {
                Text = "✨ ATUALIZAÇÕES DO SIGNAL NO AR",
                Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(245, 240, 255),
                Dock = DockStyle.Left,
                AutoSize = true
            };
            var lblVersionBadge = new Label
            {
                Text = $"Versão Atual: v{UpdateService.AppVersion}",
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = Color.FromArgb(255, 120, 0),
                Dock = DockStyle.Right,
                AutoSize = true
            };
            pnlUpdatesHeader.Controls.Add(lblVersionBadge);
            pnlUpdatesHeader.Controls.Add(lblUpdatesTitle);
            pnlUpdatesCard.Controls.Add(pnlUpdatesHeader);

            _chkAutoCheckUpdates = new CheckBox
            {
                Text = "Verificar e atualizar automaticamente em segundo plano ao ligar o computador",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Regular),
                ForeColor = Color.FromArgb(230, 225, 245),
                Dock = DockStyle.Top,
                Height = 28,
                Checked = _config.AutoCheckUpdates
            };
            pnlUpdatesCard.Controls.Add(_chkAutoCheckUpdates);

            var pnlUpdateActionRow = new Panel { Dock = DockStyle.Top, Height = 36, Padding = new Padding(0, 4, 0, 0) };
            _btnCheckUpdates = new Button
            {
                Text = "🔍 Procurar Atualizações Agora",
                Dock = DockStyle.Left,
                Width = 230,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                BackColor = Color.FromArgb(46, 38, 64),
                ForeColor = Color.FromArgb(255, 200, 100),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            _btnCheckUpdates.FlatAppearance.BorderColor = Color.FromArgb(80, 70, 110);
            _btnCheckUpdates.Click += async (s, e) => await CheckUpdatesFromSettingsAsync();
            pnlUpdateActionRow.Controls.Add(_btnCheckUpdates);

            _lblUpdateStatus = new Label
            {
                Text = "Status: Clique para buscar novas versões lançadas no GitHub.",
                Font = new Font("Segoe UI", 9f, FontStyle.Regular),
                ForeColor = Color.FromArgb(170, 165, 195),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(12, 0, 0, 0)
            };
            pnlUpdateActionRow.Controls.Add(_lblUpdateStatus);
            pnlUpdatesCard.Controls.Add(pnlUpdateActionRow);

            _centerContainer.Controls.Add(pnlUpdatesCard);
            currentY += pnlUpdatesCard.Height + 14;

            // 8. Temporary Pause Notice Card
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
                Text = "💡 Se desativar temporariamente, o sinal fecha nesta sessão e reabre sozinho quando o computador reiniciar.",
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
                var s = screens[i];
                string tag = s.Primary ? " [Principal]" : " [Secundário]";
                string label = $"Monitor {i + 1}: {s.Bounds.Width}x{s.Bounds.Height}{tag}";

                var item = new MonitorItem(i, s, label);
                _cbMonitors.Items.Add(item);

                if (!string.IsNullOrEmpty(_config.SelectedMonitorDeviceName) &&
                    string.Equals(s.DeviceName, _config.SelectedMonitorDeviceName, StringComparison.OrdinalIgnoreCase))
                {
                    defaultIndex = i;
                }
                else if (_config.SelectedMonitorIndex == i && string.IsNullOrEmpty(_config.SelectedMonitorDeviceName))
                {
                    defaultIndex = i;
                }
            }

            if (_cbMonitors.Items.Count > 0)
            {
                _cbMonitors.SelectedIndex = Math.Clamp(defaultIndex, 0, _cbMonitors.Items.Count - 1);
            }

            if (_chkAutoCheckUpdates != null)
            {
                _chkAutoCheckUpdates.Checked = _config.AutoCheckUpdates;
            }

            UpdateSelectedMonitorDetails();
            UpdateStatusUI();
        }

        private async Task CheckUpdatesFromSettingsAsync()
        {
            if (_btnCheckUpdates == null || _lblUpdateStatus == null) return;

            _btnCheckUpdates.Enabled = false;
            _lblUpdateStatus.Text = "Status: Conectando aos servidores do GitHub...";
            _lblUpdateStatus.ForeColor = Color.FromArgb(255, 200, 80);

            var info = await UpdateService.CheckForUpdatesAsync();

            if (!string.IsNullOrEmpty(info.ErrorMessage))
            {
                _lblUpdateStatus.Text = $"Status: Erro ao verificar ({info.ErrorMessage})";
                _lblUpdateStatus.ForeColor = Color.FromArgb(255, 100, 100);
                _btnCheckUpdates.Enabled = true;
                return;
            }

            if (!info.HasUpdate)
            {
                _lblUpdateStatus.Text = $"Status: ✅ Você está na versão mais recente (v{info.CurrentVersion})!";
                _lblUpdateStatus.ForeColor = Color.FromArgb(70, 220, 130);
                _btnCheckUpdates.Enabled = true;
                return;
            }

            _lblUpdateStatus.Text = $"Status: ✨ Nova versão v{info.LatestVersion} disponível!";
            _lblUpdateStatus.ForeColor = Color.FromArgb(255, 120, 0);

            string msg = $"Uma nova versão do Signal No Ar está disponível!\n\n" +
                         $"Versão Atual: v{info.CurrentVersion}\n" +
                         $"Nova Versão: v{info.LatestVersion}\n\n" +
                         $"Novidades:\n{info.ReleaseNotes}\n\n" +
                         $"Deseja baixar e atualizar agora?";

            var result = MessageBox.Show(msg, "Nova Versão Encontrada", MessageBoxButtons.YesNo, MessageBoxIcon.Information);
            if (result == DialogResult.Yes)
            {
                _lblUpdateStatus.Text = "Status: ⬇️ Baixando atualização...";
                _lblUpdateStatus.ForeColor = Color.FromArgb(70, 220, 130);

                try
                {
                    await UpdateService.DownloadAndInstallUpdateAsync(info.DownloadUrl, autoMode: false, progress =>
                    {
                        if (InvokeRequired)
                        {
                            Invoke(new Action(() => _lblUpdateStatus.Text = $"Status: ⬇️ Baixando atualização... {progress}%"));
                        }
                        else
                        {
                            _lblUpdateStatus.Text = $"Status: ⬇️ Baixando atualização... {progress}%";
                        }
                    });
                }
                catch (Exception ex)
                {
                    _lblUpdateStatus.Text = $"Status: Erro no download: {ex.Message}";
                    _lblUpdateStatus.ForeColor = Color.FromArgb(255, 100, 100);
                    _btnCheckUpdates.Enabled = true;
                }
            }
            else
            {
                _btnCheckUpdates.Enabled = true;
            }
        }

        private async Task TestVmixConnectionAsync()
        {
            if (_lblVmixStatusBadge == null || _btnTestVmix == null) return;

            _btnTestVmix.Enabled = false;
            _lblVmixStatusBadge.Text = "Status: ⏳ Verificando API do vMix...";
            _lblVmixStatusBadge.ForeColor = Color.FromArgb(255, 200, 80);

            string url = _txtVmixUrl?.Text?.Trim() ?? _config.VmixUrl;
            var state = await _appContext.VmixWatcher.CheckNowAsync(url);

            if (state.IsConnected)
            {
                if (state.IsStreaming)
                {
                    _lblVmixStatusBadge.Text = $"Status: 🔴 vMix CONECTADO (v{state.Version}) — AO VIVO / STREAMING! (PGM: Entrada {state.ActiveInput})";
                    _lblVmixStatusBadge.ForeColor = Color.FromArgb(255, 80, 80);
                }
                else
                {
                    _lblVmixStatusBadge.Text = $"Status: 🟡 vMix CONECTADO (v{state.Version}) — Standby / Offline (PGM: Entrada {state.ActiveInput})";
                    _lblVmixStatusBadge.ForeColor = Color.FromArgb(255, 210, 80);
                }
            }
            else
            {
                _lblVmixStatusBadge.Text = $"Status: ⚪ vMix não respondeu na porta 8088 (vMix fechado ou URL inválida)";
                _lblVmixStatusBadge.ForeColor = Color.FromArgb(180, 175, 205);
            }

            _btnTestVmix.Enabled = true;
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
                _lblStatusBadge.Text = "🟡 DESATIVADO (TEMPORÁRIO)";
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
            if (_chkVmixEnabled != null)
            {
                _config.VmixEnabled = _chkVmixEnabled.Checked;
            }
            if (_txtVmixUrl != null)
            {
                _config.VmixUrl = _txtVmixUrl.Text.Trim();
            }
            if (_chkAutoCheckUpdates != null)
            {
                _config.AutoCheckUpdates = _chkAutoCheckUpdates.Checked;
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
