using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class ResultController : MonoBehaviour
{
    [SerializeField] TMP_Text listText;

    void Start()
    {
        if (!ResultStore.HasSummary)
        {
            listText.text = "No result";
            return;
        }

        var s = ResultStore.Summary;

        listText.text =
            $"Marvelous  {s.GetCount(Judgement.Marvelous)}\n" +
            $"Perfect    {s.GetCount(Judgement.Perfect)}\n" +
            $"Great      {s.GetCount(Judgement.Great)}\n" +
            $"Good       {s.GetCount(Judgement.Good)}\n" +
            $"Bad        {s.GetCount(Judgement.Bad)}\n" +
            $"Miss       {s.MissCount}\n";

        ResultStore.Clear();
    }

    public void Retry()
    {
        ResultStore.Clear();

        SceneManager.LoadScene("GameScene");
    }
}
