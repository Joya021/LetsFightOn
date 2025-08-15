using UnityEngine;
using System.Collections;
using System.Text;
using UnityEngine.UI;

public class HunterChaseAndHack : MonoBehaviour
{

    [Header("References")]
    public Transform player;
    public Text notificationText;
    public Text countdownText;

    [Header("Movement")]
    public float moveSpeed = 2f;
    public float slowMoveSpeed = 0.8f;
    public float hackRange = 1.5f;
    public float stopDistance = 0.2f; // distance to stop near player/target

    [Header("Obstacle Avoidance")]
    public LayerMask obstacleLayer;
    public float avoidanceDistance = 1f;
    public float avoidanceTurnSpeed = 1f;

    [Header("Hack Settings")]
    public int maxHacks = 2;
    public float notificationDuration = 2f;
    public Color flashColor = Color.red;
    public float flashDuration = 0.5f;
    public float minTamperDelay = 3f; // New variable for min tamper delay
    public float maxTamperDelay = 10f; // New variable for max tamper delay

    private Rigidbody2D rb;
    private StunnableScript stunScript;
    private Vector2 playerStartPos;
    private bool playerHasMoved = false;
    private bool isHacking = false;
    private int hackCount = 0;
    private Vector2 currentMoveDirection;

    private CodeCheckGame currentTarget;
    private Transform interactableTransform;
    private CodeCheckGame lastCorrectlySolvedObject;
    private bool chasingPlayer = true;
    private bool isCollidingWithObstacle = false;
    private Coroutine tamperDelayCoroutine;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        stunScript = GetComponent<StunnableScript>();
        if (player != null)
            playerStartPos = player.position;
        if (notificationText != null)
            notificationText.gameObject.SetActive(false);
        if (countdownText != null)
            countdownText.gameObject.SetActive(false);
    }

    void Update()
    {
        if (!playerHasMoved && player != null)
        {
            if (Vector2.Distance(player.position, playerStartPos) > 0.01f)
                playerHasMoved = true;
        }
    }

    void FixedUpdate()
    {
        if (stunScript != null && stunScript.IsStunned || isHacking)
        {
            rb.velocity = Vector2.zero;
            return;
        }

        if (playerHasMoved && player != null)
        {
            Vector2 targetPosition;
            float currentMoveSpeed = isCollidingWithObstacle ? slowMoveSpeed : moveSpeed;

            if (lastCorrectlySolvedObject != null && lastCorrectlySolvedObject.triggeredObject != null && hackCount < maxHacks)
            {
                targetPosition = lastCorrectlySolvedObject.triggeredObject.transform.position;
                chasingPlayer = false;
            }
            else
            {
                targetPosition = player.position;
                chasingPlayer = true;
            }

            Vector2 desiredDirection = (targetPosition - (Vector2)transform.position).normalized;

            // Obstacle avoidance
            RaycastHit2D hit = Physics2D.Raycast(transform.position, desiredDirection, avoidanceDistance, obstacleLayer);
            if (hit.collider != null)
            {
                float angle = Vector2.SignedAngle(desiredDirection, hit.normal);
                desiredDirection = Quaternion.Euler(0, 0, angle > 0 ? 90 : -90) * desiredDirection;
                currentMoveDirection = Vector2.Lerp(currentMoveDirection, desiredDirection, avoidanceTurnSpeed * Time.fixedDeltaTime);
            }
            else
            {
                currentMoveDirection = Vector2.Lerp(currentMoveDirection, desiredDirection, avoidanceTurnSpeed * Time.fixedDeltaTime);
            }

            // Stop movement when close to player or hack target
            if ((chasingPlayer && Vector2.Distance(transform.position, player.position) <= stopDistance) ||
                (!chasingPlayer && Vector2.Distance(transform.position, targetPosition) <= hackRange))
            {
                rb.velocity = Vector2.zero;

                if (!chasingPlayer && lastCorrectlySolvedObject != null)
                {
                    // Start hack if close enough
                    StartCoroutine(StunCountdownAndHack(lastCorrectlySolvedObject, lastCorrectlySolvedObject.triggeredObject.transform));
                    lastCorrectlySolvedObject = null;
                }
            }
            else
            {
                rb.velocity = currentMoveDirection.normalized * currentMoveSpeed;
            }
        }
        else
        {
            rb.velocity = Vector2.zero;
        }
    }

    public void SetObjectColor(GameObject obj, Color color)
    {
        var renderer = obj.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material.color = color;
        }
    }

    public void StartStunWarningCountdown(int seconds = 3)
    {
        if (countdownText != null)
            StartCoroutine(StunWarningCoroutine(seconds));
    }

    private IEnumerator StunWarningCoroutine(int seconds)
    {
        countdownText.gameObject.SetActive(true);

        for (int i = seconds; i > 0; i--)
        {
            countdownText.text = $"Hunter is about to stun you: {i}";
            yield return new WaitForSeconds(1f);
        }

        countdownText.gameObject.SetActive(false);
    }

    IEnumerator StunCountdownAndHack(CodeCheckGame target, Transform interactable)
    {
        if (hackCount >= maxHacks) yield break;
        isHacking = true;

        while (Vector3.Distance(transform.position, interactable.position) > hackRange)
        {
            Vector2 direction = (interactable.position - transform.position).normalized;
            rb.velocity = direction * moveSpeed;
            yield return new WaitForFixedUpdate();
        }
        rb.velocity = Vector2.zero;

        currentTarget = target;
        TamperWithInput();
        hackCount++;

        yield return new WaitForSeconds(1f);
        isHacking = false;
    }

    void TamperWithInput()
    {
        if (currentTarget == null) return;

        // This is where you would call the TamperCode() method on the CodeCheckGame script.
        currentTarget.TamperCode();

        ShowNotification("⚠ Hunter tampered with your code!");
        StartCoroutine(FlashInputField());
        Debug.Log($"💀 Hunter tampered with the code at {currentTarget.name}");
    }
    void ShowNotification(string message)
    {
        if (notificationText == null) return;
        StopCoroutine("ShowNotificationRoutine");
        StartCoroutine(ShowNotificationRoutine(message));
    }

    IEnumerator ShowNotificationRoutine(string message)
    {
        notificationText.text = message;
        notificationText.gameObject.SetActive(true);
        yield return new WaitForSeconds(notificationDuration);
        notificationText.gameObject.SetActive(false);
    }
    IEnumerator FlashInputField()
    {
        if (currentTarget == null || currentTarget.codeInputField == null) yield break;
        Graphic inputGraphic = currentTarget.codeInputField.GetComponent<Graphic>();
        if (inputGraphic != null)
        {
            Color originalColor = inputGraphic.color;
            inputGraphic.color = flashColor;
            yield return new WaitForSeconds(flashDuration);
            inputGraphic.color = originalColor;
        }
    }

    public void NotifyCorrectObjectSolved(CodeCheckGame solvedObject)
    {
        if (tamperDelayCoroutine != null)
        {
            StopCoroutine(tamperDelayCoroutine);
        }

        tamperDelayCoroutine = StartCoroutine(StartTamperAfterDelay(solvedObject));
    }

    private IEnumerator StartTamperAfterDelay(CodeCheckGame target)
    {
        float delay = Random.Range(minTamperDelay, maxTamperDelay);
        Debug.Log($"Hunter will start moving to tamper in {delay:F1} seconds.");
        yield return new WaitForSeconds(delay);

        if (stunScript != null && !stunScript.IsStunned && target != null)
        {
            lastCorrectlySolvedObject = target;
            Debug.Log("Hunter is now moving to tamper with the code.");
        }
        else
        {
            lastCorrectlySolvedObject = null;
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("lobstacle"))
        {
            isCollidingWithObstacle = true;
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("lobstacle"))
        {
            isCollidingWithObstacle = false;
        }
    }
}