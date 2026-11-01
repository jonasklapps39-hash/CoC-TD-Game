using UnityEngine;

namespace MuhDefense
{
    public enum Resource
    {
        Mais,
        Milch,
        Eier,
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
        Bauernschule,
        Huehnerstall,
        Blumenbeet,
        HeuDeko,
        Gartenzwerg,
        Brunnen,
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

        /// <summary>Deko: kleiner Dauer-Bonus statt Produktion (Anteil, z. B. 0.03 = +3 %).</summary>
        public float DecoBonus;
        public bool IsDeco => DecoBonus > 0f;
        /// <summary>Welcher Bonus: Produktion eines Rohstoffs oder (ohne) Erfahrung.</summary>
        public Resource DecoResource;
        public bool DecoXp;

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
            ProducerCapacity = new[] { 300, 600, 1050, 1650, 2400 },
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
            ProducerCapacity = new[] { 240, 480, 840, 1350, 1950 },
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
            Description = "Werkstatt für Basis-Türme. Jede Stufe erlaubt höhere Turm-Upgrades im Level.",
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
            Description = "Werkstatt für Explosiv-Türme. Jede Stufe erlaubt höhere Turm-Upgrades im Level.",
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
            Description = "Werkstatt für Technik-Türme. Jede Stufe erlaubt höhere Turm-Upgrades im Level.",
            MaxLevel = 5,
            CostResource = Resource.Milch,
            Costs = new[] { 600, 1200, 2200, 3500, 5000 },
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
            Description = "Werkstatt für Unterstützungs-Türme. Jede Stufe erlaubt höhere Turm-Upgrades im Level.",
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

        public static readonly FarmBuildingType Bauernschule = new FarmBuildingType
        {
            Kind = FarmBuildingKind.Bauernschule,
            Name = "Bauernschule",
            Description = "Hier lernst du Bauernwissen: Sterne gegen dauerhafte Boni. Jede Stufe schaltet eine neue Talent-Reihe frei.",
            MaxLevel = 5,
            CostResource = Resource.Mais,
            Costs = new[] { 300, 800, 1800, 3500, 6000 },
            BuildSeconds = new[] { 20, 60, 180, 300, 480 },
            MaxCount = new[] { 1, 1, 1, 1, 1 },
        };

        public static readonly FarmBuildingType Huehnerstall = new FarmBuildingType
        {
            Kind = FarmBuildingKind.Huehnerstall,
            Name = "Hühnerstall",
            Description = "Die Hühner legen Eier. Eier brauchst du für Helden-Training, Deko und den Ernte-Turbo.",
            MaxLevel = 5,
            CostResource = Resource.Mais,
            Costs = new[] { 400, 900, 2000, 4000, 7000 },
            BuildSeconds = new[] { 15, 60, 180, 300, 420 },
            MaxCount = new[] { 0, 1, 1, 2, 2 },
            IsProducer = true,
            ProducedResource = Resource.Eier,
            RatePerMinute = new[] { 4f, 6f, 9f, 13f, 18f },
            ProducerCapacity = new[] { 60, 120, 210, 330, 480 },
        };

        // Deko: klein, sofort fertig, bezahlt mit Eiern, gibt einen kleinen Dauer-Bonus
        public static readonly FarmBuildingType Blumenbeet = Deco(FarmBuildingKind.Blumenbeet, "Blumenbeet", "+3 % Mais-Produktion. Bienen lieben es.", 25, 0.03f, Resource.Mais);
        public static readonly FarmBuildingType HeuDeko = Deco(FarmBuildingKind.HeuDeko, "Heuballen-Stapel", "+3 % Milch-Produktion. Die Kühe fühlen sich wohl.", 25, 0.03f, Resource.Milch);
        public static readonly FarmBuildingType Brunnen = Deco(FarmBuildingKind.Brunnen, "Brunnen", "+5 % Eier-Produktion. Frisches Wasser für die Hühner.", 35, 0.05f, Resource.Eier);
        public static readonly FarmBuildingType Gartenzwerg = Deco(FarmBuildingKind.Gartenzwerg, "Gartenzwerg", "+4 % Erfahrung für die Bauernstufe. Er passt auf.", 40, 0.04f, Resource.Mais, xp: true);

        static FarmBuildingType Deco(FarmBuildingKind kind, string name, string description, int eggs, float bonus, Resource res, bool xp = false) => new FarmBuildingType
        {
            Kind = kind,
            Name = name,
            Description = description,
            Size = 1,
            MaxLevel = 1,
            CostResource = Resource.Eier,
            Costs = new[] { eggs },
            BuildSeconds = new[] { 1 },
            MaxCount = new[] { 2, 3, 4, 5, 6 },
            DecoBonus = bonus,
            DecoResource = res,
            DecoXp = xp,
        };

        public static readonly FarmBuildingType[] All =
        {
            Bauernhaus, Maisfeld, Kuhstall, Huehnerstall, Maissilo, Milchtank, Knechthuette, Schmiede, Zimmerei, Tueftlerschuppen, HofladenKontor,
            Heldenhuette, Bauernschule, Blumenbeet, HeuDeko, Brunnen, Gartenzwerg,
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

        public static string ResourceName(Resource r) => r == Resource.Mais ? "Mais" : r == Resource.Milch ? "Milch" : "Eier";

        public static string ResourceIcon(Resource r) => r == Resource.Mais ? "Maiskolben" : r == Resource.Milch ? "Milchflasche" : "Ei";
    }
}
