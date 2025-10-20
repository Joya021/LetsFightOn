using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Utils;

public class BrokenCodePopupUI : MonoBehaviour
{
    public static BrokenCodePopupUI Instance;
    public bool IsPopupOpen => popupPanel.activeSelf;

    [Header("UI References")]
    public GameObject popupPanel;
    public GameObject correctAnswerPanel;
    public GameObject wrongAnswerPanel;
    public Image brokenCodeChallengeImage;
    public Image hintImage1;
    public Image hintImage2;
    public Image hintImage3;
    public TMP_InputField answerInput;

    public TMP_Text lockedOverlay_TMP;

    public Button runButton;
    public Button closeButton;
    public TMP_Text feedbackText;

    [SerializeField] private List<Sprite> encouragementSprites;
    [SerializeField] private GameObject targetWrongAnswerPanel;
    [SerializeField] private Image encouragementGFX;

    [SerializeField] private TMP_Text playerAnswer_TMP;
    [SerializeField] private TMP_Text issueDescription_TMP;
    [SerializeField] private TMP_Text pointsToRemember_TMP;

    private BrokenCode currentCode;
    private Image[] hintSlots;
    public PlayerMovements playerMovements;
    private ComputerStation currentStation;

    private List<(int start, int end, string content)> lockedRanges = new();
    private List<string> editableSnippets = new();
    private List<string> finalEditableSnippets = new();

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        hintSlots = new[] { hintImage1, hintImage2, hintImage3 };

        answerInput.onValueChanged.AddListener(OnInputChanged);

        popupPanel.SetActive(false);
        closeButton.onClick.AddListener(HidePopup);
        runButton.onClick.AddListener(OnRunClicked);
    }

    public void ShowPopup(BrokenCode code, ComputerStation station)
    {
        // ── Freeze player movement ────────────────────────────────

        if (playerMovements == null)
            playerMovements = FindObjectOfType<PlayerMovements>();

        if (playerMovements != null)
            playerMovements.canMove = false;

        currentCode = code;
        brokenCodeChallengeImage.sprite = code.codeSprite;

        // ── Populate hints ────────────────────────────────────────

        for (int i = 0; i < hintSlots.Length; i++)
        {
            if (i < code.hintImages.Count)
            {
                hintSlots[i].sprite = code.hintImages[i];
                hintSlots[i].gameObject.SetActive(true);
            }
            else
            {
                hintSlots[i].gameObject.SetActive(false);
            }
        };

        // ── Strip lock tags and show cleaned code ─────────────────
        // Capturing the raw default code template from the scriptable object BrokenCode
        string rawCode = code.defaultCodeTemplate;
        Debug.Log("Raw code from ScriptableObject:\n" + code.defaultCodeTemplate);

        // Line that cleaned the raw default code template. Removed/stripped the lock and edit markers
        string omitLockTag = LockTagStripper.StripLockTags(code.defaultCodeTemplate);
        string cleanedCode = EditTagStripper.RemoveEditTags(omitLockTag);
        Debug.Log("Cleaned code:\n" + cleanedCode);

        // The line that shows the cleaned default code template to the input field.
        answerInput.text = cleanedCode;

        // Track locked ranges
        lockedRanges = LockRangeParser.GetLockedRangesWithContent(currentCode.defaultCodeTemplate);

        editableSnippets = LockTagStripper.ExtractEditableSnippets(EditTagStripper.RemoveEditTags(currentCode.defaultCodeTemplate));
        Debug.Log("Extracted editable snippets: " + string.Join(" | ", editableSnippets));

        #region OVERLAY_TMP

        // ── Build overlay text with protected code only ─────────────────
        string overlayText = currentCode.defaultCodeTemplate;

        // Replace <LOCK>...</LOCK> with styled gray text
        overlayText = Regex.Replace(
            overlayText,
            @"<LOCK>(.*?)</LOCK>",
            match => $"<color=#AAAAAA>{match.Groups[1].Value}</color>"
        );

        // Strip lock tags to get full visible code
        string visibleCode = LockTagStripper.StripLockTags(currentCode.defaultCodeTemplate);

        // Replace editable segments with transparent text to preserve spacing
        int overlayIndex = 0;
        string finalOverlay = "";
        foreach (Match match in Regex.Matches(overlayText, @"<color=#AAAAAA>.*?</color>|[^<]+"))
        {
            string segment = match.Value;
            if (segment.StartsWith("<color="))
            {
                finalOverlay += segment; // keep locked segment
            }
            else
            {
                finalOverlay += $"<color=#00000000>{segment}</color>"; // hide editable segment
            }
        }

        lockedOverlay_TMP.text = finalOverlay;

        #endregion

        feedbackText.text = string.Empty;

        currentStation = station;
        popupPanel.SetActive(true);
        answerInput.Select();
        answerInput.ActivateInputField();
    }


    private void OnInputChanged(string newText)
    {
        List<string> protectedSnippets = LockTagStripper.ExtractLockedSnippets(currentCode.defaultCodeTemplate);

        foreach (string snippet in protectedSnippets)
        {
            if (!newText.Contains(snippet))
            {
                string omitLockTag = LockTagStripper.StripLockTags(currentCode.defaultCodeTemplate);
                string cleanedCode = EditTagStripper.RemoveEditTags(omitLockTag);

                answerInput.text = cleanedCode;
                feedbackText.text = "You can't modify protected code!";
                break;
            }
        }
    }


    private void OnRunClicked()
    {
        if (currentCode == null)
        {
            feedbackText.text = "No code loaded!";
            return;
        }

        string playerCode = answerInput.text;
        Debug.Log("Raw player input: " + answerInput.text);

        // Extract editable snippets from the original template
        // List<string> editableSnippets = LockTagStripper.ExtractEditableSnippets(currentCode.defaultCodeTemplate);
        // editableSnippets = LockTagStripper.ExtractPlayerEditableSnippets(currentCode.defaultCodeTemplate, playerCode);

        string editableInput = LockTagStripper.ExtractEditableInputFromPlayerCode(currentCode.defaultCodeTemplate, playerCode);
        Debug.Log("Final editable input for validation: " + editableInput);


        List<string> allErrors = new List<string>();

        foreach (var rule in currentCode.validationRules)
        {
            bool isValid = true;

            if (rule is FlexibleFunctionNameRule functionRule)
            {
                isValid = functionRule.Validate(editableInput);
                if (!isValid) allErrors.AddRange(functionRule.errorMessages);
            }
            else if (rule is FlexibleVariableNameRule variableRule)
            {
                isValid = variableRule.Validate(editableInput);
                if (!isValid) allErrors.AddRange(variableRule.errorMessages);
            }
            else if (rule is FlexibleClassNameRule classRule)
            {
                isValid = classRule.Validate(editableInput);
                if (!isValid) allErrors.AddRange(classRule.errorMessages);
            }
            else if (rule is FlexibleConstantNameRule constantRule)
            {
                isValid = constantRule.Validate(editableInput);
                if (!isValid) allErrors.AddRange(constantRule.errorMessages);
            }
            else if (rule is FlexibleMethodNameRule methodRule)
            {
                isValid = methodRule.Validate(editableInput);
                if (!isValid) allErrors.AddRange(methodRule.errorMessages);
            }
            else if (rule is ProtectedBlockRule protectedRule)
            {
                protectedRule.originalTemplate = currentCode.defaultCodeTemplate;
                protectedRule.protectedSnippets = LockTagStripper.ExtractLockedSnippets(currentCode.defaultCodeTemplate);

                isValid = protectedRule.Validate(playerCode);
                if (!isValid) allErrors.AddRange(protectedRule.errorMessages);
            }
            else if (rule is LockedBindingAndVariableCodeRule lockedRule)
            {
                // ✅ Pass the cleaned editable input (no <LOCK> tags)
                Debug.Log($"🧩 Running LockedBindingAndVariableCodeRule on: {editableInput}");

                isValid = lockedRule.Validate(editableInput);
                if (!isValid) allErrors.AddRange(lockedRule.errorMessages);
            }
            else if (rule is SpecificVariableYRule yRule)
            {
                yRule.rawTemplate = currentCode.defaultCodeTemplate; // 🔹 Pass the template automatically
                isValid = yRule.Validate(editableInput); // pass full player code (not editable only)
                if (!isValid) allErrors.AddRange(yRule.errorMessages);
            }
            else if (rule is SpecificVariableCodeRule specificVariableRule)
            {
                isValid = specificVariableRule.Validate(editableInput);
                if (!isValid) allErrors.AddRange(specificVariableRule.errorMessages);
            }
            else if (rule is SpecificVariableNameRule specificVariableNameRule)
            {
                isValid = specificVariableNameRule.Validate(editableInput);
                if (!isValid) allErrors.AddRange(specificVariableNameRule.errorMessages);
            }
            else
            {
                isValid = rule.Validate(editableInput);
                if (!isValid) allErrors.Add("Invalid input.");
            }
        }


        if (allErrors.Count > 0)
        {
            feedbackText.text = string.Join("\n", allErrors);
            ShowPanelTemporarily();
        }
        else
        {
            popupPanel.SetActive(false);
            feedbackText.text = "";

            // 🎉 Randomly select a congratulatory image
            if (encouragementSprites != null && encouragementSprites.Count > 0)
            {
                int randomIndex = Random.Range(0, encouragementSprites.Count);
                encouragementGFX.sprite = encouragementSprites[randomIndex];
            }

            // ✅ Show the player's correct answer
            if (playerAnswer_TMP != null)
                playerAnswer_TMP.text = answerInput.text.Trim();

            // Show the Issue Description
            if (issueDescription_TMP != null) issueDescription_TMP.text = currentCode.issueDescription;

            // Show the Points to Remeber
            if (pointsToRemember_TMP  != null) pointsToRemember_TMP.text = currentCode.pointsToRemember;

            correctAnswerPanel.SetActive(true);
            currentStation.MarkSolved();
        }
    }

    public void HidePopup()
    {
        popupPanel.SetActive(false);
        if (playerMovements != null)
            playerMovements.canMove = true;
    }


    public void ShowPanelTemporarily()
    {
        StartCoroutine(ShowPanelForSeconds(0.5f)); // shows for 0.5 second
    }

    private IEnumerator ShowPanelForSeconds(float duration)
    {
        targetWrongAnswerPanel.SetActive(true);
        yield return new WaitForSeconds(duration);
        targetWrongAnswerPanel.SetActive(false);
        yield return new WaitForSeconds(duration);
        targetWrongAnswerPanel.SetActive(true);
        yield return new WaitForSeconds(duration);
        targetWrongAnswerPanel.SetActive(false);
        yield return new WaitForSeconds(duration);
        targetWrongAnswerPanel.SetActive(true);
        yield return new WaitForSeconds(duration);
        targetWrongAnswerPanel.SetActive(false);
        yield return new WaitForSeconds(duration);
    }

}
