using System.Collections.Generic;
using System.Linq;
using UFC.Core.Models;

namespace UFC.Core.Ranking
{
    public static class RankingManager
    {
        public static void RecomputeTop15(List<Fighter> fighters)
        {
            Fighter champ = null;
            var others = new List<Fighter>();
            foreach (var f in fighters)
            {
                if (f.IsChamp == 1)
                {
                    champ = f;
                }
                else
                {
                    others.Add(f);
                }
            }

            others = others
                .OrderByDescending(f => f.Rating)
                .ThenByDescending(f => f.Streak)
                .ToList();

            var top15 = others.Take(15).ToList();
            var rest = others.Skip(15).ToList();

            for (int i = 0; i < top15.Count; i++)
            {
                top15[i].RankSlot = (i + 1).ToString();
                top15[i].RankType = "RANKED";
                top15[i].RankRaw = (i + 1).ToString();
            }

            foreach (var f in rest)
            {
                f.RankSlot = string.Empty;
                f.RankType = "UNRANKED";
                f.RankRaw = "***";
            }

            if (champ != null)
            {
                champ.RankSlot = string.Empty;
                champ.RankType = "CHAMP";
                champ.RankRaw = "Ч";
            }
        }
    }
}
