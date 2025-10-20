using UnityEngine;

public class ComputerStation : MonoBehaviour
{
    private BrokenCode assignedCode;
    private bool isSolved = false;
    private bool playerInRange = false;

    [SerializeField] private GameObject clearedOverlay;         // Image to show when cleared
    [SerializeField] private SpriteRenderer stationSprite;      // Main visual to dim

    public void AssignCode(BrokenCode code)
    {
        assignedCode = code;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player") || isSolved) return;
        playerInRange = true;
        // Optional: show “Press F to interact” prompt here
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        playerInRange = false;
        // Optional: hide prompt here
    }

    void Update()
    {
        if (playerInRange && !isSolved && Input.GetKeyDown(KeyCode.F))
        {
            if (BrokenCodePopupUI.Instance != null && !BrokenCodePopupUI.Instance.IsPopupOpen)
            {
                BrokenCodePopupUI.Instance.ShowPopup(assignedCode, this);
            }
        }
    }

    public void MarkSolved()
    {
        isSolved = true;

        // Dim the station visually
        if (stationSprite != null)
            stationSprite.color = new Color(1f, 1f, 1f, 0.9f); // semi-transparent

        // Show cleared overlay
        if (clearedOverlay != null)
            clearedOverlay.SetActive(true);

        // Notify the manager that a station has been solved
        if (C1BrokenCodesManager.Instance != null)
            C1BrokenCodesManager.Instance.CheckAllStationsCleared();
    }

    public bool IsSolved()
    {
        return isSolved;
    }

    public bool CanInteract()
    {
        return !isSolved;
    }
}