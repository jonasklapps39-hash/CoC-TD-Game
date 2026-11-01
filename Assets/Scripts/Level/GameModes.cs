using System.Collections.Generic;
using UnityEngine;

namespace MuhDefense
{
    public enum LevelMode
    {
        Normal,
        /// <summary>Tägliche Herausforderung: ein Level mit Sonderregeln, jeden Tag neu.</summary>
        Challenge,
        /// <summary>Endlos: Wellen ohne Ende, Rekord pro Karte.</summary>
        Endless,
        /// <summary>Alien-Überfall auf den eigenen Hof.</summary>
        Raid,
    }

    /// <summary>Sonderregeln der täglichen Herausforderung.</summary>
    public class ChallengeRules
    {
        public readonly List<string> Lines = new List<string>();
        public float HpFactor = 1f;
        public float SpeedFactor = 1f;
        public float MoneyFactor = 1f;
        public bool NoHero;
        public int MaxTier = 5;
        public int CowOverride;
        public bool AllCamo;
        /// <summary>Nur Türme dieser Werkstatt (null = alle).</summary>
        public FarmBuildingType OnlyWorkshop;

        public bool Allows(TowerType t)
        {
            if (OnlyWorkshop == null) return true;
            foreach (var u in OnlyWorkshop.Unlocks)
            {
                if (u.Tower == t) return true;
            }
            return false;
        }
    }

    /// <summary>Die tägliche Herausforderung und der Endlos-Modus.</summary>
    public static class Challenges
    {
        public const int GemsReward = 25;

        static readonly FarmBuildingType[] CategoryWorkshops = { FarmBuildingType.Schmiede, FarmBuildingType.Zimmerei, FarmBuildingType.Tueftlerschuppen };
        static readonly string[] CategoryNames = { "Basis-Türme", "Explosiv-Türme", "Technik-Türme" };

        /// <summary>Herausforderung für heute: gleiche für den ganzen Tag, aus den schon freigeschalteten Levels.</summary>
        public static (LevelDefinition def, ChallengeRules rules) Today(FarmState farm)
        {
            int day = Quests.Today;
            var rng = new System.Random(day * 104729 + 17);

            var open = new List<LevelDefinition>();
            for (int i = 0; i < Levels.All.Length; i++)
            {
                if (GameFlow.IsUnlocked(i)) open.Add(Levels.All[i]);
            }
            var def = open[rng.Next(open.Count)];

            var rules = new ChallengeRules();
            // 2 Regeln aus dem Topf, nicht doppelt
            var pool = new List<int> { 0, 1, 2, 3, 4, 5, 6, 7 };
            for (int n = 0; n < 2 && pool.Count > 0; n++)
            {
                int pick = pool[rng.Next(pool.Count)];
                pool.Remove(pick);
                switch (pick)
                {
                    case 0:
                    {
                        // nur eine Turm-Kategorie, die man auch hat
                        var usable = new List<int>();
                        for (int c = 0; c < CategoryWorkshops.Length; c++)
                        {
                            foreach (var u in CategoryWorkshops[c].Unlocks)
                            {
                                if (farm.TowerMaxLevel(u.Tower) > 0) { usable.Add(c); break; }
                            }
                        }
                        if (usable.Count == 0) { n--; continue; }
                        int cat = usable[rng.Next(usable.Count)];
                        rules.OnlyWorkshop = CategoryWorkshops[cat];
                        rules.Lines.Add($"Nur {CategoryNames[cat]}");
                        break;
                    }
                    case 1:
                        rules.NoHero = true;
                        rules.Lines.Add("Kein Held");
                        break;
                    case 2:
                        rules.SpeedFactor = 1.4f;
                        rules.Lines.Add("Aliens sind 40 % schneller");
                        break;
                    case 3:
                        rules.HpFactor = 1.5f;
                        rules.Lines.Add("Aliens haben 50 % mehr Leben");
                        break;
                    case 4:
                        rules.MoneyFactor = 0.6f;
                        rules.Lines.Add("Nur 60 % Start-Münzen");
                        break;
                    case 5:
                        rules.MaxTier = 3;
                        rules.Lines.Add("Upgrades höchstens bis Stufe 3");
                        break;
                    case 6:
                        rules.CowOverride = 4;
                        rules.Lines.Add("Nur 4 Kühe auf der Weide");
                        break;
                    case 7:
                        rules.AllCamo = true;
                        rules.Lines.Add("Alle Aliens sind getarnt");
                        break;
                }
            }
            return (def, rules);
        }

        public static bool DoneToday(FarmState farm) => farm.Data.ChallengeDay == Quests.Today;

        // ---------------------------------------------------------------- Endlos

        /// <summary>Welle Nummer <paramref name="index"/> (0 = erste) im Endlos-Modus: erst die Wellen des Levels, danach immer härtere Zufallswellen.</summary>
        public static WaveDefinition EndlessWave(LevelDefinition def, int index)
        {
            if (index < def.Waves.Length) return def.Waves[index];
            int n = index - def.Waves.Length + 1;
            var rng = new System.Random(def.Id.GetHashCode() + index * 7919);
            var groups = new List<SpawnGroup>();
            EnemyKind[] basic = { EnemyKind.Gruenling, EnemyKind.Flitzer, EnemyKind.Brocken, EnemyKind.Schleicher, EnemyKind.Ufo,
                                  EnemyKind.Sanitaeter, EnemyKind.Teiler, EnemyKind.SchildAlien };
            int groupCount = 3 + Mathf.Min(4, n / 3);
            for (int g = 0; g < groupCount; g++)
            {
                var kind = basic[rng.Next(basic.Length)];
                int count = 8 + n * 2 + rng.Next(6);
                float interval = Mathf.Max(0.15f, 0.9f - n * 0.03f);
                groups.Add(new SpawnGroup(kind, count, interval, g * 2.5f));
            }
            // alle 5 Wellen ein Boss, später mehrere
            if (n % 5 == 0)
            {
                EnemyKind[] bosses = { EnemyKind.Boss, EnemyKind.ErnteRoboter, EnemyKind.Kaiser };
                var boss = bosses[Mathf.Min(bosses.Length - 1, (n / 5 - 1) % 3)];
                groups.Add(new SpawnGroup(boss, 1 + n / 15, 6f, 10f));
            }
            return new WaveDefinition(80 + 10 * n, groups.ToArray());
        }

        public static int EndlessBest(FarmState farm, string levelId)
        {
            var e = farm.Data.EndlessRecords.Find(r => r.Id == levelId);
            return e != null ? e.Waves : 0;
        }
    }
}
