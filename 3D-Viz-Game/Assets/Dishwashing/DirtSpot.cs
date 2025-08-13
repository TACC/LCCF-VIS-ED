using UnityEngine;

[RequireComponent(typeof(Renderer))]
public class DirtSpot : MonoBehaviour
{
    public float fadeSpeed = 3f;
    public float radius = 0.12f;
    public float Alpha => _alpha;

    Material _mat;
    float _alpha = 1f;

    void Awake()
    {
        _mat = GetComponent<Renderer>().material;
        SetAlpha(_alpha);
    }

    void Update()
    {
        var brushes = BrushManager.ActiveBrushes;
        bool shouldFade = false;

        for (int i = 0; i < brushes.Count; i++)
        {
            var b = brushes[i];
            if (b == null || !b.IsScrubbing) continue;
            if (Vector3.Distance(b.transform.position, transform.position) <= radius)
            {
                shouldFade = true;
                break;
            }
        }

        if (shouldFade)
        {
            _alpha = Mathf.MoveTowards(_alpha, 0f, fadeSpeed * Time.deltaTime);
            SetAlpha(_alpha);
            if (_alpha <= 0.01f) Destroy(gameObject);
        }
    }

    void SetAlpha(float a)
    {
        if (_mat.HasProperty("_BaseColor"))
        {
            var c = _mat.GetColor("_BaseColor"); c.a = a; _mat.SetColor("_BaseColor", c);
        }
        else if (_mat.HasProperty("_Color"))
        {
            var c = _mat.color; c.a = a; _mat.color = c;
        }
    }

    public void ForceClean(float fadeTime = 0f)
    {
        if (fadeTime <= 0f)
        {
            _alpha = 0f;
            SetAlpha(_alpha);
            Destroy(gameObject);
        }
        else
        {
            StopAllCoroutines();
            StartCoroutine(FadeOut(fadeTime));
        }
    }

    System.Collections.IEnumerator FadeOut(float time)
    {
        float t = 0f;
        float start = _alpha;
        while (t < 1f)
        {
            t += Time.deltaTime / time;
            _alpha = Mathf.Lerp(start, 0f, t);
            SetAlpha(_alpha);
            yield return null;
        }
        _alpha = 0f;
        SetAlpha(0f);
        Destroy(gameObject);
    }
}
