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

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { Instance = null; }

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            if (Application.isPlaying) DontDestroyOnLoad(gameObject);
            BuildUI();
            Close();
            EventBus<LanguageChanged>.Subscribe(_ => Rebuild());
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

        void OnDestroy() { if (Instance == this) Instance = null; }

        public void Toggle() { if (_open) Resume(); else Open(); }

        public void Open()
        {
            if (_open) return;
            _open = true;
            _savedScale = Time.timeScale > 0f ? Time.timeScale : 1f;
            Time.timeScale = 0f;
            _panel.SetActive(true);
        }

        public void Resume()
        {
            if (!_open) return;
            _open = false;
            Time.timeScale = _savedScale;
            _panel.SetActive(false);
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

            _panel = new GameObject("Panel");
            _panel.transform.SetParent(canvasGO.transform, false);
            var prt = _panel.AddComponent<RectTransform>();
            prt.anchorMin = new Vector2(0.15f, 0.25f);
            prt.anchorMax = new Vector2(0.85f, 0.75f);
            prt.offsetMin = prt.offsetMax = Vector2.zero;
            var bg = _panel.AddComponent<Image>();
            bg.color = new Color(0.03f, 0.07f, 0.13f, 0.95f);

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

        public void Close() { if (_panel != null) _panel.SetActive(false); _open = false; }

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
            tmp.color = new Color(0.31f, 0.76f, 0.97f);
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
            img.color = new Color(0.06f, 0.13f, 0.22f, 0.9f);
            var btn = go.AddComponent<Button>();
            btn.onClick.AddListener(() => onClick?.Invoke());

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
