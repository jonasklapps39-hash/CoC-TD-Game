using System.Collections.Generic;
using UnityEngine;

namespace MuhDefense
{
    /// <summary>Grafikqualität: Schatten und Bildrate (für ältere Handys „Niedrig“).</summary>
    public static class GameSettings
    {
        public static int Graphics
        {
            get => Mathf.Clamp(PlayerPrefs.GetInt("grafik", 2), 0, 2);
            set
            {
                PlayerPrefs.SetInt("grafik", Mathf.Clamp(value, 0, 2));
                PlayerPrefs.Save();
                ApplyGraphics();
            }
        }

        public static readonly string[] GraphicsNames = { "Niedrig", "Mittel", "Hoch" };

        public static void ApplyGraphics()
        {
            int g = Graphics;
            Application.targetFrameRate = g == 0 ? 30 : 60;
            QualitySettings.shadowDistance = g == 0 ? 0f : g == 1 ? 35f : 60f;
            QualitySettings.antiAliasing = g == 2 ? 2 : 0;
            foreach (var l in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
            {
                if (l.type != LightType.Directional) continue;
                l.shadows = g == 0 ? LightShadows.None : g == 1 ? LightShadows.Hard : LightShadows.Soft;
            }
        }
    }

    /// <summary>Einstellungen-Fenster (vom Startbildschirm und vom Hof aus): Lautstärke, Grafik, Spielstand zurücksetzen, Infos.</summary>
    public class SettingsWindow
    {
        int resetStep;
        GUIStyle small;

        /// <returns>false, wenn das Fenster geschlossen werden soll.</returns>
        public bool Draw(List<Rect> blockers)
        {
            if (small == null) small = new GUIStyle(UiKit.Small) { fontSize = 13 };
            Rect box = UiKit.DrawOverlay(blockers, 640f, 560f);
            bool open = true;
            GUILayout.BeginArea(new Rect(box.x + 26f, box.y + 18f, box.width - 52f, box.height - 36f));
            GUILayout.BeginHorizontal();
            UiKit.Heading("Einstellungen", UiKit.Title, GUILayout.Height(40f));
            GUILayout.FlexibleSpace();
            if (UiKit.Btn("Fertig", UiKit.ActiveButton, GUILayout.Width(140f), GUILayout.Height(40f)))
            {
                GameAudio.SaveSettings();
                resetStep = 0;
                open = false;
            }
            GUILayout.EndHorizontal();
            GUILayout.Space(10f);

            // Lautstärke
            VolumeRow("Musik", GameAudio.MusicOn, v => GameAudio.MusicOn = v, GameAudio.MusicVolume, v => GameAudio.MusicVolume = v);
            VolumeRow("Geräusche", GameAudio.SoundOn, v => GameAudio.SoundOn = v, GameAudio.SoundVolume, v => GameAudio.SoundVolume = v);
            GUILayout.Space(8f);

            // Grafik
            GUILayout.BeginHorizontal();
            GUILayout.Label("Grafik", UiKit.Label, GUILayout.Width(130f), GUILayout.Height(40f));
            for (int g = 0; g < 3; g++)
            {
                if (UiKit.Btn(GameSettings.GraphicsNames[g], GameSettings.Graphics == g ? UiKit.ActiveButton : UiKit.Button, GUILayout.Height(40f)))
                {
                    GameSettings.Graphics = g;
                }
            }
            GUILayout.EndHorizontal();
            GUILayout.Label("„Niedrig“ schaltet Schatten aus und spart Akku auf älteren Handys.", small);
            GUILayout.Space(12f);

            // Spielstand zurücksetzen (mit doppelter Sicherheitsabfrage)
            if (resetStep == 0)
            {
                if (UiKit.Btn("Spielstand zurücksetzen …", UiKit.RedButton, GUILayout.Height(40f))) resetStep = 1;
            }
            else
            {
                GUILayout.Label(resetStep == 1
                    ? "Wirklich? Dein ganzer Hof, alle Sterne und Diamanten gehen verloren."
                    : "Letzte Warnung: Das kann nicht rückgängig gemacht werden!", UiKit.Label);
                GUILayout.BeginHorizontal();
                if (UiKit.Btn("Abbrechen", UiKit.Button, GUILayout.Height(40f))) resetStep = 0;
                if (UiKit.Btn(resetStep == 1 ? "Ja, zurücksetzen" : "Endgültig löschen", UiKit.RedButton, GUILayout.Height(40f)))
                {
                    if (resetStep == 1) resetStep = 2;
                    else
                    {
                        resetStep = 0;
                        GameFlow.ResetGame();
                        open = false;
                    }
                }
                GUILayout.EndHorizontal();
            }

            GUILayout.FlexibleSpace();
            GUILayout.Label("Muh Defense – ein Spiel von Jonas. Schrift „Luckiest Guy“ (Apache-2.0-Lizenz). " +
                            "Alle Figuren, Geräusche und Musik sind selbst gemacht.", small);
            GUILayout.EndArea();
            return open;
        }

        static void VolumeRow(string name, bool on, System.Action<bool> setOn, float volume, System.Action<float> setVolume)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(name, UiKit.Label, GUILayout.Width(130f), GUILayout.Height(40f));
            if (UiKit.Btn(on ? "An" : "Aus", on ? UiKit.ActiveButton : UiKit.Button, GUILayout.Width(80f), GUILayout.Height(40f))) setOn(!on);
            GUI.enabled = on;
            GUILayout.BeginVertical();
            GUILayout.Space(16f);
            float v = GUILayout.HorizontalSlider(volume, 0f, 1f, GUILayout.Height(20f));
            GUILayout.EndVertical();
            if (Mathf.Abs(v - volume) > 0.001f) setVolume(v);
            GUILayout.Label($"{Mathf.RoundToInt(volume * 100f)} %", UiKit.Label, GUILayout.Width(70f), GUILayout.Height(40f));
            GUI.enabled = true;
            GUILayout.EndHorizontal();
            GUILayout.Space(4f);
        }
    }
}
