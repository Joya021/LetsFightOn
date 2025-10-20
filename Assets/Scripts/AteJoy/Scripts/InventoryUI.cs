using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InventoryUI : MonoBehaviour
{
    [Header("Inventory Panel")]
    public GameObject inventoryPanel; // The panel that shows your inventory

    [Header("Settings Panel")]
    public GameObject settingsPanel; // The settings panel to open
    public Button settingsButton;    // The button from the Inventory panel that opens Settings
    public Button settingsCloseButton; // Optional close button inside Settings

    [Header("Optional: Start Open")]
    public bool openOnStart = false; // Set true if you want inventory visible at launch

    [Header("Inventory Elements")]
    public Transform itemSlotContainer;
    public GameObject itemSlotPrefab;
    public LessonPopup lessonPopup;
    public Inventory inventory;

    void Start()
    {
        // Ensure the inventory panel starts in the correct state
        inventoryPanel.SetActive(openOnStart);

        // Ensure settings are hidden by default
        if (settingsPanel != null)
            settingsPanel.SetActive(false);

        // Hook up the Settings button
        if (settingsButton != null)
            settingsButton.onClick.AddListener(OpenSettings);

        // Hook up the Settings close button (if it exists)
        if (settingsCloseButton != null)
            settingsCloseButton.onClick.AddListener(CloseSettings);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (settingsPanel.activeSelf)
                CloseSettings();
            else if (inventoryPanel.activeSelf)
                CloseInventory();
        }
    }

    // =========================
    // 📦 INVENTORY METHODS
    // =========================
    public void ToggleInventory()
    {
        bool isActive = inventoryPanel.activeSelf;
        inventoryPanel.SetActive(!isActive);

        if (!isActive)
            RefreshInventoryUI();

        PlayerMovements player = FindObjectOfType<PlayerMovements>();
        if (player != null)
            player.canMove = !inventoryPanel.activeSelf;
    }

    public void CloseInventory()
    {
        if (inventoryPanel != null)
            inventoryPanel.SetActive(false);

        PlayerMovements player = FindObjectOfType<PlayerMovements>();
        if (player != null)
            player.canMove = true;
    }

    public void RefreshInventoryUI()
    {
        foreach (Transform child in itemSlotContainer)
            Destroy(child.gameObject);

        foreach (InventoryItem item in inventory.items)
        {
            GameObject slot = Instantiate(itemSlotPrefab, itemSlotContainer);
            InventorySlot slotScript = slot.GetComponent<InventorySlot>();
            slotScript.Setup(item, lessonPopup);
        }

        Debug.Log("Refreshing inventory UI. Items count: " + inventory.items.Count);
    }

    // =========================
    // ⚙️ SETTINGS METHODS
    // =========================
    public void OpenSettings()
    {
        // Close inventory to prevent overlap (optional)
        if (inventoryPanel != null)
            inventoryPanel.SetActive(false);

        if (settingsPanel != null)
            settingsPanel.SetActive(true);

        PlayerMovements player = FindObjectOfType<PlayerMovements>();
        if (player != null)
            player.canMove = false;

        Debug.Log("Settings Panel opened.");
    }

    public void CloseSettings()
    {
        if (settingsPanel != null)
            settingsPanel.SetActive(false);

        PlayerMovements player = FindObjectOfType<PlayerMovements>();
        if (player != null)
            player.canMove = true;

        Debug.Log("Settings Panel closed.");
    }
}
