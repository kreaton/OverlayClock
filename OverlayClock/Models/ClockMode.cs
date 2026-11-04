using System;

namespace OverlayClock.Models
{
    /// <summary>
    /// Describes a timekeeping system: how many hours in a day,
    /// minutes per hour, seconds per minute, and tick configuration.
    /// </summary>
    public class ClockMode
    {
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";

        // How many "hours" divide a full day
        public int HoursPerDay { get; set; } = 12;

        // How many "minutes" per hour
        public int MinutesPerHour { get; set; } = 60;

        // How many "seconds" per minute
        public int SecondsPerMinute { get; set; } = 60;

        // Major tick marks (labeled) — e.g. 12 for standard, 10 for decimal
        public int MajorTicks { get; set; } = 12;

        // Minor tick marks between each pair of major ticks
        public int MinorTicksPerSegment { get; set; } = 4;

        // Whether the hour hand makes 1 or 2 rotations per day
        public int RotationsPerDay { get; set; } = 2;

        // Derive current hand angles from a UTC DateTime
        public (double HourAngle, double MinuteAngle, double SecondAngle) GetAngles(DateTime utcNow, TimeZoneInfo tz)
        {
            var local = TimeZoneInfo.ConvertTimeFromUtc(utcNow, tz);
            double totalSecondsInDay = 24.0 * 3600.0;
            double elapsedSeconds = local.Hour * 3600.0 + local.Minute * 60.0 + local.Second + local.Millisecond / 1000.0;

            // Normalise to the clock's day length
            double dayFraction = elapsedSeconds / totalSecondsInDay;

            // Second hand: completes one rotation per (SecondsPerMinute * MinutesPerHour) "ticks"
            double secondsInClockMinute = SecondsPerMinute;
            double minutesInClockHour = MinutesPerHour;
            double hoursInClockDay = HoursPerDay * RotationsPerDay;

            // Real elapsed time mapped onto clock units
            double clockDayFraction = dayFraction; // 0..1 over 24h

            // Hour hand: RotationsPerDay full rotations per day
            double hourAngle = clockDayFraction * 360.0 * RotationsPerDay;

            // Minute hand: HoursPerDay * RotationsPerDay rotations per day
            double minuteAngle = clockDayFraction * 360.0 * HoursPerDay * RotationsPerDay;

            // Second hand: one rotation per clock-minute
            double secondsPerDay = minutesInClockHour * hoursInClockDay;
            double secondAngle = clockDayFraction * 360.0 * secondsPerDay;

            return (hourAngle % 360, minuteAngle % 360, secondAngle % 360);
        }

        // Built-in presets
        public static ClockMode Standard12Hour() => new ClockMode
        {
            Name = "Standard 12-Hour",
            Description = "Traditional 12-hour clock face",
            HoursPerDay = 12,
            MinutesPerHour = 60,
            SecondsPerMinute = 60,
            MajorTicks = 12,
            MinorTicksPerSegment = 4,
            RotationsPerDay = 2
        };

        public static ClockMode Standard24Hour() => new ClockMode
        {
            Name = "24-Hour (Military)",
            Description = "24-hour clock — one rotation per day",
            HoursPerDay = 24,
            MinutesPerHour = 60,
            SecondsPerMinute = 60,
            MajorTicks = 24,
            MinorTicksPerSegment = 4,
            RotationsPerDay = 1
        };

        public static ClockMode Decimal10Hour() => new ClockMode
        {
            Name = "Decimal 10-Hour",
            Description = "French Revolutionary decimal time: 10h/day, 100min/h, 100s/min",
            HoursPerDay = 10,
            MinutesPerHour = 100,
            SecondsPerMinute = 100,
            MajorTicks = 10,
            MinorTicksPerSegment = 4,
            RotationsPerDay = 1
        };

        public static ClockMode Industrial6Hour() => new ClockMode
        {
            Name = "Industrial 6-Hour",
            Description = "6-hour shifts: face rotates 4× per day",
            HoursPerDay = 6,
            MinutesPerHour = 60,
            SecondsPerMinute = 60,
            MajorTicks = 6,
            MinorTicksPerSegment = 4,
            RotationsPerDay = 4
        };

        public static ClockMode Swatch() => new ClockMode
        {
            Name = "Swatch Internet Time",
            Description = "@beats: 1000 beats per day, no time zones",
            HoursPerDay = 10,      // repurposed: 10 major ticks
            MinutesPerHour = 100,
            SecondsPerMinute = 100,
            MajorTicks = 10,
            MinorTicksPerSegment = 9,
            RotationsPerDay = 1
        };

        public static ClockMode[] AllPresets() => new[]
        {
            Standard12Hour(),
            Standard24Hour(),
            Decimal10Hour(),
            Industrial6Hour(),
            Swatch()
        };
    }
}
