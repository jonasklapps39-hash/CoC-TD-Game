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
        public static Texture2D Star;

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
        public static void FitCamera(Camera cam, float width, float height)
        {
            float s = Scale;
            float sideFraction = Mathf.Clamp01(SideBarWidth * s / Screen.width);
            float topFraction = Mathf.Clamp01(TopBarHeight * s / Screen.height);
            cam.rect = new Rect(0f, 0f, 1f - sideFraction, 1f - topFraction);
            if (cam.pixelHeight > 0) cam.aspect = cam.pixelWidth / (float)cam.pixelHeight;

            var center = new Vector3(width / 2f, 0f, height / 2f);
            cam.transform.position = center - cam.transform.forward * 30f;

            float angle = cam.transform.eulerAngles.x * Mathf.Deg2Rad;
            float neededHeight = height * Mathf.Sin(angle) + 1.8f;
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
            var r = new Rect(center.x - 120f, center.y - 12f, 240f, 24f);
            var old = style.normal.textColor;
            style.normal.textColor = new Color(0f, 0f, 0f, color.a * 0.8f);
            GUI.Label(new Rect(r.x + 1f, r.y + 1f, r.width, r.height), text, style);
            style.normal.textColor = color;
            GUI.Label(r, text, style);
            style.normal.textColor = old;
        }

        /// <summary>Dunkelt den Bildschirm ab, zeichnet ein Fenster in der Mitte und sperrt Klicks dahinter.</summary>
        public static Rect DrawOverlay(List<Rect> blockers, float width, float height)
        {
            var full = new Rect(0f, 0f, VirtualWidth, RefHeight);
            blockers.Add(full);
            DrawRect(full, new Color(0f, 0f, 0f, 0.55f));
            var box = new Rect((VirtualWidth - width) / 2f, (RefHeight - height) / 2f, width, height);
            GUI.Box(box, GUIContent.none, Panel);
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

            Panel = new GUIStyle(GUI.skin.box);
            Panel.normal.background = MakeTexture(new Color(0.28f, 0.19f, 0.11f, 0.96f));

            Title = new GUIStyle(GUI.skin.label) { fontSize = 22, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleLeft };
            Title.normal.textColor = cream;

            Label = new GUIStyle(GUI.skin.label) { fontSize = 18, wordWrap = true };
            Label.normal.textColor = cream;

            Small = new GUIStyle(GUI.skin.label) { fontSize = 15, wordWrap = true, alignment = TextAnchor.MiddleLeft };
            Small.normal.textColor = new Color(0.92f, 0.86f, 0.74f);

            Center = new GUIStyle(Label) { alignment = TextAnchor.MiddleCenter };

            Big = new GUIStyle(GUI.skin.label) { fontSize = 36, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            Big.normal.textColor = Gold;

            Button = new GUIStyle(GUI.skin.button) { fontSize = 16, wordWrap = true };

            ActiveButton = new GUIStyle(Button) { fontStyle = FontStyle.Bold };
            ActiveButton.normal.background = MakeTexture(new Color(0.35f, 0.65f, 0.25f));
            ActiveButton.hover.background = ActiveButton.normal.background;
            ActiveButton.normal.textColor = Color.white;
            ActiveButton.hover.textColor = Color.white;

            BigButton = new GUIStyle(GUI.skin.button) { fontSize = 22, fontStyle = FontStyle.Bold, wordWrap = true };
            BigButton.normal.background = MakeTexture(new Color(0.85f, 0.35f, 0.2f));
            BigButton.hover.background = MakeTexture(new Color(0.92f, 0.42f, 0.25f));
            BigButton.active.background = MakeTexture(new Color(0.7f, 0.28f, 0.15f));
            BigButton.normal.textColor = Color.white;
            BigButton.hover.textColor = Color.white;
            BigButton.active.textColor = Color.white;

            Float = new GUIStyle(GUI.skin.label) { fontSize = 18, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };

            Bubble = new GUIStyle(GUI.skin.box) { fontSize = 14, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            Bubble.normal.background = MakeTexture(new Color(1f, 0.97f, 0.88f, 0.95f));
            Bubble.normal.textColor = new Color(0.35f, 0.22f, 0.1f);

            Star = MakeStarTexture(64);
        }
    }
}
