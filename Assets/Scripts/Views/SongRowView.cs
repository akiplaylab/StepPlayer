using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class SongRowView : MonoBehaviour
{
    [SerializeField] Button button;
    [SerializeField] TMP_Text titleText;

    int index;
    SongSelectController owner;

    public void Bind(SongSelectController owner, int index, SongDefinition song)
    {
        this.owner = owner;
        this.index = index;

        titleText.text = song.songId;

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(OnClicked);
    }

    void OnClicked()
    {
        owner.SelectSong(index);
    }
}
