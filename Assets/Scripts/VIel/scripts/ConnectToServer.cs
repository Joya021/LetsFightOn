using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ConnectToServer : MonoBehaviourPunCallbacks
{
    public static ConnectToServer Instance;

    [Header("UI")]
    public GameObject connectionStatusPanel;
    public Text connectionStatusText;

    [Header("Navigation (Optional)")]
    public Button goToFindRoomButton; // Button to show when already connected

    [Header("Scene Settings")]
    public string[] offlineScenes = new string[] { "OfflineGameScene" }; // Scenes that don't need connection

    private bool isConnecting = false;
    private bool hasConnected = false;
    private float reconnectDelay = 3f;
    private bool returningFromFindRoom = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    void Start()
    {
        string currentScene = SceneManager.GetActiveScene().name;

        // Don't interfere with offline scenes or FindRoom scene
        if (IsOfflineScene(currentScene) || currentScene == "FindRoom")
        {
            Debug.Log($"[ConnectToServer] In {currentScene} scene - skipping connection logic");
            HideConnectionPanel(); // Make sure panel is hidden
            return;
        }

        CheckConnectionAndConnect();
    }

    // Check if current scene is an offline scene
    private bool IsOfflineScene(string sceneName)
    {
        foreach (string offlineScene in offlineScenes)
        {
            if (sceneName == offlineScene)
                return true;
        }
        return false;
    }

    // NEW: Check if already connected (from returning via back button)
    void CheckConnectionAndConnect()
    {
        string currentScene = SceneManager.GetActiveScene().name;

        // Check if we're already connected to Photon
        if (PhotonNetwork.IsConnected)
        {
            Debug.Log("[ConnectToServer] Already connected to Photon!");
            hasConnected = true;
            returningFromFindRoom = true;

            // Show "Already Connected" message
            ShowConnectionPanel("Already connected!");

            // Hide after a moment
            Invoke(nameof(HideConnectionPanel), 1f);

            // Enable the "Go to Find Room" button if available
            if (goToFindRoomButton != null)
            {
                goToFindRoomButton.gameObject.SetActive(true);
            }

            return;
        }

        // Not connected, start normal connection
        Connect();
    }

    void Connect()
    {
        string currentScene = SceneManager.GetActiveScene().name;

        // Don't connect in offline scenes
        if (IsOfflineScene(currentScene))
        {
            Debug.Log("[ConnectToServer] In offline scene - skipping connection");
            HideConnectionPanel();
            return;
        }

        // Prevent multiple connection attempts
        if (isConnecting || PhotonNetwork.IsConnected)
        {
            Debug.Log("[ConnectToServer] Already connecting or connected");
            return;
        }

        if (Application.internetReachability == NetworkReachability.NotReachable)
        {
            ShowConnectionPanel("No internet connection.");
            Invoke(nameof(Connect), reconnectDelay);
            return;
        }

        Debug.Log("[ConnectToServer] Connecting to Photon Master Server...");
        isConnecting = true;

        // Ensure we're not in any room or lobby before connecting
        if (PhotonNetwork.InRoom)
        {
            Debug.Log("[ConnectToServer] Leaving room before reconnect");
            PhotonNetwork.LeaveRoom();
        }

        if (PhotonNetwork.InLobby)
        {
            Debug.Log("[ConnectToServer] Leaving lobby before reconnect");
            PhotonNetwork.LeaveLobby();
        }

        PhotonNetwork.ConnectUsingSettings();
        ShowConnectionPanel("Connecting to server...");
    }

    public override void OnConnectedToMaster()
    {
        Debug.Log("[ConnectToServer] Connected to Master Server");
        isConnecting = false;
        hasConnected = true;

        string currentScene = SceneManager.GetActiveScene().name;

        // Don't show any panel in offline scenes
        if (IsOfflineScene(currentScene))
        {
            HideConnectionPanel();
            return;
        }

        // Only auto-join lobby if we're in FindRoom scene
        if (currentScene == "FindRoom")
        {
            ShowConnectionPanel("Connected! Joining lobby...");

            if (!PhotonNetwork.InRoom && !PhotonNetwork.InLobby)
            {
                PhotonNetwork.JoinLobby();
            }
            else
            {
                HideConnectionPanel();
            }
        }
        else
        {
            // For Login scene or other scenes
            ShowConnectionPanel("Connected to server!");

            // Show the find room button if available
            if (goToFindRoomButton != null)
            {
                goToFindRoomButton.gameObject.SetActive(true);
            }

            // Hide connection panel after a moment
            Invoke(nameof(HideConnectionPanel), 1.5f);
        }
    }

    public override void OnJoinedLobby()
    {
        Debug.Log("[ConnectToServer] Joined Lobby");
        HideConnectionPanel();
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        Debug.LogWarning($"[ConnectToServer] Disconnected from Photon: {cause.ToString()}");
        isConnecting = false;
        hasConnected = false;
        returningFromFindRoom = false;

        string currentScene = SceneManager.GetActiveScene().name;

        // Hide the find room button since we're disconnected
        if (goToFindRoomButton != null)
        {
            goToFindRoomButton.gameObject.SetActive(false);
        }

        // Don't show reconnection message in offline scenes or for intentional disconnects
        if (IsOfflineScene(currentScene) ||
            cause == DisconnectCause.DisconnectByClientLogic ||
            cause == DisconnectCause.ApplicationQuit)
        {
            HideConnectionPanel();
            return;
        }

        ShowConnectionPanel("Disconnected from server. Reconnecting...");

        // Cancel any pending invokes
        CancelInvoke(nameof(Connect));

        // Retry after delay
        Invoke(nameof(Connect), reconnectDelay);
    }

    public override void OnLeftRoom()
    {
        Debug.Log("[ConnectToServer] Left room");
    }

    public override void OnLeftLobby()
    {
        Debug.Log("[ConnectToServer] Left lobby");
    }

    void ShowConnectionPanel(string message)
    {
        string currentScene = SceneManager.GetActiveScene().name;

        // Never show panel in offline scenes
        if (IsOfflineScene(currentScene))
        {
            return;
        }

        if (connectionStatusPanel != null)
        {
            connectionStatusPanel.SetActive(true);

            // Make sure the panel doesn't block UI clicks
            UnityEngine.UI.Image img = connectionStatusPanel.GetComponent<UnityEngine.UI.Image>();
            if (img != null)
            {
                img.raycastTarget = false;
            }
        }

        if (connectionStatusText != null)
            connectionStatusText.text = message;
    }

    void HideConnectionPanel()
    {
        if (connectionStatusPanel != null)
            connectionStatusPanel.SetActive(false);
    }

    // Helper method to check connection status
    public bool IsReadyToJoinLobby()
    {
        return PhotonNetwork.IsConnectedAndReady && !PhotonNetwork.InRoom;
    }

    // NEW: Check if already connected (useful for other scripts)
    public bool IsAlreadyConnected()
    {
        return PhotonNetwork.IsConnected && hasConnected;
    }

    // Public method to manually trigger reconnect
    public void ForceReconnect()
    {
        if (PhotonNetwork.IsConnected)
        {
            PhotonNetwork.Disconnect();
        }

        returningFromFindRoom = false;

        CancelInvoke(nameof(Connect));
        Invoke(nameof(Connect), 1f);
    }

    // NEW: Public method to go to FindRoom (call from your login button)
    public void GoToFindRoom()
    {
        if (!PhotonNetwork.IsConnected)
        {
            Debug.LogWarning("[ConnectToServer] Cannot go to FindRoom - not connected!");
            ShowConnectionPanel("Not connected! Please wait...");
            Connect();
            return;
        }

        Debug.Log("[ConnectToServer] Navigating to FindRoom scene");
        SceneManager.LoadScene("FindRoom");
    }
}