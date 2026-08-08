using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// PURPOSE:
/// The tutorial popup view. Visually and structurally mirrors DialogueUI
/// (portrait / speaker name / text / continue button), but is a SEPARATE
/// component reused on the duplicated Tutorial Panel in the hierarchy.
///
/// UNLIKE DialogueUI, this does NOT walk a queue internally. It shows
/// exactly one step, waits for Continue, then hands control back to
/// TutorialController — which decides whether to show the next step
/// immediately or wait for a gameplay event first. That waiting logic
/// does not belong here; this class only knows how to render and hide.
///
/// RESPONSIBILITIES:
/// - Show/hide the tutorial panel
/// - Render speaker name / text / portrait for one step
/// - Show/hide the optional world-space arrow
/// - Lock/unlock player controls while visible
/// - Invoke a callback when Continue is pressed
///
/// DOES NOT:
/// - Know about TutorialStepType, gameplay completion, or step ordering
///   (that's TutorialController's job)
/// </summary>
public class TutorialUI : MonoBehaviour
{
    [Header("Panel")]
    [SerializeField] private GameObject panelRoot;

    [Header("Portrait")]
    [SerializeField] private Image portraitImage;
    [SerializeField] private Sprite defaultPortraitPlaceholder;

    [Header("Text")]
    [SerializeField] private TextMeshProUGUI speakerNameText;
    [SerializeField] private TextMeshProUGUI tutorialText;

    [Header("Continue Button")]
    [SerializeField] private Button continueButton;

    [Header("Arrow (optional)")]
    [SerializeField] private GameObject arrowObject;   // The Arrow UI Image GameObject
    [SerializeField] private RectTransform arrowRectTransform;
    [SerializeField] private Camera mainCamera;         // Assign the scene's main camera
    [SerializeField] private Vector2 arrowScreenOffset = new Vector2(0f, 100f); // Pushes arrow above target's pivot point when ON-SCREEN
    [SerializeField] private float edgePadding = 80f;   // How far from the screen edge the clamped arrow sits, in pixels

    [Tooltip("Degrees to add so the arrow SPRITE's drawn direction lines up with 0 = pointing right. " +
             "Your current sprite points down-right diagonally, so its natural rest angle isn't 0. Adjust this until rotation looks correct.")]
    [SerializeField] private float arrowSpriteAngleOffset = -45f;

    private System.Action onContinuePressed;
    private Transform arrowTarget;
    private bool arrowActive;

    private void Awake()
    {
        HidePanel();
        continueButton.onClick.AddListener(OnContinueClicked);

        if (mainCamera == null)
            mainCamera = Camera.main;
    }

    /// <summary>
    /// Shows the tutorial popup with no arrow.
    /// </summary>
    public void Show(string speakerName, string text, System.Action onContinue, string portraitId = "")
    {
        Show(speakerName, text, onContinue, target: null, portraitId: portraitId);
    }

    /// <summary>
    /// Shows the tutorial popup with an optional world-space arrow target and
    /// optional portrait. portraitId is resolved through the same
    /// DialoguePortraitDatabase used by DialogueUI — pass "" to use the
    /// default placeholder sprite.
    /// </summary>
    public void Show(string speakerName, string text, System.Action onContinue, Transform target, string portraitId = "")
    {
        speakerNameText.text = speakerName;
        tutorialText.text = text;

        Sprite portrait = null;
        if (!string.IsNullOrEmpty(portraitId) && DialoguePortraitDatabase.Instance != null)
        {
            portrait = DialoguePortraitDatabase.Instance.GetPortrait(portraitId);
        }
        portraitImage.sprite = portrait != null ? portrait : defaultPortraitPlaceholder;

        onContinuePressed = onContinue;

        CameraController.Instance.LockPlayerControls();

        panelRoot.SetActive(true);

        if (target != null)
            ShowArrow(target);
        else
            HideArrow();

        Debug.Log($"[TutorialUI] Showing step — Speaker: {speakerName}, Text: \"{text}\"");
    }

    public void HidePanel()
    {
        panelRoot.SetActive(false);
        HideArrow();
    }

    private void OnContinueClicked()
    {
        Debug.Log("[TutorialUI] Continue pressed.");

        HidePanel();
        CameraController.Instance.UnlockPlayerControls();

        onContinuePressed?.Invoke();
    }

    public void ShowArrow(Transform target)
    {
        if (arrowObject == null || target == null) return;

        arrowTarget = target;
        arrowActive = true;
        arrowObject.SetActive(true);
    }

    public void HideArrow()
    {
        arrowActive = false;
        arrowTarget = null;

        if (arrowObject != null)
            arrowObject.SetActive(false);
    }

    /// <summary>
    /// Compass-style tracking arrow:
    /// - If the target is visible on-screen: arrow sits above it (offset), no rotation, points down at it.
    /// - If the target is off-screen or behind the camera: arrow clamps to the
    ///   screen edge closest to the target's direction and ROTATES to point
    ///   toward it, like a standard objective marker.
    /// </summary>
    private void Update()
    {
        if (!arrowActive || arrowTarget == null || arrowRectTransform == null || mainCamera == null)
            return;

        if (!arrowObject.activeSelf)
            arrowObject.SetActive(true);

        Vector3 viewportPos = mainCamera.WorldToViewportPoint(arrowTarget.position);
        bool isBehindCamera = viewportPos.z < 0f;
        bool isOnScreen = !isBehindCamera
            && viewportPos.x > 0f && viewportPos.x < 1f
            && viewportPos.y > 0f && viewportPos.y < 1f;

        if (isOnScreen)
        {
            // Simple on-screen case: sit above the target, no rotation.
            Vector3 screenPos = mainCamera.WorldToScreenPoint(arrowTarget.position);
            arrowRectTransform.position = (Vector3)((Vector2)screenPos + arrowScreenOffset);
            arrowRectTransform.rotation = Quaternion.identity;
            return;
        }

        // OFF-SCREEN (or behind camera): clamp to edge + rotate to point toward target.
        // If behind the camera, flip the viewport direction so the arrow points
        // the correct way instead of backward.
        Vector3 direction = viewportPos - new Vector3(0.5f, 0.5f, 0f);
        if (isBehindCamera)
            direction = -direction;

        // Screen center + padding-adjusted half-extents.
        float screenCenterX = Screen.width * 0.5f;
        float screenCenterY = Screen.height * 0.5f;
        float halfWidth = screenCenterX - edgePadding;
        float halfHeight = screenCenterY - edgePadding;

        // Find how far we'd need to scale `direction` so it hits the screen's
        // padded rectangle boundary (whichever axis is more restrictive).
        float scaleX = halfWidth / Mathf.Max(Mathf.Abs(direction.x), 0.0001f);
        float scaleY = halfHeight / Mathf.Max(Mathf.Abs(direction.y), 0.0001f);
        float scale = Mathf.Min(scaleX, scaleY);

        Vector2 clampedPos = new Vector2(
            screenCenterX + direction.x * scale,
            screenCenterY + direction.y * scale
        );

        arrowRectTransform.position = clampedPos;

        // Rotate to point from screen center toward the target direction.
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        arrowRectTransform.rotation = Quaternion.Euler(0f, 0f, angle + arrowSpriteAngleOffset);
    }
}