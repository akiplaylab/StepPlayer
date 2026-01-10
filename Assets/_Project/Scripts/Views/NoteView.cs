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
    [SerializeField] float burstScale = 1.5f;
    [SerializeField] float burstDuration = 0.18f;
    [SerializeField] float flashScale = 1.2f;
    [SerializeField] float flashDuration = 0.05f;
    [SerializeField] float spinDegrees = 20f;
    [SerializeField] Color flashColor = new(1f, 0.95f, 0.4f, 1f);

    [SerializeField] SpriteRenderer spriteRenderer;
    Coroutine burstRoutine;
    Vector3 baseScale;
    Color baseColor;
    Quaternion baseRotation;

    public void Init(Note note)
    {
        Lane = note.Lane;
        TimeSec = note.TimeSec;
        Division = note.Division;

        ApplyRotation();
        ApplyColor();
    }

    void OnValidate()
    {
        if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    void OnEnable()
    {
        CacheBaseState();
    }

    public void PlayHitBurst(float intensity01, Action onComplete)
    {
        if (burstRoutine != null) StopCoroutine(burstRoutine);
        burstRoutine = StartCoroutine(CoHitBurst(intensity01, onComplete));
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

        CacheBaseState();
    }

    void CacheBaseState()
    {
        if (spriteRenderer == null) return;
        baseScale = transform.localScale;
        baseColor = spriteRenderer.color;
        baseRotation = transform.localRotation;
    }

    IEnumerator CoHitBurst(float intensity01, Action onComplete)
    {
        if (spriteRenderer == null)
        {
            onComplete?.Invoke();
            yield break;
        }

        CacheBaseState();

        float t = 0f;
        float flashDur = Mathf.Max(0.01f, flashDuration);
        float burstDur = Mathf.Max(0.01f, burstDuration);
        float intensity = Mathf.Clamp01(intensity01);
        float targetScale = Mathf.Lerp(1.1f, burstScale, intensity);
        float targetFlashScale = Mathf.Lerp(1.05f, flashScale, intensity);
        float targetSpin = Mathf.Lerp(5f, spinDegrees, intensity);

        while (t < flashDur)
        {
            t += Time.unscaledDeltaTime;
            float a = Mathf.Clamp01(t / flashDur);
            float eased = Mathf.SmoothStep(0f, 1f, a);
            transform.localScale = baseScale * Mathf.Lerp(1f, targetFlashScale, eased);
            transform.localRotation = baseRotation * Quaternion.Euler(0f, 0f, Mathf.Lerp(0f, targetSpin, eased));
            spriteRenderer.color = Color.Lerp(baseColor, flashColor, eased);
            yield return null;
        }

        t = 0f;
        while (t < burstDur)
        {
            t += Time.unscaledDeltaTime;
            float a = Mathf.Clamp01(t / burstDur);
            float eased = Mathf.SmoothStep(0f, 1f, a);
            transform.localScale = baseScale * Mathf.Lerp(targetFlashScale, targetScale, eased);
            transform.localRotation = baseRotation * Quaternion.Euler(0f, 0f, Mathf.Lerp(targetSpin, targetSpin * 2f, eased));
            var color = Color.Lerp(flashColor, baseColor, eased);
            color.a = Mathf.Lerp(baseColor.a, 0f, eased);
            spriteRenderer.color = color;
            yield return null;
        }

        transform.localScale = baseScale;
        transform.localRotation = baseRotation;
        spriteRenderer.color = baseColor;
        burstRoutine = null;
        onComplete?.Invoke();
    }
}
