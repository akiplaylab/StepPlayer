using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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

    [Header("Scroll (Beat Based)")]
    [SerializeField] float beatsAhead = 4.0f; // 何拍前に表示するか
    [SerializeField] float hiSpeed = 1.0f;    // ハイスピ倍率

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
        loader = GetComponent<StreamingAssetLoader>();
        if (songInfoPresenter == null)
            songInfoPresenter = gameObject.AddComponent<PlaySceneSongInfoPresenter>();

        if (notesRoot == null)
        {
            var go = new GameObject("Notes");
            go.transform.SetParent(transform, false);
            notesRoot = go.transform;
        }

        notePool = new NoteViewPool(notePrefab, notesRoot, prewarm: 16);
    }

    IEnumerator Start()
    {
        ResultStore.Clear();
        counter.Reset();
        UpdateComboDisplay();

        var song = (SelectedSong.Value ?? GetFallbackSong())
            ?? throw new InvalidOperationException("No song selected.");
        currentSong = song;
        songInfoPresenter?.SetSong(song, song.ChartDifficulty);

        if (song.MusicClip == null)
            yield return loader.LoadAudioClip(song, clip => song.MusicClip = clip);

        var chartRelativePath = GetRelativeStreamingAssetsPath(song.SmFilePath);
        chart = ChartLoader.LoadFromStreamingAssets(chartRelativePath, song.ChartDifficulty);

        audioSource.clip = song.MusicClip;
        initialVolume = audioSource.volume;

        AudioSettings.GetDSPBufferSize(out var bufferLength, out var numBuffers);
        outputLatencySec =
            (double)bufferLength * numBuffers / AudioSettings.outputSampleRate;

        dspStartTime = AudioSettings.dspTime + 0.2;
        audioSource.PlayScheduled(dspStartTime);

        nextSpawnIndex = 0;
    }

    void Update()
    {
        if (AudioSettings.dspTime < dspStartTime || isEnding)
            return;

        var songTime = GetSongTimeSec();

        SpawnNotes(songTime);
        UpdateNotePositions(songTime);
        HandleInput(songTime);
        CleanupMissed(songTime);

        if (endWhenChartFinished)
        {
            bool allSpawned = nextSpawnIndex >= chart.Notes.Count;
            bool noActiveNotes = active.Values.All(l => l.Count == 0);

            if (allSpawned && noActiveNotes)
            {
                if (double.IsNaN(chartFinishedAtSongTime))
                    chartFinishedAtSongTime = songTime;

                if (songTime - chartFinishedAtSongTime >= endWhenChartFinishedDelaySec)
                    EndToResult();
            }
            else
            {
                chartFinishedAtSongTime = double.NaN;
            }
        }
    }

    double GetSongTimeSec()
        => (AudioSettings.dspTime - dspStartTime)
           - chart.OffsetSec
           - outputLatencySec;

    // ==========================
    // ノーツ生成（拍距離基準）
    // ==========================
    void SpawnNotes(double songTime)
    {
        double currentBeat = chart.SecondsToBeat(songTime);

        while (nextSpawnIndex < chart.Notes.Count)
        {
            var note = chart.Notes[nextSpawnIndex];

            if (note.Beat - currentBeat > beatsAhead)
                break;

            var noteTimeSec = chart.BeatToSeconds(note.Beat);

            var view = notePool.Rent();
            view.Init(note, noteTimeSec);
            view.transform.position =
                new Vector3(GetLaneX(note.Lane), spawnY.position.y, 0);

            active[note.Lane].AddLast(view);
            nextSpawnIndex++;
        }
    }

    // ==========================
    // BPM変化完全対応スクロール
    // ==========================
    void UpdateNotePositions(double songTime)
{
    double currentBeat = chart.SecondsToBeat(songTime);

    foreach (var lane in active.Keys.ToArray())
    {
        foreach (var n in active[lane])
        {
            double beatDiff =
                (n.Beat - currentBeat) * hiSpeed;

            float t = (float)(beatDiff / beatsAhead);

            float y = Mathf.LerpUnclamped(
                judgeLineY.position.y,
                spawnY.position.y,
                t
            );

            n.transform.position =
                new Vector3(GetLaneX(lane), y, 0);
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
        var list = active[lane];
        if (list.First == null) return;

        var note = list.First.Value;
        var dt = Math.Abs(note.TimeSec - songTime);

        var judgement = judge.JudgeHit(lane, dt);
        GetFx(lane).Play(judgement.Intensity);

        if (judgement.ShouldConsumeNote)
        {
            counter.Record(judgement.Judgement);
            list.RemoveFirst();
            PlayBurstAndReturn(note, judgement.Judgement);
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
                list.RemoveFirst();
                PlayBurstAndReturn(n, Judgement.Miss);
                UpdateComboDisplay();
            }
        }
    }

    float GetLaneX(Lane lane)
        => laneXs[(int)lane];

    ReceptorHitEffect GetFx(Lane lane) => lane switch
    {
        Lane.Left => leftFx,
        Lane.Down => downFx,
        Lane.Up => upFx,
        Lane.Right => rightFx,
        _ => null
    };

    void UpdateComboDisplay()
        => comboText?.Show(counter.CurrentCombo);

    void PlayBurstAndReturn(NoteView note, Judgement judgement)
    {
        var style = judgementStyle ?? judge?.Style;
        if (style == null)
        {
            notePool.Return(note);
            return;
        }

        note.PlayHitBurst(style.GetColor(judgement),
            () => notePool.Return(note));
    }

    public void EndToResult()
    {
        if (isEnding) return;
        isEnding = true;
        StartCoroutine(FadeOutAndLoadResult());
    }

    IEnumerator FadeOutAndLoadResult()
    {
        float t = 0f;
        while (t < endFadeOutSec)
        {
            t += Time.unscaledDeltaTime;
            audioSource.volume = Mathf.Lerp(initialVolume, 0f, t / endFadeOutSec);
            yield return null;
        }

        audioSource.Stop();
        audioSource.volume = initialVolume;

        ResultStore.Summary = counter.CreateSummary(chart.Notes.Count);
        ResultStore.HasSummary = true;
        SceneManager.LoadScene(nameof(ResultScene));
    }

    SongMeta GetFallbackSong()
    {
        var songs = SongCatalog.BuildCatalog();
        if (songs.Count == 0) return null;
        return songs[Mathf.Clamp(fallbackSongIndex, 0, songs.Count - 1)];
    }

    static string GetRelativeStreamingAssetsPath(string fullPath)
    {
        var root = Application.streamingAssetsPath;
        if (fullPath.StartsWith(root))
            return fullPath[root.Length..].TrimStart('/', '\\');
        return fullPath;
    }
}
