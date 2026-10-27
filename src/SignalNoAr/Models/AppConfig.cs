using System;
using System.IO;
using System.Text.Json;
using System.Windows.Forms;
using Microsoft.Win32;

namespace SignalNoAr.Models
{
    public class AppConfig
    {
        // Primary Display (e.g. No Ar / Kiosk)
        public string Url { get; set; } = "https://signal.mathguthmann.com/no-ar";
        public string SelectedMonitorDeviceName { get; set; } = "";
        public int SelectedMonitorIndex { get; set; } = -1;
        public int SelectedMonitorWidth { get; set; } = 0;
        public int SelectedMonitorHeight { get; set; } = 0;
        public bool AutoStart { get; set; } = true;
        public bool IsConfigured { get; set; } = false;
        public bool WindowedPreviewOnSingleMonitor { get; set; } = false;

        // Secondary Display (e.g. Stage Monitor / Retorno de Palco)
        public bool SecondDisplayEnabled { get; set; } = false;
        public string SecondDisplayUrl { get; set; } = "https://signal.mathguthmann.com/palco";
        public string SecondDisplayMonitorDeviceName { get; set; } = "";
        public int SecondDisplayMonitorIndex { get; set; } = -1;
        public int SecondDisplayMonitorWidth { get; set; } = 0;
        public int SecondDisplayMonitorHeight { get; set; } = 0;

        // vMix Integration
        public bool VmixEnabled { get; set; } = true;
        public string VmixUrl { get; set; } = "http://localhost:8088/api";
        public int VmixPollIntervalSeconds { get; set; } = 1;

        // Holyrics Integration
        public bool HolyricsEnabled { get; set; } = true;
        public string HolyricsUrl { get; set; } = "http://localhost:8091";
        public string HolyricsToken { get; set; } = "";
        public int HolyricsPollIntervalSeconds { get; set; } = 1;

        // Power Management / Sleep Blocker
        public bool PreventSleep { get; set; } = true;

        // Auto Update
        public bool AutoCheckUpdates { get; set; } = true;

        private const string RunRegistryKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string AppRegistryName = "SignalNoAr";

        public static string GetConfigDirectory()
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string dir = Path.Combine(appData, "SignalNoAr");
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            return dir;
        }

        public static string GetConfigFilePath()
        {
            return Path.Combine(GetConfigDirectory(), "config.json");
        }

        public static AppConfig Load()
        {
            string path = GetConfigFilePath();
            if (File.Exists(path))
            {
                try
                {
                    string json = File.ReadAllText(path);
                    var config = JsonSerializer.Deserialize<AppConfig>(json);
                    if (config != null)
                    {
                        return config;
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error reading config: {ex.Message}");
                }
            }

            return new AppConfig();
        }

        public void Save()
        {
            try
            {
                string path = GetConfigFilePath();
                var options = new JsonSerializerOptions { WriteIndented = true };
                string json = JsonSerializer.Serialize(this, options);
                File.WriteAllText(path, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving config: {ex.Message}");
            }
        }

        public Screen ResolveScreen()
        {
            Screen[] screens = Screen.AllScreens;
            if (screens.Length == 0)
            {
                return Screen.PrimaryScreen ?? throw new InvalidOperationException("Nenhum monitor encontrado");
            }

            // 1. Try match by device name
            if (!string.IsNullOrEmpty(SelectedMonitorDeviceName))
            {
                foreach (var screen in screens)
                {
                    if (string.Equals(screen.DeviceName, SelectedMonitorDeviceName, StringComparison.OrdinalIgnoreCase))
                    {
                        return screen;
                    }
                }
            }

            // 2. Try match by index
            if (SelectedMonitorIndex >= 0 && SelectedMonitorIndex < screens.Length)
            {
                return screens[SelectedMonitorIndex];
            }

            // 3. Smart Default: If multiple screens, default to secondary screen
            if (screens.Length > 1)
            {
                for (int i = 0; i < screens.Length; i++)
                {
                    if (!screens[i].Primary)
                    {
                        SelectedMonitorIndex = i;
                        SelectedMonitorDeviceName = screens[i].DeviceName;
                        return screens[i];
                    }
                }
            }

            // Fallback to Primary screen
            SelectedMonitorIndex = 0;
            SelectedMonitorDeviceName = screens[0].DeviceName;
            return screens[0];
        }

        public Screen? ResolveSecondScreen()
        {
            if (!SecondDisplayEnabled) return null;

            Screen[] screens = Screen.AllScreens;
            if (screens.Length <= 1) return null;

            // 1. Try match by device name
            if (!string.IsNullOrEmpty(SecondDisplayMonitorDeviceName))
            {
                foreach (var screen in screens)
                {
                    if (string.Equals(screen.DeviceName, SecondDisplayMonitorDeviceName, StringComparison.OrdinalIgnoreCase))
                    {
                        return screen;
                    }
                }
            }

            // 2. Try match by index
            if (SecondDisplayMonitorIndex >= 0 && SecondDisplayMonitorIndex < screens.Length)
            {
                return screens[SecondDisplayMonitorIndex];
            }

            // 3. Smart Default: pick any screen that is different from primary display
            var primaryScreen = ResolveScreen();
            foreach (var screen in screens)
            {
                if (!string.Equals(screen.DeviceName, primaryScreen.DeviceName, StringComparison.OrdinalIgnoreCase))
                {
                    return screen;
                }
            }

            return null;
        }

        public static void SetStartupRegistration(bool enable, string? exePath = null)
        {
            try
            {
                using RegistryKey? key = Registry.CurrentUser.OpenSubKey(RunRegistryKey, true);
                if (key == null) return;

                if (enable)
                {
                    exePath ??= Application.ExecutablePath;
                    key.SetValue(AppRegistryName, $"\"{exePath}\"");
                }
                else
                {
                    if (key.GetValue(AppRegistryName) != null)
                    {
                        key.DeleteValue(AppRegistryName, false);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error configuring autostart: {ex.Message}");
            }
        }

        public static bool IsStartupRegistered()
        {
            try
            {
                using RegistryKey? key = Registry.CurrentUser.OpenSubKey(RunRegistryKey, false);
                return key?.GetValue(AppRegistryName) != null;
            }
            catch
            {
                return false;
            }
        }
    }
}
