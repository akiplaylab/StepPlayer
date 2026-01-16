using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class InputJudge
{
    readonly Judge judge;
    readonly ComboTextPresenter comboText;
    readonly JudgementStyle judgementStyle;
    readonly JudgementCounter counter;
    readonly NoteViewPool notePool;
    readonly Dictionary<Lane, LinkedList<NoteView>> active;
    readonly Func<Lane, ReceptorHitEffect> getFx;

    public InputJudge(
        Judge judge,
        ComboTextPresenter comboText,
        JudgementStyle judgementStyle,
        JudgementCounter counter,
        NoteViewPool notePool,
        Dictionary<Lane, LinkedList<NoteView>> active,
        Func<Lane, ReceptorHitEffect> getFx)
    {
        this.judge = judge;
        this.comboText = comboText;
        this.judgementStyle = judgementStyle;
        this.counter = counter;
        this.notePool = notePool;
        this.active = active;
        this.getFx = getFx;
    }

    public void HandleInput(double songTime)
    {
        TryHit(Lane.Left, KeyBindings.LanePressedThisFrame(Lane.Left), songTime);
        TryHit(Lane.Down, KeyBindings.LanePressedThisFrame(Lane.Down), songTime);
        TryHit(Lane.Up, KeyBindings.LanePressedThisFrame(Lane.Up), songTime);
        TryHit(Lane.Right, KeyBindings.LanePressedThisFrame(Lane.Right), songTime);
    }

    public void ShowCombo()
    {
        comboText?.Show(counter.CurrentCombo);
    }

    public void CleanupMissed(double songTime)
    {
        foreach (var lane in active.Keys)
        {
            var list = active[lane];
            while (list.First != null)
            {
                var n = list.First.Value;
                if (songTime <= n.TimeSec + judge.MissWindow) break;

                counter.RecordMiss();
                Debug.Log($"{lane}: Miss (late)");
                list.RemoveFirst();
                PlayBurstAndReturn(n, Judgement.Miss);

                UpdateComboDisplay();
            }
        }
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

        var note = list.First.Value;
        var dt = Math.Abs(note.TimeSec - songTime);

        var judgement = judge.JudgeHit(lane, dt);
        getFx(lane).Play(judgement.Intensity);

        if (judgement.ShouldConsumeNote)
        {
            counter.Record(judgement.Judgement);
            list.RemoveFirst();
            PlayBurstAndReturn(note, judgement.Judgement);
            UpdateComboDisplay();
        }
    }

    void UpdateComboDisplay()
    {
        comboText?.Show(counter.CurrentCombo);
    }

    void PlayBurstAndReturn(NoteView note, Judgement judgement)
    {
        var style = judgementStyle != null ? judgementStyle : judge?.Style;
        if (style == null)
        {
            notePool.Return(note);
            return;
        }

        note.PlayHitBurst(style.GetColor(judgement), () => notePool.Return(note));
    }

}
