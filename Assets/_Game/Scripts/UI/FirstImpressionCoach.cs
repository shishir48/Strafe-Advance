using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace StrafAdvance
{
    /// <summary>
    /// One-time first-run dopamine + guidance. On the player's very first kill ever: a big
    /// "FIRST BLOOD!" punch-in banner plus a 0.3s freeze frame. On their first death ever: a
    /// soft toast nudging them toward Pause → Perks. Both gated on persistent profile flags so
    /// they fire exactly once across all sessions.
    /// </summary>
    public class FirstImpressionCoach : MonoBehaviour
    {
        public static FirstImpressionCoach Instance { get; private set; }

        GameObject    _root;
        RectTransform _bannerRT;
        CanvasGroup   _bannerGroup;
        TMP_Text      _bannerLbl;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { Instance = null; }

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            if (Application.isPlaying) DontDestroyOnLoad(gameObject);
            BuildUI();
            EventBus<EnemyKilled>.Subscribe(OnKill);
            EventBus<GameStateChanged>.Subscribe(OnState);
            Hide();
        }

        void OnDestroy()
        {
            EventBus<EnemyKilled>.Unsubscribe(OnKill);
            EventBus<GameStateChanged>.Unsubscribe(OnState);
            if (Instance == this) Instance = null;
        }

        void OnKill(EnemyKilled _)
        {
            if (SaveSystem.Current.profile.firstBloodSeen) return;
            SaveSystem.Current.profile.firstBloodSeen = true;
            SaveSystem.Save();
            if (Hitstop.Instance != null) Hitstop.Instance.Freeze(0.3f);
            StartCoroutine(PlayBanner());
        }

        void OnState(GameStateChanged e)
        {
            if (e.Current != GameState.GameOver) return;
            if (SaveSystem.Current.profile.firstDeathCoached) return;
            SaveSystem.Current.profile.firstDeathCoached = true;
            SaveSystem.Save();
            if (ToastNotifier.Instance != null)
                ToastNotifier.Instance.Enqueue("Try a perk?", "Pause → Perks to equip a buff", new Color(0.6f, 0.8f, 1f));
        }

        IEnumerator PlayBanner()
        {
            Show();
            _bannerLbl.text = "FIRST BLOOD!";

            // Punch-in: scale 0.6 → 1.12 → 1.0 with fade-in over 0.35s (unscaled — freeze is active).
            float t = 0f;
            while (t < 0.35f)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / 0.35f);
                _bannerGroup.alpha = k;
                float s = k < 0.7f ? Mathf.Lerp(0.6f, 1.12f, k / 0.7f) : Mathf.Lerp(1.12f, 1f, (k - 0.7f) / 0.3f);
                _bannerRT.localScale = Vector3.one * s;
                yield return null;
            }
            _bannerRT.localScale = Vector3.one;

            yield return new WaitForSecondsRealtime(0.8f);

            t = 0f;
            while (t < 0.4f)
            {
                t += Time.unscaledDeltaTime;
                _bannerGroup.alpha = 1f - Mathf.Clamp01(t / 0.4f);
                yield return null;
            }
            Hide();
        }

        void Show() { if (_root != null) _root.SetActive(true); }
        void Hide() { if (_root != null) _root.SetActive(false); }

        void BuildUI()
        {
            var canvasGO = new GameObject("FirstBloodCanvas");
            canvasGO.transform.SetParent(transform, false);
            var c = canvasGO.AddComponent<Canvas>();
            c.renderMode = RenderMode.ScreenSpaceOverlay;
            c.sortingOrder = 95; // above HUD, below toasts (90 is toast; banner is hero so keep above)
            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            canvasGO.AddComponent<GraphicRaycaster>();
            _root = canvasGO;

            var banner = new GameObject("Banner");
            banner.transform.SetParent(canvasGO.transform, false);
            _bannerRT = banner.AddComponent<RectTransform>();
            _bannerRT.anchorMin = _bannerRT.anchorMax = new Vector2(0.5f, 0.62f);
            _bannerRT.pivot = new Vector2(0.5f, 0.5f);
            _bannerRT.sizeDelta = new Vector2(900, 200);
            _bannerGroup = banner.AddComponent<CanvasGroup>();
            _bannerGroup.blocksRaycasts = false;
            _bannerGroup.interactable = false;

            var go = new GameObject("Label");
            go.transform.SetParent(banner.transform, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            _bannerLbl = go.AddComponent<TextMeshProUGUI>();
            var font = TMP_Settings.defaultFontAsset;
            if (font != null) _bannerLbl.font = font;
            _bannerLbl.text = "FIRST BLOOD!";
            _bannerLbl.fontSize = 110;
            _bannerLbl.fontStyle = FontStyles.Bold;
            _bannerLbl.alignment = TextAlignmentOptions.Center;
            _bannerLbl.color = new Color(1f, 0.25f, 0.2f);
            _bannerLbl.raycastTarget = false;
            _bannerLbl.enableVertexGradient = true;
        }
    }
}
