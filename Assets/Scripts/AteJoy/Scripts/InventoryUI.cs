using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InventoryUI : MonoBehaviour
{
    [Header("Inventory Panel")]
    public GameObject inventoryPanel; // The panel that shows your inventory

    [Header("Optional: Start Open")]
    public bool openOnStart = false; // Set true if you want inventory visible at launch

    public Transform itemSlotContainer;
    public GameObject itemSlotPrefab;
    public LessonPopup lessonPopup;
    public Inventory inventory;

    void Start()
    {
        // Ensure the inventory panel starts in the correct state
        inventoryPanel.SetActive(openOnStart);
    }

    // Close inventory directly
    public void CloseInventory()
    {
        if (inventoryPanel != null)
            inventoryPanel.SetActive(false);
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

    // Called when the bag button is clicked
    public void ToggleInventory()
    {
        bool isActive = inventoryPanel.activeSelf;
        inventoryPanel.SetActive(!isActive);

        if (!isActive)
            RefreshInventoryUI();
    }
}
