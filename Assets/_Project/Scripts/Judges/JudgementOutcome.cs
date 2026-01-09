using System;

[Serializable]
public readonly struct JudgementOutcome
{
    public JudgementOutcome(Judgement judgement, float intensity, bool shouldConsumeNote)
    {
        Judgement = judgement;
        Intensity = intensity;
        ShouldConsumeNote = shouldConsumeNote;
    }

    public Judgement Judgement { get; }
    public float Intensity { get; }
    public bool ShouldConsumeNote { get; }
}
