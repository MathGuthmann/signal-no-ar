using System;
using System.IO;
using System.IO.Pipes;
using System.Threading;
using System.Windows.Forms;
using SignalNoAr.Models;

namespace SignalNoAr
{
    public static class Program
    {
        private const string MutexName = @"Global\SignalNoAr_Application_Mutex";
        public static bool IsExplicitExit { get; set; } = false;
        public static string IconPath => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app.ico");

        [STAThread]
        public static void Main(string[] args)
        {
            bool forceSetup = false;
            foreach (var arg in args)
            {
                if (arg.Equals("--setup", StringComparison.OrdinalIgnoreCase))
                {
                    forceSetup = true;
                    break;
                }
            }

            bool isInitialInstance;
            Mutex? mutex = null;

            try
            {
                mutex = new Mutex(true, MutexName, out isInitialInstance);
            }
            catch (AbandonedMutexException)
            {
                isInitialInstance = true;
            }

            if (!isInitialInstance)
            {
                SignalExistingInstance(forceSetup ? "SETUP" : "SHOW_SETTINGS");
                return;
            }

            try
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);

                var config = AppConfig.Load();
                if (forceSetup)
                {
                    config.IsConfigured = false;
                }

                var appContext = new AppContext(config);
                Application.Run(appContext);
            }
            finally
            {
                mutex?.Dispose();
            }
        }

        private static void SignalExistingInstance(string command)
        {
            try
            {
                using var client = new NamedPipeClientStream(".", "SignalNoAr_Ipc_Pipe", PipeDirection.Out);
                client.Connect(1500);
                using var writer = new StreamWriter(client) { AutoFlush = true };
                writer.WriteLine(command);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to notify running instance: {ex.Message}");
            }
        }
    }
}
