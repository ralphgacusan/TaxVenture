using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Dual-purpose HUD icon.
///
/// QUICK CLICK:
///     Closed window -> Open centered
///     Open window   -> Close
///
/// DRAG:
///     Press and drag -> Creates ghost icon.
///
///     If released on the assigned submission target:
///         Submit the associated DataValue.
///
///     If released anywhere else:
///         Open the floating window at the DROP POSITION.
///
/// IMPORTANT:
/// This component is the SINGLE pointer controller for the HUD icon.
///
/// DO NOT add FloatingWindowIcon to the same GameObject.
///
/// Designed for:
/// - Case Folder
/// - Tax Return
/// - Other draggable HUD documents
/// </summary>
public class HudSubmittableIcon :
    MonoBehaviour,
    IDataValueSource,
    IPointerDownHandler,
    IDragHandler,
    IPointerUpHandler
{
    // =========================================================
    // IDENTITY
    // =========================================================

    public enum SubmittableKind
    {
        CaseFolder,
        TaxReturn
    }

    [Header("Identity")]
    [SerializeField] private SubmittableKind kind;


    // =========================================================
    // FLOATING WINDOW
    // =========================================================

    [Header("Floating Window")]

    [Tooltip(
        "FloatingWindow controlled by this icon. " +
        "A normal click toggles the window. " +
        "Dragging the icon and releasing elsewhere opens the " +
        "window at the release position."
    )]
    [SerializeField] private FloatingWindow floatingWindow;


    // =========================================================
    // SUBMISSION TARGET
    // =========================================================

    [Header("Submission Target")]

    [Tooltip(
        "The ONLY object that can receive this dragged value. " +
        "Dragging will only submit when the pointer is actually " +
        "over this specific target."
    )]
    [SerializeField] private MonoBehaviour submissionTarget;


    // =========================================================
    // DRAG SETTINGS
    // =========================================================

    [Header("Drag Behavior")]

    [Tooltip(
        "Pointer movement required before a click becomes a drag."
    )]
    [SerializeField] private float dragThresholdPixels = 12f;

    [Tooltip(
        "Ghost icon prefab displayed while dragging."
    )]
    [SerializeField] private DragGhostIcon ghostPrefab;

    [Tooltip(
        "Sprite displayed by the drag ghost."
    )]
    [SerializeField] private Sprite ghostSprite;

    [Tooltip(
        "Canvas used for the drag ghost."
    )]
    [SerializeField] private Canvas rootCanvas;


    // =========================================================
    // AVAILABILITY
    // =========================================================

    [Header("Availability")]

    [Tooltip(
        "When disabled, the icon cannot be clicked or dragged."
    )]
    [SerializeField] private bool isAvailable = false;


    // =========================================================
    // INTERNAL STATE
    // =========================================================

    private Vector2 pointerDownScreenPos;

    private bool pointerIsDown;

    private bool isDragging;

    private DragGhostIcon activeGhost;

    private Image iconImage;


    // =========================================================
    // UNITY
    // =========================================================

    private void Awake()
    {
        iconImage =
            GetComponent<Image>();

        UpdateRaycastState();

        ValidateSubmissionTarget();
    }


    private void OnDisable()
    {
        DestroyGhost();

        pointerIsDown = false;

        isDragging = false;

        pointerDownScreenPos =
            Vector2.zero;
    }


    // =========================================================
    // VALIDATE TARGET
    // =========================================================

    private void ValidateSubmissionTarget()
    {
        if (submissionTarget == null)
        {
            Debug.LogWarning(
                $"[HudSubmittableIcon] {name}: " +
                "No Submission Target assigned. " +
                "Dragging will not submit."
            );

            return;
        }


        if (!(submissionTarget is IDataValueDestination))
        {
            Debug.LogError(
                $"[HudSubmittableIcon] {name}: " +
                $"Submission Target '{submissionTarget.name}' " +
                "does NOT implement IDataValueDestination!"
            );
        }
        else
        {
            Debug.Log(
                $"[HudSubmittableIcon] {name}: " +
                $"Submission Target = {submissionTarget.name}"
            );
        }
    }


    // =========================================================
    // AVAILABILITY
    // =========================================================

    public void SetAvailable(
        bool available
    )
    {
        Debug.Log(
            $"[HudSubmittableIcon] " +
            $"{name}: SetAvailable({available})"
        );

        isAvailable =
            available;

        UpdateRaycastState();
    }


    private void UpdateRaycastState()
    {
        if (iconImage != null)
        {
            iconImage.raycastTarget =
                isAvailable;
        }
    }


    // =========================================================
    // POINTER DOWN
    // =========================================================

    public void OnPointerDown(
        PointerEventData eventData
    )
    {
        if (!isAvailable)
            return;


        pointerIsDown =
            true;

        isDragging =
            false;

        pointerDownScreenPos =
            eventData.position;


        Debug.Log(
            $"[HudSubmittableIcon] " +
            $"{name}: POINTER DOWN"
        );
    }


    // =========================================================
    // DRAG
    // =========================================================

    public void OnDrag(
        PointerEventData eventData
    )
    {
        if (!isAvailable ||
            !pointerIsDown)
        {
            return;
        }


        float moved =
            Vector2.Distance(
                pointerDownScreenPos,
                eventData.position
            );


        // -----------------------------------------------------
        // CLICK -> DRAG
        // -----------------------------------------------------

        if (!isDragging &&
            moved >= dragThresholdPixels)
        {
            BeginDrag(
                eventData
            );
        }


        // -----------------------------------------------------
        // FOLLOW GHOST
        // -----------------------------------------------------

        if (isDragging &&
            activeGhost != null)
        {
            activeGhost.FollowPointer(
                eventData.position,
                rootCanvas
            );
        }
    }


    // =========================================================
    // BEGIN DRAG
    // =========================================================

    private void BeginDrag(
        PointerEventData eventData
    )
    {
        isDragging =
            true;


        Debug.Log(
            $"[HudSubmittableIcon] " +
            $"{name}: BEGIN DRAG"
        );


        // -----------------------------------------------------
        // CREATE GHOST
        // -----------------------------------------------------

        if (ghostPrefab != null &&
            rootCanvas != null)
        {
            activeGhost =
                Instantiate(
                    ghostPrefab,
                    rootCanvas.transform
                );


            activeGhost.SetSprite(
                ghostSprite
            );


            activeGhost.FollowPointer(
                eventData.position,
                rootCanvas
            );
        }


        // -----------------------------------------------------
        // SELECT DATA VALUE
        // -----------------------------------------------------

        if (ValueTransferManager.Instance != null)
        {
            ValueTransferManager.Instance.SelectValue(
                this,
                this
            );
        }
        else
        {
            Debug.LogError(
                "[HudSubmittableIcon] " +
                "ValueTransferManager.Instance is NULL!"
            );
        }
    }


    // =========================================================
    // POINTER UP
    // =========================================================

    public void OnPointerUp(
        PointerEventData eventData
    )
    {
        if (!isAvailable ||
            !pointerIsDown)
        {
            return;
        }


        // -----------------------------------------------------
        // Save state BEFORE resetting.
        // -----------------------------------------------------

        bool wasDragging =
            isDragging;


        pointerIsDown =
            false;


        // =====================================================
        // NORMAL CLICK
        // =====================================================

        if (!wasDragging)
        {
            Debug.Log(
                $"[HudSubmittableIcon] " +
                $"{name}: CLICK"
            );


            ToggleFloatingWindow();


            ResetInteraction();

            return;
        }


        // =====================================================
        // DRAG RELEASE
        // =====================================================

        Debug.Log(
            $"[HudSubmittableIcon] " +
            $"{name}: DRAG RELEASE AT " +
            $"{eventData.position}"
        );


        bool submitted =
            ResolveDragDrop(
                eventData
            );


        // -----------------------------------------------------
        // DRAG MISSED TARGET
        //
        // IMPORTANT:
        //
        // A drag that misses the submission target is NOT
        // treated as a normal ToggleWindow().
        //
        // Instead, the window opens at the exact position where
        // the user released the icon.
        //
        // Example:
        //
        // Release on center:
        //     Window opens in center.
        //
        // Release on left:
        //     Window opens on left.
        //
        // Release on right:
        //     Window opens on right.
        // -----------------------------------------------------

        if (!submitted)
        {
            Debug.Log(
                $"[HudSubmittableIcon] " +
                $"{name}: DRAG MISSED TARGET " +
                "→ OPEN WINDOW AT DROP POSITION"
            );


            OpenFloatingWindowAtDropPosition(
                eventData.position
            );
        }


        ResetInteraction();
    }


    // =========================================================
    // OPEN WINDOW AT DROP POSITION
    // =========================================================

    private void OpenFloatingWindowAtDropPosition(
        Vector2 screenPosition
    )
    {
        if (floatingWindow == null)
        {
            Debug.LogError(
                $"[HudSubmittableIcon] " +
                $"{name}: No FloatingWindow assigned!"
            );

            return;
        }


        Debug.Log(
            $"[HudSubmittableIcon] " +
            $"{name}: Opening window at screen position " +
            $"{screenPosition}"
        );


        floatingWindow.OpenWindowAtScreenPosition(
            screenPosition
        );
    }


    // =========================================================
    // TOGGLE FLOATING WINDOW
    // =========================================================

    private void ToggleFloatingWindow()
    {
        if (floatingWindow == null)
        {
            Debug.LogError(
                $"[HudSubmittableIcon] " +
                $"{name} has no FloatingWindow assigned!"
            );

            return;
        }


        Debug.Log(
            $"[HudSubmittableIcon] " +
            $"{name}: TOGGLE"
        );


        Debug.Log(
            $"[HudSubmittableIcon] " +
            $"Window state BEFORE toggle: " +
            $"{floatingWindow.gameObject.activeSelf}"
        );


        // -----------------------------------------------------
        // NORMAL CLICK
        //
        // FloatingWindow.ToggleWindow() uses OpenWindow(),
        // which opens the window CENTERED.
        // -----------------------------------------------------

        floatingWindow.ToggleWindow();


        Debug.Log(
            $"[HudSubmittableIcon] " +
            $"Window state AFTER toggle: " +
            $"{floatingWindow.gameObject.activeSelf}"
        );
    }


    // =========================================================
    // RESOLVE DRAG DROP
    // =========================================================

    /// <summary>
    /// Attempts to submit the dragged value.
    ///
    /// Returns:
    ///     true  = dropped on the correct target
    ///     false = dropped somewhere else
    /// </summary>
    private bool ResolveDragDrop(
        PointerEventData eventData
    )
    {
        // -----------------------------------------------------
        // ValueTransferManager
        // -----------------------------------------------------

        if (ValueTransferManager.Instance == null)
        {
            Debug.LogError(
                "[HudSubmittableIcon] " +
                "ValueTransferManager.Instance is NULL!"
            );


            DestroyGhost();

            return false;
        }


        // -----------------------------------------------------
        // Submission target must exist
        // -----------------------------------------------------

        if (submissionTarget == null)
        {
            Debug.LogWarning(
                "[HudSubmittableIcon] " +
                $"{name}: No Submission Target assigned."
            );


            ValueTransferManager.Instance
                .ClearSelection();


            DestroyGhost();

            return false;
        }


        // -----------------------------------------------------
        // Convert target to IDataValueDestination
        // -----------------------------------------------------

        IDataValueDestination destination =
            submissionTarget as IDataValueDestination;


        if (destination == null)
        {
            Debug.LogError(
                "[HudSubmittableIcon] " +
                $"{submissionTarget.name} does not implement " +
                "IDataValueDestination."
            );


            ValueTransferManager.Instance
                .ClearSelection();


            DestroyGhost();

            return false;
        }


        // -----------------------------------------------------
        // Check ONLY the assigned target.
        // -----------------------------------------------------

        if (!IsPointerOverSubmissionTarget(
                eventData))
        {
            Debug.Log(
                "[HudSubmittableIcon] " +
                "Pointer is NOT over the assigned " +
                "submission target."
            );


            ValueTransferManager.Instance
                .ClearSelection();


            DestroyGhost();

            return false;
        }


        // -----------------------------------------------------
        // VALID DROP
        // -----------------------------------------------------

        Debug.Log(
            "[HudSubmittableIcon] " +
            $"VALID DROP → {submissionTarget.name}"
        );


        ValueTransferManager.Instance
            .TryPlaceOnDestination(
                destination
            );


        DestroyGhost();

        return true;
    }


    // =========================================================
    // CHECK SPECIFIC TARGET
    // =========================================================

    private bool IsPointerOverSubmissionTarget(
        PointerEventData eventData
    )
    {
        Camera cam =
            Camera.main;


        if (cam == null)
        {
            Debug.LogError(
                "[HudSubmittableIcon] " +
                "No Main Camera found."
            );

            return false;
        }


        // -----------------------------------------------------
        // Create ray from pointer.
        // -----------------------------------------------------

        Ray ray =
            cam.ScreenPointToRay(
                eventData.position
            );


        Debug.DrawRay(
            ray.origin,
            ray.direction * 100f,
            Color.green,
            2f
        );


        // -----------------------------------------------------
        // Raycast ALL colliders.
        //
        // We intentionally check all hits because a random
        // collider may be in front of the actual target.
        //
        // However, only the explicitly assigned
        // submissionTarget can ever return TRUE.
        // -----------------------------------------------------

        RaycastHit[] hits =
            Physics.RaycastAll(
                ray,
                100f
            );


        // -----------------------------------------------------
        // Check every hit for the SPECIFIC target.
        // -----------------------------------------------------

        foreach (RaycastHit hit in hits)
        {
            GameObject hitObject =
                hit.collider.gameObject;


            Debug.Log(
                $"[HudSubmittableIcon] " +
                $"Ray hit: {hitObject.name}"
            );


            // -------------------------------------------------
            // EXACT OBJECT MATCH
            // -------------------------------------------------

            if (hitObject ==
                submissionTarget.gameObject)
            {
                Debug.Log(
                    "[HudSubmittableIcon] " +
                    "DIRECT TARGET MATCH!"
                );

                return true;
            }


            // -------------------------------------------------
            // COLLIDER IS CHILD OF TARGET
            //
            // Example:
            //
            // Auditor
            //   └── Body
            //       └── Collider
            //
            // If Auditor is assigned as submissionTarget,
            // this will still count as a valid drop.
            // -------------------------------------------------

            if (hitObject.transform.IsChildOf(
                    submissionTarget.transform))
            {
                Debug.Log(
                    "[HudSubmittableIcon] " +
                    "TARGET CHILD MATCH!"
                );

                return true;
            }


            // -------------------------------------------------
            // TARGET IS CHILD OF HIT OBJECT
            //
            // Supports the opposite hierarchy arrangement.
            // -------------------------------------------------

            if (submissionTarget.transform.IsChildOf(
                    hitObject.transform))
            {
                Debug.Log(
                    "[HudSubmittableIcon] " +
                    "TARGET PARENT MATCH!"
                );

                return true;
            }
        }


        // -----------------------------------------------------
        // Nothing matched.
        // -----------------------------------------------------

        Debug.Log(
            "[HudSubmittableIcon] " +
            "No matching submission target was hit."
        );


        return false;
    }


    // =========================================================
    // RESET INTERACTION
    // =========================================================

    private void ResetInteraction()
    {
        isDragging =
            false;

        pointerIsDown =
            false;

        pointerDownScreenPos =
            Vector2.zero;
    }


    // =========================================================
    // DESTROY GHOST
    // =========================================================

    private void DestroyGhost()
    {
        if (activeGhost != null)
        {
            Destroy(
                activeGhost.gameObject
            );

            activeGhost = null;
        }
    }


    // =========================================================
    // DATA VALUE SOURCE
    // =========================================================

    public DataValue GetDataValue()
    {
        string key =
            kind == SubmittableKind.CaseFolder
                ? "Submit_CaseFolder"
                : "Submit_TaxReturn";


        return new DataValue(
            kind,
            kind.ToString(),
            DataValueType.Text,
            key
        );
    }
}