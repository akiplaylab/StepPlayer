using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(StreamingAssetLoader))]
public sealed class PlayScene : MonoBehaviour
{
    [Header("Song Select")]
    StreamingAssetLoader loader;
    [SerializeField] int fallbackSongIndex = 0;

    [Header("Audio")]
    [SerializeField] AudioSource audioSource;

    [Header("UI")]
    [SerializeField] PlaySceneSongInfoPresenter songInfoPresenter;

    [Header("Spawn/Move")]
    [SerializeField] Transform notesRoot;
    [SerializeField] NoteView notePrefab;
    [SerializeField] Transform spawnY;
    [SerializeField] Transform judgeLineY;
    [SerializeField] float travelTimeSec = 2.6f;

    [Header("Lane X positions (Left, Down, Up, Right)")]
    readonly float[] laneXs = { -2.6f, -0.85f, 0.85f, 2.6f };

    [Header("Receptor Effects (fixed lanes)")]
    [SerializeField] ReceptorHitEffect leftFx;
    [SerializeField] ReceptorHitEffect downFx;
    [SerializeField] ReceptorHitEffect upFx;
    [SerializeField] ReceptorHitEffect rightFx;

    [Header("Judgement")]
    [SerializeField] Judge judge;
    [SerializeField] ComboTextPresenter comboText;
    [SerializeField] JudgementStyle judgementStyle;

    [SerializeField] float endFadeOutSec = 0.4f;

    [SerializeField] bool endWhenChartFinished = true;
    [SerializeField] float endWhenChartFinishedDelaySec = 0.8f;

    Chart chart;
    NoteViewPool notePool;
    NoteSpawner noteSpawner;
    InputJudge inputJudge;
    ResultFlow resultFlow;
    double dspStartTime;
    double outputLatencySec;
    bool isEnding;
    float initialVolume;
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
        loader = GetComponent<StreamingAssetLoader>();
        if (songInfoPresenter == null)
            songInfoPresenter = gameObject.AddComponent<PlaySceneSongInfoPresenter>();

        if (notesRoot == null)
        {
            var go = new GameObject("Notes");
            go.transform.SetParent(transform, worldPositionStays: false);
            notesRoot = go.transform;
        }

        notePool = new NoteViewPool(notePrefab, notesRoot, prewarm: 16);
    }

    IEnumerator Start()
    {
        ResultStore.Clear();
        counter.Reset();
        comboText?.Show(counter.CurrentCombo);

        var song = (SelectedSong.Value ?? GetFallbackSong()) ?? throw new InvalidOperationException("No song selected and no fallback song available (catalog empty).");
        currentSong = song;
        songInfoPresenter?.SetSong(song, song.ChartDifficulty);

        if (song.MusicClip == null)
            yield return loader.LoadAudioClip(song, clip => song.MusicClip = clip);

        if (song.MusicClip == null)
            throw new InvalidOperationException($"SongMeta.MusicClip が未設定です: {song.SmFilePath}");

        var chartRelativePath = GetRelativeStreamingAssetsPath(song.SmFilePath);
        chart = ChartLoader.LoadFromStreamingAssets(chartRelativePath, song.ChartDifficulty);

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

        noteSpawner = new NoteSpawner(chart, notePool, spawnY, judgeLineY, travelTimeSec, laneXs, active);
        inputJudge = new InputJudge(judge, comboText, judgementStyle, counter, notePool, active, GetFx);
        resultFlow = new ResultFlow(endWhenChartFinished, endWhenChartFinishedDelaySec);

        Debug.Log($"Loaded song: {song.DisplayTitle}, notes: {chart.Notes.Count}, offset: {chart.OffsetSec:0.###}, bpm: {chart.Bpm:0.###}, outputLatency: {outputLatencySec:0.###}");
    }

    void Update()
    {
        if (noteSpawner == null || inputJudge == null || resultFlow == null || chart == null)
            return;

        if (AudioSettings.dspTime < dspStartTime)
            return;

        if (isEnding)
            return;

        var songTime = GetSongTimeSec();

        noteSpawner.SpawnNotes(songTime);
        noteSpawner.UpdateNotePositions(songTime);
        inputJudge.HandleInput(songTime);
        inputJudge.CleanupMissed(songTime);

        if (resultFlow.ShouldEnd(songTime, noteSpawner.AllSpawned, active, audioSource.isPlaying))
            EndToResult();
    }

    double GetSongTimeSec()
    => (AudioSettings.dspTime - dspStartTime) - chart.OffsetSec - outputLatencySec;

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

        ResultStore.Summary = counter.CreateSummary(chart?.Notes.Count ?? 0);
        ResultStore.HasSummary = true;
        if (currentSong != null)
        {
            ResultStore.SongTitle = currentSong.DisplayTitle;
            ResultStore.MusicSource = string.IsNullOrWhiteSpace(currentSong.Artist) ? string.Empty : currentSong.Artist;
            ResultStore.ChartDifficulty = currentSong.ChartDifficulty;
        }

        SceneManager.LoadScene(nameof(ResultScene));
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
            var relative = fullPath[root.Length..].TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            return relative;
        }

        return fullPath;
    }
}
