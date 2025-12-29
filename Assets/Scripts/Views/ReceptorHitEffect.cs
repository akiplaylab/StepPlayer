using System.Collections;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public sealed class ReceptorHitEffect : MonoBehaviour
{
    [Header("Flash")]
    [SerializeField] Color flashColor = new(1f, 0.95f, 0.4f, 1f);

    [Header("Punch Scale")]
    [SerializeField] float punchScale = 1.12f;
    [SerializeField] float punchUpTime = 0.04f;
    [SerializeField] float punchDownTime = 0.06f;

    SpriteRenderer sr;
    Color baseColor;
    Vector3 baseScale;
    Coroutine running;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        baseColor = sr.color;
        baseScale = transform.localScale;
    }

    void OnEnable()
    {
        if (sr == null) sr = GetComponent<SpriteRenderer>();
        baseColor = sr.color;
        baseScale = transform.localScale;
    }

    public void Play(float intensity01)
    {
        if (running != null) StopCoroutine(running);
        running = StartCoroutine(CoPlay(intensity01));
    }

    IEnumerator CoPlay(float intensity01)
    {
        var targetColor = Color.Lerp(baseColor, flashColor, Mathf.Clamp01(intensity01));

        var upScale = baseScale * Mathf.Lerp(1.02f, punchScale, Mathf.Clamp01(intensity01));

        float t = 0f;
        while (t < punchUpTime)
        {
            t += Time.unscaledDeltaTime;
            float a = Mathf.Clamp01(t / punchUpTime);
            transform.localScale = Vector3.Lerp(baseScale, upScale, a);
            sr.color = Color.Lerp(baseColor, targetColor, a);
            yield return null;
        }

        t = 0f;
        while (t < punchDownTime)
        {
            t += Time.unscaledDeltaTime;
            float a = Mathf.Clamp01(t / punchDownTime);
            transform.localScale = Vector3.Lerp(upScale, baseScale, a);
            sr.color = Color.Lerp(targetColor, baseColor, a);
            yield return null;
        }

        transform.localScale = baseScale;
        sr.color = baseColor;
        running = null;
    }
}
