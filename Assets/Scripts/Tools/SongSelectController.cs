using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class SongSelectController : MonoBehaviour
{
    [SerializeField] SongLibrary library;

    public void SelectSong(int index)
    {
        if (library == null)
            throw new System.InvalidOperationException("SongLibrary is not assigned.");

        var song = library.Get(index) ?? throw new System.InvalidOperationException("SongLibrary has no songs.");

        SelectedSong.Value = song;
        SceneManager.LoadScene("GameScene");
    }
}
