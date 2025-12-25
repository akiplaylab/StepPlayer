using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public enum Lane { Left, Down, Up, Right }

[Serializable]
public class ChartJson
{
    public string musicFile;
    public float offsetSec;
    public NoteJson[] notes;
}

[Serializable]
public class NoteJson
{
    public float timeSec;
    public string lane;
}

public sealed class Chart
{
    public string MusicFile { get; }
    public double OffsetSec { get; }
    public IReadOnlyList<NoteEvent> Notes { get; }

    public Chart(string musicFile, double offsetSec, IReadOnlyList<NoteEvent> notes)
    {
        MusicFile = musicFile;
        OffsetSec = offsetSec;
        Notes = notes;
    }

    public static Chart LoadFromStreamingAssets(string fileName)
    {
        var path = Path.Combine(Application.streamingAssetsPath, fileName);
        var json = File.ReadAllText(path);
        var raw = JsonUtility.FromJson<ChartJson>(json);

        var notes = raw.notes
            .Select(n => new NoteEvent(
                timeSec: n.timeSec,
                lane: (Lane)Enum.Parse(typeof(Lane), n.lane, ignoreCase: true)))
            .OrderBy(n => n.TimeSec)
            .ToList();

        return new Chart(raw.musicFile, raw.offsetSec, notes);
    }
}

public readonly struct NoteEvent
{
    public double TimeSec { get; }
    public Lane Lane { get; }
    public NoteEvent(double timeSec, Lane lane)
    {
        TimeSec = timeSec;
        Lane = lane;
    }
}
