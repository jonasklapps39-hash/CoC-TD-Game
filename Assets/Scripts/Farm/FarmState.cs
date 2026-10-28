using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace MuhDefense
{
    /// <summary>
    /// Spiellogik des Hofs ohne Grafik: Rohstoffe, Gebäude, Bauzeiten, Lager und Level-Fortschritt.
    /// Alles ist zeitbasiert (echte Uhrzeit), Produktion und Bauarbeiten laufen also auch weiter,
    /// während man ein Level spielt oder das Spiel geschlossen ist.
    /// </summary>
    public class FarmState
    {
        public const int Width = 22;
        public const int Height = 12;
        const int FarmhouseBaseCapacity = 500;
        const int FarmhouseCapacityPerLevel = 250;

        static readonly DateTime Epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        public static double Now => (DateTime.UtcNow - Epoch).TotalSeconds;

        static string SavePath => Path.Combine(Application.persistentDataPath, "hof.json");

        public SaveData Data { get; private set; }

        public IReadOnlyList<BuildingData> Buildings => Data.Buildings;

        // ---------------------------------------------------------------- Laden und Speichern

        public static FarmState LoadOrCreate()
        {
            var state = new FarmState();
            try
            {
                if (File.Exists(SavePath))
                {
                    state.Data = JsonUtility.FromJson<SaveData>(File.ReadAllText(SavePath));
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("Spielstand konnte nicht geladen werden, starte neu: " + e.Message);
                state.Data = null;
            }

            if (state.Data == null || state.Data.Buildings == null || state.Data.Buildings.Count == 0)
            {
                state.Data = CreateNewFarm();
            }
            state.Tick();
            return state;
        }

        public void Save()
        {
            try
            {
                File.WriteAllText(SavePath, JsonUtility.ToJson(Data));
            }
            catch (Exception e)
            {
                Debug.LogWarning("Spielstand konnte nicht gespeichert werden: " + e.Message);
            }
        }

        public static void DeleteSave()
        {
            if (File.Exists(SavePath)) File.Delete(SavePath);
        }

        static SaveData CreateNewFarm()
        {
            var data = new SaveData { Corn = 300, Milk = 300 };
            AddStartBuilding(data, FarmBuildingType.Bauernhaus, 9, 5);
            AddStartBuilding(data, FarmBuildingType.Maisfeld, 4, 8);
            AddStartBuilding(data, FarmBuildingType.Kuhstall, 15, 8);
            AddStartBuilding(data, FarmBuildingType.Knechthuette, 4, 2);
            AddStartBuilding(data, FarmBuildingType.Schmiede, 15, 2);
            return data;
        }

        static void AddStartBuilding(SaveData data, FarmBuildingType type, int x, int y)
        {
            data.Buildings.Add(new BuildingData
            {
                Id = data.NextBuildingId++, Type = type.Id, X = x, Y = y, Level = 1, LastTick = Now,
            });
        }

        // ---------------------------------------------------------------- Zeit

        /// <summary>Bringt alle Gebäude auf den aktuellen Zeitpunkt. Liefert die Gebäude, die gerade fertig geworden sind.</summary>
        public List<BuildingData> Tick()
        {
            var finished = new List<BuildingData>();
            double now = Now;
            foreach (var b in Data.Buildings)
            {
                if (b.IsUpgrading)
                {
                    if (now < b.UpgradeEnd)
                    {
                        // Während des Baus ruht die Produktion.
                        b.LastTick = now;
                        continue;
                    }
                    b.Level++;
                    b.LastTick = b.UpgradeEnd;
                    b.UpgradeEnd = 0;
                    finished.Add(b);
                }

                var type = b.TypeDef;
                if (type != null && type.IsProducer && b.Level > 0)
                {
                    double elapsed = Math.Max(0, now - b.LastTick);
                    b.Stored = Math.Min(ProducerCapacity(b), b.Stored + ProductionPerMinute(b) / 60.0 * elapsed);
                }
                b.LastTick = now;
            }
            return finished;
        }

        // ---------------------------------------------------------------- Rohstoffe

        public double Amount(Resource r) => r == Resource.Mais ? Data.Corn : Data.Milk;

        public int FarmhouseLevel
        {
            get
            {
                foreach (var b in Data.Buildings)
                {
                    if (b.Type == FarmBuildingType.Bauernhaus.Id) return Mathf.Max(1, b.Level);
                }
                return 1;
            }
        }

        public static int FarmhouseCapacity(int farmhouseLevel) => FarmhouseBaseCapacity + FarmhouseCapacityPerLevel * (farmhouseLevel - 1);

        public double Capacity(Resource r)
        {
            double cap = FarmhouseCapacity(FarmhouseLevel);
            foreach (var b in Data.Buildings)
            {
                var type = b.TypeDef;
                if (type != null && type.IsStorage && type.StoredResource == r && b.Level > 0)
                {
                    cap += type.StorageCapacity[b.Level - 1];
                }
            }
            return cap;
        }

        /// <summary>Fügt Rohstoffe hinzu (höchstens bis zur Lagergrenze). Liefert die tatsächlich hinzugefügte Menge.</summary>
        public double Add(Resource r, double amount)
        {
            double space = Math.Max(0, Capacity(r) - Amount(r));
            double added = Math.Min(space, Math.Max(0, amount));
            if (r == Resource.Mais) Data.Corn += added;
            else Data.Milk += added;
            return added;
        }

        public bool CanAfford(Resource r, int amount) => Amount(r) >= amount;

        bool Spend(Resource r, int amount)
        {
            if (!CanAfford(r, amount)) return false;
            if (r == Resource.Mais) Data.Corn -= amount;
            else Data.Milk -= amount;
            return true;
        }

        // ---------------------------------------------------------------- Produktion

        public float ProductionPerMinute(BuildingData b)
        {
            var type = b.TypeDef;
            return type != null && type.IsProducer && b.Level > 0 ? type.RatePerMinute[b.Level - 1] : 0f;
        }

        public double ProducerCapacity(BuildingData b)
        {
            var type = b.TypeDef;
            return type != null && type.IsProducer && b.Level > 0 ? type.ProducerCapacity[b.Level - 1] : 0;
        }

        /// <summary>Sammelt die angesammelte Menge ein, soweit Platz im Lager ist. Liefert die eingesammelte Menge.</summary>
        public double Collect(BuildingData b)
        {
            var type = b.TypeDef;
            if (type == null || !type.IsProducer) return 0;
            double added = Add(type.ProducedResource, Math.Floor(b.Stored));
            b.Stored -= added;
            return added;
        }

        // ---------------------------------------------------------------- Knechte

        public int BuildersTotal
        {
            get
            {
                int n = 0;
                foreach (var b in Data.Buildings)
                {
                    if (b.Type == FarmBuildingType.Knechthuette.Id && b.Level > 0) n++;
                }
                return n;
            }
        }

        public int BuildersBusy
        {
            get
            {
                int n = 0;
                foreach (var b in Data.Buildings)
                {
                    if (b.IsUpgrading) n++;
                }
                return n;
            }
        }

        public int BuildersFree => Mathf.Max(0, BuildersTotal - BuildersBusy);

        // ---------------------------------------------------------------- Bauen und Verbessern

        public int Count(FarmBuildingType type)
        {
            int n = 0;
            foreach (var b in Data.Buildings)
            {
                if (b.Type == type.Id) n++;
            }
            return n;
        }

        public int MaxCount(FarmBuildingType type) => type.MaxCount[Mathf.Clamp(FarmhouseLevel - 1, 0, type.MaxCount.Length - 1)];

        /// <summary>Höchste erlaubte Stufe: Gebäude können nicht höher sein als das Bauernhaus.</summary>
        public int MaxLevelFor(FarmBuildingType type)
        {
            return type == FarmBuildingType.Bauernhaus ? type.MaxLevel : Mathf.Min(type.MaxLevel, FarmhouseLevel);
        }

        /// <returns>null, wenn gebaut werden kann, sonst der Grund.</returns>
        public string WhyCannotBuild(FarmBuildingType type)
        {
            if (FarmhouseLevel < type.UnlockFarmhouseLevel) return $"Bauernhaus Stufe {type.UnlockFarmhouseLevel} nötig";
            if (Count(type) >= MaxCount(type))
            {
                return MaxCount(type) < type.MaxCount[type.MaxCount.Length - 1]
                    ? "Maximum erreicht – Bauernhaus verbessern"
                    : "Maximum erreicht";
            }
            if (!CanAfford(type.CostResource, type.CostForLevel(1))) return $"Zu wenig {FarmBuildingType.ResourceName(type.CostResource)}";
            if (BuildersFree <= 0) return "Kein freier Knecht";
            return null;
        }

        /// <returns>null, wenn verbessert werden kann, sonst der Grund.</returns>
        public string WhyCannotUpgrade(BuildingData b)
        {
            var type = b.TypeDef;
            if (b.IsUpgrading) return "Wird gerade gebaut";
            if (b.Level >= type.MaxLevel) return "Maximale Stufe erreicht";
            if (b.Level >= MaxLevelFor(type)) return $"Bauernhaus Stufe {b.Level + 1} nötig";
            if (!CanAfford(type.CostResource, type.CostForLevel(b.Level + 1))) return $"Zu wenig {FarmBuildingType.ResourceName(type.CostResource)}";
            if (BuildersFree <= 0) return "Kein freier Knecht";
            return null;
        }

        public BuildingData Build(FarmBuildingType type, int x, int y)
        {
            if (WhyCannotBuild(type) != null || !IsAreaFree(x, y, type.Size, -1)) return null;
            Spend(type.CostResource, type.CostForLevel(1));
            double now = Now;
            var b = new BuildingData
            {
                Id = Data.NextBuildingId++, Type = type.Id, X = x, Y = y, Level = 0,
                UpgradeEnd = now + type.SecondsForLevel(1), LastTick = now,
            };
            Data.Buildings.Add(b);
            return b;
        }

        public bool StartUpgrade(BuildingData b)
        {
            if (WhyCannotUpgrade(b) != null) return false;
            Tick();
            var type = b.TypeDef;
            Spend(type.CostResource, type.CostForLevel(b.Level + 1));
            b.UpgradeEnd = Now + type.SecondsForLevel(b.Level + 1);
            return true;
        }

        public double RemainingSeconds(BuildingData b) => b.IsUpgrading ? Math.Max(0, b.UpgradeEnd - Now) : 0;

        public float UpgradeProgress(BuildingData b)
        {
            if (!b.IsUpgrading) return 1f;
            double total = b.TypeDef.SecondsForLevel(b.Level + 1);
            return total <= 0 ? 1f : Mathf.Clamp01((float)(1.0 - RemainingSeconds(b) / total));
        }

        // ---------------------------------------------------------------- Platz auf dem Hof

        public BuildingData BuildingAt(Vector2Int cell)
        {
            foreach (var b in Data.Buildings)
            {
                int size = b.TypeDef.Size;
                if (cell.x >= b.X && cell.x < b.X + size && cell.y >= b.Y && cell.y < b.Y + size) return b;
            }
            return null;
        }

        public bool IsAreaFree(int x, int y, int size, int ignoreId)
        {
            if (x < 0 || y < 0 || x + size > Width || y + size > Height) return false;
            foreach (var b in Data.Buildings)
            {
                if (b.Id == ignoreId) continue;
                int s = b.TypeDef.Size;
                bool overlap = x < b.X + s && b.X < x + size && y < b.Y + s && b.Y < y + size;
                if (overlap) return false;
            }
            return true;
        }

        public bool Move(BuildingData b, int x, int y)
        {
            if (!IsAreaFree(x, y, b.TypeDef.Size, b.Id)) return false;
            b.X = x;
            b.Y = y;
            return true;
        }

        // ---------------------------------------------------------------- Türme und Levels

        /// <summary>Höchste Upgrade-Stufe im Level für eine Werkstatt-Stufe (0 = Turm nicht freigeschaltet).</summary>
        public static int TowerLevelForWorkshop(int workshopLevel) => workshopLevel <= 0 ? 0 : Mathf.Min(workshopLevel + 1, TowerType.MaxTier);

        /// <summary>Höchste Stufe, bis zu der dieser Turm im Level gebaut werden darf (0 = nicht freigeschaltet).</summary>
        public int TowerMaxLevel(TowerType tower)
        {
            int workshopLevel = 0;
            foreach (var b in Data.Buildings)
            {
                var type = b.TypeDef;
                if (type != null && type.UnlocksTower == tower) workshopLevel = Mathf.Max(workshopLevel, b.Level);
            }
            return TowerLevelForWorkshop(workshopLevel);
        }

        public int BestStars(string levelId)
        {
            foreach (var p in Data.Levels)
            {
                if (p.Id == levelId) return p.BestStars;
            }
            return 0;
        }

        public int TotalStars
        {
            get
            {
                int n = 0;
                foreach (var p in Data.Levels) n += p.BestStars;
                return n;
            }
        }

        public void RecordStars(string levelId, int stars)
        {
            foreach (var p in Data.Levels)
            {
                if (p.Id != levelId) continue;
                p.BestStars = Mathf.Max(p.BestStars, stars);
                return;
            }
            Data.Levels.Add(new LevelProgress { Id = levelId, BestStars = stars });
        }
    }
}
