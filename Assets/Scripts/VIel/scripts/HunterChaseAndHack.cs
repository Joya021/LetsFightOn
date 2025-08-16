using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class HunterChaseAndHack : MonoBehaviour
{
    [Header("References")]
    public Transform player;

    [Header("Notify UI")]
    public GameObject notificationCanvas; // Background + text
    public Text notificationText;

    [Header("Stun Warning UI")]
    public GameObject countdownCanvas; // Background + text
    public Text countdownText;

    [Header("Movement")]
    public float moveSpeed = 2f;
    public float slowMoveSpeed = 0.8f;
    public float hackRange = 1.5f;
    public float stopDistance = 0.2f;

    [Header("Obstacle Avoidance")]
    public LayerMask obstacleLayer;
    public float avoidanceDistance = 1f;
    public float avoidanceTurnSpeed = 1f;

    [Header("Hack Settings")]
    public int maxHacks = 2;
    public float notificationDuration = 2f;
    public Color flashColor = Color.red;
    public float flashDuration = 0.5f;
    public float minTamperDelay = 3f;
    public float maxTamperDelay = 10f;

    private Rigidbody2D rb;
    private StunnableScript stunScript;
    private Vector2 playerStartPos;
    private bool playerHasMoved = false;
    private bool isHacking = false;
    private int hackCount = 0;
    private Vector2 currentMoveDirection;

    private CodeCheckGame currentTarget;
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

        if (notificationCanvas != null)
            notificationCanvas.SetActive(false);
        if (countdownCanvas != null)
            countdownCanvas.SetActive(false);
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
        if ((stunScript != null && stunScript.IsStunned) || isHacking)
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

            RaycastHit2D hit = Physics2D.Raycast(transform.position, desiredDirection, avoidanceDistance, obstacleLayer);
            if (hit.collider != null)
            {
                float angle = Vector2.SignedAngle(desiredDirection, hit.normal);
                desiredDirection = Quaternion.Euler(0, 0, angle > 0 ? 90 : -90) * desiredDirection;
            }
            currentMoveDirection = Vector2.Lerp(currentMoveDirection, desiredDirection, avoidanceTurnSpeed * Time.fixedDeltaTime);

            if ((chasingPlayer && Vector2.Distance(transform.position, player.position) <= stopDistance) ||
                (!chasingPlayer && Vector2.Distance(transform.position, targetPosition) <= hackRange))
            {
                rb.velocity = Vector2.zero;

                if (!chasingPlayer && lastCorrectlySolvedObject != null)
                {
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

    public void StartStunWarningCountdown(int seconds = 3)
    {
        if (countdownCanvas != null)
            StartCoroutine(StunWarningCoroutine(seconds));
    }

    private IEnumerator StunWarningCoroutine(int seconds)
    {
        countdownCanvas.SetActive(true);

        for (int i = seconds; i > 0; i--)
        {
            if (countdownText != null)
                countdownText.text = $"{i}";
            yield return new WaitForSeconds(1f);
        }

        countdownCanvas.SetActive(false);
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

        currentTarget.TamperCode();

        // Remove from solved list if it was solved before
        GameManager gm = FindObjectOfType<GameManager>();
        if (gm != null)
            gm.UnregisterCorrectObject(currentTarget);

        ShowNotification("⚠ Hunter tampered with your code!");
        StartCoroutine(FlashInputField());
    }


    void ShowNotification(string message)
    {
        if (notificationCanvas == null || notificationText == null) return;
        StopCoroutine(nameof(ShowNotificationRoutine));
        StartCoroutine(ShowNotificationRoutine(message));
    }

    IEnumerator ShowNotificationRoutine(string message)
    {
        notificationCanvas.SetActive(true);
        notificationText.text = message;
        yield return new WaitForSeconds(notificationDuration);
        notificationCanvas.SetActive(false);
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
            StopCoroutine(tamperDelayCoroutine);

        tamperDelayCoroutine = StartCoroutine(StartTamperAfterDelay(solvedObject));
    }

    private IEnumerator StartTamperAfterDelay(CodeCheckGame target)
    {
        float delay = Random.Range(minTamperDelay, maxTamperDelay);
        yield return new WaitForSeconds(delay);

        if (stunScript != null && !stunScript.IsStunned && target != null)
            lastCorrectlySolvedObject = target;
        else
            lastCorrectlySolvedObject = null;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("lobstacle"))
            isCollidingWithObstacle = true;
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("lobstacle"))
            isCollidingWithObstacle = false;
    }
}
