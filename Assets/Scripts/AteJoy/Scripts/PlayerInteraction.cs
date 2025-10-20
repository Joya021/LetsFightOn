using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class PlayerInteraction : MonoBehaviour
{
    public Inventory inventory;
    public LessonPopup lessonPopup;

    private bool isLessonActive = false; // Trigger guard

    void OnTriggerEnter2D(Collider2D other)
    {
        if (isLessonActive) return; // Prevent multiple triggers

        if (other.CompareTag("LearnableObject"))
        {
            LearnableObject obj = other.GetComponent<LearnableObject>();
            if (obj != null)
            {
                isLessonActive = true;

                // Pass object data to the popup
                lessonPopup.ShowLesson(
                    obj.learnableObjectSprite,
                    obj.learnableObjectSprite1,
                    obj.learnableObjectSprite2,
                    obj.closeButtonSprite1,
                    obj.closeButtonSprite2,
                    obj.closeButtonSprite3,
                    obj.lessonSprite,
                    obj.brokenCodeSprite,
                    obj.objectObtainedMessage,
                    obj.successMessageSprite,
                    obj.expectedFix,
                    obj.itemID,
                    inventory,
                    obj
                );
            }
        }
    }

    // Called by LessonPopup when the lesson is closed
    public void ResetLessonFlag()
    {
        isLessonActive = false;
    }

    // 🕒 Optional: add a delay before resetting
    public void ResetLessonFlagDelayed()
    {
        StartCoroutine(ResetLessonFlagCoroutine());
    }

    private IEnumerator ResetLessonFlagCoroutine()
    {
        yield return new WaitForSeconds(0.2f);
        isLessonActive = false;
    }
}

