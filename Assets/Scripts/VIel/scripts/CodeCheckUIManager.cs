using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Manages the shared CodeCheck UI overlay panel
/// Place this script on a GameObject in the scene hierarchy (NOT on a prefab)
/// </summary>
public class CodeCheckUIManager : MonoBehaviour
{
    public static CodeCheckUIManager Instance { get; private set; }

    [Header("Shared UI References - Assign in Inspector")]
    public GameObject codeCheckOverlayPanel;
    public InputField codeCheckInputField;
    public Text codeCheckTaskText;
    public Button codeCheckSubmitButton;
    public GameObject[] correctAnswerImages;
    public GameObject wrongAnswerImage;
    public GameObject interactCooldownCanvas;
    public Text interactCooldownText;

    [Header("Debug Info")]
    public bool showDebugLogs = true;

    private void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Optional: Keep across scenes

            if (showDebugLogs)
                Debug.Log("[CodeCheckUIManager] Singleton instance created");
        }
        else
        {
            if (showDebugLogs)
                Debug.LogWarning("[CodeCheckUIManager] Duplicate instance destroyed");
            Destroy(gameObject);
            return;
        }

        // Validate all references are assigned
        ValidateReferences();
    }

    private void ValidateReferences()
    {
        bool allValid = true;

        if (codeCheckOverlayPanel == null)
        {
            Debug.LogError("[CodeCheckUIManager] codeCheckOverlayPanel is not assigned!");
            allValid = false;
        }

        if (codeCheckInputField == null)
        {
            Debug.LogError("[CodeCheckUIManager] codeCheckInputField is not assigned!");
            allValid = false;
        }

        if (codeCheckTaskText == null)
        {
            Debug.LogError("[CodeCheckUIManager] codeCheckTaskText is not assigned!");
            allValid = false;
        }

        if (codeCheckSubmitButton == null)
        {
            Debug.LogError("[CodeCheckUIManager] codeCheckSubmitButton is not assigned!");
            allValid = false;
        }

        if (correctAnswerImages == null || correctAnswerImages.Length == 0)
        {
            Debug.LogWarning("[CodeCheckUIManager] correctAnswerImages array is empty!");
        }

        if (wrongAnswerImage == null)
        {
            Debug.LogWarning("[CodeCheckUIManager] wrongAnswerImage is not assigned!");
        }

        if (allValid && showDebugLogs)
        {
            Debug.Log("[CodeCheckUIManager] All UI references validated successfully");
        }
    }

    /// <summary>
    /// Check if all required references are properly assigned
    /// </summary>
    public bool IsReady()
    {
        return codeCheckOverlayPanel != null &&
               codeCheckInputField != null &&
               codeCheckTaskText != null &&
               codeCheckSubmitButton != null;
    }

    /// <summary>
    /// Get the overlay panel (for backward compatibility)
    /// </summary>
    public GameObject GetOverlayPanel()
    {
        return codeCheckOverlayPanel;
    }

    /// <summary>
    /// Show the overlay panel with specified task
    /// </summary>
    public void ShowOverlay(string taskText)
    {
        if (!IsReady())
        {
            Debug.LogError("[CodeCheckUIManager] Cannot show overlay - references not ready!");
            return;
        }

        codeCheckOverlayPanel.SetActive(true);
        codeCheckTaskText.text = taskText;
        codeCheckInputField.text = "";
        codeCheckInputField.Select();
        codeCheckInputField.ActivateInputField();

        if (showDebugLogs)
            Debug.Log($"[CodeCheckUIManager] Overlay shown with task: {taskText}");
    }

    /// <summary>
    /// Hide the overlay panel
    /// </summary>
    public void HideOverlay()
    {
        if (codeCheckOverlayPanel != null)
        {
            codeCheckOverlayPanel.SetActive(false);

            if (showDebugLogs)
                Debug.Log("[CodeCheckUIManager] Overlay hidden");
        }
    }

    /// <summary>
    /// Reset all UI elements to default state
    /// </summary>
    public void ResetUI()
    {
        if (codeCheckInputField != null)
            codeCheckInputField.text = "";

        if (codeCheckTaskText != null)
            codeCheckTaskText.text = "";

        // Hide all feedback images
        if (correctAnswerImages != null)
        {
            foreach (var img in correctAnswerImages)
            {
                if (img != null) img.SetActive(false);
            }
        }

        if (wrongAnswerImage != null)
            wrongAnswerImage.SetActive(false);

        if (interactCooldownCanvas != null)
            interactCooldownCanvas.SetActive(false);

        if (showDebugLogs)
            Debug.Log("[CodeCheckUIManager] UI reset to default state");
    }
}