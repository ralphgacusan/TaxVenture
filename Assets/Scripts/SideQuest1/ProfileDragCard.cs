using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class ProfileDragCard : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [Header("References")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private Renderer cardRenderer;

    [Header("Drag Settings")]
    [SerializeField] private float dragSmoothness = 20f;

    [Header("Feedback Colors")]
    [SerializeField] private Color neutralColor = Color.white;
    [SerializeField] private Color correctColor = Color.green;
    [SerializeField] private Color wrongColor = Color.red;

    [Header("Card Text")]
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text descriptionText;

    private const int NoPointer = int.MinValue;
    private const float PinningDuration = 0.25f;
    private const float ReturnDuration = 0.22f;
    private const float ArrivalDuration = 0.55f;
    private const float ArrivalStartScale = 0.14f;
    private const float DragLift = 0.035f;
    private const float DragTiltLimit = 5f;
    private const float PinImpactLift = 0.015f;
    private const float PinImpactAngle = 4f;
    private const float PinHeadDiameter = 0.04f;

    private enum MotionState { Idle, Dragging, Arriving, Pinning, Returning, Placed }

    private static ProfileDragCard activeCard;
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");

    private MaterialPropertyBlock materialBlock;
    private GameObject pinMarker;
    private Material pinMaterial;
    private TaxpayerProfile profile;
    private CorkboardSideQuestManager manager;
    private CorkboardSlot currentSlot;
    private CorkboardSlot pendingSlot;
    private MotionState state;
    private HighlightEffect pointerHighlight;
    private bool pointerHovered;
    private Vector2 pointerPosition;
    private int activePointerId = NoPointer;
    private bool initialized;
    private Collider[] arrivalColliders;
    private bool[] arrivalColliderStates;
    private bool arrivalCollidersDisabled;

    private Vector3 originalPosition, originalScale, pinPivotLocal, pinPivotOffset, dragTargetPosition, dragVelocity;
    private Quaternion originalRotation, dragStartRotation;
    private Vector3 transitionFromPosition, transitionTargetPosition, pinnedPosition;
    private Quaternion transitionFromRotation, transitionTargetRotation, pinnedRotation;
    private float transitionStartedAt, transitionDuration, placedAt;
    private Vector2 arrivalScreenPoint;
    private float arrivalElapsed, arrivalDelay, arrivalDuration;
    private float arrivalStartDepth, arrivalTargetDepth, arrivalStartScale;
    private Quaternion arrivalStartRotation;
    private Plane dragPlane;
    private Vector3 dragOffset;

    public TaxpayerProfile Profile => profile;
    public bool IsCorrectlyPlaced { get; private set; }

    private void Awake()
    {
        materialBlock = new MaterialPropertyBlock();
        pointerHighlight = HighlightEffect.For(this);
    }

    public void SetCardText(TaxpayerProfile data)
    {
        if (nameText != null) nameText.text = data.TaxpayerName;
        if (descriptionText != null) descriptionText.text = data.Description;
        ProfileCardTextLayout.Apply(nameText, descriptionText);
    }

    public void Initialize(TaxpayerProfile data, CorkboardSideQuestManager owningManager, Camera activeCamera)
    {
        profile = data;
        manager = owningManager;
        if (activeCamera != null) mainCamera = activeCamera;
        else if (mainCamera == null) mainCamera = Camera.main;

        originalPosition = transform.position;
        originalRotation = transform.rotation;
        originalScale = transform.localScale;
        FindPinPivot();
        currentSlot = pendingSlot = null;
        activePointerId = NoPointer;
        IsCorrectlyPlaced = false;
        state = MotionState.Idle;
        initialized = true;
        SetColor(neutralColor);
        SetCardText(data);
    }

    private void Update()
    {
        switch (state)
        {
            case MotionState.Dragging: UpdateDrag(); break;
            case MotionState.Arriving: UpdateArrival(); break;
            case MotionState.Pinning:
            case MotionState.Returning: UpdateTransition(); break;
            case MotionState.Placed: UpdateSway(); break;
        }

        RefreshPointerHighlight();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        pointerHovered = eventData != null && !DialogueUI.IsAnyDialogueActive &&
                         !VirtualJoystick.BlocksWorldInput &&
                         !MobileInputGuard.IsPointerOverUI(eventData.position);
        if (eventData != null) pointerPosition = eventData.position;
        RefreshPointerHighlight();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        pointerHovered = false;
        RefreshPointerHighlight();
    }

    private void RefreshPointerHighlight()
    {
        bool dragging = state == MotionState.Dragging;
        if (dragging)
        {
            if (pointerHighlight != null) pointerHighlight.SetFocused(true);
            return;
        }

        bool canHover = initialized && (state == MotionState.Idle || state == MotionState.Placed) &&
                        (manager == null || manager.CanInteract);
        if (!pointerHovered || !canHover)
        {
            if (pointerHighlight != null) pointerHighlight.SetFocused(false);
            return;
        }

        bool pointerBlocked = DialogueUI.IsAnyDialogueActive || VirtualJoystick.BlocksWorldInput ||
                              MobileInputGuard.IsPointerOverUI(pointerPosition);
        if (pointerHighlight != null)
        {
            pointerHighlight.SetFocused(!pointerBlocked);
        }
    }

    /// <summary>Time (unscaled) until which this card can't be picked up (after repeated misses).</summary>
    public float LockedUntil { get; set; }

    public bool IsLocked => Time.unscaledTime < LockedUntil;

    public void OnPointerDown(PointerEventData eventData)
    {
        if (IsLocked) return;
        if (eventData.button != PointerEventData.InputButton.Left || state == MotionState.Pinning ||
            state == MotionState.Dragging || state == MotionState.Arriving || activePointerId != NoPointer ||
            (activeCard != null && activeCard != this) || (manager != null && !manager.CanInteract)) return;

        if (mainCamera == null) mainCamera = Camera.main;
        if (mainCamera == null) return;

        dragPlane = new Plane(mainCamera.transform.forward, transform.position);
        Ray ray = mainCamera.ScreenPointToRay(eventData.position);
        if (!dragPlane.Raycast(ray, out float enter)) return;

        activeCard = this;
        activePointerId = eventData.pointerId;
        dragOffset = transform.position - ray.GetPoint(enter);
        dragTargetPosition = transform.position - mainCamera.transform.forward * DragLift;
        dragVelocity = Vector3.zero;
        dragStartRotation = transform.rotation;
        state = MotionState.Dragging;
        if (pointerHighlight != null) pointerHighlight.SetFocused(true);
        VacateCurrentSlot();
        HidePinMarker();

        if (AudioManager.Instance != null) AudioManager.Instance.PlayPaperSFX();
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (state != MotionState.Dragging || eventData.pointerId != activePointerId) return;
        if (mainCamera == null)
        {
            ReturnToOrigin();
            return;
        }

        Ray ray = mainCamera.ScreenPointToRay(eventData.position);
        if (dragPlane.Raycast(ray, out float enter))
            dragTargetPosition = ray.GetPoint(enter) + dragOffset - mainCamera.transform.forward * DragLift;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (state != MotionState.Dragging || eventData.pointerId != activePointerId) return;
        activePointerId = NoPointer;

        if (mainCamera != null && manager != null)
        {
            Ray releaseRay = mainCamera.ScreenPointToRay(eventData.position);
            // Preserve the grab offset using a camera ray through the intended card center.
            if (dragPlane.Raycast(releaseRay, out float enter))
            {
                Vector3 center = releaseRay.GetPoint(enter) + dragOffset;
                releaseRay = mainCamera.ScreenPointToRay(mainCamera.WorldToScreenPoint(center));
            }
            if (manager.TryGetDrop(releaseRay, transform, out CorkboardSlot slot, out Vector3 position, out Quaternion rotation))
            {
                pendingSlot = slot;
                BeginTransition(MotionState.Pinning, position, rotation, PinningDuration);
                return;
            }
        }

        ReturnToOrigin();
    }

    public void ReturnToOrigin()
    {
        if (state == MotionState.Arriving)
        {
            RestoreArrivalColliders();
            transform.localScale = originalScale;
        }
        HidePinMarker();
        VacateCurrentSlot();
        pendingSlot = null;
        IsCorrectlyPlaced = false;
        SetColor(neutralColor);
        BeginTransition(MotionState.Returning, originalPosition, originalRotation, ReturnDuration);
    }

    public void AnimateFromHud(Vector2 screenPoint, float delay)
    {
        if (!initialized || mainCamera == null || state != MotionState.Idle)
        {
            return;
        }

        Vector3 targetScreen = mainCamera.WorldToScreenPoint(originalPosition);
        arrivalStartDepth = mainCamera.nearClipPlane + 0.4f;
        if (targetScreen.z <= mainCamera.nearClipPlane || arrivalStartDepth <= 0f)
        {
            return;
        }

        arrivalScreenPoint = screenPoint;
        arrivalTargetDepth = targetScreen.z;
        arrivalDelay = Mathf.Max(0f, delay);
        arrivalDuration = ArrivalDuration;
        arrivalElapsed = 0f;
        arrivalStartRotation = mainCamera.transform.rotation;
        arrivalStartScale = ArrivalStartScale;
        if (!mainCamera.orthographic)
        {
            arrivalStartScale *= arrivalStartDepth / arrivalTargetDepth;
        }

        transform.position = mainCamera.ScreenToWorldPoint(
            new Vector3(screenPoint.x, screenPoint.y, arrivalStartDepth));
        transform.rotation = arrivalStartRotation;
        transform.localScale = originalScale * arrivalStartScale;
        SetArrivalCollidersDisabled();
        state = MotionState.Arriving;
    }

    private void UpdateArrival()
    {
        if (mainCamera == null)
        {
            FinishArrival();
            return;
        }

        arrivalElapsed += Time.unscaledDeltaTime;
        if (arrivalElapsed < arrivalDelay)
        {
            return;
        }

        float t = Mathf.Clamp01((arrivalElapsed - arrivalDelay) / arrivalDuration);
        float eased = t * t * (3f - 2f * t);
        Vector3 targetScreen = mainCamera.WorldToScreenPoint(originalPosition);
        Vector2 screen = Vector2.Lerp(arrivalScreenPoint, new Vector2(targetScreen.x, targetScreen.y), eased);
        float depth = Mathf.Lerp(arrivalStartDepth, arrivalTargetDepth, eased);
        Vector3 position = mainCamera.ScreenToWorldPoint(new Vector3(screen.x, screen.y, depth));
        float arc = Mathf.Sin(t * Mathf.PI);
        float flutter = Mathf.Sin(Time.unscaledTime * 18f) * 0.018f * arc;
        position += mainCamera.transform.up * (0.12f * arc + flutter);

        transform.position = position;
        transform.rotation = Quaternion.Slerp(arrivalStartRotation, originalRotation, eased) *
            Quaternion.AngleAxis(Mathf.Sin(Time.unscaledTime * 15f) * 2f * arc, Vector3.forward);
        float depthScale = mainCamera.orthographic ? 1f : depth / arrivalTargetDepth;
        transform.localScale = originalScale * Mathf.Lerp(ArrivalStartScale, 1f, eased) * depthScale;

        if (t < 1f)
        {
            return;
        }

        FinishArrival();
    }

    private void FinishArrival()
    {
        transform.SetPositionAndRotation(originalPosition, originalRotation);
        transform.localScale = originalScale;
        RestoreArrivalColliders();
        arrivalDelay = arrivalElapsed = 0f;
        state = MotionState.Idle;
    }

    private void SetArrivalCollidersDisabled()
    {
        arrivalColliders = GetComponentsInChildren<Collider>(true);
        arrivalColliderStates = new bool[arrivalColliders.Length];
        for (int i = 0; i < arrivalColliders.Length; i++)
        {
            if (arrivalColliders[i] == null) continue;
            arrivalColliderStates[i] = arrivalColliders[i].enabled;
            if (arrivalColliderStates[i]) arrivalColliders[i].enabled = false;
        }
        arrivalCollidersDisabled = arrivalColliders.Length > 0;
    }

    private void RestoreArrivalColliders()
    {
        if (!arrivalCollidersDisabled) return;
        for (int i = 0; i < arrivalColliders.Length; i++)
        {
            if (arrivalColliders[i] != null) arrivalColliders[i].enabled = arrivalColliderStates[i];
        }
        arrivalCollidersDisabled = false;
        arrivalColliders = null;
        arrivalColliderStates = null;
    }

    private void UpdateDrag()
    {
        float dt = Time.unscaledDeltaTime;
        if (mainCamera == null)
        {
            ReturnToOrigin();
            return;
        }
        if (dt <= 0f) return;
        transform.position = Vector3.SmoothDamp(transform.position, dragTargetPosition, ref dragVelocity,
            1f / Mathf.Max(0.01f, dragSmoothness), Mathf.Infinity, dt);

        float speed = Vector3.Dot(dragVelocity, mainCamera.transform.right);
        float tilt = Mathf.Clamp(-speed * 1.5f, -DragTiltLimit, DragTiltLimit);
        Quaternion targetRotation = dragStartRotation * Quaternion.Euler(0f, 0f, tilt);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 1f - Mathf.Exp(-12f * dt));
    }

    private void BeginTransition(MotionState transition, Vector3 position, Quaternion rotation, float duration)
    {
        activePointerId = NoPointer;
        if (activeCard == null || activeCard == this) activeCard = this;
        transitionFromPosition = transform.position;
        transitionFromRotation = transform.rotation;
        transitionTargetPosition = position;
        transitionTargetRotation = rotation;
        transitionStartedAt = Time.unscaledTime;
        transitionDuration = duration;
        state = transition;
        RefreshPointerHighlight();
    }

    private void UpdateTransition()
    {
        float t = Mathf.Clamp01((Time.unscaledTime - transitionStartedAt) / transitionDuration);
        float eased = t * t * (3f - 2f * t);
        float impact = Mathf.Sin(t * Mathf.PI);
        transform.position = Vector3.Lerp(transitionFromPosition, transitionTargetPosition, eased);
        if (state == MotionState.Pinning && mainCamera != null)
            transform.position -= mainCamera.transform.forward * (PinImpactLift * impact);
        transform.rotation = Quaternion.Slerp(transitionFromRotation, transitionTargetRotation, eased) *
            Quaternion.AngleAxis((state == MotionState.Pinning ? PinImpactAngle : 1.5f) * impact, Vector3.forward);

        if (t < 1f) return;
        transform.SetPositionAndRotation(transitionTargetPosition, transitionTargetRotation);
        if (state == MotionState.Returning)
        {
            state = MotionState.Idle;
            ReleaseActiveCard();
            return;
        }

        if (pendingSlot == null || manager == null || !manager.CanInteract)
        {
            ReturnToOrigin();
            return;
        }

        bool correct = manager.HandleCardDropped(this, pendingSlot);
        if (AudioManager.Instance != null) AudioManager.Instance.PlayPaperSFX();
        currentSlot = pendingSlot;
        pendingSlot = null;
        IsCorrectlyPlaced = correct;
        pinnedPosition = transitionTargetPosition;
        pinnedRotation = transitionTargetRotation;
        placedAt = Time.unscaledTime;
        state = MotionState.Placed;
        SetColor(Color.Lerp(neutralColor, correct ? correctColor : wrongColor, 0.12f));
        ShowPinMarker();
        ReleaseActiveCard();
    }

    private void UpdateSway()
    {
        float sway = Mathf.Sin((Time.unscaledTime - placedAt) * (2f * Mathf.PI / 3.5f)) * 0.7f;
        Quaternion rotation = pinnedRotation * Quaternion.AngleAxis(sway, Vector3.forward);
        Vector3 pin = pinnedPosition + pinnedRotation * pinPivotOffset;
        transform.SetPositionAndRotation(pin - rotation * pinPivotOffset, rotation);
    }

    private void VacateCurrentSlot()
    {
        if (currentSlot == null) return;
        currentSlot.VacateSlot(this);
        currentSlot = null;
        IsCorrectlyPlaced = false;
        SetColor(neutralColor);
        manager?.NotifyCardRemovedFromSlot();
    }

    private void FindPinPivot()
    {
        pinPivotLocal = new Vector3(0f, 0.5f, 0f);
        if (cardRenderer != null)
        {
            Bounds bounds = cardRenderer.localBounds;
            Vector3 top = new Vector3(bounds.center.x, bounds.max.y, bounds.center.z);
            pinPivotLocal = transform.InverseTransformPoint(cardRenderer.transform.TransformPoint(top));
        }

        Vector3 worldOffset = transform.TransformPoint(pinPivotLocal) - transform.position;
        pinPivotOffset = Quaternion.Inverse(transform.rotation) * worldOffset;
    }

    private void ShowPinMarker()
    {
        if (pinMarker == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ??
                            Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) return;

            pinMarker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            pinMarker.name = "Profile Pin";
            Collider markerCollider = pinMarker.GetComponent<Collider>();
            if (markerCollider != null)
            {
                markerCollider.enabled = false;
                Destroy(markerCollider);
            }

            pinMaterial = new Material(shader);
            pinMaterial.color = new Color(0.62f, 0.19f, 0.16f);
            Renderer markerRenderer = pinMarker.GetComponent<Renderer>();
            markerRenderer.sharedMaterial = pinMaterial;
            markerRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            markerRenderer.receiveShadows = false;
            pinMarker.transform.SetParent(transform, false);
            pinMarker.transform.localScale = new Vector3(
                PinHeadDiameter / Mathf.Max(0.001f, Mathf.Abs(transform.lossyScale.x)),
                PinHeadDiameter / Mathf.Max(0.001f, Mathf.Abs(transform.lossyScale.y)),
                PinHeadDiameter / Mathf.Max(0.001f, Mathf.Abs(transform.lossyScale.z)));
        }

        Vector3 towardCamera = mainCamera != null ? -mainCamera.transform.forward : -transform.forward;
        pinMarker.transform.localPosition = pinPivotLocal +
            transform.InverseTransformVector(towardCamera * 0.008f);
        pinMarker.transform.localRotation = Quaternion.identity;
        pinMarker.SetActive(true);
    }

    private void HidePinMarker()
    {
        if (pinMarker != null) pinMarker.SetActive(false);
    }

    private static bool ApproximatelyEqual(Color a, Color b) => ColorDistance(a, b) < 0.001f;

    private static float ColorDistance(Color a, Color b) =>
        Mathf.Abs(a.r - b.r) + Mathf.Abs(a.g - b.g) + Mathf.Abs(a.b - b.b);

    private void SetColor(Color color)
    {
        // Office HUD card skin: neutral = no frame, otherwise a clear green
        // (right area) or red (wrong area) frame.
        ProfileCardSkin.SetResult(this, ApproximatelyEqual(color, neutralColor)
            ? (bool?)null
            : ColorDistance(color, correctColor) <= ColorDistance(color, wrongColor));
        if (cardRenderer == null) return;
        // Also supports editor validation, which can initialize cards before Awake.
        if (materialBlock == null) materialBlock = new MaterialPropertyBlock();
        cardRenderer.GetPropertyBlock(materialBlock);
        materialBlock.SetColor(BaseColorId, color);
        materialBlock.SetColor(ColorId, color);
        cardRenderer.SetPropertyBlock(materialBlock);
    }

    private void ReleaseActiveCard()
    {
        if (activeCard == this) activeCard = null;
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus && state == MotionState.Dragging) ReturnToOrigin();
    }

    private void OnApplicationPause(bool paused)
    {
        if (paused && state == MotionState.Dragging) ReturnToOrigin();
    }

    private void OnDisable()
    {
        pointerHovered = false;
        if (pointerHighlight != null) pointerHighlight.SetFocused(false);
        activePointerId = NoPointer;
        ReleaseActiveCard();
        RestoreArrivalColliders();
        VacateCurrentSlot();
        HidePinMarker();
        if (initialized)
        {
            transform.SetPositionAndRotation(originalPosition, originalRotation);
            transform.localScale = originalScale;
            SetColor(neutralColor);
        }
        pendingSlot = null;
        arrivalElapsed = arrivalDelay = arrivalDuration = 0f;
        state = MotionState.Idle;
    }

    private void OnDestroy()
    {
        ReleaseActiveCard();
        if (pinMaterial != null) Destroy(pinMaterial);
    }
}
