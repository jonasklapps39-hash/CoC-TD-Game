using System.Collections.Generic;
using UnityEngine;

namespace MuhDefense
{
    /// <summary>Gemeinsame Basis für Einheiten, die ein Turm auf den Weg schickt (Mähdrescher, Hofhunde).</summary>
    public abstract class TowerUnit : MonoBehaviour
    {
        protected LevelManager level;
        protected Tower tower;
        protected int index;
        protected List<Vector3> route = new List<Vector3>();
        protected Transform model;

        public void Init(LevelManager level, Tower tower, int index)
        {
            this.level = level;
            this.tower = tower;
            this.index = index;
            model = new GameObject("Modell").transform;
            model.SetParent(transform, false);
            BuildModel();
            OnStatsChanged();
        }

        protected abstract void BuildModel();

        /// <summary>Nach einem Upgrade: Strecke neu berechnen (Reichweite kann sich geändert haben).</summary>
        public virtual void OnStatsChanged()
        {
            route = LongestRun(level.PathPointsWithin(tower.transform.position, tower.Range));
        }

        /// <summary>Der Weg kann den Kreis mehrmals schneiden – wir nehmen das längste zusammenhängende Stück.</summary>
        static List<Vector3> LongestRun(List<Vector3> points)
        {
            var best = new List<Vector3>();
            var current = new List<Vector3>();
            for (int i = 0; i < points.Count; i++)
            {
                if (current.Count > 0 && Vector3.Distance(current[current.Count - 1], points[i]) > 0.4f)
                {
                    if (current.Count > best.Count) best = current;
                    current = new List<Vector3>();
                }
                current.Add(points[i]);
            }
            if (current.Count > best.Count) best = current;
            return best;
        }

        protected bool CanHit(Enemy e) => e != null && e.Alive && !e.IsFlying && tower.CanTarget(e);

        /// <summary>Zusatzwirkungen eines Treffers aus den Turm-Upgrades.</summary>
        protected void ApplyEffects(Enemy e)
        {
            var s = tower.Stats;
            if (s.Slow < 1f) e.ApplySlow(s.Slow, s.SlowDuration);
            if (s.DotDps > 0f) e.ApplyDot(s.DotDps, s.DotDuration);
            if (s.BurnDps > 0f) e.ApplyBurn(s.BurnDps, s.BurnDuration, false);
            if (s.ArmorShred > 0f) e.ShredArmor(s.ArmorShred, 2f);
            if (s.PermanentVulnerability > 0f) e.AddPermanentVulnerability(s.PermanentVulnerability);
            if (s.BonusReward > 0f) e.MarkBonusReward(s.BonusReward);
        }

        protected void Face(Vector3 direction)
        {
            direction.y = 0f;
            if (direction.sqrMagnitude > 0.0001f)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), 10f * Time.deltaTime);
            }
        }
    }

    /// <summary>Mähdrescher: fährt auf dem Weg in Reichweite hin und her und mäht Aliens nieder.</summary>
    public class HarvesterUnit : TowerUnit
    {
        float position;
        float direction = 1f;
        readonly Dictionary<Enemy, float> lastHit = new Dictionary<Enemy, float>();

        protected override void BuildModel()
        {
            if (ModelLibrary.Spawn("Maehdrescher", model, 0.75f, ModelLibrary.Fit.Footprint) != null) return;
            Visuals.Prim(PrimitiveType.Cube, model, new Vector3(0f, 0.25f, 0f), new Vector3(0.5f, 0.4f, 0.6f), new Color(0.3f, 0.65f, 0.25f), "Mähdrescher");
            Visuals.Prim(PrimitiveType.Cylinder, model, new Vector3(0f, 0.15f, 0.35f), new Vector3(0.18f, 0.35f, 0.18f), new Color(0.95f, 0.75f, 0.25f), "Haspel")
                .transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
        }

        public override void OnStatsChanged()
        {
            base.OnStatsChanged();
            if (route.Count == 0) return;
            // Mehrere Mähdrescher verteilen sich über die Strecke
            position = (route.Count - 1) * (index + 0.5f) / tower.UnitTotal;
            transform.position = route[Mathf.Clamp(Mathf.RoundToInt(position), 0, route.Count - 1)];
        }

        void Update()
        {
            if (route.Count < 2) return;
            if (level.State != LevelState.WaveRunning) return;
            float dt = Time.deltaTime;
            var s = tower.Stats;

            // Strecke abfahren (Punkte liegen 0,25 Felder auseinander)
            position += direction * s.UnitSpeed * dt / 0.25f;
            if (position >= route.Count - 1)
            {
                position = route.Count - 1;
                direction = -1f;
            }
            else if (position <= 0f)
            {
                position = 0f;
                direction = 1f;
            }
            int i = Mathf.Clamp(Mathf.FloorToInt(position), 0, route.Count - 2);
            Vector3 p = Vector3.Lerp(route[i], route[i + 1], position - i);
            Face(p - transform.position);
            transform.position = p;
            model.localPosition = new Vector3(0f, Mathf.Abs(Mathf.Sin(Time.time * 14f)) * 0.03f, 0f);

            // Mähen
            float interval = 1f / Mathf.Max(0.1f, s.FireRate);
            var enemies = level.Enemies;
            for (int k = enemies.Count - 1; k >= 0; k--)
            {
                if (k >= enemies.Count) continue;
                var e = enemies[k];
                if (!CanHit(e)) continue;
                Vector3 d = e.transform.position - transform.position;
                d.y = 0f;
                if (d.magnitude > s.UnitRadius) continue;
                if (lastHit.TryGetValue(e, out float t) && Time.time - t < interval) continue;
                lastHit[e] = Time.time;
                ApplyEffects(e);
                if (s.Knockback > 0f) e.PushBack(s.Knockback);
                e.TakeDamage(s.Damage, s.ArmorPiercing, s.ArmoredBonus);
                Effects.Burst(level.transform, e.AimPoint, new Color(0.95f, 0.8f, 0.35f), 3, 2f);
                GameAudio.Play(SoundId.Hit, 0.7f);
            }
            if (lastHit.Count > 60) lastHit.Clear();
        }
    }

    /// <summary>Hofhund: wartet auf dem Weg, hält ein Alien fest und beißt, bis er erschöpft ist.</summary>
    public class DogUnit : TowerUnit
    {
        const float EngageRadius = 0.55f;
        const float LeashRadius = 1.2f;

        Vector3 post;
        float hp;
        float respawnTimer;
        bool alive = true;
        Enemy target;
        float biteTimer;

        protected override void BuildModel()
        {
            if (ModelLibrary.Spawn("Hofhund", model, 0.55f) != null) return;
            var fur = new Color(0.78f, 0.55f, 0.3f);
            Visuals.Prim(PrimitiveType.Capsule, model, new Vector3(0f, 0.2f, 0f), new Vector3(0.18f, 0.2f, 0.18f), fur, "Körper")
                .transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            Visuals.Prim(PrimitiveType.Sphere, model, new Vector3(0f, 0.3f, 0.2f), Vector3.one * 0.16f, fur, "Kopf");
        }

        public override void OnStatsChanged()
        {
            base.OnStatsChanged();
            if (route.Count == 0) return;
            int n = tower.UnitTotal;
            post = route[Mathf.Clamp(Mathf.RoundToInt((route.Count - 1) * (index + 1f) / (n + 1f)), 0, route.Count - 1)];
            if (target == null) transform.position = post;
            if (alive) hp = Mathf.Max(hp, tower.Stats.UnitHp);
        }

        void Update()
        {
            if (route.Count == 0) return;
            float dt = Time.deltaTime;
            var s = tower.Stats;

            if (level.State != LevelState.WaveRunning)
            {
                // Zwischen den Wellen: alle Hunde sind wieder fit
                if (!alive) Revive();
                hp = s.UnitHp;
                target = null;
                transform.position = Vector3.MoveTowards(transform.position, post, 2f * dt);
                return;
            }

            if (!alive)
            {
                respawnTimer -= dt;
                if (respawnTimer <= 0f) Revive();
                return;
            }

            if (target != null && (!target.Alive || target.IsFlying || Vector3.Distance(target.transform.position, post) > LeashRadius))
            {
                target = null;
            }
            if (target == null) target = FindTarget(s);

            if (target == null)
            {
                Face(post - transform.position);
                transform.position = Vector3.MoveTowards(transform.position, post, 2.5f * dt);
                model.localPosition = Vector3.zero;
                return;
            }

            // Festhalten und beißen
            Vector3 at = target.transform.position - target.transform.forward * 0.25f;
            Face(target.transform.position - transform.position);
            transform.position = Vector3.MoveTowards(transform.position, at, 4f * dt);
            target.Hold(0.2f, s.BlocksBoss);
            model.localPosition = new Vector3(0f, Mathf.Abs(Mathf.Sin(Time.time * 18f)) * 0.05f, 0f);

            biteTimer -= dt;
            if (biteTimer <= 0f)
            {
                biteTimer = 0.5f;
                float bite = s.Damage * 0.5f * (s.RescuesCows && target.Carried != null ? 3f : 1f);
                ApplyEffects(target);
                target.TakeDamage(bite, s.ArmorPiercing, s.ArmoredBonus);
            }

            // Das Alien wehrt sich: stärkere Aliens erschöpfen den Hund schneller
            float strain = 6f + target.MaxHp * 0.06f;
            if (target.IsBig) strain *= 1.5f;
            hp -= strain * dt;
            if (hp <= 0f) Exhausted();
        }

        Enemy FindTarget(TowerStats s)
        {
            Enemy best = null;
            float bestDist = EngageRadius;
            foreach (var e in level.Enemies)
            {
                if (!CanHit(e)) continue;
                if (e.IsBoss && !s.BlocksBoss) continue;
                float d = Vector3.Distance(e.transform.position, post);
                if (d < bestDist)
                {
                    bestDist = d;
                    best = e;
                }
            }
            if (best != null)
            {
                GameAudio.Play(SoundId.Bark, 0.6f);
                biteTimer = 0f;
            }
            return best;
        }

        void Exhausted()
        {
            alive = false;
            target = null;
            respawnTimer = tower.Stats.RespawnTime;
            model.gameObject.SetActive(false);
            Effects.Burst(level.transform, transform.position + Vector3.up * 0.3f, new Color(0.78f, 0.55f, 0.3f), 8, 2f);
            level.AddFloatingText(transform.position + Vector3.up * 0.6f, "Wuff …", new Color(1f, 0.85f, 0.6f));
        }

        void Revive()
        {
            alive = true;
            hp = tower.Stats.UnitHp;
            transform.position = tower.transform.position;
            model.gameObject.SetActive(true);
        }
    }

    /// <summary>Güllepfütze: Aliens darin werden vergiftet und rutschen langsamer.</summary>
    public class SlurryPuddle : MonoBehaviour
    {
        LevelManager level;
        TowerStats stats;
        float age;
        float lifetime;
        float radius;
        Transform visual;

        public static void Spawn(LevelManager level, Tower tower, Vector3 position)
        {
            var go = new GameObject("Gülle");
            go.transform.SetParent(level.transform, false);
            go.transform.position = new Vector3(position.x, 0f, position.z);
            var p = go.AddComponent<SlurryPuddle>();
            p.level = level;
            p.stats = tower.Stats;
            p.lifetime = tower.Stats.PuddleDuration;
            p.radius = tower.Stats.PuddleRadius;
            p.visual = new GameObject("Pfütze").transform;
            p.visual.SetParent(go.transform, false);
            Visuals.Prim(PrimitiveType.Cylinder, p.visual, new Vector3(0f, 0.015f, 0f), new Vector3(p.radius * 2f, 0.01f, p.radius * 1.7f),
                new Color(0.42f, 0.28f, 0.12f), "Gülle", outline: false);
            Visuals.Prim(PrimitiveType.Sphere, p.visual, new Vector3(p.radius * 0.3f, 0.03f, 0.1f), new Vector3(0.14f, 0.05f, 0.14f),
                new Color(0.55f, 0.4f, 0.16f), "Blase", outline: false);
            Visuals.Prim(PrimitiveType.Sphere, p.visual, new Vector3(-p.radius * 0.35f, 0.03f, -0.12f), new Vector3(0.1f, 0.04f, 0.1f),
                new Color(0.55f, 0.4f, 0.16f), "Blase", outline: false);
        }

        void Update()
        {
            if (level.State != LevelState.WaveRunning)
            {
                Destroy(gameObject);
                return;
            }
            age += Time.deltaTime;
            float shrink = Mathf.Clamp01((lifetime - age) / 0.6f);
            visual.localScale = new Vector3(shrink, 1f, shrink);
            foreach (var e in level.Enemies)
            {
                if (!e.Alive || (e.IsFlying && !stats.HitsAir)) continue;
                Vector3 d = e.transform.position - transform.position;
                d.y = 0f;
                if (d.magnitude > radius) continue;
                e.ApplyDot(stats.DotDps, stats.DotDuration);
                if (stats.Slow < 1f) e.ApplySlow(stats.Slow, stats.SlowDuration);
                if (stats.ArmorShred > 0f) e.ShredArmor(stats.ArmorShred, 1f);
                if (stats.PermanentVulnerability > 0f) e.AddPermanentVulnerability(stats.PermanentVulnerability);
            }
            if (age >= lifetime) Destroy(gameObject);
        }
    }
}
