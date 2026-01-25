using System;
using UnityEngine;

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
