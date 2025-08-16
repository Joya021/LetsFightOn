using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class PlayerMovement : MonoBehaviour
{
    public float dropThroughTime = 0.5f;
    public float speed = 5f;
    public Rigidbody2D rb;
    public bool canMove = true;

    [Header("UI - Stun Ability Cooldown")]
    public GameObject stunCooldownCanvas;
    public Text stunCooldownText;

    [Header("UI - Move Again Cooldown")]
    public GameObject moveAgainCanvas;
    public Text moveAgainText;

    [HideInInspector] public bool isStunned = false;

    private float stunCooldownTimer = 0f;
    private PlatformEffector2D currentEffector;
    private SpriteRenderer spriteRenderer;
    private Color originalColor;

    private Animator animator;

    Vector2 movement;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
        if (spriteRenderer != null)
            originalColor = spriteRenderer.color;

        if (stunCooldownCanvas != null)
            stunCooldownCanvas.SetActive(false);
        if (moveAgainCanvas != null)
            moveAgainCanvas.SetActive(false);
    }

    void Update()
    {
        RaycastHit2D hit = Physics2D.Raycast(transform.position, Vector2.down, 1f);
        if (hit.collider != null)
            currentEffector = hit.collider.GetComponent<PlatformEffector2D>();
        else
            currentEffector = null;

        if (Input.GetKeyDown(KeyCode.J) && currentEffector != null)
            StartCoroutine(DropThroughPlatform(currentEffector));

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
    }

    void FixedUpdate()
    {
        if (!canMove)
        {
            rb.velocity = Vector2.zero;
            return;
        }

        movement.x = Input.GetAxisRaw("Horizontal");
        movement.y = Input.GetAxisRaw("Vertical");

        // ✅ Only allow one axis at a time (no diagonals)
        //if (Mathf.Abs(horizontal) > 0.1f)
        //    vertical = 0f;
        //else if (Mathf.Abs(vertical) > 0.1f)
        //    horizontal = 0f;

        //Vector2 movement = new Vector2(horizontal, vertical) * speed * Time.fixedDeltaTime;
        rb.MovePosition(rb.position + movement.normalized * speed * Time.deltaTime);

        animator.SetFloat("MovementX", movement.x);
        animator.SetFloat("MovementY", movement.y);
        animator.SetFloat("Speed", movement.magnitude);
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

        if (spriteRenderer != null)
            spriteRenderer.color = originalColor;

        if (moveAgainText != null)
            moveAgainText.text = "";

        if (moveAgainCanvas != null)
            moveAgainCanvas.SetActive(false);
    }
}
