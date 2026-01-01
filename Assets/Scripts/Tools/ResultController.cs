using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class ResultController : MonoBehaviour
{
    [Header("Rows")]
    [SerializeField] ResultJudgementRowView rowMarvelous;
    [SerializeField] ResultJudgementRowView rowPerfect;
    [SerializeField] ResultJudgementRowView rowGreat;
    [SerializeField] ResultJudgementRowView rowGood;
    [SerializeField] ResultJudgementRowView rowBad;
    [SerializeField] ResultJudgementRowView rowMiss;

    void Start()
    {
        if (!ResultStore.HasSummary)
        {
            SetAllZero();
            return;
        }

        var s = ResultStore.Summary;

        rowMarvelous.Set(Judgement.Marvelous, s.GetCount(Judgement.Marvelous));
        rowPerfect.Set(Judgement.Perfect, s.GetCount(Judgement.Perfect));
        rowGreat.Set(Judgement.Great, s.GetCount(Judgement.Great));
        rowGood.Set(Judgement.Good, s.GetCount(Judgement.Good));
        rowBad.Set(Judgement.Bad, s.GetCount(Judgement.Bad));
        rowMiss.Set(Judgement.Miss, s.MissCount);

        ResultStore.Clear();
    }

    void SetAllZero()
    {
        rowMarvelous.Set(Judgement.Marvelous, 0);
        rowPerfect.Set(Judgement.Perfect, 0);
        rowGreat.Set(Judgement.Great, 0);
        rowGood.Set(Judgement.Good, 0);
        rowBad.Set(Judgement.Bad, 0);
        rowMiss.Set(Judgement.Miss, 0);
    }

    public void Retry()
    {
        ResultStore.Clear();
        SceneManager.LoadScene("GameScene");
    }
}
