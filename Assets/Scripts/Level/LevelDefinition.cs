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

    public enum MapTheme
    {
        Heimattal,
        Maisfeld,
        Absturzstelle,
    }

    /// <summary>Ein Gebiet auf der Weltkarte (Heimattal, Maisfeld-Labyrinth …).</summary>
    public class Region
    {
        public string Name;
        public MapTheme Theme;
        /// <summary>So viele Sterne braucht man insgesamt, um das Gebiet zu betreten.</summary>
        public int UnlockStars;
        public string Story;
        /// <summary>Symbol auf der Weltkarte.</summary>
        public string Icon;
        public string BossIcon;
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
        /// <summary>Index in <see cref="Levels.Regions"/>.</summary>
        public int Region;
        /// <summary>Felder mit Hindernissen (Maisstauden, Kristalle): dort kann man nicht bauen.</summary>
        public RectInt[] Corn = new RectInt[0];
        /// <summary>Text im Start-Fenster (leer = Standardtext).</summary>
        public string Story = "";
        /// <summary>Letztes Level des Gebiets (Boss-Symbol auf der Weltkarte).</summary>
        public bool BossLevel;

        public MapTheme Theme => Levels.Regions[Region].Theme;

        public bool IsCorn(Vector2Int c)
        {
            foreach (var r in Corn)
            {
                if (r.Contains(c)) return true;
            }
            return false;
        }
    }

    public static class Levels
    {
        const EnemyKind G = EnemyKind.Gruenling;
        const EnemyKind F = EnemyKind.Flitzer;
        const EnemyKind B = EnemyKind.Brocken;
        const EnemyKind S = EnemyKind.Schleicher;
        const EnemyKind U = EnemyKind.Ufo;
        const EnemyKind M = EnemyKind.Sanitaeter;
        const EnemyKind T = EnemyKind.Teiler;
        const EnemyKind Q = EnemyKind.SchildAlien;

        public static readonly Region[] Regions =
        {
            new Region
            {
                Name = "Heimattal", Theme = MapTheme.Heimattal, UnlockStars = 0, Icon = "KuhUfo", BossIcon = "Boss",
                Story = "Die Aliens kommen wieder – diesmal stärker und auf einem neuen Weg. Beschütze die Kühe!",
            },
            new Region
            {
                Name = "Maisfeld-Labyrinth", Theme = MapTheme.Maisfeld, UnlockStars = 12, Icon = "Teiler", BossIcon = "ErnteRoboter",
                Story = "Gorg ist geschlagen, doch die Aliens haben sich im riesigen Maisfeld von Nachbar Kalle verschanzt. " +
                        "Zwischen den hohen Stauden kannst du nicht bauen – nutze die Lücken klug!",
            },
            new Region
            {
                Name = "Absturzstelle", Theme = MapTheme.Absturzstelle, UnlockStars = 26, Icon = "SchildAlien", BossIcon = "AlienKaiser",
                Story = "Hinter dem Maisfeld liegt die Absturzstelle des Mutterschiffs. Zwischen leuchtenden Kristallen bereitet " +
                        "Alien-Kaiser Zorbax den großen Milchraub vor. Seine Schild-Aliens sind zäh – zeig ihnen, wer hier der Bauer ist!",
            },
        };

        /// <summary>Fügt einzelnen Wellen (1 = erste Welle) zusätzliche Alien-Gruppen hinzu.</summary>
        static WaveDefinition[] With(WaveDefinition[] waves, params (int wave, SpawnGroup group)[] extras)
        {
            foreach (var (wave, group) in extras)
            {
                var w = waves[wave - 1];
                var groups = new SpawnGroup[w.Groups.Length + 1];
                w.Groups.CopyTo(groups, 0);
                groups[groups.Length - 1] = group;
                waves[wave - 1] = new WaveDefinition(w.Bonus, groups);
            }
            return waves;
        }

        public static readonly LevelDefinition[] All =
        {
            Level1(), Level2(), Level3(), Level4(), Level5(),
            Mais1(), Mais2(), Mais3(), Mais4(), Mais5(),
            Crash1(), Crash2(), Crash3(), Crash4(), Crash5(),
        };

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
            Waves = With(StandardWaves(),
                (5, new SpawnGroup(S, 4, 1.2f, 6f)),
                (7, new SpawnGroup(S, 6, 1f, 3f)),
                (9, new SpawnGroup(S, 8, 0.8f, 5f))),
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
            Waves = With(StandardWaves(),
                (4, new SpawnGroup(S, 5, 1f, 4f)),
                (5, new SpawnGroup(U, 3, 2f, 4f)),
                (7, new SpawnGroup(U, 5, 1.5f, 2f)),
                (8, new SpawnGroup(S, 8, 0.8f, 6f)),
                (9, new SpawnGroup(U, 6, 1.2f, 8f)),
                (10, new SpawnGroup(U, 4, 1.5f, 3f))),
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
            Waves = With(ExtendedWaves(),
                (4, new SpawnGroup(M, 2, 3f, 4f)),
                (5, new SpawnGroup(S, 6, 1f, 3f)),
                (6, new SpawnGroup(M, 3, 2.5f, 2f)),
                (6, new SpawnGroup(U, 5, 1.4f, 6f)),
                (8, new SpawnGroup(M, 4, 2f, 5f)),
                (9, new SpawnGroup(S, 10, 0.7f, 4f)),
                (10, new SpawnGroup(U, 8, 1f, 5f)),
                (11, new SpawnGroup(M, 5, 1.5f, 3f))),
            HpMultiplier = 2.0f,
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
            Waves = With(ExtendedWaves(finalBosses: 2),
                (3, new SpawnGroup(S, 6, 1f, 3f)),
                (4, new SpawnGroup(U, 4, 1.5f, 4f)),
                (5, new SpawnGroup(M, 3, 2f, 3f)),
                (7, new SpawnGroup(S, 10, 0.7f, 2f)),
                (8, new SpawnGroup(U, 8, 1f, 4f)),
                (9, new SpawnGroup(M, 5, 1.5f, 6f)),
                (10, new SpawnGroup(U, 10, 0.8f, 2f)),
                (11, new SpawnGroup(S, 14, 0.5f, 6f)),
                (12, new SpawnGroup(M, 6, 1.5f, 8f))),
            HpMultiplier = 2.4f,
            RewardCorn = 1200,
            RewardMilk = 1200,
            BossLevel = true,
        };

        // ---------------------------------------------------------------- Gebiet 2: Maisfeld-Labyrinth

        static LevelDefinition Mais1() => new LevelDefinition
        {
            Id = "maisfeld-1",
            Name = "Maisfeld 1 – Kalles Acker",
            Region = 1,
            Width = 16,
            Height = 9,
            Waypoints = new[]
            {
                new Vector2Int(-1, 7), new Vector2Int(5, 7), new Vector2Int(5, 1), new Vector2Int(10, 1),
                new Vector2Int(10, 5), new Vector2Int(13, 5),
            },
            Corn = new[] { new RectInt(1, 1, 3, 4), new RectInt(7, 3, 2, 5), new RectInt(11, 7, 4, 2) },
            Pasture = new RectInt(13, 3, 3, 3),
            StartMoney = 550,
            CowCount = 10,
            Waves = With(ExtendedWaves(),
                (2, new SpawnGroup(T, 3, 2f, 3f)),
                (4, new SpawnGroup(T, 5, 1.5f, 2f)),
                (5, new SpawnGroup(S, 6, 1f, 3f)),
                (6, new SpawnGroup(U, 5, 1.4f, 4f)),
                (7, new SpawnGroup(T, 8, 1.2f, 3f)),
                (9, new SpawnGroup(M, 4, 2f, 5f)),
                (10, new SpawnGroup(T, 10, 1f, 4f)),
                (11, new SpawnGroup(S, 10, 0.7f, 3f))),
            HpMultiplier = 2.7f,
            RewardCorn = 1400,
            RewardMilk = 1400,
        };

        static LevelDefinition Mais2() => new LevelDefinition
        {
            Id = "maisfeld-2",
            Name = "Maisfeld 2 – Die Vogelscheuche",
            Region = 1,
            Width = 16,
            Height = 9,
            Waypoints = new[]
            {
                new Vector2Int(-1, 2), new Vector2Int(3, 2), new Vector2Int(3, 7), new Vector2Int(7, 7),
                new Vector2Int(7, 2), new Vector2Int(11, 2), new Vector2Int(11, 7), new Vector2Int(12, 7),
                new Vector2Int(12, 4), new Vector2Int(13, 4),
            },
            Corn = new[] { new RectInt(5, 1, 1, 5), new RectInt(9, 3, 1, 4), new RectInt(0, 4, 2, 4) },
            Pasture = new RectInt(13, 3, 3, 3),
            StartMoney = 575,
            CowCount = 10,
            Waves = With(ExtendedWaves(),
                (2, new SpawnGroup(T, 4, 1.8f, 2f)),
                (3, new SpawnGroup(U, 4, 1.5f, 3f)),
                (5, new SpawnGroup(T, 6, 1.2f, 4f)),
                (6, new SpawnGroup(M, 3, 2f, 3f)),
                (8, new SpawnGroup(U, 8, 1f, 3f)),
                (9, new SpawnGroup(T, 10, 1f, 6f)),
                (11, new SpawnGroup(U, 10, 0.8f, 2f)),
                (12, new SpawnGroup(T, 12, 0.8f, 4f))),
            HpMultiplier = 2.9f,
            RewardCorn = 1600,
            RewardMilk = 1600,
        };

        static LevelDefinition Mais3() => new LevelDefinition
        {
            Id = "maisfeld-3",
            Name = "Maisfeld 3 – Der Irrgarten",
            Region = 1,
            Width = 16,
            Height = 9,
            Waypoints = new[]
            {
                new Vector2Int(-1, 1), new Vector2Int(2, 1), new Vector2Int(2, 7), new Vector2Int(4, 7),
                new Vector2Int(4, 1), new Vector2Int(6, 1), new Vector2Int(6, 7), new Vector2Int(8, 7),
                new Vector2Int(8, 1), new Vector2Int(10, 1), new Vector2Int(10, 7), new Vector2Int(12, 7),
                new Vector2Int(12, 4), new Vector2Int(13, 4),
            },
            Corn = new[]
            {
                new RectInt(3, 3, 1, 3), new RectInt(5, 3, 1, 3), new RectInt(7, 3, 1, 3),
                new RectInt(9, 3, 1, 3), new RectInt(11, 2, 1, 4),
            },
            Pasture = new RectInt(13, 3, 3, 3),
            StartMoney = 600,
            CowCount = 10,
            Waves = With(ExtendedWaves(),
                (3, new SpawnGroup(T, 5, 1.5f, 3f)),
                (4, new SpawnGroup(S, 8, 0.8f, 2f)),
                (5, new SpawnGroup(M, 3, 2f, 4f)),
                (6, new SpawnGroup(T, 8, 1.2f, 3f)),
                (8, new SpawnGroup(S, 12, 0.6f, 3f)),
                (9, new SpawnGroup(M, 5, 1.6f, 4f)),
                (10, new SpawnGroup(T, 12, 0.9f, 2f)),
                (12, new SpawnGroup(M, 6, 1.4f, 6f))),
            HpMultiplier = 3.15f,
            RewardCorn = 1800,
            RewardMilk = 1800,
        };

        static LevelDefinition Mais4() => new LevelDefinition
        {
            Id = "maisfeld-4",
            Name = "Maisfeld 4 – Die alte Scheune",
            Region = 1,
            Width = 16,
            Height = 9,
            Waypoints = new[]
            {
                new Vector2Int(-1, 4), new Vector2Int(2, 4), new Vector2Int(2, 7), new Vector2Int(6, 7),
                new Vector2Int(6, 4), new Vector2Int(9, 4), new Vector2Int(9, 1), new Vector2Int(12, 1),
                new Vector2Int(12, 4), new Vector2Int(13, 4),
            },
            Corn = new[] { new RectInt(0, 0, 5, 2), new RectInt(3, 5, 2, 1), new RectInt(7, 6, 5, 2), new RectInt(10, 2, 1, 2) },
            Pasture = new RectInt(13, 3, 3, 3),
            StartMoney = 650,
            CowCount = 10,
            Waves = With(ExtendedWaves(),
                (2, new SpawnGroup(T, 6, 1.4f, 2f)),
                (4, new SpawnGroup(U, 6, 1.2f, 3f)),
                (5, new SpawnGroup(T, 8, 1f, 3f)),
                (6, new SpawnGroup(S, 10, 0.7f, 4f)),
                (7, new SpawnGroup(M, 4, 2f, 3f)),
                (8, new SpawnGroup(T, 12, 0.8f, 3f)),
                (10, new SpawnGroup(U, 12, 0.7f, 2f)),
                (11, new SpawnGroup(T, 14, 0.7f, 5f)),
                (12, new SpawnGroup(M, 6, 1.4f, 4f))),
            HpMultiplier = 3.45f,
            RewardCorn = 2100,
            RewardMilk = 2100,
        };

        static LevelDefinition Mais5() => new LevelDefinition
        {
            Id = "maisfeld-5",
            Name = "Maisfeld 5 – Der Ernte-Roboter",
            Region = 1,
            Width = 16,
            Height = 9,
            Waypoints = new[]
            {
                new Vector2Int(-1, 6), new Vector2Int(4, 6), new Vector2Int(4, 2), new Vector2Int(8, 2),
                new Vector2Int(8, 6), new Vector2Int(11, 6), new Vector2Int(11, 2), new Vector2Int(12, 2),
                new Vector2Int(12, 4), new Vector2Int(13, 4),
            },
            Corn = new[] { new RectInt(0, 1, 3, 4), new RectInt(5, 3, 2, 3), new RectInt(9, 0, 4, 2), new RectInt(9, 7, 4, 2) },
            Pasture = new RectInt(13, 3, 3, 3),
            StartMoney = 700,
            CowCount = 10,
            Waves = With(ExtendedWaves(finalBosses: 1, boss: EnemyKind.ErnteRoboter),
                (3, new SpawnGroup(T, 6, 1.2f, 3f)),
                (4, new SpawnGroup(U, 6, 1.2f, 4f)),
                (5, new SpawnGroup(M, 4, 2f, 3f)),
                (6, new SpawnGroup(T, 10, 0.9f, 2f)),
                (7, new SpawnGroup(S, 12, 0.6f, 2f)),
                (8, new SpawnGroup(U, 10, 0.9f, 4f)),
                (9, new SpawnGroup(EnemyKind.Boss, 1, 0f, 10f)),
                (10, new SpawnGroup(T, 16, 0.6f, 2f)),
                (11, new SpawnGroup(M, 6, 1.4f, 6f)),
                (12, new SpawnGroup(T, 12, 0.8f, 8f))),
            HpMultiplier = 3.8f,
            RewardCorn = 2500,
            RewardMilk = 2500,
            BossLevel = true,
        };

        // ---------------------------------------------------------------- Gebiet 3: Absturzstelle

        static LevelDefinition Crash1() => new LevelDefinition
        {
            Id = "absturz-1",
            Name = "Absturzstelle 1 – Der Krater",
            Region = 2,
            Width = 16,
            Height = 9,
            Waypoints = new[]
            {
                new Vector2Int(-1, 4), new Vector2Int(3, 4), new Vector2Int(3, 1), new Vector2Int(7, 1),
                new Vector2Int(7, 7), new Vector2Int(11, 7), new Vector2Int(11, 3), new Vector2Int(13, 3),
            },
            Corn = new[] { new RectInt(4, 3, 2, 3), new RectInt(8, 2, 2, 4), new RectInt(0, 6, 3, 2) },
            Pasture = new RectInt(13, 3, 3, 3),
            StartMoney = 750,
            CowCount = 10,
            Waves = With(ExtendedWaves(),
                (2, new SpawnGroup(Q, 3, 2f, 3f)),
                (3, new SpawnGroup(T, 5, 1.4f, 2f)),
                (4, new SpawnGroup(Q, 5, 1.5f, 2f)),
                (6, new SpawnGroup(U, 6, 1.2f, 3f)),
                (7, new SpawnGroup(Q, 8, 1.1f, 3f)),
                (8, new SpawnGroup(M, 4, 2f, 4f)),
                (10, new SpawnGroup(Q, 10, 0.9f, 3f)),
                (11, new SpawnGroup(T, 10, 0.9f, 5f))),
            HpMultiplier = 4.1f,
            RewardCorn = 2800,
            RewardMilk = 2800,
        };

        static LevelDefinition Crash2() => new LevelDefinition
        {
            Id = "absturz-2",
            Name = "Absturzstelle 2 – Das Wrack",
            Region = 2,
            Width = 16,
            Height = 9,
            Waypoints = new[]
            {
                new Vector2Int(-1, 1), new Vector2Int(5, 1), new Vector2Int(5, 4), new Vector2Int(1, 4),
                new Vector2Int(1, 7), new Vector2Int(9, 7), new Vector2Int(9, 2), new Vector2Int(12, 2),
                new Vector2Int(12, 4), new Vector2Int(13, 4),
            },
            Corn = new[] { new RectInt(2, 5, 2, 1), new RectInt(6, 3, 2, 3), new RectInt(10, 4, 2, 2), new RectInt(6, 0, 3, 1) },
            Pasture = new RectInt(13, 3, 3, 3),
            StartMoney = 780,
            CowCount = 10,
            Waves = With(ExtendedWaves(),
                (2, new SpawnGroup(Q, 4, 1.8f, 2f)),
                (3, new SpawnGroup(S, 6, 1f, 3f)),
                (5, new SpawnGroup(Q, 6, 1.2f, 2f)),
                (6, new SpawnGroup(U, 8, 1f, 3f)),
                (7, new SpawnGroup(T, 8, 1f, 4f)),
                (9, new SpawnGroup(Q, 12, 0.8f, 2f)),
                (10, new SpawnGroup(M, 5, 1.6f, 3f)),
                (12, new SpawnGroup(Q, 12, 0.8f, 6f))),
            HpMultiplier = 4.4f,
            RewardCorn = 3100,
            RewardMilk = 3100,
        };

        static LevelDefinition Crash3() => new LevelDefinition
        {
            Id = "absturz-3",
            Name = "Absturzstelle 3 – Kristallhöhle",
            Region = 2,
            Width = 16,
            Height = 9,
            Waypoints = new[]
            {
                new Vector2Int(-1, 7), new Vector2Int(2, 7), new Vector2Int(2, 1), new Vector2Int(5, 1),
                new Vector2Int(5, 7), new Vector2Int(8, 7), new Vector2Int(8, 1), new Vector2Int(11, 1),
                new Vector2Int(11, 6), new Vector2Int(12, 6), new Vector2Int(12, 4), new Vector2Int(13, 4),
            },
            Corn = new[] { new RectInt(3, 3, 1, 3), new RectInt(6, 2, 1, 4), new RectInt(9, 3, 1, 4), new RectInt(0, 0, 2, 4) },
            Pasture = new RectInt(13, 3, 3, 3),
            StartMoney = 820,
            CowCount = 10,
            Waves = With(ExtendedWaves(),
                (2, new SpawnGroup(S, 6, 1f, 2f)),
                (3, new SpawnGroup(Q, 6, 1.4f, 3f)),
                (5, new SpawnGroup(M, 4, 2f, 3f)),
                (6, new SpawnGroup(Q, 8, 1f, 2f)),
                (7, new SpawnGroup(S, 12, 0.6f, 3f)),
                (8, new SpawnGroup(T, 10, 0.9f, 3f)),
                (10, new SpawnGroup(Q, 14, 0.7f, 2f)),
                (11, new SpawnGroup(U, 12, 0.7f, 4f))),
            HpMultiplier = 4.7f,
            RewardCorn = 3400,
            RewardMilk = 3400,
        };

        static LevelDefinition Crash4() => new LevelDefinition
        {
            Id = "absturz-4",
            Name = "Absturzstelle 4 – Der Leitstrahl",
            Region = 2,
            Width = 16,
            Height = 9,
            Waypoints = new[]
            {
                new Vector2Int(-1, 4), new Vector2Int(2, 4), new Vector2Int(2, 0), new Vector2Int(6, 0),
                new Vector2Int(6, 7), new Vector2Int(10, 7), new Vector2Int(10, 1), new Vector2Int(12, 1),
                new Vector2Int(12, 4), new Vector2Int(13, 4),
            },
            Corn = new[] { new RectInt(3, 2, 2, 4), new RectInt(7, 2, 2, 3), new RectInt(0, 6, 2, 3), new RectInt(13, 7, 3, 2) },
            Pasture = new RectInt(13, 3, 3, 3),
            StartMoney = 860,
            CowCount = 10,
            Waves = With(ExtendedWaves(),
                (2, new SpawnGroup(Q, 6, 1.4f, 2f)),
                (3, new SpawnGroup(U, 6, 1.2f, 3f)),
                (4, new SpawnGroup(T, 8, 1f, 2f)),
                (6, new SpawnGroup(Q, 10, 0.9f, 3f)),
                (7, new SpawnGroup(M, 5, 1.6f, 4f)),
                (8, new SpawnGroup(S, 14, 0.5f, 3f)),
                (9, new SpawnGroup(EnemyKind.ErnteRoboter, 1, 0f, 10f)),
                (11, new SpawnGroup(Q, 16, 0.6f, 4f)),
                (12, new SpawnGroup(T, 14, 0.7f, 6f))),
            HpMultiplier = 5.0f,
            RewardCorn = 3700,
            RewardMilk = 3700,
        };

        static LevelDefinition Crash5() => new LevelDefinition
        {
            Id = "absturz-5",
            Name = "Absturzstelle 5 – Das Mutterschiff",
            Region = 2,
            Width = 16,
            Height = 9,
            Waypoints = new[]
            {
                new Vector2Int(-1, 1), new Vector2Int(4, 1), new Vector2Int(4, 7), new Vector2Int(8, 7),
                new Vector2Int(8, 1), new Vector2Int(12, 1), new Vector2Int(12, 4), new Vector2Int(13, 4),
            },
            Corn = new[] { new RectInt(1, 3, 2, 4), new RectInt(5, 2, 2, 3), new RectInt(9, 3, 2, 4) },
            Pasture = new RectInt(13, 3, 3, 3),
            StartMoney = 900,
            CowCount = 10,
            Waves = With(ExtendedWaves(finalBosses: 1, boss: EnemyKind.Kaiser),
                (2, new SpawnGroup(Q, 6, 1.2f, 2f)),
                (3, new SpawnGroup(T, 8, 1f, 3f)),
                (4, new SpawnGroup(U, 8, 1f, 3f)),
                (5, new SpawnGroup(Q, 10, 0.9f, 2f)),
                (6, new SpawnGroup(EnemyKind.Boss, 1, 0f, 8f)),
                (7, new SpawnGroup(M, 6, 1.4f, 3f)),
                (8, new SpawnGroup(Q, 14, 0.7f, 2f)),
                (9, new SpawnGroup(EnemyKind.ErnteRoboter, 1, 0f, 6f)),
                (10, new SpawnGroup(S, 16, 0.5f, 3f)),
                (11, new SpawnGroup(Q, 18, 0.5f, 4f)),
                (12, new SpawnGroup(Q, 12, 0.8f, 8f))),
            HpMultiplier = 5.4f,
            RewardCorn = 4000,
            RewardMilk = 4000,
            BossLevel = true,
        };

        /// <summary>12 Wellen: die Standard-Wellen ohne Boss-Welle, dann zwei harte Wellen und das Finale.</summary>
        static WaveDefinition[] ExtendedWaves(int finalBosses = 1, EnemyKind boss = EnemyKind.Boss)
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
                new SpawnGroup(boss, finalBosses, 6f, 12f));
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
