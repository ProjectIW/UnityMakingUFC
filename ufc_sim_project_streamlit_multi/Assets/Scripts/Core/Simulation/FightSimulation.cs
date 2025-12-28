using System;
using UFC.Core.Math;
using UFC.Core.Models;

namespace UFC.Core.Simulation
{
    public static class FightSimulation
    {
        public static bool ChooseWinner(float ratingAEff, float ratingBEff, System.Random rng)
        {
            float p = Formulas.EloProb(ratingAEff, ratingBEff);
            return rng.NextDouble() < p;
        }

        public static (DateTime nextDate, int extraDays) AfterFightAvailability(DateTime eventDate, SimConfig cfg, System.Random rng)
        {
            var baseDate = eventDate.AddDays(cfg.RestDays);
            if (rng.NextDouble() < cfg.InjuryAfterFightChance)
            {
                int extra = rng.Next(cfg.InjuryExtraMin, cfg.InjuryExtraMax + 1);
                return (baseDate.AddDays(extra), extra);
            }
            return (baseDate, 0);
        }

        public static FightResult SimulateFight(Fighter fa, Fighter fb, DateTime eventDate, SimConfig cfg, System.Random rng)
        {
            float raEff = Formulas.EffectiveRating(fa.Rating, fa.Age, fa.Streak, cfg.Sigma, rng);
            float rbEff = Formulas.EffectiveRating(fb.Rating, fb.Age, fb.Streak, cfg.Sigma, rng);
            bool aWins = ChooseWinner(raEff, rbEff, rng);

            int winnerId = aWins ? fa.Id : fb.Id;
            int loserId = aWins ? fb.Id : fa.Id;

            int? RankValue(Fighter f)
            {
                if (f.IsChamp == 1)
                {
                    return 0;
                }
                if (string.IsNullOrWhiteSpace(f.RankSlot))
                {
                    return null;
                }
                return int.TryParse(f.RankSlot, out var parsed) ? parsed : (int?)null;
            }

            var (raNew, rbNew) = Formulas.ApplyElo(fa.Rating, fb.Rating, aWins, cfg.K, RankValue(fa), RankValue(fb));
            var (nextA, injA) = AfterFightAvailability(eventDate, cfg, rng);
            var (nextB, injB) = AfterFightAvailability(eventDate, cfg, rng);

            return new FightResult
            {
                WinnerId = winnerId,
                LoserId = loserId,
                RaNew = raNew,
                RbNew = rbNew,
                NextA = nextA,
                NextB = nextB,
                InjAExtra = injA,
                InjBExtra = injB
            };
        }

        public static (string method, int round, string timeMmss) RandomMethodAndTime(System.Random rng)
        {
            double r = rng.NextDouble();
            if (r < 0.52)
            {
                string[] methods = { "U-DEC", "S-DEC", "M-DEC" };
                int round = 3;
                int sec = rng.Next(10, 301);
                return (methods[rng.Next(methods.Length)], round, FormatMmss(sec));
            }
            if (r < 0.82)
            {
                string[] methods =
                {
                    "KO (head kick)",
                    "TKO (punches)",
                    "TKO (ground and pound)",
                    "TKO (doctor stoppage)"
                };
                int[] rounds = { 1, 1, 2, 2, 3 };
                int round = rounds[rng.Next(rounds.Length)];
                int sec = rng.Next(10, round < 3 ? 291 : 301);
                return (methods[rng.Next(methods.Length)], round, FormatMmss(sec));
            }
            else
            {
                string[] methods = { "SUB (RNC)", "SUB (Armbar)", "SUB (Guillotine)", "SUB (Triangle)", "SUB (Kimura)" };
                int[] rounds = { 1, 2, 2, 3 };
                int round = rounds[rng.Next(rounds.Length)];
                int sec = rng.Next(10, round < 3 ? 291 : 301);
                return (methods[rng.Next(methods.Length)], round, FormatMmss(sec));
            }
        }

        private static string FormatMmss(int sec)
        {
            int mm = sec / 60;
            int ss = sec % 60;
            return $"{mm:00}:{ss:00}";
        }
    }

    public class FightResult
    {
        public int WinnerId;
        public int LoserId;
        public float RaNew;
        public float RbNew;
        public DateTime NextA;
        public DateTime NextB;
        public int InjAExtra;
        public int InjBExtra;
    }
}
