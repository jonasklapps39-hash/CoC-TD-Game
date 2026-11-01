using System.Collections.Generic;
using UnityEngine;

namespace MuhDefense
{
    /// <summary>
    /// Ein Held im Level (siehe Spielkonzept, Kapitel 9): einmal pro Level kostenlos aufstellbar,
    /// steigt durch besiegte Aliens und geschaffte Wellen von Stufe 1 bis 10 auf und hat zwei Fähigkeiten
    /// (ab Stufe 3 und ab Stufe 7). Welcher Held es ist, bestimmt <see cref="HeroType"/>.
    /// </summary>
    public class Hero : MonoBehaviour
    {
        public const int MaxLevel = 10;
        public const int Ability1Level = 3;
        public const int Ability2Level = 7;

        /// <summary>Gesamte Erfahrung, die für Stufe 1..10 nötig ist.</summary>
        static readonly int[] XpForLevel = { 0, 60, 150, 280, 450, 670, 950, 1300, 1720, 2200 };

        public HeroType Type { get; private set; }
        public int Level { get; private set; } = 1;
        public int Xp { get; private set; }
        public Vector2Int Cell { get; private set; }
        public TargetMode TargetMode { get; private set; } = TargetMode.First;
        public TowerStats Stats { get; private set; }
        public float Ability1CooldownLeft { get; private set; }
        public float Ability2CooldownLeft { get; private set; }

        public bool HasAbility1 => Level >= Ability1Level;
        public bool HasAbility2 => Level >= Ability2Level;
        public bool Ability1Ready => HasAbility1 && Ability1CooldownLeft <= 0f;
        public bool Ability2Ready => HasAbility2 && Ability2CooldownLeft <= 0f;

        /// <summary>Fortschritt zur nächsten Stufe (0..1).</summary>
        public float XpProgress
        {
            get
            {
                if (Level >= MaxLevel) return 1f;
                int from = XpForLevel[Level - 1], to = XpForLevel[Level];
                return Mathf.Clamp01((Xp - from) / (float)(to - from));
            }
        }

        LevelManager level;
        Transform body;
        LineRenderer rangeCircle;
        float cooldown;
        /// <summary>Trainingsstufe aus der Heldenhütte (0..5).</summary>
        int training;

        public void Init(LevelManager level, Vector2Int cell, HeroType type)
        {
            this.level = level;
            Type = type;
            Cell = cell;
            name = type.Name;
            transform.position = LevelManager.CellCenter(cell);
            training = GameFlow.Farm.HeroTrainingLevel(type);
            RecalculateStats();
            body = new GameObject("Körper").transform;
            body.SetParent(transform, false);
            Visuals.Prim(PrimitiveType.Cylinder, transform, new Vector3(0f, 0.04f, 0f), new Vector3(0.85f, 0.04f, 0.85f), new Color(1f, 0.82f, 0.25f), "Heldenring");
            var model = ModelLibrary.Spawn(type.ModelName, body, type.Kind == HeroKind.Berta ? 0.85f : 1.15f);
            var skin = GameFlow.Farm.SelectedHeroSkin(type);
            if (model != null && skin != null)
            {
                foreach (var r in model.GetComponentsInChildren<Renderer>()) r.sharedMaterial = Visuals.TintedVertexMaterial(skin.Tint);
            }
            if (model == null)
            {
                Visuals.Prim(PrimitiveType.Capsule, body, new Vector3(0f, 0.45f, 0f), new Vector3(0.35f, 0.32f, 0.35f), new Color(0.8f, 0.2f, 0.15f), "Körper");
                Visuals.Prim(PrimitiveType.Sphere, body, new Vector3(0f, 0.9f, 0f), Vector3.one * 0.28f, new Color(1f, 0.78f, 0.62f), "Kopf");
            }
            rangeCircle = Visuals.Circle(transform, Stats.Range, new Color(1f, 0.85f, 0.3f, 0.9f));
            rangeCircle.enabled = false;
        }

        public void SetSelected(bool selected) => rangeCircle.enabled = selected;

        void RecalculateStats()
        {
            int l = Level - 1;
            switch (Type.Kind)
            {
                case HeroKind.Gertrud:
                    Stats = new TowerStats
                    {
                        Range = 2.4f + 0.1f * l,
                        Damage = 3f + 1.2f * l,
                        FireRate = 1f + 0.05f * l,
                        Projectiles = Level >= 5 ? 2 : 1,
                        ProjectileSpeed = 8f,
                        Slow = Level >= 4 ? 0.75f : 0.85f,
                        SlowDuration = 1.2f,
                        SeesCamo = Level >= Ability1Level,
                        HitsAir = true,
                        SupportRateBonus = 0.1f + 0.02f * l,
                    };
                    break;
                case HeroKind.Berta:
                    Stats = new TowerStats
                    {
                        Range = 2.8f + 0.1f * l,
                        Damage = 6f + 2.5f * l,
                        FireRate = 1.2f + 0.05f * l,
                        Projectiles = Level >= 5 ? 2 : 1,
                        SeesCamo = Level >= Ability1Level,
                        HitsAir = true,
                        ArmorPiercing = Level >= 6,
                    };
                    break;
                default:
                    Stats = new TowerStats
                    {
                        Range = 2.6f + 0.1f * l,
                        Damage = 5f + 2f * l,
                        FireRate = 1f + 0.07f * l,
                        Projectiles = Level >= 9 ? 5 : Level >= 5 ? 4 : 3,
                        ProjectileSpeed = 14f,
                        SeesCamo = Level >= Ability1Level,
                        HitsAir = true,
                        ArmorPiercing = Level >= 8,
                    };
                    break;
            }
            // Dauerhaftes Training in der Heldenhütte
            Stats.Damage *= HeroTraining.DamageFactor(training);
            Stats.Range *= HeroTraining.RangeFactor(training);
        }

        /// <summary>Startstufe (Heldenhütte, Talent „Veteran“).</summary>
        public void SetStartLevel(int startLevel)
        {
            startLevel = Mathf.Clamp(startLevel, 1, MaxLevel);
            if (startLevel <= Level) return;
            Level = startLevel;
            Xp = XpForLevel[Level - 1];
            RecalculateStats();
            if (rangeCircle != null) Visuals.SetCircleRadius(rangeCircle, Stats.Range);
        }

        public void AddXp(int amount)
        {
            if (Level >= MaxLevel) return;
            Xp += amount;
            bool leveled = false;
            while (Level < MaxLevel && Xp >= XpForLevel[Level])
            {
                Level++;
                leveled = true;
            }
            if (!leveled) return;
            RecalculateStats();
            if (rangeCircle != null) Visuals.SetCircleRadius(rangeCircle, Stats.Range);
            RingEffect.Spawn(level.transform, transform.position, 1.1f, new Color(1f, 0.85f, 0.3f, 1f), 0.6f);
            string extra = Level == Ability1Level ? $" – {Type.Ability1Name}!" : Level == Ability2Level ? $" – {Type.Ability2Name}!" : "";
            level.AddFloatingText(transform.position + Vector3.up * 1.4f, $"Stufe {Level}{extra}", new Color(1f, 0.85f, 0.3f));
            GameAudio.Play(SoundId.LevelUp);
        }

        public void CycleTargetMode() => TargetMode = (TargetMode)(((int)TargetMode + 1) % 4);

        // ---------------------------------------------------------------- Texte für das Infofenster

        public string AttackText
        {
            get
            {
                var st = Stats;
                string extra = (st.SeesCamo ? ", sieht Getarnte" : "") + (st.ArmorPiercing ? ", durchschlägt Panzer" : "");
                switch (Type.Kind)
                {
                    case HeroKind.Gertrud:
                        return $"Kekse: {st.Projectiles} × {st.Damage:0} Schaden, verlangsamen; Türme in Reichweite +{st.SupportRateBonus * 100f:0} % Tempo{extra}";
                    case HeroKind.Berta:
                        return $"Laseraugen: {st.Projectiles} × {st.Damage:0} Schaden, {st.FireRate:0.0}/s; Schutzkreis: hier schnappt kein Alien eine Kuh{extra}";
                    default:
                        return $"Schrotflinte: {st.Projectiles} Kugeln à {st.Damage:0} Schaden, {st.FireRate:0.0} Schüsse/s{extra}";
                }
            }
        }

        public string Ability1Text
        {
            get
            {
                switch (Type.Kind)
                {
                    case HeroKind.Gertrud: return $"{Type.Ability1Name}: {Ability1Damage:0} Flächenschaden, verlangsamt stark";
                    case HeroKind.Berta: return $"{Type.Ability1Name}: stößt Aliens in Reichweite weit zurück ({Ability1Damage:0} Schaden)";
                    default: return $"{Type.Ability1Name}: {Ability1Damage:0} Schaden an allen Aliens in Reichweite";
                }
            }
        }

        public string Ability2Text
        {
            get
            {
                switch (Type.Kind)
                {
                    case HeroKind.Gertrud: return $"{Type.Ability2Name}: alle Aliens bleiben 4 Sekunden verzückt stehen (Bosse 2 Sekunden)";
                    case HeroKind.Berta: return $"{Type.Ability2Name}: knackt alle Panzer, holt Ufos vom Himmel ({Ability2Damage:0} Schaden)";
                    default: return $"{Type.Ability2Name}: überrollt alle Aliens am Boden ({Ability2Damage:0} Schaden)";
                }
            }
        }

        float Ability1Damage => Type.Kind == HeroKind.Gertrud ? 25f + 10f * Level : Type.Kind == HeroKind.Berta ? 20f + 8f * Level : 30f + 12f * Level;
        float Ability2Damage => Type.Kind == HeroKind.Berta ? 40f + 10f * Level : 100f + 15f * Level;

        // ---------------------------------------------------------------- Fähigkeiten

        public void UseAbility1()
        {
            if (!Ability1Ready) return;
            Ability1CooldownLeft = Type.Ability1Cooldown * level.Bonuses.AbilityCooldownFactor * HeroTraining.CooldownFactor(training);
            var enemies = level.Enemies;
            switch (Type.Kind)
            {
                case HeroKind.Gertrud:
                {
                    // Apfelkuchen auf das vorderste Alien in Reichweite
                    var targets = FindTargets(1);
                    Vector3 center = targets.Count > 0 ? targets[0].transform.position : transform.position;
                    for (int i = enemies.Count - 1; i >= 0; i--)
                    {
                        if (i >= enemies.Count) continue;
                        var e = enemies[i];
                        if (Vector3.Distance(e.transform.position, center) > 1.8f) continue;
                        e.ApplySlow(0.5f, 3f);
                        e.TakeDamage(Ability1Damage, true);
                    }
                    RingEffect.Spawn(level.transform, center, 1.8f, new Color(0.95f, 0.75f, 0.4f, 1f), 0.5f);
                    Effects.Burst(level.transform, center + Vector3.up * 0.3f, new Color(0.9f, 0.6f, 0.3f), 16, 3f);
                    GameAudio.Play(SoundId.Boom);
                    break;
                }
                case HeroKind.Berta:
                    for (int i = enemies.Count - 1; i >= 0; i--)
                    {
                        if (i >= enemies.Count) continue;
                        var e = enemies[i];
                        if (e.IsFlying || !InRange(e)) continue;
                        e.PushBack(2.5f);
                        e.TakeDamage(Ability1Damage, true);
                    }
                    RingEffect.Spawn(level.transform, transform.position, Stats.Range, new Color(1f, 0.95f, 0.8f, 1f), 0.4f);
                    GameAudio.Play(SoundId.Moo);
                    GameAudio.Play(SoundId.Wind);
                    break;
                default:
                    for (int i = enemies.Count - 1; i >= 0; i--)
                    {
                        if (i >= enemies.Count) continue;
                        var e = enemies[i];
                        if (InRange(e)) e.TakeDamage(Ability1Damage, true);
                    }
                    RingEffect.Spawn(level.transform, transform.position, Stats.Range, new Color(1f, 0.7f, 0.2f, 1f), 0.4f);
                    Effects.Burst(level.transform, transform.position + Vector3.up * 0.8f, new Color(1f, 0.75f, 0.2f), 14, 3f);
                    GameAudio.Play(SoundId.Boom);
                    break;
            }
        }

        public void UseAbility2()
        {
            if (!Ability2Ready) return;
            Ability2CooldownLeft = Type.Ability2Cooldown * level.Bonuses.AbilityCooldownFactor * HeroTraining.CooldownFactor(training);
            var enemies = level.Enemies;
            switch (Type.Kind)
            {
                case HeroKind.Gertrud:
                    // Sonntagsbraten-Duft: alle Aliens bleiben verzückt stehen
                    foreach (var e in enemies) e.Freeze(4f, 1f, true, false);
                    RingEffect.Spawn(level.transform, transform.position, 8f, new Color(1f, 0.85f, 0.6f, 0.9f), 1.2f);
                    level.AddFloatingText(transform.position + Vector3.up * 1.6f, "Mmmh, Braten!", new Color(1f, 0.85f, 0.6f));
                    GameAudio.Play(SoundId.LevelUp);
                    break;
                case HeroKind.Berta:
                    // Muh-Schallwelle: alle Panzer knacken, Ufos werden kurz festgehalten
                    for (int i = enemies.Count - 1; i >= 0; i--)
                    {
                        if (i >= enemies.Count) continue;
                        var e = enemies[i];
                        e.ShredArmor(99f, 8f);
                        if (e.IsFlying) e.Freeze(3f, 1.3f, true, false);
                        e.TakeDamage(Ability2Damage, true);
                    }
                    RingEffect.Spawn(level.transform, transform.position, 10f, new Color(0.9f, 0.95f, 1f, 0.9f), 0.8f);
                    GameAudio.Play(SoundId.Moo);
                    GameAudio.Play(SoundId.Boom);
                    break;
                default:
                    var path = level.PathWorld;
                    RollingBale.Spawn(level, path[path.Length - 1], path.Length - 2, 999f, Ability2Damage, 0.6f, "Traktor");
                    GameAudio.Play(SoundId.Tractor);
                    break;
            }
        }

        // ---------------------------------------------------------------- Kampf

        void Update()
        {
            if (level.State != LevelState.WaveRunning) return;
            float dt = Time.deltaTime;
            if (Ability1CooldownLeft > 0f) Ability1CooldownLeft -= dt;
            if (Ability2CooldownLeft > 0f) Ability2CooldownLeft -= dt;
            cooldown = Mathf.Max(0f, cooldown - dt);

            var targets = FindTargets(Stats.Projectiles);
            if (targets.Count == 0) return;

            Vector3 look = targets[0].transform.position - transform.position;
            look.y = 0f;
            if (look.sqrMagnitude > 0.001f) body.rotation = Quaternion.Slerp(body.rotation, Quaternion.LookRotation(look), 12f * dt);

            if (cooldown > 0f) return;
            cooldown = 1f / Stats.FireRate;
            Vector3 from = transform.position + Vector3.up * 0.7f;
            switch (Type.Kind)
            {
                case HeroKind.Berta:
                    // Laser trifft sofort
                    Vector3 eyes = transform.position + Vector3.up * 0.55f;
                    for (int i = 0; i < Stats.Projectiles; i++)
                    {
                        var t = targets[i % targets.Count];
                        Effects.Beam(level.transform, eyes, t.AimPoint, new Color(1f, 0.25f, 0.15f));
                        t.TakeDamage(Stats.Damage, Stats.ArmorPiercing);
                    }
                    GameAudio.Play(SoundId.Hit);
                    break;
                case HeroKind.Gertrud:
                    for (int i = 0; i < Stats.Projectiles; i++)
                    {
                        Projectile.Spawn(level, null, from, targets[i % targets.Count], Stats.Damage, Stats, false,
                            PrimitiveType.Cylinder, new Vector3(0.18f, 0.03f, 0.18f), new Color(0.8f, 0.55f, 0.3f));
                    }
                    GameAudio.Play(SoundId.Fork);
                    break;
                default:
                    for (int i = 0; i < Stats.Projectiles; i++)
                    {
                        Projectile.Spawn(level, null, from, targets[i % targets.Count], Stats.Damage, Stats, false,
                            PrimitiveType.Sphere, Vector3.one * 0.09f, new Color(0.25f, 0.25f, 0.28f));
                    }
                    Effects.Burst(level.transform, from + body.forward * 0.4f, new Color(1f, 0.85f, 0.4f), 3, 1.2f);
                    GameAudio.Play(SoundId.Shotgun);
                    break;
            }
        }

        public bool InRange(Enemy e)
        {
            Vector3 d = e.transform.position - transform.position;
            d.y = 0f;
            return d.magnitude <= Stats.Range;
        }

        bool CanTarget(Enemy e) => e.Alive && (!e.IsCamo || Stats.SeesCamo);

        float Score(Enemy e)
        {
            switch (TargetMode)
            {
                case TargetMode.Last: return -e.Priority;
                case TargetMode.Strong: return e.Hp + (e.Carried != null ? 100000f : 0f);
                case TargetMode.Close: return -Vector3.Distance(e.transform.position, transform.position);
                default: return e.Priority;
            }
        }

        List<Enemy> FindTargets(int count)
        {
            var candidates = new List<Enemy>();
            foreach (var e in level.Enemies)
            {
                if (CanTarget(e) && InRange(e)) candidates.Add(e);
            }
            candidates.Sort((a, b) => Score(b).CompareTo(Score(a)));
            if (candidates.Count > count) candidates.RemoveRange(count, candidates.Count - count);
            return candidates;
        }
    }
}
