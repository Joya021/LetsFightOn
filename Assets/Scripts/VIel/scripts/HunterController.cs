using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class HunterController : MonoBehaviour
{
    [Header("References")]
    public Transform survivor; // The survivor player
    public GameManager gameManager;

    [Header("Movement")]
    public float normalMoveSpeed = 3f;
    public float rageMoveSpeed = 6f;
    public LayerMask obstacleLayer;
    public float avoidanceDistance = 1f;

    [Header("Interrupt Task Settings")]
    public float interruptRange = 2f;
    public float interruptCooldown = 8f;
    public int maxInterrupts = 3;
    private int currentInterrupts = 0;
    private float interruptTimer = 0f;

    [Header("Stun Settings")]
    public float stunCooldown = 40f;
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
    public GameObject[] interruptIcons; // 3 interrupt icons
    public GameObject[] stunIcons; // 5 stun icons
    public GameObject rageIcon;

    [Header("Input Keys")]
    public KeyCode interruptKey = KeyCode.E;
    public KeyCode stunKey = KeyCode.Q;
    public KeyCode rageKey = KeyCode.Space;

    private Rigidbody2D rb;
    private PlayerMovement playerMovement;
    private SpriteRenderer spriteRenderer;
    private Color originalColor;
    private Vector2 currentMoveDirection;
    private StunnableScript survivorStunScript;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        playerMovement = GetComponent<PlayerMovement>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (spriteRenderer != null)
            originalColor = spriteRenderer.color;

        // Find survivor's stun script
        if (survivor != null)
            survivorStunScript = survivor.GetComponent<StunnableScript>();

        // Initialize UI
        if (interruptCooldownCanvas != null) interruptCooldownCanvas.SetActive(false);
        if (stunCooldownCanvas != null) stunCooldownCanvas.SetActive(false);
        if (rageCooldownCanvas != null) rageCooldownCanvas.SetActive(false);

        UpdateAbilityIcons();
    }

    void Update()
    {
        HandleCooldowns();
        HandleInput();
        UpdateUI();
    }

    void FixedUpdate()
    {
        HandleMovement();
    }

    void HandleCooldowns()
    {
        // Interrupt cooldown
        if (interruptTimer > 0)
        {
            interruptTimer -= Time.deltaTime;
        }

        // Stun cooldown
        if (stunTimer > 0)
        {
            stunTimer -= Time.deltaTime;
        }

        // Rage cooldown and duration
        if (rageTimer > 0)
        {
            rageTimer -= Time.deltaTime;
            if (isInRageMode && rageTimer <= 0)
            {
                EndRageMode();
            }
        }
    }

    void HandleInput()
    {
        // Interrupt task
        if (Input.GetKeyDown(interruptKey) && CanUseInterrupt())
        {
            TryInterruptTask();
        }

        // Stun survivor
        if (Input.GetKeyDown(stunKey) && CanUseStun())
        {
            TryStunSurvivor();
        }

        // Rage mode
        if (Input.GetKeyDown(rageKey) && CanUseRage())
        {
            ActivateRageMode();
        }
    }

    void HandleMovement()
    {
        if (playerMovement != null && !playerMovement.canMove)
        {
            rb.velocity = Vector2.zero;
            return;
        }

        Vector2 movement = Vector2.zero;
        movement.x = Input.GetAxisRaw("Horizontal");
        movement.y = Input.GetAxisRaw("Vertical");

        if (movement.magnitude > 0)
        {
            // Obstacle avoidance
            RaycastHit2D hit = Physics2D.Raycast(transform.position, movement.normalized, avoidanceDistance, obstacleLayer);
            if (hit.collider != null)
            {
                float angle = Vector2.SignedAngle(movement, hit.normal);
                movement = Quaternion.Euler(0, 0, angle > 0 ? 90 : -90) * movement;
            }

            float currentSpeed = isInRageMode ? rageMoveSpeed : normalMoveSpeed;
            rb.velocity = movement.normalized * currentSpeed;
        }
        else
        {
            rb.velocity = Vector2.zero;
        }
    }

    bool CanUseInterrupt()
    {
        return currentInterrupts < maxInterrupts && interruptTimer <= 0f;
    }

    bool CanUseStun()
    {
        return currentStuns < maxStuns && stunTimer <= 0f;
    }

    bool CanUseRage()
    {
        return !isInRageMode && rageTimer <= 0f;
    }

    void TryInterruptTask()
    {
        // Find nearby code check games
        CodeCheckGame[] codeGames = FindObjectsOfType<CodeCheckGame>();

        foreach (CodeCheckGame codeGame in codeGames)
        {
            if (codeGame.triggeredObject != null)
            {
                float distance = Vector2.Distance(transform.position, codeGame.triggeredObject.transform.position);
                if (distance <= interruptRange)
                {
                    // Interrupt the task
                    codeGame.TamperCode();

                    currentInterrupts++;
                    interruptTimer = interruptCooldown;

                    UpdateAbilityIcons();

                    Debug.Log("Hunter interrupted a task!");
                    break; // Only interrupt one task at a time
                }
            }
        }
    }

    void TryStunSurvivor()
    {
        if (survivor != null && survivorStunScript != null)
        {
            float distance = Vector2.Distance(transform.position, survivor.position);

            // Stun has a longer range than interrupt
            if (distance <= interruptRange * 2f)
            {
                survivorStunScript.Stun(stunDuration);

                currentStuns++;
                stunTimer = stunCooldown;

                UpdateAbilityIcons();

                Debug.Log("Hunter stunned the survivor!");
            }
        }
    }

    void ActivateRageMode()
    {
        isInRageMode = true;
        rageTimer = rageDuration;

        // Change visual appearance during rage mode
        if (spriteRenderer != null)
            spriteRenderer.color = Color.red;

        UpdateAbilityIcons();

        Debug.Log("Hunter activated Rage Mode!");
    }

    void EndRageMode()
    {
        isInRageMode = false;
        rageTimer = rageCooldown; // Set cooldown timer

        // Restore original appearance
        if (spriteRenderer != null)
            spriteRenderer.color = originalColor;

        UpdateAbilityIcons();

        Debug.Log("Rage Mode ended. Cooldown started.");
    }

    void UpdateUI()
    {
        // Interrupt cooldown UI
        if (interruptTimer > 0 && interruptCooldownCanvas != null)
        {
            interruptCooldownCanvas.SetActive(true);
            if (interruptCooldownText != null)
                interruptCooldownText.text = $"Interrupt: {interruptTimer:F1}s";
        }
        else if (interruptCooldownCanvas != null)
        {
            interruptCooldownCanvas.SetActive(false);
        }

        // Stun cooldown UI
        if (stunTimer > 0 && stunCooldownCanvas != null)
        {
            stunCooldownCanvas.SetActive(true);
            if (stunCooldownText != null)
                stunCooldownText.text = $"Stun: {stunTimer:F1}s";
        }
        else if (stunCooldownCanvas != null)
        {
            stunCooldownCanvas.SetActive(false);
        }

        // Rage cooldown/duration UI
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
        {
            rageCooldownCanvas.SetActive(false);
        }
    }

    void UpdateAbilityIcons()
    {
        // Update interrupt icons
        for (int i = 0; i < interruptIcons.Length; i++)
        {
            if (interruptIcons[i] != null)
            {
                interruptIcons[i].SetActive(i < (maxInterrupts - currentInterrupts));
            }
        }

        // Update stun icons
        for (int i = 0; i < stunIcons.Length; i++)
        {
            if (stunIcons[i] != null)
            {
                stunIcons[i].SetActive(i < (maxStuns - currentStuns));
            }
        }

        // Update rage icon
        if (rageIcon != null)
        {
            // Show icon if rage is available or currently active
            rageIcon.SetActive(isInRageMode || rageTimer <= 0f);
        }
    }

    // Method to reset abilities (called when round starts)
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
    }

    private void OnDrawGizmosSelected()
    {
        // Draw interrupt range
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interruptRange);

        // Draw stun range
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, interruptRange * 2f);
    }
}