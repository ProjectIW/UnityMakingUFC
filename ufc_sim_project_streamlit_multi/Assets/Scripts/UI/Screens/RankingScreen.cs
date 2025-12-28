using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UFC.Core.Models;
using UFC.Infrastructure.Data;
using UnityEngine;
using UFC.UI.Widgets;

namespace UFC.UI.Screens
{
    public class RankingScreen : MonoBehaviour
    {
        public Transform ListRoot;
        public RankingEntryWidget EntryPrefab;

        public void Refresh(GameState state)
        {
            if (state == null || ListRoot == null || EntryPrefab == null)
            {
                return;
            }

            ClearList();

            foreach (var division in state.FightersByDivision.Keys.OrderBy(d => d))
            {
                AddEntry(division.ToUpperInvariant(), string.Empty);

                var fighters = state.FightersByDivision[division];
                var champ = fighters.FirstOrDefault(f => f.IsChamp == 1);
                if (champ != null)
                {
                    AddEntry($"C. {champ.Name}", FormatRecord(champ));
                }

                var ranked = fighters
                    .Where(f => f.IsChamp != 1 && !string.IsNullOrWhiteSpace(f.RankSlot))
                    .OrderBy(f => SafeRank(f.RankSlot))
                    .Take(15);

                foreach (var fighter in ranked)
                {
                    AddEntry($"#{fighter.RankSlot} {fighter.Name}", FormatRecord(fighter));
                }
            }
        }

        private void AddEntry(string title, string subtitle)
        {
            var entry = Instantiate(EntryPrefab, ListRoot);
            entry.Bind(title, subtitle);
        }

        private static string FormatRecord(Fighter fighter)
        {
            return $"{fighter.Wins}-{fighter.Losses}-{fighter.Draws} · {fighter.Rating.ToString("F1", CultureInfo.InvariantCulture)}";
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
