using UnityEngine;
using UnityEngine.UI;

public class StunController : MonoBehaviour
{
    public float baseStunRange = 2f;
    public float nearEnemyStunRange = 4f;
    public float detectEnemyRange = 5f;
    public KeyCode stunKey = KeyCode.E;
    public LayerMask stunnableLayer;
    public LayerMask enemyLayer;

    public float stunCooldown = 5f;
    private float cooldownTimer = 0f;

    [Header("UI")]
    public GameObject stunCooldownCanvas; // BG + text
    public Text cooldownText;

    void Start()
    {
        if (stunCooldownCanvas != null)
            stunCooldownCanvas.SetActive(false);
    }

    void Update()
    {
        // Cooldown logic
        if (cooldownTimer > 0)
        {
            cooldownTimer -= Time.deltaTime;

            if (stunCooldownCanvas != null)
                stunCooldownCanvas.SetActive(true);
            if (cooldownText != null)
                cooldownText.text = $"{Mathf.Max(cooldownTimer, 0f):F1}s";

            if (cooldownTimer <= 0f)
            {
                cooldownTimer = 0f;
                if (stunCooldownCanvas != null)
                    stunCooldownCanvas.SetActive(false);
            }
        }

        // Prevent stun if player is in CodeCheckGame
        if (IsInCodeCheckGame()) return;

        // Adjust stun range if enemy nearby
        bool enemyNearby = Physics2D.OverlapCircle(transform.position, detectEnemyRange, enemyLayer);
        float stunRange = enemyNearby ? nearEnemyStunRange : baseStunRange;

        // Try stun
        if (Input.GetKeyDown(stunKey) && cooldownTimer <= 0f)
        {
            TryStun(stunRange);
            StartCooldown();
        }
    }

    private bool IsInCodeCheckGame()
    {
        GameManager gm = FindObjectOfType<GameManager>();
        if (gm != null)
        {
            foreach (var codeGame in gm.allCodeGames)
            {
                if (codeGame != null && codeGame.IsBeingInteractedWith)
                    return true;
            }
        }
        return false;
    }

    public void StartCooldown()
    {
        cooldownTimer = stunCooldown;
        if (stunCooldownCanvas != null)
            stunCooldownCanvas.SetActive(true);
    }

    void TryStun(float stunRange)
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, stunRange, stunnableLayer);
        foreach (Collider2D hit in hits)
        {
            StunnableScript stunnable = hit.GetComponent<StunnableScript>();
            if (stunnable != null)
            {
                stunnable.Stun();
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, baseStunRange);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectEnemyRange);
    }
}
