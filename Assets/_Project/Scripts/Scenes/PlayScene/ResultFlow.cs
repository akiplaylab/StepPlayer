using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class ResultFlow
{
    readonly bool endWhenChartFinished;
    readonly float endWhenChartFinishedDelaySec;

    double chartFinishedAtSongTime = double.NaN;

    public ResultFlow(bool endWhenChartFinished, float endWhenChartFinishedDelaySec)
    {
        this.endWhenChartFinished = endWhenChartFinished;
        this.endWhenChartFinishedDelaySec = endWhenChartFinishedDelaySec;
    }

    public bool ShouldEnd(double songTime, bool allSpawned, Dictionary<Lane, LinkedList<NoteView>> active, bool audioPlaying)
    {
        if (endWhenChartFinished)
        {
            bool noActiveNotes = true;
            foreach (var list in active.Values)
            {
                if (list.Count > 0)
                {
                    noActiveNotes = false;
                    break;
                }
            }

            if (allSpawned && noActiveNotes)
            {
                if (double.IsNaN(chartFinishedAtSongTime))
                    chartFinishedAtSongTime = songTime;

                if (songTime - chartFinishedAtSongTime >= endWhenChartFinishedDelaySec)
                    return true;

                return false;
            }

            chartFinishedAtSongTime = double.NaN;
        }

        if (allSpawned && !audioPlaying)
            return true;

        return false;
    }
}
