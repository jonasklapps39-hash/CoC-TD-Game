using System.Collections.Generic;
using UnityEngine;

namespace MuhDefense
{
    /// <summary>Was ein gewonnenes Level dem Hof eingebracht hat.</summary>
    public class LevelReward
    {
        public int Stars;
        public int Corn;
        public int Milk;
        /// <summary>Menge, die nicht ins Lager gepasst hat.</summary>
        public int CornLost;
        public int MilkLost;
        /// <summary>Diamanten für neue Sterne und das erste Schaffen.</summary>
        public int Gems;
        /// <summary>Endlos: neuer Rekord (Wellen).</summary>
        public bool NewRecord;
        public int Waves;
        /// <summary>Zum ersten Mal auf „Schwer“ geschafft.</summary>
        public bool FirstHardClear;
        /// <summary>Dadurch wurde ein neuer Schwierigkeitsgrad für dieses Level frei (Mittel oder Schwer).</summary>
        public string UnlockedDifficulty = "";
    }

    /// <summary>
    /// Wechselt zwischen Hof und Level und hält den Spielstand des Hofs.
    /// Es ist immer genau eines von beiden aktiv (ein Wurzel-Objekt in der Szene).
    /// </summary>
    public static class GameFlow
    {
        static GameObject root;
        static FarmState farm;

        public static FarmState Farm
        {
            get
            {
                if (farm == null)
                {
                    farm = FarmState.LoadOrCreate();
                    CurrentDifficulty = (Difficulty)Mathf.Clamp(farm.Data.LastDifficulty, 0, 2);
                }
                return farm;
            }
        }

        /// <summary>Startbildschirm mit Logo, „Spielen“ und „Einstellungen“.</summary>
        public static void ShowTitle()
        {
            var go = ReplaceRoot("MuhDefense Start");
            go.AddComponent<TitleScreen>().Init();
        }

        /// <summary>Spielstand löschen und neu beginnen (aus den Einstellungen).</summary>
        public static void ResetGame()
        {
            FarmState.DeleteSave();
            farm = null;
            CurrentDifficulty = Difficulty.Leicht;
            ShowTitle();
        }

        public static void ShowFarm()
        {
            var go = ReplaceRoot("MuhDefense Hof");
            go.AddComponent<FarmManager>().Init(Farm);
        }

        /// <summary>Gewählter Schwierigkeitsgrad (gilt für das nächste und das laufende Level).</summary>
        public static Difficulty CurrentDifficulty = Difficulty.Leicht;
        /// <summary>Modus für das nächste und das laufende Level (Normal, Herausforderung, Endlos).</summary>
        public static LevelMode CurrentMode = LevelMode.Normal;
        public static ChallengeRules CurrentRules;

        /// <summary>Startet ein Level in einem besonderen Modus.</summary>
        public static void StartMode(LevelDefinition def, LevelMode mode, ChallengeRules rules = null)
        {
            CurrentMode = mode;
            CurrentRules = rules;
            StartLevel(def);
        }

        /// <summary>Tägliche Herausforderung geschafft: Diamanten (einmal pro Tag) und etwas Mais und Milch.</summary>
        public static LevelReward CompleteChallenge(LevelDefinition def, int stars)
        {
            Farm.Tick();
            bool first = !Challenges.DoneToday(Farm);
            int corn = (int)Farm.Add(Resource.Mais, Mathf.RoundToInt(def.RewardCorn * 0.6f));
            int milk = (int)Farm.Add(Resource.Milch, Mathf.RoundToInt(def.RewardMilk * 0.6f));
            int gems = first ? Challenges.GemsReward : 0;
            Farm.AddGems(gems);
            Farm.Data.ChallengeDay = Quests.Today;
            if (first) Farm.Count(Counter.Challenges);
            Farm.AddXp(first ? 60 : 15, new Vector3(FarmState.Width / 2f, 2.5f, FarmState.Height / 2f));
            Farm.Save();
            return new LevelReward { Stars = stars, Corn = corn, Milk = milk, Gems = gems };
        }

        /// <summary>Überfall auf den Hof vorbei: gewonnen = Belohnung, verloren = die Aliens nehmen Vorräte mit.</summary>
        public static LevelReward CompleteRaid(bool won, int stars)
        {
            Farm.Tick();
            var reward = new LevelReward { Stars = stars };
            if (won)
            {
                int amount = Raids.RewardAmount(Farm);
                reward.Corn = (int)Farm.Add(Resource.Mais, amount);
                reward.Milk = (int)Farm.Add(Resource.Milch, amount);
                reward.Gems = 3 + stars * 2;
                Farm.AddGems(reward.Gems);
                Farm.AddXp(30 + 10 * stars, new Vector3(FarmState.Width / 2f, 2.5f, FarmState.Height / 2f));
                Farm.Count(Counter.RaidsWon);
            }
            else
            {
                Farm.StealSupplies("Überfall verloren");
            }
            Farm.ScheduleNextRaid(FarmState.Now);
            Farm.Save();
            return reward;
        }

        /// <summary>Endlos vorbei: Mais und Milch je geschaffter Welle, Diamanten für einen neuen Rekord.</summary>
        public static LevelReward CompleteEndless(LevelDefinition def, int waves)
        {
            Farm.Tick();
            int best = Challenges.EndlessBest(Farm, def.Id);
            bool record = waves > best;
            int corn = (int)Farm.Add(Resource.Mais, 40 * waves);
            int milk = (int)Farm.Add(Resource.Milch, 40 * waves);
            int gems = record ? Mathf.Max(1, (waves - best) / 2) : 0;
            Farm.AddGems(gems);
            if (record)
            {
                var e = Farm.Data.EndlessRecords.Find(r => r.Id == def.Id);
                if (e == null) Farm.Data.EndlessRecords.Add(new EndlessRecord { Id = def.Id, Waves = waves });
                else e.Waves = waves;
            }
            Farm.AddXp(5 * waves, new Vector3(FarmState.Width / 2f, 2.5f, FarmState.Height / 2f));
            Farm.Save();
            return new LevelReward { Corn = corn, Milk = milk, Gems = gems, NewRecord = record, Waves = waves };
        }

        public static void StartLevel(LevelDefinition def, Difficulty difficulty)
        {
            CurrentMode = LevelMode.Normal;
            CurrentRules = null;
            CurrentDifficulty = difficulty;
            Farm.Data.LastDifficulty = (int)difficulty;
            StartLevel(def);
        }

        public static void StartLevel(LevelDefinition def)
        {
            Farm.Tick();
            Farm.Save();
            var go = ReplaceRoot("MuhDefense Level");
            var bonuses = Farm.LevelBonuses;
            // Heldenhütte: jede Stufe lässt den Helden eine Stufe höher starten
            bonuses.HeroStartLevels += Mathf.Max(0, Farm.HeroHutLevel - 1);
            // Herausforderung und Endlos laufen immer auf „Mittel“
            var difficulty = CurrentMode == LevelMode.Normal ? CurrentDifficulty : Difficulty.Mittel;
            go.AddComponent<LevelManager>().Init(def, BuildTowerLoadout(), bonuses, Farm.SelectedHero, difficulty);
        }

        /// <summary>Welche Türme im Level verfügbar sind und bis zu welcher Stufe – bestimmt durch die Werkstätten auf dem Hof.</summary>
        public static Dictionary<TowerType, int> BuildTowerLoadout()
        {
            var loadout = new Dictionary<TowerType, int>();
            foreach (var tower in TowerType.All)
            {
                int max = Farm.TowerMaxLevel(tower);
                if (max > 0) loadout[tower] = max;
            }
            return loadout;
        }

        /// <summary>
        /// Ein Level ist freigeschaltet, wenn das vorherige mit mindestens 1 Stern geschafft wurde
        /// und man genug Sterne für sein Gebiet hat.
        /// </summary>
        public static bool IsUnlocked(int levelIndex)
        {
            if (!IsRegionUnlocked(Levels.All[levelIndex].Region)) return false;
            return levelIndex == 0 || Farm.BestStars(Levels.All[levelIndex - 1].Id) > 0;
        }

        public static bool IsRegionUnlocked(int region) => Farm.TotalStars >= Levels.Regions[region].UnlockStars;

        public static LevelReward CompleteLevel(LevelDefinition def, int stars, Difficulty difficulty = Difficulty.Mittel)
        {
            Farm.Tick();
            float factor = DifficultyRules.RewardFactor(difficulty);
            int corn = Mathf.RoundToInt(def.RewardCorn * stars / 3f * factor);
            int milk = Mathf.RoundToInt(def.RewardMilk * stars / 3f * factor);
            int cornAdded = (int)Farm.Add(Resource.Mais, corn);
            int milkAdded = (int)Farm.Add(Resource.Milch, milk);
            // Sterne und Diamanten zählen je Schwierigkeitsgrad
            int previous = Farm.Stars(def.Id, difficulty);
            bool firstClearEver = Farm.BestStars(def.Id) == 0;
            int gems = Mathf.Max(0, stars - previous) * FarmState.GemsPerNewStar + (firstClearEver ? FarmState.GemsFirstClear : 0);
            bool firstHard = difficulty == Difficulty.Schwer && previous == 0;
            if (firstHard) gems += DifficultyRules.HardFirstClearGems;
            Difficulty? next = difficulty == Difficulty.Leicht ? Difficulty.Mittel : difficulty == Difficulty.Mittel ? Difficulty.Schwer : (Difficulty?)null;
            bool nextWasLocked = next.HasValue && !Farm.IsDifficultyUnlocked(def.Id, next.Value);
            Farm.AddGems(gems);
            Farm.RecordStars(def.Id, stars, difficulty);
            // Erfahrung für die Bauernstufe (schwerer = mehr)
            float xpFactor = difficulty == Difficulty.Schwer ? 2f : difficulty == Difficulty.Mittel ? 1.5f : 1f;
            Farm.AddXp(Mathf.RoundToInt((15 + 10 * stars) * xpFactor), new Vector3(FarmState.Width / 2f, 2.5f, FarmState.Height / 2f));
            Farm.Save();
            return new LevelReward
            {
                Stars = stars,
                Corn = cornAdded,
                Milk = milkAdded,
                CornLost = corn - cornAdded,
                MilkLost = milk - milkAdded,
                Gems = gems,
                FirstHardClear = firstHard,
                UnlockedDifficulty = nextWasLocked ? DifficultyRules.Name(next.Value) : "",
            };
        }

        static GameObject ReplaceRoot(string name)
        {
            if (root != null) Object.Destroy(root);
            root = new GameObject(name);
            return root;
        }
    }
}
