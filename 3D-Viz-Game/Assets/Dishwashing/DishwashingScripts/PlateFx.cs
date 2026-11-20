using System.Collections;
using UnityEngine;

public class PlateFX : MonoBehaviour
{
    Coroutine scaleCo;
    Vector3 baseScale;

    void Awake() { baseScale = transform.localScale; }

    public void ScaleTo(float target, float time)
    {
        if (scaleCo != null) StopCoroutine(scaleCo);
        scaleCo = StartCoroutine(ScaleRoutine(target, time));
    }

    IEnumerator ScaleRoutine(float target, float time)
    {
        Vector3 start = transform.localScale;
        Vector3 end   = baseScale * target;
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / Mathf.Max(0.0001f, time);
            float k = Mathf.SmoothStep(0, 1, t);
            transform.localScale = Vector3.Lerp(start, end, k);
            yield return null;
        }
        transform.localScale = end;
        scaleCo = null;
    }

    public void ResetScale(float time = 0.15f) => ScaleTo(1f, time);
}
