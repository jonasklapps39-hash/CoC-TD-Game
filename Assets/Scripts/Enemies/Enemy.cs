using UnityEngine;

namespace MuhDefense
{
    /// <summary>
    /// Ein Alien. Läuft den Weg entlang zur Weide, schnappt sich dort eine Kuh
    /// und trägt sie den Weg zurück zum Eingang. Wird es vorher besiegt,
    /// läuft die Kuh zurück auf die Weide.
    /// </summary>
    public class Enemy : MonoBehaviour
    {
        public EnemyType Type { get; private set; }
        public float MaxHp { get; private set; }
        public float Hp { get; private set; }
        public Cow Carried { get; private set; }
        public bool Alive => Hp > 0f;

        /// <summary>Je höher, desto dringender sollten Türme dieses Alien angreifen.</summary>
        public float Priority => returning ? 1000f + travelled : travelled;

        public Vector3 AimPoint => transform.position + Vector3.up * (0.45f * Type.Scale);
        public Vector3 HealthBarPoint => transform.position + Vector3.up * (1.05f * Type.Scale + (Carried != null ? 0.6f : 0f));

        LevelManager level;
        Vector3[] path;
        int targetIndex;
        bool returning;
        bool waitingForCow;
        float travelled;
        float slowFactor = 1f;
        float slowTimer;
        Transform model;

        public void Init(LevelManager level, EnemyType type, float hpMultiplier, Vector3[] path)
        {
            this.level = level;
            this.path = path;
            Type = type;
            MaxHp = type.Hp * hpMultiplier;
            Hp = MaxHp;
            name = type.Name;
            transform.position = path[0];
            targetIndex = 1;
            BuildModel();
        }

        void Update()
        {
            if (level.State != LevelState.WaveRunning) return;

            float dt = Time.deltaTime;
            if (slowTimer > 0f)
            {
                slowTimer -= dt;
                if (slowTimer <= 0f) slowFactor = 1f;
            }

            if (waitingForCow)
            {
                var cow = level.TryGrabCow(this);
                if (cow == null) return;
                Carried = cow;
                waitingForCow = false;
                returning = true;
                travelled = 0f;
                targetIndex = path.Length - 2;
            }

            float speed = Type.Speed * slowFactor * (Carried != null ? 0.75f : 1f);
            Move(speed * dt);

            // Wackeln beim Laufen
            if (model != null)
            {
                float bob = Mathf.Abs(Mathf.Sin(Time.time * speed * 6f)) * 0.06f;
                model.localPosition = new Vector3(0f, bob, 0f);
            }
        }

        void Move(float step)
        {
            while (step > 0f)
            {
                Vector3 target = path[targetIndex];
                Vector3 delta = target - transform.position;
                float dist = delta.magnitude;
                if (dist > 0.0001f) transform.rotation = Quaternion.LookRotation(delta);

                if (dist <= step)
                {
                    transform.position = target;
                    step -= dist;
                    travelled += dist;
                    if (!AdvanceWaypoint()) return;
                }
                else
                {
                    transform.position += delta / dist * step;
                    travelled += step;
                    return;
                }
            }
        }

        /// <returns>false, wenn das Alien nicht weiterläuft (Weide erreicht oder entkommen).</returns>
        bool AdvanceWaypoint()
        {
            if (!returning)
            {
                targetIndex++;
                if (targetIndex >= path.Length)
                {
                    targetIndex = path.Length - 1;
                    waitingForCow = true;
                    return false;
                }
            }
            else
            {
                targetIndex--;
                if (targetIndex < 0)
                {
                    level.OnEnemyEscaped(this);
                    Destroy(gameObject);
                    return false;
                }
            }
            return true;
        }

        void LateUpdate()
        {
            if (Carried != null)
            {
                Carried.transform.position = transform.position + Vector3.up * (0.9f * Type.Scale);
                Carried.transform.Rotate(0f, 120f * Time.deltaTime, 0f, Space.World);
            }
        }

        public void TakeDamage(float damage)
        {
            if (!Alive) return;
            float dealt = Mathf.Max(damage - Type.Armor, damage * 0.25f);
            Hp -= dealt;
            if (Hp <= 0f) Die();
        }

        /// <param name="factor">Geschwindigkeits-Faktor, z. B. 0.6 = 40 % langsamer.</param>
        public void ApplySlow(float factor, float duration)
        {
            slowFactor = Mathf.Min(slowFactor, factor);
            slowTimer = Mathf.Max(slowTimer, duration);
        }

        void Die()
        {
            if (Carried != null)
            {
                Carried.Drop(transform.position);
                Carried = null;
            }
            level.OnEnemyKilled(this);
            Destroy(gameObject);
        }

        void BuildModel()
        {
            model = new GameObject("Modell").transform;
            model.SetParent(transform, false);
            var root = new GameObject("Skalierung").transform;
            root.SetParent(model, false);
            root.localScale = Vector3.one * Type.Scale;

            Color body = Type.Color;
            Color dark = Color.Lerp(body, Color.black, 0.5f);
            var black = new Color(0.05f, 0.05f, 0.05f);

            Visuals.Prim(PrimitiveType.Capsule, root, new Vector3(0f, 0.25f, 0f), new Vector3(0.35f, 0.25f, 0.35f), body, "Körper");
            Visuals.Prim(PrimitiveType.Sphere, root, new Vector3(0f, 0.6f, 0f), Vector3.one * 0.38f, body, "Kopf");
            Visuals.Prim(PrimitiveType.Sphere, root, new Vector3(-0.08f, 0.65f, 0.15f), Vector3.one * 0.1f, black, "Auge");
            Visuals.Prim(PrimitiveType.Sphere, root, new Vector3(0.08f, 0.65f, 0.15f), Vector3.one * 0.1f, black, "Auge");
            for (int side = -1; side <= 1; side += 2)
            {
                Visuals.Prim(PrimitiveType.Cube, root, new Vector3(0.08f * side, 0.86f, 0f), new Vector3(0.03f, 0.18f, 0.03f), dark, "Fühler");
                Visuals.Prim(PrimitiveType.Sphere, root, new Vector3(0.08f * side, 0.97f, 0f), Vector3.one * 0.07f, Color.yellow, "Fühlerspitze");
            }

            switch (Type.Kind)
            {
                case EnemyKind.Brocken:
                    Visuals.Prim(PrimitiveType.Cube, root, new Vector3(0f, 0.3f, 0.12f), new Vector3(0.34f, 0.28f, 0.1f), new Color(0.55f, 0.55f, 0.6f), "Panzerplatte");
                    break;
                case EnemyKind.Boss:
                    Visuals.Prim(PrimitiveType.Cube, root, new Vector3(0f, 0.3f, 0.12f), new Vector3(0.34f, 0.28f, 0.1f), new Color(0.3f, 0.3f, 0.35f), "Panzerplatte");
                    Visuals.Prim(PrimitiveType.Cylinder, root, new Vector3(0f, 0.82f, 0f), new Vector3(0.22f, 0.06f, 0.22f), new Color(1f, 0.8f, 0.1f), "Krone");
                    break;
                case EnemyKind.Flitzer:
                    Visuals.Prim(PrimitiveType.Cube, root, new Vector3(0f, 0.35f, -0.18f), new Vector3(0.05f, 0.05f, 0.2f), dark, "Düse");
                    break;
            }
        }
    }
}
