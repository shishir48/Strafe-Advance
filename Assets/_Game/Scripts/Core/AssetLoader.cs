using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace StrafAdvance
{
    /// <summary>
    /// Asset loading facade. Uses Addressables when the key is registered, falls back to
    /// Resources for legacy keys still living in <c>Assets/Resources</c>. Caches sync handles
    /// per key so hot-path callers (bullets, deaths) don't re-fault every spawn.
    ///
    /// Migration path: register assets in Addressables Groups (Window → Asset Management →
    /// Addressables → Groups) using the same string key as the legacy Resources path
    /// (e.g. <c>"VFX/HitSpark"</c>). Once registered, the call routes through Addressables.
    /// </summary>
    public static class AssetLoader
    {
        private static readonly Dictionary<string, Object> _cache = new Dictionary<string, Object>();

        /// <summary>Synchronous load. Caches the result. Returns null if neither source has the key.</summary>
        public static T Load<T>(string key) where T : Object
        {
            if (_cache.TryGetValue(key, out var cached)) return cached as T;

            T asset = null;
            try
            {
                AsyncOperationHandle<T> op = Addressables.LoadAssetAsync<T>(key);
                asset = op.WaitForCompletion();
            }
            catch
            {
                // Key not registered in Addressables — fall through to Resources.
            }

            if (asset == null) asset = Resources.Load<T>(key);
            if (asset != null) _cache[key] = asset;
            return asset;
        }

        /// <summary>Drop the cache. Call between scene loads if memory budget is tight.</summary>
        public static void ClearCache() => _cache.Clear();
    }
}
