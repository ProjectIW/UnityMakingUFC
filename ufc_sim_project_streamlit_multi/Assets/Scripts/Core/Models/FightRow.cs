using System;
using System.Collections.Generic;

namespace UFC.Core.Models
{
    [Serializable]
    public class FightRow
    {
        public int FightId;
        public int EventId;
        public string Division;
        public int AId;
        public int BId;
        public int IsTop15;
        public int IsMainEvent;
        public int IsTitleFight;
        public string CardSlot;
        public string Status;
        public string WinnerId;
        public string Method;
        public string Round;
        public string TimeMmss;

        public static FightRow FromDict(Dictionary<string, string> row)
        {
            return new FightRow
            {
                FightId = ParseInt(row, "fight_id"),
                EventId = ParseInt(row, "event_id"),
                Division = Get(row, "division"),
                AId = ParseInt(row, "a_id"),
                BId = ParseInt(row, "b_id"),
                IsTop15 = ParseInt(row, "is_top15"),
                IsMainEvent = ParseInt(row, "is_main_event"),
                IsTitleFight = ParseInt(row, "is_title_fight"),
                CardSlot = Get(row, "card_slot"),
                Status = Get(row, "status"),
                WinnerId = Get(row, "winner_id"),
                Method = Get(row, "method"),
                Round = Get(row, "round"),
                TimeMmss = Get(row, "time_mmss")
            };
        }

        public Dictionary<string, string> ToDict()
        {
            return new Dictionary<string, string>
            {
                {"fight_id", FightId.ToString()},
                {"event_id", EventId.ToString()},
                {"division", Division ?? string.Empty},
                {"a_id", AId.ToString()},
                {"b_id", BId.ToString()},
                {"is_top15", IsTop15.ToString()},
                {"is_main_event", IsMainEvent.ToString()},
                {"is_title_fight", IsTitleFight.ToString()},
                {"card_slot", CardSlot ?? string.Empty},
                {"status", Status ?? string.Empty},
                {"winner_id", WinnerId ?? string.Empty},
                {"method", Method ?? string.Empty},
                {"round", Round ?? string.Empty},
                {"time_mmss", TimeMmss ?? string.Empty}
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
    }
}
