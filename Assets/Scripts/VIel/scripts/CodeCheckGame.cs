using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Text;

[System.Serializable]
public class CodeTask
{
    public string instruction;
    public string correctAnswer;
    public string startingInput;
}

public class CodeCheckGame : MonoBehaviour
{
    [Header("References")]
    public GameManager gameManager;
    public HunterChaseAndHack hunter;
    private PlayerMovement playerMovement;
    private GameObject playerObject;

    [Header("UI Elements")]
    public InputField codeInputField;
    public Text taskText;
    public GameObject overlayPanel;
    public Button checkButton;
    public Text interactCooldownText;

    [Header("Target Object")]
    public GameObject triggeredObject;

    [Header("Interaction Settings")]
    public float interactionDistance = 2f;

    [Header("Cooldown Settings")]
    public float interactCooldown = 5f;
    [HideInInspector] public bool isOnCooldown = false;

    public bool IsBeingInteractedWith { get; private set; } = false;

    private TaskManager.CodeTask assignedTask;
    private bool isTriggered = false;
    private string savedInput = "";

    void Start()
    {
        if (checkButton != null)
            checkButton.onClick.AddListener(CheckAnswer);

        if (codeInputField != null)
            codeInputField.onValueChanged.AddListener(OnInputChanged);

        if (overlayPanel != null)
            overlayPanel.SetActive(false);

        playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
            playerMovement = playerObject.GetComponent<PlayerMovement>();
    }

    void Update()
    {
        IsBeingInteractedWith = overlayPanel != null && overlayPanel.activeSelf;

        // NEW: Check distance while the panel is active
        if (IsBeingInteractedWith && !IsPlayerInRange())
        {
            CloseCodePanel();
            return; // Exit the rest of the Update method to prevent other checks
        }

        if (IsBeingInteractedWith && Input.GetKeyDown(KeyCode.Escape))
        {
            CloseCodePanel();
        }

        bool playerStunned = (playerMovement != null && playerMovement.isStunned);

        if (checkButton != null)
            checkButton.interactable = !playerStunned;
    }

    private void OnInputChanged(string value)
    {
        savedInput = value;
    }

    private void AssignRandomTask()
    {
        assignedTask = TaskManager.Instance.GetUniqueRandomTask();
    }

    private bool IsPlayerInRange()
    {
        if (playerObject == null || triggeredObject == null) return false;

        float distance = Vector2.Distance(playerObject.transform.position, triggeredObject.transform.position);
        return distance <= interactionDistance;
    }

    public void OpenCodePanel()
    {
        if (isOnCooldown || !IsPlayerInRange()) return;

        if (assignedTask == null)
            AssignRandomTask();

        if (codeInputField != null)
            codeInputField.text = string.IsNullOrEmpty(savedInput) ? assignedTask.startingInput : savedInput;

        if (taskText != null)
            taskText.text = assignedTask != null ? $"Task:\n{assignedTask.instruction}" : "No tasks available.";

        if (overlayPanel != null)
        {
            overlayPanel.SetActive(true);
        }

        isTriggered = true;
        if (playerMovement != null) playerMovement.LockMovement();
    }

    public void CloseCodePanel()
    {
        if (overlayPanel != null)
        {
            overlayPanel.SetActive(false);
        }

        isTriggered = false;
        if (playerMovement != null) playerMovement.UnlockMovement();
    }

    public void CheckAnswer()
    {
        if (!isTriggered || assignedTask == null) return;

        if (!IsPlayerInRange())
        {
            CloseCodePanel();
            return;
        }

        string userInput = codeInputField.text.Trim();
        savedInput = userInput;

        if (userInput == assignedTask.correctAnswer)
        {
            SetObjectColor(Color.green);

            if (gameManager != null)
                gameManager.RegisterCorrectObject(this);

            if (hunter != null)
            {
                hunter.NotifyCorrectObjectSolved(this);
            }
        }
        else
        {
            SetObjectColor(Color.red);
        }

        CloseCodePanel();

        if (!string.IsNullOrEmpty(userInput) && !isOnCooldown)
            StartCoroutine(InteractCooldownRoutine());
    }

    private IEnumerator InteractCooldownRoutine()
    {
        isOnCooldown = true;
        float timer = interactCooldown;

        while (timer > 0)
        {
            if (interactCooldownText != null)
                interactCooldownText.text = $"Interact in: {timer:F1}s";
            timer -= Time.deltaTime;
            yield return null;
        }

        if (interactCooldownText != null)
            interactCooldownText.text = "";
        isOnCooldown = false;
    }

    public void SetObjectColor(Color color)
    {
        if (triggeredObject == null) return;
        Renderer rend = triggeredObject.GetComponent<Renderer>();
        if (rend != null)
            rend.material.color = color;
    }

    public void SetObjectColor(GameObject obj, Color color)
    {
        if (obj == null) return;
        Renderer rend = obj.GetComponent<Renderer>();
        if (rend != null)
            rend.material.color = color;
    }

    public void TamperCode()
    {
        Debug.Log("CodeCheckGame has been tampered with!");

        if (codeInputField != null)
        {
            string currentText = codeInputField.text;
            if (!string.IsNullOrEmpty(currentText))
            {
                System.Text.StringBuilder newText = new System.Text.StringBuilder(currentText);
                int changes = Random.Range(1, 3);
                for (int i = 0; i < changes; i++)
                {
                    int action = Random.Range(0, 3);
                    int index = Random.Range(0, newText.Length);

                    if (action == 0)
                        newText[index] = GetRandomChar();
                    else if (action == 1)
                        newText.Insert(index, GetRandomChar());
                    else if (action == 2 && newText.Length > 1)
                        newText.Remove(index, 1);
                }
                codeInputField.text = newText.ToString();
            }
        }

        SetObjectColor(Color.red);
    }

    private char GetRandomChar()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*";
        return chars[Random.Range(0, chars.Length)];
    }
}