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
    [ExecuteAlways]
    public class EventsScreen : MonoBehaviour
    {
        public Transform EventsListRoot;
        public Transform FightListRoot;
        public EventCardWidget EventCardPrefab;
        public FightCardWidget FightCardPrefab;

        private GameState _state;

        private void OnEnable()
        {
            if (!Application.isPlaying)
            {
                RenderPreview();
            }
        }

        private void OnValidate()
        {
            if (!Application.isPlaying)
            {
                RenderPreview();
            }
        }

        public void Refresh(GameState state)
        {
            UiTheme.EnsureInitialized(this);
            UiTheme.EnsureListLayout(EventsListRoot);
            UiTheme.EnsureListLayout(FightListRoot);

            _state = state;
            if (_state == null || EventsListRoot == null || EventCardPrefab == null)
            {
                return;
            }

            ClearList(EventsListRoot);
            ClearList(FightListRoot);

            var today = DateUtil.ParseDateOrDefault(_state.Save.CurrentDate, new DateTime(2026, 1, 1));
            var datedEvents = _state.Events
                .Select(e => new { Event = e, EventDate = DateUtil.ParseDate(e.EventDate) })
                .Where(e => e.EventDate.HasValue)
                .ToList();

            var upcoming = datedEvents
                .Where(e => e.Event.Completed == 0 && e.EventDate.Value >= today)
                .OrderBy(e => e.EventDate.Value)
                .Select(e => e.Event)
                .ToList();

            if (upcoming.Count == 0)
            {
                upcoming = datedEvents
                    .Where(e => e.Event.Completed == 0)
                    .OrderBy(e => e.EventDate.Value)
                    .Select(e => e.Event)
                    .ToList();
            }

            foreach (var ev in upcoming)
            {
                string title = $"{ev.EventDate} · {ev.EventKind}";
                string subtitle = $"{ev.Location} {ev.ThemeCountry}".Trim();
                var card = Instantiate(EventCardPrefab, EventsListRoot);
                UiTheme.ApplyLayerFromParent(card.gameObject, EventsListRoot);
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
                UiTheme.ApplyLayerFromParent(card.gameObject, FightListRoot);
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
                DestroyItem(root.GetChild(i).gameObject);
            }
        }

        private void RenderPreview()
        {
            UiTheme.EnsureInitialized(this);
            UiTheme.EnsureListLayout(EventsListRoot);
            UiTheme.EnsureListLayout(FightListRoot);

            if (EventsListRoot == null || FightListRoot == null || EventCardPrefab == null || FightCardPrefab == null)
            {
                return;
            }

            ClearList(EventsListRoot);
            ClearList(FightListRoot);

            var previewEvents = new[]
            {
                new { Title = "12 Aug · UFC Fight Night", Subtitle = "Las Vegas, USA" },
                new { Title = "26 Aug · UFC 300", Subtitle = "New York, USA" },
                new { Title = "09 Sep · UFC International", Subtitle = "Tokyo, Japan" }
            };

            foreach (var ev in previewEvents)
            {
                var card = Instantiate(EventCardPrefab, EventsListRoot);
                UiTheme.ApplyLayerFromParent(card.gameObject, EventsListRoot);
                card.Bind(ev.Title, ev.Subtitle, () => ShowPreviewFights());
            }

            ShowPreviewFights();
        }

        private void ShowPreviewFights()
        {
            if (FightListRoot == null || FightCardPrefab == null)
            {
                return;
            }

            ClearList(FightListRoot);

            var previewFights = new[]
            {
                new { Title = "Volkanovski vs Topuria", Subtitle = "Featherweight · Title Fight" },
                new { Title = "O'Malley vs Vera", Subtitle = "Bantamweight · Title Fight" },
                new { Title = "Pereira vs Hill", Subtitle = "Light Heavyweight" },
                new { Title = "Shevchenko vs Grasso", Subtitle = "Women's Flyweight" }
            };

            foreach (var fight in previewFights)
            {
                var card = Instantiate(FightCardPrefab, FightListRoot);
                UiTheme.ApplyLayerFromParent(card.gameObject, FightListRoot);
                card.Bind(fight.Title, fight.Subtitle);
            }
        }

        private static void DestroyItem(GameObject item)
        {
            if (item == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(item);
            }
            else
            {
                DestroyImmediate(item);
            }
        }
    }
}
