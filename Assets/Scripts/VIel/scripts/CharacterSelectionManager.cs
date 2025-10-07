// CharacterSelectionManager.cs
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine.UI;
using Hashtable = ExitGames.Client.Photon.Hashtable;
using UnityEngine.SceneManagement;

/// <summary>
/// Modified to require assigned prefabs via inspector for both survivors and hunters.
/// No hardcoded 'Hunter' name fallback.
/// Sends the selected prefab's name (prefab.name) to Photon custom properties for consistent spawning.
/// </summary>
public class CharacterSelectionManager : MonoBehaviourPunCallbacks
{
    [System.Serializable]
    public class CharacterData
    {
        public Button characterButton;
        public GameObject selectionIndicator;
        [Tooltip("Assign the character prefab directly here (instead of typing name).")]
        public GameObject characterPrefab;
    }

    [System.Serializable]
    public class SelectedPlayerUI
    {
        public GameObject container;
        public Image characterIcon;
        public Text playerNameText;
        public Text roleText;
    }

    [Header("Survivor Character Options")]
    public CharacterData[] survivorCharacters;

    [Header("Hunter Character Options")]
    public CharacterData[] hunterCharacters;

    [Header("Role-specific Canvases")]
    public GameObject hunterSelectedCanvas;
    public GameObject survivorSelectedCanvas;

    [Header("UI Elements")]
    public Button lockInButton;
    public GameObject selectedCharacterDisplay;
    public Image selectedCharacterIcon;
    public Text roleText;
    public Text timerText;

    [Header("Selected Character Panel")]
    public SelectedPlayerUI[] playerSlots;

    [Header("Timer Settings")]
    public float selectionTime = 15f;
    private float remainingTime;
    private bool timerStarted = false;

    private int selectedCharacterIndex = -1;
    private CharacterData[] currentCharacters;
    private bool isHunter = false;
    private bool isLockedIn = false;

    private const string PLAYER_ROLE = "PlayerRole";
    private const string PLAYER_CHARACTER = "PlayerCharacter";
    private const string PLAYER_LOCKED_IN = "PlayerLockedIn";
    private const string SELECTION_END_TIME_KEY = "SelectionEndTime";
    private double selectionEndTime = 0.0;

    void Start()
    {
        remainingTime = selectionTime;

        if (PhotonNetwork.IsMasterClient)
        {
            AssignUniqueRolesToPlayers();
            SetSelectionEndTime();
        }
        else
        {
            if (PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey(SELECTION_END_TIME_KEY))
            {
                selectionEndTime = (double)PhotonNetwork.CurrentRoom.CustomProperties[SELECTION_END_TIME_KEY];
                timerStarted = true;
            }
        }

        StartCoroutine(WaitForRoleAssignment());
    }

    void AssignUniqueRolesToPlayers()
    {
        Player[] players = PhotonNetwork.PlayerList;
        if (players.Length == 0) return;

        foreach (Player p in players)
        {
            Hashtable clearProps = new Hashtable();
            clearProps[PLAYER_ROLE] = null;
            p.SetCustomProperties(clearProps);
        }

        int hunterIndex = Random.Range(0, players.Length);
        Player hunter = players[hunterIndex];

        Hashtable hunterProp = new Hashtable();
        hunterProp[PLAYER_ROLE] = true;
        hunter.SetCustomProperties(hunterProp);

        for (int i = 0; i < players.Length; i++)
        {
            if (i == hunterIndex) continue;
            Hashtable survivorProp = new Hashtable();
            survivorProp[PLAYER_ROLE] = false;
            players[i].SetCustomProperties(survivorProp);
        }

        Debug.Log($"[RoleAssignment] Hunter: {hunter.NickName}");
    }

    void SetSelectionEndTime()
    {
        if (!PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey(SELECTION_END_TIME_KEY))
        {
            Hashtable roomProps = new Hashtable();
            roomProps[SELECTION_END_TIME_KEY] = PhotonNetwork.Time + selectionTime;
            PhotonNetwork.CurrentRoom.SetCustomProperties(roomProps);
            selectionEndTime = PhotonNetwork.Time + selectionTime;
        }
        else
        {
            selectionEndTime = (double)PhotonNetwork.CurrentRoom.CustomProperties[SELECTION_END_TIME_KEY];
        }

        timerStarted = true;
    }

    IEnumerator WaitForRoleAssignment()
    {
        while (!PhotonNetwork.LocalPlayer.CustomProperties.ContainsKey(PLAYER_ROLE))
        {
            yield return null;
        }

        isHunter = (bool)PhotonNetwork.LocalPlayer.CustomProperties[PLAYER_ROLE];
        currentCharacters = isHunter ? hunterCharacters : survivorCharacters;

        hunterSelectedCanvas?.SetActive(isHunter);
        survivorSelectedCanvas?.SetActive(!isHunter);

        if (roleText != null)
            roleText.text = isHunter ? "HUNTER" : "SURVIVOR";

        SetupCharacterSelection();
        UpdateSelectedCharacterPanel();
    }

    void SetupCharacterSelection()
    {
        for (int i = 0; i < currentCharacters.Length; i++)
        {
            int index = i;
            if (currentCharacters[i].characterButton != null)
            {
                currentCharacters[i].characterButton.onClick.RemoveAllListeners();
                currentCharacters[i].characterButton.onClick.AddListener(() => SelectCharacter(index));
            }

            if (currentCharacters[i].selectionIndicator != null)
                currentCharacters[i].selectionIndicator.SetActive(false);
        }

        if (lockInButton != null)
        {
            lockInButton.onClick.RemoveAllListeners();
            lockInButton.onClick.AddListener(LockInCharacter);
            lockInButton.interactable = false;
        }

        selectedCharacterDisplay?.SetActive(false);
    }

    void Update()
    {
        if (timerStarted)
        {
            remainingTime = (float)(selectionEndTime - PhotonNetwork.Time);
            if (remainingTime < 0) remainingTime = 0f;

            if (timerText != null)
                timerText.text = $"Time: {Mathf.CeilToInt(remainingTime)}s";

            if (PhotonNetwork.Time >= selectionEndTime)
            {
                if (!isLockedIn)
                {
                    AutoPickRandomCharacter();
                }

                if (PhotonNetwork.IsMasterClient)
                {
                    PhotonNetwork.LoadLevel("GameLoadingScreen");
                }

                timerStarted = false;
            }
        }
    }

    public void SelectCharacter(int characterIndex)
    {
        if (isLockedIn || characterIndex < 0 || characterIndex >= currentCharacters.Length)
            return;

        if (selectedCharacterIndex >= 0 && selectedCharacterIndex < currentCharacters.Length)
        {
            currentCharacters[selectedCharacterIndex].selectionIndicator?.SetActive(false);
        }

        selectedCharacterIndex = characterIndex;
        currentCharacters[selectedCharacterIndex].selectionIndicator?.SetActive(true);

        if (lockInButton != null)
            lockInButton.interactable = true;
    }

    public void LockInCharacter()
    {
        if (isLockedIn) return;

        if (selectedCharacterIndex < 0)
        {
            AutoPickRandomCharacter();
            return;
        }

        CommitCharacterSelection();
    }

    private void AutoPickRandomCharacter()
    {
        if (currentCharacters.Length == 0) return;

        if (selectedCharacterIndex < 0)
            selectedCharacterIndex = Random.Range(0, currentCharacters.Length);

        CommitCharacterSelection();
    }

    private void CommitCharacterSelection()
    {
        if (isLockedIn) return;
        isLockedIn = true;

        if (selectedCharacterDisplay != null && selectedCharacterIcon != null)
        {
            selectedCharacterDisplay.SetActive(true);

            if (selectedCharacterIndex >= 0 && selectedCharacterIndex < currentCharacters.Length)
            {
                Image btnImg = currentCharacters[selectedCharacterIndex].characterButton.GetComponent<Image>();
                if (btnImg != null)
                {
                    selectedCharacterIcon.sprite = btnImg.sprite;
                    selectedCharacterIcon.color = btnImg.color;
                }
            }
        }

        string prefabName = "";
        if (selectedCharacterIndex >= 0 && selectedCharacterIndex < currentCharacters.Length)
        {
            GameObject assignedPrefab = currentCharacters[selectedCharacterIndex].characterPrefab;
            if (assignedPrefab != null)
                prefabName = assignedPrefab.name;
            else
                Debug.LogWarning("[CharacterSelectionManager] Selected CharacterData.characterPrefab is null - please assign it in the Inspector.");
        }

        Hashtable props = new Hashtable();
        props[PLAYER_CHARACTER] = prefabName;
        props[PLAYER_LOCKED_IN] = true;
        PhotonNetwork.LocalPlayer.SetCustomProperties(props);

        foreach (var charData in currentCharacters)
        {
            if (charData.characterButton != null)
                charData.characterButton.interactable = false;
        }

        if (lockInButton != null)
            lockInButton.interactable = false;

        Debug.Log($"[CharacterSelection] {PhotonNetwork.NickName} selected prefab: {prefabName}");
        UpdateSelectedCharacterPanel();
    }

    private Sprite GetCharacterIconByPrefabName(string prefabName, bool isHunter)
    {
        CharacterData[] chars = isHunter ? hunterCharacters : survivorCharacters;
        foreach (var c in chars)
        {
            if (c.characterPrefab != null && c.characterPrefab.name == prefabName && c.characterButton != null)
            {
                Image img = c.characterButton.GetComponent<Image>();
                if (img != null)
                    return img.sprite;
            }
        }
        return null;
    }

    private void UpdateSelectedCharacterPanel()
    {
        Player[] players = PhotonNetwork.PlayerList;

        for (int i = 0; i < playerSlots.Length; i++)
        {
            if (i >= players.Length)
            {
                playerSlots[i].container.SetActive(false);
                continue;
            }

            Player p = players[i];
            playerSlots[i].container.SetActive(true);

            playerSlots[i].playerNameText.text = p.NickName;

            bool playerIsHunter = p.CustomProperties.ContainsKey(PLAYER_ROLE) && (bool)p.CustomProperties[PLAYER_ROLE];
            playerSlots[i].roleText.text = playerIsHunter ? "Hunter" : "Survivor";

            if (p.CustomProperties.ContainsKey(PLAYER_LOCKED_IN) && (bool)p.CustomProperties[PLAYER_LOCKED_IN])
            {
                string prefabName = p.CustomProperties[PLAYER_CHARACTER] as string;
                Sprite selectedIcon = GetCharacterIconByPrefabName(prefabName, playerIsHunter);

                if (selectedIcon != null)
                {
                    playerSlots[i].characterIcon.sprite = selectedIcon;
                    playerSlots[i].characterIcon.color = Color.white;
                }
                else
                {
                    playerSlots[i].characterIcon.sprite = null;
                    playerSlots[i].characterIcon.color = Color.clear;
                }
            }
            else
            {
                playerSlots[i].characterIcon.sprite = null;
                playerSlots[i].characterIcon.color = Color.clear;
            }
        }
    }

    public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
    {
        if (changedProps.ContainsKey(PLAYER_CHARACTER) || changedProps.ContainsKey(PLAYER_LOCKED_IN) || changedProps.ContainsKey(PLAYER_ROLE))
        {
            UpdateSelectedCharacterPanel();
        }
    }

    public override void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged)
    {
        if (propertiesThatChanged.ContainsKey(SELECTION_END_TIME_KEY))
        {
            selectionEndTime = (double)propertiesThatChanged[SELECTION_END_TIME_KEY];
            timerStarted = true;
        }
    }
}
