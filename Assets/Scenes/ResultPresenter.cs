using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;

public sealed class ResultPresenter : MonoBehaviour
{
    [SerializeField] GameObject root;
    [SerializeField] TMP_Text headline;
    [SerializeField] TMP_Text judgementList;
    [SerializeField] string headlineText = "RESULT";

    void Awake()
    {
        if (root != null)
            root.SetActive(false);
    }

    public void Hide()
    {
        if (root != null)
            root.SetActive(false);
    }

    public void Show(JudgementSummary summary)
    {
        if (root != null)
            root.SetActive(true);

        if (headline != null)
            headline.text = headlineText;

        if (judgementList != null)
            judgementList.text = BuildLines(summary);
    }

    string BuildLines(JudgementSummary summary)
    {
        var order = new List<(string label, int count)>
        {
            ("Marvelous", summary.GetCount(Judgement.Marvelous)),
            ("Perfect", summary.GetCount(Judgement.Perfect)),
            ("Great", summary.GetCount(Judgement.Great)),
            ("Good", summary.GetCount(Judgement.Good)),
            ("Bad", summary.GetCount(Judgement.Bad)),
            ("Miss", summary.MissCount),
        };

        var sb = new StringBuilder();
        for (int i = 0; i < order.Count; i++)
        {
            var (label, count) = order[i];
            sb.Append(label).Append(": ").Append(count);
            if (i < order.Count - 1)
                sb.Append('\n');
        }

        return sb.ToString();
    }
}
