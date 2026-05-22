using UnityEditor;
using UnityEngine;

namespace StrafAdvance.Editor
{
    public static class BatchBuilder
    {
        // Called via: Unity -executeMethod StrafAdvance.Editor.BatchBuilder.BuildAndroid
        public static void BuildAndroid()
        {
            PlayerSettings.productName = "Strafe Advance";
            PlayerSettings.applicationIdentifier = "com.strafegame.advance";
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
            PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.Mono2x);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARMv7 | AndroidArchitecture.ARM64;

            var scenes = new[]
            {
                "Assets/_Game/Scenes/Bootstrap.unity",
                "Assets/_Game/Scenes/GameScene.unity"
            };

            string outputPath = "/Users/shishirsingh/StrafeAdvance.apk";

            var options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = outputPath,
                target = BuildTarget.Android,
                options = BuildOptions.None
            };

            Debug.Log($"[BatchBuilder] Building to {outputPath}");
            var report = BuildPipeline.BuildPlayer(options);
            var summary = report.summary;

            if (summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
                Debug.Log($"[BatchBuilder] SUCCESS: {outputPath}");
            else
            {
                Debug.LogError($"[BatchBuilder] FAILED: {summary.result}");
                EditorApplication.Exit(1);
            }
        }
    }
}
