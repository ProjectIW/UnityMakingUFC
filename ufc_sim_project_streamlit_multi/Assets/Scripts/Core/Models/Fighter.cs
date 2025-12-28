using System;
using System.Collections.Generic;

namespace UFC.Core.Models
{
    [Serializable]
    public class Fighter
    {
        public int Id;
        public string Division;
        public string Name;
        public string Country;
        public int Age;
        public string RankRaw;
        public string RankType;
        public string RankSlot;
        public int IsChamp;
        public int Wins;
        public int Draws;
        public int Losses;
        public float Rating;
        public int Streak;
        public string LastFightDate;
        public string NextAvailableDate;
        public string RatingHistory;
        public string RankHistory;
        public int IsActive;

        public static Fighter FromDict(Dictionary<string, string> row)
        {
            return new Fighter
            {
                Id = ParseInt(row, "id"),
                Division = Get(row, "division"),
                Name = Get(row, "name"),
                Country = Get(row, "country"),
                Age = ParseInt(row, "age"),
                RankRaw = Get(row, "rank_raw"),
                RankType = Get(row, "rank_type"),
                RankSlot = Get(row, "rank_slot"),
                IsChamp = ParseInt(row, "is_champ"),
                Wins = ParseInt(row, "wins"),
                Draws = ParseInt(row, "draws"),
                Losses = ParseInt(row, "losses"),
                Rating = ParseFloat(row, "rating", 1500f),
                Streak = ParseInt(row, "streak"),
                LastFightDate = Get(row, "last_fight_date"),
                NextAvailableDate = Get(row, "next_available_date"),
                RatingHistory = Get(row, "rating_history"),
                RankHistory = Get(row, "rank_history"),
                IsActive = ParseInt(row, "is_active", 1)
            };
        }

        public Dictionary<string, string> ToDict()
        {
            return new Dictionary<string, string>
            {
                {"id", Id.ToString()},
                {"division", Division ?? string.Empty},
                {"name", Name ?? string.Empty},
                {"country", Country ?? string.Empty},
                {"age", Age.ToString()},
                {"rank_raw", RankRaw ?? string.Empty},
                {"rank_type", RankType ?? string.Empty},
                {"rank_slot", RankSlot ?? string.Empty},
                {"is_champ", IsChamp.ToString()},
                {"wins", Wins.ToString()},
                {"draws", Draws.ToString()},
                {"losses", Losses.ToString()},
                {"rating", Rating.ToString("F2")},
                {"streak", Streak.ToString()},
                {"last_fight_date", LastFightDate ?? string.Empty},
                {"next_available_date", NextAvailableDate ?? string.Empty},
                {"rating_history", RatingHistory ?? string.Empty},
                {"rank_history", RankHistory ?? string.Empty},
                {"is_active", IsActive.ToString()}
            };
        }

        private static string Get(Dictionary<string, string> row, string key)
        {
            return row != null && row.TryGetValue(key, out var value) ? value : string.Empty;
        }

        private static int ParseInt(Dictionary<string, string> row, string key, int defaultValue = 0)
        {
            if (row != null && row.TryGetValue(key, out var value) && int.TryParse(value, out var parsed))
            {
                return parsed;
            }
            return defaultValue;
        }

        private static float ParseFloat(Dictionary<string, string> row, string key, float defaultValue = 0f)
        {
            if (row != null && row.TryGetValue(key, out var value) && float.TryParse(value, out var parsed))
            {
                return parsed;
            }
            return defaultValue;
        }
    }
}
