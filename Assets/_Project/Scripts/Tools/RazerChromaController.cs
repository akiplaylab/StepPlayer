using System;
using System.Collections;
using UnityEngine;

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
using ChromaSDK;
#endif

public sealed class RazerChromaController : MonoBehaviour
{
    [Header("Chroma App Info")]
    [SerializeField] string appTitle = "DanceDanceRevolution";
    [SerializeField] string appDescription = "Light up Razer Chroma devices on judgements.";
    [SerializeField] string authorName = "DanceDanceRevolution";
    [SerializeField] string authorContact = "https://developer.razer.com/chroma";

    [Header("Flash Settings")]
    [SerializeField] float flashDuration = 0.08f;
    [SerializeField] Color offColor = Color.black;

    [Header("Intensity per Judgement")]
    [SerializeField, Range(0f, 1f)] float marvelousIntensity = 1.00f;
    [SerializeField, Range(0f, 1f)] float perfectIntensity = 1.00f;
    [SerializeField, Range(0f, 1f)] float greatIntensity = 0.70f;
    [SerializeField, Range(0f, 1f)] float goodIntensity = 0.00f;
    [SerializeField, Range(0f, 1f)] float badIntensity = 0.00f;

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
    bool initialized;
    APPINFOTYPE appInfo;
    Coroutine flashing;
#endif

    void OnEnable()
    {
        InitializeChroma();
    }

    void OnDisable()
    {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        if (flashing != null)
        {
            StopCoroutine(flashing);
            flashing = null;
        }

        ApplyUnityColor(offColor);
#endif
        ShutdownChroma();
    }

    public void TriggerJudgement(Judgement judgement, Color baseColor)
    {
        if (!InitializeChroma())
            return;

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        float intensity = GetIntensity(judgement);
        if (intensity <= 0f)
            return;

        var c = baseColor * intensity;
        c.a = 1f;

        if (flashing != null) StopCoroutine(flashing);
        flashing = StartCoroutine(CoFlash(c, flashDuration));
#endif
    }

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
    float GetIntensity(Judgement j) => j switch
    {
        Judgement.Marvelous => marvelousIntensity,
        Judgement.Perfect => perfectIntensity,
        Judgement.Great => greatIntensity,
        Judgement.Good => goodIntensity,
        Judgement.Bad => badIntensity,
        _ => 0f
    };

    IEnumerator CoFlash(Color onColor, float duration)
    {
        ApplyUnityColor(onColor);

        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            yield return null;
        }

        ApplyUnityColor(offColor);
        flashing = null;
    }

    void ApplyUnityColor(Color unityColor)
    {
        byte r = (byte)Mathf.Clamp(Mathf.RoundToInt(Mathf.Clamp01(unityColor.r) * 255f), 0, 255);
        byte g = (byte)Mathf.Clamp(Mathf.RoundToInt(Mathf.Clamp01(unityColor.g) * 255f), 0, 255);
        byte b = (byte)Mathf.Clamp(Mathf.RoundToInt(Mathf.Clamp01(unityColor.b) * 255f), 0, 255);

        int chromaColor = ChromaAnimationAPI.GetRGB(r, g, b);
        ApplyStaticColor(chromaColor);
    }
#endif

    bool InitializeChroma()
    {
#if !UNITY_STANDALONE_WIN && !UNITY_EDITOR_WIN
        return false;
#else
        if (initialized)
            return true;

        try
        {
            appInfo = new APPINFOTYPE
            {
                Title = appTitle,
                Description = appDescription,
                Author_Name = authorName,
                Author_Contact = authorContact,
                SupportedDevice = (0x01 | 0x02 | 0x04 | 0x08 | 0x10 | 0x20),
                Category = 1,
            };

            ChromaAnimationAPI.UseIdleAnimations(false);
            int result = ChromaAnimationAPI.InitSDK(ref appInfo);

            if (result != 0)
            {
                Debug.LogWarning($"Razer Chroma SDK initialization failed with code {result}.");
                return false;
            }

            initialized = true;
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"Failed to initialize Razer Chroma SDK: {ex.Message}");
            initialized = false;
            return false;
        }
#endif
    }

    void ShutdownChroma()
    {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        if (!initialized)
            return;

        try
        {
            ChromaAnimationAPI.UseIdleAnimations(true);
            ChromaAnimationAPI.Uninit();
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"Failed to shut down Razer Chroma SDK cleanly: {ex.Message}");
        }

        initialized = false;
#endif
    }

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
    static void ApplyStaticColor(int color)
    {
        ChromaAnimationAPI.SetStaticColor((int)ChromaAnimationAPI.DeviceType.DE_1D, (int)ChromaAnimationAPI.Device1D.ChromaLink, color);
        ChromaAnimationAPI.SetStaticColor((int)ChromaAnimationAPI.DeviceType.DE_1D, (int)ChromaAnimationAPI.Device1D.Headset, color);
        ChromaAnimationAPI.SetStaticColor((int)ChromaAnimationAPI.DeviceType.DE_1D, (int)ChromaAnimationAPI.Device1D.Mousepad, color);

        ChromaAnimationAPI.SetStaticColor((int)ChromaAnimationAPI.DeviceType.DE_2D, (int)ChromaAnimationAPI.Device2D.Keyboard, color);
        ChromaAnimationAPI.SetStaticColor((int)ChromaAnimationAPI.DeviceType.DE_2D, (int)ChromaAnimationAPI.Device2D.Keypad, color);
        ChromaAnimationAPI.SetStaticColor((int)ChromaAnimationAPI.DeviceType.DE_2D, (int)ChromaAnimationAPI.Device2D.Mouse, color);
    }
#endif
}
