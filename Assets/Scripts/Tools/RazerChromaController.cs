using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public sealed class RazerChromaController : MonoBehaviour
{
    [SerializeField] string appTitle = "DanceDanceRevolution";
    [SerializeField] string appDescription = "Light up Razer Chroma devices on judgements.";
    [SerializeField] string authorName = "DanceDanceRevolution";
    [SerializeField] string authorContact = "";

    readonly List<string> device1DNames = new() { "ChromaLink", "Headset", "Mousepad" };
    readonly List<string> device2DNames = new() { "Keyboard", "Keypad", "Mouse" };

    Type chromaApiType;
    Type device1DEnum;
    Type device2DEnum;
    MethodInfo initMethod;
    MethodInfo uninitMethod;
    MethodInfo useIdleAnimationsMethod;
    MethodInfo setStaticColor1DMethod;
    MethodInfo setStaticColor2DMethod;

    bool apiReady;

    void OnEnable()
    {
        if (PrepareChromaApi() && InitializeChroma())
            apiReady = true;
    }

    void OnDisable()
    {
        if (apiReady)
            ShutdownChroma();
        apiReady = false;
    }

    public void TriggerJudgement(Judgement judgement, Color color)
    {
        if (judgement != Judgement.Perfect && judgement != Judgement.Great)
            return;

        if (!apiReady)
        {
            Debug.LogWarning("Razer Chroma SDK is not ready. Ensure the Unity Chroma SDK plugin is installed and initialized.");
            return;
        }

        int chromaColor = ToBgr(color);
        ApplyStaticColor(chromaColor);
    }

    bool PrepareChromaApi()
    {
#if !UNITY_STANDALONE_WIN
        Debug.LogWarning("Razer Chroma SDK is only supported on Windows builds.");
        return false;
#else
        chromaApiType = FindType("ChromaSDK.ChromaAnimationAPI");
        device1DEnum = FindType("ChromaSDK.Device1DEnum");
        device2DEnum = FindType("ChromaSDK.Device2DEnum");

        if (chromaApiType == null || device1DEnum == null || device2DEnum == null)
        {
            Debug.LogWarning("Razer Chroma SDK types were not found. Please install the Chroma SDK Unity package from Razer's sample project and restart the editor.");
            return false;
        }

        initMethod = chromaApiType.GetMethod("Init", BindingFlags.Public | BindingFlags.Static);
        uninitMethod = chromaApiType.GetMethod("Uninit", BindingFlags.Public | BindingFlags.Static);
        useIdleAnimationsMethod = chromaApiType.GetMethod("UseIdleAnimations", BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(bool) }, null);
        setStaticColor1DMethod = chromaApiType.GetMethod("SetStaticColor", BindingFlags.Public | BindingFlags.Static, null, new[] { device1DEnum, typeof(int) }, null);
        setStaticColor2DMethod = chromaApiType.GetMethod("SetStaticColor", BindingFlags.Public | BindingFlags.Static, null, new[] { device2DEnum, typeof(int) }, null);

        if (initMethod == null || uninitMethod == null || useIdleAnimationsMethod == null || setStaticColor1DMethod == null || setStaticColor2DMethod == null)
        {
            Debug.LogWarning("Razer Chroma SDK methods were not found. Please ensure the SDK package matches the Unity sample from Razer.");
            return false;
        }

        return true;
#endif
    }

    bool InitializeChroma()
    {
        try
        {
            useIdleAnimationsMethod.Invoke(null, new object[] { false });

            var result = initMethod.Invoke(null, null);
            if (result is int code && code != 0)
            {
                Debug.LogWarning($"Razer Chroma SDK initialization failed with code {code}.");
                return false;
            }

            return true;
        }
        catch (TargetInvocationException ex)
        {
            Debug.LogWarning($"Failed to initialize Razer Chroma SDK: {ex.InnerException?.Message ?? ex.Message}");
            return false;
        }
    }

    void ShutdownChroma()
    {
        try
        {
            useIdleAnimationsMethod.Invoke(null, new object[] { true });
            uninitMethod.Invoke(null, null);
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"Failed to shut down Razer Chroma SDK cleanly: {ex.Message}");
        }
    }

    void ApplyStaticColor(int color)
    {
        foreach (var deviceName in device1DNames)
        {
            var device = GetEnumValue(device1DEnum, deviceName);
            if (device != null)
                setStaticColor1DMethod.Invoke(null, new[] { device, (object)color });
        }

        foreach (var deviceName in device2DNames)
        {
            var device = GetEnumValue(device2DEnum, deviceName);
            if (device != null)
                setStaticColor2DMethod.Invoke(null, new[] { device, (object)color });
        }
    }

    static int ToBgr(Color color)
    {
        int r = Mathf.Clamp(Mathf.RoundToInt(color.r * 255f), 0, 255);
        int g = Mathf.Clamp(Mathf.RoundToInt(color.g * 255f), 0, 255);
        int b = Mathf.Clamp(Mathf.RoundToInt(color.b * 255f), 0, 255);
        return (b << 16) | (g << 8) | r;
    }

    static Type FindType(string fullName)
    {
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            var type = assembly.GetType(fullName);
            if (type != null)
                return type;
        }
        return null;
    }

    static object GetEnumValue(Type enumType, string name)
    {
        if (enumType == null || !enumType.IsEnum)
            return null;

        try
        {
            return Enum.Parse(enumType, name, ignoreCase: true);
        }
        catch
        {
            Debug.LogWarning($"Razer Chroma SDK missing enum value: {name}");
            return null;
        }
    }
}
