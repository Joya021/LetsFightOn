using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class PlayerMovement : MonoBehaviour
{
    public float dropThroughTime = 0.5f;
    public float speed = 5f;
    public Rigidbody2D rb;
    public bool canMove = true;

    [Header("UI")]
    public Text stunCooldownText; // Assign in Inspector

    [HideInInspector] public bool isStunned = false;

    private float stunCooldownTimer = 0f;
    private PlatformEffector2D currentEffector;
    private SpriteRenderer spriteRenderer;
    private Color originalColor;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
            originalColor = spriteRenderer.color;
    }

    void Update()
    {
        // Detect platform directly under player
        RaycastHit2D hit = Physics2D.Raycast(transform.position, Vector2.down, 1f);
        if (hit.collider != null)
        {
            currentEffector = hit.collider.GetComponent<PlatformEffector2D>();
        }
        else
        {
            currentEffector = null;
        }

        // Press J to drop through platform
        if (Input.GetKeyDown(KeyCode.J) && currentEffector != null)
        {
            StartCoroutine(DropThroughPlatform(currentEffector));
        }

        // Update stun cooldown
        if (isStunned)
        {
            stunCooldownTimer -= Time.deltaTime;
            if (stunCooldownText != null)
                stunCooldownText.text = $"Stunned: {stunCooldownTimer:F1}s";

            if (stunCooldownTimer <= 0f)
            {
                isStunned = false;
                UnlockMovement();
            }
        }
    }

    void FixedUpdate()
    {
        if (!canMove)
        {
            rb.velocity = Vector2.zero;
            return;
        }

        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        Vector2 movement = new Vector2(horizontal, vertical).normalized * speed * Time.fixedDeltaTime;

        rb.MovePosition(rb.position + movement);
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
    }

    public void UnlockMovement()
    {
        canMove = true;
        isStunned = false;

        if (spriteRenderer != null)
            spriteRenderer.color = originalColor;

        if (stunCooldownText != null)
            stunCooldownText.text = "";
    }
}