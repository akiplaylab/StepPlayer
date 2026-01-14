using System;
using System.Collections.Generic;

public sealed class JudgementCounter
{
    readonly Dictionary<Judgement, int> counts = new();
    int missCount;
    int currentCombo;
    int maxCombo;

    public void Reset()
    {
        counts.Clear();
        missCount = 0;
        currentCombo = 0;
        maxCombo = 0;
    }

    public void Record(Judgement judgement)
    {
        if (judgement == Judgement.None) return;

        if (!counts.TryGetValue(judgement, out var current))
            current = 0;

        counts[judgement] = current + 1;

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

    public JudgementSummary CreateSummary(int totalNotes)
    {
        return new JudgementSummary(counts, missCount, maxCombo, totalNotes);
    }

    public int CurrentCombo => currentCombo;
    public int MaxCombo => maxCombo;

    static bool IsComboJudgement(Judgement judgement)
    {
        return judgement != Judgement.None && judgement <= Judgement.Good;
    }
}
