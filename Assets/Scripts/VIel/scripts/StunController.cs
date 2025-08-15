using System.Collections;
using System.Collections.Generic;
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

    public Text cooldownText; // Assign in Inspector

    void Update()
    {
        // Cooldown countdown
        if (cooldownTimer > 0)
        {
            cooldownTimer -= Time.deltaTime;
            cooldownText.text = $"{cooldownTimer:F1}";
        }
        else
        {
            cooldownText.text = " ";
        }

        // Adjust stun range if enemy nearby
        bool enemyNearby = Physics2D.OverlapCircle(transform.position, detectEnemyRange, enemyLayer);
        float stunRange = enemyNearby ? nearEnemyStunRange : baseStunRange;

        // Try stun
        if (Input.GetKeyDown(stunKey) && cooldownTimer <= 0f)
        {
            TryStun(stunRange);
            cooldownTimer = stunCooldown;
        }
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

