using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class PanelAnimator : MonoBehaviour
{
    [Header("Animation Settings")]
    [Tooltip("Choose the animation effect when panel shows")]
    public AnimationType animationType = AnimationType.FadeIn;

    [Header("Timing")]
    [Range(0.1f, 3f)]
    [Tooltip("Duration of the animation in seconds")]
    public float animationDuration = 0.5f;

    [Range(0f, 2f)]
    [Tooltip("Delay before animation starts")]
    public float startDelay = 0f;

    [Header("Auto Play")]
    [Tooltip("Play animation automatically when enabled")]
    public bool playOnEnable = true;

    [Header("Looping Animation")]
    [Tooltip("Enable smooth looping idle animation after entry")]
    public bool enableLoopAnimation = false;

    [Tooltip("Type of loop animation to play")]
    public LoopAnimationType loopType = LoopAnimationType.FloatGentle;

    [Range(0.5f, 5f)]
    [Tooltip("Speed of the loop animation")]
    public float loopSpeed = 1f;

    [Range(0f, 1f)]
    [Tooltip("Intensity of the loop effect")]
    public float loopIntensity = 0.5f;

    [Header("Audio (Optional)")]
    [Tooltip("Sound effect to play when animation starts")]
    public AudioClip animationSound;

    private CanvasGroup canvasGroup;
    private RectTransform rectTransform;
    private Image panelImage;
    private Vector3 originalScale;
    private Vector2 originalPosition;
    private Quaternion originalRotation;
    private Color originalColor;
    private bool isAnimating = false;
    private bool isLooping = false;
    private float loopTime = 0f;

    // Animation types enum
    public enum AnimationType
    {
        FadeIn,
        FadeInScale,
        SlideFromLeft,
        SlideFromRight,
        SlideFromTop,
        SlideFromBottom,
        ScalePopIn,
        ScaleBounce,
        RotateIn,
        GlitchFade,
        GlitchSlide,
        WaveDistortion,
        PixelDissolve,
        ElasticBounce,
        ShakeEntry,
        SpiralIn,
        FlipHorizontal,
        FlipVertical,
        ZoomBlur,
        TypewriterReveal,
        // NEW GLITCH EFFECTS
        GlitchScanlines,
        GlitchRGBSplit,
        GlitchDataCorruption,
        GlitchHologram,
        GlitchMatrixRain,
        GlitchVHSDistort,
        GlitchDigitalNoise,
        GlitchFragmentation
    }

    public enum LoopAnimationType
    {
        FloatGentle,
        FloatWave,
        BreathePulse,
        RotateIdle,
        BobAndWeave,
        GlowPulse,
        ScaleHeartbeat,
        Figure8Motion,
        PendulumSwing,
        OrbitCircle,
        ShimmerWave,
        MagneticPull
    }

    void Awake()
    {
        // Get or add CanvasGroup
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        rectTransform = GetComponent<RectTransform>();
        panelImage = GetComponent<Image>();

        // Store original values
        originalScale = rectTransform.localScale;
        originalPosition = rectTransform.anchoredPosition;
        originalRotation = rectTransform.localRotation;

        if (panelImage != null)
        {
            originalColor = panelImage.color;
        }
    }

    void OnEnable()
    {
        if (playOnEnable)
        {
            PlayAnimation();
        }
    }

    void OnDisable()
    {
        isLooping = false;
    }

    void Update()
    {
        if (isLooping && enableLoopAnimation)
        {
            loopTime += Time.deltaTime * loopSpeed;
            ApplyLoopAnimation();
        }
    }

    public void PlayAnimation()
    {
        if (isAnimating) return;

        StopAllCoroutines();
        StartCoroutine(AnimatePanel());
    }

    public void ResetPanel()
    {
        StopAllCoroutines();
        isAnimating = false;
        isLooping = false;
        loopTime = 0f;

        canvasGroup.alpha = 1f;
        rectTransform.localScale = originalScale;
        rectTransform.anchoredPosition = originalPosition;
        rectTransform.localRotation = originalRotation;

        if (panelImage != null)
        {
            panelImage.color = originalColor;
        }
    }

    private IEnumerator AnimatePanel()
    {
        isAnimating = true;
        isLooping = false;

        // Play sound if assigned
        if (animationSound != null && AudioManager.Instance != null)
        {
            AudioManager.Instance.sfxSource.PlayOneShot(animationSound);
        }

        // Wait for start delay
        if (startDelay > 0f)
        {
            yield return new WaitForSeconds(startDelay);
        }

        // Execute animation based on type
        switch (animationType)
        {
            case AnimationType.FadeIn:
                yield return FadeIn();
                break;
            case AnimationType.FadeInScale:
                yield return FadeInScale();
                break;
            case AnimationType.SlideFromLeft:
                yield return SlideFrom(Vector2.left);
                break;
            case AnimationType.SlideFromRight:
                yield return SlideFrom(Vector2.right);
                break;
            case AnimationType.SlideFromTop:
                yield return SlideFrom(Vector2.up);
                break;
            case AnimationType.SlideFromBottom:
                yield return SlideFrom(Vector2.down);
                break;
            case AnimationType.ScalePopIn:
                yield return ScalePopIn();
                break;
            case AnimationType.ScaleBounce:
                yield return ScaleBounce();
                break;
            case AnimationType.RotateIn:
                yield return RotateIn();
                break;
            case AnimationType.GlitchFade:
                yield return GlitchFade();
                break;
            case AnimationType.GlitchSlide:
                yield return GlitchSlide();
                break;
            case AnimationType.WaveDistortion:
                yield return WaveDistortion();
                break;
            case AnimationType.PixelDissolve:
                yield return PixelDissolve();
                break;
            case AnimationType.ElasticBounce:
                yield return ElasticBounce();
                break;
            case AnimationType.ShakeEntry:
                yield return ShakeEntry();
                break;
            case AnimationType.SpiralIn:
                yield return SpiralIn();
                break;
            case AnimationType.FlipHorizontal:
                yield return FlipHorizontal();
                break;
            case AnimationType.FlipVertical:
                yield return FlipVertical();
                break;
            case AnimationType.ZoomBlur:
                yield return ZoomBlur();
                break;
            case AnimationType.TypewriterReveal:
                yield return TypewriterReveal();
                break;
            // NEW GLITCH EFFECTS
            case AnimationType.GlitchScanlines:
                yield return GlitchScanlines();
                break;
            case AnimationType.GlitchRGBSplit:
                yield return GlitchRGBSplit();
                break;
            case AnimationType.GlitchDataCorruption:
                yield return GlitchDataCorruption();
                break;
            case AnimationType.GlitchHologram:
                yield return GlitchHologram();
                break;
            case AnimationType.GlitchMatrixRain:
                yield return GlitchMatrixRain();
                break;
            case AnimationType.GlitchVHSDistort:
                yield return GlitchVHSDistort();
                break;
            case AnimationType.GlitchDigitalNoise:
                yield return GlitchDigitalNoise();
                break;
            case AnimationType.GlitchFragmentation:
                yield return GlitchFragmentation();
                break;
        }

        isAnimating = false;

        // Start loop animation if enabled
        if (enableLoopAnimation)
        {
            isLooping = true;
            loopTime = 0f;
        }
    }

    // ==================== LOOP ANIMATIONS ====================

    private void ApplyLoopAnimation()
    {
        float intensity = loopIntensity;

        switch (loopType)
        {
            case LoopAnimationType.FloatGentle:
                FloatGentle(intensity);
                break;
            case LoopAnimationType.FloatWave:
                FloatWave(intensity);
                break;
            case LoopAnimationType.BreathePulse:
                BreathePulse(intensity);
                break;
            case LoopAnimationType.RotateIdle:
                RotateIdle(intensity);
                break;
            case LoopAnimationType.BobAndWeave:
                BobAndWeave(intensity);
                break;
            case LoopAnimationType.GlowPulse:
                GlowPulse(intensity);
                break;
            case LoopAnimationType.ScaleHeartbeat:
                ScaleHeartbeat(intensity);
                break;
            case LoopAnimationType.Figure8Motion:
                Figure8Motion(intensity);
                break;
            case LoopAnimationType.PendulumSwing:
                PendulumSwing(intensity);
                break;
            case LoopAnimationType.OrbitCircle:
                OrbitCircle(intensity);
                break;
            case LoopAnimationType.ShimmerWave:
                ShimmerWave(intensity);
                break;
            case LoopAnimationType.MagneticPull:
                MagneticPull(intensity);
                break;
        }
    }

    private void FloatGentle(float intensity)
    {
        float y = Mathf.Sin(loopTime) * 15f * intensity;
        rectTransform.anchoredPosition = originalPosition + new Vector2(0f, y);
    }

    private void FloatWave(float intensity)
    {
        float x = Mathf.Sin(loopTime) * 10f * intensity;
        float y = Mathf.Cos(loopTime * 1.3f) * 12f * intensity;
        rectTransform.anchoredPosition = originalPosition + new Vector2(x, y);
    }

    private void BreathePulse(float intensity)
    {
        float scale = 1f + Mathf.Sin(loopTime * 0.8f) * 0.05f * intensity;
        rectTransform.localScale = originalScale * scale;
    }

    private void RotateIdle(float intensity)
    {
        float angle = Mathf.Sin(loopTime * 0.5f) * 3f * intensity;
        rectTransform.localRotation = Quaternion.Euler(0f, 0f, angle);
    }

    private void BobAndWeave(float intensity)
    {
        float x = Mathf.Sin(loopTime * 1.5f) * 8f * intensity;
        float y = Mathf.Abs(Mathf.Sin(loopTime)) * 12f * intensity;
        rectTransform.anchoredPosition = originalPosition + new Vector2(x, y);
    }

    private void GlowPulse(float intensity)
    {
        if (panelImage != null)
        {
            float alpha = Mathf.Lerp(0.7f, 1f, (Mathf.Sin(loopTime * 2f) + 1f) * 0.5f);
            Color c = originalColor;
            c.a = Mathf.Lerp(originalColor.a, alpha, intensity);
            panelImage.color = c;
        }
    }

    private void ScaleHeartbeat(float intensity)
    {
        float beat = Mathf.Abs(Mathf.Sin(loopTime * 3f));
        float scale = 1f + beat * 0.08f * intensity;
        rectTransform.localScale = originalScale * scale;
    }

    private void Figure8Motion(float intensity)
    {
        float x = Mathf.Sin(loopTime) * 20f * intensity;
        float y = Mathf.Sin(loopTime * 2f) * 15f * intensity;
        rectTransform.anchoredPosition = originalPosition + new Vector2(x, y);
    }

    private void PendulumSwing(float intensity)
    {
        float angle = Mathf.Sin(loopTime) * 5f * intensity;
        rectTransform.localRotation = Quaternion.Euler(0f, 0f, angle);

        float x = Mathf.Sin(loopTime) * 10f * intensity;
        rectTransform.anchoredPosition = originalPosition + new Vector2(x, 0f);
    }

    private void OrbitCircle(float intensity)
    {
        float radius = 15f * intensity;
        float x = Mathf.Cos(loopTime) * radius;
        float y = Mathf.Sin(loopTime) * radius;
        rectTransform.anchoredPosition = originalPosition + new Vector2(x, y);
    }

    private void ShimmerWave(float intensity)
    {
        if (panelImage != null)
        {
            float shimmer = (Mathf.Sin(loopTime * 4f) + 1f) * 0.5f;
            Color c = originalColor;
            c.r = Mathf.Lerp(originalColor.r, 1f, shimmer * 0.2f * intensity);
            c.g = Mathf.Lerp(originalColor.g, 1f, shimmer * 0.2f * intensity);
            c.b = Mathf.Lerp(originalColor.b, 1f, shimmer * 0.2f * intensity);
            panelImage.color = c;
        }
    }

    private void MagneticPull(float intensity)
    {
        float pull = Mathf.Sin(loopTime * 1.2f);
        float scale = 1f + pull * 0.03f * intensity;
        rectTransform.localScale = originalScale * scale;

        float y = pull * 8f * intensity;
        rectTransform.anchoredPosition = originalPosition + new Vector2(0f, y);
    }

    // ==================== ORIGINAL ANIMATION METHODS ====================

    private IEnumerator FadeIn()
    {
        canvasGroup.alpha = 0f;
        float elapsed = 0f;

        while (elapsed < animationDuration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / animationDuration);
            yield return null;
        }

        canvasGroup.alpha = 1f;
    }

    private IEnumerator FadeInScale()
    {
        canvasGroup.alpha = 0f;
        rectTransform.localScale = originalScale * 0.5f;
        float elapsed = 0f;

        while (elapsed < animationDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / animationDuration;
            canvasGroup.alpha = Mathf.Lerp(0f, 1f, t);
            rectTransform.localScale = Vector3.Lerp(originalScale * 0.5f, originalScale, t);
            yield return null;
        }

        canvasGroup.alpha = 1f;
        rectTransform.localScale = originalScale;
    }

    private IEnumerator SlideFrom(Vector2 direction)
    {
        Vector2 startPos = originalPosition + direction * 2000f;
        rectTransform.anchoredPosition = startPos;
        float elapsed = 0f;

        while (elapsed < animationDuration)
        {
            elapsed += Time.deltaTime;
            float t = EaseOutCubic(elapsed / animationDuration);
            rectTransform.anchoredPosition = Vector2.Lerp(startPos, originalPosition, t);
            yield return null;
        }

        rectTransform.anchoredPosition = originalPosition;
    }

    private IEnumerator ScalePopIn()
    {
        rectTransform.localScale = Vector3.zero;
        float elapsed = 0f;

        while (elapsed < animationDuration)
        {
            elapsed += Time.deltaTime;
            float t = EaseOutBack(elapsed / animationDuration);
            rectTransform.localScale = Vector3.Lerp(Vector3.zero, originalScale, t);
            yield return null;
        }

        rectTransform.localScale = originalScale;
    }

    private IEnumerator ScaleBounce()
    {
        rectTransform.localScale = Vector3.zero;
        float elapsed = 0f;

        while (elapsed < animationDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / animationDuration;
            float bounce = Mathf.Sin(t * Mathf.PI * 2f) * 0.2f * (1f - t);
            float scale = EaseOutElastic(t);
            rectTransform.localScale = originalScale * (scale + bounce);
            yield return null;
        }

        rectTransform.localScale = originalScale;
    }

    private IEnumerator RotateIn()
    {
        canvasGroup.alpha = 0f;
        rectTransform.localRotation = Quaternion.Euler(0f, 0f, 180f);
        rectTransform.localScale = originalScale * 0.3f;
        float elapsed = 0f;

        while (elapsed < animationDuration)
        {
            elapsed += Time.deltaTime;
            float t = EaseOutCubic(elapsed / animationDuration);
            canvasGroup.alpha = t;
            rectTransform.localRotation = Quaternion.Lerp(Quaternion.Euler(0f, 0f, 180f), originalRotation, t);
            rectTransform.localScale = Vector3.Lerp(originalScale * 0.3f, originalScale, t);
            yield return null;
        }

        canvasGroup.alpha = 1f;
        rectTransform.localRotation = originalRotation;
        rectTransform.localScale = originalScale;
    }

    private IEnumerator GlitchFade()
    {
        canvasGroup.alpha = 0f;
        float elapsed = 0f;

        while (elapsed < animationDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / animationDuration;

            // Glitch effect with random position offsets
            if (Random.value > 0.7f)
            {
                rectTransform.anchoredPosition = originalPosition + new Vector2(
                    Random.Range(-20f, 20f),
                    Random.Range(-20f, 20f)
                );
            }
            else
            {
                rectTransform.anchoredPosition = originalPosition;
            }

            // Fade in with glitchy opacity
            float glitchAlpha = Random.value > 0.8f ? Random.Range(0.3f, 1f) : Mathf.Lerp(0f, 1f, t);
            canvasGroup.alpha = glitchAlpha;

            yield return new WaitForSeconds(0.05f);
        }

        rectTransform.anchoredPosition = originalPosition;
        canvasGroup.alpha = 1f;
    }

    private IEnumerator GlitchSlide()
    {
        Vector2 startPos = originalPosition + Vector2.right * 2000f;
        rectTransform.anchoredPosition = startPos;
        float elapsed = 0f;

        while (elapsed < animationDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / animationDuration;

            Vector2 targetPos = Vector2.Lerp(startPos, originalPosition, t);

            // Add glitch jumps
            if (Random.value > 0.7f)
            {
                targetPos += new Vector2(Random.Range(-50f, 50f), Random.Range(-30f, 30f));
            }

            rectTransform.anchoredPosition = targetPos;

            // Glitch scale
            if (Random.value > 0.8f)
            {
                rectTransform.localScale = originalScale * Random.Range(0.9f, 1.1f);
            }

            yield return new WaitForSeconds(0.03f);
        }

        rectTransform.anchoredPosition = originalPosition;
        rectTransform.localScale = originalScale;
    }

    private IEnumerator WaveDistortion()
    {
        canvasGroup.alpha = 0f;
        float elapsed = 0f;
        Vector2 startPos = originalPosition;

        while (elapsed < animationDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / animationDuration;

            // Wave motion
            float wave = Mathf.Sin(t * Mathf.PI * 8f) * 50f * (1f - t);
            rectTransform.anchoredPosition = startPos + Vector2.right * wave;

            // Fade in
            canvasGroup.alpha = Mathf.Lerp(0f, 1f, t);

            // Scale pulse
            float scale = 1f + Mathf.Sin(t * Mathf.PI * 4f) * 0.1f * (1f - t);
            rectTransform.localScale = originalScale * scale;

            yield return null;
        }

        rectTransform.anchoredPosition = originalPosition;
        rectTransform.localScale = originalScale;
        canvasGroup.alpha = 1f;
    }

    private IEnumerator PixelDissolve()
    {
        if (panelImage == null) yield break;

        canvasGroup.alpha = 1f;
        float elapsed = 0f;

        while (elapsed < animationDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / animationDuration;

            // Simulate pixelation by scaling
            float pixelScale = Mathf.Lerp(0.1f, 1f, t);
            rectTransform.localScale = originalScale * pixelScale;

            // Color shift for pixel effect
            if (Random.value > 0.9f)
            {
                panelImage.color = new Color(
                    originalColor.r * Random.Range(0.8f, 1.2f),
                    originalColor.g * Random.Range(0.8f, 1.2f),
                    originalColor.b * Random.Range(0.8f, 1.2f),
                    originalColor.a
                );
            }
            else
            {
                panelImage.color = originalColor;
            }

            yield return null;
        }

        rectTransform.localScale = originalScale;
        panelImage.color = originalColor;
    }

    private IEnumerator ElasticBounce()
    {
        rectTransform.localScale = Vector3.zero;
        float elapsed = 0f;

        while (elapsed < animationDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / animationDuration;
            float elasticScale = EaseOutElastic(t);
            rectTransform.localScale = originalScale * elasticScale;
            yield return null;
        }

        rectTransform.localScale = originalScale;
    }

    private IEnumerator ShakeEntry()
    {
        canvasGroup.alpha = 0f;
        rectTransform.localScale = originalScale * 1.5f;
        float elapsed = 0f;

        while (elapsed < animationDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / animationDuration;

            // Shake
            float shakeAmount = 30f * (1f - t);
            rectTransform.anchoredPosition = originalPosition + new Vector2(
                Random.Range(-shakeAmount, shakeAmount),
                Random.Range(-shakeAmount, shakeAmount)
            );

            // Scale down
            rectTransform.localScale = Vector3.Lerp(originalScale * 1.5f, originalScale, t);

            // Fade in
            canvasGroup.alpha = t;

            yield return null;
        }

        rectTransform.anchoredPosition = originalPosition;
        rectTransform.localScale = originalScale;
        canvasGroup.alpha = 1f;
    }

    private IEnumerator SpiralIn()
    {
        canvasGroup.alpha = 0f;
        rectTransform.localScale = Vector3.zero;
        float elapsed = 0f;

        while (elapsed < animationDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / animationDuration;

            // Spiral rotation
            float angle = (1f - t) * 720f;
            rectTransform.localRotation = Quaternion.Euler(0f, 0f, angle);

            // Scale up
            rectTransform.localScale = Vector3.Lerp(Vector3.zero, originalScale, EaseOutCubic(t));

            // Fade in
            canvasGroup.alpha = t;

            yield return null;
        }

        rectTransform.localRotation = originalRotation;
        rectTransform.localScale = originalScale;
        canvasGroup.alpha = 1f;
    }

    private IEnumerator FlipHorizontal()
    {
        canvasGroup.alpha = 0f;
        float elapsed = 0f;

        while (elapsed < animationDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / animationDuration;

            // Flip effect using scale
            float scaleX = Mathf.Lerp(0f, 1f, t);
            rectTransform.localScale = new Vector3(scaleX, originalScale.y, originalScale.z);

            // Fade in
            canvasGroup.alpha = t;

            yield return null;
        }

        rectTransform.localScale = originalScale;
        canvasGroup.alpha = 1f;
    }

    private IEnumerator FlipVertical()
    {
        canvasGroup.alpha = 0f;
        float elapsed = 0f;

        while (elapsed < animationDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / animationDuration;

            // Flip effect using scale
            float scaleY = Mathf.Lerp(0f, 1f, t);
            rectTransform.localScale = new Vector3(originalScale.x, scaleY, originalScale.z);

            // Fade in
            canvasGroup.alpha = t;

            yield return null;
        }

        rectTransform.localScale = originalScale;
        canvasGroup.alpha = 1f;
    }

    private IEnumerator ZoomBlur()
    {
        canvasGroup.alpha = 0f;
        rectTransform.localScale = originalScale * 3f;
        float elapsed = 0f;

        while (elapsed < animationDuration)
        {
            elapsed += Time.deltaTime;
            float t = EaseOutCubic(elapsed / animationDuration);

            // Zoom in effect
            rectTransform.localScale = Vector3.Lerp(originalScale * 3f, originalScale, t);

            // Fade in
            canvasGroup.alpha = t;

            // Rotation for extra effect
            float rotation = (1f - t) * 45f;
            rectTransform.localRotation = Quaternion.Euler(0f, 0f, rotation);

            yield return null;
        }

        rectTransform.localScale = originalScale;
        rectTransform.localRotation = originalRotation;
        canvasGroup.alpha = 1f;
    }

    private IEnumerator TypewriterReveal()
    {
        canvasGroup.alpha = 1f;
        float elapsed = 0f;
        Vector2 startPos = originalPosition + Vector2.down * 100f;
        rectTransform.anchoredPosition = startPos;

        while (elapsed < animationDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / animationDuration;

            // Move up
            rectTransform.anchoredPosition = Vector2.Lerp(startPos, originalPosition, EaseOutCubic(t));

            // Slight shake
            if (Random.value > 0.95f)
            {
                rectTransform.anchoredPosition += new Vector2(Random.Range(-3f, 3f), 0f);
            }

            yield return null;
        }

        rectTransform.anchoredPosition = originalPosition;
    }

    // ==================== NEW GLITCH EFFECTS ====================

    private IEnumerator GlitchScanlines()
    {
        canvasGroup.alpha = 0f;
        float elapsed = 0f;

        while (elapsed < animationDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / animationDuration;

            // Scanline effect - horizontal slices
            float scanLine = Mathf.Repeat(Time.time * 500f, Screen.height);
            float distanceFromScan = Mathf.Abs(rectTransform.anchoredPosition.y - scanLine);

            if (distanceFromScan < 50f)
            {
                float xOffset = Random.Range(-30f, 30f);
                rectTransform.anchoredPosition = originalPosition + new Vector2(xOffset, 0f);
            }
            else
            {
                rectTransform.anchoredPosition = originalPosition;
            }

            // Fade in
            canvasGroup.alpha = Mathf.Lerp(0f, 1f, t);

            // Random brightness flicker
            if (panelImage != null && Random.value > 0.85f)
            {
                panelImage.color = originalColor * Random.Range(0.7f, 1.3f);
            }
            else if (panelImage != null)
            {
                panelImage.color = originalColor;
            }

            yield return null;
        }

        rectTransform.anchoredPosition = originalPosition;
        canvasGroup.alpha = 1f;
        if (panelImage != null) panelImage.color = originalColor;
    }

    private IEnumerator GlitchRGBSplit()
    {
        if (panelImage == null) yield break;

        canvasGroup.alpha = 1f;
        float elapsed = 0f;

        while (elapsed < animationDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / animationDuration;

            // RGB channel separation effect
            float splitAmount = (1f - t) * 40f;

            if (Random.value > 0.7f)
            {
                // Simulate RGB split by shifting position
                float offset = Random.Range(-splitAmount, splitAmount);
                rectTransform.anchoredPosition = originalPosition + new Vector2(offset, 0f);

                // Color shift to simulate channel separation
                float colorShift = Random.Range(0.8f, 1.2f);
                panelImage.color = new Color(
                    originalColor.r * colorShift,
                    originalColor.g * (2f - colorShift),
                    originalColor.b * Random.Range(0.9f, 1.1f),
                    Mathf.Lerp(0f, originalColor.a, t)
                );
            }
            else
            {
                rectTransform.anchoredPosition = originalPosition;
                panelImage.color = new Color(originalColor.r, originalColor.g, originalColor.b, Mathf.Lerp(0f, originalColor.a, t));
            }

            yield return null;
        }

        rectTransform.anchoredPosition = originalPosition;
        panelImage.color = originalColor;
    }

    private IEnumerator GlitchDataCorruption()
    {
        canvasGroup.alpha = 0f;
        float elapsed = 0f;

        while (elapsed < animationDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / animationDuration;

            // Data corruption effect - random jumps and freezes
            if (Random.value > 0.6f)
            {
                // Random teleportation
                Vector2 corruptPos = new Vector2(
                    originalPosition.x + Random.Range(-100f, 100f),
                    originalPosition.y + Random.Range(-100f, 100f)
                );
                rectTransform.anchoredPosition = corruptPos;

                // Random scale corruption
                float corruptScale = Random.Range(0.5f, 1.5f);
                rectTransform.localScale = originalScale * corruptScale;

                // Random rotation glitch
                float corruptRotation = Random.Range(-45f, 45f);
                rectTransform.localRotation = Quaternion.Euler(0f, 0f, corruptRotation);
            }
            else
            {
                // Lerp back to normal
                rectTransform.anchoredPosition = Vector2.Lerp(rectTransform.anchoredPosition, originalPosition, 0.3f);
                rectTransform.localScale = Vector3.Lerp(rectTransform.localScale, originalScale, 0.3f);
                rectTransform.localRotation = Quaternion.Lerp(rectTransform.localRotation, originalRotation, 0.3f);
            }

            // Fade in
            canvasGroup.alpha = Mathf.Lerp(0f, 1f, t);

            yield return new WaitForSeconds(0.02f);
        }

        rectTransform.anchoredPosition = originalPosition;
        rectTransform.localScale = originalScale;
        rectTransform.localRotation = originalRotation;
        canvasGroup.alpha = 1f;
    }

    private IEnumerator GlitchHologram()
    {
        if (panelImage == null) yield break;

        canvasGroup.alpha = 0f;
        float elapsed = 0f;

        while (elapsed < animationDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / animationDuration;

            // Hologram flicker effect
            if (Random.value > 0.5f)
            {
                canvasGroup.alpha = Random.Range(0.3f, 1f) * t;
            }
            else
            {
                canvasGroup.alpha = t;
            }

            // Horizontal scanlines
            float scanSpeed = Time.time * 20f;
            float scanPattern = Mathf.Sin(scanSpeed) * 0.5f + 0.5f;

            // Color tint (cyan/blue hologram)
            panelImage.color = new Color(
                originalColor.r * 0.7f,
                originalColor.g * (0.9f + scanPattern * 0.2f),
                originalColor.b * 1.2f,
                originalColor.a
            );

            // Jitter position
            if (Random.value > 0.8f)
            {
                rectTransform.anchoredPosition = originalPosition + new Vector2(
                    Random.Range(-5f, 5f),
                    Random.Range(-3f, 3f)
                );
            }
            else
            {
                rectTransform.anchoredPosition = originalPosition;
            }

            // Slight scale pulse
            float scalePulse = 1f + Mathf.Sin(Time.time * 30f) * 0.02f;
            rectTransform.localScale = originalScale * scalePulse;

            yield return null;
        }

        rectTransform.anchoredPosition = originalPosition;
        rectTransform.localScale = originalScale;
        canvasGroup.alpha = 1f;
        panelImage.color = originalColor;
    }

    private IEnumerator GlitchMatrixRain()
    {
        if (panelImage == null) yield break;

        canvasGroup.alpha = 0f;
        rectTransform.localScale = originalScale * 0.1f;
        float elapsed = 0f;

        while (elapsed < animationDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / animationDuration;

            // Digital rain effect - rapid vertical movement
            if (Random.value > 0.6f)
            {
                float rainDrop = Random.Range(-50f, 50f);
                rectTransform.anchoredPosition = originalPosition + new Vector2(0f, rainDrop);
            }
            else
            {
                rectTransform.anchoredPosition = Vector2.Lerp(rectTransform.anchoredPosition, originalPosition, 0.4f);
            }

            // Green digital color
            panelImage.color = new Color(
                originalColor.r * 0.3f,
                originalColor.g * Random.Range(0.8f, 1.5f),
                originalColor.b * 0.3f,
                originalColor.a
            );

            // Scale up
            rectTransform.localScale = Vector3.Lerp(originalScale * 0.1f, originalScale, EaseOutCubic(t));

            // Fade in with flicker
            canvasGroup.alpha = Random.value > 0.7f ? Random.Range(0.5f, 1f) * t : t;

            yield return new WaitForSeconds(0.02f);
        }

        rectTransform.anchoredPosition = originalPosition;
        rectTransform.localScale = originalScale;
        canvasGroup.alpha = 1f;
        panelImage.color = originalColor;
    }

    private IEnumerator GlitchVHSDistort()
    {
        float elapsed = 0f;
        canvasGroup.alpha = 0f;

        while (elapsed < animationDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / animationDuration;

            // VHS tracking error - horizontal stretching
            if (Random.value > 0.7f)
            {
                float stretchX = Random.Range(0.8f, 1.3f);
                rectTransform.localScale = new Vector3(
                    originalScale.x * stretchX,
                    originalScale.y,
                    originalScale.z
                );

                // Horizontal offset
                float trackingError = Random.Range(-80f, 80f);
                rectTransform.anchoredPosition = originalPosition + new Vector2(trackingError, 0f);
            }
            else
            {
                rectTransform.localScale = originalScale;
                rectTransform.anchoredPosition = originalPosition;
            }

            // VHS color bleed
            if (panelImage != null && Random.value > 0.8f)
            {
                panelImage.color = new Color(
                    originalColor.r * Random.Range(0.9f, 1.2f),
                    originalColor.g * Random.Range(0.8f, 1.1f),
                    originalColor.b * Random.Range(0.7f, 1.3f),
                    originalColor.a
                );
            }
            else if (panelImage != null)
            {
                panelImage.color = originalColor;
            }

            // Fade in
            canvasGroup.alpha = Mathf.Lerp(0f, 1f, t);

            yield return new WaitForSeconds(0.04f);
        }

        rectTransform.anchoredPosition = originalPosition;
        rectTransform.localScale = originalScale;
        canvasGroup.alpha = 1f;
        if (panelImage != null) panelImage.color = originalColor;
    }

    private IEnumerator GlitchDigitalNoise()
    {
        canvasGroup.alpha = 0f;
        float elapsed = 0f;

        while (elapsed < animationDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / animationDuration;

            // Static noise position
            rectTransform.anchoredPosition = originalPosition + new Vector2(
                Random.Range(-15f, 15f) * (1f - t),
                Random.Range(-15f, 15f) * (1f - t)
            );

            // Noise scale jitter
            float noiseScale = 1f + Random.Range(-0.1f, 0.1f) * (1f - t);
            rectTransform.localScale = originalScale * noiseScale;

            // Digital noise alpha flicker
            float noiseAlpha = Random.Range(0.5f, 1f);
            canvasGroup.alpha = noiseAlpha * t;

            // Color noise
            if (panelImage != null)
            {
                panelImage.color = new Color(
                    originalColor.r * Random.Range(0.9f, 1.1f),
                    originalColor.g * Random.Range(0.9f, 1.1f),
                    originalColor.b * Random.Range(0.9f, 1.1f),
                    originalColor.a
                );
            }

            yield return null;
        }

        rectTransform.anchoredPosition = originalPosition;
        rectTransform.localScale = originalScale;
        canvasGroup.alpha = 1f;
        if (panelImage != null) panelImage.color = originalColor;
    }

    private IEnumerator GlitchFragmentation()
    {
        canvasGroup.alpha = 0f;
        float elapsed = 0f;
        int fragments = 8;

        while (elapsed < animationDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / animationDuration;

            // Fragment explosion effect
            if (Random.value > 0.6f)
            {
                // Simulate fragments by rapid position changes
                float fragmentAngle = Random.Range(0f, 360f);
                float fragmentDistance = Random.Range(20f, 100f) * (1f - t);

                Vector2 fragmentOffset = new Vector2(
                    Mathf.Cos(fragmentAngle * Mathf.Deg2Rad) * fragmentDistance,
                    Mathf.Sin(fragmentAngle * Mathf.Deg2Rad) * fragmentDistance
                );

                rectTransform.anchoredPosition = originalPosition + fragmentOffset;

                // Fragment rotation
                float fragmentRotation = Random.Range(-180f, 180f) * (1f - t);
                rectTransform.localRotation = Quaternion.Euler(0f, 0f, fragmentRotation);

                // Fragment scale
                float fragmentScale = Random.Range(0.3f, 1.2f);
                rectTransform.localScale = originalScale * fragmentScale * Mathf.Lerp(0.5f, 1f, t);
            }
            else
            {
                // Quickly snap back to position
                rectTransform.anchoredPosition = Vector2.Lerp(rectTransform.anchoredPosition, originalPosition, 0.5f);
                rectTransform.localRotation = Quaternion.Lerp(rectTransform.localRotation, originalRotation, 0.5f);
                rectTransform.localScale = Vector3.Lerp(rectTransform.localScale, originalScale, 0.5f);
            }

            // Fade in
            canvasGroup.alpha = t;

            // Color distortion
            if (panelImage != null && Random.value > 0.7f)
            {
                panelImage.color = new Color(
                    Random.Range(0.5f, 1f),
                    Random.Range(0.5f, 1f),
                    Random.Range(0.5f, 1f),
                    originalColor.a
                );
            }
            else if (panelImage != null)
            {
                panelImage.color = originalColor;
            }

            yield return new WaitForSeconds(0.02f);
        }

        rectTransform.anchoredPosition = originalPosition;
        rectTransform.localRotation = originalRotation;
        rectTransform.localScale = originalScale;
        canvasGroup.alpha = 1f;
        if (panelImage != null) panelImage.color = originalColor;
    }

    // ==================== EASING FUNCTIONS ====================

    private float EaseOutCubic(float t)
    {
        return 1f - Mathf.Pow(1f - t, 3f);
    }

    private float EaseOutBack(float t)
    {
        float c1 = 1.70158f;
        float c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
    }

    private float EaseOutElastic(float t)
    {
        float c4 = (2f * Mathf.PI) / 3f;
        return t == 0f ? 0f : t == 1f ? 1f : Mathf.Pow(2f, -10f * t) * Mathf.Sin((t * 10f - 0.75f) * c4) + 1f;
    }
}