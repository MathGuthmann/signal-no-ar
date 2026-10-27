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

        // Display 1 (No Ar)
        private ComboBox _cbMonitors = null!;
        private TextBox _txtUrl = null!;
        private Label _lblSelectedMonitorDetails = null!;

        // Display 2 (Palco / Retorno - Multi-Monitor)
        private CheckBox _chkSecondDisplayEnabled = null!;
        private ComboBox _cbSecondDisplayMonitors = null!;
        private TextBox _txtSecondDisplayUrl = null!;
        private Label _lblSecondDisplayMonitorDetails = null!;

        // System Options & Power
        private CheckBox _chkAutoStart = null!;
        private CheckBox _chkPreventSleep = null!;
        private CheckBox _chkLaunchImmediately = null!;
        private CheckBox _chkWindowedOnSingle = null!;

        // vMix
        private CheckBox _chkVmixEnabled = null!;
        private TextBox _txtVmixUrl = null!;
        private Label _lblVmixStatusBadge = null!;
        private Button _btnTestVmix = null!;

        // Holyrics
        private CheckBox _chkHolyricsEnabled = null!;
        private TextBox _txtHolyricsUrl = null!;
        private TextBox _txtHolyricsToken = null!;
        private Label _lblHolyricsStatusBadge = null!;
        private Button _btnTestHolyrics = null!;

        // Updates
        private CheckBox _chkAutoCheckUpdates = null!;
        private Button _btnCheckUpdates = null!;
        private Label _lblUpdateStatus = null!;

        // Session controls
        private Button _btnTemporaryToggle = null!;
        private Label _lblStatusBadge = null!;
        private Label _lblStatusDesc = null!;
        private Button _btnApply = null!;
        private Button _btnIdentify = null!;
        private Panel _pnlSingleMonitorWarning = null!;

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
            Size = new Size(780, 840);
            MinimumSize = new Size(680, 680);
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
                Height = 120,
                BackColor = Color.FromArgb(22, 20, 32),
                Padding = new Padding(24, 12, 24, 10)
            };

            var picHeaderLogo = new PictureBox
            {
                Dock = DockStyle.Top,
                Height = 48,
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
                    : "Gerencie a exibição em tela cheia, selecione monitores, integre vMix/Holyrics ou pause.",
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
                Height = 68,
                BackColor = Color.FromArgb(18, 17, 26),
                Padding = new Padding(24, 12, 24, 12)
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

            // Centered Card Container
            _centerContainer = new Panel
            {
                Location = new Point(20, 10),
                Width = Math.Max(600, scrollPanel.ClientSize.Width - 40),
                BackColor = Color.Transparent
            };
            scrollPanel.Controls.Add(_centerContainer);

            scrollPanel.Resize += (s, e) =>
            {
                int availableWidth = scrollPanel.ClientSize.Width - 40;
                int targetWidth = Math.Min(840, Math.Max(600, availableWidth));
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
                Height = 84,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                BackColor = Color.FromArgb(44, 30, 18),
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

            // 3. Display 1 (No Ar) Card
            var pnlDisplay1Card = new Panel
            {
                Location = new Point(0, currentY),
                Width = _centerContainer.Width,
                Height = 220,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                BackColor = Color.FromArgb(24, 22, 34),
                Padding = new Padding(16, 12, 16, 12)
            };

            var pnlDisplay1Header = new Panel { Dock = DockStyle.Top, Height = 28 };
            var lblDisplay1Title = new Label
            {
                Text = "🖥️ DISPLAY 1 — SINAL PRINCIPAL (NO AR)",
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
                Height = 26,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                BackColor = Color.FromArgb(46, 38, 64),
                ForeColor = Color.FromArgb(255, 190, 80),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            _btnIdentify.FlatAppearance.BorderColor = Color.FromArgb(70, 60, 95);
            _btnIdentify.Click += (s, e) => MonitorIdentifyForm.IdentifyAllScreens();

            pnlDisplay1Header.Controls.Add(_btnIdentify);
            pnlDisplay1Header.Controls.Add(lblDisplay1Title);
            pnlDisplay1Card.Controls.Add(pnlDisplay1Header);

            var lblMonitorSub = new Label
            {
                Text = "Monitor de exibição em tela cheia:",
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = Color.FromArgb(180, 175, 205),
                Dock = DockStyle.Top,
                Height = 22
            };
            pnlDisplay1Card.Controls.Add(lblMonitorSub);

            _cbMonitors = new ComboBox
            {
                Dock = DockStyle.Top,
                Height = 32,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Color.FromArgb(34, 30, 48),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10.5f),
                FlatStyle = FlatStyle.Flat
            };
            _cbMonitors.SelectedIndexChanged += (s, e) => UpdateSelectedMonitorDetails();
            pnlDisplay1Card.Controls.Add(_cbMonitors);

            _lblSelectedMonitorDetails = new Label
            {
                Text = "Resolução: Carregando...",
                Font = new Font("Segoe UI", 8.5f, FontStyle.Italic),
                ForeColor = Color.FromArgb(190, 185, 220),
                Dock = DockStyle.Top,
                Height = 22,
                Padding = new Padding(0, 3, 0, 0)
            };
            pnlDisplay1Card.Controls.Add(_lblSelectedMonitorDetails);

            var lblUrlTitle = new Label
            {
                Text = "URL do Display 1:",
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = Color.FromArgb(180, 175, 205),
                Dock = DockStyle.Top,
                Height = 22
            };
            pnlDisplay1Card.Controls.Add(lblUrlTitle);

            _txtUrl = new TextBox
            {
                Dock = DockStyle.Top,
                Height = 30,
                BackColor = Color.FromArgb(34, 30, 48),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10f),
                BorderStyle = BorderStyle.FixedSingle,
                Text = _config.Url
            };
            pnlDisplay1Card.Controls.Add(_txtUrl);

            _centerContainer.Controls.Add(pnlDisplay1Card);
            currentY += pnlDisplay1Card.Height + 14;

            // 4. Display 2 (Retorno / Palco - Multi-Monitor) Card
            var pnlDisplay2Card = new Panel
            {
                Location = new Point(0, currentY),
                Width = _centerContainer.Width,
                Height = 220,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                BackColor = Color.FromArgb(24, 22, 34),
                Padding = new Padding(16, 12, 16, 12)
            };

            var pnlDisplay2Header = new Panel { Dock = DockStyle.Top, Height = 28 };
            var lblDisplay2Title = new Label
            {
                Text = "🎤 DISPLAY 2 — RETORNO / PALCO (OPCIONAL)",
                Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(245, 240, 255),
                Dock = DockStyle.Left,
                AutoSize = true
            };
            pnlDisplay2Header.Controls.Add(lblDisplay2Title);
            pnlDisplay2Card.Controls.Add(pnlDisplay2Header);

            _chkSecondDisplayEnabled = new CheckBox
            {
                Text = "Ativar Segunda Tela Simultânea neste computador (ex: Monitor de Palco dos músicos)",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(255, 195, 100),
                Dock = DockStyle.Top,
                Height = 28,
                Checked = _config.SecondDisplayEnabled
            };
            _chkSecondDisplayEnabled.CheckedChanged += (s, e) =>
            {
                bool enabled = _chkSecondDisplayEnabled.Checked;
                if (_cbSecondDisplayMonitors != null) _cbSecondDisplayMonitors.Enabled = enabled;
                if (_txtSecondDisplayUrl != null) _txtSecondDisplayUrl.Enabled = enabled;
            };
            pnlDisplay2Card.Controls.Add(_chkSecondDisplayEnabled);

            var lblSecondMonitorSub = new Label
            {
                Text = "Monitor para o Display 2 (Palco):",
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = Color.FromArgb(180, 175, 205),
                Dock = DockStyle.Top,
                Height = 22
            };
            pnlDisplay2Card.Controls.Add(lblSecondMonitorSub);

            _cbSecondDisplayMonitors = new ComboBox
            {
                Dock = DockStyle.Top,
                Height = 32,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Color.FromArgb(34, 30, 48),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10.5f),
                FlatStyle = FlatStyle.Flat,
                Enabled = _config.SecondDisplayEnabled
            };
            _cbSecondDisplayMonitors.SelectedIndexChanged += (s, e) => UpdateSecondMonitorDetails();
            pnlDisplay2Card.Controls.Add(_cbSecondDisplayMonitors);

            _lblSecondDisplayMonitorDetails = new Label
            {
                Text = "Resolução: Carregando...",
                Font = new Font("Segoe UI", 8.5f, FontStyle.Italic),
                ForeColor = Color.FromArgb(190, 185, 220),
                Dock = DockStyle.Top,
                Height = 22,
                Padding = new Padding(0, 3, 0, 0)
            };
            pnlDisplay2Card.Controls.Add(_lblSecondDisplayMonitorDetails);

            var lblSecondUrlTitle = new Label
            {
                Text = "URL do Display 2 (Palco / Retorno):",
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = Color.FromArgb(180, 175, 205),
                Dock = DockStyle.Top,
                Height = 22
            };
            pnlDisplay2Card.Controls.Add(lblSecondUrlTitle);

            _txtSecondDisplayUrl = new TextBox
            {
                Dock = DockStyle.Top,
                Height = 30,
                BackColor = Color.FromArgb(34, 30, 48),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10f),
                BorderStyle = BorderStyle.FixedSingle,
                Text = _config.SecondDisplayUrl,
                Enabled = _config.SecondDisplayEnabled
            };
            pnlDisplay2Card.Controls.Add(_txtSecondDisplayUrl);

            _centerContainer.Controls.Add(pnlDisplay2Card);
            currentY += pnlDisplay2Card.Height + 14;

            // 5. Options & Behavior Card
            var pnlOptionsCard = new Panel
            {
                Location = new Point(0, currentY),
                Width = _centerContainer.Width,
                Height = _isWizardMode ? 140 : 160,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                BackColor = Color.FromArgb(24, 22, 34),
                Padding = new Padding(16, 12, 16, 12)
            };

            var lblOptionsTitle = new Label
            {
                Text = "⚙️ COMPORTAMENTO & ENERGIA (KIOSK)",
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

            _chkPreventSleep = new CheckBox
            {
                Text = "Impedir suspensão de tela e descanso do Windows durante a transmissão (Sleep Blocker)",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Regular),
                ForeColor = Color.FromArgb(230, 225, 245),
                Dock = DockStyle.Top,
                Height = 28,
                Checked = _config.PreventSleep
            };
            pnlOptionsCard.Controls.Add(_chkPreventSleep);

            if (_isWizardMode)
            {
                _chkLaunchImmediately = new CheckBox
                {
                    Text = "Abrir o sinal em tela cheia imediatamente após salvar",
                    Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                    ForeColor = Color.FromArgb(255, 120, 0),
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
                    Text = "Abrir em modo janela de teste quando houver apenas 1 monitor conectado",
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
                Height = 155,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                BackColor = Color.FromArgb(24, 22, 34),
                Padding = new Padding(16, 12, 16, 12)
            };

            var pnlVmixHeader = new Panel { Dock = DockStyle.Top, Height = 28 };
            var lblVmixTitle = new Label
            {
                Text = "🔴 INTEGRAÇÃO vMIX (SWITCHER AO VIVO)",
                Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(245, 240, 255),
                Dock = DockStyle.Left,
                AutoSize = true
            };

            _btnTestVmix = new Button
            {
                Text = "Testar Conexão vMix",
                Dock = DockStyle.Right,
                Width = 150,
                Height = 26,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                BackColor = Color.FromArgb(46, 38, 64),
                ForeColor = Color.FromArgb(255, 180, 100),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            _btnTestVmix.FlatAppearance.BorderColor = Color.FromArgb(120, 80, 50);
            _btnTestVmix.Click += async (s, e) => await TestVmixConnectionAsync();

            pnlVmixHeader.Controls.Add(_btnTestVmix);
            pnlVmixHeader.Controls.Add(lblVmixTitle);
            pnlVmixCard.Controls.Add(pnlVmixHeader);

            _chkVmixEnabled = new CheckBox
            {
                Text = "Monitorar status do vMix na rede local (Streaming / Gravação com latência zero)",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Regular),
                ForeColor = Color.FromArgb(230, 225, 245),
                Dock = DockStyle.Top,
                Height = 28,
                Checked = _config.VmixEnabled
            };
            _chkVmixEnabled.CheckedChanged += (s, e) =>
            {
                if (_txtVmixUrl != null) _txtVmixUrl.Enabled = _chkVmixEnabled.Checked;
                if (_btnTestVmix != null) _btnTestVmix.Enabled = _chkVmixEnabled.Checked;
            };
            pnlVmixCard.Controls.Add(_chkVmixEnabled);

            var pnlVmixUrlRow = new Panel { Dock = DockStyle.Top, Height = 34 };
            var lblVmixUrlPrefix = new Label
            {
                Text = "URL da API do vMix:",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(190, 185, 215),
                Dock = DockStyle.Left,
                Width = 140,
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

            // 7. Holyrics Integration Card
            var pnlHolyricsCard = new Panel
            {
                Location = new Point(0, currentY),
                Width = _centerContainer.Width,
                Height = 195,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                BackColor = Color.FromArgb(24, 22, 34),
                Padding = new Padding(16, 12, 16, 12)
            };

            var pnlHolyricsHeader = new Panel { Dock = DockStyle.Top, Height = 28 };
            var lblHolyricsTitle = new Label
            {
                Text = "🎵 INTEGRAÇÃO HOLYRICS (LETRA & BÍBLIA)",
                Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(245, 240, 255),
                Dock = DockStyle.Left,
                AutoSize = true
            };

            _btnTestHolyrics = new Button
            {
                Text = "Testar Conexão Holyrics",
                Dock = DockStyle.Right,
                Width = 165,
                Height = 26,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                BackColor = Color.FromArgb(46, 38, 64),
                ForeColor = Color.FromArgb(255, 180, 100),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            _btnTestHolyrics.FlatAppearance.BorderColor = Color.FromArgb(120, 80, 50);
            _btnTestHolyrics.Click += async (s, e) => await TestHolyricsConnectionAsync();

            pnlHolyricsHeader.Controls.Add(_btnTestHolyrics);
            pnlHolyricsHeader.Controls.Add(lblHolyricsTitle);
            pnlHolyricsCard.Controls.Add(pnlHolyricsHeader);

            _chkHolyricsEnabled = new CheckBox
            {
                Text = "Ativar leitura local do Holyrics na rede local (latência zero para palco e estúdio)",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Regular),
                ForeColor = Color.FromArgb(230, 225, 245),
                Dock = DockStyle.Top,
                Height = 28,
                Checked = _config.HolyricsEnabled
            };
            _chkHolyricsEnabled.CheckedChanged += (s, e) =>
            {
                bool enabled = _chkHolyricsEnabled.Checked;
                if (_txtHolyricsUrl != null) _txtHolyricsUrl.Enabled = enabled;
                if (_txtHolyricsToken != null) _txtHolyricsToken.Enabled = enabled;
                if (_btnTestHolyrics != null) _btnTestHolyrics.Enabled = enabled;
            };
            pnlHolyricsCard.Controls.Add(_chkHolyricsEnabled);

            var pnlHolyricsUrlRow = new Panel { Dock = DockStyle.Top, Height = 34 };
            var lblHolyricsUrlPrefix = new Label
            {
                Text = "URL / IP Holyrics:",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(190, 185, 215),
                Dock = DockStyle.Left,
                Width = 140,
                TextAlign = ContentAlignment.MiddleLeft
            };
            pnlHolyricsUrlRow.Controls.Add(lblHolyricsUrlPrefix);

            _txtHolyricsUrl = new TextBox
            {
                Dock = DockStyle.Fill,
                Height = 32,
                BackColor = Color.FromArgb(34, 30, 48),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10.5f),
                BorderStyle = BorderStyle.FixedSingle,
                Text = _config.HolyricsUrl
            };
            pnlHolyricsUrlRow.Controls.Add(_txtHolyricsUrl);
            pnlHolyricsCard.Controls.Add(pnlHolyricsUrlRow);

            var pnlHolyricsTokenRow = new Panel { Dock = DockStyle.Top, Height = 34 };
            var lblHolyricsTokenPrefix = new Label
            {
                Text = "Token API (Opcional):",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(190, 185, 215),
                Dock = DockStyle.Left,
                Width = 140,
                TextAlign = ContentAlignment.MiddleLeft
            };
            pnlHolyricsTokenRow.Controls.Add(lblHolyricsTokenPrefix);

            _txtHolyricsToken = new TextBox
            {
                Dock = DockStyle.Fill,
                Height = 32,
                BackColor = Color.FromArgb(34, 30, 48),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10.5f),
                BorderStyle = BorderStyle.FixedSingle,
                Text = _config.HolyricsToken
            };
            pnlHolyricsTokenRow.Controls.Add(_txtHolyricsToken);
            pnlHolyricsCard.Controls.Add(pnlHolyricsTokenRow);

            _lblHolyricsStatusBadge = new Label
            {
                Text = "Status: Aguardando verificação...",
                Font = new Font("Segoe UI", 9f, FontStyle.Regular),
                ForeColor = Color.FromArgb(170, 165, 195),
                Dock = DockStyle.Top,
                Height = 26
            };
            pnlHolyricsCard.Controls.Add(_lblHolyricsStatusBadge);

            _centerContainer.Controls.Add(pnlHolyricsCard);
            currentY += pnlHolyricsCard.Height + 14;

            // 8. Updates Section Card
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

            _btnCheckUpdates = new Button
            {
                Text = "Procurar Atualizações Agora",
                Dock = DockStyle.Right,
                Width = 195,
                Height = 26,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                BackColor = Color.FromArgb(46, 38, 64),
                ForeColor = Color.FromArgb(100, 220, 140),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            _btnCheckUpdates.FlatAppearance.BorderColor = Color.FromArgb(50, 100, 70);
            _btnCheckUpdates.Click += async (s, e) => await CheckUpdatesFromSettingsAsync();

            pnlUpdatesHeader.Controls.Add(_btnCheckUpdates);
            pnlUpdatesHeader.Controls.Add(lblUpdatesTitle);
            pnlUpdatesCard.Controls.Add(pnlUpdatesHeader);

            _chkAutoCheckUpdates = new CheckBox
            {
                Text = "Verificar e aplicar atualizações automaticamente ao iniciar o Windows (Recomendado)",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Regular),
                ForeColor = Color.FromArgb(230, 225, 245),
                Dock = DockStyle.Top,
                Height = 28,
                Checked = _config.AutoCheckUpdates
            };
            pnlUpdatesCard.Controls.Add(_chkAutoCheckUpdates);

            var lblCurrentVersion = new Label
            {
                Text = $"Versão Instalada: v{UpdateService.CurrentVersion}",
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = Color.FromArgb(180, 175, 210),
                Dock = DockStyle.Top,
                Height = 22
            };
            pnlUpdatesCard.Controls.Add(lblCurrentVersion);

            _lblUpdateStatus = new Label
            {
                Text = "Status: O Signal No Ar está configurado para manter-se sempre atualizado.",
                Font = new Font("Segoe UI", 9f, FontStyle.Regular),
                ForeColor = Color.FromArgb(160, 155, 185),
                Dock = DockStyle.Top,
                Height = 26
            };
            pnlUpdatesCard.Controls.Add(_lblUpdateStatus);

            _centerContainer.Controls.Add(pnlUpdatesCard);
            currentY += pnlUpdatesCard.Height + 14;

            // 9. Temporary Pause Notice Card
            var pnlNotice = new Panel
            {
                Location = new Point(0, currentY),
                Width = _centerContainer.Width,
                Height = 55,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                BackColor = Color.FromArgb(20, 18, 28),
                Padding = new Padding(14, 8, 14, 8)
            };

            var lblNotice = new Label
            {
                Text = "💡 Dica: Se pausar temporariamente, as telas fecham nesta sessão e reabrem sozinhas quando o computador reiniciar ou pelo menu da bandeja.",
                Font = new Font("Segoe UI", 9f, FontStyle.Regular),
                ForeColor = Color.FromArgb(190, 185, 220),
                Dock = DockStyle.Fill
            };
            pnlNotice.Controls.Add(lblNotice);
            _centerContainer.Controls.Add(pnlNotice);
            currentY += 65;

            _centerContainer.Height = currentY + 30;
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
            _cbSecondDisplayMonitors.Items.Clear();

            Screen[] screens = Screen.AllScreens;
            int defaultDisplay1Index = 0;
            int defaultDisplay2Index = screens.Length > 1 ? 1 : 0;

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
                        defaultDisplay1Index = i;
                        break;
                    }
                }
            }

            for (int i = 0; i < screens.Length; i++)
            {
                var s = screens[i];
                string tag = s.Primary ? " [Principal]" : " [Secundário]";
                string label = $"Monitor {i + 1}: {s.Bounds.Width}x{s.Bounds.Height}{tag}";

                var item1 = new MonitorItem(i, s, label);
                var item2 = new MonitorItem(i, s, label);

                _cbMonitors.Items.Add(item1);
                _cbSecondDisplayMonitors.Items.Add(item2);

                if (!string.IsNullOrEmpty(_config.SelectedMonitorDeviceName) &&
                    string.Equals(s.DeviceName, _config.SelectedMonitorDeviceName, StringComparison.OrdinalIgnoreCase))
                {
                    defaultDisplay1Index = i;
                }
                else if (_config.SelectedMonitorIndex == i && string.IsNullOrEmpty(_config.SelectedMonitorDeviceName))
                {
                    defaultDisplay1Index = i;
                }

                if (!string.IsNullOrEmpty(_config.SecondDisplayMonitorDeviceName) &&
                    string.Equals(s.DeviceName, _config.SecondDisplayMonitorDeviceName, StringComparison.OrdinalIgnoreCase))
                {
                    defaultDisplay2Index = i;
                }
                else if (_config.SecondDisplayMonitorIndex == i && string.IsNullOrEmpty(_config.SecondDisplayMonitorDeviceName))
                {
                    defaultDisplay2Index = i;
                }
            }

            if (_cbMonitors.Items.Count > 0)
            {
                _cbMonitors.SelectedIndex = Math.Clamp(defaultDisplay1Index, 0, _cbMonitors.Items.Count - 1);
            }

            if (_cbSecondDisplayMonitors.Items.Count > 0)
            {
                _cbSecondDisplayMonitors.SelectedIndex = Math.Clamp(defaultDisplay2Index, 0, _cbSecondDisplayMonitors.Items.Count - 1);
            }

            if (_txtUrl != null) _txtUrl.Text = _config.Url;
            if (_chkSecondDisplayEnabled != null) _chkSecondDisplayEnabled.Checked = _config.SecondDisplayEnabled;
            if (_txtSecondDisplayUrl != null) _txtSecondDisplayUrl.Text = _config.SecondDisplayUrl;

            if (_chkAutoStart != null) _chkAutoStart.Checked = _config.AutoStart;
            if (_chkPreventSleep != null) _chkPreventSleep.Checked = _config.PreventSleep;
            if (_chkWindowedOnSingle != null) _chkWindowedOnSingle.Checked = _config.WindowedPreviewOnSingleMonitor;

            if (_chkVmixEnabled != null)
            {
                _chkVmixEnabled.Checked = _config.VmixEnabled;
                if (_txtVmixUrl != null) _txtVmixUrl.Enabled = _config.VmixEnabled;
                if (_btnTestVmix != null) _btnTestVmix.Enabled = _config.VmixEnabled;
            }
            if (_txtVmixUrl != null) _txtVmixUrl.Text = _config.VmixUrl;

            if (_chkHolyricsEnabled != null)
            {
                _chkHolyricsEnabled.Checked = _config.HolyricsEnabled;
                if (_txtHolyricsUrl != null) _txtHolyricsUrl.Enabled = _config.HolyricsEnabled;
                if (_txtHolyricsToken != null) _txtHolyricsToken.Enabled = _config.HolyricsEnabled;
                if (_btnTestHolyrics != null) _btnTestHolyrics.Enabled = _config.HolyricsEnabled;
            }
            if (_txtHolyricsUrl != null) _txtHolyricsUrl.Text = _config.HolyricsUrl;
            if (_txtHolyricsToken != null) _txtHolyricsToken.Text = _config.HolyricsToken;

            if (_chkAutoCheckUpdates != null)
            {
                _chkAutoCheckUpdates.Checked = _config.AutoCheckUpdates;
            }

            UpdateSelectedMonitorDetails();
            UpdateSecondMonitorDetails();
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

        private async Task TestHolyricsConnectionAsync()
        {
            if (_lblHolyricsStatusBadge == null || _btnTestHolyrics == null) return;

            _btnTestHolyrics.Enabled = false;
            _lblHolyricsStatusBadge.Text = "Status: ⏳ Verificando servidor do Holyrics...";
            _lblHolyricsStatusBadge.ForeColor = Color.FromArgb(255, 200, 80);

            string url = _txtHolyricsUrl?.Text?.Trim() ?? _config.HolyricsUrl;
            string token = _txtHolyricsToken?.Text?.Trim() ?? _config.HolyricsToken;
            var state = await _appContext.HolyricsWatcher.CheckNowAsync(url, token);

            if (state.IsConnected)
            {
                string extra = !string.IsNullOrWhiteSpace(state.CurrentSongTitle)
                    ? $" [Música: {state.CurrentSongTitle}]"
                    : "";
                _lblHolyricsStatusBadge.Text = $"Status: 🟢 Holyrics CONECTADO ({url}){extra} — Servidor ativo e respondendo!";
                _lblHolyricsStatusBadge.ForeColor = Color.FromArgb(100, 240, 140);
            }
            else
            {
                string err = !string.IsNullOrEmpty(state.ErrorMessage) ? $" ({state.ErrorMessage})" : "";
                _lblHolyricsStatusBadge.Text = $"Status: ⚪ Holyrics não respondeu ({url}){err}";
                _lblHolyricsStatusBadge.ForeColor = Color.FromArgb(255, 140, 140);
            }

            _btnTestHolyrics.Enabled = true;
        }

        private void UpdateSelectedMonitorDetails()
        {
            if (_cbMonitors.SelectedItem is MonitorItem item && _lblSelectedMonitorDetails != null)
            {
                string type = item.Screen.Primary ? "Monitor Principal do Windows" : "Monitor Secundário (Recomendado para NO AR)";
                _lblSelectedMonitorDetails.Text = $"Resolução: {item.Screen.Bounds.Width} × {item.Screen.Bounds.Height} px | {type}";
            }
        }

        private void UpdateSecondMonitorDetails()
        {
            if (_cbSecondDisplayMonitors.SelectedItem is MonitorItem item && _lblSecondDisplayMonitorDetails != null)
            {
                string type = item.Screen.Primary ? "Monitor Principal" : "Monitor Secundário (Recomendado para Palco)";
                _lblSecondDisplayMonitorDetails.Text = $"Resolução: {item.Screen.Bounds.Width} × {item.Screen.Bounds.Height} px | {type}";
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
                string desc = _config.SecondDisplayEnabled
                    ? "Exibindo sinal no Display 1 (No Ar) e Display 2 (Palco)."
                    : "Exibindo sinal contínuo no monitor selecionado.";
                _lblStatusDesc.Text = desc;
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
            // Display 1
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

            // Display 2 (Palco)
            if (_chkSecondDisplayEnabled != null)
            {
                _config.SecondDisplayEnabled = _chkSecondDisplayEnabled.Checked;
            }
            if (_cbSecondDisplayMonitors.SelectedItem is MonitorItem selectedSecondItem)
            {
                _config.SecondDisplayMonitorIndex = selectedSecondItem.Index;
                _config.SecondDisplayMonitorDeviceName = selectedSecondItem.Screen.DeviceName;
                _config.SecondDisplayMonitorWidth = selectedSecondItem.Screen.Bounds.Width;
                _config.SecondDisplayMonitorHeight = selectedSecondItem.Screen.Bounds.Height;
            }
            if (_txtSecondDisplayUrl != null)
            {
                string secUrl = _txtSecondDisplayUrl.Text.Trim();
                if (!string.IsNullOrEmpty(secUrl))
                {
                    if (!secUrl.StartsWith("http://") && !secUrl.StartsWith("https://"))
                    {
                        secUrl = "https://" + secUrl;
                    }
                    _config.SecondDisplayUrl = secUrl;
                }
            }

            // Behavior & Power
            _config.AutoStart = _chkAutoStart.Checked;
            if (_chkPreventSleep != null)
            {
                _config.PreventSleep = _chkPreventSleep.Checked;
            }
            if (_chkWindowedOnSingle != null)
            {
                _config.WindowedPreviewOnSingleMonitor = _chkWindowedOnSingle.Checked;
            }

            // vMix
            if (_chkVmixEnabled != null)
            {
                _config.VmixEnabled = _chkVmixEnabled.Checked;
            }
            if (_txtVmixUrl != null)
            {
                _config.VmixUrl = _txtVmixUrl.Text.Trim();
            }

            // Holyrics
            if (_chkHolyricsEnabled != null)
            {
                _config.HolyricsEnabled = _chkHolyricsEnabled.Checked;
            }
            if (_txtHolyricsUrl != null)
            {
                _config.HolyricsUrl = _txtHolyricsUrl.Text.Trim();
            }
            if (_txtHolyricsToken != null)
            {
                _config.HolyricsToken = _txtHolyricsToken.Text.Trim();
            }

            // Updates
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
