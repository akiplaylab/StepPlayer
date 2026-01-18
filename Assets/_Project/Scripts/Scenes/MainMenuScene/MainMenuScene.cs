using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuScene : MonoBehaviour
{
    // ボタンなどから呼び出す用
    public void GoToSongSelect()
    {
        SceneManager.LoadScene("SongSelectScene");
    }
}
