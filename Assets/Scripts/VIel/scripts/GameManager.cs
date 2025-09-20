using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    [System.Serializable]
    public class CodeTask
    {
        public string instruction;
        public string correctAnswer;
        public string startingInput;
    }
    private bool nearWinStunTriggered = false;
    [Header("Multiplayer Settings")]
    public bool isMultiplayerMode = false; // Set to true for online mode
    public int localPlayerId = 0; // This player's ID
    public bool localPlayerIsSurvivor = true;

    [Header("Game Start Canvas")]
    public GameObject gameStartCanvas;
    public float gameStartDisplayTime = 3f;

    [Header("Auto Start Settings")]
    public bool useAutoStart = false; // Enable for online mode
    public float autoStartDelay = 5f;
    private float autoStartTimer;
    private bool autoStartTriggered = false;

    [Header("Timer")]
    public float gameDuration = 120f;
    private float remainingTime;
    public Text timerText;

    [Header("Endgame UI")]
    public GameObject endPanel;
    public Text endText;
    public Button playAgainButton;
    public Button exitButton;

    [Header("Records Display")]
    public GameObject recordsPanel;
    public Text recordsText;

    [Header("Pause Menu UI")]
    public GameObject pauseMenuPanel;
    public Button pauseButton;
    public Button resumeButton;
    public Button helpButton;
    public Button settingsButton;

    [Header("Help & Settings Panels")]
    public GameObject helpPanel;
    public GameObject settingsPanel;
    public Button helpCloseButton;
    public Button settingsCloseButton;

    [Header("Quick Chat UI")]
    public Button quickChatButton;
    public GameObject quickChatPanel;
    public Button[] quickChatMessageButtons;
    public GameObject[] quickChatImages;
    public float quickChatImageDuration = 3f;
    public float quickChatCooldown = 5f;
    private float quickChatTimer = 0f;
    private bool isQuickChatPanelOpen = false;
    public Text quickChatWarningText;

    [Header("Correct Objects")]
    public List<CodeCheckGame> allCodeGames;
    private HashSet<CodeCheckGame> correctObjects = new HashSet<CodeCheckGame>();

    [Header("Progress UI")]
    public Text progressText;

    [Header("Player Reference")]
    public Transform player;
    private PlayerMovement playerMovement;
    private Vector2 playerStartPos;
    [HideInInspector] public bool timerStarted = false;

    [Header("Random Stun Settings")]
    public float stunMinTime = 10f;
    public float stunMaxTime = 100f;
    private float stunTriggerTime;
    private bool stunTriggered = false;

    [Header("Game State")]
    public bool gameEnded = false;
    private bool isPaused = false;

    [Header("Game End Images")]
    public GameObject winImage;
    public GameObject loseImage;

    [Header("Player HP System")]
    public int maxHP = 6;
    public int currentHP;
    public GameObject[] heartIcons;
    public GameObject gameOverPanel;

    // Player Records
    [Header("Player Records")]
    public bool isHunterMode = false; // Set this based on player role
    private float gameStartTime;
    private int codesDebuggedCount = 0;
    private int codesInterruptedCount = 0;
    private List<float> debuggingTimes = new List<float>();
    private float currentCodeStartTime;

    private UIManager uiManager;

    void Start()
    {
        remainingTime = gameDuration;
        endPanel.SetActive(false);
        pauseMenuPanel.SetActive(false);
        currentHP = maxHP;
        gameStartTime = Time.time;
        autoStartTimer = autoStartDelay;

        // Get UIManager
        uiManager = FindObjectOfType<UIManager>();

        // Show game start canvas
        if (gameStartCanvas != null)
        {
            StartCoroutine(ShowGameStartCanvas());
        }

        playAgainButton.onClick.AddListener(RestartGame);
        exitButton.onClick.AddListener(ExitGame);

        if (pauseButton != null)
            pauseButton.onClick.AddListener(TogglePause);

        if (resumeButton != null)
            resumeButton.onClick.AddListener(ResumeGame);

        // Help and Settings buttons
        if (helpButton != null)
            helpButton.onClick.AddListener(() => {
                if (uiManager != null && helpPanel != null)
                    uiManager.ShowPanel(helpPanel);
            });

        if (settingsButton != null)
            settingsButton.onClick.AddListener(() => {
                if (uiManager != null && settingsPanel != null)
                    uiManager.ShowPanel(settingsPanel);
            });

        if (helpCloseButton != null)
            helpCloseButton.onClick.AddListener(() => {
                if (uiManager != null)
                    uiManager.GoBack();
            });

        if (settingsCloseButton != null)
            settingsCloseButton.onClick.AddListener(() => {
                if (uiManager != null)
                    uiManager.GoBack();
            });

        // Quick Chat setup
        if (quickChatButton != null)
            quickChatButton.onClick.AddListener(ToggleQuickChat);

        if (quickChatPanel != null)
            quickChatPanel.SetActive(false);

        // Setup quick chat message buttons
        for (int i = 0; i < quickChatMessageButtons.Length && i < quickChatImages.Length; i++)
        {
            int index = i;
            quickChatMessageButtons[i].onClick.AddListener(() => ShowQuickChatMessage(index));
        }

        if (player != null)
        {
            playerStartPos = player.position;
            playerMovement = player.GetComponent<PlayerMovement>();
        }

        GameObject[] mapParts = GameObject.FindGameObjectsWithTag("Map");
        if (mapParts.Length > 0)
        {
            Renderer[] allRenderers = System.Array.FindAll(
                System.Array.ConvertAll(mapParts, p => p.GetComponent<Renderer>()),
                r => r != null
            );

            if (allRenderers.Length > 0)
            {
                CameraFlow camFollow = Camera.main != null ? Camera.main.GetComponent<CameraFlow>() : null;
                if (camFollow != null && player != null)
                {
                    camFollow.player = player;
                    camFollow.SetBoundsFromMultipleRenderers(allRenderers);
                }
            }
        }

        stunTriggerTime = Random.Range(Mathf.Max(stunMinTime, 5f), stunMaxTime);

        if (winImage != null) winImage.SetActive(false);
        if (loseImage != null) loseImage.SetActive(false);

        UpdateProgressText();
        UpdateHeartDisplay();
    }

    private IEnumerator ShowGameStartCanvas()
    {
        gameStartCanvas.SetActive(true);
        yield return new WaitForSeconds(gameStartDisplayTime);
        gameStartCanvas.SetActive(false);
    }

    void Update()
    {
        if (gameEnded || isPaused) return;

        // Auto start timer for online mode
        if (useAutoStart && !timerStarted && !autoStartTriggered)
        {
            autoStartTimer -= Time.deltaTime;
            if (autoStartTimer <= 0f)
            {
                timerStarted = true;
                autoStartTriggered = true;
            }
        }

        // Quick chat cooldown timer
        if (quickChatTimer > 0)
        {
            quickChatTimer -= Time.deltaTime;
            if (quickChatWarningText != null && quickChatTimer > 0)
            {
                quickChatWarningText.text = $"{quickChatTimer:F1}s";
                quickChatWarningText.gameObject.SetActive(true);
            }
            else if (quickChatWarningText != null)
            {
                quickChatWarningText.gameObject.SetActive(false);
            }
        }

        if (!timerStarted && player != null && !useAutoStart)
        {
            if (Vector2.Distance(player.position, playerStartPos) > 0.01f)
                timerStarted = true;
        }

        if (timerStarted)
        {
            remainingTime -= Time.deltaTime;
            UpdateTimerDisplay();

            if (!stunTriggered && (gameDuration - remainingTime) >= stunTriggerTime)
                TriggerRandomStun();

            if (remainingTime <= 0f)
                GameOver(false);
        }
    }

    public void StartCodeDebugging()
    {
        currentCodeStartTime = Time.time;
    }

    public void FinishCodeDebugging(bool success)
    {
        float debugTime = Time.time - currentCodeStartTime;
        if (success)
        {
            debuggingTimes.Add(debugTime);
            codesDebuggedCount++;
        }
    }

    public void RecordCodeInterrupted()
    {
        if (isHunterMode)
            codesInterruptedCount++;
    }

    private float GetAverageDebuggingTime()
    {
        if (debuggingTimes.Count == 0) return 0f;

        float total = 0f;
        foreach (float time in debuggingTimes)
        {
            total += time;
        }
        return total / debuggingTimes.Count;
    }

    void ToggleQuickChat()
    {
        if (quickChatTimer > 0)
        {
            if (quickChatWarningText != null)
            {
                quickChatWarningText.text = $" {quickChatTimer:F1}";
                quickChatWarningText.gameObject.SetActive(true);
                StartCoroutine(HideWarningAfterDelay(2f));
            }
            return;
        }

        isQuickChatPanelOpen = !isQuickChatPanelOpen;
        if (quickChatPanel != null)
            quickChatPanel.SetActive(isQuickChatPanelOpen);
    }

    void ShowQuickChatMessage(int messageIndex)
    {
        if (messageIndex < quickChatImages.Length && quickChatImages[messageIndex] != null)
        {
            // Show locally
            StartCoroutine(ShowQuickChatImageCoroutine(messageIndex));
            quickChatTimer = quickChatCooldown;

            // If multiplayer, also show on multiplayer UI
            if (isMultiplayerMode && MultiplayerUIManager.Instance != null)
            {
                MultiplayerUIManager.Instance.ShowPlayerQuickChat(localPlayerId, messageIndex);
            }

            // Close quick chat panel
            isQuickChatPanelOpen = false;
            if (quickChatPanel != null)
                quickChatPanel.SetActive(false);
        }
    }
    public void OnPlayerDeath(int playerId)
    {
        if (isMultiplayerMode && MultiplayerUIManager.Instance != null)
        {
            MultiplayerUIManager.Instance.SetPlayerAliveStatus(playerId, false);
        }
    }
    public void AddPlayerToUI(int playerId, string playerName, bool isSurvivor, int characterIndex)
    {
        if (isMultiplayerMode && MultiplayerUIManager.Instance != null)
        {
            MultiplayerUIManager.Instance.AddPlayer(playerId, playerName, isSurvivor, characterIndex);
        }
    }
    public void RemovePlayerFromUI(int playerId)
    {
        if (isMultiplayerMode && MultiplayerUIManager.Instance != null)
        {
            MultiplayerUIManager.Instance.RemovePlayer(playerId);
        }
    }
    IEnumerator ShowQuickChatImageCoroutine(int imageIndex)
    {
        quickChatImages[imageIndex].SetActive(true);
        yield return new WaitForSeconds(quickChatImageDuration);
        quickChatImages[imageIndex].SetActive(false);
    }

    IEnumerator HideWarningAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (quickChatWarningText != null)
            quickChatWarningText.gameObject.SetActive(false);
    }

    public void TakeDamage(bool hunterTampered = false)
    {
        if (hunterTampered) return;

        currentHP--;
        UpdateHeartDisplay();

        if (currentHP <= 0)
        {
            GameOver(false);
        }
    }

    void UpdateHeartDisplay()
    {
        for (int i = 0; i < heartIcons.Length; i++)
        {
            if (heartIcons[i] != null)
            {
                heartIcons[i].SetActive(i < currentHP);
            }
        }
    }

    void TriggerRandomStun()
    {
        if (player == null) return;
        stunTriggered = true;

        HunterChaseAndHack hunter = FindObjectOfType<HunterChaseAndHack>();
        if (hunter != null)
            hunter.StartStunWarningCountdown(3);

        Invoke(nameof(ApplyStun), 3f);
    }

    void ApplyStun()
    {
        if (player == null) return;

        PlayerMovement pm = player.GetComponent<PlayerMovement>();
        if (pm != null)
            pm.LockMovement(5f);

        Invoke(nameof(ReleasePlayerStun), 5f);
    }

    void ReleasePlayerStun()
    {
        PlayerMovement pm = player.GetComponent<PlayerMovement>();
        if (pm != null)
            pm.UnlockMovement();
    }

    void UpdateTimerDisplay()
    {
        if (remainingTime < 0f) remainingTime = 0f;

        int minutes = Mathf.FloorToInt(remainingTime / 60f);
        int seconds = Mathf.FloorToInt(remainingTime % 60f);
        timerText.text = $"{minutes:00}:{seconds:00}";
    }

    private void TriggerNearWinStun()
    {
        Debug.Log("Near-win stun triggered!");

        if (player == null) return;

        HunterChaseAndHack hunter = FindObjectOfType<HunterChaseAndHack>();
        if (hunter != null)
            hunter.StartStunWarningCountdown(3);

        Invoke(nameof(ApplyStun), 3f);
    }

    public void RegisterCorrectObject(CodeCheckGame obj)
    {
        if (correctObjects.Contains(obj)) return;
        correctObjects.Add(obj);
        UpdateProgressText();

        if (!nearWinStunTriggered && correctObjects.Count == allCodeGames.Count - 1)
        {
            nearWinStunTriggered = true;
            TriggerNearWinStun();
        }

        if (correctObjects.Count >= allCodeGames.Count)
            GameOver(true);

        HunterChaseAndHack hunter = FindObjectOfType<HunterChaseAndHack>();
        if (hunter != null)
            hunter.NotifyCorrectObjectSolved(obj);
    }

    public void UnregisterCorrectObject(CodeCheckGame obj)
    {
        if (correctObjects.Contains(obj))
        {
            correctObjects.Remove(obj);
            UpdateProgressText();
        }
    }

    private void UpdateProgressText()
    {
        if (progressText != null)
            progressText.text = $"{correctObjects.Count}/{allCodeGames.Count}";
    }

    void GameOver(bool won)
    {
        gameEnded = true;

        Debug.Log($"Game Over called. Won = {won}");

        // Show records
        ShowPlayerRecords(won);

        endPanel.SetActive(true);
        endText.text = won ? " You Won!" : " Game Over";
        timerText.gameObject.SetActive(false);

        if (winImage != null)
        {
            winImage.SetActive(won);
            Debug.Log("Win image active: " + won);
        }
        if (loseImage != null)
        {
            loseImage.SetActive(!won);
            Debug.Log("Lose image active: " + !won);
        }
    }

    private void ShowPlayerRecords(bool won)
    {
        if (recordsPanel != null && recordsText != null)
        {
            recordsPanel.SetActive(true);

            string recordsString = "";
            int xpGained = won ? 100 : 50;

            if (isHunterMode)
            {
                recordsString = $"Hunter Mode: {(won ? "You Won!" : "You Lost!")}\n" +
                               $"+{xpGained} XP\n" +
                               $"Codes Interrupted: {codesInterruptedCount}";
            }
            else
            {
                float avgTime = GetAverageDebuggingTime();
                recordsString = $"Survivor Mode: {(won ? "You Won!" : "You Lost!")}\n" +
                               $"+{xpGained} XP\n" +
                               $"Debugging Time Average: {avgTime:F1} seconds\n" +
                               $"Codes Debugged: {codesDebuggedCount}";
            }

            recordsText.text = recordsString;
        }
    }

    public void TogglePause()
    {
        foreach (var codeGame in allCodeGames)
        {
            if (codeGame != null && codeGame.isActiveAndEnabled && codeGame.IsBeingInteractedWith)
                return;
        }

        isPaused = !isPaused;
        if (isPaused)
        {
            Time.timeScale = 0f;
            pauseMenuPanel.SetActive(true);
        }
        else
        {
            Time.timeScale = 1f;
            pauseMenuPanel.SetActive(false);
        }
    }

    public void ResumeGame()
    {
        isPaused = false;
        Time.timeScale = 1f;
        pauseMenuPanel.SetActive(false);
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    void ExitGame()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}