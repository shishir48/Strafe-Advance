using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace StrafAdvance
{
    /// <summary>
    /// Pre-run loadout selector. Lists unlocked weapons + currently equipped perks.
    /// Tap a weapon card to equip; persists via SaveSystem. Start Run hands control to GameManager.
    /// </summary>
    public class LoadoutPanel : MonoBehaviour
    {
        public static LoadoutPanel Instance { get; private set; }

        private GameObject _root;
        private RectTransform _weaponList;
        private RectTransform _perksList;
        private TMP_Text _equippedLabel;
        private readonly List<GameObject> _cardPool = new List<GameObject>();
        private readonly List<GameObject> _perkRowPool = new List<GameObject>();

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

        public void Show()
        {
            _root.SetActive(true);
            Rebuild();
        }

        public void Hide() { if (_root != null) _root.SetActive(false); }

        void Rebuild()
        {
            // Weapons
            foreach (var go in _cardPool) Destroy(go);
            _cardPool.Clear();

            var unlocked = SaveSystem.Current.progress.unlockedWeaponIds;
            string equipped = SaveSystem.Current.progress.equippedWeaponId;
            float y = 0f;
            foreach (var w in WeaponCatalog.All)
            {
                if (!unlocked.Contains(w.id)) continue;
                bool isEq = w.id == equipped;
                var card = MakeWeaponCard(w, isEq, new Vector2(0, y));
                _cardPool.Add(card);
                y -= 150f;
            }
            _equippedLabel.text = $"Equipped: <b>{WeaponCatalog.Find(equipped).displayName}</b>";

            // Perks
            foreach (var go in _perkRowPool) Destroy(go);
            _perkRowPool.Clear();
            var eqPerks = SaveSystem.Current.progress.equippedPerkIds;
            if (eqPerks.Count == 0)
            {
                var none = MakeText(_perksList, "(none equipped — level up to unlock)", new Vector2(0, 0), 22, new Color(0.6f, 0.7f, 0.85f, 0.9f));
                _perkRowPool.Add(none.gameObject);
            }
            else
            {
                float py = 0f;
                foreach (var pid in eqPerks)
                {
                    var perk = PerkCatalog.Find(pid);
                    if (perk == null) continue;
                    var row = MakeText(_perksList, $"<color=#4fc3f7>●</color> {perk.displayName}  <size=18><color=#8aa>{perk.description}</color></size>", new Vector2(0, py), 22, Color.white);
                    _perkRowPool.Add(row.gameObject);
                    py -= 40f;
                }
            }
        }

        void EquipWeapon(string id)
        {
            SaveSystem.Current.progress.equippedWeaponId = id;
            SaveSystem.Save();
            var shooter = FindFirstObjectByType<AutoShooter>();
            if (shooter != null) shooter.RefreshLoadout();
            Rebuild();
        }

        void StartRun()
        {
            Hide();
            if (MainHubController.Instance != null) MainHubController.Instance.Hide();
            if (GameManager.Instance != null) GameManager.Instance.BeginRunFromMenu();
        }

        void Back()
        {
            Hide();
            if (MainHubController.Instance != null) MainHubController.Instance.Show();
        }

        // ─── UI Construction ────────────────────────────────────────────────────

        void BuildUI()
        {
            var canvasGO = new GameObject("LoadoutCanvas");
            canvasGO.transform.SetParent(transform, false);
            var c = canvasGO.AddComponent<Canvas>();
            c.renderMode = RenderMode.ScreenSpaceOverlay;
            c.sortingOrder = 70;
            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            canvasGO.AddComponent<GraphicRaycaster>();
            _root = canvasGO;

            // Dim
            var dim = new GameObject("Dim");
            dim.transform.SetParent(canvasGO.transform, false);
            var dimRT = dim.AddComponent<RectTransform>();
            dimRT.anchorMin = Vector2.zero; dimRT.anchorMax = Vector2.one;
            dimRT.offsetMin = dimRT.offsetMax = Vector2.zero;
            dim.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.82f);

            // Panel
            var panel = new GameObject("Panel");
            panel.transform.SetParent(canvasGO.transform, false);
            var prt = panel.AddComponent<RectTransform>();
            prt.anchorMin = new Vector2(0.05f, 0.08f); prt.anchorMax = new Vector2(0.95f, 0.92f);
            prt.offsetMin = prt.offsetMax = Vector2.zero;
            panel.AddComponent<Image>().color = new Color(0.03f, 0.07f, 0.13f, 0.97f);

            // Title
            var title = MakeText(panel.transform, "LOADOUT", new Vector2(0, -30f), 56, new Color(0.31f, 0.76f, 0.97f));
            title.alignment = TextAlignmentOptions.Center;
            title.rectTransform.anchorMin = new Vector2(0, 1); title.rectTransform.anchorMax = new Vector2(1, 1);
            title.rectTransform.pivot = new Vector2(0.5f, 1f);
            title.rectTransform.sizeDelta = new Vector2(-40, 80);
            title.fontStyle = FontStyles.Bold;

            // Equipped label
            _equippedLabel = MakeText(panel.transform, "Equipped: ?", new Vector2(0, -110f), 26, new Color(1f, 0.85f, 0.3f));
            _equippedLabel.alignment = TextAlignmentOptions.Center;
            _equippedLabel.rectTransform.anchorMin = new Vector2(0, 1); _equippedLabel.rectTransform.anchorMax = new Vector2(1, 1);
            _equippedLabel.rectTransform.pivot = new Vector2(0.5f, 1f);
            _equippedLabel.rectTransform.sizeDelta = new Vector2(-40, 40);
            _equippedLabel.richText = true;

            // Weapons header
            var wh = MakeText(panel.transform, "WEAPONS", new Vector2(40, -170f), 28, new Color(0.6f, 0.7f, 0.85f));
            wh.alignment = TextAlignmentOptions.Left;
            wh.rectTransform.anchorMin = new Vector2(0, 1); wh.rectTransform.anchorMax = new Vector2(0.5f, 1);
            wh.rectTransform.pivot = new Vector2(0, 1);
            wh.rectTransform.sizeDelta = new Vector2(-40, 36);

            // Weapon list region (left half)
            var wlGO = new GameObject("Weapons");
            wlGO.transform.SetParent(panel.transform, false);
            _weaponList = wlGO.AddComponent<RectTransform>();
            _weaponList.anchorMin = new Vector2(0.02f, 0.18f); _weaponList.anchorMax = new Vector2(0.55f, 0.85f);
            _weaponList.offsetMin = _weaponList.offsetMax = Vector2.zero;

            // Perks header (right half)
            var ph = MakeText(panel.transform, "EQUIPPED PERKS", new Vector2(0, -170f), 28, new Color(0.6f, 0.7f, 0.85f));
            ph.alignment = TextAlignmentOptions.Left;
            ph.rectTransform.anchorMin = new Vector2(0.55f, 1); ph.rectTransform.anchorMax = new Vector2(1f, 1);
            ph.rectTransform.pivot = new Vector2(0, 1);
            ph.rectTransform.anchoredPosition = new Vector2(20, -170f);
            ph.rectTransform.sizeDelta = new Vector2(-40, 36);

            // Perks list region (right half)
            var plGO = new GameObject("Perks");
            plGO.transform.SetParent(panel.transform, false);
            _perksList = plGO.AddComponent<RectTransform>();
            _perksList.anchorMin = new Vector2(0.58f, 0.30f); _perksList.anchorMax = new Vector2(0.98f, 0.85f);
            _perksList.offsetMin = _perksList.offsetMax = Vector2.zero;

            // Perk note
            var note = MakeText(panel.transform, "Equip perks via Pause → Perks (max 3)", new Vector2(0, 30f), 18, new Color(0.6f, 0.7f, 0.85f, 0.7f));
            note.alignment = TextAlignmentOptions.Center;
            note.rectTransform.anchorMin = new Vector2(0.55f, 0.18f); note.rectTransform.anchorMax = new Vector2(1f, 0.28f);
            note.rectTransform.offsetMin = note.rectTransform.offsetMax = Vector2.zero;

            // Bottom buttons
            MakeButton(panel.transform, "BACK", new Vector2(0.05f, 0.02f), new Vector2(0.3f, 0.13f), new Color(0.15f, 0.2f, 0.3f), Back);
            MakeButton(panel.transform, "START RUN", new Vector2(0.55f, 0.02f), new Vector2(0.95f, 0.13f), new Color(0.18f, 0.6f, 0.85f), StartRun);
        }

        GameObject MakeWeaponCard(WeaponConfig w, bool isEquipped, Vector2 pos)
        {
            var go = new GameObject("Card_" + w.id);
            go.transform.SetParent(_weaponList, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 1); rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(-20, 140);
            var img = go.AddComponent<Image>();
            img.color = isEquipped ? new Color(0.10f, 0.30f, 0.50f, 0.95f) : new Color(0.06f, 0.13f, 0.22f, 0.92f);
            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            string capturedId = w.id;
            btn.onClick.AddListener(() => EquipWeapon(capturedId));

            var name = MakeText(go.transform, $"{(isEquipped ? "<color=#4fc3f7>[EQUIPPED] </color>" : "")}{w.displayName}", new Vector2(20, -10), 28, Color.white);
            name.alignment = TextAlignmentOptions.TopLeft;
            name.rectTransform.anchorMin = new Vector2(0, 1); name.rectTransform.anchorMax = new Vector2(1, 1);
            name.rectTransform.pivot = new Vector2(0, 1);
            name.rectTransform.sizeDelta = new Vector2(-40, 40);
            name.fontStyle = FontStyles.Bold;
            name.richText = true;

            var stats = MakeText(go.transform, $"<color=#8aa>DMG {w.damage}  ·  FR {1f / w.fireRate:F1}/s  ·  {(w.multishotCount > 1 ? $"x{w.multishotCount} spread" : (w.homingStrength > 0 ? "homing" : "single"))}</color>", new Vector2(20, -54), 20, Color.white);
            stats.alignment = TextAlignmentOptions.TopLeft;
            stats.rectTransform.anchorMin = new Vector2(0, 1); stats.rectTransform.anchorMax = new Vector2(1, 1);
            stats.rectTransform.pivot = new Vector2(0, 1);
            stats.rectTransform.sizeDelta = new Vector2(-40, 30);
            stats.richText = true;

            var desc = MakeText(go.transform, $"<size=18><color=#aab>{w.description}</color></size>", new Vector2(20, -88), 18, Color.white);
            desc.alignment = TextAlignmentOptions.TopLeft;
            desc.rectTransform.anchorMin = new Vector2(0, 1); desc.rectTransform.anchorMax = new Vector2(1, 1);
            desc.rectTransform.pivot = new Vector2(0, 1);
            desc.rectTransform.sizeDelta = new Vector2(-40, 36);
            desc.richText = true;

            return go;
        }

        static TMP_Text MakeText(Transform parent, string text, Vector2 anchoredPos, float size, Color color)
        {
            var go = new GameObject("T");
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchoredPosition = anchoredPos;
            var tmp = go.AddComponent<TextMeshProUGUI>();
            var font = TMP_Settings.defaultFontAsset;
            if (font != null) tmp.font = font;
            tmp.text = text;
            tmp.fontSize = size;
            tmp.color = color;
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
            var img = go.AddComponent<Image>();
            img.color = bg;
            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(() => onClick?.Invoke());

            var t = new GameObject("Label");
            t.transform.SetParent(go.transform, false);
            var trt = t.AddComponent<RectTransform>();
            trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
            trt.offsetMin = trt.offsetMax = Vector2.zero;
            var tmp = t.AddComponent<TextMeshProUGUI>();
            var font = TMP_Settings.defaultFontAsset;
            if (font != null) tmp.font = font;
            tmp.text = label; tmp.fontSize = 30;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            tmp.fontStyle = FontStyles.Bold;
        }
    }
}
