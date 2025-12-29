using System;
using System.Linq;
using UFC.Core.Game;
using UFC.Core.Models;
using UFC.Infrastructure.Data;
using UFC.UI.Widgets;
using UnityEngine;
using UFC.UI.Theme;
using UnityEngine.UI;

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
        private bool _needsPreviewRefresh;

        private void OnEnable()
        {
            if (!Application.isPlaying)
            {
                SchedulePreview();
            }
        }

        private void OnValidate()
        {
            if (!Application.isPlaying)
            {
                SchedulePreview();
            }
        }

        private void Update()
        {
            if (!Application.isPlaying && _needsPreviewRefresh)
            {
                _needsPreviewRefresh = false;
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
                string subtitle = BuildEventSubtitle(ev);
                var card = Instantiate(EventCardPrefab, EventsListRoot, false);
                UiTheme.ApplyLayerFromParent(card.gameObject, EventsListRoot);
                var captured = ev;
                card.Bind(title, subtitle, () => ShowEvent(captured));
            }

            if (upcoming.Count > 0)
            {
                ShowEvent(upcoming[0]);
            }

            FinalizeLayout(EventsListRoot);
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
                string ratingText = BuildFightRating(fight);
                if (!string.IsNullOrWhiteSpace(ratingText))
                {
                    subtitle = $"{subtitle} · {ratingText}";
                }
                var card = Instantiate(FightCardPrefab, FightListRoot, false);
                UiTheme.ApplyLayerFromParent(card.gameObject, FightListRoot);
                card.Bind(title, subtitle);
            }

            FinalizeLayout(FightListRoot);
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
                new { Title = "12 Aug · UFC Fight Night", Subtitle = "Las Vegas, USA · Avg rating 1820" },
                new { Title = "26 Aug · UFC 300", Subtitle = "New York, USA · Avg rating 1915" },
                new { Title = "09 Sep · UFC International", Subtitle = "Tokyo, Japan · Avg rating 1764" }
            };

            foreach (var ev in previewEvents)
            {
                var card = Instantiate(EventCardPrefab, EventsListRoot, false);
                UiTheme.ApplyLayerFromParent(card.gameObject, EventsListRoot);
                card.Bind(ev.Title, ev.Subtitle, () => ShowPreviewFights());
            }

            ShowPreviewFights();
            FinalizeLayout(EventsListRoot);
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
                new { Title = "Volkanovski vs Topuria", Subtitle = "Featherweight · Title Fight · Avg rating 1934" },
                new { Title = "O'Malley vs Vera", Subtitle = "Bantamweight · Title Fight · Avg rating 1872" },
                new { Title = "Pereira vs Hill", Subtitle = "Light Heavyweight · Avg rating 1810" },
                new { Title = "Shevchenko vs Grasso", Subtitle = "Women's Flyweight · Avg rating 1786" }
            };

            foreach (var fight in previewFights)
            {
                var card = Instantiate(FightCardPrefab, FightListRoot, false);
                UiTheme.ApplyLayerFromParent(card.gameObject, FightListRoot);
                card.Bind(fight.Title, fight.Subtitle);
            }

            FinalizeLayout(FightListRoot);
        }

        private static void FinalizeLayout(Transform root)
        {
            if (root is RectTransform rect)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
            }
            Canvas.ForceUpdateCanvases();
        }

        private string BuildEventSubtitle(EventRow ev)
        {
            string location = $"{ev.Location} {ev.ThemeCountry}".Trim();
            string ratingText = BuildEventRating(ev.EventId);
            if (string.IsNullOrWhiteSpace(ratingText))
            {
                return location;
            }

            if (string.IsNullOrWhiteSpace(location))
            {
                return ratingText;
            }

            return $"{location} · {ratingText}";
        }

        private string BuildEventRating(int eventId)
        {
            if (_state == null)
            {
                return string.Empty;
            }

            var ratings = _state.Fights
                .Where(f => f.EventId == eventId)
                .Select(BuildFightRatingValue)
                .Where(r => r > 0f)
                .ToList();

            if (ratings.Count == 0)
            {
                return "Rating TBD";
            }

            return $"Avg rating {Mathf.RoundToInt(ratings.Average())}";
        }

        private string BuildFightRating(FightRow fight)
        {
            float rating = BuildFightRatingValue(fight);
            if (rating <= 0f)
            {
                return "Rating TBD";
            }

            return $"Avg rating {Mathf.RoundToInt(rating)}";
        }

        private float BuildFightRatingValue(FightRow fight)
        {
            if (_state == null || fight == null)
            {
                return 0f;
            }

            float aRating = GetFighterRating(fight.Division, fight.AId);
            float bRating = GetFighterRating(fight.Division, fight.BId);

            if (aRating <= 0f && bRating <= 0f)
            {
                return 0f;
            }

            if (aRating <= 0f)
            {
                return bRating;
            }

            if (bRating <= 0f)
            {
                return aRating;
            }

            return (aRating + bRating) * 0.5f;
        }

        private float GetFighterRating(string division, int id)
        {
            if (_state == null || string.IsNullOrWhiteSpace(division) || !_state.FightersByDivision.ContainsKey(division))
            {
                return 0f;
            }

            var fighter = _state.FightersByDivision[division].FirstOrDefault(f => f.Id == id);
            return fighter != null ? fighter.Rating : 0f;
        }

        private static void DestroyItem(GameObject item)
        {
            if (item == null)
            {
                return;
            }

            Destroy(item);
        }

        private void SchedulePreview()
        {
            _needsPreviewRefresh = true;
        }
    }
}
