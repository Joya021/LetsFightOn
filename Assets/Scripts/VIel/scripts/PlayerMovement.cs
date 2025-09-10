using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class PlayerMovement : MonoBehaviour
{
    public float dropThroughTime = 0.5f;
    public float speed = 5f;
    public float jumpThroughDuration = 0.3f; // How long to disable collisions
    public LayerMask enemyLayers = 1 << 6; // Layers that can be jumped through (enemies, obstacles)
    public Rigidbody2D rb;
    public bool canMove = true;

    [Header("UI - Stun Ability Cooldown")]
    public GameObject stunCooldownCanvas;
    public Text stunCooldownText;

    [Header("UI - Move Again Cooldown")]
    public GameObject moveAgainCanvas;
    public Text moveAgainText;

    [HideInInspector] public bool isStunned = false;
    [HideInInspector] public bool isJumpingThrough = false;

    private float stunCooldownTimer = 0f;
    private PlatformEffector2D currentEffector;
    private SpriteRenderer spriteRenderer;
    private Color originalColor;
    private Animator animk; // Using animk as the Animator reference now
    private Collider2D playerCollider;
    Vector2 movement;
    private bool moving;
    private Vector2 lastDirection;  // Keep track of the last direction
    private bool isAttacking = false; // Track whether the player is attacking
    private bool attackInputReceived = false;  // To ensure the input is only processed once per attack

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        animk = GetComponent<Animator>(); // Get the reference to animk
        playerCollider = GetComponent<Collider2D>();

        if (spriteRenderer != null)
            originalColor = spriteRenderer.color;

        if (stunCooldownCanvas != null)
            stunCooldownCanvas.SetActive(false);

        if (moveAgainCanvas != null)
            moveAgainCanvas.SetActive(false);
    }

    void Update()
    {
        // Jump through colliders with Spacebar
        if (Input.GetKeyDown(KeyCode.Space) && canMove && !isStunned && !isJumpingThrough)
        {
            StartCoroutine(JumpThroughColliders());
        }

        // Platform drop-through detection
        RaycastHit2D hit = Physics2D.Raycast(transform.position, Vector2.down, 1f);
        if (hit.collider != null)
            currentEffector = hit.collider.GetComponent<PlatformEffector2D>();
        else
            currentEffector = null;

        if (Input.GetKeyDown(KeyCode.J) && currentEffector != null)
            StartCoroutine(DropThroughPlatform(currentEffector));

        // Stun system
        if (isStunned)
        {
            stunCooldownTimer -= Time.deltaTime;
            if (moveAgainCanvas != null)
                moveAgainCanvas.SetActive(true);
            if (moveAgainText != null)
                moveAgainText.text = $"{stunCooldownTimer:F1}";
            if (stunCooldownTimer <= 0f)
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

        // Attack input check
        if (Input.GetKeyDown(KeyCode.E) && canMove && !isStunned && !attackInputReceived)
        {
            TriggerAttack();
        }
    }

    void FixedUpdate()
    {
        if (!canMove)
        {
            rb.velocity = Vector2.zero;
            animk.SetFloat("X", movement.x);
            animk.SetFloat("Y", movement.y);
            return;
        }

        movement.x = Input.GetAxisRaw("Horizontal");
        movement.y = Input.GetAxisRaw("Vertical");

        // Save the last movement direction
        if (movement.magnitude > 0.1f)
        {
            lastDirection = movement.normalized;
        }

        // Apply movement
        rb.MovePosition(rb.position + movement.normalized * speed * Time.deltaTime);

        // Animate based on movement
        Animate();
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

        // If moving, set the Moving bool and X/Y direction
        if (moving)
        {
            animk.SetBool("Moving", true);
            animk.SetFloat("X", movement.x);  // Horizontal movement direction
            animk.SetFloat("Y", movement.y);  // Vertical movement direction
        }
        else
        {
            animk.SetBool("Moving", false);

            // If not moving, use the last direction
            animk.SetFloat("X", lastDirection.x);
            animk.SetFloat("Y", lastDirection.y);
        }

        // Handle Attacking animation
        if (isAttacking)
        {
            animk.SetBool("Attacking", true);  // Trigger the attack animation
        }
        else
        {
            animk.SetBool("Attacking", false); // Reset the attack animation
        }
    }

    private void TriggerAttack()
    {
        isAttacking = true;
        attackInputReceived = true;  // Mark that the attack input was received

        // Optionally: You could add a cooldown before the attack animation resets, for example:
        // StartCoroutine(ResetAttackAnimationAfterDelay(0.5f)); // Reset attack animation after 0.5 seconds (adjust as needed)

        // Reset after animation finishes (You can do this with animation events or just a delay)
        // Here, I will reset the attack animation after 0.5 seconds (adjust timing based on the animation length)
        StartCoroutine(ResetAttackAnimationAfterDelay(0.5f));
    }

    private IEnumerator ResetAttackAnimationAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        isAttacking = false;  // Reset attack state
        attackInputReceived = false;  // Allow the attack to be triggered again
    }

    IEnumerator JumpThroughColliders()
    {
        isJumpingThrough = true;

        // Get all colliders that are on enemy layers
        Collider2D[] enemyColliders = Physics2D.OverlapCircleAll(transform.position, 10f, enemyLayers);

        // Disable collisions with enemy/obstacle colliders only
        foreach (Collider2D enemyCollider in enemyColliders)
        {
            if (playerCollider != null && enemyCollider != null)
                Physics2D.IgnoreCollision(playerCollider, enemyCollider, true);
        }

        // Optional: Change color to indicate jump-through state
        if (spriteRenderer != null)
            spriteRenderer.color = Color.yellow;

        // Wait for the duration
        yield return new WaitForSeconds(jumpThroughDuration);

        // Re-enable collisions with enemy colliders
        foreach (Collider2D enemyCollider in enemyColliders)
        {
            if (playerCollider != null && enemyCollider != null)
                Physics2D.IgnoreCollision(playerCollider, enemyCollider, false);
        }

        // Restore original color
        if (spriteRenderer != null)
            spriteRenderer.color = originalColor;

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
        isStunned = stunTime > 0f;
        stunCooldownTimer = stunTime;

        if (spriteRenderer != null && stunTime > 0)
            spriteRenderer.color = Color.blue;

        if (moveAgainCanvas != null)
            moveAgainCanvas.SetActive(true);
    }

    public void UnlockMovement()
    {
        canMove = true;
        isStunned = false;

        if (spriteRenderer != null && !isJumpingThrough)
            spriteRenderer.color = originalColor;

        if (moveAgainText != null)
            moveAgainText.text = "";

        if (moveAgainCanvas != null)
            moveAgainCanvas.SetActive(false);
    }
}
