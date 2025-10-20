using UnityEngine;
using Photon.Pun;

public class StunController : MonoBehaviourPunCallbacks
{
    [Header("Stun Settings")]
    public float stunDuration = 3f;
    public float stunRange = 2f;
    public LayerMask hunterLayer = 1 << 7; // Hunter layer
    public KeyCode stunKey = KeyCode.E;

    [Header("Cooldown Settings")]
    public float stunCooldown = 5f;
    private float stunTimer = 0f;

    private PlayerMovement playerMovement;
    private PhotonView photonView;
    private bool isMultiplayer = false;

    void Start()
    {
        playerMovement = GetComponent<PlayerMovement>();
        photonView = GetComponent<PhotonView>();
        isMultiplayer = PhotonNetwork.IsConnected && photonView != null;
    }

    void Update()
    {
        // Only handle input for local player
        if (isMultiplayer && !photonView.IsMine) return;

        // Handle cooldown
        if (stunTimer > 0)
            stunTimer -= Time.deltaTime;

        // Handle stun input
        if (Input.GetKeyDown(stunKey) && CanStun())
        {
            TryStunHunter();
        }
    }

    bool CanStun()
    {
        return stunTimer <= 0f && playerMovement != null && playerMovement.canMove && !playerMovement.isStunned;
    }

    void TryStunHunter()
    {
        if (isMultiplayer)
        {
            // Find hunters in multiplayer
            GameObject[] allPlayers = GameObject.FindGameObjectsWithTag("Player");

            foreach (GameObject playerObj in allPlayers)
            {
                PhotonView targetView = playerObj.GetComponent<PhotonView>();
                if (targetView == null || targetView == photonView) continue;

                // Check if target is hunter
                if (targetView.Owner.CustomProperties.ContainsKey("PlayerRole"))
                {
                    bool targetIsHunter = (bool)targetView.Owner.CustomProperties["PlayerRole"];
                    if (!targetIsHunter) continue; // Skip survivors
                }

                float distance = Vector2.Distance(transform.position, playerObj.transform.position);
                if (distance <= stunRange)
                {
                    // Stun the hunter via RPC
                    photonView.RPC("RPC_StunTarget", RpcTarget.All, targetView.ViewID, stunDuration);

                    stunTimer = stunCooldown;
                    Debug.Log("Survivor stunned a hunter!");
                    break;
                }
            }
        }
        else
        {
            // Offline mode - find hunters by layer or tag
            Collider2D[] nearbyColliders = Physics2D.OverlapCircleAll(transform.position, stunRange, hunterLayer);

            foreach (Collider2D col in nearbyColliders)
            {
                if (col.gameObject == gameObject) continue; // Skip self

                StunnableScript hunterStunnable = col.GetComponent<StunnableScript>();
                if (hunterStunnable != null)
                {
                    hunterStunnable.Stun(stunDuration);
                    stunTimer = stunCooldown;
                    Debug.Log("Survivor stunned a hunter!");
                    break;
                }
            }
        }
    }

    [PunRPC]
    void RPC_StunTarget(int targetViewID, float duration)
    {
        PhotonView targetView = PhotonView.Find(targetViewID);
        if (targetView != null)
        {
            StunnableScript stunnable = targetView.GetComponent<StunnableScript>();
            if (stunnable != null)
            {
                stunnable.Stun(duration);
                Debug.Log($"Hunter {targetView.gameObject.name} was stunned by survivor!");
            }
        }
    }

    // Visual debug in scene view
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, stunRange);
    }
}