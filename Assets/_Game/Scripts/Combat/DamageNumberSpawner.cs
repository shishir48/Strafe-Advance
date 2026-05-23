using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace StrafAdvance
{
    /// <summary>
    /// Singleton spawner for floating damage numbers. Builds its own prefab at runtime
    /// (no Inspector wiring required) and pools instances. Listens to <c>EventBus&lt;EnemyDamaged&gt;</c>.
    /// </summary>
    public class DamageNumberSpawner : MonoBehaviour
    {
        public static DamageNumberSpawner Instance { get; private set; }
        private readonly Queue<DamageNumber> _pool = new Queue<DamageNumber>();
        private GameObject _prefab;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { Instance = null; }

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            if (Application.isPlaying) DontDestroyOnLoad(gameObject);
            _prefab = BuildPrefab();
            EventBus<EnemyDamaged>.Subscribe(OnEnemyDamaged);
        }

        void OnDestroy()
        {
            EventBus<EnemyDamaged>.Unsubscribe(OnEnemyDamaged);
            if (Instance == this) Instance = null;
        }

        void OnEnemyDamaged(EnemyDamaged msg)
        {
            var color = msg.IsCritical
                ? new Color(1f, 0.85f, 0.2f)        // gold for crits
                : new Color(0.95f, 0.95f, 0.95f);   // white otherwise
            Spawn(msg.Amount, msg.Position, color);
        }

        public void Spawn(int amount, Vector3 worldPos, Color color)
        {
            if (!isActiveAndEnabled) return;
            DamageNumber n;
            if (_pool.Count > 0) n = _pool.Dequeue();
            else
            {
                var go = Instantiate(_prefab, transform);
                n = go.GetComponent<DamageNumber>();
            }
            n.Show(amount, worldPos + Random.insideUnitSphere * 0.2f, color);
            // Recycle: when GO becomes inactive again, requeue. Cheap polling via coroutine-less approach.
            StartCoroutine(RecycleWhenInactive(n));
        }

        System.Collections.IEnumerator RecycleWhenInactive(DamageNumber n)
        {
            while (n != null && n.gameObject.activeSelf) yield return null;
            if (n != null) _pool.Enqueue(n);
        }

        /// <summary>Builds a TMP-driven damage-number prefab at runtime so no scene wiring is needed.</summary>
        static GameObject BuildPrefab()
        {
            var go = new GameObject("DamageNumber(Template)");
            go.SetActive(false);
            var tmp = go.AddComponent<TextMeshPro>();
            // Assign the TMP Settings default font asset so outline/material lookups don't NRE.
            var defaultFont = TMP_Settings.defaultFontAsset;
            if (defaultFont != null) tmp.font = defaultFont;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontSize  = 6f;
            tmp.text      = "0";
            tmp.fontStyle = FontStyles.Bold;
            if (defaultFont != null)
            {
                tmp.outlineWidth = 0.25f;
                tmp.outlineColor = Color.black;
            }

            go.AddComponent<DamageNumber>();
            DontDestroyOnLoad(go);
            return go;
        }
    }

    /// <summary>Published whenever an enemy or the player takes damage.</summary>
    public readonly struct EnemyDamaged
    {
        public readonly Vector3 Position;
        public readonly int     Amount;
        public readonly bool    IsCritical;
        public EnemyDamaged(Vector3 position, int amount, bool isCritical)
        { Position = position; Amount = amount; IsCritical = isCritical; }
    }
}
