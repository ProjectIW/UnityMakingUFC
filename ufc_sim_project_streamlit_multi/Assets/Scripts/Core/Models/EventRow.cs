using System;
using System.Collections.Generic;

namespace UFC.Core.Models
{
    [Serializable]
    public class EventRow
    {
        public int EventId;
        public string EventDate;
        public string GeneratedOn;
        public string AnnouncedMainOn;
        public string AnnouncedFullOn;
        public int Completed;
        public string MainFightId;
        public string EventKind;
        public string Location;
        public string ThemeCountry;
        public string NotesJson;

        public static EventRow FromDict(Dictionary<string, string> row)
        {
            return new EventRow
            {
                EventId = ParseInt(row, "event_id"),
                EventDate = Get(row, "event_date"),
                GeneratedOn = Get(row, "generated_on"),
                AnnouncedMainOn = Get(row, "announced_main_on"),
                AnnouncedFullOn = Get(row, "announced_full_on"),
                Completed = ParseInt(row, "completed"),
                MainFightId = Get(row, "main_fight_id"),
                EventKind = Get(row, "event_kind"),
                Location = Get(row, "location"),
                ThemeCountry = Get(row, "theme_country"),
                NotesJson = Get(row, "notes_json")
            };
        }

        public Dictionary<string, string> ToDict()
        {
            return new Dictionary<string, string>
            {
                {"event_id", EventId.ToString()},
                {"event_date", EventDate ?? string.Empty},
                {"generated_on", GeneratedOn ?? string.Empty},
                {"announced_main_on", AnnouncedMainOn ?? string.Empty},
                {"announced_full_on", AnnouncedFullOn ?? string.Empty},
                {"completed", Completed.ToString()},
                {"main_fight_id", MainFightId ?? string.Empty},
                {"event_kind", EventKind ?? string.Empty},
                {"location", Location ?? string.Empty},
                {"theme_country", ThemeCountry ?? string.Empty},
                {"notes_json", NotesJson ?? string.Empty}
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
