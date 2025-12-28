using System;
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
            var dataRoot = string.IsNullOrWhiteSpace(DataRootOverride)
                ? SaveSlotsService.SlotDataPath(SaveSlot)
                : DataRootOverride;

            _database = new GameDatabase(dataRoot);
            int? seed = TryParseSeed();
            _service = new GameService(_database, seed);
            _state = _service.LoadState();

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
            _service.RunEvent(_state, today);
            var nextDate = _service.AdvanceToNextWeek(_state);
            _service.EnsureEventsPlanned(_state, nextDate);
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

        private void EnsureSaveSlot()
        {
            if (!SaveSlotsService.SlotExists(SaveSlot))
            {
                SaveSlotsService.CreateSlot(SaveSlot, overwrite: false);
            }
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
    }
}
