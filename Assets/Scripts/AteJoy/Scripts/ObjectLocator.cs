using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using System.Collections.Generic;

public class ObjectLocator : MonoBehaviour
{
    [Header("Refernces")]
    public Transform player;
    public RectTransform arrowUI;
    public float detectionRadius = 100f;

    [Header("Debug Info")]
    public Transform currentTarget;
    public List<Transform> activeTargets = new List<Transform>();

    private void Update()
    {
        if (player == null)
        {
            Debug.LogWarning("Player not assigned to ObjectLocator!");
            FindNearestTarget();
            PointArrowTowardTarget();
        }
    }

    public void RegisterTarget(Transform target)
    {
        if (!activeTargets.Contains(target))
        {
            activeTargets.Add(target);
        }
    }

    public void UnregisterTarget(Transform target)
    {
        if (activeTargets.Contains(target))
        {
            activeTargets.Remove(target);
        }
    }

    void FindNearestTarget()
    {
        if (activeTargets.Count == 0)
        {
            currentTarget = null;
            return;
        }

        float minDistance = Mathf.Infinity;
        Transform nearest = null;

        foreach (var t in activeTargets)
        {
            if (t == null) continue;

            float dist = Vector3.Distance(player.position, t.position);
            if (dist < minDistance && dist <= detectionRadius)
            {
                minDistance = dist;
                nearest = t.transform;
            }

            currentTarget = nearest;
        }
    }

    void PointArrowTowardTarget()
    {
            if (currentTarget == null)
            {
                arrowUI.gameObject.SetActive(false);
                return;
            }

            arrowUI.gameObject.SetActive(true);

            Vector3 dir = currentTarget.position - player.position;
            dir.y = 0f;

            float angle = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;

            arrowUI.rotation = Quaternion.Euler(0, 0, -angle + player.eulerAngles.y);
    }
}