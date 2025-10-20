using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

public class HunterChaseAndHack : MonoBehaviour
{
    [Header("References")]
    public Transform player;

    [Header("Notify UI")]
    public GameObject notificationCanvas;
    public Text notificationText;

    [Header("Stun Warning UI")]
    public GameObject countdownCanvas;
    public Text countdownText;

    [Header("Movement")]
    public float moveSpeed = 2f;
    public float slowMoveSpeed = 0.8f;
    public float hackRange = 1.5f;
    public float stopDistance = 0.2f;
    public float rotationSpeed = 5f;

    [Header("Smart Movement")]
    public float predictionTime = 0.5f;
    public float stuckCheckTime = 1f;
    public float stuckThreshold = 0.1f;
    public float unstuckForce = 2f;

    [Header("Advanced AI Behaviors")]
    public bool useAmbushMode = true;
    public float ambushActivationDistance = 8f; // Distance to start flanking
    public float ambushCircleRadius = 3f; // How far to circle around player
    public float ambushDuration = 3f; // How long to circle before attacking
    public bool useLungeAttack = true;
    public float lungeSpeed = 4f; // Speed when lunging at player
    public float lungeDistance = 5f; // Distance to start lunge
    public float lungeCooldown = 8f;
    public bool hideWhenPlayerLooksAway = true;
    public float hideDistance = 6f; // Max distance to hide behavior
    public float hideSlowdownFactor = 0.3f; // How much to slow when hiding

    [Header("Patrol & Hunt")]
    public bool usePatrolMode = true;
    public Transform[] patrolPoints; // Set patrol waypoints
    public float patrolWaitTime = 2f;
    public float detectionRange = 7f; // Range to detect player
    public float losePlayerTime = 5f; // Time before going back to patrol

    [Header("Psychological Warfare")]
    public bool useJumpScares = true;
    public float jumpScareChance = 0.3f; // 30% chance per ambush
    public GameObject jumpScareEffect; // Optional particle/sprite
    public AudioClip jumpScareSound;
    public AudioClip huntingSound;
    public AudioClip ambushSound;

    [Header("Obstacle Avoidance")]
    public LayerMask obstacleLayer;
    public float avoidanceDistance = 1.5f;
    public float avoidanceRayCount = 5;
    public float avoidanceAngleRange = 90f;
    public float wallAvoidanceStrength = 2f;

    [Header("Hack Settings")]
    public int maxHacks = 2;
    public float notificationDuration = 2f;
    public Color flashColor = Color.red;
    public float flashDuration = 0.5f;
    public float minTamperDelay = 3f;
    public float maxTamperDelay = 10f;

    private Rigidbody2D rb;
    private StunnableScript stunScript;
    private AudioSource audioSource;
    private Vector2 playerStartPos;
    private Vector2 lastPlayerPos;
    private bool playerHasMoved = false;
    private bool isHacking = false;
    private int hackCount = 0;

    private OfflineCodeCheckGame currentTarget;
    private OfflineCodeCheckGame lastCorrectlySolvedObject;
    private bool chasingPlayer = true;
    private bool isCollidingWithObstacle = false;
    private Coroutine tamperDelayCoroutine;

    // Smart movement tracking
    private Vector2 lastPosition;
    private float stuckTimer = 0f;
    private Vector2 unstuckDirection;
    private bool isUnstucking = false;

    // Advanced AI states
    private enum HunterState { Patrol, Hunting, Ambush, Lunging, Hiding, Hacking }
    private HunterState currentState = HunterState.Patrol;
    private int currentPatrolIndex = 0;
    private float patrolWaitTimer = 0f;
    private float losePlayerTimer = 0f;
    private float lungeTimer = 0f;
    private bool canLunge = true;
    private float ambushTimer = 0f;
    private Vector2 ambushTargetPos;
    private bool hasTriggeredJumpScare = false;

    // Player awareness
    private Vector2 lastKnownPlayerPosition;
    private bool playerInSight = false;
    private float playerFacingAngle = 0f;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        stunScript = GetComponent<StunnableScript>();
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        if (player != null)
        {
            playerStartPos = player.position;
            lastPlayerPos = player.position;
            lastKnownPlayerPosition = player.position;
        }

        if (notificationCanvas != null)
            notificationCanvas.SetActive(false);
        if (countdownCanvas != null)
            countdownCanvas.SetActive(false);

        lastPosition = transform.position;
        InvokeRepeating(nameof(CheckIfStuck), stuckCheckTime, stuckCheckTime);

        if (usePatrolMode && patrolPoints != null && patrolPoints.Length > 0)
            currentState = HunterState.Patrol;
    }

    void Update()
    {
        if (!playerHasMoved && player != null)
        {
            if (Vector2.Distance(player.position, playerStartPos) > 0.01f)
            {
                playerHasMoved = true;
                if (usePatrolMode)
                    currentState = HunterState.Hunting;
            }
        }

        if (player != null)
        {
            lastPlayerPos = player.position;
            UpdatePlayerAwareness();
        }

        UpdateHunterState();
    }

    void UpdatePlayerAwareness()
    {
        Vector2 toPlayer = (Vector2)player.position - (Vector2)transform.position;
        float distanceToPlayer = toPlayer.magnitude;

        // Check if player is in detection range
        if (distanceToPlayer <= detectionRange)
        {
            // Raycast to see if there's line of sight
            RaycastHit2D hit = Physics2D.Raycast(transform.position, toPlayer.normalized, distanceToPlayer, obstacleLayer);
            playerInSight = (hit.collider == null);

            if (playerInSight)
            {
                lastKnownPlayerPosition = player.position;
                losePlayerTimer = 0f;
            }
        }
        else
        {
            playerInSight = false;
        }

        // Check if player is looking at hunter (assuming player faces right when moving right)
        if (player.GetComponent<Rigidbody2D>() != null)
        {
            Vector2 playerVelocity = player.GetComponent<Rigidbody2D>().velocity;
            if (playerVelocity.magnitude > 0.1f)
            {
                Vector2 hunterDirection = ((Vector2)transform.position - (Vector2)player.position).normalized;
                float dot = Vector2.Dot(playerVelocity.normalized, hunterDirection);
                playerFacingAngle = dot; // Positive = player moving toward hunter
            }
        }
    }

    void UpdateHunterState()
    {
        if (!playerHasMoved) return;
        if (stunScript != null && stunScript.IsStunned) return;

        float distanceToPlayer = Vector2.Distance(transform.position, player.position);

        switch (currentState)
        {
            case HunterState.Patrol:
                if (playerInSight)
                {
                    currentState = HunterState.Hunting;
                    PlaySound(huntingSound);
                }
                break;

            case HunterState.Hunting:
                // Transition to ambush if far enough and enabled
                if (useAmbushMode && distanceToPlayer > ambushActivationDistance && Random.value > 0.7f)
                {
                    currentState = HunterState.Ambush;
                    ambushTimer = ambushDuration;
                    CalculateAmbushPosition();
                    PlaySound(ambushSound);

                    if (useJumpScares && Random.value < jumpScareChance && !hasTriggeredJumpScare)
                    {
                        TriggerJumpScare();
                        hasTriggeredJumpScare = true;
                    }
                }
                // Transition to lunge if close and ready
                else if (useLungeAttack && canLunge && distanceToPlayer <= lungeDistance && distanceToPlayer > stopDistance)
                {
                    currentState = HunterState.Lunging;
                    StartCoroutine(LungeCooldown());
                }
                // Transition to hiding if player looking away
                else if (hideWhenPlayerLooksAway && playerFacingAngle < -0.3f && distanceToPlayer < hideDistance)
                {
                    currentState = HunterState.Hiding;
                }
                // Lose player and go back to patrol
                else if (!playerInSight)
                {
                    losePlayerTimer += Time.deltaTime;
                    if (losePlayerTimer >= losePlayerTime && usePatrolMode)
                    {
                        currentState = HunterState.Patrol;
                    }
                }
                break;

            case HunterState.Ambush:
                ambushTimer -= Time.deltaTime;
                if (ambushTimer <= 0 || distanceToPlayer < stopDistance * 2f)
                {
                    currentState = HunterState.Hunting;
                }
                break;

            case HunterState.Lunging:
                if (distanceToPlayer <= stopDistance)
                {
                    currentState = HunterState.Hunting;
                }
                break;

            case HunterState.Hiding:
                if (playerFacingAngle > 0f || distanceToPlayer > hideDistance)
                {
                    currentState = HunterState.Hunting;
                }
                break;
        }
    }

    void FixedUpdate()
    {
        if (stunScript != null && stunScript.IsStunned)
        {
            rb.velocity = Vector2.zero;
            return;
        }

        if (!playerHasMoved || player == null)
        {
            rb.velocity = Vector2.zero;
            return;
        }

        Vector2 targetPosition = GetTargetPosition();
        float distanceToTarget = Vector2.Distance(transform.position, targetPosition);

        // Check if reached destination
        float requiredDistance = chasingPlayer ? stopDistance : hackRange;
        if (distanceToTarget <= requiredDistance && currentState != HunterState.Ambush)
        {
            rb.velocity = Vector2.zero;

            if (!chasingPlayer && lastCorrectlySolvedObject != null)
            {
                StartCoroutine(StunCountdownAndHack(lastCorrectlySolvedObject, lastCorrectlySolvedObject.triggeredObject.transform));
                lastCorrectlySolvedObject = null;
            }
            return;
        }

        // Calculate movement based on state
        Vector2 moveDirection = CalculateMovementByState(targetPosition);
        float currentSpeed = GetCurrentSpeed();

        rb.velocity = moveDirection * currentSpeed;
    }

    Vector2 GetTargetPosition()
    {
        // Hacking terminal takes priority
        if (lastCorrectlySolvedObject != null &&
            lastCorrectlySolvedObject.triggeredObject != null &&
            hackCount < maxHacks)
        {
            chasingPlayer = false;
            currentState = HunterState.Hacking;
            return lastCorrectlySolvedObject.triggeredObject.transform.position;
        }

        chasingPlayer = true;

        switch (currentState)
        {
            case HunterState.Patrol:
                if (patrolPoints != null && patrolPoints.Length > 0)
                {
                    if (Vector2.Distance(transform.position, patrolPoints[currentPatrolIndex].position) < 0.5f)
                    {
                        patrolWaitTimer += Time.fixedDeltaTime;
                        if (patrolWaitTimer >= patrolWaitTime)
                        {
                            currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;
                            patrolWaitTimer = 0f;
                        }
                    }
                    return patrolPoints[currentPatrolIndex].position;
                }
                return lastKnownPlayerPosition;

            case HunterState.Ambush:
                return ambushTargetPos;

            case HunterState.Hunting:
            case HunterState.Lunging:
            case HunterState.Hiding:
                Vector2 playerVelocity = ((Vector2)player.position - lastPlayerPos) / Time.fixedDeltaTime;
                return (Vector2)player.position + (playerVelocity * predictionTime);

            default:
                return player.position;
        }
    }

    void CalculateAmbushPosition()
    {
        // Calculate a flanking position around the player
        Vector2 toPlayer = (Vector2)player.position - (Vector2)transform.position;
        float angle = Mathf.Atan2(toPlayer.y, toPlayer.x);

        // Randomly flank left or right
        float flankAngle = angle + (Random.value > 0.5f ? 90f : -90f) * Mathf.Deg2Rad;

        ambushTargetPos = (Vector2)player.position + new Vector2(
            Mathf.Cos(flankAngle) * ambushCircleRadius,
            Mathf.Sin(flankAngle) * ambushCircleRadius
        );
    }

    float GetCurrentSpeed()
    {
        if (isCollidingWithObstacle)
            return slowMoveSpeed;

        switch (currentState)
        {
            case HunterState.Lunging:
                return lungeSpeed;
            case HunterState.Hiding:
                return moveSpeed * hideSlowdownFactor;
            case HunterState.Patrol:
                return moveSpeed * 0.7f;
            case HunterState.Ambush:
                return moveSpeed * 1.2f;
            default:
                return moveSpeed;
        }
    }

    Vector2 CalculateMovementByState(Vector2 targetPosition)
    {
        Vector2 currentPos = transform.position;
        Vector2 desiredDirection = (targetPosition - currentPos).normalized;

        if (isUnstucking)
        {
            isUnstucking = false;
            return unstuckDirection;
        }

        // Add some erratic movement for ambush
        if (currentState == HunterState.Ambush)
        {
            float wobble = Mathf.Sin(Time.time * 3f) * 0.3f;
            desiredDirection = Quaternion.Euler(0, 0, wobble * 30f) * desiredDirection;
        }

        Vector2 avoidanceVector = GetAvoidanceVector(desiredDirection);
        Vector2 finalDirection = (desiredDirection + avoidanceVector).normalized;

        return finalDirection;
    }

    Vector2 GetAvoidanceVector(Vector2 desiredDirection)
    {
        Vector2 avoidanceForce = Vector2.zero;
        float baseAngle = Mathf.Atan2(desiredDirection.y, desiredDirection.x) * Mathf.Rad2Deg;

        for (int i = 0; i < avoidanceRayCount; i++)
        {
            float t = i / (avoidanceRayCount - 1f);
            float angle = baseAngle + (t - 0.5f) * avoidanceAngleRange;
            Vector2 rayDirection = new Vector2(
                Mathf.Cos(angle * Mathf.Deg2Rad),
                Mathf.Sin(angle * Mathf.Deg2Rad)
            );

            RaycastHit2D hit = Physics2D.Raycast(
                transform.position,
                rayDirection,
                avoidanceDistance,
                obstacleLayer
            );

            if (hit.collider != null)
            {
                float distanceFactor = 1f - (hit.distance / avoidanceDistance);
                Vector2 avoidDirection = ((Vector2)transform.position - hit.point).normalized;
                avoidanceForce += avoidDirection * distanceFactor * wallAvoidanceStrength;
            }
        }

        return avoidanceForce;
    }

    void CheckIfStuck()
    {
        if (!playerHasMoved || stunScript.IsStunned) return;

        float distanceMoved = Vector2.Distance(transform.position, lastPosition);

        if (distanceMoved < stuckThreshold && rb.velocity.magnitude > 0.1f)
        {
            stuckTimer += stuckCheckTime;

            if (stuckTimer >= stuckCheckTime * 2f)
            {
                UnstuckHunter();
                stuckTimer = 0f;
            }
        }
        else
        {
            stuckTimer = 0f;
        }

        lastPosition = transform.position;
    }

    void UnstuckHunter()
    {
        Vector2 currentVelocity = rb.velocity.normalized;
        float randomSign = Random.value > 0.5f ? 1f : -1f;
        unstuckDirection = new Vector2(-currentVelocity.y, currentVelocity.x) * randomSign;
        unstuckDirection = (unstuckDirection + currentVelocity * 0.5f).normalized;
        isUnstucking = true;
        rb.AddForce(unstuckDirection * unstuckForce, ForceMode2D.Impulse);
    }

    IEnumerator LungeCooldown()
    {
        canLunge = false;
        yield return new WaitForSeconds(lungeCooldown);
        canLunge = true;
    }

    void TriggerJumpScare()
    {
        if (jumpScareEffect != null)
        {
            GameObject effect = Instantiate(jumpScareEffect, transform.position, Quaternion.identity);
            Destroy(effect, 2f);
        }

        PlaySound(jumpScareSound);
        ShowNotification("👁️ THE HUNTER IS WATCHING...");
    }

    void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
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

    IEnumerator StunCountdownAndHack(OfflineCodeCheckGame target, Transform interactable)
    {
        if (hackCount >= maxHacks) yield break;
        isHacking = true;
        currentState = HunterState.Hacking;

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
        currentState = HunterState.Hunting;
    }

    void TamperWithInput()
    {
        if (currentTarget == null) return;

        currentTarget.TamperCode();

        OfflineGameManager gm = FindObjectOfType<OfflineGameManager>();
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

    public void NotifyCorrectObjectSolved(OfflineCodeCheckGame solvedObject)
    {
        if (tamperDelayCoroutine != null)
            StopCoroutine(tamperDelayCoroutine);

        tamperDelayCoroutine = StartCoroutine(StartTamperAfterDelay(solvedObject));
    }

    private IEnumerator StartTamperAfterDelay(OfflineCodeCheckGame target)
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

    void OnDestroy()
    {
        CancelInvoke();
    }

    // Debug visualization
    void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying) return;

        // Draw detection range
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        // Draw ambush range
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, ambushActivationDistance);

        // Draw lunge range
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, lungeDistance);

        // Draw current target
        if (currentState == HunterState.Ambush)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, ambushTargetPos);
            Gizmos.DrawWireSphere(ambushTargetPos, 0.5f);
        }
    }
}