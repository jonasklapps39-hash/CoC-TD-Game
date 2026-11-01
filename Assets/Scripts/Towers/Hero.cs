using System.Collections.Generic;
using UnityEngine;

namespace MuhDefense
{
    /// <summary>
    /// Held Bauer Heinz (siehe Spielkonzept, Kapitel 9): einmal pro Level kostenlos aufstellbar,
    /// steigt durch besiegte Aliens und geschaffte Wellen von Stufe 1 bis 10 auf und hat zwei Fähigkeiten.
    /// </summary>
    public class Hero : MonoBehaviour
    {
        public const int MaxLevel = 10;
        public const string HeroName = "Bauer Heinz";
        public const float BlastCooldown = 25f;
        public const float TractorCooldown = 45f;
        public const int BlastLevel = 3;
        public const int TractorLevel = 7;

        /// <summary>Gesamte Erfahrung, die für Stufe 1..10 nötig ist.</summary>
        static readonly int[] XpForLevel = { 0, 60, 150, 280, 450, 670, 950, 1300, 1720, 2200 };

        public int Level { get; private set; } = 1;
        public int Xp { get; private set; }
        public Vector2Int Cell { get; private set; }
        public TargetMode TargetMode { get; private set; } = TargetMode.First;
        public TowerStats Stats { get; private set; }
        public float BlastCooldownLeft { get; private set; }
        public float TractorCooldownLeft { get; private set; }

        public bool HasBlast => Level >= BlastLevel;
        public bool HasTractor => Level >= TractorLevel;
        public bool BlastReady => HasBlast && BlastCooldownLeft <= 0f;
        public bool TractorReady => HasTractor && TractorCooldownLeft <= 0f;
        public float BlastDamage => 30f + 12f * Level;
        public float TractorDamage => 100f + 15f * Level;

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

        public void Init(LevelManager level, Vector2Int cell)
        {
            this.level = level;
            Cell = cell;
            name = HeroName;
            transform.position = LevelManager.CellCenter(cell);
            RecalculateStats();
            body = new GameObject("Körper").transform;
            body.SetParent(transform, false);
            Visuals.Prim(PrimitiveType.Cylinder, transform, new Vector3(0f, 0.04f, 0f), new Vector3(0.85f, 0.04f, 0.85f), new Color(1f, 0.82f, 0.25f), "Heldenring");
            if (ModelLibrary.Spawn("BauerHeinz", body, 1.15f) == null)
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
            Stats = new TowerStats
            {
                Range = 2.6f + 0.1f * l,
                Damage = 5f + 2f * l,
                FireRate = 1f + 0.07f * l,
                Projectiles = Level >= 9 ? 5 : Level >= 5 ? 4 : 3,
                ProjectileSpeed = 14f,
                SeesCamo = Level >= BlastLevel,
                HitsAir = true,
                ArmorPiercing = Level >= 8,
            };
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
            string extra = Level == BlastLevel ? " – Ladung Schrot!" : Level == TractorLevel ? " – Traktor-Raserei!" : "";
            level.AddFloatingText(transform.position + Vector3.up * 1.4f, $"Heinz Stufe {Level}{extra}", new Color(1f, 0.85f, 0.3f));
            GameAudio.Play(SoundId.LevelUp);
        }

        public void CycleTargetMode() => TargetMode = (TargetMode)(((int)TargetMode + 1) % 4);

        // ---------------------------------------------------------------- Fähigkeiten

        /// <summary>Ladung Schrot: hoher Schaden an allen Aliens in Reichweite.</summary>
        public void UseBlast()
        {
            if (!BlastReady) return;
            BlastCooldownLeft = BlastCooldown;
            var enemies = level.Enemies;
            for (int i = enemies.Count - 1; i >= 0; i--)
            {
                if (i >= enemies.Count) continue;
                var e = enemies[i];
                if (InRange(e)) e.TakeDamage(BlastDamage, true);
            }
            RingEffect.Spawn(level.transform, transform.position, Stats.Range, new Color(1f, 0.7f, 0.2f, 1f), 0.4f);
            Effects.Burst(level.transform, transform.position + Vector3.up * 0.8f, new Color(1f, 0.75f, 0.2f), 14, 3f);
            GameAudio.Play(SoundId.Boom);
        }

        /// <summary>Traktor-Raserei: Heinz fährt mit dem Traktor von der Weide den ganzen Weg zum Eingang.</summary>
        public void UseTractor()
        {
            if (!TractorReady) return;
            TractorCooldownLeft = TractorCooldown;
            var path = level.PathWorld;
            RollingBale.Spawn(level, path[path.Length - 1], path.Length - 2, 999f, TractorDamage, 0.6f, "Traktor");
            GameAudio.Play(SoundId.Tractor);
        }

        // ---------------------------------------------------------------- Kampf

        void Update()
        {
            if (level.State != LevelState.WaveRunning) return;
            float dt = Time.deltaTime;
            if (BlastCooldownLeft > 0f) BlastCooldownLeft -= dt;
            if (TractorCooldownLeft > 0f) TractorCooldownLeft -= dt;
            cooldown = Mathf.Max(0f, cooldown - dt);

            var targets = FindTargets(Stats.Projectiles);
            if (targets.Count == 0) return;

            Vector3 look = targets[0].transform.position - transform.position;
            look.y = 0f;
            if (look.sqrMagnitude > 0.001f) body.rotation = Quaternion.Slerp(body.rotation, Quaternion.LookRotation(look), 12f * dt);

            if (cooldown > 0f) return;
            cooldown = 1f / Stats.FireRate;
            Vector3 from = transform.position + Vector3.up * 0.7f;
            for (int i = 0; i < Stats.Projectiles; i++)
            {
                Projectile.Spawn(level, null, from, targets[i % targets.Count], Stats.Damage, Stats, false,
                    PrimitiveType.Sphere, Vector3.one * 0.09f, new Color(0.25f, 0.25f, 0.28f));
            }
            Effects.Burst(level.transform, from + body.forward * 0.4f, new Color(1f, 0.85f, 0.4f), 3, 1.2f);
            GameAudio.Play(SoundId.Shotgun);
        }

        bool InRange(Enemy e)
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
