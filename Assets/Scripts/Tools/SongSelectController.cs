using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class SongSelectController : MonoBehaviour
{
    [SerializeField] SongLibrary library;

    [Header("UI")]
    [SerializeField] Transform listRoot;
    [SerializeField] SongRowView rowPrefab;

    void Start()
    {
        if (library == null) throw new InvalidOperationException("SongLibrary is not assigned.");
        if (listRoot == null) throw new InvalidOperationException("listRoot is not assigned.");
        if (rowPrefab == null) throw new InvalidOperationException("rowPrefab is not assigned.");
        if (library.Count == 0) throw new InvalidOperationException("SongLibrary has no songs.");

        BuildList();
    }

    void BuildList()
    {
        for (int i = listRoot.childCount - 1; i >= 0; i--)
            Destroy(listRoot.GetChild(i).gameObject);

        var songs = library.Songs;
        for (int i = 0; i < songs.Count; i++)
        {
            var row = Instantiate(rowPrefab, listRoot);
            row.Bind(this, i, songs[i]);
        }
    }

    public void SelectSong(int index)
    {
        var song = library.Get(index) ?? throw new InvalidOperationException("Invalid song index.");

        SelectedSong.Value = song;
        SceneManager.LoadScene("GameScene");
    }
}
