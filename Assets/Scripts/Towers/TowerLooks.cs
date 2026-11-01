using System.Collections.Generic;
using UnityEngine;

namespace MuhDefense
{
    /// <summary>Thema eines Upgrade-Pfads – bestimmt, welche Teile der Turm ab Stufe 3 und 5 trägt.</summary>
    public enum UpgradeLook
    {
        Power,
        Speed,
        Range,
        Fire,
        Ice,
        Poison,
        Gold,
        Money,
        Swarm,
        Electric,
    }

    /// <summary>
    /// Sichtbare Upgrades wie in Bloons TD6: Ab Stufe 3 trägt der Turm ein Teil passend zum Pfad
    /// (Flammen, Eiskristalle, Zahnräder …), auf Stufe 5 ein großes Teil, eine leuchtende Aura und eine Färbung.
    /// </summary>
    public static class TowerLooks
    {
        static readonly Dictionary<string, UpgradeLook> byPath = new Dictionary<string, UpgradeLook>
        {
            { "Schärfe", UpgradeLook.Power }, { "Tempo", UpgradeLook.Speed }, { "Weitblick", UpgradeLook.Range },
            { "Wucht", UpgradeLook.Power }, { "Feuer", UpgradeLook.Fire }, { "Mechanik", UpgradeLook.Speed },
            { "Kälte", UpgradeLook.Ice }, { "Druck", UpgradeLook.Range }, { "Sauermilch", UpgradeLook.Poison },
            { "Schwarm", UpgradeLook.Swarm }, { "Gift", UpgradeLook.Poison }, { "Honig", UpgradeLook.Gold },
            { "Orkan", UpgradeLook.Power }, { "Ufo-Fänger", UpgradeLook.Range }, { "Böen", UpgradeLook.Speed },
            { "Radar", UpgradeLook.Range }, { "Kampfhahn", UpgradeLook.Power }, { "Krähen", UpgradeLook.Swarm },
            { "Großmarkt", UpgradeLook.Money }, { "Bio-Siegel", UpgradeLook.Gold }, { "Lieferdienst", UpgradeLook.Speed },
            { "Maschinenhuhn", UpgradeLook.Speed }, { "Faule Eier", UpgradeLook.Poison }, { "Goldene Eier", UpgradeLook.Gold },
            { "Karamell", UpgradeLook.Gold }, { "Mega-Plopp", UpgradeLook.Power }, { "Butter", UpgradeLook.Speed },
            { "Panzerknacker", UpgradeLook.Power }, { "Kürbis-Regen", UpgradeLook.Speed }, { "Riesenkürbis", UpgradeLook.Fire },
            { "Gewitter", UpgradeLook.Electric }, { "Überladung", UpgradeLook.Electric }, { "Generator", UpgradeLook.Speed },
            { "Extra-Würzig", UpgradeLook.Poison }, { "Gülle-Fontäne", UpgradeLook.Range }, { "Matsch", UpgradeLook.Ice },
            { "Turbo-Mäher", UpgradeLook.Speed }, { "Strohballen-Presse", UpgradeLook.Power }, { "Fuhrpark", UpgradeLook.Gold },
            { "Rudel", UpgradeLook.Swarm }, { "Bernhardiner", UpgradeLook.Power }, { "Schnauze", UpgradeLook.Fire },
            { "Gruselscheuche", UpgradeLook.Electric }, { "Krähenschwarm", UpgradeLook.Swarm }, { "Feldwächter", UpgradeLook.Range },
        };

        public static UpgradeLook LookFor(UpgradePath p) => byPath.TryGetValue(p.Name, out var l) ? l : UpgradeLook.Power;

        /// <summary>Farbe, mit der das ganze Turm-Modell auf Stufe 5 eingefärbt wird.</summary>
        public static Color TintFor(UpgradeLook look, Color pathColor)
        {
            switch (look)
            {
                case UpgradeLook.Gold:
                case UpgradeLook.Money: return new Color(1f, 0.86f, 0.45f);
                case UpgradeLook.Fire: return new Color(1f, 0.72f, 0.55f);
                case UpgradeLook.Ice: return new Color(0.75f, 0.9f, 1f);
                case UpgradeLook.Poison: return new Color(0.75f, 1f, 0.65f);
                case UpgradeLook.Electric: return new Color(0.75f, 0.85f, 1f);
                case UpgradeLook.Power: return new Color(0.82f, 0.82f, 0.9f);
                default: return Color.Lerp(Color.white, pathColor, 0.3f);
            }
        }

        static readonly Color GoldColor = new Color(1f, 0.8f, 0.2f);
        static readonly Color Metal = new Color(0.65f, 0.67f, 0.75f);
        static readonly Color DarkMetal = new Color(0.32f, 0.33f, 0.38f);

        /// <summary>
        /// Baut die Teile für einen Pfad. <paramref name="slot"/> (0..2) verteilt die Pfade um den Turm,
        /// damit sich die Teile von zwei Pfaden nicht überdecken.
        /// </summary>
        public static void Build(Transform parent, UpgradeFx fx, UpgradeLook look, int tier, Color color, float top, int slot)
        {
            if (tier < 3) return;
            bool big = tier >= 5;
            float side = slot == 0 ? -1f : slot == 1 ? 1f : 0f;
            var sidePos = new Vector3(0.36f * side, top * 0.55f, slot == 2 ? -0.32f : 0.05f);

            switch (look)
            {
                case UpgradeLook.Gold:
                {
                    // Goldring um den Sockel, auf Stufe 5 eine Krone obendrauf
                    Ring(parent, new Vector3(0f, 0.16f, 0f), 0.44f, GoldColor);
                    if (big) Crown(parent, new Vector3(0f, top + 0.05f, 0f), 0.2f, fx);
                    else Visuals.Prim(PrimitiveType.Sphere, parent, sidePos, Vector3.one * 0.14f, GoldColor, "Goldkugel");
                    break;
                }
                case UpgradeLook.Money:
                {
                    int coins = big ? 7 : 3;
                    for (int i = 0; i < coins; i++)
                    {
                        Visuals.Prim(PrimitiveType.Cylinder, parent, sidePos + new Vector3(0f, -sidePos.y + 0.12f + i * 0.05f, 0f),
                            new Vector3(0.16f, 0.02f, 0.16f), GoldColor, "Münze");
                    }
                    if (big) fx.AddSpin(Visuals.Prim(PrimitiveType.Cylinder, parent, new Vector3(0f, top + 0.3f, 0f),
                        new Vector3(0.26f, 0.03f, 0.26f), GoldColor, "Riesenmünze").transform, new Vector3(0f, 0f, 0f), new Vector3(0f, 180f, 0f), true);
                    break;
                }
                case UpgradeLook.Fire:
                {
                    int n = big ? 6 : 2;
                    for (int i = 0; i < n; i++)
                    {
                        float a = big ? i * Mathf.PI * 2f / n : (i == 0 ? -0.6f : 0.6f) + (slot == 2 ? Mathf.PI : 0f);
                        float r = big ? 0.42f : 0.3f;
                        var p = big ? new Vector3(Mathf.Cos(a) * r, 0.2f, Mathf.Sin(a) * r) : sidePos + new Vector3(Mathf.Cos(a) * 0.12f, 0f, Mathf.Sin(a) * 0.12f);
                        var flame = Visuals.Prim(PrimitiveType.Sphere, parent, p, new Vector3(0.14f, 0.24f, 0.14f), new Color(1f, 0.45f, 0.1f), "Flamme", outline: false);
                        Visuals.Prim(PrimitiveType.Sphere, flame.transform, new Vector3(0f, 0.15f, 0f), new Vector3(0.6f, 0.6f, 0.6f), new Color(1f, 0.85f, 0.25f), "Kern", outline: false);
                        fx.AddFlicker(flame.transform);
                    }
                    break;
                }
                case UpgradeLook.Ice:
                {
                    int n = big ? 6 : 3;
                    for (int i = 0; i < n; i++)
                    {
                        float a = i * Mathf.PI * 2f / n + slot;
                        float r = big ? 0.4f : 0.2f;
                        var basePos = big ? new Vector3(0f, 0.12f, 0f) : new Vector3(sidePos.x, 0.12f, sidePos.z);
                        var c = Visuals.Prim(PrimitiveType.Cube, parent, basePos + new Vector3(Mathf.Cos(a) * r, 0.08f, Mathf.Sin(a) * r),
                            new Vector3(0.09f, big ? 0.34f : 0.22f, 0.09f), new Color(0.7f, 0.92f, 1f), "Eiskristall");
                        c.transform.localRotation = Quaternion.Euler(Mathf.Sin(a) * 25f, a * Mathf.Rad2Deg, Mathf.Cos(a) * 25f);
                    }
                    if (big)
                    {
                        var crystal = Visuals.Prim(PrimitiveType.Cube, parent, new Vector3(0f, top + 0.25f, 0f), new Vector3(0.18f, 0.32f, 0.18f), new Color(0.75f, 0.95f, 1f), "Eiskrone");
                        crystal.transform.localRotation = Quaternion.Euler(45f, 0f, 45f);
                        fx.AddSpin(crystal.transform, Vector3.zero, new Vector3(0f, 60f, 0f), true);
                    }
                    break;
                }
                case UpgradeLook.Speed:
                {
                    // Zahnrad an der Seite, auf Stufe 5 zwei große
                    fx.AddSpin(Gear(parent, sidePos, big ? 0.24f : 0.16f, Metal), Vector3.zero, new Vector3(0f, 0f, 240f), false);
                    if (big) fx.AddSpin(Gear(parent, sidePos + new Vector3(0f, 0.3f, -0.05f), 0.15f, GoldColor), Vector3.zero, new Vector3(0f, 0f, -360f), false);
                    break;
                }
                case UpgradeLook.Range:
                {
                    // Fahnenmast, auf Stufe 5 eine Antenne mit leuchtender Spitze
                    float h = big ? top + 0.7f : top + 0.25f;
                    var mastPos = new Vector3(0.36f * (side == 0f ? 1f : side), 0f, slot == 2 ? -0.3f : -0.2f);
                    Visuals.Prim(PrimitiveType.Cylinder, parent, mastPos + new Vector3(0f, h / 2f, 0f), new Vector3(0.04f, h / 2f, 0.04f), DarkMetal, "Mast");
                    var flag = Visuals.Prim(PrimitiveType.Cube, parent, mastPos + new Vector3(0.12f, h - 0.08f, 0f), new Vector3(0.22f, 0.14f, 0.02f), color, "Fahne");
                    fx.AddWave(flag.transform);
                    if (big)
                    {
                        var tip = Visuals.Prim(PrimitiveType.Sphere, parent, mastPos + new Vector3(0f, h + 0.06f, 0f), Vector3.one * 0.12f, new Color(1f, 0.35f, 0.25f), "Antennenspitze");
                        fx.AddPulse(tip.transform);
                    }
                    break;
                }
                case UpgradeLook.Poison:
                {
                    int n = big ? 5 : 3;
                    for (int i = 0; i < n; i++)
                    {
                        var b = Visuals.Prim(PrimitiveType.Sphere, parent, sidePos + new Vector3((i - n / 2) * 0.08f, 0.05f * i, 0f),
                            Vector3.one * (0.07f + 0.02f * (i % 2)), new Color(0.45f, 0.95f, 0.3f), "Giftblase", outline: false);
                        fx.AddBob(b.transform, i * 0.7f);
                    }
                    if (big)
                    {
                        Visuals.Prim(PrimitiveType.Cylinder, parent, new Vector3(0f, 0.17f, 0f), new Vector3(0.95f, 0.01f, 0.95f), new Color(0.35f, 0.75f, 0.2f), "Giftpfütze", outline: false);
                    }
                    break;
                }
                case UpgradeLook.Swarm:
                {
                    // Kleine Begleiter (Bienen, Krähen, Welpen) kreisen um den Turm
                    int n = big ? 5 : 2;
                    var orbit = new GameObject("Schwarm").transform;
                    orbit.SetParent(parent, false);
                    orbit.localPosition = new Vector3(0f, top * 0.8f, 0f);
                    for (int i = 0; i < n; i++)
                    {
                        float a = i * Mathf.PI * 2f / n;
                        var buddy = Visuals.Prim(PrimitiveType.Sphere, orbit, new Vector3(Mathf.Cos(a) * 0.5f, Mathf.Sin(i) * 0.08f, Mathf.Sin(a) * 0.5f),
                            new Vector3(0.1f, 0.08f, 0.13f), color, "Begleiter");
                        Visuals.Prim(PrimitiveType.Sphere, buddy.transform, new Vector3(0f, 0.6f, 0f), new Vector3(0.9f, 0.3f, 0.6f), Color.white, "Flügel", outline: false);
                    }
                    fx.AddSpin(orbit, Vector3.zero, new Vector3(0f, big ? 140f : 90f, 0f), true);
                    break;
                }
                case UpgradeLook.Electric:
                {
                    var coilPos = new Vector3(sidePos.x, 0f, sidePos.z);
                    int rings = big ? 4 : 2;
                    for (int i = 0; i < rings; i++)
                    {
                        Visuals.Prim(PrimitiveType.Cylinder, parent, coilPos + new Vector3(0f, 0.25f + i * 0.1f, 0f),
                            new Vector3(0.18f - i * 0.02f, 0.015f, 0.18f - i * 0.02f), new Color(0.85f, 0.5f, 0.2f), "Spule");
                    }
                    var spark = Visuals.Prim(PrimitiveType.Sphere, parent, coilPos + new Vector3(0f, 0.3f + rings * 0.1f, 0f), Vector3.one * 0.12f, new Color(0.55f, 0.9f, 1f), "Funke", outline: false);
                    fx.AddPulse(spark.transform);
                    if (big)
                    {
                        var orbit = new GameObject("Funken").transform;
                        orbit.SetParent(parent, false);
                        orbit.localPosition = new Vector3(0f, top + 0.15f, 0f);
                        for (int i = 0; i < 3; i++)
                        {
                            float a = i * Mathf.PI * 2f / 3f;
                            Visuals.Prim(PrimitiveType.Sphere, orbit, new Vector3(Mathf.Cos(a) * 0.3f, 0f, Mathf.Sin(a) * 0.3f), Vector3.one * 0.08f, new Color(0.7f, 0.95f, 1f), "Blitzkugel", outline: false);
                        }
                        fx.AddSpin(orbit, Vector3.zero, new Vector3(0f, 300f, 0f), true);
                    }
                    break;
                }
                default: // Power: Stacheln und Panzerplatten
                {
                    int n = big ? 8 : 4;
                    for (int i = 0; i < n; i++)
                    {
                        float a = i * Mathf.PI * 2f / n + 0.3f;
                        var spike = Visuals.Prim(PrimitiveType.Cube, parent, new Vector3(Mathf.Cos(a) * 0.44f, 0.18f, Mathf.Sin(a) * 0.44f),
                            new Vector3(0.07f, big ? 0.26f : 0.18f, 0.07f), Metal, "Stachel");
                        spike.transform.localRotation = Quaternion.Euler(0f, -a * Mathf.Rad2Deg, 25f) * Quaternion.Euler(0f, 0f, 0f);
                    }
                    if (big)
                    {
                        Visuals.Prim(PrimitiveType.Cube, parent, new Vector3(-0.3f, top * 0.6f, 0f), new Vector3(0.12f, 0.22f, 0.3f), DarkMetal, "Panzerplatte");
                        Visuals.Prim(PrimitiveType.Cube, parent, new Vector3(0.3f, top * 0.6f, 0f), new Vector3(0.12f, 0.22f, 0.3f), DarkMetal, "Panzerplatte");
                    }
                    break;
                }
            }

            if (big)
            {
                // Leuchtende Aura am Boden in Pfadfarbe
                var aura = Visuals.Prim(PrimitiveType.Cylinder, parent, new Vector3(0f, 0.02f, 0f), new Vector3(1.05f, 0.005f, 1.05f),
                    Color.Lerp(color, Color.white, 0.3f), "Aura", outline: false);
                fx.AddPulse(aura.transform, 0.06f);
            }
        }

        static void Ring(Transform parent, Vector3 pos, float radius, Color color)
        {
            for (int i = 0; i < 12; i++)
            {
                float a = i * Mathf.PI * 2f / 12f;
                var seg = Visuals.Prim(PrimitiveType.Cube, parent, pos + new Vector3(Mathf.Cos(a) * radius, 0f, Mathf.Sin(a) * radius),
                    new Vector3(0.24f, 0.05f, 0.06f), color, "Goldring");
                seg.transform.localRotation = Quaternion.Euler(0f, -a * Mathf.Rad2Deg + 90f, 0f);
            }
        }

        static void Crown(Transform parent, Vector3 pos, float r, UpgradeFx fx)
        {
            var crown = new GameObject("Krone").transform;
            crown.SetParent(parent, false);
            crown.localPosition = pos;
            Visuals.Prim(PrimitiveType.Cylinder, crown, Vector3.zero, new Vector3(r * 2f, 0.05f, r * 2f), GoldColor, "Kronenreif");
            for (int i = 0; i < 5; i++)
            {
                float a = i * Mathf.PI * 2f / 5f;
                Visuals.Prim(PrimitiveType.Sphere, crown, new Vector3(Mathf.Cos(a) * r * 0.9f, 0.1f, Mathf.Sin(a) * r * 0.9f), new Vector3(0.07f, 0.14f, 0.07f), GoldColor, "Zacke");
            }
            Visuals.Prim(PrimitiveType.Sphere, crown, new Vector3(0f, 0.08f, 0f), Vector3.one * 0.08f, new Color(0.9f, 0.15f, 0.2f), "Edelstein");
            fx.AddBob(crown, 0f);
        }

        static Transform Gear(Transform parent, Vector3 pos, float r, Color color)
        {
            var gear = new GameObject("Zahnrad").transform;
            gear.SetParent(parent, false);
            gear.localPosition = pos;
            var disc = Visuals.Prim(PrimitiveType.Cylinder, gear, Vector3.zero, new Vector3(r * 2f, 0.03f, r * 2f), color, "Scheibe");
            disc.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            for (int i = 0; i < 8; i++)
            {
                float a = i * Mathf.PI / 4f;
                var tooth = Visuals.Prim(PrimitiveType.Cube, gear, new Vector3(Mathf.Cos(a) * r, Mathf.Sin(a) * r, 0f), new Vector3(0.06f, 0.06f, 0.05f), color, "Zahn");
                tooth.transform.localRotation = Quaternion.Euler(0f, 0f, a * Mathf.Rad2Deg);
            }
            Visuals.Prim(PrimitiveType.Sphere, gear, new Vector3(0f, 0f, 0.02f), Vector3.one * r * 0.6f, DarkMetal, "Nabe");
            return gear;
        }
    }

    /// <summary>Kleine Animationen der Upgrade-Teile: drehen, flackern, wehen, pulsieren, schweben.</summary>
    public class UpgradeFx : MonoBehaviour
    {
        enum Kind { Spin, Flicker, Wave, Pulse, Bob }

        class Item
        {
            public Transform T;
            public Kind Kind;
            public Vector3 Axis;
            public bool Local;
            public Vector3 BaseScale;
            public Vector3 BasePos;
            public float Phase;
            public float Amount;
        }

        readonly List<Item> items = new List<Item>();

        public void AddSpin(Transform t, Vector3 unused, Vector3 degPerSecond, bool local) =>
            items.Add(new Item { T = t, Kind = Kind.Spin, Axis = degPerSecond, Local = local });

        public void AddFlicker(Transform t) => items.Add(new Item { T = t, Kind = Kind.Flicker, BaseScale = t.localScale, Phase = Random.value * 10f });

        public void AddWave(Transform t) => items.Add(new Item { T = t, Kind = Kind.Wave, Phase = Random.value * 10f });

        public void AddPulse(Transform t, float amount = 0.2f) => items.Add(new Item { T = t, Kind = Kind.Pulse, BaseScale = t.localScale, Amount = amount, Phase = Random.value * 10f });

        public void AddBob(Transform t, float phase) => items.Add(new Item { T = t, Kind = Kind.Bob, BasePos = t.localPosition, Phase = phase });

        void Update()
        {
            float time = Time.time;
            float dt = Time.deltaTime;
            foreach (var it in items)
            {
                if (it.T == null) continue;
                switch (it.Kind)
                {
                    case Kind.Spin:
                        it.T.Rotate(it.Axis * dt, Space.Self);
                        break;
                    case Kind.Flicker:
                        float f = 1f + 0.18f * Mathf.Sin(time * 17f + it.Phase) + 0.08f * Mathf.Sin(time * 31f + it.Phase);
                        it.T.localScale = new Vector3(it.BaseScale.x, it.BaseScale.y * f, it.BaseScale.z);
                        break;
                    case Kind.Wave:
                        it.T.localRotation = Quaternion.Euler(0f, Mathf.Sin(time * 4f + it.Phase) * 20f, 0f);
                        break;
                    case Kind.Pulse:
                        float p = 1f + it.Amount * Mathf.Sin(time * 4f + it.Phase);
                        it.T.localScale = new Vector3(it.BaseScale.x * p, it.BaseScale.y, it.BaseScale.z * p);
                        break;
                    case Kind.Bob:
                        it.T.localPosition = it.BasePos + Vector3.up * (0.04f * Mathf.Sin(time * 3f + it.Phase));
                        break;
                }
            }
        }
    }
}
