using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace StrafAdvance
{
    /// <summary>
    /// Runtime-built in-run HUD. Replaces the legacy HUDController's wave-label-only layout.
    /// Subscribes to EventBus for combo/score/damage and polls PlayerController for stamina/dodge.
    ///
    /// Anchors top-left: HP + stamina bars + dodge pip.
    /// Anchors top-center: wave label + combo widget.
    /// Anchors top-right: score with rolling tween.
    /// </summary>
    public class ModernHUD : MonoBehaviour
    {
        public static ModernHUD Instance { get; private set; }

        private Canvas _canvas;
        private Image _hpFill, _staminaFill, _dodgePip;
        private TMP_Text _waveLabel, _comboLabel, _scoreLabel;
        private PlayerController _player;
        private PlayerHealth _playerHealth;

        private int   _displayedScore;
        private float _scoreTween;
        private int      _prevMultiplier = 1;
        private Coroutine _popRoutine;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { Instance = null; }

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            if (Application.isPlaying) DontDestroyOnLoad(gameObject);
            BuildUI();
            EventBus<ComboChanged>.Subscribe(OnCombo);
            EventBus<WaveStarted>.Subscribe(OnWaveStarted);
        }

        void OnDestroy()
        {
            EventBus<ComboChanged>.Unsubscribe(OnCombo);
            EventBus<WaveStarted>.Unsubscribe(OnWaveStarted);
            if (Instance == this) Instance = null;
        }

        void Start()
        {
            _player       = FindFirstObjectByType<PlayerController>();
            _playerHealth = FindFirstObjectByType<PlayerHealth>();
            if (_playerHealth != null)
            {
                _playerHealth.OnHealthChanged += SetHp;
                SetHp(_playerHealth.CurrentHp, _playerHealth.MaxHp);
            }
            // Wire wave label off the live spawner (level may have any wave count).
            var spawner = FindFirstObjectByType<WaveSpawner>();
            if (spawner != null) spawner.OnWaveStarted += idx =>
            {
                int total = AssetLoader.Load<LevelConfig>("Level1")?.waves?.Length ?? 0;
                _waveLabel.text = $"WAVE {idx + 1}/{total}";
            };
        }

        void Update()
        {
            // Stamina + dodge polled (no event for continuous values).
            if (_player != null)
            {
                if (_staminaFill != null) _staminaFill.fillAmount = _player.StaminaT;
                if (_dodgePip != null)
                {
                    var c = _dodgePip.color;
                    c.a = _player.DodgeReady ? 1f : 0.3f;
                    _dodgePip.color = c;
                }
            }
            // Score tween toward GM.Score
            if (GameManager.Instance != null)
            {
                int target = GameManager.Instance.Score;
                if (_displayedScore != target)
                {
                    _scoreTween = Mathf.MoveTowards(_scoreTween, target, (target - _displayedScore) * 6f * Time.deltaTime + 4f);
                    _displayedScore = Mathf.RoundToInt(_scoreTween);
                    if (_scoreLabel != null) _scoreLabel.text = $"SCORE  {_displayedScore:000000}";
                }
            }
        }

        void SetHp(int cur, int max)
        {
            if (_hpFill == null || max <= 0) return;
            _hpFill.fillAmount = (float)cur / max;
        }

        void OnCombo(ComboChanged c)
        {
            if (_comboLabel == null) return;

            if (c.Streak == 0)
            {
                _comboLabel.text = "";
                _prevMultiplier  = 1;
                return;
            }

            _comboLabel.text = c.Multiplier > 1
                ? $"<color=#ffd166>×{c.Multiplier}</color>  <size=22>x{c.Streak}</size>"
                : $"<size=22>x{c.Streak}</size>";

            if (c.Multiplier > _prevMultiplier)
            {
                if (_popRoutine != null) StopCoroutine(_popRoutine);
                _popRoutine = StartCoroutine(ComboPopRoutine());
            }

            _prevMultiplier = c.Multiplier;
        }

        void OnWaveStarted(WaveStarted w)
        {
            if (_waveLabel == null) return;
            _waveLabel.text = $"WAVE {w.Index + 1}/{w.Total}";
        }

        IEnumerator ComboPopRoutine()
        {
            Transform t     = _comboLabel.transform;
            float     upT   = 0.08f;
            float     downT = 0.12f;

            float elapsed = 0f;
            while (elapsed < upT)
            {
                elapsed += Time.unscaledDeltaTime;
                float s = Mathf.Lerp(1f, 1.5f, elapsed / upT);
                t.localScale = new Vector3(s, s, 1f);
                yield return null;
            }

            elapsed = 0f;
            while (elapsed < downT)
            {
                elapsed += Time.unscaledDeltaTime;
                float s = Mathf.Lerp(1.5f, 1f, elapsed / downT);
                t.localScale = new Vector3(s, s, 1f);
                yield return null;
            }

            t.localScale = Vector3.one;
            _popRoutine  = null;
        }

        // ─── UI Construction ────────────────────────────────────────────────────

        void BuildUI()
        {
            var go = new GameObject("ModernHUDCanvas");
            go.transform.SetParent(transform, false);
            _canvas = go.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 20;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            go.AddComponent<GraphicRaycaster>();

            // ── Top-left: HP + stamina + dodge pip ─────────────────────────────
            _hpFill      = MakeBar(go.transform, "HP",      new Vector2(0, 1), new Vector2(30, -30),  new Vector2(360, 26), new Color(0.31f, 0.76f, 0.97f));
            _staminaFill = MakeBar(go.transform, "Stamina", new Vector2(0, 1), new Vector2(30, -70),  new Vector2(280, 14), new Color(0.85f, 0.95f, 0.4f));
            _dodgePip    = MakePip(go.transform, new Vector2(0, 1), new Vector2(330, -70), new Vector2(22, 22), new Color(0.4f, 1f, 0.6f));

            // ── Top-center: wave + combo ───────────────────────────────────────
            _waveLabel   = MakeLabel(go.transform, "Wave",  new Vector2(0.5f, 1), new Vector2(0, -28), 44, new Color(0.31f, 0.76f, 0.97f));
            _comboLabel  = MakeLabel(go.transform, "Combo", new Vector2(0.5f, 1), new Vector2(0, -80), 40, Color.white);

            // ── Top-right: score ────────────────────────────────────────────────
            _scoreLabel  = MakeLabel(go.transform, "Score", new Vector2(1, 1), new Vector2(-30, -30), 32, Color.white);
            _scoreLabel.alignment = TextAlignmentOptions.Right;
            var rt = _scoreLabel.rectTransform;
            rt.pivot = new Vector2(1, 1);
        }

        static Image MakeBar(Transform parent, string name, Vector2 anchor, Vector2 pos, Vector2 size, Color fill)
        {
            var bg = new GameObject(name + "Bg");
            bg.transform.SetParent(parent, false);
            var bgRT = bg.AddComponent<RectTransform>();
            bgRT.anchorMin = anchor; bgRT.anchorMax = anchor;
            bgRT.pivot = new Vector2(0, 1);
            bgRT.anchoredPosition = pos; bgRT.sizeDelta = size;
            var bgImg = bg.AddComponent<Image>();
            bgImg.color = new Color(0.02f, 0.05f, 0.10f, 0.85f);

            var fillGO = new GameObject(name + "Fill");
            fillGO.transform.SetParent(bg.transform, false);
            var fillRT = fillGO.AddComponent<RectTransform>();
            fillRT.anchorMin = Vector2.zero; fillRT.anchorMax = Vector2.one;
            fillRT.offsetMin = new Vector2(2, 2); fillRT.offsetMax = new Vector2(-2, -2);
            var img = fillGO.AddComponent<Image>();
            img.type = Image.Type.Filled;
            img.fillMethod = Image.FillMethod.Horizontal;
            img.color = fill;
            img.fillAmount = 1f;
            return img;
        }

        static Image MakePip(Transform parent, Vector2 anchor, Vector2 pos, Vector2 size, Color color)
        {
            var go = new GameObject("DodgePip");
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = anchor; rt.anchorMax = anchor;
            rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = pos; rt.sizeDelta = size;
            var img = go.AddComponent<Image>();
            img.color = color;
            return img;
        }

        static TMP_Text MakeLabel(Transform parent, string name, Vector2 anchor, Vector2 pos, float size, Color color)
        {
            var go = new GameObject(name + "Label");
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = anchor; rt.anchorMax = anchor;
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = pos; rt.sizeDelta = new Vector2(600, 60);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            var defaultFont = TMP_Settings.defaultFontAsset;
            if (defaultFont != null) tmp.font = defaultFont;
            tmp.text = "";
            tmp.fontSize = size;
            tmp.color = color;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.richText = true;
            return tmp;
        }
    }
}
