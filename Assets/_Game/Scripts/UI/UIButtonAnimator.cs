using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

namespace StrafAdvance
{
    public class UIButtonAnimator : MonoBehaviour,
        IPointerEnterHandler, IPointerExitHandler,
        IPointerDownHandler, IPointerUpHandler
    {
        private Coroutine _tween;
        private bool      _isHovered;
        private static readonly Vector3 Hover  = new Vector3(1.05f, 1.05f, 1f);
        private static readonly Vector3 Press  = new Vector3(0.95f, 0.95f, 1f);
        private static readonly Vector3 Normal = Vector3.one;

        public void OnPointerEnter(PointerEventData _) { _isHovered = true;  Animate(Hover,  0.15f); }
        public void OnPointerExit (PointerEventData _) { _isHovered = false; Animate(Normal, 0.15f); }
        public void OnPointerDown (PointerEventData _) => Animate(Press, 0.08f);
        public void OnPointerUp   (PointerEventData _) => Animate(_isHovered ? Hover : Normal, 0.08f);

        void Animate(Vector3 target, float duration)
        {
            if (_tween != null) StopCoroutine(_tween);
            _tween = StartCoroutine(ScaleTo(target, duration));
        }

        IEnumerator ScaleTo(Vector3 target, float duration)
        {
            Vector3 start = transform.localScale;
            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                float p = 1f - Mathf.Pow(1f - Mathf.Clamp01(t / duration), 3f);
                transform.localScale = Vector3.LerpUnclamped(start, target, p);
                yield return null;
            }
            transform.localScale = target;
            _tween = null;
        }
    }
}
