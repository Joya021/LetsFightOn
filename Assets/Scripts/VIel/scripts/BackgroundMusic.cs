using UnityEngine;

// Attach this script to your Pencil GameObject
public class PencilTriggerd : MonoBehaviour
{
    [Header("References")]
    public TutorialGameManager gameManager;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            gameManager.OnTriggerEnter2D(other);
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            gameManager.OnTriggerExit2D(other);
        }
    }
}