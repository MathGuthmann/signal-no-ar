using System;
using System.Windows.Forms;

namespace Installer
{
    public static class Program
    {
        [STAThread]
        public static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);

            Application.Run(new InstallerForm());
        }
    }
}
