// MODIFIED GAMELOADINGSCREEN.CS
// Only small changes needed to maintain proper sync flow

using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine.SceneManagement;

public class GameLoadingScreen : MonoBehaviourPunCallbacks
{
    [Header("UI Elements")]
    public Text statusText;
    public Text pingText;
    public GameObject[] playerLoadingSlots;

    [Header("Scene")]
    public string gameSceneName = "GameScene";
    public string loadingSceneName = "GameLoadingScreen"; // Changed from "LoadingScreen"

    private bool allPlayersReady = false;
    private int playersReady = 0;
    private bool isCheckingPlayers = false;

    public PrefabRegistry prefabRegistry;

    void Start()
    {
        // Only run if we're in the loading screen scene
        if (!IsInLoadingScene())
        {
            enabled = false;
            return;
        }

        // *** ENSURE auto-sync stays enabled during loading ***
        if (!PhotonNetwork.AutomaticallySyncScene)
        {
            PhotonNetwork.AutomaticallySyncScene = true;
            Debug.Log("[GameLoadingScreen] Auto scene sync ENABLED");
        }

        if (prefabRegistry == null)
            prefabRegistry = FindObjectOfType<PrefabRegistry>();

        if (!isCheckingPlayers)
        {
            isCheckingPlayers = true;
            StartCoroutine(CheckPlayersReady());
        }

        DisplayPlayerCharacters();
    }

    void Update()
    {
        if (!IsInLoadingScene())
            return;

        if (pingText != null)
            pingText.text = $"Ping: {PhotonNetwork.GetPing()}ms";
    }

    bool IsInLoadingScene()
    {
        string currentScene = SceneManager.GetActiveScene().name;
        return currentScene == loadingSceneName;
    }

    IEnumerator CheckPlayersReady()
    {
        while (!allPlayersReady && IsInLoadingScene())
        {
            bool pingStable = PhotonNetwork.GetPing() < 150;

            if (statusText != null)
            {
                if (!pingStable)
                    statusText.text = "Checking connection stability...";
                else
                    statusText.text = $"Waiting for players... ({playersReady}/{PhotonNetwork.PlayerList.Length})";
            }

            yield return new WaitForSeconds(0.5f);

            if (CheckAllPlayersConnected())
            {
                allPlayersReady = true;
                if (statusText != null)
                    statusText.text = "All players ready! Starting game...";

                yield return new WaitForSeconds(2f);

                // *** Load game scene - all players will follow because AutomaticallySyncScene is true ***
                if (PhotonNetwork.IsMasterClient && IsInLoadingScene())
                {
                    Debug.Log("[GameLoadingScreen] MasterClient loading game scene for ALL players");
                    PhotonNetwork.LoadLevel(gameSceneName);
                }
            }
        }
    }

    bool CheckAllPlayersConnected()
    {
        if (!PhotonNetwork.InRoom)
            return false;

        playersReady = 0;
        foreach (Player player in PhotonNetwork.PlayerList)
        {
            if (player.CustomProperties.ContainsKey("PlayerCharacter") &&
                player.CustomProperties.ContainsKey("PlayerLockedIn") &&
                (bool)player.CustomProperties["PlayerLockedIn"] == true)
            {
                playersReady++;
            }
        }
        return playersReady == PhotonNetwork.PlayerList.Length;
    }

    void DisplayPlayerCharacters()
    {
        if (!IsInLoadingScene() || playerLoadingSlots == null)
            return;

        Player[] players = PhotonNetwork.PlayerList;

        for (int i = 0; i < playerLoadingSlots.Length && i < players.Length; i++)
        {
            GameObject slot = playerLoadingSlots[i];
            if (slot == null) continue;

            Image iconImage = slot.GetComponent<Image>();
            slot.SetActive(false);

            if (players[i].CustomProperties.ContainsKey("PlayerRole") &&
                players[i].CustomProperties.ContainsKey("PlayerCharacter"))
            {
                bool isHunter = (bool)players[i].CustomProperties["PlayerRole"];
                string characterName = (string)players[i].CustomProperties["PlayerCharacter"];

                if (string.IsNullOrEmpty(characterName) && prefabRegistry != null)
                {
                    GameObject fallback = prefabRegistry.GetRandomPrefab(isHunter);
                    if (fallback != null)
                        characterName = fallback.name;
                }

                if (iconImage != null && !string.IsNullOrEmpty(characterName))
                {
                    slot.SetActive(true);
                    Sprite icon = null;
                    if (prefabRegistry != null)
                        icon = prefabRegistry.GetIconByName(characterName);

                    if (icon != null)
                        iconImage.sprite = icon;
                    else
                        iconImage.sprite = null;
                }
            }
        }
    }

    void OnDisable()
    {
        StopAllCoroutines();
        isCheckingPlayers = false;
    }
}

