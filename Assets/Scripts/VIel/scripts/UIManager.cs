using System.Collections.Generic;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    // --- PANEL MANAGEMENT ---
    private Stack<GameObject> panelHistory = new Stack<GameObject>();
    private GameObject currentPanel;

    public void ShowPanel(GameObject newPanel)
    {
        if (newPanel == null) return;

        // If a panel is already active, remember it
        if (currentPanel != null)
        {
            panelHistory.Push(currentPanel);
        }

        // Show the new panel without closing previous
        newPanel.SetActive(true);
        currentPanel = newPanel;

        // Optional: Hide image when showing new panel
        HideCurrentImage();

        Debug.Log("ShowPanel: " + newPanel.name);
    }

    public void CloseCurrentPanel()
    {
        if (currentPanel != null)
        {
            currentPanel.SetActive(false);
            currentPanel = null;
        }
    }

    public void GoBack()
    {
        // Close current panel
        if (currentPanel != null)
        {
            currentPanel.SetActive(false);
        }

        // Return to previous panel
        if (panelHistory.Count > 0)
        {
            currentPanel = panelHistory.Pop();
            currentPanel.SetActive(true);
        }
        else
        {
            currentPanel = null;
        }
    }

    public void CloseParentPanelOfButton(GameObject buttonGO)
    {
        if (buttonGO == null) return;

        Transform panel = buttonGO.transform.parent;
        if (panel != null)
            panel.gameObject.SetActive(false);
    }

    // --- IMAGE MANAGEMENT ---
    private GameObject currentImage;

    public void ShowImage(GameObject newImage)
    {
        if (newImage == null) return;

        // Hide current image if different
        if (currentImage != null && currentImage != newImage)
        {
            currentImage.SetActive(false);
        }

        // Show new image
        newImage.SetActive(true);
        currentImage = newImage;

        // ⚠️ Do NOT close the panel underneath
        Debug.Log("ShowImage: " + newImage.name);
    }

    public void HideCurrentImage()
    {
        if (currentImage != null)
        {
            currentImage.SetActive(false);
            currentImage = null;
        }
    }
}
