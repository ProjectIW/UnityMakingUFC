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
    public class PastEventsScreen : MonoBehaviour
    {
        public Transform EventsListRoot;
        public Transform ResultsListRoot;
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
                string subtitle = BuildEventSubtitle(ev);
                var card = Instantiate(EventCardPrefab, EventsListRoot, false);
                UiTheme.ApplyLayerFromParent(card.gameObject, EventsListRoot);
                var captured = ev;
                card.Bind(title, subtitle, () => ShowEventResults(captured));
            }

            if (past.Count > 0)
            {
                ShowEventResults(past[0]);
            }

            FinalizeLayout(EventsListRoot);
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
                string ratingText = BuildFightRating(fight);
                if (!string.IsNullOrWhiteSpace(ratingText))
                {
                    result = $"{result} · {ratingText}";
                }
                var card = Instantiate(FightCardPrefab, ResultsListRoot, false);
                UiTheme.ApplyLayerFromParent(card.gameObject, ResultsListRoot);
                card.Bind(title, result);
            }

            FinalizeLayout(ResultsListRoot);
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
                DestroyItem(root.GetChild(i).gameObject);
            }
        }

        private void RenderPreview()
        {
            UiTheme.EnsureInitialized(this);
            UiTheme.EnsureListLayout(EventsListRoot);
            UiTheme.EnsureListLayout(ResultsListRoot);

            if (EventsListRoot == null || ResultsListRoot == null || EventCardPrefab == null || FightCardPrefab == null)
            {
                return;
            }

            ClearList(EventsListRoot);
            ClearList(ResultsListRoot);

            var previewEvents = new[]
            {
                new { Title = "28 Jul · UFC 299", Subtitle = "Miami, USA · Avg rating 1886" },
                new { Title = "13 Jul · UFC Fight Night", Subtitle = "London, UK · Avg rating 1752" },
                new { Title = "22 Jun · UFC 298", Subtitle = "Sydney, AU · Avg rating 1834" }
            };

            foreach (var ev in previewEvents)
            {
                var card = Instantiate(EventCardPrefab, EventsListRoot, false);
                UiTheme.ApplyLayerFromParent(card.gameObject, EventsListRoot);
                card.Bind(ev.Title, ev.Subtitle, () => ShowPreviewResults());
            }

            ShowPreviewResults();
            FinalizeLayout(EventsListRoot);
        }

        private void ShowPreviewResults()
        {
            if (ResultsListRoot == null || FightCardPrefab == null)
            {
                return;
            }

            ClearList(ResultsListRoot);

            var previewResults = new[]
            {
                new { Title = "Adesanya vs Strickland", Subtitle = "Strickland · Decision R5 5:00 · Avg rating 1902" },
                new { Title = "Gaethje vs Holloway", Subtitle = "Holloway · KO R5 4:59 · Avg rating 1864" },
                new { Title = "Weili vs Yan", Subtitle = "Weili · Decision R5 5:00 · Avg rating 1835" },
                new { Title = "Pantoja vs Royval", Subtitle = "Pantoja · Submission R2 3:12 · Avg rating 1804" }
            };

            foreach (var fight in previewResults)
            {
                var card = Instantiate(FightCardPrefab, ResultsListRoot, false);
                UiTheme.ApplyLayerFromParent(card.gameObject, ResultsListRoot);
                card.Bind(fight.Title, fight.Subtitle);
            }

            FinalizeLayout(ResultsListRoot);
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
