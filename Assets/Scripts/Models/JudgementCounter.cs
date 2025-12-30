using System.Collections.Generic;

// Judgement集計の方針:
// - ノーツが消費される瞬間の判定 (Game.TryHit 内の ShouldConsumeNote が true のとき) を記録する
// - 判定ウィンドウを超えて回収されたノーツを CleanupMissed から Miss として加算する
// - 空振りや消費されなかった入力は集計対象に含めない
public sealed class JudgementCounter
{
    readonly Dictionary<Judgement, int> counts = new();
    int missCount;

    public void Reset()
    {
        counts.Clear();
        missCount = 0;
    }

    public void Record(Judgement judgement)
    {
        if (judgement == Judgement.None) return;

        if (!counts.TryGetValue(judgement, out var current))
            current = 0;

        counts[judgement] = current + 1;
    }

    public void RecordMiss()
    {
        missCount++;
    }

    public JudgementSummary CreateSummary()
    {
        return new JudgementSummary(counts, missCount);
    }
}

public readonly struct JudgementSummary
{
    readonly IReadOnlyDictionary<Judgement, int> counts;

    public JudgementSummary(IReadOnlyDictionary<Judgement, int> counts, int missCount)
    {
        this.counts = new Dictionary<Judgement, int>(counts);
        MissCount = missCount;
    }

    public int MissCount { get; }

    public int GetCount(Judgement judgement)
    {
        return counts.TryGetValue(judgement, out var count) ? count : 0;
    }
}
