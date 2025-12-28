using System;
using System.Linq;
using UFC.Core.Game;
using UFC.Core.Models;
using UFC.UI.Widgets;
using UnityEngine;

namespace UFC.UI.Screens
{
    public class EventsScreen : MonoBehaviour
    {
        public Transform EventsListRoot;
        public Transform FightListRoot;
        public EventCardWidget EventCardPrefab;
        public FightCardWidget FightCardPrefab;

        private GameState _state;

        public void Refresh(GameState state)
        {
            _state = state;
            if (_state == null || EventsListRoot == null || EventCardPrefab == null)
            {
                return;
            }

            ClearList(EventsListRoot);
            ClearList(FightListRoot);

            var today = DateUtil.ParseDateOrDefault(_state.Save.CurrentDate, new DateTime(2026, 1, 1));
            var upcoming = _state.Events
                .Select(e => new { Event = e, EventDate = DateUtil.ParseDate(e.EventDate) })
                .Where(e => e.Event.Completed == 0 && e.EventDate.HasValue && e.EventDate.Value >= today)
                .OrderBy(e => e.EventDate.Value)
                .Select(e => e.Event)
                .ToList();

            foreach (var ev in upcoming)
            {
                string title = $"{ev.EventDate} · {ev.EventKind}";
                string subtitle = $"{ev.Location} {ev.ThemeCountry}".Trim();
                var card = Instantiate(EventCardPrefab, EventsListRoot);
                var captured = ev;
                card.Bind(title, subtitle, () => ShowEvent(captured));
            }

            if (upcoming.Count > 0)
            {
                ShowEvent(upcoming[0]);
            }
        }

        private void ShowEvent(EventRow ev)
        {
            if (_state == null || FightListRoot == null || FightCardPrefab == null || ev == null)
            {
                return;
            }

            ClearList(FightListRoot);

            var fights = _state.Fights
                .Where(f => f.EventId == ev.EventId)
                .OrderByDescending(f => f.IsMainEvent)
                .ThenBy(f => f.CardSlot)
                .ToList();

            foreach (var fight in fights)
            {
                string aName = FighterName(fight.Division, fight.AId);
                string bName = FighterName(fight.Division, fight.BId);
                string title = $"{aName} vs {bName}";
                string subtitle = fight.IsTitleFight == 1 ? $"{fight.Division} · Title Fight" : fight.Division;
                var card = Instantiate(FightCardPrefab, FightListRoot);
                card.Bind(title, subtitle);
            }
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
