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
        public bool IsBoss => Type.Kind == EnemyKind.Boss;
        public bool IsArmored => Type.Armor > 0f;
        /// <summary>Große Aliens (Brocken, Boss) lassen sich nur von starker Kälte einfrieren.</summary>
        public bool IsBig => Type.Kind == EnemyKind.Brocken || IsBoss;
        public bool IsFrozen => freezeTimer > 0f;

        /// <summary>Je höher, desto weiter ist das Alien gekommen (Kuh-Träger zuerst).</summary>
        public float Priority => returning ? 1000f + travelled : travelled;

        /// <summary>Index des Weg-Punkts hinter dem Alien in Richtung Eingang.</summary>
        public int WaypointTowardsEntrance => returning ? targetIndex : Mathf.Max(0, targetIndex - 1);

        public Vector3 AimPoint => transform.position + Vector3.up * (0.45f * Type.Scale);
        public Vector3 HealthBarPoint => transform.position + Vector3.up * (1.05f * Type.Scale + (Carried != null ? 0.6f : 0f));

        LevelManager level;
        Vector3[] path;
        int targetIndex;
        bool returning;
        bool waitingForCow;
        float travelled;
        Transform model;

        float slowFactor = 1f;
        float slowTimer;
        float freezeTimer;
        float frozenVulnerability = 1f;
        float dotDps;
        float dotTimer;
        float burnDps;
        float burnTimer;
        bool burnIgnoresArmor;
        float armorShred;
        float armorShredTimer;
        float permanentVulnerability;
        GameObject iceBlock;
        GameObject flame;

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
            UpdateEffects(dt);
            if (!Alive) return;

            if (freezeTimer > 0f) return;

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

            if (model != null)
            {
                float bob = Mathf.Abs(Mathf.Sin(Time.time * speed * 6f)) * 0.06f;
                model.localPosition = new Vector3(0f, bob, 0f);
            }
        }

        void UpdateEffects(float dt)
        {
            if (slowTimer > 0f)
            {
                slowTimer -= dt;
                if (slowTimer <= 0f) slowFactor = 1f;
            }
            if (freezeTimer > 0f)
            {
                freezeTimer -= dt;
                if (freezeTimer <= 0f) SetIce(false);
            }
            if (armorShredTimer > 0f)
            {
                armorShredTimer -= dt;
                if (armorShredTimer <= 0f) armorShred = 0f;
            }
            if (dotTimer > 0f)
            {
                dotTimer -= dt;
                TakeDamage(dotDps * dt, true);
            }
            if (burnTimer > 0f)
            {
                burnTimer -= dt;
                TakeDamage(burnDps * dt, burnIgnoresArmor);
                if (burnTimer <= 0f)
                {
                    burnIgnoresArmor = false;
                    SetFlame(false);
                }
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

        // ---------------------------------------------------------------- Schaden und Effekte

        /// <param name="armorPiercing">Panzer wird ignoriert.</param>
        /// <param name="armoredBonus">Faktor gegen gepanzerte Aliens.</param>
        /// <returns>Tatsächlich verursachter Schaden.</returns>
        public float TakeDamage(float damage, bool armorPiercing = false, float armoredBonus = 1f)
        {
            if (!Alive || damage <= 0f) return 0f;
            if (IsArmored) damage *= armoredBonus;
            float armor = armorPiercing ? 0f : Mathf.Max(0f, Type.Armor - armorShred);
            float dealt = Mathf.Max(damage - armor, damage * 0.25f);
            dealt *= 1f + permanentVulnerability + (IsFrozen ? frozenVulnerability - 1f : 0f);
            Hp -= dealt;
            if (Hp <= 0f) Die();
            return dealt;
        }

        /// <param name="factor">Geschwindigkeits-Faktor, z. B. 0.6 = 40 % langsamer.</param>
        public void ApplySlow(float factor, float duration)
        {
            slowFactor = Mathf.Min(slowFactor, Mathf.Max(0.15f, factor));
            slowTimer = Mathf.Max(slowTimer, duration);
        }

        public void Freeze(float duration, float vulnerability, bool canFreezeBig)
        {
            if (IsBig && !canFreezeBig) return;
            if (IsBoss) duration *= 0.5f;
            freezeTimer = Mathf.Max(freezeTimer, duration);
            frozenVulnerability = Mathf.Max(frozenVulnerability, vulnerability);
            SetIce(true);
        }

        public void ApplyDot(float dps, float duration)
        {
            dotDps = Mathf.Max(dotDps, dps);
            dotTimer = Mathf.Max(dotTimer, duration);
        }

        public void ApplyBurn(float dps, float duration, bool ignoresArmor)
        {
            if (dps <= 0f) return;
            burnDps = Mathf.Max(burnTimer > 0f ? burnDps : 0f, dps);
            burnTimer = Mathf.Max(burnTimer, duration);
            burnIgnoresArmor |= ignoresArmor;
            SetFlame(true);
        }

        public void ShredArmor(float amount, float duration)
        {
            armorShred = Mathf.Max(armorShred, amount);
            armorShredTimer = Mathf.Max(armorShredTimer, duration);
        }

        public void AddPermanentVulnerability(float amount)
        {
            permanentVulnerability = Mathf.Max(permanentVulnerability, amount);
        }

        /// <summary>Schiebt das Alien ein Stück entgegen seiner Laufrichtung zurück (nicht bei Bossen).</summary>
        public void PushBack(float distance)
        {
            if (IsBoss || !Alive) return;
            if (waitingForCow)
            {
                waitingForCow = false;
                targetIndex = path.Length - 1;
            }
            while (distance > 0f)
            {
                int back = returning ? targetIndex + 1 : targetIndex - 1;
                if (back < 0 || back >= path.Length) return;
                Vector3 p = path[back];
                float d = Vector3.Distance(transform.position, p);
                if (d <= distance)
                {
                    transform.position = p;
                    distance -= d;
                    travelled = Mathf.Max(0f, travelled - d);
                    targetIndex = back;
                }
                else
                {
                    transform.position += (p - transform.position).normalized * distance;
                    travelled = Mathf.Max(0f, travelled - distance);
                    return;
                }
            }
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

        void SetIce(bool on)
        {
            if (on && iceBlock == null)
            {
                iceBlock = Visuals.Prim(PrimitiveType.Cube, transform, new Vector3(0f, 0.45f * Type.Scale, 0f),
                    new Vector3(0.6f, 0.95f, 0.6f) * Type.Scale, new Color(0.7f, 0.9f, 1f), "Eisblock");
            }
            if (iceBlock != null) iceBlock.SetActive(on);
        }

        void SetFlame(bool on)
        {
            if (on && flame == null)
            {
                flame = Visuals.Prim(PrimitiveType.Sphere, transform, new Vector3(0f, 1.0f * Type.Scale, 0f),
                    new Vector3(0.18f, 0.28f, 0.18f) * Type.Scale, new Color(1f, 0.45f, 0.1f), "Flamme");
            }
            if (flame != null) flame.SetActive(on);
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
