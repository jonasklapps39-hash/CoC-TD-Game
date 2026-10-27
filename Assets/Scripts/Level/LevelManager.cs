using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MuhDefense
{
    public enum LevelState
    {
        Intro,
        Building,
        WaveRunning,
        Won,
        Lost,
    }

    public class FloatingText
    {
        public Vector3 Position;
        public string Text;
        public Color Color;
        public float Age;
    }

    /// <summary>
    /// Steuert ein Tower-Defense-Level: Münzen, Wellen, Türme, Kühe und die Sterne-Wertung.
    /// </summary>
    public class LevelManager : MonoBehaviour
    {
        public const float FloatingTextLifetime = 1.2f;

        public LevelDefinition Def { get; private set; }
        public LevelState State { get; private set; } = LevelState.Intro;
        public int Money { get; private set; }
        /// <summary>Anzahl der bisher gestarteten Wellen (= Nummer der aktuellen Welle).</summary>
        public int WavesStarted { get; private set; }
        public int CowsLost { get; private set; }
        public int CowsRemaining => Def.CowCount - CowsLost;
        public int Stars { get; private set; }
        public int BestStars => PlayerPrefs.GetInt(StarsKey, 0);

        public Tower SelectedTower { get; private set; }
        public TowerType PlacingType { get; private set; }

        public readonly List<Enemy> Enemies = new List<Enemy>();
        public readonly List<Tower> Towers = new List<Tower>();
        public readonly List<Cow> Cows = new List<Cow>();
        public readonly List<FloatingText> FloatingTexts = new List<FloatingText>();

        MapView map;
        Vector3[] pathWorld;
        int pendingGroups;

        string StarsKey => "stars_" + Def.Name;

        public static Vector3 CellCenter(Vector2Int c) => new Vector3(c.x + 0.5f, 0f, c.y + 0.5f);

        public void Init(LevelDefinition def)
        {
            Def = def;
            Money = def.StartMoney;

            map = new GameObject("Karte").AddComponent<MapView>();
            map.transform.SetParent(transform, false);
            map.Build(def);

            pathWorld = new Vector3[def.Waypoints.Length];
            for (int i = 0; i < pathWorld.Length; i++) pathWorld[i] = CellCenter(def.Waypoints[i]);

            for (int i = 0; i < def.CowCount; i++)
            {
                var cow = new GameObject("Kuh").AddComponent<Cow>();
                cow.transform.SetParent(transform, false);
                cow.Init(this, RandomPastureSpot());
                Cows.Add(cow);
            }

            gameObject.AddComponent<GameUI>().Init(this);
        }

        public Vector3 RandomPastureSpot()
        {
            var p = Def.Pasture;
            return new Vector3(Random.Range(p.xMin + 0.35f, p.xMax - 0.35f), 0f, Random.Range(p.yMin + 0.35f, p.yMax - 0.35f));
        }

        void Update()
        {
            for (int i = FloatingTexts.Count - 1; i >= 0; i--)
            {
                FloatingTexts[i].Age += Time.deltaTime;
                if (FloatingTexts[i].Age > FloatingTextLifetime) FloatingTexts.RemoveAt(i);
            }

            if (State == LevelState.WaveRunning && pendingGroups == 0 && Enemies.Count == 0)
            {
                WaveCleared();
            }
        }

        // ---------------------------------------------------------------- Ablauf

        public void BeginLevel()
        {
            if (State == LevelState.Intro) State = LevelState.Building;
        }

        public bool HasMoreWaves => WavesStarted < Def.Waves.Length;

        public void StartNextWave()
        {
            if (State != LevelState.Building || !HasMoreWaves) return;

            var wave = Def.Waves[WavesStarted];
            float hpMultiplier = 1f + Def.HpGrowthPerWave * WavesStarted;
            WavesStarted++;
            State = LevelState.WaveRunning;
            pendingGroups = wave.Groups.Length;
            foreach (var group in wave.Groups) StartCoroutine(SpawnGroup(group, hpMultiplier));
        }

        IEnumerator SpawnGroup(SpawnGroup group, float hpMultiplier)
        {
            if (group.StartDelay > 0f) yield return new WaitForSeconds(group.StartDelay);
            for (int i = 0; i < group.Count; i++)
            {
                SpawnEnemy(group.Kind, hpMultiplier);
                if (i < group.Count - 1) yield return new WaitForSeconds(group.Interval);
            }
            pendingGroups--;
        }

        void SpawnEnemy(EnemyKind kind, float hpMultiplier)
        {
            var enemy = new GameObject("Alien").AddComponent<Enemy>();
            enemy.transform.SetParent(transform, false);
            enemy.Init(this, EnemyType.All[kind], hpMultiplier, pathWorld);
            Enemies.Add(enemy);
        }

        void WaveCleared()
        {
            var wave = Def.Waves[WavesStarted - 1];
            if (wave.Bonus > 0)
            {
                Money += wave.Bonus;
                AddFloatingText(CellCenter(new Vector2Int(Def.Width / 2, Def.Height / 2)), $"Welle geschafft! +{wave.Bonus}", new Color(1f, 0.9f, 0.3f));
            }

            if (HasMoreWaves)
            {
                State = LevelState.Building;
                return;
            }

            State = LevelState.Won;
            Stars = CowsLost == 0 ? 3 : CowsLost <= 2 ? 2 : 1;
            if (Stars > BestStars)
            {
                PlayerPrefs.SetInt(StarsKey, Stars);
                PlayerPrefs.Save();
            }
            CancelPlacing();
            Select(null);
        }

        void Lose()
        {
            State = LevelState.Lost;
            Stars = 0;
            StopAllCoroutines();
            CancelPlacing();
            Select(null);
        }

        public void Restart()
        {
            GameBootstrap.StartLevel();
        }

        // ---------------------------------------------------------------- Aliens und Kühe

        /// <summary>Gibt dem Alien die nächste freie Kuh auf der Weide, falls es eine gibt.</summary>
        public Cow TryGrabCow(Enemy enemy)
        {
            Cow best = null;
            float bestDist = float.MaxValue;
            foreach (var cow in Cows)
            {
                if (cow.State != CowState.Grazing) continue;
                float d = (cow.transform.position - enemy.transform.position).sqrMagnitude;
                if (d < bestDist)
                {
                    best = cow;
                    bestDist = d;
                }
            }
            if (best != null) best.Grab();
            return best;
        }

        public void OnEnemyKilled(Enemy enemy)
        {
            Enemies.Remove(enemy);
            Money += enemy.Type.Reward;
            AddFloatingText(enemy.transform.position + Vector3.up * 0.5f, "+" + enemy.Type.Reward, new Color(1f, 0.9f, 0.3f));
        }

        public void OnEnemyEscaped(Enemy enemy)
        {
            Enemies.Remove(enemy);
            if (enemy.Carried == null) return;

            var cow = enemy.Carried;
            Cows.Remove(cow);
            Destroy(cow.gameObject);
            CowsLost++;
            AddFloatingText(enemy.transform.position + Vector3.up, "Kuh entführt!", new Color(1f, 0.35f, 0.3f));
            if (CowsRemaining <= 0) Lose();
        }

        // ---------------------------------------------------------------- Türme

        public void HandleClick(Vector3 worldPoint)
        {
            if (State != LevelState.Building && State != LevelState.WaveRunning) return;

            var cell = new Vector2Int(Mathf.FloorToInt(worldPoint.x), Mathf.FloorToInt(worldPoint.z));

            if (PlacingType != null)
            {
                if (TryPlace(PlacingType, cell)) CancelPlacing();
                return;
            }

            Select(Towers.Find(t => t.Cell == cell));
        }

        public bool IsBuildable(Vector2Int cell)
        {
            return map.InBounds(cell)
                && !map.IsPath(cell)
                && !Def.Pasture.Contains(cell)
                && !Towers.Exists(t => t.Cell == cell);
        }

        public void BeginPlacing(TowerType type)
        {
            if (Money < type.Cost) return;
            Select(null);
            PlacingType = type;
            map.ShowBuildable(true, IsBuildable);
        }

        public void CancelPlacing()
        {
            if (PlacingType == null) return;
            PlacingType = null;
            map.ShowBuildable(false, IsBuildable);
        }

        bool TryPlace(TowerType type, Vector2Int cell)
        {
            if (!IsBuildable(cell))
            {
                AddFloatingText(CellCenter(cell), "Hier nicht!", new Color(1f, 0.4f, 0.3f));
                return false;
            }
            if (Money < type.Cost) return false;

            Money -= type.Cost;
            var tower = new GameObject("Turm").AddComponent<Tower>();
            tower.transform.SetParent(transform, false);
            tower.Init(this, type, cell);
            Towers.Add(tower);
            return true;
        }

        public void Select(Tower tower)
        {
            if (SelectedTower != null) SelectedTower.SetSelected(false);
            SelectedTower = tower;
            if (SelectedTower != null) SelectedTower.SetSelected(true);
        }

        public void UpgradeSelected()
        {
            var tower = SelectedTower;
            if (tower == null || !tower.NextUpgradeCost.HasValue) return;
            int cost = tower.NextUpgradeCost.Value;
            if (Money < cost) return;
            Money -= cost;
            tower.Upgrade(cost);
        }

        public void SellSelected()
        {
            var tower = SelectedTower;
            if (tower == null) return;
            Select(null);
            Money += tower.SellValue;
            AddFloatingText(tower.transform.position + Vector3.up * 0.5f, "+" + tower.SellValue, new Color(1f, 0.9f, 0.3f));
            Towers.Remove(tower);
            Destroy(tower.gameObject);
        }

        public void AddFloatingText(Vector3 position, string text, Color color)
        {
            FloatingTexts.Add(new FloatingText { Position = position, Text = text, Color = color });
        }
    }
}
