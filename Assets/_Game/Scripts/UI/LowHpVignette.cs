using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace StrafAdvance
{
    public class LowHpVignette : MonoBehaviour
    {
        public static LowHpVignette Instance { get; private set; }

        [SerializeField] private float hpThreshold   = 0.30f;
        [SerializeField] private float minIntensity  = 0.25f;
        [SerializeField] private float maxIntensity  = 0.55f;
        [SerializeField] private float minFreq       = 0.8f;   // Hz at threshold
        [SerializeField] private float maxFreq       = 2.0f;   // Hz near 0 HP
        [SerializeField] private float fadeOutSpeed  = 2.0f;   // intensity/second when HP > threshold

        private Vignette        _vignette;
        private PlayerHealth    _playerHealth;
        private VolumeProfile   _runtimeProfile;
        private float           _hpRatio = 1f;
        private float           _currentIntensity;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { Instance = null; }

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            if (Application.isPlaying) DontDestroyOnLoad(gameObject);
            EventBus<GameStateChanged>.Subscribe(OnStateChanged);
        }

        void Start()
        {
            _playerHealth = FindFirstObjectByType<PlayerHealth>();
            if (_playerHealth != null)
                _playerHealth.OnHealthChanged += OnHealthChanged;

            // Create a dedicated Volume so we never mutate shared scene Volume profiles.
            var volGO = new GameObject("LowHpVignetteVolume");
            volGO.transform.SetParent(transform);
            var vol       = volGO.AddComponent<Volume>();
            vol.isGlobal  = true;
            vol.priority  = 10f;                    // above default scene volume
            vol.profile   = ScriptableObject.CreateInstance<VolumeProfile>();
            _runtimeProfile = vol.profile;
            _vignette     = vol.profile.Add<Vignette>(overrides: true);
            _vignette.active = true;
            _vignette.intensity.Override(0f);
            _vignette.color.Override(Color.red);
            _vignette.rounded.Override(true);
        }

        void OnDestroy()
        {
            if (_playerHealth != null) _playerHealth.OnHealthChanged -= OnHealthChanged;
            EventBus<GameStateChanged>.Unsubscribe(OnStateChanged);
            if (_runtimeProfile != null) Destroy(_runtimeProfile);
            if (Instance == this) Instance = null;
        }

        void OnStateChanged(GameStateChanged e)
        {
            if (e.Current != GameState.Playing) return;
            if (_playerHealth != null) _playerHealth.OnHealthChanged -= OnHealthChanged;
            _playerHealth = FindFirstObjectByType<PlayerHealth>();
            if (_playerHealth != null) _playerHealth.OnHealthChanged += OnHealthChanged;
            _hpRatio = _playerHealth != null && _playerHealth.MaxHp > 0
                ? (float)_playerHealth.CurrentHp / _playerHealth.MaxHp
                : 1f;
        }

        void OnHealthChanged(int cur, int max)
            => _hpRatio = max > 0 ? (float)cur / max : 1f;

        void Update()
        {
            if (_vignette == null) return;

            if (_hpRatio < hpThreshold)
            {
                // t: 0 at threshold boundary, 1 at 0 HP
                float t    = 1f - (_hpRatio / hpThreshold);
                float freq = Mathf.Lerp(minFreq, maxFreq, t);
                // sin pulse: smooth 0..1 oscillation
                float pulse = (Mathf.Sin(Time.time * freq * Mathf.PI * 2f) + 1f) * 0.5f;
                _currentIntensity = Mathf.Lerp(minIntensity, maxIntensity, pulse);
            }
            else
            {
                _currentIntensity = Mathf.MoveTowards(
                    _currentIntensity, 0f, fadeOutSpeed * Time.deltaTime);
            }

            _vignette.intensity.Override(_currentIntensity);
        }
    }
}
