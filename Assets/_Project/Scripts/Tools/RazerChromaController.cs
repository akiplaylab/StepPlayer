using System;
using UnityEngine;

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
using ChromaSDK;
#endif

public sealed class RazerChromaController : MonoBehaviour
{
    public enum RazerStage
    {
        Base,
        ComboLow,
        ComboMid,
        ComboHigh,
        Rainbow,
        RainbowPlus,
    }

    public enum RazerEffectType
    {
        Solid,
        PulseTwoColor,
        FlowTwoColor,
        Rainbow,
    }

    [Serializable]
    public sealed class ChromaEffectSettings
    {
        public RazerEffectType effectType = RazerEffectType.PulseTwoColor;
        public Color colorA = new(0.10f, 0.20f, 0.25f, 1f);
        public Color colorB = new(0.08f, 0.12f, 0.18f, 1f);
        [Range(0f, 1f)] public float intensity = 0.35f;
        [Min(0f)] public float speed = 0.12f;
        [Range(0f, 1f)] public float rainbowSaturation = 0.7f;
    }

    [Serializable]
    public sealed class ComboLayerSettings
    {
        [Header("Combo Stage Thresholds")]
        [Min(0)] public int comboLowMin = 10;
        [Min(0)] public int comboMidMin = 30;
        [Min(0)] public int comboHighMin = 60;
        [Min(0)] public int rainbowMin = 100;
        [Min(0)] public int rainbowPlusMin = 100;

        [Header("Stage Cooldown")]
        [Min(0f)] public float stageHoldSeconds = 0.8f;

        [Header("Fade Down")]
        [Min(0.01f)] public float fadeDownDuration = 1.5f;

        [Header("Stage Effects")]
        public ChromaEffectSettings comboLow = new()
        {
            effectType = RazerEffectType.PulseTwoColor,
            colorA = new Color(0.10f, 0.26f, 0.35f, 1f),
            colorB = new Color(0.10f, 0.16f, 0.26f, 1f),
            intensity = 0.45f,
            speed = 0.16f,
        };

        public ChromaEffectSettings comboMid = new()
        {
            effectType = RazerEffectType.FlowTwoColor,
            colorA = new Color(0.12f, 0.32f, 0.38f, 1f),
            colorB = new Color(0.06f, 0.18f, 0.26f, 1f),
            intensity = 0.50f,
            speed = 0.24f,
        };

        public ChromaEffectSettings comboHigh = new()
        {
            effectType = RazerEffectType.Rainbow,
            intensity = 0.55f,
            speed = 0.08f,
            rainbowSaturation = 0.8f,
        };

        public ChromaEffectSettings rainbow = new()
        {
            effectType = RazerEffectType.Rainbow,
            intensity = 0.60f,
            speed = 0.10f,
            rainbowSaturation = 0.9f,
        };

        public ChromaEffectSettings rainbowPlus = new()
        {
            effectType = RazerEffectType.Rainbow,
            intensity = 0.70f,
            speed = 0.12f,
            rainbowSaturation = 1.0f,
        };
    }

    [Serializable]
    public sealed class AccentLayerSettings
    {
        [Min(0.01f)] public float duration = 0.08f;
        [Range(0f, 0.5f)] public float brightnessBoost = 0.1f;
        public bool triggerOnGood = false;
        public bool triggerOnBad = false;

        public bool ShouldTrigger(Judgement judgement) => judgement switch
        {
            Judgement.Marvelous => true,
            Judgement.Perfect => true,
            Judgement.Great => true,
            Judgement.Good => triggerOnGood,
            Judgement.Bad => triggerOnBad,
            _ => false,
        };
    }

    [Header("Chroma App Info")]
    [SerializeField] string appTitle = "StepPlayer";
    [SerializeField] string appDescription = "Light up Razer Chroma devices based on gameplay state.";
    [SerializeField] string authorName = "StepPlayer";
    [SerializeField] string authorContact = "https://developer.razer.com/chroma";

    [Header("Base Layer")]
    [SerializeField] ChromaEffectSettings baseLayer = new()
    {
        effectType = RazerEffectType.PulseTwoColor,
        colorA = new Color(0.08f, 0.10f, 0.16f, 1f),
        colorB = new Color(0.10f, 0.18f, 0.22f, 1f),
        intensity = 0.35f,
        speed = 0.10f,
    };

    [Header("Combo Layer")]
    [SerializeField] ComboLayerSettings comboLayer = new();

    [Header("Accent Layer")]
    [SerializeField] AccentLayerSettings accentLayer = new();

    [Header("Shutdown")]
    [SerializeField] Color offColor = Color.black;

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
    bool initialized;
    APPINFOTYPE appInfo;
#endif

    RazerStage currentStage = RazerStage.Base;
    float comboIntensity = 0f;
    float accentEndTime;
    float fadeStartTime;
    bool fadingDown;
    float lastStageChangeTime;

    void OnEnable()
    {
        InitializeChroma();
    }

    void OnDisable()
    {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        ApplyUnityColor(offColor);
#endif
        ShutdownChroma();
    }

    void Update()
    {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        if (!initialized)
            return;

        UpdateComboFade();

        var time = Time.unscaledTime;
        var baseColor = EvaluateEffect(baseLayer, time) * baseLayer.intensity;
        var comboColor = GetComboColor(time);

        var combined = CombineColors(baseColor, comboColor);

        if (Time.unscaledTime < accentEndTime)
        {
            combined *= 1f + accentLayer.brightnessBoost;
        }

        combined.a = 1f;
        ApplyUnityColor(combined);
#endif
    }

    public void OnComboChanged(int combo)
    {
        if (!InitializeChroma())
            return;

        if (combo <= 0)
            return;

        fadingDown = false;
        comboIntensity = 1f;

        var nextStage = CalculateStage(combo);
        if (nextStage == RazerStage.Base)
            return;

        var now = Time.unscaledTime;
        if (nextStage != currentStage)
        {
            if (now - lastStageChangeTime >= comboLayer.stageHoldSeconds)
            {
                currentStage = nextStage;
                lastStageChangeTime = now;
            }
            else if (IsStageUpgrade(nextStage, currentStage))
            {
                currentStage = nextStage;
                lastStageChangeTime = now;
            }
        }
    }

    public void OnComboBreak()
    {
        if (!InitializeChroma())
            return;

        if (comboIntensity <= 0f)
            return;

        fadingDown = true;
        fadeStartTime = Time.unscaledTime;
    }

    public void TriggerAccent(Judgement judgement)
    {
        if (!InitializeChroma())
            return;

        if (!accentLayer.ShouldTrigger(judgement))
            return;

        accentEndTime = Time.unscaledTime + Mathf.Max(0.01f, accentLayer.duration);
    }

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
    void UpdateComboFade()
    {
        if (!fadingDown)
            return;

        float duration = Mathf.Max(0.01f, comboLayer.fadeDownDuration);
        float t = (Time.unscaledTime - fadeStartTime) / duration;
        comboIntensity = Mathf.Lerp(1f, 0f, Mathf.Clamp01(t));

        if (t >= 1f)
        {
            comboIntensity = 0f;
            fadingDown = false;
            currentStage = RazerStage.Base;
        }
    }

    Color GetComboColor(float time)
    {
        if (comboIntensity <= 0f || currentStage == RazerStage.Base)
            return Color.black;

        var settings = GetStageSettings(currentStage);
        var color = EvaluateEffect(settings, time) * settings.intensity;
        return color * comboIntensity;
    }

    static Color EvaluateEffect(ChromaEffectSettings settings, float time)
    {
        float speed = Mathf.Max(0.01f, settings.speed);
        return settings.effectType switch
        {
            RazerEffectType.Solid => settings.colorA,
            RazerEffectType.PulseTwoColor => Color.Lerp(settings.colorA, settings.colorB, GetPulse(time, speed)),
            RazerEffectType.FlowTwoColor => Color.Lerp(settings.colorA, settings.colorB, Mathf.PingPong(time * speed, 1f)),
            RazerEffectType.Rainbow => Color.HSVToRGB(Mathf.Repeat(time * speed, 1f), Mathf.Clamp01(settings.rainbowSaturation), 1f),
            _ => settings.colorA,
        };
    }

    static float GetPulse(float time, float speed)
    {
        return (Mathf.Sin(time * speed * Mathf.PI * 2f) + 1f) * 0.5f;
    }

    static Color CombineColors(Color baseColor, Color comboColor)
    {
        return new Color(
            Mathf.Clamp01(baseColor.r + comboColor.r),
            Mathf.Clamp01(baseColor.g + comboColor.g),
            Mathf.Clamp01(baseColor.b + comboColor.b),
            1f);
    }

    static bool IsStageUpgrade(RazerStage next, RazerStage current)
    {
        return next > current;
    }

    RazerStage CalculateStage(int combo)
    {
        if (combo >= comboLayer.rainbowPlusMin)
            return RazerStage.RainbowPlus;
        if (combo >= comboLayer.rainbowMin)
            return RazerStage.Rainbow;
        if (combo >= comboLayer.comboHighMin)
            return RazerStage.ComboHigh;
        if (combo >= comboLayer.comboMidMin)
            return RazerStage.ComboMid;
        if (combo >= comboLayer.comboLowMin)
            return RazerStage.ComboLow;
        return RazerStage.Base;
    }

    ChromaEffectSettings GetStageSettings(RazerStage stage) => stage switch
    {
        RazerStage.ComboLow => comboLayer.comboLow,
        RazerStage.ComboMid => comboLayer.comboMid,
        RazerStage.ComboHigh => comboLayer.comboHigh,
        RazerStage.Rainbow => comboLayer.rainbow,
        RazerStage.RainbowPlus => comboLayer.rainbowPlus,
        _ => comboLayer.comboLow,
    };

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
