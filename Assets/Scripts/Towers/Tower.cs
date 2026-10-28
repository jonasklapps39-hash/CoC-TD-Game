using System.Collections.Generic;
using UnityEngine;

namespace MuhDefense
{
    /// <summary>Ein platzierter Turm im Level mit drei Upgrade-Pfaden.</summary>
    public class Tower : MonoBehaviour
    {
        const float GlobalRangeValue = 100f;

        public TowerType Type { get; private set; }
        public Vector2Int Cell { get; private set; }
        /// <summary>Gekaufte Stufe je Pfad (0..5).</summary>
        public readonly int[] Tiers = new int[TowerType.PathCount];
        /// <summary>Insgesamt ausgegebene Münzen (für den Verkaufswert).</summary>
        public int Invested { get; private set; }
        /// <summary>Höchste Upgrade-Stufe in diesem Level – bestimmt durch die Werkstatt auf dem Hof.</summary>
        public int AllowedMaxTier { get; private set; } = TowerType.MaxTier;
        public TowerStats Stats { get; private set; }
        public TargetMode TargetMode { get; private set; } = TargetMode.First;
        public float AbilityCooldownLeft { get; private set; }

        public float Range => Stats.GlobalRange ? GlobalRangeValue : Stats.Range;
        public int SellValue => Mathf.RoundToInt(Invested * 0.7f);
        public bool HasAbility => Stats.Ability != TowerAbility.None;
        public bool AbilityReady => HasAbility && AbilityCooldownLeft <= 0f;

        LevelManager level;
        float cooldown;
        int pulseCount;
        Transform turret;
        Transform decorations;
        LineRenderer rangeCircle;

        public void Init(LevelManager level, TowerType type, Vector2Int cell, int allowedMaxTier)
        {
            this.level = level;
            Type = type;
            Cell = cell;
            Invested = type.Cost;
            AllowedMaxTier = Mathf.Clamp(allowedMaxTier, 0, TowerType.MaxTier);
            name = type.Name;
            transform.position = LevelManager.CellCenter(cell);
            RecalculateStats();
            BuildModel();
            rangeCircle = Visuals.Circle(transform, DisplayRange, new Color(1f, 1f, 1f, 0.8f));
            rangeCircle.enabled = false;
            UpdateDecorations();
        }

        float DisplayRange => Stats.GlobalRange ? 1.2f : Stats.Range;

        public void SetSelected(bool selected) => rangeCircle.enabled = selected;

        // ---------------------------------------------------------------- Upgrades

        public UpgradeTier NextTier(int path) => Tiers[path] < TowerType.MaxTier ? Type.Paths[path].Tiers[Tiers[path]] : null;

        /// <returns>null, wenn das Upgrade möglich ist (vom Geld abgesehen), sonst der Grund.</returns>
        public string WhyCannotUpgrade(int path)
        {
            if (Tiers[path] >= TowerType.MaxTier) return "Maximale Stufe";
            int next = Tiers[path] + 1;
            int otherActive = 0;
            bool otherAboveTwo = false;
            for (int p = 0; p < TowerType.PathCount; p++)
            {
                if (p == path) continue;
                if (Tiers[p] > 0) otherActive++;
                if (Tiers[p] > 2) otherAboveTwo = true;
            }
            if (Tiers[path] == 0 && otherActive >= 2) return "Gesperrt: nur 2 Pfade möglich";
            if (next > 2 && otherAboveTwo) return "Gesperrt: nur ein Pfad über Stufe 2";
            if (next > AllowedMaxTier) return $"Werkstatt auf dem Hof verbessern (für Stufe {next})";
            return null;
        }

        public void Upgrade(int path)
        {
            var tier = NextTier(path);
            if (tier == null) return;
            Tiers[path]++;
            Invested += tier.Cost;
            RecalculateStats();
            Visuals.SetCircleRadius(rangeCircle, DisplayRange);
            UpdateDecorations();
            RingEffect.Spawn(level.transform, transform.position, 0.8f, Type.Paths[path].Color, 0.5f);
        }

        void RecalculateStats()
        {
            var s = Type.Base.Clone();
            for (int p = 0; p < TowerType.PathCount; p++)
            {
                for (int t = 0; t < Tiers[p]; t++) Type.Paths[p].Tiers[t].Apply(s);
            }
            s.Slow = Mathf.Clamp(s.Slow, 0.2f, 1f);
            Stats = s;
        }

        public void CycleTargetMode()
        {
            TargetMode = (TargetMode)(((int)TargetMode + 1) % 4);
        }

        public void TriggerAbility()
        {
            if (!AbilityReady) return;
            AbilityCooldownLeft = Stats.AbilityCooldown;
        }

        // ---------------------------------------------------------------- Kampf

        void Update()
        {
            if (level.State != LevelState.WaveRunning) return;

            float dt = Time.deltaTime;
            if (AbilityCooldownLeft > 0f) AbilityCooldownLeft -= dt;
            cooldown = Mathf.Max(0f, cooldown - dt);

            float rate = Stats.FireRate;
            if (Type == TowerType.Mistgabel && level.FarmerFrenzyTimer > 0f) rate *= 2f;

            if (Type.Kind == TowerKind.Aura)
            {
                if (cooldown <= 0f && Pulse()) cooldown = 1f / rate;
                return;
            }

            var targets = FindTargets(Mathf.Max(1, Stats.Projectiles));
            if (targets.Count == 0) return;

            Vector3 look = targets[0].transform.position - transform.position;
            look.y = 0f;
            if (look.sqrMagnitude > 0.001f)
            {
                turret.rotation = Quaternion.Slerp(turret.rotation, Quaternion.LookRotation(look), 12f * dt);
            }

            if (cooldown <= 0f)
            {
                for (int i = 0; i < Stats.Projectiles; i++) Fire(targets[i % targets.Count]);
                cooldown = 1f / rate;
            }
        }

        bool InRange(Enemy e)
        {
            Vector3 d = e.transform.position - transform.position;
            d.y = 0f;
            return d.magnitude <= Range;
        }

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

        /// <summary>Bis zu <paramref name="count"/> verschiedene Ziele, sortiert nach Zielmodus.</summary>
        List<Enemy> FindTargets(int count)
        {
            var candidates = new List<Enemy>();
            foreach (var e in level.Enemies)
            {
                if (e.Alive && InRange(e)) candidates.Add(e);
            }
            candidates.Sort((a, b) => Score(b).CompareTo(Score(a)));
            if (candidates.Count > count) candidates.RemoveRange(count, candidates.Count - count);
            return candidates;
        }

        void Fire(Enemy target)
        {
            Vector3 from = transform.position + Vector3.up * 0.8f;
            float damage = Stats.Damage;
            if (Stats.CritChance > 0f && Random.value < Stats.CritChance)
            {
                damage *= Stats.CritMultiplier;
                level.AddFloatingText(target.AimPoint + Vector3.up * 0.4f, "Krit!", new Color(1f, 0.5f, 0.2f));
            }

            switch (Type.Kind)
            {
                case TowerKind.Projectile:
                    Projectile.Spawn(level, from, target, damage, Stats, false,
                        PrimitiveType.Cube, new Vector3(0.05f, 0.05f, 0.35f),
                        Stats.ArmorPiercing ? new Color(1f, 0.82f, 0.2f) : new Color(0.75f, 0.75f, 0.8f));
                    break;
                case TowerKind.Arc:
                    Projectile.Spawn(level, from, target, damage, Stats, true,
                        PrimitiveType.Cube, new Vector3(0.28f, 0.22f, 0.28f),
                        Stats.BurnDps > 0f ? new Color(1f, 0.55f, 0.2f) : new Color(0.95f, 0.82f, 0.35f));
                    break;
            }
        }

        /// <returns>true, wenn mindestens ein Alien getroffen wurde.</returns>
        bool Pulse()
        {
            bool hit = false;
            var enemies = level.Enemies;
            if (!enemies.Exists(InRange)) return false;
            pulseCount++;
            bool freezeNow = Stats.FreezeEveryNthPulse > 0 && pulseCount % Stats.FreezeEveryNthPulse == 0;
            for (int i = enemies.Count - 1; i >= 0; i--)
            {
                if (i >= enemies.Count) continue;
                var e = enemies[i];
                if (!InRange(e)) continue;
                hit = true;
                e.ApplySlow(Stats.Slow, Stats.SlowDuration);
                if (Stats.ArmorShred > 0f) e.ShredArmor(Stats.ArmorShred, 2f);
                if (Stats.DotDps > 0f) e.ApplyDot(Stats.DotDps, Stats.DotDuration);
                if (Stats.PermanentVulnerability > 0f) e.AddPermanentVulnerability(Stats.PermanentVulnerability);
                if (freezeNow) e.Freeze(Stats.FreezeDuration, Stats.FrozenVulnerability, Stats.FreezeBig);
                if (Stats.Knockback > 0f) e.PushBack(Stats.Knockback);
                e.TakeDamage(Stats.Damage);
            }
            if (hit)
            {
                var color = freezeNow ? new Color(0.85f, 0.95f, 1f, 1f) : new Color(0.7f, 0.9f, 1f, 0.9f);
                RingEffect.Spawn(level.transform, transform.position, Mathf.Min(Range, 6f), color, 0.45f);
            }
            return hit;
        }

        // ---------------------------------------------------------------- Aussehen

        /// <summary>Zeigt die Upgrades am Turm: farbige Kugeln je Pfad, ab Stufe 3 ein Abzeichen, ab Stufe 5 ein Leuchten.</summary>
        void UpdateDecorations()
        {
            if (decorations != null) Destroy(decorations.gameObject);
            decorations = new GameObject("Upgrades").transform;
            decorations.SetParent(transform, false);

            int total = 0;
            for (int p = 0; p < TowerType.PathCount; p++)
            {
                total += Tiers[p];
                var color = Type.Paths[p].Color;
                for (int t = 0; t < Tiers[p]; t++)
                {
                    Visuals.Prim(PrimitiveType.Sphere, decorations, new Vector3(-0.36f + 0.36f * p, 0.12f, -0.44f + 0.11f * t),
                        Vector3.one * 0.09f, color, "Stufe");
                }
                if (Tiers[p] >= 3)
                {
                    Visuals.Prim(PrimitiveType.Sphere, decorations, new Vector3(-0.25f + 0.25f * p, 1.15f, 0f),
                        Vector3.one * (Tiers[p] >= 5 ? 0.24f : 0.14f), color, "Abzeichen");
                }
            }
            turret.localScale = Vector3.one * (1f + 0.035f * total);
        }

        void BuildModel()
        {
            var wood = new Color(0.55f, 0.36f, 0.20f);
            var stone = new Color(0.6f, 0.58f, 0.55f);
            Visuals.Prim(PrimitiveType.Cylinder, transform, new Vector3(0f, 0.05f, 0f), new Vector3(0.8f, 0.05f, 0.8f), stone, "Sockel");

            turret = new GameObject("Turm").transform;
            turret.SetParent(transform, false);

            switch (Type.Kind)
            {
                case TowerKind.Projectile:
                    Visuals.Prim(PrimitiveType.Capsule, turret, new Vector3(0f, 0.4f, 0f), new Vector3(0.3f, 0.28f, 0.3f), Type.Color, "Latzhose");
                    Visuals.Prim(PrimitiveType.Sphere, turret, new Vector3(0f, 0.78f, 0f), Vector3.one * 0.24f, new Color(1f, 0.8f, 0.65f), "Kopf");
                    Visuals.Prim(PrimitiveType.Cylinder, turret, new Vector3(0f, 0.9f, 0f), new Vector3(0.4f, 0.02f, 0.4f), new Color(0.95f, 0.85f, 0.4f), "Strohhut");
                    Visuals.Prim(PrimitiveType.Cylinder, turret, new Vector3(0f, 0.96f, 0f), new Vector3(0.2f, 0.06f, 0.2f), new Color(0.95f, 0.85f, 0.4f), "Hutspitze");
                    Visuals.Prim(PrimitiveType.Cube, turret, new Vector3(0.2f, 0.6f, 0.08f), new Vector3(0.04f, 0.75f, 0.04f), wood, "Stiel");
                    Visuals.Prim(PrimitiveType.Cube, turret, new Vector3(0.2f, 1f, 0.08f), new Vector3(0.14f, 0.12f, 0.03f), new Color(0.7f, 0.7f, 0.75f), "Gabel");
                    break;

                case TowerKind.Arc:
                    Visuals.Prim(PrimitiveType.Cube, turret, new Vector3(0f, 0.2f, 0f), new Vector3(0.7f, 0.25f, 0.6f), wood, "Gestell");
                    var arm = Visuals.Prim(PrimitiveType.Cube, turret, new Vector3(0f, 0.5f, -0.05f), new Vector3(0.1f, 0.08f, 0.8f), wood, "Arm");
                    arm.transform.localRotation = Quaternion.Euler(-25f, 0f, 0f);
                    Visuals.Prim(PrimitiveType.Cube, turret, new Vector3(0f, 0.72f, -0.38f), new Vector3(0.3f, 0.24f, 0.3f), Type.Color, "Heuballen");
                    break;

                case TowerKind.Aura:
                    Visuals.Prim(PrimitiveType.Cylinder, turret, new Vector3(0f, 0.4f, 0f), new Vector3(0.55f, 0.35f, 0.55f), Color.white, "Tank");
                    Visuals.Prim(PrimitiveType.Cylinder, turret, new Vector3(0f, 0.5f, 0f), new Vector3(0.57f, 0.05f, 0.57f), new Color(0.3f, 0.55f, 0.95f), "Streifen");
                    Visuals.Prim(PrimitiveType.Sphere, turret, new Vector3(0f, 0.8f, 0f), Vector3.one * 0.3f, Type.Color, "Düse");
                    break;
            }
        }
    }
}
