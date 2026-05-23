using System.IO;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

namespace StrafAdvance.Editor
{
    /// <summary>
    /// One-click bootstrap for Addressables: creates the AddressableAssetSettings asset (if missing),
    /// then registers every legacy <c>Assets/Resources</c> asset under the same key path so
    /// <see cref="AssetLoader"/> can route the load through Addressables instead of Resources.
    ///
    /// Run via <c>StrafAdvance &gt; 11. Bootstrap Addressables</c>.
    /// </summary>
    public static class AddressablesSetup
    {
        private const string ResourcesRoot = "Assets/Resources";

        [MenuItem("StrafAdvance/11. Bootstrap Addressables", priority = 110)]
        public static void Bootstrap()
        {
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
            {
                settings = AddressableAssetSettings.Create(
                    AddressableAssetSettingsDefaultObject.kDefaultConfigFolder,
                    AddressableAssetSettingsDefaultObject.kDefaultConfigAssetName,
                    true, true);
                AddressableAssetSettingsDefaultObject.Settings = settings;
                Debug.Log("[Addressables] Created AddressableAssetSettings.");
            }

            var group = settings.DefaultGroup;
            int added = 0, updated = 0;

            foreach (var path in EnumerateAssets(ResourcesRoot))
            {
                string guid = AssetDatabase.AssetPathToGUID(path);
                if (string.IsNullOrEmpty(guid)) continue;

                // Key = path under Resources/ minus extension, matching old Resources.Load("...") strings.
                string key = path.Substring(ResourcesRoot.Length + 1);
                key = Path.ChangeExtension(key, null).Replace('\\', '/');

                var entry = settings.FindAssetEntry(guid);
                if (entry == null)
                {
                    entry = settings.CreateOrMoveEntry(guid, group);
                    entry.address = key;
                    added++;
                }
                else if (entry.address != key)
                {
                    entry.address = key;
                    updated++;
                }
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[Addressables] Bootstrap done. Added {added}, updated {updated} entries in group '{group.Name}'.");
        }

        private static System.Collections.Generic.IEnumerable<string> EnumerateAssets(string root)
        {
            if (!AssetDatabase.IsValidFolder(root)) yield break;
            string[] guids = AssetDatabase.FindAssets("", new[] { root });
            foreach (var g in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(g);
                if (AssetDatabase.IsValidFolder(path)) continue;
                yield return path;
            }
        }
    }
}
