using UnityEngine;

namespace MuhDefense
{
    public enum Resource
    {
        Mais,
        Milch,
    }

    public enum FarmBuildingKind
    {
        Bauernhaus,
        Maisfeld,
        Kuhstall,
        Maissilo,
        Milchtank,
        Knechthuette,
        Schmiede,
        Zimmerei,
        Tueftlerschuppen,
        HofladenKontor,
        Heldenhuette,
    }

    /// <summary>Ein Turm, den eine Werkstatt ab einer bestimmten Stufe freischaltet.</summary>
    public class TowerUnlock
    {
        public TowerType Tower;
        public int FromLevel;

        public TowerUnlock(TowerType tower, int fromLevel)
        {
            Tower = tower;
            FromLevel = fromLevel;
        }
    }

    /// <summary>
    /// Werte eines Hof-Gebäudes (siehe Spielkonzept, Kapitel 7).
    /// Arrays mit Stufen-Werten: Index 0 = Stufe 1. Bei Kosten und Bauzeit ist Index 0 der Neubau.
    /// Zeiten sind für den Prototyp bewusst kurz.
    /// </summary>
    public class FarmBuildingType
    {
        public FarmBuildingKind Kind;
        public string Name;
        public string Description;
        public int Size = 2;
        public int MaxLevel;
        public Resource CostResource;
        public int[] Costs;
        public int[] BuildSeconds;
        /// <summary>Erlaubte Anzahl je Bauernhaus-Stufe 1..5.</summary>
        public int[] MaxCount;

        public bool IsProducer;
        public Resource ProducedResource;
        public float[] RatePerMinute;
        public int[] ProducerCapacity;

        public bool IsStorage;
        public Resource StoredResource;
        public int[] StorageCapacity;

        /// <summary>
        /// Werkstatt: Türme, die sie freischaltet. Höchste Upgrade-Stufe im Level = Werkstatt-Stufe − Freischalt-Stufe + 2
        /// (höchstens 5), also z. B. Mistgabel ab Stufe 1: Werkstatt 4 = alle Upgrades.
        /// </summary>
        public TowerUnlock[] Unlocks = new TowerUnlock[0];
        public bool IsWorkshop => Unlocks.Length > 0;

        public string Id => Kind.ToString();

        public static readonly FarmBuildingType Bauernhaus = new FarmBuildingType
        {
            Kind = FarmBuildingKind.Bauernhaus,
            Name = "Bauernhaus",
            Description = "Das Herz deines Hofs. Seine Stufe bestimmt, welche und wie viele Gebäude du bauen kannst und wie weit du sie verbessern darfst.",
            Size = 3,
            MaxLevel = 5,
            CostResource = Resource.Mais,
            Costs = new[] { 0, 400, 1200, 3000, 7000 },
            BuildSeconds = new[] { 0, 30, 120, 300, 600 },
            MaxCount = new[] { 1, 1, 1, 1, 1 },
        };

        public static readonly FarmBuildingType Maisfeld = new FarmBuildingType
        {
            Kind = FarmBuildingKind.Maisfeld,
            Name = "Maisfeld",
            Description = "Produziert Mais. Mais brauchst du für Gebäude.",
            MaxLevel = 5,
            CostResource = Resource.Milch,
            Costs = new[] { 80, 150, 400, 900, 2000 },
            BuildSeconds = new[] { 5, 20, 60, 180, 360 },
            MaxCount = new[] { 2, 3, 4, 5, 6 },
            IsProducer = true,
            ProducedResource = Resource.Mais,
            RatePerMinute = new[] { 20f, 30f, 45f, 65f, 90f },
            ProducerCapacity = new[] { 100, 200, 350, 550, 800 },
        };

        public static readonly FarmBuildingType Kuhstall = new FarmBuildingType
        {
            Kind = FarmBuildingKind.Kuhstall,
            Name = "Kuhstall",
            Description = "Hier wohnen deine Kühe und geben Milch. Milch brauchst du für Felder und Werkstätten.",
            MaxLevel = 5,
            CostResource = Resource.Mais,
            Costs = new[] { 80, 150, 400, 900, 2000 },
            BuildSeconds = new[] { 5, 20, 60, 180, 360 },
            MaxCount = new[] { 1, 2, 3, 4, 5 },
            IsProducer = true,
            ProducedResource = Resource.Milch,
            RatePerMinute = new[] { 15f, 25f, 38f, 55f, 75f },
            ProducerCapacity = new[] { 80, 160, 280, 450, 650 },
        };

        public static readonly FarmBuildingType Maissilo = new FarmBuildingType
        {
            Kind = FarmBuildingKind.Maissilo,
            Name = "Maissilo",
            Description = "Lagert zusätzlichen Mais.",
            MaxLevel = 5,
            CostResource = Resource.Milch,
            Costs = new[] { 150, 350, 800, 1800, 4000 },
            BuildSeconds = new[] { 10, 30, 90, 240, 480 },
            MaxCount = new[] { 1, 1, 2, 2, 3 },
            IsStorage = true,
            StoredResource = Resource.Mais,
            StorageCapacity = new[] { 800, 1600, 3000, 5000, 8000 },
        };

        public static readonly FarmBuildingType Milchtank = new FarmBuildingType
        {
            Kind = FarmBuildingKind.Milchtank,
            Name = "Milchtank",
            Description = "Lagert zusätzliche Milch.",
            MaxLevel = 5,
            CostResource = Resource.Mais,
            Costs = new[] { 150, 350, 800, 1800, 4000 },
            BuildSeconds = new[] { 10, 30, 90, 240, 480 },
            MaxCount = new[] { 1, 1, 2, 2, 3 },
            IsStorage = true,
            StoredResource = Resource.Milch,
            StorageCapacity = new[] { 800, 1600, 3000, 5000, 8000 },
        };

        public static readonly FarmBuildingType Knechthuette = new FarmBuildingType
        {
            Kind = FarmBuildingKind.Knechthuette,
            Name = "Knechthütte",
            Description = "Jede Hütte bringt einen Knecht. Ein Knecht kann ein Gebäude gleichzeitig bauen oder verbessern.",
            MaxLevel = 1,
            CostResource = Resource.Mais,
            Costs = new[] { 1500 },
            BuildSeconds = new[] { 60 },
            MaxCount = new[] { 1, 1, 2, 2, 2 },
        };

        public static readonly FarmBuildingType Schmiede = new FarmBuildingType
        {
            Kind = FarmBuildingKind.Schmiede,
            Name = "Schmiede",
            Description = "Werkstatt für Basis-Türme: Mistgabel-Bauer und Eier-Huhn, ab Stufe 2 Bienenstock und Hofhund-Hütte. Jede Stufe erlaubt im Level höhere Upgrades.",
            MaxLevel = 5,
            CostResource = Resource.Milch,
            Costs = new[] { 200, 500, 1200, 2500, 4500 },
            BuildSeconds = new[] { 10, 60, 180, 300, 420 },
            MaxCount = new[] { 1, 1, 1, 1, 1 },
            Unlocks = new[]
            {
                new TowerUnlock(TowerType.Mistgabel, 1), new TowerUnlock(TowerType.EierHuhn, 1),
                new TowerUnlock(TowerType.Bienenstock, 2), new TowerUnlock(TowerType.Hofhund, 2),
            },
        };

        public static readonly FarmBuildingType Zimmerei = new FarmBuildingType
        {
            Kind = FarmBuildingKind.Zimmerei,
            Name = "Zimmerei",
            Description = "Werkstatt für Explosiv-Türme: Heuballen-Katapult und Popcorn-Kanone, ab Stufe 2 Kürbis-Mörser und Mähdrescher. Jede Stufe erlaubt im Level höhere Upgrades.",
            MaxLevel = 5,
            CostResource = Resource.Milch,
            Costs = new[] { 400, 1000, 2000, 3500, 5000 },
            BuildSeconds = new[] { 30, 120, 240, 360, 480 },
            MaxCount = new[] { 0, 1, 1, 1, 1 },
            Unlocks = new[]
            {
                new TowerUnlock(TowerType.Heuballen, 1), new TowerUnlock(TowerType.Popcorn, 1),
                new TowerUnlock(TowerType.Moerser, 2), new TowerUnlock(TowerType.Maehdrescher, 2),
            },
        };

        public static readonly FarmBuildingType Tueftlerschuppen = new FarmBuildingType
        {
            Kind = FarmBuildingKind.Tueftlerschuppen,
            Name = "Tüftlerschuppen",
            Description = "Werkstatt für Technik-Türme: Eismilch-Sprüher, ab Stufe 2 Windmühle, Traktor-Tesla und Güllefass. Jede Stufe erlaubt im Level höhere Upgrades.",
            MaxLevel = 5,
            CostResource = Resource.Milch,
            Costs = new[] { 900, 1800, 3000, 4500, 6000 },
            BuildSeconds = new[] { 60, 180, 300, 420, 540 },
            MaxCount = new[] { 0, 0, 1, 1, 1 },
            Unlocks = new[]
            {
                new TowerUnlock(TowerType.Eismilch, 1), new TowerUnlock(TowerType.Windmuehle, 2),
                new TowerUnlock(TowerType.Tesla, 2), new TowerUnlock(TowerType.Guelle, 2),
            },
        };

        public static readonly FarmBuildingType HofladenKontor = new FarmBuildingType
        {
            Kind = FarmBuildingKind.HofladenKontor,
            Name = "Hofladen-Kontor",
            Description = "Werkstatt für Unterstützungs-Türme: Wetterhahn und Vogelscheuche, ab Stufe 2 der Hofladen. Jede Stufe erlaubt im Level höhere Upgrades.",
            MaxLevel = 5,
            CostResource = Resource.Mais,
            Costs = new[] { 600, 1200, 2400, 4000, 6000 },
            BuildSeconds = new[] { 30, 120, 240, 360, 480 },
            MaxCount = new[] { 0, 1, 1, 1, 1 },
            Unlocks = new[]
            {
                new TowerUnlock(TowerType.Wetterhahn, 1), new TowerUnlock(TowerType.Vogelscheuche, 1), new TowerUnlock(TowerType.Hofladen, 2),
            },
        };

        public static readonly FarmBuildingType Heldenhuette = new FarmBuildingType
        {
            Kind = FarmBuildingKind.Heldenhuette,
            Name = "Heldenhütte",
            Description = "Hier wählst du deinen Helden und schaltest neue frei. Jede Stufe lässt den Helden im Level eine Stufe höher starten.",
            MaxLevel = 5,
            CostResource = Resource.Milch,
            Costs = new[] { 600, 1200, 2200, 3500, 5000 },
            BuildSeconds = new[] { 30, 90, 180, 300, 420 },
            MaxCount = new[] { 0, 1, 1, 1, 1 },
        };

        public static readonly FarmBuildingType[] All =
        {
            Bauernhaus, Maisfeld, Kuhstall, Maissilo, Milchtank, Knechthuette, Schmiede, Zimmerei, Tueftlerschuppen, HofladenKontor, Heldenhuette,
        };

        public static FarmBuildingType Get(string id)
        {
            foreach (var t in All)
            {
                if (t.Id == id) return t;
            }
            return null;
        }

        /// <summary>Kosten, um auf diese Stufe zu kommen (1 = Neubau).</summary>
        public int CostForLevel(int level) => Costs[Mathf.Clamp(level - 1, 0, Costs.Length - 1)];

        public int SecondsForLevel(int level) => BuildSeconds[Mathf.Clamp(level - 1, 0, BuildSeconds.Length - 1)];

        /// <summary>Ab welcher Bauernhaus-Stufe das Gebäude gebaut werden darf.</summary>
        public int UnlockFarmhouseLevel
        {
            get
            {
                for (int i = 0; i < MaxCount.Length; i++)
                {
                    if (MaxCount[i] > 0) return i + 1;
                }
                return int.MaxValue;
            }
        }

        public static string ResourceName(Resource r) => r == Resource.Mais ? "Mais" : "Milch";
    }
}
