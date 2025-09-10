using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LobbyTimer : MonoBehaviour
{
    public float countdownTime = 60f; // Time in seconds
    public string nextSceneName = "game"; // Replace with your actual scene name
    public Text countdownText; // Optional: assign a UI Text element

    private float timer;

    void Start()
    {
        timer = countdownTime;
    }

    void Update()
    {
        timer -= Time.deltaTime;

        // Optional: update UI text
        if (countdownText != null)
        {
            countdownText.text = Mathf.CeilToInt(timer).ToString() + "s";
        }

        if (timer <= 0)
        {
            LoadNextScene();
        }
    }

    void LoadNextScene()
    {
        SceneManager.LoadScene(nextSceneName);
    }
}
