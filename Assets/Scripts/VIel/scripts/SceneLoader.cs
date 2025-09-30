using UnityEngine;
using UnityEngine.SceneManagement;  // Make sure to include this namespace

public class SceneLoader : MonoBehaviour
{
    // This function will be called when the button is clicked
    public void LoadScene(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }
}
