// Assets/_Game/Scripts/Editor/FontAssetCreator.cs
using TMPro;
using UnityEditor;
using UnityEngine;

namespace StrafAdvance.Editor
{
    public static class FontAssetCreator
    {
        [MenuItem("StrafAdvance/UI Polish/Generate Orbitron-Bold SDF", priority = 200)]
        public static void GenerateOrbitronSDF()
        {
            var ttf = AssetDatabase.LoadAssetAtPath<Font>(
                "Assets/TextMesh Pro/Fonts/Orbitron-Bold.ttf");
            if (ttf == null)
            {
                Debug.LogError("[FontAssetCreator] Orbitron-Bold.ttf not found.");
                return;
            }

            var fontAsset = TMP_FontAsset.CreateFontAsset(
                ttf,
                samplingPointSize: 90,
                padding: 9,
                renderMode: GlyphRenderMode.SDFAA,
                atlasWidth: 1024,
                atlasHeight: 1024);

            fontAsset.name = "Orbitron-Bold SDF";
            const string savePath = "Assets/TextMesh Pro/Resources/Fonts & Materials/Orbitron-Bold SDF.asset";
            AssetDatabase.CreateAsset(fontAsset, savePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // Set as TMP default font
            var settings = TMP_Settings.instance;
            if (settings != null)
            {
                SerializedObject so = new SerializedObject(settings);
                var prop = so.FindProperty("m_defaultFontAsset");
                if (prop != null)
                {
                    prop.objectReferenceValue = fontAsset;
                    so.ApplyModifiedProperties();
                    EditorUtility.SetDirty(settings);
                    AssetDatabase.SaveAssets();
                }
            }
            Debug.Log("[FontAssetCreator] Orbitron-Bold SDF generated and set as TMP default.");
        }
    }
}
