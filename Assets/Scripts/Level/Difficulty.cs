namespace MuhDefense
{
    public enum Difficulty
    {
        Leicht,
        Mittel,
        Schwer,
    }

    /// <summary>Schwierigkeitsgrade wie in Bloons TD6: dasselbe Level, andere Werte und Belohnungen.</summary>
    public static class DifficultyRules
    {
        public static string Name(Difficulty d) => d.ToString();

        /// <summary>Faktor für das Leben der Aliens.</summary>
        public static float HpFactor(Difficulty d) => d == Difficulty.Leicht ? 0.75f : d == Difficulty.Schwer ? 1.35f : 1f;

        /// <summary>Faktor für das Lauftempo der Aliens.</summary>
        public static float SpeedFactor(Difficulty d) => d == Difficulty.Schwer ? 1.1f : 1f;

        public static float StartMoneyFactor(Difficulty d) => d == Difficulty.Leicht ? 1.2f : d == Difficulty.Schwer ? 0.9f : 1f;

        /// <summary>Faktor für Mais und Milch als Belohnung.</summary>
        public static float RewardFactor(Difficulty d) => d == Difficulty.Leicht ? 0.7f : d == Difficulty.Schwer ? 1.5f : 1f;

        /// <summary>Auf „Leicht“ gibt es höchstens 2 Sterne.</summary>
        public static int MaxStars(Difficulty d) => d == Difficulty.Leicht ? 2 : 3;

        /// <summary>Diamanten, wenn man das Level zum ersten Mal auf „Schwer“ schafft.</summary>
        public const int HardFirstClearGems = 15;

        public static string Description(Difficulty d)
        {
            switch (d)
            {
                case Difficulty.Leicht: return "Schwächere Aliens, mehr Start-Münzen. Weniger Belohnung, höchstens 2 Sterne.";
                case Difficulty.Schwer: return "Stärkere, schnellere Aliens. 50 % mehr Belohnung und eine Krone (+1 Stern, Diamanten).";
                default: return "Das normale Spiel.";
            }
        }
    }
}
