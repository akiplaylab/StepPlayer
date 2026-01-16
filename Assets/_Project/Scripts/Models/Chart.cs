using System;
using System.Collections.Generic;

public sealed class Chart
{
    public string MusicFile { get; }
    public int Bpm { get; }
    public float OffsetSec { get; }
    public IReadOnlyList<Note> Notes { get; }
    public IReadOnlyList<BpmChange> BpmChanges { get; }

    public Chart(string musicFile, int bpm, float offsetSec, IReadOnlyList<Note> notes, IReadOnlyList<BpmChange> bpmChanges)
    {
        if (bpm <= 0) throw new ArgumentOutOfRangeException(nameof(bpm), "bpm must be > 0");
        MusicFile = musicFile;
        Bpm = bpm;
        OffsetSec = offsetSec;
        Notes = notes ?? throw new ArgumentNullException(nameof(notes));
        BpmChanges = bpmChanges ?? throw new ArgumentNullException(nameof(bpmChanges));
    }

    public double BeatToSeconds(double beat)
    {
        double seconds = 0.0;
        double lastBeat = 0.0;
        double currentBpm = Bpm;

        for (int i = 0; i < BpmChanges.Count; i++)
        {
            var change = BpmChanges[i];

            if (beat <= change.Beat)
                break;

            double segmentEnd = Math.Min(beat, change.Beat);
            seconds += (segmentEnd - lastBeat) * 60.0 / currentBpm;

            currentBpm = change.Bpm;
            lastBeat = change.Beat;
        }

        if (beat > lastBeat)
            seconds += (beat - lastBeat) * 60.0 / currentBpm;

        return seconds;
    }

    public double SecondsToBeat(double seconds)
    {
        double remaining = seconds;
        double lastBeat = 0.0;
        double currentBpm = Bpm;

        for (int i = 0; i < BpmChanges.Count; i++)
        {
            var change = BpmChanges[i];
            double segmentBeats = change.Beat - lastBeat;
            double segmentSeconds = segmentBeats * 60.0 / currentBpm;

            if (remaining <= segmentSeconds)
                return lastBeat + remaining * currentBpm / 60.0;

            remaining -= segmentSeconds;
            currentBpm = change.Bpm;
            lastBeat = change.Beat;
        }

        return lastBeat + remaining * currentBpm / 60.0;
    }
}
