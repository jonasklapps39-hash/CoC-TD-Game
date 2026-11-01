using System.Collections.Generic;

namespace MuhDefense
{
    /// <summary>Dauerhafte Boni im Level aus dem Talentbaum „Bauernwissen“.</summary>
    public class LevelBonuses
    {
        public int StartMoney;
        public float TowerCostFactor = 1f;
        public float RewardFactor = 1f;
        public float WaveBonusFactor = 1f;
        public int ExtraCows;
        public float HeroXpFactor = 1f;
        public int HeroStartLevels;
        public float AbilityCooldownFactor = 1f;
        public float MistgabelRange = 1f;
        public float KatapultSplash = 1f;
        public float SlowBonus;
        public float PoisonFactor = 1f;
        public float DamageFactor = 1f;
        public float RangeFactor = 1f;

        /// <summary>Wendet die Boni auf die Werte eines Turms an (nach den Upgrades).</summary>
        public void ApplyTo(TowerType type, TowerStats s)
        {
            if (type == TowerType.Mistgabel) s.Range *= MistgabelRange;
            if (type == TowerType.Heuballen) s.Splash *= KatapultSplash;
            if (s.Slow < 1f) s.Slow -= SlowBonus;
            s.DotDps *= PoisonFactor;
            s.Damage *= DamageFactor;
            s.Range *= RangeFactor;
            s.AbilityCooldown *= AbilityCooldownFactor;
        }
    }

    public class Talent
    {
        public string Id;
        public string Name;
        public string Description;
        public int Cost;
        /// <summary>Spalte im Talentbaum: 0 = Wirtschaft, 1 = Türme, 2 = Held und Herde.</summary>
        public int Column;
        /// <summary>Talent, das vorher gekauft sein muss (null = keins).</summary>
        public string Requires;
        /// <summary>Symbol im Talentbaum.</summary>
        public string Icon;
        public System.Action<LevelBonuses> Apply;
    }

    /// <summary>Talentbaum „Bauernwissen“: Sterne aus den Levels gegen dauerhafte Boni tauschen (siehe Spielkonzept, Kapitel 5).</summary>
    public static class Talents
    {
        public static readonly string[] ColumnNames = { "Wirtschaft", "Türme", "Held und Herde" };

        public static readonly Talent[] All =
        {
            new Talent { Id = "sparschwein", Column = 0, Cost = 1, Name = "Sparschwein", Description = "+60 Münzen zu Beginn jedes Levels.",
                Apply = b => b.StartMoney += 60 },
            new Talent { Id = "feilschen", Column = 0, Cost = 2, Requires = "sparschwein", Name = "Feilschen", Description = "Besiegte Aliens bringen 10 % mehr Münzen.",
                Apply = b => b.RewardFactor *= 1.1f },
            new Talent { Id = "grosseinkauf", Column = 0, Cost = 3, Requires = "feilschen", Name = "Großeinkauf", Description = "Alle Türme kosten 8 % weniger.",
                Apply = b => b.TowerCostFactor *= 0.92f },
            new Talent { Id = "erntedank", Column = 0, Cost = 4, Requires = "grosseinkauf", Name = "Erntedank", Description = "+50 % Wellen-Bonus und noch einmal +150 Start-Münzen.",
                Apply = b => { b.WaveBonusFactor *= 1.5f; b.StartMoney += 150; } },
            new Talent { Id = "goldeneernte", Column = 0, Cost = 5, Requires = "erntedank", Name = "Goldene Ernte", Description = "Aliens bringen 15 % mehr Münzen, Türme 5 % billiger.",
                Apply = b => { b.RewardFactor *= 1.15f; b.TowerCostFactor *= 0.95f; } },

            new Talent { Id = "gabelschliff", Column = 1, Cost = 1, Name = "Gabelschliff", Description = "Mistgabel-Bauern haben 10 % mehr Reichweite.",
                Apply = b => b.MistgabelRange *= 1.1f },
            new Talent { Id = "heuprofi", Column = 1, Cost = 2, Requires = "gabelschliff", Name = "Heu-Profi", Description = "Heuballen treffen eine 20 % größere Fläche.",
                Apply = b => b.KatapultSplash *= 1.2f },
            new Talent { Id = "kuehlkette", Column = 1, Cost = 2, Requires = "heuprofi", Name = "Kühlkette", Description = "Verlangsamung 10 % und Gift 25 % stärker.",
                Apply = b => { b.SlowBonus += 0.1f; b.PoisonFactor *= 1.25f; } },
            new Talent { Id = "meisterhandwerk", Column = 1, Cost = 4, Requires = "kuehlkette", Name = "Meisterhandwerk", Description = "Alle Türme richten 10 % mehr Schaden an.",
                Apply = b => b.DamageFactor *= 1.1f },
            new Talent { Id = "praezision", Column = 1, Cost = 5, Requires = "meisterhandwerk", Name = "Präzision", Description = "Alle Türme haben 10 % mehr Reichweite.",
                Apply = b => b.RangeFactor *= 1.1f },

            new Talent { Id = "herde", Column = 2, Cost = 1, Name = "Große Herde", Description = "2 Kühe mehr auf der Weide.",
                Apply = b => b.ExtraCows += 2 },
            new Talent { Id = "heldenkost", Column = 2, Cost = 2, Requires = "herde", Name = "Heldenkost", Description = "Der Held sammelt 30 % mehr Erfahrung.",
                Apply = b => b.HeroXpFactor *= 1.3f },
            new Talent { Id = "flinkefinger", Column = 2, Cost = 3, Requires = "heldenkost", Name = "Flinke Finger", Description = "Alle Fähigkeiten laden 20 % schneller.",
                Apply = b => b.AbilityCooldownFactor *= 0.8f },
            new Talent { Id = "veteran", Column = 2, Cost = 4, Requires = "flinkefinger", Name = "Veteran", Description = "Der Held startet jedes Level 2 Stufen höher.",
                Apply = b => b.HeroStartLevels += 2 },
            new Talent { Id = "heldenruhm", Column = 2, Cost = 5, Requires = "veteran", Name = "Heldenruhm", Description = "Fähigkeiten laden noch 10 % schneller, 2 Kühe mehr.",
                Apply = b => { b.AbilityCooldownFactor *= 0.9f; b.ExtraCows += 2; } },
        };

        public const int Rows = 5;

        static readonly Dictionary<string, string> icons = new Dictionary<string, string>
        {
            { "sparschwein", "Muenze" }, { "feilschen", "Muenze" }, { "grosseinkauf", "Hofladen" }, { "erntedank", "Maiskolben" }, { "goldeneernte", "Diamant" },
            { "gabelschliff", "MistgabelBauer" }, { "heuprofi", "HeuballenKatapult" }, { "kuehlkette", "EismilchSprueher" },
            { "meisterhandwerk", "Schmiede" }, { "praezision", "Wetterhahn" },
            { "herde", "Kuh" }, { "heldenkost", "BauerHeinz" }, { "flinkefinger", "OmaGertrud" }, { "veteran", "Heldenhuette" }, { "heldenruhm", "Berta" },
        };

        public static string IconFor(Talent t) => icons.TryGetValue(t.Id, out var i) ? i : "Maiskolben";

        /// <summary>Reihe im Talentbaum (0 = oben). Die Bauernschule braucht Stufe Reihe + 1.</summary>
        public static int RowOf(Talent t)
        {
            int row = 0;
            foreach (var other in All)
            {
                if (other == t) return row;
                if (other.Column == t.Column) row++;
            }
            return row;
        }

        public static Talent At(int column, int row)
        {
            int r = 0;
            foreach (var t in All)
            {
                if (t.Column != column) continue;
                if (r == row) return t;
                r++;
            }
            return null;
        }

        public static Talent Get(string id)
        {
            foreach (var t in All)
            {
                if (t.Id == id) return t;
            }
            return null;
        }

        public static LevelBonuses BuildBonuses(IEnumerable<string> owned)
        {
            var bonuses = new LevelBonuses();
            foreach (var id in owned)
            {
                var t = Get(id);
                if (t != null) t.Apply(bonuses);
            }
            return bonuses;
        }
    }
}
