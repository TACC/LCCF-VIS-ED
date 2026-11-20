using System.Collections;
using UnityEngine;

public static class PlateMover
{
    public static IEnumerator MoveWithArcRot(Transform t,
        Vector3 fromPos, Quaternion fromRot,
        Vector3 toPos, Quaternion toRot,
        float duration, AnimationCurve ease = null,
        float arcHeight = 0.1f, bool smallBounce = true)
    {
        if (t == null) yield break;
        ease ??= AnimationCurve.EaseInOut(0, 0, 1, 1);
        float start = Time.time;

        while (true)
        {
            if (t == null) yield break;
            float u = Mathf.InverseLerp(start, start + duration, Time.time);
            if (u >= 1f) break;

            float k = ease.Evaluate(u);
            Vector3 pos = Vector3.Lerp(fromPos, toPos, k) + Vector3.up * (Mathf.Sin(k * Mathf.PI) * arcHeight);
            t.position = pos;
            t.rotation = Quaternion.Slerp(fromRot, toRot, k);
            yield return null;
        }

        if (t == null) yield break;
        t.position = toPos;
        t.rotation = toRot;

        if (smallBounce)
        {
            float b = 0f; Vector3 basePos = toPos;
            while (b < 1f)
            {
                if (t == null) yield break;
                b += Time.deltaTime / 0.18f;
                t.position = basePos + Vector3.up * (Mathf.Sin(Mathf.Clamp01(b) * Mathf.PI) * 0.02f);
                yield return null;
            }
            if (t != null) t.position = basePos;
        }
    }
}
