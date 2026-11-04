using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using OverlayClock.Models;

namespace OverlayClock.Views
{
    public partial class MainWindow : Window
    {
        private ClockSettings _settings = ClockSettings.Load();
        private readonly DispatcherTimer _timer = new();

        // Rendered clock face size (square, in WPF device-independent pixels)
        private double _faceSize = 300;

        // Centre of the clock face
        private double Centre => _faceSize / 2.0;

        // ── Bindable property for context menu ────────────────────────────────
        public bool AlwaysOnTop
        {
            get => _settings.AlwaysOnTop;
            set
            {
                _settings.AlwaysOnTop = value;
                Topmost = value;
                _settings.Save();
            }
        }

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;

            Left = _settings.WindowLeft;
            Top  = _settings.WindowTop;
            Opacity = _settings.WindowOpacity;
            Topmost = _settings.AlwaysOnTop;

            ApplySettings();

            _timer.Interval = TimeSpan.FromMilliseconds(200);
            _timer.Tick += (_, _) => DrawHands();
            _timer.Start();
        }

        // ── Public: called by SettingsWindow after changes ─────────────────────
        public void ApplySettings()
        {
            _settings = ClockSettings.Load();
            Topmost   = _settings.AlwaysOnTop;
            Opacity   = _settings.WindowOpacity;
            LoadFaceImage();
            RebuildStaticOverlay();
            DrawHands();
        }

        // ── Face image ─────────────────────────────────────────────────────────
        private void LoadFaceImage()
        {
            BitmapImage? bmp = null;

            // Try user-specified image
            if (!string.IsNullOrWhiteSpace(_settings.ClockFaceImagePath) &&
                System.IO.File.Exists(_settings.ClockFaceImagePath))
            {
                bmp = SafeLoadBitmap(_settings.ClockFaceImagePath);
            }

            // Fall back to bundled default
            if (bmp == null)
            {
                var defaultPath = System.IO.Path.Combine(
                    AppContext.BaseDirectory, "Assets", "clockface_default.png");
                if (System.IO.File.Exists(defaultPath))
                    bmp = SafeLoadBitmap(defaultPath);
            }

            // Scale: use image pixel size as native, then apply ScaleFactor
            double nativeSize = 512; // fallback
            if (bmp != null)
            {
                // Use whichever dimension is smaller (face should be square-ish)
                nativeSize = Math.Min(bmp.PixelWidth, bmp.PixelHeight);
            }

            _faceSize = Math.Max(80, nativeSize * _settings.ScaleFactor);

            // Size root canvas & image
            RootCanvas.Width  = _faceSize;
            RootCanvas.Height = _faceSize;
            FaceImage.Width   = _faceSize;
            FaceImage.Height  = _faceSize;
            FaceImage.Source  = bmp;

            OverlayCanvas.Width  = _faceSize;
            OverlayCanvas.Height = _faceSize;
        }

        private static BitmapImage? SafeLoadBitmap(string path)
        {
            try
            {
                var bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.UriSource = new Uri(path, UriKind.Absolute);
                bmp.CacheOption = BitmapCacheOption.OnLoad;
                bmp.EndInit();
                bmp.Freeze();
                return bmp;
            }
            catch { return null; }
        }

        // ── Static overlay: tick marks ─────────────────────────────────────────
        private void RebuildStaticOverlay()
        {
            OverlayCanvas.Children.Clear();

            var mode         = _settings.ActiveMode();
            var majorColor   = ParseColor(_settings.MajorTickColor);
            var minorColor   = ParseColor(_settings.MinorTickColor);

            double r          = Centre;
            double totalTicks = mode.MajorTicks * (mode.MinorTicksPerSegment + 1);

            for (int i = 0; i < totalTicks; i++)
            {
                bool isMajor = (i % (mode.MinorTicksPerSegment + 1)) == 0;
                if (isMajor && !_settings.ShowMajorTicks) continue;
                if (!isMajor && !_settings.ShowMinorTicks) continue;

                double angle    = 360.0 * i / totalTicks - 90.0; // -90 so 0 is at top
                double rad      = angle * Math.PI / 180.0;

                double outerR   = r * 0.93;
                double innerR   = isMajor ? r * 0.80 : r * 0.88;
                double thick    = isMajor
                    ? Math.Max(1.5, r * 0.018)
                    : Math.Max(0.8, r * 0.010);

                var line = new Line
                {
                    X1              = r + Math.Cos(rad) * innerR,
                    Y1              = r + Math.Sin(rad) * innerR,
                    X2              = r + Math.Cos(rad) * outerR,
                    Y2              = r + Math.Sin(rad) * outerR,
                    StrokeThickness    = thick,
                    Stroke             = new SolidColorBrush(isMajor ? majorColor : minorColor),
                    StrokeStartLineCap = PenLineCap.Round,
                    StrokeEndLineCap   = PenLineCap.Round
                };
                OverlayCanvas.Children.Add(line);
            }
        }

        // ── Hands ─────────────────────────────────────────────────────────────
        // Tag constants so we can remove old hands before redrawing
        private const string TagHour   = "H";
        private const string TagMinute = "M";
        private const string TagSecond = "S";
        private const string TagDot    = "D";

        private void DrawHands()
        {
            // Remove previous hand elements
            for (int i = OverlayCanvas.Children.Count - 1; i >= 0; i--)
            {
                var el = OverlayCanvas.Children[i];
                var tag = (el as FrameworkElement)?.Tag as string;
                if (tag is TagHour or TagMinute or TagSecond or TagDot)
                    OverlayCanvas.Children.RemoveAt(i);
            }

            var mode    = _settings.ActiveMode();
            var tz      = _settings.ActiveTimeZone();
            var (hourAngle, minuteAngle, secondAngle) = mode.GetAngles(DateTime.UtcNow, tz);

            double r   = Centre;
            double bas = r * 0.010; // base thickness unit

            // Hour hand
            AddHand(hourAngle,
                length: r * 0.52,
                tailLength: r * 0.12,
                thickness: bas * 5.5 * _settings.HourHandThickness,
                color: ParseColor(_settings.HourHandColor),
                tag: TagHour);

            // Minute hand
            AddHand(minuteAngle,
                length: r * 0.76,
                tailLength: r * 0.12,
                thickness: bas * 3.5 * _settings.MinuteHandThickness,
                color: ParseColor(_settings.MinuteHandColor),
                tag: TagMinute);

            // Second hand
            if (_settings.ShowSeconds)
            {
                AddHand(secondAngle,
                    length: r * 0.84,
                    tailLength: r * 0.18,
                    thickness: bas * 1.5 * _settings.SecondHandThickness,
                    color: ParseColor(_settings.SecondHandColor),
                    tag: TagSecond);
            }

            // Centre dot
            double dotR = bas * 4;
            var dot = new Ellipse
            {
                Width  = dotR * 2,
                Height = dotR * 2,
                Fill   = new SolidColorBrush(ParseColor(_settings.CenterDotColor)),
                Tag    = TagDot
            };
            Canvas.SetLeft(dot, r - dotR);
            Canvas.SetTop(dot,  r - dotR);
            OverlayCanvas.Children.Add(dot);
        }

        private void AddHand(double angleDeg, double length, double tailLength,
                              double thickness, Color color, string tag)
        {
            double r   = Centre;
            double rad = (angleDeg - 90) * Math.PI / 180.0;

            double x2 = r + Math.Cos(rad) * length;
            double y2 = r + Math.Sin(rad) * length;
            double x1 = r - Math.Cos(rad) * tailLength;
            double y1 = r - Math.Sin(rad) * tailLength;

            var line = new Line
            {
                X1              = x1, Y1 = y1,
                X2              = x2, Y2 = y2,
                StrokeThickness    = thickness,
                Stroke             = new SolidColorBrush(color),
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap   = PenLineCap.Round,
                Tag             = tag
            };
            OverlayCanvas.Children.Add(line);
        }

        // ── Helpers ────────────────────────────────────────────────────────────
        private static Color ParseColor(string hex)
        {
            try { return (Color)ColorConverter.ConvertFromString(hex); }
            catch { return Colors.Black; }
        }

        // ── Window dragging ────────────────────────────────────────────────────
        private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                OpenSettings_Click(sender, e);
                return;
            }
            DragMove();
        }

        protected override void OnLocationChanged(EventArgs e)
        {
            base.OnLocationChanged(e);
            _settings.WindowLeft = Left;
            _settings.WindowTop  = Top;
            _settings.Save();
        }

        // ── Context menu handlers ──────────────────────────────────────────────
        private void OpenSettings_Click(object sender, RoutedEventArgs e)
        {
            _timer.Stop();
            var win = new SettingsWindow(_settings) { Owner = this };
            if (win.ShowDialog() == true)
                ApplySettings();
            _timer.Start();
        }

        private void AlwaysOnTop_Click(object sender, RoutedEventArgs e)
        {
            AlwaysOnTop = !AlwaysOnTop;
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            _settings.Save();
            Application.Current.Shutdown();
        }
    }
}
