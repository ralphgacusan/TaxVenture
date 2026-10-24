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
    private float collectionDuration = 0.6f;
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
        WorldPickupMotion.Fly(transform, collectionCamera, collectionStartScreenPoint, targetScreenPoint,
            collectionStartDepth, collectionTargetDepth, collectionStartRotation, collectionStartScale, progress);

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

        AudioManager.Instance?.QueueInteractionSFX("paper");
        Camera inputCamera = eventData != null ? eventData.pressEventCamera : null;
        collectionCamera = inputCamera != null ? inputCamera : Camera.main;
        if (collectionCamera == null || !hud.TryGetCollectionScreenPoint(out _))
        {
            CompleteCollection(hud);
            return;
        }

        Vector3 startScreenPoint = collectionCamera.WorldToScreenPoint(transform.position);
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
        collectionStartRotation = transform.rotation;
        collectionStartScale = transform.localScale;
        collectElapsed = 0f;
        isCollecting = true;
    }
    public TaxpayerProfile Profile => profileData;
}
