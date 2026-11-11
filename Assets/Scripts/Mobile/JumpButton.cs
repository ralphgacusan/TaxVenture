using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// PURPOSE:
/// Milestone T03: the right-side JUMP control for adopted exploration
/// (Office_Level1). Built at runtime under MobileControls (so the existing
/// safe-area inset applies) by CameraController; nothing in the scene is
/// rewritten.
///
/// - Its own pointer: the movement stick (left) and look swipes (right)
///   keep their fingers, so move, look and jump work together. Being UI, a
///   press here never rotates the camera or taps the world.
/// - One press = one jump request (PlayerMovement decides if grounded);
///   holding does not repeat.
/// - Effective target at least 48 dp on any screen and canvas scale.
/// - Shown only while the movement stick is shown and movement is live
///   (holds, tasks, dialogue and transitions hide the stick, so it goes too).
/// </summary>
public sealed class JumpButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public const string ObjectName = "JumpButton";
    public const float MinimumDp = 48f;
    private const float ReferenceSize = 150f;

    private PlayerMovement movement;
    private VirtualJoystick stick;
    private RectTransform rect;
    private Canvas canvas;
    private Image face;
    private Image hitArea;
    private Graphic label;
    private int pointer = int.MinValue;

    public bool IsPressed => pointer != int.MinValue;

    /// <summary>Builds the button beside the movement stick's own controls group (once).</summary>
    public static JumpButton Create(VirtualJoystick movementStick, PlayerMovement player)
    {
        if (movementStick == null || player == null || !player.SprintAndJumpEnabled) return null;
        Transform group = movementStick.transform.parent != null ? movementStick.transform.parent : movementStick.transform;
        Transform existing = group.Find(ObjectName);
        if (existing != null && existing.TryGetComponent(out JumpButton found)) { found.Bind(movementStick, player); return found; }

        var host = new GameObject(ObjectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        var rect = (RectTransform)host.transform;
        rect.SetParent(group, false);
        rect.anchorMin = rect.anchorMax = new Vector2(1f, 0f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(-310f, 230f);
        rect.sizeDelta = new Vector2(ReferenceSize, ReferenceSize);
        Image hit = host.GetComponent<Image>();
        hit.color = new Color(0f, 0f, 0f, 0f);   // the whole square is the touch target
        hit.raycastTarget = true;

        var faceHost = new GameObject("Face", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        var faceRect = (RectTransform)faceHost.transform;
        faceRect.SetParent(rect, false);
        faceRect.anchorMin = Vector2.zero; faceRect.anchorMax = Vector2.one;
        faceRect.offsetMin = new Vector2(10f, 10f); faceRect.offsetMax = new Vector2(-10f, -10f);
        Image disc = faceHost.GetComponent<Image>();
        disc.sprite = HudTheme.Circle;
        disc.color = new Color32(18, 28, 41, 190);
        disc.raycastTarget = false;
        var iconHost = new GameObject("JumpIcon", typeof(RectTransform), typeof(CanvasRenderer));
        var iconRect = (RectTransform)iconHost.transform;
        iconRect.SetParent(faceRect, false);
        HudTheme.Stretch(iconRect);
        iconRect.offsetMin = new Vector2(24f, 24f);
        iconRect.offsetMax = new Vector2(-24f, -24f);
        var label = iconHost.AddComponent<TraversalIcon>();
        label.Symbol = TraversalIcon.Kind.Jump;
        label.color = HudTheme.TextPrimary;
        label.raycastTarget = false;

        JumpButton button = host.AddComponent<JumpButton>();
        button.face = disc;
        button.hitArea = hit;
        button.label = label;
        button.Bind(movementStick, player);
        return button;
    }

    private void Bind(VirtualJoystick movementStick, PlayerMovement player)
    {
        stick = movementStick;
        movement = player;
        rect = (RectTransform)transform;
        canvas = GetComponentInParent<Canvas>();
        EnsureMinimumSize();
    }

    /// <summary>At least 48 dp on the physical screen, whatever the canvas scaling does.</summary>
    private void EnsureMinimumSize()
    {
        if (rect == null) return;
        float dpi = Screen.dpi > 1f ? Screen.dpi : 160f;
        float minimumPixels = MinimumDp * dpi / 160f;
        float scale = canvas != null && canvas.rootCanvas != null ? Mathf.Max(0.01f, canvas.rootCanvas.scaleFactor) : 1f;
        float side = Mathf.Max(ReferenceSize, minimumPixels / scale);
        rect.sizeDelta = new Vector2(side, side);
    }

    /// <summary>Effective on-screen size in pixels (for checks and the measurement readout).</summary>
    public float ScreenPixels
    {
        get
        {
            float scale = canvas != null && canvas.rootCanvas != null ? canvas.rootCanvas.scaleFactor : 1f;
            return rect != null ? Mathf.Min(rect.rect.width, rect.rect.height) * scale : 0f;
        }
    }

    private void LateUpdate()
    {
        bool show = stick != null && stick.gameObject.activeInHierarchy && movement != null && movement.isActiveAndEnabled &&
                    movement.SprintAndJumpEnabled;
        if (face != null && face.enabled != show) face.enabled = show;
        if (label != null && label.enabled != show) label.enabled = show;
        if (hitArea != null && hitArea.raycastTarget != show) { hitArea.raycastTarget = show; if (!show) Release(); }
        if (face != null) face.color = IsPressed ? new Color32(120, 92, 44, 220) : new Color32(18, 28, 41, 190);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData == null || IsPressed) return;
        pointer = eventData.pointerId;
        if (movement != null) movement.RequestJump();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (eventData != null && eventData.pointerId == pointer) Release();
    }

    private void Release() => pointer = int.MinValue;

    private void OnDisable() => Release();
    private void OnApplicationFocus(bool focused) { if (!focused) Release(); }
    private void OnApplicationPause(bool paused) { if (paused) Release(); }
}
