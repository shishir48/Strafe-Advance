using UnityEditor;
using UnityEngine;

namespace StrafAdvance.Editor
{
    public static class SwitchPlatform
    {
        public static void ToStandalone()
        {
            Debug.Log("[SwitchPlatform] Switching to Standalone Mac...");
            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Standalone, BuildTarget.StandaloneOSX);
            Debug.Log("[SwitchPlatform] Done. Restart Unity to apply.");
        }

        public static void ToAndroid()
        {
            Debug.Log("[SwitchPlatform] Switching to Android...");
            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
            Debug.Log("[SwitchPlatform] Done.");
        }
    }
}
