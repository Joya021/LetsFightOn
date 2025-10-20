using UnityEngine;
using UnityEngine.UI;

public class PlayerStatusUI : MonoBehaviour
{
    [Header("UI References")]
    public Image characterIcon;
    public Text playerNameText;
    public Image statusImage;
    public GameObject healthBar;
    public Image healthFillImage;

    [Header("Character Icons")]
    public Sprite[] characterSprites; // Assign character icons (Rio, etc.)

    [Header("Status Sprites")]
    public Sprite aliveSprite;
    public Sprite deadSprite;

    private int playerId;
    private bool isSurvivor;
    private bool isAlive = true;
    private int currentHP;
    private int maxHP;

    public void Initialize(int id, string playerName, bool survivor, int characterIndex)
    {
        playerId = id;
        isSurvivor = survivor;
        isAlive = true;

        // Set player name
        if (playerNameText != null)
        {
            playerNameText.text = playerName;
        }

        // Set character icon
        if (characterIcon != null && characterSprites != null && characterIndex < characterSprites.Length)
        {
            characterIcon.sprite = characterSprites[characterIndex];
        }

        // Set initial status to alive
        SetAliveStatus(true);

        // Show health bar only for survivors
        if (healthBar != null)
        {
            healthBar.SetActive(isSurvivor);
        }
    }

    public void SetAliveStatus(bool alive)
    {
        isAlive = alive;

        if (statusImage != null)
        {
            statusImage.sprite = alive ? aliveSprite : deadSprite;
        }

        // Optional: Change visual style when dead
        if (!alive)
        {
            // Darken the UI element
            CanvasGroup canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
            canvasGroup.alpha = 0.5f;
        }
    }

    public void UpdateHealth(int current, int max)
    {
        if (!isSurvivor) return;

        currentHP = current;
        maxHP = max;

        if (healthFillImage != null)
        {
            healthFillImage.fillAmount = (float)currentHP / maxHP;
        }

        // Check if player died
        if (currentHP <= 0 && isAlive)
        {
            SetAliveStatus(false);
        }
    }

    public int GetPlayerId()
    {
        return playerId;
    }

    public bool IsSurvivor()
    {
        return isSurvivor;
    }

    public bool IsAlive()
    {
        return isAlive;
    }
}