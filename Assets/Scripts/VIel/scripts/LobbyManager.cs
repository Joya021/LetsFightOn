using System.Collections;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine.UI;
using Hashtable = ExitGames.Client.Photon.Hashtable;
using UnityEngine.SceneManagement;

/// <summary>
/// Manages the lobby room waiting area where players wait before starting the game
/// FIXED: Properly handles grace period lifecycle and prevents infinite timer loops
/// </summary>
public class LobbyManager : MonoBehaviourPunCallbacks
{
    [Header("Lobby UI")]
    public Text lobbyRoomNameText;
    public GameObject[] playerSlots;
    public GameObject playerSlotsParent;

    [Header("Action Buttons")]
    public Button readyButton;
    public Button leaveRoomButton;
    public Button deleteRoomButton;

    [Header("Grace Period UI")]
    public GameObject gracePeriodPanel;
    public Text gracePeriodTimerText;
    public Text gracePeriodInfoText;

    [Header("Settings")]
    public int minPlayers = 2;
    public float gracePeriodDuration = 20f;

    [Header("Scene Names")]
    public string findRoomSceneName = "FindRoom";
    public string characterSelectionSceneName = "CSCharPickPage";

    [Header("Loading Panel (Optional)")]
    public GameObject loadingPanel;
    public Text loadingText;

    private bool isProcessingAction = false;
    private bool isLeavingRoom = false;
    private const string GAME_END_TIME_KEY = "GameEndTime";
    private const string GAME_IN_PROGRESS_KEY = "GameInProgress";
    private const string PLAYER_CURRENT_SCENE_KEY = "PlayerCurrentScene";
    private const string ORIGINAL_HOST_ID_KEY = "OriginalHostID";
    private const string GRACE_PERIOD_ACTIVE_KEY = "GracePeriodActive";

    private double gracePeriodEndTime = 0;
    private bool gracePeriodActive = false;
    private int originalHostID = -1;
    private bool hasCheckedGracePeriod = false;

    void Start()
    {
        Debug.Log("[LobbyManager] Lobby scene started");

        PhotonNetwork.AutomaticallySyncScene = false;
        HideLoadingPanel();

        MarkPlayerInLobby();

        if (!PhotonNetwork.InRoom)
        {
            Debug.LogError("[LobbyManager] Not in a room! Redirecting to FindRoom");
            SceneManager.LoadScene(findRoomSceneName);
            return;
        }

        InitializeUI();

        // Small delay before checking grace period to ensure room props are synced
        StartCoroutine(DelayedGracePeriodCheck());
    }

    IEnumerator DelayedGracePeriodCheck()
    {
        yield return new WaitForSeconds(0.3f);

        // CRITICAL: Reopen the room when returning to lobby
        if (PhotonNetwork.IsMasterClient && PhotonNetwork.InRoom)
        {
            ReopenRoomForNewPlayers();
        }

        CheckGracePeriodStatus();
        UpdateLobbyUI();
    }

    // NEW: Reopen room when returning to lobby after game
    void ReopenRoomForNewPlayers()
    {
        if (!PhotonNetwork.IsMasterClient || !PhotonNetwork.InRoom) return;

        var room = PhotonNetwork.CurrentRoom;

        // Check if game is not in progress
        bool gameInProgress = false;
        if (room.CustomProperties.ContainsKey(GAME_IN_PROGRESS_KEY))
        {
            gameInProgress = (bool)room.CustomProperties[GAME_IN_PROGRESS_KEY];
        }

        // If game is not in progress, reopen the room
        if (!gameInProgress)
        {
            Debug.Log("[LobbyManager] Reopening room for new players");

            room.IsOpen = true;
            room.IsVisible = true;

            Hashtable roomProps = new Hashtable();
            roomProps["IsOpen"] = true;
            roomProps["IsVisible"] = true;
            room.SetCustomProperties(roomProps);
        }
    }

    void OnDestroy()
    {
        Debug.Log("[LobbyManager] Lobby manager destroyed");

        if (readyButton != null)
            readyButton.onClick.RemoveAllListeners();
        if (leaveRoomButton != null)
            leaveRoomButton.onClick.RemoveAllListeners();
        if (deleteRoomButton != null)
            deleteRoomButton.onClick.RemoveAllListeners();
    }

    void MarkPlayerInLobby()
    {
        if (PhotonNetwork.LocalPlayer != null && PhotonNetwork.InRoom)
        {
            Hashtable sceneProps = new Hashtable();
            sceneProps[PLAYER_CURRENT_SCENE_KEY] = "Lobby";
            PhotonNetwork.LocalPlayer.SetCustomProperties(sceneProps);
            Debug.Log("[LobbyManager] Marked player as in Lobby scene");
        }
    }

    void CheckGracePeriodStatus()
    {
        if (!PhotonNetwork.InRoom || hasCheckedGracePeriod) return;

        var room = PhotonNetwork.CurrentRoom;

        // Check if grace period is explicitly active
        if (room.CustomProperties.ContainsKey(GRACE_PERIOD_ACTIVE_KEY) &&
            (bool)room.CustomProperties[GRACE_PERIOD_ACTIVE_KEY])
        {
            if (room.CustomProperties.ContainsKey(GAME_END_TIME_KEY))
            {
                double gameEndTime = (double)room.CustomProperties[GAME_END_TIME_KEY];
                gracePeriodEndTime = gameEndTime + gracePeriodDuration;
                gracePeriodActive = true;

                if (room.CustomProperties.ContainsKey(ORIGINAL_HOST_ID_KEY))
                {
                    originalHostID = (int)room.CustomProperties[ORIGINAL_HOST_ID_KEY];
                }

                Debug.Log($"[LobbyManager] Grace period already active. Ends at: {gracePeriodEndTime}, Original host: {originalHostID}");
            }
        }
        // Only start NEW grace period if no grace period is active
        else if (PhotonNetwork.IsMasterClient)
        {
            // Check if game just ended (not in progress but has the flag)
            if (room.CustomProperties.ContainsKey(GAME_IN_PROGRESS_KEY))
            {
                bool gameInProgress = (bool)room.CustomProperties[GAME_IN_PROGRESS_KEY];

                if (!gameInProgress && !room.CustomProperties.ContainsKey(GAME_END_TIME_KEY))
                {
                    // Game ended but no grace period started yet - start it now
                    Debug.Log("[LobbyManager] First player in lobby after game end - starting grace period");
                    StartGracePeriod();
                }
            }
        }

        hasCheckedGracePeriod = true;
    }

    void StartGracePeriod()
    {
        if (!PhotonNetwork.IsMasterClient || gracePeriodActive) return;

        gracePeriodEndTime = PhotonNetwork.Time + gracePeriodDuration;
        gracePeriodActive = true;

        // Get or set original host ID
        if (PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey(ORIGINAL_HOST_ID_KEY))
        {
            originalHostID = (int)PhotonNetwork.CurrentRoom.CustomProperties[ORIGINAL_HOST_ID_KEY];
        }
        else
        {
            originalHostID = PhotonNetwork.MasterClient.ActorNumber;
        }

        Hashtable props = new Hashtable();
        props[GAME_END_TIME_KEY] = PhotonNetwork.Time;
        props[ORIGINAL_HOST_ID_KEY] = originalHostID;
        props[GAME_IN_PROGRESS_KEY] = false;
        props[GRACE_PERIOD_ACTIVE_KEY] = true;
        PhotonNetwork.CurrentRoom.SetCustomProperties(props);

        Debug.Log($"[LobbyManager] Grace period started. Original host ID: {originalHostID}");
    }

    void Update()
    {
        if (gracePeriodActive && PhotonNetwork.InRoom)
        {
            UpdateGracePeriodUI();
            CheckGracePeriodExpiry();
        }
    }

    void UpdateGracePeriodUI()
    {
        double remainingTime = gracePeriodEndTime - PhotonNetwork.Time;

        if (gracePeriodPanel != null)
            gracePeriodPanel.SetActive(true);

        if (gracePeriodTimerText != null)
        {
            if (remainingTime > 0)
                gracePeriodTimerText.text = $"Host Transfer: {Mathf.CeilToInt((float)remainingTime)}s";
            else
                gracePeriodTimerText.text = "Processing...";
        }

        if (gracePeriodInfoText != null)
        {
            gracePeriodInfoText.text = "Waiting for original host to return to lobby...";
        }
    }

    void CheckGracePeriodExpiry()
    {
        if (!gracePeriodActive || !PhotonNetwork.InRoom) return;

        double remainingTime = gracePeriodEndTime - PhotonNetwork.Time;

        if (remainingTime <= 0)
        {
            // Grace period expired
            gracePeriodActive = false;

            if (gracePeriodPanel != null)
                gracePeriodPanel.SetActive(false);

            bool originalHostInLobby = IsPlayerInLobby(originalHostID);

            Debug.Log($"[LobbyManager] Grace period expired. Original host in lobby: {originalHostInLobby}");

            if (!originalHostInLobby)
            {
                // Original host didn't return
                if (PhotonNetwork.LocalPlayer.ActorNumber == originalHostID)
                {
                    // We are the late host - kick ourselves
                    ShowLoadingPanel("You took too long! Redirecting to FindRoom...");
                    StartCoroutine(KickSelfToFindRoom());
                }
                else if (PhotonNetwork.IsMasterClient)
                {
                    // Clear grace period and allow new host to take over
                    ClearGracePeriod();
                }
            }
            else
            {
                // Original host returned in time
                Debug.Log("[LobbyManager] Original host returned in time!");

                if (PhotonNetwork.IsMasterClient)
                {
                    ClearGracePeriod();
                }
            }
        }
    }

    void ClearGracePeriod()
    {
        if (!PhotonNetwork.IsMasterClient) return;

        Debug.Log("[LobbyManager] Clearing grace period");

        Hashtable clearProps = new Hashtable();
        clearProps[GAME_END_TIME_KEY] = null;
        clearProps[ORIGINAL_HOST_ID_KEY] = null;
        clearProps[GRACE_PERIOD_ACTIVE_KEY] = false;
        PhotonNetwork.CurrentRoom.SetCustomProperties(clearProps);

        gracePeriodActive = false;

        // Force UI update
        UpdateLobbyUI();
    }

    bool IsPlayerInLobby(int actorNumber)
    {
        Player player = PhotonNetwork.CurrentRoom.GetPlayer(actorNumber);
        if (player == null) return false;

        if (player.CustomProperties.ContainsKey(PLAYER_CURRENT_SCENE_KEY))
        {
            string scene = (string)player.CustomProperties[PLAYER_CURRENT_SCENE_KEY];
            return scene == "Lobby";
        }

        return false;
    }

    IEnumerator KickSelfToFindRoom()
    {
        isLeavingRoom = true;
        yield return new WaitForSeconds(2f);

        ClearPlayerProperties();

        if (PhotonNetwork.InRoom)
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

        yield return new WaitForSeconds(0.5f);
        SceneManager.LoadScene(findRoomSceneName);
    }

    void InitializeUI()
    {
        if (gracePeriodPanel != null)
            gracePeriodPanel.SetActive(false);

        if (readyButton != null)
        {
            readyButton.gameObject.SetActive(false);
            readyButton.onClick.RemoveAllListeners();
            readyButton.onClick.AddListener(OnReadyButtonClicked);
        }

        if (leaveRoomButton != null)
        {
            leaveRoomButton.onClick.RemoveAllListeners();
            leaveRoomButton.onClick.AddListener(OnLeaveRoomClicked);
        }

        if (deleteRoomButton != null)
        {
            deleteRoomButton.gameObject.SetActive(false);
            deleteRoomButton.onClick.RemoveAllListeners();
            deleteRoomButton.onClick.AddListener(OnDeleteRoomClicked);
        }
    }

    void UpdateLobbyUI()
    {
        if (!PhotonNetwork.InRoom)
            return;

        if (lobbyRoomNameText != null)
            lobbyRoomNameText.text = PhotonNetwork.CurrentRoom.Name;

        UpdatePlayerSlots();
        UpdateButtonVisibility();
    }

    void UpdatePlayerSlots()
    {
        if (playerSlots == null || playerSlots.Length == 0)
            return;

        Player[] allPlayers = PhotonNetwork.PlayerList;
        System.Collections.Generic.List<Player> playersInLobby = new System.Collections.Generic.List<Player>();

        foreach (Player p in allPlayers)
        {
            if (IsPlayerInLobby(p.ActorNumber))
            {
                playersInLobby.Add(p);
            }
        }

        Debug.Log($"[LobbyManager] Displaying {playersInLobby.Count} players in lobby (out of {allPlayers.Length} total)");

        for (int i = 0; i < playerSlots.Length; i++)
        {
            if (playerSlots[i] == null) continue;

            if (i < playersInLobby.Count)
            {
                playerSlots[i].SetActive(true);

                // FIXED: Get UserInfoDisplay and call SetPlayerInfo
                UserInfoDisplay userInfo = playerSlots[i].GetComponent<UserInfoDisplay>();
                if (userInfo != null)
                {
                    userInfo.SetPlayerInfo(playersInLobby[i].NickName);
                    userInfo.SetHostStatus(playersInLobby[i].IsMasterClient);
                    Debug.Log($"[LobbyManager] Slot {i}: Set player name to '{playersInLobby[i].NickName}'");
                }
                else
                {
                    Debug.LogWarning($"[LobbyManager] Slot {i} has no UserInfoDisplay component!");
                }
            }
            else
            {
                playerSlots[i].SetActive(false);
            }
        }

        if (playerSlotsParent != null)
        {
            HorizontalLayoutGroup layout = playerSlotsParent.GetComponent<HorizontalLayoutGroup>();
            if (layout != null)
            {
                layout.spacing = playersInLobby.Count == 2 ? 80f : 40f;
            }
        }
    }

    void UpdateButtonVisibility()
    {
        if (!PhotonNetwork.InRoom)
            return;

        if (deleteRoomButton != null)
        {
            deleteRoomButton.gameObject.SetActive(PhotonNetwork.IsMasterClient);
        }

        if (readyButton != null)
        {
            bool isHost = PhotonNetwork.IsMasterClient;

            int playersInLobby = 0;
            foreach (Player p in PhotonNetwork.PlayerList)
            {
                if (IsPlayerInLobby(p.ActorNumber) || !p.CustomProperties.ContainsKey(PLAYER_CURRENT_SCENE_KEY))
                {
                    playersInLobby++;
                }
            }

            bool enoughPlayers = playersInLobby >= minPlayers;
            bool gracePeriodOver = !gracePeriodActive;

            // Also check room property to be extra sure
            if (PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey(GRACE_PERIOD_ACTIVE_KEY))
            {
                gracePeriodOver = !(bool)PhotonNetwork.CurrentRoom.CustomProperties[GRACE_PERIOD_ACTIVE_KEY];
            }

            bool shouldShow = isHost && enoughPlayers && gracePeriodOver;
            readyButton.gameObject.SetActive(shouldShow);

            Debug.Log($"[LobbyManager] Ready button: Host={isHost}, Players={playersInLobby}/{minPlayers}, GracePeriod={gracePeriodActive}, Visible={shouldShow}");
        }
    }

    void OnReadyButtonClicked()
    {
        if (!PhotonNetwork.IsMasterClient || isProcessingAction || gracePeriodActive)
            return;

        Debug.Log("[LobbyManager] Host starting character selection");
        isProcessingAction = true;

        PhotonNetwork.CurrentRoom.IsOpen = false;

        Hashtable gameProps = new Hashtable();
        gameProps[GAME_IN_PROGRESS_KEY] = true;
        gameProps[GAME_END_TIME_KEY] = null;
        gameProps[ORIGINAL_HOST_ID_KEY] = PhotonNetwork.LocalPlayer.ActorNumber;
        gameProps[GRACE_PERIOD_ACTIVE_KEY] = false;
        PhotonNetwork.CurrentRoom.SetCustomProperties(gameProps);

        PhotonNetwork.AutomaticallySyncScene = true;

        Hashtable props = new Hashtable();
        props["StartCharacterSelection"] = true;
        PhotonNetwork.CurrentRoom.SetCustomProperties(props);

        PhotonNetwork.LoadLevel(characterSelectionSceneName);
    }

    void OnLeaveRoomClicked()
    {
        if (isProcessingAction || isLeavingRoom)
        {
            Debug.LogWarning("[LobbyManager] Already processing an action");
            return;
        }

        if (!PhotonNetwork.IsConnected)
        {
            Debug.LogWarning("[LobbyManager] Not connected to Photon, redirecting to FindRoom");
            ShowLoadingPanel("Redirecting to room list...");
            StartCoroutine(DelayedSceneLoad(findRoomSceneName, 1f));
            return;
        }

        if (!PhotonNetwork.InRoom)
        {
            Debug.LogWarning("[LobbyManager] Not in room, redirecting to FindRoom");
            ShowLoadingPanel("Redirecting to room list...");
            StartCoroutine(DelayedSceneLoad(findRoomSceneName, 1f));
            return;
        }

        Debug.Log("[LobbyManager] Player leaving room");
        isProcessingAction = true;
        isLeavingRoom = true;

        if (leaveRoomButton != null)
            leaveRoomButton.interactable = false;

        ShowLoadingPanel("Leaving room...");

        // Clear properties BEFORE leaving
        ClearPlayerProperties();

        StartCoroutine(WaitAndLeaveRoom());
    }

    IEnumerator WaitAndLeaveRoom()
    {
        // Small delay to let property clearing propagate
        yield return new WaitForSeconds(0.2f);

        if (!PhotonNetwork.IsConnected)
        {
            Debug.LogWarning("[LobbyManager] Not connected, redirecting immediately");
            ShowLoadingPanel("Redirecting to room list...");
            yield return new WaitForSeconds(0.5f);
            SceneManager.LoadScene(findRoomSceneName);
            yield break;
        }

        if (!PhotonNetwork.InRoom)
        {
            Debug.Log("[LobbyManager] Already left room, redirecting");
            ShowLoadingPanel("Redirecting to room list...");
            yield return new WaitForSeconds(0.5f);
            SceneManager.LoadScene(findRoomSceneName);
            yield break;
        }

        ShowLoadingPanel("Leaving room...");

        try
        {
            PhotonNetwork.LeaveRoom();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[LobbyManager] Error leaving room: {e.Message}");
            StartCoroutine(HandleLeaveError());
        }
    }

    IEnumerator HandleLeaveError()
    {
        ShowLoadingPanel("Error leaving room. Redirecting...");
        yield return new WaitForSeconds(2f);
        SceneManager.LoadScene(findRoomSceneName);
    }

    void OnDeleteRoomClicked()
    {
        if (isProcessingAction || isLeavingRoom || !PhotonNetwork.InRoom || !PhotonNetwork.IsMasterClient)
            return;

        Debug.Log("[LobbyManager] Host deleting room");
        isProcessingAction = true;
        isLeavingRoom = true;

        if (deleteRoomButton != null)
            deleteRoomButton.interactable = false;

        ShowLoadingPanel("Deleting room...");
        StartCoroutine(WaitAndCloseRoom());
    }

    IEnumerator WaitAndCloseRoom()
    {
        float timeout = 10f;
        float elapsed = 0f;

        while (!PhotonNetwork.IsConnectedAndReady && elapsed < timeout)
        {
            ShowLoadingPanel("Waiting for connection...");
            elapsed += Time.deltaTime;
            yield return new WaitForSeconds(0.1f);
        }

        if (!PhotonNetwork.IsConnectedAndReady || !PhotonNetwork.InRoom || !PhotonNetwork.IsMasterClient)
        {
            ShowLoadingPanel("Error. Redirecting...");
            yield return new WaitForSeconds(1f);
            SceneManager.LoadScene(findRoomSceneName);
            yield break;
        }

        ShowLoadingPanel("Deleting room...");

        Hashtable roomProps = new Hashtable();
        roomProps["IsOpen"] = false;
        roomProps["IsVisible"] = false;
        PhotonNetwork.CurrentRoom.SetCustomProperties(roomProps);

        PhotonNetwork.CurrentRoom.IsOpen = false;
        PhotonNetwork.CurrentRoom.IsVisible = false;

        ClearPlayerProperties();

        yield return new WaitForSeconds(0.3f);
        PhotonNetwork.LeaveRoom();
    }

    void ClearPlayerProperties()
    {
        if (PhotonNetwork.LocalPlayer != null && !isLeavingRoom)
        {
            try
            {
                Hashtable clearProps = new Hashtable();
                clearProps["PlayerRole"] = null;
                clearProps["PlayerCharacter"] = null;
                clearProps["PlayerLockedIn"] = null;
                clearProps["CharacterIndex"] = null;
                clearProps["IsReady"] = null;
                clearProps[PLAYER_CURRENT_SCENE_KEY] = null;
                PhotonNetwork.LocalPlayer.SetCustomProperties(clearProps);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[LobbyManager] Error clearing properties: {e.Message}");
            }
        }
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        Debug.Log($"[LobbyManager] Player {newPlayer.NickName} entered room");
        UpdateLobbyUI();
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        Debug.Log($"[LobbyManager] Player {otherPlayer.NickName} left room");
        UpdateLobbyUI();
    }

    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        Debug.Log($"[LobbyManager] Master client switched to {newMasterClient.NickName}");
        UpdateLobbyUI();
    }

    public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
    {
        if (changedProps.ContainsKey(PLAYER_CURRENT_SCENE_KEY))
        {
            Debug.Log($"[LobbyManager] Player {targetPlayer.NickName} scene changed");
            UpdateLobbyUI();
        }
    }

    public override void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged)
    {
        if (propertiesThatChanged.ContainsKey("StartCharacterSelection"))
        {
            bool start = (bool)propertiesThatChanged["StartCharacterSelection"];
            if (start && !PhotonNetwork.IsMasterClient && PhotonNetwork.InRoom)
            {
                SceneManager.LoadScene(characterSelectionSceneName);
            }
        }

        // Check if grace period status changed
        if (propertiesThatChanged.ContainsKey(GRACE_PERIOD_ACTIVE_KEY))
        {
            bool gpActive = (bool)propertiesThatChanged[GRACE_PERIOD_ACTIVE_KEY];

            if (!gpActive && gracePeriodActive)
            {
                // Grace period was cleared by master
                gracePeriodActive = false;
                if (gracePeriodPanel != null)
                    gracePeriodPanel.SetActive(false);

                Debug.Log("[LobbyManager] Grace period cleared by master client");
            }
        }

        UpdateLobbyUI();
    }

    public override void OnLeftRoom()
    {
        Debug.Log("[LobbyManager] Successfully left room");
        isProcessingAction = false;
        isLeavingRoom = false;
        PhotonNetwork.AutomaticallySyncScene = false;
        ShowLoadingPanel("Redirecting to room list...");
        StartCoroutine(DelayedSceneLoad(findRoomSceneName, 0.5f));
    }

    void ShowLoadingPanel(string message)
    {
        if (loadingPanel != null)
            loadingPanel.SetActive(true);
        if (loadingText != null)
            loadingText.text = message;
    }

    void HideLoadingPanel()
    {
        if (loadingPanel != null)
            loadingPanel.SetActive(false);
    }

    IEnumerator DelayedSceneLoad(string sceneName, float delay)
    {
        yield return new WaitForSeconds(delay);
        SceneManager.LoadScene(sceneName);
    }
}