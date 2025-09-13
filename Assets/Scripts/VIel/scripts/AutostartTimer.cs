using UnityEngine;
using UnityEngine.UI;

public class AutoStartTimer : MonoBehaviour
{
    [Header("UI Elements")]
    public Text countdownText;
    public GameObject countdownPanel;

    [Header("Settings")]
    public float countdownDuration = 10f;
    public bool enableAutoStart = false;

    private float timer;
    private GameManager gameManager;
    private PlayerMovement playerMovement;
    private bool countdownStarted = false;

    void Start()
    {
        gameManager = FindObjectOfType<GameManager>();

        // Find player by tag instead of component
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
        {
            playerMovement = playerObject.GetComponent<PlayerMovement>();
        }

        timer = countdownDuration;

        if (enableAutoStart)
        {
            if (countdownPanel != null)
                countdownPanel.SetActive(true);
            countdownStarted = true;

            // Lock player movement during countdown using the existing LockMovement method
            if (playerMovement != null)
            {
                playerMovement.LockMovement();
            }
        }
        else
        {
            if (countdownPanel != null)
                countdownPanel.SetActive(false);
        }
    }

    void Update()
    {
        if (!enableAutoStart || !countdownStarted) return;

        timer -= Time.deltaTime;

        if (countdownText != null)
        {
            countdownText.text = $"Game starts in: {Mathf.Ceil(timer):F0}";
        }

        if (timer <= 0f && gameManager != null)
        {
            // Force start the game
            gameManager.timerStarted = true;

            // Unlock player movement when game starts
            if (playerMovement != null)
            {
                playerMovement.UnlockMovement();
            }

            if (countdownPanel != null)
                countdownPanel.SetActive(false);

            countdownStarted = false;
            enabled = false; // Disable this script
        }
    }

    public void EnableAutoStart(bool enable)
    {
        enableAutoStart = enable;

        if (enableAutoStart && !countdownStarted)
        {
            timer = countdownDuration;
            countdownStarted = true;

            // Lock player movement when enabling auto start
            if (playerMovement != null)
            {
                playerMovement.LockMovement();
            }

            if (countdownPanel != null)
                countdownPanel.SetActive(true);
        }
        else if (!enableAutoStart)
        {
            // Unlock player if disabling auto start
            if (playerMovement != null)
            {
                playerMovement.UnlockMovement();
            }

            if (countdownPanel != null)
                countdownPanel.SetActive(false);
            countdownStarted = false;
        }
    }
}