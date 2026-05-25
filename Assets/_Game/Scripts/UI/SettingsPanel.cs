using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace StrafAdvance
{
    /// <summary>
    /// Runtime-built settings panel. Wires <see cref="SaveData.settings"/> to AudioManager + QualitySettings live.
    /// Saves on every change so closing the panel cannot lose state.
    /// </summary>
    public class SettingsPanel : MonoBehaviour
    {
        public static SettingsPanel Instance { get; private set; }

        private GameObject _root;
        private Slider _music, _sfx, _ui, _sensitivity;
        private Toggle _vibration, _invertY, _colorblind;
        private TMP_Dropdown _quality;
        private TMP_Dropdown _language;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { Instance = null; }

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            if (Application.isPlaying) DontDestroyOnLoad(gameObject);
            BuildUI();
            ApplySavedToControls();
            ApplyToSystems();
            Hide();
            EventBus<LanguageChanged>.Subscribe(_ => Rebuild());
        }

        void Rebuild()
        {
            bool wasOpen = _root != null && _root.activeSelf;
            if (_root != null) Destroy(_root);
            BuildUI();
            ApplySavedToControls();
            if (wasOpen) Show(); else Hide();
        }

        void OnDestroy() { if (Instance == this) Instance = null; }

        public void Show() { if (_root != null) _root.SetActive(true); }
        public void Hide() { if (_root != null) _root.SetActive(false); }

        void ApplySavedToControls()
        {
            var s = SaveSystem.Current.settings;
            if (_music != null)       _music.SetValueWithoutNotify(s.musicVolume);
            if (_sfx != null)         _sfx.SetValueWithoutNotify(s.sfxVolume);
            if (_ui != null)          _ui.SetValueWithoutNotify(s.uiVolume);
            if (_sensitivity != null) _sensitivity.SetValueWithoutNotify(s.aimSensitivity);
            if (_vibration != null)   _vibration.SetIsOnWithoutNotify(s.vibration);
            if (_invertY != null)     _invertY.SetIsOnWithoutNotify(s.invertY);
            if (_colorblind != null)  _colorblind.SetIsOnWithoutNotify(s.colorblindMode);
            if (_quality != null)     _quality.SetValueWithoutNotify(Mathf.Clamp(QualitySettings.GetQualityLevel(), 0, _quality.options.Count - 1));
            if (_language != null)    _language.SetValueWithoutNotify(Mathf.Max(0, Loc.LanguageIndex));
        }

        void ApplyToSystems()
        {
            var s = SaveSystem.Current.settings;
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.SetMusicVolume(s.musicVolume);
                AudioManager.Instance.SetSFXVolume(s.sfxVolume);
            }
        }

        void PersistAndApply()
        {
            SaveSystem.Save();
            ApplyToSystems();
        }

        // ─── UI Construction ────────────────────────────────────────────────────

        void BuildUI()
        {
            var canvasGO = new GameObject("SettingsCanvas");
            canvasGO.transform.SetParent(transform, false);
            var c = canvasGO.AddComponent<Canvas>();
            c.renderMode = RenderMode.ScreenSpaceOverlay;
            c.sortingOrder = 78;
            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            canvasGO.AddComponent<GraphicRaycaster>();
            _root = canvasGO;

            var dim = new GameObject("Dim");
            dim.transform.SetParent(canvasGO.transform, false);
            var dimRT = dim.AddComponent<RectTransform>();
            dimRT.anchorMin = Vector2.zero; dimRT.anchorMax = Vector2.one;
            dimRT.offsetMin = dimRT.offsetMax = Vector2.zero;
            dim.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.82f);

            var panel = new GameObject("Panel");
            panel.transform.SetParent(canvasGO.transform, false);
            var prt = panel.AddComponent<RectTransform>();
            prt.anchorMin = new Vector2(0.08f, 0.10f); prt.anchorMax = new Vector2(0.92f, 0.90f);
            prt.offsetMin = prt.offsetMax = Vector2.zero;
            panel.AddComponent<Image>().color = new Color(0.03f, 0.07f, 0.13f, 0.97f);

            MakeTitle(panel.transform, Loc.Tr("settings.title"));

            float yStart = -130f, gap = 85f;

            _music       = MakeSlider(panel.transform, Loc.Tr("settings.music_volume"),    yStart - gap * 0, 0f, 1f, v => { SaveSystem.Current.settings.musicVolume = v; PersistAndApply(); });
            _sfx         = MakeSlider(panel.transform, Loc.Tr("settings.sfx_volume"),      yStart - gap * 1, 0f, 1f, v => { SaveSystem.Current.settings.sfxVolume   = v; PersistAndApply(); });
            _ui          = MakeSlider(panel.transform, Loc.Tr("settings.ui_volume"),       yStart - gap * 2, 0f, 1f, v => { SaveSystem.Current.settings.uiVolume    = v; PersistAndApply(); });
            _sensitivity = MakeSlider(panel.transform, Loc.Tr("settings.aim_sensitivity"), yStart - gap * 3, 0.25f, 2.5f, v => { SaveSystem.Current.settings.aimSensitivity = v; PersistAndApply(); });

            _vibration  = MakeToggle(panel.transform, Loc.Tr("settings.vibration"),  yStart - gap * 4 + 10f,        v => { SaveSystem.Current.settings.vibration      = v; SaveSystem.Save(); });
            _invertY    = MakeToggle(panel.transform, Loc.Tr("settings.invert_y"),   yStart - gap * 4 + 10f - 56f,  v => { SaveSystem.Current.settings.invertY        = v; SaveSystem.Save(); });
            _colorblind = MakeToggle(panel.transform, Loc.Tr("settings.colorblind"), yStart - gap * 4 + 10f - 112f, v => { SaveSystem.Current.settings.colorblindMode = v; SaveSystem.Save(); });

            _quality  = MakeDropdown(panel.transform, Loc.Tr("settings.quality"),  yStart - gap * 4 + 10f - 180f, new[] { "Low", "Medium", "High" }, idx =>
            {
                QualitySettings.SetQualityLevel(idx, true);
            });
            _language = MakeDropdown(panel.transform, Loc.Tr("settings.language"), yStart - gap * 4 + 10f - 240f, new[] { "English", "Español", "日本語", "中文" }, idx =>
            {
                if (idx < 0 || idx >= Loc.SupportedLanguages.Length) return;
                Loc.SetLanguage(Loc.SupportedLanguages[idx]);
            });

            // Buttons
            MakeButton(panel.transform, Loc.Tr("settings.reset_profile"),  new Vector2(0.04f, 0.02f), new Vector2(0.30f, 0.10f), new Color(0.4f, 0.1f, 0.1f, 0.9f), () =>
            {
                SaveSystem.Reset();
                Loc.Init();
                ApplySavedToControls();
                ApplyToSystems();
            });
            MakeButton(panel.transform, Loc.Tr("settings.reset_tutorial"), new Vector2(0.33f, 0.02f), new Vector2(0.65f, 0.10f), new Color(0.2f, 0.2f, 0.4f, 0.9f), () =>
            {
                if (TutorialController.Instance != null) TutorialController.Instance.ResetAndArm();
                else { SaveSystem.Current.profile.tutorialCompleted = false; SaveSystem.Save(); }
            });
            MakeButton(panel.transform, Loc.Tr("settings.close"),          new Vector2(0.68f, 0.02f), new Vector2(0.94f, 0.10f), new Color(0.15f, 0.2f, 0.3f, 0.95f), Hide);
        }

        // ─── Widget Factories ───────────────────────────────────────────────────

        static void MakeTitle(Transform parent, string text)
        {
            var go = new GameObject("Title");
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 1); rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0, -30f);
            rt.sizeDelta = new Vector2(-40, 80);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            var font = TMP_Settings.defaultFontAsset;
            if (font != null) tmp.font = font;
            tmp.text = text; tmp.fontSize = 56;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = new Color(0.31f, 0.76f, 0.97f);
            tmp.fontStyle = FontStyles.Bold;
        }

        static Slider MakeSlider(Transform parent, string label, float y, float min, float max, System.Action<float> onChange)
        {
            // Label
            var labelGO = new GameObject("Lbl_" + label);
            labelGO.transform.SetParent(parent, false);
            var lrt = labelGO.AddComponent<RectTransform>();
            lrt.anchorMin = new Vector2(0, 1); lrt.anchorMax = new Vector2(0.45f, 1);
            lrt.pivot = new Vector2(0, 1);
            lrt.anchoredPosition = new Vector2(40, y);
            lrt.sizeDelta = new Vector2(-40, 60);
            var tmp = labelGO.AddComponent<TextMeshProUGUI>();
            var font = TMP_Settings.defaultFontAsset;
            if (font != null) tmp.font = font;
            tmp.text = label; tmp.fontSize = 26;
            tmp.alignment = TextAlignmentOptions.MidlineLeft;
            tmp.color = Color.white;

            // Slider
            var sGO = new GameObject("Slider_" + label);
            sGO.transform.SetParent(parent, false);
            var srt = sGO.AddComponent<RectTransform>();
            srt.anchorMin = new Vector2(0.5f, 1); srt.anchorMax = new Vector2(1, 1);
            srt.pivot = new Vector2(0, 1);
            srt.anchoredPosition = new Vector2(0, y - 14);
            srt.sizeDelta = new Vector2(-40, 30);

            var bg = new GameObject("Bg");
            bg.transform.SetParent(sGO.transform, false);
            var bgRT = bg.AddComponent<RectTransform>();
            bgRT.anchorMin = Vector2.zero; bgRT.anchorMax = Vector2.one;
            bgRT.offsetMin = bgRT.offsetMax = Vector2.zero;
            var bgImg = bg.AddComponent<Image>();
            bgImg.color = new Color(0.06f, 0.13f, 0.22f, 0.95f);

            var fillArea = new GameObject("FillArea");
            fillArea.transform.SetParent(sGO.transform, false);
            var faRT = fillArea.AddComponent<RectTransform>();
            faRT.anchorMin = new Vector2(0, 0.25f); faRT.anchorMax = new Vector2(1, 0.75f);
            faRT.offsetMin = new Vector2(8, 0); faRT.offsetMax = new Vector2(-8, 0);

            var fill = new GameObject("Fill");
            fill.transform.SetParent(fillArea.transform, false);
            var fRT = fill.AddComponent<RectTransform>();
            fRT.anchorMin = Vector2.zero; fRT.anchorMax = new Vector2(1, 1);
            fRT.offsetMin = fRT.offsetMax = Vector2.zero;
            var fImg = fill.AddComponent<Image>();
            fImg.color = new Color(0.31f, 0.76f, 0.97f);

            var handleArea = new GameObject("HandleArea");
            handleArea.transform.SetParent(sGO.transform, false);
            var haRT = handleArea.AddComponent<RectTransform>();
            haRT.anchorMin = Vector2.zero; haRT.anchorMax = Vector2.one;
            haRT.offsetMin = new Vector2(8, 0); haRT.offsetMax = new Vector2(-8, 0);

            var handle = new GameObject("Handle");
            handle.transform.SetParent(handleArea.transform, false);
            var hRT = handle.AddComponent<RectTransform>();
            hRT.sizeDelta = new Vector2(24, 42);
            var hImg = handle.AddComponent<Image>();
            hImg.color = Color.white;

            var slider = sGO.AddComponent<Slider>();
            slider.targetGraphic = hImg;
            slider.fillRect      = fRT;
            slider.handleRect    = hRT;
            slider.minValue      = min;
            slider.maxValue      = max;
            slider.onValueChanged.AddListener(v => onChange?.Invoke(v));

            return slider;
        }

        static Toggle MakeToggle(Transform parent, string label, float y, System.Action<bool> onChange)
        {
            var go = new GameObject("Toggle_" + label);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 1); rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = new Vector2(40, y);
            rt.sizeDelta = new Vector2(-40, 50);

            // Background box
            var box = new GameObject("Box");
            box.transform.SetParent(go.transform, false);
            var boxRT = box.AddComponent<RectTransform>();
            boxRT.anchorMin = new Vector2(0, 0.5f); boxRT.anchorMax = new Vector2(0, 0.5f);
            boxRT.pivot = new Vector2(0, 0.5f);
            boxRT.anchoredPosition = new Vector2(0, 0); boxRT.sizeDelta = new Vector2(40, 40);
            var boxImg = box.AddComponent<Image>();
            boxImg.color = new Color(0.06f, 0.13f, 0.22f, 0.95f);

            var check = new GameObject("Check");
            check.transform.SetParent(box.transform, false);
            var cRT = check.AddComponent<RectTransform>();
            cRT.anchorMin = Vector2.zero; cRT.anchorMax = Vector2.one;
            cRT.offsetMin = new Vector2(6, 6); cRT.offsetMax = new Vector2(-6, -6);
            var cImg = check.AddComponent<Image>();
            cImg.color = new Color(0.31f, 0.76f, 0.97f);

            var labelGO = new GameObject("Label");
            labelGO.transform.SetParent(go.transform, false);
            var lrt = labelGO.AddComponent<RectTransform>();
            lrt.anchorMin = new Vector2(0, 0); lrt.anchorMax = new Vector2(1, 1);
            lrt.offsetMin = new Vector2(60, 0); lrt.offsetMax = new Vector2(0, 0);
            var tmp = labelGO.AddComponent<TextMeshProUGUI>();
            var font = TMP_Settings.defaultFontAsset;
            if (font != null) tmp.font = font;
            tmp.text = label; tmp.fontSize = 24;
            tmp.alignment = TextAlignmentOptions.MidlineLeft;
            tmp.color = Color.white;

            var toggle = go.AddComponent<Toggle>();
            toggle.targetGraphic = boxImg;
            toggle.graphic = cImg;
            toggle.onValueChanged.AddListener(v => onChange?.Invoke(v));
            return toggle;
        }

        static TMP_Dropdown MakeDropdown(Transform parent, string label, float y, string[] options, System.Action<int> onChange)
        {
            // Label
            var lblGO = new GameObject("Lbl_" + label);
            lblGO.transform.SetParent(parent, false);
            var lrt = lblGO.AddComponent<RectTransform>();
            lrt.anchorMin = new Vector2(0, 1); lrt.anchorMax = new Vector2(0.45f, 1);
            lrt.pivot = new Vector2(0, 1);
            lrt.anchoredPosition = new Vector2(40, y);
            lrt.sizeDelta = new Vector2(-40, 50);
            var tmp = lblGO.AddComponent<TextMeshProUGUI>();
            var font = TMP_Settings.defaultFontAsset;
            if (font != null) tmp.font = font;
            tmp.text = label; tmp.fontSize = 26;
            tmp.alignment = TextAlignmentOptions.MidlineLeft;
            tmp.color = Color.white;

            // Dropdown box
            var ddGO = new GameObject("DD_" + label);
            ddGO.transform.SetParent(parent, false);
            var drt = ddGO.AddComponent<RectTransform>();
            drt.anchorMin = new Vector2(0.5f, 1); drt.anchorMax = new Vector2(1, 1);
            drt.pivot = new Vector2(0, 1);
            drt.anchoredPosition = new Vector2(0, y);
            drt.sizeDelta = new Vector2(-40, 50);
            var img = ddGO.AddComponent<Image>();
            img.color = new Color(0.06f, 0.13f, 0.22f, 0.95f);

            // Label text inside dropdown (TMP_Dropdown expects child TMP_Text)
            var ddLabel = new GameObject("Label");
            ddLabel.transform.SetParent(ddGO.transform, false);
            var dlrt = ddLabel.AddComponent<RectTransform>();
            dlrt.anchorMin = new Vector2(0, 0); dlrt.anchorMax = new Vector2(1, 1);
            dlrt.offsetMin = new Vector2(15, 0); dlrt.offsetMax = new Vector2(-30, 0);
            var dltmp = ddLabel.AddComponent<TextMeshProUGUI>();
            if (font != null) dltmp.font = font;
            dltmp.fontSize = 22; dltmp.alignment = TextAlignmentOptions.MidlineLeft;
            dltmp.color = Color.white; dltmp.text = options.Length > 0 ? options[0] : "";

            var dd = ddGO.AddComponent<TMP_Dropdown>();
            dd.targetGraphic = img;
            dd.captionText = dltmp;
            dd.ClearOptions();
            foreach (var o in options) dd.options.Add(new TMP_Dropdown.OptionData(o));
            dd.onValueChanged.AddListener(v => onChange?.Invoke(v));
            return dd;
        }

        static void MakeButton(Transform parent, string label, Vector2 aMin, Vector2 aMax, Color bg, System.Action onClick)
        {
            var go = new GameObject("Btn_" + label);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = aMin; rt.anchorMax = aMax;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            var img = go.AddComponent<Image>(); img.color = bg;
            var btn = go.AddComponent<Button>(); btn.targetGraphic = img;
            btn.onClick.AddListener(() => onClick?.Invoke());
            var t = new GameObject("Label");
            t.transform.SetParent(go.transform, false);
            var trt = t.AddComponent<RectTransform>();
            trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
            trt.offsetMin = trt.offsetMax = Vector2.zero;
            var tmp = t.AddComponent<TextMeshProUGUI>();
            var font = TMP_Settings.defaultFontAsset;
            if (font != null) tmp.font = font;
            tmp.text = label; tmp.fontSize = 26;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            tmp.fontStyle = FontStyles.Bold;
        }
    }
}
