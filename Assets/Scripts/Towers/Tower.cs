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

        public float Range => Stats.GlobalRange ? GlobalRangeValue : Stats.Range * (1f + level.RangeBonusAt(this));

        /// <summary>Getarnte Aliens: eigene Tarnsicht oder ein Wetterhahn in der Nähe.</summary>
        public bool CanSee(Enemy e) => !e.IsCamo || Stats.SeesCamo || level.RevealsCamoFor(this);

        /// <summary>Darf dieser Turm das Alien angreifen (Tarnung, Fluggegner)?</summary>
        public bool CanTarget(Enemy e) => e.Alive && (!e.IsFlying || Stats.HitsAir) && CanSee(e);
        public int SellValue => Mathf.RoundToInt(Invested * 0.7f);
        public bool HasAbility => Stats.Ability != TowerAbility.None;
        public bool AbilityReady => HasAbility && AbilityCooldownLeft <= 0f;

        LevelManager level;
        float cooldown;
        float incomeTimer;
        int pulseCount;
        Transform turret;
        Transform decorations;
        /// <summary>Höhe der Turmspitze über dem Boden (für die Upgrade-Abzeichen).</summary>
        float modelTop = 1.15f;
        LineRenderer rangeCircle;
        /// <summary>Rückstoß-Animation beim Schießen (1 = gerade geschossen).</summary>
        float kick;
        float baseScale = 1f;

        public void Init(LevelManager level, TowerType type, Vector2Int cell, int allowedMaxTier, int pricePaid = -1)
        {
            this.level = level;
            Type = type;
            Cell = cell;
            Invested = pricePaid >= 0 ? pricePaid : type.Cost;
            AllowedMaxTier = Mathf.Clamp(allowedMaxTier, 0, TowerType.MaxTier);
            name = type.Name;
            transform.position = LevelManager.CellCenter(cell);
            RecalculateStats();
            BuildModel();
            rangeCircle = Visuals.Circle(transform, DisplayRange, new Color(1f, 1f, 1f, 0.8f));
            rangeCircle.enabled = false;
            UpdateDecorations();
            SyncUnits();
        }

        float DisplayRange => Stats.GlobalRange ? 1.2f : Stats.Range;

        public void SetSelected(bool selected)
        {
            rangeCircle.enabled = selected;
            // Reichweite inklusive Wetterhahn-Bonus anzeigen
            if (selected) Visuals.SetCircleRadius(rangeCircle, Stats.GlobalRange ? 1.2f : Range);
        }

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

        /// <param name="paid">Tatsächlich bezahlter Preis (mit Hofladen-Rabatt); bestimmt den Verkaufswert.</param>
        public void Upgrade(int path, int paid = -1)
        {
            var tier = NextTier(path);
            if (tier == null) return;
            Tiers[path]++;
            Invested += paid >= 0 ? paid : tier.Cost;
            GameAudio.Play(SoundId.Upgrade);
            Effects.Burst(level.transform, transform.position + Vector3.up * 0.6f, Type.Paths[path].Color, 10, 2.5f);
            RecalculateStats();
            Visuals.SetCircleRadius(rangeCircle, DisplayRange);
            UpdateDecorations();
            SyncUnits();
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
            level.Bonuses.ApplyTo(Type, s);
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

        void LateUpdate()
        {
            if (kick <= 0f || turret == null) return;
            kick = Mathf.Max(0f, kick - Time.deltaTime * 6f);
            // kurz stauchen und wieder aufrichten
            turret.localScale = new Vector3(1f + 0.08f * kick, 1f - 0.14f * kick, 1f + 0.08f * kick) * baseScale;
        }

        void Update()
        {
            if (level.State != LevelState.WaveRunning) return;

            float dt = Time.deltaTime;
            if (AbilityCooldownLeft > 0f) AbilityCooldownLeft -= dt;
            cooldown = Mathf.Max(0f, cooldown - dt);

            if (Stats.IncomeDuringWave > 0)
            {
                incomeTimer += dt;
                if (incomeTimer >= 5f)
                {
                    incomeTimer = 0f;
                    level.AddMoney(Stats.IncomeDuringWave, transform.position + Vector3.up * 1.2f);
                }
            }
            if (Stats.NoAttack) return;

            float rate = Stats.FireRate * (1f + level.RateBonusAt(this));
            if (Type == TowerType.Mistgabel && level.FarmerFrenzyTimer > 0f) rate *= 2f;

            if (Type.Kind == TowerKind.Aura)
            {
                if (cooldown <= 0f && Pulse()) cooldown = 1f / rate;
                return;
            }
            if (Type.Kind == TowerKind.Harvester || Type.Kind == TowerKind.Dogs)
            {
                // Die Einheiten (Mähdrescher, Hunde) arbeiten selbst
                return;
            }
            if (Type.Kind == TowerKind.Puddle)
            {
                if (cooldown <= 0f && LayPuddles()) cooldown = 1f / rate;
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

        public bool InRange(Enemy e)
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
                if (CanTarget(e) && InRange(e)) candidates.Add(e);
            }
            candidates.Sort((a, b) => Score(b).CompareTo(Score(a)));
            if (candidates.Count > count) candidates.RemoveRange(count, candidates.Count - count);
            return candidates;
        }

        void Fire(Enemy target)
        {
            Vector3 from = transform.position + Vector3.up * 0.8f;
            float damage = Stats.Damage;
            kick = 1f;
            if (Stats.CritChance > 0f && Random.value < Stats.CritChance)
            {
                damage *= Stats.CritMultiplier;
                level.AddFloatingText(target.AimPoint + Vector3.up * 0.4f, "Krit!", new Color(1f, 0.5f, 0.2f));
            }

            switch (Type.Kind)
            {
                case TowerKind.Chain:
                    FireChain(target, damage);
                    break;
                case TowerKind.Projectile when Type == TowerType.EierHuhn:
                    GameAudio.Play(SoundId.Pop, 0.4f);
                    Projectile.Spawn(level, this, from, target, damage, Stats, false, Type.ShotShape, Type.ShotScale,
                        Stats.DotDps > 0f ? new Color(0.75f, 0.85f, 0.45f) : Type.ShotColor);
                    break;
                case TowerKind.Arc when Type != TowerType.Heuballen:
                    GameAudio.Play(SoundId.Pop, Type == TowerType.Moerser ? 0.4f : 0.7f);
                    Projectile.Spawn(level, this, from, target, damage, Stats, true, Type.ShotShape, Type.ShotScale, Type.ShotColor);
                    break;
                case TowerKind.Projectile when Type == TowerType.Bienenstock:
                    GameAudio.Play(SoundId.Buzz);
                    Projectile.Spawn(level, this, from, target, damage, Stats, false,
                        PrimitiveType.Sphere, new Vector3(0.1f, 0.08f, 0.13f), new Color(1f, 0.8f, 0.1f));
                    break;
                case TowerKind.Projectile:
                    GameAudio.Play(SoundId.Fork);
                    Projectile.Spawn(level, this, from, target, damage, Stats, false,
                        PrimitiveType.Cube, new Vector3(0.05f, 0.05f, 0.35f),
                        Stats.ArmorPiercing ? new Color(1f, 0.82f, 0.2f) : new Color(0.75f, 0.75f, 0.8f));
                    break;
                case TowerKind.Arc:
                    GameAudio.Play(SoundId.Pop, 0.6f);
                    Projectile.Spawn(level, this, from, target, damage, Stats, true,
                        PrimitiveType.Cube, new Vector3(0.28f, 0.22f, 0.28f),
                        Stats.BurnDps > 0f ? new Color(1f, 0.55f, 0.2f) : new Color(0.95f, 0.82f, 0.35f));
                    break;
            }
        }

        /// <summary>Blitz auf das Ziel, der danach auf weitere Aliens in der Nähe überspringt.</summary>
        void FireChain(Enemy first, float damage)
        {
            GameAudio.Play(SoundId.Zap);
            var hit = new HashSet<Enemy>();
            Vector3 from = transform.position + Vector3.up * (modelTop * 0.9f);
            Enemy current = first;
            var color = new Color(0.55f, 0.85f, 1f);
            for (int jump = 0; jump <= Stats.ChainCount && current != null; jump++)
            {
                hit.Add(current);
                Vector3 to = current.AimPoint;
                Effects.Beam(level.transform, from, to, color);
                if (Stats.StunDuration > 0f) current.Hold(Stats.StunDuration, Stats.StunBoss);
                if (Stats.ArmorShred > 0f) current.ShredArmor(Stats.ArmorShred, 2f);
                current.TakeDamage(damage, Stats.ArmorPiercing, Stats.ArmoredBonus);
                damage *= Stats.ChainFalloff;
                from = to;

                Enemy next = null;
                float best = Stats.ChainRange;
                foreach (var e in level.Enemies)
                {
                    if (hit.Contains(e) || !CanTarget(e)) continue;
                    float d = Vector3.Distance(e.transform.position, to);
                    if (d < best)
                    {
                        best = d;
                        next = e;
                    }
                }
                current = next;
            }
        }

        /// <summary>Güllefass: Pfützen auf den Weg in Reichweite – bevorzugt dort, wo gerade Aliens laufen.</summary>
        bool LayPuddles()
        {
            var spots = level.PathPointsWithin(transform.position, Range);
            if (spots.Count == 0) return false;
            var targets = FindTargets(Mathf.Max(1, Stats.Projectiles));
            GameAudio.Play(SoundId.Splat, 0.6f);
            kick = 1f;
            for (int i = 0; i < Mathf.Max(1, Stats.Projectiles); i++)
            {
                Vector3 spot;
                if (i < targets.Count)
                {
                    // etwas vor das Alien, damit es hineinläuft
                    spot = level.ClosestPointOnPath(targets[i].transform.position + targets[i].transform.forward * 0.6f, out _);
                }
                else
                {
                    spot = spots[Random.Range(0, spots.Count)];
                }
                SlurryPuddle.Spawn(level, this, spot);
            }
            return true;
        }

        /// <returns>true, wenn mindestens ein Alien getroffen wurde.</returns>
        bool Pulse()
        {
            if (Stats.ScareCount > 0) return Scare();
            bool hit = false;
            var enemies = level.Enemies;
            if (!enemies.Exists(e => InRange(e) && CanTarget(e))) return false;
            pulseCount++;
            bool freezeNow = Stats.FreezeEveryNthPulse > 0 && pulseCount % Stats.FreezeEveryNthPulse == 0;
            for (int i = enemies.Count - 1; i >= 0; i--)
            {
                if (i >= enemies.Count) continue;
                var e = enemies[i];
                if (!InRange(e) || !CanTarget(e)) continue;
                hit = true;
                e.ApplySlow(Stats.Slow, Stats.SlowDuration);
                if (Stats.ArmorShred > 0f) e.ShredArmor(Stats.ArmorShred, 2f);
                if (Stats.DotDps > 0f) e.ApplyDot(Stats.DotDps, Stats.DotDuration);
                if (Stats.PermanentVulnerability > 0f) e.AddPermanentVulnerability(Stats.PermanentVulnerability);
                if (freezeNow) e.Freeze(Stats.FreezeDuration, Stats.FrozenVulnerability, Stats.FreezeBig);
                if (e.IsFlying && Stats.AirStun > 0f) e.Freeze(Stats.AirStun, 1f, true);
                if (Stats.Knockback > 0f) e.PushBack(Stats.Knockback);
                e.TakeDamage(Stats.Damage * (e.IsFlying ? Stats.AirDamageMultiplier : 1f));
            }
            if (hit)
            {
                kick = 1f;
                var color = freezeNow ? new Color(0.85f, 0.95f, 1f, 1f) : new Color(0.7f, 0.9f, 1f, 0.9f);
                RingEffect.Spawn(level.transform, transform.position, Mathf.Min(Range, 6f), color, 0.45f);
                if (freezeNow) GameAudio.Play(SoundId.Freeze);
                else if (Type == TowerType.Eismilch) GameAudio.Play(SoundId.Spray);
                else if (Type == TowerType.Wetterhahn) GameAudio.Play(SoundId.Hit);
                if (Stats.Knockback > 0f) GameAudio.Play(SoundId.Wind, 0.5f);
            }
            return hit;
        }

        /// <summary>Vogelscheuche: die vordersten Aliens erschrecken (bleiben stehen, weichen zurück), Krähen picken alle in Reichweite.</summary>
        bool Scare()
        {
            var inRange = level.Enemies.FindAll(e => InRange(e) && CanTarget(e));
            if (inRange.Count == 0) return false;
            inRange.Sort((a, b) => b.Priority.CompareTo(a.Priority));
            int scared = 0;
            foreach (var e in inRange)
            {
                if (scared >= Stats.ScareCount) break;
                if (!e.Hold(Stats.StunDuration, false)) continue;
                scared++;
                if (Stats.Knockback > 0f) e.PushBack(Stats.Knockback);
                if (Stats.PermanentVulnerability > 0f) e.AddPermanentVulnerability(Stats.PermanentVulnerability);
                level.AddFloatingText(e.AimPoint + Vector3.up * 0.3f, "!", new Color(1f, 0.95f, 0.4f));
            }
            if (Stats.Damage > 0f)
            {
                for (int i = inRange.Count - 1; i >= 0; i--)
                {
                    var e = inRange[i];
                    if (Stats.ArmorShred > 0f) e.ShredArmor(Stats.ArmorShred, 2f);
                    e.TakeDamage(Stats.Damage);
                }
            }
            kick = 1f;
            RingEffect.Spawn(level.transform, transform.position, Mathf.Min(Range, 6f), new Color(0.6f, 0.35f, 0.2f, 0.9f), 0.45f);
            GameAudio.Play(SoundId.Hit, 0.8f);
            return true;
        }

        // ---------------------------------------------------------------- Einheiten (Mähdrescher, Hunde)

        readonly List<TowerUnit> units = new List<TowerUnit>();

        /// <summary>Sorgt dafür, dass so viele Einheiten unterwegs sind, wie die Upgrades erlauben.</summary>
        /// <summary>Einheiten neu ausrichten (z. B. wenn ein Wetterhahn in der Nähe gebaut oder verkauft wurde).</summary>
        public void RefreshUnits() => SyncUnits();

        void SyncUnits()
        {
            if (Type.Kind != TowerKind.Harvester && Type.Kind != TowerKind.Dogs) return;
            units.RemoveAll(u => u == null);
            while (units.Count < Stats.UnitCount)
            {
                var go = new GameObject(Type.Kind == TowerKind.Dogs ? "Hofhund" : "Mähdrescher");
                go.transform.SetParent(level.transform, false);
                TowerUnit unit = Type.Kind == TowerKind.Dogs ? go.AddComponent<DogUnit>() : (TowerUnit)go.AddComponent<HarvesterUnit>();
                unit.Init(level, this, units.Count);
                units.Add(unit);
            }
            foreach (var u in units) u.OnStatsChanged();
        }

        public int UnitTotal => Mathf.Max(1, Stats.UnitCount);

        void OnDestroy()
        {
            foreach (var u in units)
            {
                if (u != null) Destroy(u.gameObject);
            }
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
                    Visuals.Prim(PrimitiveType.Sphere, decorations, new Vector3(-0.25f + 0.25f * p, modelTop + 0.15f, 0f),
                        Vector3.one * (Tiers[p] >= 5 ? 0.24f : 0.14f), color, "Abzeichen");
                }
            }
            baseScale = 1f + 0.035f * total;
            turret.localScale = Vector3.one * baseScale;
        }

        public static string ModelNameFor(TowerType type) => type.ModelName;

        void BuildModel()
        {
            turret = new GameObject("Turm").transform;
            turret.SetParent(transform, false);
            var model = ModelLibrary.Spawn(ModelNameFor(Type), turret, 0.92f, ModelLibrary.Fit.Footprint);
            if (model != null)
            {
                float top = 0f;
                foreach (var r in model.GetComponentsInChildren<Renderer>()) top = Mathf.Max(top, r.bounds.max.y - transform.position.y);
                modelTop = top;
                return;
            }

            // Ersatz aus Grundkörpern, falls das Modell fehlt
            var wood = new Color(0.55f, 0.36f, 0.20f);
            var stone = new Color(0.6f, 0.58f, 0.55f);
            Visuals.Prim(PrimitiveType.Cylinder, transform, new Vector3(0f, 0.05f, 0f), new Vector3(0.8f, 0.05f, 0.8f), stone, "Sockel");

            switch (Type.Kind)
            {
                default:
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
