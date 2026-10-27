using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace SignalNoAr.Services
{
    /// <summary>
    /// Prevents Windows from turning off the display, starting screensavers, or entering sleep mode
    /// during live studio / church production.
    /// </summary>
    public static class SleepBlocker
    {
        [Flags]
        public enum ExecutionState : uint
        {
            ES_AWAYMODE_REQUIRED = 0x00000040,
            ES_CONTINUOUS = 0x80000000,
            ES_DISPLAY_REQUIRED = 0x00000002,
            ES_SYSTEM_REQUIRED = 0x00000001
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern ExecutionState SetThreadExecutionState(ExecutionState esFlags);

        private static bool _isBlocked = false;

        public static void Enable()
        {
            try
            {
                var prev = SetThreadExecutionState(
                    ExecutionState.ES_CONTINUOUS |
                    ExecutionState.ES_SYSTEM_REQUIRED |
                    ExecutionState.ES_DISPLAY_REQUIRED |
                    ExecutionState.ES_AWAYMODE_REQUIRED
                );
                _isBlocked = true;
                Debug.WriteLine("[SleepBlocker] Display and System sleep prevention ENABLED.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SleepBlocker] Failed to set thread execution state: {ex.Message}");
            }
        }

        public static void Disable()
        {
            try
            {
                if (_isBlocked)
                {
                    SetThreadExecutionState(ExecutionState.ES_CONTINUOUS);
                    _isBlocked = false;
                    Debug.WriteLine("[SleepBlocker] Display and System sleep prevention DISABLED.");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SleepBlocker] Failed to restore thread execution state: {ex.Message}");
            }
        }
    }
}

