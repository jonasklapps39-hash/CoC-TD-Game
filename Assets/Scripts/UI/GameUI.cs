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

        public void Init(LevelManager level)
        {
            this.level = level;
            cam = Camera.main;
        }

        void Update()
        {
            if (cam == null) cam = Camera.main;
            if (cam != null) UiKit.FitCamera(cam, level.Def.Width, level.Def.Height);
        }

        void OnGUI()
        {
            if (level == null || cam == null) return;

            Vector2 rawMouse = UiKit.BeginGui();
            blockers.Clear();

            DrawWorldOverlays();
            DrawTopBar();
            DrawSideBar();

            switch (level.State)
            {
                case LevelState.Intro: DrawIntro(); break;
                case LevelState.Won: DrawEndScreen(true); break;
                case LevelState.Lost: DrawEndScreen(false); break;
            }

            if (UiKit.EscapePressed())
            {
                level.CancelPlacing();
                level.Select(null);
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

        void DrawTowerInfo(Tower tower)
        {
            GUILayout.Label($"{tower.Type.Name} (Stufe {tower.Level})", UiKit.Label);

            string stats = $"Schaden: {tower.Damage:0.#}\nReichweite: {tower.Range:0.#}\nAngriffe pro Sek.: {tower.FireRate:0.##}";
            if (tower.Splash > 0f) stats += $"\nFlächenradius: {tower.Splash:0.#}";
            if (tower.Slow < 1f) stats += $"\nVerlangsamung: {(1f - tower.Slow) * 100f:0} %";
            GUILayout.Label(stats, UiKit.Small);

            int? upgrade = tower.NextUpgradeCost;
            if (upgrade.HasValue)
            {
                GUI.enabled = level.Money >= upgrade.Value;
                if (GUILayout.Button($"Verbessern ({upgrade.Value})", UiKit.Button, GUILayout.Height(44f))) level.UpgradeSelected();
                GUI.enabled = true;
            }
            else if (tower.LimitedByWorkshop)
            {
                GUILayout.Label($"Für Stufe {tower.Level + 1} die Werkstatt auf dem Hof verbessern.", UiKit.Small);
            }
            else
            {
                GUILayout.Label("Maximale Stufe erreicht", UiKit.Small);
            }

            if (GUILayout.Button($"Verkaufen (+{tower.SellValue})", UiKit.Button, GUILayout.Height(40f))) level.SellSelected();
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
