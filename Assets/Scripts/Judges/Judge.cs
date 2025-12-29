using System;
using UnityEngine;

[Serializable]
public sealed class Judge
{
    [Header("Judgement Windows (sec)")]
    [SerializeField] float marvelous = 0.015f;
    [SerializeField] float perfect = 0.03f;
    [SerializeField] float great = 0.06f;
    [SerializeField] float good = 0.10f;
    [SerializeField] float miss = 0.20f;

    [SerializeField] JudgementTextPresenter judgementText;
    [SerializeField] RazerChromaController razerChroma;

    public float MissWindow => miss;

    public JudgementOutcome JudgeHit(Lane lane, double dt)
    {
        var result =
            dt <= marvelous ? "Marvelous" :
            dt <= perfect ? "Perfect" :
            dt <= great ? "Great" :
            dt <= good ? "Good" :
            dt <= miss ? "Bad" : "TooEarly/TooLate";

        float intensity =
            dt <= marvelous ? 1.0f :
            dt <= perfect ? 1.0f :
            dt <= great ? 0.75f :
            dt <= good ? 0.55f :
            dt <= miss ? 0.35f : 0.0f;

        var judgement =
            dt <= marvelous ? Judgement.Marvelous :
            dt <= perfect ? Judgement.Perfect :
            dt <= great ? Judgement.Great :
            dt <= good ? Judgement.Good :
            Judgement.Bad;

        judgementText.Show(judgement);
        razerChroma?.TriggerJudgement(judgement, judgementText.GetColor(judgement));

        Debug.Log($"{lane}: {result} (dt={dt:0.000})");

        return new JudgementOutcome(judgement, intensity, dt <= miss);
    }
}

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
