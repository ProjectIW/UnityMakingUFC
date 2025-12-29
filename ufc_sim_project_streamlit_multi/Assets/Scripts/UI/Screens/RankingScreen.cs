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
            var entryObject = new GameObject("RankingEntry", typeof(RectTransform));
            entryObject.transform.SetParent(ListRoot, false);
            UiTheme.ApplyLayerFromParent(entryObject, ListRoot);
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
