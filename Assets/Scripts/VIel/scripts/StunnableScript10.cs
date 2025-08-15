using UnityEngine;

public class StunnableScript : MonoBehaviour
{
    public float stunDuration = 3f;
    public bool IsStunned { get; private set; }

    private float stunTimer;
    private PlayerMovement playerMovement;
    private SpriteRenderer sr;

    void Start()
    {
        playerMovement = GetComponent<PlayerMovement>();
        sr = GetComponent<SpriteRenderer>();
        if (sr != null)
            sr.color = Color.white;
    }

    void Update()
    {
        if (IsStunned)
        {
            stunTimer -= Time.deltaTime;
            if (stunTimer <= 0f)
                Unstun();
        }
    }

    public void Stun()
    {
        Stun(stunDuration);
    }

    public void Stun(float duration)
    {
        if (IsStunned) return;

        IsStunned = true;
        stunTimer = duration;

        Debug.Log($"{gameObject.name} is stunned for {duration} seconds!");

        if (playerMovement != null)
            playerMovement.LockMovement(duration);

        if (sr != null)
            sr.color = Color.blue;
    }

    void Unstun()
    {
        IsStunned = false;

        Debug.Log($"{gameObject.name} is no longer stunned.");

        if (playerMovement != null)
            playerMovement.UnlockMovement();

        if (sr != null)
            sr.color = Color.white;
    }
}