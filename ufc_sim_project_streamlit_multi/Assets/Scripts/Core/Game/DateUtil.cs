using System;
using System.Globalization;

namespace UFC.Core.Game
{
    public static class DateUtil
    {
        public static DateTime? ParseDate(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }
            if (DateTime.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
            {
                return parsed;
            }
            return null;
        }

        public static DateTime ParseDateOrDefault(string value, DateTime defaultValue)
        {
            return ParseDate(value) ?? defaultValue;
        }
    }
}
