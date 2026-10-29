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
    private const float PickupSeconds = 0.08f;  // lifts to the finger, then follows it exactly (T28)
    private const float PinImpactLift = 0.015f;
    private const float PinImpactAngle = 4f;
    private const float PinHeadDiameter = 0.04f;
    // The pin-in (2026-10-26): the same for every card (no hint of right or wrong before the check).
    private const float SettleSeconds = 1.1f;     // damped swing on the pin
    private const float SettleSwingDegrees = 7f;
    private const float SquashSeconds = 0.32f;    // the card squashes and springs back
    private const float SquashAmount = 0.06f;
    private const float PinPopSeconds = 0.3f;     // the pin head pops in from in front
    private const float PinPopDrop = 0.05f;

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

    private Vector3 originalPosition, originalScale, pinPivotLocal, pinPivotOffset, dragTargetPosition, dragVelocity, lastDragTarget;
    private Quaternion originalRotation, dragStartRotation;
    private float dragStartedAt;
    private Vector3 transitionFromPosition, transitionTargetPosition, pinnedPosition;
    private Quaternion transitionFromRotation, transitionTargetRotation, pinnedRotation;
    private float transitionStartedAt, transitionDuration, placedAt;
    private Vector3 placedScale = Vector3.one;
    private Vector3 pinMarkerScale, pinMarkerRest;
    private bool settleAnimated;
    private Vector2 arrivalScreenPoint;
    private float arrivalElapsed, arrivalDelay, arrivalDuration;
    private float arrivalStartDepth, arrivalTargetDepth, arrivalStartScale;
    private Quaternion arrivalStartRotation;
    private Plane dragPlane;
    private Vector3 dragOffset;

    public TaxpayerProfile Profile => profile;
    /// <summary>True only after a board check marked this pinned card right (T06: never during drafting).</summary>
    public bool IsCorrectlyPlaced { get; private set; }

    // T06: the board's verdict after a check (null = not checked since it was last placed).
    private bool? checkedResult;
    // A card marked right by a check stays pinned while the others are corrected.
    private bool lockedCorrect;

    /// <summary>Pinned in an area (draft or checked).</summary>
    public bool IsPlaced => currentSlot != null;

    /// <summary>The area this card is pinned in (null in the tray).</summary>
    public CorkboardSlot CurrentSlot => currentSlot;

    /// <summary>Marked right by a check: stays pinned and its area takes no other card.</summary>
    public bool IsLockedCorrect => lockedCorrect;

    /// <summary>Being dragged or still moving to/from an area (the board waits before a check).</summary>
    public bool IsBeingMoved => state == MotionState.Dragging || state == MotionState.Arriving ||
                                state == MotionState.Pinning || state == MotionState.Returning;

    /// <summary>Worth pointing at: not placed yet, or marked wrong by the last check (never reveals a draft).</summary>
    public bool NeedsAttention => currentSlot == null || checkedResult == false;

    /// <summary>
    /// The board's check result for this pinned card (T06): right cards get the
    /// green frame and stay pinned; wrong ones the red frame and can be moved.
    /// No sound or reaction here; the board owns the check feedback.
    /// </summary>
    public void MarkChecked(bool correct)
    {
        if (currentSlot == null) return;
        checkedResult = correct;
        IsCorrectlyPlaced = correct;
        lockedCorrect = correct;
        SetColor(Color.Lerp(neutralColor, correct ? correctColor : wrongColor, 0.12f));
    }

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
        checkedResult = null;
        lockedCorrect = false;
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

    // 2026-10-27: a press only becomes a drag once the finger moves past the
    // drag threshold; a tap (press and release in place) turns the ID card
    // over instead, so reading a card never changes the draft.
    private bool pressPending;
    private int pressPointerId = NoPointer;

    /// <summary>A tap turned this card over (the guide's flip step listens).</summary>
    public static event System.Action<ProfileDragCard> Flipped;

    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left || state == MotionState.Pinning ||
            state == MotionState.Dragging || state == MotionState.Arriving || activePointerId != NoPointer ||
            (manager != null && !manager.CanInteract)) return;
        pressPending = true;
        pressPointerId = eventData.pointerId;
    }

    /// <summary>The press turned into a drag: pick the card up where the finger first went down.</summary>
    private bool TryBeginDrag(PointerEventData eventData)
    {
        if (IsLocked || lockedCorrect) return false;
        if (state == MotionState.Pinning || state == MotionState.Dragging || state == MotionState.Arriving || activePointerId != NoPointer ||
            (activeCard != null && activeCard != this) || (manager != null && !manager.CanInteract)) return false;

        if (mainCamera == null) mainCamera = Camera.main;
        if (mainCamera == null) return false;
        // A card read on its back turns to its ID again as it is picked up.
        CardFlip turn = ProfileCardSkin.FlipOf(this);
        if (turn != null && turn.ShowingBack) turn.Toggle();

        dragPlane = new Plane(mainCamera.transform.forward, transform.position);
        Ray ray = mainCamera.ScreenPointToRay(eventData.pressPosition);
        if (!dragPlane.Raycast(ray, out float enter)) return false;

        // Picked up mid pin-in: back to its own size at once.
        if (state == MotionState.Placed) transform.localScale = placedScale;
        activeCard = this;
        activePointerId = eventData.pointerId;
        dragOffset = transform.position - ray.GetPoint(enter);
        dragTargetPosition = transform.position - mainCamera.transform.forward * DragLift;
        lastDragTarget = dragTargetPosition;
        dragVelocity = Vector3.zero;
        dragStartRotation = transform.rotation;
        dragStartedAt = Time.unscaledTime;
        state = MotionState.Dragging;
        if (pointerHighlight != null) pointerHighlight.SetFocused(true);
        VacateCurrentSlot();
        HidePinMarker();

        if (AudioManager.Instance != null) AudioManager.Instance.PlayEffect("sfx_card_pickup_01");
        return true;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (pressPending && eventData.pointerId == pressPointerId)
        {
            pressPending = false;
            pressPointerId = NoPointer;
            if (!TryBeginDrag(eventData)) return;
        }
        if (state != MotionState.Dragging || eventData.pointerId != activePointerId) return;
        if (mainCamera == null)
        {
            ReturnToOrigin();
            return;
        }

        Ray ray = mainCamera.ScreenPointToRay(eventData.position);
        if (dragPlane.Raycast(ray, out float enter))
        {
            dragTargetPosition = ray.GetPoint(enter) + dragOffset - mainCamera.transform.forward * DragLift;
            // Once lifted it sits exactly under the finger this frame (no spring lag).
            if (PickedUp) transform.position = dragTargetPosition;
        }
    }

    private bool PickedUp => Time.unscaledTime - dragStartedAt >= PickupSeconds;

    public void OnPointerUp(PointerEventData eventData)
    {
        if (pressPending && eventData.pointerId == pressPointerId)
        {
            pressPending = false;
            pressPointerId = NoPointer;
            // A tap in place: read the other side (the board, the draft and the check are untouched).
            if (!eventData.dragging && (state == MotionState.Idle || state == MotionState.Placed) &&
                (manager == null || manager.CanInteract) && ProfileCardSkin.Flip(this))
                Flipped?.Invoke(this);
            return;
        }
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
        bool shouldPlayReturn = state != MotionState.Returning && state != MotionState.Idle;
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
        if (shouldPlayReturn && AudioManager.Instance != null)
            AudioManager.Instance.PlayEffect("sfx_card_return_01", 0.65f);
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
        // A short lift from where it was pressed to the finger (the press became a drag a few
        // pixels later), then exactly under the finger; settling happens after release.
        float pickup = Mathf.Clamp01((Time.unscaledTime - dragStartedAt) / PickupSeconds);
        transform.position = pickup >= 1f ? dragTargetPosition
            : Vector3.Lerp(transform.position, dragTargetPosition, Mathf.Max(pickup, 1f - Mathf.Exp(-dragSmoothness * dt)));

        // The finger's own speed gives the slight tilt (smoothed, so jitter never shakes it).
        dragVelocity = Vector3.Lerp(dragVelocity, (dragTargetPosition - lastDragTarget) / dt, 1f - Mathf.Exp(-14f * dt));
        lastDragTarget = dragTargetPosition;
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
        // Pinning speeds up into the board (a push), other moves ease in and out.
        float eased = state == MotionState.Pinning ? t * t * (1.6f - 0.6f * t) : t * t * (3f - 2f * t);
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

        // T06: a drop is only a draft; the neutral place sound, no verdict.
        CorkboardSlot target = pendingSlot;
        currentSlot = target;
        pendingSlot = null;
        IsCorrectlyPlaced = false;
        checkedResult = null;
        pinnedPosition = transitionTargetPosition;
        pinnedRotation = transitionTargetRotation;
        placedAt = Time.unscaledTime;
        placedScale = transform.localScale;
        settleAnimated = true;
        state = MotionState.Placed;
        SetColor(neutralColor);
        ShowPinMarker();
        ReleaseActiveCard();
        if (AudioManager.Instance != null) AudioManager.Instance.PlayEffect("sfx_card_place_01");
        manager.HandleCardDropped(this, target);
    }

    /// <summary>
    /// Resume (T04/T06): appears already pinned in its saved area as a neutral
    /// draft (a restored check is marked afterwards by the board), with no
    /// drop, sound or reaction.
    /// </summary>
    public void RestorePinned(CorkboardSlot slot, Vector3 position, Quaternion rotation)
    {
        if (!initialized || slot == null || profile == null) return;
        currentSlot = slot;
        pendingSlot = null;
        IsCorrectlyPlaced = false;
        checkedResult = null;
        lockedCorrect = false;
        transform.SetPositionAndRotation(position, rotation);
        pinnedPosition = position;
        pinnedRotation = rotation;
        placedAt = Time.unscaledTime;
        placedScale = transform.localScale;
        settleAnimated = false;   // a restore appears already settled (no pin-in motion)
        state = MotionState.Placed;
        SetColor(neutralColor);
        ShowPinMarker();
    }

    /// <summary>
    /// Pinned: the gentle idle sway, and just after a pin-in a short squash
    /// and spring, a damped swing on the pin and the pin head popping in.
    /// </summary>
    private void UpdateSway()
    {
        float t = Time.unscaledTime - placedAt;
        float sway = Mathf.Sin(t * (2f * Mathf.PI / 3.5f)) * 0.7f;
        float swing = settleAnimated && t < SettleSeconds
            ? Mathf.Sin(t * 15f) * SettleSwingDegrees * Mathf.Exp(-t * 4.5f) * (1f - t / SettleSeconds) : 0f;
        float squash = 1f;
        if (settleAnimated && t < SquashSeconds)
        {
            float k = t / SquashSeconds;
            squash = 1f - Mathf.Sin(k * Mathf.PI * 2f) * SquashAmount * (1f - k);
        }
        Quaternion rotation = pinnedRotation * Quaternion.AngleAxis(sway + swing, Vector3.forward);
        Vector3 pin = pinnedPosition + pinnedRotation * pinPivotOffset;
        transform.localScale = placedScale * squash;
        transform.SetPositionAndRotation(pin - rotation * (pinPivotOffset * squash), rotation);
        AnimatePinHead(t);
    }

    private void AnimatePinHead(float t)
    {
        if (pinMarker == null || !pinMarker.activeSelf) return;
        if (!settleAnimated || t >= PinPopSeconds)
        {
            pinMarker.transform.localScale = pinMarkerScale;
            pinMarker.transform.localPosition = pinMarkerRest;
            return;
        }
        float k = Mathf.Clamp01(t / PinPopSeconds);
        float pop = DeskMotion.EaseOutBack(k, 2.2f);
        pinMarker.transform.localScale = pinMarkerScale * Mathf.Max(0f, pop);
        Vector3 towardCamera = mainCamera != null ? -mainCamera.transform.forward : -transform.forward;
        pinMarker.transform.localPosition = pinMarkerRest + transform.InverseTransformVector(towardCamera * PinPopDrop * (1f - DeskMotion.EaseOutCubic(k)));
    }

    private void VacateCurrentSlot()
    {
        if (currentSlot == null) return;
        currentSlot.VacateSlot(this);
        currentSlot = null;
        IsCorrectlyPlaced = false;
        checkedResult = null;
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
        pinMarkerRest = pinMarker.transform.localPosition;
        pinMarkerScale = pinMarker.transform.localScale;
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
