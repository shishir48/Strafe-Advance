using System.Collections;
using UnityEngine;

namespace StrafAdvance
{
    public static class UITransition
    {
        // Slides rt from (its current anchoredPosition + fromOffset) to its current anchoredPosition.
        public static void SlideIn(MonoBehaviour owner, RectTransform rt, Vector2 fromOffset, float duration = 0.2f)
        {
            owner.StartCoroutine(SlideInRoutine(rt, fromOffset, duration));
        }

        static IEnumerator SlideInRoutine(RectTransform rt, Vector2 fromOffset, float duration)
        {
            Vector2 target = rt.anchoredPosition;
            rt.anchoredPosition = target + fromOffset;
            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                float p = 1f - Mathf.Pow(1f - Mathf.Clamp01(t / duration), 3f);
                rt.anchoredPosition = Vector2.LerpUnclamped(target + fromOffset, target, p);
                yield return null;
            }
            rt.anchoredPosition = target;
        }
    }
}
