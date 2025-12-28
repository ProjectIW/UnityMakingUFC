namespace UFC.Core.Game
{
    public static class NewsService
    {
        public static string WithdrawalMsg(string name)
        {
            return $"⚠️ Снятие: {name} выбыл из боя (травма/болезнь).";
        }

        public static string ReplacementMsg(string outName, string inName)
        {
            return $"🔁 Замена: вместо {outName} выходит {inName}.";
        }

        public static string CancelledMsg(string a, string b)
        {
            return $"❌ Бой отменён: {a} vs {b} (не найден заменяющий).";
        }

        public static string InjuryMsg(string name, int extraDays)
        {
            int weeks = System.Math.Max(1, extraDays / 7);
            return $"🩼 Травма: {name} выбыл минимум на {weeks} нед.";
        }

        public static string ResultMsg(string winner, string loser)
        {
            return $"✅ Результат: {winner} победил {loser}.";
        }

        public static string TitleChangeMsg(string newChamp)
        {
            return $"🏆 Новый чемпион: {newChamp}!";
        }
    }
}
