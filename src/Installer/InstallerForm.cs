using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Win32;

namespace Installer
{
    public class InstallerForm : Form
    {
        private ProgressBar _progressBar = null!;
        private Label _lblStatus = null!;
        private Button _btnInstall = null!;
        private Button _btnCancel = null!;
        private CheckBox _chkLaunchSetup = null!;
        private readonly string _installDir;
        private readonly bool _isUpdate;
        private bool _isFinished = false;

        public InstallerForm()
        {
            _installDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "SignalNoAr"
            );

            _isUpdate = File.Exists(Path.Combine(_installDir, "SignalNoAr.exe"));

            InitializeForm();
            BuildUI();
        }

        private void InitializeForm()
        {
            Text = _isUpdate ? "Atualizador do Signal No Ar" : "Instalador do Signal No Ar";
            Size = new Size(680, 670);
            MinimumSize = new Size(650, 640);
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.Sizable;
            MaximizeBox = true;
            MinimizeBox = true;
            BackColor = Color.FromArgb(14, 13, 20); // Signal Dark Slate
            ForeColor = Color.White;
            ShowInTaskbar = true;
            AutoScaleMode = AutoScaleMode.Dpi;

            try
            {
                using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Installer.app.ico");
                if (stream != null)
                {
                    Icon = new Icon(stream);
                }
            }
            catch { }
        }

        private void BuildUI()
        {
            // Top Header Panel
            var headerPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 135,
                BackColor = Color.FromArgb(22, 20, 32),
                Padding = new Padding(24, 14, 24, 10)
            };

            var picHeaderLogo = new PictureBox
            {
                Dock = DockStyle.Top,
                Height = 56,
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.Transparent
            };

            try
            {
                using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Installer.logo.png");
                if (stream != null)
                {
                    picHeaderLogo.Image = Image.FromStream(stream);
                }
            }
            catch { }

            var lblHeaderTitle = new Label
            {
                Text = _isUpdate ? "ATUALIZAÇÃO DO SIGNAL NO AR" : "INSTALADOR DO DISPLAY EM TELA CHEIA",
                Font = new Font("Segoe UI", 11.5f, FontStyle.Bold),
                ForeColor = _isUpdate ? Color.FromArgb(70, 220, 130) : Color.FromArgb(255, 120, 0), // Green for update, Orange for install
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Top,
                Height = 28
            };

            var lblHeaderSub = new Label
            {
                Text = _isUpdate ? "Nova versão com leitor do vMix! Seus monitores e configurações serão preservados." : "Exibição contínua do sinal 'NO AR' com inicialização automática do Windows",
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
                Padding = new Padding(28, 14, 28, 14)
            };

            _btnInstall = new Button
            {
                Text = _isUpdate ? "Atualizar Agora" : "Instalar Agora",
                Dock = DockStyle.Right,
                Width = 210,
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                BackColor = _isUpdate ? Color.FromArgb(40, 160, 80) : Color.FromArgb(255, 110, 0),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            _btnInstall.FlatAppearance.BorderSize = 0;
            _btnInstall.Click += BtnInstall_Click;
            bottomPanel.Controls.Add(_btnInstall);

            _btnCancel = new Button
            {
                Text = "Cancelar",
                Dock = DockStyle.Left,
                Width = 120,
                Font = new Font("Segoe UI", 10f, FontStyle.Regular),
                BackColor = Color.FromArgb(36, 33, 48),
                ForeColor = Color.FromArgb(215, 210, 230),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            _btnCancel.FlatAppearance.BorderColor = Color.FromArgb(60, 55, 80);
            _btnCancel.Click += (s, e) => Close();
            bottomPanel.Controls.Add(_btnCancel);

            Controls.Add(bottomPanel);

            // Main Content Container (auto-scroll & centered on resize)
            var scrollPanel = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                Padding = new Padding(24, 16, 24, 16),
                BackColor = Color.FromArgb(14, 13, 20)
            };
            Controls.Add(scrollPanel);

            var centerBox = new Panel
            {
                Location = new Point(24, 10),
                Width = Math.Max(580, scrollPanel.ClientSize.Width - 48),
                BackColor = Color.Transparent
            };
            scrollPanel.Controls.Add(centerBox);

            scrollPanel.Resize += (s, e) =>
            {
                int availableWidth = scrollPanel.ClientSize.Width - 48;
                int targetWidth = Math.Min(820, Math.Max(580, availableWidth));
                int targetX = Math.Max(12, (scrollPanel.ClientSize.Width - targetWidth) / 2);
                centerBox.Width = targetWidth;
                centerBox.Left = targetX;
            };

            int currentY = 0;

            // Features Card
            var pnlCard = new Panel
            {
                Location = new Point(0, currentY),
                Width = centerBox.Width,
                Height = 220,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                BackColor = Color.FromArgb(24, 22, 34),
                Padding = new Padding(18, 14, 18, 14)
            };

            var lblCardTitle = new Label
            {
                Text = _isUpdate ? "O que será atualizado no seu sistema:" : "Recursos que serão configurados no seu sistema:",
                Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(245, 240, 255),
                Dock = DockStyle.Top,
                Height = 24
            };
            pnlCard.Controls.Add(lblCardTitle);

            string[] features = _isUpdate
                ? new[]
                {
                    "🚀 Atualização do Sistema: Atualiza o aplicativo com integração vMix e otimizações",
                    "🎬 Leitor Automático do vMix: Detecta transmissões ao vivo na porta 8088",
                    "💾 Configurações Preservadas: Seu monitor escolhido continua salvo sem reconfigurar",
                    "⚡ Reinicialização Suave: Fecha a versão antiga e inicia a nova automaticamente",
                    "⚙️ Controle na Bandeja: Novo painel com teste de conexão com o vMix"
                }
                : new[]
                {
                    "⚡ Inicialização Automática: Inicia sozinho ao ligar ou reiniciar o computador",
                    "📺 Tela Cheia no Monitor Escolhido: Memoriza seu monitor e abre sem bordas",
                    "🎬 Leitor Automático do vMix: Detecta transmissões ao vivo na porta 8088",
                    "🛡️ Blindagem Contínua: Protegido contra Alt+F4 e com reconexão automática",
                    "🚫 Oculto da Barra de Tarefas: Não ocupa espaço e não atrapalha seu trabalho"
                };

            var pnlFeatureList = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent,
                Padding = new Padding(0, 6, 0, 0)
            };

            int itemY = 6;
            foreach (var feat in features)
            {
                var lblFeat = new Label
                {
                    Text = feat,
                    Font = new Font("Segoe UI", 9.3f, FontStyle.Regular),
                    ForeColor = Color.FromArgb(220, 215, 235),
                    Location = new Point(4, itemY),
                    Size = new Size(pnlFeatureList.Width - 8, 28),
                    Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                    TextAlign = ContentAlignment.MiddleLeft
                };
                pnlFeatureList.Controls.Add(lblFeat);
                itemY += 32;
            }

            pnlCard.Controls.Add(pnlFeatureList);
            centerBox.Controls.Add(pnlCard);
            currentY += 235;

            // Directory Info
            var lblDirInfo = new Label
            {
                Text = $"Pasta de instalação: {_installDir}",
                Font = new Font("Segoe UI", 9f, FontStyle.Italic),
                ForeColor = Color.FromArgb(160, 155, 180),
                Location = new Point(4, currentY),
                Size = new Size(centerBox.Width - 8, 22),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            centerBox.Controls.Add(lblDirInfo);
            currentY += 28;

            // Launch Setup Checkbox
            _chkLaunchSetup = new CheckBox
            {
                Text = _isUpdate
                    ? "Reiniciar o Signal No Ar automaticamente após a atualização"
                    : "Abrir a seleção de monitor logo após concluir a instalação",
                Font = new Font("Segoe UI", 10f, FontStyle.Regular),
                ForeColor = Color.FromArgb(240, 235, 255),
                Location = new Point(4, currentY),
                Size = new Size(centerBox.Width - 8, 28),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                Checked = true
            };
            centerBox.Controls.Add(_chkLaunchSetup);
            currentY += 38;

            // Progress Bar
            _progressBar = new ProgressBar
            {
                Location = new Point(4, currentY),
                Size = new Size(centerBox.Width - 8, 14),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                Style = ProgressBarStyle.Blocks,
                Value = 0,
                Visible = false
            };
            centerBox.Controls.Add(_progressBar);
            currentY += 20;

            // Status Label
            _lblStatus = new Label
            {
                Text = _isUpdate
                    ? "Versão anterior detectada. Clique em 'Atualizar Agora' para prosseguir."
                    : "Pronto para instalar. Clique em 'Instalar Agora' para prosseguir.",
                Font = new Font("Segoe UI", 9.8f, FontStyle.Regular),
                ForeColor = Color.FromArgb(190, 185, 210),
                Location = new Point(4, currentY),
                Size = new Size(centerBox.Width - 8, 26),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            centerBox.Controls.Add(_lblStatus);
            currentY += 35;

            centerBox.Height = currentY + 20;
        }

        private async void BtnInstall_Click(object? sender, EventArgs e)
        {
            _btnInstall.Enabled = false;
            _btnCancel.Enabled = false;
            _progressBar.Visible = true;
            _progressBar.Style = ProgressBarStyle.Marquee;

            try
            {
                await Task.Run(() => PerformInstallation());

                _progressBar.Style = ProgressBarStyle.Blocks;
                _progressBar.Value = 100;
                _lblStatus.Text = _isUpdate ? "✅ Atualização concluída com sucesso!" : "✅ Instalação concluída com sucesso!";
                _lblStatus.ForeColor = Color.FromArgb(70, 220, 130);

                _btnInstall.Text = _isUpdate ? "Concluir e Iniciar" : "Concluir e Abrir Tela";
                _btnInstall.BackColor = Color.FromArgb(40, 160, 80);
                _btnInstall.Enabled = true;
                _btnInstall.Click -= BtnInstall_Click;
                _btnInstall.Click += (s, ev) => FinishAndLaunchSetup();

                await Task.Delay(1400);
                if (Visible && !IsDisposed)
                {
                    FinishAndLaunchSetup();
                }
            }
            catch (Exception ex)
            {
                _progressBar.Visible = false;
                _lblStatus.Text = $"Erro na instalação: {ex.Message}";
                _lblStatus.ForeColor = Color.FromArgb(255, 100, 100);
                _btnInstall.Enabled = true;
                _btnCancel.Enabled = true;

                MessageBox.Show(
                    $"Ocorreu um erro durante a instalação:\n{ex.Message}",
                    "Erro de Instalação",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
        }

        private void FinishAndLaunchSetup()
        {
            if (_isFinished) return;
            _isFinished = true;

            string targetExe = Path.Combine(_installDir, "SignalNoAr.exe");
            if (_chkLaunchSetup.Checked && File.Exists(targetExe))
            {
                string configPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "SignalNoAr",
                    "config.json"
                );

                bool hasExistingConfig = File.Exists(configPath);
                string args = (_isUpdate && hasExistingConfig) ? "" : "--setup";

                Process.Start(new ProcessStartInfo
                {
                    FileName = targetExe,
                    Arguments = args,
                    UseShellExecute = true
                });
            }

            Close();
        }

        private void PerformInstallation()
        {
            UpdateStatus(_isUpdate ? "Fechando versão anterior em execução..." : "Fechando instâncias anteriores...");
            KillExistingProcesses();

            UpdateStatus("Preparando diretório de instalação...");
            if (!Directory.Exists(_installDir))
            {
                Directory.CreateDirectory(_installDir);
            }

            UpdateStatus(_isUpdate ? "Atualizando arquivos do aplicativo..." : "Extraindo arquivos do aplicativo...");
            ExtractPayload();

            UpdateStatus("Verificando componente Microsoft WebView2...");
            EnsureWebView2Installed();

            UpdateStatus("Configurando inicialização automática...");
            ConfigureStartupRegistry();

            UpdateStatus("Criando atalhos na Área de Trabalho e Menu Iniciar...");
            CreateShortcuts();

            UpdateStatus("Criando script de desinstalação...");
            CreateUninstallerScript();

            UpdateStatus("Finalizando...");
        }

        private void UpdateStatus(string message)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => _lblStatus.Text = message));
            }
            else
            {
                _lblStatus.Text = message;
            }
        }

        private void KillExistingProcesses()
        {
            try
            {
                var processes = Process.GetProcessesByName("SignalNoAr");
                foreach (var proc in processes)
                {
                    try
                    {
                        proc.Kill();
                        proc.WaitForExit(4000);
                    }
                    catch { }
                }
            }
            catch { }

            // Allow operating system to fully release file locks
            System.Threading.Thread.Sleep(600);
        }

        private void ExtractPayload()
        {
            using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Installer.payload.zip");
            if (stream == null)
            {
                throw new InvalidOperationException("Embedded payload.zip resource not found.");
            }

            using var archive = new ZipArchive(stream, ZipArchiveMode.Read);
            foreach (var entry in archive.Entries)
            {
                if (string.IsNullOrEmpty(entry.Name)) continue;

                string destPath = Path.Combine(_installDir, entry.FullName);
                string? destDir = Path.GetDirectoryName(destPath);
                if (destDir != null && !Directory.Exists(destDir))
                {
                    Directory.CreateDirectory(destDir);
                }

                // Retry loop in case a file is momentarily locked by antivirus or ending process
                int retries = 4;
                while (retries > 0)
                {
                    try
                    {
                        entry.ExtractToFile(destPath, overwrite: true);
                        break;
                    }
                    catch (IOException) when (retries > 1)
                    {
                        retries--;
                        System.Threading.Thread.Sleep(500);
                    }
                }
            }
        }

        private void EnsureWebView2Installed()
        {
            if (!IsWebView2Installed())
            {
                UpdateStatus("Instalando componente Microsoft WebView2 Runtime...");
                try
                {
                    string tempInstaller = Path.Combine(Path.GetTempPath(), "MicrosoftEdgeWebview2Setup.exe");
                    using (var client = new System.Net.Http.HttpClient())
                    {
                        client.Timeout = TimeSpan.FromSeconds(30);
                        var task = client.GetByteArrayAsync("https://go.microsoft.com/fwlink/p/?LinkId=2124703");
                        task.Wait();
                        File.WriteAllBytes(tempInstaller, task.Result);
                    }

                    var psi = new ProcessStartInfo
                    {
                        FileName = tempInstaller,
                        Arguments = "/silent /install",
                        UseShellExecute = true
                    };
                    var proc = Process.Start(psi);
                    proc?.WaitForExit(60000);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Failed to auto-install WebView2: {ex.Message}");
                }
            }
        }

        private static bool IsWebView2Installed()
        {
            try
            {
                string[] keys = new[]
                {
                    @"SOFTWARE\WOW6432Node\Microsoft\EdgeUpdate\Clients\{F3017226-4D2A-4265-B2E0-9C0208376413}",
                    @"SOFTWARE\Microsoft\EdgeUpdate\Clients\{F3017226-4D2A-4265-B2E0-9C0208376413}"
                };

                foreach (var subKey in keys)
                {
                    using var key = Registry.LocalMachine.OpenSubKey(subKey);
                    if (key != null)
                    {
                        var pv = key.GetValue("pv") as string;
                        if (!string.IsNullOrEmpty(pv) && pv != "0.0.0.0") return true;
                    }
                }

                using var cuKey = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\EdgeUpdate\Clients\{F3017226-4D2A-4265-B2E0-9C0208376413}");
                if (cuKey != null)
                {
                    var pv = cuKey.GetValue("pv") as string;
                    if (!string.IsNullOrEmpty(pv) && pv != "0.0.0.0") return true;
                }
            }
            catch { }
            return false;
        }

        private void ConfigureStartupRegistry()
        {
            string exePath = Path.Combine(_installDir, "SignalNoAr.exe");
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true);
            if (key != null)
            {
                key.SetValue("SignalNoAr", $"\"{exePath}\"");
            }
        }

        private void CreateShortcuts()
        {
            string exePath = Path.Combine(_installDir, "SignalNoAr.exe");
            string iconPath = Path.Combine(_installDir, "app.ico");

            string desktopDir = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            string desktopShortcut = Path.Combine(desktopDir, "Signal No Ar.lnk");

            string startMenuDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                @"Microsoft\Windows\Start Menu\Programs"
            );
            string startMenuShortcut = Path.Combine(startMenuDir, "Signal No Ar.lnk");

            CreateShortcut(desktopShortcut, exePath, iconPath, "Signal No Ar - Painel de Controle e Display");
            CreateShortcut(startMenuShortcut, exePath, iconPath, "Signal No Ar - Painel de Controle e Display");
        }

        private void CreateShortcut(string shortcutPath, string targetPath, string iconPath, string description)
        {
            try
            {
                Type? shellType = Type.GetTypeFromProgID("WScript.Shell");
                if (shellType != null)
                {
                    dynamic? shell = Activator.CreateInstance(shellType);
                    if (shell != null)
                    {
                        dynamic shortcut = shell.CreateShortcut(shortcutPath);
                        shortcut.TargetPath = targetPath;
                        shortcut.WorkingDirectory = Path.GetDirectoryName(targetPath);
                        shortcut.Description = description;
                        if (File.Exists(iconPath))
                        {
                            shortcut.IconLocation = iconPath + ",0";
                        }
                        shortcut.Save();
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to create shortcut {shortcutPath}: {ex.Message}");
            }
        }

        private void CreateUninstallerScript()
        {
            string uninstallBat = Path.Combine(_installDir, "Desinstalar.bat");
            string content = @"@echo off
chcp 65001 >nul
echo ===========================================
echo       Desinstalador do Signal No Ar
echo ===========================================
echo.
echo Tem certeza de que deseja desinstalar o Signal No Ar?
echo.
pause

echo Fechando aplicativo...
taskkill /f /im SignalNoAr.exe >nul 2>&1

echo Removendo inicialização automática...
reg delete ""HKCU\Software\Microsoft\Windows\CurrentVersion\Run"" /v ""SignalNoAr"" /f >nul 2>&1

echo Removendo atalhos...
del ""%USERPROFILE%\Desktop\Signal No Ar.lnk"" >nul 2>&1
del ""%APPDATA%\Microsoft\Windows\Start Menu\Programs\Signal No Ar.lnk"" >nul 2>&1

echo Removendo configurações...
rd /s /q ""%APPDATA%\SignalNoAr"" >nul 2>&1

echo Removendo arquivos do programa...
cd /d ""%LOCALAPPDATA%""
rd /s /q ""%LOCALAPPDATA%\SignalNoAr"" >nul 2>&1

echo.
echo Desinstalação concluída com sucesso!
pause
";
            File.WriteAllText(uninstallBat, content);
        }
    }
}
