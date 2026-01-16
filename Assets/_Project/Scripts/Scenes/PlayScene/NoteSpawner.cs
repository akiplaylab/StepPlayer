using System.Collections.Generic;
using UnityEngine;

public sealed class NoteSpawner
{
    readonly Chart chart;
    readonly NoteViewPool notePool;
    readonly Transform spawnY;
    readonly Transform judgeLineY;
    readonly float travelTimeSec;
    readonly float[] laneXs;
    readonly Dictionary<Lane, LinkedList<NoteView>> active;

    int nextSpawnIndex;

    public NoteSpawner(
        Chart chart,
        NoteViewPool notePool,
        Transform spawnY,
        Transform judgeLineY,
        float travelTimeSec,
        float[] laneXs,
        Dictionary<Lane, LinkedList<NoteView>> active)
    {
        this.chart = chart;
        this.notePool = notePool;
        this.spawnY = spawnY;
        this.judgeLineY = judgeLineY;
        this.travelTimeSec = travelTimeSec;
        this.laneXs = laneXs;
        this.active = active;
    }

    public bool AllSpawned => chart != null && nextSpawnIndex >= chart.Notes.Count;

    public void SpawnNotes(double songTime)
    {
        if (chart == null) return;

        while (nextSpawnIndex < chart.Notes.Count)
        {
            var note = chart.Notes[nextSpawnIndex];
            var noteTimeSec = chart.BeatToSeconds(note.Beat);
            var travelTime = GetTravelTimeSec(note.Beat);
            var spawnTime = noteTimeSec - travelTime;
            if (songTime < spawnTime) break;

            var view = notePool.Rent();
            view.Init(note, noteTimeSec);
            view.transform.position = new Vector3(GetLaneX(note.Lane), spawnY.position.y, 0);

            active[note.Lane].AddLast(view);
            nextSpawnIndex++;
        }
    }

    public void UpdateNotePositions(double songTime)
    {
        foreach (var lane in active.Keys)
        {
            foreach (var n in active[lane])
            {
                var travelTime = GetTravelTimeSec(n.Beat);
                var t = (float)((n.TimeSec - songTime) / travelTime);
                var y = Mathf.LerpUnclamped(judgeLineY.position.y, spawnY.position.y, t);
                var x = GetLaneX(lane);
                n.transform.position = new Vector3(x, y, 0);
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

    float GetTravelTimeSec(double beat)
    {
        if (chart == null) return travelTimeSec;
        var bpm = chart.GetBpmAtBeat(beat);
        if (bpm <= 0) return travelTimeSec;
        var scale = chart.Bpm / bpm;
        return travelTimeSec * (float)scale;
    }
}
