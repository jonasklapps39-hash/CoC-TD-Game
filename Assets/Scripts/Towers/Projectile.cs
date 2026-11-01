using System.Collections.Generic;
using UnityEngine;

namespace MuhDefense
{
    /// <summary>Geschoss eines Turms – verfolgend (mit Durchschlag) oder im Bogen mit Flächenschaden, Feuer und rollenden Ballen.</summary>
    public class Projectile : MonoBehaviour
    {
        const float PierceSearchRadius = 2f;

        LevelManager level;
        Enemy target;
        TowerStats stats;
        float damage;
        Vector3 targetPos;
        Vector3 start;
        bool arc;
        float flightTime;
        float age;
        int pierceLeft;
        Tower source;
        readonly HashSet<Enemy> alreadyHit = new HashSet<Enemy>();

        public static void Spawn(LevelManager level, Tower source, Vector3 from, Enemy target, float damage, TowerStats stats, bool arc,
            PrimitiveType shape, Vector3 scale, Color color)
        {
            var go = Visuals.Prim(shape, level.transform, from, scale, color, "Geschoss");
            go.transform.position = from;
            var p = go.AddComponent<Projectile>();
            p.level = level;
            p.source = source;
            p.target = target;
            p.stats = stats;
            p.damage = damage;
            p.targetPos = target.AimPoint;
            p.start = from;
            p.arc = arc;
            p.pierceLeft = Mathf.Max(1, stats.Pierce);
            p.flightTime = Mathf.Max(0.3f, Vector3.Distance(from, p.targetPos) / stats.ProjectileSpeed);
        }

        void Update()
        {
            float dt = Time.deltaTime;
            if (arc)
            {
                age += dt;
                float k = Mathf.Clamp01(age / flightTime);
                transform.position = Vector3.Lerp(start, targetPos, k) + Vector3.up * (1.5f * 4f * k * (1f - k));
                transform.Rotate(240f * dt, 0f, 0f);
                if (k >= 1f) ArcImpact();
                return;
            }

            if (target != null && target.Alive) targetPos = target.AimPoint;
            Vector3 delta = targetPos - transform.position;
            float dist = delta.magnitude;
            float step = stats.ProjectileSpeed * dt;
            if (dist <= step || dist < 0.05f)
            {
                transform.position = targetPos;
                DirectHit();
                return;
            }
            transform.position += delta / dist * step;
            transform.rotation = Quaternion.LookRotation(delta);
        }

        /// <summary>Treffer eines verfolgenden Geschosses. Mit Durchschlag fliegt es zum nächsten Alien weiter.</summary>
        void DirectHit()
        {
            if (target != null && target.Alive && !alreadyHit.Contains(target))
            {
                alreadyHit.Add(target);
                ApplyHitEffects(target);
                target.TakeDamage(damage, stats.ArmorPiercing, stats.ArmoredBonus);
                Effects.Burst(level.transform, transform.position, new Color(0.85f, 0.85f, 0.9f), 2, 1.5f, 0.06f);
                GameAudio.Play(SoundId.Hit);
                pierceLeft--;
            }

            if (pierceLeft > 0)
            {
                Enemy next = null;
                float best = PierceSearchRadius;
                foreach (var e in level.Enemies)
                {
                    if (!e.Alive || alreadyHit.Contains(e)) continue;
                    if (source != null && !source.CanTarget(e)) continue;
                    float d = Vector3.Distance(e.transform.position, transform.position);
                    if (d < best)
                    {
                        best = d;
                        next = e;
                    }
                }
                if (next != null)
                {
                    target = next;
                    targetPos = next.AimPoint;
                    return;
                }
            }
            Destroy(gameObject);
        }

        /// <summary>Zusatzwirkungen eines Treffers: Gift, Honig, Panzerschwächung, Verwundbarkeit, Bonus-Münzen.</summary>
        void ApplyHitEffects(Enemy e)
        {
            if (stats.DotDps > 0f) e.ApplyDot(stats.DotDps, stats.DotDuration);
            if (stats.Slow < 1f) e.ApplySlow(stats.Slow, stats.SlowDuration);
            if (stats.ArmorShred > 0f) e.ShredArmor(stats.ArmorShred, 2f);
            if (stats.PermanentVulnerability > 0f) e.AddPermanentVulnerability(stats.PermanentVulnerability);
            if (stats.BonusReward > 0f) e.MarkBonusReward(stats.BonusReward);
        }

        /// <summary>Popcorn: Körner fliegen in alle Richtungen und treffen Aliens in der Nähe.</summary>
        void PopcornShards(Vector3 center)
        {
            var near = new List<Enemy>();
            float radius = Mathf.Max(1.4f, stats.Splash * 2.4f);
            foreach (var e in level.Enemies)
            {
                if (!e.Alive || (e.IsFlying && !stats.HitsAir)) continue;
                Vector3 d = e.transform.position - center;
                d.y = 0f;
                if (d.magnitude <= radius) near.Add(e);
            }
            Effects.Burst(level.transform, center + Vector3.up * 0.3f, new Color(1f, 0.97f, 0.85f), Mathf.Min(24, stats.Shards + 4), 3.5f, 0.11f);
            if (near.Count == 0) return;
            near.Sort((a, b) => (a.transform.position - center).sqrMagnitude.CompareTo((b.transform.position - center).sqrMagnitude));
            for (int k = 0; k < stats.Shards; k++)
            {
                var e = near[k % near.Count];
                if (!e.Alive) continue;
                if (stats.Slow < 1f) e.ApplySlow(stats.Slow, stats.SlowDuration);
                if (stats.StunDuration > 0f) e.Hold(stats.StunDuration, false);
                e.TakeDamage(stats.ShardDamage, stats.ArmorPiercing, stats.ArmoredBonus);
            }
        }

        void ArcImpact()
        {
            Vector3 center = new Vector3(targetPos.x, 0f, targetPos.z);
            var enemies = level.Enemies;
            for (int i = enemies.Count - 1; i >= 0; i--)
            {
                if (i >= enemies.Count) continue;
                var e = enemies[i];
                if (e.IsFlying && !stats.HitsAir) continue;
                Vector3 d = e.transform.position - center;
                d.y = 0f;
                if (d.magnitude > stats.Splash) continue;
                if (stats.BurnDps > 0f)
                {
                    float dps = stats.BurnDps * (e.IsArmored ? stats.BurnArmoredMultiplier : 1f);
                    e.ApplyBurn(dps, stats.BurnDuration, stats.FireMeltsArmor);
                }
                ApplyHitEffects(e);
                if (stats.StunDuration > 0f) e.Hold(stats.StunDuration, stats.StunBoss);
                if (stats.Knockback > 0f) e.PushBack(stats.Knockback);
                e.TakeDamage(damage, stats.ArmorPiercing, stats.ArmoredBonus);
            }
            if (stats.Shards > 0) PopcornShards(center);
            RingEffect.Spawn(level.transform, center, stats.Splash, new Color(1f, 0.85f, 0.3f, 0.9f), 0.3f);
            Effects.Burst(level.transform, center + Vector3.up * 0.2f, stats.BurnDps > 0f ? new Color(1f, 0.5f, 0.15f) : new Color(0.95f, 0.8f, 0.35f), 8, 2.5f);
            GameAudio.Play(SoundId.Hit, 1.5f);

            if (stats.FireFieldDps > 0f)
            {
                GroundFire.Spawn(level, center, stats.Splash * stats.FireFieldRadiusScale, stats.FireFieldDps, stats.FireFieldDuration,
                    stats.FireMeltsArmor, stats.BurnArmoredMultiplier);
            }
            if (stats.RollDistance > 0f)
            {
                Vector3 onPath = level.ClosestPointOnPath(center, out int towardsEntrance);
                RollingBale.Spawn(level, onPath, towardsEntrance, stats.RollDistance, damage * 0.6f, 0.35f);
            }
            Destroy(gameObject);
        }
    }
}
