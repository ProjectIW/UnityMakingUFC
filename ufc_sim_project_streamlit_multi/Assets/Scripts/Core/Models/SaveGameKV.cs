using System;
using System.Collections.Generic;

namespace UFC.Core.Models
{
    [Serializable]
    public class SaveGameKV
    {
        public string CurrentDate;
        public string NextEventId;
        public string NextFightId;
        public string LastTitleFightDate;
        public string RandomSeed;

        public static SaveGameKV FromDict(Dictionary<string, string> row)
        {
            return new SaveGameKV
            {
                CurrentDate = Get(row, "current_date"),
                NextEventId = Get(row, "next_event_id"),
                NextFightId = Get(row, "next_fight_id"),
                LastTitleFightDate = Get(row, "last_title_fight_date"),
                RandomSeed = Get(row, "random_seed")
            };
        }

        public Dictionary<string, string> ToDict()
        {
            return new Dictionary<string, string>
            {
                {"current_date", CurrentDate ?? string.Empty},
                {"next_event_id", NextEventId ?? string.Empty},
                {"next_fight_id", NextFightId ?? string.Empty},
                {"last_title_fight_date", LastTitleFightDate ?? string.Empty},
                {"random_seed", RandomSeed ?? string.Empty}
            };
        }

        private static string Get(Dictionary<string, string> row, string key)
        {
            return row != null && row.TryGetValue(key, out var value) ? value : string.Empty;
        }
    }
}
