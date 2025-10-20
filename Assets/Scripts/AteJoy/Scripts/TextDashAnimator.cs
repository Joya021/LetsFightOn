using UnityEngine;
using TMPro;
using System;
using System.Collections;

public class TextDashAnimator : MonoBehaviour
{
    public RectTransform textTransform;
    public CanvasGroup canvasGroup;
    public Vector2 offscreenPos = new Vector2(-1000, 0); // Start offscreen
    public Vector2 onscreenPos = new Vector2(0, 0);      // Final position
    public float duration = 0.5f;

    private Action onAnimationComplete; // Callback for animation end

    void Start()
    {
        textTransform.anchoredPosition = offscreenPos;
        canvasGroup.alpha = 0;
        // You can optionally start with dash in if needed
        // DashIn(() => { /* optional callback after dash in */ });
    }

    // Modified DashIn to be coroutine with callback
    public IEnumerator DashIn(Action onComplete = null)
    {
        StopAllCoroutines();
        onAnimationComplete = onComplete;
        yield return StartCoroutine(Dash(textTransform.anchoredPosition, onscreenPos, 0, 1));
        // Invoke callback after animation completes
        onAnimationComplete?.Invoke();
    }

    // Modified DashOut to be coroutine
    public IEnumerator DashOut()
    {
        StopAllCoroutines();
        yield return StartCoroutine(Dash(textTransform.anchoredPosition, offscreenPos, 1, 0));
    }

    // Helper method for dash animation
    IEnumerator Dash(Vector2 from, Vector2 to, float alphaFrom, float alphaTo)
    {
        float elapsed = 0;
        while (elapsed < duration)
        {
            float t = elapsed / duration;
            textTransform.anchoredPosition = Vector2.Lerp(from, to, t);
            canvasGroup.alpha = Mathf.Lerp(alphaFrom, alphaTo, t);
            elapsed += Time.deltaTime;
            yield return null;
        }
        textTransform.anchoredPosition = to;
        canvasGroup.alpha = alphaTo;
    }

    // Delayed DashOut
    public void DashOutWithDelay(float delaySeconds)
    {
        StartCoroutine(DelayedDashOut(delaySeconds));
    }

    IEnumerator DelayedDashOut(float delay)
    {
        yield return new WaitForSeconds(delay);
        yield return StartCoroutine(DashOut());
    }

    // Delayed DashIn
    public void DashInWithDelay(float delaySeconds, Action onComplete = null)
    {
        StartCoroutine(DelayedDashIn(delaySeconds, onComplete));
    }

    IEnumerator DelayedDashIn(float delay, Action onComplete = null)
    {
        yield return new WaitForSeconds(delay);
        yield return StartCoroutine(DashIn(onComplete));
    }
}