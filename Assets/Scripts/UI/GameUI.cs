using System.Collections.Generic;
using UnityEngine;

namespace MuhDefense
{
    /// <summary>
    /// Bedienoberfläche eines Levels (Unity IMGUI, ohne zusätzliche Pakete).
    /// Im fertigen Spiel wird das durch eine richtige UI (uGUI oder UI Toolkit) ersetzt.
    /// </summary>
    public class GameUI : MonoBehaviour
    {
        LevelManager level;
        Camera cam;
        readonly List<Rect> blockers = new List<Rect>();
        bool confirmLeave;

        public void Init(LevelManager level)
        {
            this.level = level;
            cam = Camera.main;
        }

        void Update()
        {
            if (cam == null) cam = Camera.main;
            if (cam != null) UiKit.FitCamera(cam, level.Def.Width, level.Def.Height, 62f);
        }

        void OnGUI()
        {
            if (level == null || cam == null) return;

            Vector2 rawMouse = UiKit.BeginGui();
            blockers.Clear();

            DrawWorldOverlays();
            DrawTopBar();
            DrawSideBar();
            if (level.SelectedTower != null && !level.Paused) DrawUpgradePanel(level.SelectedTower);

            switch (level.State)
            {
                case LevelState.Intro: DrawIntro(); break;
                case LevelState.Won: DrawEndScreen(true); break;
                case LevelState.Lost: DrawEndScreen(false); break;
            }
            if (level.Paused) DrawPauseMenu();
            else if (level.GiftedTower != null) DrawGift();

            if (UiKit.EscapePressed())
            {
                if (level.Paused) ResumeGame();
                else if (level.PlacingType != null || level.SelectedTower != null)
                {
                    level.CancelPlacing();
                    level.Select(null);
                }
                else level.SetPaused(true);
            }
            else if (UiKit.TryGetWorldClick(rawMouse, blockers, cam, out var world))
            {
                level.HandleClick(world);
            }
        }

        // ---------------------------------------------------------------- Bereiche

        void DrawTopBar()
        {
            float vw = UiKit.VirtualWidth;
            var r = new Rect(0f, 0f, vw, UiKit.TopBarHeight);
            blockers.Add(r);
            GUI.Box(r, GUIContent.none, UiKit.Panel);

            int totalWaves = level.Def.Waves.Length;
            string text = $"Münzen: {level.Money}      Kühe: {level.CowsRemaining}/{level.Def.CowCount}      Welle: {level.WavesStarted}/{totalWaves}";
            GUI.Label(new Rect(16f, 0f, vw - UiKit.SideBarWidth - 32f, UiKit.TopBarHeight), text, UiKit.Title);
            GUI.Label(new Rect(vw - UiKit.SideBarWidth, 0f, UiKit.SideBarWidth - 16f, UiKit.TopBarHeight), level.Def.Name, UiKit.Small);
        }

        void DrawSideBar()
        {
            float vw = UiKit.VirtualWidth;
            var r = new Rect(vw - UiKit.SideBarWidth, UiKit.TopBarHeight, UiKit.SideBarWidth, UiKit.RefHeight - UiKit.TopBarHeight);
            blockers.Add(r);
            GUI.Box(r, GUIContent.none, UiKit.Panel);

            GUILayout.BeginArea(new Rect(r.x + 12f, r.y + 10f, r.width - 24f, r.height - 20f));
            bool playing = level.State == LevelState.Building || level.State == LevelState.WaveRunning;

            GUI.enabled = playing;
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Menü", UiKit.Button, GUILayout.Height(40f))) level.SetPaused(true);
            bool fast = LevelManager.GameSpeed > 1f;
            if (GUILayout.Button(fast ? "Tempo: 2x" : "Tempo: 1x", fast ? UiKit.ActiveButton : UiKit.Button, GUILayout.Height(40f))) level.ToggleSpeed();
            GUILayout.EndHorizontal();
            GUI.enabled = true;

            GUILayout.Label("Türme", UiKit.Title);
            foreach (var type in level.AvailableTowers)
            {
                bool active = level.PlacingType == type;
                GUI.enabled = playing && (active || level.Money >= type.Cost);
                if (GUILayout.Button($"{type.Name}\n{type.Cost} Münzen", active ? UiKit.ActiveButton : UiKit.Button, GUILayout.Height(56f)))
                {
                    if (active) level.CancelPlacing();
                    else level.BeginPlacing(type);
                }
            }
            GUI.enabled = true;
            if (level.AvailableTowers.Count < TowerType.All.Length)
            {
                GUILayout.Label("Weitere Türme schaltest du mit Werkstätten auf dem Hof frei.", UiKit.Small);
            }
            GUILayout.Space(10f);

            if (level.PlacingType != null)
            {
                GUILayout.Label(level.PlacingType.Description, UiKit.Small);
                GUILayout.Label("Tippe auf ein hell markiertes Feld.", UiKit.Small);
                if (GUILayout.Button("Abbrechen", UiKit.Button, GUILayout.Height(40f))) level.CancelPlacing();
            }
            else if (level.SelectedTower != null)
            {
                DrawTowerInfo(level.SelectedTower);
            }
            else if (playing)
            {
                GUILayout.Label("Wähle einen Turm und tippe auf ein freies Feld. Tippe auf einen Turm, um ihn zu verbessern.", UiKit.Small);
            }

            GUILayout.FlexibleSpace();
            DrawAbilities();

            if (level.State == LevelState.Building && level.HasMoreWaves)
            {
                bool lastWave = level.WavesStarted == level.Def.Waves.Length - 1;
                string label = lastWave ? "Letzte Welle starten!" : $"Welle {level.WavesStarted + 1} starten";
                if (GUILayout.Button(label, UiKit.BigButton, GUILayout.Height(64f))) level.StartNextWave();
            }
            else if (level.State == LevelState.WaveRunning)
            {
                GUILayout.Label($"Welle {level.WavesStarted} läuft …", UiKit.Center, GUILayout.Height(64f));
            }

            GUILayout.EndArea();
        }

        /// <summary>Kurzinfo in der Seitenleiste; die Upgrades selbst stehen im Fenster unten.</summary>
        void DrawTowerInfo(Tower tower)
        {
            var st = tower.Stats;
            GUILayout.Label(tower.Type.Name, UiKit.Label);
            string stats = $"Schaden: {st.Damage:0.#}   Tempo: {st.FireRate:0.##}/s\n" +
                           (st.GlobalRange ? "Reichweite: unbegrenzt" : $"Reichweite: {st.Range:0.#}");
            if (st.Splash > 0f) stats += $"   Fläche: {st.Splash:0.#}";
            if (st.Slow < 1f) stats += $"\nVerlangsamung: {(1f - st.Slow) * 100f:0} %";
            GUILayout.Label(stats, UiKit.Small);
            GUILayout.Label("Upgrades: unten im Bild", UiKit.Small);
        }

        /// <summary>Knöpfe für Fähigkeiten der Stufe-5-Türme.</summary>
        void DrawAbilities()
        {
            bool any = false;
            int shown = 0;
            foreach (var tower in level.Towers)
            {
                if (!tower.HasAbility || shown >= 4) continue;
                shown++;
                if (!any)
                {
                    GUILayout.Label("Fähigkeiten", UiKit.Title);
                    any = true;
                }
                string name = TowerType.AbilityName(tower.Stats.Ability);
                bool ready = tower.AbilityReady && level.State == LevelState.WaveRunning;
                string label = tower.AbilityReady ? name : $"{name} ({Mathf.CeilToInt(tower.AbilityCooldownLeft)} s)";
                GUI.enabled = ready;
                if (GUILayout.Button(label, tower.AbilityReady ? UiKit.ActiveButton : UiKit.Button, GUILayout.Height(40f))) level.UseAbility(tower);
                GUI.enabled = true;
            }
        }

        /// <summary>Upgrade-Fenster wie in Bloons TD6: drei Pfade nebeneinander, links Zielmodus und Verkaufen.</summary>
        void DrawUpgradePanel(Tower tower)
        {
            float width = UiKit.VirtualWidth - UiKit.SideBarWidth - 16f;
            const float height = 200f;
            var r = new Rect(8f, UiKit.RefHeight - height - 8f, width, height);
            blockers.Add(r);
            GUI.Box(r, GUIContent.none, UiKit.Panel);

            const float leftWidth = 190f;
            GUILayout.BeginArea(new Rect(r.x + 10f, r.y + 8f, leftWidth, height - 16f));
            GUILayout.Label(tower.Type.Name, UiKit.Label);
            if (GUILayout.Button($"Ziel: {TowerType.TargetModeName(tower.TargetMode)}", UiKit.Button, GUILayout.Height(40f))) level.CycleTargetMode();
            if (GUILayout.Button($"Verkaufen (+{tower.SellValue})", UiKit.Button, GUILayout.Height(40f))) level.SellSelected();
            if (GUILayout.Button("Schließen", UiKit.Button, GUILayout.Height(34f))) level.Select(null);
            GUILayout.EndArea();

            float colWidth = (width - leftWidth - 30f) / TowerType.PathCount;
            for (int p = 0; p < TowerType.PathCount; p++)
            {
                var path = tower.Type.Paths[p];
                var col = new Rect(r.x + leftWidth + 20f + p * colWidth, r.y + 8f, colWidth - 8f, height - 16f);
                UiKit.DrawRect(col, new Color(0f, 0f, 0f, 0.2f));
                UiKit.DrawRect(new Rect(col.x, col.y, col.width, 4f), path.Color);

                GUILayout.BeginArea(new Rect(col.x + 8f, col.y + 8f, col.width - 16f, col.height - 12f));
                GUILayout.BeginHorizontal();
                GUILayout.Label(path.Name, UiKit.Label);
                GUILayout.FlexibleSpace();
                Rect pips = GUILayoutUtility.GetRect(5 * 14f, 20f, GUILayout.Width(5 * 14f));
                for (int t = 0; t < TowerType.MaxTier; t++)
                {
                    var pip = new Rect(pips.x + t * 14f, pips.y + 5f, 10f, 10f);
                    UiKit.DrawRect(pip, t < tower.Tiers[p] ? path.Color : new Color(1f, 1f, 1f, 0.15f));
                }
                GUILayout.EndHorizontal();

                var next = tower.NextTier(p);
                if (next == null)
                {
                    GUILayout.Label("Alle Stufen gekauft", UiKit.Small);
                }
                else
                {
                    GUILayout.Label($"Stufe {tower.Tiers[p] + 1}: {next.Name}", UiKit.Small);
                    GUILayout.Label(next.Description, UiKit.Small, GUILayout.Height(54f));
                    string reason = tower.WhyCannotUpgrade(p);
                    if (reason != null)
                    {
                        GUILayout.Label(reason, UiKit.Small);
                    }
                    else
                    {
                        GUI.enabled = level.Money >= next.Cost;
                        if (GUILayout.Button($"Kaufen: {next.Cost}", UiKit.ActiveButton, GUILayout.Height(40f))) level.UpgradeSelected(p);
                        GUI.enabled = true;
                    }
                }
                GUILayout.EndArea();
            }
        }

        void DrawWorldOverlays()
        {
            foreach (var enemy in level.Enemies)
            {
                if (!UiKit.WorldToGui(cam, enemy.HealthBarPoint, out var p)) continue;
                float w = 34f * Mathf.Clamp(enemy.Type.Scale, 1f, 1.8f);
                float frac = Mathf.Clamp01(enemy.Hp / enemy.MaxHp);
                UiKit.DrawBar(new Rect(p.x - w / 2f, p.y, w, 6f), frac, Color.Lerp(new Color(0.95f, 0.2f, 0.15f), new Color(0.3f, 0.9f, 0.25f), frac));
            }

            foreach (var ft in level.FloatingTexts)
            {
                float k = ft.Age / LevelManager.FloatingTextLifetime;
                if (!UiKit.WorldToGui(cam, ft.Position, out var p)) continue;
                p.y -= k * 40f;
                var c = ft.Color;
                c.a = 1f - k;
                UiKit.ShadowLabel(p, ft.Text, c);
            }
        }

        void ResumeGame()
        {
            confirmLeave = false;
            level.SetPaused(false);
        }

        void DrawPauseMenu()
        {
            Rect box = UiKit.DrawOverlay(blockers, 460f, confirmLeave ? 300f : 380f);
            GUILayout.BeginArea(new Rect(box.x + 24f, box.y + 20f, box.width - 48f, box.height - 40f));

            if (confirmLeave)
            {
                GUILayout.Label("Level verlassen?", UiKit.Big);
                GUILayout.Label("Dein Fortschritt in diesem Level geht verloren und du bekommst keine Belohnung.", UiKit.Center);
                GUILayout.FlexibleSpace();
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Abbrechen", UiKit.Button, GUILayout.Height(56f))) confirmLeave = false;
                if (GUILayout.Button("Verlassen", UiKit.BigButton, GUILayout.Height(56f))) level.BackToFarm();
                GUILayout.EndHorizontal();
            }
            else
            {
                GUILayout.Label("Pause", UiKit.Big);
                GUILayout.Space(10f);
                if (GUILayout.Button("Weiterspielen", UiKit.BigButton, GUILayout.Height(60f))) ResumeGame();
                GUILayout.Space(6f);
                if (GUILayout.Button("Neu starten", UiKit.Button, GUILayout.Height(52f))) level.Restart();
                GUILayout.Space(6f);
                if (GUILayout.Button("Level verlassen", UiKit.Button, GUILayout.Height(52f))) confirmLeave = true;
            }
            GUILayout.EndArea();
        }

        void DrawGift()
        {
            var tower = level.GiftedTower;
            Rect box = UiKit.DrawOverlay(blockers, 520f, 320f);
            GUILayout.BeginArea(new Rect(box.x + 24f, box.y + 20f, box.width - 48f, box.height - 40f));
            GUILayout.Label("Neuer Turm!", UiKit.Big);
            GUILayout.Label(tower.Name, UiKit.Center);
            GUILayout.Space(6f);
            GUILayout.Label(tower.Description + $"\n\nKosten: {tower.Cost} Münzen. Du findest ihn rechts in der Turm-Liste.", UiKit.Label);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Super!", UiKit.BigButton, GUILayout.Height(56f))) level.DismissGift();
            GUILayout.EndArea();
        }

        void DrawIntro()
        {
            Rect box = UiKit.DrawOverlay(blockers, 640f, 440f);
            GUILayout.BeginArea(new Rect(box.x + 24f, box.y + 20f, box.width - 48f, box.height - 40f));
            GUILayout.Label(level.Def.Name, UiKit.Big);

            string story = level.Def.Id == Levels.All[0].Id
                ? "Ein Alien-Schiff vom Planeten Zorb ist auf Bauer Heinz' Weide bruchgelandet. " +
                  "Die Aliens haben Bertas Milch probiert – und jetzt wollen sie ALLE Kühe!\n\n" +
                  "Baue Türme neben den Weg, damit die Aliens die Weide nicht erreichen. " +
                  "Schnappt sich ein Alien eine Kuh, besiege es, bevor es zurück zum Ufo kommt.\n\n"
                : "Die Aliens kommen wieder – diesmal stärker und auf einem neuen Weg. Beschütze die Kühe!\n\n";
            GUILayout.Label(story +
                "3 Sterne: keine Kuh verloren   •   2 Sterne: höchstens 2 verloren   •   1 Stern: Level geschafft\n" +
                $"Belohnung bei 3 Sternen: {level.Def.RewardCorn} Mais und {level.Def.RewardMilk} Milch",
                UiKit.Label);

            GUILayout.FlexibleSpace();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Zurück zum Hof", UiKit.Button, GUILayout.Height(60f), GUILayout.Width(200f))) level.BackToFarm();
            if (GUILayout.Button("Los geht's!", UiKit.BigButton, GUILayout.Height(60f))) level.BeginLevel();
            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

        void DrawEndScreen(bool won)
        {
            Rect box = UiKit.DrawOverlay(blockers, 560f, 400f);
            GUILayout.BeginArea(new Rect(box.x + 24f, box.y + 20f, box.width - 48f, box.height - 40f));
            GUILayout.Label(won ? "Gewonnen!" : "Alle Kühe weg!", UiKit.Big);

            if (won)
            {
                Rect starsRect = GUILayoutUtility.GetRect(box.width - 48f, 90f);
                UiKit.DrawStars(starsRect, level.Stars, 80f);
                GUILayout.Label($"Kühe gerettet: {level.CowsRemaining}/{level.Def.CowCount}   •   Bestes Ergebnis: {level.BestStars} Sterne", UiKit.Center);

                var reward = level.Reward;
                if (reward != null)
                {
                    string text = $"Für deinen Hof: +{reward.Corn} Mais, +{reward.Milk} Milch";
                    if (reward.CornLost > 0 || reward.MilkLost > 0) text += "\n(Lager voll – baue Silos und Tanks!)";
                    GUILayout.Label(text, UiKit.Center);
                }
            }
            else
            {
                GUILayout.Label("Die Aliens haben alle Kühe entführt. Versuch es mit einer anderen Turm-Aufstellung " +
                                "oder verbessere deine Werkstätten auf dem Hof!", UiKit.Label);
            }

            GUILayout.FlexibleSpace();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Nochmal spielen", UiKit.Button, GUILayout.Height(60f), GUILayout.Width(200f))) level.Restart();
            if (GUILayout.Button("Zurück zum Hof", UiKit.BigButton, GUILayout.Height(60f))) level.BackToFarm();
            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }
    }
}
