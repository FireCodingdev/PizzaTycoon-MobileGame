#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.Build;

namespace PizzaTycoon.Editor
{
    // Configura PlayerSettings para Android e iOS via menu
    public static class BuildConfigurator
    {
        private const string BUNDLE_ID       = "com.seuestudio.pizzatycoon";
        private const string PRODUCT_NAME    = "Pizza Tycoon";
        private const string COMPANY_NAME    = "Seu Studio";
        private const string BUNDLE_VERSION  = "1.0.0";

        // ── Android ───────────────────────────────────────────────────────────

        [MenuItem("PizzaTycoon/Build/Configure Android", priority = 200)]
        public static void ConfigureAndroid()
        {
            EditorUserBuildSettings.SwitchActiveBuildTarget(
                BuildTargetGroup.Android, BuildTarget.Android);

            PlayerSettings.applicationIdentifier    = BUNDLE_ID;
            PlayerSettings.productName              = PRODUCT_NAME;
            PlayerSettings.companyName              = COMPANY_NAME;
            PlayerSettings.bundleVersion            = BUNDLE_VERSION;

            PlayerSettings.Android.bundleVersionCode = 1;
            PlayerSettings.Android.minSdkVersion     = AndroidSdkVersions.AndroidApiLevel24;
            PlayerSettings.Android.targetSdkVersion  = AndroidSdkVersions.AndroidApiLevel34;

#if UNITY_2022_1_OR_NEWER
            PlayerSettings.Android.targetArchitectures =
                AndroidArchitecture.ARM64 | AndroidArchitecture.ARMv7;
#endif

            PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android,
                ScriptingImplementation.IL2CPP);
            PlayerSettings.SetManagedStrippingLevel(BuildTargetGroup.Android,
                ManagedStrippingLevel.Medium);

            // Orientação portrait
            PlayerSettings.defaultInterfaceOrientation   = UIOrientation.Portrait;
            PlayerSettings.allowedAutorotateToPortrait   = true;
            PlayerSettings.allowedAutorotateToLandscapeLeft  = false;
            PlayerSettings.allowedAutorotateToLandscapeRight = false;

            // FPS manual
            QualitySettings.vSyncCount = 0;

            AssetDatabase.SaveAssets();
            Debug.Log("[BuildConfigurator] Android configurado com sucesso!");
            EditorUtility.DisplayDialog("Android Configurado",
                "PlayerSettings configurados para Android.\n\n" +
                "ATENÇÃO: Configure o Keystore em:\n" +
                "Player Settings → Android → Publishing Settings → Keystore", "OK");
        }

        // ── iOS ───────────────────────────────────────────────────────────────

        [MenuItem("PizzaTycoon/Build/Configure iOS", priority = 201)]
        public static void ConfigureiOS()
        {
            EditorUserBuildSettings.SwitchActiveBuildTarget(
                BuildTargetGroup.iOS, BuildTarget.iOS);

            PlayerSettings.applicationIdentifier = BUNDLE_ID;
            PlayerSettings.productName           = PRODUCT_NAME;
            PlayerSettings.companyName           = COMPANY_NAME;
            PlayerSettings.bundleVersion         = BUNDLE_VERSION;

            PlayerSettings.iOS.targetOSVersionString    = "13.0";
            PlayerSettings.iOS.requiresPersistentWiFi   = false;
            // API moderna substitui Allow HTTP Download em Unity 2022.2+
            PlayerSettings.insecureHttpOption           = InsecureHttpOption.NotAllowed;

            PlayerSettings.SetScriptingBackend(BuildTargetGroup.iOS,
                ScriptingImplementation.IL2CPP);
            PlayerSettings.SetManagedStrippingLevel(BuildTargetGroup.iOS,
                ManagedStrippingLevel.Medium);

            PlayerSettings.defaultInterfaceOrientation        = UIOrientation.Portrait;
            PlayerSettings.allowedAutorotateToLandscapeLeft   = false;
            PlayerSettings.allowedAutorotateToLandscapeRight  = false;

            QualitySettings.vSyncCount = 0;

            AssetDatabase.SaveAssets();
            Debug.Log("[BuildConfigurator] iOS configurado!");
            EditorUtility.DisplayDialog("iOS Configurado",
                "PlayerSettings configurados para iOS.\n\n" +
                "ATENÇÃO: Configure o Team ID e Provisioning Profile no Xcode.", "OK");
        }

        // ── Builds ────────────────────────────────────────────────────────────

        [MenuItem("PizzaTycoon/Build/Build Android APK", priority = 210)]
        public static void BuildAndroidAPK()
        {
            ConfigureAndroid();
            string path = "Builds/Android/PizzaTycoon.apk";
            System.IO.Directory.CreateDirectory("Builds/Android");

            BuildPlayerOptions options = new BuildPlayerOptions
            {
                scenes             = GetScenes(),
                locationPathName   = path,
                target             = BuildTarget.Android,
                targetGroup        = BuildTargetGroup.Android,
                options            = BuildOptions.None
            };

            var report = BuildPipeline.BuildPlayer(options);
            Debug.Log($"[Build] APK: {report.summary.result} — {path}");
        }

        [MenuItem("PizzaTycoon/Build/Build Android AAB", priority = 211)]
        public static void BuildAndroidAAB()
        {
            ConfigureAndroid();
            EditorUserBuildSettings.buildAppBundle = true;
            string path = "Builds/Android/PizzaTycoon.aab";
            System.IO.Directory.CreateDirectory("Builds/Android");

            BuildPlayerOptions options = new BuildPlayerOptions
            {
                scenes           = GetScenes(),
                locationPathName = path,
                target           = BuildTarget.Android,
                targetGroup      = BuildTargetGroup.Android,
                options          = BuildOptions.None
            };

            var report = BuildPipeline.BuildPlayer(options);
            EditorUserBuildSettings.buildAppBundle = false;
            Debug.Log($"[Build] AAB: {report.summary.result} — {path}");
        }

        [MenuItem("PizzaTycoon/Build/Build iOS Xcode Project", priority = 212)]
        public static void BuildiOSXcode()
        {
            ConfigureiOS();
            string path = "Builds/iOS";
            System.IO.Directory.CreateDirectory(path);

            BuildPlayerOptions options = new BuildPlayerOptions
            {
                scenes           = GetScenes(),
                locationPathName = path,
                target           = BuildTarget.iOS,
                targetGroup      = BuildTargetGroup.iOS,
                options          = BuildOptions.None
            };

            var report = BuildPipeline.BuildPlayer(options);
            Debug.Log($"[Build] iOS Xcode: {report.summary.result} — {path}");
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private static string[] GetScenes()
        {
            return new[]
            {
                "Assets/_Project/Scenes/SplashScene.unity",
                "Assets/_Project/Scenes/LoadingScene.unity",
                "Assets/_Project/Scenes/MainMenu.unity",
                "Assets/_Project/Scenes/GameScene.unity",
            };
        }
    }
}
#endif
