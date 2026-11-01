using UnityEngine;

namespace MuhDefense
{
    public enum TowerKind
    {
        /// <summary>Verfolgendes Geschoss auf einzelne Ziele.</summary>
        Projectile,
        /// <summary>Geschoss im Bogen mit Flächenschaden.</summary>
        Arc,
        /// <summary>Wirkt regelmäßig auf alle Aliens in Reichweite.</summary>
        Aura,
        /// <summary>Blitz, der von Alien zu Alien springt (Traktor-Tesla).</summary>
        Chain,
        /// <summary>Legt Pfützen auf den Weg (Güllefass).</summary>
        Puddle,
        /// <summary>Fährt selbst auf dem Weg hin und her (Mähdrescher).</summary>
        Harvester,
        /// <summary>Schickt Hunde auf den Weg, die Aliens festhalten (Hofhund-Hütte).</summary>
        Dogs,
    }

    public enum TowerAbility
    {
        None,
        /// <summary>Mistgabel 1-5: Gabelregen über die ganze Karte.</summary>
        Gabelsturm,
        /// <summary>Mistgabel 2-5: alle Mistgabel-Bauern werfen 10 s doppelt so schnell.</summary>
        Bauernaufstand,
        /// <summary>Heuballen 1-5: riesige Ballen rollen den ganzen Weg entlang.</summary>
        Heulawine,
        /// <summary>Eismilch 1-5: friert alle Aliens ein.</summary>
        Eiszeit,
    }

    public enum TargetMode
    {
        First,
        Last,
        Strong,
        Close,
    }

    /// <summary>
    /// Alle Kampfwerte eines Turms. Ein Turm startet mit den Grundwerten seines Typs,
    /// jedes gekaufte Upgrade verändert eine Kopie davon.
    /// </summary>
    public class TowerStats
    {
        public float Range;
        public float Damage;
        /// <summary>Angriffe pro Sekunde.</summary>
        public float FireRate;
        public float ProjectileSpeed = 10f;
        /// <summary>Geschosse pro Angriff (gehen auf verschiedene Ziele, wenn möglich).</summary>
        public int Projectiles = 1;
        /// <summary>Wie viele Aliens ein Geschoss nacheinander treffen kann.</summary>
        public int Pierce = 1;
        public float Splash;
        public bool GlobalRange;
        public bool SeesCamo;
        /// <summary>Kann fliegende Aliens (Ufos) treffen.</summary>
        public bool HitsAir = true;

        // Unterstützung für Türme in Reichweite (Wetterhahn, Hofladen)
        public bool RevealsCamo;
        public float SupportRangeBonus;
        public float SupportRateBonus;
        /// <summary>Münzen am Ende jeder Welle (Hofladen).</summary>
        public int IncomePerWave;
        /// <summary>Kann selbst nicht angreifen (reiner Unterstützungs-Turm).</summary>
        public bool NoAttack;
        /// <summary>Münzen alle paar Sekunden während einer Welle (Hofladen).</summary>
        public int IncomeDuringWave;
        /// <summary>Upgrades von Türmen in Reichweite werden um diesen Anteil billiger (Hofladen).</summary>
        public float SupportDiscount;
        /// <summary>Schadens-Faktor gegen fliegende Aliens (Windmühle).</summary>
        public float AirDamageMultiplier = 1f;
        /// <summary>Fliegende Aliens werden so lange festgehalten (Windmühle).</summary>
        public float AirStun;

        public bool ArmorPiercing;
        /// <summary>Schadens-Faktor gegen gepanzerte Aliens.</summary>
        public float ArmoredBonus = 1f;
        public float CritChance;
        public float CritMultiplier = 1f;

        // Feuer (Heuballen)
        public float BurnDps;
        public float BurnDuration;
        public float FireFieldDps;
        public float FireFieldDuration;
        public float FireFieldRadiusScale = 1f;
        public bool FireMeltsArmor;
        public float BurnArmoredMultiplier = 1f;
        /// <summary>Strecke in Feldern, die ein Ballen nach dem Aufprall den Weg zurückrollt (0 = gar nicht).</summary>
        public float RollDistance;

        // Eismilch
        /// <summary>Geschwindigkeits-Faktor für getroffene Aliens (1 = keine Verlangsamung).</summary>
        public float Slow = 1f;
        public float SlowDuration = 1f;
        public int FreezeEveryNthPulse;
        public float FreezeDuration;
        public bool FreezeBig;
        public float FrozenVulnerability = 1f;
        public float Knockback;
        public float DotDps;
        public float DotDuration;
        public float ArmorShred;
        public float PermanentVulnerability;
        /// <summary>Zusätzliche Münzen für Aliens, die von diesem Turm getroffen wurden (Anteil).</summary>
        public float BonusReward;

        // Popcorn-Kanone: Splitter nach dem Aufprall
        public int Shards;
        public float ShardDamage;

        /// <summary>Getroffene Aliens bleiben so lange stehen (Blitz, Kürbis, Vogelscheuche).</summary>
        public float StunDuration;
        /// <summary>Betäubung wirkt auch auf Bosse.</summary>
        public bool StunBoss;

        // Traktor-Tesla
        public int ChainCount;
        public float ChainRange = 1.6f;
        /// <summary>Schadens-Faktor pro Sprung.</summary>
        public float ChainFalloff = 0.85f;

        // Güllefass
        public float PuddleRadius;
        public float PuddleDuration;

        // Mähdrescher und Hofhunde
        public int UnitCount;
        public float UnitSpeed = 1.5f;
        public float UnitRadius = 0.45f;
        public float UnitHp;
        public float RespawnTime = 6f;
        /// <summary>Hunde halten auch Bosse fest.</summary>
        public bool BlocksBoss;
        /// <summary>Hunde beißen Kuh-Träger dreifach.</summary>
        public bool RescuesCows;

        /// <summary>Vogelscheuche: so viele Aliens werden pro Schreck erschreckt (0 = keine Schreck-Wirkung).</summary>
        public int ScareCount;

        public TowerAbility Ability = TowerAbility.None;
        public float AbilityCooldown;

        public TowerStats Clone() => (TowerStats)MemberwiseClone();
    }

    public class UpgradeTier
    {
        public string Name;
        public string Description;
        public int Cost;
        public System.Action<TowerStats> Apply;

        public UpgradeTier(string name, string description, int cost, System.Action<TowerStats> apply)
        {
            Name = name;
            Description = description;
            Cost = cost;
            Apply = apply;
        }
    }

    public class UpgradePath
    {
        public string Name;
        public Color Color;
        public UpgradeTier[] Tiers;

        public UpgradePath(string name, Color color, params UpgradeTier[] tiers)
        {
            Name = name;
            Color = color;
            Tiers = tiers;
        }
    }

    /// <summary>
    /// Ein Turmtyp mit Grundwerten und drei Upgrade-Pfaden à 5 Stufen (siehe Spielkonzept, Kapitel 15).
    /// Regeln wie in Bloons TD6: ein Pfad bis Stufe 5, ein zweiter bis Stufe 2, der dritte bleibt gesperrt.
    /// </summary>
    public class TowerType
    {
        public const int MaxTier = 5;
        public const int PathCount = 3;

        public string Name;
        /// <summary>Kurzer Name für die Turm-Karten in der Seitenleiste.</summary>
        public string ShortName;
        public string Description;
        /// <summary>Modell in Assets/Resources/Models.</summary>
        public string ModelName;
        /// <summary>Symbol in Assets/Resources/Icons (leer = wie das Modell).</summary>
        public string IconName = "";
        /// <summary>Form und Farbe der Geschosse.</summary>
        public PrimitiveType ShotShape = PrimitiveType.Cube;
        public Vector3 ShotScale = new Vector3(0.05f, 0.05f, 0.35f);
        public Color ShotColor = new Color(0.75f, 0.75f, 0.8f);
        public TowerKind Kind;
        public int Cost;
        public Color Color;
        public TowerStats Base;
        public UpgradePath[] Paths;

        static readonly Color Red = new Color(0.95f, 0.35f, 0.25f);
        static readonly Color Blue = new Color(0.3f, 0.6f, 1f);
        static readonly Color Green = new Color(0.4f, 0.85f, 0.3f);

        static UpgradeTier T(string name, string description, int cost, System.Action<TowerStats> apply)
            => new UpgradeTier(name, description, cost, apply);

        public static readonly TowerType Mistgabel = new TowerType
        {
            Name = "Mistgabel-Bauer",
            ModelName = "MistgabelBauer",
            ShortName = "Mistgabel",
            Description = "Wirft Mistgabeln auf einzelne Aliens. Günstig und zuverlässig.",
            Kind = TowerKind.Projectile,
            Cost = 100,
            Color = new Color(0.25f, 0.45f, 0.85f),
            Base = new TowerStats { Range = 2.6f, Damage = 6f, FireRate = 1.2f, ProjectileSpeed = 12f },
            Paths = new[]
            {
                new UpgradePath("Schärfe", Red,
                    T("Spitze Zinken", "Jede Gabel trifft 2 Aliens.", 90, s => s.Pierce += 1),
                    T("Stahlgabel", "+2 Schaden.", 150, s => s.Damage += 2f),
                    T("Gabel-Hagel", "Wirft 3 Gabeln auf einmal.", 350, s => s.Projectiles += 2),
                    T("Goldene Gabel", "Durchbohrt Panzer, doppelter Schaden gegen gepanzerte Aliens.", 750,
                        s => { s.ArmorPiercing = true; s.ArmoredBonus *= 2f; s.Damage += 2f; }),
                    T("Gabelsturm", "Fähigkeit: Gabelregen über die ganze Karte (60 Schaden an jedem Alien).", 1600,
                        s => { s.Ability = TowerAbility.Gabelsturm; s.AbilityCooldown = 30f; s.Damage += 4f; })),
                new UpgradePath("Tempo", Blue,
                    T("Flinke Hände", "+20 % Wurftempo.", 100, s => s.FireRate *= 1.2f),
                    T("Doppelwurf", "Wirft 2 Gabeln pro Wurf.", 180, s => s.Projectiles += 1),
                    T("Kaffeepause", "+50 % Wurftempo.", 400, s => s.FireRate *= 1.5f),
                    T("Gabel-Maschine", "Wirft doppelt so schnell.", 850, s => s.FireRate *= 2f),
                    T("Bauernaufstand", "Fähigkeit: Alle Mistgabel-Bauern werfen 10 Sekunden doppelt so schnell.", 1700,
                        s => { s.Ability = TowerAbility.Bauernaufstand; s.AbilityCooldown = 40f; s.FireRate *= 1.3f; })),
                new UpgradePath("Weitblick", Green,
                    T("Fernglas", "+25 % Reichweite.", 80, s => s.Range *= 1.25f),
                    T("Hochsitz", "Sieht getarnte Aliens, +10 % Reichweite.", 150, s => { s.SeesCamo = true; s.Range *= 1.1f; }),
                    T("Weitwurf", "Sehr große Reichweite und +3 Schaden.", 380, s => { s.Range *= 1.5f; s.Damage += 3f; }),
                    T("Scharfschütze", "Unbegrenzte Reichweite und +8 Schaden.", 900,
                        s => { s.GlobalRange = true; s.Damage += 8f; s.ProjectileSpeed = 30f; }),
                    T("Meisterjäger", "Jeder fünfte Treffer ist ein kritischer Treffer mit fünffachem Schaden.", 1700,
                        s => { s.CritChance = 0.2f; s.CritMultiplier = 5f; s.Damage += 4f; })),
            },
        };

        public static readonly TowerType Heuballen = new TowerType
        {
            Name = "Heuballen-Katapult",
            ModelName = "HeuballenKatapult",
            ShortName = "Katapult",
            Description = "Schleudert Heuballen im Bogen. Flächenschaden, gut gegen Gruppen und Panzer.",
            Kind = TowerKind.Arc,
            Cost = 180,
            Color = new Color(0.95f, 0.80f, 0.30f),
            Base = new TowerStats { Range = 3f, Damage = 14f, FireRate = 0.5f, Splash = 1f, ProjectileSpeed = 6f, HitsAir = false },
            Paths = new[]
            {
                new UpgradePath("Wucht", Red,
                    T("Große Ballen", "+30 % Flächenradius.", 120, s => s.Splash *= 1.3f),
                    T("Schwere Ballen", "+6 Schaden.", 220, s => s.Damage += 6f),
                    T("Riesenballen", "Ballen rollen nach dem Aufprall 3 Felder den Weg zurück.", 500, s => s.RollDistance = 3f),
                    T("Strohwalze", "Ballen rollen den ganzen Weg zurück, +10 Schaden.", 1000, s => { s.RollDistance = 99f; s.Damage += 10f; }),
                    T("Heulawine", "Fähigkeit: Drei riesige Ballen walzen den ganzen Weg platt.", 1800,
                        s => { s.Ability = TowerAbility.Heulawine; s.AbilityCooldown = 35f; s.Damage += 10f; })),
                new UpgradePath("Feuer", Blue,
                    T("Glimmende Ballen", "Getroffene Aliens brennen kurz.", 110, s => { s.BurnDps = 3f; s.BurnDuration = 2f; }),
                    T("Brandfläche", "Hinterlässt 3 Sekunden Feuer auf dem Weg.", 230, s => { s.FireFieldDps = 4f; s.FireFieldDuration = 3f; }),
                    T("Strohfeuer", "Das Feuer ist größer und brennt länger.", 450,
                        s => { s.FireFieldRadiusScale = 1.5f; s.FireFieldDuration = 5f; s.BurnDps += 2f; }),
                    T("Glutkern", "Feuer ignoriert Panzer.", 900, s => { s.FireMeltsArmor = true; s.FireFieldDps += 3f; }),
                    T("Inferno", "Dreifacher Feuerschaden, gepanzerte Aliens brennen doppelt.", 1700,
                        s => { s.BurnDps *= 3f; s.FireFieldDps *= 3f; s.BurnArmoredMultiplier = 2f; })),
                new UpgradePath("Mechanik", Green,
                    T("Schnellspanner", "+20 % Tempo.", 130, s => s.FireRate *= 1.2f),
                    T("Weitschuss", "+30 % Reichweite.", 200, s => s.Range *= 1.3f),
                    T("Doppelkatapult", "2 Ballen pro Schuss.", 500, s => s.Projectiles += 1),
                    T("Ballen-Artillerie", "+80 % Tempo.", 950, s => s.FireRate *= 1.8f),
                    T("Erntemaschine", "Schießt auf bis zu 6 Ziele gleichzeitig.", 1800, s => { s.Projectiles = 6; s.Range *= 1.1f; })),
            },
        };

        public static readonly TowerType Eismilch = new TowerType
        {
            Name = "Eismilch-Sprüher",
            ModelName = "EismilchSprueher",
            ShortName = "Eismilch",
            Description = "Versprüht eiskalte Milch und verlangsamt alle Aliens in der Nähe.",
            Kind = TowerKind.Aura,
            Cost = 140,
            Color = new Color(0.65f, 0.88f, 1f),
            Base = new TowerStats { Range = 2f, Damage = 1f, FireRate = 2f, Slow = 0.6f, SlowDuration = 1f, HitsAir = false },
            Paths = new[]
            {
                new UpgradePath("Kälte", Red,
                    T("Eiskalt", "Verlangsamt stärker.", 100, s => s.Slow -= 0.1f),
                    T("Tiefkühltruhe", "Jeder dritte Sprühstoß friert Aliens kurz ein (nicht große).", 240,
                        s => { s.FreezeEveryNthPulse = 3; s.FreezeDuration = 0.6f; }),
                    T("Permafrost", "Eingefrorene Aliens nehmen 50 % mehr Schaden.", 480, s => s.FrozenVulnerability = 1.5f),
                    T("Gletscher", "Friert auch große Aliens ein, länger.", 950, s => { s.FreezeBig = true; s.FreezeDuration = 1f; }),
                    T("Eiszeit", "Fähigkeit: Friert alle Aliens auf der Karte 4 Sekunden ein (Bosse 2 Sekunden).", 1700,
                        s => { s.Ability = TowerAbility.Eiszeit; s.AbilityCooldown = 40f; s.Damage += 2f; })),
                new UpgradePath("Druck", Blue,
                    T("Druckpumpe", "+25 % Reichweite.", 90, s => s.Range *= 1.25f),
                    T("Sprühnebel", "+15 % Reichweite, trifft auch fliegende Ufos.", 200, s => { s.Range *= 1.15f; s.HitsAir = true; }),
                    T("Milchpfützen", "Verlangsamung hält länger und ist stärker.", 450, s => { s.SlowDuration = 2.5f; s.Slow -= 0.05f; }),
                    T("Hochdruck", "Stößt Aliens ein Stück zurück (nicht Bosse).", 1000, s => s.Knockback = 0.25f),
                    T("Milch-Monsun", "Doppelte Reichweite und stärkerer Rückstoß.", 1700, s => { s.Range *= 2f; s.Knockback = 0.3f; })),
                new UpgradePath("Sauermilch", Green,
                    T("Sauermilch", "Schaden über Zeit.", 110, s => { s.DotDps = 2f; s.DotDuration = 3f; }),
                    T("Joghurt-Säure", "Schwächt Panzer um 2.", 220, s => s.ArmorShred = 2f),
                    T("Käsekleber", "Aliens kleben fest und werden noch langsamer.", 500, s => s.Slow -= 0.15f),
                    T("Stinkkäse", "Mehr Schaden über Zeit, Panzer um 4 geschwächt.", 1000, s => { s.DotDps += 4f; s.ArmorShred = 4f; }),
                    T("Käse-Imperium", "Getroffene Aliens nehmen dauerhaft 25 % mehr Schaden.", 1700,
                        s => { s.PermanentVulnerability = 0.25f; s.DotDps += 4f; })),
            },
        };

        public static readonly TowerType Bienenstock = new TowerType
        {
            Name = "Bienenstock",
            ModelName = "Bienenstock",
            ShortName = "Bienen",
            Description = "Bienen verfolgen die Aliens, treffen auch Ufos und stechen mit Gift (Schaden über Zeit).",
            Kind = TowerKind.Projectile,
            Cost = 160,
            Color = new Color(1f, 0.8f, 0.2f),
            Base = new TowerStats { Range = 2.8f, Damage = 4f, FireRate = 1.1f, Projectiles = 2, ProjectileSpeed = 6f, DotDps = 3f, DotDuration = 2f },
            Paths = new[]
            {
                new UpgradePath("Schwarm", Red,
                    T("Mehr Bienen", "3 Bienen pro Angriff.", 110, s => s.Projectiles += 1),
                    T("Fleißige Bienen", "+30 % Tempo.", 180, s => s.FireRate *= 1.3f),
                    T("Großer Schwarm", "5 Bienen pro Angriff.", 420, s => s.Projectiles += 2),
                    T("Killerbienen", "Doppelter Schaden und Gift.", 900, s => { s.Damage *= 2f; s.DotDps *= 2f; }),
                    T("Bienenkönigin", "8 Bienen pro Angriff, jede trifft 2 Aliens.", 1700, s => { s.Projectiles += 3; s.Pierce += 1; })),
                new UpgradePath("Gift", Blue,
                    T("Stärkeres Gift", "+50 % Gift.", 100, s => s.DotDps *= 1.5f),
                    T("Langes Gift", "Gift wirkt doppelt so lange.", 200, s => s.DotDuration *= 2f),
                    T("Panzerstich", "Stiche schwächen den Panzer um 3.", 450, s => s.ArmorShred = 3f),
                    T("Wespennest", "Dreifaches Gift.", 950, s => s.DotDps *= 3f),
                    T("Hornissenplage", "Gestochene Aliens nehmen dauerhaft 20 % mehr Schaden.", 1700,
                        s => { s.PermanentVulnerability = 0.2f; s.DotDps *= 1.5f; })),
                new UpgradePath("Honig", Green,
                    T("Klebriger Honig", "Stiche verlangsamen leicht.", 90, s => { s.Slow = 0.8f; s.SlowDuration = 1.5f; }),
                    T("Weiter Flug", "+30 % Reichweite, sieht getarnte Aliens.", 200, s => { s.Range *= 1.3f; s.SeesCamo = true; }),
                    T("Honigfalle", "Stärkere Verlangsamung.", 420, s => s.Slow = 0.6f),
                    T("Honigsee", "Verlangsamung hält doppelt so lange, +30 % Reichweite.", 900, s => { s.SlowDuration *= 2f; s.Range *= 1.3f; }),
                    T("Goldener Honig", "Getroffene Aliens bringen 50 % mehr Münzen.", 1600, s => s.BonusReward = 0.5f)),
            },
        };

        public static readonly TowerType Windmuehle = new TowerType
        {
            Name = "Windmühle",
            ModelName = "Windmuehle",
            ShortName = "Windmühle",
            Description = "Pustet Aliens den Weg zurück. Richtet vierfachen Schaden an fliegenden Ufos an.",
            Kind = TowerKind.Aura,
            Cost = 170,
            Color = new Color(0.85f, 0.9f, 1f),
            Base = new TowerStats { Range = 2.2f, Damage = 3f, FireRate = 0.6f, Knockback = 0.3f, AirDamageMultiplier = 4f },
            Paths = new[]
            {
                new UpgradePath("Orkan", Red,
                    T("Starker Wind", "Stößt deutlich weiter zurück.", 120, s => s.Knockback += 0.25f),
                    T("Großes Rad", "+25 % Reichweite.", 220, s => s.Range *= 1.25f),
                    T("Sturmböe", "Stößt noch weiter zurück und pustet öfter.", 480, s => { s.Knockback += 0.2f; s.FireRate *= 1.3f; }),
                    T("Wirbelsturm", "Stößt noch weiter zurück und macht +6 Schaden.", 1000, s => { s.Damage += 6f; s.Knockback += 0.2f; }),
                    T("Tornado", "Riesige Reichweite, sehr starker Rückstoß.", 1800, s => { s.Range *= 1.5f; s.Knockback += 0.4f; })),
                new UpgradePath("Ufo-Fänger", Blue,
                    T("Aufwind", "Sechsfacher Schaden an Ufos.", 110, s => s.AirDamageMultiplier = 6f),
                    T("Luftwirbel", "Hält Ufos kurz fest.", 240, s => s.AirStun = 0.6f),
                    T("Fallwind", "+8 Schaden.", 500, s => s.Damage += 8f),
                    T("Ufo-Falle", "Hält Ufos länger fest, zehnfacher Schaden an Ufos.", 950, s => { s.AirStun = 1.4f; s.AirDamageMultiplier = 10f; }),
                    T("Himmelsfänger", "Aliens in Reichweite werden dauerhaft verwundbar (+40 % Schaden).", 1700,
                        s => { s.PermanentVulnerability = 0.4f; s.Damage += 6f; })),
                new UpgradePath("Böen", Green,
                    T("Gleichmäßiger Wind", "+30 % Tempo.", 100, s => s.FireRate *= 1.3f),
                    T("Gegenwind", "Verlangsamt Aliens in Reichweite.", 200, s => { s.Slow = 0.7f; s.SlowDuration = 1.5f; }),
                    T("Staubwolke", "Sieht getarnte Aliens, +20 % Reichweite.", 450, s => { s.SeesCamo = true; s.Range *= 1.2f; }),
                    T("Dauerböe", "Doppeltes Tempo.", 950, s => s.FireRate *= 2f),
                    T("Wetterküche", "Starke Verlangsamung und +10 Schaden.", 1700, s => { s.Slow = 0.45f; s.Damage += 10f; })),
            },
        };

        public static readonly TowerType Wetterhahn = new TowerType
        {
            Name = "Wetterhahn",
            ModelName = "Wetterhahn",
            ShortName = "Wetterhahn",
            Description = "Greift nicht an, aber Türme in seiner Nähe sehen getarnte Aliens und haben 10 % mehr Reichweite.",
            Kind = TowerKind.Aura,
            Cost = 150,
            Color = new Color(0.95f, 0.45f, 0.2f),
            Base = new TowerStats { Range = 2.5f, Damage = 0f, FireRate = 1f, NoAttack = true, RevealsCamo = true, SupportRangeBonus = 0.1f },
            Paths = new[]
            {
                new UpgradePath("Radar", Red,
                    T("Hoher Mast", "Unterstützt Türme in größerem Umkreis.", 90, s => s.Range *= 1.3f),
                    T("Weitsicht", "Türme in der Nähe: +20 % Reichweite.", 200, s => s.SupportRangeBonus = 0.2f),
                    T("Radarhahn", "Noch größerer Umkreis.", 450, s => s.Range *= 1.4f),
                    T("Adlerauge", "Türme in der Nähe: +30 % Reichweite.", 900, s => s.SupportRangeBonus = 0.3f),
                    T("Wetterstation", "Riesiger Umkreis, +40 % Reichweite.", 1600, s => { s.Range *= 1.5f; s.SupportRangeBonus = 0.4f; })),
                new UpgradePath("Kampfhahn", Blue,
                    T("Weckruf", "Türme in der Nähe: +10 % Tempo.", 120, s => s.SupportRateBonus = 0.1f),
                    T("Ansporn", "Türme in der Nähe: +15 % Tempo.", 240, s => s.SupportRateBonus = 0.15f),
                    T("Schlachtruf", "Türme in der Nähe: +25 % Tempo.", 550, s => s.SupportRateBonus = 0.25f),
                    T("Kampfgeist", "Türme in der Nähe: +35 % Tempo.", 1100, s => s.SupportRateBonus = 0.35f),
                    T("Goldener Hahn", "Türme in der Nähe: +50 % Tempo.", 1900, s => s.SupportRateBonus = 0.5f)),
                new UpgradePath("Krähen", Green,
                    T("Krähenfreund", "Eine Krähe pickt Aliens in der Nähe (wenig Schaden).", 100,
                        s => { s.NoAttack = false; s.Damage = 3f; s.FireRate = 1f; s.HitsAir = true; }),
                    T("Krähenpaar", "Doppeltes Tempo.", 200, s => s.FireRate *= 2f),
                    T("Krähenschwarm", "+6 Schaden.", 450, s => s.Damage += 6f),
                    T("Rabenmutter", "Krähen schwächen Panzer um 3.", 900, s => { s.ArmorShred = 3f; s.Damage += 4f; }),
                    T("Rabenkönig", "+50 % Tempo, +6 Schaden.", 2000, s => { s.FireRate *= 1.5f; s.Damage += 6f; })),
            },
        };

        public static readonly TowerType Hofladen = new TowerType
        {
            Name = "Hofladen",
            ModelName = "Hofladen",
            ShortName = "Hofladen",
            Description = "Greift nicht an, bringt aber nach jeder Welle 40 Münzen extra.",
            Kind = TowerKind.Aura,
            Cost = 250,
            Color = new Color(0.9f, 0.3f, 0.25f),
            Base = new TowerStats { Range = 2f, Damage = 0f, FireRate = 1f, NoAttack = true, IncomePerWave = 40 },
            Paths = new[]
            {
                new UpgradePath("Großmarkt", Red,
                    T("Mehr Ware", "60 Münzen pro Welle.", 150, s => s.IncomePerWave = 60),
                    T("Marktstand", "90 Münzen pro Welle.", 280, s => s.IncomePerWave = 90),
                    T("Wochenmarkt", "140 Münzen pro Welle.", 600, s => s.IncomePerWave = 140),
                    T("Großmarkt", "220 Münzen pro Welle.", 900, s => s.IncomePerWave = 220),
                    T("Handelsimperium", "350 Münzen pro Welle.", 1600, s => s.IncomePerWave = 350)),
                new UpgradePath("Bio-Siegel", Blue,
                    T("Stammkunden", "Upgrades von Türmen in der Nähe 5 % billiger.", 120, s => s.SupportDiscount = 0.05f),
                    T("Bio-Siegel", "Upgrades in der Nähe 15 % billiger.", 250, s => s.SupportDiscount = 0.15f),
                    T("Großer Laden", "Größerer Umkreis.", 500, s => s.Range *= 1.5f),
                    T("Genossenschaft", "Upgrades in der Nähe 25 % billiger.", 1000, s => s.SupportDiscount = 0.25f),
                    T("Bauernverband", "Upgrades in der Nähe 30 % billiger, noch größerer Umkreis.", 1800,
                        s => { s.SupportDiscount = 0.3f; s.Range *= 1.3f; })),
                new UpgradePath("Lieferdienst", Green,
                    T("Fahrradbote", "Während der Welle alle 5 Sekunden 5 Münzen.", 140, s => s.IncomeDuringWave = 5),
                    T("Lieferwagen", "Alle 5 Sekunden 10 Münzen.", 280, s => s.IncomeDuringWave = 10),
                    T("Express", "Alle 5 Sekunden 20 Münzen.", 600, s => s.IncomeDuringWave = 20),
                    T("Lieferflotte", "Alle 5 Sekunden 35 Münzen.", 1200, s => s.IncomeDuringWave = 35),
                    T("Milchstraße", "Alle 5 Sekunden 60 Münzen.", 2200, s => s.IncomeDuringWave = 60)),
            },
        };

        public static readonly TowerType EierHuhn = new TowerType
        {
            Name = "Eier-Huhn",
            ModelName = "EierHuhn",
            ShortName = "Huhn",
            Description = "Ein grimmiges Huhn feuert Eier in schneller Folge. Billig, schnell, wenig Schaden pro Ei.",
            Kind = TowerKind.Projectile,
            Cost = 90,
            Color = new Color(0.97f, 0.95f, 0.9f),
            ShotShape = PrimitiveType.Sphere,
            ShotScale = new Vector3(0.11f, 0.11f, 0.15f),
            ShotColor = new Color(1f, 0.97f, 0.88f),
            Base = new TowerStats { Range = 2.3f, Damage = 3f, FireRate = 3f, ProjectileSpeed = 11f },
            Paths = new[]
            {
                new UpgradePath("Maschinenhuhn", Red,
                    T("Hektisch", "+25 % Tempo.", 90, s => s.FireRate *= 1.25f),
                    T("Doppelei", "2 Eier pro Schuss.", 170, s => s.Projectiles += 1),
                    T("Legebatterie", "+60 % Tempo.", 380, s => s.FireRate *= 1.6f),
                    T("Eier-MG", "+60 % Tempo.", 850, s => s.FireRate *= 1.6f),
                    T("Maschinenhuhn", "+50 % Tempo, 3 Eier pro Schuss, +2 Schaden.", 2000,
                        s => { s.FireRate *= 1.5f; s.Projectiles += 1; s.Damage += 2f; })),
                new UpgradePath("Faule Eier", Blue,
                    T("Faule Eier", "Treffer verlangsamen leicht.", 100, s => { s.Slow = 0.8f; s.SlowDuration = 1f; }),
                    T("Stinkbombe", "Gestank: Schaden über Zeit.", 200, s => { s.DotDps = 2f; s.DotDuration = 3f; }),
                    T("Schwefelei", "Schwächt Panzer um 2.", 420, s => s.ArmorShred = 2f),
                    T("Ekel-Ei", "Stärkere Verlangsamung, doppelter Gestank.", 900, s => { s.Slow = 0.6f; s.DotDps *= 2f; }),
                    T("Faulgas-Wolke", "Getroffene Aliens nehmen dauerhaft 20 % mehr Schaden.", 1600,
                        s => { s.PermanentVulnerability = 0.2f; s.DotDps *= 2f; })),
                new UpgradePath("Goldene Eier", Green,
                    T("Hartgekocht", "+2 Schaden.", 110, s => s.Damage += 2f),
                    T("Weitwurf", "+30 % Reichweite, sieht getarnte Aliens.", 200, s => { s.Range *= 1.3f; s.SeesCamo = true; }),
                    T("Durchschlag", "Jedes Ei trifft 3 Aliens.", 450, s => s.Pierce += 2),
                    T("Goldenes Ei", "+4 Schaden, getroffene Aliens bringen 30 % mehr Münzen.", 900, s => { s.Damage += 4f; s.BonusReward = 0.3f; }),
                    T("Henne der Legende", "Jedes vierte Ei ist kritisch (vierfacher Schaden), trifft 5 Aliens.", 1700,
                        s => { s.CritChance = 0.25f; s.CritMultiplier = 4f; s.Pierce += 2; })),
            },
        };

        public static readonly TowerType Popcorn = new TowerType
        {
            Name = "Popcorn-Kanone",
            ModelName = "PopcornKanone",
            ShortName = "Popcorn",
            Description = "Schießt Maiskolben, die beim Aufprall zu Popcorn platzen. Die Körner fliegen in alle Richtungen.",
            Kind = TowerKind.Arc,
            Cost = 200,
            Color = new Color(1f, 0.9f, 0.4f),
            ShotShape = PrimitiveType.Capsule,
            ShotScale = new Vector3(0.14f, 0.2f, 0.14f),
            ShotColor = new Color(1f, 0.82f, 0.2f),
            Base = new TowerStats { Range = 3f, Damage = 8f, FireRate = 0.6f, Splash = 0.7f, Shards = 4, ShardDamage = 4f, ProjectileSpeed = 7f, HitsAir = false },
            Paths = new[]
            {
                new UpgradePath("Karamell", Red,
                    T("Karamell", "Körner kleben und verlangsamen.", 120, s => { s.Slow = 0.75f; s.SlowDuration = 1.5f; }),
                    T("Klebrig", "Verlangsamung hält länger.", 230, s => s.SlowDuration = 2.5f),
                    T("Zuckerguss", "Stärkere Verlangsamung.", 480, s => s.Slow = 0.55f),
                    T("Karamellfalle", "Körner kleben Aliens kurz fest.", 950, s => s.StunDuration = 0.5f),
                    T("Karamell-Lawine", "6 Körner mehr, sehr starke Verlangsamung.", 1700, s => { s.Shards += 6; s.Slow = 0.45f; })),
                new UpgradePath("Mega-Plopp", Blue,
                    T("Mehr Körner", "3 Körner mehr.", 130, s => s.Shards += 3),
                    T("Großer Topf", "+40 % Flächenradius.", 240, s => s.Splash *= 1.4f),
                    T("Mega-Plopp", "5 Körner mehr, +3 Kornschaden.", 520, s => { s.Shards += 5; s.ShardDamage += 3f; }),
                    T("Kinosaal", "+10 Schaden, +30 % Flächenradius.", 1000, s => { s.Damage += 10f; s.Splash *= 1.3f; }),
                    T("Popcorn-Explosion", "10 Körner mehr, +50 % Kornschaden.", 1800, s => { s.Shards += 10; s.ShardDamage *= 1.5f; })),
                new UpgradePath("Butter", Green,
                    T("Gebuttert", "+25 % Tempo.", 110, s => s.FireRate *= 1.25f),
                    T("Salzig", "+2 Kornschaden.", 210, s => s.ShardDamage += 2f),
                    T("Doppellauf", "2 Kolben pro Schuss.", 480, s => s.Projectiles += 1),
                    T("Butterschmelze", "Durchdringt Panzer, +50 % gegen gepanzerte Aliens.", 950, s => { s.ArmorPiercing = true; s.ArmoredBonus *= 1.5f; }),
                    T("Popcorn-Fabrik", "Doppeltes Tempo, 3 Kolben pro Schuss.", 1800, s => { s.FireRate *= 2f; s.Projectiles += 1; })),
            },
        };

        public static readonly TowerType Moerser = new TowerType
        {
            Name = "Kürbis-Mörser",
            ModelName = "KuerbisMoerser",
            ShortName = "Mörser",
            Description = "Artillerie mit riesiger Reichweite und großem Explosionsradius, aber langsam. Trifft nur Bodengegner.",
            Kind = TowerKind.Arc,
            Cost = 260,
            Color = new Color(0.95f, 0.5f, 0.1f),
            ShotShape = PrimitiveType.Sphere,
            ShotScale = new Vector3(0.32f, 0.28f, 0.32f),
            ShotColor = new Color(0.95f, 0.5f, 0.1f),
            Base = new TowerStats { Range = 5f, Damage = 30f, FireRate = 0.3f, Splash = 1.3f, ProjectileSpeed = 5f, HitsAir = false },
            Paths = new[]
            {
                new UpgradePath("Panzerknacker", Red,
                    T("Harte Schale", "Schwächt Panzer um 2.", 150, s => s.ArmorShred = 2f),
                    T("Panzerknacker", "Ignoriert Panzer.", 300, s => s.ArmorPiercing = true),
                    T("Schalensprenger", "Doppelter Schaden gegen gepanzerte Aliens.", 600, s => s.ArmoredBonus *= 2f),
                    T("Stahlkürbis", "+30 Schaden.", 1100, s => s.Damage += 30f),
                    T("Kürbis-Kanone", "Doppelter Schaden, dreifach gegen Panzer.", 2000, s => { s.Damage *= 2f; s.ArmoredBonus *= 1.5f; })),
                new UpgradePath("Kürbis-Regen", Blue,
                    T("Schnelllader", "+30 % Tempo.", 140, s => s.FireRate *= 1.3f),
                    T("Zwei Kürbisse", "2 Kürbisse pro Schuss.", 320, s => s.Projectiles += 1),
                    T("Kürbis-Hagel", "3 Kürbisse, +20 % Tempo.", 650, s => { s.Projectiles += 1; s.FireRate *= 1.2f; }),
                    T("Kürbisfeld", "5 Kürbisse pro Schuss.", 1200, s => s.Projectiles += 2),
                    T("Kürbis-Regen", "8 Kürbisse pro Schuss, +50 % Tempo.", 2100, s => { s.Projectiles += 3; s.FireRate *= 1.5f; })),
                new UpgradePath("Riesenkürbis", Green,
                    T("Großer Kürbis", "+30 % Explosionsradius.", 130, s => s.Splash *= 1.3f),
                    T("Betäubung", "Getroffene Aliens sind kurz betäubt.", 280, s => s.StunDuration = 0.4f),
                    T("Riesenkürbis", "+40 % Radius, +15 Schaden.", 600, s => { s.Splash *= 1.4f; s.Damage += 15f; }),
                    T("Schockwelle", "Längere Betäubung, stößt Aliens zurück.", 1100, s => { s.StunDuration = 1f; s.Knockback = 0.4f; }),
                    T("Halloween", "Unbegrenzte Reichweite, +40 Schaden.", 2000, s => { s.GlobalRange = true; s.Damage += 40f; })),
            },
        };

        public static readonly TowerType Tesla = new TowerType
        {
            Name = "Traktor-Tesla",
            ModelName = "TraktorTesla",
            ShortName = "Tesla",
            Description = "Ein umgebauter Traktor mit Teslaspule. Blitze springen von Alien zu Alien, auch auf Ufos.",
            Kind = TowerKind.Chain,
            Cost = 240,
            Color = new Color(0.45f, 0.8f, 1f),
            Base = new TowerStats { Range = 2.6f, Damage = 11f, FireRate = 1f, ChainCount = 2, ChainRange = 1.6f, ChainFalloff = 0.85f },
            Paths = new[]
            {
                new UpgradePath("Gewitter", Red,
                    T("Mehr Spannung", "Springt auf ein Alien mehr.", 140, s => s.ChainCount += 1),
                    T("Kettenblitz", "Springt auf 2 Aliens mehr.", 300, s => s.ChainCount += 2),
                    T("Gewitterfront", "Weitere Sprünge, +4 Schaden.", 620, s => { s.ChainRange *= 1.4f; s.Damage += 4f; }),
                    T("Blitzgewitter", "Springt auf 4 Aliens mehr.", 1200, s => s.ChainCount += 4),
                    T("Kugelblitz", "Zwei Blitze gleichzeitig, 6 Sprünge mehr.", 2100, s => { s.ChainCount += 6; s.Projectiles += 1; })),
                new UpgradePath("Überladung", Blue,
                    T("Funkenflug", "+4 Schaden.", 130, s => s.Damage += 4f),
                    T("Betäuber", "Blitze betäuben kurz.", 280, s => s.StunDuration = 0.3f),
                    T("Hochspannung", "+10 Schaden.", 600, s => s.Damage += 10f),
                    T("Überladung", "Längere Betäubung, wirkt auch auf Bosse.", 1100, s => { s.StunDuration = 0.8f; s.StunBoss = true; }),
                    T("Tesla-Turm", "Doppelter Schaden, ignoriert Panzer.", 2000, s => { s.Damage *= 2f; s.ArmorPiercing = true; })),
                new UpgradePath("Generator", Green,
                    T("Dynamo", "+25 % Tempo.", 120, s => s.FireRate *= 1.25f),
                    T("Antenne", "+30 % Reichweite, sieht getarnte Aliens.", 240, s => { s.Range *= 1.3f; s.SeesCamo = true; }),
                    T("Turbo", "+50 % Tempo.", 520, s => s.FireRate *= 1.5f),
                    T("Starkstrom", "Blitze verlieren beim Springen keinen Schaden.", 1000, s => s.ChainFalloff = 1f),
                    T("Kraftwerk", "Doppeltes Tempo, +30 % Reichweite.", 1900, s => { s.FireRate *= 2f; s.Range *= 1.3f; })),
            },
        };

        public static readonly TowerType Guelle = new TowerType
        {
            Name = "Güllefass",
            ModelName = "Guellefass",
            ShortName = "Gülle",
            Description = "Verteilt Gülle auf dem Weg. Aliens, die durchlaufen, nehmen Giftschaden und rutschen langsamer.",
            Kind = TowerKind.Puddle,
            Cost = 200,
            Color = new Color(0.45f, 0.3f, 0.12f),
            Base = new TowerStats
            {
                Range = 2.4f, FireRate = 0.35f, DotDps = 6f, DotDuration = 1f, Slow = 0.75f, SlowDuration = 0.5f,
                PuddleRadius = 0.6f, PuddleDuration = 5f, HitsAir = false,
            },
            Paths = new[]
            {
                new UpgradePath("Extra-Würzig", Red,
                    T("Würzig", "+50 % Giftschaden.", 110, s => s.DotDps *= 1.5f),
                    T("Ätzend", "Schwächt Panzer um 2.", 230, s => s.ArmorShred = 2f),
                    T("Lange Haltbarkeit", "Pfützen bleiben fast doppelt so lange.", 450, s => s.PuddleDuration *= 1.8f),
                    T("Extra-Würzig", "Zweieinhalbfacher Giftschaden.", 950, s => s.DotDps *= 2.5f),
                    T("Biogas", "Aliens nehmen dauerhaft 25 % mehr Schaden, mehr Gift.", 1800,
                        s => { s.PermanentVulnerability = 0.25f; s.DotDps *= 1.5f; })),
                new UpgradePath("Gülle-Fontäne", Blue,
                    T("Großer Schlauch", "Größere Pfützen.", 120, s => s.PuddleRadius *= 1.3f),
                    T("Fontäne", "Spritzt auch auf fliegende Ufos.", 260, s => s.HitsAir = true),
                    T("Zweite Düse", "2 Pfützen auf einmal.", 520, s => s.Projectiles += 1),
                    T("Sprühregen", "+50 % Reichweite, 3 Pfützen.", 1000, s => { s.Range *= 1.5f; s.Projectiles += 1; }),
                    T("Gülle-Tsunami", "Riesige Pfützen, 5 auf einmal.", 1800, s => { s.PuddleRadius *= 1.6f; s.Projectiles += 2; })),
                new UpgradePath("Matsch", Green,
                    T("Rutschig", "Stärkere Verlangsamung.", 100, s => s.Slow = 0.6f),
                    T("Schlammbad", "Noch stärkere Verlangsamung.", 240, s => s.Slow = 0.45f),
                    T("Rasenmatsch", "+40 % Tempo.", 480, s => s.FireRate *= 1.4f),
                    T("Sumpf", "Aliens kommen kaum voran.", 950, s => s.Slow = 0.3f),
                    T("Moor", "Doppelter Giftschaden, Aliens versinken fast.", 1700, s => { s.DotDps *= 2f; s.Slow = 0.25f; })),
            },
        };

        public static readonly TowerType Maehdrescher = new TowerType
        {
            Name = "Mähdrescher",
            ModelName = "Maschinenschuppen",
            IconName = "Maehdrescher",
            ShortName = "Mäher",
            Description = "Fährt selbst auf dem Weg in seiner Nähe hin und her und mäht Aliens nieder. Hoher Nahkampfschaden.",
            Kind = TowerKind.Harvester,
            Cost = 350,
            Color = new Color(0.3f, 0.65f, 0.25f),
            Base = new TowerStats { Range = 2.5f, Damage = 12f, FireRate = 2f, UnitCount = 1, UnitSpeed = 1.5f, UnitRadius = 0.45f, HitsAir = false },
            Paths = new[]
            {
                new UpgradePath("Turbo-Mäher", Red,
                    T("Schärfere Messer", "+6 Schaden.", 150, s => s.Damage += 6f),
                    T("Turbo", "+50 % Geschwindigkeit.", 300, s => s.UnitSpeed *= 1.5f),
                    T("Doppelmesser", "+12 Schaden.", 650, s => s.Damage += 12f),
                    T("Diesel-Monster", "+50 % Geschwindigkeit, +15 Schaden.", 1200, s => { s.UnitSpeed *= 1.5f; s.Damage += 15f; }),
                    T("Turbo-Mäher", "Doppelter Schaden, noch schneller.", 2100, s => { s.Damage *= 2f; s.UnitSpeed *= 1.3f; })),
                new UpgradePath("Strohballen-Presse", Blue,
                    T("Rückwärtsgang", "Schiebt Aliens zurück.", 140, s => s.Knockback = 0.25f),
                    T("Presse", "Verlangsamt getroffene Aliens.", 280, s => { s.Slow = 0.6f; s.SlowDuration = 1.5f; }),
                    T("Strohballen", "Getroffene Aliens bringen 25 % mehr Münzen.", 550, s => s.BonusReward = 0.25f),
                    T("Ballenwerfer", "Schiebt weiter zurück.", 1000, s => s.Knockback = 0.5f),
                    T("Großpresse", "+20 Schaden, breiteres Schneidwerk.", 2000, s => { s.Damage += 20f; s.UnitRadius *= 1.4f; })),
                new UpgradePath("Fuhrpark", Green,
                    T("Breites Schneidwerk", "Trifft in breiterem Streifen.", 130, s => s.UnitRadius *= 1.3f),
                    T("Größeres Feld", "Fährt eine längere Strecke ab.", 260, s => s.Range *= 1.4f),
                    T("Zweiter Mähdrescher", "2 Mähdrescher.", 600, s => s.UnitCount = 2),
                    T("Flutlicht", "Sieht getarnte Aliens, längere Strecke.", 900, s => { s.SeesCamo = true; s.Range *= 1.2f; }),
                    T("Erntekolonne", "3 Mähdrescher, +10 Schaden.", 1900, s => { s.UnitCount = 3; s.Damage += 10f; })),
            },
        };

        public static readonly TowerType Hofhund = new TowerType
        {
            Name = "Hofhund-Hütte",
            ModelName = "Hundehuette",
            ShortName = "Hofhund",
            Description = "Schickt Hofhunde auf den Weg. Sie halten Aliens fest und beißen zu. Bosse sind zu stark für sie.",
            Kind = TowerKind.Dogs,
            Cost = 220,
            Color = new Color(0.78f, 0.55f, 0.3f),
            Base = new TowerStats { Range = 2.2f, Damage = 8f, FireRate = 1f, UnitCount = 2, UnitHp = 60f, RespawnTime = 6f, HitsAir = false },
            Paths = new[]
            {
                new UpgradePath("Rudel", Red,
                    T("Welpe", "Ein Hund mehr.", 120, s => s.UnitCount += 1),
                    T("Rudel", "Noch ein Hund mehr.", 260, s => s.UnitCount += 1),
                    T("Leitwolf", "+6 Bissschaden.", 550, s => s.Damage += 6f),
                    T("Großes Rudel", "2 Hunde mehr.", 1000, s => s.UnitCount += 2),
                    T("Wolfsrudel", "2 Hunde mehr, +50 % Bissschaden.", 1900, s => { s.UnitCount += 2; s.Damage *= 1.5f; })),
                new UpgradePath("Bernhardiner", Blue,
                    T("Kräftig", "Hunde halten länger durch.", 130, s => s.UnitHp *= 1.6f),
                    T("Dickes Fell", "Hunde halten noch länger durch.", 280, s => s.UnitHp *= 1.6f),
                    T("Bernhardiner", "Große Hunde: doppelte Ausdauer, +8 Bissschaden.", 600, s => { s.UnitHp *= 2f; s.Damage += 8f; }),
                    T("Bollwerk", "Halten sogar Bosse fest.", 1100, s => s.BlocksBoss = true),
                    T("Retter", "Beißen Kuh-Träger dreifach.", 2000, s => { s.RescuesCows = true; s.UnitHp *= 1.5f; })),
                new UpgradePath("Schnauze", Green,
                    T("Spürnase", "Erschnüffeln getarnte Aliens, Bisse schwächen Panzer um 1.", 100, s => { s.SeesCamo = true; s.ArmorShred = Mathf.Max(s.ArmorShred, 1f); }),
                    T("Schnelle Pfoten", "Neue Hunde kommen schneller.", 220, s => s.RespawnTime *= 0.6f),
                    T("Kräftiger Biss", "+6 Bissschaden, schwächt Panzer um 2.", 500, s => { s.Damage += 6f; s.ArmorShred = 2f; }),
                    T("Tollwut", "Bisse vergiften.", 950, s => { s.DotDps = 6f; s.DotDuration = 2f; }),
                    T("Höllenhund", "Doppelter Bissschaden, Bisse setzen Aliens in Brand.", 1800, s => { s.Damage *= 2f; s.BurnDps = 10f; s.BurnDuration = 2f; })),
            },
        };

        public static readonly TowerType Vogelscheuche = new TowerType
        {
            Name = "Vogelscheuche",
            ModelName = "Vogelscheuche",
            ShortName = "Scheuche",
            Description = "Erschreckt Aliens: Sie bleiben kurz wie angewurzelt stehen. Macht selbst keinen Schaden. Bosse lachen nur.",
            Kind = TowerKind.Aura,
            Cost = 180,
            Color = new Color(0.55f, 0.25f, 0.15f),
            Base = new TowerStats { Range = 2f, Damage = 0f, FireRate = 0.5f, StunDuration = 0.8f, ScareCount = 1, HitsAir = false },
            Paths = new[]
            {
                new UpgradePath("Gruselscheuche", Red,
                    T("Gruselig", "Erschreckt 2 Aliens auf einmal.", 100, s => s.ScareCount += 1),
                    T("Kürbisfratze", "Erschreckt 4 Aliens auf einmal.", 220, s => s.ScareCount += 2),
                    T("Schreckgespenst", "Aliens bleiben länger stehen.", 480, s => s.StunDuration *= 1.5f),
                    T("Albtraum", "Erschreckt 8 Aliens auf einmal.", 1000, s => s.ScareCount += 4),
                    T("Gruselscheuche", "Erschreckt alle Aliens in Reichweite, noch länger.", 1800, s => { s.ScareCount = 99; s.StunDuration += 0.2f; })),
                new UpgradePath("Krähenschwarm", Blue,
                    T("Krähe", "Eine Krähe pickt Aliens (auch getarnte und Ufos).", 120,
                        s => { s.Damage = 3f; s.HitsAir = true; s.SeesCamo = true; }),
                    T("Zwei Krähen", "+3 Schaden.", 240, s => s.Damage += 3f),
                    T("Krähenschwarm", "+6 Schaden, +30 % Tempo.", 500, s => { s.Damage += 6f; s.FireRate *= 1.3f; }),
                    T("Rabenschwarm", "Schwächt Panzer um 3, +6 Schaden.", 950, s => { s.ArmorShred = 3f; s.Damage += 6f; }),
                    T("Hitchcock", "+15 Schaden, +50 % Tempo.", 1800, s => { s.Damage += 15f; s.FireRate *= 1.5f; })),
                new UpgradePath("Feldwächter", Green,
                    T("Weiter Blick", "+30 % Reichweite.", 90, s => s.Range *= 1.3f),
                    T("Rückzug", "Erschreckte Aliens weichen zurück.", 220, s => s.Knockback = 0.3f),
                    T("Panik", "Weichen weiter zurück, +20 % Tempo.", 480, s => { s.Knockback = 0.5f; s.FireRate *= 1.2f; }),
                    T("Schreckensherrschaft", "Erschreckte Aliens nehmen dauerhaft 20 % mehr Schaden.", 950, s => s.PermanentVulnerability = 0.2f),
                    T("Feldherr", "+50 % Reichweite, weichen weit zurück.", 1700, s => { s.Range *= 1.5f; s.Knockback = 0.7f; })),
            },
        };

        public static readonly TowerType[] All =
        {
            Mistgabel, EierHuhn, Bienenstock, Heuballen, Popcorn, Moerser, Tesla, Eismilch, Windmuehle,
            Guelle, Maehdrescher, Hofhund, Vogelscheuche, Wetterhahn, Hofladen,
        };

        public static string AbilityName(TowerAbility ability)
        {
            switch (ability)
            {
                case TowerAbility.Gabelsturm: return "Gabelsturm";
                case TowerAbility.Bauernaufstand: return "Bauernaufstand";
                case TowerAbility.Heulawine: return "Heulawine";
                case TowerAbility.Eiszeit: return "Eiszeit";
                default: return "";
            }
        }

        public static string TargetModeName(TargetMode mode)
        {
            switch (mode)
            {
                case TargetMode.Last: return "Letzter";
                case TargetMode.Strong: return "Stärkster";
                case TargetMode.Close: return "Nächster";
                default: return "Erster";
            }
        }
    }
}
