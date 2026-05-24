using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace StrafAdvance
{
    /// <summary>
    /// First-run tutorial driver. Four steps (Strafe → Sprint → Dodge → Combo) gated on the
    /// player actually doing the action; advances on success, persists completion in
    /// <see cref="PlayerProfile.tutorialCompleted"/> so it only fires once.
    ///
    /// Skip button always available; Reset Tutorial in Settings re-arms it.
    /// Pure runtime UI — no scene prefabs required.
    /// </summary>
    public class TutorialController : MonoBehaviour
    {
        public static TutorialController Instance { get; private set; }

        private GameObject _root;
        private TMP_Text _promptLabel;
        private TMP_Text _stepLabel;
        private CanvasGroup _panelGroup;
        private RectTransform _panelRT;

        private readonly List<Step> _steps = new List<Step>();
        private int _activeIdx = -1;
        private float _stepStartedAt;
        private float _slideT;
        private bool _running;

        // Per-step state captured at activation (e.g. player x at start for drag detection).
        private float _playerStartX;
        private bool _dodgeSeen;
        private bool _comboHit;

        private PlayerController _player;

        // ─── Step Definition ────────────────────────────────────────────────────

        class Step
        {
            public string Prompt;
            public Action  OnEnter;
            public Func<bool> IsComplete;
        }

        // ─── Lifecycle ──────────────────────────────────────────────────────────

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { Instance = null; }

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            if (Application.isPlaying) DontDestroyOnLoad(gameObject);
            BuildUI();
            BuildSteps();
            EventBus<GameStateChanged>.Subscribe(OnState);
            EventBus<DodgePerformed>.Subscribe(_ => _dodgeSeen = true);
            EventBus<ComboChanged>.Subscribe(c => { if (c.Multiplier >= 2) _comboHit = true; });
            Hide();
        }

        void OnDestroy()
        {
            EventBus<GameStateChanged>.Unsubscribe(OnState);
            if (Instance == this) Instance = null;
        }

        void OnState(GameStateChanged e)
        {
            if (e.Current != GameState.Playing) { Hide(); return; }
            if (SaveSystem.Current.profile.tutorialCompleted) return;
            BeginTutorial();
        }

        void Update()
        {
            if (!_running) return;
            AnimatePanel();
            if (_activeIdx < 0 || _activeIdx >= _steps.Count) return;
            var step = _steps[_activeIdx];
            if (step.IsComplete != null && step.IsComplete()) NextStep();
        }

        // ─── Public API ─────────────────────────────────────────────────────────

        /// <summary>Re-arm tutorial — used by Settings "Reset Tutorial".</summary>
        public void ResetAndArm()
        {
            SaveSystem.Current.profile.tutorialCompleted = false;
            SaveSystem.Save();
            if (GameManager.Instance != null && GameManager.Instance.State == GameState.Playing)
                BeginTutorial();
        }

        public void Skip()
        {
            _running = false;
            SaveSystem.Current.profile.tutorialCompleted = true;
            SaveSystem.Save();
            Hide();
        }

        // ─── Flow ───────────────────────────────────────────────────────────────

        void BeginTutorial()
        {
            _player = FindAnyObjectByType<PlayerController>();
            if (_player == null) return;
            _running = true;
            _activeIdx = -1;
            NextStep();
        }

        void NextStep()
        {
            _activeIdx++;
            if (_activeIdx >= _steps.Count) { Complete(); return; }
            var step = _steps[_activeIdx];
            step.OnEnter?.Invoke();
            _stepStartedAt = Time.time;
            _slideT = 0f;
            _promptLabel.text = step.Prompt;
            _stepLabel.text   = $"TUTORIAL  {_activeIdx + 1}/{_steps.Count}";
            Show();
        }

        void Complete()
        {
            _running = false;
            SaveSystem.Current.profile.tutorialCompleted = true;
            SaveSystem.Save();
            Hide();
        }

        void BuildSteps()
        {
            _steps.Clear();

            // 1. Strafe — drag left/right
            _steps.Add(new Step
            {
                Prompt    = "DRAG  ◄ ►  to STRAFE",
                OnEnter   = () => { if (_player != null) _playerStartX = _player.transform.position.x; },
                IsComplete= () => _player != null && Mathf.Abs(_player.transform.position.x - _playerStartX) > 1.0f && Time.time - _stepStartedAt > 0.4f,
            });

            // 2. Sprint — hold sprint
            _steps.Add(new Step
            {
                Prompt    = "HOLD  ▲  to SPRINT\n<size=22>(2-finger touch on mobile)</size>",
                OnEnter   = () => { },
                IsComplete= () => _player != null && _player.IsSprinting,
            });

            // 3. Dodge — perform dodge
            _steps.Add(new Step
            {
                Prompt    = "DOUBLE-TAP or press DODGE\n<size=22>brief invincibility frames</size>",
                OnEnter   = () => _dodgeSeen = false,
                IsComplete= () => _dodgeSeen,
            });

            // 4. Combo — chain 2x multiplier
            _steps.Add(new Step
            {
                Prompt    = "CHAIN kills for COMBO  ×2 → ×8\n<size=22>more multiplier = more score</size>",
                OnEnter   = () => _comboHit = false,
                IsComplete= () => _comboHit,
            });
        }

        void AnimatePanel()
        {
            // Slide-up + fade-in over 0.35s after each step swap.
            _slideT = Mathf.MoveTowards(_slideT, 1f, Time.unscaledDeltaTime / 0.35f);
            float ease = 1f - (1f - _slideT) * (1f - _slideT); // ease-out
            if (_panelGroup != null) _panelGroup.alpha = ease;
            if (_panelRT != null)
            {
                var pos = _panelRT.anchoredPosition;
                pos.y = Mathf.Lerp(-60f, 30f, ease);
                _panelRT.anchoredPosition = pos;
            }
        }

        void Show() { if (_root != null) _root.SetActive(true); }
        void Hide() { if (_root != null) _root.SetActive(false); }

        // ─── UI Construction ────────────────────────────────────────────────────

        void BuildUI()
        {
            var canvasGO = new GameObject("TutorialCanvas");
            canvasGO.transform.SetParent(transform, false);
            var c = canvasGO.AddComponent<Canvas>();
            c.renderMode = RenderMode.ScreenSpaceOverlay;
            c.sortingOrder = 45; // above HUD (20), below PauseMenu (50)
            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            canvasGO.AddComponent<GraphicRaycaster>();
            _root = canvasGO;

            // Panel anchored bottom-center
            var panel = new GameObject("Panel");
            panel.transform.SetParent(canvasGO.transform, false);
            _panelRT = panel.AddComponent<RectTransform>();
            _panelRT.anchorMin = new Vector2(0.05f, 0f); _panelRT.anchorMax = new Vector2(0.95f, 0f);
            _panelRT.pivot = new Vector2(0.5f, 0f);
            _panelRT.anchoredPosition = new Vector2(0, 30f);
            _panelRT.sizeDelta = new Vector2(-40f, 240f);
            var bg = panel.AddComponent<Image>();
            bg.color = new Color(0.02f, 0.05f, 0.10f, 0.92f);
            _panelGroup = panel.AddComponent<CanvasGroup>();
            _panelGroup.blocksRaycasts = false; // never blocks gameplay input

            // Step counter (top-left)
            _stepLabel = MakeText(panel.transform, "TUTORIAL 1/4", new Vector2(0, 1), new Vector2(0, 1), new Vector2(20, -10f), new Vector2(280, 30), 18, new Color(0.31f, 0.76f, 0.97f), TextAlignmentOptions.TopLeft);

            // Big prompt (center)
            _promptLabel = MakeText(panel.transform, "", new Vector2(0, 0), new Vector2(1, 1), new Vector2(20, 60), new Vector2(-40, -20), 38, Color.white, TextAlignmentOptions.Center);
            _promptLabel.fontStyle = FontStyles.Bold;

            // Skip button (bottom-right, never blocks gameplay because parent disables raycast)
            var skip = new GameObject("Skip");
            skip.transform.SetParent(panel.transform, false);
            var srt = skip.AddComponent<RectTransform>();
            srt.anchorMin = new Vector2(1, 0); srt.anchorMax = new Vector2(1, 0);
            srt.pivot = new Vector2(1, 0);
            srt.anchoredPosition = new Vector2(-15, 15);
            srt.sizeDelta = new Vector2(180, 50);
            var simg = skip.AddComponent<Image>();
            simg.color = new Color(0.10f, 0.10f, 0.14f, 0.95f);
            var btn = skip.AddComponent<Button>();
            btn.targetGraphic = simg;
            btn.onClick.AddListener(Skip);
            // Re-enable raycast on the skip button only.
            var skipGroup = skip.AddComponent<CanvasGroup>();
            skipGroup.blocksRaycasts = true;
            skipGroup.interactable = true;
            var skipLbl = MakeText(skip.transform, "SKIP", new Vector2(0, 0), new Vector2(1, 1), Vector2.zero, Vector2.zero, 22, new Color(0.7f, 0.8f, 0.95f), TextAlignmentOptions.Center);
            skipLbl.rectTransform.anchorMin = Vector2.zero; skipLbl.rectTransform.anchorMax = Vector2.one;
            skipLbl.rectTransform.offsetMin = skipLbl.rectTransform.offsetMax = Vector2.zero;
        }

        static TMP_Text MakeText(Transform parent, string text, Vector2 aMin, Vector2 aMax, Vector2 pos, Vector2 size, float fontSize, Color color, TextAlignmentOptions align)
        {
            var go = new GameObject("T");
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = aMin; rt.anchorMax = aMax;
            if (aMin == aMax)
            {
                rt.anchoredPosition = pos;
                rt.sizeDelta = size;
            }
            else
            {
                rt.offsetMin = pos;
                rt.offsetMax = size;
            }
            var tmp = go.AddComponent<TextMeshProUGUI>();
            var font = TMP_Settings.defaultFontAsset;
            if (font != null) tmp.font = font;
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.color = color;
            tmp.alignment = align;
            tmp.richText = true;
            return tmp;
        }
    }
}
