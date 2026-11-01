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
    public partial class FarmState
    {
        public const int Width = 30;
        public const int Height = 18;
        const int FarmhouseBaseCapacity = 1000;
        public const int WelcomeGems = 30;
        public const int GemsPerNewStar = 5;
        public const int GemsFirstClear = 10;
        public const int ResourcePackGems = 20;
        public const int ResourcePackAmount = 1500;
        public const int MaxExtraBuilders = 2;
        const int FarmhouseCapacityPerLevel = 500;

        static readonly DateTime Epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        public static double Now => (DateTime.UtcNow - Epoch).TotalSeconds;

        static string SavePath => Path.Combine(Application.persistentDataPath, "hof.json");

        public SaveData Data { get; private set; }

        public IReadOnlyList<BuildingData> Buildings => Data.Buildings;

        // ---------------------------------------------------------------- Laden und Speichern

        public static FarmState LoadOrCreate()
        {
            var state = new FarmState();
            state.Data = TryLoad(SavePath);
            if (!IsValid(state.Data))
            {
                // Hauptdatei kaputt oder leer: Sicherung versuchen, kaputte Datei aufheben statt überschreiben
                var backup = TryLoad(SavePath + ".bak");
                if (IsValid(backup))
                {
                    if (File.Exists(SavePath))
                    {
                        try { File.Copy(SavePath, SavePath + ".kaputt", true); } catch (Exception) { }
                    }
                    state.Data = backup;
                }
            }

            if (state.Data == null || state.Data.Buildings == null || state.Data.Buildings.Count == 0)
            {
                state.Data = CreateNewFarm();
                state.SpawnObstacles(14, Environment.TickCount);
            }
            state.Migrate();
            state.Tick();
            return state;
        }

        /// <summary>Ältere Spielstände auf den aktuellen Stand bringen.</summary>
        void Migrate()
        {
            if (Data.Version < 2)
            {
                Data.Gems += WelcomeGems;
                Data.Version = 2;
            }
            if (Data.Version < 3)
            {
                // Der Hof ist größer geworden (22×12 → 30×18): alte Gebäude in die Mitte rücken
                foreach (var b in Data.Buildings)
                {
                    b.X += 4;
                    b.Y += 3;
                }
                Data.Version = 3;
            }
            if (Data.Version < 4)
            {
                // Wer schon gespielt hat, braucht das Hof-Tutorial nicht mehr
                if (Data.Levels.Count > 0) Data.TutorialStep = -1;
                Data.Version = 4;
            }
            if (Data.Version < 5)
            {
                // Sterne gibt es jetzt je Schwierigkeitsgrad. Alte Sterne zählen als „Mittel“, alte Kronen als „Schwer“.
                foreach (var p in Data.Levels)
                {
                    if (p.HardCleared) p.HardStars = Mathf.Max(1, p.BestStars);
                    p.HardCleared = false;
                }
                Data.Version = 5;
            }
            if (Data.Obstacles == null) Data.Obstacles = new List<ObstacleData>();
            if (Data.EndlessRecords == null) Data.EndlessRecords = new List<EndlessRecord>();
            // Unbekannte Gebäude (z. B. aus einer neueren Version) würden sonst überall Fehler auslösen
            Data.Buildings.RemoveAll(b => b == null || b.TypeDef == null);
            if (Data.Version < 6)
            {
                // Neu: Hindernisse auf dem Hof und Bauernstufe
                if (Data.FarmLevel < 1) Data.FarmLevel = 1;
                SpawnObstacles(10, Data.NextBuildingId * 31 + 7);
                Data.LastObstacleSpawn = Now;
                Data.Version = 6;
            }
            if (Data.OwnedSkins == null || Data.OwnedSkins.Count == 0) Data.OwnedSkins = new List<string> { "standard" };
            if (string.IsNullOrEmpty(Data.CowSkin)) Data.CowSkin = "standard";
            if (Data.Talents == null) Data.Talents = new List<string>();
            if (Data.SeenEnemies == null) Data.SeenEnemies = new List<string>();
            if (Data.Counters == null) Data.Counters = new List<CounterEntry>();
            if (Data.Daily == null) Data.Daily = new List<DailyQuestState>();
            if (Data.ClaimedAchievements == null) Data.ClaimedAchievements = new List<string>();
            if (Data.HeroProgress == null) Data.HeroProgress = new List<HeroProgress>();
            if (Data.OwnedHeroSkins == null) Data.OwnedHeroSkins = new List<string>();
            if (Data.UnlockedHeroes == null) Data.UnlockedHeroes = new List<string> { "Heinz" };
            if (string.IsNullOrEmpty(Data.SelectedHero)) Data.SelectedHero = "Heinz";
        }

        static SaveData TryLoad(string path)
        {
            try
            {
                if (File.Exists(path)) return JsonUtility.FromJson<SaveData>(File.ReadAllText(path));
            }
            catch (Exception e)
            {
                Debug.LogWarning("Spielstand konnte nicht geladen werden (" + path + "): " + e.Message);
            }
            return null;
        }

        static bool IsValid(SaveData d) => d != null && d.Buildings != null && d.Buildings.Count > 0;

        public void Save()
        {
            try
            {
                // Sicher speichern: erst in eine Zwischendatei, dann austauschen (die alte bleibt als .bak).
                // So geht der Hof nicht verloren, wenn das Handy die App mitten im Speichern beendet.
                string tmp = SavePath + ".tmp";
                File.WriteAllText(tmp, JsonUtility.ToJson(Data));
                if (!File.Exists(SavePath))
                {
                    File.Move(tmp, SavePath);
                    return;
                }
                try
                {
                    File.Replace(tmp, SavePath, SavePath + ".bak");
                }
                catch (Exception)
                {
                    // Falls File.Replace auf der Plattform fehlt: von Hand austauschen
                    File.Copy(SavePath, SavePath + ".bak", true);
                    File.Delete(SavePath);
                    File.Move(tmp, SavePath);
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("Spielstand konnte nicht gespeichert werden: " + e.Message);
            }
        }

        public static void DeleteSave()
        {
            foreach (var suffix in new[] { "", ".bak", ".tmp" })
            {
                if (File.Exists(SavePath + suffix)) File.Delete(SavePath + suffix);
            }
        }

        static SaveData CreateNewFarm()
        {
            var data = new SaveData { Corn = 300, Milk = 300, Gems = WelcomeGems, Version = 6 };
            AddStartBuilding(data, FarmBuildingType.Bauernhaus, 13, 8);
            AddStartBuilding(data, FarmBuildingType.Maisfeld, 8, 11);
            AddStartBuilding(data, FarmBuildingType.Kuhstall, 19, 11);
            AddStartBuilding(data, FarmBuildingType.Knechthuette, 8, 5);
            AddStartBuilding(data, FarmBuildingType.Schmiede, 19, 5);
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
        readonly List<BuildingData> pendingFinished = new List<BuildingData>();

        /// <summary>Gebäude, die seit dem letzten Abholen fertig geworden sind (auch wenn Tick von woanders kam).</summary>
        public List<BuildingData> TakeFinished()
        {
            var list = new List<BuildingData>(pendingFinished);
            pendingFinished.Clear();
            return list;
        }

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
                        b.LastTick = Math.Max(b.LastTick, now);
                        continue;
                    }
                    b.Level++;
                    b.LastTick = b.UpgradeEnd;
                    b.UpgradeEnd = 0;
                    finished.Add(b);
                    pendingFinished.Add(b);
                    if (b.TypeDef != null) AddXp(XpForBuilding(b.TypeDef, b.Level), new Vector3(b.X + 1f, 2f, b.Y + 1f));
                }

                var type = b.TypeDef;
                if (type != null && type.IsProducer && b.Level > 0)
                {
                    double elapsed = Math.Max(0, now - b.LastTick);
                    // Ernte-Turbo: der Teil der Zeit, in dem der Turbo lief, zählt doppelt
                    double boosted = Math.Max(0, Math.Min(now, b.BoostEnd) - b.LastTick);
                    b.Stored = Math.Min(ProducerCapacity(b), b.Stored + ProductionPerMinute(b) / 60.0 * (elapsed + boosted));
                }
                // Zeit läuft nur vorwärts (Uhr zurückstellen bringt keine doppelte Produktion)
                b.LastTick = Math.Max(b.LastTick, now);
            }
            TickObstacles(now);
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
                return n + Data.ExtraBuilders;
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
                return n + ObstaclesClearing;
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
            Count(Counter.FarmBuilds);
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
            Count(Counter.FarmBuilds);
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
            foreach (var o in Data.Obstacles)
            {
                if (o.X >= x && o.X < x + size && o.Y >= y && o.Y < y + size) return false;
            }
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

        /// <summary>Höchste Upgrade-Stufe im Level für eine Werkstatt-Stufe (0 = Turm noch nicht freigeschaltet).</summary>
        public static int TowerLevelForWorkshop(int workshopLevel, int fromLevel = 1)
            => workshopLevel < fromLevel ? 0 : Mathf.Min(workshopLevel - fromLevel + 2, TowerType.MaxTier);

        /// <summary>Höchste Stufe, bis zu der dieser Turm im Level gebaut werden darf (0 = nicht freigeschaltet).</summary>
        public int TowerMaxLevel(TowerType tower)
        {
            int best = 0;
            foreach (var b in Data.Buildings)
            {
                var type = b.TypeDef;
                if (type == null) continue;
                foreach (var unlock in type.Unlocks)
                {
                    if (unlock.Tower == tower) best = Mathf.Max(best, TowerLevelForWorkshop(b.Level, unlock.FromLevel));
                }
            }
            return best;
        }

        /// <returns>true, wenn die Alien-Art zum ersten Mal auftaucht (und merkt sie sich).</returns>
        public bool MarkEnemySeen(EnemyKind kind)
        {
            if (Data.SeenEnemies == null) Data.SeenEnemies = new List<string>();
            string id = kind.ToString();
            if (Data.SeenEnemies.Contains(id)) return false;
            Data.SeenEnemies.Add(id);
            return true;
        }

        LevelProgress FindLevel(string levelId) => Data.Levels.Find(p => p.Id == levelId);

        /// <summary>Beste Sterne eines Levels auf einem Schwierigkeitsgrad.</summary>
        public int Stars(string levelId, Difficulty d)
        {
            var p = FindLevel(levelId);
            if (p == null) return 0;
            return d == Difficulty.Leicht ? p.EasyStars : d == Difficulty.Schwer ? p.HardStars : p.BestStars;
        }

        /// <summary>Beste Sterne eines Levels über alle Schwierigkeitsgrade (für das Freischalten des nächsten Levels).</summary>
        public int BestStars(string levelId)
        {
            var p = FindLevel(levelId);
            return p == null ? 0 : Mathf.Max(p.EasyStars, Mathf.Max(p.BestStars, p.HardStars));
        }

        /// <summary>Erst „Leicht“ schaffen, dann wird „Mittel“ frei, danach „Schwer“.</summary>
        public bool IsDifficultyUnlocked(string levelId, Difficulty d)
        {
            switch (d)
            {
                case Difficulty.Mittel: return Stars(levelId, Difficulty.Leicht) > 0 || Stars(levelId, Difficulty.Mittel) > 0;
                case Difficulty.Schwer: return Stars(levelId, Difficulty.Mittel) > 0 || Stars(levelId, Difficulty.Schwer) > 0;
                default: return true;
            }
        }

        /// <summary>Alle Sterne zusammen – jeder Schwierigkeitsgrad zählt einzeln (bis zu 9 pro Level).</summary>
        public int TotalStars
        {
            get
            {
                int n = 0;
                foreach (var p in Data.Levels) n += p.EasyStars + p.BestStars + p.HardStars;
                return n;
            }
        }

        // ---------------------------------------------------------------- Diamanten

        public int Gems => Data.Gems;

        public void AddGems(int amount) => Data.Gems += Mathf.Max(0, amount);

        bool SpendGems(int amount)
        {
            if (Data.Gems < amount) return false;
            Data.Gems -= amount;
            return true;
        }

        /// <summary>Diamanten, um einen Bau sofort fertigzustellen (1 Diamant pro angefangene 30 Sekunden).</summary>
        public int InstantFinishCost(BuildingData b) => b.IsUpgrading ? Mathf.Max(1, Mathf.CeilToInt((float)(RemainingSeconds(b) / 30.0))) : 0;

        public bool InstantFinish(BuildingData b)
        {
            if (!b.IsUpgrading || !SpendGems(InstantFinishCost(b))) return false;
            b.UpgradeEnd = Now;
            return true;
        }

        public const int BoostGems = 10;
        public const double BoostSeconds = 3600;

        public bool IsBoosted(BuildingData b) => b.BoostEnd > Now;

        public bool BoostProducer(BuildingData b)
        {
            if (b.TypeDef == null || !b.TypeDef.IsProducer || b.Level <= 0 || b.IsUpgrading || IsBoosted(b) || !SpendGems(BoostGems)) return false;
            Tick();
            b.BoostEnd = Now + BoostSeconds;
            return true;
        }

        public int ExtraBuilderCost => 80 * (Data.ExtraBuilders + 1);

        public bool BuyExtraBuilder()
        {
            if (Data.ExtraBuilders >= MaxExtraBuilders || !SpendGems(ExtraBuilderCost)) return false;
            Data.ExtraBuilders++;
            return true;
        }

        /// <returns>Tatsächlich hinzugefügte Menge (0, wenn zu wenig Diamanten oder Lager voll).</returns>
        public double BuyResourcePack(Resource r)
        {
            // Nur kaufen, wenn mindestens ein Viertel des Pakets ins Lager passt
            if (Capacity(r) - Amount(r) < ResourcePackAmount * 0.25 || !SpendGems(ResourcePackGems)) return 0;
            return Add(r, ResourcePackAmount);
        }

        public bool OwnsSkin(string id) => Data.OwnedSkins.Contains(id);

        public bool BuySkin(CowSkin skin)
        {
            if (OwnsSkin(skin.Id) || !SpendGems(skin.Cost)) return false;
            Data.OwnedSkins.Add(skin.Id);
            Data.CowSkin = skin.Id;
            return true;
        }

        public void SelectSkin(CowSkin skin)
        {
            if (OwnsSkin(skin.Id)) Data.CowSkin = skin.Id;
        }

        // ---------------------------------------------------------------- Helden

        public bool IsHeroUnlocked(HeroType h) => h.UnlockStars == 0 || Data.UnlockedHeroes.Contains(h.Id) || TotalStars >= h.UnlockStars;

        public bool UnlockHeroWithGems(HeroType h)
        {
            if (IsHeroUnlocked(h) || !SpendGems(h.UnlockGems)) return false;
            Data.UnlockedHeroes.Add(h.Id);
            return true;
        }

        public void SelectHero(HeroType h)
        {
            if (IsHeroUnlocked(h)) Data.SelectedHero = h.Id;
        }

        /// <summary>Gewählter Held (ohne Heldenhütte immer Bauer Heinz).</summary>
        public HeroType SelectedHero
        {
            get
            {
                var h = HeroType.Get(Data.SelectedHero);
                return HeroHutLevel > 0 && IsHeroUnlocked(h) ? h : HeroType.Heinz;
            }
        }

        public int SchoolLevel
        {
            get
            {
                int best = 0;
                foreach (var b in Data.Buildings)
                {
                    if (b.Type == FarmBuildingType.Bauernschule.Id) best = Mathf.Max(best, b.Level);
                }
                return best;
            }
        }

        public int HeroHutLevel
        {
            get
            {
                int best = 0;
                foreach (var b in Data.Buildings)
                {
                    if (b.Type == FarmBuildingType.Heldenhuette.Id) best = Mathf.Max(best, b.Level);
                }
                return best;
            }
        }

        // ---------------------------------------------------------------- Talentbaum

        public int SpentStars
        {
            get
            {
                int n = 0;
                if (Data.Talents == null) return 0;
                foreach (var id in Data.Talents)
                {
                    var t = MuhDefense.Talents.Get(id);
                    if (t != null) n += t.Cost;
                }
                return n;
            }
        }

        public int AvailableStars => TotalStars - SpentStars;

        public bool HasTalent(string id) => Data.Talents != null && Data.Talents.Contains(id);

        /// <returns>null, wenn das Talent gekauft werden kann, sonst der Grund.</returns>
        public string WhyCannotBuyTalent(Talent t)
        {
            if (HasTalent(t.Id)) return "Gelernt";
            int row = MuhDefense.Talents.RowOf(t);
            if (SchoolLevel < row + 1) return SchoolLevel == 0 ? "Bauernschule bauen" : $"Bauernschule Stufe {row + 1}";
            if (t.Requires != null && !HasTalent(t.Requires)) return "Erst das Talent davor";
            if (AvailableStars < t.Cost) return "Zu wenig Sterne";
            return null;
        }

        public bool BuyTalent(Talent t)
        {
            if (WhyCannotBuyTalent(t) != null) return false;
            if (Data.Talents == null) Data.Talents = new List<string>();
            Data.Talents.Add(t.Id);
            return true;
        }

        public LevelBonuses LevelBonuses => MuhDefense.Talents.BuildBonuses(Data.Talents ?? new List<string>());

        // ---------------------------------------------------------------- Helden-Training und Skins

        HeroProgress Progress(HeroType h)
        {
            var p = Data.HeroProgress.Find(x => x.Id == h.Id);
            if (p == null)
            {
                p = new HeroProgress { Id = h.Id };
                Data.HeroProgress.Add(p);
            }
            return p;
        }

        public int HeroTrainingLevel(HeroType h) => Progress(h).Training;

        public string WhyCannotTrain(HeroType h)
        {
            int next = HeroTrainingLevel(h) + 1;
            if (next > MuhDefense.HeroTraining.MaxLevel) return "Voll trainiert";
            if (!IsHeroUnlocked(h)) return "Held noch gesperrt";
            if (HeroHutLevel < next) return $"Heldenhütte Stufe {next} nötig";
            if (!CanAfford(Resource.Milch, MuhDefense.HeroTraining.CostFor(next))) return "Zu wenig Milch";
            return null;
        }

        public bool TrainHero(HeroType h)
        {
            if (WhyCannotTrain(h) != null) return false;
            var p = Progress(h);
            Spend(Resource.Milch, MuhDefense.HeroTraining.CostFor(p.Training + 1));
            p.Training++;
            return true;
        }

        public bool OwnsHeroSkin(HeroSkin s) => Data.OwnedHeroSkins.Contains(s.Id);

        public bool BuyHeroSkin(HeroSkin s)
        {
            if (OwnsHeroSkin(s) || Data.Gems < s.Gems) return false;
            Data.Gems -= s.Gems;
            Data.OwnedHeroSkins.Add(s.Id);
            return true;
        }

        public HeroSkin SelectedHeroSkin(HeroType h) => HeroSkin.Get(Progress(h).Skin);

        public void SelectHeroSkin(HeroType h, HeroSkin s)
        {
            if (s != null && !OwnsHeroSkin(s)) return;
            Progress(h).Skin = s != null ? s.Id : "";
        }

        public int CrownCount
        {
            get
            {
                int n = 0;
                foreach (var p in Data.Levels)
                {
                    if (p.HardStars > 0) n++;
                }
                return n;
            }
        }

        // ---------------------------------------------------------------- Aufgaben und Erfolge

        /// <summary>Zählt einen Zähler hoch (auch die passenden täglichen Aufgaben).</summary>
        public void Count(string counter, int amount = 1)
        {
            if (amount <= 0) return;
            var entry = Data.Counters.Find(c => c.Id == counter);
            if (entry == null)
            {
                entry = new CounterEntry { Id = counter };
                Data.Counters.Add(entry);
            }
            entry.Value += amount;

            EnsureDaily();
            foreach (var q in Data.Daily)
            {
                var def = Quests.GetDaily(q.Id);
                if (def != null && def.Counter == counter) q.Progress = Mathf.Min(def.Target, q.Progress + amount);
            }
        }

        public long CounterValue(string counter)
        {
            var entry = Data.Counters.Find(c => c.Id == counter);
            return entry != null ? entry.Value : 0;
        }

        /// <summary>Neuer Tag → neue Aufgaben.</summary>
        public void EnsureDaily()
        {
            int today = Quests.Today;
            // Nur vorwärts: Datum zurückstellen bringt keine neuen Aufgaben
            if (Data.Daily.Count > 0 && today <= Data.DailyDay) return;
            Data.DailyDay = today;
            Data.Daily.Clear();
            foreach (var id in Quests.PickForDay(today)) Data.Daily.Add(new DailyQuestState { Id = id });
        }

        public List<DailyQuestState> DailyQuests
        {
            get
            {
                EnsureDaily();
                return Data.Daily;
            }
        }

        public bool ClaimDaily(DailyQuestState q)
        {
            var def = Quests.GetDaily(q.Id);
            if (def == null || q.Claimed || q.Progress < def.Target) return false;
            if (WhyCannotClaim(def) != null) return false;
            q.Claimed = true;
            GiveReward(def.Reward, def.Amount);
            return true;
        }

        /// <summary>Mais- oder Milch-Belohnung nur abholen, wenn sie ins Lager passt.</summary>
        public string WhyCannotClaim(DailyQuestDef def)
        {
            if (def.Reward == RewardKind.Gems) return null;
            var r = def.Reward == RewardKind.Corn ? Resource.Mais : Resource.Milch;
            return Capacity(r) - Amount(r) < def.Amount ? "Lager zu voll" : null;
        }

        void GiveReward(RewardKind kind, int amount)
        {
            switch (kind)
            {
                case RewardKind.Corn: Add(Resource.Mais, amount); break;
                case RewardKind.Milk: Add(Resource.Milch, amount); break;
                default: AddGems(amount); break;
            }
        }

        public int AchievementProgress(AchievementDef a)
        {
            long v = a.Value != null ? a.Value(this) : CounterValue(a.Counter);
            return (int)Math.Min(a.Target, v);
        }

        public bool IsAchievementClaimed(AchievementDef a) => Data.ClaimedAchievements.Contains(a.Id);

        public bool ClaimAchievement(AchievementDef a)
        {
            if (IsAchievementClaimed(a) || AchievementProgress(a) < a.Target) return false;
            Data.ClaimedAchievements.Add(a.Id);
            AddGems(a.Gems);
            return true;
        }

        /// <summary>Wie viele Belohnungen gerade abgeholt werden können (für das Abzeichen am Knopf).</summary>
        public int ClaimableCount
        {
            get
            {
                int n = 0;
                foreach (var q in DailyQuests)
                {
                    var def = Quests.GetDaily(q.Id);
                    if (def != null && !q.Claimed && q.Progress >= def.Target) n++;
                }
                foreach (var a in Quests.Achievements)
                {
                    if (!IsAchievementClaimed(a) && AchievementProgress(a) >= a.Target) n++;
                }
                return n;
            }
        }

        public void RecordStars(string levelId, int stars, Difficulty d)
        {
            var p = FindLevel(levelId);
            if (p == null)
            {
                p = new LevelProgress { Id = levelId };
                Data.Levels.Add(p);
            }
            switch (d)
            {
                case Difficulty.Leicht: p.EasyStars = Mathf.Max(p.EasyStars, stars); break;
                case Difficulty.Schwer: p.HardStars = Mathf.Max(p.HardStars, stars); break;
                default: p.BestStars = Mathf.Max(p.BestStars, stars); break;
            }
        }
    }
}
