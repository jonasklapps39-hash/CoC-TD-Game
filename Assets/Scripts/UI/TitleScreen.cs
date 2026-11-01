using System.Collections.Generic;
using UnityEngine;

namespace MuhDefense
{
    /// <summary>
    /// Startbildschirm: kleine Weide mit Kühen, ein Ufo versucht eine Kuh hochzuziehen, Bauer Heinz und Berta passen auf.
    /// Darüber das Logo, „Spielen“ und „Einstellungen“. „Spielen“ zeigt kurz einen Ladebildschirm und öffnet den Hof.
    /// </summary>
    public class TitleScreen : MonoBehaviour
    {
        const float LoadDuration = 1.1f;

        Camera cam;
        readonly List<Rect> blockers = new List<Rect>();
        readonly List<Transform> cows = new List<Transform>();
        Transform ufo;
        Transform liftedCow;
        LineRenderer beam;
        GUIStyle logoStyle, subStyle, tinyStyle;
        SettingsWindow settings;
        bool loading;
        float loadTime;
        int loadStep;

        public void Init()
        {
            cam = Camera.main;
            BuildScene();
            GameAudio.PlayMusic(MusicId.Farm);
        }

        void BuildScene()
        {
            var ground = Visuals.Prim(PrimitiveType.Cube, transform, new Vector3(0f, -0.25f, 0f), new Vector3(40f, 0.5f, 40f), Color.white, "Wiese", outline: false);
            ground.GetComponent<Renderer>().sharedMaterial = GroundTextures.Material(GroundTextures.Kind.Grass, new Color(0.45f, 0.78f, 0.28f), 40f);
            GroundTextures.ScatterDecor(transform, new Rect(-12f, -8f, 24f, 16f), 160, 11, p => Mathf.Abs(p.x) > 1.5f || Mathf.Abs(p.y) > 1.5f);

            Place("Bauernhaus", new Vector3(0f, 0f, 4.5f), 3.2f, 180f, ModelLibrary.Fit.Footprint);
            Place("Kuhstall", new Vector3(-4.2f, 0f, 3.8f), 2.2f, 200f, ModelLibrary.Fit.Footprint);
            Place("Maissilo", new Vector3(4.2f, 0f, 4f), 1.8f, 160f, ModelLibrary.Fit.Footprint);
            var rng = new System.Random(5);
            for (int i = 0; i < 14; i++)
            {
                float x = (float)rng.NextDouble() * 22f - 11f;
                float z = 5.5f + (float)rng.NextDouble() * 5f;
                if (Mathf.Abs(x) < 6f && z < 7f) continue;
                Place("Baum", new Vector3(x, 0f, z), 1.8f + (float)rng.NextDouble(), (float)rng.NextDouble() * 360f, ModelLibrary.Fit.Height);
            }

            // Kühe auf der Weide
            for (int i = 0; i < 5; i++)
            {
                var cow = Place("Kuh", new Vector3(-3f + i * 1.5f, 0f, -0.5f + (i % 2) * 1.1f), 0.8f, 150f + i * 40f, ModelLibrary.Fit.Height);
                if (cow != null) cows.Add(cow);
            }
            liftedCow = Place("Kuh", new Vector3(0.6f, 1.2f, 1.2f), 0.8f, 20f, ModelLibrary.Fit.Height);

            // Ufo mit Traktorstrahl
            ufo = Place("Ufo", new Vector3(0.6f, 3.6f, 1.2f), 2.4f, 0f, ModelLibrary.Fit.Footprint);
            var beamGo = new GameObject("Strahl");
            beamGo.transform.SetParent(transform, false);
            beam = beamGo.AddComponent<LineRenderer>();
            beam.sharedMaterial = new Material(Shader.Find("Sprites/Default"));
            beam.positionCount = 2;
            beam.widthMultiplier = 1.2f;
            beam.startColor = new Color(0.6f, 1f, 0.6f, 0.45f);
            beam.endColor = new Color(0.6f, 1f, 0.6f, 0.1f);
            beam.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            // Die Helden passen auf
            Place("BauerHeinz", new Vector3(-2.2f, 0f, -2.2f), 1.5f, 160f, ModelLibrary.Fit.Height);
            Place("Berta", new Vector3(2.6f, 0f, -2.1f), 1.1f, 200f, ModelLibrary.Fit.Height);
            Place("OmaGertrud", new Vector3(-4.2f, 0f, -1.2f), 1.4f, 150f, ModelLibrary.Fit.Height);
        }

        Transform Place(string model, Vector3 pos, float size, float yaw, ModelLibrary.Fit fit)
        {
            var holder = new GameObject(model).transform;
            holder.SetParent(transform, false);
            holder.localPosition = pos;
            holder.localRotation = Quaternion.Euler(0f, yaw, 0f);
            if (ModelLibrary.Spawn(model, holder, size, fit) == null)
            {
                Visuals.Prim(PrimitiveType.Cube, holder, new Vector3(0f, size * 0.3f, 0f), Vector3.one * size * 0.6f, new Color(0.8f, 0.8f, 0.8f), model);
            }
            return holder;
        }

        void Update()
        {
            if (cam == null) cam = Camera.main;
            if (cam != null)
            {
                cam.rect = new Rect(0f, 0f, 1f, 1f);
                cam.aspect = Screen.width / (float)Mathf.Max(1, Screen.height);
                cam.transform.rotation = Quaternion.Euler(38f, 0f, 0f);
                cam.transform.position = new Vector3(0f, 0f, 0.8f) - cam.transform.forward * 30f;
                cam.orthographicSize = 5.2f;
            }

            float t = Time.time;
            if (ufo != null)
            {
                ufo.localPosition = new Vector3(0.6f + Mathf.Sin(t * 0.7f) * 0.3f, 3.6f + Mathf.Sin(t * 1.6f) * 0.12f, 1.2f);
                ufo.Rotate(0f, 50f * Time.deltaTime, 0f);
            }
            if (liftedCow != null && ufo != null)
            {
                // Die Kuh zappelt im Strahl, kommt aber nie ganz hoch – die Helden passen ja auf
                liftedCow.localPosition = new Vector3(ufo.localPosition.x, 1.1f + Mathf.Sin(t * 1.3f) * 0.35f, 1.2f);
                liftedCow.localRotation = Quaternion.Euler(Mathf.Sin(t * 6f) * 12f, t * 40f, Mathf.Sin(t * 5f) * 10f);
                beam.SetPosition(0, ufo.position);
                beam.SetPosition(1, new Vector3(ufo.position.x, 0f, ufo.position.z));
            }
            for (int i = 0; i < cows.Count; i++)
            {
                if (cows[i] == null) continue;
                cows[i].localRotation = Quaternion.Euler(6f + Mathf.Sin(t * 2.2f + i) * 5f, 150f + i * 40f, 0f);
            }

            if (loading)
            {
                loadTime += Time.unscaledDeltaTime;
                // Vorbereitung in kleinen Schritten, während der Balken läuft
                if (loadStep == 0) { GroundTextures.Get(GroundTextures.Kind.Grass); loadStep++; }
                else if (loadStep == 1) { GroundTextures.Get(GroundTextures.Kind.Dirt); GroundTextures.Get(GroundTextures.Kind.Field); loadStep++; }
                else if (loadStep == 2) { var f = GameFlow.Farm; loadStep++; }
                if (loadTime >= LoadDuration) GameFlow.ShowFarm();
            }
        }

        void OnGUI()
        {
            UiKit.BeginGui();
            blockers.Clear();
            if (logoStyle == null)
            {
                logoStyle = new GUIStyle(UiKit.Big) { fontSize = 96, alignment = TextAnchor.MiddleCenter, wordWrap = false };
                subStyle = new GUIStyle(UiKit.Title) { fontSize = 26, alignment = TextAnchor.MiddleCenter };
                tinyStyle = new GUIStyle(UiKit.Small) { fontSize = 13, alignment = TextAnchor.LowerRight };
            }
            float vw = UiKit.VirtualWidth;

            if (loading)
            {
                DrawLoading(vw);
                return;
            }

            // Logo mit leichtem Wackeln
            var logo = new Rect(0f, 40f, vw, 120f);
            var pivot = new Vector2(vw / 2f, 100f);
            var m = GUI.matrix;
            GUIUtility.RotateAroundPivot(Mathf.Sin(Time.time * 1.5f) * 2.5f, pivot);
            UiKit.OutlinedText(logo, "MUH DEFENSE", logoStyle, new Color(1f, 0.86f, 0.25f), 5f);
            GUI.matrix = m;
            UiKit.OutlinedText(new Rect(0f, 150f, vw, 40f), "Die Aliens wollen eure Milch!", subStyle, Color.white, 2.5f);

            if (settings != null)
            {
                if (!settings.Draw(blockers)) settings = null;
                return;
            }

            float bw = 320f;
            var play = new Rect((vw - bw) / 2f, UiKit.RefHeight - 210f, bw, 84f);
            if (UiKit.BtnRect(play, "Spielen", UiKit.BigButton, UiKit.Icon("Kuh")))
            {
                loading = true;
                loadTime = 0f;
                loadStep = 0;
            }
            var opt = new Rect((vw - bw) / 2f, UiKit.RefHeight - 114f, bw, 56f);
            if (UiKit.BtnRect(opt, "Einstellungen", UiKit.Button, null)) settings = new SettingsWindow();

            GUI.Label(new Rect(vw - 320f, UiKit.RefHeight - 34f, 304f, 24f), "Version " + Application.version, tinyStyle);
        }

        void DrawLoading(float vw)
        {
            UiKit.DrawRect(new Rect(0f, 0f, vw, UiKit.RefHeight), new Color(0.12f, 0.08f, 0.04f, 0.85f));
            UiKit.OutlinedText(new Rect(0f, 220f, vw, 100f), "MUH DEFENSE", logoStyle, new Color(1f, 0.86f, 0.25f), 5f);
            float k = Mathf.Clamp01(loadTime / LoadDuration);
            var bar = new Rect(vw / 2f - 260f, 380f, 520f, 28f);
            UiKit.DrawBar(bar, k, new Color(0.45f, 0.85f, 0.3f));
            // eine Kuh läuft über den Balken
            var cow = UiKit.Icon("Kuh");
            if (cow != null) GUI.DrawTexture(new Rect(bar.x + bar.width * k - 30f, bar.y - 58f, 60f, 60f), cow, ScaleMode.ScaleToFit);
            string[] tips =
            {
                "Tipp: Hindernisse auf dem Hof bringen Diamanten.",
                "Tipp: Ziehe einen Turm direkt von der Karte aufs Feld.",
                "Tipp: Erst Leicht, dann Mittel, dann Schwer – jeder Grad hat eigene Sterne.",
                "Tipp: Der Wetterhahn macht getarnte Aliens sichtbar.",
            };
            string tip = tips[(int)(Time.realtimeSinceStartup / 3f) % tips.Length];
            UiKit.OutlinedText(new Rect(0f, 430f, vw, 40f), tip, subStyle, Color.white, 2f);
        }
    }
}
