using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace StrafAdvance
{
    /// <summary>
    /// Spawns floating "+N ◆" popups at enemy death positions so the player sees the currency drop
    /// they just earned. Subscribes <see cref="EnemyKilled"/>, queries <see cref="CurrencyService"/>
    /// for the drop amount, pools popups. Skips when amount &lt;= 0 or position is the origin
    /// (legacy publishers).
    /// </summary>
    public class CurrencyPopupSpawner : MonoBehaviour
    {
        public static CurrencyPopupSpawner Instance { get; private set; }

        private readonly Queue<CurrencyPopup> _pool = new Queue<CurrencyPopup>();
        private GameObject _prefab;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { Instance = null; }

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
            if (k.WorldPos == Vector3.zero) return;
            int amount = CurrencyService.Instance != null
                ? CurrencyService.Instance.DropFor(k.Type)
                : 0;
            if (amount <= 0) return;
            Spawn(amount, k.WorldPos);
        }

        public void Spawn(int amount, Vector3 worldPos)
        {
            if (!isActiveAndEnabled) return;
            CurrencyPopup p;
            if (_pool.Count > 0) p = _pool.Dequeue();
            else
            {
                var go = Instantiate(_prefab, transform);
                p = go.GetComponent<CurrencyPopup>();
            }
            p.Show(amount, worldPos + Random.insideUnitSphere * 0.25f);
            StartCoroutine(RecycleWhenInactive(p));
        }

        System.Collections.IEnumerator RecycleWhenInactive(CurrencyPopup p)
        {
            while (p != null && p.gameObject.activeSelf) yield return null;
            if (p != null) _pool.Enqueue(p);
        }

        static GameObject BuildPrefab()
        {
            var go = new GameObject("CurrencyPopup(Template)");
            go.SetActive(false);
            var tmp = go.AddComponent<TextMeshPro>();
            var defaultFont = TMP_Settings.defaultFontAsset;
            if (defaultFont != null) tmp.font = defaultFont;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontSize  = 4.8f;
            tmp.text      = "+0  ◆";
            tmp.color     = new Color(1f, 0.95f, 0.55f);
            tmp.fontStyle = FontStyles.Bold;
            tmp.richText  = true;
            if (defaultFont != null)
            {
                tmp.outlineWidth = 0.22f;
                tmp.outlineColor = Color.black;
            }
            go.AddComponent<CurrencyPopup>();
            DontDestroyOnLoad(go);
            return go;
        }
    }
}
