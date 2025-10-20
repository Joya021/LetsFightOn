using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InstructionGuideController : MonoBehaviour
{
    [SerializeField] private GameObject[] guides;
    [SerializeField] private Button nextButton;
    [SerializeField] private Button letsFightOnButton;
    [SerializeField] private float autoCloseDelay = 3f;
    [SerializeField] private GameObject chapterOneStartPanel;

    private int currentIndex = 0;

    private void Start()
    {
        ShowGuide(0);

        if (nextButton != null)
        {
            nextButton.onClick.AddListener(NextGuide);
        }

        if (letsFightOnButton != null)
            letsFightOnButton.onClick.AddListener(OnLetsFightOnClicked);
    }

    private void ShowGuide(int index)
    {
        for (int i = 0; i < guides.Length; i++)
            guides[i].SetActive(i == index);

        // Show or hide the Next button depending on if it's the last guide
        if (nextButton != null)
            nextButton.gameObject.SetActive(index < guides.Length - 1);
    }

    private void NextGuide()
    {
        currentIndex++;

        if (currentIndex >= guides.Length)
        {
            // Last guide finished, auto-close
            StartCoroutine(AutoCloseSequence());
        }
        else
        {
            ShowGuide(currentIndex);
        }
    }

    private IEnumerator AutoCloseSequence()
    {
        yield return new WaitForSeconds(autoCloseDelay);
        Debug.Log("Last guide done - closing panel automatically");
        gameObject.SetActive(false);

        if (chapterOneStartPanel != null)
            chapterOneStartPanel.SetActive(false);
    }

    private void OnLetsFightOnClicked()
    {
        Debug.Log("Let's Fight On clicked - closing guide and start panel.");

        // Close the FindCompStationsGuide (this script is on InstructionGuide)
        foreach (var guide in guides)
        {
            if (guide != null && guide.name == "FindCompStationsGuide")
                guide.SetActive(false);
        }

        // Close ChapterOneStartPanel
        if (chapterOneStartPanel != null)
            chapterOneStartPanel.SetActive(false);
    }
}
