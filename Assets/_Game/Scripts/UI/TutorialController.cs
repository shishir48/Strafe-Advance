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
        private RectTransform _dragHint;
        private CanvasGroup   _dragHintGroup;

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
            AnimateDragHint();
            if (_activeIdx < 0 || _activeIdx >= _steps.Count) return;
            var step = _steps[_activeIdx];
            if (step.IsComplete != null && step.IsComplete()) NextStep();
        }

        // Finger/touch dot slides L↔R over the play area during the drag step only.
        void AnimateDragHint()
        {
            if (_dragHint == null) return;
            bool show = _running && _activeIdx == 0;
            if (_dragHintGroup != null) _dragHintGroup.alpha = show ? 1f : 0f;
            if (!show) return;
            float ping = Mathf.Sin(Time.unscaledTime * 2.2f); // -1..1
            var p = _dragHint.anchoredPosition; p.x = ping * 170f; _dragHint.anchoredPosition = p;
            _dragHint.localScale = Vector3.one * Mathf.Lerp(0.85f, 1f, 1f - Mathf.Abs(ping)); // press at center
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

            // Drag-hint touch dot — lives on the canvas (above play area), not the panel.
            var hint = new GameObject("DragHint");
            hint.transform.SetParent(canvasGO.transform, false);
            _dragHint = hint.AddComponent<RectTransform>();
            _dragHint.anchorMin = _dragHint.anchorMax = new Vector2(0.5f, 0.42f);
            _dragHint.pivot = new Vector2(0.5f, 0.5f);
            _dragHint.sizeDelta = new Vector2(110, 110);
            _dragHintGroup = hint.AddComponent<CanvasGroup>();
            _dragHintGroup.alpha = 0f;
            _dragHintGroup.blocksRaycasts = false;
            _dragHintGroup.interactable = false;
            var circle = MakeCircleSprite();
            var ring = new GameObject("Ring");
            ring.transform.SetParent(hint.transform, false);
            var ringRT = ring.AddComponent<RectTransform>();
            ringRT.anchorMin = Vector2.zero; ringRT.anchorMax = Vector2.one;
            ringRT.offsetMin = ringRT.offsetMax = Vector2.zero;
            var ringImg = ring.AddComponent<Image>();
            ringImg.sprite = circle; ringImg.color = new Color(0f, 0.9f, 1f, 0.35f); ringImg.raycastTarget = false;
            var dot = new GameObject("Dot");
            dot.transform.SetParent(hint.transform, false);
            var dotRT = dot.AddComponent<RectTransform>();
            dotRT.anchorMin = new Vector2(0.5f, 0.5f); dotRT.anchorMax = new Vector2(0.5f, 0.5f);
            dotRT.pivot = new Vector2(0.5f, 0.5f); dotRT.sizeDelta = new Vector2(56, 56);
            var dotImg = dot.AddComponent<Image>();
            dotImg.sprite = circle; dotImg.color = new Color(0f, 0.9f, 1f, 0.95f); dotImg.raycastTarget = false;

            // Step counter (top-left)
            _stepLabel = MakeText(panel.transform, "TUTORIAL 1/4", new Vector2(0, 1), new Vector2(0, 1), new Vector2(20, -10f), new Vector2(280, 30), 18, new Color(0.0f, 0.9f, 1.0f), TextAlignmentOptions.TopLeft);

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

        // One-off antialiased white circle sprite for the touch dot / ring.
        static Sprite MakeCircleSprite()
        {
            const int n = 64;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
            float r = n * 0.5f, c = r - 0.5f;
            var px = new Color32[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float d = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c));
                    float a = Mathf.Clamp01(r - d); // 1px soft edge
                    px[y * n + x] = new Color32(255, 255, 255, (byte)(a * 255));
                }
            tex.SetPixels32(px);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f);
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
