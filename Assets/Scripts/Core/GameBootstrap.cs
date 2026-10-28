using UnityEngine;

namespace MuhDefense
{
    /// <summary>
    /// Startet den Prototyp automatisch, sobald in Unity "Play" gedrückt wird –
    /// in jeder Szene, die noch keinen Hof und kein Level enthält.
    /// Kamera, Licht, Hof und Level werden komplett per Code aufgebaut, es sind also
    /// (noch) keine Szenen, Prefabs oder Assets nötig.
    /// </summary>
    public static class GameBootstrap
    {
        public const float CameraAngle = 55f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void AutoStart()
        {
            if (Object.FindFirstObjectByType<LevelManager>() != null) return;
            if (Object.FindFirstObjectByType<FarmManager>() != null) return;

            ConfigureDevice();
            SetupCameras();
            SetupLight();
            GameFlow.ShowFarm();
        }

        static void ConfigureDevice()
        {
            // Das Spiel läuft im Querformat.
            Screen.autorotateToPortrait = false;
            Screen.autorotateToPortraitUpsideDown = false;
            Screen.autorotateToLandscapeLeft = true;
            Screen.autorotateToLandscapeRight = true;
            Screen.orientation = ScreenOrientation.AutoRotation;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            Application.targetFrameRate = 60;
        }

        static void SetupCameras()
        {
            var cam = Camera.main;
            if (cam == null)
            {
                var go = new GameObject("Main Camera") { tag = "MainCamera" };
                cam = go.AddComponent<Camera>();
                go.AddComponent<AudioListener>();
            }
            cam.orthographic = true;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.23f, 0.42f, 0.18f);
            cam.transform.rotation = Quaternion.Euler(CameraAngle, 0f, 0f);
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 100f;
            cam.depth = 0f;

            // Die Hauptkamera zeichnet nur den Spielbereich (ohne Seitenleiste).
            // Diese Kamera füllt den Rest des Bildschirms mit einer Farbe.
            var bg = new GameObject("Hintergrund-Kamera").AddComponent<Camera>();
            bg.depth = -10f;
            bg.cullingMask = 0;
            bg.clearFlags = CameraClearFlags.SolidColor;
            bg.backgroundColor = new Color(0.16f, 0.12f, 0.08f);
        }

        static void SetupLight()
        {
            // Helles, gleichmäßiges Umgebungslicht und Schatten bis zum Rand des Spielfelds (Grafikstil 7)
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.55f, 0.6f, 0.7f);
            QualitySettings.shadowDistance = 60f;

            Light light = null;
            foreach (var l in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
            {
                if (l.type == LightType.Directional) light = l;
            }
            if (light == null)
            {
                light = new GameObject("Sonne").AddComponent<Light>();
                light.type = LightType.Directional;
            }
            light.intensity = 1.15f;
            light.color = new Color(1f, 0.95f, 0.85f);
            light.shadows = LightShadows.Soft;
            light.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        }
    }
}
