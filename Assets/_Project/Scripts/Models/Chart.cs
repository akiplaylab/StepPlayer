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
        // BPM変化がない場合は、基本BPMから計算 (60/Bpm = 1拍の秒数)
        if (BpmChanges == null || BpmChanges.Count == 0)
            return beat * (60.0 / Bpm);

        double seconds = 0;
        for (int i = 0; i < BpmChanges.Count; i++)
        {
            var current = BpmChanges[i];
            var nextBeat = (i + 1 < BpmChanges.Count) ? BpmChanges[i + 1].Beat : beat;

            if (beat <= current.Beat) break;

            var segmentEnd = Math.Min(beat, nextBeat);
            if (segmentEnd > current.Beat)
                seconds += (segmentEnd - current.Beat) * 60.0 / current.Bpm;

            if (beat <= nextBeat) break;
        }
        return seconds;
    }
}
