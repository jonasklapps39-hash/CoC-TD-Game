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
                    T("Eiszeit", "Fähigkeit: Friert alle Aliens auf der Karte 4 Sekunden ein.", 1700,
                        s => { s.Ability = TowerAbility.Eiszeit; s.AbilityCooldown = 40f; s.Damage += 2f; })),
                new UpgradePath("Druck", Blue,
                    T("Druckpumpe", "+25 % Reichweite.", 90, s => s.Range *= 1.25f),
                    T("Sprühnebel", "+15 % Reichweite, trifft auch fliegende Ufos.", 200, s => { s.Range *= 1.15f; s.HitsAir = true; }),
                    T("Milchpfützen", "Verlangsamung hält länger und ist stärker.", 450, s => { s.SlowDuration = 2.5f; s.Slow -= 0.05f; }),
                    T("Hochdruck", "Stößt Aliens ein Stück zurück (nicht Bosse).", 1000, s => s.Knockback = 0.25f),
                    T("Milch-Monsun", "Doppelte Reichweite und stärkerer Rückstoß.", 1700, s => { s.Range *= 2f; s.Knockback = 0.45f; })),
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
            ShortName = "Bienen",
            Description = "Bienen verfolgen die Aliens, treffen auch Ufos und stechen mit Gift (Schaden über Zeit).",
            Kind = TowerKind.Projectile,
            Cost = 160,
            Color = new Color(1f, 0.8f, 0.2f),
            Base = new TowerStats { Range = 2.8f, Damage = 2f, FireRate = 1.1f, Projectiles = 2, ProjectileSpeed = 6f, DotDps = 3f, DotDuration = 2f },
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
            ShortName = "Windmühle",
            Description = "Pustet Aliens den Weg zurück. Richtet vierfachen Schaden an fliegenden Ufos an.",
            Kind = TowerKind.Aura,
            Cost = 220,
            Color = new Color(0.85f, 0.9f, 1f),
            Base = new TowerStats { Range = 2.2f, Damage = 2f, FireRate = 0.6f, Knockback = 0.3f, AirDamageMultiplier = 4f },
            Paths = new[]
            {
                new UpgradePath("Orkan", Red,
                    T("Starker Wind", "Stößt weiter zurück.", 120, s => s.Knockback += 0.15f),
                    T("Großes Rad", "+25 % Reichweite.", 220, s => s.Range *= 1.25f),
                    T("Sturmböe", "Stößt noch weiter zurück und pustet öfter.", 480, s => { s.Knockback += 0.2f; s.FireRate *= 1.3f; }),
                    T("Wirbelsturm", "Stößt auch große Aliens zurück und macht +6 Schaden.", 1000, s => { s.Damage += 6f; s.Knockback += 0.2f; }),
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
                    T("Rabenkönig", "Sehr schnelles Picken, +10 Schaden.", 1600, s => { s.FireRate *= 2f; s.Damage += 10f; })),
            },
        };

        public static readonly TowerType Hofladen = new TowerType
        {
            Name = "Hofladen",
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
                    T("Großmarkt", "220 Münzen pro Welle.", 1200, s => s.IncomePerWave = 220),
                    T("Handelsimperium", "350 Münzen pro Welle.", 2200, s => s.IncomePerWave = 350)),
                new UpgradePath("Bio-Siegel", Blue,
                    T("Stammkunden", "Upgrades von Türmen in der Nähe 5 % billiger.", 120, s => s.SupportDiscount = 0.05f),
                    T("Bio-Siegel", "Upgrades in der Nähe 10 % billiger.", 250, s => s.SupportDiscount = 0.1f),
                    T("Großer Laden", "Größerer Umkreis.", 500, s => s.Range *= 1.5f),
                    T("Genossenschaft", "Upgrades in der Nähe 15 % billiger.", 1000, s => s.SupportDiscount = 0.15f),
                    T("Bauernverband", "Upgrades in der Nähe 20 % billiger, noch größerer Umkreis.", 1800,
                        s => { s.SupportDiscount = 0.2f; s.Range *= 1.3f; })),
                new UpgradePath("Lieferdienst", Green,
                    T("Fahrradbote", "Während der Welle alle 5 Sekunden 5 Münzen.", 140, s => s.IncomeDuringWave = 5),
                    T("Lieferwagen", "Alle 5 Sekunden 10 Münzen.", 280, s => s.IncomeDuringWave = 10),
                    T("Express", "Alle 5 Sekunden 20 Münzen.", 600, s => s.IncomeDuringWave = 20),
                    T("Lieferflotte", "Alle 5 Sekunden 35 Münzen.", 1200, s => s.IncomeDuringWave = 35),
                    T("Milchstraße", "Alle 5 Sekunden 60 Münzen.", 2200, s => s.IncomeDuringWave = 60)),
            },
        };

        public static readonly TowerType[] All = { Mistgabel, Heuballen, Eismilch, Bienenstock, Windmuehle, Wetterhahn, Hofladen };

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
