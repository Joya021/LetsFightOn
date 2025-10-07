using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class PlayerMovementOffline : MonoBehaviour
{
    public float dropThroughTime = 0.5f;
    public float speed = 5f;
    public float jumpThroughDuration = 0.3f;
    public LayerMask enemyLayers = 1 << 6;
    public Rigidbody2D rb;
    public bool canMove = true;

    [Header("Audio Settings")]
    public bool isSurvivor = true;

    [Header("UI - Move Again Cooldown")]
    public GameObject moveAgainCanvas;
    public Text moveAgainText;

    [HideInInspector] public bool isStunned = false;
    [HideInInspector] public bool isJumpingThrough = false;

    private float stunTimer = 0f;
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

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        animk = GetComponent<Animator>();
        playerCollider = GetComponent<Collider2D>();

        if (spriteRenderer != null)
            originalColor = spriteRenderer.color;

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
            stunTimer -= Time.deltaTime;

            if (moveAgainCanvas != null)
                moveAgainCanvas.SetActive(true);

            if (moveAgainText != null)
                moveAgainText.text = $"{stunTimer:F1}";

            if (stunTimer <= 0f)
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

            if (wasMovingLastFrame && AudioManager.Instance != null)
            {
                AudioManager.Instance.StopWalking();
                wasMovingLastFrame = false;
            }

            return;
        }

        movement.x = Input.GetAxisRaw("Horizontal");
        movement.y = Input.GetAxisRaw("Vertical");

        if (movement.magnitude > 0.1f)
        {
            lastDirection = movement.normalized;
        }

        rb.MovePosition(rb.position + movement.normalized * speed * Time.deltaTime);

        Animate();
        HandleWalkingAudio();
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

        if (stunTime > 0f)
        {
            isStunned = true;
            stunTimer = stunTime;

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

        if (spriteRenderer != null && !isJumpingThrough)
            spriteRenderer.color = originalColor;

        if (moveAgainText != null)
            moveAgainText.text = "";

        if (moveAgainCanvas != null)
            moveAgainCanvas.SetActive(false);
    }
}