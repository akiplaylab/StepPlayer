using UnityEngine;
using UnityEngine.SceneManagement;

public class TItleSceneScript : MonoBehaviour
{
    // ボタンなどから呼び出す用
    public void GoToMainMenu()
    {
        SceneManager.LoadScene("MainMenuScene");
    }
}
