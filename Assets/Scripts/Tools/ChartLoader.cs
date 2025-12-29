using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public static class ChartLoader
{
    const int MaxSupportedBpm = 1000;

    public static Chart LoadFromStreamingAssets(string fileName)
    {
        var path = Path.Combine(Application.streamingAssetsPath, fileName);
        var json = File.ReadAllText(path);

        var raw = JsonUtility.FromJson<ChartJson>(json)
                  ?? throw new InvalidDataException($"Failed to parse {nameof(ChartJson)}.");
        raw.measures ??= Array.Empty<ChartJson.Measure>();

        if (raw.bpm <= 0 || raw.bpm > MaxSupportedBpm)
            throw new InvalidDataException($"Invalid bpm: {raw.bpm} (must be between 1 and {MaxSupportedBpm})");

        var secPerBeat = 60.0 / raw.bpm;
        var secPerMeasure = secPerBeat * 4.0;

        var notes = new List<Note>(1024);

        for (int m = 0; m < raw.measures.Length; m++)
        {
            var measure = raw.measures[m];
            if (measure == null) continue;

            var subdiv = measure.subdiv <= 0 ? 16 : measure.subdiv;
            var rows = measure.rows ?? Array.Empty<string>();

            var rowCount = Math.Min(subdiv, rows.Length);

            for (int r = 0; r < rowCount; r++)
            {
                var row = rows[r] ?? "0000";

                for (int laneIndex = 0; laneIndex < 4 && laneIndex < row.Length; laneIndex++)
                {
                    if (row[laneIndex] != '1') continue;

                    var timeSec =
                        raw.offsetSec
                        + (m * secPerMeasure)
                        + ((double)r / subdiv) * secPerMeasure;

                    var division = DivisionFromRow(r);

                    notes.Add(new Note(
                        timeSec,
                        (Lane)laneIndex,
                        division
                    ));
                }
            }
        }

        var ordered = notes.OrderBy(n => n.TimeSec).ToList();
        return new Chart(raw.musicFile, raw.bpm, raw.offsetSec, ordered);
    }

    static NoteDivision DivisionFromRow(int row)
    {
        if (row % 4 == 0) return NoteDivision.Quarter;
        if (row % 2 == 0) return NoteDivision.Eighth;
        return NoteDivision.Sixteenth;
    }
}
