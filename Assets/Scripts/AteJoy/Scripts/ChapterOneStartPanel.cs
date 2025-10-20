using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChapterOneStartPanel : MonoBehaviour
{
    [SerializeField] private GameObject instructionGuidePanel;
    [SerializeField] private TextDashAnimator textDashAnimator; // Reference to your TextDashAnimator
    public GameObject chapterOneTextPanel; // The panel with the TextDashAnimator

    void Start()
    {
        textDashAnimator = chapterOneTextPanel.GetComponent<TextDashAnimator>();
        StartCoroutine(StartSequence());
    }

    private System.Collections.IEnumerator StartSequence()
    {
        // Dash in the text panel
        yield return StartCoroutine(textDashAnimator.DashIn());

        // Wait for 3 seconds
        yield return new WaitForSeconds(3f);

        // Dash out the text panel
        yield return StartCoroutine(textDashAnimator.DashOut());

        // Activate instruction guide after dash out completes
        instructionGuidePanel.SetActive(true);
    }

    public void OnIntroFinished()
    {
        // Hide this panel
        gameObject.SetActive(false);

        // Show instruction guide panel
        if (instructionGuidePanel != null)
            instructionGuidePanel.SetActive(true);
    }
}