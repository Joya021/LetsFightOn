using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Collections;


public class MultiplayerUIManager : MonoBehaviour
{
    [Header("Player Icons Panel")]
    public GameObject playerIconsPanel;
    public GameObject playerIconPrefab; // Prefab for individual player UI

    [Header("Status Indicator Images")]
    public Sprite aliveIndicatorSprite;
    public Sprite deadIndicatorSprite;

    [Header("Quick Chat Settings")]
    public float quickChatDisplayDuration = 3f;
    public GameObject[] quickChatImages; // Array of quick chat images to choose from

    [Header("Character Icons")]
    public Sprite[] survivorCharacterIcons; // 4 survivor character icons
    public Sprite[] hunterCharacterIcons;   // 3 hunter character icons

    [Header("Layout Settings")]
    public int maxPlayersPerRow = 4;
    public float iconSpacing = 10f;

    // Player management
    private Dictionary<int, PlayerUIData> players = new Dictionary<int, PlayerUIData>();
    private List<Coroutine> activeQuickChatCoroutines = new List<Coroutine>();

    public static MultiplayerUIManager Instance;

    [System.Serializable]
    public class PlayerUIData
    {
        public string playerName;
        public int playerId;
        public bool isSurvivor; // true = survivor, false = hunter
        public bool isAlive = true;
        public GameObject playerIconObject; // The UI GameObject containing all player UI elements
        public Image playerIcon; // The character icon
        public Image statusIndicator; // Alive/Dead indicator
        public Text playerNameText; // Player name display
        public GameObject quickChatIndicator; // Shows when player uses quick chat
        public Image quickChatImage; // The actual quick chat image
    }
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        if (playerIconsPanel != null)
            playerIconsPanel.SetActive(true);
    }

    #region Player Management

    public void AddPlayer(int playerId, string playerName, bool isSurvivor, int characterIndex)
    {
        if (players.ContainsKey(playerId))
        {
            Debug.LogWarning($"Player {playerId} already exists!");
            return;
        }

        // Create player UI
        GameObject playerIconObj = Instantiate(playerIconPrefab, playerIconsPanel.transform);

        PlayerUIData playerData = new PlayerUIData
        {
            playerId = playerId,
            playerName = playerName,
            isSurvivor = isSurvivor,
            isAlive = true,
            playerIconObject = playerIconObj
        };

        // Get UI components from the instantiated prefab
        playerData.playerIcon = playerIconObj.transform.Find("PlayerIcon").GetComponent<Image>();
        playerData.statusIndicator = playerIconObj.transform.Find("StatusIndicator").GetComponent<Image>();
        playerData.playerNameText = playerIconObj.transform.Find("PlayerName").GetComponent<Text>();
        playerData.quickChatIndicator = playerIconObj.transform.Find("QuickChatIndicator").gameObject;
        playerData.quickChatImage = playerIconObj.transform.Find("QuickChatIndicator/QuickChatImage").GetComponent<Image>();

        // Set up player icon
        SetPlayerCharacterIcon(playerData, characterIndex);

        // Set up status indicator
        if (playerData.statusIndicator != null)
            playerData.statusIndicator.sprite = aliveIndicatorSprite;

        // Set up player name
        if (playerData.playerNameText != null)
            playerData.playerNameText.text = playerName;

        // Hide quick chat indicator initially
        if (playerData.quickChatIndicator != null)
            playerData.quickChatIndicator.SetActive(false);

        players.Add(playerId, playerData);
        UpdatePlayerIconsLayout();

        Debug.Log($"Added player: {playerName} (ID: {playerId}, Survivor: {isSurvivor})");
    }

    public void RemovePlayer(int playerId)
    {
        if (players.ContainsKey(playerId))
        {
            if (players[playerId].playerIconObject != null)
                Destroy(players[playerId].playerIconObject);

            players.Remove(playerId);
            UpdatePlayerIconsLayout();
        }
    }

    public void SetPlayerAliveStatus(int playerId, bool isAlive)
    {
        if (players.ContainsKey(playerId))
        {
            players[playerId].isAlive = isAlive;

            if (players[playerId].statusIndicator != null)
            {
                players[playerId].statusIndicator.sprite = isAlive ? aliveIndicatorSprite : deadIndicatorSprite;
            }
        }
    }

    #endregion

    #region Quick Chat System

    public void ShowPlayerQuickChat(int playerId, int quickChatIndex)
    {
        if (!players.ContainsKey(playerId) || quickChatIndex < 0 || quickChatIndex >= quickChatImages.Length)
            return;

        PlayerUIData playerData = players[playerId];

        if (playerData.quickChatIndicator != null && playerData.quickChatImage != null)
        {
            // Stop any existing quick chat coroutine for this player
            StopPlayerQuickChat(playerId);

            // Set the quick chat image
            playerData.quickChatImage.sprite = quickChatImages[quickChatIndex].GetComponent<Image>().sprite;

            // Start the quick chat display coroutine
            Coroutine quickChatCoroutine = StartCoroutine(ShowQuickChatCoroutine(playerId));
            activeQuickChatCoroutines.Add(quickChatCoroutine);
        }
    }

    private void StopPlayerQuickChat(int playerId)
    {
        if (players.ContainsKey(playerId))
        {
            PlayerUIData playerData = players[playerId];
            if (playerData.quickChatIndicator != null)
                playerData.quickChatIndicator.SetActive(false);
        }
    }

    private IEnumerator ShowQuickChatCoroutine(int playerId)
    {
        if (!players.ContainsKey(playerId))
            yield break;

        PlayerUIData playerData = players[playerId];

        // Show quick chat indicator
        if (playerData.quickChatIndicator != null)
            playerData.quickChatIndicator.SetActive(true);

        // Wait for duration
        yield return new WaitForSeconds(quickChatDisplayDuration);

        // Hide quick chat indicator
        if (playerData.quickChatIndicator != null)
            playerData.quickChatIndicator.SetActive(false);
    }

    #endregion

    #region Helper Methods

    private void SetPlayerCharacterIcon(PlayerUIData playerData, int characterIndex)
    {
        if (playerData.playerIcon == null) return;

        Sprite[] characterIcons = playerData.isSurvivor ? survivorCharacterIcons : hunterCharacterIcons;

        if (characterIndex >= 0 && characterIndex < characterIcons.Length)
        {
            playerData.playerIcon.sprite = characterIcons[characterIndex];
        }
        else
        {
            Debug.LogWarning($"Invalid character index {characterIndex} for player {playerData.playerId}");
        }
    }

    private void UpdatePlayerIconsLayout()
    {
        // Simple layout update - you can make this more sophisticated
        int playerCount = 0;
        foreach (var player in players.Values)
        {
            if (player.playerIconObject != null)
            {
                // Position based on player count
                RectTransform rt = player.playerIconObject.GetComponent<RectTransform>();
                if (rt != null)
                {
                    int row = playerCount / maxPlayersPerRow;
                    int col = playerCount % maxPlayersPerRow;

                    float x = col * (rt.sizeDelta.x + iconSpacing);
                    float y = -row * (rt.sizeDelta.y + iconSpacing);

                    rt.anchoredPosition = new Vector2(x, y);
                }
                playerCount++;
            }
        }
    }

    public bool IsPlayerAlive(int playerId)
    {
        if (players.ContainsKey(playerId))
            return players[playerId].isAlive;
        return false;
    }

    public int GetPlayerCount()
    {
        return players.Count;
    }

    public int GetAlivePlayerCount()
    {
        int count = 0;
        foreach (var player in players.Values)
        {
            if (player.isAlive)
                count++;
        }
        return count;
    }

    #endregion
}