using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine.UI;
using Hashtable = ExitGames.Client.Photon.Hashtable;
using UnityEngine.SceneManagement;

public class CreateAndJoinRooms : MonoBehaviourPunCallbacks
{
    [Header("Room Creation")]
    public InputField createInput;
    public Button deleteRoomButton;

    [Header("Room List")]
    public GameObject roomListContent;
    public GameObject roomButtonPrefab;

    [Header("Lobby UI")]
    public Text lobbyRoomNameText;
    public GameObject[] playerSlots;
    public GameObject playerSlotsParent;
    public Button readyButton;
    public Button leaveRoomButton;

    [Header("Minimum Players")]
    public int minPlayers = 2;

    [Header("Scene Management")]
    public string lobbySceneName = "Lobby";
    public string findRoomSceneName = "FindRoom";
    public string characterSelectionSceneName = "CSCharPickPage";

    private Dictionary<string, RoomInfo> cachedRoomList = new Dictionary<string, RoomInfo>();
    private Dictionary<string, GameObject> roomButtons = new Dictionary<string, GameObject>();
    private bool isInitialized = false;
    private bool isLeavingRoom = false;
    private bool isDeletingRoom = false;
    private bool isDestroyed = false; // Track if this instance is destroyed

    void Start()
    {
        string currentScene = SceneManager.GetActiveScene().name;

        // CRITICAL: Disable auto scene sync by default
        PhotonNetwork.AutomaticallySyncScene = false;

        if (currentScene == findRoomSceneName && PhotonNetwork.InRoom)
        {
            Debug.Log("[CreateAndJoinRooms] Still in room on FindRoom scene - leaving room");
            ClearPlayerProperties();
            PhotonNetwork.LeaveRoom();
        }

        if (IsInLobbyRelatedScene())
        {
            InitializeUI();
            isInitialized = true;

            if (PhotonNetwork.InRoom)
            {
                StartCoroutine(DelayedUpdateLobbyUI());
            }
        }
    }

    public override void OnEnable()
    {
        base.OnEnable();

        if (!IsInLobbyRelatedScene())
            return;

        if (!isInitialized)
        {
            InitializeUI();
            isInitialized = true;
        }

        StartCoroutine(WaitAndJoinLobby());
    }

    IEnumerator WaitAndJoinLobby()
    {
        while (!PhotonNetwork.IsConnectedAndReady)
        {
            yield return new WaitForSeconds(0.1f);
        }

        string currentScene = SceneManager.GetActiveScene().name;

        // If we're in FindRoom and still in a room, leave it first
        if (currentScene == findRoomSceneName && PhotonNetwork.InRoom)
        {
            Debug.Log("[CreateAndJoinRooms] Leaving existing room before joining lobby");
            ClearPlayerProperties();
            PhotonNetwork.LeaveRoom();

            float timeout = 5f;
            float elapsed = 0f;
            while (PhotonNetwork.InRoom && elapsed < timeout)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }
        }

        if (!PhotonNetwork.InLobby && !PhotonNetwork.InRoom)
        {
            PhotonNetwork.JoinLobby();
        }
        else if (PhotonNetwork.InLobby)
        {
            UpdateRoomListUI();
        }
    }

    void OnDisable()
    {
        if (readyButton != null)
            readyButton.onClick.RemoveAllListeners();
        if (leaveRoomButton != null)
            leaveRoomButton.onClick.RemoveAllListeners();
        if (deleteRoomButton != null)
            deleteRoomButton.onClick.RemoveAllListeners();
    }

    bool IsInLobbyRelatedScene()
    {
        string currentScene = SceneManager.GetActiveScene().name;
        return currentScene == lobbySceneName || currentScene == findRoomSceneName;
    }

    void InitializeUI()
    {
        if (readyButton != null)
        {
            readyButton.gameObject.SetActive(false);
            readyButton.onClick.RemoveAllListeners();
            readyButton.onClick.AddListener(OnReadyButtonClicked);
        }

        if (leaveRoomButton != null)
        {
            leaveRoomButton.onClick.RemoveAllListeners();
            leaveRoomButton.onClick.AddListener(LeaveRoom);
        }

        if (deleteRoomButton != null)
        {
            deleteRoomButton.gameObject.SetActive(false);
            deleteRoomButton.onClick.RemoveAllListeners();
            deleteRoomButton.onClick.AddListener(DeleteRoom);
        }
    }

    public void CreateRoom()
    {
        if (!IsInLobbyRelatedScene() || createInput == null)
            return;

        if (string.IsNullOrEmpty(createInput.text))
        {
            Debug.LogWarning("Room name cannot be empty!");
            return;
        }

        RoomOptions roomOptions = new RoomOptions
        {
            MaxPlayers = 4,
            IsVisible = true,
            IsOpen = true,
            CustomRoomProperties = new Hashtable(),
            PublishUserId = true // Ensure room appears in lobby list
        };

        PhotonNetwork.CreateRoom(createInput.text, roomOptions);
    }

    public void LeaveRoom()
    {
        if (!PhotonNetwork.InRoom || isLeavingRoom)
        {
            Debug.LogWarning("[CreateAndJoinRooms] Cannot leave room - already leaving or not in room");
            return;
        }

        string currentScene = SceneManager.GetActiveScene().name;
        if (currentScene != lobbySceneName)
        {
            Debug.LogWarning("[CreateAndJoinRooms] Leave room should only be called from Lobby scene");
            return;
        }

        Debug.Log("[CreateAndJoinRooms] Player leaving room from Lobby scene...");
        isLeavingRoom = true;

        // Disable the button to prevent double-clicks
        if (leaveRoomButton != null)
            leaveRoomButton.interactable = false;

        ClearPlayerProperties();

        // Keep room open when leaving (unless you're the last player)
        PhotonNetwork.LeaveRoom();
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
            PhotonNetwork.LocalPlayer.SetCustomProperties(clearProps);

            Debug.Log("[CreateAndJoinRooms] Cleared player properties");
        }
    }

    public void DeleteRoom()
    {
        if (!PhotonNetwork.InRoom)
        {
            Debug.LogError("[CreateAndJoinRooms] Cannot delete room - not in a room!");
            return;
        }

        if (!PhotonNetwork.IsMasterClient)
        {
            Debug.LogError("[CreateAndJoinRooms] Only the host can delete the room!");
            return;
        }

        if (isDeletingRoom)
        {
            Debug.LogWarning("[CreateAndJoinRooms] Already deleting room...");
            return;
        }

        string currentScene = SceneManager.GetActiveScene().name;
        if (currentScene != lobbySceneName)
        {
            Debug.LogWarning("[CreateAndJoinRooms] Delete room should only be called from Lobby scene");
            return;
        }

        Debug.Log("[CreateAndJoinRooms] Master deleting room from Lobby scene...");
        isDeletingRoom = true;

        // Disable the button to prevent double-clicks
        if (deleteRoomButton != null)
            deleteRoomButton.interactable = false;

        // CRITICAL: Close and hide the room BEFORE leaving
        Hashtable roomProps = new Hashtable();
        roomProps["IsOpen"] = false;
        roomProps["IsVisible"] = false;
        PhotonNetwork.CurrentRoom.SetCustomProperties(roomProps);

        // Also set the room properties directly
        PhotonNetwork.CurrentRoom.IsOpen = false;
        PhotonNetwork.CurrentRoom.IsVisible = false;

        ClearPlayerProperties();

        // Small delay to ensure properties are set before leaving
        StartCoroutine(DelayedLeaveAfterDelete());
    }

    IEnumerator DelayedLeaveAfterDelete()
    {
        yield return new WaitForSeconds(0.2f);

        if (PhotonNetwork.InRoom)
        {
            PhotonNetwork.LeaveRoom();
        }
    }

    public void JoinRoom(string roomName)
    {
        if (!IsInLobbyRelatedScene())
            return;

        PhotonNetwork.JoinRoom(roomName);
    }

    public override void OnCreatedRoom()
    {
        Debug.Log("[CreateAndJoinRooms] Room created successfully: " + PhotonNetwork.CurrentRoom.Name);
    }

    public override void OnJoinedRoom()
    {
        Debug.Log("[CreateAndJoinRooms] Joined room: " + PhotonNetwork.CurrentRoom.Name);

        if (IsInLobbyRelatedScene())
        {
            ClearPlayerProperties();

            // Load lobby scene
            SceneManager.LoadScene(lobbySceneName);
        }
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        Debug.LogError("[CreateAndJoinRooms] Failed to create room: " + message);
    }

    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        Debug.LogError("[CreateAndJoinRooms] Failed to join room: " + message);
    }

    public override void OnRoomListUpdate(List<RoomInfo> roomList)
    {
        if (!IsInLobbyRelatedScene())
            return;

        Debug.Log($"[CreateAndJoinRooms] Room list updated. Received {roomList.Count} room updates");

        foreach (RoomInfo room in roomList)
        {
            // Remove rooms that are closed, hidden, or marked as removed
            if (room.RemovedFromList || !room.IsVisible || !room.IsOpen || room.PlayerCount == 0)
            {
                if (cachedRoomList.ContainsKey(room.Name))
                {
                    Debug.Log($"[CreateAndJoinRooms] Removing room: {room.Name}");
                    cachedRoomList.Remove(room.Name);
                }
            }
            else
            {
                Debug.Log($"[CreateAndJoinRooms] Adding/Updating room: {room.Name} ({room.PlayerCount}/{room.MaxPlayers})");
                cachedRoomList[room.Name] = room;
            }
        }

        UpdateRoomListUI();
    }

    void UpdateRoomListUI()
    {
        string currentScene = SceneManager.GetActiveScene().name;

        // Only update room list in FindRoom scene
        if (currentScene != findRoomSceneName || roomListContent == null || roomButtonPrefab == null)
            return;

        Debug.Log($"[CreateAndJoinRooms] Updating room list UI. Cached rooms: {cachedRoomList.Count}");

        // Clear existing buttons
        foreach (var button in roomButtons.Values)
        {
            if (button != null)
                Destroy(button);
        }
        roomButtons.Clear();

        // Create buttons for all available rooms
        foreach (var roomInfo in cachedRoomList.Values)
        {
            // Show ALL open and visible rooms
            if (roomInfo.IsOpen && roomInfo.IsVisible)
            {
                GameObject roomButton = Instantiate(roomButtonPrefab, roomListContent.transform);

                Text nameText = roomButton.transform.Find("RoomName")?.GetComponent<Text>();
                if (nameText != null)
                    nameText.text = roomInfo.Name;

                Text countText = roomButton.transform.Find("PlayerCount")?.GetComponent<Text>();
                if (countText != null)
                    countText.text = roomInfo.PlayerCount + "/" + roomInfo.MaxPlayers;

                Button btn = roomButton.GetComponent<Button>();
                string roomName = roomInfo.Name;

                // Disable button if room is full
                bool isFull = roomInfo.PlayerCount >= roomInfo.MaxPlayers;
                btn.interactable = !isFull;

                btn.onClick.AddListener(() => JoinRoom(roomName));

                roomButtons[roomInfo.Name] = roomButton;

                Debug.Log($"[CreateAndJoinRooms] Created button for room: {roomInfo.Name}");
            }
        }
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        Debug.Log($"[CreateAndJoinRooms] Player {newPlayer.NickName} entered the room");

        if (IsInLobbyRelatedScene())
        {
            UpdateLobbyUI();
        }
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        Debug.Log($"[CreateAndJoinRooms] Player {otherPlayer.NickName} left the room");

        if (IsInLobbyRelatedScene())
        {
            UpdateLobbyUI();
        }
    }

    IEnumerator DelayedUpdateLobbyUI()
    {
        yield return new WaitForSeconds(0.15f);
        if (IsInLobbyRelatedScene())
        {
            UpdateLobbyUI();
        }
    }

    void UpdateLobbyUI()
    {
        string currentScene = SceneManager.GetActiveScene().name;

        // Only update lobby UI in Lobby scene
        if (currentScene != lobbySceneName || !PhotonNetwork.InRoom)
            return;

        if (lobbyRoomNameText != null)
            lobbyRoomNameText.text = PhotonNetwork.CurrentRoom.Name;

        if (playerSlots == null || playerSlots.Length == 0)
            return;

        Player[] players = PhotonNetwork.PlayerList;

        for (int i = 0; i < playerSlots.Length; i++)
        {
            if (playerSlots[i] == null) continue;

            if (i < players.Length)
            {
                playerSlots[i].SetActive(true);

                UserInfoDisplay userInfo = playerSlots[i].GetComponent<UserInfoDisplay>();
                if (userInfo != null)
                {
                    if (userInfo.displayNameText != null)
                        userInfo.displayNameText.text = players[i].NickName;

                    userInfo.SetHostStatus(players[i].IsMasterClient);
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
                layout.spacing = players.Length == 2 ? 80f : 40f;
            }
        }

        if (deleteRoomButton != null)
        {
            deleteRoomButton.gameObject.SetActive(PhotonNetwork.IsMasterClient);
        }

        if (readyButton != null)
        {
            bool isHost = PhotonNetwork.IsMasterClient;
            bool enoughPlayers = PhotonNetwork.CurrentRoom.PlayerCount >= minPlayers;
            readyButton.gameObject.SetActive(isHost && enoughPlayers);
        }
    }

    void OnReadyButtonClicked()
    {
        if (!PhotonNetwork.IsMasterClient)
            return;

        Debug.Log("[CreateAndJoinRooms] Starting character selection...");

        // Close the room so no one else can join during character selection
        PhotonNetwork.CurrentRoom.IsOpen = false;

        // CRITICAL: Enable auto-sync ONLY when starting the game
        PhotonNetwork.AutomaticallySyncScene = true;

        Hashtable props = new Hashtable();
        props["StartCharacterSelection"] = true;
        PhotonNetwork.CurrentRoom.SetCustomProperties(props);

        PhotonNetwork.LoadLevel(characterSelectionSceneName);
    }

    public override void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged)
    {
        if (!IsInLobbyRelatedScene())
            return;

        if (propertiesThatChanged.ContainsKey("StartCharacterSelection"))
        {
            bool start = (bool)propertiesThatChanged["StartCharacterSelection"];
            if (start && SceneManager.GetActiveScene().name != characterSelectionSceneName)
            {
                if (PhotonNetwork.InRoom)
                {
                    SceneManager.LoadScene(characterSelectionSceneName);
                }
            }
        }
    }

    public override void OnLeftRoom()
    {
        Debug.Log("[CreateAndJoinRooms] Successfully left room");

        ClearPlayerProperties();

        string currentScene = SceneManager.GetActiveScene().name;
        Debug.Log($"[CreateAndJoinRooms] Current scene when left room: {currentScene}");

        // Disable auto sync when returning to lobby
        PhotonNetwork.AutomaticallySyncScene = false;

        // Navigate to FindRoom scene ONLY if we're currently in the Lobby scene
        if (currentScene == lobbySceneName)
        {
            Debug.Log("[CreateAndJoinRooms] Redirecting to FindRoom scene...");

            // Reset flags before changing scene
            isLeavingRoom = false;
            isDeletingRoom = false;

            SceneManager.LoadScene(findRoomSceneName);
        }
        else
        {
            // Reset flags
            isLeavingRoom = false;
            isDeletingRoom = false;
        }

        // Rejoin lobby to see available rooms
        if (!PhotonNetwork.InLobby)
        {
            StartCoroutine(RejoinLobbyAfterLeaving());
        }
    }

    IEnumerator RejoinLobbyAfterLeaving()
    {
        yield return new WaitForSeconds(0.3f);

        if (PhotonNetwork.IsConnectedAndReady && !PhotonNetwork.InLobby && !PhotonNetwork.InRoom)
        {
            Debug.Log("[CreateAndJoinRooms] Rejoining lobby after leaving room");
            PhotonNetwork.JoinLobby();
        }
    }

    public override void OnJoinedLobby()
    {
        Debug.Log("[CreateAndJoinRooms] Joined lobby successfully");

        if (IsInLobbyRelatedScene())
        {
            // Small delay to ensure room list is populated
            StartCoroutine(DelayedRoomListUpdate());
        }
    }

    IEnumerator DelayedRoomListUpdate()
    {
        yield return new WaitForSeconds(0.5f);
        UpdateRoomListUI();
    }

    public void RefreshRoomList()
    {
        if (PhotonNetwork.InLobby && IsInLobbyRelatedScene())
        {
            StartCoroutine(ForceRefreshLobby());
        }
    }

    IEnumerator ForceRefreshLobby()
    {
        Debug.Log("[CreateAndJoinRooms] Force refreshing lobby...");

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
}