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
        public int BestStars => GameFlow.Farm.BestStars(Def.Id);
        /// <summary>Belohnung für den Hof, sobald das Level gewonnen ist.</summary>
        public LevelReward Reward { get; private set; }
        /// <summary>Türme, die durch die Werkstätten auf dem Hof freigeschaltet sind.</summary>
        public readonly List<TowerType> AvailableTowers = new List<TowerType>();

        public Tower SelectedTower { get; private set; }
        public TowerType PlacingType { get; private set; }

        /// <summary>Spielgeschwindigkeit (1 oder 2). Wird für die Sitzung gemerkt.</summary>
        public static float GameSpeed { get; private set; } = 1f;
        /// <summary>Pause-Menü ist offen.</summary>
        public bool Paused { get; private set; }
        /// <summary>Tutorial: gerade geschenkter Turm, der noch vorgestellt wird.</summary>
        public TowerType GiftedTower { get; private set; }

        public readonly List<Enemy> Enemies = new List<Enemy>();
        public readonly List<Tower> Towers = new List<Tower>();
        public readonly List<Cow> Cows = new List<Cow>();
        public readonly List<FloatingText> FloatingTexts = new List<FloatingText>();

        MapView map;
        Vector3[] pathWorld;
        /// <summary>Weg der Aliens in Weltkoordinaten (Eingang → Weide).</summary>
        public Vector3[] PathWorld => pathWorld;
        /// <summary>Restzeit der Fähigkeit „Bauernaufstand“ (Mistgabel-Bauern doppelt so schnell).</summary>
        public float FarmerFrenzyTimer { get; private set; }
        int pendingGroups;
        Dictionary<TowerType, int> towerMaxLevels;

        public static Vector3 CellCenter(Vector2Int c) => new Vector3(c.x + 0.5f, 0f, c.y + 0.5f);

        public void Init(LevelDefinition def, Dictionary<TowerType, int> towerLoadout)
        {
            Def = def;
            Money = def.StartMoney;
            towerMaxLevels = towerLoadout;
            foreach (var type in TowerType.All)
            {
                if (MaxTowerLevel(type) > 0) AvailableTowers.Add(type);
            }

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
            ApplyTimeScale();
        }

        /// <summary>Höchste Turm-Stufe in diesem Level (vom Hof bestimmt), 0 = nicht freigeschaltet.</summary>
        public int MaxTowerLevel(TowerType type) => towerMaxLevels.TryGetValue(type, out int max) ? max : 0;

        public Vector3 RandomPastureSpot()
        {
            var p = Def.Pasture;
            return new Vector3(Random.Range(p.xMin + 0.35f, p.xMax - 0.35f), 0f, Random.Range(p.yMin + 0.35f, p.yMax - 0.35f));
        }

        void Update()
        {
            // Jedes Bild setzen: Beim Neustart setzt das alte Level beim Zerstören die Zeit sonst auf 1x zurück.
            ApplyTimeScale();

            if (FarmerFrenzyTimer > 0f && State == LevelState.WaveRunning) FarmerFrenzyTimer -= Time.deltaTime;

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
            if (State != LevelState.Intro) return;
            State = LevelState.Building;
            GrantTowerGifts();
        }

        // ---------------------------------------------------------------- Geschwindigkeit und Pause

        public void ToggleSpeed()
        {
            GameSpeed = GameSpeed > 1f ? 1f : 2f;
            ApplyTimeScale();
        }

        public void SetPaused(bool paused)
        {
            if (paused && State != LevelState.Building && State != LevelState.WaveRunning) return;
            Paused = paused;
            if (paused)
            {
                CancelPlacing();
                Select(null);
            }
            ApplyTimeScale();
        }

        void ApplyTimeScale()
        {
            Time.timeScale = Paused ? 0f : GameSpeed;
        }

        void OnDestroy()
        {
            // Der Hof läuft immer in normaler Geschwindigkeit.
            Time.timeScale = 1f;
        }

        // ---------------------------------------------------------------- Tutorial

        /// <summary>Schenkt Türme, die vor der nächsten Welle fällig sind (Tutorial in Level 1).</summary>
        void GrantTowerGifts()
        {
            foreach (var gift in Def.TowerGifts)
            {
                if (gift.BeforeWave != WavesStarted + 1 || MaxTowerLevel(gift.Tower) > 0) continue;
                towerMaxLevels[gift.Tower] = gift.MaxLevel;
                AvailableTowers.Clear();
                foreach (var type in TowerType.All)
                {
                    if (MaxTowerLevel(type) > 0) AvailableTowers.Add(type);
                }
                GiftedTower = gift.Tower;
            }
        }

        public void DismissGift()
        {
            GiftedTower = null;
        }

        public bool HasMoreWaves => WavesStarted < Def.Waves.Length;

        public void StartNextWave()
        {
            if (State != LevelState.Building || !HasMoreWaves) return;

            var wave = Def.Waves[WavesStarted];
            float hpMultiplier = Def.HpMultiplier * (1f + Def.HpGrowthPerWave * WavesStarted);
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
                GrantTowerGifts();
                return;
            }

            State = LevelState.Won;
            Stars = CowsLost == 0 ? 3 : CowsLost <= 2 ? 2 : 1;
            Reward = GameFlow.CompleteLevel(Def, Stars);
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
            GameFlow.StartLevel(Def);
        }

        public void BackToFarm()
        {
            GameFlow.ShowFarm();
        }

        void OnApplicationPause(bool paused)
        {
            if (paused) GameFlow.Farm.Save();
        }

        void OnApplicationQuit()
        {
            GameFlow.Farm.Save();
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
            if (Money < type.Cost || MaxTowerLevel(type) <= 0) return;
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
            tower.Init(this, type, cell, MaxTowerLevel(type));
            Towers.Add(tower);
            return true;
        }

        public void Select(Tower tower)
        {
            if (SelectedTower != null) SelectedTower.SetSelected(false);
            SelectedTower = tower;
            if (SelectedTower != null) SelectedTower.SetSelected(true);
        }

        public void UpgradeSelected(int path)
        {
            var tower = SelectedTower;
            if (tower == null || tower.WhyCannotUpgrade(path) != null) return;
            int cost = tower.NextTier(path).Cost;
            if (Money < cost) return;
            Money -= cost;
            tower.Upgrade(path);
        }

        // ---------------------------------------------------------------- Fähigkeiten (Stufe 5)

        public void UseAbility(Tower tower)
        {
            if (State != LevelState.WaveRunning || tower == null || !tower.AbilityReady) return;
            var ability = tower.Stats.Ability;
            tower.TriggerAbility();
            AddFloatingText(tower.transform.position + Vector3.up * 1.2f, TowerType.AbilityName(ability) + "!", new Color(1f, 0.9f, 0.3f));

            switch (ability)
            {
                case TowerAbility.Gabelsturm:
                    for (int i = Enemies.Count - 1; i >= 0; i--)
                    {
                        if (i >= Enemies.Count) continue;
                        var e = Enemies[i];
                        RingEffect.Spawn(transform, e.transform.position, 0.6f, new Color(0.9f, 0.9f, 1f, 1f), 0.4f);
                        e.TakeDamage(60f, true);
                    }
                    break;
                case TowerAbility.Bauernaufstand:
                    FarmerFrenzyTimer = 10f;
                    break;
                case TowerAbility.Heulawine:
                    StartCoroutine(HayAvalanche());
                    break;
                case TowerAbility.Eiszeit:
                    foreach (var e in Enemies) e.Freeze(4f, 1.5f, true);
                    break;
            }
        }

        IEnumerator HayAvalanche()
        {
            for (int i = 0; i < 3; i++)
            {
                RollingBale.Spawn(this, pathWorld[pathWorld.Length - 1], pathWorld.Length - 2, 999f, 80f, 0.55f);
                yield return new WaitForSeconds(0.6f);
            }
        }

        public void CycleTargetMode()
        {
            if (SelectedTower != null) SelectedTower.CycleTargetMode();
        }

        /// <summary>Nächster Punkt auf dem Weg und der Index des Weg-Punkts davor (Richtung Eingang).</summary>
        public Vector3 ClosestPointOnPath(Vector3 position, out int towardsEntrance)
        {
            towardsEntrance = 0;
            Vector3 best = pathWorld[0];
            float bestDist = float.MaxValue;
            for (int i = 0; i < pathWorld.Length - 1; i++)
            {
                Vector3 a = pathWorld[i], b = pathWorld[i + 1];
                Vector3 ab = b - a;
                float t = ab.sqrMagnitude > 0f ? Mathf.Clamp01(Vector3.Dot(position - a, ab) / ab.sqrMagnitude) : 0f;
                Vector3 p = a + ab * t;
                float d = (p - position).sqrMagnitude;
                if (d < bestDist)
                {
                    bestDist = d;
                    best = p;
                    towardsEntrance = i;
                }
            }
            return best;
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
