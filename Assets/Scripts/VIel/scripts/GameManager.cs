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

    [Header("Timer")]
    public float gameDuration = 120f;
    private float remainingTime;
    public Text timerText;

    [Header("Endgame UI")]
    public GameObject endPanel;
    public Text endText;
    public Button playAgainButton;
    public Button exitButton;

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
    public Button[] quickChatMessageButtons; // Array of message buttons
    public GameObject[] quickChatImages; // Array of images to show
    public float quickChatImageDuration = 3f;
    public float quickChatCooldown = 5f;
    private float quickChatTimer = 0f;
    private bool isQuickChatPanelOpen = false;
    public Text quickChatWarningText; // Warning text for spam

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
    public GameObject[] heartIcons; // Array of heart UI elements
    public GameObject gameOverPanel; // Panel to show when HP reaches 0

    private UIManager uiManager;

    void Start()
    {
        remainingTime = gameDuration;
        endPanel.SetActive(false);
        pauseMenuPanel.SetActive(false);
        currentHP = maxHP;

        // Get UIManager
        uiManager = FindObjectOfType<UIManager>();

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
            int index = i; // Capture for closure
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

    void Update()
    {
        if (gameEnded || isPaused) return;

        // Quick chat cooldown timer
        if (quickChatTimer > 0)
        {
            quickChatTimer -= Time.deltaTime;
            if (quickChatWarningText != null && quickChatTimer > 0)
            {
                quickChatWarningText.text = $"Quick Chat on cooldown: {quickChatTimer:F1}s";
                quickChatWarningText.gameObject.SetActive(true);
            }
            else if (quickChatWarningText != null)
            {
                quickChatWarningText.gameObject.SetActive(false);
            }
        }

        if (!timerStarted && player != null)
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

    void ToggleQuickChat()
    {
        if (quickChatTimer > 0)
        {
            // Show warning
            if (quickChatWarningText != null)
            {
                quickChatWarningText.text = $"Please wait {quickChatTimer:F1}s before using Quick Chat again!";
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
            StartCoroutine(ShowQuickChatImageCoroutine(messageIndex));
            quickChatTimer = quickChatCooldown;

            // Close quick chat panel
            isQuickChatPanelOpen = false;
            if (quickChatPanel != null)
                quickChatPanel.SetActive(false);
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
        if (hunterTampered) return; // Don't take damage if hunter tampered

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