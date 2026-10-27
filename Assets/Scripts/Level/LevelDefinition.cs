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

    /// <summary>
    /// Alle Daten eines Levels. Später als ScriptableObject im Editor bearbeitbar;
    /// für den Prototyp reicht eine Definition im Code.
    /// </summary>
    public class LevelDefinition
    {
        public string Name;
        public int Width;
        public int Height;
        /// <summary>Eckpunkte des Weges im Raster. Der erste liegt außerhalb der Karte (Eingang der Aliens), der letzte auf der Weide.</summary>
        public Vector2Int[] Waypoints;
        public RectInt Pasture;
        public int StartMoney;
        public int CowCount;
        public WaveDefinition[] Waves;
        /// <summary>Leben der Aliens steigt pro Welle um diesen Anteil.</summary>
        public float HpGrowthPerWave = 0.08f;
    }

    public static class Levels
    {
        public static LevelDefinition Level1()
        {
            const EnemyKind G = EnemyKind.Gruenling;
            const EnemyKind F = EnemyKind.Flitzer;
            const EnemyKind B = EnemyKind.Brocken;

            return new LevelDefinition
            {
                Name = "Heimattal 1 – Heinz' Weide",
                Width = 16,
                Height = 9,
                Waypoints = new[]
                {
                    new Vector2Int(-1, 6),
                    new Vector2Int(3, 6),
                    new Vector2Int(3, 2),
                    new Vector2Int(7, 2),
                    new Vector2Int(7, 6),
                    new Vector2Int(11, 6),
                    new Vector2Int(11, 3),
                    new Vector2Int(13, 3),
                },
                Pasture = new RectInt(13, 2, 3, 3),
                StartMoney = 250,
                CowCount = 10,
                Waves = new[]
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
                },
            };
        }
    }
}
