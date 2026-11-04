using System;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows;
using Microsoft.Win32;
using OverlayClock.Models;

namespace OverlayClock.Views
{
    public partial class SettingsWindow : Window
    {
        private ClockSettings _settings;
        private bool _loading = true;

        public SettingsWindow(ClockSettings settings)
        {
            InitializeComponent();
            _settings = CloneSettings(settings);
            PopulateControls();
            _loading = false;
        }

        // ── Populate all controls from _settings ───────────────────────────────
        private void PopulateControls()
        {
            // Image path
            TxtImagePath.Text = string.IsNullOrWhiteSpace(_settings.ClockFaceImagePath)
                ? "(using built-in default face)"
                : _settings.ClockFaceImagePath;

            // Clock modes
            var presets = ClockMode.AllPresets();
            CmbMode.ItemsSource   = presets.Select(m => m.Name).ToArray();
            CmbMode.SelectedIndex = Math.Clamp(_settings.ClockModeIndex, 0, presets.Length - 1);
            CmbMode.SelectionChanged += CmbMode_SelectionChanged;
            UpdateModeDescription();

            // Custom mode values
            var custom = _settings.CustomMode ?? ClockMode.Standard12Hour();
            TxtCustomHours.Text      = custom.HoursPerDay.ToString();
            TxtCustomMinutes.Text    = custom.MinutesPerHour.ToString();
            TxtCustomSeconds.Text    = custom.SecondsPerMinute.ToString();
            TxtCustomMajorTicks.Text = custom.MajorTicks.ToString();
            TxtCustomMinorTicks.Text = custom.MinorTicksPerSegment.ToString();
            TxtCustomRotations.Text  = custom.RotationsPerDay.ToString();

            // Time zones
            var zones = TimeZoneInfo.GetSystemTimeZones();
            CmbTimeZone.ItemsSource   = zones.Select(z => z.DisplayName).ToArray();
            var currentZone = _settings.ActiveTimeZone();
            CmbTimeZone.SelectedIndex = zones
                .Select((z, i) => (z, i))
                .FirstOrDefault(t => t.z.Id == currentZone.Id).i;

            // Scale
            SldScale.Value   = _settings.ScaleFactor;
            TxtScaleVal.Text = $"{_settings.ScaleFactor:P0}";

            // Opacity
            SldOpacity.Value   = _settings.WindowOpacity;
            TxtOpacityVal.Text = $"{_settings.WindowOpacity:P0}";

            // Checkboxes
            ChkAlwaysOnTop.IsChecked = _settings.AlwaysOnTop;
            ChkShowSeconds.IsChecked = _settings.ShowSeconds;
            ChkShowDate.IsChecked    = _settings.ShowDate;
            ChkShowMajor.IsChecked   = _settings.ShowMajorTicks;
            ChkShowMinor.IsChecked   = _settings.ShowMinorTicks;

            // Colours
            TxtHourColor.Text       = _settings.HourHandColor;
            TxtMinuteColor.Text     = _settings.MinuteHandColor;
            TxtSecondColor.Text     = _settings.SecondHandColor;
            TxtMajorTickColor.Text  = _settings.MajorTickColor;
            TxtMinorTickColor.Text  = _settings.MinorTickColor;
            TxtDateColor.Text       = _settings.DateColor;

            // About
            TxtVersion.Text = $"OverlayClock {Assembly.GetExecutingAssembly().GetName().Version?.ToString(3)}";
            TxtRuntime.Text = RuntimeInformation.FrameworkDescription;

            // Thickness sliders
            SldHourThick.Value   = _settings.HourHandThickness;
            SldMinuteThick.Value = _settings.MinuteHandThickness;
            SldSecondThick.Value = _settings.SecondHandThickness;
            TxtHourThick.Text    = $"{_settings.HourHandThickness:F1}×";
            TxtMinuteThick.Text  = $"{_settings.MinuteHandThickness:F1}×";
            TxtSecondThick.Text  = $"{_settings.SecondHandThickness:F1}×";
        }

        private void UpdateModeDescription()
        {
            var presets = ClockMode.AllPresets();
            if (CmbMode.SelectedIndex >= 0 && CmbMode.SelectedIndex < presets.Length)
                TxtModeDesc.Text = presets[CmbMode.SelectedIndex].Description;
        }

        // ── Event handlers ─────────────────────────────────────────────────────
        private void CmbMode_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            UpdateModeDescription();
        }

        private void ExpCustom_Expanded(object sender, RoutedEventArgs e)
        {
            // Auto-select "Custom" slot — if user fills this in we use index -1
        }

        private void SldScale_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_loading || TxtScaleVal == null) return;
            TxtScaleVal.Text = $"{SldScale.Value:P0}";
        }

        private void SldOpacity_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_loading || TxtOpacityVal == null) return;
            TxtOpacityVal.Text = $"{SldOpacity.Value:P0}";
        }

        private void SldThick_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_loading) return;
            if (TxtHourThick   != null) TxtHourThick.Text   = $"{SldHourThick.Value:F1}×";
            if (TxtMinuteThick != null) TxtMinuteThick.Text = $"{SldMinuteThick.Value:F1}×";
            if (TxtSecondThick != null) TxtSecondThick.Text = $"{SldSecondThick.Value:F1}×";
        }

        private void BtnBrowse_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                Title  = "Select Clock Face Image",
                Filter = "Image files|*.png;*.jpg;*.jpeg;*.bmp;*.gif|All files|*.*"
            };
            if (dlg.ShowDialog() == true)
            {
                _settings.ClockFaceImagePath = dlg.FileName;
                TxtImagePath.Text = dlg.FileName;
            }
        }

        private void BtnClearImage_Click(object sender, RoutedEventArgs e)
        {
            _settings.ClockFaceImagePath = "";
            TxtImagePath.Text = "(using built-in default face)";
        }

        private void BtnOK_Click(object sender, RoutedEventArgs e)
        {
            // Gather all control values back into _settings
            _settings.ScaleFactor    = SldScale.Value;
            _settings.WindowOpacity  = SldOpacity.Value;
            _settings.AlwaysOnTop    = ChkAlwaysOnTop.IsChecked == true;
            _settings.ShowSeconds    = ChkShowSeconds.IsChecked == true;
            _settings.ShowDate       = ChkShowDate.IsChecked == true;
            _settings.ShowMajorTicks = ChkShowMajor.IsChecked == true;
            _settings.ShowMinorTicks = ChkShowMinor.IsChecked == true;

            _settings.HourHandColor    = TxtHourColor.Text.Trim();
            _settings.MinuteHandColor  = TxtMinuteColor.Text.Trim();
            _settings.SecondHandColor  = TxtSecondColor.Text.Trim();
            _settings.MajorTickColor   = TxtMajorTickColor.Text.Trim();
            _settings.MinorTickColor   = TxtMinorTickColor.Text.Trim();
            _settings.DateColor        = TxtDateColor.Text.Trim();

            _settings.HourHandThickness   = SldHourThick.Value;
            _settings.MinuteHandThickness = SldMinuteThick.Value;
            _settings.SecondHandThickness = SldSecondThick.Value;

            // Clock mode
            _settings.ClockModeIndex = CmbMode.SelectedIndex;

            // Custom mode (only use if expander is expanded and fields have values)
            if (ExpCustom.IsExpanded)
            {
                _settings.ClockModeIndex = -1;
                _settings.CustomMode = new ClockMode
                {
                    Name               = "Custom",
                    Description        = "User-defined",
                    HoursPerDay        = ParseInt(TxtCustomHours.Text, 12),
                    MinutesPerHour     = ParseInt(TxtCustomMinutes.Text, 60),
                    SecondsPerMinute   = ParseInt(TxtCustomSeconds.Text, 60),
                    MajorTicks         = ParseInt(TxtCustomMajorTicks.Text, 12),
                    MinorTicksPerSegment = ParseInt(TxtCustomMinorTicks.Text, 4),
                    RotationsPerDay    = ParseInt(TxtCustomRotations.Text, 2)
                };
            }

            // Time zone
            var zones = TimeZoneInfo.GetSystemTimeZones().ToArray();
            if (CmbTimeZone.SelectedIndex >= 0 && CmbTimeZone.SelectedIndex < zones.Length)
                _settings.TimeZoneId = zones[CmbTimeZone.SelectedIndex].Id;

            _settings.Save();
            DialogResult = true;
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        // ── Helpers ────────────────────────────────────────────────────────────
        private static int ParseInt(string s, int fallback)
        {
            return int.TryParse(s, out var v) && v > 0 ? v : fallback;
        }

        private static ClockSettings CloneSettings(ClockSettings s)
        {
            // Simple JSON round-trip clone
            var json = System.Text.Json.JsonSerializer.Serialize(s);
            return System.Text.Json.JsonSerializer.Deserialize<ClockSettings>(json) ?? new ClockSettings();
        }
    }
}
