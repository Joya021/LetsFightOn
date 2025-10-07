// GameLoadingScreen.cs
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using Photon.Pun;
using Photon.Realtime;

/// <summary>
/// Modified to use PrefabRegistry for icons and to avoid relying on string arrays for "Hunter" fallback.
/// Assign a PrefabRegistry in the scene (or it will try to Find one).
/// </summary>
public class GameLoadingScreen : MonoBehaviourPunCallbacks
{
    [Header("UI Elements")]
    public Text statusText;
    public Text pingText;
    public GameObject[] playerLoadingSlots; // Each slot should have an Image component for character icon

    [Header("Scene")]
    public string gameSceneName = "GameScene";

    private bool allPlayersReady = false;
    private int playersReady = 0;

    // PrefabRegistry reference (assign via Inspector or the script will Find one)
    public PrefabRegistry prefabRegistry;

    void Start()
    {
        if (prefabRegistry == null)
            prefabRegistry = FindObjectOfType<PrefabRegistry>();

        StartCoroutine(CheckPlayersReady());
        DisplayPlayerCharacters();
    }

    void Update()
    {
        if (pingText != null)
            pingText.text = $"Ping: {PhotonNetwork.GetPing()}ms";
    }

    IEnumerator CheckPlayersReady()
    {
        while (!allPlayersReady)
        {
            bool pingStable = PhotonNetwork.GetPing() < 150;

            if (statusText != null)
            {
                if (!pingStable)
                    statusText.text = "Checking connection stability.";
                else
                    statusText.text = $"Waiting for players. ({playersReady}/{PhotonNetwork.PlayerList.Length})";
            }

            yield return new WaitForSeconds(0.5f);

            if (CheckAllPlayersConnected())
            {
                allPlayersReady = true;
                if (statusText != null)
                    statusText.text = "All players ready! Starting game.";

                yield return new WaitForSeconds(2f);

                if (PhotonNetwork.IsMasterClient)
                {
                    PhotonNetwork.LoadLevel(gameSceneName);
                }
            }
        }
    }

    bool CheckAllPlayersConnected()
    {
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
                    // fallback: pick a random one for UI only (prefer registry mapping)
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
                        iconImage.sprite = null; // no icon assigned in registry
                }
            }
        }
    }
}
