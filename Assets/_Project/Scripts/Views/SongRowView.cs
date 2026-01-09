using System;
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

    int index;
    SongSelectController owner;

    public void Bind(SongSelectController owner, int index, SongDefinition song)
    {
        this.owner = owner;
        this.index = index;

        titleText.text = string.IsNullOrWhiteSpace(song.songName) ? song.songId : song.songName;
        sourceText.text = song.musicSource.ToString();

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => owner.OnRowClicked(index));
    }

    public void SetSelected(bool selected)
    {
        background.color = selected ? selectedColor : normalColor;
    }
}
