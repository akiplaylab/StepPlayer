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
        if (BpmChanges.Count == 0) return beat * (60.0 / Bpm);

        double seconds = 0;
        for (int i = 0; i < BpmChanges.Count; i++)
        {
            var current = BpmChanges[i];
            var nextBeat = (i + 1 < BpmChanges.Count) ? BpmChanges[i + 1].Beat : beat;
            if (beat <= current.Beat) break;

            var segmentEnd = Math.Min(beat, nextBeat);
            seconds += (segmentEnd - current.Beat) * 60.0 / current.Bpm;
            if (beat <= nextBeat) break;
        }
        return seconds;
    }

    // 追加: 現在の再生時間(sec)から、現在の累計Beatを算出する
    public double SecondsToBeat(double seconds)
    {
        if (BpmChanges.Count == 0) return seconds / (60.0 / Bpm);

        double currentSec = 0;
        double currentBeat = 0;

        for (int i = 0; i < BpmChanges.Count; i++)
        {
            var current = BpmChanges[i];
            double nextBeat = (i + 1 < BpmChanges.Count) ? BpmChanges[i + 1].Beat : double.MaxValue;
            double duration = (nextBeat - current.Beat) * 60.0 / current.Bpm;

            if (seconds <= currentSec + duration)
            {
                return current.Beat + (seconds - currentSec) * (current.Bpm / 60.0);
            }
            currentSec += duration;
            currentBeat = nextBeat;
        }
        return currentBeat;
    }
}
