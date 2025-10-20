using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InventorySlot : MonoBehaviour
{
    public Image icon;
    private InventoryItem itemData;
    private LessonPopup lessonPopup;

    public void Setup(InventoryItem item, LessonPopup popup)
    {
        itemData = item;
        lessonPopup = popup;

        if (icon != null && itemData.learnableObjectSprite != null)
            icon.sprite = itemData.learnableObjectSprite;
    }

    public void OnClickSlot()
    {
        if (lessonPopup != null && itemData != null)
        {
            lessonPopup.ShowLessonFromInventory(itemData.learnableObjectSprite, itemData.lessonSprite);
        }
    }
}
