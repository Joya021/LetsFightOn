using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using Photon.Pun;

public class HunterController : MonoBehaviourPunCallbacks, IPunObservable
{
    [Header("References")]
    public GameManager gameManager;

    [Header("Movement")]
    public float normalMoveSpeed = 3f;
    public float rageMoveSpeed = 6f;
    public LayerMask obstacleLayer;
    public float avoidanceDistance = 1f;

    [Header("Network Smoothing")]
    public float positionLerpSpeed = 10f;

    [Header("Interrupt Task Settings")]
    public float interruptRange = 2f;
    public float interruptCooldown = 8f;
    public int maxInterrupts = 3;
    private int currentInterrupts = 0;
    private float interruptTimer = 0f;

    [Header("Stun Settings")]
    public float stunCooldown = 5f;  // Fixed: using the actual cooldown you want
    public int maxStuns = 5;
    public float stunDuration = 5f;
    private int currentStuns = 0;
    private float stunTimer = 0f;

    [Header("Rage Mode Settings")]
    public float rageDuration = 8f;
    public float rageCooldown = 20f;
    private float rageTimer = 0f;
    private bool isInRageMode = false;

    [Header("UI Elements")]
    public GameObject interruptCooldownCanvas;
    public Text interruptCooldownText;
    public GameObject stunCooldownCanvas;
    public Text stunCooldownText;
    public GameObject rageCooldownCanvas;
    public Text rageCooldownText;

    [Header("Ability Icons")]
    public GameObject[] interruptIcons;
    public GameObject[] stunIcons;
    public GameObject rageIcon;

    [Header("Input Keys")]
    public KeyCode interruptKey = KeyCode.E;
    public KeyCode stunKey = KeyCode.Q;
    public KeyCode rageKey = KeyCode.Space;

    [Header("Hunter Movement Keys")]
    public KeyCode moveUpKey = KeyCode.W;
    public KeyCode moveDownKey = KeyCode.S;
    public KeyCode moveLeftKey = KeyCode.A;
    public KeyCode moveRightKey = KeyCode.D;

    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private Color originalColor;
    private PhotonView photonView;
    private bool isMultiplayer = false;
    private Animator animator;

    // Network sync variables
    private Vector2 networkPosition;
    private int networkInterrupts;
    private int networkStuns;
    private bool networkRageMode;
    private Vector2 networkMovement;
    private bool networkMoving;

    // Movement variables
    private Vector2 movement;
    private Vector2 lastDirection = Vector2.down;
    private bool isMoving = false;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        photonView = GetComponent<PhotonView>();
        animator = GetComponent<Animator>();

        isMultiplayer = PhotonNetwork.IsConnected && photonView != null;

        if (spriteRenderer != null)
            originalColor = spriteRenderer.color;

        if (interruptCooldownCanvas != null) interruptCooldownCanvas.SetActive(false);
        if (stunCooldownCanvas != null) stunCooldownCanvas.SetActive(false);
        if (rageCooldownCanvas != null) rageCooldownCanvas.SetActive(false);

        UpdateAbilityIcons();

        if (rb != null)
            networkPosition = rb.position;

        // Notify camera to follow this hunter if it's mine
        if (isMultiplayer && photonView.IsMine)
        {
            CameraFlow cam = Camera.main?.GetComponent<CameraFlow>();
            if (cam != null)
            {
                cam.SetFollowTarget(transform);
            }
        }
        else if (!isMultiplayer)
        {
            CameraFlow cam = Camera.main?.GetComponent<CameraFlow>();
            if (cam != null)
            {
                cam.SetFollowTarget(transform);
            }
        }
    }

    void Update()
    {
        if (isMultiplayer && !photonView.IsMine)
        {
            if (rb != null)
                rb.position = Vector2.Lerp(rb.position, networkPosition, Time.deltaTime * positionLerpSpeed);

            currentInterrupts = networkInterrupts;
            currentStuns = networkStuns;
            isInRageMode = networkRageMode;
            UpdateAbilityIcons();
            UpdateNetworkAnimation();
            return;
        }

        HandleCooldowns();
        HandleInput();
        HandleMovementInput();
        UpdateUI();
    }

    void FixedUpdate()
    {
        if (isMultiplayer && !photonView.IsMine)
            return;

        float currentSpeed = isInRageMode ? rageMoveSpeed : normalMoveSpeed;
        rb.MovePosition(rb.position + movement.normalized * currentSpeed * Time.fixedDeltaTime);

        UpdateAnimation();
    }

    void HandleMovementInput()
    {
        movement = Vector2.zero;

        if (Input.GetKey(moveUpKey)) movement.y = 1f;
        if (Input.GetKey(moveDownKey)) movement.y = -1f;
        if (Input.GetKey(moveLeftKey)) movement.x = -1f;
        if (Input.GetKey(moveRightKey)) movement.x = 1f;

        if (movement.magnitude > 0.1f)
        {
            isMoving = true;
            lastDirection = movement.normalized;
        }
        else
        {
            isMoving = false;
        }
    }

    void UpdateAnimation()
    {
        if (animator != null)
        {
            animator.SetBool("Moving", isMoving);
            animator.SetFloat("X", isMoving ? movement.x : lastDirection.x);
            animator.SetFloat("Y", isMoving ? movement.y : lastDirection.y);
        }
    }

    void UpdateNetworkAnimation()
    {
        if (animator != null)
        {
            animator.SetBool("Moving", networkMoving);
            animator.SetFloat("X", networkMoving ? networkMovement.x : lastDirection.x);
            animator.SetFloat("Y", networkMoving ? networkMovement.y : lastDirection.y);
        }
    }

    void HandleCooldowns()
    {
        if (interruptTimer > 0)
            interruptTimer -= Time.deltaTime;

        if (stunTimer > 0)
            stunTimer -= Time.deltaTime;

        if (rageTimer > 0)
        {
            rageTimer -= Time.deltaTime;
            if (isInRageMode && rageTimer <= 0)
                EndRageMode();
        }
    }

    void HandleInput()
    {
        if (Input.GetKeyDown(interruptKey) && CanUseInterrupt())
            TryInterruptTask();

        if (Input.GetKeyDown(stunKey) && CanUseStun())
            TryStunSurvivor();

        if (Input.GetKeyDown(rageKey) && CanUseRage())
            ActivateRageMode();
    }

    bool CanUseInterrupt()
    {
        return currentInterrupts < maxInterrupts && interruptTimer <= 0f;
    }

    bool CanUseStun()
    {
        // FIXED: Only check cooldown, not the count limit
        // The count is just for UI display, not a hard limit
        return stunTimer <= 0f;
    }

    bool CanUseRage()
    {
        return !isInRageMode && rageTimer <= 0f;
    }

    void TryInterruptTask()
    {
        CodeCheckGame[] codeGames = FindObjectsOfType<CodeCheckGame>();

        foreach (CodeCheckGame codeGame in codeGames)
        {
            if (codeGame.triggeredObject != null)
            {
                float distance = Vector2.Distance(transform.position, codeGame.triggeredObject.transform.position);
                if (distance <= interruptRange)
                {
                    if (isMultiplayer)
                    {
                        int index = System.Array.IndexOf(codeGames, codeGame);
                        photonView.RPC("RPC_TamperCode", RpcTarget.All, index);
                    }
                    else
                    {
                        codeGame.TamperCode();
                    }

                    currentInterrupts++;
                    interruptTimer = interruptCooldown;
                    UpdateAbilityIcons();

                    Debug.Log("Hunter interrupted a task!");
                    break;
                }
            }
        }
    }

    [PunRPC]
    void RPC_TamperCode(int codeGameIndex)
    {
        CodeCheckGame[] codeGames = FindObjectsOfType<CodeCheckGame>();
        if (codeGameIndex >= 0 && codeGameIndex < codeGames.Length)
        {
            codeGames[codeGameIndex].TamperCode();
        }
    }

    void TryStunSurvivor()
    {
        if (isMultiplayer)
        {
            GameObject[] allPlayers = GameObject.FindGameObjectsWithTag("Player");

            foreach (GameObject playerObj in allPlayers)
            {
                PhotonView targetView = playerObj.GetComponent<PhotonView>();
                if (targetView == null || targetView == photonView) continue;

                if (targetView.Owner.CustomProperties.ContainsKey("PlayerRole"))
                {
                    bool targetIsHunter = (bool)targetView.Owner.CustomProperties["PlayerRole"];
                    if (targetIsHunter) continue;
                }

                float distance = Vector2.Distance(transform.position, playerObj.transform.position);
                if (distance <= interruptRange * 2f)
                {
                    photonView.RPC("RPC_StunPlayer", RpcTarget.All, targetView.ViewID, stunDuration);

                    // FIXED: Increment for tracking only, not as a limit
                    currentStuns++;
                    stunTimer = stunCooldown;
                    UpdateAbilityIcons();

                    Debug.Log("Hunter stunned a survivor!");
                    break;
                }
            }
        }
        else
        {
            GameObject[] survivors = GameObject.FindGameObjectsWithTag("Player");
            foreach (GameObject survivor in survivors)
            {
                if (survivor == gameObject) continue;

                float distance = Vector2.Distance(transform.position, survivor.transform.position);
                if (distance <= interruptRange * 2f)
                {
                    StunnableScript stunnable = survivor.GetComponent<StunnableScript>();
                    if (stunnable != null)
                    {
                        stunnable.Stun(stunDuration);
                    }

                    // FIXED: Increment for tracking only
                    currentStuns++;
                    stunTimer = stunCooldown;
                    UpdateAbilityIcons();

                    Debug.Log("Hunter stunned a survivor!");
                    break;
                }
            }
        }
    }

    [PunRPC]
    void RPC_StunPlayer(int targetViewID, float duration)
    {
        PhotonView targetView = PhotonView.Find(targetViewID);
        if (targetView != null)
        {
            StunnableScript stunnable = targetView.GetComponent<StunnableScript>();
            if (stunnable != null)
            {
                stunnable.Stun(duration);
            }
        }
    }

    void ActivateRageMode()
    {
        isInRageMode = true;
        rageTimer = rageDuration;

        if (spriteRenderer != null)
            spriteRenderer.color = Color.red;

        if (isMultiplayer)
            photonView.RPC("RPC_SetRageMode", RpcTarget.Others, true);

        UpdateAbilityIcons();
        Debug.Log("Hunter activated Rage Mode!");
    }

    void EndRageMode()
    {
        isInRageMode = false;
        rageTimer = rageCooldown;

        if (spriteRenderer != null)
            spriteRenderer.color = originalColor;

        if (isMultiplayer)
            photonView.RPC("RPC_SetRageMode", RpcTarget.Others, false);

        UpdateAbilityIcons();
        Debug.Log("Rage Mode ended. Cooldown started.");
    }

    [PunRPC]
    void RPC_SetRageMode(bool active)
    {
        isInRageMode = active;

        if (spriteRenderer != null)
            spriteRenderer.color = active ? Color.red : originalColor;
    }

    void UpdateUI()
    {
        bool showUI = !isMultiplayer || photonView.IsMine;

        if (!showUI)
        {
            if (interruptCooldownCanvas != null) interruptCooldownCanvas.SetActive(false);
            if (stunCooldownCanvas != null) stunCooldownCanvas.SetActive(false);
            if (rageCooldownCanvas != null) rageCooldownCanvas.SetActive(false);
            return;
        }

        if (interruptTimer > 0 && interruptCooldownCanvas != null)
        {
            interruptCooldownCanvas.SetActive(true);
            if (interruptCooldownText != null)
                interruptCooldownText.text = $"Interrupt: {interruptTimer:F1}s";
        }
        else if (interruptCooldownCanvas != null)
            interruptCooldownCanvas.SetActive(false);

        if (stunTimer > 0 && stunCooldownCanvas != null)
        {
            stunCooldownCanvas.SetActive(true);
            if (stunCooldownText != null)
                stunCooldownText.text = $"Stun: {stunTimer:F1}s";
        }
        else if (stunCooldownCanvas != null)
            stunCooldownCanvas.SetActive(false);

        if (rageTimer > 0 && rageCooldownCanvas != null)
        {
            rageCooldownCanvas.SetActive(true);
            if (rageCooldownText != null)
            {
                if (isInRageMode)
                    rageCooldownText.text = $"Rage: {rageTimer:F1}s";
                else
                    rageCooldownText.text = $"Rage Cooldown: {rageTimer:F1}s";
            }
        }
        else if (rageCooldownCanvas != null)
            rageCooldownCanvas.SetActive(false);
    }

    void UpdateAbilityIcons()
    {
        bool showIcons = !isMultiplayer || photonView.IsMine;

        if (!showIcons)
        {
            foreach (var icon in interruptIcons)
                if (icon != null) icon.SetActive(false);
            foreach (var icon in stunIcons)
                if (icon != null) icon.SetActive(false);
            if (rageIcon != null) rageIcon.SetActive(false);
            return;
        }

        for (int i = 0; i < interruptIcons.Length; i++)
        {
            if (interruptIcons[i] != null)
                interruptIcons[i].SetActive(i < (maxInterrupts - currentInterrupts));
        }

        // FIXED: Show all stun icons since there's no hard limit
        // Just display cooldown in UI instead
        for (int i = 0; i < stunIcons.Length; i++)
        {
            if (stunIcons[i] != null)
                stunIcons[i].SetActive(true);
        }

        if (rageIcon != null)
            rageIcon.SetActive(isInRageMode || rageTimer <= 0f);
    }

    public void ResetAbilities()
    {
        currentInterrupts = 0;
        currentStuns = 0;
        interruptTimer = 0f;
        stunTimer = 0f;
        rageTimer = 0f;
        isInRageMode = false;

        if (spriteRenderer != null)
            spriteRenderer.color = originalColor;
        UpdateAbilityIcons();

        if (isMultiplayer && photonView.IsMine)
            photonView.RPC("RPC_ResetAbilities", RpcTarget.Others);
    }

    [PunRPC]
    void RPC_ResetAbilities()
    {
        ResetAbilities();
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            stream.SendNext(rb != null ? rb.position : Vector2.zero);
            stream.SendNext(currentInterrupts);
            stream.SendNext(currentStuns);
            stream.SendNext(isInRageMode);
            stream.SendNext(movement);
            stream.SendNext(isMoving);
        }
        else
        {
            networkPosition = (Vector2)stream.ReceiveNext();
            networkInterrupts = (int)stream.ReceiveNext();
            networkStuns = (int)stream.ReceiveNext();
            networkRageMode = (bool)stream.ReceiveNext();
            networkMovement = (Vector2)stream.ReceiveNext();
            networkMoving = (bool)stream.ReceiveNext();
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interruptRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, interruptRange * 2f);
    }
}