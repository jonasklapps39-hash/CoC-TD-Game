using System.Collections.Generic;
using UnityEngine;

namespace MuhDefense
{
    /// <summary>
    /// Bedienoberfläche des Prototyps (Unity IMGUI, ohne zusätzliche Pakete).
    /// Im fertigen Spiel wird das durch eine richtige UI (uGUI oder UI Toolkit) ersetzt.
    /// Alle Maße sind für eine Bildschirmhöhe von 720 Einheiten ausgelegt und werden skaliert.
    /// </summary>
    public class GameUI : MonoBehaviour
    {
        const float RefHeight = 720f;
        const float TopBarHeight = 56f;
        const float SideBarWidth = 240f;

        LevelManager level;
        Camera cam;
        float scale;
        float virtualWidth;
        readonly List<Rect> blockers = new List<Rect>();

        bool stylesReady;
        GUIStyle panelStyle, titleStyle, labelStyle, smallStyle, centerStyle, bigStyle, buttonStyle, activeButtonStyle, bigButtonStyle, floatStyle;
        Texture2D starTexture;

        public void Init(LevelManager level)
        {
            this.level = level;
            cam = Camera.main;
        }

        void Update()
        {
            if (cam == null) cam = Camera.main;
            if (cam == null) return;
            FitCamera();
        }

        /// <summary>Richtet die Kamera so aus, dass die ganze Karte neben der Seitenleiste sichtbar ist.</summary>
        void FitCamera()
        {
            float s = Screen.height / RefHeight;
            float sideFraction = Mathf.Clamp01(SideBarWidth * s / Screen.width);
            float topFraction = Mathf.Clamp01(TopBarHeight * s / Screen.height);
            cam.rect = new Rect(0f, 0f, 1f - sideFraction, 1f - topFraction);
            if (cam.pixelHeight > 0) cam.aspect = cam.pixelWidth / (float)cam.pixelHeight;

            var def = level.Def;
            var center = new Vector3(def.Width / 2f, 0f, def.Height / 2f);
            cam.transform.position = center - cam.transform.forward * 30f;

            float angle = cam.transform.eulerAngles.x * Mathf.Deg2Rad;
            float neededHeight = def.Height * Mathf.Sin(angle) + 1.6f;
            float neededWidth = def.Width + 1f;
            cam.orthographicSize = Mathf.Max(neededHeight / 2f, neededWidth / (2f * cam.aspect));
        }

        void OnGUI()
        {
            if (level == null || cam == null) return;

            GUI.matrix = Matrix4x4.identity;
            Vector2 rawMouse = Event.current.mousePosition;

            scale = Screen.height / RefHeight;
            virtualWidth = Screen.width / scale;
            EnsureStyles();
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(scale, scale, 1f));
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

            HandleWorldClick(rawMouse);
        }

        void HandleWorldClick(Vector2 rawMouse)
        {
            var e = Event.current;
            if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Escape)
            {
                level.CancelPlacing();
                level.Select(null);
                e.Use();
                return;
            }
            if (e.type != EventType.MouseDown || e.button != 0) return;

            Vector2 virtualMouse = rawMouse / scale;
            foreach (var r in blockers)
            {
                if (r.Contains(virtualMouse)) return;
            }

            var screenPoint = new Vector3(rawMouse.x, Screen.height - rawMouse.y, 0f);
            var ray = cam.ScreenPointToRay(screenPoint);
            var ground = new Plane(Vector3.up, Vector3.zero);
            if (ground.Raycast(ray, out float enter))
            {
                level.HandleClick(ray.GetPoint(enter));
                e.Use();
            }
        }

        // ---------------------------------------------------------------- Bereiche

        void DrawTopBar()
        {
            var r = new Rect(0f, 0f, virtualWidth, TopBarHeight);
            blockers.Add(r);
            GUI.Box(r, GUIContent.none, panelStyle);

            int totalWaves = level.Def.Waves.Length;
            string text = $"Münzen: {level.Money}      Kühe: {level.CowsRemaining}/{level.Def.CowCount}      Welle: {level.WavesStarted}/{totalWaves}";
            GUI.Label(new Rect(16f, 0f, virtualWidth - SideBarWidth - 32f, TopBarHeight), text, titleStyle);
            GUI.Label(new Rect(virtualWidth - SideBarWidth, 0f, SideBarWidth - 16f, TopBarHeight), level.Def.Name, smallStyle);
        }

        void DrawSideBar()
        {
            var r = new Rect(virtualWidth - SideBarWidth, TopBarHeight, SideBarWidth, RefHeight - TopBarHeight);
            blockers.Add(r);
            GUI.Box(r, GUIContent.none, panelStyle);

            GUILayout.BeginArea(new Rect(r.x + 12f, r.y + 10f, r.width - 24f, r.height - 20f));
            bool playing = level.State == LevelState.Building || level.State == LevelState.WaveRunning;

            GUILayout.Label("Türme", titleStyle);
            foreach (var type in TowerType.All)
            {
                bool active = level.PlacingType == type;
                GUI.enabled = playing && (active || level.Money >= type.Cost);
                if (GUILayout.Button($"{type.Name}\n{type.Cost} Münzen", active ? activeButtonStyle : buttonStyle, GUILayout.Height(56f)))
                {
                    if (active) level.CancelPlacing();
                    else level.BeginPlacing(type);
                }
            }
            GUI.enabled = true;
            GUILayout.Space(10f);

            if (level.PlacingType != null)
            {
                GUILayout.Label(level.PlacingType.Description, smallStyle);
                GUILayout.Label("Tippe auf ein hell markiertes Feld.", smallStyle);
                if (GUILayout.Button("Abbrechen", buttonStyle, GUILayout.Height(40f))) level.CancelPlacing();
            }
            else if (level.SelectedTower != null)
            {
                DrawTowerInfo(level.SelectedTower);
            }
            else if (playing)
            {
                GUILayout.Label("Wähle einen Turm und tippe auf ein freies Feld. Tippe auf einen Turm, um ihn zu verbessern.", smallStyle);
            }

            GUILayout.FlexibleSpace();

            if (level.State == LevelState.Building && level.HasMoreWaves)
            {
                bool lastWave = level.WavesStarted == level.Def.Waves.Length - 1;
                string label = lastWave ? "Letzte Welle starten!" : $"Welle {level.WavesStarted + 1} starten";
                if (GUILayout.Button(label, bigButtonStyle, GUILayout.Height(64f))) level.StartNextWave();
            }
            else if (level.State == LevelState.WaveRunning)
            {
                GUILayout.Label($"Welle {level.WavesStarted} läuft …", centerStyle, GUILayout.Height(64f));
            }

            GUILayout.EndArea();
        }

        void DrawTowerInfo(Tower tower)
        {
            GUILayout.Label($"{tower.Type.Name} (Stufe {tower.Level})", labelStyle);

            string stats = $"Schaden: {tower.Damage:0.#}\nReichweite: {tower.Range:0.#}\nAngriffe pro Sek.: {tower.FireRate:0.##}";
            if (tower.Splash > 0f) stats += $"\nFlächenradius: {tower.Splash:0.#}";
            if (tower.Slow < 1f) stats += $"\nVerlangsamung: {(1f - tower.Slow) * 100f:0} %";
            GUILayout.Label(stats, smallStyle);

            int? upgrade = tower.NextUpgradeCost;
            if (upgrade.HasValue)
            {
                GUI.enabled = level.Money >= upgrade.Value;
                if (GUILayout.Button($"Verbessern ({upgrade.Value})", buttonStyle, GUILayout.Height(44f))) level.UpgradeSelected();
                GUI.enabled = true;
            }
            else
            {
                GUILayout.Label("Maximale Stufe erreicht", smallStyle);
            }

            if (GUILayout.Button($"Verkaufen (+{tower.SellValue})", buttonStyle, GUILayout.Height(40f))) level.SellSelected();
        }

        void DrawWorldOverlays()
        {
            // Lebensbalken
            foreach (var enemy in level.Enemies)
            {
                if (!WorldToGui(enemy.HealthBarPoint, out var p)) continue;
                float w = 34f * Mathf.Clamp(enemy.Type.Scale, 1f, 1.8f);
                var bg = new Rect(p.x - w / 2f, p.y, w, 6f);
                float frac = Mathf.Clamp01(enemy.Hp / enemy.MaxHp);
                DrawRect(bg, new Color(0f, 0f, 0f, 0.7f));
                DrawRect(new Rect(bg.x + 1f, bg.y + 1f, (w - 2f) * frac, 4f), Color.Lerp(new Color(0.95f, 0.2f, 0.15f), new Color(0.3f, 0.9f, 0.25f), frac));
            }

            // Schwebende Texte (+Münzen, "Kuh entführt!")
            foreach (var ft in level.FloatingTexts)
            {
                float k = ft.Age / LevelManager.FloatingTextLifetime;
                if (!WorldToGui(ft.Position, out var p)) continue;
                p.y -= k * 40f;
                var c = ft.Color;
                c.a = 1f - k;
                floatStyle.normal.textColor = new Color(0f, 0f, 0f, c.a * 0.8f);
                GUI.Label(new Rect(p.x - 99f, p.y - 11f, 200f, 24f), ft.Text, floatStyle);
                floatStyle.normal.textColor = c;
                GUI.Label(new Rect(p.x - 100f, p.y - 12f, 200f, 24f), ft.Text, floatStyle);
            }
        }

        void DrawIntro()
        {
            Rect box = DrawOverlay(620f, 400f);
            GUILayout.BeginArea(new Rect(box.x + 24f, box.y + 20f, box.width - 48f, box.height - 40f));
            GUILayout.Label("Muh Defense", bigStyle);
            GUILayout.Label(
                "Ein Alien-Schiff vom Planeten Zorb ist auf Bauer Heinz' Weide bruchgelandet. " +
                "Die Aliens haben Bertas Milch probiert – und jetzt wollen sie ALLE Kühe!\n\n" +
                "Baue Türme neben den Weg, damit die Aliens die Weide nicht erreichen. " +
                "Schnappt sich ein Alien eine Kuh, besiege es, bevor es zurück zum Ufo kommt.\n\n" +
                "3 Sterne: keine Kuh verloren   •   2 Sterne: höchstens 2 verloren   •   1 Stern: Level geschafft",
                labelStyle);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Los geht's!", bigButtonStyle, GUILayout.Height(60f))) level.BeginLevel();
            GUILayout.EndArea();
        }

        void DrawEndScreen(bool won)
        {
            Rect box = DrawOverlay(520f, 340f);
            GUILayout.BeginArea(new Rect(box.x + 24f, box.y + 20f, box.width - 48f, box.height - 40f));
            GUILayout.Label(won ? "Gewonnen!" : "Alle Kühe weg!", bigStyle);

            if (won)
            {
                Rect starsRect = GUILayoutUtility.GetRect(box.width - 48f, 90f);
                float size = 80f;
                float startX = starsRect.x + (starsRect.width - size * 3f - 20f) / 2f;
                for (int i = 0; i < 3; i++)
                {
                    GUI.color = i < level.Stars ? new Color(1f, 0.82f, 0.1f) : new Color(0.3f, 0.3f, 0.3f);
                    GUI.DrawTexture(new Rect(startX + i * (size + 10f), starsRect.y + 5f, size, size), starTexture);
                }
                GUI.color = Color.white;
                GUILayout.Label($"Kühe gerettet: {level.CowsRemaining}/{level.Def.CowCount}   •   Bestes Ergebnis: {level.BestStars} Sterne", centerStyle);
            }
            else
            {
                GUILayout.Label("Die Aliens haben alle Kühe entführt. Versuch es mit einer anderen Turm-Aufstellung!", labelStyle);
            }

            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Nochmal spielen", bigButtonStyle, GUILayout.Height(60f))) level.Restart();
            GUILayout.EndArea();
        }

        /// <summary>Dunkelt den Bildschirm ab und zeichnet ein Fenster in der Mitte.</summary>
        Rect DrawOverlay(float width, float height)
        {
            var full = new Rect(0f, 0f, virtualWidth, RefHeight);
            blockers.Add(full);
            DrawRect(full, new Color(0f, 0f, 0f, 0.55f));
            var box = new Rect((virtualWidth - width) / 2f, (RefHeight - height) / 2f, width, height);
            GUI.Box(box, GUIContent.none, panelStyle);
            return box;
        }

        // ---------------------------------------------------------------- Hilfen

        bool WorldToGui(Vector3 world, out Vector2 gui)
        {
            Vector3 sp = cam.WorldToScreenPoint(world);
            gui = new Vector2(sp.x / scale, (Screen.height - sp.y) / scale);
            return sp.z > 0f;
        }

        static void DrawRect(Rect r, Color c)
        {
            var old = GUI.color;
            GUI.color = c;
            GUI.DrawTexture(r, Texture2D.whiteTexture);
            GUI.color = old;
        }

        static Texture2D MakeTexture(Color c)
        {
            var tex = new Texture2D(1, 1);
            tex.SetPixel(0, 0, c);
            tex.Apply();
            return tex;
        }

        /// <summary>Erzeugt eine weiße Stern-Textur (eingefärbt wird beim Zeichnen).</summary>
        static Texture2D MakeStarTexture(int size)
        {
            var points = new Vector2[10];
            for (int i = 0; i < 10; i++)
            {
                float a = Mathf.PI / 2f + i * Mathf.PI / 5f;
                float radius = i % 2 == 0 ? 0.48f : 0.2f;
                points[i] = new Vector2(0.5f + Mathf.Cos(a) * radius, 0.5f + Mathf.Sin(a) * radius);
            }

            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    var p = new Vector2((x + 0.5f) / size, (y + 0.5f) / size);
                    pixels[y * size + x] = InsidePolygon(p, points) ? new Color32(255, 255, 255, 255) : new Color32(255, 255, 255, 0);
                }
            }
            tex.SetPixels32(pixels);
            tex.Apply();
            return tex;
        }

        static bool InsidePolygon(Vector2 p, Vector2[] poly)
        {
            bool inside = false;
            for (int i = 0, j = poly.Length - 1; i < poly.Length; j = i++)
            {
                if ((poly[i].y > p.y) != (poly[j].y > p.y) &&
                    p.x < (poly[j].x - poly[i].x) * (p.y - poly[i].y) / (poly[j].y - poly[i].y) + poly[i].x)
                {
                    inside = !inside;
                }
            }
            return inside;
        }

        void EnsureStyles()
        {
            if (stylesReady) return;
            stylesReady = true;

            var cream = new Color(1f, 0.96f, 0.86f);

            panelStyle = new GUIStyle(GUI.skin.box);
            panelStyle.normal.background = MakeTexture(new Color(0.28f, 0.19f, 0.11f, 0.96f));

            titleStyle = new GUIStyle(GUI.skin.label) { fontSize = 22, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleLeft };
            titleStyle.normal.textColor = cream;

            labelStyle = new GUIStyle(GUI.skin.label) { fontSize = 18, wordWrap = true };
            labelStyle.normal.textColor = cream;

            smallStyle = new GUIStyle(GUI.skin.label) { fontSize = 15, wordWrap = true, alignment = TextAnchor.MiddleLeft };
            smallStyle.normal.textColor = new Color(0.92f, 0.86f, 0.74f);

            centerStyle = new GUIStyle(labelStyle) { alignment = TextAnchor.MiddleCenter };

            bigStyle = new GUIStyle(GUI.skin.label) { fontSize = 36, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            bigStyle.normal.textColor = new Color(1f, 0.85f, 0.3f);

            buttonStyle = new GUIStyle(GUI.skin.button) { fontSize = 16, wordWrap = true };
            activeButtonStyle = new GUIStyle(buttonStyle) { fontStyle = FontStyle.Bold };
            activeButtonStyle.normal.background = MakeTexture(new Color(0.35f, 0.65f, 0.25f));
            activeButtonStyle.hover.background = activeButtonStyle.normal.background;
            activeButtonStyle.normal.textColor = Color.white;
            activeButtonStyle.hover.textColor = Color.white;

            bigButtonStyle = new GUIStyle(GUI.skin.button) { fontSize = 22, fontStyle = FontStyle.Bold };
            bigButtonStyle.normal.background = MakeTexture(new Color(0.85f, 0.35f, 0.2f));
            bigButtonStyle.hover.background = MakeTexture(new Color(0.92f, 0.42f, 0.25f));
            bigButtonStyle.active.background = MakeTexture(new Color(0.7f, 0.28f, 0.15f));
            bigButtonStyle.normal.textColor = Color.white;
            bigButtonStyle.hover.textColor = Color.white;
            bigButtonStyle.active.textColor = Color.white;

            floatStyle = new GUIStyle(GUI.skin.label) { fontSize = 18, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };

            starTexture = MakeStarTexture(64);
        }
    }
}
