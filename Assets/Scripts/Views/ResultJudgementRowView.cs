using System.Text;
using TMPro;
using UnityEngine;

public sealed class ResultJudgementRowView : MonoBehaviour
{
    [SerializeField] TMP_Text labelText;
    [SerializeField] TMP_Text countText;
    [SerializeField] JudgementStyle style;

    public void Set(Judgement judgement, int count)
    {
        labelText.text = judgement.ToString().ToUpper();
        labelText.color = style.GetColor(judgement);

        countText.richText = true;
        countText.text = BuildCount(count, digits: 4, style.GetCountActiveColor(), style.GetCountInactiveColor());
        countText.color = Color.white;
    }

    static string BuildCount(int value, int digits, Color active, Color inactive)
    {
        if (value < 0) value = 0;

        string s = value.ToString("D" + digits);
        int firstNonZero = -1;

        for (int i = 0; i < s.Length; i++)
        {
            if (s[i] != '0')
            {
                firstNonZero = i;
                break;
            }
        }

        string a = ColorUtility.ToHtmlStringRGB(active);
        string g = ColorUtility.ToHtmlStringRGB(inactive);

        var sb = new StringBuilder(s.Length + 40);

        for (int i = 0; i < s.Length; i++)
        {
            bool inactiveDigit = (firstNonZero == -1) || (i < firstNonZero);
            sb.Append(inactiveDigit ? $"<color=#{g}>" : $"<color=#{a}>");
            sb.Append(s[i]);
            sb.Append("</color>");
        }

        return sb.ToString();
    }
}
