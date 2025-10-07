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
    public GameObject[] playerSlots;               // Player slots (e.g., avatars)
    public GameObject playerSlotsParent;           // Parent with HorizontalLayoutGroup
    public Button readyButton;
    public Button leaveRoomButton;

    [Header("Minimum Players")]
    public int minPlayers = 2;

    private Dictionary<string, RoomInfo> cachedRoomList = new Dictionary<string, RoomInfo>();
    private Dictionary<string, GameObject> roomButtons = new Dictionary<string, GameObject>();

    void Start()
    {
        PhotonNetwork.AutomaticallySyncScene = true;

        if (readyButton != null)
        {
            readyButton.gameObject.SetActive(false);
            readyButton.onClick.AddListener(OnReadyButtonClicked);
        }

        if (leaveRoomButton != null)
        {
            leaveRoomButton.onClick.AddListener(LeaveRoom);
        }

        if (deleteRoomButton != null)
        {
            deleteRoomButton.gameObject.SetActive(false);
        }

        if (PhotonNetwork.InRoom)
        {
            StartCoroutine(DelayedUpdateLobbyUI());
        }
    }

    public override void OnEnable()
    {
        base.OnEnable();

        if (PhotonNetwork.IsConnected && !PhotonNetwork.InLobby)
        {
            PhotonNetwork.JoinLobby();
        }
        else if (PhotonNetwork.InLobby)
        {
            StartCoroutine(CheckAndDisplayCachedRooms());
        }
    }

    IEnumerator CheckAndDisplayCachedRooms()
    {
        yield return null;

        if (PhotonNetwork.InLobby)
        {
            if (cachedRoomList.Count == 0)
            {
                PhotonNetwork.LeaveLobby();
                yield return new WaitForSeconds(0.5f);
                PhotonNetwork.JoinLobby();
            }
            else
            {
                UpdateRoomListUI();
            }
        }
    }

    public void CreateRoom()
    {
        if (string.IsNullOrEmpty(createInput.text))
        {
            Debug.LogWarning("Room name cannot be empty!");
            return;
        }

        RoomOptions roomOptions = new RoomOptions
        {
            MaxPlayers = 4,
            IsVisible = true,
            IsOpen = true
        };

        PhotonNetwork.CreateRoom(createInput.text, roomOptions);
    }

    public void LeaveRoom()
    {
        if (PhotonNetwork.InRoom)
        {
            PhotonNetwork.LeaveRoom();
        }
    }

    public void DeleteRoom()
    {
        if (!PhotonNetwork.InRoom || !PhotonNetwork.IsMasterClient) return;

        PhotonNetwork.CurrentRoom.IsOpen = false;
        PhotonNetwork.CurrentRoom.IsVisible = false;
        PhotonNetwork.LeaveRoom();
    }

    public void JoinRoom(string roomName)
    {
        PhotonNetwork.JoinRoom(roomName);
    }

    public override void OnCreatedRoom()
    {
        Debug.Log("Room created successfully");
    }

    public override void OnJoinedRoom()
    {
        SceneManager.LoadScene("Lobby");
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        Debug.LogError("Failed to create room: " + message);
    }

    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        Debug.LogError("Failed to join room: " + message);
    }

    public override void OnRoomListUpdate(List<RoomInfo> roomList)
    {
        foreach (RoomInfo room in roomList)
        {
            if (room.RemovedFromList || !room.IsVisible || !room.IsOpen)
            {
                cachedRoomList.Remove(room.Name);
            }
            else
            {
                cachedRoomList[room.Name] = room;
            }
        }

        UpdateRoomListUI();
    }

    void UpdateRoomListUI()
    {
        if (roomListContent == null || roomButtonPrefab == null) return;

        foreach (var button in roomButtons.Values)
        {
            if (button != null)
                Destroy(button);
        }

        roomButtons.Clear();

        foreach (var roomInfo in cachedRoomList.Values)
        {
            if (roomInfo.IsOpen && roomInfo.IsVisible && roomInfo.PlayerCount < roomInfo.MaxPlayers)
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
                btn.onClick.AddListener(() => JoinRoom(roomName));

                roomButtons[roomInfo.Name] = roomButton;
            }
        }
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        UpdateLobbyUI();
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        UpdateLobbyUI();

        if (PhotonNetwork.InRoom && PhotonNetwork.CurrentRoom.PlayerCount == 0)
        {
            PhotonNetwork.LeaveRoom();
        }
    }

    IEnumerator DelayedUpdateLobbyUI()
    {
        yield return new WaitForSeconds(0.15f);
        UpdateLobbyUI();
    }

    void UpdateLobbyUI()
    {
        if (!PhotonNetwork.InRoom) return;

        if (lobbyRoomNameText != null)
            lobbyRoomNameText.text = PhotonNetwork.CurrentRoom.Name;

        Player[] players = PhotonNetwork.PlayerList;

        for (int i = 0; i < playerSlots.Length; i++)
        {
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

        // Dynamically adjust spacing in Horizontal Layout Group (optional)
        if (playerSlotsParent != null)
        {
            HorizontalLayoutGroup layout = playerSlotsParent.GetComponent<HorizontalLayoutGroup>();
            if (layout != null)
            {
                layout.spacing = players.Length == 2 ? 80f : 40f; // adjust based on number of players
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
        if (PhotonNetwork.IsMasterClient)
        {
            PhotonNetwork.CurrentRoom.IsOpen = false;

            Hashtable props = new Hashtable();
            props["StartCharacterSelection"] = true;
            PhotonNetwork.CurrentRoom.SetCustomProperties(props);

            PhotonNetwork.LoadLevel("CSCharPickPage");
        }
    }

    public override void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged)
    {
        if (propertiesThatChanged.ContainsKey("StartCharacterSelection"))
        {
            bool start = (bool)propertiesThatChanged["StartCharacterSelection"];
            if (start && SceneManager.GetActiveScene().name != "CSCharPickPage")
            {
                SceneManager.LoadScene("CSCharPickPage");
            }
        }
    }

    public override void OnLeftRoom()
    {
        // We are still connected to master — no need to reconnect!

        Debug.Log("Left room. Returning to lobby...");

        if (!PhotonNetwork.InLobby)
        {
            PhotonNetwork.JoinLobby(); // Re-enter lobby to see rooms again
        }

        // Show your UI panel again or refresh room list
        if (roomListContent != null)
        {
          //  ClearRoomListUI(); // Optional method to clear previous room buttons
        }

        StartCoroutine(ForceRefreshLobby());
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
        PhotonNetwork.LeaveLobby();
        yield return new WaitForSeconds(0.5f);
        PhotonNetwork.JoinLobby();
    }
}
    