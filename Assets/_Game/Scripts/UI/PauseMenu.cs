using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace StrafAdvance
{
    /// <summary>
    /// Runtime-built pause menu. Toggled by Esc / Start button / two-finger long-press.
    /// Pauses Time.timeScale to 0; KillCam/Hitstop restore base on their own routines.
    /// Buttons: Resume / Restart / Perks / Quit-to-Menu.
    /// </summary>
    public class PauseMenu : MonoBehaviour
    {
        public static PauseMenu Instance { get; private set; }

        private Canvas _canvas;
        private GameObject _panel;
        private bool _open;
        private float _savedScale = 1f;
        private CanvasGroup   _dimGroup;
        private Coroutine     _dimTween;
        private RectTransform _panelRT;
        private System.Action<LanguageChanged> _onLangChanged;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { Instance = null; }

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            if (Application.isPlaying) DontDestroyOnLoad(gameObject);
            BuildUI();
            Close();
            _onLangChanged = _ => Rebuild();
            EventBus<LanguageChanged>.Subscribe(_onLangChanged);
        }

        void Rebuild()
        {
            bool wasOpen = _open;
            if (_canvas != null) Destroy(_canvas.gameObject);
            BuildUI();
            if (wasOpen) Open(); else Close();
        }

        void Update()
        {
            // Toggle on Esc (keyboard) or Start (gamepad).
            bool toggle =
                (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) ||
                (Gamepad.current  != null && Gamepad.current.startButton.wasPressedThisFrame);
            if (toggle) Toggle();
        }

        void OnDestroy()
        {
            EventBus<LanguageChanged>.Unsubscribe(_onLangChanged);
            if (Instance == this) Instance = null;
        }

        public void Toggle() { if (_open) Resume(); else Open(); }

        public void Open()
        {
            if (_open) return;
            _open = true;
            _savedScale = Time.timeScale > 0f ? Time.timeScale : 1f;
            Time.timeScale = 0f;
            if (_canvas != null) _canvas.gameObject.SetActive(true);
            if (_panel != null)
            {
                _panel.SetActive(true);
                if (_panelRT == null) _panelRT = _panel.GetComponent<RectTransform>();
                UITransition.SlideIn(this, _panelRT, new Vector2(0f, -600f));
            }
            if (_dimTween != null) StopCoroutine(_dimTween);
            _dimTween = StartCoroutine(FadeDim(0f, 1f, 0.15f));
        }

        IEnumerator FadeDim(float from, float to, float duration)
        {
            if (_dimGroup == null) yield break;
            if (duration <= 0f) { _dimGroup.alpha = to; _dimTween = null; yield break; }
            _dimGroup.alpha = from;
            for (float t = 0; t < duration; t += Time.unscaledDeltaTime)
            {
                _dimGroup.alpha = Mathf.Lerp(from, to, t / duration);
                yield return null;
            }
            _dimGroup.alpha = to;
            _dimTween = null;
        }

        public void Resume()
        {
            if (!_open) return;
            _open = false;
            Time.timeScale = _savedScale;
            // Hide the whole canvas — full-screen dim Image would otherwise eat clicks on the MainHub / HUD beneath us.
            if (_canvas != null) _canvas.gameObject.SetActive(false);
            if (_panel != null)  _panel.SetActive(false);
            if (_dimTween != null) { StopCoroutine(_dimTween); _dimTween = null; }
            if (_dimGroup != null) _dimGroup.alpha = 0f;
        }

        void RestartLevel()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        void QuitToMenu()
        {
            Time.timeScale = 1f;
            // Bootstrap scene = build index 0
            if (SceneManager.sceneCountInBuildSettings > 1) SceneManager.LoadScene(0);
            else SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        void OpenPerks()
        {
            PerkEquipPanel.Instance?.Open();
        }

        // ─── UI Construction ────────────────────────────────────────────────────

        void BuildUI()
        {
            var canvasGO = new GameObject("PauseMenuCanvas");
            canvasGO.transform.SetParent(transform, false);
            _canvas = canvasGO.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 60; // above HUD
            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            canvasGO.AddComponent<GraphicRaycaster>();

            // Dim background
            var dim = new GameObject("Dim");
            dim.transform.SetParent(canvasGO.transform, false);
            var dimRT = dim.AddComponent<RectTransform>();
            dimRT.anchorMin = Vector2.zero; dimRT.anchorMax = Vector2.one;
            dimRT.offsetMin = Vector2.zero; dimRT.offsetMax = Vector2.zero;
            var dimImg = dim.AddComponent<Image>();
            dimImg.color = new Color(0f, 0f, 0f, 0.6f);
            dimImg.raycastTarget = true;
            _dimGroup = dim.AddComponent<CanvasGroup>();
            _dimGroup.alpha = 0f;
            _dimGroup.blocksRaycasts = true;
            _dimGroup.interactable = false;

            _panel = new GameObject("Panel");
            _panel.transform.SetParent(canvasGO.transform, false);
            var prt = _panel.AddComponent<RectTransform>();
            prt.anchorMin = new Vector2(0.15f, 0.25f);
            prt.anchorMax = new Vector2(0.85f, 0.75f);
            prt.offsetMin = prt.offsetMax = Vector2.zero;
            var bg = _panel.AddComponent<Image>();
            bg.color = new Color(0.05f, 0.08f, 0.18f, 0.95f);

            MakeTitle(_panel.transform, Loc.Tr("pause.title"));
            float y = -120f, gap = 95f;
            MakeButton(_panel.transform, Loc.Tr("pause.resume"),   y, Resume);        y -= gap;
            MakeButton(_panel.transform, Loc.Tr("pause.perks"),    y, OpenPerks);     y -= gap;
            MakeButton(_panel.transform, Loc.Tr("pause.settings"), y, OpenSettings);  y -= gap;
            MakeButton(_panel.transform, Loc.Tr("pause.restart"),  y, RestartLevel);  y -= gap;
            MakeButton(_panel.transform, Loc.Tr("pause.quit"),     y, QuitToMenu);
        }

        void OpenSettings()
        {
            // Settings panel uses its own Show/Hide; do NOT resume here so closing settings
            // returns the player to the still-paused menu.
            SettingsPanel.Instance?.Show();
        }

        public void Close()
        {
            if (_canvas != null) _canvas.gameObject.SetActive(false);
            if (_panel != null)  _panel.SetActive(false);
            if (_dimTween != null) { StopCoroutine(_dimTween); _dimTween = null; }
            if (_dimGroup != null) _dimGroup.alpha = 0f;
            _open = false;
        }

        static void MakeTitle(Transform parent, string text)
        {
            var go = new GameObject("Title");
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 1); rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0, -20); rt.sizeDelta = new Vector2(0, 90);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            var defaultFont = TMP_Settings.defaultFontAsset;
            if (defaultFont != null) tmp.font = defaultFont;
            tmp.text = text;
            tmp.fontSize = 56;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = new Color(0.0f, 0.9f, 1.0f);
        }

        static void MakeButton(Transform parent, string label, float y, System.Action onClick)
        {
            var go = new GameObject("Btn_" + label);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 1); rt.anchorMax = new Vector2(0.5f, 1);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0, y); rt.sizeDelta = new Vector2(420, 80);
            var img = go.AddComponent<Image>();
            img.color = new Color(0.08f, 0.18f, 0.38f, 0.92f);
            var btn = go.AddComponent<Button>();
            btn.onClick.AddListener(() => onClick?.Invoke());
            go.AddComponent<UIButtonAnimator>();

            var textGO = new GameObject("Label");
            textGO.transform.SetParent(go.transform, false);
            var trt = textGO.AddComponent<RectTransform>();
            trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
            trt.offsetMin = trt.offsetMax = Vector2.zero;
            var tmp = textGO.AddComponent<TextMeshProUGUI>();
            var defaultFont = TMP_Settings.defaultFontAsset;
            if (defaultFont != null) tmp.font = defaultFont;
            tmp.text = label;
            tmp.fontSize = 36;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
        }
    }
}
