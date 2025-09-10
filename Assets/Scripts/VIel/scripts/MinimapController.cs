using UnityEngine;

public class MinimapController : MonoBehaviour
{
    [Header("References")]
    public Transform player;
    public Camera minimapCamera;

    [Header("Minimap Settings")]
    public float followHeight = 10f;
    public bool smoothFollow = true;
    public float followSpeed = 5f;

    [Header("Layers")]
    public string intercomLayerName = "SHIBAL";
    private int intercomLayer;

    [Header("Hunter Mode")]
    public bool isHunterMode = false; // Set this to true in the hunter scene

    void Start()
    {
        if (player == null)
            player = GameObject.FindGameObjectWithTag("Player")?.transform;

        if (minimapCamera == null)
            minimapCamera = GetComponent<Camera>();

        if (minimapCamera != null)
        {
            minimapCamera.orthographic = true;
            intercomLayer = LayerMask.NameToLayer(intercomLayerName);

            if (intercomLayer == -1)
            {
                Debug.LogWarning($"[MinimapController] Layer '{intercomLayerName}' does not exist!");
            }
            else
            {
                if (isHunterMode)
                {
                    // Hunter can see intercoms immediately
                    minimapCamera.cullingMask |= (1 << intercomLayer);
                }
                else
                {
                    // Hide SHIBAL layer at start for survivor
                    minimapCamera.cullingMask &= ~(1 << intercomLayer);
                }
            }
        }
    }

    void LateUpdate()
    {
        if (player == null || minimapCamera == null) return;

        Vector3 targetPosition = new Vector3(
            player.position.x,
            player.position.y + followHeight,
            player.position.z
        );

        if (smoothFollow)
        {
            transform.position = Vector3.Lerp(transform.position, targetPosition, followSpeed * Time.deltaTime);
        }
        else
        {
            transform.position = targetPosition;
        }

        transform.rotation = Quaternion.Euler(90f, 0f, 0f);
    }

    // Call this from InterCom when survivor interacts (not used in hunter mode)
    public void RevealIntercomLayer()
    {
        if (minimapCamera == null || intercomLayer == -1 || isHunterMode) return;

        minimapCamera.cullingMask |= (1 << intercomLayer);
    }

    // Method to toggle hunter mode (useful for testing)
    public void SetHunterMode(bool hunterMode)
    {
        isHunterMode = hunterMode;

        if (minimapCamera != null && intercomLayer != -1)
        {
            if (isHunterMode)
            {
                // Show intercoms for hunter
                minimapCamera.cullingMask |= (1 << intercomLayer);
            }
            else
            {
                // Hide intercoms for survivor (until revealed)
                minimapCamera.cullingMask &= ~(1 << intercomLayer);
            }
        }
    }
}