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
        public int TotalCows => Rules != null && Rules.CowOverride > 0 ? Rules.CowOverride : Def.CowCount + Bonuses.ExtraCows;

        /// <summary>Normales Level, tägliche Herausforderung oder Endlos.</summary>
        public LevelMode Mode { get; private set; } = LevelMode.Normal;
        /// <summary>Sonderregeln der Herausforderung (sonst null).</summary>
        public ChallengeRules Rules { get; private set; }
        public float EnemySpeedFactor => DifficultyRules.SpeedFactor(Difficulty) * (Rules != null ? Rules.SpeedFactor : 1f);
        public bool AllCamo => Rules != null && Rules.AllCamo;
        public bool HeroAllowed => Rules == null || !Rules.NoHero;
        public bool IsEndless => Mode == LevelMode.Endless;
        /// <summary>Endlos: geschaffte Wellen und bisheriger Rekord auf dieser Karte.</summary>
        public int EndlessBest { get; private set; }
        public int CowsRemaining => TotalCows - CowsLost;
        /// <summary>Dauerhafte Boni aus dem Talentbaum „Bauernwissen“.</summary>
        public LevelBonuses Bonuses { get; private set; } = new LevelBonuses();
        public int Stars { get; private set; }
        public int BestStars => GameFlow.Farm.Stars(Def.Id, Difficulty);
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

        public Difficulty Difficulty { get; private set; } = Difficulty.Mittel;

        public void Init(LevelDefinition def, Dictionary<TowerType, int> towerLoadout, LevelBonuses bonuses = null, HeroType hero = null,
            Difficulty difficulty = Difficulty.Mittel)
        {
            Def = def;
            Difficulty = difficulty;
            Mode = GameFlow.CurrentMode;
            Rules = Mode == LevelMode.Challenge ? GameFlow.CurrentRules : null;
            if (Mode == LevelMode.Endless) EndlessBest = Challenges.EndlessBest(GameFlow.Farm, def.Id);
            HeroChoice = hero ?? HeroType.Heinz;
            Bonuses = bonuses ?? new LevelBonuses();
            Money = Mathf.RoundToInt(def.StartMoney * DifficultyRules.StartMoneyFactor(difficulty) * (Rules != null ? Rules.MoneyFactor : 1f)) + Bonuses.StartMoney;
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
        public int MaxTowerLevel(TowerType type)
        {
            int max = towerMaxLevels.TryGetValue(type, out int m) ? m : 0;
            if (Rules != null)
            {
                if (!Rules.Allows(type)) return 0;
                max = Mathf.Min(max, Rules.MaxTier);
            }
            return max;
        }

        public Vector3 RandomPastureSpot()
        {
            var p = Def.Pasture;
            return new Vector3(Random.Range(p.xMin + 0.35f, p.xMax - 0.35f), 0f, Random.Range(p.yMin + 0.35f, p.yMax - 0.35f));
        }

        /// <summary>Stärke des Bildschirmwackelns (Bosse).</summary>
        public float ShakeAmount { get; private set; }

        public void Shake(float amount) => ShakeAmount = Mathf.Max(ShakeAmount, amount);

        void Update()
        {
            // Jedes Bild setzen: Beim Neustart setzt das alte Level beim Zerstören die Zeit sonst auf 1x zurück.
            ApplyTimeScale();
            if (ShakeAmount > 0f) ShakeAmount = Mathf.Max(0f, ShakeAmount - Time.unscaledDeltaTime * 1.5f);

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

        public bool HasMoreWaves => IsEndless || WavesStarted < Def.Waves.Length;

        /// <summary>Welle Nummer <paramref name="index"/> (im Endlos-Modus auch über das Level-Ende hinaus).</summary>
        WaveDefinition WaveAt(int index) => IsEndless ? Challenges.EndlessWave(Def, index) : Def.Waves[index];

        /// <summary>Anzeige „Welle 3/12“ bzw. im Endlos-Modus nur „Welle 3“.</summary>
        public string WaveLabel => IsEndless ? $"Welle {WavesStarted}" : $"Welle {WavesStarted}/{Def.Waves.Length}";

        public void StartNextWave()
        {
            if (State != LevelState.Building || !HasMoreWaves || Paused || NewEnemyIntro != null) return;

            var wave = WaveAt(WavesStarted);
            float hpMultiplier = Def.HpMultiplier * (1f + Def.HpGrowthPerWave * WavesStarted) * DifficultyRules.HpFactor(Difficulty)
                                 * (Rules != null ? Rules.HpFactor : 1f);
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
            if (type.IsBoss)
            {
                // Auftritt des Bosses: Wackeln, Knall, Warnung
                Shake(0.7f);
                GameAudio.Play(SoundId.Boom);
                RingEffect.Spawn(transform, pathWorld[0], 2.5f, new Color(1f, 0.3f, 0.2f, 1f), 0.8f);
                AddFloatingText(CellCenter(new Vector2Int(Def.Width / 2, Def.Height / 2)) + Vector3.up, $"BOSS: {type.Name}!", new Color(1f, 0.35f, 0.25f));
            }
            if (type.Description.Length > 0 && NewEnemyIntro == null && GameFlow.Farm.MarkEnemySeen(kind))
            {
                NewEnemyIntro = type;
                GameFlow.Farm.Save();
                ApplyTimeScale();
            }
        }

        /// <summary>Neue Aliens an der Stelle eines anderen (Teiler zerfällt, Ernte-Roboter wirft Flitzer ab).</summary>
        public void SpawnFrom(Enemy source, EnemyKind kind, int count)
        {
            var type = EnemyType.All[kind];
            for (int i = 0; i < count; i++)
            {
                var enemy = new GameObject("Alien").AddComponent<Enemy>();
                enemy.transform.SetParent(transform, false);
                float a = (i + 0.5f) / count * Mathf.PI * 2f;
                var offset = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * 0.25f;
                enemy.InitFrom(this, type, source.HpMultiplier, pathWorld, source, offset);
                Enemies.Add(enemy);
            }
            Effects.Burst(transform, source.AimPoint, source.Type.Color, 10, 2.5f);
        }

        void WaveCleared()
        {
            FarmerFrenzyTimer = 0f;
            var wave = WaveAt(WavesStarted - 1);
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
            GameFlow.Farm.Count(Counter.LevelsWon);
            if (CowsLost == 0) GameFlow.Farm.Count(Counter.PerfectWins);
            if (Difficulty == Difficulty.Schwer) GameFlow.Farm.Count(Counter.HardWins);
            Reward = Mode == LevelMode.Challenge ? GameFlow.CompleteChallenge(Def, Stars)
                : Mode == LevelMode.Raid ? GameFlow.CompleteRaid(true, Stars)
                : GameFlow.CompleteLevel(Def, Stars, Difficulty);
            CancelPlacing();
            Select(null);
        }

        void Lose()
        {
            State = LevelState.Lost;
            GameAudio.Play(SoundId.Lose);
            Stars = 0;
            // Endlos: das Ende ist das Ziel – Belohnung für die geschafften Wellen
            if (IsEndless) Reward = GameFlow.CompleteEndless(Def, Mathf.Max(0, WavesStarted - 1));
            if (Mode == LevelMode.Raid) Reward = GameFlow.CompleteRaid(false, 0);
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
            GameFlow.Farm.Save();
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
            if (Hero != null && Hero.Type.Kind == HeroKind.Berta && Hero.InRange(enemy) && (!enemy.IsCamo || Hero.Stats.SeesCamo)) return null;
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
            if (State == LevelState.Lost)
            {
                Enemies.Remove(enemy);
                return;
            }
            Enemies.Remove(enemy);
            int reward = Mathf.RoundToInt(enemy.Type.Reward * (1f + enemy.BonusReward) * Bonuses.RewardFactor);
            Money += reward;
            AddFloatingText(enemy.transform.position + Vector3.up * 0.5f, "+" + reward, new Color(1f, 0.9f, 0.3f));
            if (Hero != null) Hero.AddXp(Mathf.RoundToInt((enemy.IsBoss ? 60 : 3) * Bonuses.HeroXpFactor));
            GameFlow.Farm.Count(Counter.Kills);
            if (enemy.IsBoss) GameFlow.Farm.Count(Counter.BossKills);
            Effects.Burst(transform, enemy.AimPoint, enemy.Type.Color, enemy.IsBoss ? 40 : 8, enemy.IsBoss ? 5f : 3f);
            if (enemy.IsBoss)
            {
                Shake(1f);
                GameAudio.Play(SoundId.Boom);
                RingEffect.Spawn(transform, enemy.transform.position, 1.5f, new Color(1f, 0.85f, 0.3f, 1f), 0.6f);
                RingEffect.Spawn(transform, enemy.transform.position, 3f, new Color(1f, 0.5f, 0.2f, 1f), 0.9f);
                Effects.Burst(transform, enemy.AimPoint + Vector3.up * 0.5f, new Color(1f, 0.85f, 0.3f), 20, 4f, 0.12f);
            }
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
            if (Hero != null || !HeroAllowed) return;
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
            if (State == LevelState.WaveRunning && !Paused && NewEnemyIntro == null && Hero != null) Hero.UseAbility1();
        }

        public void UseHeroAbility2()
        {
            if (State == LevelState.WaveRunning && !Paused && NewEnemyIntro == null && Hero != null) Hero.UseAbility2();
        }

        /// <summary>Preis eines Turms in diesem Level (mit Talent-Rabatt).</summary>
        public int TowerCost(TowerType type) => Mathf.RoundToInt(type.Cost * Bonuses.TowerCostFactor);

        public bool IsBuildable(Vector2Int cell)
        {
            return map.InBounds(cell)
                && !map.IsPath(cell)
                && !Def.Pasture.Contains(cell)
                && !Def.IsCorn(cell)
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
            GameFlow.Farm.Count(Counter.TowersBuilt);
            foreach (var t in Towers) t.RefreshUnits();
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
            tower.Upgrade(path, cost);
            GameFlow.Farm.Count(Counter.Upgrades);
        }

        // ---------------------------------------------------------------- Fähigkeiten (Stufe 5)

        public void UseAbility(Tower tower)
        {
            if (State != LevelState.WaveRunning || Paused || NewEnemyIntro != null || tower == null || !tower.AbilityReady) return;
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
                    foreach (var e in Enemies) e.Freeze(4f, tower.Stats.FrozenVulnerability, true);
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
        /// <summary>Punkte auf dem Weg (alle 0,25 Felder) im Umkreis, in Laufrichtung sortiert (Güllefass, Mähdrescher, Hunde).</summary>
        public List<Vector3> PathPointsWithin(Vector3 center, float radius)
        {
            var points = new List<Vector3>();
            center.y = 0f;
            for (int i = 0; i < pathWorld.Length - 1; i++)
            {
                Vector3 a = pathWorld[i], b = pathWorld[i + 1];
                float len = Vector3.Distance(a, b);
                int steps = Mathf.Max(1, Mathf.CeilToInt(len / 0.25f));
                for (int k = 0; k < steps; k++)
                {
                    Vector3 p = Vector3.Lerp(a, b, k / (float)steps);
                    var flat = new Vector3(p.x, 0f, p.z);
                    // nur auf dem Spielfeld (nicht im Eingang außerhalb der Karte)
                    if (flat.x < 0f || flat.z < 0f || flat.x > Def.Width || flat.z > Def.Height) continue;
                    if ((flat - center).magnitude <= radius) points.Add(p);
                }
            }
            return points;
        }

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
            foreach (var t in Towers)
            {
                if (t != tower) t.RefreshUnits();
            }
            Destroy(tower.gameObject);
        }

        public void AddFloatingText(Vector3 position, string text, Color color)
        {
            FloatingTexts.Add(new FloatingText { Position = position, Text = text, Color = color });
        }
    }
}
