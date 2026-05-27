using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace StrafAdvance
{
    public class DamageFlashVignette : MonoBehaviour
    {
        private Image         _vignette;
        private RectTransform _hpBarRT;
        private Vector2       _hpBarOrigin;
        private Coroutine     _flash;
        private Coroutine     _shake;
        private bool          _initialised;

        public void Init(Canvas canvas, RectTransform hpBarRT)
        {
            _hpBarRT     = hpBarRT;
            _initialised = true;

            if (hpBarRT != null) _hpBarOrigin = hpBarRT.anchoredPosition;

            var go = new GameObject("DamageVignette");
            go.transform.SetParent(canvas.transform, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            _vignette = go.AddComponent<Image>();
            _vignette.color = new Color(0.9f, 0.05f, 0.05f, 0f);
            _vignette.raycastTarget = false;

            EventBus<PlayerDamaged>.Subscribe(OnDamaged);
        }

        void OnDestroy()
        {
            if (_initialised) EventBus<PlayerDamaged>.Unsubscribe(OnDamaged);
            if (_vignette != null) Destroy(_vignette.gameObject);
        }

        void OnDamaged(PlayerDamaged _)
        {
            if (_vignette == null) return;
            if (_flash != null) StopCoroutine(_flash);
            if (_shake != null) { StopCoroutine(_shake); if (_hpBarRT != null) _hpBarRT.anchoredPosition = _hpBarOrigin; }
            _flash = StartCoroutine(FlashRoutine());
            if (_hpBarRT != null) _shake = StartCoroutine(ShakeRoutine());
        }

        IEnumerator FlashRoutine()
        {
            const float fadeIn = 0.05f, fadeOut = 0.20f, peak = 0.45f;
            for (float t = 0; t < fadeIn; t += Time.unscaledDeltaTime)
            {
                SetAlpha(Mathf.Lerp(0f, peak, t / fadeIn));
                yield return null;
            }
            for (float t = 0; t < fadeOut; t += Time.unscaledDeltaTime)
            {
                SetAlpha(Mathf.Lerp(peak, 0f, t / fadeOut));
                yield return null;
            }
            SetAlpha(0f);
            _flash = null;
        }

        IEnumerator ShakeRoutine()
        {
            const float dur = 0.25f;
            const float mag = 8f;
            for (float t = 0; t < dur; t += Time.unscaledDeltaTime)
            {
                if (_hpBarRT == null) yield break;
                float decay = 1f - t / dur;
                _hpBarRT.anchoredPosition = _hpBarOrigin + new Vector2(
                    Random.Range(-mag, mag) * decay,
                    Random.Range(-mag * 0.4f, mag * 0.4f) * decay);
                yield return null;
            }
            if (_hpBarRT != null) _hpBarRT.anchoredPosition = _hpBarOrigin;
            _shake = null;
        }

        void SetAlpha(float a)
        {
            if (_vignette == null) return;
            var c = _vignette.color; c.a = a; _vignette.color = c;
        }
    }
}
