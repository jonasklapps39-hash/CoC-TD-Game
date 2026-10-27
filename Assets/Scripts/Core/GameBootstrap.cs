using UnityEngine;

namespace MuhDefense
{
    /// <summary>
    /// Startet den Prototyp automatisch, sobald in Unity "Play" gedrückt wird –
    /// in jeder Szene, die noch keinen <see cref="LevelManager"/> enthält.
    /// Kamera, Licht und Level werden komplett per Code aufgebaut, es sind also
    /// (noch) keine Szenen, Prefabs oder Assets nötig.
    /// </summary>
    public static class GameBootstrap
    {
        public const float CameraAngle = 55f;

        static GameObject levelRoot;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void AutoStart()
        {
            if (Object.FindFirstObjectByType<LevelManager>() != null) return;

            ConfigureDevice();
            SetupCameras();
            SetupLight();
            StartLevel();
        }

        /// <summary>Baut das Level (neu) auf. Wird auch für "Nochmal spielen" genutzt.</summary>
        public static void StartLevel()
        {
            if (levelRoot != null) Object.Destroy(levelRoot);
            levelRoot = new GameObject("MuhDefense Level");
            var level = levelRoot.AddComponent<LevelManager>();
            level.Init(Levels.Level1());
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
            foreach (var l in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
            {
                if (l.type == LightType.Directional) return;
            }
            var light = new GameObject("Sonne").AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.1f;
            light.shadows = LightShadows.Soft;
            light.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        }
    }
}
