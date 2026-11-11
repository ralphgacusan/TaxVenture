using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class VirtualJoystick : MonoBehaviour,
    IPointerDownHandler,
    IPointerUpHandler,
    IDragHandler,
    ICancelHandler
{
    [Header("References")]
    [SerializeField] private RectTransform background;
    [SerializeField] private RectTransform handle;

    [Header("Settings")]
    [SerializeField] private float handleRange = 100f;
    [SerializeField, Range(0f, 0.95f)] private float deadZone = 0.12f;
    [SerializeField, Range(0.1f, 2f)] private float sensitivity = 1f;

    public Vector2 Input { get; private set; }
    /// <summary>T03: stick travel before the dead zone/response curve (0 centre, 1 walking rim, 1.65 sprint extension).</summary>
    public float RawMagnitude { get; private set; }
    public float Horizontal => Input.x;
    public float Vertical => Input.y;
    public bool IsHeld => isHeld;
    public static bool IsAnyHeld => activeJoystickCount > 0;
    public static bool BlocksWorldInput => IsAnyHeld || releasedFrame == Time.frameCount;

    private const float VisualEaseSpeed = 18f;
    private const float PressedScaleMultiplier = 1.06f;

    private static int activeJoystickCount;
    private static int releasedFrame = -1;

    private Camera uiCamera;
    private Image handleImage;
    private Vector2 homeHandlePosition;
    private Vector3 homeHandleScale;
    private Color homeHandleColor;
    private Vector3[] handleCorners;
    private bool visualsCached;
    private bool isHeld;
    private int activePointerId;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetActiveJoystickCount()
    {
        activeJoystickCount = 0;
        releasedFrame = -1;
    }

    private void Awake()
    {
        if (background == null)
            background = GetComponent<RectTransform>();

        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            uiCamera = canvas.worldCamera;

        if (handle != null)
        {
            homeHandlePosition = handle.anchoredPosition;
            homeHandleScale = handle.localScale;
            handleImage = handle.GetComponent<Image>();
            if (handleImage != null)
                homeHandleColor = handleImage.color;
            handleCorners = new Vector3[4];
            visualsCached = true;
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (isHeld || eventData.button != PointerEventData.InputButton.Left ||
            background == null || handle == null)
        {
            return;
        }

        if (!TryGetLocalPoint(eventData, out Vector2 localPoint))
            return;

        activePointerId = eventData.pointerId;
        SetHeld(true);
        UpdateInputAndHandle(localPoint);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isHeld || eventData.pointerId != activePointerId ||
            background == null || handle == null)
        {
            return;
        }

        if (TryGetLocalPoint(eventData, out Vector2 localPoint))
            UpdateInputAndHandle(localPoint);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (isHeld && eventData.pointerId == activePointerId)
            ReleasePointer();
    }

    public void OnCancel(BaseEventData eventData)
    {
        if (isHeld)
            ResetJoystick();
    }

    private void UpdateInputAndHandle(Vector2 localPoint)
    {
        Rect rect = background.rect;
        Vector2 travel = GetTravelRadius(rect);
        Vector2 fromCenter = localPoint - rect.center;
        Vector2 rawVector = new Vector2(
            travel.x > Mathf.Epsilon ? fromCenter.x / travel.x : 0f,
            travel.y > Mathf.Epsilon ? fromCenter.y / travel.y : 0f);
        rawVector = Vector2.ClampMagnitude(rawVector, sprintRing != null && sprintRing.gameObject.activeSelf ? ExplorationRules.SprintTravelLimit : 1f);
        RawMagnitude = rawVector.magnitude;

        float boundedDeadZone = Mathf.Clamp(deadZone, 0f, 0.95f);
        float boundedSensitivity = Mathf.Clamp(sensitivity, 0.1f, 2f);
        float magnitude = Mathf.Clamp01(JoystickInputMath.RemapMagnitude(
            rawVector.magnitude, boundedDeadZone, boundedSensitivity));
        Input = rawVector.sqrMagnitude > 0f
            ? rawVector.normalized * magnitude
            : Vector2.zero;

        Vector2 localOffset = Vector2.Scale(rawVector, travel);
        Transform handleParent = handle.parent;
        Vector3 worldOffset = background.TransformVector(
            new Vector3(localOffset.x, localOffset.y, 0f));
        Vector3 parentOffset = handleParent != null
            ? handleParent.InverseTransformVector(worldOffset)
            : worldOffset;
        handle.anchoredPosition = homeHandlePosition +
            new Vector2(parentOffset.x, parentOffset.y);
    }

    private bool TryGetLocalPoint(PointerEventData eventData, out Vector2 localPoint)
    {
        Camera eventCamera = eventData.pressEventCamera != null
            ? eventData.pressEventCamera
            : uiCamera;
        return RectTransformUtility.ScreenPointToLocalPointInRectangle(
            background, eventData.position, eventCamera, out localPoint);
    }

    private Vector2 GetTravelRadius(Rect rect)
    {
        if (rect.width <= Mathf.Epsilon || rect.height <= Mathf.Epsilon)
            return Vector2.zero;

        Vector2 center = rect.center;
        float left = center.x - rect.xMin;
        float right = rect.xMax - center.x;
        float bottom = center.y - rect.yMin;
        float top = rect.yMax - center.y;

        if (handle != null && handleCorners != null)
        {
            handle.GetWorldCorners(handleCorners);
            Vector2 pivot = background.InverseTransformPoint(handle.position);
            float minX = pivot.x;
            float maxX = pivot.x;
            float minY = pivot.y;
            float maxY = pivot.y;

            for (int i = 0; i < handleCorners.Length; i++)
            {
                Vector2 corner = background.InverseTransformPoint(handleCorners[i]);
                minX = Mathf.Min(minX, corner.x);
                maxX = Mathf.Max(maxX, corner.x);
                minY = Mathf.Min(minY, corner.y);
                maxY = Mathf.Max(maxY, corner.y);
            }

            left -= pivot.x - minX;
            right -= maxX - pivot.x;
            bottom -= pivot.y - minY;
            top -= maxY - pivot.y;
        }

        float range = Mathf.Max(0f, handleRange);
        return new Vector2(
            Mathf.Min(range, Mathf.Max(0f, Mathf.Min(left, right))),
            Mathf.Min(range, Mathf.Max(0f, Mathf.Min(bottom, top))));
    }

    private void Update()
    {
        float amount = 1f - Mathf.Exp(-VisualEaseSpeed * Time.unscaledDeltaTime);

        if (handle != null)
        {
            if (!isHeld)
            {
                handle.anchoredPosition = Vector2.Lerp(
                    handle.anchoredPosition, homeHandlePosition, amount);
                if ((handle.anchoredPosition - homeHandlePosition).sqrMagnitude < 0.01f)
                    handle.anchoredPosition = homeHandlePosition;
            }

            if (visualsCached)
            {
                Vector3 targetScale = homeHandleScale *
                    (isHeld ? PressedScaleMultiplier : 1f);
                handle.localScale = Vector3.Lerp(handle.localScale, targetScale, amount);
            }
        }

        if (handleImage != null && visualsCached)
        {
            Color targetColor = homeHandleColor;
            if (isHeld)
            {
                Color pressedColor = new Color(
                    homeHandleColor.r * 0.9f,
                    homeHandleColor.g * 0.94f,
                    homeHandleColor.b,
                    homeHandleColor.a);
                targetColor = pressedColor;
            }

            handleImage.color = Color.Lerp(handleImage.color, targetColor, amount);
        }
    }

    private void ReleasePointer()
    {
        SetHeld(false);
        activePointerId = 0;
        Input = Vector2.zero;
        RawMagnitude = 0f;
        SetSprintLit(false);
    }

    private void SetHeld(bool held)
    {
        if (isHeld == held)
            return;

        isHeld = held;
        if (!held) releasedFrame = Time.frameCount;
        activeJoystickCount = held
            ? activeJoystickCount + 1
            : Mathf.Max(0, activeJoystickCount - 1);
    }

    public void ResetJoystick()
    {
        SetHeld(false);
        activePointerId = 0;
        Input = Vector2.zero;
        RawMagnitude = 0f;
        SetSprintLit(false);

        if (handle != null && visualsCached)
        {
            handle.anchoredPosition = homeHandlePosition;
            handle.localScale = homeHandleScale;
        }

        if (handleImage != null && visualsCached)
            handleImage.color = homeHandleColor;
    }

    // =========================================================
    // T03: THE INDICATED SPRINT RING (adopted scenes only)
    // =========================================================

    private Image sprintRing;
    private bool sprintLit;
    private GameObject sprintMarker;
    private TraversalIcon sprintIcon;
    private TMPro.TextMeshProUGUI sprintLabel;
    private static Sprite ringSprite;
    private static readonly Color RingIdle = new Color(1f, 1f, 1f, 0.22f);
    private static readonly Color RingLit = new Color(1f, 0.86f, 0.45f, 0.75f);

    /// <summary>Shows the outer ring that marks where pushing the stick turns walking into sprinting.</summary>
    public void ShowSprintRing(bool show)
    {
        if (show && sprintRing == null && background != null)
        {
            var host = new GameObject("SprintRing", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            var rect = (RectTransform)host.transform;
            rect.SetParent(background, false);
            rect.SetAsFirstSibling();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = background.rect.center;
            Vector2 travel = GetTravelRadius(background.rect);
            float sprintDiameter = 2f * Mathf.Min(travel.x, travel.y) * ExplorationRules.SprintEnter;
            rect.sizeDelta = new Vector2(sprintDiameter, sprintDiameter);
            sprintRing = host.GetComponent<Image>();
            sprintRing.sprite = RingSprite();
            sprintRing.raycastTarget = false;
            sprintRing.color = sprintLit ? RingLit : RingIdle;
            sprintMarker = new GameObject("SprintMarker", typeof(RectTransform));
            var markerRect = (RectTransform)sprintMarker.transform;
            markerRect.SetParent(background, false);
            markerRect.anchorMin = markerRect.anchorMax = new Vector2(0.5f, 0.5f);
            markerRect.anchoredPosition = background.rect.center + Vector2.up * (Mathf.Min(travel.x, travel.y) * ExplorationRules.SprintTravelLimit + 38f);
            markerRect.sizeDelta = new Vector2(64f, 64f);
            sprintIcon = sprintMarker.AddComponent<TraversalIcon>();
            sprintIcon.Symbol = TraversalIcon.Kind.Sprint;
            sprintIcon.color = RingIdle;
            sprintIcon.raycastTarget = false;
            sprintLabel = HudTheme.CreateLabel("Pace", markerRect, "RUN", 22f, RingIdle, TMPro.TextAlignmentOptions.Center, TMPro.FontStyles.Bold);
            sprintLabel.raycastTarget = false;
            sprintLabel.rectTransform.anchorMin = sprintLabel.rectTransform.anchorMax = new Vector2(0.5f, 0f);
            sprintLabel.rectTransform.anchoredPosition = new Vector2(0f, -12f);
            sprintLabel.rectTransform.sizeDelta = new Vector2(100f, 28f);
        }
        if (sprintRing != null) sprintRing.gameObject.SetActive(show);
        if (sprintMarker != null) sprintMarker.SetActive(show);
    }

    /// <summary>Lights the ring while the held stick is sprinting.</summary>
    public void SetSprintLit(bool lit)
    {
        sprintLit = lit;
        if (sprintRing != null) sprintRing.color = lit ? RingLit : RingIdle;
        if (sprintIcon != null) sprintIcon.color = lit ? RingLit : new Color(1f, 1f, 1f, 0.65f);
        if (sprintLabel != null) { sprintLabel.color = lit ? RingLit : new Color(1f, 1f, 1f, 0.65f); sprintLabel.text = lit ? "RUNNING" : "RUN"; }
    }

    /// <summary>A soft ring at the stick's outer edge (the sprint zone), built once.</summary>
    private static Sprite RingSprite()
    {
        if (ringSprite != null) return ringSprite;
        const int size = 128;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { name = "SprintRing", wrapMode = TextureWrapMode.Clamp };
        var pixels = new Color32[size * size];
        float outer = size * 0.5f - 1f, inner = outer * 0.84f;
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float d = new Vector2(x - size * 0.5f + 0.5f, y - size * 0.5f + 0.5f).magnitude;
                float a = Mathf.Clamp01(outer - d) * Mathf.Clamp01(d - inner);
                pixels[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255f));
            }
        texture.SetPixels32(pixels);
        texture.Apply(false, true);
        ringSprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        return ringSprite;
    }

    public void Show()
    {
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        ResetJoystick();
        gameObject.SetActive(false);
    }

    public void SetVisible(bool visible)
    {
        if (visible)
            Show();
        else
            Hide();
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus)
            ResetJoystick();
    }

    private void OnApplicationPause(bool paused)
    {
        if (paused)
            ResetJoystick();
    }

    private void OnDisable()
    {
        ResetJoystick();
    }

    private void OnDestroy()
    {
        ResetJoystick();
    }
}
