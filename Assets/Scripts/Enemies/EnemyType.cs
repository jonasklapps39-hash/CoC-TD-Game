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
        Schleicher,
        Ufo,
        Sanitaeter,
    }

    /// <summary>Werte einer Alien-Art (siehe Spielkonzept, Kapitel 10).</summary>
    public class EnemyType
    {
        public EnemyKind Kind;
        public string Name;
        /// <summary>Kurze Erklärung für das Fenster „Neuer Gegner“.</summary>
        public string Description = "";
        public float Hp;
        /// <summary>Felder pro Sekunde.</summary>
        public float Speed;
        /// <summary>Wird von jedem Treffer abgezogen (mindestens 25 % Schaden kommen immer durch).</summary>
        public float Armor;
        public int Reward;
        /// <summary>Größe des Modells.</summary>
        public float Scale;
        public Color Color;
        /// <summary>Name des Modells in Assets/Resources/Models.</summary>
        public string ModelName;

        /// <summary>Getarnt: nur Türme mit Tarnsicht können es angreifen.</summary>
        public bool Camo;
        /// <summary>Fliegt in gerader Linie über alles hinweg; Bodentürme (Katapult) treffen es nicht.</summary>
        public bool Flying;
        /// <summary>Heilt regelmäßig Aliens in der Nähe.</summary>
        public float HealPercent;
        public float HealRadius;
        public float HealInterval;

        public static readonly Dictionary<EnemyKind, EnemyType> All = new Dictionary<EnemyKind, EnemyType>
        {
            {
                EnemyKind.Gruenling, new EnemyType
                {
                    Kind = EnemyKind.Gruenling, Name = "Grünling", ModelName = "Gruenling",
                    Hp = 22f, Speed = 1.6f, Armor = 0f, Reward = 6, Scale = 1f,
                    Color = new Color(0.45f, 0.85f, 0.35f),
                }
            },
            {
                EnemyKind.Flitzer, new EnemyType
                {
                    Kind = EnemyKind.Flitzer, Name = "Flitzer", ModelName = "Flitzer",
                    Hp = 12f, Speed = 3f, Armor = 0f, Reward = 5, Scale = 0.8f,
                    Color = new Color(0.70f, 0.45f, 0.95f),
                }
            },
            {
                EnemyKind.Brocken, new EnemyType
                {
                    Kind = EnemyKind.Brocken, Name = "Brocken", ModelName = "Brocken",
                    Hp = 90f, Speed = 0.9f, Armor = 3f, Reward = 16, Scale = 1.35f,
                    Color = new Color(0.40f, 0.50f, 0.35f),
                }
            },
            {
                EnemyKind.Boss, new EnemyType
                {
                    Kind = EnemyKind.Boss, Name = "Oberbrocken Gorg", ModelName = "Boss",
                    Hp = 900f, Speed = 0.6f, Armor = 5f, Reward = 150, Scale = 2.1f,
                    Color = new Color(0.85f, 0.25f, 0.25f),
                }
            },
            {
                EnemyKind.Schleicher, new EnemyType
                {
                    Kind = EnemyKind.Schleicher, Name = "Schleicher", ModelName = "Schleicher",
                    Description = "Getarnt! Nur Türme mit Tarnsicht können ihn angreifen – zum Beispiel der Mistgabel-Bauer mit dem Upgrade „Hochsitz“, " +
                                  "ein Turm in der Nähe eines Wetterhahns oder Bauer Heinz ab Stufe 3. Flächenschaden trifft ihn trotzdem.",
                    Hp = 30f, Speed = 1.8f, Armor = 0f, Reward = 8, Scale = 0.95f, Camo = true,
                    Color = new Color(0.45f, 0.4f, 0.55f),
                }
            },
            {
                EnemyKind.Ufo, new EnemyType
                {
                    Kind = EnemyKind.Ufo, Name = "Kuhfänger-Ufo", ModelName = "KuhUfo",
                    Description = "Fliegt in gerader Linie über alles hinweg direkt zur Weide. Das Heuballen-Katapult trifft es nicht – " +
                                  "nimm Mistgabel-Bauern, Bienenstöcke, die Windmühle oder den Eismilch-Sprüher mit „Sprühnebel“.",
                    Hp = 45f, Speed = 1.1f, Armor = 0f, Reward = 10, Scale = 1f, Flying = true,
                    Color = new Color(0.6f, 0.65f, 0.75f),
                }
            },
            {
                EnemyKind.Sanitaeter, new EnemyType
                {
                    Kind = EnemyKind.Sanitaeter, Name = "Sanitäter", ModelName = "Sanitaeter",
                    Description = "Heilt alle zwei Sekunden die Aliens in seiner Nähe. Schalte ihn zuerst aus – mit dem Zielmodus „Stärkster“ " +
                                  "oder Türmen mit großer Reichweite.",
                    Hp = 50f, Speed = 1.2f, Armor = 1f, Reward = 12, Scale = 1f,
                    HealPercent = 0.12f, HealRadius = 1.6f, HealInterval = 2f,
                    Color = new Color(0.9f, 0.95f, 0.95f),
                }
            },
        };
    }
}
