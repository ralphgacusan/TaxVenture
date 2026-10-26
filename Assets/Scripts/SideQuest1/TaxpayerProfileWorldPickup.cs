using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// PURPOSE:
/// The physical "1-page paper" taxpayer profile that appears beside an
/// NPC once their conversation ends. Clicking it collects it into the
/// TaxpayerProfileCollectionHUD, the same way FolderToHUDIcon collects
/// the case folder — just click instead of drag, since this is a single
/// small pickup rather than a draggable object.
///
/// Place one of these inactive in the scene beside each of the 4 NPCs;
/// TaxpayerNpcInteractable activates and initializes it after dialogue.
/// </summary>
public class TaxpayerProfileWorldPickup : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("Idle Visual (optional)")]
    [SerializeField] private float bobHeight = 0.05f;
    [SerializeField] private float bobSpeed = 2f;


    [Header("Card Text (match ProfileCard_TEMP layout)")]
    [SerializeField] private TMPro.TMP_Text nameText;
    [SerializeField] private TMPro.TMP_Text descriptionText;

    private TaxpayerProfile profileData;
    private bool collected;
    private Vector3 basePosition;
    private Quaternion baseRotation;
    private Vector3 baseScale;
    private bool hasCachedPose;
    private bool isDropping;
    private float dropElapsed;
    private float dropDuration = 0.65f;
    private Vector3 dropStartPosition;
    private bool isCollecting;
    private float collectElapsed;
    // T11: lift about 8 cm and turn to face the camera (first quarter), then glide into the binder; 0.45 s in all.
    private float collectionDuration = 0.45f;
    private const float LiftShare = 0.25f;
    private const float LiftHeight = 0.08f;
    private Vector3 liftFromPosition;
    private Quaternion liftFromRotation;
    private Vector3 liftedPosition;
    private TaxpayerProfileCollectionHUD collectionHUD;
    private Camera collectionCamera;
    private Vector2 collectionStartScreenPoint;
    private float collectionStartDepth;
    private float collectionTargetDepth;
    private Quaternion collectionStartRotation;
    private Vector3 collectionStartScale;
    private bool missingHudLogged;
    private HighlightEffect pointerHighlight;
    private bool pointerHovered;
    private Vector2 pointerPosition;

    public bool IsDropping => isDropping;
    public bool IsCollecting => isCollecting;

    private void Awake()
    {
        pointerHighlight = HighlightEffect.For(this);
    }

    public void Initialize(TaxpayerProfile data)
    {
        CancelCollectionAndRestoreTarget();
        StopDropAndRestoreTarget();
        CacheBasePose();

        profileData = data;
        collected = false;

        if (nameText != null)
        {
            nameText.text = data.TaxpayerName;
        }

        if (descriptionText != null)
        {
            descriptionText.text = data.Description;
        }

        ProfileCardTextLayout.Apply(nameText, descriptionText);
    }

    public void DropFrom(Vector3 source)
    {
        if (profileData == null)
        {
            return;
        }

        ClearPointerFocus();
        CacheBasePose();

        dropStartPosition = source;
        dropElapsed = 0f;
        isDropping = true;
        transform.SetPositionAndRotation(source, baseRotation);
    }

    private void Update()
    {
        RefreshPointerHighlight();

        if (collected)
        {
            return;
        }

        if (isCollecting)
        {
            UpdateCollection();
            return;
        }

        if (isDropping)
        {
            UpdateDrop();
            return;
        }

        ApplyIdleBob();
    }

    private void UpdateDrop()
    {
        dropElapsed += Time.deltaTime;
        float progress = Mathf.Clamp01(dropElapsed / dropDuration);
        Vector3 landingPosition = WorldPickupMotion.Hover(basePosition, Time.time, bobSpeed, bobHeight);
        // The pop-up motion is shared with Marie's Client Copy (WorldPickupMotion, T08).
        WorldPickupMotion.Drop(transform, dropStartPosition, basePosition, baseRotation, landingPosition, progress);

        if (progress >= 1f)
        {
            isDropping = false;
            dropElapsed = 0f;
            transform.SetPositionAndRotation(landingPosition, baseRotation);
        }
    }

    private void ApplyIdleBob()
    {
        transform.position = WorldPickupMotion.Hover(basePosition, Time.time, bobSpeed, bobHeight);
    }

    private void StopDropAndRestoreTarget()
    {
        isDropping = false;
        dropElapsed = 0f;
        if (hasCachedPose)
        {
            transform.SetPositionAndRotation(basePosition, baseRotation);
            transform.localScale = baseScale;
        }
    }

    private void CacheBasePose()
    {
        if (hasCachedPose)
        {
            return;
        }

        basePosition = transform.position;
        baseRotation = transform.rotation;
        baseScale = transform.localScale;
        hasCachedPose = true;
    }

    private void UpdateCollection()
    {
        if (collectionHUD == null)
        {
            CancelCollectionAndRestoreTarget();
            LogMissingHUD();
            return;
        }

        if (collectionCamera == null)
        {
            collectionCamera = Camera.main;
            if (collectionCamera == null)
            {
                CompleteCollection(collectionHUD);
                return;
            }
        }

        if (!collectionHUD.TryGetCollectionScreenPoint(out Vector2 targetScreenPoint))
        {
            CompleteCollection(collectionHUD);
            return;
        }

        collectElapsed += Time.deltaTime;
        float progress = Mathf.Clamp01(collectElapsed / collectionDuration);
        if (progress < LiftShare)
        {
            // Tip toward the camera and lift off the spot.
            float lift = WorldPickupMotion.Smooth(progress / LiftShare);
            transform.SetPositionAndRotation(Vector3.Lerp(liftFromPosition, liftedPosition, lift),
                Quaternion.Slerp(liftFromRotation, collectionStartRotation, lift));
            return;
        }
        WorldPickupMotion.Fly(transform, collectionCamera, collectionStartScreenPoint, targetScreenPoint,
            collectionStartDepth, collectionTargetDepth, collectionStartRotation, collectionStartScale, (progress - LiftShare) / (1f - LiftShare));

        if (progress >= 1f)
        {
            CompleteCollection(collectionHUD);
        }
    }

    private void CancelCollectionAndRestoreTarget()
    {
        if (!isCollecting)
        {
            return;
        }

        isCollecting = false;
        collectElapsed = 0f;
        collectionHUD = null;
        collectionCamera = null;
        if (hasCachedPose)
        {
            transform.SetPositionAndRotation(basePosition, baseRotation);
            transform.localScale = baseScale;
        }
    }

    private void CompleteCollection(TaxpayerProfileCollectionHUD hud)
    {
        if (collected || profileData == null || hud == null)
        {
            return;
        }

        collected = true;
        isCollecting = false;
        ClearPointerFocus();
        collectElapsed = 0f;
        collectionHUD = null;
        collectionCamera = null;

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayEffect("sfx_profile_collected_01");
        }

        hud.CollectProfile(profileData);
        gameObject.SetActive(false);
    }

    private void LogMissingHUD()
    {
        if (missingHudLogged)
        {
            return;
        }

        missingHudLogged = true;
        Debug.LogError("[TaxpayerProfileWorldPickup] Cannot collect this profile because TaxpayerProfileCollectionHUD.Instance is missing.", this);
    }

    private void OnDisable()
    {
        ClearPointerFocus();
        CancelCollectionAndRestoreTarget();
        StopDropAndRestoreTarget();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (eventData == null)
        {
            ClearPointerFocus();
            return;
        }

        pointerPosition = eventData.position;
        pointerHovered = !DialogueUI.IsAnyDialogueActive && !VirtualJoystick.BlocksWorldInput &&
                         !MobileInputGuard.IsPointerOverUI(eventData.position);
        RefreshPointerHighlight();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        ClearPointerFocus();
    }

    private void RefreshPointerHighlight()
    {
        bool focused = pointerHovered && profileData != null && !collected && !isDropping && !isCollecting &&
                       !DialogueUI.IsAnyDialogueActive && !VirtualJoystick.BlocksWorldInput &&
                       !MobileInputGuard.IsPointerOverUI(pointerPosition);
        if (pointerHighlight != null) pointerHighlight.SetFocused(focused);
    }

    private void ClearPointerFocus()
    {
        pointerHovered = false;
        if (pointerHighlight != null) pointerHighlight.SetFocused(false);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData != null && (eventData.button != PointerEventData.InputButton.Left || MobileInputGuard.IsDragOrCancelled(eventData) || VirtualJoystick.BlocksWorldInput ||
            MobileInputGuard.IsPointerOverUI(eventData.position))) return;
        // A second tap while it flies lands it at once.
        if (isCollecting && collectionHUD != null)
        {
            CompleteCollection(collectionHUD);
            return;
        }
        if (collected || isCollecting || isDropping || profileData == null)
        {
            return;
        }

        // Only within reach (taps through the EventSystem have no distance limit).
        if (eventData != null && !InteractionRange.PlayerCanReach(this)) return;

        TaxpayerProfileCollectionHUD hud = TaxpayerProfileCollectionHUD.Instance;
        if (hud == null)
        {
            LogMissingHUD();
            return;
        }

        // T11: the card is committed to the campaign first (claim + Profile Binder, one save).
        // A failed save leaves it here, touchable, with no sound or motion; it can be tapped again.
        if (CollectGate != null && !CollectGate(profileData)) return;

        AudioManager.Instance?.QueueInteractionSFX("paper");
        Camera inputCamera = eventData != null ? eventData.pressEventCamera : null;
        collectionCamera = inputCamera != null ? inputCamera : Camera.main;
        if (collectionCamera == null || !hud.TryGetCollectionScreenPoint(out _))
        {
            CompleteCollection(hud);
            return;
        }

        liftFromPosition = transform.position;
        liftFromRotation = transform.rotation;
        liftedPosition = liftFromPosition + Vector3.up * LiftHeight;
        Vector3 startScreenPoint = collectionCamera.WorldToScreenPoint(liftedPosition);
        if (startScreenPoint.z <= collectionCamera.nearClipPlane)
        {
            CompleteCollection(hud);
            return;
        }

        collectionHUD = hud;
        ClearPointerFocus();
        collectionStartScreenPoint = new Vector2(startScreenPoint.x, startScreenPoint.y);
        collectionStartDepth = startScreenPoint.z;
        collectionTargetDepth = WorldPickupMotion.FlightEndDepth(collectionCamera, collectionStartDepth);
        // Face-forward at the end of the lift: the same camera-facing pose the flight ends in.
        collectionStartRotation = Quaternion.Slerp(liftFromRotation, collectionCamera.transform.rotation, 0.6f);
        collectionStartScale = transform.localScale;
        collectElapsed = 0f;
        isCollecting = true;
    }
    public TaxpayerProfile Profile => profileData;

    /// <summary>
    /// T11 (Level 1 hotbar): asked before a tapped card flies. True = it is
    /// committed to the campaign (or this run keeps no campaign record);
    /// false = the save failed and the card stays where it is.
    /// </summary>
    public static System.Func<TaxpayerProfile, bool> CollectGate;
}
