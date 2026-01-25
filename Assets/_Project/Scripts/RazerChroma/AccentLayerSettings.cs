using System;
using UnityEngine;

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
