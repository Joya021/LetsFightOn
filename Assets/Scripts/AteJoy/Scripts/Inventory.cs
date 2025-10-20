using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class InventoryItem
{
    public string itemID;
    public Sprite learnableObjectSprite;
    public Sprite lessonSprite;
    public Sprite sampleCodeSprite;
}

public class Inventory : MonoBehaviour
{
    public List<InventoryItem> items = new List<InventoryItem>();

    void Start()
    {
        items.Clear(); // Reset inventory on play
    }

    // Add a LearnableObject to the inventory
    public void AddItem(LearnableObject obj)
    {
        // Prevent duplicates
        if (items.Exists(item => item.itemID == obj.itemID))
        {
            Debug.Log("Item already exists: " + obj.itemID);
            return;
        }

        InventoryItem newItem = new InventoryItem
        {
            itemID = obj.itemID,
            learnableObjectSprite = obj.learnableObjectSprite,
            lessonSprite = obj.lessonSprite,
            sampleCodeSprite = obj.sampleCodeSprite,
        };

        items.Add(newItem);
        Debug.Log("Item added: " + obj.itemID);
    }
}