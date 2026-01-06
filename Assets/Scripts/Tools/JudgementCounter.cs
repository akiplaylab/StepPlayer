using System;
using System.Collections.Generic;

public sealed class JudgementCounter
{
    readonly Dictionary<Judgement, int> counts = new();
    int missCount;
    int currentCombo;
    int maxCombo;
    int score;

    public void Reset()
    {
        counts.Clear();
        missCount = 0;
        currentCombo = 0;
        maxCombo = 0;
        score = 0;
    }

    public void Record(Judgement judgement)
    {
        if (judgement == Judgement.None) return;

        if (!counts.TryGetValue(judgement, out var current))
            current = 0;

        counts[judgement] = current + 1;

        score += GetScoreFor(judgement);

        if (IsComboJudgement(judgement))
        {
            currentCombo++;
            maxCombo = Math.Max(maxCombo, currentCombo);
        }
        else
        {
            currentCombo = 0;
        }
    }

    public void RecordMiss()
    {
        missCount++;
        currentCombo = 0;
    }

    public JudgementSummary CreateSummary()
    {
        return new JudgementSummary(counts, missCount, maxCombo, score);
    }

    public int CurrentCombo => currentCombo;
    public int MaxCombo => maxCombo;
    public int Score => score;

    static bool IsComboJudgement(Judgement judgement)
    {
        return judgement != Judgement.None && judgement <= Judgement.Good;
    }

    static int GetScoreFor(Judgement judgement)
    {
        return judgement switch
        {
            Judgement.Marvelous => 1000,
            Judgement.Perfect => 900,
            Judgement.Great => 700,
            Judgement.Good => 500,
            Judgement.Bad => 200,
            _ => 0,
        };
    }
}

public readonly struct JudgementSummary
{
    readonly IReadOnlyDictionary<Judgement, int> counts;
    readonly int maxCombo;
    readonly int score;

    public JudgementSummary(IReadOnlyDictionary<Judgement, int> counts, int missCount, int maxCombo, int score)
    {
        this.counts = new Dictionary<Judgement, int>(counts);
        MissCount = missCount;
        this.maxCombo = maxCombo;
        this.score = score;
    }

    public int MissCount { get; }
    public int MaxCombo => maxCombo;
    public int Score => score;

    public int GetCount(Judgement judgement)
    {
        return counts.TryGetValue(judgement, out var count) ? count : 0;
    }
}
