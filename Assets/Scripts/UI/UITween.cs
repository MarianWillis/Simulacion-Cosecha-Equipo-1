using System.Collections;
using UnityEngine;

namespace FarmDashboard
{
    // Minimal coroutine-based tweening -- no external dependency needed for the
    // handful of fades/scale-ins the intro screen and hover states use.
    public static class UITween
    {
        public static float EaseOutCubic(float t) => 1f - Mathf.Pow(1f - t, 3f);
        public static float EaseOutQuint(float t) => 1f - Mathf.Pow(1f - t, 5f);

        public static IEnumerator ScaleFadeIn(RectTransform rt, CanvasGroup cg, float fromScale, float duration, float delay = 0f)
        {
            if (delay > 0f) yield return new WaitForSeconds(delay);
            float t = 0f;
            rt.localScale = Vector3.one * fromScale;
            cg.alpha = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                float p = EaseOutQuint(Mathf.Clamp01(t / duration));
                rt.localScale = Vector3.one * Mathf.Lerp(fromScale, 1f, p);
                cg.alpha = Mathf.Lerp(0f, 1f, p);
                yield return null;
            }
            rt.localScale = Vector3.one;
            cg.alpha = 1f;
        }

        public static IEnumerator FadeUp(RectTransform rt, CanvasGroup cg, float fromOffsetY, float duration, float delay = 0f)
        {
            cg.alpha = 0f;
            var startPos = rt.anchoredPosition;
            if (delay > 0f) yield return new WaitForSeconds(delay);
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                float p = EaseOutCubic(Mathf.Clamp01(t / duration));
                rt.anchoredPosition = new Vector2(startPos.x, startPos.y - Mathf.Lerp(fromOffsetY, 0f, p));
                cg.alpha = Mathf.Lerp(0f, 1f, p);
                yield return null;
            }
            rt.anchoredPosition = startPos;
            cg.alpha = 1f;
        }

        // Fade-only (no position offset) -- for elements whose position is owned by
        // a parent LayoutGroup, where nudging anchoredPosition would fight the
        // group's own layout pass. Use FadeUp instead for freely-positioned elements.
        public static IEnumerator FadeIn(CanvasGroup cg, float duration, float delay = 0f)
        {
            cg.alpha = 0f;
            if (delay > 0f) yield return new WaitForSeconds(delay);
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                cg.alpha = EaseOutCubic(Mathf.Clamp01(t / duration));
                yield return null;
            }
            cg.alpha = 1f;
        }

        public static IEnumerator FadeTo(CanvasGroup cg, float to, float duration)
        {
            float from = cg.alpha;
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                cg.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(t / duration));
                yield return null;
            }
            cg.alpha = to;
        }
    }
}
