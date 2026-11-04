using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace OverlayClock.Models
{
    public class ClockSettings
    {
        // Path to the clock face image (PNG/JPG)
        public string ClockFaceImagePath { get; set; } = "";

        // Index into ClockMode.AllPresets(), or -1 for custom
        public int ClockModeIndex { get; set; } = 0;

        // Custom mode (used when ClockModeIndex == -1)
        public ClockMode? CustomMode { get; set; }

        // Window scale factor (1.0 = image native size)
        public double ScaleFactor { get; set; } = 0.5;

        // Always on top
        public bool AlwaysOnTop { get; set; } = true;

        // Show seconds hand
        public bool ShowSeconds { get; set; } = true;

        // Show the date on the lower part of the clock face
        public bool ShowDate { get; set; } = false;

        // Window position
        public double WindowLeft { get; set; } = 100;
        public double WindowTop { get; set; } = 100;

        // Clock hand colours
        public string HourHandColor { get; set; } = "#1A1A1A";
        public string MinuteHandColor { get; set; } = "#1A1A1A";
        public string SecondHandColor { get; set; } = "#CC2222";
        public string MajorTickColor { get; set; } = "#1A1A1A";
        public string MinorTickColor { get; set; } = "#555555";
        public string CenterDotColor { get; set; } = "#CC2222";
        public string DateColor { get; set; } = "#1A1A1A";

        // Hand thickness multipliers (relative to default)
        public double HourHandThickness { get; set; } = 1.0;
        public double MinuteHandThickness { get; set; } = 1.0;
        public double SecondHandThickness { get; set; } = 1.0;

        // Show/hide tick marks (useful if the face image already has them)
        public bool ShowMajorTicks { get; set; } = true;
        public bool ShowMinorTicks { get; set; } = true;

        // Time zone ID
        public string TimeZoneId { get; set; } = TimeZoneInfo.Local.Id;

        // Opacity of the whole window
        public double WindowOpacity { get; set; } = 1.0;

        // ── Persistence ────────────────────────────────────────────────────────

        private static string SettingsPath =>
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "OverlayClock",
                "settings.json");

        public static ClockSettings Load()
        {
            try
            {
                if (File.Exists(SettingsPath))
                {
                    var json = File.ReadAllText(SettingsPath);
                    return JsonSerializer.Deserialize<ClockSettings>(json) ?? new ClockSettings();
                }
            }
            catch { /* fall through to defaults */ }
            return new ClockSettings();
        }

        public void Save()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);
                var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(SettingsPath, json);
            }
            catch { /* ignore save errors */ }
        }

        public ClockMode ActiveMode()
        {
            var presets = ClockMode.AllPresets();
            if (ClockModeIndex >= 0 && ClockModeIndex < presets.Length)
                return presets[ClockModeIndex];
            return CustomMode ?? ClockMode.Standard12Hour();
        }

        public TimeZoneInfo ActiveTimeZone()
        {
            try { return TimeZoneInfo.FindSystemTimeZoneById(TimeZoneId); }
            catch { return TimeZoneInfo.Local; }
        }
    }
}
