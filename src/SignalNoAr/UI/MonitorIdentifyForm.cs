using System;
using System.Drawing;
using System.Windows.Forms;

namespace SignalNoAr.UI
{
    public class MonitorIdentifyForm : Form
    {
        private readonly System.Windows.Forms.Timer _timer;

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= 0x80; // WS_EX_TOOLWINDOW: hide from Alt+Tab
                cp.ExStyle &= ~0x40000; // Clear WS_EX_APPWINDOW
                return cp;
            }
        }

        public MonitorIdentifyForm(int monitorNumber, Screen screen)
        {
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.Manual;
            ShowInTaskbar = false;
            TopMost = true;
            BackColor = Color.FromArgb(16, 15, 23); // Signal Dark
            Opacity = 0.94;

            int width = Math.Min(480, screen.Bounds.Width - 40);
            int height = Math.Min(320, screen.Bounds.Height - 40);
            int x = screen.Bounds.X + (screen.Bounds.Width - width) / 2;
            int y = screen.Bounds.Y + (screen.Bounds.Height - height) / 2;

            Bounds = new Rectangle(x, y, width, height);

            var panel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(20)
            };

            var lblNumber = new Label
            {
                Text = monitorNumber.ToString(),
                Font = new Font("Segoe UI", 68, FontStyle.Bold),
                ForeColor = Color.FromArgb(255, 120, 0), // Signal Orange
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Top,
                Height = 120
            };

            var lblTitle = new Label
            {
                Text = $"Monitor {monitorNumber}" + (screen.Primary ? " (Principal)" : ""),
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                ForeColor = Color.White,
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Top,
                Height = 35
            };

            var lblResolution = new Label
            {
                Text = $"{screen.Bounds.Width} × {screen.Bounds.Height}",
                Font = new Font("Segoe UI", 12, FontStyle.Regular),
                ForeColor = Color.FromArgb(200, 180, 240), // Signal Soft Violet
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Top,
                Height = 30
            };

            var lblHint = new Label
            {
                Text = "Identificação de Monitor - Signal",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Italic),
                ForeColor = Color.FromArgb(150, 140, 170),
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Bottom,
                Height = 30
            };

            panel.Controls.Add(lblHint);
            panel.Controls.Add(lblResolution);
            panel.Controls.Add(lblTitle);
            panel.Controls.Add(lblNumber);
            Controls.Add(panel);

            _timer = new System.Windows.Forms.Timer { Interval = 3000 };
            _timer.Tick += (s, e) =>
            {
                _timer.Stop();
                Close();
            };

            Click += (s, e) => Close();
            panel.Click += (s, e) => Close();
            lblNumber.Click += (s, e) => Close();
            lblTitle.Click += (s, e) => Close();
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            _timer.Start();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _timer?.Dispose();
            }
            base.Dispose(disposing);
        }

        public static void IdentifyAllScreens()
        {
            Screen[] screens = Screen.AllScreens;
            for (int i = 0; i < screens.Length; i++)
            {
                var form = new MonitorIdentifyForm(i + 1, screens[i]);
                form.Show();
            }
        }
    }
}
