using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ConnectToServer : MonoBehaviourPunCallbacks
{
    public static ConnectToServer Instance;

    [Header("UI")]
    public GameObject connectionStatusPanel; // Assign in Inspector
    public Text connectionStatusText; // Assign in Inspector (or TMP_Text)

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

        Connect();
    }

    void Connect()
    {
        if (Application.internetReachability == NetworkReachability.NotReachable)
        {
            ShowConnectionPanel("No internet connection.");
            return;
        }

        Debug.Log("Connecting to Photon Master Server...");
        PhotonNetwork.ConnectUsingSettings();
        ShowConnectionPanel("Connecting to server...");
    }

    public override void OnConnectedToMaster()
    {
        Debug.Log("Connected to Master Server.");
        HideConnectionPanel();
        PhotonNetwork.JoinLobby();
    }

    public override void OnJoinedLobby()
    {
        Debug.Log("Joined Lobby.");
        HideConnectionPanel();
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        Debug.LogWarning($"Disconnected from Photon: {cause.ToString()}");
        ShowConnectionPanel("Disconnected from server. Reconnecting...");

        // Optionally: Try to reconnect
        Invoke(nameof(Connect), 3f); // retry after 3 seconds
    }

    void ShowConnectionPanel(string message)
    {
        if (connectionStatusPanel != null)
            connectionStatusPanel.SetActive(true);
        if (connectionStatusText != null)
            connectionStatusText.text = message;
    }

    void HideConnectionPanel()
    {
        if (connectionStatusPanel != null)
            connectionStatusPanel.SetActive(false);
    }
}
