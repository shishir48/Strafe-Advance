using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace StrafAdvance
{
    /// <summary>
    /// Queued bottom-center toast popups. Subscribes to AchievementUnlocked + DailyLoginCheckedIn,
    /// shows one card at a time for ~2.5s with slide-in / fade-out. Non-blocking — never grabs input.
    /// </summary>
    public class ToastNotifier : MonoBehaviour
    {
        public static ToastNotifier Instance { get; private set; }

        const float ShowDuration = 2.5f;
        const float SlideDuration = 0.3f;

        readonly Queue<(string title, string sub, Color accent)> _queue = new Queue<(string, string, Color)>();
        bool _running;

        RectTransform _cardRT;
        CanvasGroup   _cardGroup;
        TMP_Text      _titleLbl;
        TMP_Text      _subLbl;
        Image         _accentBar;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { Instance = null; }

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            if (Application.isPlaying) DontDestroyOnLoad(gameObject);
            BuildUI();
            EventBus<AchievementUnlocked>.Subscribe(OnAchievement);
            EventBus<DailyLoginCheckedIn>.Subscribe(OnDaily);
            EventBus<BattlePassTierReached>.Subscribe(OnTier);
            Hide();
        }

        void OnDestroy()
        {
            EventBus<AchievementUnlocked>.Unsubscribe(OnAchievement);
            EventBus<DailyLoginCheckedIn>.Unsubscribe(OnDaily);
            EventBus<BattlePassTierReached>.Unsubscribe(OnTier);
            if (Instance == this) Instance = null;
        }

        void OnAchievement(AchievementUnlocked a)
            => Enqueue($"ACHIEVEMENT: {a.DisplayName}", $"+{a.Reward} credits", new Color(1f, 0.85f, 0.3f));

        void OnDaily(DailyLoginCheckedIn d)
            => Enqueue($"DAY {d.Streak} LOGIN", $"+{d.Reward} credits", new Color(0.4f, 1f, 0.6f));

        void OnTier(BattlePassTierReached t)
            => Enqueue($"BATTLE PASS TIER {t.Tier}", t.Tier >= t.MaxTier ? "Season complete!" : "Claim rewards in the Battle Pass screen", new Color(0.5f, 0.9f, 1f));

        public void Enqueue(string title, string sub, Color accent)
        {
            _queue.Enqueue((title, sub, accent));
            if (!_running) StartCoroutine(Pump());
        }

        IEnumerator Pump()
        {
            _running = true;
            while (_queue.Count > 0)
            {
                var (title, sub, accent) = _queue.Dequeue();
                yield return ShowOne(title, sub, accent);
            }
            _running = false;
        }

        IEnumerator ShowOne(string title, string sub, Color accent)
        {
            _titleLbl.text  = title;
            _subLbl.text    = sub;
            _accentBar.color = accent;
            Show();

            // Slide up + fade in
            float t = 0f;
            while (t < SlideDuration)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / SlideDuration);
                _cardGroup.alpha = k;
                var p = _cardRT.anchoredPosition; p.y = Mathf.Lerp(-40f, 120f, k); _cardRT.anchoredPosition = p;
                yield return null;
            }
            yield return new WaitForSecondsRealtime(ShowDuration);
            // Fade out
            t = 0f;
            while (t < SlideDuration)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / SlideDuration);
                _cardGroup.alpha = 1f - k;
                yield return null;
            }
            Hide();
        }

        void Show() { if (_cardRT != null) _cardRT.gameObject.SetActive(true); }
        void Hide() { if (_cardRT != null) _cardRT.gameObject.SetActive(false); }

        void BuildUI()
        {
            var canvasGO = new GameObject("ToastCanvas");
            canvasGO.transform.SetParent(transform, false);
            var c = canvasGO.AddComponent<Canvas>();
            c.renderMode = RenderMode.ScreenSpaceOverlay;
            c.sortingOrder = 90;
            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            canvasGO.AddComponent<GraphicRaycaster>();

            var card = new GameObject("Card");
            card.transform.SetParent(canvasGO.transform, false);
            _cardRT = card.AddComponent<RectTransform>();
            _cardRT.anchorMin = new Vector2(0.5f, 0f); _cardRT.anchorMax = new Vector2(0.5f, 0f);
            _cardRT.pivot = new Vector2(0.5f, 0f);
            _cardRT.anchoredPosition = new Vector2(0, 120f);
            _cardRT.sizeDelta = new Vector2(640, 140);
            var bg = card.AddComponent<Image>();
            bg.color = new Color(0.04f, 0.08f, 0.14f, 0.96f);
            _cardGroup = card.AddComponent<CanvasGroup>();
            _cardGroup.blocksRaycasts = false;
            _cardGroup.interactable = false;

            // Left accent bar
            var accent = new GameObject("Accent");
            accent.transform.SetParent(card.transform, false);
            var aRT = accent.AddComponent<RectTransform>();
            aRT.anchorMin = new Vector2(0, 0); aRT.anchorMax = new Vector2(0, 1);
            aRT.pivot = new Vector2(0, 0.5f);
            aRT.sizeDelta = new Vector2(10, 0);
            aRT.anchoredPosition = Vector2.zero;
            _accentBar = accent.AddComponent<Image>();
            _accentBar.color = Color.white;

            _titleLbl = AddText(card.transform, new Vector2(0.04f, 0.50f), new Vector2(0.98f, 1f), 30, Color.white, TextAlignmentOptions.MidlineLeft);
            _titleLbl.fontStyle = FontStyles.Bold;
            _subLbl   = AddText(card.transform, new Vector2(0.04f, 0f),    new Vector2(0.98f, 0.50f), 22, new Color(0.8f, 0.85f, 0.95f), TextAlignmentOptions.MidlineLeft);
        }

        static TMP_Text AddText(Transform parent, Vector2 aMin, Vector2 aMax, float size, Color color, TextAlignmentOptions align)
        {
            var go = new GameObject("T");
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = aMin; rt.anchorMax = aMax;
            rt.offsetMin = new Vector2(20, 4); rt.offsetMax = new Vector2(-20, -4);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            var font = TMP_Settings.defaultFontAsset;
            if (font != null) tmp.font = font;
            tmp.fontSize = size; tmp.color = color; tmp.alignment = align;
            tmp.richText = true;
            return tmp;
        }
    }
}
