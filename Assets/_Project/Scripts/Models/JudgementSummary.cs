using System.Collections.Generic;

public readonly struct JudgementSummary
{
    readonly IReadOnlyDictionary<Judgement, int> counts;
    readonly int maxCombo;

    public JudgementSummary(IReadOnlyDictionary<Judgement, int> counts, int missCount, int maxCombo, int totalNotes)
    {
        var snapshot = new Dictionary<Judgement, int>(counts);

        this.counts = snapshot;
        MissCount = missCount;
        this.maxCombo = maxCombo;
        TotalNotes = totalNotes;

        Score = ScoreCalculator.Calculate(
            totalNotes,
            GetCount(snapshot, Judgement.Marvelous),
            GetCount(snapshot, Judgement.Perfect),
            GetCount(snapshot, Judgement.Great),
            GetCount(snapshot, Judgement.Good),
            GetCount(snapshot, Judgement.Bad),
            missCount);

        DanceLevel = ScoreCalculator.GetDanceLevel(Score);
    }

    public int MissCount { get; }
    public int MaxCombo => maxCombo;
    public int TotalNotes { get; }
    public int Score { get; }
    public string DanceLevel { get; }

    static int GetCount(IReadOnlyDictionary<Judgement, int> source, Judgement judgement)
    {
        return source.TryGetValue(judgement, out var count) ? count : 0;
    }

    public int GetCount(Judgement judgement)
    {
        return counts.TryGetValue(judgement, out var count) ? count : 0;
    }
}
