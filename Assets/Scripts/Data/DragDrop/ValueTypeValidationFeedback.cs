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

    [SerializeField]
    private Color correctColor = Color.green;
    [SerializeField]
    private Color incorrectColor = Color.red;


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


    // =========================================================
    // AWAKE
    // =========================================================

    private void Awake()
    {
        SetupVisualTargets();
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
                GetComponentsInChildren<Image>(true);
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

        Debug.Log(
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
                Debug.Log(
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

        Debug.Log(
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

        Debug.Log(
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
            return;

        bool correct =
            IsCorrectValue(value);

        Debug.Log(
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
                ? correctColor
                : incorrectColor;

        Debug.Log(
            $"[Type Feedback] APPLYING " +
            $"{(correct ? "GREEN" : "RED")} " +
            $"to {gameObject.name}"
        );

        Debug.Log(
            $"[Type Feedback] Color = " +
            $"{feedbackColor} | " +
            $"Target Count = {GetTargetCount()}"
        );

        // -----------------------------------------------------
        // Apply color
        // -----------------------------------------------------

        if (targetImages != null)
        {
            for (int i = 0;
                 i < targetImages.Length;
                 i++)
            {
                Image image =
                    targetImages[i];

                if (image == null)
                {
                    Debug.LogWarning(
                        $"[Type Feedback] " +
                        $"Target Image {i} is NULL."
                    );

                    continue;
                }

                Debug.Log(
                    $"[Type Feedback] " +
                    $"Changing {image.gameObject.name} " +
                    $"from {image.color} " +
                    $"to {feedbackColor}"
                );

                image.color =
                    feedbackColor;

                // -------------------------------------------------
                // Force Unity UI refresh
                // -------------------------------------------------

                image.SetVerticesDirty();
                image.SetMaterialDirty();
            }
        }

        // -----------------------------------------------------
        // Wait
        // -----------------------------------------------------

        yield return new WaitForSeconds(
            feedbackDuration
        );

        // -----------------------------------------------------
        // Restore
        // -----------------------------------------------------

        RestoreOriginalColors();

        feedbackCoroutine = null;

        Debug.Log(
            $"[Type Feedback] " +
            $"Feedback finished on {gameObject.name}"
        );
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
                originalColors[i];

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
}