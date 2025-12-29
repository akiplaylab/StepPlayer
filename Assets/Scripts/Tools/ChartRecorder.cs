using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

public sealed class ChartRecorder
{
    readonly bool enable;
    readonly string recordedFileName;
    readonly int recordSubdiv;
    bool warnedInvalidSubdiv;

    bool isRecording;
    readonly List<RecordedNote> notes = new();

    sealed class RecordedNote
    {
        public int MeasureIndex { get; }
        public int RowIndex { get; }
        public double QuantizedTimeSec { get; }
        public Lane Lane { get; }
        public NoteDivision Division { get; }

        public RecordedNote(int measureIndex, int rowIndex, double quantizedTimeSec, Lane lane, NoteDivision division)
        {
            MeasureIndex = measureIndex;
            RowIndex = rowIndex;
            QuantizedTimeSec = quantizedTimeSec;
            Lane = lane;
            Division = division;
        }
    }

    public ChartRecorder(bool enable, string recordedFileName, int recordSubdiv)
    {
        this.enable = enable;
        this.recordedFileName = recordedFileName;
        this.recordSubdiv = recordSubdiv <= 0 ? 16 : recordSubdiv;
    }

    public bool IsRecording => enable && isRecording;

    public void UpdateHotkeys(Chart chart)
    {
        if (!enable) return;

        var kb = Keyboard.current;
        if (kb == null) return;

        if (kb.rKey.wasPressedThisFrame)
        {
            isRecording = !isRecording;
            Debug.Log(isRecording ? "Recording: ON" : "Recording: OFF");
        }

        if (kb.sKey.wasPressedThisFrame)
        {
            Save(chart);
        }

        if (kb.bKey.wasPressedThisFrame)
        {
            notes.Clear();
            Debug.Log("Recorded notes cleared.");
        }
    }

    public void OnKeyPressed(Lane lane, double songTime, Chart chart)
    {
        if (!IsRecording || chart == null) return;

        var subdiv = GetEffectiveSubdiv();
        var secPerMeasure = (60.0 / chart.Bpm) * 4.0;

        var (measureIndex, rowIndex) = QuantizeToGrid(songTime, secPerMeasure, subdiv);
        var quantizedTimeSec =
            (measureIndex * secPerMeasure)
            + ((double)rowIndex / subdiv) * secPerMeasure;

        notes.Add(new RecordedNote(
            measureIndex,
            rowIndex,
            quantizedTimeSec,
            lane,
            DivisionFromRow(rowIndex)
        ));
    }

    void Save(Chart chart)
    {
        var subdiv = GetEffectiveSubdiv();

        var measures = new Dictionary<int, string[]>();

        foreach (var n in notes
                     .OrderBy(n => n.MeasureIndex)
                     .ThenBy(n => n.RowIndex)
                     .ThenBy(n => n.Lane))
        {
            var measureIndex = n.MeasureIndex;
            var row = n.RowIndex;

            if (!measures.TryGetValue(measureIndex, out var rows))
            {
                rows = Enumerable.Repeat("0000", subdiv).ToArray();
                measures[measureIndex] = rows;
            }

            var chars = rows[row].ToCharArray();
            chars[(int)n.Lane] = '1';
            rows[row] = new string(chars);
        }

        var maxM = measures.Count == 0 ? 0 : measures.Keys.Max();
        var outMeasures = new ChartJson.Measure[maxM + 1];

        for (int m = 0; m <= maxM; m++)
        {
            if (!measures.TryGetValue(m, out var rows))
                rows = Enumerable.Repeat("0000", subdiv).ToArray();

            outMeasures[m] = new ChartJson.Measure { subdiv = subdiv, rows = rows };
        }

        var outJson = new ChartJson
        {
            musicFile = chart.MusicFile,
            bpm = chart.Bpm,
            offsetSec = chart.OffsetSec,
            measures = outMeasures
        };

        Directory.CreateDirectory(Application.streamingAssetsPath);
        var json = JsonUtility.ToJson(outJson, prettyPrint: true) + "\n";
        var path = Path.Combine(Application.streamingAssetsPath, recordedFileName);
        File.WriteAllText(path, json);

        Debug.Log($"Saved recorded chart: {path} (notes={notes.Count}, subdiv={subdiv})");
    }

    int GetEffectiveSubdiv()
    {
        if (recordSubdiv % 4 == 0) return recordSubdiv;

        if (!warnedInvalidSubdiv)
        {
            Debug.LogWarning($"recordSubdiv should be multiple of 4. current={recordSubdiv}. Forced to 16.");
            warnedInvalidSubdiv = true;
        }

        return 16;
    }

    static (int measureIndex, int rowIndex) QuantizeToGrid(double songTime, double secPerMeasure, int subdiv)
    {
        var measureIndex = (int)Math.Floor(songTime / secPerMeasure);
        var inMeasure = songTime - measureIndex * secPerMeasure;
        var row = (int)Math.Round((inMeasure / secPerMeasure) * subdiv, MidpointRounding.AwayFromZero);

        if (row >= subdiv) { row = 0; measureIndex += 1; }
        if (row < 0) { row = 0; }

        return (measureIndex, row);
    }

    static NoteDivision DivisionFromRow(int row)
    {
        if (row % 4 == 0) return NoteDivision.Quarter;
        if (row % 2 == 0) return NoteDivision.Eighth;
        return NoteDivision.Sixteenth;
    }
}
