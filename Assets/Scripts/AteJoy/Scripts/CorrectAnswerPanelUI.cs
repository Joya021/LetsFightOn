using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CorrectAnswerPanelUI : MonoBehaviour
{
    [Header("UI References")]
    public GameObject correctAnswerPopupPanel;

    public TMP_Text issueDescription;
    public TMP_Text yourAnswer;
    public TMP_Text pointsToRemember;

    public Button closeButton;

    [Header("Gameplay References")]
    public PlayerMovements playerMovements;

    private void Awake()
    {
        if (closeButton != null)
            closeButton.onClick.AddListener(HidePopup);
    }

    public void HidePopup()
    {
        if (correctAnswerPopupPanel != null)
            correctAnswerPopupPanel.SetActive(false);

        if (playerMovements != null)
            playerMovements.canMove = true;

        // ✅ Only show the panel *after* closing the last popup
        if (C1BrokenCodesManager.Instance != null)
            C1BrokenCodesManager.Instance.ShowChapterClearedPanelIfReady();
    }
}
