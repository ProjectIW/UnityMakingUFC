using System;

namespace UFC.Core.Math
{
    public static class Formulas
    {
        public static float Clamp(float x, float lo, float hi)
        {
            return System.Math.Max(lo, System.Math.Min(hi, x));
        }

        public static float AgeBonus(int age)
        {
            return System.Math.Max(-40f, 20f - 4f * System.Math.Abs(age - 30));
        }

        public static float StreakBonus(int streak)
        {
            var s = (int)Clamp(streak, -3, 5);
            return s * 10f;
        }

        public static float EloProb(float ra, float rb)
        {
            return 1f / (1f + (float)System.Math.Pow(10f, (rb - ra) / 400f));
        }

        public static float EffectiveRating(float baseRating, int age, int streak, float sigma, System.Random rng)
        {
            return baseRating + AgeBonus(age) + StreakBonus(streak) + NextGaussian(rng, 0f, sigma);
        }

        private static float RankFactor(int? rankA, int? rankB)
        {
            if (!rankA.HasValue && !rankB.HasValue)
            {
                return 0f;
            }
            float gap = (!rankA.HasValue || !rankB.HasValue) ? 12f : System.Math.Abs(rankA.Value - rankB.Value);
            return System.Math.Min(gap / 15f, 1f) * 0.35f;
        }

        private static float MismatchFactor(float ra, float rb)
        {
            float diff = System.Math.Abs(ra - rb);
            return 0.65f + System.Math.Min(diff / 350f, 1f) * 0.75f;
        }

        private static float UpsetBonus(float expected)
        {
            return System.Math.Max(0f, 0.5f - expected) * 1.6f;
        }

        public static (float, float) ApplyElo(float ra, float rb, bool winnerA, float k = 24f, int? rankA = null, int? rankB = null)
        {
            float pa = EloProb(ra, rb);
            float sa = winnerA ? 1f : 0f;
            float mult = MismatchFactor(ra, rb) + RankFactor(rankA, rankB) + UpsetBonus(winnerA ? pa : (1f - pa));
            mult = Clamp(mult, 0.55f, 2.2f);
            float kEff = k * mult;
            float ra2 = ra + kEff * (sa - pa);
            float rb2 = rb + kEff * ((1f - sa) - (1f - pa));
            return (ra2, rb2);
        }

        private static float NextGaussian(System.Random rng, float mean, float stdDev)
        {
            double u1 = 1.0 - rng.NextDouble();
            double u2 = 1.0 - rng.NextDouble();
            double randStdNormal = System.Math.Sqrt(-2.0 * System.Math.Log(u1)) *
                                   System.Math.Sin(2.0 * System.Math.PI * u2);
            return (float)(mean + stdDev * randStdNormal);
        }
    }
}
