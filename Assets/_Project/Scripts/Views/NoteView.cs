using System;
using System.Collections;
using UnityEngine;

public sealed class NoteView : MonoBehaviour
{
    public Lane Lane { get; private set; }
    public double Beat { get; private set; }
    public double TimeSec { get; private set; }
    public NoteDivision Division { get; private set; }

    [Header("Division Colors")]
    [SerializeField] Color quarterColor = Color.red;
    [SerializeField] Color eighthColor = Color.blue;
    [SerializeField] Color sixteenthColor = Color.yellow;
    [SerializeField] Color otherDivisionColor = Color.green;

    [Header("Hit Burst")]
    [SerializeField] float burstDuration = 0.12f;
    [SerializeField] float burstScale = 1.4f;

    [SerializeField] SpriteRenderer spriteRenderer;

    Vector3 baseScale;
    Coroutine burstRoutine;

    public void Init(Note note, double timeSec)
    {
        Lane = note.Lane;
        Beat = note.Beat;
        TimeSec = timeSec;
        Division = DivisionFromBeat(Beat);

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
            _ => otherDivisionColor,
        };
    }

    static NoteDivision DivisionFromBeat(double beat)
    {
        const double epsilon = 1e-6;
        if (IsMultiple(beat, 1.0, epsilon))
            return NoteDivision.Quarter;
        if (IsMultiple(beat, 0.5, epsilon))
            return NoteDivision.Eighth;
        if (IsMultiple(beat, 0.25, epsilon))
            return NoteDivision.Sixteenth;
        return NoteDivision.Other;
    }

    static bool IsMultiple(double value, double step, double epsilon)
    {
        var ratio = value / step;
        return Math.Abs(ratio - Math.Round(ratio)) <= epsilon;
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
