using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine.UI;
using Hashtable = ExitGames.Client.Photon.Hashtable;
using UnityEngine.SceneManagement;

public class CharacterSelectionManager : MonoBehaviourPunCallbacks
{
    [System.Serializable]
    public class CharacterData
    {
        public Button characterButton;
        public GameObject selectionIndicator;
        [Tooltip("Assign the character prefab directly here (instead of typing name).")]
        public GameObject characterPrefab;
        [Tooltip("Voiceline that plays when this character is locked in")]
        public AudioClip lockInVoiceline;
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

    [Header("Default Characters")]
    [Tooltip("Default survivor character prefab (e.g., June)")]
    public GameObject defaultSurvivorPrefab;
    [Tooltip("Default hunter character prefab (e.g., RedCloaked)")]
    public GameObject defaultHunterPrefab;

    [Header("Audio Settings")]
    [Tooltip("AudioSource for playing character voicelines (local only)")]
    public AudioSource voicelineAudioSource;

    [Header("Role-specific Canvases")]
    public GameObject hunterSelectedCanvas;
    public GameObject survivorSelectedCanvas;
    [Header("Role Configuration")]
    [Tooltip("If enabled, all players will be survivors. No hunter will be assigned.")]
    public bool allSurvivorsMode = false;

    [Header("UI Elements")]
    public Button lockInButton;
    public GameObject selectedCharacterDisplay;
    public Image selectedCharacterIcon;
    public Text roleText;
    public Text timerText;

    [Header("Selected Character Panel")]
    [Tooltip("First 3 slots should be for survivors (horizontally aligned), 4th slot for hunter (below survivors)")]
    public SelectedPlayerUI[] playerSlots; // Ensure you have at least 4 slots in Inspector

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

        // Setup AudioSource if not assigned
        if (voicelineAudioSource == null)
        {
            voicelineAudioSource = gameObject.GetComponent<AudioSource>();
            if (voicelineAudioSource == null)
            {
                voicelineAudioSource = gameObject.AddComponent<AudioSource>();
            }
        }

        // *** CRITICAL: Enable auto-sync for character selection → game transition ***
        PhotonNetwork.AutomaticallySyncScene = true;
        Debug.Log("[CharacterSelection] Auto scene sync ENABLED");

        // CRITICAL: Clear old timer when scene loads (fixes timer issue when returning from game)
        if (PhotonNetwork.IsMasterClient)
        {
            ClearOldSelectionTimer();
            AssignUniqueRolesToPlayers();
            SetSelectionEndTime();
        }
        else
        {
            if (PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey(SELECTION_END_TIME_KEY))
            {
                selectionEndTime = (double)PhotonNetwork.CurrentRoom.CustomProperties[SELECTION_END_TIME_KEY];

                // Check if timer is already expired (when returning from game scene)
                if (PhotonNetwork.Time >= selectionEndTime)
                {
                    Debug.Log("[CharacterSelection] Old timer detected, waiting for MasterClient to reset");
                    timerStarted = false;
                }
                else
                {
                    timerStarted = true;
                }
            }
        }

        StartCoroutine(WaitForRoleAssignment());
    }

    void ClearOldSelectionTimer()
    {
        if (PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey(SELECTION_END_TIME_KEY))
        {
            Hashtable roomProps = new Hashtable();
            roomProps[SELECTION_END_TIME_KEY] = null;
            PhotonNetwork.CurrentRoom.SetCustomProperties(roomProps);
            Debug.Log("[CharacterSelection] Cleared old selection timer");
        }
    }

    void AssignUniqueRolesToPlayers()
    {
        Player[] players = PhotonNetwork.PlayerList;
        if (players.Length == 0) return;

        foreach (Player p in players)
        {
            Hashtable clearProps = new Hashtable();
            clearProps[PLAYER_ROLE] = null;
            clearProps[PLAYER_CHARACTER] = null;
            clearProps[PLAYER_LOCKED_IN] = false;
            p.SetCustomProperties(clearProps);
        }

        if (allSurvivorsMode)
        {
            foreach (Player p in players)
            {
                Hashtable survivorProp = new Hashtable();
                survivorProp[PLAYER_ROLE] = false; // false = survivor
                p.SetCustomProperties(survivorProp);
            }

            Debug.Log("[RoleAssignment] All-Survivors Mode ENABLED - no hunters assigned");
            return;
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
        Hashtable roomProps = new Hashtable();
        roomProps[SELECTION_END_TIME_KEY] = PhotonNetwork.Time + selectionTime;
        PhotonNetwork.CurrentRoom.SetCustomProperties(roomProps);
        selectionEndTime = PhotonNetwork.Time + selectionTime;
        timerStarted = true;

        Debug.Log($"[CharacterSelection] New timer set: {selectionTime}s");
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
        ShowDefaultCharacter();
        UpdateSelectedCharacterPanel();
    }

    void ShowDefaultCharacter()
    {
        if (selectedCharacterDisplay != null && selectedCharacterIcon != null)
        {
            selectedCharacterDisplay.SetActive(true);

            GameObject defaultPrefab = isHunter ? defaultHunterPrefab : defaultSurvivorPrefab;

            if (defaultPrefab != null)
            {
                int defaultIndex = FindCharacterIndexByPrefab(defaultPrefab);

                if (defaultIndex >= 0 && defaultIndex < currentCharacters.Length)
                {
                    if (currentCharacters[defaultIndex].selectionIndicator != null)
                    {
                        currentCharacters[defaultIndex].selectionIndicator.SetActive(true);
                    }

                    selectedCharacterIndex = defaultIndex;

                    Image btnImg = currentCharacters[defaultIndex].characterButton?.GetComponent<Image>();
                    if (btnImg != null)
                    {
                        selectedCharacterIcon.sprite = btnImg.sprite;
                        selectedCharacterIcon.color = btnImg.color;
                    }
                }
            }
        }
    }

    int FindCharacterIndexByPrefab(GameObject prefab)
    {
        if (prefab == null) return -1;

        for (int i = 0; i < currentCharacters.Length; i++)
        {
            if (currentCharacters[i].characterPrefab == prefab)
            {
                return i;
            }
        }
        return -1;
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
            lockInButton.interactable = true;
        }
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
                    AutoPickDefaultCharacter();
                }

                if (PhotonNetwork.IsMasterClient)
                {
                    Debug.Log("[CharacterSelection] MasterClient loading game scene for ALL players");
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

        if (selectedCharacterDisplay != null && selectedCharacterIcon != null)
        {
            selectedCharacterDisplay.SetActive(true);

            Image btnImg = currentCharacters[selectedCharacterIndex].characterButton?.GetComponent<Image>();
            if (btnImg != null)
            {
                selectedCharacterIcon.sprite = btnImg.sprite;
                selectedCharacterIcon.color = btnImg.color;
            }
        }

        if (lockInButton != null)
            lockInButton.interactable = true;
    }

    public void LockInCharacter()
    {
        if (isLockedIn) return;

        if (selectedCharacterIndex < 0)
        {
            AutoPickDefaultCharacter();
            return;
        }

        CommitCharacterSelection();
    }

    private void AutoPickDefaultCharacter()
    {
        if (currentCharacters.Length == 0) return;

        GameObject defaultPrefab = isHunter ? defaultHunterPrefab : defaultSurvivorPrefab;

        if (defaultPrefab != null)
        {
            int defaultIndex = FindCharacterIndexByPrefab(defaultPrefab);

            if (defaultIndex >= 0)
            {
                selectedCharacterIndex = defaultIndex;
                Debug.Log($"[CharacterSelection] Auto-picked default character: {defaultPrefab.name}");
            }
            else
            {
                selectedCharacterIndex = 0;
                Debug.LogWarning($"[CharacterSelection] Default prefab not found in character list, using first character");
            }
        }
        else
        {
            selectedCharacterIndex = 0;
            Debug.LogWarning($"[CharacterSelection] No default prefab assigned, using first character");
        }

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
                Image btnImg = currentCharacters[selectedCharacterIndex].characterButton?.GetComponent<Image>();
                if (btnImg != null)
                {
                    selectedCharacterIcon.sprite = btnImg.sprite;
                    selectedCharacterIcon.color = btnImg.color;
                }
            }
        }

        // *** NEW: Play character voiceline (local only) ***
        PlayCharacterVoiceline();

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
        props["CharacterIndex"] = selectedCharacterIndex;
        PhotonNetwork.LocalPlayer.SetCustomProperties(props);

        foreach (var charData in currentCharacters)
        {
            if (charData.characterButton != null)
                charData.characterButton.interactable = false;
        }

        if (lockInButton != null)
            lockInButton.interactable = false;

        Debug.Log($"[CharacterSelection] {PhotonNetwork.NickName} selected prefab: {prefabName}, CharacterIndex: {selectedCharacterIndex}");
        UpdateSelectedCharacterPanel();
    }

    // *** NEW: Play voiceline for the selected character (local only) ***
    private void PlayCharacterVoiceline()
    {
        if (voicelineAudioSource == null)
        {
            Debug.LogWarning("[CharacterSelection] No AudioSource assigned for voicelines");
            return;
        }

        if (selectedCharacterIndex < 0 || selectedCharacterIndex >= currentCharacters.Length)
        {
            Debug.LogWarning("[CharacterSelection] Invalid character index for voiceline");
            return;
        }

        AudioClip voiceline = currentCharacters[selectedCharacterIndex].lockInVoiceline;

        if (voiceline != null)
        {
            voicelineAudioSource.PlayOneShot(voiceline);
            Debug.Log($"[CharacterSelection] Playing voiceline for {currentCharacters[selectedCharacterIndex].characterPrefab?.name}");
        }
        else
        {
            Debug.Log($"[CharacterSelection] No voiceline assigned for {currentCharacters[selectedCharacterIndex].characterPrefab?.name}");
        }
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

        List<Player> survivors = new List<Player>();
        Player hunterPlayer = null;

        foreach (Player p in players)
        {
            bool playerIsHunter = p.CustomProperties.ContainsKey(PLAYER_ROLE) && (bool)p.CustomProperties[PLAYER_ROLE];

            if (playerIsHunter)
            {
                hunterPlayer = p;
            }
            else
            {
                survivors.Add(p);
            }
        }

        int slotIndex = 0;

        for (int i = 0; i < 3; i++)
        {
            if (i < survivors.Count)
            {
                DisplayPlayerInSlot(playerSlots[slotIndex], survivors[i], false);
            }
            else
            {
                playerSlots[slotIndex].container.SetActive(false);
            }
            slotIndex++;
        }

        if (hunterPlayer != null && slotIndex < playerSlots.Length)
        {
            DisplayPlayerInSlot(playerSlots[slotIndex], hunterPlayer, true);
            slotIndex++;
        }

        for (int i = slotIndex; i < playerSlots.Length; i++)
        {
            playerSlots[i].container.SetActive(false);
        }
    }

    // FIXED: Helper method to display player info in a slot
    private void DisplayPlayerInSlot(SelectedPlayerUI slot, Player p, bool isHunterSlot)
    {
        slot.container.SetActive(true);

        // FIXED: Call SetPlayerInfo on UserInfoDisplay
        UserInfoDisplay userInfo = slot.container.GetComponent<UserInfoDisplay>();
        if (userInfo != null)
        {
            userInfo.SetPlayerInfo(p.NickName);
            Debug.Log($"[CharacterSelection] Slot: Set player name to '{p.NickName}'");
        }
        else
        {
            // Fallback: set text directly
            if (slot.playerNameText != null)
                slot.playerNameText.text = p.NickName;
            Debug.LogWarning("[CharacterSelection] No UserInfoDisplay component found on slot container");
        }

        bool playerIsHunter = p.CustomProperties.ContainsKey(PLAYER_ROLE) && (bool)p.CustomProperties[PLAYER_ROLE];
        if (slot.roleText != null)
            slot.roleText.text = playerIsHunter ? "Hunter" : "Survivor";

        if (p.CustomProperties.ContainsKey(PLAYER_LOCKED_IN) && (bool)p.CustomProperties[PLAYER_LOCKED_IN])
        {
            string prefabName = p.CustomProperties[PLAYER_CHARACTER] as string;
            Sprite selectedIcon = GetCharacterIconByPrefabName(prefabName, playerIsHunter);

            if (selectedIcon != null)
            {
                slot.characterIcon.sprite = selectedIcon;
                slot.characterIcon.color = Color.white;
            }
            else
            {
                slot.characterIcon.sprite = null;
                slot.characterIcon.color = Color.clear;
            }
        }
        else
        {
            GameObject defaultPrefab = playerIsHunter ? defaultHunterPrefab : defaultSurvivorPrefab;
            if (defaultPrefab != null)
            {
                Sprite defaultIcon = GetCharacterIconByPrefabName(defaultPrefab.name, playerIsHunter);
                if (defaultIcon != null)
                {
                    slot.characterIcon.sprite = defaultIcon;
                    slot.characterIcon.color = new Color(1f, 1f, 1f, 0.5f);
                }
                else
                {
                    slot.characterIcon.sprite = null;
                    slot.characterIcon.color = Color.clear;
                }
            }
            else
            {
                slot.characterIcon.sprite = null;
                slot.characterIcon.color = Color.clear;
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
            object endTimeObj = propertiesThatChanged[SELECTION_END_TIME_KEY];

            if (endTimeObj == null)
            {
                timerStarted = false;
                Debug.Log("[CharacterSelection] Timer cleared by MasterClient");
                return;
            }

            selectionEndTime = (double)endTimeObj;

            if (PhotonNetwork.Time < selectionEndTime)
            {
                timerStarted = true;
                Debug.Log("[CharacterSelection] Timer updated from room properties");
            }
            else
            {
                timerStarted = false;
                Debug.Log("[CharacterSelection] Received expired timer, not starting");
            }
        }
    }
}