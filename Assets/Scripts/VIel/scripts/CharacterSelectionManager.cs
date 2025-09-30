using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class CharacterSelectionManager : MonoBehaviour
{
    [System.Serializable]
    public class CharacterData
    {
        public Button characterButton;
        public GameObject selectionIndicator; // Image to show above icon when selected
        public string sceneName; // Scene to load for this character
    }

    [Header("Character Selection")]
    public CharacterData[] survivorCharacters; // 4 survivors
    public CharacterData[] hunterCharacters;   // 3 hunters

    [Header("UI Elements")]
    public Button lockInButton;
    public GameObject selectedCharacterDisplay; // Where to show the locked-in character icon
    public Image selectedCharacterIcon; // The actual icon image component

    [Header("Role Settings")]
    public bool isHunterMode = false; // Set based on which canvas this is on

    [Header("Scene Settings")]
    public string defaultSurvivorScene = "SurvivorGameScene"; // Default scene for now
    public string defaultHunterScene = "HunterGameScene"; // For future use

    private int selectedCharacterIndex = -1;
    private CharacterData[] currentCharacters;

    void Start()
    {
        // Determine which character set to use
        currentCharacters = isHunterMode ? hunterCharacters : survivorCharacters;

        // Setup character button listeners
        for (int i = 0; i < currentCharacters.Length; i++)
        {
            if (currentCharacters[i].characterButton != null)
            {
                int index = i; // Capture for closure
                currentCharacters[i].characterButton.onClick.AddListener(() => SelectCharacter(index));
            }

            // Hide all selection indicators initially
            if (currentCharacters[i].selectionIndicator != null)
                currentCharacters[i].selectionIndicator.SetActive(false);
        }

        // Setup lock-in button
        if (lockInButton != null)
        {
            lockInButton.onClick.AddListener(LockInCharacter);
            lockInButton.interactable = false; // Disabled until character is selected
        }

        // Hide selected character display initially
        if (selectedCharacterDisplay != null)
            selectedCharacterDisplay.SetActive(false);
    }

    public void SelectCharacter(int characterIndex)
    {
        if (characterIndex < 0 || characterIndex >= currentCharacters.Length)
            return;

        // Hide previous selection indicator
        if (selectedCharacterIndex >= 0 && selectedCharacterIndex < currentCharacters.Length)
        {
            if (currentCharacters[selectedCharacterIndex].selectionIndicator != null)
                currentCharacters[selectedCharacterIndex].selectionIndicator.SetActive(false);
        }

        // Update selected character
        selectedCharacterIndex = characterIndex;

        // Show new selection indicator
        if (currentCharacters[selectedCharacterIndex].selectionIndicator != null)
            currentCharacters[selectedCharacterIndex].selectionIndicator.SetActive(true);

        // Enable lock-in button
        if (lockInButton != null)
            lockInButton.interactable = true;

        Debug.Log($"Selected character {selectedCharacterIndex}");
    }

    public void LockInCharacter()
    {
        if (selectedCharacterIndex < 0 || selectedCharacterIndex >= currentCharacters.Length)
            return;

        // Show the selected character in the display area
        if (selectedCharacterDisplay != null && selectedCharacterIcon != null)
        {
            selectedCharacterDisplay.SetActive(true);

            // Get the icon from the selected character button
            Image buttonImage = currentCharacters[selectedCharacterIndex].characterButton.GetComponent<Image>();
            if (buttonImage != null)
            {
                selectedCharacterIcon.sprite = buttonImage.sprite;
                selectedCharacterIcon.color = buttonImage.color;
            }
        }

        // For now, always load the default survivor scene as requested
        // In the future, you can use currentCharacters[selectedCharacterIndex].sceneName
        Debug.Log($"Locked in character {selectedCharacterIndex}. Loading scene...");
    }

        // Load the appropriate scene
       
    // Method to set role mode (can be called from other scripts)
    public void SetHunterMode(bool hunterMode)
    {
        isHunterMode = hunterMode;
        currentCharacters = isHunterMode ? hunterCharacters : survivorCharacters;

        // Reset selection when switching modes
        selectedCharacterIndex = -1;

        if (lockInButton != null)
            lockInButton.interactable = false;

        if (selectedCharacterDisplay != null)
            selectedCharacterDisplay.SetActive(false);
    }
}