using System;
using System.IO;
using UFC.Core.Game;
using UFC.Infrastructure.Data;
using UFC.Infrastructure.Save;
using UFC.UI.Screens;
using UnityEngine;

namespace UFC.Infrastructure
{
    public class GameManager : MonoBehaviour
    {
        public int SaveSlot = 1;
        public string DataRootOverride;
        public RankingScreen RankingScreen;
        public EventsScreen EventsScreen;
        public PastEventsScreen PastEventsScreen;
        public MainTabsScreen TabsScreen;

        private GameDatabase _database;
        private GameService _service;
        private GameState _state;

        private void Awake()
        {
            Initialize();
        }

        public void Initialize()
        {
            EnsureSaveSlot();
            var dataRoot = ResolveDataRoot();
            if (!HasDivisionData(dataRoot))
            {
                var streamingRoot = Path.Combine(Application.streamingAssetsPath, "BaseData");
                if (HasDivisionData(streamingRoot))
                {
                    dataRoot = streamingRoot;
                }
                else
                {
                    SaveSlotsService.EnsureSlotData(SaveSlot);
                    var slotRoot = SaveSlotsService.SlotDataPath(SaveSlot);
                    if (HasDivisionData(slotRoot))
                    {
                        dataRoot = slotRoot;
                    }
                }
            }

            ResolveUiReferences();

            _database = new GameDatabase(dataRoot);
            _state = _database.LoadState();
            EnsureFighterDataLoaded(dataRoot);
            int? seed = TryParseSeed();
            _service = new GameService(_database, seed);

            var today = DateUtil.ParseDateOrDefault(_state.Save.CurrentDate, new DateTime(2026, 1, 1));
            _service.EnsureHistoriesInitialized(_state, today);
            _service.EnsureEventsPlanned(_state, today);
            _service.SaveState(_state);
            RefreshUi();
        }

        public void NextWeek()
        {
            if (_state == null || _service == null)
            {
                Initialize();
                if (_state == null || _service == null)
                {
                    return;
                }
            }

            var today = DateUtil.ParseDateOrDefault(_state.Save.CurrentDate, new DateTime(2026, 1, 1));
            _service.EnsureHistoriesInitialized(_state, today);
            _service.EnsureEventsPlanned(_state, today);
            var nextDate = _service.AdvanceToNextWeek(_state);
            _service.EnsureEventsPlanned(_state, nextDate);
            _service.RunEvent(_state, nextDate);
            _service.SaveState(_state);
            RefreshUi();
        }

        private void RefreshUi()
        {
            RankingScreen?.Refresh(_state);
            EventsScreen?.Refresh(_state);
            PastEventsScreen?.Refresh(_state);
            TabsScreen?.ShowRanking();
        }

        private string ResolveDataRoot()
        {
            if (string.IsNullOrWhiteSpace(DataRootOverride))
            {
                return SaveSlotsService.SlotDataPath(SaveSlot);
            }

            var overridePath = DataRootOverride.Trim();
            if (Path.IsPathRooted(overridePath))
            {
                return overridePath;
            }

            var projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
            if (!string.IsNullOrEmpty(projectRoot))
            {
                var combined = Path.Combine(projectRoot, overridePath);
                if (Directory.Exists(combined))
                {
                    return combined;
                }
            }

            var normalized = overridePath.Replace("\\", "/");
            var streamingToken = "/StreamingAssets/";
            var idx = normalized.IndexOf(streamingToken, StringComparison.OrdinalIgnoreCase);
            if (idx >= 0)
            {
                var suffix = normalized.Substring(idx + streamingToken.Length);
                var candidate = Path.Combine(Application.streamingAssetsPath, suffix);
                if (Directory.Exists(candidate))
                {
                    return candidate;
                }
            }

            return overridePath;
        }

        private void ResolveUiReferences()
        {
            if (RankingScreen == null)
            {
                RankingScreen = FindObjectOfType<RankingScreen>(true);
            }
            if (EventsScreen == null)
            {
                EventsScreen = FindObjectOfType<EventsScreen>(true);
            }
            if (PastEventsScreen == null)
            {
                PastEventsScreen = FindObjectOfType<PastEventsScreen>(true);
            }
            if (TabsScreen == null)
            {
                TabsScreen = FindObjectOfType<MainTabsScreen>(true);
            }
        }

        private void EnsureSaveSlot()
        {
            if (!string.IsNullOrWhiteSpace(DataRootOverride))
            {
                return;
            }

            SaveSlotsService.EnsureSlotData(SaveSlot);
        }

        private int? TryParseSeed()
        {
            if (_state == null || string.IsNullOrWhiteSpace(_state.Save.RandomSeed))
            {
                return null;
            }
            if (int.TryParse(_state.Save.RandomSeed, out var parsed))
            {
                return parsed;
            }
            return null;
        }

        private static bool HasDivisionData(string dataRoot)
        {
            if (string.IsNullOrWhiteSpace(dataRoot) || !Directory.Exists(dataRoot))
            {
                return false;
            }

            foreach (var dir in Directory.GetDirectories(dataRoot))
            {
                var name = Path.GetFileName(dir);
                if (string.IsNullOrEmpty(name) || name == "_global")
                {
                    continue;
                }

                if (File.Exists(Path.Combine(dir, "fighters.csv")))
                {
                    return true;
                }
            }

            return false;
        }

        private void EnsureFighterDataLoaded(string dataRoot)
        {
            if (HasFighterData(_state))
            {
                return;
            }

            var streamingRoot = Path.Combine(Application.streamingAssetsPath, "BaseData");
            if (string.IsNullOrWhiteSpace(streamingRoot) || !Directory.Exists(streamingRoot))
            {
                return;
            }

            if (string.Equals(dataRoot, streamingRoot, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            var fallbackDatabase = new GameDatabase(streamingRoot);
            var fallbackState = fallbackDatabase.LoadState();
            if (!HasFighterData(fallbackState))
            {
                return;
            }

            _database = fallbackDatabase;
            _state = fallbackState;
        }

        private static bool HasFighterData(GameState state)
        {
            if (state?.FightersByDivision == null)
            {
                return false;
            }

            foreach (var division in state.FightersByDivision.Values)
            {
                if (division != null && division.Count > 0)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
