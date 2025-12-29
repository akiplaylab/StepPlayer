using System;
using System.Collections.Generic;

public sealed class Chart
{
    public string MusicFile { get; }
    public int Bpm { get; }
    public float OffsetSec { get; }
    public IReadOnlyList<Note> Notes { get; }

    public Chart(string musicFile, int bpm, float offsetSec, IReadOnlyList<Note> notes)
    {
        if (bpm <= 0) throw new ArgumentOutOfRangeException(nameof(bpm), "bpm must be > 0");

        MusicFile = musicFile;
        Bpm = bpm;
        OffsetSec = offsetSec;
        Notes = notes ?? throw new ArgumentNullException(nameof(notes));
    }
}
