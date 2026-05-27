using System.Collections;
using UnityEngine;

namespace StrafAdvance
{
    public static class UITransition
    {
        public static void SlideIn(MonoBehaviour owner, RectTransform rt, Vector2 fromOffset, float duration = 0.2f)
        {
            if (owner == null || rt == null || !owner.gameObject.activeInHierarchy) return;
            owner.StartCoroutine(SlideInRoutine(rt, fromOffset, duration));
        }

        static IEnumerator SlideInRoutine(RectTransform rt, Vector2 fromOffset, float duration)
        {
            if (rt == null) yield break;
            Vector2 target = rt.anchoredPosition;
            if (duration <= 0f) { rt.anchoredPosition = target; yield break; }
            rt.anchoredPosition = target + fromOffset;
            float t = 0f;
            while (t < duration)
            {
                if (rt == null) yield break;
                t += Time.unscaledDeltaTime;
                float p = 1f - Mathf.Pow(1f - Mathf.Clamp01(t / duration), 3f);
                rt.anchoredPosition = Vector2.LerpUnclamped(target + fromOffset, target, p);
                yield return null;
            }
            if (rt != null) rt.anchoredPosition = target;
        }
    }
}
