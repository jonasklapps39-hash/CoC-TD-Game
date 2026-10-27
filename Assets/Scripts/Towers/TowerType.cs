using UnityEngine;

namespace MuhDefense
{
    public enum TowerKind
    {
        /// <summary>Verfolgendes Geschoss auf ein einzelnes Ziel.</summary>
        Projectile,
        /// <summary>Geschoss im Bogen mit Flächenschaden (nur Bodengegner).</summary>
        Arc,
        /// <summary>Wirkt regelmäßig auf alle Aliens in Reichweite.</summary>
        Aura,
    }

    /// <summary>
    /// Werte eines Turmtyps. Index 0..2 = Stufe 1..3 im Level.
    /// Im fertigen Spiel gibt es zwei Ausbaupfade, und die Werkstätten auf dem Hof
    /// bestimmen die maximale Stufe – der Prototyp hat einen einfachen Pfad mit 3 Stufen.
    /// </summary>
    public class TowerType
    {
        public const int MaxLevel = 3;

        public string Name;
        public string Description;
        public TowerKind Kind;
        public int Cost;
        /// <summary>Kosten für Stufe 2 und Stufe 3.</summary>
        public int[] UpgradeCosts;
        public float[] Range;
        public float[] Damage;
        /// <summary>Angriffe pro Sekunde.</summary>
        public float[] FireRate;
        public float[] Splash = { 0f, 0f, 0f };
        /// <summary>Geschwindigkeits-Faktor für getroffene Aliens (1 = keine Verlangsamung).</summary>
        public float[] Slow = { 1f, 1f, 1f };
        public float SlowDuration = 1f;
        public float ProjectileSpeed = 10f;
        public Color Color;

        public static readonly TowerType Mistgabel = new TowerType
        {
            Name = "Mistgabel-Bauer",
            Description = "Wirft Mistgabeln auf einzelne Aliens. Günstig und zuverlässig.",
            Kind = TowerKind.Projectile,
            Cost = 100,
            UpgradeCosts = new[] { 80, 160 },
            Range = new[] { 2.6f, 2.9f, 3.2f },
            Damage = new[] { 6f, 10f, 16f },
            FireRate = new[] { 1.2f, 1.5f, 1.9f },
            ProjectileSpeed = 12f,
            Color = new Color(0.25f, 0.45f, 0.85f),
        };

        public static readonly TowerType Heuballen = new TowerType
        {
            Name = "Heuballen-Katapult",
            Description = "Schleudert Heuballen im Bogen. Flächenschaden, gut gegen Gruppen und Panzer.",
            Kind = TowerKind.Arc,
            Cost = 180,
            UpgradeCosts = new[] { 140, 260 },
            Range = new[] { 3f, 3.3f, 3.6f },
            Damage = new[] { 14f, 22f, 36f },
            FireRate = new[] { 0.5f, 0.55f, 0.65f },
            Splash = new[] { 1f, 1.2f, 1.5f },
            ProjectileSpeed = 6f,
            Color = new Color(0.95f, 0.80f, 0.30f),
        };

        public static readonly TowerType Eismilch = new TowerType
        {
            Name = "Eismilch-Sprüher",
            Description = "Versprüht eiskalte Milch und verlangsamt alle Aliens in der Nähe.",
            Kind = TowerKind.Aura,
            Cost = 140,
            UpgradeCosts = new[] { 110, 200 },
            Range = new[] { 2f, 2.3f, 2.6f },
            Damage = new[] { 1f, 2f, 4f },
            FireRate = new[] { 2f, 2f, 2f },
            Slow = new[] { 0.6f, 0.5f, 0.4f },
            SlowDuration = 1f,
            Color = new Color(0.65f, 0.88f, 1f),
        };

        public static readonly TowerType[] All = { Mistgabel, Heuballen, Eismilch };
    }
}
