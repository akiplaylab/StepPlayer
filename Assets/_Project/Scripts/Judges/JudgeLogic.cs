public static class JudgeLogic
{
    public static JudgementOutcome Evaluate(
        double dt,
        float marvelous = 0.015f,
        float perfect = 0.03f,
        float great = 0.06f,
        float good = 0.10f,
        float miss = 0.20f)
    {
        if (dt > miss)
        {
            return new JudgementOutcome(Judgement.None, 0f, shouldConsumeNote: false);
        }

        var judgement =
            dt <= marvelous ? Judgement.Marvelous :
            dt <= perfect ? Judgement.Perfect :
            dt <= great ? Judgement.Great :
            dt <= good ? Judgement.Good :
            Judgement.Bad;

        float intensity =
            dt <= marvelous ? 1.0f :
            dt <= perfect ? 1.0f :
            dt <= great ? 0.75f :
            dt <= good ? 0.55f :
            0.35f;

        return new JudgementOutcome(judgement, intensity, shouldConsumeNote: true);
    }
}
