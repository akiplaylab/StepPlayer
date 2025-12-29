using System;
using System.Collections;
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

    [Tooltip("録画を小節内で何分割するか。DDRなら16が基本。")]
    [SerializeField] int recordSubdiv = 16;

    [Header("Receptor Effects (fixed lanes)")]
    [SerializeField] ReceptorHitEffect leftFx;
    [SerializeField] ReceptorHitEffect downFx;
    [SerializeField] ReceptorHitEffect upFx;
    [SerializeField] ReceptorHitEffect rightFx;

    [SerializeField] JudgementTextPresenter judgementText;

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
    readonly List<Note> recordedNotes = new();

    IEnumerator Start()
    {
        chart = Chart.LoadFromStreamingAssets(chartFileName);

        audioSource.clip = musicClip;

        if (!musicClip.preloadAudioData)
            musicClip.LoadAudioData();

        while (musicClip.loadState == AudioDataLoadState.Loading)
            yield return null;

        dspStartTime = AudioSettings.dspTime + 0.2;
        audioSource.PlayScheduled(dspStartTime);

        nextSpawnIndex = 0;

        Debug.Log($"Loaded notes: {chart.Notes.Count}, offset: {chart.OffsetSec:0.###}, bpm: {chart.Bpm:0.###}");
    }

    void Update()
    {
        if (AudioSettings.dspTime < dspStartTime)
            return;

        var songTime = GetSongTimeSec();

        SpawnNotes(songTime);
        UpdateNotePositions(songTime);

        HandleRecordingHotkeys();
        HandleInput(songTime);

        CleanupMissed(songTime);
    }

    double GetSongTimeSec()
    => (AudioSettings.dspTime - dspStartTime) - chart.OffsetSec;

    void HandleRecordingHotkeys()
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
            SaveRecordedChartJson();
        }

        if (kb.bKey.wasPressedThisFrame)
        {
            recordedNotes.Clear();
            Debug.Log("Recorded notes cleared.");
        }
    }

    void SaveRecordedChartJson()
    {
        if (!enableRecording) return;

        if (recordSubdiv <= 0) recordSubdiv = 16;
        if (recordSubdiv % 4 != 0)
        {
            Debug.LogWarning($"recordSubdiv should be multiple of 4. current={recordSubdiv}. Forced to 16.");
            recordSubdiv = 16;
        }

        // 4/4固定（1小節=4拍）
        var secPerBeat = 60.0 / chart.Bpm;
        var secPerMeasure = secPerBeat * 4.0;

        // measureIndex -> rows
        var measures = new Dictionary<int, string[]>();

        foreach (var n in recordedNotes)
        {
            var rawTime = n.TimeSec;

            // measure/row を計算
            var measureIndex = (int)Math.Floor(rawTime / secPerMeasure);
            var inMeasure = rawTime - measureIndex * secPerMeasure;
            var row = (int)Math.Round((inMeasure / secPerMeasure) * recordSubdiv);

            // 端の丸め（row==recordSubdiv になったら次小節の0へ）
            if (row >= recordSubdiv) { row = 0; measureIndex += 1; }
            if (row < 0) { row = 0; }

            if (!measures.TryGetValue(measureIndex, out var rows))
            {
                rows = Enumerable.Repeat("0000", recordSubdiv).ToArray();
                measures[measureIndex] = rows;
            }

            // row の lane を 1 にする（同時押しは OR）
            var chars = rows[row].ToCharArray();
            chars[(int)n.Lane] = '1';
            rows[row] = new string(chars);
        }

        // 0..maxMeasure で欠けを埋めて配列化
        var maxM = measures.Count == 0 ? 0 : measures.Keys.Max();
        var outMeasures = new ChartJson.Measure[maxM + 1];

        for (int m = 0; m <= maxM; m++)
        {
            if (!measures.TryGetValue(m, out var rows))
                rows = Enumerable.Repeat("0000", recordSubdiv).ToArray();

            outMeasures[m] = new ChartJson.Measure
            {
                subdiv = recordSubdiv,
                rows = rows
            };
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

        Debug.Log($"Saved recorded chart: {path} (notes={recordedNotes.Count}, subdiv={recordSubdiv})");
    }

    void SpawnNotes(double songTime)
    {
        while (nextSpawnIndex < chart.Notes.Count)
        {
            var note = chart.Notes[nextSpawnIndex];
            var spawnTime = note.TimeSec - travelTimeSec;
            if (songTime < spawnTime) break;

            var view = Instantiate(notePrefab, transform);
            view.Init(note.Lane, note.TimeSec, note.Division);
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
            recordedNotes.Add(new Note(
                songTime,
                lane,
                NoteDivision.Quarter   // 録画中は仮でOK
            ));

            Debug.Log($"REC {lane} @ {songTime:0.000}");
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

        float intensity =
            dt <= perfect ? 1.0f :
            dt <= great ? 0.75f :
            dt <= good ? 0.55f :
            dt <= miss ? 0.35f : 0.0f;

        GetFx(lane).Play(intensity);

        Judgement judgement =
            dt <= perfect ? Judgement.Perfect :
            dt <= great ? Judgement.Great :
            dt <= good ? Judgement.Good :
            Judgement.Bad;

        judgementText.Show(judgement);

        Debug.Log($"{lane}: {result} (dt={dt:0.000})");

        if (dt <= miss)
        {
            list.RemoveFirst();
            Destroy(note.gameObject);
        }
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

    ReceptorHitEffect GetFx(Lane lane) => lane switch
    {
        Lane.Left => leftFx,
        Lane.Down => downFx,
        Lane.Up => upFx,
        Lane.Right => rightFx,
        _ => throw new InvalidDataException($"Invalid lane: {lane}"),
    };
}
