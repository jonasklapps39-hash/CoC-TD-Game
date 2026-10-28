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
        readonly HashSet<Enemy> alreadyHit = new HashSet<Enemy>();

        public static void Spawn(LevelManager level, Vector3 from, Enemy target, float damage, TowerStats stats, bool arc,
            PrimitiveType shape, Vector3 scale, Color color)
        {
            var go = Visuals.Prim(shape, level.transform, from, scale, color, "Geschoss");
            go.transform.position = from;
            var p = go.AddComponent<Projectile>();
            p.level = level;
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
                target.TakeDamage(damage, stats.ArmorPiercing, stats.ArmoredBonus);
                pierceLeft--;
            }

            if (pierceLeft > 0)
            {
                Enemy next = null;
                float best = PierceSearchRadius;
                foreach (var e in level.Enemies)
                {
                    if (!e.Alive || alreadyHit.Contains(e)) continue;
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

        void ArcImpact()
        {
            Vector3 center = new Vector3(targetPos.x, 0f, targetPos.z);
            var enemies = level.Enemies;
            for (int i = enemies.Count - 1; i >= 0; i--)
            {
                if (i >= enemies.Count) continue;
                var e = enemies[i];
                Vector3 d = e.transform.position - center;
                d.y = 0f;
                if (d.magnitude > stats.Splash) continue;
                if (stats.BurnDps > 0f)
                {
                    float dps = stats.BurnDps * (e.IsArmored ? stats.BurnArmoredMultiplier : 1f);
                    e.ApplyBurn(dps, stats.BurnDuration, stats.FireMeltsArmor);
                }
                e.TakeDamage(damage, stats.ArmorPiercing, stats.ArmoredBonus);
            }
            RingEffect.Spawn(level.transform, center, stats.Splash, new Color(1f, 0.85f, 0.3f, 0.9f), 0.3f);

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
