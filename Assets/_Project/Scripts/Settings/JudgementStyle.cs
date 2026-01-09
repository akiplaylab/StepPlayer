using UnityEngine;

[CreateAssetMenu(menuName = "DDR/Settings/Judgement Style", fileName = "JudgementStyle")]
public sealed class JudgementStyle : ScriptableObject
{
    [Header("Colors")]
    public Color marvelous = ColorUtil.HexToColor("#E9ECE9");
    public Color perfect = ColorUtil.HexToColor("#D5E01C");
    public Color great = ColorUtil.HexToColor("#11D914");
    public Color good = ColorUtil.HexToColor("#0F95BE");
    public Color bad = ColorUtil.HexToColor("#B911B8");
    public Color miss = ColorUtil.HexToColor("#BE120C");
    public Color maxCombo = ColorUtil.HexToColor("#BEA20C");

    public Color GetColor(Judgement judgement) => judgement switch
    {
        Judgement.Marvelous => marvelous,
        Judgement.Perfect => perfect,
        Judgement.Great => great,
        Judgement.Good => good,
        Judgement.Bad => bad,
        Judgement.Miss => miss,
        _ => Color.white
    };

    public Color GetMaxComboColor() => maxCombo;

    [Header("Result Count Digits")]
    public Color countActive = ColorUtil.HexToColor("#FFFFFF");
    public Color countInactive = ColorUtil.HexToColor("#5A5A5A");

    public Color GetCountActiveColor() => countActive;
    public Color GetCountInactiveColor() => countInactive;
}
