using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MuhDefense.EditorTools
{
    /// <summary>
    /// Menüpunkt "Muh Defense > Android einrichten": stellt alle Projekt-Einstellungen
    /// für die Android-App ein, legt die Startszene an und wechselt die Plattform auf Android.
    /// </summary>
    public static class AndroidSetup
    {
        const string ScenePath = "Assets/Scenes/Main.unity";
        const string PackageName = "com.jonasklapps.muhdefense";

        [MenuItem("Muh Defense/Android einrichten")]
        public static void Configure()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            ApplySettings();
            CreateMainScene();

            bool switched = EditorUserBuildSettings.activeBuildTarget == BuildTarget.Android
                || EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
            ShowResult(switched);
        }

        /// <summary>
        /// Wird von GitHub Actions aufgerufen (game-ci, buildMethod): stellt alles ein und baut die APK nach build/Android/MuhDefense.apk.
        /// </summary>
        public static void BuildForCi()
        {
            ApplySettings();
            CreateMainScene();
            Directory.CreateDirectory("build/Android");
            var options = new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = "build/Android/MuhDefense.apk",
                target = BuildTarget.Android,
                options = BuildOptions.None,
            };
            EditorUserBuildSettings.buildAppBundle = false;
            var report = BuildPipeline.BuildPlayer(options);
            Debug.Log("Muh Defense Build: " + report.summary.result + ", " + report.summary.totalSize + " Bytes");
            if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded) EditorApplication.Exit(1);
        }

        static void ApplySettings()
        {
            // App-Name und Paketname (eindeutige Kennung im Play Store)
            PlayerSettings.productName = "Muh Defense";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, PackageName);

            // Nur Querformat
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;

            // Vom Google Play Store verlangt: IL2CPP und 64 Bit
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;

            PlayerSettings.bundleVersion = "0.1";
        }

        static void ShowResult(bool switched)
        {
            if (switched)
            {
                EditorUtility.DisplayDialog("Muh Defense",
                    "Android ist eingerichtet.\n\nHandy per USB anschließen (USB-Debugging an) und unter " +
                    "File > Build Profiles auf \"Build And Run\" klicken.", "OK");
            }
            else
            {
                EditorUtility.DisplayDialog("Muh Defense",
                    "Die Einstellungen sind gesetzt, aber die Plattform konnte nicht auf Android umgestellt werden.\n\n" +
                    "Bitte in Unity Hub unter Installs bei deiner Unity-Version das Modul \"Android Build Support\" " +
                    "(mit OpenJDK und Android SDK & NDK) installieren und den Menüpunkt danach erneut ausführen.", "OK");
            }
        }

        /// <summary>Legt eine Startszene an (das Level baut sich darin per Code auf) und trägt sie in die Build-Liste ein.</summary>
        static void CreateMainScene()
        {
            if (!File.Exists(ScenePath))
            {
                if (!AssetDatabase.IsValidFolder("Assets/Scenes")) AssetDatabase.CreateFolder("Assets", "Scenes");
                var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
                EditorSceneManager.SaveScene(scene, ScenePath);
            }
            else
            {
                EditorSceneManager.OpenScene(ScenePath);
            }
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        }
    }
}
