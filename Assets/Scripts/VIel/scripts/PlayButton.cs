using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayButton : MonoBehaviour
{
    public string gameSceneName = "game";

    public void PlayGame()
    {
        Time.timeScale = 1f; // ensure the game runs normally
        SceneManager.LoadScene(gameSceneName);
    }
}
