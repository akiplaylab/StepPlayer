using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class PlayController : MonoBehaviour
{
    [Header("Song Select")]
    [SerializeField] StreamingAssetLoader loader;
    [SerializeField] int fallbackSongIndex = 0;

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
    [SerializeField] ComboTextPresenter comboText;

    [SerializeField] float endFadeOutSec = 0.4f;

    [SerializeField] bool endWhenChartFinished = true;
    [SerializeField] float endWhenChartFinishedDelaySec = 0.8f;

    Chart chart;
    ChartRecorder recorder;
    NoteViewPool notePool;
    double dspStartTime;
    double outputLatencySec;
    int nextSpawnIndex;
    bool isEnding;
    float initialVolume;
    double chartFinishedAtSongTime = double.NaN;
    SongMeta currentSong;

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
        UpdateComboDisplay();

        var song = SelectedSong.Value ?? GetFallbackSong();
        if (song == null)
            throw new InvalidOperationException("No song selected and no fallback song available (catalog empty).");

        currentSong = song;

        if (loader == null)
            loader = GetComponent<StreamingAssetLoader>();
        if (loader == null)
            loader = gameObject.AddComponent<StreamingAssetLoader>();

        if (song.MusicClip == null)
            yield return loader.LoadAudioClip(song, clip => song.MusicClip = clip);

        if (song.MusicClip == null)
            throw new InvalidOperationException($"SongMeta.MusicClip が未設定です: {song.SmFilePath}");

        var chartRelativePath = GetRelativeStreamingAssetsPath(song.SmFilePath);
        chart = ChartLoader.LoadFromStreamingAssets(chartRelativePath, song.ChartDifficulty);

        recorder = new ChartRecorder(enableRecording, recordedFileName, recordSubdiv);

        audioSource.clip = song.MusicClip;

        initialVolume = audioSource != null ? audioSource.volume : 1f;

        if (!song.MusicClip.preloadAudioData)
            song.MusicClip.LoadAudioData();

        while (song.MusicClip.loadState == AudioDataLoadState.Loading)
            yield return null;

        AudioSettings.GetDSPBufferSize(out var bufferLength, out var numBuffers);
        outputLatencySec = (double)bufferLength * numBuffers / AudioSettings.outputSampleRate;

        dspStartTime = AudioSettings.dspTime + 0.2;
        audioSource.PlayScheduled(dspStartTime);

        nextSpawnIndex = 0;

        Debug.Log($"Loaded song: {song.DisplayTitle}, notes: {chart.Notes.Count}, offset: {chart.OffsetSec:0.###}, bpm: {chart.Bpm:0.###}, outputLatency: {outputLatencySec:0.###}");
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
    => (AudioSettings.dspTime - dspStartTime) - chart.OffsetSec - outputLatencySec;

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
        TryHit(Lane.Left, KeyBindings.LanePressedThisFrame(Lane.Left), songTime);
        TryHit(Lane.Down, KeyBindings.LanePressedThisFrame(Lane.Down), songTime);
        TryHit(Lane.Up, KeyBindings.LanePressedThisFrame(Lane.Up), songTime);
        TryHit(Lane.Right, KeyBindings.LanePressedThisFrame(Lane.Right), songTime);
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
            UpdateComboDisplay();
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

                UpdateComboDisplay();
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

    void UpdateComboDisplay()
    {
        comboText?.Show(counter.CurrentCombo);
    }

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

        ResultStore.Summary = counter.CreateSummary(chart?.Notes.Count ?? 0);
        ResultStore.HasSummary = true;
        if (currentSong != null)
        {
            ResultStore.SongTitle = currentSong.DisplayTitle;
            ResultStore.MusicSource = string.IsNullOrWhiteSpace(currentSong.Artist) ? string.Empty : currentSong.Artist;
        }

        SceneManager.LoadScene("Result");
    }

    SongMeta GetFallbackSong()
    {
        var songs = SongCatalog.BuildCatalog();
        if (songs.Count == 0) return null;
        var index = Mathf.Clamp(fallbackSongIndex, 0, songs.Count - 1);
        return songs[index];
    }

    static string GetRelativeStreamingAssetsPath(string fullPath)
    {
        var root = Application.streamingAssetsPath;
        if (fullPath.StartsWith(root, StringComparison.OrdinalIgnoreCase))
        {
            var relative = fullPath.Substring(root.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            return relative;
        }

        return fullPath;
    }
}
