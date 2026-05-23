using UnityEngine;

namespace StrafAdvance
{
    /// <summary>
    /// Listens to <c>EnemyDamaged</c>-tracked death (via <c>EnemyKilled</c>) and rolls for a power-up drop.
    /// Spawns the PowerUp prefab at the kill position. Drop chance scales by enemy type.
    ///
    /// Wire the PowerUp prefab once in Inspector OR auto-load from Resources/PowerUp.
    /// </summary>
    public class PowerUpDropper : MonoBehaviour
    {
        public static PowerUpDropper Instance { get; private set; }

        [SerializeField] private GameObject powerUpPrefab;
        [Range(0f, 1f)] [SerializeField] private float gruntDropChance   = 0.05f;
        [Range(0f, 1f)] [SerializeField] private float flankerDropChance = 0.10f;
        [Range(0f, 1f)] [SerializeField] private float eliteDropChance   = 0.40f;
        [Range(0f, 1f)] [SerializeField] private float chargerDropChance = 0.10f;

        private Vector3 _lastDeathPos;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { Instance = null; }

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            if (Application.isPlaying) DontDestroyOnLoad(gameObject);
            EventBus<EnemyDamaged>.Subscribe(OnDamage);
            EventBus<EnemyKilled>.Subscribe(OnKilled);
        }

        void OnDestroy()
        {
            EventBus<EnemyDamaged>.Unsubscribe(OnDamage);
            EventBus<EnemyKilled>.Unsubscribe(OnKilled);
            if (Instance == this) Instance = null;
        }

        // EnemyKilled has no position field, so we cache it from the most recent damage event.
        void OnDamage(EnemyDamaged d) => _lastDeathPos = d.Position;

        void OnKilled(EnemyKilled k)
        {
            float chance = k.Type switch
            {
                EnemyType.Grunt   => gruntDropChance,
                EnemyType.Flanker => flankerDropChance,
                EnemyType.Elite   => eliteDropChance,
                EnemyType.Charger => chargerDropChance,
                _                 => 0f,
            };
            if (Random.value > chance) return;
            var prefab = powerUpPrefab != null ? powerUpPrefab : AssetLoader.Load<GameObject>("PowerUp");
            if (prefab == null) return;
            Instantiate(prefab, _lastDeathPos, Quaternion.identity);
        }
    }
}
