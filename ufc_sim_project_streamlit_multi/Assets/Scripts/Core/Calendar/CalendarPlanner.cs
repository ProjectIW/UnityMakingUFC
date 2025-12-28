using System;
using System.Collections.Generic;

namespace UFC.Core.Calendar
{
    public static class CalendarPlanner
    {
        private const int Saturday = 6;

        public static DateTime NextSaturday(DateTime date)
        {
            int delta = (Saturday - (int)date.DayOfWeek + 7) % 7;
            return date.AddDays(delta);
        }

        public static DateTime MainAnnounceDate(DateTime eventDate, PlanConfig cfg)
        {
            return eventDate.AddDays(-7 * cfg.MainAnnounceWeeks);
        }

        public static DateTime FullGenerateDate(DateTime eventDate, PlanConfig cfg)
        {
            return eventDate.AddDays(-7 * cfg.FullGenerateWeeks);
        }

        public static List<DateTime> EventDatesInHorizon(DateTime startDate, int horizonWeeks, System.Random rng)
        {
            var firstSat = NextSaturday(startDate);
            var saturdays = new List<DateTime>();
            for (int w = 0; w < horizonWeeks; w++)
            {
                saturdays.Add(firstSat.AddDays(7 * w));
            }

            var byMonth = new Dictionary<(int, int), List<DateTime>>();
            foreach (var d in saturdays)
            {
                var key = (d.Year, d.Month);
                if (!byMonth.ContainsKey(key))
                {
                    byMonth[key] = new List<DateTime>();
                }
                byMonth[key].Add(d);
            }

            var picks = new List<DateTime>();
            foreach (var kvp in byMonth)
            {
                var days = kvp.Value;
                if (days.Count == 0)
                {
                    continue;
                }
                int[] options = { 1, 2, 2, 2, 3 };
                int count = options[rng.Next(options.Length)];
                count = System.Math.Min(count, days.Count);
                for (int i = 0; i < count; i++)
                {
                    int idx = rng.Next(days.Count);
                    picks.Add(days[idx]);
                    days.RemoveAt(idx);
                }
            }
            picks.Sort();
            return picks;
        }
    }

    public class PlanConfig
    {
        public int MainAnnounceWeeks = 8;
        public int FullGenerateWeeks = 4;
        public int HorizonWeeks = 12;
    }
}
