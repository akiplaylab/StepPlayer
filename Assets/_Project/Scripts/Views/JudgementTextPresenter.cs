using System.Collections;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(TMP_Text))]
public sealed class JudgementTextPresenter : MonoBehaviour
{
    [Header("Text")]
    [SerializeField] string marvelousText = "MARVELOUS!!";
    [SerializeField] string perfectText = "PERFECT!";
    [SerializeField] string greatText = "GREAT!";
    [SerializeField] string goodText = "GOOD";
    [SerializeField] string badText = "BAD";

    [Header("Colors")]
    [SerializeField] JudgementStyle style;

    [Header("Punch Scale")]

    [Tooltip("Marvelous の最大拡大率")]
    [SerializeField] float marvelousPunchScale = 1.35f;

    [Tooltip("Perfect の最大拡大率")]
    [SerializeField] float perfectPunchScale = 1.25f;

    [Tooltip("Great の最大拡大率")]
    [SerializeField] float greatPunchScale = 1.15f;

    [Tooltip("Good/Bad はパンチしない")]
    [SerializeField] float normalScale = 1.0f;

    [Header("Timing")]
    [SerializeField] float showDuration = 0.25f;
    [SerializeField] float punchUpTime = 0.05f;
    [SerializeField] float punchDownTime = 0.08f;

    TMP_Text text;
    Vector3 baseScale;
    Coroutine running;

    void Awake()
    {
        text = GetComponent<TMP_Text>();
        baseScale = transform.localScale;
        HideImmediate();
    }

    public void Show(Judgement j)
    {
        if (j == Judgement.None) return;

        if (running != null) StopCoroutine(running);
        running = StartCoroutine(CoShow(j));
    }

    void HideImmediate()
    {
        text.text = "";
        text.alpha = 0f;
        transform.localScale = baseScale;
    }

    IEnumerator CoShow(Judgement j)
    {
        // set content
        (string s, float targetScale) = j switch
        {
            Judgement.Marvelous => (marvelousText, marvelousPunchScale),
            Judgement.Perfect => (perfectText, perfectPunchScale),
            Judgement.Great => (greatText, greatPunchScale),
            Judgement.Good => (goodText, normalScale),
            Judgement.Bad => (badText, normalScale),
            _ => ("", normalScale)
        };

        text.text = s;
        text.color = style.GetColor(j);
        text.alpha = 1f;

        if (targetScale > normalScale)
        {
            var up = baseScale * targetScale;

            float t = 0f;
            while (t < punchUpTime)
            {
                t += Time.unscaledDeltaTime;
                float a = Mathf.Clamp01(t / punchUpTime);
                transform.localScale = Vector3.Lerp(baseScale, up, a);
                yield return null;
            }

            t = 0f;
            while (t < punchDownTime)
            {
                t += Time.unscaledDeltaTime;
                float a = Mathf.Clamp01(t / punchDownTime);
                transform.localScale = Vector3.Lerp(up, baseScale, a);
                yield return null;
            }
        }
        else
        {
            transform.localScale = baseScale;
        }

        float elapsed = 0f;
        while (elapsed < showDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        float fadeTime = 0.08f;
        float ft = 0f;
        while (ft < fadeTime)
        {
            ft += Time.unscaledDeltaTime;
            text.alpha = 1f - Mathf.Clamp01(ft / fadeTime);
            yield return null;
        }

        HideImmediate();
        running = null;
    }
}
