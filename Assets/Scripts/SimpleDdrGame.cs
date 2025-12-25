using System.Collections.Generic;
using System.Linq;
using UnityEngine;

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
    [SerializeField] Transform laneLeftX;
    [SerializeField] Transform laneDownX;
    [SerializeField] Transform laneUpX;
    [SerializeField] Transform laneRightX;
    [SerializeField] float travelTimeSec = 1.5f; // 出現→判定ラインまで何秒

    [Header("Judgement Windows (sec)")]
    [SerializeField] float perfect = 0.03f;
    [SerializeField] float great = 0.06f;
    [SerializeField] float good = 0.10f;
    [SerializeField] float miss = 0.20f;

    Chart chart;
    double dspStartTime;
    int nextSpawnIndex;

    // レーンごとに「まだ叩かれてないノーツ」を保持（先頭が次に叩くべき）
    readonly Dictionary<Lane, LinkedList<NoteView>> active = new()
    {
        [Lane.Left] = new(),
        [Lane.Down] = new(),
        [Lane.Up] = new(),
        [Lane.Right] = new(),
    };

    void Start()
    {
        chart = Chart.LoadFromStreamingAssets(chartFileName);

        // musicClip は Inspector で入れてもいいし、
        // StreamingAssetsからロードしたいなら別途ロード処理が必要（まずはInspector推奨）
        audioSource.clip = musicClip;

        // DSP基準でスタート時刻を固定
        dspStartTime = AudioSettings.dspTime + 0.2; // 少し先に予約
        audioSource.PlayScheduled(dspStartTime);

        nextSpawnIndex = 0;
    }

    void Update()
    {
        var songTime = GetSongTimeSec();

        SpawnNotes(songTime);
        UpdateNotePositions(songTime);
        HandleInput(songTime);
        CleanupMissed(songTime);
    }

    double GetSongTimeSec()
        => (AudioSettings.dspTime - dspStartTime) + chart.OffsetSec;

    void SpawnNotes(double songTime)
    {
        // 出現タイミング = noteTime - travelTime
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
                // t=1 ならスポーン位置、t=0 なら判定ライン
                var y = Mathf.Lerp(judgeLineY.position.y, spawnY.position.y, Mathf.Clamp01(t));
                var x = GetLaneX(lane);
                n.transform.position = new Vector3(x, y, 0);
            }
        }
    }

    void HandleInput(double songTime)
    {
        TryHit(Lane.Left, Input.GetKeyDown(KeyCode.LeftArrow), songTime);
        TryHit(Lane.Down, Input.GetKeyDown(KeyCode.DownArrow), songTime);
        TryHit(Lane.Up, Input.GetKeyDown(KeyCode.UpArrow), songTime);
        TryHit(Lane.Right, Input.GetKeyDown(KeyCode.RightArrow), songTime);
    }

    void TryHit(Lane lane, bool pressed, double songTime)
    {
        if (!pressed) return;

        var list = active[lane];
        if (list.First == null)
        {
            Debug.Log($"{lane}: 空振り");
            return;
        }

        // 最小版：先頭ノーツのみを対象（DDRの基本挙動）
        var note = list.First.Value;
        var dt = System.Math.Abs(note.timeSec - songTime);

        var result =
            dt <= perfect ? "Perfect" :
            dt <= great ? "Great" :
            dt <= good ? "Good" :
            dt <= miss ? "Bad" : "TooEarly/TooLate";

        Debug.Log($"{lane}: {result} (dt={dt:0.000})");

        // 当たった扱いにする範囲
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
                // ノーツ時刻 + miss を過ぎたらミス確定で消す
                if (songTime <= n.timeSec + miss) break;

                Debug.Log($"{lane}: Miss (late)");
                list.RemoveFirst();
                Destroy(n.gameObject);
            }
        }
    }

    float GetLaneX(Lane lane) => lane switch
    {
        Lane.Left => laneLeftX.position.x,
        Lane.Down => laneDownX.position.x,
        Lane.Up => laneUpX.position.x,
        Lane.Right => laneRightX.position.x,
        _ => 0
    };
}
