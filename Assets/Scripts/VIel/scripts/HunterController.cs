using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using Photon.Pun;

public class HunterController : MonoBehaviourPunCallbacks, IPunObservable
{
    [Header("References")]
    public GameManager gameManager;
    [Header("Mobile Controls")]
    public VirtualJoystick virtualJoystick;
    public bool useMobileControls = true;
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
    public float stunCooldown = 5f;
    public int maxStuns = 5;
    public float stunDuration = 5f;
    private int currentStuns = 0;
    private float stunTimer = 0f;

    [Header("Rage Mode Settings")]
    public float rageDuration = 8f;
    public float rageCooldown = 20f;
    private float rageTimer = 0f;
    private bool isInRageMode = false;

    [Header("Unique Abilities - Red Cloaked")]
    public float grogDuration = 20f;
    public float grogCooldown = 50f;
    public float grogMoveSpeedReduction = -10f;
    public int grogHPLoss = 2;
    private float grogTimer = 0f;
    private bool hasRedCloakedAbility = false;
    private double grogEndTime = 0f;
    private bool grogActive = false;

    [Header("Unique Abilities - Fatal Exception")]
    public float auraFarmDuration = 5f;
    public float auraFarmCooldown = 50f;
    public float auraFarmSlowAmount = -2f;
    private float auraFarmTimer = 0f;
    private bool hasFatalExceptionAbility = false;
    private double auraFarmEndTime = 0f;
    private bool auraFarmActive = false;

    [Header("Single Ability Canvas")]
    public GameObject abilityCanvas;

    [Header("Ability Panels")]
    public GameObject interruptPanel;
    public Text interruptCooldownText;
    public Button interruptButton;
    public GameObject stunPanel;
    public Text stunCooldownText;
    public Button stunButton;
    public GameObject ragePanel;
    public Text rageCooldownText;
    public Button rageButton;
    public GameObject grogPanel;
    public Text grogCooldownText;
    public Button grogButton;
    public GameObject auraFarmPanel;
    public Text auraFarmCooldownText;
    public Button auraFarmButton;

    [Header("Ability Icons")]
    public GameObject[] interruptIcons;
    public GameObject[] stunIcons;
    public GameObject rageIcon;
    public GameObject grogIcon;
    public GameObject auraFarmIcon;

    [Header("UI - Effect Duration Panels")]
    public GameObject grogEffectPanel;
    public Text grogEffectText;
    public GameObject auraFarmEffectPanel;
    public Text auraFarmEffectText;
    public GameObject stunEffectPanel;
    public Text stunEffectText;

    [Header("Input Keys")]
    public KeyCode interruptKey = KeyCode.E;
    public KeyCode stunKey = KeyCode.Q;
    public KeyCode rageKey = KeyCode.Space;
    public KeyCode uniqueAbilityKey = KeyCode.R;

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

    private Vector2 networkPosition;
    private int networkInterrupts;
    private int networkStuns;
    private bool networkRageMode;
    private Vector2 networkMovement;
    private bool networkMoving;
    private Vector2 networkLastDirection;
    private bool networkAttacking;

    private Vector2 movement;
    private Vector2 lastDirection = Vector2.down;
    private bool moving = false;
    private bool isMoving = false;

    public bool canMove = true;
    [HideInInspector] public bool isStunned = false;
    private bool isAttacking = false;
    private bool attackInputReceived = false;

    [HideInInspector] public int codesInterrupted = 0;
    [HideInInspector] public int survivorsStunned = 0;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        photonView = GetComponent<PhotonView>();
        animator = GetComponent<Animator>();

        isMultiplayer = PhotonNetwork.IsConnected && photonView != null;

        if (spriteRenderer != null)
            originalColor = spriteRenderer.color;

        DetectHunterType();

        bool isLocalPlayer = !photonView || photonView.IsMine;

        //  JOYSTICK visibility setup
        if (virtualJoystick != null)
        {
            if (isLocalPlayer || PhotonNetwork.OfflineMode)
            {
                virtualJoystick.gameObject.SetActive(true);
                Debug.Log("[JOYSTICK] Enabled for local/offline hunter");
            }
            else
            {
                virtualJoystick.gameObject.SetActive(false);
                Debug.Log("[JOYSTICK] Hidden for remote hunter");
            }
        }

      
        if (isLocalPlayer)
        {
            if (grogEffectPanel != null) grogEffectPanel.SetActive(false);
            if (auraFarmEffectPanel != null) auraFarmEffectPanel.SetActive(false);
            if (stunEffectPanel != null) stunEffectPanel.SetActive(false);
        }
        else
        {
            if (abilityCanvas != null) abilityCanvas.SetActive(false);
            if (grogEffectPanel != null) grogEffectPanel.SetActive(false);
            if (auraFarmEffectPanel != null) auraFarmEffectPanel.SetActive(false);
            if (stunEffectPanel != null) stunEffectPanel.SetActive(false);
        }

        if (abilityCanvas != null && isLocalPlayer)
            abilityCanvas.SetActive(true);

        SetupInstantAbilityDisplay();
        SetupAbilityButtons();
        UpdateAbilityIcons();

        if (rb != null)
            networkPosition = rb.position;

        if ((isMultiplayer && photonView.IsMine) || !isMultiplayer)
        {
            CameraFlow cam = Camera.main?.GetComponent<CameraFlow>();
            if (cam != null)
                cam.SetFollowTarget(transform);
        }
    } 

    void DetectHunterType()
    {
        string prefabName = gameObject.name.Replace("(Clone)", "").Trim();

        if (prefabName.Contains("RedCloaked") || prefabName.Contains("Hunter2"))
        {
            hasRedCloakedAbility = true;
            Debug.Log("Hunter has Red Cloaked (Grog) ability");
        }

        if (prefabName.Contains("FatalException") || prefabName.Contains("Hunter3"))
        {
            hasFatalExceptionAbility = true;
            Debug.Log("Hunter has Fatal Exception (Aura Farm) ability");
        }
    }

    void SetupInstantAbilityDisplay()
    {
        bool isLocalPlayer = !photonView || photonView.IsMine;
        if (!isLocalPlayer) return;

        if (interruptPanel != null)
        {
            interruptPanel.SetActive(true);
            if (interruptCooldownText != null)
                interruptCooldownText.text = "Ready";
        }

        if (stunPanel != null)
        {
            stunPanel.SetActive(true);
            if (stunCooldownText != null)
                stunCooldownText.text = "Ready";
        }

        if (ragePanel != null)
        {
            ragePanel.SetActive(true);
            if (rageCooldownText != null)
                rageCooldownText.text = "Ready";
        }

        if (hasRedCloakedAbility && grogPanel != null)
        {
            grogPanel.SetActive(true);
            if (grogCooldownText != null)
                grogCooldownText.text = "Ready";
        }
        else if (grogPanel != null)
        {
            grogPanel.SetActive(false);
        }

        if (hasFatalExceptionAbility && auraFarmPanel != null)
        {
            auraFarmPanel.SetActive(true);
            if (auraFarmCooldownText != null)
                auraFarmCooldownText.text = "Ready";
        }
        else if (auraFarmPanel != null)
        {
            auraFarmPanel.SetActive(false);
        }
    }

    void SetupAbilityButtons()
    {
        bool isLocalPlayer = !photonView || photonView.IsMine;
        if (!isLocalPlayer) return;

        if (interruptButton != null)
            interruptButton.onClick.AddListener(TryInterruptTask);

        if (stunButton != null)
            stunButton.onClick.AddListener(TryStunSurvivor);

        if (rageButton != null)
            rageButton.onClick.AddListener(ActivateRageMode);

        if (grogButton != null)
            grogButton.onClick.AddListener(ActivateGrog);

        if (auraFarmButton != null)
            auraFarmButton.onClick.AddListener(ActivateAuraFarm);
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
            ApplyNetworkAnimation();
            return;
        }

        HandleCooldowns();
        HandleInput();
        UpdateUI();
        UpdateEffectDurationPanels();
    }

    void UpdateEffectDurationPanels()
    {
        bool isLocalPlayer = !photonView || photonView.IsMine;
        if (!isLocalPlayer) return;

        if (grogActive)
        {
            double timeRemaining = grogEndTime - PhotonNetwork.Time;
            if (timeRemaining > 0 && grogEffectPanel != null)
            {
                grogEffectPanel.SetActive(true);
                if (grogEffectText != null)
                    grogEffectText.text = $"Grog Active: {timeRemaining:F1}s";
            }
            else
            {
                grogActive = false;
                if (grogEffectPanel != null)
                    grogEffectPanel.SetActive(false);
            }
        }
        else if (grogEffectPanel != null)
        {
            grogEffectPanel.SetActive(false);
        }

        if (auraFarmActive)
        {
            double timeRemaining = auraFarmEndTime - PhotonNetwork.Time;
            if (timeRemaining > 0 && auraFarmEffectPanel != null)
            {
                auraFarmEffectPanel.SetActive(true);
                if (auraFarmEffectText != null)
                    auraFarmEffectText.text = $"Aura Farm Active: {timeRemaining:F1}s";
            }
            else
            {
                auraFarmActive = false;
                if (auraFarmEffectPanel != null)
                    auraFarmEffectPanel.SetActive(false);
            }
        }
        else if (auraFarmEffectPanel != null)
        {
            auraFarmEffectPanel.SetActive(false);
        }
    }

    void FixedUpdate()
    {
        if (isMultiplayer && !photonView.IsMine)
            return;

        if (!canMove || isStunned)
        {
            rb.velocity = Vector2.zero;
            if (animator != null)
            {
                animator.SetFloat("X", lastDirection.x);
                animator.SetFloat("Y", lastDirection.y);
            }
            return;
        }

        HandleMovementInput();

        float currentSpeed = isInRageMode ? rageMoveSpeed : normalMoveSpeed;
        rb.MovePosition(rb.position + movement.normalized * currentSpeed * Time.fixedDeltaTime);

        Animate();
    }

    void HandleMovementInput()
    {
        movement = Vector2.zero;

        if (useMobileControls && virtualJoystick != null)
        {
            movement.x = virtualJoystick.GetHorizontalAxis();
            movement.y = virtualJoystick.GetVerticalAxis();
        }
        else
        {
            if (Input.GetKey(moveUpKey)) movement.y = 1f;
            if (Input.GetKey(moveDownKey)) movement.y = -1f;
            if (Input.GetKey(moveLeftKey)) movement.x = -1f;
            if (Input.GetKey(moveRightKey)) movement.x = 1f;
        }

        if (movement.magnitude > 0.1f)
        {
            moving = true;
            isMoving = true;
            lastDirection = movement.normalized;
        }
        else
        {
            moving = false;
            isMoving = false;
        }
    }

    private void Animate()
    {
        if (animator == null) return;

        if (movement.magnitude > 0.1f || movement.magnitude < -0.1f)
        {
            moving = true;
        }
        else
        {
            moving = false;
        }

        if (moving)
        {
            animator.SetBool("Moving", true);
            animator.SetFloat("X", movement.x);
            animator.SetFloat("Y", movement.y);
        }
        else
        {
            animator.SetBool("Moving", false);
            animator.SetFloat("X", lastDirection.x);
            animator.SetFloat("Y", lastDirection.y);
        }

        animator.SetBool("Attacking", isAttacking);
    }

    private void ApplyNetworkAnimation()
    {
        if (animator == null) return;

        if (networkMoving)
        {
            animator.SetBool("Moving", true);
            animator.SetFloat("X", networkMovement.x);
            animator.SetFloat("Y", networkMovement.y);
        }
        else
        {
            animator.SetBool("Moving", false);
            animator.SetFloat("X", networkLastDirection.x);
            animator.SetFloat("Y", networkLastDirection.y);
        }

        animator.SetBool("Attacking", networkAttacking);
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

        if (grogTimer > 0)
            grogTimer -= Time.deltaTime;

        if (auraFarmTimer > 0)
            auraFarmTimer -= Time.deltaTime;
    }

    void HandleInput()
    {
        if (Input.GetKeyDown(interruptKey) && CanUseInterrupt())
            TryInterruptTask();

        if (Input.GetKeyDown(stunKey) && CanUseStun())
            TryStunSurvivor();

        if (Input.GetKeyDown(rageKey) && CanUseRage())
            ActivateRageMode();

        // FIXED: Only allow using unique ability if this hunter has one
        if (Input.GetKeyDown(uniqueAbilityKey))
        {
            if (hasRedCloakedAbility && CanUseGrog())
                ActivateGrog();
            else if (hasFatalExceptionAbility && CanUseAuraFarm())
                ActivateAuraFarm();
            else if (!hasRedCloakedAbility && !hasFatalExceptionAbility)
                Debug.Log("This hunter doesn't have a unique ability!");
        }
    }

    bool CanUseInterrupt()
    {
        return currentInterrupts < maxInterrupts && interruptTimer <= 0f;
    }

    bool CanUseStun()
    {
        return stunTimer <= 0f;
    }

    bool CanUseRage()
    {
        return !isInRageMode && rageTimer <= 0f;
    }

    bool CanUseGrog()
    {
       
        return hasRedCloakedAbility && grogTimer <= 0f;
    }

    bool CanUseAuraFarm()
    {
       
        return hasFatalExceptionAbility && auraFarmTimer <= 0f;
    }

    void TryInterruptTask()
    {
        if (!CanUseInterrupt()) return;

        TriggerAttackAnimation();

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
                    codesInterrupted++;
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
        if (!CanUseStun()) return;

        TriggerAttackAnimation();

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

                    currentStuns++;
                    stunTimer = stunCooldown;
                    survivorsStunned++;
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

                    currentStuns++;
                    stunTimer = stunCooldown;
                    survivorsStunned++;
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
        if (!CanUseRage()) return;

        TriggerAttackAnimation();

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

    void ActivateGrog()
    {
        if (!CanUseGrog()) return;

        // FIXED: Only RedCloaked can use this ability
        if (!hasRedCloakedAbility)
        {
            Debug.Log("This hunter doesn't have the Grog ability!");
            return;
        }

        TriggerAttackAnimation();

        grogTimer = grogCooldown;
        grogActive = true;
        grogEndTime = PhotonNetwork.Time + grogDuration;

        if (isMultiplayer)
        {
            photonView.RPC("RPC_ActivateGrog", RpcTarget.All);
        }
        else
        {
            ApplyGrogToSurvivors();
        }

        UpdateAbilityIcons();
        Debug.Log("Red Cloaked activated Grog ability!");
    }

    [PunRPC]
    void RPC_ActivateGrog()
    {
        ApplyGrogToSurvivors();
    }

    void ApplyGrogToSurvivors()
    {
        GameObject[] allPlayers = GameObject.FindGameObjectsWithTag("Player");

        foreach (GameObject playerObj in allPlayers)
        {
            PhotonView targetView = playerObj.GetComponent<PhotonView>();

            if (targetView != null && targetView == photonView) continue;

            bool isSurvivor = true;
            if (targetView != null && targetView.Owner.CustomProperties.ContainsKey("PlayerRole"))
            {
                isSurvivor = !(bool)targetView.Owner.CustomProperties["PlayerRole"];
            }

            if (isSurvivor)
            {
                PlayerMovement pm = playerObj.GetComponent<PlayerMovement>();
                StunnableScript stunnable = playerObj.GetComponent<StunnableScript>();

                if (pm != null)
                {
                    // FIXED: Enable movement reversal for grog
                    StartCoroutine(ApplyGrogEffect(pm));
                }

                if (stunnable != null)
                {
                    stunnable.ApplyGrogEffect(grogDuration);
                }

                if (gameManager != null)
                {
                    gameManager.TakeDamage(false);
                    gameManager.TakeDamage(false);
                }
            }
        }
    }

    IEnumerator ApplyGrogEffect(PlayerMovement pm)
    {
        Debug.Log("[DEBUG] ApplyGrogEffect started - REVERSING movement (Grog only)");

        // Enable movement reversal for RedCloaked grog ability ONLY
        pm.movementReversed = true;

        Debug.Log($"[DEBUG] Grog effect applied - Movement reversed: {pm.movementReversed}");

        yield return new WaitForSeconds(grogDuration);

        // Disable movement reversal when grog ends
        pm.movementReversed = false;

        Debug.Log($"[DEBUG] Grog effect ended - Movement reversed: {pm.movementReversed}");
    }
    void ActivateAuraFarm()
    {
        if (!CanUseAuraFarm()) return;

        // FIXED: Only FatalException can use this ability
        if (!hasFatalExceptionAbility)
        {
            Debug.Log("[AURA FARM] This hunter doesn't have the Aura Farm ability!");
            return;
        }

        TriggerAttackAnimation();

        auraFarmTimer = auraFarmCooldown;
        auraFarmActive = true;
        auraFarmEndTime = PhotonNetwork.Time + auraFarmDuration;

        Debug.Log("[AURA FARM] ✅ Fatal Exception activated Aura Farm!");

        if (isMultiplayer)
        {
            photonView.RPC("RPC_ActivateAuraFarm", RpcTarget.All);
        }
        else
        {
            ApplyAuraFarmToSurvivors();
        }

        UpdateAbilityIcons();
    }

    [PunRPC]
    void RPC_ActivateAuraFarm()
    {
        Debug.Log("[AURA FARM RPC] Received Aura Farm activation command");
        ApplyAuraFarmToSurvivors();
    }

    void ApplyAuraFarmToSurvivors()
    {
        Debug.Log("[AURA FARM] Applying slow effect to all survivors...");

        GameObject[] allPlayers = GameObject.FindGameObjectsWithTag("Player");
        int survivorsAffected = 0;

        foreach (GameObject playerObj in allPlayers)
        {
            PhotonView targetView = playerObj.GetComponent<PhotonView>();

            // Skip self
            if (targetView != null && targetView == photonView)
            {
                Debug.Log($"[AURA FARM] Skipping self");
                continue;
            }

            // Check if target is a survivor
            bool isSurvivor = true;
            if (targetView != null && targetView.Owner != null && targetView.Owner.CustomProperties.ContainsKey("PlayerRole"))
            {
                // PlayerRole: false = survivor, true = hunter
                isSurvivor = !(bool)targetView.Owner.CustomProperties["PlayerRole"];
            }
            else
            {
                // Offline mode: check if they have PlayerMovement (survivor) component
                PlayerMovement playerMovements = playerObj.GetComponent<PlayerMovement>();
                isSurvivor = (playerMovements != null);
            }

            if (!isSurvivor)
            {
                Debug.Log($"[AURA FARM] {playerObj.name} is not a survivor - skipping");
                continue;
            }

            Debug.Log($"[AURA FARM] Found survivor: {playerObj.name}");

            // Apply slow effect to survivor
            PlayerMovement playerMovement = playerObj.GetComponent<PlayerMovement>();
            if (playerMovement != null)
            {
                Debug.Log($"[AURA FARM] Applying slow effect to {playerObj.name}");
                StartCoroutine(ApplyAuraFarmSlowEffect(playerMovement));
                survivorsAffected++;
            }

            // Apply visual effect (yellow color)
            StunnableScript stunnable = playerObj.GetComponent<StunnableScript>();
            if (stunnable != null)
            {
                Debug.Log($"[AURA FARM] Applying yellow visual to {playerObj.name}");
                stunnable.ApplyAuraFarmEffect(auraFarmDuration);
            }
        }

        Debug.Log($"[AURA FARM] ✅ Affected {survivorsAffected} survivors");
    }



    IEnumerator ApplyAuraFarmSlowEffect(PlayerMovement playerMove)
    {
        Debug.Log($"[AURA FARM SLOW] ━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
        Debug.Log($"[AURA FARM SLOW] Starting on: {playerMove.gameObject.name}");
        Debug.Log($"[AURA FARM SLOW] Original speed: {playerMove.speed}");
        Debug.Log($"[AURA FARM SLOW] Slow amount: {auraFarmSlowAmount}");
        Debug.Log($"[AURA FARM SLOW] Duration: {auraFarmDuration}s");
        Debug.Log($"[AURA FARM SLOW] Movement reversed BEFORE: {playerMove.movementReversed}");

        float originalSpeed = playerMove.speed;

        // ⚠️ CRITICAL: Aura Farm ONLY slows, NEVER reverses movement
        playerMove.movementReversed = false;

        // Apply speed reduction (auraFarmSlowAmount should be negative, e.g. -2f)
        float newSpeed = playerMove.speed + auraFarmSlowAmount;

        // Prevent speed from going too low or negative
        playerMove.speed = Mathf.Max(0.5f, newSpeed);

        Debug.Log($"[AURA FARM SLOW] ✅ Applied! New speed: {playerMove.speed}");
        Debug.Log($"[AURA FARM SLOW] Movement reversed AFTER: {playerMove.movementReversed}");

        yield return new WaitForSeconds(auraFarmDuration);

        // Restore original speed
        playerMove.speed = originalSpeed;

        // ⚠️ CRITICAL: Ensure movement is still NOT reversed when effect ends
        playerMove.movementReversed = false;

        Debug.Log($"[AURA FARM SLOW] ━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
        Debug.Log($"[AURA FARM SLOW] Effect ended on: {playerMove.gameObject.name}");
        Debug.Log($"[AURA FARM SLOW] Speed restored to: {playerMove.speed}");
        Debug.Log($"[AURA FARM SLOW] Movement reversed FINAL: {playerMove.movementReversed}");
    }
    void TriggerAttackAnimation()
    {
        if (animator != null)
        {
            isAttacking = true;
            animator.SetBool("Attacking", true);
            StartCoroutine(ResetAttackAnimation());
        }
    }

    IEnumerator ResetAttackAnimation()
    {
        yield return new WaitForSeconds(0.5f);
        isAttacking = false;
        if (animator != null)
            animator.SetBool("Attacking", false);
    }

    void UpdateUI()
    {
        bool showUI = !isMultiplayer || photonView.IsMine;

        if (!showUI)
        {
            if (abilityCanvas != null) abilityCanvas.SetActive(false);
            return;
        }

        if (abilityCanvas != null)
            abilityCanvas.SetActive(true);

        if (interruptPanel != null)
        {
            interruptPanel.SetActive(true);
            if (interruptCooldownText != null)
            {
                if (interruptTimer > 0)
                    interruptCooldownText.text = $"{interruptTimer:F1}s";
                else if (currentInterrupts >= maxInterrupts)
                    interruptCooldownText.text = "Max Uses";
                else
                    interruptCooldownText.text = "Ready";
            }
        }

        if (stunPanel != null)
        {
            stunPanel.SetActive(true);
            if (stunCooldownText != null)
            {
                if (stunTimer > 0)
                    stunCooldownText.text = $"{stunTimer:F1}s";
                else
                    stunCooldownText.text = "Ready";
            }
        }

        if (ragePanel != null)
        {
            ragePanel.SetActive(true);
            if (rageCooldownText != null)
            {
                if (isInRageMode)
                    rageCooldownText.text = $"Active: {rageTimer:F1}s";
                else if (rageTimer > 0)
                    rageCooldownText.text = $"{rageTimer:F1}s";
                else
                    rageCooldownText.text = "Ready";
            }
        }

        if (hasRedCloakedAbility && grogPanel != null)
        {
            grogPanel.SetActive(true);
            if (grogCooldownText != null)
            {
                if (grogTimer > 0)
                    grogCooldownText.text = $"{grogTimer:F1}s";
                else
                    grogCooldownText.text = "Ready";
            }
        }

        if (hasFatalExceptionAbility && auraFarmPanel != null)
        {
            auraFarmPanel.SetActive(true);
            if (auraFarmCooldownText != null)
            {
                if (auraFarmTimer > 0)
                    auraFarmCooldownText.text = $"{auraFarmTimer:F1}s";
                else
                    auraFarmCooldownText.text = "Ready";
            }
        }
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
            if (grogIcon != null) grogIcon.SetActive(false);
            if (auraFarmIcon != null) auraFarmIcon.SetActive(false);
            return;
        }

        for (int i = 0; i < interruptIcons.Length; i++)
        {
            if (interruptIcons[i] != null)
                interruptIcons[i].SetActive(i < (maxInterrupts - currentInterrupts));
        }

        for (int i = 0; i < stunIcons.Length; i++)
        {
            if (stunIcons[i] != null)
                stunIcons[i].SetActive(true);
        }

        if (rageIcon != null)
            rageIcon.SetActive(isInRageMode || rageTimer <= 0f);

        if (grogIcon != null)
            grogIcon.SetActive(hasRedCloakedAbility && grogTimer <= 0f);

        if (auraFarmIcon != null)
            auraFarmIcon.SetActive(hasFatalExceptionAbility && auraFarmTimer <= 0f);
    }

    public void LockMovement(float stunTime = 0f)
    {
        canMove = false;
        if (stunTime > 0f)
        {
            isStunned = true;
            if (spriteRenderer != null)
                spriteRenderer.color = Color.blue;
        }
    }

    public void UnlockMovement()
    {
        canMove = true;
        isStunned = false;
        if (spriteRenderer != null)
            spriteRenderer.color = originalColor;
    }

    public void ResetAbilities()
    {
        currentInterrupts = 0;
        currentStuns = 0;
        interruptTimer = 0f;
        stunTimer = 0f;
        rageTimer = 0f;
        grogTimer = 0f;
        auraFarmTimer = 0f;
        isInRageMode = false;
        grogActive = false;
        auraFarmActive = false;

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
            stream.SendNext(moving);
            stream.SendNext(lastDirection);
            stream.SendNext(isAttacking);
        }
        else
        {
            networkPosition = (Vector2)stream.ReceiveNext();
            networkInterrupts = (int)stream.ReceiveNext();
            networkStuns = (int)stream.ReceiveNext();
            networkRageMode = (bool)stream.ReceiveNext();
            networkMovement = (Vector2)stream.ReceiveNext();
            networkMoving = (bool)stream.ReceiveNext();
            networkLastDirection = (Vector2)stream.ReceiveNext();
            networkAttacking = (bool)stream.ReceiveNext();
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