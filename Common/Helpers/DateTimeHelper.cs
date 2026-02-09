using System.Globalization;

namespace SphereScheduleAPI.Common.Helpers
{
    public static class DateTimeHelper
    {
        public static DateTimeOffset ConvertToUtc(DateTimeOffset dateTime, string timezoneId)
        {
            try
            {
                var timezone = TimeZoneInfo.FindSystemTimeZoneById(timezoneId);
                return TimeZoneInfo.ConvertTimeToUtc(dateTime.DateTime, timezone);
            }
            catch (TimeZoneNotFoundException)
            {
                // Fallback to UTC if timezone not found
                return dateTime.ToUniversalTime();
            }
        }

        public static DateTimeOffset ConvertFromUtc(DateTimeOffset utcDateTime, string timezoneId)
        {
            try
            {
                var timezone = TimeZoneInfo.FindSystemTimeZoneById(timezoneId);
                return TimeZoneInfo.ConvertTimeFromUtc(utcDateTime.DateTime, timezone);
            }
            catch (TimeZoneNotFoundException)
            {
                // Fallback to UTC if timezone not found
                return utcDateTime;
            }
        }

        public static DateTimeOffset GetStartOfDay(DateTimeOffset date)
        {
            return new DateTimeOffset(date.Year, date.Month, date.Day, 0, 0, 0, date.Offset);
        }

        public static DateTimeOffset GetEndOfDay(DateTimeOffset date)
        {
            return new DateTimeOffset(date.Year, date.Month, date.Day, 23, 59, 59, 999, date.Offset);
        }

        public static DateTimeOffset GetStartOfWeek(DateTimeOffset date, DayOfWeek startOfWeek = DayOfWeek.Monday)
        {
            int diff = (7 + (date.DayOfWeek - startOfWeek)) % 7;
            return GetStartOfDay(date.AddDays(-1 * diff));
        }

        public static DateTimeOffset GetEndOfWeek(DateTimeOffset date, DayOfWeek startOfWeek = DayOfWeek.Monday)
        {
            var start = GetStartOfWeek(date, startOfWeek);
            return GetEndOfDay(start.AddDays(6));
        }

        public static DateTimeOffset GetStartOfMonth(DateTimeOffset date)
        {
            return new DateTimeOffset(date.Year, date.Month, 1, 0, 0, 0, date.Offset);
        }

        public static DateTimeOffset GetEndOfMonth(DateTimeOffset date)
        {
            var lastDay = DateTime.DaysInMonth(date.Year, date.Month);
            return new DateTimeOffset(date.Year, date.Month, lastDay, 23, 59, 59, 999, date.Offset);
        }

        public static DateTimeOffset GetStartOfYear(DateTimeOffset date)
        {
            return new DateTimeOffset(date.Year, 1, 1, 0, 0, 0, date.Offset);
        }

        public static DateTimeOffset GetEndOfYear(DateTimeOffset date)
        {
            return new DateTimeOffset(date.Year, 12, 31, 23, 59, 59, 999, date.Offset);
        }

        public static bool IsBusinessDay(DateTimeOffset date, CultureInfo culture = null)
        {
            culture ??= CultureInfo.CurrentCulture;

            // Check if it's a weekend
            if (date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday)
                return false;

            // Check if it's a holiday (simplified - in production, use a holiday calendar)
            // You would typically load holidays from a database or configuration
            return true;
        }

        public static DateTimeOffset AddBusinessDays(DateTimeOffset date, int days, CultureInfo culture = null)
        {
            if (days == 0) return date;

            var result = date;
            int direction = days > 0 ? 1 : -1;

            while (days != 0)
            {
                result = result.AddDays(direction);
                if (IsBusinessDay(result, culture))
                {
                    days -= direction;
                }
            }

            return result;
        }

        public static int GetBusinessDaysBetween(DateTimeOffset start, DateTimeOffset end, CultureInfo culture = null)
        {
            if (start > end)
                return -GetBusinessDaysBetween(end, start, culture);

            int businessDays = 0;
            var current = start;

            while (current <= end)
            {
                if (IsBusinessDay(current, culture))
                    businessDays++;

                current = current.AddDays(1);
            }

            return businessDays;
        }

        public static string FormatDuration(TimeSpan duration)
        {
            if (duration.TotalDays >= 1)
                return $"{(int)duration.TotalDays}d {duration.Hours}h {duration.Minutes}m";
            else if (duration.TotalHours >= 1)
                return $"{(int)duration.TotalHours}h {duration.Minutes}m";
            else
                return $"{duration.Minutes}m";
        }

        public static string GetRelativeTimeString(DateTimeOffset date)
        {
            var now = DateTimeOffset.UtcNow;
            var diff = now - date;

            if (diff.TotalSeconds < 60)
                return "just now";
            if (diff.TotalMinutes < 60)
                return $"{(int)diff.TotalMinutes} minute{(diff.TotalMinutes >= 2 ? "s" : "")} ago";
            if (diff.TotalHours < 24)
                return $"{(int)diff.TotalHours} hour{(diff.TotalHours >= 2 ? "s" : "")} ago";
            if (diff.TotalDays < 7)
                return $"{(int)diff.TotalDays} day{(diff.TotalDays >= 2 ? "s" : "")} ago";
            if (diff.TotalDays < 30)
                return $"{(int)(diff.TotalDays / 7)} week{(diff.TotalDays / 7 >= 2 ? "s" : "")} ago";
            if (diff.TotalDays < 365)
                return $"{(int)(diff.TotalDays / 30)} month{(diff.TotalDays / 30 >= 2 ? "s" : "")} ago";

            return $"{(int)(diff.TotalDays / 365)} year{(diff.TotalDays / 365 >= 2 ? "s" : "")} ago";
        }

        public static bool IsDateInRange(DateTimeOffset date, DateTimeOffset start, DateTimeOffset end)
        {
            return date >= start && date <= end;
        }

        public static bool DoDateRangesOverlap(DateTimeOffset start1, DateTimeOffset end1, DateTimeOffset start2, DateTimeOffset end2)
        {
            return start1 <= end2 && start2 <= end1;
        }

        public static TimeSpan? CalculateOverlap(DateTimeOffset start1, DateTimeOffset end1, DateTimeOffset start2, DateTimeOffset end2)
        {
            if (!DoDateRangesOverlap(start1, end1, start2, end2))
                return null;

            var overlapStart = start1 > start2 ? start1 : start2;
            var overlapEnd = end1 < end2 ? end1 : end2;

            return overlapEnd - overlapStart;
        }

        public static DateTimeOffset RoundToNearestMinutes(DateTimeOffset date, int minutes)
        {
            var totalMinutes = (int)(date.Ticks / TimeSpan.TicksPerMinute);
            var roundedMinutes = (int)(Math.Round((double)totalMinutes / minutes) * minutes);
            return new DateTimeOffset(roundedMinutes * TimeSpan.TicksPerMinute, date.Offset);
        }

        public static IEnumerable<DateTimeOffset> GetDateRange(DateTimeOffset start, DateTimeOffset end)
        {
            for (var date = start.Date; date <= end.Date; date = date.AddDays(1))
            {
                yield return date;
            }
        }

        public static int GetAge(DateTimeOffset birthDate, DateTimeOffset? referenceDate = null)
        {
            var reference = referenceDate ?? DateTimeOffset.UtcNow;
            var age = reference.Year - birthDate.Year;

            if (birthDate.Date > reference.Date.AddYears(-age))
                age--;

            return age;
        }

        public static bool IsLeapYear(int year)
        {
            return DateTime.IsLeapYear(year);
        }

        public static int GetDaysInMonth(int year, int month)
        {
            return DateTime.DaysInMonth(year, month);
        }

        public static DayOfWeek GetFirstDayOfWeek(CultureInfo culture = null)
        {
            culture ??= CultureInfo.CurrentCulture;
            return culture.DateTimeFormat.FirstDayOfWeek;
        }
    }
}