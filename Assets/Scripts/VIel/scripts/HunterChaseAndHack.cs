using UnityEngine;
using System.Collections;

public class HunterChaseAndHack : MonoBehaviour
{
    [Header("References")]
    public Transform player;
    public Rigidbody2D rb;

    [Header("Notify UI")]
    public GameObject notificationCanvas;
    public UnityEngine.UI.Text notificationText;

    [Header("Stun Warning UI")]
    public GameObject countdownCanvas;
    public UnityEngine.UI.Text countdownText;

    [Header("Movement")]
    public float moveSpeed = 2f;
    public float slowMoveSpeed = 0.8f;
    public float hackRange = 1.5f;
    public float stopDistance = 0.2f;

    [Header("Hack Settings")]
    public int maxHacks = 2;
    public float notificationDuration = 2f;
    public Color flashColor = Color.red;
    public float flashDuration = 0.5f;
    public float minTamperDelay = 3f;
    public float maxTamperDelay = 10f;

    private StunnableScript stunScript;
    private bool playerHasMoved = false;
    private Vector2 playerStartPos;
    private bool isHacking = false;
    private int hackCount = 0;

    private CodeCheckGame currentTarget;
    private CodeCheckGame lastCorrectlySolvedObject;
    private bool chasingPlayer = true;

    private Coroutine tamperDelayCoroutine;

    void Awake()
    {
        if (rb == null)
            rb = GetComponent<Rigidbody2D>();

        if (rb == null)
        {
            Debug.LogError("Hunter is missing Rigidbody2D component!", this);
            enabled = false;
            return;
        }

        stunScript = GetComponent<StunnableScript>();
    }

    void Start()
    {
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
        if (stunScript != null && stunScript.IsStunned || isHacking || player == null || !playerHasMoved)
        {
            rb.velocity = Vector2.zero;
            return;
        }

        Vector2 targetPosition;

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

        float distToTarget = Vector2.Distance(transform.position, targetPosition);

        if ((chasingPlayer && distToTarget <= stopDistance) || (!chasingPlayer && distToTarget <= hackRange))
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
            Vector2 direction = (targetPosition - (Vector2)transform.position).normalized;
            rb.velocity = direction * moveSpeed;
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

        while (Vector2.Distance(transform.position, interactable.position) > hackRange)
        {
            Vector2 direction = ((Vector2)interactable.position - (Vector2)transform.position).normalized;
            rb.velocity = direction * moveSpeed;
            yield return new WaitForSeconds(0.1f);
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

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayHunterInterruptCode();

        if (GameObject.FindObjectOfType<GameManager>() is GameManager gm)
            gm.UnregisterCorrectObject(currentTarget);

        ShowNotification(" Hunter tampered with your code!");
        StartCoroutine(FlashInputField());
    }

    void ShowNotification(string message)
    {
        if (notificationCanvas == null || notificationText == null) return;

        StopAllCoroutines();
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

        UnityEngine.UI.Graphic inputGraphic = currentTarget.codeInputField.GetComponent<UnityEngine.UI.Graphic>();
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
}
