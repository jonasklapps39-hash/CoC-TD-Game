using System.Collections.Generic;
using UnityEngine;

namespace MuhDefense
{
    /// <summary>
    /// Gemeinsame Bausteine für die IMGUI-Oberflächen von Hof und Level:
    /// Stile, Skalierung, Kamera-Ausrichtung und Klicks auf die Spielwelt.
    /// Alle Maße sind für eine Bildschirmhöhe von 720 Einheiten ausgelegt und werden skaliert.
    /// </summary>
    public static class UiKit
    {
        public const float RefHeight = 720f;
        public const float TopBarHeight = 56f;
        public const float SideBarWidth = 260f;

        public static readonly Color Gold = new Color(1f, 0.85f, 0.3f);
        public static readonly Color CornColor = new Color(1f, 0.85f, 0.3f);
        public static readonly Color MilkColor = new Color(0.85f, 0.95f, 1f);
        public static readonly Color BadColor = new Color(1f, 0.4f, 0.3f);

        public static GUIStyle Panel, Title, Label, Small, Center, Big, Button, ActiveButton, BigButton, Float, Bubble;
        /// <summary>Blaues Fenster mit Goldrand (Dialoge wie in Bloons TD6).</summary>
        public static GUIStyle Dialog;
        /// <summary>Dunkle, halbtransparente Fläche innerhalb von Fenstern.</summary>
        public static GUIStyle Card;
        public static GUIStyle BlueCard, YellowButton, RedButton, Pill;
        /// <summary>Schrift auf Knöpfen (Comic-Schrift, weiß mit dunkler Umrandung).</summary>
        public static GUIStyle ButtonText;
        public static Texture2D Star;
        /// <summary>Comic-Schrift „Luckiest Guy“ (Apache-2.0-Lizenz), null falls nicht vorhanden.</summary>
        public static Font ComicFont;

        public static readonly Color OutlineColor = new Color(0.14f, 0.07f, 0.03f);
        static readonly Dictionary<string, Texture2D> icons = new Dictionary<string, Texture2D>();

        /// <summary>Faktor von virtuellen Einheiten zu Bildschirmpixeln.</summary>
        public static float Scale => Screen.height / RefHeight;
        public static float VirtualWidth => Screen.width / Scale;

        static bool stylesReady;

        /// <summary>Zu Beginn jedes OnGUI aufrufen. Liefert die rohe Mausposition (Pixel) für Klicks in die Welt.</summary>
        public static Vector2 BeginGui()
        {
            GUI.matrix = Matrix4x4.identity;
            Vector2 rawMouse = Event.current.mousePosition;
            EnsureStyles();
            float s = Scale;
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(s, s, 1f));
            return rawMouse;
        }

        /// <summary>Richtet die Kamera so aus, dass ein Feld der Größe width × height neben Seitenleiste und Kopfzeile ganz sichtbar ist.</summary>
        /// <param name="angle">Neigung der Kamera in Grad (90 = senkrecht von oben).</param>
        public static void FitCamera(Camera cam, float width, float height, float angle = 55f)
        {
            cam.transform.rotation = Quaternion.Euler(angle, 0f, 0f);
            float s = Scale;
            float sideFraction = Mathf.Clamp01(SideBarWidth * s / Screen.width);
            float topFraction = Mathf.Clamp01(TopBarHeight * s / Screen.height);
            cam.rect = new Rect(0f, 0f, 1f - sideFraction, 1f - topFraction);
            if (cam.pixelHeight > 0) cam.aspect = cam.pixelWidth / (float)cam.pixelHeight;

            var center = new Vector3(width / 2f, 0f, height / 2f);
            cam.transform.position = center - cam.transform.forward * 30f;

            float rad = angle * Mathf.Deg2Rad;
            float neededHeight = height * Mathf.Sin(rad) + 1.8f;
            float neededWidth = width + 1f;
            cam.orthographicSize = Mathf.Max(neededHeight / 2f, neededWidth / (2f * cam.aspect));
        }

        /// <summary>Ist das aktuelle Ereignis ein Klick in die Spielwelt (nicht auf ein Bedienelement), liefert es den Punkt auf dem Boden.</summary>
        public static bool TryGetWorldClick(Vector2 rawMouse, List<Rect> blockers, Camera cam, out Vector3 world)
        {
            world = default;
            var e = Event.current;
            if (e.type != EventType.MouseDown || e.button != 0) return false;

            Vector2 virtualMouse = rawMouse / Scale;
            foreach (var r in blockers)
            {
                if (r.Contains(virtualMouse)) return false;
            }

            var ray = cam.ScreenPointToRay(new Vector3(rawMouse.x, Screen.height - rawMouse.y, 0f));
            var ground = new Plane(Vector3.up, Vector3.zero);
            if (!ground.Raycast(ray, out float enter)) return false;
            world = ray.GetPoint(enter);
            e.Use();
            return true;
        }

        /// <summary>Liegt der Zeiger über einem Bedienelement?</summary>
        public static bool IsOverUi(Vector2 rawMouse, List<Rect> blockers)
        {
            Vector2 v = rawMouse / Scale;
            foreach (var r in blockers)
            {
                if (r.Contains(v)) return true;
            }
            return false;
        }

        /// <summary>Punkt auf dem Boden unter dem Zeiger.</summary>
        public static bool ScreenToGround(Vector2 rawMouse, Camera cam, out Vector3 world)
        {
            world = default;
            var ray = cam.ScreenPointToRay(new Vector3(rawMouse.x, Screen.height - rawMouse.y, 0f));
            var ground = new Plane(Vector3.up, Vector3.zero);
            if (!ground.Raycast(ray, out float enter)) return false;
            world = ray.GetPoint(enter);
            return true;
        }

        public static bool EscapePressed()
        {
            var e = Event.current;
            if (e.type != EventType.KeyDown || e.keyCode != KeyCode.Escape) return false;
            e.Use();
            return true;
        }

        public static bool WorldToGui(Camera cam, Vector3 world, out Vector2 gui)
        {
            Vector3 sp = cam.WorldToScreenPoint(world);
            float s = Scale;
            gui = new Vector2(sp.x / s, (Screen.height - sp.y) / s);
            return sp.z > 0f;
        }

        public static void DrawRect(Rect r, Color c)
        {
            var old = GUI.color;
            GUI.color = c;
            GUI.DrawTexture(r, Texture2D.whiteTexture);
            GUI.color = old;
        }

        /// <summary>Balken mit Hintergrund, z. B. Lebensbalken oder Baufortschritt.</summary>
        public static void DrawBar(Rect r, float fraction, Color fill)
        {
            DrawRect(r, new Color(0f, 0f, 0f, 0.7f));
            DrawRect(new Rect(r.x + 1f, r.y + 1f, (r.width - 2f) * Mathf.Clamp01(fraction), r.height - 2f), fill);
        }

        /// <summary>Text mit leichtem Schatten, zentriert um einen Punkt.</summary>
        public static void ShadowLabel(Vector2 center, string text, Color color, GUIStyle style = null)
        {
            style = style ?? Float;
            var r = new Rect(center.x - 140f, center.y - 14f, 280f, 28f);
            OutlinedText(r, text, style, color, 1.6f);
        }

        /// <summary>Dunkelt den Bildschirm ab, zeichnet ein Fenster in der Mitte und sperrt Klicks dahinter.</summary>
        public static Rect DrawOverlay(List<Rect> blockers, float width, float height)
        {
            var full = new Rect(0f, 0f, VirtualWidth, RefHeight);
            blockers.Add(full);
            DrawRect(full, new Color(0f, 0f, 0f, 0.55f));
            var box = new Rect((VirtualWidth - width) / 2f, (RefHeight - height) / 2f, width, height);
            GUI.Box(box, GUIContent.none, Dialog);
            return box;
        }

        /// <summary>Zeichnet 3 Sterne, davon <paramref name="filled"/> gelb.</summary>
        public static void DrawStars(Rect area, int filled, float size)
        {
            float gap = size * 0.12f;
            float startX = area.x + (area.width - size * 3f - gap * 2f) / 2f;
            float y = area.y + (area.height - size) / 2f;
            var old = GUI.color;
            for (int i = 0; i < 3; i++)
            {
                GUI.color = i < filled ? new Color(1f, 0.82f, 0.1f) : new Color(0.3f, 0.3f, 0.3f);
                GUI.DrawTexture(new Rect(startX + i * (size + gap), y, size, size), Star);
            }
            GUI.color = old;
        }

        public static string FormatTime(double seconds)
        {
            int s = Mathf.Max(0, Mathf.CeilToInt((float)seconds));
            if (s >= 3600) return $"{s / 3600} h {s % 3600 / 60} min";
            if (s >= 60) return $"{s / 60} min {s % 60} s";
            return $"{s} s";
        }

        static Texture2D MakeTexture(Color c)
        {
            var tex = new Texture2D(1, 1);
            tex.SetPixel(0, 0, c);
            tex.Apply();
            return tex;
        }

        /// <summary>
        /// Abgerundete Fläche mit Farbverlauf (oben → unten), Rand und leichtem Glanz oben.
        /// Wird als 9-Slice-Hintergrund genutzt (GUIStyle.border = Radius + Rand).
        /// </summary>
        static Texture2D MakeRounded(Color top, Color bottom, Color border, int radius = 14, int borderWidth = 3, bool gloss = true)
        {
            const int size = 64;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            var px = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    // Abstand zur abgerundeten Kante (negativ = innen)
                    float cx = Mathf.Clamp(x + 0.5f, radius, size - radius);
                    float cy = Mathf.Clamp(y + 0.5f, radius, size - radius);
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(cx, cy)) - radius;
                    float alpha = Mathf.Clamp01(0.5f - d);
                    float t = y / (float)(size - 1); // 0 = unten (Texturen beginnen unten)
                    Color c = Color.Lerp(bottom, top, t);
                    if (gloss && t > 0.55f) c = Color.Lerp(c, Color.white, 0.12f * (t - 0.55f) / 0.45f);
                    if (d > -borderWidth) c = border;
                    c.a *= alpha;
                    px[y * size + x] = c;
                }
            }
            tex.SetPixels(px);
            tex.Apply();
            return tex;
        }

        static GUIStyle RoundedStyle(GUIStyle baseStyle, Color top, Color bottom, Color border, int radius = 14, int borderWidth = 3)
        {
            var st = new GUIStyle(baseStyle);
            int b = radius + borderWidth;
            st.border = new RectOffset(b, b, b, b);
            st.normal.background = MakeRounded(top, bottom, border, radius, borderWidth);
            st.hover.background = MakeRounded(Color.Lerp(top, Color.white, 0.12f), Color.Lerp(bottom, Color.white, 0.12f), border, radius, borderWidth);
            st.active.background = MakeRounded(Color.Lerp(top, Color.black, 0.15f), Color.Lerp(bottom, Color.black, 0.15f), border, radius, borderWidth);
            st.focused.background = st.normal.background;
            st.onNormal.background = st.active.background;
            st.normal.textColor = Color.white;
            st.hover.textColor = Color.white;
            st.active.textColor = Color.white;
            return st;
        }

        // ---------------------------------------------------------------- Text mit Umrandung, Knöpfe, Symbole

        /// <summary>Text mit dunkler Umrandung (wie in Bloons TD6).</summary>
        public static void OutlinedText(Rect r, string text, GUIStyle style, Color fill, float thickness = 2f)
        {
            var old = style.normal.textColor;
            var outline = OutlineColor;
            outline.a = fill.a;
            style.normal.textColor = outline;
            for (int i = 0; i < 8; i++)
            {
                float a = i * Mathf.PI / 4f;
                GUI.Label(new Rect(r.x + Mathf.Cos(a) * thickness, r.y + Mathf.Sin(a) * thickness + 1f, r.width, r.height), text, style);
            }
            style.normal.textColor = fill;
            GUI.Label(r, text, style);
            style.normal.textColor = old;
        }

        public static void OutlinedText(Rect r, string text, GUIStyle style) => OutlinedText(r, text, style, Color.white);

        /// <summary>Überschrift im Layout (Comic-Schrift mit Umrandung).</summary>
        public static void Heading(string text, GUIStyle style, params GUILayoutOption[] options)
        {
            Rect r = GUILayoutUtility.GetRect(new GUIContent(text), style, options);
            OutlinedText(r, text, style, Color.white);
        }

        /// <summary>Knopf im Layout: Hintergrund aus dem Stil, Text in Comic-Schrift mit Umrandung, Klick-Geräusch.</summary>
        public static bool Btn(string text, GUIStyle style, params GUILayoutOption[] options)
        {
            Rect r = GUILayoutUtility.GetRect(GUIContent.none, style, options);
            return BtnRect(r, text, style, null);
        }

        /// <summary>Knopf im Layout mit Symbol links.</summary>
        public static bool IconBtn(string text, Texture2D icon, GUIStyle style, params GUILayoutOption[] options)
        {
            Rect r = GUILayoutUtility.GetRect(GUIContent.none, style, options);
            return BtnRect(r, text, style, icon);
        }

        public static bool BtnRect(Rect r, string text, GUIStyle style, Texture2D icon)
        {
            bool clicked = GUI.Button(r, GUIContent.none, style);
            if (clicked) GameAudio.Play(SoundId.Click);
            var content = new Rect(r.x + 6f, r.y + 3f, r.width - 12f, r.height - 6f);
            if (icon != null)
            {
                float s = Mathf.Min(content.height, 48f);
                GUI.DrawTexture(new Rect(content.x, content.y + (content.height - s) / 2f, s, s), icon, ScaleMode.ScaleToFit);
                content.xMin += s + 4f;
            }
            ButtonText.fontSize = Mathf.Max(12, style.fontSize);
            OutlinedText(content, text, ButtonText, Color.white, 1.6f);
            return clicked;
        }

        /// <summary>Anzeige-Pille mit Symbol und Wert (Kopfzeile).</summary>
        public static void PillLabel(Rect r, Texture2D icon, string text)
        {
            GUI.Box(r, GUIContent.none, Pill);
            float s = r.height + 6f;
            if (icon != null) GUI.DrawTexture(new Rect(r.x - 6f, r.y - 3f, s, s), icon, ScaleMode.ScaleToFit);
            var tr = new Rect(r.x + (icon != null ? s - 2f : 10f), r.y, r.width - (icon != null ? s : 14f), r.height);
            Title.alignment = TextAnchor.MiddleLeft;
            OutlinedText(tr, text, Title, Color.white);
        }

        /// <summary>Symbol aus Assets/Resources/Icons (gerendert aus den Blender-Modellen), null falls nicht vorhanden.</summary>
        public static Texture2D Icon(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            if (!icons.TryGetValue(name, out var tex))
            {
                tex = Resources.Load<Texture2D>("Icons/" + name);
                icons[name] = tex;
            }
            return tex;
        }

        public static Texture2D TowerIcon(TowerType t)
        {
            if (t == TowerType.Mistgabel) return Icon("MistgabelBauer");
            if (t == TowerType.Heuballen) return Icon("HeuballenKatapult");
            if (t == TowerType.Eismilch) return Icon("EismilchSprueher");
            if (t == TowerType.Bienenstock) return Icon("Bienenstock");
            if (t == TowerType.Windmuehle) return Icon("Windmuehle");
            if (t == TowerType.Wetterhahn) return Icon("Wetterhahn");
            if (t == TowerType.Hofladen) return Icon("Hofladen");
            return null;
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

        static void EnsureStyles()
        {
            if (stylesReady && Panel != null && Panel.normal.background != null) return;
            stylesReady = true;

            var cream = new Color(1f, 0.96f, 0.86f);
            ComicFont = Resources.Load<Font>("Fonts/LuckiestGuy-Regular");

            // Holzrahmen für Kopfzeile und Seitenleiste
            Panel = RoundedStyle(GUI.skin.box, new Color(0.56f, 0.37f, 0.2f), new Color(0.42f, 0.26f, 0.13f), new Color(0.24f, 0.13f, 0.05f), 12, 3);
            Panel.normal.background = Panel.normal.background;
            Dialog = RoundedStyle(GUI.skin.box, new Color(0.25f, 0.47f, 0.85f), new Color(0.13f, 0.27f, 0.6f), new Color(0.98f, 0.78f, 0.25f), 16, 4);
            Card = RoundedStyle(GUI.skin.box, new Color(0f, 0f, 0f, 0.22f), new Color(0f, 0f, 0f, 0.32f), new Color(0f, 0f, 0f, 0.35f), 10, 2);
            Pill = RoundedStyle(GUI.skin.box, new Color(0.12f, 0.07f, 0.03f, 0.55f), new Color(0.12f, 0.07f, 0.03f, 0.7f), new Color(0.06f, 0.03f, 0.01f, 0.6f), 14, 2);

            Title = new GUIStyle(GUI.skin.label) { fontSize = 22, alignment = TextAnchor.MiddleLeft };
            if (ComicFont != null) Title.font = ComicFont;
            Title.normal.textColor = Color.white;

            Big = new GUIStyle(Title) { fontSize = 40, alignment = TextAnchor.MiddleCenter, wordWrap = true };

            ButtonText = new GUIStyle(GUI.skin.label) { fontSize = 18, alignment = TextAnchor.MiddleCenter, wordWrap = true, clipping = TextClipping.Overflow };
            if (ComicFont != null) ButtonText.font = ComicFont;

            Label = new GUIStyle(GUI.skin.label) { fontSize = 18, wordWrap = true };
            Label.normal.textColor = cream;

            Small = new GUIStyle(GUI.skin.label) { fontSize = 15, wordWrap = true, alignment = TextAnchor.MiddleLeft };
            Small.normal.textColor = new Color(0.97f, 0.92f, 0.82f);

            Center = new GUIStyle(Label) { alignment = TextAnchor.MiddleCenter };

            var brown = RoundedStyle(GUI.skin.button, new Color(0.8f, 0.6f, 0.36f), new Color(0.6f, 0.41f, 0.2f), new Color(0.28f, 0.15f, 0.06f));
            Button = new GUIStyle(brown) { fontSize = 17 };
            ActiveButton = RoundedStyle(GUI.skin.button, new Color(0.58f, 0.93f, 0.3f), new Color(0.24f, 0.66f, 0.12f), new Color(0.08f, 0.28f, 0.04f));
            ActiveButton.fontSize = 17;
            BigButton = new GUIStyle(ActiveButton) { fontSize = 24 };
            YellowButton = RoundedStyle(GUI.skin.button, new Color(1f, 0.9f, 0.35f), new Color(0.96f, 0.66f, 0.1f), new Color(0.42f, 0.25f, 0.04f));
            YellowButton.fontSize = 17;
            RedButton = RoundedStyle(GUI.skin.button, new Color(1f, 0.48f, 0.38f), new Color(0.8f, 0.18f, 0.12f), new Color(0.32f, 0.05f, 0.03f));
            RedButton.fontSize = 17;
            BlueCard = RoundedStyle(GUI.skin.button, new Color(0.48f, 0.8f, 1f), new Color(0.2f, 0.52f, 0.92f), new Color(0.98f, 0.78f, 0.25f), 12, 3);
            BlueCard.fontSize = 14;

            Float = new GUIStyle(Title) { fontSize = 20, alignment = TextAnchor.MiddleCenter };

            Bubble = RoundedStyle(GUI.skin.box, new Color(1f, 0.98f, 0.9f, 0.97f), new Color(0.98f, 0.9f, 0.72f, 0.97f), new Color(0.45f, 0.28f, 0.12f), 10, 2);
            Bubble.fontSize = 14;
            Bubble.fontStyle = FontStyle.Bold;
            Bubble.alignment = TextAnchor.MiddleCenter;
            Bubble.normal.textColor = new Color(0.35f, 0.22f, 0.1f);
            Bubble.hover.textColor = Bubble.normal.textColor;

            Star = MakeStarTexture(64);
        }
    }
}
