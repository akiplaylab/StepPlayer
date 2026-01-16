using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
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
    [SerializeField] float beatsShown = 4.0f;

    [Header("Lane X positions")]
    readonly float[] laneXs = { -2.6f, -0.85f, 0.85f, 2.6f };

    [Header("Receptor Effects")]
    [SerializeField] ReceptorHitEffect leftFx, downFx, upFx, rightFx;

    [Header("Judgement")]
    [SerializeField] Judge judge;
    [SerializeField] ComboTextPresenter comboText;
    [SerializeField] JudgementStyle judgementStyle;

    [SerializeField] float endFadeOutSec = 0.4f;
    [SerializeField] bool endWhenChartFinished = true;
    [SerializeField] float endWhenChartFinishedDelaySec = 0.8f;

    Chart chart;
    NoteViewPool notePool;
    double dspStartTime, outputLatencySec;
    int nextSpawnIndex;
    bool isEnding;
    float initialVolume;
    double chartFinishedAtSongTime = double.NaN;
    SongMeta currentSong;
    
    // BPM管理用
    float currentTravelTimeSec;
    int bpmChangeIndex = 0;
    double nextBpmChangeTime = double.MaxValue;

    readonly JudgementCounter counter = new();
    readonly Dictionary<Lane, LinkedList<NoteView>> active = new() {
        [Lane.Left] = new(), [Lane.Down] = new(), [Lane.Up] = new(), [Lane.Right] = new()
    };

    void Awake()
    {
        loader = GetComponent<StreamingAssetLoader>();
        if (songInfoPresenter == null) songInfoPresenter = gameObject.AddComponent<PlaySceneSongInfoPresenter>();
        notePool = new NoteViewPool(notePrefab, notesRoot, prewarm: 16);
    }

    IEnumerator Start()
    {
        ResultStore.Clear();
        counter.Reset();
        UpdateComboDisplay();

        var song = (SelectedSong.Value ?? GetFallbackSong()) ?? throw new InvalidOperationException("No song selected.");
        currentSong = song;
        songInfoPresenter?.SetSong(song, song.ChartDifficulty);

        if (song.MusicClip == null) yield return loader.LoadAudioClip(song, clip => song.MusicClip = clip);
        
        chart = ChartLoader.LoadFromStreamingAssets(GetRelativeStreamingAssetsPath(song.SmFilePath), song.ChartDifficulty);

        // BPM初期設定
        bpmChangeIndex = 0;
        ApplyBpm(chart.Bpm);
        if (chart.BpmChanges != null && chart.BpmChanges.Count > 0)
            nextBpmChangeTime = chart.BeatToSeconds(chart.BpmChanges[0].Beat);

        audioSource.clip = song.MusicClip;
        initialVolume = audioSource != null ? audioSource.volume : 1f;
        
        AudioSettings.GetDSPBufferSize(out var bufferLength, out var numBuffers);
        outputLatencySec = (double)bufferLength * numBuffers / AudioSettings.outputSampleRate;
        dspStartTime = AudioSettings.dspTime + 0.2;
        audioSource.PlayScheduled(dspStartTime);
    }

    void Update()
    {
        if (AudioSettings.dspTime < dspStartTime || isEnding) return;

        double songTime = GetSongTimeSec();

        // BPM変化のチェック
        if (songTime >= nextBpmChangeTime) UpdateCurrentBpm(songTime);

        SpawnNotes(songTime);
        UpdateNotePositions(songTime);
        HandleInput(songTime);
        CleanupMissed(songTime);

        // 終了判定
        if (endWhenChartFinished)
        {
            if (nextSpawnIndex >= chart.Notes.Count && active.Values.All(l => l.Count == 0))
            {
                if (double.IsNaN(chartFinishedAtSongTime)) chartFinishedAtSongTime = songTime;
                if (songTime - chartFinishedAtSongTime >= endWhenChartFinishedDelaySec) EndToResult();
            }
            else { chartFinishedAtSongTime = double.NaN; }
        }
    }

    void UpdateCurrentBpm(double songTime)
    {
        while (bpmChangeIndex < chart.BpmChanges.Count && songTime >= chart.BeatToSeconds(chart.BpmChanges[bpmChangeIndex].Beat))
        {
            ApplyBpm(chart.BpmChanges[bpmChangeIndex].Bpm);
            bpmChangeIndex++;
        }
        nextBpmChangeTime = (bpmChangeIndex < chart.BpmChanges.Count) 
            ? chart.BeatToSeconds(chart.BpmChanges[bpmChangeIndex].Beat) 
            : double.MaxValue;
    }

    void ApplyBpm(double bpm) => currentTravelTimeSec = (float)((60.0 / bpm) * beatsShown);

    double GetSongTimeSec() => (AudioSettings.dspTime - dspStartTime) - chart.OffsetSec - outputLatencySec;

    void SpawnNotes(double songTime)
    {
        while (nextSpawnIndex < chart.Notes.Count)
        {
            var note = chart.Notes[nextSpawnIndex];
            var noteTimeSec = chart.BeatToSeconds(note.Beat);
            if (songTime < noteTimeSec - currentTravelTimeSec) break;

            var view = notePool.Rent();
            view.Init(note, noteTimeSec);
            active[note.Lane].AddLast(view);
            nextSpawnIndex++;
        }
    }

    void UpdateNotePositions(double songTime)
    {
        float jY = judgeLineY.position.y;
        float sY = spawnY.position.y;
        foreach (var lane in active.Keys)
        {
            float x = GetLaneX(lane);
            foreach (var n in active[lane])
            {
                float t = (float)((n.TimeSec - songTime) / currentTravelTimeSec);
                n.transform.position = new Vector3(x, Mathf.LerpUnclamped(jY, sY, t), 0);
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

    float GetLaneX(Lane lane) => (uint)lane < laneXs.Length ? laneXs[(int)lane] : 0f;
    ReceptorHitEffect GetFx(Lane lane) => lane switch { Lane.Left => leftFx, Lane.Down => downFx, Lane.Up => upFx, Lane.Right => rightFx, _ => null };
    void UpdateComboDisplay() => comboText?.Show(counter.CurrentCombo);

    void PlayBurstAndReturn(NoteView note, Judgement judgement)
    {
        var style = judgementStyle ?? judge?.Style;
        if (style == null) { notePool.Return(note); return; }
        note.PlayHitBurst(style.GetColor(judgement), () => notePool.Return(note));
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
            float dur = Mathf.Max(0.01f, endFadeOutSec);
            for (float t = 0; t < dur; t += Time.unscaledDeltaTime)
            {
                audioSource.volume = Mathf.Lerp(from, 0f, t / dur);
                yield return null;
            }
            audioSource.Stop();
            audioSource.volume = initialVolume;
        }

        // リザルト画面へのデータ転送（復元）
        ResultStore.Summary = counter.CreateSummary(chart?.Notes.Count ?? 0);
        ResultStore.HasSummary = true;
        if (currentSong != null)
        {
            ResultStore.SongTitle = currentSong.DisplayTitle;
            ResultStore.MusicSource = currentSong.Artist ?? string.Empty;
            ResultStore.ChartDifficulty = currentSong.ChartDifficulty;
        }

        SceneManager.LoadScene(nameof(ResultScene));
    }

    SongMeta GetFallbackSong() {
        var songs = SongCatalog.BuildCatalog();
        return songs.Count > 0 ? songs[Mathf.Clamp(fallbackSongIndex, 0, songs.Count - 1)] : null;
    }

    static string GetRelativeStreamingAssetsPath(string fullPath) {
        var root = Application.streamingAssetsPath;
        return fullPath.StartsWith(root, StringComparison.OrdinalIgnoreCase) 
            ? fullPath[root.Length..].TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) 
            : fullPath;
    }
}
