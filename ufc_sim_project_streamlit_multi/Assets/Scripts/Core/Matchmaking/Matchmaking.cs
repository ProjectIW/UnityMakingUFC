using System;
using System.Collections.Generic;
using UFC.Core.Models;

namespace UFC.Core.Matchmaking
{
    public static class Matchmaking
    {
        public static (int, int) PairKey(int aId, int bId)
        {
            return aId < bId ? (aId, bId) : (bId, aId);
        }

        public static float ScorePair(
            Fighter fa,
            Fighter fb,
            bool isTitleFight,
            DateTime? pairLastFought,
            DateTime eventDate,
            int rematchCooldownDays = 210)
        {
            if (pairLastFought.HasValue && (eventDate - pairLastFought.Value).Days < rematchCooldownDays)
            {
                return -9999f;
            }

            float ra = fa.Rating;
            float rb = fb.Rating;
            float score = 1000f - Math.Abs(ra - rb);

            if (!string.IsNullOrWhiteSpace(fa.RankSlot) && !string.IsNullOrWhiteSpace(fb.RankSlot))
            {
                int a = int.Parse(fa.RankSlot);
                int b = int.Parse(fb.RankSlot);
                score += 120f - 20f * Math.Abs(a - b);
                if (a <= 5 && b <= 5)
                {
                    score += 40f;
                }
            }

            score += 10f * ClampInt(fa.Streak, -3, 5);
            score += 10f * ClampInt(fb.Streak, -3, 5);

            if (isTitleFight)
            {
                score *= 2f;
            }

            return score;
        }

        public static Fighter PickBestOpponent(
            Fighter a,
            List<Fighter> candidates,
            HashSet<int> usedIds,
            Dictionary<(int, int), DateTime> pairLastFoughtMap,
            DateTime eventDate,
            bool isTitleFight)
        {
            Fighter best = null;
            float bestScore = float.MinValue;
            int aId = a.Id;
            foreach (var b in candidates)
            {
                int bId = b.Id;
                if (bId == aId || usedIds.Contains(bId))
                {
                    continue;
                }
                pairLastFoughtMap.TryGetValue(PairKey(aId, bId), out var last);
                float score = ScorePair(a, b, isTitleFight, last == default ? (DateTime?)null : last, eventDate);
                if (score > bestScore)
                {
                    bestScore = score;
                    best = b;
                }
            }
            return best;
        }

        private static int ClampInt(int x, int lo, int hi)
        {
            return Math.Max(lo, Math.Min(hi, x));
        }
    }
}
