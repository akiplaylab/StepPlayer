using System;
using UnityEngine;

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
