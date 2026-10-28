using System.Collections.Generic;
using UnityEngine;

namespace MuhDefense
{
    /// <summary>Brennende Fläche auf dem Boden (Heuballen-Feuerpfad). Aliens darin fangen Feuer.</summary>
    public class GroundFire : MonoBehaviour
    {
        LevelManager level;
        float radius;
        float dps;
        float lifetime;
        float age;
        bool meltsArmor;
        float armoredMultiplier;
        Transform flames;

        public static void Spawn(LevelManager level, Vector3 position, float radius, float dps, float duration, bool meltsArmor, float armoredMultiplier)
        {
            var go = new GameObject("Feuer");
            go.transform.SetParent(level.transform, false);
            go.transform.position = position;
            var fire = go.AddComponent<GroundFire>();
            fire.level = level;
            fire.radius = radius;
            fire.dps = dps;
            fire.lifetime = duration;
            fire.meltsArmor = meltsArmor;
            fire.armoredMultiplier = armoredMultiplier;
            fire.BuildVisual();
        }

        void BuildVisual()
        {
            Visuals.Prim(PrimitiveType.Cylinder, transform, new Vector3(0f, 0.02f, 0f), new Vector3(radius * 2f, 0.01f, radius * 2f),
                new Color(0.55f, 0.18f, 0.05f), "Glut");
            flames = new GameObject("Flammen").transform;
            flames.SetParent(transform, false);
            for (int i = 0; i < 6; i++)
            {
                float a = i * Mathf.PI / 3f;
                float r = radius * 0.55f;
                Visuals.Prim(PrimitiveType.Sphere, flames, new Vector3(Mathf.Cos(a) * r, 0.12f, Mathf.Sin(a) * r),
                    new Vector3(0.18f, 0.3f, 0.18f), i % 2 == 0 ? new Color(1f, 0.5f, 0.1f) : new Color(1f, 0.8f, 0.2f), "Flamme");
            }
        }

        void Update()
        {
            if (level.State != LevelState.WaveRunning)
            {
                Destroy(gameObject);
                return;
            }
            age += Time.deltaTime;
            flames.localScale = new Vector3(1f, 0.8f + 0.3f * Mathf.Sin(age * 14f), 1f);
            foreach (var e in level.Enemies)
            {
                Vector3 d = e.transform.position - transform.position;
                d.y = 0f;
                if (d.magnitude <= radius) e.ApplyBurn(dps * (e.IsArmored ? armoredMultiplier : 1f), 0.5f, meltsArmor);
            }
            if (age >= lifetime) Destroy(gameObject);
        }
    }

    /// <summary>Ein Heuballen, der den Weg in Richtung Eingang zurückrollt und jedes Alien einmal trifft.</summary>
    public class RollingBale : MonoBehaviour
    {
        const float Speed = 4f;

        LevelManager level;
        int nextWaypoint;
        float distanceLeft;
        float damage;
        float hitRadius;
        Transform bale;
        readonly HashSet<Enemy> hit = new HashSet<Enemy>();

        /// <param name="towardsEntrance">Index des nächsten Weg-Punkts in Richtung Eingang.</param>
        public static void Spawn(LevelManager level, Vector3 position, int towardsEntrance, float distance, float damage, float size)
        {
            var go = new GameObject("Rollender Ballen");
            go.transform.SetParent(level.transform, false);
            go.transform.position = position;
            var r = go.AddComponent<RollingBale>();
            r.level = level;
            r.nextWaypoint = towardsEntrance;
            r.distanceLeft = distance;
            r.damage = damage;
            r.hitRadius = size + 0.25f;
            var pivot = new GameObject("Drehpunkt").transform;
            pivot.SetParent(go.transform, false);
            pivot.localPosition = new Vector3(0f, size, 0f);
            r.bale = pivot;
            var cyl = Visuals.Prim(PrimitiveType.Cylinder, pivot, Vector3.zero, new Vector3(size * 2f, size * 0.9f, size * 2f),
                new Color(0.95f, 0.8f, 0.35f), "Ballen");
            cyl.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
        }

        void Update()
        {
            if (level.State != LevelState.WaveRunning)
            {
                Destroy(gameObject);
                return;
            }

            float step = Mathf.Min(Speed * Time.deltaTime, distanceLeft);
            distanceLeft -= step;
            var path = level.PathWorld;
            while (step > 0f && nextWaypoint >= 0)
            {
                Vector3 delta = path[nextWaypoint] - transform.position;
                float d = delta.magnitude;
                if (d > 0.001f) transform.rotation = Quaternion.LookRotation(delta);
                if (d <= step)
                {
                    transform.position = path[nextWaypoint];
                    step -= d;
                    nextWaypoint--;
                }
                else
                {
                    transform.position += delta / d * step;
                    step = 0f;
                }
            }
            bale.Rotate(Speed * 120f * Time.deltaTime, 0f, 0f, Space.Self);

            var enemies = level.Enemies;
            for (int i = enemies.Count - 1; i >= 0; i--)
            {
                if (i >= enemies.Count) continue;
                var e = enemies[i];
                if (hit.Contains(e)) continue;
                Vector3 off = e.transform.position - transform.position;
                off.y = 0f;
                if (off.magnitude > hitRadius) continue;
                hit.Add(e);
                e.TakeDamage(damage);
            }

            if (distanceLeft <= 0f || nextWaypoint < 0) Destroy(gameObject);
        }
    }
}
