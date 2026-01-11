using System;
using System.Collections;
using UnityEngine;

public sealed class NoteView : MonoBehaviour
{
    public Lane Lane { get; private set; }
    public double TimeSec { get; private set; }
    public NoteDivision Division { get; private set; }

    [Header("Division Colors")]
    [SerializeField] Color quarterColor = Color.red;
    [SerializeField] Color eighthColor = Color.blue;
    [SerializeField] Color sixteenthColor = Color.yellow;

    [Header("Hit Burst")]
    [SerializeField] float burstDuration = 0.12f;
    [SerializeField] float burstScale = 1.4f;

    [SerializeField] SpriteRenderer spriteRenderer;

    Vector3 baseScale;
    Coroutine burstRoutine;

    public void Init(Note note)
    {
        Lane = note.Lane;
        TimeSec = note.TimeSec;
        Division = note.Division;

        StopBurst();
        transform.localScale = baseScale;

        ApplyRotation();
        ApplyColor();
    }

    void Awake()
    {
        if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        baseScale = transform.localScale;
    }

    void OnEnable()
    {
        if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        baseScale = transform.localScale;
    }

    void OnValidate()
    {
        if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    public void PlayHitBurst(Color color, Action onComplete)
    {
        StopBurst();
        burstRoutine = StartCoroutine(CoHitBurst(color, onComplete));
    }

    private void ApplyRotation()
    {
        float z = Lane switch
        {
            Lane.Left => 0f,
            Lane.Down => 90f,
            Lane.Up => -90f,
            Lane.Right => 180f,
            _ => 0f
        };
        transform.localRotation = Quaternion.Euler(0, 0, z);
    }

    private void ApplyColor()
    {
        if (spriteRenderer == null) return;

        spriteRenderer.color = Division switch
        {
            NoteDivision.Quarter => quarterColor,
            NoteDivision.Eighth => eighthColor,
            NoteDivision.Sixteenth => sixteenthColor,
            _ => Color.white
        };
    }

    void StopBurst()
    {
        if (burstRoutine != null)
        {
            StopCoroutine(burstRoutine);
            burstRoutine = null;
        }
    }

    IEnumerator CoHitBurst(Color color, Action onComplete)
    {
        if (spriteRenderer == null) yield break;

        var startScale = baseScale;
        var endScale = baseScale * burstScale;

        float t = 0f;
        while (t < burstDuration)
        {
            t += Time.unscaledDeltaTime;
            float a = Mathf.Clamp01(t / burstDuration);
            transform.localScale = Vector3.Lerp(startScale, endScale, a);
            var c = color;
            c.a = Mathf.Lerp(1f, 0f, a);
            spriteRenderer.color = c;
            yield return null;
        }

        transform.localScale = baseScale;
        burstRoutine = null;
        onComplete?.Invoke();
    }
}
