using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;

public class MultiplayerUIManager : MonoBehaviourPunCallbacks
{
    public static MultiplayerUIManager Instance;

    [Header("Player Status UI")]
    public GameObject playerStatusPanel;
    public Transform playerListContainer;
    public GameObject playerStatusPrefab;

    [Header("Quick Chat Settings")]
    public GameObject[] quickChatImages;
    public float quickChatDisplayDuration = 3f;

    // Track player UI elements
    private Dictionary<int, PlayerStatusUI> playerStatusElements = new Dictionary<int, PlayerStatusUI>();

    // Track which players are survivors (for quick chat filtering)
    private HashSet<int> survivorPlayerIds = new HashSet<int>();

    private bool isInitialized = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        // Validate references
        if (!ValidateReferences())
        {
            Debug.LogError("[MultiplayerUIManager] CRITICAL: Missing references! Please assign in Inspector.");
            enabled = false;
            return;
        }

        isInitialized = true;

        if (playerStatusPanel != null)
        {
            playerStatusPanel.SetActive(true);
        }

        // Initialize for all players in room
        if (PhotonNetwork.IsConnected && PhotonNetwork.InRoom)
        {
            foreach (Player player in PhotonNetwork.PlayerList)
            {
                AddPlayerFromPhoton(player);
            }
        }
    }

    private bool ValidateReferences()
    {
        bool isValid = true;

        if (playerStatusPanel == null)
        {
            Debug.LogError("[MultiplayerUIManager] PlayerStatusPanel is NOT assigned! Please assign it in the Inspector.");
            isValid = false;
        }

        if (playerListContainer == null)
        {
            Debug.LogError("[MultiplayerUIManager] PlayerListContainer is NOT assigned! Please assign it in the Inspector.");
            isValid = false;
        }

        if (playerStatusPrefab == null)
        {
            Debug.LogError("[MultiplayerUIManager] PlayerStatusPrefab is NOT assigned! Please assign it in the Inspector.");
            isValid = false;
        }

        if (quickChatImages == null || quickChatImages.Length == 0)
        {
            Debug.LogWarning("[MultiplayerUIManager] QuickChatImages array is empty. Quick chat will not work.");
        }

        if (isValid)
        {
            Debug.Log("[MultiplayerUIManager] All references validated successfully!");
        }

        return isValid;
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        if (!isInitialized) return;
        AddPlayerFromPhoton(newPlayer);
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        if (!isInitialized) return;
        RemovePlayer(otherPlayer.ActorNumber);
    }

    private void AddPlayerFromPhoton(Player player)
    {
        if (!isInitialized)
        {
            Debug.LogWarning("[MultiplayerUIManager] Not initialized, skipping AddPlayerFromPhoton");
            return;
        }

        bool isSurvivor = true;
        int characterIndex = 0;

        if (player.CustomProperties.ContainsKey("PlayerRole"))
        {
            isSurvivor = !(bool)player.CustomProperties["PlayerRole"];
        }

        if (player.CustomProperties.ContainsKey("CharacterIndex"))
        {
            characterIndex = (int)player.CustomProperties["CharacterIndex"];
        }

        AddPlayer(player.ActorNumber, player.NickName, isSurvivor, characterIndex);
    }

    public void AddPlayer(int playerId, string playerName, bool isSurvivor, int characterIndex)
    {
        if (!isInitialized)
        {
            Debug.LogWarning("[MultiplayerUIManager] Cannot add player - not initialized!");
            return;
        }

        if (playerStatusElements.ContainsKey(playerId))
        {
            Debug.LogWarning($"[MultiplayerUIManager] Player {playerId} already exists in UI");
            return;
        }

        if (playerStatusPrefab == null)
        {
            Debug.LogError("[MultiplayerUIManager] PlayerStatusPrefab is NULL! Cannot instantiate.");
            return;
        }

        if (playerListContainer == null)
        {
            Debug.LogError("[MultiplayerUIManager] PlayerListContainer is NULL! Cannot add player.");
            return;
        }

        GameObject statusObj = Instantiate(playerStatusPrefab, playerListContainer);
        PlayerStatusUI statusUI = statusObj.GetComponent<PlayerStatusUI>();

        if (statusUI != null)
        {
            statusUI.Initialize(playerId, playerName, isSurvivor, characterIndex);
            playerStatusElements[playerId] = statusUI;

            if (isSurvivor)
            {
                survivorPlayerIds.Add(playerId);
            }

            Debug.Log($"[MultiplayerUIManager] Added player {playerName} (ID: {playerId}) to UI. Survivor: {isSurvivor}");
        }
        else
        {
            Debug.LogError("[MultiplayerUIManager] PlayerStatusUI component not found on prefab!");
            Destroy(statusObj);
        }
    }

    public void RemovePlayer(int playerId)
    {
        if (!isInitialized) return;

        if (playerStatusElements.ContainsKey(playerId))
        {
            Destroy(playerStatusElements[playerId].gameObject);
            playerStatusElements.Remove(playerId);
            survivorPlayerIds.Remove(playerId);
            Debug.Log($"[MultiplayerUIManager] Removed player {playerId} from UI");
        }
    }

    public void SetPlayerAliveStatus(int playerId, bool isAlive)
    {
        if (!isInitialized) return;

        if (playerStatusElements.ContainsKey(playerId))
        {
            playerStatusElements[playerId].SetAliveStatus(isAlive);
        }
    }

    public void ShowPlayerQuickChat(int playerId, int messageIndex)
    {
        if (!isInitialized)
        {
            Debug.LogWarning("[MultiplayerUIManager] Cannot show quick chat - not initialized!");
            return;
        }

        // Only survivors should see survivor quick chats
        if (!survivorPlayerIds.Contains(playerId))
        {
            Debug.Log($"[MultiplayerUIManager] Player {playerId} is not a survivor, quick chat not shown");
            return;
        }

        // Check if PhotonView is available
        PhotonView pv = GetComponent<PhotonView>();
        if (pv == null)
        {
            Debug.LogError("[MultiplayerUIManager] PhotonView not found! Please add PhotonView component.");
            return;
        }

        // Send RPC to all clients
        pv.RPC("RPC_ShowQuickChat", RpcTarget.All, playerId, messageIndex);
    }

    [PunRPC]
    void RPC_ShowQuickChat(int senderId, int messageIndex)
    {
        if (!isInitialized) return;

        // Check if local player is a survivor
        bool localPlayerIsSurvivor = true;
        if (PhotonNetwork.LocalPlayer.CustomProperties.ContainsKey("PlayerRole"))
        {
            localPlayerIsSurvivor = !(bool)PhotonNetwork.LocalPlayer.CustomProperties["PlayerRole"];
        }

        // Only show to survivors
        if (!localPlayerIsSurvivor)
        {
            Debug.Log("[MultiplayerUIManager] Local player is Hunter, not showing survivor quick chat");
            return;
        }

        // Validate quick chat images
        if (quickChatImages == null || quickChatImages.Length == 0)
        {
            Debug.LogWarning("[MultiplayerUIManager] Quick chat images array is empty!");
            return;
        }

        // Show the quick chat image
        if (messageIndex >= 0 && messageIndex < quickChatImages.Length)
        {
            if (quickChatImages[messageIndex] != null)
            {
                StartCoroutine(ShowQuickChatCoroutine(messageIndex));
                Debug.Log($"[MultiplayerUIManager] Showing quick chat message {messageIndex} from player {senderId}");
            }
            else
            {
                Debug.LogWarning($"[MultiplayerUIManager] Quick chat image at index {messageIndex} is NULL!");
            }
        }
        else
        {
            Debug.LogWarning($"[MultiplayerUIManager] Invalid quick chat index: {messageIndex}");
        }
    }

    private IEnumerator ShowQuickChatCoroutine(int messageIndex)
    {
        if (quickChatImages[messageIndex] != null)
        {
            quickChatImages[messageIndex].SetActive(true);
            yield return new WaitForSeconds(quickChatDisplayDuration);
            quickChatImages[messageIndex].SetActive(false);
        }
    }

    public void UpdatePlayerHealth(int playerId, int currentHP, int maxHP)
    {
        if (!isInitialized) return;

        if (playerStatusElements.ContainsKey(playerId))
        {
            playerStatusElements[playerId].UpdateHealth(currentHP, maxHP);
        }
    }
}