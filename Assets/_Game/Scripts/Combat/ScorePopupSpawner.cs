using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace StrafAdvance
{
    public class ScorePopupSpawner : MonoBehaviour
    {
        public static ScorePopupSpawner Instance { get; private set; }

        private readonly Queue<ScorePopup> _pool = new Queue<ScorePopup>();
        private GameObject _prefab;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { Instance = null; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Bootstrap()
        {
            if (Instance != null) return;
            var go = new GameObject("ScorePopupSpawner");
            go.AddComponent<ScorePopupSpawner>();
        }

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            if (Application.isPlaying) DontDestroyOnLoad(gameObject);
            _prefab = BuildPrefab();
            EventBus<EnemyKilled>.Subscribe(OnKill);
        }

        void OnDestroy()
        {
            EventBus<EnemyKilled>.Unsubscribe(OnKill);
            if (Instance == this) Instance = null;
        }

        void OnKill(EnemyKilled k)
        {
            if (k.WorldPos == Vector3.zero || k.ScoreReward <= 0) return;
            Spawn(k.ScoreReward, k.WorldPos + Vector3.up * 0.5f);
        }

        public void Spawn(int amount, Vector3 worldPos)
        {
            if (!isActiveAndEnabled) return;
            ScorePopup p = _pool.Count > 0 ? _pool.Dequeue() : Instantiate(_prefab, transform).GetComponent<ScorePopup>();
            p.Show(amount, worldPos + Random.insideUnitSphere * 0.15f);
            StartCoroutine(RecycleWhenInactive(p));
        }

        IEnumerator RecycleWhenInactive(ScorePopup p)
        {
            while (p != null && p.gameObject.activeSelf) yield return null;
            if (p != null) _pool.Enqueue(p);
        }

        static GameObject BuildPrefab()
        {
            var go = new GameObject("ScorePopup(Template)");
            go.SetActive(false);
            var tmp = go.AddComponent<TextMeshPro>();
            var defaultFont = TMP_Settings.defaultFontAsset;
            if (defaultFont != null) tmp.font = defaultFont;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontSize  = 5.2f;
            tmp.text      = "+0";
            tmp.color     = new Color(1f, 1f, 1f, 0f);
            tmp.fontStyle = FontStyles.Bold;
            if (defaultFont != null) { tmp.outlineWidth = 0.22f; tmp.outlineColor = Color.black; }
            go.AddComponent<ScorePopup>();
            DontDestroyOnLoad(go);
            return go;
        }
    }

    public class ScorePopup : MonoBehaviour
    {
        private TextMeshPro _tmp;

        void Awake() => _tmp = GetComponent<TextMeshPro>();

        public void Show(int amount, Vector3 worldPos)
        {
            transform.position = worldPos;
            StopAllCoroutines();
            gameObject.SetActive(true);
            _tmp.text = $"+{amount}";
            StartCoroutine(Float());
        }

        IEnumerator Float()
        {
            const float dur = 0.9f;
            Vector3 start = transform.position;
            for (float t = 0; t < dur; t += Time.deltaTime)
            {
                float p = t / dur;
                transform.position = start + Vector3.up * (1.8f * p);
                var c = _tmp.color;
                c.a = p < 0.1f ? p / 0.1f : (p < 0.6f ? 1f : 1f - ((p - 0.6f) / 0.4f));
                _tmp.color = c;
                yield return null;
            }
            gameObject.SetActive(false);
        }
    }
}
