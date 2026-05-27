using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace StrafAdvance
{
    /// <summary>
    /// Post-run summary screen: outcome (WIN / GAME OVER), score, kills, XP, currency earned.
    /// Persists best-score on win. Two buttons: Restart / Menu.
    /// </summary>
    public class RunSummaryPanel : MonoBehaviour
    {
        public static RunSummaryPanel Instance { get; private set; }

        private GameObject _canvasGO;
        private GameObject _panel;
        private TMP_Text _title, _scoreText, _killsText, _xpText, _currencyText, _bestText;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { Instance = null; }

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            if (Application.isPlaying) DontDestroyOnLoad(gameObject);
            BuildUI();
            Hide();
        }

        void OnDestroy() { if (Instance == this) Instance = null; }

        public void Show(bool won)
        {
            int score    = GameManager.Instance != null ? GameManager.Instance.Score    : 0;
            int kills    = GameManager.Instance != null ? GameManager.Instance.KillCount : 0;
            int currency = CurrencyService.Instance != null ? CurrencyService.Instance.EarnedThisRun : 0;
            int xpGain   = score / 10; // simple derived metric for run summary

            // Persist best score on a win.
            var p = SaveSystem.Current.progress;
            if (won && score > p.bestScore) p.bestScore = score;
            if (kills > p.totalKills) p.totalKills = kills;
            SaveSystem.Save();

            _title.text       = won
                ? $"<color=#00eeff>{Loc.Tr("run_summary.win")}</color>"
                : $"<color=#ff2828>{Loc.Tr("run_summary.loss")}</color>";
            _scoreText.text   = $"Score:     <b>{score:N0}</b>";
            _killsText.text   = $"Kills:     <b>{kills}</b>";
            _xpText.text      = $"XP earned: <b>{xpGain}</b>";
            _currencyText.text = $"Credits:   <b>{currency:N0}</b>";
            _bestText.text    = $"Best:      <b>{p.bestScore:N0}</b>";

            if (_canvasGO != null) _canvasGO.SetActive(true);
            _panel.SetActive(true);
        }

        public void Hide()
        {
            // Hide whole canvas so the full-screen dim Image doesn't intercept clicks meant for MainHub/HUD.
            if (_canvasGO != null) _canvasGO.SetActive(false);
            if (_panel != null)    _panel.SetActive(false);
        }

        void Restart()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        void Menu()
        {
            Time.timeScale = 1f;
            if (SceneManager.sceneCountInBuildSettings > 1) SceneManager.LoadScene(0);
            else SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        // ─── UI Construction ────────────────────────────────────────────────────

        void BuildUI()
        {
            var canvasGO = new GameObject("RunSummaryCanvas");
            _canvasGO = canvasGO;
            canvasGO.transform.SetParent(transform, false);
            var c = canvasGO.AddComponent<Canvas>();
            c.renderMode = RenderMode.ScreenSpaceOverlay;
            c.sortingOrder = 80;
            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            canvasGO.AddComponent<GraphicRaycaster>();

            var dim = new GameObject("Dim");
            dim.transform.SetParent(canvasGO.transform, false);
            var dimRT = dim.AddComponent<RectTransform>();
            dimRT.anchorMin = Vector2.zero; dimRT.anchorMax = Vector2.one;
            dimRT.offsetMin = dimRT.offsetMax = Vector2.zero;
            var dimImg = dim.AddComponent<Image>();
            dimImg.color = new Color(0f, 0f, 0f, 0.75f);

            _panel = new GameObject("Panel");
            _panel.transform.SetParent(canvasGO.transform, false);
            var prt = _panel.AddComponent<RectTransform>();
            prt.anchorMin = new Vector2(0.1f, 0.2f); prt.anchorMax = new Vector2(0.9f, 0.8f);
            prt.offsetMin = prt.offsetMax = Vector2.zero;
            var bg = _panel.AddComponent<Image>();
            bg.color = new Color(0.05f, 0.08f, 0.18f, 0.97f);

            _title        = MakeLine(_panel.transform, "Title",    -10f, 64f);
            _scoreText    = MakeLine(_panel.transform, "Score",    -140f, 36f);
            _killsText    = MakeLine(_panel.transform, "Kills",    -200f, 36f);
            _xpText       = MakeLine(_panel.transform, "Xp",       -260f, 36f);
            _currencyText = MakeLine(_panel.transform, "Currency", -320f, 36f);
            _bestText     = MakeLine(_panel.transform, "Best",     -390f, 32f);

            MakeButton(_panel.transform, Loc.Tr("run_summary.restart"), 30f,  -60f, Restart);
            MakeButton(_panel.transform, Loc.Tr("run_summary.menu"),    280f, -60f, Menu);
        }

        static TMP_Text MakeLine(Transform parent, string name, float y, float size)
        {
            var go = new GameObject(name + "Line");
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 1); rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0, y); rt.sizeDelta = new Vector2(-40, size + 10);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            var defaultFont = TMP_Settings.defaultFontAsset;
            if (defaultFont != null) tmp.font = defaultFont;
            tmp.fontSize = size;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            tmp.richText = true;
            return tmp;
        }

        static void MakeButton(Transform parent, string label, float x, float yFromBottom, System.Action onClick)
        {
            var go = new GameObject("Btn_" + label);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0); rt.anchorMax = new Vector2(0.5f, 0);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.anchoredPosition = new Vector2(x - 155, -yFromBottom);
            rt.sizeDelta = new Vector2(240, 80);
            var img = go.AddComponent<Image>();
            img.color = new Color(0.08f, 0.18f, 0.38f, 0.95f);
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
            tmp.fontSize = 30;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
        }
    }
}
