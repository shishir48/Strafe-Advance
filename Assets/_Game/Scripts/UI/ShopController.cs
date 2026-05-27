using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace StrafAdvance
{
    /// <summary>
    /// Runtime-built shop. Two tabs: Weapons (spend soft currency) and Cosmetics (IAP bundles).
    /// Balance display top-right reacts to <see cref="CurrencyEarned"/>.
    /// </summary>
    public class ShopController : MonoBehaviour
    {
        public enum Tab { Weapons, Skins, Cosmetics }

        public static ShopController Instance { get; private set; }

        private GameObject _root;
        private RectTransform _panelRT;
        private RectTransform _content;
        private TMP_Text _balanceLabel;
        private Tab _activeTab = Tab.Weapons;
        private readonly List<GameObject> _rowPool = new List<GameObject>();
        private Button _tabWeaponsBtn, _tabSkinsBtn, _tabCosmeticsBtn;

        // Legacy IAP cosmetic items (kept as the Cosmetics tab data).
        private static readonly (string id, string name, string price)[] CosmeticItems =
        {
            ("skin_bundle_1", "Character Skins Pack", "$2.99"),
            ("skin_bundle_2", "Weapon Skins Pack",    "$2.99"),
        };

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { Instance = null; }

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            if (Application.isPlaying) DontDestroyOnLoad(gameObject);
            BuildUI();
            EventBus<CurrencyEarned>.Subscribe(OnCurrencyChanged);
            Hide();
        }

        void OnDestroy()
        {
            EventBus<CurrencyEarned>.Unsubscribe(OnCurrencyChanged);
            if (Instance == this) Instance = null;
        }

        public void Show()
        {
            _root.SetActive(true);
            if (_panelRT != null) UITransition.SlideIn(this, _panelRT, new Vector2(1200f, 0f));
            SwitchTab(Tab.Weapons);
            RefreshBalance();
        }

        public void Hide() { if (_root != null) _root.SetActive(false); }

        void OnCurrencyChanged(CurrencyEarned _) { RefreshBalance(); RebuildContent(); }

        void RefreshBalance()
        {
            int bal = CurrencyService.Instance != null ? CurrencyService.Instance.Balance : SaveSystem.Current.progress.softCurrency;
            if (_balanceLabel != null) _balanceLabel.text = $"<color=#ffd166>◆</color>  {bal:N0}";
        }

        void SwitchTab(Tab t)
        {
            _activeTab = t;
            HighlightTab(_tabWeaponsBtn,   t == Tab.Weapons);
            HighlightTab(_tabSkinsBtn,     t == Tab.Skins);
            HighlightTab(_tabCosmeticsBtn, t == Tab.Cosmetics);
            RebuildContent();
        }

        void RebuildContent()
        {
            foreach (var go in _rowPool) Destroy(go);
            _rowPool.Clear();

            switch (_activeTab)
            {
                case Tab.Weapons:   BuildWeaponList();    break;
                case Tab.Skins:     BuildSkinsList();     break;
                case Tab.Cosmetics: BuildCosmeticsList(); break;
            }
        }

        void BuildSkinsList()
        {
            var owned = SaveSystem.Current.progress.unlockedSkins;
            float y = 0f;
            foreach (var s in CosmeticCatalog.All)
            {
                bool isOwned   = owned.Contains(s.Id) || s.Price == 0;
                bool isEquipped = SlotEquipped(s.Slot) == s.Id;
                bool canAfford = CurrencyService.Instance != null && CurrencyService.Instance.Balance >= s.Price;
                string priceLine = isOwned
                    ? (isEquipped ? "<color=#00eeff>EQUIPPED</color>" : "<color=#7ed957>OWNED</color>")
                    : (canAfford ? $"<color=#ffd166>◆ {s.Price:N0}</color>" : $"<color=#888>◆ {s.Price:N0}</color>");
                string action = isOwned ? (isEquipped ? "EQUIPPED" : "EQUIP") : (canAfford ? "BUY" : "LOCKED");

                string colorHex = ColorUtility.ToHtmlStringRGB(s.TintColor);
                var rowSkin = s;
                var row = MakeShopRow(
                    $"<b>{s.DisplayName}</b>  <size=18><color=#aab>{s.Slot}</color></size>\n<size=18><color=#{colorHex}>■■■</color>  <color=#aab>tint preview</color></size>",
                    priceLine,
                    action,
                    enabled: isOwned || canAfford,
                    new Vector2(0, y),
                    () => OnSkinAction(rowSkin));
                _rowPool.Add(row);
                y -= 130f;
            }
        }

        void OnSkinAction(CosmeticSkin s)
        {
            var p = SaveSystem.Current.progress;
            if (!p.unlockedSkins.Contains(s.Id) && s.Price > 0)
            {
                if (CurrencyService.Instance == null) return;
                if (!CurrencyService.Instance.TrySpend(s.Price)) return;
                p.unlockedSkins.Add(s.Id);
                SaveSystem.Save();
                RebuildContent();
                return;
            }
            if (!p.unlockedSkins.Contains(s.Id) && s.Price == 0)
                p.unlockedSkins.Add(s.Id);

            // Already owned (or just unlocked) → equip into the slot.
            EquipSlot(s.Slot, s.Id);
            SaveSystem.Save();
            EventBus<SkinEquipped>.Publish(new SkinEquipped(s.Slot, s.Id));
            RebuildContent();
        }

        static string SlotEquipped(CosmeticSlot slot) => slot switch
        {
            CosmeticSlot.Player => SaveSystem.Current.progress.equippedPlayerSkinId,
            CosmeticSlot.Bullet => SaveSystem.Current.progress.equippedBulletSkinId,
            CosmeticSlot.Trail  => SaveSystem.Current.progress.equippedTrailSkinId,
            _                    => "",
        };

        static void EquipSlot(CosmeticSlot slot, string id)
        {
            switch (slot)
            {
                case CosmeticSlot.Player: SaveSystem.Current.progress.equippedPlayerSkinId = id; break;
                case CosmeticSlot.Bullet: SaveSystem.Current.progress.equippedBulletSkinId = id; break;
                case CosmeticSlot.Trail:  SaveSystem.Current.progress.equippedTrailSkinId  = id; break;
            }
        }

        void BuildWeaponList()
        {
            var unlocked = SaveSystem.Current.progress.unlockedWeaponIds;
            float y = 0f;
            foreach (var w in WeaponCatalog.All)
            {
                bool owned = unlocked.Contains(w.id);
                bool canAfford = CurrencyService.Instance != null && CurrencyService.Instance.Balance >= w.price;
                string priceLine = owned
                    ? "<color=#00eeff>OWNED</color>"
                    : (w.price == 0
                        ? "FREE"
                        : (canAfford ? $"<color=#ffd166>◆ {w.price:N0}</color>" : $"<color=#888>◆ {w.price:N0}</color>"));
                string action = owned ? "EQUIP" : (w.price == 0 ? "UNLOCK" : (canAfford ? "BUY" : "LOCKED"));

                var row = MakeShopRow(
                    $"<b>{w.displayName}</b>  <size=18><color=#8aa>DMG {w.damage} · FR {1f / w.fireRate:F1}/s</color></size>\n<size=18><color=#aab>{w.description}</color></size>",
                    priceLine,
                    action,
                    enabled: owned || canAfford || w.price == 0,
                    new Vector2(0, y),
                    () => OnWeaponAction(w));
                _rowPool.Add(row);
                y -= 130f;
            }
        }

        void BuildCosmeticsList()
        {
            UnlockRegistry reg = IAPManager.Instance != null ? IAPManager.Instance.Registry : null;
            float y = 0f;
            foreach (var (id, name, price) in CosmeticItems)
            {
                bool owned = reg != null && reg.IsUnlocked(id);
                string priceLine = owned ? "<color=#00eeff>OWNED</color>" : price;
                string action    = owned ? "OWNED" : "BUY";
                string capturedId = id;
                var row = MakeShopRow(
                    $"<b>{name}</b>\n<size=18><color=#aab>IAP cosmetic bundle</color></size>",
                    priceLine,
                    action,
                    enabled: !owned,
                    new Vector2(0, y),
                    () => { if (IAPManager.Instance != null) IAPManager.Instance.BuyProduct(capturedId); });
                _rowPool.Add(row);
                y -= 130f;
            }
        }

        void OnWeaponAction(WeaponConfig w)
        {
            var p = SaveSystem.Current.progress;
            if (p.unlockedWeaponIds.Contains(w.id))
            {
                // Already owned → equip it.
                p.equippedWeaponId = w.id;
                SaveSystem.Save();
                var shooter = FindFirstObjectByType<AutoShooter>();
                if (shooter != null) shooter.RefreshLoadout();
                RebuildContent();
                return;
            }

            if (w.price == 0)
            {
                p.unlockedWeaponIds.Add(w.id);
                SaveSystem.Save();
                RebuildContent();
                return;
            }

            if (CurrencyService.Instance == null) return;
            if (!CurrencyService.Instance.TrySpend(w.price)) return;
            p.unlockedWeaponIds.Add(w.id);
            SaveSystem.Save();
            RebuildContent();
        }

        // ─── UI Construction ────────────────────────────────────────────────────

        void BuildUI()
        {
            var canvasGO = new GameObject("ShopCanvas");
            canvasGO.transform.SetParent(transform, false);
            var c = canvasGO.AddComponent<Canvas>();
            c.renderMode = RenderMode.ScreenSpaceOverlay;
            c.sortingOrder = 75;
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

            // Title
            var title = MakeText(panel.transform, "SHOP", 0.5f, 1f, new Vector2(0, -30f), new Vector2(-40, 80), 56, new Color(0.0f, 0.9f, 1.0f));
            title.fontStyle = FontStyles.Bold;

            // Balance chip
            var balRT = MakeRect(panel.transform, "Balance", new Vector2(1, 1), new Vector2(1, 1), new Vector2(-30f, -30f), new Vector2(320, 60));
            balRT.pivot = new Vector2(1f, 1f);
            balRT.gameObject.AddComponent<Image>().color = new Color(0.06f, 0.12f, 0.28f, 0.92f);
            _balanceLabel = AttachText(balRT.gameObject, "◆  0", 28, new Color(1f, 0.85f, 0.3f), TextAlignmentOptions.Center);

            // Tab buttons (3-up — Weapons / Skins / Cosmetics)
            _tabWeaponsBtn   = MakeTabButton(panel.transform, "WEAPONS",   new Vector2(0.04f, 0.82f), new Vector2(0.27f, 0.90f), () => SwitchTab(Tab.Weapons));
            _tabSkinsBtn     = MakeTabButton(panel.transform, "SKINS",     new Vector2(0.29f, 0.82f), new Vector2(0.52f, 0.90f), () => SwitchTab(Tab.Skins));
            _tabCosmeticsBtn = MakeTabButton(panel.transform, "COSMETICS", new Vector2(0.54f, 0.82f), new Vector2(0.77f, 0.90f), () => SwitchTab(Tab.Cosmetics));

            // Content region
            var cGO = new GameObject("Content");
            cGO.transform.SetParent(panel.transform, false);
            _content = cGO.AddComponent<RectTransform>();
            _content.anchorMin = new Vector2(0.04f, 0.14f); _content.anchorMax = new Vector2(0.96f, 0.80f);
            _content.offsetMin = _content.offsetMax = Vector2.zero;

            // Close button
            MakeButton(panel.transform, "CLOSE", new Vector2(0.70f, 0.02f), new Vector2(0.95f, 0.12f), new Color(0.08f, 0.18f, 0.38f), Hide);
        }

        Button MakeTabButton(Transform parent, string label, Vector2 aMin, Vector2 aMax, System.Action onClick)
        {
            var go = new GameObject("Tab_" + label);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = aMin; rt.anchorMax = aMax;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            var img = go.AddComponent<Image>();
            img.color = new Color(0.08f, 0.18f, 0.38f, 0.92f);
            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(() => onClick?.Invoke());
            go.AddComponent<UIButtonAnimator>();
            AttachText(go, label, 26, Color.white, TextAlignmentOptions.Center);
            return btn;
        }

        void HighlightTab(Button btn, bool on)
        {
            if (btn == null) return;
            var img = btn.GetComponent<Image>();
            img.color = on ? new Color(0.0f, 0.42f, 0.88f, 0.95f) : new Color(0.08f, 0.18f, 0.38f, 0.92f);
        }

        GameObject MakeShopRow(string title, string priceText, string actionLabel, bool enabled, Vector2 pos, System.Action onClick)
        {
            var go = new GameObject("Row");
            go.transform.SetParent(_content, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 1); rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(-20, 120);
            var bg = go.AddComponent<Image>();
            bg.color = new Color(0.08f, 0.18f, 0.38f, 0.92f);

            // Title (left)
            var titleRT = MakeRect(go.transform, "Title", new Vector2(0, 0), new Vector2(0.62f, 1), new Vector2(0, 0), Vector2.zero);
            titleRT.offsetMin = new Vector2(20, 8); titleRT.offsetMax = new Vector2(-20, -8);
            var t = AttachText(titleRT.gameObject, title, 22, Color.white, TextAlignmentOptions.Left);
            t.richText = true;

            // Price (middle)
            var priceRT = MakeRect(go.transform, "Price", new Vector2(0.62f, 0), new Vector2(0.78f, 1), Vector2.zero, Vector2.zero);
            priceRT.offsetMin = priceRT.offsetMax = Vector2.zero;
            var p = AttachText(priceRT.gameObject, priceText, 24, Color.white, TextAlignmentOptions.Center);
            p.richText = true;

            // Action button (right)
            var actGO = new GameObject("Action");
            actGO.transform.SetParent(go.transform, false);
            var actRT = actGO.AddComponent<RectTransform>();
            actRT.anchorMin = new Vector2(0.79f, 0.15f); actRT.anchorMax = new Vector2(0.98f, 0.85f);
            actRT.offsetMin = actRT.offsetMax = Vector2.zero;
            var actImg = actGO.AddComponent<Image>();
            actImg.color = enabled ? new Color(0.0f, 0.5f, 1.0f, 0.95f) : new Color(0.25f, 0.25f, 0.28f, 0.7f);
            var btn = actGO.AddComponent<Button>();
            btn.targetGraphic = actImg;
            btn.interactable = enabled;
            btn.onClick.AddListener(() => onClick?.Invoke());
            actGO.AddComponent<UIButtonAnimator>();
            AttachText(actGO, actionLabel, 24, Color.white, TextAlignmentOptions.Center);

            return go;
        }

        // ─── Helpers ────────────────────────────────────────────────────────────

        static RectTransform MakeRect(Transform parent, string name, Vector2 aMin, Vector2 aMax, Vector2 pos, Vector2 size)
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
                rt.offsetMin = pos;
                rt.offsetMax = size;
            }
            return rt;
        }

        static TMP_Text MakeText(Transform parent, string text, float ax, float ay, Vector2 pos, Vector2 size, float fontSize, Color color)
        {
            var go = new GameObject(text);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(ax, ay); rt.anchorMax = new Vector2(ax, ay);
            rt.pivot = new Vector2(0.5f, ay);
            rt.anchoredPosition = pos; rt.sizeDelta = size;
            return AttachText(go, text, fontSize, color, TextAlignmentOptions.Center);
        }

        static TMP_Text AttachText(GameObject host, string text, float size, Color color, TextAlignmentOptions align)
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
            go.AddComponent<UIButtonAnimator>();
            AttachText(go, label, 26, Color.white, TextAlignmentOptions.Center);
        }
    }
}
