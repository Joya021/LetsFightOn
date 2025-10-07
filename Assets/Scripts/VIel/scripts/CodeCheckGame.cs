using UnityEngine;
using UnityEngine.UI;
using System.Collections;

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
    public InterCom linkedIntercom;

    [Header("UI Elements")]
    public InputField codeInputField;
    public Text taskText;
    public GameObject overlayPanel;
    public Button checkButton;

    [Header("Answer Feedback Images")]
    public GameObject[] correctAnswerImages;
    public GameObject wrongAnswerImage;
    public float answerImageDisplayTime = 2f;

    [Header("Cooldown UI")]
    public GameObject interactCooldownCanvas;
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
    private bool hasBeenSolved = false;
    private bool lastAnswerWasHunterTampered = false;
    private bool hasRegistered = false;

    void Start()
    {
        // AUTO-REGISTER WITH GAMEMANAGER
        RegisterWithGameManager();

        if (linkedIntercom == null)
        {
            linkedIntercom = GetComponent<InterCom>();
            if (linkedIntercom == null)
            {
                Debug.LogWarning("No InterCom script found on this GameObject! Please ensure it's on the same object or assigned manually.");
            }
        }

        if (checkButton != null)
            checkButton.onClick.AddListener(CheckAnswer);

        if (codeInputField != null)
            codeInputField.onValueChanged.AddListener(OnInputChanged);

        if (overlayPanel != null)
            overlayPanel.SetActive(false);

        if (interactCooldownCanvas != null)
            interactCooldownCanvas.SetActive(false);

        // Hide answer feedback images at start
        if (wrongAnswerImage != null)
            wrongAnswerImage.SetActive(false);

        foreach (GameObject img in correctAnswerImages)
        {
            if (img != null)
                img.SetActive(false);
        }

        playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
            playerMovement = playerObject.GetComponent<PlayerMovement>();
    }

    void OnEnable()
    {
        // Re-register if this object is re-enabled
        if (!hasRegistered)
            RegisterWithGameManager();
    }

    void OnDestroy()
    {
        // Unregister from GameManager when destroyed
        UnregisterFromGameManager();
    }

    private void RegisterWithGameManager()
    {
        if (hasRegistered) return;

        // Find GameManager if not assigned
        if (gameManager == null)
        {
            gameManager = FindObjectOfType<GameManager>();
        }

        if (gameManager != null)
        {
            // Initialize list if null
            if (gameManager.allCodeGames == null)
            {
                gameManager.allCodeGames = new System.Collections.Generic.List<CodeCheckGame>();
            }

            // Add this CodeCheckGame to the list if not already present
            if (!gameManager.allCodeGames.Contains(this))
            {
                gameManager.allCodeGames.Add(this);
                hasRegistered = true;
                Debug.Log($"CodeCheckGame '{gameObject.name}' registered with GameManager. Total: {gameManager.allCodeGames.Count}");

                // Update progress UI
                gameManager.UpdateProgressText();
            }
        }
        else
        {
            Debug.LogWarning($"CodeCheckGame '{gameObject.name}': Could not find GameManager to register with!");
        }
    }

    private void UnregisterFromGameManager()
    {
        if (gameManager != null && gameManager.allCodeGames != null)
        {
            if (gameManager.allCodeGames.Contains(this))
            {
                gameManager.allCodeGames.Remove(this);
                Debug.Log($"CodeCheckGame '{gameObject.name}' unregistered from GameManager. Remaining: {gameManager.allCodeGames.Count}");

                // Update progress UI
                gameManager.UpdateProgressText();
            }
        }
        hasRegistered = false;
    }

    void Update()
    {
        IsBeingInteractedWith = overlayPanel != null && overlayPanel.activeSelf;

        if (IsBeingInteractedWith && !IsPlayerInRange())
        {
            CloseCodePanel();
            return;
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
            overlayPanel.SetActive(true);

        isTriggered = true;
        if (playerMovement != null) playerMovement.LockMovement();

        // Start tracking debugging time
        if (gameManager != null)
            gameManager.StartCodeDebugging();

        if (!hasBeenSolved && linkedIntercom != null)
        {
            linkedIntercom.OnInteractionComplete();
        }
    }

    public void CloseCodePanel()
    {
        if (overlayPanel != null)
            overlayPanel.SetActive(false);

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

            // Play correct answer sound
            if (AudioManager.Instance != null)
                AudioManager.Instance.PlayCorrectAnswer();

            if (gameManager != null)
            {
                gameManager.RegisterCorrectObject(this);
                gameManager.FinishCodeDebugging(true); // Record successful debugging
            }

            if (hunter != null)
                hunter.NotifyCorrectObjectSolved(this);

            if (!hasBeenSolved)
            {
                hasBeenSolved = true;
            }

            lastAnswerWasHunterTampered = false;

            // Show random correct answer image
            StartCoroutine(ShowCorrectAnswerImage());
        }
        else
        {
            SetObjectColor(Color.red);

            // Play wrong answer sound
            if (AudioManager.Instance != null)
                AudioManager.Instance.PlayWrongAnswer();

            // Player takes damage only if hunter didn't tamper with the code
            if (gameManager != null)
            {
                gameManager.TakeDamage(lastAnswerWasHunterTampered);
                gameManager.FinishCodeDebugging(false); // Record failed debugging
            }

            lastAnswerWasHunterTampered = false;

            // Show wrong answer image
            StartCoroutine(ShowWrongAnswerImage());
        }

        CloseCodePanel();

        if (!string.IsNullOrEmpty(userInput) && !isOnCooldown)
            StartCoroutine(InteractCooldownRoutine());
    }

    private IEnumerator ShowCorrectAnswerImage()
    {
        if (correctAnswerImages.Length > 0)
        {
            int randomIndex = Random.Range(0, correctAnswerImages.Length);
            if (correctAnswerImages[randomIndex] != null)
            {
                correctAnswerImages[randomIndex].SetActive(true);
                yield return new WaitForSeconds(answerImageDisplayTime);
                correctAnswerImages[randomIndex].SetActive(false);
            }
        }
    }

    private IEnumerator ShowWrongAnswerImage()
    {
        if (wrongAnswerImage != null)
        {
            wrongAnswerImage.SetActive(true);
            yield return new WaitForSeconds(answerImageDisplayTime);
            wrongAnswerImage.SetActive(false);
        }
    }

    private IEnumerator InteractCooldownRoutine()
    {
        isOnCooldown = true;
        float timer = interactCooldown;

        if (interactCooldownCanvas != null)
            interactCooldownCanvas.SetActive(true);

        while (timer > 0)
        {
            if (interactCooldownText != null)
                interactCooldownText.text = $"{timer:F0}";
            timer -= Time.deltaTime;
            yield return null;
        }

        if (interactCooldownText != null)
            interactCooldownText.text = "";

        if (interactCooldownCanvas != null)
            interactCooldownCanvas.SetActive(false);

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

        // Play code interrupted sound
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayCodeInterrupted();

        // Mark that the hunter tampered with this code
        lastAnswerWasHunterTampered = true;

        // Record code interruption
        if (gameManager != null)
            gameManager.RecordCodeInterrupted();

        if (hasBeenSolved)
        {
            hasBeenSolved = false;
        }
    }

    private char GetRandomChar()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*";
        return chars[Random.Range(0, chars.Length)];
    }
}