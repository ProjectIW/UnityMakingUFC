using System.Globalization;
using System.Linq;
using UFC.Core.Models;
using UFC.Infrastructure.Data;
using UFC.UI.Theme;
using UFC.UI.Widgets;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Serialization;

namespace UFC.UI.Screens
{
    [ExecuteAlways]
    public class RankingScreen : MonoBehaviour
    {
        public Transform ListRoot;
        [FormerlySerializedAs("EntryPrefab")]
        public RankingEntryWidget RankingEntryPrefab;

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
            UiTheme.Initialize();

            if (state == null || ListRoot == null)
            {
                return;
            }

            ConfigureListRoot();
            ClearList();

            foreach (var division in state.FightersByDivision.Keys.OrderBy(d => d))
            {
                AddHeader(division.ToUpperInvariant());

                var fighters = state.FightersByDivision[division];
                var champ = fighters.FirstOrDefault(f => f.IsChamp == 1);
                if (champ != null)
                {
                    AddEntry(champ, "C");
                }

                var ranked = fighters
                    .Where(f => f.IsChamp != 1 && !string.IsNullOrWhiteSpace(f.RankSlot))
                    .OrderBy(f => SafeRank(f.RankSlot))
                    .Take(15);

                foreach (var fighter in ranked)
                {
                    AddEntry(fighter, $"#{fighter.RankSlot}");
                }
            }
        }

        private void ConfigureListRoot()
        {
            UiTheme.EnsureListLayout(ListRoot);
        }

        private void AddHeader(string title)
        {
            var headerObject = UiTheme.CreateText(ListRoot, title, 18, UiTheme.TextMuted, true);
            headerObject.name = $"{title}_Header";
            var headerText = headerObject.GetComponent<Text>();
            headerText.alignment = TextAnchor.MiddleLeft;
            UiTheme.ApplyTextStyle(headerText, true, true);
        }

        private void AddEntry(Fighter fighter, string rank)
        {
            if (ListRoot == null)
            {
                return;
            }

            RankingEntryWidget widget;
            if (RankingEntryPrefab != null)
            {
                widget = Instantiate(RankingEntryPrefab, ListRoot);
                UiTheme.ApplyLayerFromParent(widget.gameObject, ListRoot);
            }
            else
            {
                var entryObject = new GameObject("RankingEntry", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                entryObject.transform.SetParent(ListRoot, false);
                UiTheme.ApplyLayerFromParent(entryObject, ListRoot);
                var background = entryObject.GetComponent<Image>();
                background.color = UiTheme.PanelElevated;
                background.sprite = UiTheme.RoundedSquare;
                background.type = UiTheme.RoundedSquare != null ? Image.Type.Sliced : Image.Type.Simple;
                widget = entryObject.AddComponent<RankingEntryWidget>();
            }

            widget.Bind(fighter, rank);
        }

        private static int SafeRank(string rankSlot)
        {
            if (string.IsNullOrWhiteSpace(rankSlot))
            {
                return int.MaxValue;
            }

            if (int.TryParse(rankSlot, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
            {
                return parsed;
            }

            if (float.TryParse(rankSlot, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsedFloat))
            {
                return Mathf.RoundToInt(parsedFloat);
            }

            return int.MaxValue;
        }

        private void ClearList()
        {
            for (int i = ListRoot.childCount - 1; i >= 0; i--)
            {
                DestroyItem(ListRoot.GetChild(i).gameObject);
            }
        }

        private void RenderPreview()
        {
            UiTheme.Initialize();

            if (ListRoot == null)
            {
                return;
            }

            ConfigureListRoot();
            ClearList();

            AddHeader("LIGHTWEIGHT");
            AddEntry(PreviewFighter("C", "Islam Makhachev", 32, 25, 1, 0, 1985f), "C");
            AddEntry(PreviewFighter("#1", "Charles Oliveira", 34, 34, 10, 0, 1892f), "#1");
            AddEntry(PreviewFighter("#2", "Dustin Poirier", 35, 30, 8, 0, 1870f), "#2");

            AddHeader("MIDDLEWEIGHT");
            AddEntry(PreviewFighter("C", "Dricus Du Plessis", 30, 22, 2, 0, 1934f), "C");
            AddEntry(PreviewFighter("#1", "Israel Adesanya", 34, 24, 3, 0, 1902f), "#1");
            AddEntry(PreviewFighter("#2", "Robert Whittaker", 33, 26, 7, 0, 1858f), "#2");

            AddHeader("HEAVYWEIGHT");
            AddEntry(PreviewFighter("C", "Jon Jones", 36, 27, 1, 0, 2012f), "C");
            AddEntry(PreviewFighter("#1", "Ciryl Gane", 33, 12, 2, 0, 1887f), "#1");
            AddEntry(PreviewFighter("#2", "Tom Aspinall", 31, 13, 3, 0, 1860f), "#2");
        }

        private static Fighter PreviewFighter(string rank, string name, int age, int wins, int losses, int draws, float rating)
        {
            return new Fighter
            {
                Name = name,
                Age = age,
                Wins = wins,
                Losses = losses,
                Draws = draws,
                Rating = rating,
                IsChamp = rank == "C" ? 1 : 0
            };
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
