using UnityEngine;
using UnityEngine.SceneManagement;
using Photon.Pun;

public class SceneLoader : MonoBehaviour
{
    // This function will be called when the button is clicked
    public void LoadScene(string sceneName)
    {
        // Special handling for FindRoom scene
        if (sceneName == "FindRoom")
        {
            LoadFindRoomScene();
        }
        else
        {
            // Normal scene loading for other scenes
            SceneManager.LoadScene(sceneName);
        }
    }

    // Smart navigation to FindRoom - maintains Photon connection
    void LoadFindRoomScene()
    {
        // Check if already connected to Photon
        if (PhotonNetwork.IsConnected)
        {
            Debug.Log("[SceneLoader] Already connected to Photon - navigating to FindRoom");
            // Already connected, just load the scene (keeps connection alive)
            SceneManager.LoadScene("FindRoom");
        }
        else
        {
            Debug.Log("[SceneLoader] Not connected yet - will connect in FindRoom");
            // Not connected yet, load scene normally (FindRoomManager will handle connection)
            SceneManager.LoadScene("FindRoom");
        }
    }
}