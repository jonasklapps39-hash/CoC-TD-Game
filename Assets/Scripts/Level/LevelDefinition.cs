using UnityEngine;

namespace MuhDefense
{
    /// <summary>Eine Gruppe gleicher Aliens innerhalb einer Welle.</summary>
    public class SpawnGroup
    {
        public EnemyKind Kind;
        public int Count;
        /// <summary>Sekunden zwischen zwei Aliens dieser Gruppe.</summary>
        public float Interval;
        /// <summary>Sekunden nach Wellenstart, bevor die Gruppe losläuft.</summary>
        public float StartDelay;

        public SpawnGroup(EnemyKind kind, int count, float interval, float startDelay = 0f)
        {
            Kind = kind;
            Count = count;
            Interval = interval;
            StartDelay = startDelay;
        }
    }

    public class WaveDefinition
    {
        public SpawnGroup[] Groups;
        /// <summary>Münzen, die man nach der Welle zusätzlich bekommt.</summary>
        public int Bonus;

        public WaveDefinition(int bonus, params SpawnGroup[] groups)
        {
            Bonus = bonus;
            Groups = groups;
        }
    }

    /// <summary>Tutorial: ein Turm, den man in diesem Level vor einer bestimmten Welle geschenkt bekommt.</summary>
    public class TowerGift
    {
        /// <summary>Der Turm kommt in der Bauphase vor dieser Welle (1 = vor der ersten Welle).</summary>
        public int BeforeWave;
        public TowerType Tower;
        /// <summary>Bis zu dieser Stufe darf der geschenkte Turm verbessert werden.</summary>
        public int MaxLevel = 2;

        public TowerGift(int beforeWave, TowerType tower)
        {
            BeforeWave = beforeWave;
            Tower = tower;
        }
    }

    /// <summary>
    /// Alle Daten eines Levels. Später als ScriptableObject im Editor bearbeitbar;
    /// für den Prototyp reicht eine Definition im Code.
    /// </summary>
    public class LevelDefinition
    {
        /// <summary>Eindeutige Kennung, unter der die Sterne gespeichert werden.</summary>
        public string Id;
        public string Name;
        public int Width;
        public int Height;
        /// <summary>Eckpunkte des Weges im Raster. Der erste liegt außerhalb der Karte (Eingang der Aliens), der letzte auf der Weide.</summary>
        public Vector2Int[] Waypoints;
        public RectInt Pasture;
        public int StartMoney;
        public int CowCount;
        public WaveDefinition[] Waves;
        /// <summary>Grundfaktor für das Leben aller Aliens (Schwierigkeit des Levels).</summary>
        public float HpMultiplier = 1f;
        /// <summary>Leben der Aliens steigt pro Welle um diesen Anteil.</summary>
        public float HpGrowthPerWave = 0.08f;
        /// <summary>Belohnung für den Hof bei 3 Sternen (bei weniger Sternen anteilig).</summary>
        public int RewardCorn;
        public int RewardMilk;
        /// <summary>Türme, die man in diesem Level geschenkt bekommt (nur im Tutorial-Level).</summary>
        public TowerGift[] TowerGifts = new TowerGift[0];
    }

    public static class Levels
    {
        const EnemyKind G = EnemyKind.Gruenling;
        const EnemyKind F = EnemyKind.Flitzer;
        const EnemyKind B = EnemyKind.Brocken;

        public static readonly LevelDefinition[] All = { Level1(), Level2(), Level3(), Level4(), Level5() };

        static LevelDefinition Level1() => new LevelDefinition
        {
            Id = "heimattal-1",
            Name = "Heimattal 1 – Heinz' Weide",
            Width = 16,
            Height = 9,
            Waypoints = new[]
            {
                new Vector2Int(-1, 6), new Vector2Int(3, 6), new Vector2Int(3, 2), new Vector2Int(7, 2),
                new Vector2Int(7, 6), new Vector2Int(11, 6), new Vector2Int(11, 3), new Vector2Int(13, 3),
            },
            Pasture = new RectInt(13, 2, 3, 3),
            StartMoney = 250,
            CowCount = 10,
            Waves = StandardWaves(),
            RewardCorn = 300,
            RewardMilk = 300,
            TowerGifts = new[]
            {
                new TowerGift(3, TowerType.Heuballen),
                new TowerGift(6, TowerType.Eismilch),
            },
        };

        static LevelDefinition Level2() => new LevelDefinition
        {
            Id = "heimattal-2",
            Name = "Heimattal 2 – Am Maisfeld",
            Width = 16,
            Height = 9,
            Waypoints = new[]
            {
                new Vector2Int(-1, 1), new Vector2Int(4, 1), new Vector2Int(4, 7), new Vector2Int(9, 7),
                new Vector2Int(9, 1), new Vector2Int(12, 1), new Vector2Int(12, 4), new Vector2Int(13, 4),
            },
            Pasture = new RectInt(13, 3, 3, 3),
            StartMoney = 280,
            CowCount = 10,
            Waves = StandardWaves(),
            HpMultiplier = 1.35f,
            RewardCorn = 450,
            RewardMilk = 450,
        };

        static LevelDefinition Level3() => new LevelDefinition
        {
            Id = "heimattal-3",
            Name = "Heimattal 3 – Die Mühle",
            Width = 16,
            Height = 9,
            Waypoints = new[]
            {
                new Vector2Int(-1, 4), new Vector2Int(2, 4), new Vector2Int(2, 7), new Vector2Int(6, 7),
                new Vector2Int(6, 1), new Vector2Int(10, 1), new Vector2Int(10, 6), new Vector2Int(12, 6),
                new Vector2Int(12, 4), new Vector2Int(13, 4),
            },
            Pasture = new RectInt(13, 3, 3, 3),
            StartMoney = 320,
            CowCount = 10,
            Waves = StandardWaves(),
            HpMultiplier = 1.8f,
            RewardCorn = 650,
            RewardMilk = 650,
        };

        static LevelDefinition Level4() => new LevelDefinition
        {
            Id = "heimattal-4",
            Name = "Heimattal 4 – Der Hühnerhof",
            Width = 16,
            Height = 9,
            Waypoints = new[]
            {
                new Vector2Int(-1, 2), new Vector2Int(2, 2), new Vector2Int(2, 7), new Vector2Int(5, 7),
                new Vector2Int(5, 1), new Vector2Int(8, 1), new Vector2Int(8, 7), new Vector2Int(11, 7),
                new Vector2Int(11, 2), new Vector2Int(12, 2), new Vector2Int(12, 4), new Vector2Int(13, 4),
            },
            Pasture = new RectInt(13, 3, 3, 3),
            StartMoney = 400,
            CowCount = 10,
            Waves = ExtendedWaves(),
            HpMultiplier = 2.2f,
            RewardCorn = 900,
            RewardMilk = 900,
        };

        static LevelDefinition Level5() => new LevelDefinition
        {
            Id = "heimattal-5",
            Name = "Heimattal 5 – Gorgs Rückkehr",
            Width = 16,
            Height = 9,
            Waypoints = new[]
            {
                new Vector2Int(-1, 4), new Vector2Int(4, 4), new Vector2Int(4, 1), new Vector2Int(9, 1),
                new Vector2Int(9, 7), new Vector2Int(12, 7), new Vector2Int(12, 4), new Vector2Int(13, 4),
            },
            Pasture = new RectInt(13, 3, 3, 3),
            StartMoney = 450,
            CowCount = 10,
            Waves = ExtendedWaves(finalBosses: 2),
            HpMultiplier = 2.7f,
            RewardCorn = 1200,
            RewardMilk = 1200,
        };

        /// <summary>12 Wellen: die Standard-Wellen ohne Boss-Welle, dann zwei harte Wellen und das Finale.</summary>
        static WaveDefinition[] ExtendedWaves(int finalBosses = 1)
        {
            var standard = StandardWaves();
            var waves = new WaveDefinition[12];
            for (int i = 0; i < 9; i++) waves[i] = standard[i];
            waves[9] = new WaveDefinition(110, new SpawnGroup(B, 10, 1.4f), new SpawnGroup(F, 30, 0.25f, 3f));
            waves[10] = new WaveDefinition(120, new SpawnGroup(G, 30, 0.3f), new SpawnGroup(B, 12, 1.2f, 4f), new SpawnGroup(F, 20, 0.3f, 10f));
            waves[11] = new WaveDefinition(0,
                new SpawnGroup(G, 25, 0.4f),
                new SpawnGroup(F, 25, 0.3f, 4f),
                new SpawnGroup(B, 12, 1.5f, 6f),
                new SpawnGroup(EnemyKind.Boss, finalBosses, 6f, 12f));
            return waves;
        }

        static WaveDefinition[] StandardWaves() => new[]
        {
            new WaveDefinition(40, new SpawnGroup(G, 8, 1.2f)),
            new WaveDefinition(45, new SpawnGroup(G, 12, 0.9f)),
            new WaveDefinition(50, new SpawnGroup(G, 10, 0.9f), new SpawnGroup(F, 6, 0.7f, 5f)),
            new WaveDefinition(55, new SpawnGroup(F, 14, 0.5f), new SpawnGroup(G, 6, 1f, 3f)),
            new WaveDefinition(60, new SpawnGroup(B, 3, 3f), new SpawnGroup(G, 12, 0.8f, 1f)),
            new WaveDefinition(70, new SpawnGroup(G, 16, 0.5f), new SpawnGroup(F, 10, 0.4f, 6f)),
            new WaveDefinition(80, new SpawnGroup(B, 6, 2f), new SpawnGroup(F, 14, 0.45f, 4f)),
            new WaveDefinition(90, new SpawnGroup(G, 24, 0.4f), new SpawnGroup(B, 5, 2.5f, 3f)),
            new WaveDefinition(100, new SpawnGroup(F, 24, 0.3f), new SpawnGroup(B, 8, 1.8f, 2f), new SpawnGroup(G, 10, 0.5f, 8f)),
            new WaveDefinition(0,
                new SpawnGroup(G, 20, 0.5f),
                new SpawnGroup(F, 15, 0.4f, 5f),
                new SpawnGroup(B, 6, 2f, 8f),
                new SpawnGroup(EnemyKind.Boss, 1, 0f, 14f)),
        };
    }
}
