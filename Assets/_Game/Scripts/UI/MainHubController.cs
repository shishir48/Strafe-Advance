using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace StrafAdvance
{
    /// <summary>
    /// Runtime-built main menu. Visible only when GameState=Menu.
    /// Title + currency display + four routes (Play→Loadout, Shop, Settings, Quit).
    /// Subscribes to <see cref="GameStateChanged"/> for show/hide and <see cref="CurrencyEarned"/> for balance refresh.
    /// </summary>
    public class MainHubController : MonoBehaviour
    {
        public static MainHubController Instance { get; private set; }

        private GameObject _root;
        private TMP_Text   _credits;
        private TMP_Text   _bestScoreLabel;
        private TMP_Text   _bpTierChip;
        private float      _titlePulseTime;
        private TMP_Text   _title;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { Instance = null; }

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            if (Application.isPlaying) DontDestroyOnLoad(gameObject);
            BuildUI();
            EventBus<GameStateChanged>.Subscribe(OnState);
            EventBus<CurrencyEarned>.Subscribe(OnCurrencyChanged);
            EventBus<BattlePassTierReached>.Subscribe(_ => RefreshCredits());
            EventBus<LanguageChanged>.Subscribe(_ => Rebuild());
            ApplyVisibility(GameManager.Instance != null ? GameManager.Instance.State : GameState.Menu);
            RefreshCredits();
        }

        void OnDestroy()
        {
            EventBus<GameStateChanged>.Unsubscribe(OnState);
            EventBus<CurrencyEarned>.Unsubscribe(OnCurrencyChanged);
            if (Instance == this) Instance = null;
        }

        void Rebuild()
        {
            if (_root != null) Destroy(_root);
            BuildUI();
            ApplyVisibility(GameManager.Instance != null ? GameManager.Instance.State : GameState.Menu);
            RefreshCredits();
        }

        void Update()
        {
            if (_root == null || !_root.activeSelf || _title == null) return;
            // Gentle title pulse (alpha + scale) for life.
            _titlePulseTime += Time.unscaledDeltaTime;
            float t = (Mathf.Sin(_titlePulseTime * 1.6f) + 1f) * 0.5f;
            _title.transform.localScale = Vector3.one * Mathf.Lerp(0.98f, 1.02f, t);
            var c = _title.color; c.a = Mathf.Lerp(0.85f, 1f, t); _title.color = c;
        }

        public void Show() => ApplyVisibility(GameState.Menu);
        public void Hide() { if (_root != null) _root.SetActive(false); }

        void OnState(GameStateChanged e) => ApplyVisibility(e.Current);
        void ApplyVisibility(GameState state) { if (_root != null) _root.SetActive(state == GameState.Menu); }

        void OnCurrencyChanged(CurrencyEarned _) => RefreshCredits();

        void RefreshCredits()
        {
            int bal = CurrencyService.Instance != null ? CurrencyService.Instance.Balance : SaveSystem.Current.progress.softCurrency;
            if (_credits != null)        _credits.text        = $"<color=#ffd166>◆</color>  {bal:N0}";
            if (_bestScoreLabel != null) _bestScoreLabel.text = string.Format(Loc.Tr("menu.best_score"), SaveSystem.Current.progress.bestScore.ToString("N0"), SaveSystem.Current.progress.playerLevel);
            if (_bpTierChip != null)
            {
                int tier = BattlePassService.Instance != null ? BattlePassService.Instance.Tier : SaveSystem.Current.progress.battlePassTier;
                _bpTierChip.text = $"<color=#7ed957>BP</color>  Tier {tier}/{BattlePassCatalog.MaxTier}";
            }
        }

        // ─── Actions ────────────────────────────────────────────────────────────

        void OnPlay()
        {
            if (LoadoutPanel.Instance != null) LoadoutPanel.Instance.Show();
            else BeginRunDirect();
        }

        void OnLoadout()    { if (LoadoutPanel.Instance != null) LoadoutPanel.Instance.Show(); }
        void OnShop()       { if (ShopController.Instance != null) ShopController.Instance.Show(); }
        void OnBattlePass() { if (BattlePassPanel.Instance != null) BattlePassPanel.Instance.Show(); }
        void OnSettings()   { if (SettingsPanel.Instance != null) SettingsPanel.Instance.Show(); }
        void OnQuit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        /// <summary>Bypass loadout (called by Loadout's Start Run button or directly).</summary>
        public static void BeginRunDirect()
        {
            var gm = GameManager.Instance;
            if (gm == null) return;
            gm.BeginRunFromMenu();
        }

        // ─── UI Construction ────────────────────────────────────────────────────

        void BuildUI()
        {
            var canvasGO = new GameObject("MainHubCanvas");
            canvasGO.transform.SetParent(transform, false);
            var c = canvasGO.AddComponent<Canvas>();
            c.renderMode = RenderMode.ScreenSpaceOverlay;
            c.sortingOrder = 60;
            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            canvasGO.AddComponent<GraphicRaycaster>();
            _root = canvasGO;

            // Full-screen background gradient
            var bg = MakeRect(canvasGO.transform, "BG", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, Vector2.zero);
            var bgImg = bg.gameObject.AddComponent<Image>();
            bgImg.color = new Color(0.04f, 0.06f, 0.16f, 1f);

            // Title (top-center)
            var titleRT = MakeRect(canvasGO.transform, "Title", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -220f), Vector2.zero, new Vector2(960, 140));
            titleRT.pivot = new Vector2(0.5f, 0.5f);
            _title = AddText(titleRT.gameObject, "STRAFE ADVANCE", 96, new Color(0.0f, 0.9f, 1.0f), TextAlignmentOptions.Center);
            _title.fontStyle = FontStyles.Bold;

            // Subtitle
            var subRT = MakeRect(canvasGO.transform, "Subtitle", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -310f), Vector2.zero, new Vector2(720, 40));
            subRT.pivot = new Vector2(0.5f, 0.5f);
            AddText(subRT.gameObject, Loc.Tr("menu.subtitle"), 22, new Color(0.65f, 0.78f, 0.95f, 0.9f), TextAlignmentOptions.Center);

            // Top-right currency chip
            var creditsRT = MakeRect(canvasGO.transform, "Credits", new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-30f, -30f), Vector2.zero, new Vector2(320, 60));
            creditsRT.pivot = new Vector2(1f, 1f);
            var creditsBg = creditsRT.gameObject.AddComponent<Image>();
            creditsBg.color = new Color(0.06f, 0.12f, 0.28f, 0.92f);
            _credits = AddText(creditsRT.gameObject, "◆  0", 32, new Color(1f, 0.85f, 0.3f), TextAlignmentOptions.Center);
            _credits.richText = true;

            // Center column buttons (5 stacked — tightened gap)
            float cy = 60f, gap = 116f;
            MakeButton(canvasGO.transform, Loc.Tr("menu.play"),        new Vector2(0, cy + gap * 2f), new Color(0.0f, 0.5f, 1.0f), OnPlay);
            MakeButton(canvasGO.transform, Loc.Tr("menu.loadout"),     new Vector2(0, cy + gap * 1f), new Color(0.08f, 0.18f, 0.38f), OnLoadout);
            MakeButton(canvasGO.transform, Loc.Tr("menu.shop"),        new Vector2(0, cy + gap * 0f), new Color(0.08f, 0.18f, 0.38f), OnShop);
            MakeButton(canvasGO.transform, Loc.Tr("menu.battle_pass"), new Vector2(0, cy - gap * 1f), new Color(0.15f, 0.30f, 0.18f), OnBattlePass);
            MakeButton(canvasGO.transform, Loc.Tr("menu.settings"),    new Vector2(0, cy - gap * 2f), new Color(0.08f, 0.18f, 0.38f), OnSettings);

            // BP tier chip — left of credits chip (top-right area)
            var bpRT = MakeRect(canvasGO.transform, "BpChip", new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-370f, -30f), Vector2.zero, new Vector2(280, 60));
            bpRT.pivot = new Vector2(1f, 1f);
            bpRT.gameObject.AddComponent<Image>().color = new Color(0.06f, 0.22f, 0.14f, 0.92f);
            _bpTierChip = AddText(bpRT.gameObject, "BP  Tier 0/10", 26, new Color(0.7f, 1f, 0.85f), TextAlignmentOptions.Center);
            _bpTierChip.richText = true;

            // Bottom-right quit
            var quitRT = MakeRect(canvasGO.transform, "Quit", new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-30f, 30f), Vector2.zero, new Vector2(180, 70));
            quitRT.pivot = new Vector2(1f, 0f);
            var quitImg = quitRT.gameObject.AddComponent<Image>();
            quitImg.color = new Color(0.28f, 0.04f, 0.08f, 0.92f);
            var quitBtn = quitRT.gameObject.AddComponent<Button>();
            quitBtn.onClick.AddListener(OnQuit);
            quitRT.gameObject.AddComponent<UIButtonAnimator>();
            AddText(quitRT.gameObject, Loc.Tr("menu.quit"), 26, new Color(1f, 0.5f, 0.5f), TextAlignmentOptions.Center);

            // Bottom-left status line (best score + level)
            var bestRT = MakeRect(canvasGO.transform, "BestScore", new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(30f, 30f), Vector2.zero, new Vector2(520, 60));
            bestRT.pivot = new Vector2(0f, 0f);
            _bestScoreLabel = AddText(bestRT.gameObject, "Best: 0   ·   Lvl 1", 22, new Color(0.65f, 0.78f, 0.95f, 0.9f), TextAlignmentOptions.Left);
        }

        // ─── Construction Helpers ───────────────────────────────────────────────

        static RectTransform MakeRect(Transform parent, string name, Vector2 aMin, Vector2 aMax, Vector2 pos, Vector2 offMin, Vector2 size)
        {
            var go = new GameObject(name);
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
                rt.offsetMin = offMin;
                rt.offsetMax = size; // when stretching, size doubles as offMax
            }
            return rt;
        }

        static TMP_Text AddText(GameObject host, string text, float size, Color color, TextAlignmentOptions align)
        {
            var go = new GameObject("Text");
            go.transform.SetParent(host.transform, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(8, 4); rt.offsetMax = new Vector2(-8, -4);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            var font = TMP_Settings.defaultFontAsset;
            if (font != null) tmp.font = font;
            tmp.text = text;
            tmp.fontSize = size;
            tmp.color = color;
            tmp.alignment = align;
            tmp.richText = true;
            return tmp;
        }

        void MakeButton(Transform parent, string label, Vector2 pos, Color bg, System.Action onClick)
        {
            var rt = MakeRect(parent, "Btn_" + label, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), pos, Vector2.zero, new Vector2(420, 110));
            rt.pivot = new Vector2(0.5f, 0.5f);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = bg;
            var btn = rt.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;
            var colors = btn.colors;
            colors.highlightedColor = new Color(bg.r * 1.3f, bg.g * 1.3f, bg.b * 1.3f, 1f);
            colors.pressedColor     = new Color(bg.r * 0.7f, bg.g * 0.7f, bg.b * 0.7f, 1f);
            btn.colors = colors;
            btn.onClick.AddListener(() => onClick?.Invoke());
            rt.gameObject.AddComponent<UIButtonAnimator>();
            var labelText = AddText(rt.gameObject, label, 38, Color.white, TextAlignmentOptions.Center);
            labelText.fontStyle = FontStyles.Bold;
        }
    }
}
