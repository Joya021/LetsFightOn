using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ChapterOneClearedPanel : MonoBehaviour
{
    public GameObject chapterOneClearedPanel;

    public Button closeButton;

    public PlayerMovements playerMovements;

    private void Awake()
    {
        closeButton.onClick.AddListener(HidePopup);
    }

    public void HidePopup()
    {
        chapterOneClearedPanel.SetActive(false);
        if (playerMovements != null)
            playerMovements.canMove = true;
    }

    public void OnCloseButtonClicked()
    {
        SceneManager.LoadScene("LoginScene");
    }
}
