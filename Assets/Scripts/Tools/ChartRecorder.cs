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

    bool isRecording;
    readonly List<Note> notes = new();

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

    public void OnKeyPressed(Lane lane, double songTime)
    {
        if (!IsRecording) return;

        notes.Add(new Note(
            songTime,
            lane,
            NoteDivision.Quarter // 仮
        ));
    }

    void Save(Chart chart)
    {
        var subdiv = recordSubdiv;
        if (subdiv % 4 != 0)
        {
            Debug.LogWarning($"recordSubdiv should be multiple of 4. current={subdiv}. Forced to 16.");
            subdiv = 16;
        }

        var secPerBeat = 60.0 / chart.Bpm;
        var secPerMeasure = secPerBeat * 4.0;

        var measures = new Dictionary<int, string[]>();

        var secPerRow = secPerMeasure / subdiv;

        foreach (var n in notes)
        {
            var rawRow = n.TimeSec / secPerRow;
            var quantizedRow = (int)Math.Round(rawRow, MidpointRounding.AwayFromZero);

            if (quantizedRow < 0)
                quantizedRow = 0;

            var measureIndex = quantizedRow / subdiv;
            var row = quantizedRow % subdiv;

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
}
