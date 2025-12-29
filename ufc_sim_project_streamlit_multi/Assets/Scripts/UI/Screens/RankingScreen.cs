using System.Linq;
using UFC.Core.Models;
using UFC.Infrastructure.Data;
using UFC.UI.Theme;
using UFC.UI.Widgets;
using UnityEngine;
using UnityEngine.UI;

namespace UFC.UI.Screens
{
    public class RankingScreen : MonoBehaviour
    {
        public Transform ListRoot;

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
            var rect = ListRoot as RectTransform;
            if (rect != null)
            {
                rect.anchorMin = new Vector2(0f, 1f);
                rect.anchorMax = new Vector2(1f, 1f);
                rect.pivot = new Vector2(0.5f, 1f);
            }

            var layout = ListRoot.GetComponent<VerticalLayoutGroup>();
            if (layout == null)
            {
                layout = ListRoot.gameObject.AddComponent<VerticalLayoutGroup>();
            }

            layout.spacing = 10f;
            layout.padding = new RectOffset(20, 20, 20, 20);
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;

            var fitter = ListRoot.GetComponent<ContentSizeFitter>();
            if (fitter == null)
            {
                fitter = ListRoot.gameObject.AddComponent<ContentSizeFitter>();
            }

            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }

        private void AddHeader(string title)
        {
            var headerObject = UiTheme.CreateText(ListRoot, title, 16, UiTheme.TextMuted, true);
            headerObject.name = $"{title}_Header";
            var headerText = headerObject.GetComponent<Text>();
            headerText.alignment = TextAnchor.MiddleLeft;
        }

        private void AddEntry(Fighter fighter, string rank)
        {
            var entryObject = new GameObject("RankingEntry", typeof(RectTransform));
            entryObject.transform.SetParent(ListRoot, false);
            entryObject.layer = ListRoot.gameObject.layer;
            var widget = entryObject.AddComponent<RankingEntryWidget>();
            widget.Bind(fighter, rank);
        }

        private static int SafeRank(string rankSlot)
        {
            return int.TryParse(rankSlot, out var parsed) ? parsed : int.MaxValue;
        }

        private void ClearList()
        {
            for (int i = ListRoot.childCount - 1; i >= 0; i--)
            {
                Destroy(ListRoot.GetChild(i).gameObject);
            }
        }
    }
}
