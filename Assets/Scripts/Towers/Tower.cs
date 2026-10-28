using UnityEngine;

namespace MuhDefense
{
    /// <summary>Ein platzierter Turm im Level.</summary>
    public class Tower : MonoBehaviour
    {
        public TowerType Type { get; private set; }
        public int Level { get; private set; } = 1;
        public Vector2Int Cell { get; private set; }
        /// <summary>Insgesamt ausgegebene Münzen (für den Verkaufswert).</summary>
        public int Invested { get; private set; }
        /// <summary>Höchste Stufe in diesem Level – bestimmt durch die Werkstatt auf dem Hof.</summary>
        public int AllowedMaxLevel { get; private set; } = TowerType.MaxLevel;
        /// <summary>Der Turm könnte höher, aber die Werkstatt auf dem Hof ist noch nicht weit genug.</summary>
        public bool LimitedByWorkshop => Level >= AllowedMaxLevel && Level < TowerType.MaxLevel;

        public float Range => Type.Range[Level - 1];
        public float Damage => Type.Damage[Level - 1];
        public float FireRate => Type.FireRate[Level - 1];
        public float Splash => Type.Splash[Level - 1];
        public float Slow => Type.Slow[Level - 1];
        public int SellValue => Mathf.RoundToInt(Invested * 0.7f);
        public int? NextUpgradeCost => Level < AllowedMaxLevel ? Type.UpgradeCosts[Level - 1] : (int?)null;

        LevelManager level;
        float cooldown;
        Transform turret;
        Transform pips;
        LineRenderer rangeCircle;

        public void Init(LevelManager level, TowerType type, Vector2Int cell, int allowedMaxLevel)
        {
            this.level = level;
            AllowedMaxLevel = Mathf.Clamp(allowedMaxLevel, 1, TowerType.MaxLevel);
            Type = type;
            Cell = cell;
            Invested = type.Cost;
            name = type.Name;
            transform.position = LevelManager.CellCenter(cell);
            BuildModel();
            rangeCircle = Visuals.Circle(transform, Range, new Color(1f, 1f, 1f, 0.8f));
            rangeCircle.enabled = false;
            UpdatePips();
        }

        public void SetSelected(bool selected)
        {
            rangeCircle.enabled = selected;
        }

        public void Upgrade(int cost)
        {
            Level++;
            Invested += cost;
            Visuals.SetCircleRadius(rangeCircle, Range);
            turret.localScale = Vector3.one * (1f + 0.12f * (Level - 1));
            UpdatePips();
            RingEffect.Spawn(level.transform, transform.position, 0.8f, new Color(1f, 0.9f, 0.3f, 1f), 0.5f);
        }

        void Update()
        {
            if (level.State != LevelState.WaveRunning) return;

            cooldown = Mathf.Max(0f, cooldown - Time.deltaTime);

            if (Type.Kind == TowerKind.Aura)
            {
                if (cooldown <= 0f && Pulse()) cooldown = 1f / FireRate;
                return;
            }

            var target = FindTarget();
            if (target == null) return;

            Vector3 look = target.transform.position - transform.position;
            look.y = 0f;
            if (look.sqrMagnitude > 0.001f)
            {
                turret.rotation = Quaternion.Slerp(turret.rotation, Quaternion.LookRotation(look), 12f * Time.deltaTime);
            }

            if (cooldown <= 0f)
            {
                Fire(target);
                cooldown = 1f / FireRate;
            }
        }

        bool InRange(Enemy e)
        {
            Vector3 d = e.transform.position - transform.position;
            d.y = 0f;
            return d.magnitude <= Range;
        }

        /// <summary>Sucht das Alien in Reichweite mit der höchsten Priorität (Kuh-Träger zuerst).</summary>
        Enemy FindTarget()
        {
            Enemy best = null;
            float bestPriority = float.MinValue;
            foreach (var e in level.Enemies)
            {
                if (!e.Alive || !InRange(e)) continue;
                if (e.Priority > bestPriority)
                {
                    best = e;
                    bestPriority = e.Priority;
                }
            }
            return best;
        }

        void Fire(Enemy target)
        {
            Vector3 from = transform.position + Vector3.up * 0.8f;
            switch (Type.Kind)
            {
                case TowerKind.Projectile:
                    Projectile.Spawn(level, from, target, Damage, Type.ProjectileSpeed, 0f, false,
                        PrimitiveType.Cube, new Vector3(0.05f, 0.05f, 0.35f), new Color(0.75f, 0.75f, 0.8f));
                    break;
                case TowerKind.Arc:
                    Projectile.Spawn(level, from, target, Damage, Type.ProjectileSpeed, Splash, true,
                        PrimitiveType.Cube, new Vector3(0.28f, 0.22f, 0.28f), new Color(0.95f, 0.82f, 0.35f));
                    break;
            }
        }

        /// <returns>true, wenn mindestens ein Alien getroffen wurde.</returns>
        bool Pulse()
        {
            bool hit = false;
            var enemies = level.Enemies;
            for (int i = enemies.Count - 1; i >= 0; i--)
            {
                var e = enemies[i];
                if (!InRange(e)) continue;
                hit = true;
                e.ApplySlow(Slow, Type.SlowDuration);
                e.TakeDamage(Damage);
            }
            if (hit) RingEffect.Spawn(level.transform, transform.position, Range, new Color(0.7f, 0.9f, 1f, 0.9f), 0.45f);
            return hit;
        }

        void UpdatePips()
        {
            if (pips != null) Destroy(pips.gameObject);
            pips = new GameObject("Stufe").transform;
            pips.SetParent(transform, false);
            for (int i = 0; i < Level; i++)
            {
                Visuals.Prim(PrimitiveType.Sphere, pips, new Vector3(-0.2f + 0.2f * i, 0.14f, -0.42f), Vector3.one * 0.12f,
                    new Color(1f, 0.85f, 0.15f), "Stern");
            }
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
                    // Bauer mit Latzhose, Strohhut und Mistgabel
                    Visuals.Prim(PrimitiveType.Capsule, turret, new Vector3(0f, 0.4f, 0f), new Vector3(0.3f, 0.28f, 0.3f), Type.Color, "Latzhose");
                    Visuals.Prim(PrimitiveType.Sphere, turret, new Vector3(0f, 0.78f, 0f), Vector3.one * 0.24f, new Color(1f, 0.8f, 0.65f), "Kopf");
                    Visuals.Prim(PrimitiveType.Cylinder, turret, new Vector3(0f, 0.9f, 0f), new Vector3(0.4f, 0.02f, 0.4f), new Color(0.95f, 0.85f, 0.4f), "Strohhut");
                    Visuals.Prim(PrimitiveType.Cylinder, turret, new Vector3(0f, 0.96f, 0f), new Vector3(0.2f, 0.06f, 0.2f), new Color(0.95f, 0.85f, 0.4f), "Hutspitze");
                    Visuals.Prim(PrimitiveType.Cube, turret, new Vector3(0.2f, 0.6f, 0.08f), new Vector3(0.04f, 0.75f, 0.04f), wood, "Stiel");
                    Visuals.Prim(PrimitiveType.Cube, turret, new Vector3(0.2f, 1f, 0.08f), new Vector3(0.14f, 0.12f, 0.03f), new Color(0.7f, 0.7f, 0.75f), "Gabel");
                    break;

                case TowerKind.Arc:
                    // Holzkatapult mit Heuballen
                    Visuals.Prim(PrimitiveType.Cube, turret, new Vector3(0f, 0.2f, 0f), new Vector3(0.7f, 0.25f, 0.6f), wood, "Gestell");
                    var arm = Visuals.Prim(PrimitiveType.Cube, turret, new Vector3(0f, 0.5f, -0.05f), new Vector3(0.1f, 0.08f, 0.8f), wood, "Arm");
                    arm.transform.localRotation = Quaternion.Euler(-25f, 0f, 0f);
                    Visuals.Prim(PrimitiveType.Cube, turret, new Vector3(0f, 0.72f, -0.38f), new Vector3(0.3f, 0.24f, 0.3f), Type.Color, "Heuballen");
                    break;

                case TowerKind.Aura:
                    // Milchtank mit Sprühdüse
                    Visuals.Prim(PrimitiveType.Cylinder, turret, new Vector3(0f, 0.4f, 0f), new Vector3(0.55f, 0.35f, 0.55f), Color.white, "Tank");
                    Visuals.Prim(PrimitiveType.Cylinder, turret, new Vector3(0f, 0.5f, 0f), new Vector3(0.57f, 0.05f, 0.57f), new Color(0.3f, 0.55f, 0.95f), "Streifen");
                    Visuals.Prim(PrimitiveType.Sphere, turret, new Vector3(0f, 0.8f, 0f), Vector3.one * 0.3f, Type.Color, "Düse");
                    break;
            }
        }
    }
}
