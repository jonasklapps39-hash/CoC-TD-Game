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
            Description = "Schleudert Heuballen im Bogen. Flächenschaden, gut gegen Gruppen und Panzer.",
            Kind = TowerKind.Arc,
            Cost = 180,
            Color = new Color(0.95f, 0.80f, 0.30f),
            Base = new TowerStats { Range = 3f, Damage = 14f, FireRate = 0.5f, Splash = 1f, ProjectileSpeed = 6f },
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
            Description = "Versprüht eiskalte Milch und verlangsamt alle Aliens in der Nähe.",
            Kind = TowerKind.Aura,
            Cost = 140,
            Color = new Color(0.65f, 0.88f, 1f),
            Base = new TowerStats { Range = 2f, Damage = 1f, FireRate = 2f, Slow = 0.6f, SlowDuration = 1f },
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
                    T("Sprühnebel", "+15 % Reichweite, trifft auch Fluggegner.", 200, s => s.Range *= 1.15f),
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

        public static readonly TowerType[] All = { Mistgabel, Heuballen, Eismilch };

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
