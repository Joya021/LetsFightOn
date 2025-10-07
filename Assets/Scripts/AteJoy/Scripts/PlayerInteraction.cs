using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class PlayerInteraction : MonoBehaviour
{
    public Inventory inventory;
    public LessonPopup lessonPopup;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("LearnableObject"))
        {
            LearnableObject obj = other.GetComponent<LearnableObject>();

            // Pass object data to the popup
            lessonPopup.ShowLesson(
                obj.learnableObjectSprite,
                obj.learnableObjectSprite1,
                obj.learnableObjectSprite2,
                obj.closeButtonSprite1,
                obj.closeButtonSprite2,
                obj.closeButtonSprite3,
                obj.lessonSprite,
                obj.sampleCodeSprite,
                obj.brokenCodeSprite,
                obj.successMessageSprite,
                obj.expectedFix,
                obj.itemID,
                inventory,
                obj
            );
        }
    }
}
