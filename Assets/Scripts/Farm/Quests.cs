using System;
using System.Collections.Generic;

namespace MuhDefense
{
    /// <summary>Zähler für Aufgaben und Erfolge.</summary>
    public static class Counter
    {
        public const string Kills = "kills";
        public const string BossKills = "boss_kills";
        public const string LevelsWon = "levels_won";
        public const string PerfectWins = "perfect_wins";
        public const string HardWins = "hard_wins";
        public const string TowersBuilt = "towers_built";
        public const string Upgrades = "upgrades";
        public const string Collected = "collected";
        public const string FarmBuilds = "farm_builds";
    }

    public enum RewardKind
    {
        Gems,
        Corn,
        Milk,
    }

    /// <summary>Vorlage für eine tägliche Aufgabe.</summary>
    public class DailyQuestDef
    {
        public string Id;
        public string Text;
        public string Counter;
        public int Target;
        public RewardKind Reward;
        public int Amount;
    }

    /// <summary>Ein Erfolg mit Abzeichen. Zählt entweder einen Zähler hoch oder liest einen Wert aus dem Spielstand.</summary>
    public class AchievementDef
    {
        public string Id;
        public string Name;
        public string Description;
        public string Counter;
        public Func<FarmState, int> Value;
        public int Target;
        public int Gems;
        public string Icon;
    }

    /// <summary>Tägliche Aufgaben (jeden Tag 3 neue) und Erfolge (einmalig, mit Diamanten-Belohnung).</summary>
    public static class Quests
    {
        public const int DailyCount = 3;

        public static readonly DailyQuestDef[] DailyPool =
        {
            new DailyQuestDef { Id = "d_kills", Text = "Besiege 150 Aliens", Counter = Counter.Kills, Target = 150, Reward = RewardKind.Gems, Amount = 10 },
            new DailyQuestDef { Id = "d_boss", Text = "Besiege 2 Bosse", Counter = Counter.BossKills, Target = 2, Reward = RewardKind.Gems, Amount = 15 },
            new DailyQuestDef { Id = "d_wins", Text = "Gewinne 2 Levels", Counter = Counter.LevelsWon, Target = 2, Reward = RewardKind.Corn, Amount = 600 },
            new DailyQuestDef { Id = "d_perfect", Text = "Gewinne ein Level ohne Kuhverlust", Counter = Counter.PerfectWins, Target = 1, Reward = RewardKind.Gems, Amount = 10 },
            new DailyQuestDef { Id = "d_towers", Text = "Baue 15 Türme", Counter = Counter.TowersBuilt, Target = 15, Reward = RewardKind.Milk, Amount = 500 },
            new DailyQuestDef { Id = "d_upgrades", Text = "Kaufe 10 Turm-Upgrades", Counter = Counter.Upgrades, Target = 10, Reward = RewardKind.Corn, Amount = 500 },
            new DailyQuestDef { Id = "d_collect", Text = "Sammle 800 Mais oder Milch auf dem Hof", Counter = Counter.Collected, Target = 800, Reward = RewardKind.Gems, Amount = 8 },
            new DailyQuestDef { Id = "d_build", Text = "Starte 2 Bauarbeiten auf dem Hof", Counter = Counter.FarmBuilds, Target = 2, Reward = RewardKind.Milk, Amount = 400 },
            new DailyQuestDef { Id = "d_hard", Text = "Gewinne ein Level auf „Schwer“", Counter = Counter.HardWins, Target = 1, Reward = RewardKind.Gems, Amount = 15 },
        };

        public static readonly AchievementDef[] Achievements =
        {
            new AchievementDef { Id = "a_firstwin", Name = "Erster Sieg", Description = "Gewinne dein erstes Level.", Counter = Counter.LevelsWon, Target = 1, Gems = 10, Icon = "Kuh" },
            new AchievementDef { Id = "a_kills1", Name = "Alien-Jäger", Description = "Besiege 500 Aliens.", Counter = Counter.Kills, Target = 500, Gems = 15, Icon = "Gruenling" },
            new AchievementDef { Id = "a_kills2", Name = "Alien-Schreck", Description = "Besiege 5000 Aliens.", Counter = Counter.Kills, Target = 5000, Gems = 40, Icon = "Brocken" },
            new AchievementDef { Id = "a_boss", Name = "Bossbezwinger", Description = "Besiege 5 Bosse.", Counter = Counter.BossKills, Target = 5, Gems = 20, Icon = "Boss" },
            new AchievementDef { Id = "a_perfect", Name = "Keine Kuh verloren", Description = "Gewinne 5 Levels ohne Kuhverlust.", Counter = Counter.PerfectWins, Target = 5, Gems = 20, Icon = "Milchflasche" },
            new AchievementDef { Id = "a_towers", Name = "Turmbauer", Description = "Baue 100 Türme.", Counter = Counter.TowersBuilt, Target = 100, Gems = 15, Icon = "MistgabelBauer" },
            new AchievementDef { Id = "a_upgrades", Name = "Tüftler", Description = "Kaufe 200 Turm-Upgrades.", Counter = Counter.Upgrades, Target = 200, Gems = 20, Icon = "TraktorTesla" },
            new AchievementDef { Id = "a_collect", Name = "Fleißiger Bauer", Description = "Sammle 10 000 Mais und Milch auf dem Hof.", Counter = Counter.Collected, Target = 10000, Gems = 20, Icon = "Maiskolben" },
            new AchievementDef { Id = "a_region1", Name = "Held vom Heimattal", Description = "Hol alle 15 Sterne im Heimattal.", Value = f => RegionStars(f, 0), Target = 15, Gems = 30, Icon = "KuhUfo" },
            new AchievementDef { Id = "a_region2", Name = "Herr des Maisfelds", Description = "Hol alle 15 Sterne im Maisfeld-Labyrinth.", Value = f => RegionStars(f, 1), Target = 15, Gems = 30, Icon = "ErnteRoboter" },
            new AchievementDef { Id = "a_region3", Name = "Retter der Kühe", Description = "Hol alle 15 Sterne an der Absturzstelle.", Value = f => RegionStars(f, 2), Target = 15, Gems = 40, Icon = "AlienKaiser" },
            new AchievementDef { Id = "a_emperor", Name = "Kaiser-Bezwinger", Description = "Besiege Alien-Kaiser Zorbax.", Value = f => f.BestStars("absturz-5") > 0 ? 1 : 0, Target = 1, Gems = 50, Icon = "AlienKaiser" },
            new AchievementDef { Id = "a_crowns", Name = "Kronenträger", Description = "Schaffe 5 Levels auf „Schwer“.", Value = f => f.CrownCount, Target = 5, Gems = 30, Icon = "Diamant" },
            new AchievementDef { Id = "a_stars", Name = "Sternensammler", Description = "Sammle 40 Sterne.", Value = f => f.TotalStars, Target = 40, Gems = 30, Icon = "Maiskolben" },
            new AchievementDef { Id = "a_farmhouse", Name = "Großbauer", Description = "Verbessere das Bauernhaus auf Stufe 5.", Value = f => f.FarmhouseLevel, Target = 5, Gems = 30, Icon = "Bauernhaus" },
        };

        static int RegionStars(FarmState f, int region)
        {
            int n = 0;
            foreach (var def in Levels.All)
            {
                if (def.Region == region) n += f.BestStars(def.Id);
            }
            return n;
        }

        public static DailyQuestDef GetDaily(string id)
        {
            foreach (var q in DailyPool)
            {
                if (q.Id == id) return q;
            }
            return null;
        }

        /// <summary>Heutiger Tag (lokale Zeit) als Zahl, wechselt um Mitternacht.</summary>
        public static int Today => (int)(DateTime.Now.Date - new DateTime(2024, 1, 1)).TotalDays;

        /// <summary>Wählt 3 verschiedene Aufgaben für einen Tag (jeder Tag hat immer dieselben).</summary>
        public static List<string> PickForDay(int day)
        {
            var rng = new Random(day * 7919);
            var ids = new List<string>();
            foreach (var q in DailyPool) ids.Add(q.Id);
            var picked = new List<string>();
            while (picked.Count < DailyCount && ids.Count > 0)
            {
                int i = rng.Next(ids.Count);
                picked.Add(ids[i]);
                ids.RemoveAt(i);
            }
            return picked;
        }

        public static string RewardText(RewardKind kind, int amount)
        {
            switch (kind)
            {
                case RewardKind.Corn: return $"{amount} Mais";
                case RewardKind.Milk: return $"{amount} Milch";
                default: return $"{amount} Diamanten";
            }
        }
    }
}
