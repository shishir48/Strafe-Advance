using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace StrafAdvance
{
    /// <summary>
    /// Bootstrap-scene entry. Shows a procedural wordmark splash (fade-in → hold → fade-out,
    /// ~1.5s total) before loading the GameScene, so the first frame the player sees feels
    /// intentional rather than a hard cut into the menu.
    /// </summary>
    public class BootstrapLoader : MonoBehaviour
    {
        const float FadeIn  = 0.6f;
        const float Hold    = 0.4f;
        const float FadeOut = 0.5f;

        CanvasGroup   _group;
        RectTransform _wordmark;
        RectTransform _accent;

        void Start()
        {
            BuildSplash();
            StartCoroutine(Run());
        }

        IEnumerator Run()
        {
            // Fade in + gentle slide-down settle.
            float t = 0f;
            while (t < FadeIn)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / FadeIn);
                float ease = 1f - (1f - k) * (1f - k);
                _group.alpha = ease;
                if (_wordmark != null) { var p = _wordmark.anchoredPosition; p.y = Mathf.Lerp(60f, 0f, ease); _wordmark.anchoredPosition = p; }
                if (_accent != null) _accent.localScale = new Vector3(ease, 1f, 1f); // line wipe L→R
                yield return null;
            }
            _group.alpha = 1f;

            yield return new WaitForSecondsRealtime(Hold);

            // Fade out.
            t = 0f;
            while (t < FadeOut)
            {
                t += Time.unscaledDeltaTime;
                _group.alpha = 1f - Mathf.Clamp01(t / FadeOut);
                yield return null;
            }

            SceneManager.LoadScene("GameScene");
        }

        void BuildSplash()
        {
            var canvasGO = new GameObject("SplashCanvas");
            canvasGO.transform.SetParent(transform, false);
            var c = canvasGO.AddComponent<Canvas>();
            c.renderMode = RenderMode.ScreenSpaceOverlay;
            c.sortingOrder = 100;
            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            _group = canvasGO.AddComponent<CanvasGroup>();
            _group.alpha = 0f;

            // Full-screen dark background.
            var bg = new GameObject("BG");
            bg.transform.SetParent(canvasGO.transform, false);
            var bgRT = bg.AddComponent<RectTransform>();
            bgRT.anchorMin = Vector2.zero; bgRT.anchorMax = Vector2.one;
            bgRT.offsetMin = bgRT.offsetMax = Vector2.zero;
            bg.AddComponent<Image>().color = new Color(0.04f, 0.06f, 0.16f, 1f);

            // Wordmark (two-line, center).
            var wmGO = new GameObject("Wordmark");
            wmGO.transform.SetParent(canvasGO.transform, false);
            _wordmark = wmGO.AddComponent<RectTransform>();
            _wordmark.anchorMin = _wordmark.anchorMax = new Vector2(0.5f, 0.5f);
            _wordmark.pivot = new Vector2(0.5f, 0.5f);
            _wordmark.sizeDelta = new Vector2(960, 320);
            var tmp = wmGO.AddComponent<TextMeshProUGUI>();
            var font = TMP_Settings.defaultFontAsset;
            if (font != null) tmp.font = font;
            tmp.text = "STRAFE\nADVANCE";
            tmp.fontSize = 120;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = new Color(0.0f, 0.9f, 1.0f);
            tmp.characterSpacing = 6f;

            // Accent line under the wordmark (wipes in L→R).
            var accentGO = new GameObject("Accent");
            accentGO.transform.SetParent(canvasGO.transform, false);
            _accent = accentGO.AddComponent<RectTransform>();
            _accent.anchorMin = _accent.anchorMax = new Vector2(0.5f, 0.5f);
            _accent.pivot = new Vector2(0.5f, 0.5f);
            _accent.anchoredPosition = new Vector2(0, -170f);
            _accent.sizeDelta = new Vector2(420, 6);
            _accent.localScale = new Vector3(0f, 1f, 1f);
            accentGO.AddComponent<Image>().color = new Color(1f, 0.85f, 0.3f);
        }
    }
}
