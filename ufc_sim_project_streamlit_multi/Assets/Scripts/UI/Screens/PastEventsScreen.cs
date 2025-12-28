using System;
using System.Linq;
using UFC.Core.Game;
using UFC.Core.Models;
using UFC.Infrastructure.Data;
using UFC.UI.Widgets;
using UnityEngine;
using UFC.UI.Theme;

namespace UFC.UI.Screens
{
    public class PastEventsScreen : MonoBehaviour
    {
        public Transform EventsListRoot;
        public Transform ResultsListRoot;
        public EventCardWidget EventCardPrefab;
        public FightCardWidget FightCardPrefab;

        private GameState _state;

        public void Refresh(GameState state)
        {
            UiTheme.EnsureInitialized(this);
            UiTheme.EnsureListLayout(EventsListRoot);
            UiTheme.EnsureListLayout(ResultsListRoot);

            _state = state;
            if (_state == null || EventsListRoot == null || EventCardPrefab == null)
            {
                return;
            }

            ClearList(EventsListRoot);
            ClearList(ResultsListRoot);

            var today = DateUtil.ParseDateOrDefault(_state.Save.CurrentDate, new DateTime(2026, 1, 1));
            var past = _state.Events
                .Select(e => new { Event = e, EventDate = DateUtil.ParseDate(e.EventDate) })
                .Where(e => e.Event.Completed == 1 || (e.EventDate.HasValue && e.EventDate.Value < today))
                .OrderByDescending(e => e.EventDate ?? DateTime.MinValue)
                .Select(e => e.Event)
                .ToList();

            foreach (var ev in past)
            {
                string title = $"{ev.EventDate} · {ev.EventKind}";
                string subtitle = $"{ev.Location} {ev.ThemeCountry}".Trim();
                var card = Instantiate(EventCardPrefab, EventsListRoot);
                var captured = ev;
                card.Bind(title, subtitle, () => ShowEventResults(captured));
            }

            if (past.Count > 0)
            {
                ShowEventResults(past[0]);
            }
        }

        private void ShowEventResults(EventRow ev)
        {
            if (_state == null || ResultsListRoot == null || FightCardPrefab == null || ev == null)
            {
                return;
            }

            ClearList(ResultsListRoot);

            var fights = _state.Fights
                .Where(f => f.EventId == ev.EventId)
                .OrderByDescending(f => f.IsMainEvent)
                .ThenBy(f => f.CardSlot)
                .ToList();

            foreach (var fight in fights)
            {
                string aName = FighterName(fight.Division, fight.AId);
                string bName = FighterName(fight.Division, fight.BId);
                string result = fight.Status == "completed"
                    ? $"{WinnerName(fight, aName, bName)} · {fight.Method} R{fight.Round} {fight.TimeMmss}"
                    : fight.Status;
                string title = $"{aName} vs {bName}";
                var card = Instantiate(FightCardPrefab, ResultsListRoot);
                card.Bind(title, result);
            }
        }

        private string WinnerName(FightRow fight, string aName, string bName)
        {
            if (fight == null || string.IsNullOrWhiteSpace(fight.WinnerId))
            {
                return string.Empty;
            }
            if (int.TryParse(fight.WinnerId, out var winnerId))
            {
                return winnerId == fight.AId ? aName : winnerId == fight.BId ? bName : $"#{winnerId}";
            }
            return fight.WinnerId;
        }

        private string FighterName(string division, int id)
        {
            if (_state == null || string.IsNullOrWhiteSpace(division) || !_state.FightersByDivision.ContainsKey(division))
            {
                return $"#{id}";
            }
            var fighter = _state.FightersByDivision[division].FirstOrDefault(f => f.Id == id);
            return fighter != null ? fighter.Name : $"#{id}";
        }

        private static void ClearList(Transform root)
        {
            if (root == null)
            {
                return;
            }
            for (int i = root.childCount - 1; i >= 0; i--)
            {
                Destroy(root.GetChild(i).gameObject);
            }
        }
    }
}
