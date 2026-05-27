using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace StrafAdvance
{
    /// <summary>
    /// Runtime-built battle-pass viewer. Top: current tier + XP-to-next progress bar +
    /// "Unlock Premium" CTA. Body: vertical tier list (free + premium claim columns).
    /// Refreshes on <see cref="BattlePassTierReached"/> and <see cref="CurrencyEarned"/>.
    /// </summary>
    public class BattlePassPanel : MonoBehaviour
    {
        public static BattlePassPanel Instance { get; private set; }

        GameObject _root;
        RectTransform _panelRT;
        TMP_Text _tierLabel, _xpLabel, _premiumLabel;
        Image _xpFill;
        RectTransform _listRoot;
        Button _premiumBtn;
        readonly List<GameObject> _rowPool = new List<GameObject>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { Instance = null; }

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            if (Application.isPlaying) DontDestroyOnLoad(gameObject);
            BuildUI();
            EventBus<BattlePassTierReached>.Subscribe(_ => RefreshIfOpen());
            EventBus<CurrencyEarned>.Subscribe(_ => RefreshIfOpen());
            Hide();
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void Show() { _root.SetActive(true); if (_panelRT != null) UITransition.SlideIn(this, _panelRT, new Vector2(0f, -1800f)); Refresh(); }
        public void Hide() { if (_root != null) _root.SetActive(false); }

        void RefreshIfOpen() { if (_root != null && _root.activeSelf) Refresh(); }

        void Refresh()
        {
            var svc = BattlePassService.Instance;
            int tier = svc != null ? svc.Tier         : SaveSystem.Current.progress.battlePassTier;
            int max  = svc != null ? svc.MaxTier      : BattlePassCatalog.MaxTier;
            float prog = svc != null ? svc.ProgressToNext : 0f;
            int into   = svc != null ? svc.XpIntoCurrentTier : 0;
            int span   = svc != null ? svc.XpSpanCurrentTier : BattlePassCatalog.XpPerTier;

            _tierLabel.text = $"SEASON 1   ·   TIER <color=#00eeff>{tier}</color> / {max}";
            _xpLabel.text   = tier >= max ? "MAX TIER" : $"{into:N0} / {span:N0} XP";
            _xpFill.fillAmount = tier >= max ? 1f : prog;

            bool premium = svc != null ? svc.PremiumOwned : SaveSystem.Current.progress.premiumPassOwned;
            _premiumLabel.text = premium ? "<color=#ffd166>PREMIUM PASS UNLOCKED</color>" : "Premium pass: 500 credits";
            _premiumBtn.interactable = !premium;
            _premiumBtn.GetComponent<Image>().color = premium ? new Color(0.10f, 0.30f, 0.15f, 0.95f) : new Color(0.6f, 0.45f, 0.1f, 0.95f);

            RebuildRows(tier, premium);
        }

        void RebuildRows(int currentTier, bool premium)
        {
            foreach (var go in _rowPool) Destroy(go);
            _rowPool.Clear();

            float y = 0f;
            foreach (var t in BattlePassCatalog.Tiers)
            {
                var row = MakeRow(t, currentTier, premium, new Vector2(0, y));
                _rowPool.Add(row);
                y -= 100f;
            }

            // Resize content for vertical scroll.
            _listRoot.sizeDelta = new Vector2(_listRoot.sizeDelta.x, BattlePassCatalog.Tiers.Length * 100f + 20f);
        }

        GameObject MakeRow(BattlePassTier t, int currentTier, bool premiumOwned, Vector2 pos)
        {
            var row = new GameObject($"Tier_{t.TierIndex}");
            row.transform.SetParent(_listRoot, false);
            var rt = row.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 1); rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(-20, 88);

            bool unlocked = t.TierIndex <= currentTier;
            var bg = row.AddComponent<Image>();
            bg.color = unlocked ? new Color(0.06f, 0.14f, 0.22f, 0.95f) : new Color(0.04f, 0.06f, 0.10f, 0.92f);

            // Tier number
            var label = AttachText(row, $"<b>{t.TierIndex}</b>", 36, unlocked ? new Color(0.0f, 0.9f, 1.0f) : new Color(0.5f, 0.55f, 0.65f), TextAlignmentOptions.Center);
            label.rectTransform.anchorMin = new Vector2(0, 0); label.rectTransform.anchorMax = new Vector2(0.10f, 1);
            label.rectTransform.offsetMin = label.rectTransform.offsetMax = Vector2.zero;
            label.richText = true;

            // Free lane chip
            bool freeClaimed = BattlePassService.Instance != null && BattlePassService.Instance.IsClaimed(t.TierIndex, premiumLane: false);
            MakeLaneChip(row.transform, "FREE",    t.Free,    unlocked, freeClaimed, true,                                                                          0.12f, 0.49f, () => Claim(t.TierIndex, false));
            // Premium lane chip
            bool premiumClaimed = BattlePassService.Instance != null && BattlePassService.Instance.IsClaimed(t.TierIndex, premiumLane: true);
            MakeLaneChip(row.transform, "PREMIUM", t.Premium, unlocked, premiumClaimed, premiumOwned,                                                                  0.51f, 0.99f, () => Claim(t.TierIndex, true));

            return row;
        }

        void Claim(int tier, bool premium)
        {
            if (BattlePassService.Instance == null) return;
            if (BattlePassService.Instance.Claim(tier, premium)) Refresh();
        }

        void MakeLaneChip(Transform parent, string laneLabel, BattlePassReward reward, bool tierUnlocked, bool claimed, bool laneEligible, float aMin, float aMax, System.Action onClick)
        {
            var go = new GameObject("Lane_" + laneLabel);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(aMin, 0.08f); rt.anchorMax = new Vector2(aMax, 0.92f);
            rt.offsetMin = rt.offsetMax = Vector2.zero;

            bool canClaim = tierUnlocked && !claimed && laneEligible;
            var img = go.AddComponent<Image>();
            img.color = claimed ? new Color(0.10f, 0.25f, 0.15f, 0.85f) :
                       canClaim ? new Color(0.12f, 0.34f, 0.50f, 0.95f) :
                                  new Color(0.08f, 0.10f, 0.14f, 0.85f);
            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.interactable = canClaim;
            btn.onClick.AddListener(() => onClick?.Invoke());
            go.AddComponent<UIButtonAnimator>();

            string laneLine = laneLabel == "PREMIUM" && !laneEligible
                ? "<color=#aaa>PREMIUM LOCKED</color>"
                : (claimed ? "<color=#7ed957>CLAIMED</color>" : (canClaim ? "<b>CLAIM</b>" : laneLabel));
            var combined = AttachText(go, $"{reward.Label}\n<size=18>{laneLine}</size>", 22, Color.white, TextAlignmentOptions.Center);
            combined.richText = true;
        }

        // ─── UI Construction ────────────────────────────────────────────────────

        void BuildUI()
        {
            var canvasGO = new GameObject("BattlePassCanvas");
            canvasGO.transform.SetParent(transform, false);
            var c = canvasGO.AddComponent<Canvas>();
            c.renderMode = RenderMode.ScreenSpaceOverlay;
            c.sortingOrder = 76;
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
            prt.anchorMin = new Vector2(0.05f, 0.08f); prt.anchorMax = new Vector2(0.95f, 0.92f);
            prt.offsetMin = prt.offsetMax = Vector2.zero;
            panel.AddComponent<Image>().color = new Color(0.05f, 0.08f, 0.18f, 0.97f);
            _panelRT = prt;

            _tierLabel = AttachText(panel, "SEASON 1  ·  TIER 0 / 10", 32, new Color(0.0f, 0.9f, 1.0f), TextAlignmentOptions.Center);
            _tierLabel.rectTransform.anchorMin = new Vector2(0, 1); _tierLabel.rectTransform.anchorMax = new Vector2(1, 1);
            _tierLabel.rectTransform.pivot = new Vector2(0.5f, 1f);
            _tierLabel.rectTransform.anchoredPosition = new Vector2(0, -30f);
            _tierLabel.rectTransform.sizeDelta = new Vector2(-40, 60);
            _tierLabel.fontStyle = FontStyles.Bold;

            // XP bar
            var bar = new GameObject("XpBar");
            bar.transform.SetParent(panel.transform, false);
            var brt = bar.AddComponent<RectTransform>();
            brt.anchorMin = new Vector2(0.08f, 1f); brt.anchorMax = new Vector2(0.92f, 1f);
            brt.pivot = new Vector2(0.5f, 1f);
            brt.anchoredPosition = new Vector2(0, -100f);
            brt.sizeDelta = new Vector2(0, 22);
            bar.AddComponent<Image>().color = new Color(0.05f, 0.10f, 0.18f, 0.95f);
            var fillGO = new GameObject("Fill"); fillGO.transform.SetParent(bar.transform, false);
            var frt = fillGO.AddComponent<RectTransform>();
            frt.anchorMin = Vector2.zero; frt.anchorMax = Vector2.one;
            frt.offsetMin = new Vector2(2, 2); frt.offsetMax = new Vector2(-2, -2);
            _xpFill = fillGO.AddComponent<Image>();
            _xpFill.type = Image.Type.Filled;
            _xpFill.fillMethod = Image.FillMethod.Horizontal;
            _xpFill.color = new Color(0.0f, 0.82f, 1.0f);
            _xpFill.fillAmount = 0f;

            _xpLabel = AttachText(panel, "0 / 500 XP", 18, new Color(0.7f, 0.8f, 0.95f, 0.9f), TextAlignmentOptions.Center);
            _xpLabel.rectTransform.anchorMin = new Vector2(0, 1); _xpLabel.rectTransform.anchorMax = new Vector2(1, 1);
            _xpLabel.rectTransform.pivot = new Vector2(0.5f, 1f);
            _xpLabel.rectTransform.anchoredPosition = new Vector2(0, -132f);
            _xpLabel.rectTransform.sizeDelta = new Vector2(-40, 24);

            // Premium CTA
            var premium = new GameObject("PremiumBtn");
            premium.transform.SetParent(panel.transform, false);
            var pmRT = premium.AddComponent<RectTransform>();
            pmRT.anchorMin = new Vector2(0.55f, 0.86f); pmRT.anchorMax = new Vector2(0.95f, 0.94f);
            pmRT.offsetMin = pmRT.offsetMax = Vector2.zero;
            var pmImg = premium.AddComponent<Image>(); pmImg.color = new Color(0.6f, 0.45f, 0.1f, 0.95f);
            _premiumBtn = premium.AddComponent<Button>();
            _premiumBtn.targetGraphic = pmImg;
            premium.AddComponent<UIButtonAnimator>();
            _premiumBtn.onClick.AddListener(() =>
            {
                if (BattlePassService.Instance == null) return;
                if (CurrencyService.Instance == null) return;
                if (!CurrencyService.Instance.TrySpend(500)) return;
                BattlePassService.Instance.UnlockPremium();
                Refresh();
            });
            _premiumLabel = AttachText(premium, "Premium pass: 500 credits", 22, Color.white, TextAlignmentOptions.Center);
            _premiumLabel.fontStyle = FontStyles.Bold;
            _premiumLabel.richText = true;

            // Scroll view body
            var scroll = new GameObject("Scroll");
            scroll.transform.SetParent(panel.transform, false);
            var srt = scroll.AddComponent<RectTransform>();
            srt.anchorMin = new Vector2(0.03f, 0.10f); srt.anchorMax = new Vector2(0.97f, 0.83f);
            srt.offsetMin = srt.offsetMax = Vector2.zero;
            var sr = scroll.AddComponent<ScrollRect>();
            sr.horizontal = false; sr.vertical = true;
            sr.movementType = ScrollRect.MovementType.Clamped;
            var viewportGO = new GameObject("Viewport"); viewportGO.transform.SetParent(scroll.transform, false);
            var vrt = viewportGO.AddComponent<RectTransform>();
            vrt.anchorMin = Vector2.zero; vrt.anchorMax = Vector2.one;
            vrt.offsetMin = vrt.offsetMax = Vector2.zero;
            viewportGO.AddComponent<Image>().color = new Color(0, 0, 0, 0.0001f); // transparent but raycast target
            viewportGO.AddComponent<Mask>().showMaskGraphic = false;
            sr.viewport = vrt;
            var contentGO = new GameObject("Content"); contentGO.transform.SetParent(viewportGO.transform, false);
            _listRoot = contentGO.AddComponent<RectTransform>();
            _listRoot.anchorMin = new Vector2(0, 1); _listRoot.anchorMax = new Vector2(1, 1);
            _listRoot.pivot = new Vector2(0.5f, 1f);
            _listRoot.anchoredPosition = Vector2.zero;
            _listRoot.sizeDelta = new Vector2(0, 0);
            sr.content = _listRoot;

            // Close button
            var close = new GameObject("Close");
            close.transform.SetParent(panel.transform, false);
            var crt = close.AddComponent<RectTransform>();
            crt.anchorMin = new Vector2(0.40f, 0.02f); crt.anchorMax = new Vector2(0.60f, 0.08f);
            crt.offsetMin = crt.offsetMax = Vector2.zero;
            var cimg = close.AddComponent<Image>(); cimg.color = new Color(0.15f, 0.2f, 0.3f, 0.95f);
            var cbtn = close.AddComponent<Button>();
            cbtn.targetGraphic = cimg;
            cbtn.onClick.AddListener(Hide);
            close.AddComponent<UIButtonAnimator>();
            AttachText(close, "CLOSE", 22, Color.white, TextAlignmentOptions.Center);
        }

        static TMP_Text AttachText(GameObject host, string text, float size, Color color, TextAlignmentOptions align)
        {
            var go = new GameObject("T");
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
    }
}
