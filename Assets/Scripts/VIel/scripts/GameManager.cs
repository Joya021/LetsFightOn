using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;
using Hashtable = ExitGames.Client.Photon.Hashtable;

public class GameManager : MonoBehaviourPunCallbacks
{
    [System.Serializable]
    public class CodeTask
    {
        public string instruction;
        public string correctAnswer;
        public string startingInput;
    }

    // LAG COMPENSATION: Stores recent player positions for rollback
    [Header("Lag Compensation")]
    public bool enableLagCompensation = true;
    public float lagCompensationWindow = 1f;
    private Dictionary<int, List<PlayerStateSnapshot>> playerStateHistory = new Dictionary<int, List<PlayerStateSnapshot>>();

    [System.Serializable]
    private class PlayerStateSnapshot
    {
        public Vector3 position;
        public double timestamp;

        public PlayerStateSnapshot(Vector3 pos, double time)
        {
            position = pos;
            timestamp = time;
        }
    }

    private bool nearWinStunTriggered = false;
    private bool gameLogCalled = false;

    [Header("Multiplayer Settings")]
    public Text pingText;
    public bool isMultiplayerMode = false;
    public int localPlayerId = 0;
    public bool localPlayerIsSurvivor = true;
    public bool localPlayerIsHunter = false;

    [Header("CodeCheckGame UI References")]
    public GameObject codeCheckOverlayPanel;
    public InputField codeCheckInputField;
    public Text codeCheckTaskText;
    public Button codeCheckButton;
    public GameObject[] correctAnswerImages;
    public GameObject wrongAnswerImage;
    public GameObject interactCooldownCanvas;
    public Text interactCooldownText;

    [Header("Role-Specific UI Canvases")]
    public GameObject survivorUICanvas;
    public GameObject hunterUICanvas;

    [Header("Game Start Canvas")]
    public GameObject survivorGameStartCanvas;
    public GameObject hunterGameStartCanvas;
    public float gameStartDisplayTime = 3f;

    [Header("Auto Start Settings")]
    public bool useAutoStart = false;
    public float autoStartDelay = 5f;
    private float autoStartTimer;
    private bool autoStartTriggered = false;

    [Header("Timer")]
    public float gameDuration = 300f;
    private float remainingTime;
    public Text timerText;

    private const float WRONG_CODE_TIME_PENALTY = 15f;

    private double gameStartTimestamp = 0;
    private const string GAME_START_TIME_KEY = "GameStartTime";
    private const string TIMER_STARTED_KEY = "TimerStarted";
    private const string GAME_END_TIME_KEY = "GameEndTime";
    private const string PLAYER_CURRENT_SCENE_KEY = "PlayerCurrentScene";

    [Header("Endgame UI")]
    public GameObject endPanel;
    public Text endText;
    public Text endReasonText;
    public Button playAgainButton;
    public Button exitButton;
    public Button lobbyButton;
    public Button onlineExitButton;

    [Header("Dead Survivor Panel")]
    public GameObject deadSurvivorPanel;
    public Text deadSurvivorTimerText;
    public Button deadSurvivorFindRoomButton;

    [Header("Cleanup UI")]
    public GameObject cleanupPanel;
    public Text cleanupText;

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

    [Header("Quick Chat Name Display")]
    public Text quickChatPlayerNameText;
    public GameObject quickChatNamePanel;
    public Image quickChatCharacterImage;
    public Sprite[] characterSprites;

    [Header("Gameplay Log System")]
    public GameObject gameplayLogPanel;
    public Text gameplayLogText;
    public ScrollRect gameplayLogScrollRect;
    public float logMessageDuration = 5f;
    private List<string> logMessages = new List<string>();
    private const int MAX_LOG_MESSAGES = 10;

    [Header("Intercom Interaction")]
    public Button intercomInteractButton;
    public Text intercomInteractText;
    private CodeCheckGame nearbyIntercom = null;
    private float intercomCheckDistance = 3f;

    [Header("Correct Objects")]
    public List<CodeCheckGame> allCodeGames;
    private HashSet<CodeCheckGame> correctObjects = new HashSet<CodeCheckGame>();

    [Header("Progress UI")]
    public Text progressText;

    [Header("Player Reference")]
    public Transform player;
    private PlayerMovement playerMovement;
    private HunterController hunterController;
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
    private string gameEndReason = "";
    private bool isCleaningUp = false;
    private bool localPlayerIsDead = false;
    private bool recordGameProgress = true;

    [Header("Game End Images")]
    public GameObject winImage;
    public GameObject loseImage;

    [Header("Player HP System")]
    public int maxHP = 6;
    public int currentHP;
    public GameObject[] heartIcons;
    public GameObject gameOverPanel;

    [Header("Player Records")]
    public bool isHunterMode = false;
    private float gameStartTime;
    private int codesDebuggedCount = 0;
    private int codesInterruptedCount = 0;
    private List<float> debuggingTimes = new List<float>();
    private float currentCodeStartTime;

    private int aliveSurvivors = 0;
    private int totalSurvivors = 0;

    private UIManager uiManager;

    private bool codeGamesInitialized = false;
    private Coroutine codeGameDiscoveryCoroutine;

    void Awake()
    {
        if (allCodeGames == null)
        {
            allCodeGames = new List<CodeCheckGame>();
        }
    }

    void Start()
    {
        StartCodeGameDiscovery();

        remainingTime = gameDuration;
        endPanel.SetActive(false);
        pauseMenuPanel.SetActive(false);

        if (deadSurvivorPanel != null)
            deadSurvivorPanel.SetActive(false);

        if (cleanupPanel != null)
            cleanupPanel.SetActive(false);

        autoStartTimer = autoStartDelay;

        uiManager = FindObjectOfType<UIManager>();

        if (isMultiplayerMode && PhotonNetwork.IsConnected)
        {
            if (PhotonNetwork.LocalPlayer.CustomProperties.ContainsKey("PlayerRole"))
            {
                localPlayerIsHunter = (bool)PhotonNetwork.LocalPlayer.CustomProperties["PlayerRole"];
                localPlayerIsSurvivor = !localPlayerIsHunter;
                isHunterMode = localPlayerIsHunter;

                PhotonNetwork.AutomaticallySyncScene = false;
            }

            // Mark that this player is in the Game scene
            if (PhotonNetwork.LocalPlayer != null)
            {
                Hashtable sceneProps = new Hashtable();
                sceneProps[PLAYER_CURRENT_SCENE_KEY] = "Game";
                PhotonNetwork.LocalPlayer.SetCustomProperties(sceneProps);
                Debug.Log("[GameManager] Marked player as in Game scene");
            }
        }

        SetupRoleSpecificUI();

        if (localPlayerIsSurvivor)
        {
            currentHP = maxHP;
        }
        else
        {
            currentHP = maxHP;
            foreach (var heart in heartIcons)
            {
                if (heart != null) heart.SetActive(false);
            }
        }

        gameStartTime = Time.time;

        if (survivorGameStartCanvas != null && hunterGameStartCanvas != null)
        {
            if (localPlayerIsHunter)
            {
                StartCoroutine(ShowGameStartCanvas(hunterGameStartCanvas));
            }
            else
            {
                StartCoroutine(ShowGameStartCanvas(survivorGameStartCanvas));
            }
        }

        playAgainButton.onClick.AddListener(RestartGame);
        exitButton.onClick.AddListener(ExitGame);

        if (onlineExitButton != null)
            onlineExitButton.onClick.AddListener(OnlineExit);

        if (deadSurvivorFindRoomButton != null)
            deadSurvivorFindRoomButton.onClick.AddListener(DeadSurvivorGoToFindRoom);

        if (pauseButton != null)
            pauseButton.onClick.AddListener(TogglePause);

        if (resumeButton != null)
            resumeButton.onClick.AddListener(ResumeGame);

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

        if (quickChatButton != null)
            quickChatButton.onClick.AddListener(ToggleQuickChat);

        if (quickChatPanel != null)
            quickChatPanel.SetActive(false);

        if (quickChatNamePanel != null)
            quickChatNamePanel.SetActive(false);

        if (quickChatCharacterImage != null)
            quickChatCharacterImage.gameObject.SetActive(false);

        if (gameplayLogPanel != null)
            gameplayLogPanel.SetActive(true);

        if (intercomInteractButton != null)
        {
            intercomInteractButton.onClick.AddListener(InteractWithNearbyIntercom);
            intercomInteractButton.gameObject.SetActive(false);
        }

        for (int i = 0; i < quickChatMessageButtons.Length && i < quickChatImages.Length; i++)
        {
            int index = i;
            quickChatMessageButtons[i].onClick.AddListener(() => ShowQuickChatMessage(index));
        }

        if (player != null)
        {
            playerStartPos = player.position;
            playerMovement = player.GetComponent<PlayerMovement>();
            hunterController = player.GetComponent<HunterController>();
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
                    camFollow.SetFollowTarget(player);
                    camFollow.SetBoundsFromMultipleRenderers(allRenderers);
                }
            }
        }

        stunTriggerTime = Random.Range(Mathf.Max(stunMinTime, 5f), stunMaxTime);

        if (winImage != null) winImage.SetActive(false);
        if (loseImage != null) loseImage.SetActive(false);

        UpdateProgressText();
        UpdateHeartDisplay();

        if (lobbyButton != null)
            lobbyButton.onClick.AddListener(ReturnToLobby);

        CountSurvivors();

        if (isMultiplayerMode && PhotonNetwork.IsConnected)
        {
            if (!PhotonNetwork.IsMasterClient)
            {
                useAutoStart = false;
            }

            if (enableLagCompensation)
            {
                StartCoroutine(RecordPlayerPositions());
            }
        }
    }

    IEnumerator RecordPlayerPositions()
    {
        while (!gameEnded)
        {
            if (PhotonNetwork.IsConnected && PhotonNetwork.InRoom)
            {
                foreach (var player in FindObjectsOfType<PhotonView>())
                {
                    if (player.CompareTag("Player"))
                    {
                        int playerId = player.Owner.ActorNumber;

                        if (!playerStateHistory.ContainsKey(playerId))
                        {
                            playerStateHistory[playerId] = new List<PlayerStateSnapshot>();
                        }

                        playerStateHistory[playerId].Add(new PlayerStateSnapshot(
                            player.transform.position,
                            PhotonNetwork.Time
                        ));

                        playerStateHistory[playerId].RemoveAll(s =>
                            PhotonNetwork.Time - s.timestamp > lagCompensationWindow
                        );
                    }
                }
            }

            yield return new WaitForSeconds(0.05f);
        }
    }

    public Vector3 GetCompensatedPosition(int playerId, double targetTime)
    {
        if (!enableLagCompensation || !playerStateHistory.ContainsKey(playerId))
        {
            foreach (var player in FindObjectsOfType<PhotonView>())
            {
                if (player.CompareTag("Player") && player.Owner.ActorNumber == playerId)
                {
                    return player.transform.position;
                }
            }
            return Vector3.zero;
        }

        var history = playerStateHistory[playerId];
        if (history.Count == 0) return Vector3.zero;

        PlayerStateSnapshot before = null;
        PlayerStateSnapshot after = null;

        for (int i = 0; i < history.Count - 1; i++)
        {
            if (history[i].timestamp <= targetTime && history[i + 1].timestamp >= targetTime)
            {
                before = history[i];
                after = history[i + 1];
                break;
            }
        }

        if (before == null || after == null)
        {
            float closestDist = float.MaxValue;
            Vector3 closestPos = history[0].position;

            foreach (var snap in history)
            {
                float dist = Mathf.Abs((float)(snap.timestamp - targetTime));
                if (dist < closestDist)
                {
                    closestDist = dist;
                    closestPos = snap.position;
                }
            }

            return closestPos;
        }

        float t = (float)((targetTime - before.timestamp) / (after.timestamp - before.timestamp));
        return Vector3.Lerp(before.position, after.position, t);
    }

    public void AddGameplayLog(string message)
    {
        if (gameplayLogText == null || gameLogCalled) return;

        gameLogCalled = true;

        logMessages.Add($"[{System.DateTime.Now.ToString("mm:ss")}] {message}");

        if (logMessages.Count > MAX_LOG_MESSAGES)
        {
            logMessages.RemoveAt(0);
        }

        gameplayLogText.text = string.Join("\n", logMessages);

        if (gameplayLogScrollRect != null)
        {
            StartCoroutine(ScrollToBottom());
        }

        if (isMultiplayerMode && PhotonNetwork.IsConnected && PhotonNetwork.IsMasterClient)
        {
            photonView.RPC("RPC_AddGameplayLog", RpcTarget.Others, message);
        }

        StartCoroutine(ResetLogCallFlag());
    }

    IEnumerator ResetLogCallFlag()
    {
        yield return new WaitForEndOfFrame();
        gameLogCalled = false;
    }

    [PunRPC]
    void RPC_AddGameplayLog(string message)
    {
        if (gameplayLogText == null) return;

        logMessages.Add($"[{System.DateTime.Now.ToString("mm:ss")}] {message}");

        if (logMessages.Count > MAX_LOG_MESSAGES)
        {
            logMessages.RemoveAt(0);
        }

        gameplayLogText.text = string.Join("\n", logMessages);

        if (gameplayLogScrollRect != null)
        {
            StartCoroutine(ScrollToBottom());
        }
    }

    public void DeadSurvivorGoToFindRoom()
    {
        recordGameProgress = false;
        StartCoroutine(DeadSurvivorExitToFindRoom());
    }

    IEnumerator DeadSurvivorExitToFindRoom()
    {
        if (cleanupPanel != null)
        {
            cleanupPanel.SetActive(true);
            if (cleanupText != null)
                cleanupText.text = "Leaving game...";
        }

        isCleaningUp = true;

        allCodeGames.Clear();
        correctObjects.Clear();
        playerStateHistory.Clear();
        logMessages.Clear();

        yield return new WaitForSeconds(0.3f);

        if (PhotonNetwork.LocalPlayer != null)
        {
            Hashtable clearAllProps = new Hashtable();
            clearAllProps["PlayerRole"] = null;
            clearAllProps["PlayerCharacter"] = null;
            clearAllProps["PlayerLockedIn"] = null;
            clearAllProps["CharacterIndex"] = null;
            clearAllProps[PLAYER_CURRENT_SCENE_KEY] = null;
            PhotonNetwork.LocalPlayer.SetCustomProperties(clearAllProps);
        }

        yield return new WaitForSeconds(0.2f);

        if (PhotonNetwork.IsConnected && PhotonNetwork.InRoom)
        {
            PhotonNetwork.LeaveRoom();

            float timeout = 5f;
            float elapsed = 0f;
            while (PhotonNetwork.InRoom && elapsed < timeout)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }
        }

        yield return new WaitForSeconds(0.3f);

        if (cleanupPanel != null)
            cleanupPanel.SetActive(false);

        isCleaningUp = false;

        SceneManager.LoadScene("FindRoom");
    }

    public void OnlineExit()
    {
        StartCoroutine(IndependentExit());
    }

    IEnumerator IndependentExit()
    {
        if (cleanupPanel != null)
        {
            cleanupPanel.SetActive(true);
            if (cleanupText != null)
                cleanupText.text = "Leaving room...";
        }

        isCleaningUp = true;

        allCodeGames.Clear();
        correctObjects.Clear();
        playerStateHistory.Clear();
        logMessages.Clear();

        yield return new WaitForSeconds(0.3f);

        if (PhotonNetwork.LocalPlayer != null)
        {
            Hashtable clearAllProps = new Hashtable();
            clearAllProps["PlayerRole"] = null;
            clearAllProps["PlayerCharacter"] = null;
            clearAllProps["PlayerLockedIn"] = null;
            clearAllProps["CharacterIndex"] = null;
            clearAllProps[PLAYER_CURRENT_SCENE_KEY] = null;
            PhotonNetwork.LocalPlayer.SetCustomProperties(clearAllProps);
        }

        yield return new WaitForSeconds(0.2f);

        if (PhotonNetwork.IsConnected && PhotonNetwork.InRoom)
        {
            PhotonNetwork.LeaveRoom();

            float timeout = 5f;
            float elapsed = 0f;
            while (PhotonNetwork.InRoom && elapsed < timeout)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            if (PhotonNetwork.InRoom)
            {
                Debug.LogWarning("[GameManager] Failed to leave room within timeout, forcing scene change");
            }
        }

        yield return new WaitForSeconds(0.3f);

        if (cleanupPanel != null)
            cleanupPanel.SetActive(false);

        isCleaningUp = false;

        SceneManager.LoadScene("FindRoom");
    }

    IEnumerator CleanupGameResources()
    {
        if (isCleaningUp) yield break;

        isCleaningUp = true;

        allCodeGames.Clear();
        correctObjects.Clear();
        playerStateHistory.Clear();
        logMessages.Clear();

        yield return new WaitForSeconds(0.3f);

        isCleaningUp = false;
    }

    void StartCodeGameDiscovery()
    {
        if (codeGameDiscoveryCoroutine != null)
        {
            StopCoroutine(codeGameDiscoveryCoroutine);
        }
        codeGameDiscoveryCoroutine = StartCoroutine(ContinuousCodeGameDiscovery());
    }

    IEnumerator ContinuousCodeGameDiscovery()
    {
        int attempts = 0;
        int maxAttempts = 20;
        float checkInterval = 0.5f;

        Debug.Log("[GameManager] Starting continuous CodeCheckGame discovery...");

        while (!codeGamesInitialized && attempts < maxAttempts)
        {
            attempts++;

            CodeCheckGame[] foundGames = FindObjectsOfType<CodeCheckGame>();

            if (foundGames.Length > 0)
            {
                Debug.Log($"[GameManager] Discovery attempt {attempts}: Found {foundGames.Length} CodeCheckGame objects");

                bool foundNewGames = false;
                foreach (var game in foundGames)
                {
                    if (game != null && !allCodeGames.Contains(game))
                    {
                        allCodeGames.Add(game);
                        foundNewGames = true;
                        Debug.Log($"[GameManager] Added CodeCheckGame: {game.gameObject.name}");
                    }
                }

                if (foundNewGames)
                {
                    UpdateProgressText();
                }

                if (allCodeGames.Count >= 3)
                {
                    codeGamesInitialized = true;
                    Debug.Log($"[GameManager] CodeCheckGame discovery complete! Found {allCodeGames.Count} total games");
                    break;
                }
            }
            else
            {
                Debug.Log($"[GameManager] Discovery attempt {attempts}: Found 0 CodeCheckGame objects, retrying...");
            }

            yield return new WaitForSeconds(checkInterval);
        }

        if (!codeGamesInitialized)
        {
            Debug.LogWarning($"[GameManager] CodeCheckGame discovery completed after {maxAttempts} attempts. Found {allCodeGames.Count} games.");
            codeGamesInitialized = true;
        }

        yield return new WaitForSeconds(1f);
        PerformFinalVerification();
    }

    void PerformFinalVerification()
    {
        CodeCheckGame[] allFoundGames = FindObjectsOfType<CodeCheckGame>();

        Debug.Log($"[GameManager] FINAL VERIFICATION: Found {allFoundGames.Length} CodeCheckGame objects in scene");
        Debug.Log($"[GameManager] FINAL VERIFICATION: GameManager tracking {allCodeGames.Count} CodeCheckGame objects");

        if (allFoundGames.Length == 0)
        {
            Debug.LogError("[GameManager] CRITICAL ERROR: NO CodeCheckGame objects found in scene!");
        }
        else if (allCodeGames.Count == 0)
        {
            Debug.LogError("[GameManager] ERROR: Found CodeCheckGame objects but none registered with GameManager!");
            foreach (var game in allFoundGames)
            {
                if (game != null)
                {
                    RegisterCodeCheckGame(game);
                }
            }
        }

        UpdateProgressText();
    }

    public void RegisterCodeCheckGame(CodeCheckGame game)
    {
        if (game == null) return;

        if (!allCodeGames.Contains(game))
        {
            allCodeGames.Add(game);
            Debug.Log($"[GameManager] CodeCheckGame registered: {game.gameObject.name}. Total: {allCodeGames.Count}");
            UpdateProgressText();
        }
    }

    public void UnregisterCodeCheckGame(CodeCheckGame game)
    {
        if (game == null) return;

        if (allCodeGames.Contains(game))
        {
            allCodeGames.Remove(game);
            Debug.Log($"[GameManager] CodeCheckGame unregistered: {game.gameObject.name}. Remaining: {allCodeGames.Count}");
            UpdateProgressText();
        }
    }

    void SetupRoleSpecificUI()
    {
        if (localPlayerIsHunter)
        {
            if (hunterUICanvas != null) hunterUICanvas.SetActive(true);
            if (survivorUICanvas != null) survivorUICanvas.SetActive(false);

            Debug.Log("GameManager: Showing Hunter UI Canvas");
        }
        else
        {
            if (survivorUICanvas != null) survivorUICanvas.SetActive(true);
            if (hunterUICanvas != null) hunterUICanvas.SetActive(false);

            Debug.Log("GameManager: Showing Survivor UI Canvas");
        }
    }

    void CountSurvivors()
    {
        if (isMultiplayerMode && PhotonNetwork.IsConnected)
        {
            totalSurvivors = 0;
            aliveSurvivors = 0;

            foreach (Player p in PhotonNetwork.PlayerList)
            {
                if (p.CustomProperties.ContainsKey("PlayerRole"))
                {
                    bool isHunter = (bool)p.CustomProperties["PlayerRole"];
                    if (!isHunter)
                    {
                        totalSurvivors++;
                        aliveSurvivors++;
                    }
                }
            }
        }
        else
        {
            totalSurvivors = 1;
            aliveSurvivors = 1;
        }

        Debug.Log($"Total survivors: {totalSurvivors}, Alive: {aliveSurvivors}");
    }

    public void ReturnToLobby()
    {
        StartCoroutine(IndependentReturnToLobby());
    }

    IEnumerator IndependentReturnToLobby()
    {
        if (cleanupPanel != null)
        {
            cleanupPanel.SetActive(true);
            if (cleanupText != null)
                cleanupText.text = "Returning to lobby...";
        }

        isCleaningUp = true;

        allCodeGames.Clear();
        correctObjects.Clear();
        playerStateHistory.Clear();
        logMessages.Clear();

        yield return new WaitForSeconds(0.3f);

        if (PhotonNetwork.LocalPlayer != null)
        {
            Hashtable clearProps = new Hashtable();
            clearProps["PlayerCharacter"] = null;
            clearProps["PlayerLockedIn"] = null;
            clearProps["CharacterIndex"] = null;
            clearProps["PlayerRole"] = null;
            PhotonNetwork.LocalPlayer.SetCustomProperties(clearProps);
        }

        yield return new WaitForSeconds(0.2f);

        if (PhotonNetwork.InRoom && PhotonNetwork.IsMasterClient)
        {
            if (!PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey(GAME_END_TIME_KEY))
            {
                Hashtable gameProps = new Hashtable();
                gameProps[GAME_END_TIME_KEY] = PhotonNetwork.Time;
                gameProps["GameInProgress"] = false;
                gameProps["OriginalHostID"] = PhotonNetwork.LocalPlayer.ActorNumber;
                PhotonNetwork.CurrentRoom.SetCustomProperties(gameProps);

                Debug.Log("[GameManager] First player returning to lobby - 20-second grace period started");
            }
        }

        if (cleanupPanel != null)
            cleanupPanel.SetActive(false);

        isCleaningUp = false;

        SceneManager.LoadScene("Lobby");
    }

    public override void OnLeftRoom()
    {
        Debug.Log("[GameManager] Successfully left room");

        if (!isCleaningUp)
        {
            SceneManager.LoadScene("FindRoom");
        }
    }

    private void InitializeMultiplayerTimer()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            if (!PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey(TIMER_STARTED_KEY))
            {
                ExitGames.Client.Photon.Hashtable props = new ExitGames.Client.Photon.Hashtable();
                props[TIMER_STARTED_KEY] = false;
                PhotonNetwork.CurrentRoom.SetCustomProperties(props);
            }
        }
        else
        {
            if (PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey(GAME_START_TIME_KEY))
            {
                gameStartTimestamp = (double)PhotonNetwork.CurrentRoom.CustomProperties[GAME_START_TIME_KEY];
                timerStarted = (bool)PhotonNetwork.CurrentRoom.CustomProperties[TIMER_STARTED_KEY];

                if (timerStarted)
                {
                    double elapsedTime = PhotonNetwork.Time - gameStartTimestamp;
                    remainingTime = gameDuration - (float)elapsedTime;

                    if (remainingTime < 0)
                        remainingTime = 0;
                }
            }
        }
    }

    public override void OnRoomPropertiesUpdate(ExitGames.Client.Photon.Hashtable propertiesThatChanged)
    {
        if (propertiesThatChanged.ContainsKey(TIMER_STARTED_KEY))
        {
            timerStarted = (bool)propertiesThatChanged[TIMER_STARTED_KEY];
        }

        if (propertiesThatChanged.ContainsKey(GAME_START_TIME_KEY))
        {
            gameStartTimestamp = (double)propertiesThatChanged[GAME_START_TIME_KEY];
        }
    }

    private void StartGameTimer()
    {
        if (isMultiplayerMode && PhotonNetwork.IsConnected && PhotonNetwork.IsMasterClient)
        {
            gameStartTimestamp = PhotonNetwork.Time;

            ExitGames.Client.Photon.Hashtable props = new ExitGames.Client.Photon.Hashtable();
            props[GAME_START_TIME_KEY] = gameStartTimestamp;
            props[TIMER_STARTED_KEY] = true;
            PhotonNetwork.CurrentRoom.SetCustomProperties(props);
        }

        timerStarted = true;
    }

    private IEnumerator ShowGameStartCanvas(GameObject canvas)
    {
        if (canvas != null)
        {
            canvas.SetActive(true);
            yield return new WaitForSeconds(gameStartDisplayTime);
            canvas.SetActive(false);
        }
    }

    void Update()
    {
        if (pingText != null && PhotonNetwork.IsConnected)
            pingText.text = $"Ping: {PhotonNetwork.GetPing()}ms";

        if (localPlayerIsDead && deadSurvivorPanel != null && deadSurvivorPanel.activeSelf)
        {
            UpdateDeadSurvivorTimer();
        }

        UpdateTimerSync();
        UpdateIntercomInteraction();

        if (gameEnded || isPaused) return;

        if (localPlayerIsDead) return;

        if (useAutoStart && !timerStarted && !autoStartTriggered)
        {
            autoStartTimer -= Time.deltaTime;
            if (autoStartTimer <= 0f)
            {
                StartGameTimer();
                autoStartTriggered = true;
            }
        }

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
            {
                StartGameTimer();
            }
        }

        if (timerStarted)
        {
            if (!stunTriggered && (gameDuration - remainingTime) >= stunTriggerTime)
                TriggerRandomStun();

            if (remainingTime <= 0f)
            {
                if (correctObjects.Count >= allCodeGames.Count)
                {
                    GameOver(true, "All codes debugged in time!");
                }
                else
                {
                    GameOver(false, "Time ran out! Not all codes were debugged.");
                }
            }

            if (localPlayerIsSurvivor && aliveSurvivors <= 0 && !gameEnded)
            {
                Debug.Log("[GameManager] All survivors dead detected in Update - ending game!");
                GameOver(false, "All survivors eliminated!");
            }
        }
    }

    void UpdateDeadSurvivorTimer()
    {
        if (deadSurvivorTimerText != null)
        {
            int minutes = Mathf.FloorToInt(remainingTime / 60f);
            int seconds = Mathf.FloorToInt(remainingTime % 60f);
            deadSurvivorTimerText.text = $"Game Time Remaining: {minutes:00}:{seconds:00}";
        }
    }

    void UpdateTimerSync()
    {
        if (timerStarted)
        {
            if (isMultiplayerMode && PhotonNetwork.IsConnected && gameStartTimestamp > 0)
            {
                double elapsedTime = PhotonNetwork.Time - gameStartTimestamp;
                remainingTime = gameDuration - (float)elapsedTime;
            }
            else if (!gameEnded && !isPaused)
            {
                remainingTime -= Time.deltaTime;
            }

            UpdateTimerDisplay();
        }
    }

    void UpdateIntercomInteraction()
    {
        if (player == null || gameEnded || isPaused || localPlayerIsDead)
        {
            if (intercomInteractButton != null)
                intercomInteractButton.gameObject.SetActive(false);
            return;
        }

        CodeCheckGame closest = null;
        float closestDistance = intercomCheckDistance;

        foreach (var intercom in allCodeGames)
        {
            if (intercom == null || intercom.IsSolved() || intercom.isOnCooldown) continue;

            float distance = Vector2.Distance(player.position, intercom.transform.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closest = intercom;
            }
        }

        if (closest != nearbyIntercom)
        {
            nearbyIntercom = closest;

            if (intercomInteractButton != null)
            {
                if (nearbyIntercom != null)
                {
                    intercomInteractButton.gameObject.SetActive(true);
                    if (intercomInteractText != null)
                        intercomInteractText.text = "Interact with Intercom";
                }
                else
                {
                    intercomInteractButton.gameObject.SetActive(false);
                }
            }
        }
    }

    public void InteractWithNearbyIntercom()
    {
        if (nearbyIntercom != null && !nearbyIntercom.IsSolved() && !nearbyIntercom.isOnCooldown)
        {
            nearbyIntercom.OpenCodePanel();
        }
    }

    IEnumerator ScrollToBottom()
    {
        yield return new WaitForEndOfFrame();
        if (gameplayLogScrollRect != null)
        {
            gameplayLogScrollRect.verticalNormalizedPosition = 0f;
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

            if (playerMovement != null)
            {
                playerMovement.RecordCodeDebugged(debugTime);
            }

            if (playerMovement != null)
            {
                if (playerMovement.healTimer > 0)
                    playerMovement.healTimer = Mathf.Max(0, playerMovement.healTimer - 5f);
                if (playerMovement.rushTimer > 0)
                    playerMovement.rushTimer = Mathf.Max(0, playerMovement.rushTimer - 5f);
            }
        }
    }

    public void RecordCodeInterrupted()
    {
        if (isHunterMode)
        {
            codesInterruptedCount++;

            if (hunterController != null)
            {
                hunterController.codesInterrupted++;
            }
        }
    }

    public void ApplyWrongCodePenalty()
    {
        if (isMultiplayerMode && PhotonNetwork.IsMasterClient)
        {
            gameStartTimestamp -= WRONG_CODE_TIME_PENALTY;
            ExitGames.Client.Photon.Hashtable props = new ExitGames.Client.Photon.Hashtable();
            props[GAME_START_TIME_KEY] = gameStartTimestamp;
            PhotonNetwork.CurrentRoom.SetCustomProperties(props);
        }
        else if (!isMultiplayerMode)
        {
            remainingTime = Mathf.Max(0, remainingTime - WRONG_CODE_TIME_PENALTY);
        }

        AddGameplayLog($"GAME TIME -{WRONG_CODE_TIME_PENALTY}!");

        Debug.Log($"Wrong code! Game time reduced by {WRONG_CODE_TIME_PENALTY} seconds");
    }

    public void LogHunterAbility(string hunterName, string abilityName)
    {
        AddGameplayLog($"{hunterName} used {abilityName}!");
    }

    public void LogSurvivorAbility(string survivorName, string abilityName)
    {
        AddGameplayLog($"{survivorName} used {abilityName}!");
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
        if (localPlayerIsDead) return;

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
            StartCoroutine(ShowQuickChatImageCoroutine(messageIndex));
            quickChatTimer = quickChatCooldown;

            if (isMultiplayerMode && PhotonNetwork.IsConnected)
            {
                string playerName = PhotonNetwork.LocalPlayer.NickName;
                if (string.IsNullOrEmpty(playerName))
                {
                    playerName = "Player " + PhotonNetwork.LocalPlayer.ActorNumber;
                }

                int characterIndex = 0;
                if (PhotonNetwork.LocalPlayer.CustomProperties.ContainsKey("CharacterIndex"))
                {
                    characterIndex = (int)PhotonNetwork.LocalPlayer.CustomProperties["CharacterIndex"];
                    Debug.Log($"[QuickChat] Sending RPC with character index: {characterIndex}");
                }
                else
                {
                    Debug.LogWarning("[QuickChat] CharacterIndex not found in player properties!");
                }

                photonView.RPC("RPC_ShowQuickChatToSurvivors", RpcTarget.All, messageIndex, playerName, characterIndex);
            }

            isQuickChatPanelOpen = false;
            if (quickChatPanel != null)
                quickChatPanel.SetActive(false);
        }
    }

    [PunRPC]
    void RPC_ShowQuickChatToSurvivors(int messageIndex, string playerName, int characterIndex)
    {
        if (localPlayerIsHunter)
        {
            return;
        }

        if (PhotonNetwork.LocalPlayer.NickName == playerName)
        {
            return;
        }

        if (messageIndex < quickChatImages.Length && quickChatImages[messageIndex] != null)
        {
            StartCoroutine(ShowQuickChatImageWithNameCoroutine(messageIndex, playerName, characterIndex));
        }
    }

    IEnumerator ShowQuickChatImageWithNameCoroutine(int imageIndex, string playerName, int characterIndex)
    {
        quickChatImages[imageIndex].SetActive(true);

        if (quickChatNamePanel != null && quickChatPlayerNameText != null)
        {
            quickChatPlayerNameText.text = playerName;

            if (quickChatCharacterImage != null && characterSprites != null)
            {
                Debug.Log($"[QuickChat] Setting character sprite. Index: {characterIndex}, Array Length: {characterSprites.Length}");

                if (characterIndex >= 0 && characterIndex < characterSprites.Length && characterSprites[characterIndex] != null)
                {
                    quickChatCharacterImage.sprite = characterSprites[characterIndex];
                    quickChatCharacterImage.gameObject.SetActive(true);
                    Debug.Log($"[QuickChat] Character sprite set successfully for index {characterIndex}");
                }
                else
                {
                    Debug.LogWarning($"[QuickChat] Invalid character index or null sprite. Index: {characterIndex}");
                }
            }

            quickChatNamePanel.SetActive(true);
        }

        yield return new WaitForSeconds(quickChatImageDuration);

        quickChatImages[imageIndex].SetActive(false);

        if (quickChatNamePanel != null)
        {
            quickChatNamePanel.SetActive(false);
        }

        if (quickChatCharacterImage != null)
        {
            quickChatCharacterImage.gameObject.SetActive(false);
        }
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        Debug.Log($"[GameManager] Player {otherPlayer.NickName} left the room");

        if (otherPlayer.CustomProperties.ContainsKey("PlayerRole"))
        {
            bool wasHunter = (bool)otherPlayer.CustomProperties["PlayerRole"];
            if (!wasHunter)
            {
                aliveSurvivors--;
                totalSurvivors--;
                Debug.Log($"[GameManager] Survivor left. Remaining: {aliveSurvivors}/{totalSurvivors}");

                if (aliveSurvivors <= 0 && !gameEnded)
                {
                    Debug.Log("[GameManager] All survivors eliminated/left - ending game!");
                    GameOver(false, "All survivors eliminated!");
                }
            }
        }

        if (isMultiplayerMode && MultiplayerUIManager.Instance != null)
        {
            MultiplayerUIManager.Instance.RemovePlayer(otherPlayer.ActorNumber);
        }
    }

    public void OnPlayerDeath(int playerId)
    {
        aliveSurvivors--;
        Debug.Log($"Survivor died. Alive survivors: {aliveSurvivors}");

        if (isMultiplayerMode && MultiplayerUIManager.Instance != null)
        {
            MultiplayerUIManager.Instance.SetPlayerAliveStatus(playerId, false);
        }

        if (isMultiplayerMode)
        {
            if (playerId == PhotonNetwork.LocalPlayer.ActorNumber)
            {
                localPlayerIsDead = true;
                ShowDeadSurvivorPanel();
            }
        }
        else
        {
            localPlayerIsDead = true;
            ShowDeadSurvivorPanel();
        }

        if (aliveSurvivors <= 0 && !gameEnded)
        {
            Debug.Log("[GameManager] ALL SURVIVORS DEAD - Game ending now!");
            GameOver(false, "All survivors eliminated!");
        }
    }

    void ShowDeadSurvivorPanel()
    {
        if (deadSurvivorPanel != null)
        {
            deadSurvivorPanel.SetActive(true);

            if (playerMovement != null)
            {
                playerMovement.enabled = false;
            }

            if (survivorUICanvas != null)
            {
                survivorUICanvas.SetActive(false);
            }

            if (intercomInteractButton != null)
            {
                intercomInteractButton.gameObject.SetActive(false);
            }

            if (quickChatButton != null)
            {
                quickChatButton.gameObject.SetActive(false);
            }

            if (pauseButton != null)
            {
                pauseButton.gameObject.SetActive(false);
            }

            Debug.Log("[GameManager] Dead survivor panel shown - Player can wait for game end OR press FindRoom button");
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

        if (isMultiplayerMode && PhotonNetwork.IsConnected && quickChatNamePanel != null && quickChatPlayerNameText != null)
        {
            string playerName = PhotonNetwork.LocalPlayer.NickName;
            if (string.IsNullOrEmpty(playerName))
            {
                playerName = "Player " + PhotonNetwork.LocalPlayer.ActorNumber;
            }

            quickChatPlayerNameText.text = playerName + " (You)";

            if (quickChatCharacterImage != null && characterSprites != null)
            {
                int characterIndex = 0;
                if (PhotonNetwork.LocalPlayer.CustomProperties.ContainsKey("CharacterIndex"))
                {
                    characterIndex = (int)PhotonNetwork.LocalPlayer.CustomProperties["CharacterIndex"];
                }

                if (characterIndex >= 0 && characterIndex < characterSprites.Length)
                {
                    quickChatCharacterImage.sprite = characterSprites[characterIndex];
                    quickChatCharacterImage.gameObject.SetActive(true);
                }
            }

            quickChatNamePanel.SetActive(true);
        }

        yield return new WaitForSeconds(quickChatImageDuration);

        quickChatImages[imageIndex].SetActive(false);

        if (quickChatNamePanel != null)
        {
            quickChatNamePanel.SetActive(false);
        }

        if (quickChatCharacterImage != null)
        {
            quickChatCharacterImage.gameObject.SetActive(false);
        }
    }

    public void ShowIndividualCorrectImage()
    {
        if (correctAnswerImages != null && correctAnswerImages.Length > 0)
        {
            StartCoroutine(ShowIndividualCorrectImageCoroutine());
        }
    }

    public void ShowIndividualWrongImage()
    {
        if (wrongAnswerImage != null)
        {
            StartCoroutine(ShowIndividualWrongImageCoroutine());
        }
    }

    IEnumerator ShowIndividualCorrectImageCoroutine()
    {
        if (correctAnswerImages != null && correctAnswerImages.Length > 0)
        {
            int randomIndex = Random.Range(0, correctAnswerImages.Length);
            if (correctAnswerImages[randomIndex] != null)
            {
                correctAnswerImages[randomIndex].SetActive(true);
                yield return new WaitForSeconds(2f);
                correctAnswerImages[randomIndex].SetActive(false);
            }
        }
    }

    IEnumerator ShowIndividualWrongImageCoroutine()
    {
        if (wrongAnswerImage != null)
        {
            wrongAnswerImage.SetActive(true);
            yield return new WaitForSeconds(2f);
            wrongAnswerImage.SetActive(false);
        }
    }

    IEnumerator HideWarningAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (quickChatWarningText != null)
            quickChatWarningText.gameObject.SetActive(false);
    }

    public void TakeDamage(bool hunterTampered = false)
    {
        if (localPlayerIsHunter) return;

        if (hunterTampered) return;

        currentHP--;
        UpdateHeartDisplay();

        if (currentHP <= 0)
        {
            if (isMultiplayerMode)
            {
                OnPlayerDeath(PhotonNetwork.LocalPlayer.ActorNumber);
            }
            else
            {
                GameOver(false, "You were eliminated!");
            }
        }
    }

    public void HealPlayer(int amount)
    {
        if (localPlayerIsHunter) return;

        currentHP = Mathf.Min(currentHP + amount, maxHP);
        UpdateHeartDisplay();
        Debug.Log($"Healed! Current HP: {currentHP}/{maxHP}");
    }

    void UpdateHeartDisplay()
    {
        if (localPlayerIsHunter) return;

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

        Invoke(nameof(ApplyStun), 3f);
    }

    void ApplyStun()
    {
        if (player == null) return;

        PlayerMovement pm = player.GetComponent<PlayerMovement>();
        if (pm != null)
            pm.LockMovement();

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
        {
            GameOver(true, "All codes debugged! Survivors escaped!");
        }
    }

    public void UnregisterCorrectObject(CodeCheckGame obj)
    {
        if (correctObjects.Contains(obj))
        {
            correctObjects.Remove(obj);
            UpdateProgressText();
        }
    }

    public void UpdateProgressText()
    {
        if (progressText != null)
            progressText.text = $"{correctObjects.Count}/{allCodeGames.Count}";
    }

    void GameOver(bool won, string reason)
    {
        if (gameEnded) return;

        gameEnded = true;
        gameEndReason = reason;

        Debug.Log($"Game Over called. Won = {won}, Reason = {reason}");

        if (isMultiplayerMode && PhotonNetwork.IsMasterClient)
        {
            photonView.RPC("RPC_GameOver", RpcTarget.All, won, reason);
        }
        else if (!isMultiplayerMode)
        {
            DisplayGameOver(won, reason);
        }

        StartCoroutine(CleanupGameResources());
    }

    [PunRPC]
    void RPC_GameOver(bool won, string reason)
    {
        DisplayGameOver(won, reason);
    }

    void DisplayGameOver(bool won, string reason)
    {
        if (recordGameProgress)
        {
            ShowPlayerRecords(won);
            Debug.Log("[GameManager] Player stayed until game end - progress recorded");
        }
        else
        {
            Debug.Log("[GameManager] Player left early - no progress recorded");
        }

        endPanel.SetActive(true);

        if (localPlayerIsHunter)
        {
            endText.text = won ? "Survivors Escaped!" : "Hunter Victory!";
        }
        else
        {
            endText.text = won ? "You Escaped!" : "Game Over";
        }

        if (endReasonText != null)
            endReasonText.text = reason;

        timerText.gameObject.SetActive(false);

        if (deadSurvivorPanel != null)
            deadSurvivorPanel.SetActive(false);

        if (winImage != null)
        {
            bool showWin = (localPlayerIsSurvivor && won) || (localPlayerIsHunter && !won);
            winImage.SetActive(showWin);
        }

        if (loseImage != null)
        {
            bool showLose = (localPlayerIsSurvivor && !won) || (localPlayerIsHunter && won);
            loseImage.SetActive(showLose);
        }

        if (lobbyButton != null)
            lobbyButton.gameObject.SetActive(true);

        if (onlineExitButton != null)
            onlineExitButton.gameObject.SetActive(true);

        if (isMultiplayerMode)
        {
            if (playAgainButton != null)
                playAgainButton.gameObject.SetActive(false);
            if (exitButton != null)
                exitButton.gameObject.SetActive(false);
        }
    }

    private void ShowPlayerRecords(bool survivorsWon)
    {
        if (recordsPanel != null && recordsText != null)
        {
            recordsPanel.SetActive(true);

            string recordsString = "";

            bool thisPlayerWon;
            if (isHunterMode)
            {
                thisPlayerWon = !survivorsWon;
            }
            else
            {
                thisPlayerWon = survivorsWon;
            }

            int baseXP = thisPlayerWon ? 10 : 2;
            int bonusXP = 0;

            if (isHunterMode)
            {
                bonusXP += codesInterruptedCount;

                if (hunterController != null)
                {
                    bonusXP += hunterController.survivorsStunned;
                }

                int totalXP = baseXP + bonusXP;

                recordsString = $"<b>HUNTER MODE</b>\n" +
                    $"{(thisPlayerWon ? "VICTORY" : "DEFEAT")}\n\n" +
                    $"<b>Base XP:</b> {baseXP}\n" +
                    $"<b>Bonus XP:</b> +{bonusXP}\n" +
                    $"<b>Total XP Gained:</b> {totalXP}\n\n" +
                    $"<b>Stats:</b>\n" +
                    $"Codes Interrupted: {codesInterruptedCount}\n" +
                    $"Survivors Stunned: {(hunterController != null ? hunterController.survivorsStunned : 0)}";

                RecordGameEndXP(totalXP, thisPlayerWon, bonusXP);
            }
            else
            {
                bonusXP += codesDebuggedCount * 2;

                float avgTime = 0f;
                if (playerMovement != null)
                {
                    avgTime = playerMovement.GetAverageDebuggingTime();

                    if (avgTime > 0 && avgTime < 30f)
                    {
                        bonusXP += 3;
                    }
                }

                int totalXP = baseXP + bonusXP;

                recordsString = $"<b>SURVIVOR MODE</b>\n" +
                    $"{(thisPlayerWon ? "VICTORY" : "DEFEAT")}\n\n" +
                    $"<b>Base XP:</b> {baseXP}\n" +
                    $"<b>Bonus XP:</b> +{bonusXP}\n" +
                    $"<b>Total XP Gained:</b> {totalXP}\n\n" +
                    $"<b>Stats:</b>\n" +
                    $"Codes Debugged: {codesDebuggedCount}\n" +
                    $"Avg Debug Time: {avgTime:F1}s";

                RecordGameEndXP(totalXP, thisPlayerWon, bonusXP);
            }

            recordsText.text = recordsString;
        }
    }

    private void RecordGameEndXP(int totalXP, bool won, int bonusXP)
    {
        if (UserProgressManager.Instance != null)
        {
            int baseXP = won ? 10 : 2;

            UserProgressManager.Instance.AddXP(baseXP, won, bonusXP);

            Debug.Log($"[GameManager] Game ended. Result: {(won ? "WON" : "LOST")}. Total XP: {totalXP} (Base: {baseXP} + Bonus: {bonusXP})");
        }
        else
        {
            Debug.LogWarning("UserProgressManager not found. XP not recorded.");
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