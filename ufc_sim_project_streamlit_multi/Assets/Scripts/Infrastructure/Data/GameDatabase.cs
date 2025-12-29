using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UFC.Core.Models;
using UFC.Infrastructure.Csv;

namespace UFC.Infrastructure.Data
{
    public class GameDatabase
    {
        private readonly string _dataRoot;

        public GameDatabase(string dataRoot)
        {
            _dataRoot = dataRoot;
        }

        private string GlobalDir => Path.Combine(_dataRoot, "_global");
        private string EventsCsv => Path.Combine(GlobalDir, "events.csv");
        private string FightsCsv => Path.Combine(GlobalDir, "fights.csv");
        private string SaveCsv => Path.Combine(GlobalDir, "save_game.csv");

        public void EnsureGlobalFiles()
        {
            Directory.CreateDirectory(GlobalDir);
            if (!File.Exists(EventsCsv))
            {
                CsvUtil.WriteCsvDicts(EventsCsv, new List<Dictionary<string, string>>(), GameColumns.EventsColumns);
            }
            if (!File.Exists(FightsCsv))
            {
                CsvUtil.WriteCsvDicts(FightsCsv, new List<Dictionary<string, string>>(), GameColumns.FightsColumns);
            }
            if (!File.Exists(SaveCsv))
            {
                CsvUtil.WriteKv(SaveCsv, new Dictionary<string, string>
                {
                    {"current_date", "2026-01-01"},
                    {"next_event_id", "1"},
                    {"next_fight_id", "1"},
                    {"last_title_fight_date", ""},
                    {"random_seed", "12345"}
                });
            }
        }

        public GameState LoadState()
        {
            EnsureGlobalFiles();
            var fightersByDiv = new Dictionary<string, List<Fighter>>();
            var pairsByDiv = new Dictionary<string, List<Dictionary<string, string>>>();
            foreach (var div in ListDivisions())
            {
                string divDir = Path.Combine(_dataRoot, div);
                string fightersPath = ResolveCsvPath(divDir, "fighters.csv");
                string pairsPath = ResolveCsvPath(divDir, "pair_history.csv");
                var fighterRows = CsvUtil.ReadCsvDicts(fightersPath);
                fightersByDiv[div] = fighterRows.ConvertAll(Fighter.FromDict);
                pairsByDiv[div] = CsvUtil.ReadCsvDicts(pairsPath);
            }

            var events = CsvUtil.ReadCsvDicts(EventsCsv).ConvertAll(EventRow.FromDict);
            var fights = CsvUtil.ReadCsvDicts(FightsCsv).ConvertAll(FightRow.FromDict);
            var save = SaveGameKV.FromDict(CsvUtil.ReadKv(SaveCsv));

            return new GameState
            {
                FightersByDivision = fightersByDiv,
                Events = events,
                Fights = fights,
                PairsByDivision = pairsByDiv,
                Save = save
            };
        }

        public void SaveState(GameState state)
        {
            EnsureGlobalFiles();
            foreach (var kvp in state.FightersByDivision)
            {
                string divDir = Path.Combine(_dataRoot, kvp.Key);
                Directory.CreateDirectory(divDir);
                var rows = kvp.Value.ConvertAll(f => f.ToDict());
                CsvUtil.WriteCsvDicts(Path.Combine(divDir, "fighters.csv"), rows, GameColumns.FightersColumns);
            }
            foreach (var kvp in state.PairsByDivision)
            {
                string divDir = Path.Combine(_dataRoot, kvp.Key);
                Directory.CreateDirectory(divDir);
                CsvUtil.WriteCsvDicts(Path.Combine(divDir, "pair_history.csv"), kvp.Value, GameColumns.PairsColumns);
            }

            CsvUtil.WriteCsvDicts(EventsCsv, state.Events.ConvertAll(e => e.ToDict()), GameColumns.EventsColumns);
            CsvUtil.WriteCsvDicts(FightsCsv, state.Fights.ConvertAll(f => f.ToDict()), GameColumns.FightsColumns);
            CsvUtil.WriteKv(SaveCsv, state.Save.ToDict());
        }

        public List<string> ListDivisions()
        {
            if (!Directory.Exists(_dataRoot))
            {
                return new List<string> { "Flyweight" };
            }

            var dirs = Directory.GetDirectories(_dataRoot)
                .Select(Path.GetFileName)
                .Where(name => !string.IsNullOrEmpty(name) && name != "_global")
                .ToList();

            return dirs.Count > 0 ? dirs : new List<string> { "Flyweight" };
        }

        private static string ResolveCsvPath(string directory, string fileName)
        {
            var preferred = Path.Combine(directory, fileName);
            if (File.Exists(preferred) || !Directory.Exists(directory))
            {
                return preferred;
            }

            var match = Directory.EnumerateFiles(directory)
                .FirstOrDefault(file => string.Equals(Path.GetFileName(file), fileName, StringComparison.OrdinalIgnoreCase));

            return match ?? preferred;
        }
    }

    public static class GameColumns
    {
        public static readonly List<string> EventsColumns = new List<string>
        {
            "event_id","event_date","generated_on","announced_main_on","announced_full_on",
            "completed","main_fight_id","event_kind","location","theme_country","notes_json"
        };

        public static readonly List<string> FightsColumns = new List<string>
        {
            "fight_id","event_id","division","a_id","b_id",
            "is_top15","is_main_event","is_title_fight",
            "card_slot","status","winner_id","method","round","time_mmss"
        };

        public static readonly List<string> FightersColumns = new List<string>
        {
            "id","division","name","country","age","rank_raw","rank_type","rank_slot","is_champ",
            "wins","draws","losses","rating","streak","last_fight_date","next_available_date",
            "rating_history","rank_history","is_active"
        };

        public static readonly List<string> PairsColumns = new List<string>
        {
            "a_id","b_id","last_fight_date"
        };
    }

    public class GameState
    {
        public Dictionary<string, List<Fighter>> FightersByDivision;
        public List<EventRow> Events;
        public List<FightRow> Fights;
        public Dictionary<string, List<Dictionary<string, string>>> PairsByDivision;
        public SaveGameKV Save;
    }
}
