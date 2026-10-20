using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// Provides temporary visual feedback when a DataValue
/// is attempted to be placed onto a destination.
///
/// GREEN = DataValueType AND SemanticKey are correct
/// RED   = DataValueType OR SemanticKey is incorrect
///
/// This component ONLY handles visual feedback.
/// Actual value validation and transfer remain handled by
/// CaseFolderFieldRow / ValueTransferManager.
/// </summary>
public class ValueTypeValidationFeedback : MonoBehaviour
{
    // =========================================================
    // EXPECTED DATA
    // =========================================================

    [Header("Expected Data")]

    [SerializeField]
    private DataValueType expectedType;

    [SerializeField]
    private string expectedKey;


    // =========================================================
    // FEEDBACK COLORS
    // =========================================================

    [Header("Feedback Colors")]

    [Tooltip("Accepted drop (right kind of value): soft mint.")]
    [SerializeField]
    private Color successColor = new Color(0.33f, 0.76f, 0.5f, 1f);
    [Tooltip("Rejected drop (wrong kind of value): soft coral.")]
    [SerializeField]
    private Color errorColor = new Color(0.92f, 0.37f, 0.35f, 1f);

    [Tooltip("How strongly the field fill is tinted at the peak of the pulse.")]
    [SerializeField, Range(0f, 1f)]
    private float tintStrength = 0.7f;


    // =========================================================
    // FEEDBACK DURATION
    // =========================================================

    [Header("Feedback Duration")]

    [SerializeField]
    private float feedbackDuration = 0.5f;


    // =========================================================
    // VISUAL TARGETS
    // =========================================================

    [Header("Visual Targets")]

    [Tooltip(
        "Images that should flash green/red. " +
        "If empty, all Images under this GameObject are detected automatically."
    )]
    [SerializeField]
    private Image[] targetImages;


    // =========================================================
    // INTERNAL DATA
    // =========================================================

    private Color[] originalColors;

    private Coroutine feedbackCoroutine;

    public bool IsPlaying => feedbackCoroutine != null;


    // =========================================================
    // AWAKE
    // =========================================================

    private void Awake()
    {
        SetupVisualTargets();
        ValueHoverFeedback.Ensure(this);
    }

    /// <summary>
    /// Updates the colour an image returns to after a flash (e.g. after the
    /// row was restyled). Does not repaint the image.
    /// </summary>
    public void SetRestingColor(Image image, Color color)
    {
        if (image == null || targetImages == null || originalColors == null)
            return;

        for (int i = 0; i < targetImages.Length && i < originalColors.Length; i++)
        {
            if (targetImages[i] == image)
                originalColors[i] = color;
        }
    }


    // =========================================================
    // SETUP VISUAL TARGETS
    // =========================================================

    private void SetupVisualTargets()
    {
        // -----------------------------------------------------
        // Find Images
        // -----------------------------------------------------

        if (targetImages == null ||
            targetImages.Length == 0)
        {
            targetImages =
                System.Array.FindAll(
                    GetComponentsInChildren<Image>(true),
                    IsFeedbackTarget);
        }

        // -----------------------------------------------------
        // Validate
        // -----------------------------------------------------

        if (targetImages == null ||
            targetImages.Length == 0)
        {
            Debug.LogWarning(
                $"[Type Feedback] NO UI Images found under " +
                $"{gameObject.name}. " +
                $"Green/red feedback cannot be displayed."
            );

            originalColors = new Color[0];

            return;
        }

        // -----------------------------------------------------
        // Store original colors
        // -----------------------------------------------------

        originalColors =
            new Color[targetImages.Length];

        for (int i = 0;
             i < targetImages.Length;
             i++)
        {
            Image image =
                targetImages[i];

            if (image == null)
                continue;

            originalColors[i] =
                image.color;
        }

        DebugTrace.Log(
            $"[Type Feedback] {gameObject.name} found " +
            $"{targetImages.Length} UI Image target(s)."
        );

        // -----------------------------------------------------
        // Print targets
        // -----------------------------------------------------

        for (int i = 0;
             i < targetImages.Length;
             i++)
        {
            if (targetImages[i] != null)
            {
                DebugTrace.Log(
                    $"[Type Feedback] Target {i}: " +
                    $"{targetImages[i].gameObject.name} | " +
                    $"Original Color: " +
                    $"{targetImages[i].color}"
                );
            }
        }
    }


    // =========================================================
    // INITIALIZE
    // =========================================================

    /// <summary>
    /// Sets the expected DataValueType and SemanticKey.
    ///
    /// Both must match for GREEN feedback.
    /// </summary>
    public void Initialize(
        DataValueType expectedDataType,
        string expectedDataKey)
    {
        ForceRestore();

        expectedType =
            expectedDataType;

        expectedKey =
            expectedDataKey;

        // -----------------------------------------------------
        // Rebuild visual targets if necessary
        // -----------------------------------------------------

        if (targetImages == null ||
            targetImages.Length == 0)
        {
            SetupVisualTargets();
        }

        DebugTrace.Log(
            $"[Type Feedback] INITIALIZED | " +
            $"Object={gameObject.name} | " +
            $"Expected Type={expectedType} | " +
            $"Expected Key={expectedKey} | " +
            $"Images={GetTargetCount()}"
        );
    }


    // =========================================================
    // EXPECTED TYPE
    // =========================================================

    public DataValueType ExpectedType
    {
        get
        {
            return expectedType;
        }
    }


    // =========================================================
    // EXPECTED KEY
    // =========================================================

    public string ExpectedKey
    {
        get
        {
            return expectedKey;
        }
    }


    // =========================================================
    // CHECK COMPLETE VALUE
    // =========================================================

    /// <summary>
    /// Checks:
    ///
    /// 1. DataValueType
    /// 2. SemanticKey
    ///
    /// Both must match.
    /// </summary>
    public bool IsCorrectValue(
        DataValue value)
    {
        if (value == null)
        {
            Debug.LogWarning(
                $"[Type Feedback] " +
                $"{gameObject.name}: Incoming DataValue is NULL."
            );

            return false;
        }

        // -----------------------------------------------------
        // Type check
        // -----------------------------------------------------

        bool typeMatches =
            value.Type == expectedType;

        // -----------------------------------------------------
        // Semantic key check
        // -----------------------------------------------------

        string incomingKey =
            value.SemanticKey == null
                ? string.Empty
                : value.SemanticKey.Trim();

        string destinationKey =
            expectedKey == null
                ? string.Empty
                : expectedKey.Trim();

        bool keyMatches =
            string.Equals(
                incomingKey,
                destinationKey,
                System.StringComparison.OrdinalIgnoreCase
            );

        // -----------------------------------------------------
        // Debug
        // -----------------------------------------------------

        DebugTrace.Log(
            $"[Type Feedback CHECK] " +
            $"Destination={gameObject.name} | " +
            $"Type: {value.Type} == {expectedType} -> {typeMatches} | " +
            $"Key: '{incomingKey}' == '{destinationKey}' -> {keyMatches}"
        );

        return typeMatches && keyMatches;
    }


    // =========================================================
    // CHECK TYPE ONLY
    // =========================================================

    public bool IsCorrectType(
        DataValue value)
    {
        if (value == null)
            return false;

        return value.Type == expectedType;
    }


    // =========================================================
    // SHOW FEEDBACK FROM DATAVALUE
    // =========================================================

    /// <summary>
    /// GREEN when Type + SemanticKey are correct.
    /// RED when either one is incorrect.
    /// </summary>
    public void ShowFeedback(
        DataValue value)
    {
        if (value == null)
        {
            ForceRestore();
            return;
        }

        bool correct =
            IsCorrectValue(value);

        DebugTrace.Log(
            $"[Type Feedback] " +
            $"SHOW FEEDBACK | " +
            $"Destination={gameObject.name} | " +
            $"Result={(correct ? "CORRECT / GREEN" : "WRONG / RED")} | " +
            $"Targets={GetTargetCount()}"
        );

        ShowFeedback(correct);
    }


    // =========================================================
    // SHOW FEEDBACK FROM BOOLEAN
    // =========================================================

    public void ShowFeedback(
        bool correct)
    {
        if (!isActiveAndEnabled)
            return;

        // The right kind of value: a green check pops on the field's corner.
        if (correct && transform is RectTransform)
            StartCoroutine(PopCheck());

        // -----------------------------------------------------
        // Stop previous feedback
        // -----------------------------------------------------

        if (feedbackCoroutine != null)
        {
            StopCoroutine(
                feedbackCoroutine
            );

            feedbackCoroutine = null;
        }

        // -----------------------------------------------------
        // Make sure targets exist
        // -----------------------------------------------------

        if (targetImages == null ||
            targetImages.Length == 0)
        {
            SetupVisualTargets();
        }

        // -----------------------------------------------------
        // Start feedback
        // -----------------------------------------------------

        feedbackCoroutine =
            StartCoroutine(
                FeedbackRoutine(correct)
            );
    }


    // =========================================================
    // FEEDBACK ROUTINE
    // =========================================================

    private IEnumerator FeedbackRoutine(
        bool correct)
    {
        Color feedbackColor =
            correct
                ? successColor
                : errorColor;

        // Badge, punch/wobble and edge glow (presentation only).
        DropFeedbackFX.For(this).Play(correct, feedbackColor);

        // Smooth tint pulse: quick rise, then ease back to the resting colours.
        float duration =
            Mathf.Max(0.2f, feedbackDuration + 0.3f);

        for (float elapsed = 0f; elapsed < duration; elapsed += Time.unscaledDeltaTime)
        {
            float t = elapsed / duration;
            float blend = t < 0.15f
                ? t / 0.15f
                : Mathf.SmoothStep(1f, 0f, (t - 0.15f) / 0.85f);

            ApplyTint(feedbackColor, blend * tintStrength);
            yield return null;
        }

        // -----------------------------------------------------
        // Restore
        // -----------------------------------------------------

        RestoreOriginalColors();

        feedbackCoroutine = null;
    }

    private void ApplyTint(Color tint, float amount)
    {
        if (targetImages == null || originalColors == null)
            return;

        for (int i = 0; i < targetImages.Length && i < originalColors.Length; i++)
        {
            Image image = targetImages[i];
            if (image == null)
                continue;

            Color rest = RestingColor(image, originalColors[i]);
            Color peak = tint;
            peak.a = Mathf.Max(rest.a, 0.85f);
            image.color = Color.Lerp(rest, peak, amount);
        }
    }

    private static bool IsFeedbackTarget(Image image)
    {
        for (Transform t = image.transform; t != null; t = t.parent)
        {
            if (t.name == "DropFeedbackBadge" || t.name == "PaperInkDecor" || t.name == "ValueHoverCue")
                return false;
        }

        return true;
    }


    // =========================================================
    // RESTORE ORIGINAL COLORS
    // =========================================================

    private void RestoreOriginalColors()
    {
        if (targetImages == null)
            return;

        if (originalColors == null)
            return;

        for (int i = 0;
             i < targetImages.Length;
             i++)
        {
            Image image =
                targetImages[i];

            if (image == null)
                continue;

            if (i >= originalColors.Length)
                continue;

            image.color =
                RestingColor(image, originalColors[i]);

            image.SetVerticesDirty();
            image.SetMaterialDirty();
        }
    }


    // =========================================================
    // FORCE RESTORE
    // =========================================================

    public void ForceRestore()
    {
        if (feedbackCoroutine != null)
        {
            StopCoroutine(
                feedbackCoroutine
            );

            feedbackCoroutine = null;
        }

        RestoreOriginalColors();

        DropFeedbackFX fx = GetComponent<DropFeedbackFX>();
        if (fx != null)
            fx.Restore();
    }


    // =========================================================
    // TARGET COUNT
    // =========================================================

    private int GetTargetCount()
    {
        if (targetImages == null)
            return 0;

        return targetImages.Length;
    }


    // =========================================================
    // ON DESTROY
    // =========================================================

    private Color RestingColor(Image image, Color fallback)
    {
        ValueSelectionHighlighter highlighter = GetComponent<ValueSelectionHighlighter>();
        return highlighter != null ? highlighter.RestingColor(image, fallback) : fallback;
    }

    private void OnDisable()
    {
        ForceRestore();
    }

    private void OnDestroy()
    {
        if (feedbackCoroutine != null)
        {
            StopCoroutine(
                feedbackCoroutine
            );

            feedbackCoroutine = null;
        }
    }

    /// <summary>A round green check that pops in on the right of the field, then fades.</summary>
    private IEnumerator PopCheck()
    {
        RectTransform badge = HudTheme.CreateRect("Placed Check", transform);
        badge.anchorMin = badge.anchorMax = new Vector2(1f, 0.5f);
        badge.pivot = new Vector2(0.5f, 0.5f);
        badge.anchoredPosition = new Vector2(-30f, 0f);
        badge.sizeDelta = new Vector2(44f, 44f);
        badge.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
        CanvasGroup fade = badge.gameObject.AddComponent<CanvasGroup>();
        fade.blocksRaycasts = false;
        Image disc = HudTheme.AddImage(badge, HudTheme.Circle, HudTheme.Complete);
        disc.raycastTarget = false;
        RectTransform tick = HudTheme.CreateRect("Tick", badge);
        HudTheme.Stretch(tick, 9f);
        Image mark = HudTheme.AddImage(tick, HudTheme.Check, Color.white);
        mark.preserveAspect = true;
        mark.raycastTarget = false;
        for (float t = 0f; t < 1.1f && badge != null; t += Time.unscaledDeltaTime)
        {
            float pop = Mathf.Clamp01(t / 0.28f);
            badge.localScale = Vector3.one * Mathf.LerpUnclamped(0.2f, 1f, HudTheme.EaseOutBack(pop));
            fade.alpha = t < 0.8f ? 1f : 1f - (t - 0.8f) / 0.3f;
            yield return null;
        }
        if (badge != null) Destroy(badge.gameObject);
    }
}