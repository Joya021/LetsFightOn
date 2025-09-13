using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class LobbyManager : MonoBehaviour
{
    [Header("Lobby Canvases")]
    public GameObject lobbyCanvas;
    public GameObject hunterSelectedCanvas;
    public GameObject survivorSelectedCanvas;

    [Header("Lobby Buttons")]
    public Button readyButton;

    [Header("Role Selection (Debug)")]
    public bool forceRoleSelection = true; // For testing - always survivor for now
    public bool forceSurvivor = true; // Set to true to always get survivor

    void Start()
    {
        // Setup button listeners
        if (readyButton != null)
            readyButton.onClick.AddListener(OnReadyButtonPressed);

        // Initialize canvases
        if (lobbyCanvas != null)
            lobbyCanvas.SetActive(true);
        if (hunterSelectedCanvas != null)
            hunterSelectedCanvas.SetActive(false);
        if (survivorSelectedCanvas != null)
            survivorSelectedCanvas.SetActive(false);
    }

    public void OnReadyButtonPressed()
    {
        // Hide lobby canvas
        if (lobbyCanvas != null)
            lobbyCanvas.SetActive(false);

        // For now, always show survivor canvas as requested
        if (forceSurvivor)
        {
            ShowSurvivorSelected();
        }
        else
        {
            // Random role selection (for future use)
            bool isHunter = Random.Range(0, 2) == 0;

            if (isHunter)
            {
                ShowHunterSelected();
            }
            else
            {
                ShowSurvivorSelected();
            }
        }
    }

    private void ShowHunterSelected()
    {
        if (hunterSelectedCanvas != null)
            hunterSelectedCanvas.SetActive(true);
    }

    private void ShowSurvivorSelected()
    {
        if (survivorSelectedCanvas != null)
            survivorSelectedCanvas.SetActive(true);
    }
}