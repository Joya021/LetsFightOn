using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LearnableObject : MonoBehaviour
{
    public bool isCompleted = false;

    public Sprite learnableObjectSprite;
    public Sprite learnableObjectSprite1;
    public Sprite learnableObjectSprite2;
    public Sprite closeButtonSprite1;
    public Sprite closeButtonSprite2;
    public Sprite closeButtonSprite3;
    public Sprite lessonSprite;
    public Sprite sampleCodeSprite;
    public Sprite brokenCodeSprite;
    public Sprite successMessageSprite;

    [SerializeField]
    [TextArea(3, 10)] // Min 3 lines, max 10 lines
    public string expectedFix;

    public string itemID;

    public void MarkAsCompleted()
    {
        isCompleted = true;
        // Optional: disable collider or visual cue
        GetComponent<Collider2D>().enabled = false;
        // You could also change the sprite or add a glow effect
    }
}
