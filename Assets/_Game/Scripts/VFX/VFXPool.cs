using System.Collections.Generic;
using UnityEngine;

namespace StrafAdvance
{
    public class VFXPool : MonoBehaviour
    {
        static VFXPool _instance;

        public static VFXPool Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("VFXPool");
                    _instance = go.AddComponent<VFXPool>();
                    if (Application.isPlaying) DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }

        readonly Dictionary<string, Queue<GameObject>> _pools = new();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { _instance = null; }

        void Awake()
        {
            if (_instance != null && _instance != this) { Destroy(gameObject); return; }
            _instance = this;
            if (Application.isPlaying) DontDestroyOnLoad(gameObject);
        }

        public GameObject Get(string key, Vector3 pos, Quaternion rot)
        {
            GameObject go = null;
            if (_pools.TryGetValue(key, out var queue))
            {
                // Discard pooled instances destroyed by a scene reload — this pool is DontDestroyOnLoad
                // and outlives the scene that owned them, so a Restart leaves dead refs in the queue.
                while (queue.Count > 0 && go == null)
                    go = queue.Dequeue();
            }
            if (go == null)
            {
                var prefab = AssetLoader.Load<GameObject>(key);
                if (prefab == null) return null;
                go = Instantiate(prefab);
                if (go.TryGetComponent<VFXAutoDestroy>(out var vad))
                    vad.PoolKey = key;
            }
            go.transform.SetPositionAndRotation(pos, rot);
            go.SetActive(true);
            return go;
        }

        public void Return(string key, GameObject go)
        {
            if (go == null) return;
            go.SetActive(false);
            if (!_pools.ContainsKey(key)) _pools[key] = new Queue<GameObject>();
            _pools[key].Enqueue(go);
        }
    }
}
