using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace StrafAdvance
{
    /// <summary>
    /// Runtime-built UI for equipping unlocked perks. Lists every unlocked perk as a row;
    /// tap toggles equipped state. Save persists on change. Max equipped enforced by <see cref="maxEquipped"/>.
    /// Shown on level-up + via pause menu (Phase 4 wiring).
    /// </summary>
    public class PerkEquipPanel : MonoBehaviour
    {
        public static PerkEquipPanel Instance { get; private set; }

        [SerializeField] private int maxEquipped = 3;

        private RectTransform _root;
        private RectTransform _listRoot;
        private readonly List<GameObject> _rowPool = new List<GameObject>();
        private bool _isOpen;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { Instance = null; }

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            if (Application.isPlaying) DontDestroyOnLoad(gameObject);
            BuildUI();
            Close();
            EventBus<PlayerLeveledUp>.Subscribe(OnLevelUp);
        }

        void OnDestroy()
        {
            EventBus<PlayerLeveledUp>.Unsubscribe(OnLevelUp);
            if (Instance == this) Instance = null;
        }

        void OnLevelUp(PlayerLeveledUp _) => Open();

        public void Open()
        {
            _isOpen = true;
            _root.gameObject.SetActive(true);
            RebuildRows();
        }

        public void Close()
        {
            _isOpen = false;
            if (_root != null) _root.gameObject.SetActive(false);
        }

        public void Toggle() { if (_isOpen) Close(); else Open(); }

        void BuildUI()
        {
            var canvasGO = new GameObject("PerkEquipCanvas");
            canvasGO.transform.SetParent(transform, false);
            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 50;
            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            canvasGO.AddComponent<GraphicRaycaster>();

            var panel = new GameObject("Panel");
            panel.transform.SetParent(canvasGO.transform, false);
            _root = panel.AddComponent<RectTransform>();
            _root.anchorMin = new Vector2(0.1f, 0.2f);
            _root.anchorMax = new Vector2(0.9f, 0.8f);
            _root.offsetMin = _root.offsetMax = Vector2.zero;
            var bg = panel.AddComponent<Image>();
            bg.color = new Color(0.03f, 0.07f, 0.13f, 0.95f);

            // Title
            var title = new GameObject("Title");
            title.transform.SetParent(panel.transform, false);
            var titleRT = title.AddComponent<RectTransform>();
            titleRT.anchorMin = new Vector2(0, 1); titleRT.anchorMax = new Vector2(1, 1);
            titleRT.pivot = new Vector2(0.5f, 1f);
            titleRT.anchoredPosition = new Vector2(0, -10);
            titleRT.sizeDelta = new Vector2(0, 80);
            var titleTMP = title.AddComponent<TextMeshProUGUI>();
            titleTMP.text = "PERKS  (tap to equip)";
            titleTMP.fontSize = 44;
            titleTMP.alignment = TextAlignmentOptions.Center;
            titleTMP.color = new Color(0.31f, 0.76f, 0.97f);

            // List root
            var list = new GameObject("List");
            list.transform.SetParent(panel.transform, false);
            _listRoot = list.AddComponent<RectTransform>();
            _listRoot.anchorMin = new Vector2(0, 0); _listRoot.anchorMax = new Vector2(1, 1);
            _listRoot.offsetMin = new Vector2(20, 80);
            _listRoot.offsetMax = new Vector2(-20, -100);

            // Close button
            var close = MakeButton(panel.transform, "CLOSE", new Vector2(1, 0), new Vector2(1, 0),
                                   new Vector2(-20, 20), new Vector2(200, 60), Close);
            close.GetComponent<RectTransform>().pivot = new Vector2(1, 0);
        }

        void RebuildRows()
        {
            foreach (var go in _rowPool) Destroy(go);
            _rowPool.Clear();

            var unlocked = SaveSystem.Current.progress.unlockedPerkIds;
            var equipped = SaveSystem.Current.progress.equippedPerkIds;

            float y = 0f;
            foreach (var id in unlocked)
            {
                var perk = PerkCatalog.Find(id);
                if (perk == null) continue;
                bool isEquipped = equipped.Contains(id);

                var row = MakeButton(_listRoot,
                    $"{(isEquipped ? "<color=#4fc3f7>[EQ]</color> " : "")}{perk.displayName}\n<size=22>{perk.description}</size>",
                    new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, y), new Vector2(0, 110),
                    () => ToggleEquip(id));
                row.GetComponent<RectTransform>().pivot = new Vector2(0.5f, 1f);
                _rowPool.Add(row);
                y -= 120f;
            }
        }

        void ToggleEquip(string perkId)
        {
            var eq = SaveSystem.Current.progress.equippedPerkIds;
            if (eq.Contains(perkId)) eq.Remove(perkId);
            else if (eq.Count < maxEquipped) eq.Add(perkId);
            SaveSystem.Save();
            // Live-refresh shooter loadout if mid-run.
            var shooter = Object.FindFirstObjectByType<AutoShooter>();
            if (shooter != null) shooter.RefreshLoadout();
            RebuildRows();
        }

        static GameObject MakeButton(Transform parent, string label, Vector2 anchorMin, Vector2 anchorMax,
                                     Vector2 anchoredPos, Vector2 sizeDelta, System.Action onClick)
        {
            var go = new GameObject("Btn");
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin; rt.anchorMax = anchorMax;
            rt.anchoredPosition = anchoredPos; rt.sizeDelta = sizeDelta;
            var img = go.AddComponent<Image>();
            img.color = new Color(0.06f, 0.13f, 0.22f, 0.9f);
            var btn = go.AddComponent<Button>();
            btn.onClick.AddListener(() => onClick?.Invoke());

            var textGO = new GameObject("Label");
            textGO.transform.SetParent(go.transform, false);
            var trt = textGO.AddComponent<RectTransform>();
            trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
            trt.offsetMin = new Vector2(20, 5); trt.offsetMax = new Vector2(-20, -5);
            var tmp = textGO.AddComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.fontSize = 28;
            tmp.alignment = TextAlignmentOptions.Left;
            tmp.color = Color.white;
            tmp.richText = true;
            return go;
        }
    }
}
