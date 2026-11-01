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

        public static void StartLevel(LevelDefinition def, Difficulty difficulty)
        {
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
            go.AddComponent<LevelManager>().Init(def, BuildTowerLoadout(), bonuses, Farm.SelectedHero, CurrentDifficulty);
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
