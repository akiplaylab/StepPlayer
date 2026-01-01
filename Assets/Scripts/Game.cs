using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public sealed class Game : MonoBehaviour
{
    [Header("Song Select")]
    [SerializeField] List<SongDefinition> songs = new();
    [SerializeField] int selectedSongIndex = 0;

    [Header("Audio")]
    [SerializeField] AudioSource audioSource;

    [Header("Spawn/Move")]
    [SerializeField] NoteView notePrefab;
    [SerializeField] Transform spawnY;
    [SerializeField] Transform judgeLineY;
    [SerializeField] float travelTimeSec = 1.5f;

    [Header("Lane X positions (Left, Down, Up, Right)")]
    [SerializeField] float[] laneXs = { -3f, -1f, 1f, 3f };

    [Header("Recording")]
    [SerializeField] bool enableRecording = true;
    [SerializeField] string recordedFileName = "chart_recorded.json";
    [SerializeField] int recordSubdiv = 16;

    [Header("Receptor Effects (fixed lanes)")]
    [SerializeField] ReceptorHitEffect leftFx;
    [SerializeField] ReceptorHitEffect downFx;
    [SerializeField] ReceptorHitEffect upFx;
    [SerializeField] ReceptorHitEffect rightFx;

    [Header("Judgement")]
    [SerializeField] Judge judge;

    [SerializeField] float endFadeOutSec = 0.4f;

    [SerializeField] bool endWhenChartFinished = true;
    [SerializeField] float endWhenChartFinishedDelaySec = 0.8f;

    Chart chart;
    ChartRecorder recorder;
    NoteViewPool notePool;
    double dspStartTime;
    int nextSpawnIndex;
    bool isEnding;
    float initialVolume;
    double chartFinishedAtSongTime = double.NaN;

    readonly JudgementCounter counter = new();

    readonly Dictionary<Lane, LinkedList<NoteView>> active = new()
    {
        [Lane.Left] = new(),
        [Lane.Down] = new(),
        [Lane.Up] = new(),
        [Lane.Right] = new(),
    };

    void Awake()
    {
        notePool = new NoteViewPool(notePrefab, transform, prewarm: 16);
    }

    IEnumerator Start()
    {
        ResultStore.Clear();
        counter.Reset();

        if (songs == null || songs.Count == 0)
            throw new InvalidOperationException("songs が空です。Inspector で SongDefinition を追加してください。");

        selectedSongIndex = Mathf.Clamp(selectedSongIndex, 0, songs.Count - 1);
        var song = songs[selectedSongIndex];

        if (string.IsNullOrWhiteSpace(song.songId))
            throw new InvalidOperationException("SongDefinition.songId が空です。");

        if (song.musicClip == null)
            throw new InvalidOperationException($"SongDefinition.musicClip が未設定です: {song.songId}");

        var chartRelativePath = Path.Combine("Songs", song.songId, song.chartFileName);

        chart = ChartLoader.LoadFromStreamingAssets(chartRelativePath);

        recorder = new ChartRecorder(enableRecording, recordedFileName, recordSubdiv);

        audioSource.clip = song.musicClip;

        initialVolume = audioSource != null ? audioSource.volume : 1f;

        if (!song.musicClip.preloadAudioData)
            song.musicClip.LoadAudioData();

        while (song.musicClip.loadState == AudioDataLoadState.Loading)
            yield return null;

        dspStartTime = AudioSettings.dspTime + 0.2;
        audioSource.PlayScheduled(dspStartTime);

        nextSpawnIndex = 0;

        Debug.Log($"Loaded song: {song.songId}, notes: {chart.Notes.Count}, offset: {chart.OffsetSec:0.###}, bpm: {chart.Bpm:0.###}");
    }

    void Update()
    {
        if (AudioSettings.dspTime < dspStartTime)
            return;

        if (isEnding)
            return;

        var songTime = GetSongTimeSec();

        SpawnNotes(songTime);
        UpdateNotePositions(songTime);

        recorder.UpdateHotkeys(chart);
        HandleInput(songTime);

        CleanupMissed(songTime);

        if (endWhenChartFinished)
        {
            bool allSpawned = nextSpawnIndex >= chart.Notes.Count;
            bool noActiveNotes = active.Values.All(list => list.Count == 0);

            if (allSpawned && noActiveNotes)
            {
                if (double.IsNaN(chartFinishedAtSongTime))
                    chartFinishedAtSongTime = songTime;

                if (songTime - chartFinishedAtSongTime >= endWhenChartFinishedDelaySec)
                    EndToResult();

                return;
            }
            else
            {
                chartFinishedAtSongTime = double.NaN;
            }
        }

        if (nextSpawnIndex >= chart.Notes.Count && !audioSource.isPlaying)
        {
            EndToResult();
        }
    }

    double GetSongTimeSec()
    => (AudioSettings.dspTime - dspStartTime) - chart.OffsetSec;

    void SpawnNotes(double songTime)
    {
        while (nextSpawnIndex < chart.Notes.Count)
        {
            var note = chart.Notes[nextSpawnIndex];
            var spawnTime = note.TimeSec - travelTimeSec;
            if (songTime < spawnTime) break;

            var view = notePool.Rent();
            view.Init(note);
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
                var t = (float)((n.TimeSec - songTime) / travelTimeSec);
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

        TryHit(Lane.Left, kb.leftArrowKey.wasPressedThisFrame || kb.dKey.wasPressedThisFrame, songTime);
        TryHit(Lane.Down, kb.downArrowKey.wasPressedThisFrame || kb.fKey.wasPressedThisFrame, songTime);
        TryHit(Lane.Up, kb.upArrowKey.wasPressedThisFrame || kb.jKey.wasPressedThisFrame, songTime);
        TryHit(Lane.Right, kb.rightArrowKey.wasPressedThisFrame || kb.kKey.wasPressedThisFrame, songTime);
    }

    void TryHit(Lane lane, bool pressed, double songTime)
    {
        if (!pressed) return;

        recorder.OnKeyPressed(lane, songTime);

        var list = active[lane];
        if (list.First == null)
        {
            Debug.Log($"{lane}: 空振り");
            return;
        }

        var note = list.First.Value;
        var dt = Math.Abs(note.TimeSec - songTime);

        var judgement = judge.JudgeHit(lane, dt);
        GetFx(lane).Play(judgement.Intensity);

        if (judgement.ShouldConsumeNote)
        {
            counter.Record(judgement.Judgement);
            list.RemoveFirst();
            notePool.Return(note);
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
                if (songTime <= n.TimeSec + judge.MissWindow) break;

                counter.RecordMiss();
                Debug.Log($"{lane}: Miss (late)");
                list.RemoveFirst();
                notePool.Return(n);
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

    public void EndToResult()
    {
        if (isEnding) return;
        isEnding = true;

        StartCoroutine(FadeOutAndLoadResult());
    }

    IEnumerator FadeOutAndLoadResult()
    {
        if (audioSource != null)
        {
            float from = audioSource.volume;
            float to = 0f;

            float t = 0f;
            float dur = Mathf.Max(0.01f, endFadeOutSec);

            while (t < dur)
            {
                t += Time.unscaledDeltaTime;
                float a = Mathf.Clamp01(t / dur);
                audioSource.volume = Mathf.Lerp(from, to, a);
                yield return null;
            }

            audioSource.volume = 0f;
            audioSource.Stop();

            audioSource.volume = initialVolume;
        }

        ResultStore.Summary = counter.CreateSummary();
        ResultStore.HasSummary = true;

        SceneManager.LoadScene("ResultScene");
    }
}
