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
        public int TotalCows => Def.CowCount + Bonuses.ExtraCows;
        public int CowsRemaining => TotalCows - CowsLost;
        /// <summary>Dauerhafte Boni aus dem Talentbaum „Bauernwissen“.</summary>
        public LevelBonuses Bonuses { get; private set; } = new LevelBonuses();
        public int Stars { get; private set; }
        public int BestStars => GameFlow.Farm.BestStars(Def.Id);
        /// <summary>Belohnung für den Hof, sobald das Level gewonnen ist.</summary>
        public LevelReward Reward { get; private set; }
        /// <summary>Türme, die durch die Werkstätten auf dem Hof freigeschaltet sind.</summary>
        public readonly List<TowerType> AvailableTowers = new List<TowerType>();

        public Tower SelectedTower { get; private set; }
        public TowerType PlacingType { get; private set; }
        /// <summary>Der Held (einmal pro Level), null solange er nicht aufgestellt ist.</summary>
        public Hero Hero { get; private set; }
        public bool PlacingHero { get; private set; }
        public bool HeroSelected { get; private set; }
        /// <summary>Gewählter Held (in der Heldenhütte auf dem Hof).</summary>
        public HeroType HeroChoice { get; private set; } = HeroType.Heinz;

        /// <summary>Spielgeschwindigkeit (1 oder 2). Wird für die Sitzung gemerkt.</summary>
        public static float GameSpeed { get; private set; } = 1f;
        /// <summary>Pause-Menü ist offen.</summary>
        public bool Paused { get; private set; }
        /// <summary>Tutorial: gerade geschenkter Turm, der noch vorgestellt wird.</summary>
        public TowerType GiftedTower { get; private set; }
        /// <summary>Neue Alien-Art, die gerade vorgestellt wird (Spiel ist so lange angehalten).</summary>
        public EnemyType NewEnemyIntro { get; private set; }

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

        public void Init(LevelDefinition def, Dictionary<TowerType, int> towerLoadout, LevelBonuses bonuses = null, HeroType hero = null)
        {
            Def = def;
            HeroChoice = hero ?? HeroType.Heinz;
            Bonuses = bonuses ?? new LevelBonuses();
            Money = def.StartMoney + Bonuses.StartMoney;
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

            for (int i = 0; i < TotalCows; i++)
            {
                var cow = new GameObject("Kuh").AddComponent<Cow>();
                cow.transform.SetParent(transform, false);
                cow.Init(this, RandomPastureSpot());
                Cows.Add(cow);
            }

            gameObject.AddComponent<GameUI>().Init(this);
            GameAudio.PlayMusic(MusicId.Level);
            ApplyTimeScale();
        }

        // ---------------------------------------------------------------- Unterstützung durch andere Türme

        static bool Supports(Tower supporter, Tower target)
        {
            if (supporter == target) return false;
            Vector3 d = supporter.transform.position - target.transform.position;
            d.y = 0f;
            return d.magnitude <= supporter.Stats.Range;
        }

        /// <summary>Ein Wetterhahn (oder ähnliches) in Reichweite macht getarnte Aliens für diesen Turm sichtbar.</summary>
        public bool RevealsCamoFor(Tower tower)
        {
            foreach (var t in Towers)
            {
                if (t.Stats.RevealsCamo && Supports(t, tower)) return true;
            }
            return false;
        }

        /// <summary>Reichweiten-Bonus durch Unterstützungs-Türme (der stärkste zählt).</summary>
        public float RangeBonusAt(Tower tower)
        {
            float best = 0f;
            foreach (var t in Towers)
            {
                if (t.Stats.SupportRangeBonus > best && Supports(t, tower)) best = t.Stats.SupportRangeBonus;
            }
            return best;
        }

        /// <summary>Tempo-Bonus durch Unterstützungs-Türme (der stärkste zählt).</summary>
        public float RateBonusAt(Tower tower)
        {
            float best = 0f;
            foreach (var t in Towers)
            {
                if (t.Stats.SupportRateBonus > best && Supports(t, tower)) best = t.Stats.SupportRateBonus;
            }
            // Oma Gertrud: „Kaffee und Kuchen“ für Türme in ihrer Reichweite
            if (Hero != null && Hero.Stats.SupportRateBonus > best)
            {
                Vector3 d = Hero.transform.position - tower.transform.position;
                d.y = 0f;
                if (d.magnitude <= Hero.Stats.Range) best = Hero.Stats.SupportRateBonus;
            }
            return best;
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
            Time.timeScale = Paused || NewEnemyIntro != null ? 0f : GameSpeed;
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

        public void DismissEnemyIntro()
        {
            NewEnemyIntro = null;
            ApplyTimeScale();
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
            GameAudio.Play(SoundId.WaveStart);
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
            var type = EnemyType.All[kind];
            enemy.Init(this, type, hpMultiplier, pathWorld);
            Enemies.Add(enemy);
            if (type.Description.Length > 0 && NewEnemyIntro == null && GameFlow.Farm.MarkEnemySeen(kind))
            {
                NewEnemyIntro = type;
                GameFlow.Farm.Save();
                ApplyTimeScale();
            }
        }

        void WaveCleared()
        {
            var wave = Def.Waves[WavesStarted - 1];
            if (Hero != null) Hero.AddXp(Mathf.RoundToInt((40 + 10 * WavesStarted) * Bonuses.HeroXpFactor));
            foreach (var t in Towers)
            {
                if (t.Stats.IncomePerWave > 0) AddMoney(t.Stats.IncomePerWave, t.transform.position + Vector3.up * 1.2f);
            }
            int waveBonus = Mathf.RoundToInt(wave.Bonus * Bonuses.WaveBonusFactor);
            if (waveBonus > 0)
            {
                Money += waveBonus;
                AddFloatingText(CellCenter(new Vector2Int(Def.Width / 2, Def.Height / 2)), $"Welle geschafft! +{waveBonus}", new Color(1f, 0.9f, 0.3f));
            }

            if (HasMoreWaves)
            {
                State = LevelState.Building;
                GrantTowerGifts();
                return;
            }

            State = LevelState.Won;
            GameAudio.Play(SoundId.Win);
            Stars = CowsLost == 0 ? 3 : CowsLost <= 2 ? 2 : 1;
            Reward = GameFlow.CompleteLevel(Def, Stars);
            CancelPlacing();
            Select(null);
        }

        void Lose()
        {
            State = LevelState.Lost;
            GameAudio.Play(SoundId.Lose);
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
            // Berta: in ihrem Schutzkreis kann kein Alien eine Kuh schnappen
            if (Hero != null && Hero.Type.Kind == HeroKind.Berta && Hero.InRange(enemy)) return null;
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
            if (best != null)
            {
                best.Grab();
                GameAudio.Play(SoundId.Moo);
            }
            return best;
        }

        public void OnEnemyKilled(Enemy enemy)
        {
            Enemies.Remove(enemy);
            int reward = Mathf.RoundToInt(enemy.Type.Reward * (1f + enemy.BonusReward) * Bonuses.RewardFactor);
            Money += reward;
            AddFloatingText(enemy.transform.position + Vector3.up * 0.5f, "+" + reward, new Color(1f, 0.9f, 0.3f));
            if (Hero != null) Hero.AddXp(Mathf.RoundToInt((enemy.IsBoss ? 60 : 3) * Bonuses.HeroXpFactor));
            Effects.Burst(transform, enemy.AimPoint, enemy.Type.Color, enemy.IsBoss ? 20 : 8, 3f);
            GameAudio.Play(SoundId.Pop);
            GameAudio.Play(SoundId.Coin);
        }

        public void OnEnemyEscaped(Enemy enemy)
        {
            Enemies.Remove(enemy);
            if (enemy.Carried == null) return;

            var cow = enemy.Carried;
            Cows.Remove(cow);
            Destroy(cow.gameObject);
            CowsLost++;
            GameAudio.Play(SoundId.CowLost);
            AddFloatingText(enemy.transform.position + Vector3.up, "Kuh entführt!", new Color(1f, 0.35f, 0.3f));
            if (CowsRemaining <= 0) Lose();
        }

        // ---------------------------------------------------------------- Türme

        public void HandleClick(Vector3 worldPoint)
        {
            if (State != LevelState.Building && State != LevelState.WaveRunning) return;

            var cell = new Vector2Int(Mathf.FloorToInt(worldPoint.x), Mathf.FloorToInt(worldPoint.z));

            if (PlacingHero)
            {
                if (TryPlaceHero(cell)) CancelPlacing();
                return;
            }
            if (PlacingType != null)
            {
                if (TryPlace(PlacingType, cell)) CancelPlacing();
                return;
            }

            if (Hero != null && Hero.Cell == cell)
            {
                SelectHero();
                return;
            }
            Select(Towers.Find(t => t.Cell == cell));
        }

        // ---------------------------------------------------------------- Held

        public void BeginPlacingHero()
        {
            if (Hero != null) return;
            Select(null);
            PlacingType = null;
            PlacingHero = true;
            map.ShowBuildable(true, IsBuildable);
        }

        bool TryPlaceHero(Vector2Int cell)
        {
            if (!IsBuildable(cell))
            {
                AddFloatingText(CellCenter(cell), "Hier nicht!", new Color(1f, 0.4f, 0.3f));
                return false;
            }
            Hero = new GameObject("Held").AddComponent<Hero>();
            Hero.transform.SetParent(transform, false);
            Hero.Init(this, cell, HeroChoice);
            Hero.SetStartLevel(1 + Bonuses.HeroStartLevels);
            Effects.Burst(transform, CellCenter(cell) + Vector3.up * 0.3f, new Color(1f, 0.85f, 0.3f), 10, 2.5f);
            GameAudio.Play(SoundId.Build);
            return true;
        }

        public void SelectHero()
        {
            Select(null);
            HeroSelected = true;
            Hero.SetSelected(true);
        }

        public void UseHeroAbility1()
        {
            if (State == LevelState.WaveRunning && Hero != null) Hero.UseAbility1();
        }

        public void UseHeroAbility2()
        {
            if (State == LevelState.WaveRunning && Hero != null) Hero.UseAbility2();
        }

        /// <summary>Preis eines Turms in diesem Level (mit Talent-Rabatt).</summary>
        public int TowerCost(TowerType type) => Mathf.RoundToInt(type.Cost * Bonuses.TowerCostFactor);

        public bool IsBuildable(Vector2Int cell)
        {
            return map.InBounds(cell)
                && !map.IsPath(cell)
                && !Def.Pasture.Contains(cell)
                && !Towers.Exists(t => t.Cell == cell)
                && (Hero == null || Hero.Cell != cell);
        }

        public void BeginPlacing(TowerType type)
        {
            if (Money < TowerCost(type) || MaxTowerLevel(type) <= 0) return;
            Select(null);
            PlacingHero = false;
            PlacingType = type;
            map.ShowBuildable(true, IsBuildable);
        }

        public void CancelPlacing()
        {
            HidePlacementPreview();
            if (PlacingType == null && !PlacingHero) return;
            PlacingType = null;
            PlacingHero = false;
            map.ShowBuildable(false, IsBuildable);
        }

        // ---------------------------------------------------------------- Vorschau beim Ziehen (Drag & Drop)

        GameObject placementGhost;
        GameObject placementMarker;
        object ghostFor;

        /// <summary>Zeigt den Turm (oder Helden) unter dem Finger, grün = frei, rot = belegt. null blendet die Vorschau aus.</summary>
        public void SetPlacementPreview(Vector3? world)
        {
            object target = PlacingHero ? (object)"Held" : PlacingType;
            if (world == null || target == null)
            {
                HidePlacementPreview();
                return;
            }
            if (placementGhost == null || ghostFor != target) CreatePlacementGhost(target);

            var cell = new Vector2Int(Mathf.FloorToInt(world.Value.x), Mathf.FloorToInt(world.Value.z));
            placementGhost.transform.position = CellCenter(cell);
            bool ok = IsBuildable(cell);
            placementMarker.GetComponent<Renderer>().sharedMaterial =
                Visuals.Mat(ok ? new Color(0.35f, 0.95f, 0.35f) : new Color(0.95f, 0.3f, 0.25f), false);
        }

        void CreatePlacementGhost(object target)
        {
            HidePlacementPreview();
            ghostFor = target;
            placementGhost = new GameObject("Vorschau");
            placementGhost.transform.SetParent(transform, false);
            placementMarker = Visuals.Prim(PrimitiveType.Cube, placementGhost.transform, new Vector3(0f, 0.02f, 0f),
                new Vector3(0.96f, 0.04f, 0.96f), Color.green, "Markierung", outline: false);
            float range;
            if (PlacingHero)
            {
                ModelLibrary.Spawn(HeroChoice.ModelName, placementGhost.transform, HeroChoice.Kind == HeroKind.Berta ? 0.85f : 1.15f);
                range = 2.6f;
            }
            else
            {
                ModelLibrary.Spawn(Tower.ModelNameFor(PlacingType), placementGhost.transform, 0.92f, ModelLibrary.Fit.Footprint);
                range = PlacingType.Base.GlobalRange ? 1.2f : PlacingType.Base.Range;
            }
            Visuals.Circle(placementGhost.transform, range, new Color(1f, 1f, 1f, 0.85f));
        }

        void HidePlacementPreview()
        {
            if (placementGhost != null) Destroy(placementGhost);
            placementGhost = null;
            ghostFor = null;
        }

        bool TryPlace(TowerType type, Vector2Int cell)
        {
            if (!IsBuildable(cell))
            {
                AddFloatingText(CellCenter(cell), "Hier nicht!", new Color(1f, 0.4f, 0.3f));
                return false;
            }
            int cost = TowerCost(type);
            if (Money < cost) return false;

            Money -= cost;
            var tower = new GameObject("Turm").AddComponent<Tower>();
            tower.transform.SetParent(transform, false);
            tower.Init(this, type, cell, MaxTowerLevel(type), cost);
            Towers.Add(tower);
            GameAudio.Play(SoundId.Build);
            Effects.Burst(transform, CellCenter(cell) + Vector3.up * 0.2f, new Color(0.75f, 0.6f, 0.4f), 8, 2f);
            return true;
        }

        public void Select(Tower tower)
        {
            if (HeroSelected && Hero != null) Hero.SetSelected(false);
            HeroSelected = false;
            if (SelectedTower != null) SelectedTower.SetSelected(false);
            SelectedTower = tower;
            if (SelectedTower != null) SelectedTower.SetSelected(true);
        }

        /// <summary>Münzen dazu, mit schwebender Anzeige.</summary>
        public void AddMoney(int amount, Vector3 at)
        {
            Money += amount;
            AddFloatingText(at, "+" + amount, new Color(1f, 0.9f, 0.3f));
            GameAudio.Play(SoundId.Coin);
        }

        /// <summary>Preis eines Upgrades, mit Rabatt durch einen Hofladen in der Nähe.</summary>
        public int UpgradeCost(Tower tower, int path)
        {
            var tier = tower.NextTier(path);
            if (tier == null) return 0;
            float discount = 0f;
            foreach (var t in Towers)
            {
                if (t.Stats.SupportDiscount > discount && Supports(t, tower)) discount = t.Stats.SupportDiscount;
            }
            return Mathf.RoundToInt(tier.Cost * (1f - discount));
        }

        public void UpgradeSelected(int path)
        {
            var tower = SelectedTower;
            if (tower == null || tower.WhyCannotUpgrade(path) != null) return;
            int cost = UpgradeCost(tower, path);
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
                    GameAudio.Play(SoundId.Boom);
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
                    GameAudio.Play(SoundId.LevelUp);
                    break;
                case TowerAbility.Heulawine:
                    StartCoroutine(HayAvalanche());
                    GameAudio.Play(SoundId.Boom);
                    break;
                case TowerAbility.Eiszeit:
                    foreach (var e in Enemies) e.Freeze(4f, 1.5f, true);
                    GameAudio.Play(SoundId.Freeze);
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
