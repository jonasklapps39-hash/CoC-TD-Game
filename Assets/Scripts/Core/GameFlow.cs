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
                if (farm == null) farm = FarmState.LoadOrCreate();
                return farm;
            }
        }

        public static void ShowFarm()
        {
            var go = ReplaceRoot("MuhDefense Hof");
            go.AddComponent<FarmManager>().Init(Farm);
        }

        public static void StartLevel(LevelDefinition def)
        {
            Farm.Tick();
            Farm.Save();
            var go = ReplaceRoot("MuhDefense Level");
            go.AddComponent<LevelManager>().Init(def, BuildTowerLoadout());
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

        /// <summary>Ein Level ist freigeschaltet, wenn das vorherige mit mindestens 1 Stern geschafft wurde.</summary>
        public static bool IsUnlocked(int levelIndex)
        {
            return levelIndex == 0 || Farm.BestStars(Levels.All[levelIndex - 1].Id) > 0;
        }

        public static LevelReward CompleteLevel(LevelDefinition def, int stars)
        {
            Farm.Tick();
            int corn = Mathf.RoundToInt(def.RewardCorn * stars / 3f);
            int milk = Mathf.RoundToInt(def.RewardMilk * stars / 3f);
            int cornAdded = (int)Farm.Add(Resource.Mais, corn);
            int milkAdded = (int)Farm.Add(Resource.Milch, milk);
            Farm.RecordStars(def.Id, stars);
            Farm.Save();
            return new LevelReward
            {
                Stars = stars,
                Corn = cornAdded,
                Milk = milkAdded,
                CornLost = corn - cornAdded,
                MilkLost = milk - milkAdded,
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
