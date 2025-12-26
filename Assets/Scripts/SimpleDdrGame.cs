using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

public sealed class SimpleDdrGame : MonoBehaviour
{
    [Header("Chart")]
    [SerializeField] string chartFileName = "chart.json";

    [Header("Audio")]
    [SerializeField] AudioSource audioSource;
    [SerializeField] AudioClip musicClip;

    [Header("Spawn/Move")]
    [SerializeField] NoteView notePrefab;
    [SerializeField] Transform spawnY;
    [SerializeField] Transform judgeLineY;
    [SerializeField] float travelTimeSec = 1.5f;

    [Header("Lane X positions (Left, Down, Up, Right)")]
    [SerializeField] float[] laneXs = { -3f, -1f, 1f, 3f };

    [Header("Judgement Windows (sec)")]
    [SerializeField] float perfect = 0.03f;
    [SerializeField] float great = 0.06f;
    [SerializeField] float good = 0.10f;
    [SerializeField] float miss = 0.20f;

    [Header("Recording")]
    [SerializeField] bool enableRecording = true;
    [SerializeField] string recordedFileName = "chart_recorded.json";
    [SerializeField] float recordQuantizeSec = 0.0f; // 0なら量子化なし。例: 0.01 で10ms刻み

    Chart chart;
    double dspStartTime;
    int nextSpawnIndex;

    readonly Dictionary<Lane, LinkedList<NoteView>> active = new()
    {
        [Lane.Left] = new(),
        [Lane.Down] = new(),
        [Lane.Up] = new(),
        [Lane.Right] = new(),
    };

    bool isRecording;
    readonly List<NoteEvent> recordedNotes = new();

    void Start()
    {
        chart = Chart.LoadFromStreamingAssets(chartFileName);

        audioSource.clip = musicClip;
        dspStartTime = AudioSettings.dspTime + 0.2;
        audioSource.PlayScheduled(dspStartTime);

        nextSpawnIndex = 0;

        Debug.Log($"Loaded notes: {chart.Notes.Count}, offset: {chart.OffsetSec:0.###}");
    }

    void Update()
    {
        var songTime = GetSongTimeSec();

        SpawnNotes(songTime);
        UpdateNotePositions(songTime);

        HandleRecordingHotkeys(songTime);
        HandleInput(songTime);

        CleanupMissed(songTime);
    }

    double GetSongTimeSec()
        => (AudioSettings.dspTime - dspStartTime) + chart.OffsetSec;

    void HandleRecordingHotkeys(double songTime)
    {
        if (!enableRecording) return;

        var kb = Keyboard.current;
        if (kb == null) return;

        if (kb.rKey.wasPressedThisFrame)
        {
            isRecording = !isRecording;
            Debug.Log(isRecording
                ? "Recording: ON (press arrow keys to add notes)"
                : "Recording: OFF");
        }

        if (kb.sKey.wasPressedThisFrame)
        {
            SaveRecordedChart();
        }

        // 録画中にBで録画ノーツだけ全消し（保険）
        if (kb.bKey.wasPressedThisFrame)
        {
            recordedNotes.Clear();
            Debug.Log("Recorded notes cleared.");
        }
    }

    void SaveRecordedChart()
    {
        if (!enableRecording) return;

        // StreamingAssets は Editor/PC実行では書けるが、
        // 一部プラットフォームやビルド後は書けないことがある点に注意。
        Directory.CreateDirectory(Application.streamingAssetsPath);

        var outJson = new ChartJsonOut
        {
            musicFile = chart.MusicFile,
            offsetSec = (float)chart.OffsetSec,
            notes = recordedNotes
                .OrderBy(n => n.TimeSec)
                .Select(n => new NoteJsonOut
                {
                    timeSec = (float)n.TimeSec,
                    lane = n.Lane.ToString()
                })
                .ToArray()
        };

        var json = JsonUtility.ToJson(outJson, prettyPrint: true) + "\n";
        var path = Path.Combine(Application.streamingAssetsPath, recordedFileName);
        File.WriteAllText(path, json);

        Debug.Log($"Saved recorded chart: {path} (notes={recordedNotes.Count})");
    }

    void SpawnNotes(double songTime)
    {
        while (nextSpawnIndex < chart.Notes.Count)
        {
            var note = chart.Notes[nextSpawnIndex];
            var spawnTime = note.TimeSec - travelTimeSec;
            if (songTime < spawnTime) break;

            var view = Instantiate(notePrefab, transform);
            view.Init(note.Lane, note.TimeSec);
            view.transform.position = new Vector3(GetLaneX(note.Lane), spawnY.position.y, 0);

            active[note.Lane].AddLast(view);
            nextSpawnIndex++;
        }
    }

    void UpdateNotePositions(double songTime)
    {
        foreach (var lane in active.Keys.ToArray())
        {
            foreach (var n in active[lane])
            {
                var t = (float)((n.timeSec - songTime) / travelTimeSec);
                var y = Mathf.Lerp(judgeLineY.position.y, spawnY.position.y, Mathf.Clamp01(t));
                var x = GetLaneX(lane);
                n.transform.position = new Vector3(x, y, 0);
            }
        }
    }

    void HandleInput(double songTime)
    {
        var kb = Keyboard.current;
        if (kb == null) return;

        TryHit(Lane.Left, kb.leftArrowKey.wasPressedThisFrame, songTime);
        TryHit(Lane.Down, kb.downArrowKey.wasPressedThisFrame, songTime);
        TryHit(Lane.Up, kb.upArrowKey.wasPressedThisFrame, songTime);
        TryHit(Lane.Right, kb.rightArrowKey.wasPressedThisFrame, songTime);
    }

    void TryHit(Lane lane, bool pressed, double songTime)
    {
        if (!pressed) return;

        // 録画：押した瞬間にノーツを記録（判定より先にやる）
        if (enableRecording && isRecording)
        {
            var t = Quantize(songTime, recordQuantizeSec);
            recordedNotes.Add(new NoteEvent(t, lane));
            Debug.Log($"REC {lane} @ {t:0.000}");
        }

        var list = active[lane];
        if (list.First == null)
        {
            Debug.Log($"{lane}: 空振り");
            return;
        }

        var note = list.First.Value;
        var dt = Math.Abs(note.timeSec - songTime);

        var result =
            dt <= perfect ? "Perfect" :
            dt <= great ? "Great" :
            dt <= good ? "Good" :
            dt <= miss ? "Bad" : "TooEarly/TooLate";

        Debug.Log($"{lane}: {result} (dt={dt:0.000})");

        if (dt <= miss)
        {
            list.RemoveFirst();
            Destroy(note.gameObject);
        }
    }

    static double Quantize(double timeSec, float stepSec)
    {
        if (stepSec <= 0) return timeSec;
        return Math.Round(timeSec / stepSec) * stepSec;
    }

    void CleanupMissed(double songTime)
    {
        foreach (var lane in active.Keys.ToArray())
        {
            var list = active[lane];
            while (list.First != null)
            {
                var n = list.First.Value;
                if (songTime <= n.timeSec + miss) break;

                Debug.Log($"{lane}: Miss (late)");
                list.RemoveFirst();
                Destroy(n.gameObject);
            }
        }
    }

    float GetLaneX(Lane lane)
    {
        var i = (int)lane;
        if (laneXs == null || laneXs.Length < 4) return 0f;
        if ((uint)i >= (uint)laneXs.Length) return 0f;
        return laneXs[i];
    }

    // ★ 書き出し用（JsonUtility向け）
    [Serializable]
    private class ChartJsonOut
    {
        public string musicFile;
        public float offsetSec;
        public NoteJsonOut[] notes;
    }

    [Serializable]
    private class NoteJsonOut
    {
        public float timeSec;
        public string lane;
    }
}
