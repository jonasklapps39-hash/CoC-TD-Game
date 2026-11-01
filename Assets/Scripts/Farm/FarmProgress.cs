using System;
using System.Collections.Generic;
using UnityEngine;

namespace MuhDefense
{
    /// <summary>Hindernis auf dem Hof (Stein, Busch, Baumstumpf, Pilze) – wegräumen bringt Diamanten und Erfahrung.</summary>
    public class ObstacleType
    {
        public string Id;
        public string Name;
        public Resource CostResource;
        public int Cost;
        public int Seconds;
        public int MinGems;
        public int MaxGems;
        public int Xp;

        public static readonly ObstacleType[] All =
        {
            new ObstacleType { Id = "busch", Name = "Busch", CostResource = Resource.Mais, Cost = 80, Seconds = 15, MinGems = 0, MaxGems = 2, Xp = 8 },
            new ObstacleType { Id = "pilze", Name = "Pilzkreis", CostResource = Resource.Milch, Cost = 100, Seconds = 20, MinGems = 1, MaxGems = 2, Xp = 10 },
            new ObstacleType { Id = "stein", Name = "Felsbrocken", CostResource = Resource.Milch, Cost = 250, Seconds = 45, MinGems = 1, MaxGems = 4, Xp = 18 },
            new ObstacleType { Id = "stumpf", Name = "Baumstumpf", CostResource = Resource.Mais, Cost = 300, Seconds = 60, MinGems = 2, MaxGems = 5, Xp = 22 },
        };

        public static ObstacleType Get(string id)
        {
            foreach (var t in All)
            {
                if (t.Id == id) return t;
            }
            return All[0];
        }
    }

    [Serializable]
    public class ObstacleData
    {
        public int Id;
        public string Type;
        public int X;
        public int Y;
        /// <summary>Zeitpunkt, an dem das Wegräumen fertig ist (0 = wird nicht geräumt).</summary>
        public double ClearEnd;

        public ObstacleType TypeDef => ObstacleType.Get(Type);
        public bool IsClearing => ClearEnd > 0;
    }

    /// <summary>Ergebnis, wenn etwas fertig wird (für Anzeigen auf dem Hof).</summary>
    public class FarmEvent
    {
        public Vector3 Position;
        public string Text;
        public Color Color;
    }

    /// <summary>Hindernisse, Bauernstufe (Hof-Erfahrung) und Belohnungen beim Aufstieg.</summary>
    public partial class FarmState
    {
        public const int MaxObstacles = 16;
        /// <summary>Alle zwei Stunden wächst ein neues Hindernis nach.</summary>
        const double ObstacleRegrowSeconds = 2 * 3600;

        /// <summary>Ereignisse seit dem letzten Abholen (fertig geräumte Hindernisse, Stufenaufstieg).</summary>
        public readonly List<FarmEvent> Events = new List<FarmEvent>();

        // ---------------------------------------------------------------- Hindernisse

        public ObstacleData ObstacleAt(Vector2Int cell) => Data.Obstacles.Find(o => o.X == cell.x && o.Y == cell.y);

        public int ObstaclesClearing
        {
            get
            {
                int n = 0;
                foreach (var o in Data.Obstacles)
                {
                    if (o.IsClearing) n++;
                }
                return n;
            }
        }

        public string WhyCannotClear(ObstacleData o)
        {
            var t = o.TypeDef;
            if (o.IsClearing) return "Wird gerade geräumt";
            if (!CanAfford(t.CostResource, t.Cost)) return $"Zu wenig {FarmBuildingType.ResourceName(t.CostResource)}";
            if (BuildersFree <= 0) return "Kein freier Knecht";
            return null;
        }

        public bool StartClearing(ObstacleData o)
        {
            if (WhyCannotClear(o) != null) return false;
            var t = o.TypeDef;
            Spend(t.CostResource, t.Cost);
            o.ClearEnd = Now + t.Seconds;
            return true;
        }

        public double ClearRemaining(ObstacleData o) => o.IsClearing ? Math.Max(0, o.ClearEnd - Now) : 0;

        public float ClearProgress(ObstacleData o)
        {
            if (!o.IsClearing) return 0f;
            return Mathf.Clamp01((float)(1.0 - ClearRemaining(o) / o.TypeDef.Seconds));
        }

        /// <summary>Fertig geräumte Hindernisse entfernen, Belohnung geben, neue nachwachsen lassen.</summary>
        void TickObstacles(double now)
        {
            for (int i = Data.Obstacles.Count - 1; i >= 0; i--)
            {
                var o = Data.Obstacles[i];
                if (!o.IsClearing || now < o.ClearEnd) continue;
                var t = o.TypeDef;
                var rng = new System.Random(o.Id * 7919 + (int)o.ClearEnd);
                int gems = rng.Next(t.MinGems, t.MaxGems + 1);
                Data.Obstacles.RemoveAt(i);
                AddGems(gems);
                Count(Counter.ObstaclesCleared);
                Events.Add(new FarmEvent
                {
                    Position = new Vector3(o.X + 0.5f, 1f, o.Y + 0.5f),
                    Text = gems > 0 ? $"{t.Name} weg! +{gems} Diamanten" : $"{t.Name} weg!",
                    Color = new Color(0.55f, 0.9f, 1f),
                });
                AddXp(t.Xp, new Vector3(o.X + 0.5f, 1.5f, o.Y + 0.5f));
            }

            if (Data.LastObstacleSpawn <= 0) Data.LastObstacleSpawn = now;
            while (now - Data.LastObstacleSpawn >= ObstacleRegrowSeconds)
            {
                Data.LastObstacleSpawn += ObstacleRegrowSeconds;
                if (Data.Obstacles.Count < MaxObstacles) SpawnObstacles(1, (int)Data.LastObstacleSpawn);
            }
        }

        /// <summary>Verteilt Hindernisse auf freie Felder (nicht direkt neben Gebäuden, damit Platz zum Bauen bleibt).</summary>
        public void SpawnObstacles(int count, int seed)
        {
            var rng = new System.Random(seed);
            for (int n = 0; n < count; n++)
            {
                for (int attempt = 0; attempt < 60; attempt++)
                {
                    int x = rng.Next(0, Width), y = rng.Next(0, Height);
                    if (!IsAreaFree(x, y, 1, -1) || NextToBuilding(x, y)) continue;
                    var t = ObstacleType.All[rng.Next(ObstacleType.All.Length)];
                    Data.Obstacles.Add(new ObstacleData { Id = Data.NextBuildingId++, Type = t.Id, X = x, Y = y });
                    break;
                }
            }
        }

        bool NextToBuilding(int x, int y)
        {
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    if (BuildingAt(new Vector2Int(x + dx, y + dy)) != null) return true;
                }
            }
            return false;
        }

        // ---------------------------------------------------------------- Bauernstufe

        public const int MaxFarmLevel = 30;

        /// <summary>Erfahrung, die von Stufe <paramref name="level"/> zur nächsten nötig ist.</summary>
        public static int XpToNext(int level) => 40 + 30 * (level - 1);

        public int FarmLevel => Mathf.Max(1, Data.FarmLevel);

        public float FarmXpProgress => FarmLevel >= MaxFarmLevel ? 1f : Mathf.Clamp01(Data.FarmXp / (float)XpToNext(FarmLevel));

        /// <summary>Diamanten beim Aufstieg auf diese Stufe.</summary>
        public static int LevelUpGems(int level) => 5 + level / 2;

        public void AddXp(int amount, Vector3 at)
        {
            if (amount <= 0 || FarmLevel >= MaxFarmLevel) return;
            amount = Mathf.RoundToInt(amount * (1f + DecoBonus(Resource.Mais, true)));
            if (Data.FarmLevel < 1) Data.FarmLevel = 1;
            Data.FarmXp += amount;
            Events.Add(new FarmEvent { Position = at, Text = $"+{amount} EP", Color = new Color(0.75f, 1f, 0.5f) });
            while (Data.FarmLevel < MaxFarmLevel && Data.FarmXp >= XpToNext(Data.FarmLevel))
            {
                Data.FarmXp -= XpToNext(Data.FarmLevel);
                Data.FarmLevel++;
                int gems = LevelUpGems(Data.FarmLevel);
                AddGems(gems);
                Events.Add(new FarmEvent
                {
                    Position = new Vector3(Width / 2f, 2f, Height / 2f),
                    Text = $"Bauernstufe {Data.FarmLevel}! +{gems} Diamanten",
                    Color = UiKit.Gold,
                });
                LevelUpPending = true;
            }
        }

        /// <summary>Wird gesetzt, wenn die Bauernstufe gestiegen ist (für Sound/Effekt auf dem Hof).</summary>
        public bool LevelUpPending;

        /// <summary>Erfahrung für ein fertiges Gebäude (Neubau oder Stufe), richtet sich nach den Kosten.</summary>
        public static int XpForBuilding(FarmBuildingType type, int level)
        {
            if (type.Costs == null || type.Costs.Length == 0) return 10 + 10 * level;
            return Mathf.Max(5, type.CostForLevel(level) / 25);
        }
    }
}
