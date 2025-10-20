using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine.UI;
using Hashtable = ExitGames.Client.Photon.Hashtable;
using UnityEngine.SceneManagement;

/// <summary>
/// Manages room creation and room list display in the FindRoom scene
/// FIXED: Shows ALL created rooms at all times, even if game is in progress
/// Allows late players to join rooms that are waiting in lobby
/// NEW: Back button that maintains connection for seamless return
/// </summary>
public class FindRoomManager : MonoBehaviourPunCallbacks
{
    [Header("Room Creation")]
    public InputField createRoomInput;
    public Button createRoomButton;

    [Header("Room List Display")]
    public GameObject roomListContent;
    public GameObject roomButtonPrefab;

    [Header("Navigation")]
    public Button backButton;

    [Header("Scene Names")]
    public string lobbySceneName = "Lobby";
    public string loginSceneName = "Login"; // Set your login scene name here

    [Header("Loading Panel (Optional)")]
    public GameObject loadingPanel;
    public Text loadingText;

    private Dictionary<string, RoomInfo> cachedRoomList = new Dictionary<string, RoomInfo>();
    private Dictionary<string, GameObject> roomButtons = new Dictionary<string, GameObject>();
    private bool isNavigatingAway = false;

    void Start()
    {
        Debug.Log("[FindRoomManager] FindRoom scene started");

        HideLoadingPanel();

        // CRITICAL: Ensure we're not in a room when on FindRoom scene
        if (PhotonNetwork.InRoom)
        {
            Debug.Log("[FindRoomManager] Still in room - leaving it");
            ShowLoadingPanel("Leaving previous room...");
            PhotonNetwork.LeaveRoom();
            return;
        }

        // CRITICAL: Disable auto scene sync
        PhotonNetwork.AutomaticallySyncScene = false;

        // Setup UI
        if (createRoomButton != null)
        {
            createRoomButton.onClick.RemoveAllListeners();
            createRoomButton.onClick.AddListener(CreateRoom);
        }

        if (backButton != null)
        {
            backButton.onClick.RemoveAllListeners();
            backButton.onClick.AddListener(OnBackButtonClicked);
        }

        // NEW: Check if we're returning from back button (already in lobby)
        if (PhotonNetwork.InLobby)
        {
            Debug.Log("[FindRoomManager] Already in lobby - refreshing room list");
            HideLoadingPanel();

            // Force a room list refresh after a short delay
            StartCoroutine(RefreshRoomListOnReturn());
        }
        else
        {
            // Join lobby to see rooms (first time)
            StartCoroutine(EnsureInLobby());
        }
    }

    // NEW: Force refresh room list when returning from back button
    IEnumerator RefreshRoomListOnReturn()
    {
        yield return new WaitForSeconds(0.3f);

        Debug.Log("[FindRoomManager] Forcing room list update on return");

        // Update UI with cached rooms first
        UpdateRoomListUI();

        // Force a fresh room list update from server
        yield return new WaitForSeconds(0.5f);

        if (PhotonNetwork.InLobby)
        {
            Debug.Log("[FindRoomManager] Requesting fresh room list from server");
            // Leave and rejoin lobby to force room list update
            PhotonNetwork.LeaveLobby();

            yield return new WaitForSeconds(0.3f);

            if (PhotonNetwork.IsConnectedAndReady && !PhotonNetwork.InLobby)
            {
                PhotonNetwork.JoinLobby();
            }
        }
    }

    void OnDestroy()
    {
        Debug.Log("[FindRoomManager] FindRoom manager destroyed");

        if (createRoomButton != null)
            createRoomButton.onClick.RemoveAllListeners();

        if (backButton != null)
            backButton.onClick.RemoveAllListeners();
    }

    // NEW: Back button handler - stays connected to Photon
    void OnBackButtonClicked()
    {
        if (isNavigatingAway)
        {
            Debug.LogWarning("[FindRoomManager] Already navigating away");
            return;
        }

        Debug.Log("[FindRoomManager] Back button pressed - returning to login");
        isNavigatingAway = true;

        // Show loading
        ShowLoadingPanel("Returning to login...");

        // IMPORTANT: Stay connected and in lobby - just change scene
        // This way rooms are still cached when we return
        StartCoroutine(NavigateToLogin());
    }

    IEnumerator NavigateToLogin()
    {
        // Small delay for smooth transition
        yield return new WaitForSeconds(0.3f);

        // CRITICAL: Don't leave lobby or disconnect - just change scene
        // This preserves the connection and room list
        SceneManager.LoadScene(loginSceneName);
    }

    IEnumerator EnsureInLobby()
    {
        ShowLoadingPanel("Connecting to lobby...");

        float connectionTimeout = 10f;
        float elapsed = 0f;

        while (!PhotonNetwork.IsConnectedAndReady && elapsed < connectionTimeout)
        {
            elapsed += Time.deltaTime;
            yield return new WaitForSeconds(0.1f);
        }

        if (!PhotonNetwork.IsConnectedAndReady)
        {
            Debug.LogWarning("[FindRoomManager] Connection timeout");
            ShowLoadingPanel("Connection failed. Retrying...");
            yield return new WaitForSeconds(2f);
            StartCoroutine(EnsureInLobby());
            yield break;
        }

        if (PhotonNetwork.InRoom)
        {
            ShowLoadingPanel("Leaving previous room...");
            PhotonNetwork.LeaveRoom();

            float leaveTimeout = 5f;
            elapsed = 0f;
            while (PhotonNetwork.InRoom && elapsed < leaveTimeout)
            {
                elapsed += Time.deltaTime;
                yield return new WaitForSeconds(0.1f);
            }
        }

        yield return new WaitForSeconds(0.5f);

        bool joinLobbyError = false;
        string errorMessage = "";

        if (!PhotonNetwork.InLobby && PhotonNetwork.IsConnectedAndReady && !PhotonNetwork.InRoom)
        {
            Debug.Log("[FindRoomManager] Joining lobby");
            ShowLoadingPanel("Joining lobby...");

            try
            {
                PhotonNetwork.JoinLobby();
            }
            catch (System.Exception e)
            {
                joinLobbyError = true;
                errorMessage = e.Message;
            }

            if (joinLobbyError)
            {
                Debug.LogWarning($"[FindRoomManager] Error joining lobby: {errorMessage}");
                yield return new WaitForSeconds(1f);
                StartCoroutine(EnsureInLobby());
                yield break;
            }
        }
        else if (PhotonNetwork.InLobby)
        {
            Debug.Log("[FindRoomManager] Already in lobby");
            HideLoadingPanel();
            UpdateRoomListUI();
        }
        else
        {
            Debug.LogWarning("[FindRoomManager] Cannot join lobby - invalid state");
            yield return new WaitForSeconds(1f);
            StartCoroutine(EnsureInLobby());
        }
    }

    public void CreateRoom()
    {
        if (createRoomInput == null || string.IsNullOrEmpty(createRoomInput.text))
        {
            Debug.LogWarning("[FindRoomManager] Room name cannot be empty!");
            ShowLoadingPanel("Please enter a room name!");
            StartCoroutine(HideLoadingPanelAfterDelay(2f));
            return;
        }

        // SET NICKNAME FROM FIREBASE BEFORE CREATING ROOM
        SetPlayerNicknameFromFirebase();

        string roomName = createRoomInput.text.Trim();

        RoomOptions roomOptions = new RoomOptions
        {
            MaxPlayers = 4,
            IsVisible = true,
            IsOpen = true,
            PublishUserId = true,
            CustomRoomProperties = new Hashtable()
        };

        Debug.Log($"[FindRoomManager] Creating room: {roomName}");
        ShowLoadingPanel($"Creating room '{roomName}'...");
        PhotonNetwork.CreateRoom(roomName, roomOptions);
    }

    public void JoinRoom(string roomName)
    {
        // SET NICKNAME FROM FIREBASE BEFORE JOINING ROOM
        SetPlayerNicknameFromFirebase();

        Debug.Log($"[FindRoomManager] Attempting to join room: {roomName}");
        ShowLoadingPanel($"Joining room '{roomName}'...");
        PhotonNetwork.JoinRoom(roomName);
    }

    public void RefreshRoomList()
    {
        if (PhotonNetwork.InLobby)
        {
            StartCoroutine(ForceRefreshLobby());
        }
    }

    IEnumerator ForceRefreshLobby()
    {
        Debug.Log("[FindRoomManager] Force refreshing room list");

        if (PhotonNetwork.InLobby)
        {
            PhotonNetwork.LeaveLobby();
        }

        yield return new WaitForSeconds(0.5f);

        if (PhotonNetwork.IsConnectedAndReady && !PhotonNetwork.InLobby)
        {
            PhotonNetwork.JoinLobby();
        }
    }

    // ===== PHOTON CALLBACKS =====

    public override void OnCreatedRoom()
    {
        Debug.Log("[FindRoomManager] Room created successfully");
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        Debug.LogError($"[FindRoomManager] Failed to create room: {message}");
        ShowLoadingPanel($"Failed to create room: {message}");
        StartCoroutine(HideLoadingPanelAfterDelay(3f));
    }

    public override void OnJoinedRoom()
    {
        Debug.Log($"[FindRoomManager] Joined room: {PhotonNetwork.CurrentRoom.Name}");

        // Clear any old player properties
        ClearPlayerProperties();

        ShowLoadingPanel("Entering lobby...");

        // Navigate to Lobby scene
        SceneManager.LoadScene(lobbySceneName);
    }

    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        Debug.LogError($"[FindRoomManager] Failed to join room: {message}");
        ShowLoadingPanel($"Failed to join room: {message}");
        StartCoroutine(HideLoadingPanelAfterDelay(3f));
    }

    public override void OnJoinedLobby()
    {
        Debug.Log("[FindRoomManager] Joined lobby successfully");
        HideLoadingPanel();

        // Small delay to let room list populate
        StartCoroutine(DelayedRoomListUpdate());
    }

    IEnumerator DelayedRoomListUpdate()
    {
        yield return new WaitForSeconds(0.5f);
        UpdateRoomListUI();
    }

    public override void OnLeftLobby()
    {
        Debug.Log("[FindRoomManager] Left lobby");
        cachedRoomList.Clear();
        UpdateRoomListUI();
    }

    public override void OnRoomListUpdate(List<RoomInfo> roomList)
    {
        Debug.Log($"[FindRoomManager] Room list updated - {roomList.Count} updates received");

        foreach (RoomInfo room in roomList)
        {
            // CRITICAL: Only remove rooms that are EXPLICITLY removed OR have 0 players
            // Keep ALL other rooms visible, even if IsOpen = false or IsVisible = false
            if (room.RemovedFromList || room.PlayerCount == 0)
            {
                if (cachedRoomList.ContainsKey(room.Name))
                {
                    Debug.Log($"[FindRoomManager] Removing room from list: {room.Name} (Removed: {room.RemovedFromList}, Players: {room.PlayerCount})");
                    cachedRoomList.Remove(room.Name);
                }
            }
            else
            {
                // Keep the room in the list regardless of IsVisible or IsOpen status
                Debug.Log($"[FindRoomManager] Keeping/Adding room: {room.Name} ({room.PlayerCount}/{room.MaxPlayers}) [Open: {room.IsOpen}, Visible: {room.IsVisible}]");
                cachedRoomList[room.Name] = room;
            }
        }

        UpdateRoomListUI();
    }

    void UpdateRoomListUI()
    {
        if (roomListContent == null || roomButtonPrefab == null)
        {
            Debug.LogWarning("[FindRoomManager] Room list UI elements not assigned!");
            return;
        }

        Debug.Log($"[FindRoomManager] Updating UI - {cachedRoomList.Count} rooms cached");

        // Clear existing buttons
        foreach (var button in roomButtons.Values)
        {
            if (button != null)
                Destroy(button);
        }
        roomButtons.Clear();

        // Create buttons for ALL available rooms
        foreach (var roomInfo in cachedRoomList.Values)
        {
            // CRITICAL: Show ALL rooms that have at least 1 player
            if (roomInfo.PlayerCount > 0)
            {
                GameObject roomButton = Instantiate(roomButtonPrefab, roomListContent.transform);

                // Determine room state
                bool isInLobby = true; // Default: assume in lobby
                bool gameInProgress = false;
                bool inGracePeriod = false;

                if (roomInfo.CustomProperties != null)
                {
                    // Check if game is in progress
                    if (roomInfo.CustomProperties.ContainsKey("GameInProgress"))
                    {
                        gameInProgress = (bool)roomInfo.CustomProperties["GameInProgress"];
                    }

                    // Check if in grace period
                    if (roomInfo.CustomProperties.ContainsKey("GracePeriodActive"))
                    {
                        inGracePeriod = (bool)roomInfo.CustomProperties["GracePeriodActive"];
                    }
                }

                // Determine if room is joinable
                bool isFull = roomInfo.PlayerCount >= roomInfo.MaxPlayers;

                // CRITICAL: Room is joinable if:
                // 1. Not full
                // 2. Game is NOT in progress (even if IsOpen = false, we can join if in lobby/grace period)
                bool isJoinable = !isFull && !gameInProgress;

                // Set room name with status indicator
                Text nameText = roomButton.transform.Find("RoomName")?.GetComponent<Text>();
                if (nameText != null)
                {
                    string roomDisplayName = roomInfo.Name;

                    if (gameInProgress)
                    {
                        roomDisplayName += " [In Game]";
                    }
                    else if (inGracePeriod)
                    {
                        roomDisplayName += " [Waiting]";
                    }
                    else if (!roomInfo.IsOpen)
                    {
                        roomDisplayName += " [Lobby - Reopening...]";
                    }
                    else
                    {
                        roomDisplayName += " [Lobby]";
                    }

                    nameText.text = roomDisplayName;
                }

                // Set player count
                Text countText = roomButton.transform.Find("PlayerCount")?.GetComponent<Text>();
                if (countText != null)
                    countText.text = $"{roomInfo.PlayerCount}/{roomInfo.MaxPlayers}";

                // Setup button click
                Button btn = roomButton.GetComponent<Button>();
                if (btn != null)
                {
                    string roomName = roomInfo.Name;
                    btn.interactable = isJoinable;

                    if (isJoinable)
                    {
                        btn.onClick.AddListener(() => JoinRoom(roomName));

                        // Color code for different states
                        ColorBlock colors = btn.colors;
                        if (inGracePeriod)
                        {
                            colors.normalColor = new Color(1f, 0.9f, 0.6f); // Yellow tint for grace period
                        }
                        else
                        {
                            colors.normalColor = new Color(0.7f, 1f, 0.7f); // Green tint for lobby
                        }
                        btn.colors = colors;
                    }
                    else
                    {
                        // Gray out non-joinable rooms
                        ColorBlock colors = btn.colors;
                        colors.normalColor = new Color(0.5f, 0.5f, 0.5f);
                        colors.disabledColor = new Color(0.5f, 0.5f, 0.5f);
                        btn.colors = colors;
                    }
                }

                roomButtons[roomInfo.Name] = roomButton;
                Debug.Log($"[FindRoomManager] Created UI button for room: {roomInfo.Name} (Joinable: {isJoinable}, InGame: {gameInProgress}, GracePeriod: {inGracePeriod})");
            }
        }
    }

    void ClearPlayerProperties()
    {
        if (PhotonNetwork.LocalPlayer != null)
        {
            Hashtable clearProps = new Hashtable();
            clearProps["PlayerRole"] = null;
            clearProps["PlayerCharacter"] = null;
            clearProps["PlayerLockedIn"] = null;
            clearProps["CharacterIndex"] = null;
            clearProps["IsReady"] = null;
            clearProps["PlayerCurrentScene"] = null;
            PhotonNetwork.LocalPlayer.SetCustomProperties(clearProps);
        }
    }

    // ===== HELPER METHODS =====
    void SetPlayerNicknameFromFirebase()
    {
        try
        {
            Firebase.Auth.FirebaseUser currentUser = Firebase.Auth.FirebaseAuth.DefaultInstance.CurrentUser;

            if (currentUser != null && !string.IsNullOrEmpty(currentUser.DisplayName))
            {
                PhotonNetwork.NickName = currentUser.DisplayName;
                Debug.Log($"[FindRoomManager] Set PhotonNetwork.NickName to: {currentUser.DisplayName}");
            }
            else if (currentUser != null)
            {
                // Fallback if DisplayName is empty
                PhotonNetwork.NickName = currentUser.Email ?? "Player";
                Debug.Log($"[FindRoomManager] DisplayName empty, using Email: {PhotonNetwork.NickName}");
            }
            else
            {
                PhotonNetwork.NickName = "Guest";
                Debug.LogWarning("[FindRoomManager] No Firebase user logged in");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[FindRoomManager] Error setting nickname from Firebase: {ex.Message}");
            PhotonNetwork.NickName = "Player";
        }
    }
    void ShowLoadingPanel(string message)
    {
        if (loadingPanel != null)
        {
            loadingPanel.SetActive(true);
        }

        if (loadingText != null)
        {
            loadingText.text = message;
        }

        Debug.Log($"[FindRoomManager] Loading: {message}");
    }

    void HideLoadingPanel()
    {
        if (loadingPanel != null)
        {
            loadingPanel.SetActive(false);
        }
    }

    IEnumerator HideLoadingPanelAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        HideLoadingPanel();
    }
}