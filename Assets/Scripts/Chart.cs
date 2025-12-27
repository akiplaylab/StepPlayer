using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public sealed class Chart
{
    public string MusicFile { get; }
    public byte Bpm { get; }
    public float OffsetSec { get; }
    public IReadOnlyList<NoteEvent> Notes { get; }

    public Chart(string musicFile, byte bpm, float offsetSec, IReadOnlyList<NoteEvent> notes)
    {
        if (bpm <= 0) throw new ArgumentOutOfRangeException(nameof(bpm), "bpm must be > 0");

        MusicFile = musicFile;
        Bpm = bpm;
        OffsetSec = offsetSec;
        Notes = notes;
    }

    public static Chart LoadFromStreamingAssets(string fileName)
    {
        var path = Path.Combine(Application.streamingAssetsPath, fileName);
        var json = File.ReadAllText(path);

        var raw = JsonUtility.FromJson<ChartJson>(json);
        if (raw == null) throw new InvalidDataException($"Failed to parse {nameof(ChartJson)}.");
        if (raw.measures == null) raw.measures = Array.Empty<ChartJson.Measure>();

        if (raw.bpm <= 0) throw new InvalidDataException($"Invalid bpm: {raw.bpm}");

        // 4/4固定：1小節=4拍（DDR想定）
        var secPerBeat = 60.0 / raw.bpm;
        var secPerMeasure = secPerBeat * 4.0;

        var notes = new List<NoteEvent>(1024);

        for (int m = 0; m < raw.measures.Length; m++)
        {
            var measure = raw.measures[m];
            if (measure == null) continue;

            var subdiv = measure.subdiv <= 0 ? 16 : measure.subdiv;
            var rows = measure.rows ?? Array.Empty<string>();

            // rows が subdiv 未満でも、ある分だけ読む
            var rowCount = Math.Min(subdiv, rows.Length);

            for (int r = 0; r < rowCount; r++)
            {
                var row = rows[r] ?? "0000";

                // 4レーン固定（Left, Down, Up, Right）= 0..3
                for (int laneIndex = 0; laneIndex < 4 && laneIndex < row.Length; laneIndex++)
                {
                    if (row[laneIndex] != '1') continue;

                    var timeSec =
                        raw.offsetSec
                        + (m * secPerMeasure)
                        + ((double)r / subdiv) * secPerMeasure;

                    notes.Add(new NoteEvent(timeSec, (Lane)laneIndex));
                }
            }
        }

        var ordered = notes.OrderBy(n => n.TimeSec).ToList();
        return new Chart(raw.musicFile, raw.bpm, raw.offsetSec, ordered);
    }
}
