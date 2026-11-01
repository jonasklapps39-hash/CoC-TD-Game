using UnityEngine;

namespace MuhDefense
{
    /// <summary>Aussehen eines Helden (Färbung des Modells), mit Diamanten kaufbar.</summary>
    public class HeroSkin
    {
        public string Id;
        public HeroKind Hero;
        public string Name;
        public int Gems;
        public Color Tint;

        public static readonly HeroSkin[] All =
        {
            new HeroSkin { Id = "heinz_cowboy", Hero = HeroKind.Heinz, Name = "Cowboy-Heinz", Gems = 50, Tint = new Color(0.95f, 0.75f, 0.55f) },
            new HeroSkin { Id = "heinz_gold", Hero = HeroKind.Heinz, Name = "Goldener Heinz", Gems = 100, Tint = new Color(1f, 0.84f, 0.35f) },
            new HeroSkin { Id = "gertrud_disco", Hero = HeroKind.Gertrud, Name = "Disco-Oma", Gems = 50, Tint = new Color(1f, 0.6f, 0.9f) },
            new HeroSkin { Id = "gertrud_gold", Hero = HeroKind.Gertrud, Name = "Goldene Oma", Gems = 100, Tint = new Color(1f, 0.84f, 0.35f) },
            new HeroSkin { Id = "berta_schoko", Hero = HeroKind.Berta, Name = "Schoko-Berta", Gems = 50, Tint = new Color(0.78f, 0.55f, 0.38f) },
            new HeroSkin { Id = "berta_gold", Hero = HeroKind.Berta, Name = "Goldene Berta", Gems = 100, Tint = new Color(1f, 0.84f, 0.35f) },
        };

        public static HeroSkin Get(string id)
        {
            foreach (var s in All)
            {
                if (s.Id == id) return s;
            }
            return null;
        }
    }

    /// <summary>Dauerhaftes Training in der Heldenhütte (mit Milch).</summary>
    public static class HeroTraining
    {
        public const int MaxLevel = 5;
        static readonly int[] Costs = { 800, 1600, 3000, 5000, 8000 };

        /// <summary>Milch für die nächste Trainingsstufe (Stufe 1..5).</summary>
        public static int CostFor(int nextLevel) => Costs[Mathf.Clamp(nextLevel - 1, 0, Costs.Length - 1)];

        static readonly int[] EggCosts = { 20, 40, 80, 140, 220 };

        /// <summary>Eier für die nächste Trainingsstufe (aus dem Hühnerstall).</summary>
        public static int EggCostFor(int nextLevel) => EggCosts[Mathf.Clamp(nextLevel - 1, 0, EggCosts.Length - 1)];

        public static float DamageFactor(int training) => 1f + 0.08f * training;
        public static float RangeFactor(int training) => 1f + 0.04f * training;
        public static float CooldownFactor(int training) => 1f - 0.06f * training;

        public static string Describe(int training) =>
            $"+{8 * training} % Schaden, +{4 * training} % Reichweite, Fähigkeiten {6 * training} % schneller";
    }
}
