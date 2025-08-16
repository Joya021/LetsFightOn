using UnityEngine;

public class ExitGameButton : MonoBehaviour
{
    // This method can be linked to a UI Button's OnClick event
    public void ExitGame()
    {
        Debug.Log("Exit Game called");
        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}
