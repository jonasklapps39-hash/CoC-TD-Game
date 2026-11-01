using System;
using UnityEngine;

namespace MuhDefense
{
    /// <summary>Alien-Überfälle auf den Hof: alle paar Stunden Alarm, kurze Verteidigungsrunde, sonst klauen die Aliens Vorräte.</summary>
    public partial class FarmState
    {
        /// <summary>So lange hat man Zeit, den Hof zu verteidigen.</summary>
        public const double RaidWindowSeconds = 3600;
        /// <summary>Anteil der Vorräte, den die Aliens bei einem verlorenen oder ignorierten Überfall mitnehmen.</summary>
        public const float RaidTheft = 0.1f;

        /// <summary>Überfälle gibt es erst, wenn man Heimattal 3 geschafft hat.</summary>
        public bool RaidsUnlocked => BestStars("heimattal-3") > 0;

        public bool RaidActive
        {
            get
            {
                double now = Now;
                return Data.NextRaid > 0 && now >= Data.NextRaid && now < Data.NextRaid + RaidWindowSeconds;
            }
        }

        public double RaidSecondsLeft => Math.Max(0, Data.NextRaid + RaidWindowSeconds - Now);

        /// <summary>Stärke der Angreifer wächst mit den gesammelten Sternen.</summary>
        public int RaidStrength => Mathf.Clamp(TotalStars / 6, 0, 15);

        void TickRaid(double now)
        {
            if (!RaidsUnlocked) return;
            if (Data.NextRaid <= 0)
            {
                Data.NextRaid = now + 1800;
                return;
            }
            if (now >= Data.NextRaid + RaidWindowSeconds)
            {
                // Niemand hat verteidigt: die Aliens bedienen sich
                StealSupplies("Die Aliens waren da");
                ScheduleNextRaid(now);
            }
        }

        public void ScheduleNextRaid(double now)
        {
            var rng = new System.Random((int)(now % 100000));
            Data.NextRaid = now + 4 * 3600 + rng.Next(0, 2 * 3600);
        }

        /// <summary>Die Aliens nehmen einen Teil von Mais und Milch mit.</summary>
        public void StealSupplies(string reason)
        {
            int corn = Mathf.FloorToInt((float)Data.Corn * RaidTheft);
            int milk = Mathf.FloorToInt((float)Data.Milk * RaidTheft);
            Data.Corn -= corn;
            Data.Milk -= milk;
            Events.Add(new FarmEvent
            {
                Position = new Vector3(Width / 2f, 2.5f, Height / 2f),
                Text = $"{reason}: -{corn} Mais, -{milk} Milch!",
                Color = new Color(1f, 0.45f, 0.35f),
            });
        }
    }

    public static class Raids
    {
        /// <summary>Drei Hof-Karten für Überfälle; welche, entscheidet der Zufall.</summary>
        static readonly (Vector2Int[] path, RectInt[] buildings)[] Layouts =
        {
            (new[] { new Vector2Int(-1, 4), new Vector2Int(4, 4), new Vector2Int(4, 1), new Vector2Int(9, 1), new Vector2Int(9, 7),
                     new Vector2Int(12, 7), new Vector2Int(12, 4), new Vector2Int(13, 4) },
             new[] { new RectInt(1, 6, 2, 2), new RectInt(6, 3, 2, 2), new RectInt(10, 2, 2, 2), new RectInt(6, 6, 2, 2) }),
            (new[] { new Vector2Int(-1, 1), new Vector2Int(3, 1), new Vector2Int(3, 6), new Vector2Int(7, 6), new Vector2Int(7, 2),
                     new Vector2Int(11, 2), new Vector2Int(11, 4), new Vector2Int(13, 4) },
             new[] { new RectInt(0, 3, 2, 2), new RectInt(4, 2, 2, 2), new RectInt(8, 4, 2, 2), new RectInt(12, 6, 2, 2) }),
            (new[] { new Vector2Int(-1, 7), new Vector2Int(5, 7), new Vector2Int(5, 3), new Vector2Int(2, 3), new Vector2Int(2, 1),
                     new Vector2Int(10, 1), new Vector2Int(10, 5), new Vector2Int(13, 5) },
             new[] { new RectInt(0, 4, 2, 2), new RectInt(6, 4, 2, 2), new RectInt(7, 7, 2, 2), new RectInt(11, 1, 2, 2) }),
        };

        /// <summary>Baut die Überfall-Runde: 6 kurze Wellen, Stärke nach Sternen.</summary>
        public static LevelDefinition Build(FarmState farm)
        {
            int strength = farm.RaidStrength;
            var rng = new System.Random((int)(farm.Data.NextRaid % 100000));
            var layout = Layouts[rng.Next(Layouts.Length)];
            const EnemyKind G = EnemyKind.Gruenling, F = EnemyKind.Flitzer, B = EnemyKind.Brocken, S = EnemyKind.Schleicher, U = EnemyKind.Ufo;
            var waves = new[]
            {
                new WaveDefinition(40, new SpawnGroup(G, 10, 0.8f), new SpawnGroup(F, 6, 0.6f, 4f)),
                new WaveDefinition(50, new SpawnGroup(F, 12, 0.5f), new SpawnGroup(B, 2, 2.5f, 3f)),
                new WaveDefinition(60, new SpawnGroup(G, 14, 0.5f), new SpawnGroup(strength >= 3 ? S : F, 6, 0.9f, 3f)),
                new WaveDefinition(70, new SpawnGroup(B, 5, 1.8f), new SpawnGroup(strength >= 4 ? U : F, 6, 1f, 4f)),
                new WaveDefinition(80, new SpawnGroup(G, 18, 0.4f), new SpawnGroup(F, 14, 0.35f, 4f), new SpawnGroup(B, 4, 2f, 8f)),
                new WaveDefinition(0, new SpawnGroup(G, 16, 0.4f), new SpawnGroup(B, 6, 1.6f, 4f),
                    new SpawnGroup(strength >= 8 ? EnemyKind.ErnteRoboter : EnemyKind.Boss, 1, 0f, 10f)),
            };
            return new LevelDefinition
            {
                Id = "ueberfall",
                Name = "Überfall auf den Hof",
                Region = 0,
                ThemeOverride = MapTheme.Hof,
                Width = 16,
                Height = 9,
                Waypoints = layout.path,
                Corn = layout.buildings,
                Pasture = new RectInt(13, 3, 3, 3),
                StartMoney = 400 + 30 * strength,
                CowCount = 8,
                Waves = waves,
                HpMultiplier = 1f + 0.22f * strength,
                Story = "Alarm! Ein Ufo ist direkt über deinem Hof gelandet und die Aliens wollen die Kühe aus dem Stall holen. " +
                        "Verteidige deinen Hof – sonst nehmen sie 10 % deiner Vorräte mit!",
            };
        }

        /// <summary>Belohnung für einen abgewehrten Überfall.</summary>
        public static int RewardAmount(FarmState farm) => 300 + 150 * farm.FarmhouseLevel;
    }
}
