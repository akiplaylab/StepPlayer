using TMPro;
using UnityEngine;

[RequireComponent(typeof(TMP_Text))]
public sealed class ComboTextPresenter : MonoBehaviour
{
    [SerializeField] string suffix = " COMBO";

    TMP_Text text;

    void Awake()
    {
        text = GetComponent<TMP_Text>();
        Hide();
    }

    public void Show(int combo)
    {
        if (combo <= 0)
        {
            Hide();
            return;
        }

        text.text = $"{combo}{suffix}";
        text.alpha = 1f;
    }

    void Hide()
    {
        text.text = "";
        text.alpha = 0f;
    }
}
