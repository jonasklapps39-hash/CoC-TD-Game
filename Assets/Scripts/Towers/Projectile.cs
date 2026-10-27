using UnityEngine;

namespace MuhDefense
{
    /// <summary>Geschoss eines Turms – entweder verfolgend oder im Bogen mit Flächenschaden.</summary>
    public class Projectile : MonoBehaviour
    {
        LevelManager level;
        Enemy target;
        Vector3 targetPos;
        Vector3 start;
        float damage;
        float splash;
        float speed;
        bool arc;
        float flightTime;
        float age;

        public static void Spawn(LevelManager level, Vector3 from, Enemy target, float damage, float speed, float splash, bool arc,
            PrimitiveType shape, Vector3 scale, Color color)
        {
            var go = Visuals.Prim(shape, level.transform, from, scale, color, "Geschoss");
            go.transform.position = from;
            var p = go.AddComponent<Projectile>();
            p.level = level;
            p.target = target;
            p.targetPos = target.AimPoint;
            p.start = from;
            p.damage = damage;
            p.speed = speed;
            p.splash = splash;
            p.arc = arc;
            p.flightTime = Mathf.Max(0.3f, Vector3.Distance(from, p.targetPos) / speed);
        }

        void Update()
        {
            float dt = Time.deltaTime;
            if (arc)
            {
                // Fester Zielpunkt, Flugbahn als Parabel
                age += dt;
                float k = Mathf.Clamp01(age / flightTime);
                Vector3 pos = Vector3.Lerp(start, targetPos, k) + Vector3.up * (1.5f * 4f * k * (1f - k));
                transform.position = pos;
                transform.Rotate(240f * dt, 0f, 0f);
                if (k >= 1f) Impact();
                return;
            }

            if (target != null && target.Alive) targetPos = target.AimPoint;
            Vector3 delta = targetPos - transform.position;
            float dist = delta.magnitude;
            float step = speed * dt;
            if (dist <= step || dist < 0.05f)
            {
                transform.position = targetPos;
                Impact();
                return;
            }
            transform.position += delta / dist * step;
            transform.rotation = Quaternion.LookRotation(delta);
        }

        void Impact()
        {
            if (splash > 0f)
            {
                Vector3 center = new Vector3(targetPos.x, 0f, targetPos.z);
                var enemies = level.Enemies;
                for (int i = enemies.Count - 1; i >= 0; i--)
                {
                    var e = enemies[i];
                    Vector3 d = e.transform.position - center;
                    d.y = 0f;
                    if (d.magnitude <= splash) e.TakeDamage(damage);
                }
                RingEffect.Spawn(level.transform, center, splash, new Color(1f, 0.85f, 0.3f, 0.9f), 0.3f);
            }
            else if (target != null && target.Alive)
            {
                target.TakeDamage(damage);
            }
            Destroy(gameObject);
        }
    }
}
