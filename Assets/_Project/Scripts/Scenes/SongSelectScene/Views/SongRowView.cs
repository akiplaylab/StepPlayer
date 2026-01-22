using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class SongRowView : MonoBehaviour
{
    [SerializeField] Button button;
    [SerializeField] TMP_Text titleText;
    [SerializeField] TMP_Text sourceText;
    [SerializeField] Image background;

    [Header("Colors")]
    [SerializeField] Color normalColor = Color.white;
    [SerializeField] Color selectedColor = new(0.7f, 0.85f, 1f);
    [SerializeField] Color confirmColor = new(1f, 0.95f, 0.6f);

    [Header("Confirm Visual")]
    [SerializeField] float confirmScale = 1.08f;
    [SerializeField] float confirmDuration = 0.18f;

    int index;
    SongSelectScene owner;
    bool isSelected;
    Vector3 baseScale;
    Coroutine confirmRoutine;

    public void Bind(SongSelectScene owner, int index, SongMeta song)
    {
        this.owner = owner;
        this.index = index;

        titleText.text = song.DisplayTitle;
        sourceText.text = string.IsNullOrWhiteSpace(song.Artist) ? string.Empty : song.Artist;

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => owner.OnRowClicked(index));

        if (baseScale == Vector3.zero)
            baseScale = transform.localScale;
    }

    public void SetSelected(bool selected)
    {
        isSelected = selected;

        if (confirmRoutine == null)
            background.color = GetBaseColor();
    }

    public void PlayConfirmVisual()
    {
        if (confirmRoutine != null)
            StopCoroutine(confirmRoutine);

        confirmRoutine = StartCoroutine(ConfirmVisualCoroutine());
    }

    IEnumerator ConfirmVisualCoroutine()
    {
        var baseColor = GetBaseColor();
        float elapsed = 0f;

        while (elapsed < confirmDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / confirmDuration);
            float pulse = Mathf.Sin(t * Mathf.PI);

            transform.localScale = Vector3.Lerp(baseScale, baseScale * confirmScale, pulse);
            background.color = Color.Lerp(baseColor, confirmColor, pulse);

            yield return null;
        }

        transform.localScale = baseScale;
        background.color = baseColor;
        confirmRoutine = null;
    }

    Color GetBaseColor()
    {
        return isSelected ? selectedColor : normalColor;
    }
}
