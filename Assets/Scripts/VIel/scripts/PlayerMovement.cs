using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using System.Security.Cryptography;

public class PlayerMovement : MonoBehaviour, IPunObservable
{
    PhotonView view;
    public float dropThroughTime = 0.5f;
    public float speed = 5f;
    public float jumpThroughDuration = 0.3f;
    public LayerMask enemyLayers = 1 << 6;
    public Rigidbody2D rb;
    public bool canMove = true;

    // Multiplayer detection
    private bool isMultiplayer = false;
    [Header("Network Smoothing")]
    public float positionLerpSpeed = 10f;
    public float rotationLerpSpeed = 10f;
    [Header("Audio Settings")]
    public bool isSurvivor = true;
    [Header("UI - Ability Cooldown Canvas")]
    public GameObject abilityCooldownCanvas;
    public Text stunCooldownText;
    [Header("Mobile Controls")]
    public VirtualJoystick virtualJoystick;
    public bool useMobileControls = true;
    [Header("Default Ability Panels - Debug & Stun")]
    public GameObject debugPanel;
    public Image debugCooldownImage;
    public Text debugCooldownText;
    public Button debugButton;
    public GameObject stunPanel;
    public Image stunCooldownImage;
    public Text stunCooldownText2;
    public Button stunButton;

    [Header("Unique Ability Panels - June (Heal)")]
    public GameObject healPanel;
    public Image healCooldownImage;
    public Text healCooldownText;
    public Button healButton;
    [Header("Unique Ability Panels - Ash (Rush)")]
    public GameObject rushPanel;
    public Image rushCooldownImage;
    public Text rushCooldownText;
    public Button rushButton;
    [Header("Unique Ability Panels - Rio (Cleanse)")]
    public GameObject cleansePanel;
    public Image cleanseCooldownImage;
    public Text cleanseCooldownText;
    public Button cleanseButton;
    [Header("UI - Move Again Cooldown")]
    public GameObject moveAgainCanvas;
    public Text moveAgainText;

    [Header("UI - Effect Duration Panels")]
    public GameObject grogEffectPanel;
    public Text grogEffectText;
    public GameObject auraFarmEffectPanel;
    public Text auraFarmEffectText;
    public GameObject stunEffectPanel;
    public Text stunEffectText;

    [Header("Default Ability Settings - Debug")]
    public float debugCooldown = 10f;
    [HideInInspector] public float debugTimer = 0f;

    [Header("Default Ability Settings - Stun")]
    public float survivorStunCooldown = 8f;
    public float survivorStunDuration = 3f;
    public float survivorStunRange = 2f;
    [HideInInspector] public float survivorStunTimer = 0f;

    [Header("Unique Ability Settings - June (Heal)")]
    public float healCooldown = 50f;
    public int healAmount = 2;
    [HideInInspector] public float healTimer = 0f;
    private bool hasJuneAbility = false;
    [Header("Unique Ability Settings - Ash (Rush)")]
    public float rushCooldown = 100f;
    public float rushSpeedBoost = 5f;
    public float rushDuration = 10f;
    [HideInInspector] public float rushTimer = 0f;
    [HideInInspector] public bool isRushing = false;
    private float rushEndTime = 0f;
    private float originalSpeed;
    private bool hasAshAbility = false;
    [Header("Unique Ability Settings - Rio (Cleanse)")]
    public float cleanseCooldown = 50f;
    public float cleanseRange = 5f;
    [HideInInspector] public float cleanseTimer = 0f;
    private bool hasRioAbility = false;
    [Header("Input Keys")]
    public KeyCode uniqueAbilityKey = KeyCode.R;
    public KeyCode debugKey = KeyCode.Q;
    public KeyCode stunKey = KeyCode.F;

    [HideInInspector] public bool isStunned = false;
    [HideInInspector] public bool isJumpingThrough = false;
    private double stunEndTime = 0f;
    private PlatformEffector2D currentEffector;
    private SpriteRenderer spriteRenderer;
    private Color originalColor;
    private Animator animk;
    private Collider2D playerCollider;
    Vector2 movement;
    private bool moving;
    private bool wasMovingLastFrame = false;
    private Vector2 lastDirection;
    private bool isAttacking = false;
    private bool attackInputReceived = false;

    // NEW: Movement reversal tracking with debug logging
    private bool _movementReversed = false;
    [HideInInspector]
    public bool movementReversed
    {
        get { return _movementReversed; }
        set
        {
            if (_movementReversed != value)
            {
               // Debug.Log($"[DEBUG] ⚠️ MOVEMENT REVERSAL CHANGED! From {_movementReversed} to {value}");
                _movementReversed = value;
            }
        }
    }

    // Network sync variables
    private Vector2 networkPosition;
    private Vector2 networkMovement;
    private Vector2 networkLastDirection;
    private bool networkMoving;
    private bool networkAttacking;
    private bool firstNetworkUpdate = true;

    // Bonus points tracking
    [HideInInspector] public int codesDebuggedCount = 0;
    [HideInInspector] public float totalDebuggingTime = 0f;
    private int debuggingSessionsCount = 0;
    void SetupJoystickVisibility()
    {
        bool isLocalPlayer = !view || view.IsMine;

        if (virtualJoystick != null)
        {
            // Only show joystick for the local player or in offline mode
            if (isLocalPlayer || PhotonNetwork.OfflineMode)
            {
                virtualJoystick.gameObject.SetActive(true);
                Debug.Log("[JOYSTICK] Enabled for local/offline player");
            }
            else
            {
                virtualJoystick.gameObject.SetActive(false);
                Debug.Log("[JOYSTICK] Hidden for remote player");
            }
        }
        else
        {
            Debug.LogWarning("[JOYSTICK] No VirtualJoystick assigned to player prefab!");
        }
    }
    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        animk = GetComponent<Animator>();
        playerCollider = GetComponent<Collider2D>();
        view = GetComponent<PhotonView>();
        if (spriteRenderer != null)
            originalColor = spriteRenderer.color;

        originalSpeed = speed;

        DetectSurvivorType();

        if (moveAgainCanvas != null)
            moveAgainCanvas.SetActive(false);

        if (grogEffectPanel != null) grogEffectPanel.SetActive(false);
        if (auraFarmEffectPanel != null) auraFarmEffectPanel.SetActive(false);
        if (stunEffectPanel != null) stunEffectPanel.SetActive(false);

        SetupInstantAbilityPanels();
        SetupAbilityButtons();
        networkPosition = rb.position;

        if (view != null && view.IsMine)
        {
            CameraFlow cam = Camera.main?.GetComponent<CameraFlow>();
            if (cam != null)
            {
                cam.SetFollowTarget(transform);
                Debug.Log($"Camera now following survivor: {gameObject.name}");
            }
        }
        else if (view == null || PhotonNetwork.OfflineMode)
        {
            CameraFlow cam = Camera.main?.GetComponent<CameraFlow>();
            if (cam != null)
            {
                cam.SetFollowTarget(transform);
                Debug.Log($"Camera now following offline survivor: {gameObject.name}");
            }
        }
        SetupJoystickVisibility();

      //  Debug.Log("[DEBUG] PlayerMovement Start complete");
    
   // Debug.Log("[DEBUG] PlayerMovement Start complete - Debug button key: " + debugKey);
    }

    void DetectSurvivorType()
    {
        string prefabName = gameObject.name.Replace("(Clone)", "").Trim();

        if (prefabName.Contains("June") || prefabName.Contains("Survivor1"))
        {
            hasJuneAbility = true;
            Debug.Log("Survivor has June (Heal) ability");
        }

        if (prefabName.Contains("Ash") || prefabName.Contains("Survivor2"))
        {
            hasAshAbility = true;
            Debug.Log("Survivor has Ash (Rush) ability");
        }

        if (prefabName.Contains("Rio") || prefabName.Contains("Survivor3"))
        {
            hasRioAbility = true;
            Debug.Log("Survivor has Rio (Cleanse) ability");
        }
    }

    void SetupInstantAbilityPanels()
    {
        bool isLocalPlayer = !view || view.IsMine;

        if (!isLocalPlayer)
        {
            if (debugPanel != null) debugPanel.SetActive(false);
            if (stunPanel != null) stunPanel.SetActive(false);
            if (healPanel != null) healPanel.SetActive(false);
            if (rushPanel != null) rushPanel.SetActive(false);
            if (cleansePanel != null) cleansePanel.SetActive(false);
            if (moveAgainCanvas != null) moveAgainCanvas.SetActive(false);
            if (grogEffectPanel != null) grogEffectPanel.SetActive(false);
            if (auraFarmEffectPanel != null) auraFarmEffectPanel.SetActive(false);
            if (stunEffectPanel != null) stunEffectPanel.SetActive(false);
            return;
        }

        if (debugPanel != null)
        {
            debugPanel.SetActive(true);
            Debug.Log("Debug panel now visible!");
        }

        if (stunPanel != null)
        {
            stunPanel.SetActive(true);
            Debug.Log("Stun panel now visible!");
        }

        if (hasJuneAbility && healPanel != null)
        {
            healPanel.SetActive(true);
            Debug.Log("June's heal panel now visible instantly!");
        }

        if (hasAshAbility && rushPanel != null)
        {
            rushPanel.SetActive(true);
            Debug.Log("Ash's rush panel now visible instantly!");
        }

        if (hasRioAbility && cleansePanel != null)
        {
            cleansePanel.SetActive(true);
            Debug.Log("Rio's cleanse panel now visible instantly!");
        }
    }

    void SetupAbilityButtons()
    {
        bool isLocalPlayer = !view || view.IsMine;
        if (!isLocalPlayer) return;

        if (debugButton != null)
            debugButton.onClick.AddListener(UseDebugAbility);

        if (stunButton != null)
            stunButton.onClick.AddListener(UseSurvivorStunAbility);

        if (healButton != null)
            healButton.onClick.AddListener(UseHealAbility);

        if (rushButton != null)
            rushButton.onClick.AddListener(UseRushAbility);

        if (cleanseButton != null)
            cleanseButton.onClick.AddListener(UseCleanseAbility);
    }

    void Update()
    {
        if (view != null && !view.IsMine)
        {
            SmoothNetworkPosition();
            return;
        }

       

        if (isRushing && Time.time >= rushEndTime)
        {
            EndRush();
        }

        if (Input.GetKeyDown(KeyCode.Space) && canMove && !isStunned && !isJumpingThrough)
        {
            StartCoroutine(JumpThroughColliders());
        }

        RaycastHit2D hit = Physics2D.Raycast(transform.position, Vector2.down, 1f);
        if (hit.collider != null)
            currentEffector = hit.collider.GetComponent<PlatformEffector2D>();
        else
            currentEffector = null;
        if (Input.GetKeyDown(KeyCode.J) && currentEffector != null)
            StartCoroutine(DropThroughPlatform(currentEffector));

        HandleAbilityCooldowns();

        if (isStunned)
        {
            double timeRemaining = stunEndTime - PhotonNetwork.Time;
            if (moveAgainCanvas != null)
                moveAgainCanvas.SetActive(true);
            if (moveAgainText != null)
                moveAgainText.text = $"{timeRemaining:F1}";
            if (timeRemaining <= 0f)
            {
                isStunned = false;
                UnlockMovement();
            }
        }
        else
        {
            if (moveAgainCanvas != null && moveAgainCanvas.activeSelf)
                moveAgainCanvas.SetActive(false);
        }

        if (Input.GetKeyDown(KeyCode.E) && canMove && !isStunned && !attackInputReceived)
        {
            TriggerAttack();
        }

        // MODIFIED: Debug key now acts as interact button
        if (Input.GetKeyDown(debugKey) && canMove && !isStunned)
        {
            Debug.Log("[DEBUG] Debug key pressed - calling UseDebugAbility (interact)");
            UseDebugAbility();
        }

        if (Input.GetKeyDown(stunKey) && canMove && !isStunned)
        {
            UseSurvivorStunAbility();
        }

        if (Input.GetKeyDown(uniqueAbilityKey))
        {
            if (hasRioAbility && cleanseTimer <= 0f)
            {
                UseCleanseAbility();
            }
            else if (canMove && !isStunned)
            {
                TryUseUniqueAbility();
            }
        }
    }

    void HandleAbilityCooldowns()
    {
        
        if (survivorStunTimer > 0)
            survivorStunTimer -= Time.deltaTime;
        if (healTimer > 0)
            healTimer -= Time.deltaTime;
        if (rushTimer > 0)
            rushTimer -= Time.deltaTime;
        if (cleanseTimer > 0)
            cleanseTimer -= Time.deltaTime;
        UpdateAbilityUI();
    }

   void UpdateAbilityUI()
{
    bool showUI = !view || view.IsMine;
    if (!showUI)
    {
        if (abilityCooldownCanvas != null) abilityCooldownCanvas.SetActive(false);
        return;
    }

    // Check if multiplayer or offline
    bool inMultiplayer = PhotonNetwork.IsConnected && view != null;

    // Debug panel (intercom cooldown)
    if (debugPanel != null)
    {
        debugPanel.SetActive(true);
        bool canInteract = false;

        if (inMultiplayer)
        {
            // Online mode - find CodeCheckGame
            CodeCheckGame nearestIntercom = FindNearestIntercom();
            
            if (nearestIntercom != null)
            {
                canInteract = !nearestIntercom.isOnCooldown;

                if (nearestIntercom.isOnCooldown)
                {
                    if (debugCooldownText != null)
                        debugCooldownText.text = $"Cooldown: {Mathf.Ceil(nearestIntercom.cooldownTimer)}s";
                    if (debugCooldownImage != null)
                        debugCooldownImage.fillAmount = nearestIntercom.cooldownTimer / nearestIntercom.interactCooldown;
                }
                else
                {
                    if (debugCooldownText != null)
                        debugCooldownText.text = "Interact";
                    if (debugCooldownImage != null)
                        debugCooldownImage.fillAmount = 0f;
                }
            }
            else
            {
                if (debugCooldownText != null)
                    debugCooldownText.text = "No Intercom";
                if (debugCooldownImage != null)
                    debugCooldownImage.fillAmount = 0f;
            }
        }
        else
        {
            // Offline mode - find OfflineCodeCheckGame
            OfflineCodeCheckGame nearestOfflineIntercom = FindNearestOfflineIntercom();
            
            if (nearestOfflineIntercom != null)
            {
                canInteract = !nearestOfflineIntercom.isOnCooldown;

                // Since cooldownTimer is private, just show Ready/Cooldown text
                if (nearestOfflineIntercom.isOnCooldown)
                {
                    if (debugCooldownText != null)
                        debugCooldownText.text = "On Cooldown";
                    if (debugCooldownImage != null)
                        debugCooldownImage.fillAmount = 1f; // Full = on cooldown
                }
                else
                {
                    if (debugCooldownText != null)
                        debugCooldownText.text = "Interact";
                    if (debugCooldownImage != null)
                        debugCooldownImage.fillAmount = 0f;
                }
            }
            else
            {
                if (debugCooldownText != null)
                    debugCooldownText.text = "No Intercom";
                if (debugCooldownImage != null)
                    debugCooldownImage.fillAmount = 0f;
            }
        }

        // Update button interactability
        if (debugButton != null)
            debugButton.interactable = canInteract;
    }

    // Stun panel
    if (stunPanel != null)
    {
        stunPanel.SetActive(true);
        if (survivorStunTimer > 0)
        {
            if (stunCooldownText2 != null)
                stunCooldownText2.text = $"{survivorStunTimer:F1}s";
            if (stunCooldownImage != null)
                stunCooldownImage.fillAmount = survivorStunTimer / survivorStunCooldown;
        }
        else
        {
            if (stunCooldownText2 != null)
                stunCooldownText2.text = "Ready";
            if (stunCooldownImage != null)
                stunCooldownImage.fillAmount = 0f;
        }
    }

    // June's heal panel
    if (hasJuneAbility && healPanel != null)
    {
        healPanel.SetActive(true);
        if (healTimer > 0)
        {
            if (healCooldownText != null)
                healCooldownText.text = $"{healTimer:F1}s";
            if (healCooldownImage != null)
                healCooldownImage.fillAmount = healTimer / healCooldown;
        }
        else
        {
            if (healCooldownText != null)
                healCooldownText.text = "Ready";
            if (healCooldownImage != null)
                healCooldownImage.fillAmount = 0f;
        }
    }

    // Ash's rush panel
    if (hasAshAbility && rushPanel != null)
    {
        rushPanel.SetActive(true);
        if (rushTimer > 0)
        {
            if (rushCooldownText != null)
                rushCooldownText.text = $"{rushTimer:F1}s";
            if (rushCooldownImage != null)
                rushCooldownImage.fillAmount = rushTimer / rushCooldown;
        }
        else if (isRushing)
        {
            float remainingDuration = rushEndTime - Time.time;
            if (rushCooldownText != null)
                rushCooldownText.text = $"Active: {remainingDuration:F1}s";
            if (rushCooldownImage != null)
                rushCooldownImage.fillAmount = remainingDuration / rushDuration;
        }
        else
        {
            if (rushCooldownText != null)
                rushCooldownText.text = "Ready";
            if (rushCooldownImage != null)
                rushCooldownImage.fillAmount = 0f;
        }
    }

    // Rio's cleanse panel
    if (hasRioAbility && cleansePanel != null)
    {
        cleansePanel.SetActive(true);
        if (cleanseTimer > 0)
        {
            if (cleanseCooldownText != null)
                cleanseCooldownText.text = $"{cleanseTimer:F1}s";
            if (cleanseCooldownImage != null)
                cleanseCooldownImage.fillAmount = cleanseTimer / cleanseCooldown;
        }
        else
        {
            if (cleanseCooldownText != null)
                cleanseCooldownText.text = "Ready";
            if (cleanseCooldownImage != null)
                cleanseCooldownImage.fillAmount = 0f;
        }
    }
}

    void UseDebugAbility()
    {
        if (!canMove || isStunned)
        {
            Debug.Log("[DEBUG] UseDebugAbility called but canMove=false or isStunned=true");
            return;
        }

        Debug.Log("[DEBUG] UseDebugAbility called - searching for nearest intercom");

        // Check if we're in multiplayer or offline mode
        bool inMultiplayer = PhotonNetwork.IsConnected && view != null;

        if (inMultiplayer)
        {
            // MULTIPLAYER MODE
            CodeCheckGame nearestIntercom = FindNearestIntercom();

            if (nearestIntercom != null)
            {
             

                if (!nearestIntercom.isOnCooldown)
                {
                   // Debug.Log("[DEBUG] Intercom is available - attempting to interact");
                    nearestIntercom.OpenCodePanel();

                    if (AudioManager.Instance != null)
                        AudioManager.Instance.PlayCorrectAnswer();
                }
                else
                {
                   // Debug.Log($"[DEBUG] Intercom is on cooldown - cannot interact");
                }
            }
            else
            {
               // Debug.Log("[DEBUG] No nearby intercom found");
            }
        }
        else
        {
            //OFFLINE MODE
            OfflineCodeCheckGame nearestOfflineIntercom = FindNearestOfflineIntercom();

            if (nearestOfflineIntercom != null)
            {
              //  Debug.Log($"[DEBUG] Found nearest offline intercom: {nearestOfflineIntercom.name}");
                Debug.Log($"[DEBUG] Intercom isOnCooldown: {nearestOfflineIntercom.isOnCooldown}");

                if (!nearestOfflineIntercom.isOnCooldown)
                {
                    Debug.Log("[DEBUG] Offline intercom is available - attempting to interact");
                    nearestOfflineIntercom.OpenCodePanel();

                    if (AudioManager.Instance != null)
                        AudioManager.Instance.PlayCorrectAnswer();
                }
                else
                {
                    Debug.Log($"[DEBUG] Offline intercom is on cooldown - cannot interact");
                }
            }
            else
            {
                //Debug.Log("[DEBUG] No nearby offline intercom found");
            }
        }
    }
    OfflineCodeCheckGame FindNearestOfflineIntercom()
    {
        OfflineCodeCheckGame[] allIntercoms = FindObjectsOfType<OfflineCodeCheckGame>();
        OfflineCodeCheckGame nearest = null;
        float shortestDistance = float.MaxValue;

        foreach (OfflineCodeCheckGame intercom in allIntercoms)
        {
            if (intercom != null)
            {
                float distance = Vector3.Distance(transform.position, intercom.transform.position);
                if (distance < shortestDistance)
                {
                    shortestDistance = distance;
                    nearest = intercom;
                }
            }
        }

        return nearest;
    }

    // NEW: Helper method to find the nearest intercom
    CodeCheckGame FindNearestIntercom()
    {
        CodeCheckGame[] allIntercoms = FindObjectsOfType<CodeCheckGame>();
        CodeCheckGame nearest = null;
        float shortestDistance = float.MaxValue;

        foreach (CodeCheckGame intercom in allIntercoms)
        {
            if (intercom != null)
            {
                float distance = Vector3.Distance(transform.position, intercom.transform.position);

                if (distance < shortestDistance)
                {
                    shortestDistance = distance;
                    nearest = intercom;

                }
            }
        }

        if (nearest != null)
        {

        }
        else
        {
            
        }

        return nearest;
    }
    void TriggerAttackAnimation()
    {
        isAttacking = true;
        attackInputReceived = true;
        if (animk != null)
        {
            animk.SetBool("Attacking", true);
        }
        StartCoroutine(ResetAttackAnimationAfterDelay(0.5f));
    }

   
    [PunRPC]
    void RPC_StunHunterTarget(int targetViewID, float duration)
    {
        Debug.Log($"[SURVIVOR STUN RPC] ⚡ Received RPC! ViewID={targetViewID}, duration={duration}");

        PhotonView targetView = PhotonView.Find(targetViewID);
        if (targetView == null)
        {
            Debug.LogError($"[SURVIVOR STUN RPC] ❌ Could not find PhotonView {targetViewID}");
            return;
        }

        Debug.Log($"[SURVIVOR STUN RPC] Found target: {targetView.gameObject.name}");

        StunnableScript stunnable = targetView.GetComponent<StunnableScript>();
        if (stunnable == null)
        {
            Debug.LogError($"[SURVIVOR STUN RPC] ❌ {targetView.gameObject.name} has no StunnableScript!");
            return;
        }

        Debug.Log($"[SURVIVOR STUN RPC] ✅ Applying {duration}s stun to {targetView.gameObject.name}");
        stunnable.Stun(duration);

        Debug.Log($"[SURVIVOR STUN RPC] ✅✅✅ STUN APPLIED SUCCESSFULLY!");
    }
    void UseSurvivorStunAbility()
    {
        if (survivorStunTimer > 0f || !canMove || isStunned)
        {
            Debug.Log($"[SURVIVOR STUN] Blocked - cooldown:{survivorStunTimer:F1}s, canMove:{canMove}, stunned:{isStunned}");
            return;
        }

        Debug.Log("[SURVIVOR STUN] STUN BUTTON PRESSED!");

        TriggerAttackAnimation();

        // Check if in multiplayer
        bool inMultiplayer = PhotonNetwork.IsConnected && view != null;
        Debug.Log($"[SURVIVOR STUN] Multiplayer mode: {inMultiplayer}");

        if (inMultiplayer)
        {
            //MULTIPLAYER MODE
            GameObject[] allPlayers = GameObject.FindGameObjectsWithTag("Player");
            Debug.Log($"[SURVIVOR STUN] Found {allPlayers.Length} players with 'Player' tag");

            foreach (GameObject playerObj in allPlayers)
            {
                Debug.Log($"[SURVIVOR STUN] Checking player: {playerObj.name}");

                PhotonView targetView = playerObj.GetComponent<PhotonView>();
                if (targetView == null)
                {
                    Debug.Log($"[SURVIVOR STUN]   - No PhotonView, skipping");
                    continue;
                }

                if (targetView == view)
                {
                    Debug.Log($"[SURVIVOR STUN]   - This is me, skipping");
                    continue;
                }

                // Check if target is HUNTER
                bool isTargetHunter = false;

                if (targetView.Owner != null && targetView.Owner.CustomProperties.ContainsKey("PlayerRole"))
                {
                    isTargetHunter = (bool)targetView.Owner.CustomProperties["PlayerRole"];
                    Debug.Log($"[SURVIVOR STUN]   - PlayerRole = {isTargetHunter}");
                }

                if (!isTargetHunter)
                {
                    Debug.Log($"[SURVIVOR STUN]   - Not a hunter, skipping");
                    continue;
                }

                float distance = Vector2.Distance(transform.position, playerObj.transform.position);
                Debug.Log($"[SURVIVOR STUN]   - Distance: {distance:F2} meters");

                if (distance <= survivorStunRange)
                {
                    Debug.Log($"[SURVIVOR STUN]   - IN RANGE! Stunning via RPC...");

                    view.RPC("RPC_StunHunterTarget", RpcTarget.All, targetView.ViewID, survivorStunDuration);
                    survivorStunTimer = survivorStunCooldown;

                    if (AudioManager.Instance != null)
                        AudioManager.Instance.PlayCorrectAnswer();

                    return;
                }
            }

            Debug.Log("[SURVIVOR STUN] No hunters in range");
            survivorStunTimer = survivorStunCooldown;
        }
        else
        {
            // OFFLINE MODE - FIXED 
            Debug.Log("[SURVIVOR STUN]searching for AI hunter");

            // Find AI hunters with HunterChaseAndHack
            HunterChaseAndHack[] hunters = FindObjectsOfType<HunterChaseAndHack>();
            Debug.Log($"[SURVIVOR STUN] Found {hunters.Length} AI hunters");

            foreach (HunterChaseAndHack hunter in hunters)
            {
                GameObject hunterObj = hunter.gameObject;
                Debug.Log($"[SURVIVOR STUN] Checking hunter: {hunterObj.name}");

                float distance = Vector2.Distance(transform.position, hunterObj.transform.position);
                Debug.Log($"[SURVIVOR STUN] Distance: {distance:F2}");

                if (distance <= survivorStunRange)
                {
                    StunnableScript stunnable = hunterObj.GetComponent<StunnableScript>();

                    if (stunnable != null)
                    {
                        Debug.Log($"[SURVIVOR STUN] STUNNING {hunterObj.name} for {survivorStunDuration}s!");

                        // Stun for 5 seconds
                        stunnable.Stun(survivorStunDuration);

                        // Start 20 second cooldown
                        survivorStunTimer = survivorStunCooldown;

                        if (AudioManager.Instance != null)
                            AudioManager.Instance.PlayCorrectAnswer();

                        Debug.Log($"[SURVIVOR STUN] Stun applied! Cooldown: {survivorStunCooldown}s");
                        return;
                    }
                    else
                    {
                        Debug.LogError($"[SURVIVOR STUN] No StunnableScript on {hunterObj.name}!");
                    }
                }
            }

            Debug.Log("[SURVIVOR STUN] No hunters in range");
            survivorStunTimer = survivorStunCooldown;
        }

       // Debug.Log("[SURVIVOR STUN]=");
    }

    // NEW: RPC method to stun hunter (mirrors hunter's RPC_StunPlayer)
    [PunRPC]
    void RPC_StunHunter(int targetViewID, float duration)
    {
        Debug.Log($"[STUN RPC] Received stun command for ViewID {targetViewID}, duration {duration}s");

        PhotonView targetView = PhotonView.Find(targetViewID);
        if (targetView != null)
        {
            Debug.Log($"[STUN RPC] Found target: {targetView.gameObject.name}");

            StunnableScript stunnable = targetView.GetComponent<StunnableScript>();
            if (stunnable != null)
            {
                Debug.Log($"[STUN RPC] ✓ Applying stun to {targetView.gameObject.name}!");
                stunnable.Stun(duration);
            }
            else
            {
                Debug.LogError($"[STUN RPC] Target {targetView.gameObject.name} has no StunnableScript!");
            }
        }
        else
        {
            Debug.LogError($"[STUN RPC] Could not find PhotonView with ID {targetViewID}");
        }
    }

    void TryUseUniqueAbility()
    {
        if (hasJuneAbility && healTimer <= 0f)
        {
            UseHealAbility();
        }
        else if (hasAshAbility && rushTimer <= 0f && !isRushing)
        {
            UseRushAbility();
        }
        else if (hasRioAbility && cleanseTimer <= 0f)
        {
            UseCleanseAbility();
        }
    }

    void UseHealAbility()
    {
        if (healTimer > 0f) return;

        GameManager gm = FindObjectOfType<GameManager>();
        if (gm != null)
        {
            
            if (gm.currentHP < gm.maxHP)
            {
                gm.HealPlayer(healAmount);
                healTimer = healCooldown;

             
                StunnableScript selfStunnable = GetComponent<StunnableScript>();
                if (selfStunnable != null)
                {
                    selfStunnable.ApplyHealEffect();
                }

                Debug.Log($"June used Heal ability! Healed herself for {healAmount} HP");
                if (AudioManager.Instance != null)
                    AudioManager.Instance.PlayCorrectAnswer();
            }
            else
            {
                Debug.Log("June's Heal failed: Already at full HP");
            }
        }
    }
    void UseRushAbility()
    {
        if (rushTimer > 0f || isRushing || !canMove || isStunned) return;

        isRushing = true;
        rushEndTime = Time.time + rushDuration;
        speed = originalSpeed + rushSpeedBoost;
        rushTimer = rushCooldown;

        Debug.Log($"Ash used Rush ability! Movement speed increased by {rushSpeedBoost} for {rushDuration} seconds");

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayCorrectAnswer();

        if (spriteRenderer != null && !isStunned && !isJumpingThrough)
        {
            spriteRenderer.color = new Color(1f, 1f, 0.5f);
        }
    }

    void EndRush()
    {
        isRushing = false;
        speed = originalSpeed;

        if (spriteRenderer != null && !isStunned && !isJumpingThrough)
        {
            spriteRenderer.color = originalColor;
        }

        Debug.Log("Rush ended. Speed returned to normal.");
    }

    void UseCleanseAbility()
    {
        if (cleanseTimer > 0f) return;

        // Only cleanse self
        StunnableScript rioStunnable = GetComponent<StunnableScript>();
        if (rioStunnable != null && rioStunnable.IsStunned)
        {
            rioStunnable.Unstun();
            cleanseTimer = cleanseCooldown;

            Debug.Log("Rio used Cleanse ability! Cleansed himself!");
            if (AudioManager.Instance != null)
                AudioManager.Instance.PlayCorrectAnswer();
        }
        else
        {
            Debug.Log("Rio's Cleanse failed: Not stunned");
        }
    }

    [PunRPC]
    public void RPC_UnlockMovement()
    {
        UnlockMovement();
    }

    void FixedUpdate()
    {
        if (view != null && !view.IsMine)
        {
            ApplyNetworkAnimation();
            return;
        }

        if (!canMove)
        {
            rb.velocity = Vector2.zero;
            animk.SetFloat("X", movement.x);
            animk.SetFloat("Y", movement.y);
            if (wasMovingLastFrame && AudioManager.Instance != null)
            {
                AudioManager.Instance.StopWalking();
                wasMovingLastFrame = false;
            }
            return;
        }

        if (useMobileControls && virtualJoystick != null)
        {
            movement.x = virtualJoystick.GetHorizontalAxis();
            movement.y = virtualJoystick.GetVerticalAxis();
        }
        else
        {
            movement.x = Input.GetAxisRaw("Horizontal");
            movement.y = Input.GetAxisRaw("Vertical");
        }

     
        if (movementReversed)
        {
         
            movement = -movement;
        }
        else if (Input.GetAxis("Horizontal") != 0 || Input.GetAxis("Vertical") != 0)
        {
           
        }

        if (movement.magnitude > 0.1f)
        {
            lastDirection = movement.normalized;
        }

        rb.MovePosition(rb.position + movement.normalized * speed * Time.deltaTime);
        Animate();
        HandleWalkingAudio();
    }

    private void SmoothNetworkPosition()
    {
        rb.position = Vector2.Lerp(rb.position, networkPosition, Time.deltaTime * positionLerpSpeed);
    }

    private void ApplyNetworkAnimation()
    {
        if (networkMoving)
        {
            animk.SetBool("Moving", true);
            animk.SetFloat("X", networkMovement.x);
            animk.SetFloat("Y", networkMovement.y);
        }
        else
        {
            animk.SetBool("Moving", false);
            animk.SetFloat("X", networkLastDirection.x);
            animk.SetFloat("Y", networkLastDirection.y);
        }
        animk.SetBool("Attacking", networkAttacking);
    }

    private void HandleWalkingAudio()
    {
        bool currentlyMoving = movement.magnitude > 0.1f;
        if (currentlyMoving && !wasMovingLastFrame && AudioManager.Instance != null)
        {
            if (isSurvivor)
                AudioManager.Instance.StartSurvivorWalking();
            else
                AudioManager.Instance.StartHunterWalking();
        }
        else if (!currentlyMoving && wasMovingLastFrame && AudioManager.Instance != null)
        {
            AudioManager.Instance.StopWalking();
        }
        wasMovingLastFrame = currentlyMoving;
    }

    private void Animate()
    {
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
            animk.SetBool("Moving", true);
            animk.SetFloat("X", movement.x);
            animk.SetFloat("Y", movement.y);
        }
        else
        {
            animk.SetBool("Moving", false);
            animk.SetFloat("X", lastDirection.x);
            animk.SetFloat("Y", lastDirection.y);
        }
        animk.SetBool("Attacking", isAttacking);
    }

    private void TriggerAttack()
    {
        isAttacking = true;
        attackInputReceived = true;
        StartCoroutine(ResetAttackAnimationAfterDelay(0.5f));
    }

    private IEnumerator ResetAttackAnimationAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        isAttacking = false;
        attackInputReceived = false;
    }

    IEnumerator JumpThroughColliders()
    {
        isJumpingThrough = true;
        Collider2D[] enemyColliders = Physics2D.OverlapCircleAll(transform.position, 10f, enemyLayers);
        foreach (Collider2D enemyCollider in enemyColliders)
        {
            if (playerCollider != null && enemyCollider != null)
                Physics2D.IgnoreCollision(playerCollider, enemyCollider, true);
        }
        if (spriteRenderer != null)
            spriteRenderer.color = Color.yellow;
        yield return new WaitForSeconds(jumpThroughDuration);
        foreach (Collider2D enemyCollider in enemyColliders)
        {
            if (playerCollider != null && enemyCollider != null)
                Physics2D.IgnoreCollision(playerCollider, enemyCollider, false);
        }
        if (spriteRenderer != null && !isRushing)
            spriteRenderer.color = originalColor;
        else if (spriteRenderer != null && isRushing)
            spriteRenderer.color = new Color(1f, 1f, 0.5f);
        isJumpingThrough = false;
    }

    IEnumerator DropThroughPlatform(PlatformEffector2D effector)
    {
        effector.rotationalOffset = 180f;
        yield return new WaitForSeconds(dropThroughTime);
        effector.rotationalOffset = 0f;
    }

    public void LockMovement(float stunTime = 0f)
    {
        canMove = false;
        if (stunTime > 0f)
        {
            isStunned = true;
            stunEndTime = PhotonNetwork.Time + stunTime;
            if (spriteRenderer != null)
                spriteRenderer.color = Color.blue;
            if (moveAgainCanvas != null)
                moveAgainCanvas.SetActive(true);
        }
    }

    public void UnlockMovement()
    {
        canMove = true;
        isStunned = false;
        if (spriteRenderer != null && !isJumpingThrough && !isRushing)
            spriteRenderer.color = originalColor;
        else if (spriteRenderer != null && isRushing && !isJumpingThrough)
            spriteRenderer.color = new Color(1f, 1f, 0.5f);
        if (moveAgainText != null)
            moveAgainText.text = "";
        if (moveAgainCanvas != null)
            moveAgainCanvas.SetActive(false);
    }

    public void RecordCodeDebugged(float debugTime)
    {
        codesDebuggedCount++;
        totalDebuggingTime += debugTime;
        debuggingSessionsCount++;
    }

    public float GetAverageDebuggingTime()
    {
        if (debuggingSessionsCount == 0) return 0f;
        return totalDebuggingTime / debuggingSessionsCount;
    }

    public float GetNearbyDebuggingTime()
    {
        return GetAverageDebuggingTime() * 0.8f;
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            stream.SendNext(rb.position);
            stream.SendNext(movement);
            stream.SendNext(lastDirection);
            stream.SendNext(moving);
            stream.SendNext(isAttacking);
        }
        else
        {
            networkPosition = (Vector2)stream.ReceiveNext();
            networkMovement = (Vector2)stream.ReceiveNext();
            networkLastDirection = (Vector2)stream.ReceiveNext();
            networkMoving = (bool)stream.ReceiveNext();
            networkAttacking = (bool)stream.ReceiveNext();
        }
    }
}