using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace StrafAdvance
{
    public class NearDeathEffect : MonoBehaviour
    {
        public static NearDeathEffect Instance { get; private set; }

        [SerializeField] private float slowMoScale  = 0.15f;
        [SerializeField] private float hpThreshold  = 0.20f;
        [SerializeField] private float flashAlpha   = 0.50f;

        private bool          _triggered;
        private PlayerHealth  _playerHealth;
        private Image         _flashImage;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { Instance = null; }

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            if (Application.isPlaying) DontDestroyOnLoad(gameObject);
            EventBus<PlayerDamaged>.Subscribe(OnDamaged);
            EventBus<GameStateChanged>.Subscribe(OnStateChanged);
        }

        void OnDestroy()
        {
            EventBus<PlayerDamaged>.Unsubscribe(OnDamaged);
            EventBus<GameStateChanged>.Unsubscribe(OnStateChanged);
            if (Instance == this) Instance = null;
        }

        void Start()
        {
            _playerHealth = FindFirstObjectByType<PlayerHealth>();
            BuildFlashOverlay();
        }

        void BuildFlashOverlay()
        {
            var canvasGO = new GameObject("NearDeathCanvas");
            canvasGO.transform.SetParent(transform, false);
            var canvas        = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 50;
            canvasGO.AddComponent<CanvasScaler>();

            var imgGO = new GameObject("FlashImage");
            imgGO.transform.SetParent(canvasGO.transform, false);
            var rt      = imgGO.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            _flashImage               = imgGO.AddComponent<Image>();
            _flashImage.color         = new Color(1f, 1f, 1f, 0f);
            _flashImage.raycastTarget = false;
        }

        void OnStateChanged(GameStateChanged e)
        {
            if (e.Current == GameState.Playing)
            {
                StopAllCoroutines();
                Time.timeScale = 1f;
                if (_flashImage != null) _flashImage.color = new Color(1f, 1f, 1f, 0f);
                _triggered = false;
                _playerHealth = FindFirstObjectByType<PlayerHealth>();
            }
        }

        void OnDamaged(PlayerDamaged d)
        {
            if (_triggered || _playerHealth == null) return;

            float ratio = _playerHealth.MaxHp > 0
                ? (float)_playerHealth.CurrentHp / _playerHealth.MaxHp
                : 1f;

            if (ratio < hpThreshold)
            {
                _triggered = true;
                StartCoroutine(NearDeathRoutine());
            }
        }

        IEnumerator NearDeathRoutine()
        {
            // Wait for any active hitstop to finish before we set our own timeScale.
            yield return new WaitUntil(() => Mathf.Approximately(Time.timeScale, 1f) || Time.timeScale > 0.5f);

            float restoreScale = 1f;
            Time.timeScale = slowMoScale;

            // Fade in — 0.1s real time
            float t = 0f;
            while (t < 0.10f)
            {
                t += Time.unscaledDeltaTime;
                _flashImage.color = new Color(1f, 1f, 1f, Mathf.Lerp(0f, flashAlpha, t / 0.10f));
                yield return null;
            }

            // Fade out — 0.2s real time
            t = 0f;
            while (t < 0.20f)
            {
                t += Time.unscaledDeltaTime;
                _flashImage.color = new Color(1f, 1f, 1f, Mathf.Lerp(flashAlpha, 0f, t / 0.20f));
                yield return null;
            }

            _flashImage.color = new Color(1f, 1f, 1f, 0f);
            Time.timeScale = restoreScale;
        }
    }
}
