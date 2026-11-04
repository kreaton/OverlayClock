using System.Diagnostics;
using Microsoft.Win32;

namespace OverlayClock.Services
{
    /// <summary>
    /// Opens the user's default calendar app. Windows has no "default calendar"
    /// setting of its own, so the app associated with .ics files stands in for it.
    /// </summary>
    public static class CalendarLauncher
    {
        /// <summary>Returns false if no calendar app could be found or started.</summary>
        public static bool TryOpen()
        {
            var target = DefaultIcsApp() ?? (IsProtocolRegistered("outlookcal") ? "outlookcal:" : null);
            if (target == null) return false;

            try
            {
                Process.Start(new ProcessStartInfo(target) { UseShellExecute = true });
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static string? DefaultIcsApp()
        {
            var progId =
                Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\FileExts\.ics\UserChoice")
                    ?.GetValue("ProgId") as string
                ?? Registry.ClassesRoot.OpenSubKey(".ics")?.GetValue(null) as string;
            if (string.IsNullOrEmpty(progId)) return null;

            // Store apps are started by their app ID
            if (Registry.ClassesRoot.OpenSubKey($@"{progId}\Application")?.GetValue("AppUserModelID") is string appId)
                return $@"shell:AppsFolder\{appId}";

            // Desktop apps: use the program from the open command, without the file argument
            return Registry.ClassesRoot.OpenSubKey($@"{progId}\shell\open\command")?.GetValue(null) is string command
                ? ExecutableFrom(command)
                : null;
        }

        private static string? ExecutableFrom(string command)
        {
            command = command.Trim();
            if (command.StartsWith('"'))
            {
                var closingQuote = command.IndexOf('"', 1);
                return closingQuote > 1 ? command[1..closingQuote] : null;
            }

            var space = command.IndexOf(' ');
            return space > 0 ? command[..space] : command;
        }

        private static bool IsProtocolRegistered(string scheme)
        {
            using var key = Registry.ClassesRoot.OpenSubKey(scheme);
            return key != null;
        }
    }
}
