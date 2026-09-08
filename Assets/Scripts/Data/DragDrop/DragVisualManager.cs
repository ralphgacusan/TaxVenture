using UnityEngine;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// Controls the shared floating visual used while dragging DataValues.
///
/// Hierarchy:
///
/// DragVisualCanvas
///     └── DraggedValueVisual
///             └── ValueText
///
/// DraggedValueVisual = white/background Image
/// ValueText          = TextMeshProUGUI
///
/// Responsibilities:
/// - Keep the drag visual hidden when the scene starts
/// - Show the visual only when dragging
/// - Display only the dragged value
/// - Dynamically resize the background to fit the text
/// - Move the visual with the pointer
/// - Hide the visual when dragging ends
///
/// IMPORTANT:
/// - Does NOT modify DataValue
/// - Does NOT modify ValueTransferManager
/// - Does NOT affect click-to-click selection
/// - Does NOT affect drop validation
/// - Visual-only changes
/// </summary>
public class DragVisualManager : MonoBehaviour
{
    public static DragVisualManager Instance { get; private set; }

    [Header("Drag Visual")]
    [SerializeField] private GameObject dragVisual;

    [Header("Dynamic Size")]
    [SerializeField] private float horizontalPadding = 80f;
    [SerializeField] private float verticalPadding = 52f;

    [SerializeField] private float minimumWidth = 80f;
    [SerializeField] private float minimumHeight = 40f;

    [SerializeField] private float maximumWidth = 500f;

    private TextMeshProUGUI dragVisualText;

    private RectTransform dragVisualRect;
    private RectTransform dragVisualTextRect;

    private CanvasGroup dragVisualCanvasGroup;

    private ContentSizeFitter dragVisualContentSizeFitter;
    private ContentSizeFitter textContentSizeFitter;

    // ---------------------------------------------------------
    // Awake
    // ---------------------------------------------------------

    private void Awake()
    {
        // -----------------------------------------------------
        // Singleton
        // -----------------------------------------------------

        if (Instance != null && Instance != this)
        {
            Debug.LogWarning(
                "Another DragVisualManager already exists. " +
                "Destroying this duplicate."
            );

            Destroy(gameObject);
            return;
        }

        Instance = this;

        // -----------------------------------------------------
        // Validate Drag Visual
        // -----------------------------------------------------

        if (dragVisual == null)
        {
            Debug.LogError(
                "DragVisualManager: Drag Visual is NOT assigned!"
            );

            return;
        }

        // -----------------------------------------------------
        // Get Drag Visual RectTransform
        // -----------------------------------------------------

        dragVisualRect =
            dragVisual.GetComponent<RectTransform>();

        if (dragVisualRect == null)
        {
            Debug.LogError(
                "DragVisualManager: DraggedValueVisual " +
                "does not have a RectTransform!"
            );

            return;
        }

        // -----------------------------------------------------
        // Find ValueText
        // -----------------------------------------------------

        dragVisualText =
            dragVisual.GetComponentInChildren<TextMeshProUGUI>(
                true
            );

        if (dragVisualText == null)
        {
            Debug.LogError(
                "DragVisualManager: Could not find " +
                "TextMeshProUGUI inside DraggedValueVisual!"
            );

            return;
        }

        dragVisualTextRect =
            dragVisualText.GetComponent<RectTransform>();

        // -----------------------------------------------------
        // Canvas Group
        // -----------------------------------------------------

        dragVisualCanvasGroup =
            dragVisual.GetComponent<CanvasGroup>();

        if (dragVisualCanvasGroup == null)
        {
            dragVisualCanvasGroup =
                dragVisual.AddComponent<CanvasGroup>();
        }

        // -----------------------------------------------------
        // IMPORTANT:
        // Drag visual must NOT block raycasts.
        // -----------------------------------------------------

        dragVisualCanvasGroup.blocksRaycasts = false;
        dragVisualCanvasGroup.interactable = false;

        // -----------------------------------------------------
        // Disable Content Size Fitter
        // -----------------------------------------------------
        //
        // We are manually controlling the RectTransforms.
        //
        // ContentSizeFitter can fight against the script.
        // -----------------------------------------------------

        dragVisualContentSizeFitter =
            dragVisual.GetComponent<ContentSizeFitter>();

        if (dragVisualContentSizeFitter != null)
        {
            dragVisualContentSizeFitter.enabled = false;

            Debug.Log(
                "[DragVisualManager] Disabled ContentSizeFitter " +
                "on DraggedValueVisual."
            );
        }

        textContentSizeFitter =
            dragVisualText.GetComponent<ContentSizeFitter>();

        if (textContentSizeFitter != null)
        {
            textContentSizeFitter.enabled = false;

            Debug.Log(
                "[DragVisualManager] Disabled ContentSizeFitter " +
                "on ValueText."
            );
        }

        // -----------------------------------------------------
        // Configure Text
        // -----------------------------------------------------

        dragVisualText.enableWordWrapping = false;
        dragVisualText.enableAutoSizing = false;

        // -----------------------------------------------------
        // IMPORTANT:
        // Force predictable anchors.
        //
        // This prevents existing Inspector anchor settings
        // from interfering with dynamic sizing.
        // -----------------------------------------------------

        dragVisualRect.anchorMin =
            new Vector2(0.5f, 0.5f);

        dragVisualRect.anchorMax =
            new Vector2(0.5f, 0.5f);

        dragVisualRect.pivot =
            new Vector2(0.5f, 0.5f);

        dragVisualTextRect.anchorMin =
            new Vector2(0.5f, 0.5f);

        dragVisualTextRect.anchorMax =
            new Vector2(0.5f, 0.5f);

        dragVisualTextRect.pivot =
            new Vector2(0.5f, 0.5f);

        // -----------------------------------------------------
        // Initial State
        // -----------------------------------------------------

        dragVisual.SetActive(false);

        Debug.Log(
            "DragVisualManager initialized successfully."
        );
    }

    // ---------------------------------------------------------
    // Show
    // ---------------------------------------------------------

    /// <summary>
    /// Shows the drag visual with the supplied text.
    /// The background automatically resizes to fit the text.
    /// </summary>
    public void Show(string text)
    {
        if (dragVisual == null)
        {
            Debug.LogError(
                "DragVisualManager.Show(): " +
                "Drag Visual is NULL!"
            );

            return;
        }

        if (dragVisualText == null)
        {
            Debug.LogError(
                "DragVisualManager.Show(): " +
                "ValueText is NULL!"
            );

            return;
        }

        // -----------------------------------------------------
        // Set Text
        // -----------------------------------------------------

        dragVisualText.enableWordWrapping = false;
        dragVisualText.enableAutoSizing = false;

        dragVisualText.text =
            string.IsNullOrEmpty(text)
                ? "?"
                : text;

        // -----------------------------------------------------
        // Activate
        // -----------------------------------------------------

        dragVisual.SetActive(true);

        // -----------------------------------------------------
        // Raycasts
        // -----------------------------------------------------

        if (dragVisualCanvasGroup != null)
        {
            dragVisualCanvasGroup.blocksRaycasts = false;
            dragVisualCanvasGroup.interactable = false;
        }

        // -----------------------------------------------------
        // Recalculate Size
        // -----------------------------------------------------

        ResizeToText();

        Debug.Log(
            $"[DragVisualManager] " +
            $"Drag visual shown: {dragVisualText.text}"
        );
    }

    // ---------------------------------------------------------
    // Resize To Text
    // ---------------------------------------------------------

    /// <summary>
    /// Dynamically resizes:
    ///
    /// DraggedValueVisual
    ///     └── ValueText
    ///
    /// The text determines the required size.
    /// The background is then made slightly larger using
    /// the configured padding.
    /// </summary>
    private void ResizeToText()
    {
        if (dragVisualRect == null)
            return;

        if (dragVisualText == null)
            return;

        if (dragVisualTextRect == null)
            return;

        // -----------------------------------------------------
        // Make sure TMP is configured correctly
        // -----------------------------------------------------

        dragVisualText.enableWordWrapping = false;
        dragVisualText.enableAutoSizing = false;

        // -----------------------------------------------------
        // Force TMP to update
        // -----------------------------------------------------

        dragVisualText.ForceMeshUpdate();

        // -----------------------------------------------------
        // Get preferred size
        // -----------------------------------------------------
        //
        // Use a very large width/height so the current
        // RectTransform size does NOT constrain the result.
        //
        // This is important because the original ValueText
        // may have started at something like 80 x 40.
        // -----------------------------------------------------

        Vector2 preferredSize =
            dragVisualText.GetPreferredValues(
                dragVisualText.text,
                10000f,
                10000f
            );

        float textWidth =
            preferredSize.x;

        float textHeight =
            preferredSize.y;

        // -----------------------------------------------------
        // Safety
        // -----------------------------------------------------

        textWidth =
            Mathf.Max(textWidth, 1f);

        textHeight =
            Mathf.Max(textHeight, 1f);

        // -----------------------------------------------------
        // Calculate Background Size
        // -----------------------------------------------------

        float targetWidth =
            textWidth + horizontalPadding;

        float targetHeight =
            textHeight + verticalPadding;

        // -----------------------------------------------------
        // Apply Minimum Size
        // -----------------------------------------------------

        targetWidth =
            Mathf.Max(
                targetWidth,
                minimumWidth
            );

        targetHeight =
            Mathf.Max(
                targetHeight,
                minimumHeight
            );

        // -----------------------------------------------------
        // Apply Maximum Width
        // -----------------------------------------------------

        targetWidth =
            Mathf.Min(
                targetWidth,
                maximumWidth
            );

        // -----------------------------------------------------
        // IMPORTANT:
        // Directly set sizeDelta.
        //
        // Because we forced the anchors to the center,
        // sizeDelta directly represents the width/height.
        // -----------------------------------------------------

        dragVisualRect.sizeDelta =
            new Vector2(
                targetWidth,
                targetHeight
            );

        // -----------------------------------------------------
        // Resize Text
        // -----------------------------------------------------
        //
        // Give the text its actual required size.
        // -----------------------------------------------------

        dragVisualTextRect.sizeDelta =
            new Vector2(
                textWidth,
                textHeight
            );

        // -----------------------------------------------------
        // Keep Text Centered
        // -----------------------------------------------------

        dragVisualTextRect.anchoredPosition =
            Vector2.zero;

        // -----------------------------------------------------
        // Debug
        // -----------------------------------------------------

        Debug.Log(
            "[DragVisualManager] " +
            $"Text: \"{dragVisualText.text}\" | " +
            $"TextSize: {textWidth}x{textHeight} | " +
            $"BackgroundSize: {targetWidth}x{targetHeight}"
        );
    }

    // ---------------------------------------------------------
    // Hide
    // ---------------------------------------------------------

    public void Hide()
    {
        if (dragVisual == null)
            return;

        dragVisual.SetActive(false);

        Debug.Log(
            "[DragVisualManager] Drag visual hidden."
        );
    }

    // ---------------------------------------------------------
    // Update Position
    // ---------------------------------------------------------

    public void UpdatePosition(Vector2 screenPosition)
    {
        if (dragVisual == null)
            return;

        if (dragVisualRect == null)
            return;

        dragVisualRect.position =
            screenPosition;
    }

    // ---------------------------------------------------------
    // Set Text
    // ---------------------------------------------------------

    /// <summary>
    /// Changes the drag visual text and immediately resizes
    /// the background.
    /// </summary>
    public void SetText(string text)
    {
        if (dragVisualText == null)
            return;

        dragVisualText.enableWordWrapping = false;
        dragVisualText.enableAutoSizing = false;

        dragVisualText.text =
            string.IsNullOrEmpty(text)
                ? "?"
                : text;

        ResizeToText();
    }

    // ---------------------------------------------------------
    // Force Hide
    // ---------------------------------------------------------

    public void ForceHide()
    {
        if (dragVisual == null)
            return;

        dragVisual.SetActive(false);
    }

    // ---------------------------------------------------------
    // Cleanup
    // ---------------------------------------------------------

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}