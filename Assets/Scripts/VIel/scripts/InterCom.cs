using UnityEngine;
using Photon.Pun;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// InterCom: allows any player (hunter or survivor) to interact.
/// MODIFIED: Added interact button functionality alongside F key.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class InterCom : MonoBehaviour
{
    public const string INTERCOM_POSITIONS_KEY = "IntercomPositions";

    [Header("Interaction Settings")]
    public KeyCode interactKey = KeyCode.F;

    [Header("References")]
    public CodeCheckGame codeCheckGame;
    public GameManager gameManager;

    [Header("Minimap Icon")]
    public GameObject minimapIcon;

    [Header("Interact Button UI")]
    public Button interactButton; // Assign this in the inspector
    public GameObject interactButtonPanel; // Optional: parent panel for the button

    private bool isPlayerNearby = false;
    private GameObject nearbyPlayer = null;
    private MiniMap miniMap;
    private bool isInitialized = false;

    void Awake()
    {
        // Ensure collider is trigger
        Collider2D col = GetComponent<Collider2D>();
        if (col != null && !col.isTrigger)
        {
            col.isTrigger = true;
            Debug.LogWarning($"[InterCom] Collider on {name} was not trigger – fixed automatically.");
        }

        // CRITICAL FIX: Get CodeCheckGame immediately in Awake
        if (codeCheckGame == null)
        {
            codeCheckGame = GetComponent<CodeCheckGame>();
        }

        // Setup interact button listener
        if (interactButton != null)
        {
            interactButton.onClick.AddListener(OnInteractButtonClicked);
        }

        // Hide interact button initially
        SetInteractButtonVisible(false);
    }

    void Start()
    {
        StartCoroutine(InitializeWithDelay());
    }

    IEnumerator InitializeWithDelay()
    {
        // Wait for 2 frames to ensure all components are ready
        yield return null;
        yield return null;

        int attempts = 0;
        int maxAttempts = 30;

        while (!isInitialized && attempts < maxAttempts)
        {
            attempts++;

            // Find components
            if (miniMap == null)
                miniMap = FindObjectOfType<MiniMap>();

            if (gameManager == null)
                gameManager = FindObjectOfType<GameManager>();

            // CRITICAL: Always try to get CodeCheckGame if missing
            if (codeCheckGame == null)
                codeCheckGame = GetComponent<CodeCheckGame>();

            // Check if we have essential components
            bool hasEssentials = (codeCheckGame != null);

            if (hasEssentials)
            {
                isInitialized = true;
                Debug.Log($"[InterCom] {name} initialized successfully on attempt {attempts}");
                break;
            }
            else
            {
                if (attempts % 5 == 0)
                {
                    Debug.LogWarning($"[InterCom] {name} initialization attempt {attempts}: CodeCheckGame = {(codeCheckGame != null ? "Found" : "MISSING")}");
                }

                yield return new WaitForSeconds(0.1f);
            }
        }

        if (!isInitialized)
        {
            Debug.LogError($"[InterCom] {name} failed to initialize after {maxAttempts} attempts!");
            Debug.LogError($"[InterCom] CodeCheckGame status: {(codeCheckGame != null ? "Found" : "MISSING - CHECK PREFAB!")}");
        }
        else
        {
            Debug.Log($"[InterCom] {name} READY - CodeCheckGame: {codeCheckGame.name}, ID: {codeCheckGame.GetIntercomID()}");
        }
    }

    void Update()
    {
        // CRITICAL FIX: Don't process anything until fully initialized
        if (!isInitialized || codeCheckGame == null)
        {
            return;
        }

        // Update interact button visibility based on player proximity and intercom state
        UpdateInteractButton();

        // Check for F key interaction
        if (isPlayerNearby && nearbyPlayer != null && Input.GetKeyDown(interactKey))
        {
            TryInteract();
        }
    }

    void UpdateInteractButton()
    {
        // Show button if player is nearby and intercom can be interacted with
        bool shouldShowButton = isPlayerNearby &&
                                nearbyPlayer != null &&
                                codeCheckGame != null &&
                                !codeCheckGame.isOnCooldown &&
                                !codeCheckGame.IsSolved() &&
                                (gameManager == null || !gameManager.gameEnded);

        SetInteractButtonVisible(shouldShowButton);
    }

    void SetInteractButtonVisible(bool visible)
    {
        if (interactButtonPanel != null)
        {
            interactButtonPanel.SetActive(visible);
        }
        else if (interactButton != null)
        {
            interactButton.gameObject.SetActive(visible);
        }
    }

    void OnInteractButtonClicked()
    {
        if (isPlayerNearby && nearbyPlayer != null)
        {
            TryInteract();
        }
    }

    void TryInteract()
    {
        // Double-check CodeCheckGame exists
        if (codeCheckGame == null)
        {
            Debug.LogError($"[InterCom] CodeCheckGame is null on {name} during interaction!");
            return;
        }

        if (codeCheckGame.isOnCooldown)
        {
            Debug.Log($"[InterCom] {name} is on cooldown");
            return;
        }

        if (codeCheckGame.IsSolved())
        {
            Debug.Log($"[InterCom] {name} is already solved");
            return;
        }

        if (gameManager != null && gameManager.gameEnded)
        {
            return;
        }

        Debug.Log($"[InterCom] Player interacting with {name}");
        codeCheckGame.OpenCodePanel();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        if (!IsLocalPlayer(other.gameObject)) return;

        isPlayerNearby = true;
        nearbyPlayer = other.gameObject;
        Debug.Log($"[InterCom] Player entered range of {name}");
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        if (!IsLocalPlayer(other.gameObject)) return;

        if (other.gameObject == nearbyPlayer)
        {
            isPlayerNearby = false;
            nearbyPlayer = null;
            SetInteractButtonVisible(false);
            Debug.Log($"[InterCom] Player exited range of {name}");
        }
    }

    private bool IsLocalPlayer(GameObject player)
    {
        if (PhotonNetwork.OfflineMode || !PhotonNetwork.IsConnected)
            return true;

        PhotonView pv = player.GetComponent<PhotonView>();
        return pv != null ? pv.IsMine : true;
    }

    public void OnInteractionComplete()
    {
        if (miniMap != null && minimapIcon != null)
            miniMap.RevealIntercom(minimapIcon);
    }

    public void DebugComponentStatus()
    {
        Debug.Log($"[InterCom] {name} Component Status:");
        Debug.Log($"  - Initialized: {isInitialized}");
        Debug.Log($"  - CodeCheckGame: {(codeCheckGame != null ? "Found" : "Missing")}");
        Debug.Log($"  - GameManager: {(gameManager != null ? "Found" : "Missing")}");
        Debug.Log($"  - Player Nearby: {isPlayerNearby}");

        if (codeCheckGame != null)
        {
            Debug.Log($"  - CodeCheckGame ID: {codeCheckGame.GetIntercomID()}");
            Debug.Log($"  - On Cooldown: {codeCheckGame.isOnCooldown}");
        }
    }
}