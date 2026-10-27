using System.Collections.Generic;
using UnityEngine;

namespace MuhDefense
{
    public enum EnemyKind
    {
        Gruenling,
        Flitzer,
        Brocken,
        Boss,
    }

    /// <summary>Werte einer Alien-Art (siehe Spielkonzept, Kapitel 10).</summary>
    public class EnemyType
    {
        public EnemyKind Kind;
        public string Name;
        public float Hp;
        /// <summary>Felder pro Sekunde.</summary>
        public float Speed;
        /// <summary>Wird von jedem Treffer abgezogen (mindestens 25 % Schaden kommen immer durch).</summary>
        public float Armor;
        public int Reward;
        /// <summary>Größe des Modells.</summary>
        public float Scale;
        public Color Color;

        public static readonly Dictionary<EnemyKind, EnemyType> All = new Dictionary<EnemyKind, EnemyType>
        {
            {
                EnemyKind.Gruenling, new EnemyType
                {
                    Kind = EnemyKind.Gruenling, Name = "Grünling",
                    Hp = 22f, Speed = 1.6f, Armor = 0f, Reward = 6, Scale = 1f,
                    Color = new Color(0.45f, 0.85f, 0.35f),
                }
            },
            {
                EnemyKind.Flitzer, new EnemyType
                {
                    Kind = EnemyKind.Flitzer, Name = "Flitzer",
                    Hp = 12f, Speed = 3f, Armor = 0f, Reward = 5, Scale = 0.8f,
                    Color = new Color(0.70f, 0.45f, 0.95f),
                }
            },
            {
                EnemyKind.Brocken, new EnemyType
                {
                    Kind = EnemyKind.Brocken, Name = "Brocken",
                    Hp = 90f, Speed = 0.9f, Armor = 3f, Reward = 16, Scale = 1.35f,
                    Color = new Color(0.40f, 0.50f, 0.35f),
                }
            },
            {
                EnemyKind.Boss, new EnemyType
                {
                    Kind = EnemyKind.Boss, Name = "Oberbrocken Gorg",
                    Hp = 900f, Speed = 0.6f, Armor = 5f, Reward = 150, Scale = 2.1f,
                    Color = new Color(0.85f, 0.25f, 0.25f),
                }
            },
        };
    }
}
