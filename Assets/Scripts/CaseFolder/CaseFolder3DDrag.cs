using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

public class CaseFolder3DDrag :
    MonoBehaviour,
    IPointerDownHandler,
    IDragHandler,
    IPointerUpHandler,
    IPointerEnterHandler,
    IPointerExitHandler
{
    [Header("References")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private Transform deskViewpoint;
    [SerializeField] private GameObject closedFolder;

    [Tooltip(
        "Assign the open case folder object here. " +
        "Its rotation and scale will never be changed. " +
        "Its position will be shifted sideways/up-down to match " +
        "where the closed folder was dropped, while keeping its " +
        "original distance from the camera."
    )]
    [SerializeField] private GameObject openFolder;

    [Header("Case Folder UI")]
    [SerializeField] private CaseFolderUI caseFolderUI;

    [Tooltip("Optional bounded surface shared by desk documents.")]
    [SerializeField] private DeskPlacementSurface deskSurface;

    [Tooltip("Optional open/close/settle motion shared by desk documents.")]
    [SerializeField] private DeskDocumentAnimator deskAnimator;

    [Header("HUD Storage")]
    [SerializeField] private FolderToHUDIcon folderToHUDIcon;

    [Header("External HUD Drag")]
    [SerializeField] private bool allowExternalHUDDragging = true;

    private bool externalHUDDragging;
    private Vector2 externalPointerDownPosition;


    [Header("Drag Settings")]
    [SerializeField] private bool allowDragging = true;

    [Tooltip("Keeps the closed folder at its original rotation.")]
    [SerializeField] private bool preserveClosedFolderRotation = true;

    [Tooltip("Makes the closed folder smoothly follow the pointer.")]
    [SerializeField] private bool animateDragging = true;

    [Tooltip("Higher values make the closed folder follow the pointer faster.")]
    [SerializeField] private float dragSmoothness = 18f;

    [Tooltip(
        "Moves the closed folder toward the camera while the pointer " +
        "moves toward the center of the screen."
    )]
    [SerializeField] private float cameraMovementAmount = 0.15f;

    [Tooltip(
        "Moves the closed folder downward or upward while dragging. " +
        "Negative values move it downward, while positive values move it upward."
    )]
    [SerializeField] private float closedFolderHeightOffset = 0f;

    [Tooltip(
        "Minimum screen movement required before the folder opens."
    )]
    [SerializeField] private float minimumDragDistance = 25f;

    [Header("Opening Settings")]
    [SerializeField] private bool animateOpening = false;

    [Tooltip(
        "When enabled, the open folder's position (sideways/up-down only, " +
        "not depth) will match wherever the closed folder was dropped."
    )]
    [SerializeField] private bool positionOpenFolderAtDropPoint = true;

    [Header("Debug Settings")]
    [SerializeField] private bool enableDebugLogs = true;

    [SerializeField] private bool drawDebugRay = true;

    [SerializeField] private Color debugRayColor = Color.red;


    private Plane dragPlane;

    private bool isDragging;
    private bool hasDragTarget;
    private bool isOpening;
    private bool hasMovedDuringDrag;

    /// <summary>The closed folder is being carried by the finger (T10: the hotbar lights the slot under it).</summary>
    public bool IsCarrying => isDragging && hasMovedDuringDrag;

    private bool isPointerOver;

    private Vector2 pointerPosition;

    private HighlightEffect highlightEffect;

    private Vector3 dragTargetPosition;
    private Vector3 heldPointerPosition;
    private Vector3 dragOffset;
    private Vector3 dragStartPosition;
    private Collider placementCollider;
    private Camera eventCamera;
    private bool currentDropValid;

    private Vector2 pointerDownScreenPosition;
    private Vector2 lastPointerScreenPosition;

    private Vector3 originalClosedPosition;
    private Quaternion originalClosedRotation;
    private Vector3 originalClosedScale;

    private Vector3 originalOpenPosition;
    private Quaternion originalOpenRotation;
    private Vector3 originalOpenScale;

    private Coroutine openingCoroutine;

    private void Awake()
    {
        DebugLog("Awake() started.");

        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        if (closedFolder == null)
        {
            closedFolder = gameObject;
        }

        if (deskSurface == null)
            deskSurface = GetComponentInParent<DeskPlacementSurface>();
        if (deskAnimator == null)
            deskAnimator = GetComponentInParent<DeskDocumentAnimator>();
        // Scenes saved before the animator was added still get desk motion.
        if (deskAnimator == null && deskSurface != null)
            deskAnimator = deskSurface.gameObject.AddComponent<DeskDocumentAnimator>();
        if (closedFolder != null)
            placementCollider = closedFolder.GetComponent<Collider>();

        if (deskViewpoint == null)
        {
            Debug.LogError(
                "[CaseFolder3DDrag] Desk Viewpoint is not assigned."
            );

            return;
        }

        if (openFolder == null)
        {
            Debug.LogError(
                "[CaseFolder3DDrag] Open Folder is not assigned."
            );

            return;
        }

        if (mainCamera == null)
        {
            Debug.LogError(
                "[CaseFolder3DDrag] Main Camera was not found."
            );

            return;
        }

        if (dragSmoothness <= 0f)
        {
            dragSmoothness = 18f;
        }

        if (cameraMovementAmount < 0f)
        {
            cameraMovementAmount = 0f;
        }

        if (minimumDragDistance < 0f)
        {
            minimumDragDistance = 25f;
        }

        // Save the closed folder's original transform.
        originalClosedPosition =
            closedFolder.transform.position;

        originalClosedRotation =
            closedFolder.transform.rotation;

        originalClosedScale =
            closedFolder.transform.localScale;

        /*
         * Save the open folder's original transform.
         * Rotation and scale are never changed. Position is only
         * ever shifted sideways/up-down from this saved position,
         * never in depth, and only when
         * positionOpenFolderAtDropPoint is enabled.
         */
        originalOpenPosition =
            openFolder.transform.position;

        originalOpenRotation =
            openFolder.transform.rotation;

        originalOpenScale =
            openFolder.transform.localScale;

        // Hide the open folder without changing its transform.
        openFolder.SetActive(false);

        DebugLog("References successfully initialized.");
        DebugLog(
            $"Original Closed Folder Position: {originalClosedPosition}"
        );
        DebugLog(
            $"Original Open Folder Position: {originalOpenPosition}"
        );
    }

    private void Update()
    {
        if (isOpening || (!allowDragging && !externalHUDDragging))
        {
            isPointerOver = false;
            SetOutlineFocused(false);
        }
        else if (isPointerOver || isDragging)
        {
            UpdateOutlineFocus();
        }

        if (!isDragging ||
            !hasDragTarget ||
            isOpening ||
            closedFolder == null)
        {
            return;
        }

        // Like the tax-code book, the held visual follows the pointer freely;
        // only the resting/drop position is constrained to the desk.
        Vector3 heldPosition = heldPointerPosition;
        if (deskSurface != null)
            heldPosition += deskSurface.DragLiftOffset;

        // Always a stiff exponential follow: responsive, but finger jitter and
        // uneven frames never shake the held object (it used to snap once moved).
        if (animateDragging)
        {
            float stiffness = hasMovedDuringDrag ? Mathf.Max(dragSmoothness, 28f) : dragSmoothness;
            float interpolation = 1f - Mathf.Exp(-stiffness * Mathf.Min(Time.unscaledDeltaTime, 1f / 30f));
            closedFolder.transform.position = Vector3.Lerp(closedFolder.transform.position, heldPosition, interpolation);
        }
        else
        {
            closedFolder.transform.position = heldPosition;
        }

        if (preserveClosedFolderRotation)
        {
            closedFolder.transform.rotation = originalClosedRotation;
        }
    }

    private void OnDisable()
    {
        if (isDragging && closedFolder != null)
        {
            closedFolder.transform.position = hasDragTarget ? dragTargetPosition : dragStartPosition;
            if (preserveClosedFolderRotation)
                closedFolder.transform.rotation = originalClosedRotation;
        }
        isDragging = false;
        externalHUDDragging = false;
        hasDragTarget = false;
        currentDropValid = false;
        isPointerOver = false;
        SetOutlineFocused(false);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        DebugLog("OnPointerDown() detected.");

        if (!isActiveAndEnabled || !allowDragging || isOpening)
        {
            return;
        }

        pointerPosition = eventData.position;

        if (mainCamera == null ||
            closedFolder == null ||
            openFolder == null)
        {
            Debug.LogError(
                "[CaseFolder3DDrag] A required reference is missing."
            );

            return;
        }

        if (deskAnimator != null)
            deskAnimator.Complete(closedFolder.transform);

        pointerDownScreenPosition =
            eventData.position;

        lastPointerScreenPosition =
            eventData.position;

        hasMovedDuringDrag = false;
        isDragging = true;
        dragStartPosition = closedFolder.transform.position;
        dragTargetPosition = dragStartPosition;
        heldPointerPosition = dragStartPosition;
        hasDragTarget = true;
        currentDropValid = true;

        /*
         * The plane is parallel to the camera and passes directly
         * through the closed folder's current position.
         */
        eventCamera = eventData.pressEventCamera != null ? eventData.pressEventCamera : mainCamera;
        Ray ray = eventCamera.ScreenPointToRay(eventData.position);
        Vector3 pointerWorldPosition;
        if (deskSurface != null)
        {
            if (!deskSurface.TryGetPointerPoint(ray, dragStartPosition, out pointerWorldPosition))
            {
                currentDropValid = false;
                isDragging = false;
                hasDragTarget = false;
                return;
            }
        }
        else
        {
            dragPlane = new Plane(mainCamera.transform.forward, TaxCodeBook3DDrag.VisualCentre(closedFolder));
            if (!dragPlane.Raycast(ray, out float enter))
            {
                DebugLog("The pointer did not intersect the drag plane.");
                currentDropValid = false;
                isDragging = false;
                hasDragTarget = false;
                return;
            }
            pointerWorldPosition = ray.GetPoint(enter);
        }

        /*
         * Centre the visible model under the touch, accounting for its mesh pivot.
         */
        dragOffset = closedFolder.transform.position - TaxCodeBook3DDrag.VisualCentre(closedFolder);

        dragTargetPosition =
            closedFolder.transform.position;

        hasDragTarget = true;
        UpdateOutlineFocus();

        if (preserveClosedFolderRotation)
        {
            closedFolder.transform.rotation =
                originalClosedRotation;
        }

        DebugLog(
            $"Pointer position: {eventData.position}"
        );

        DebugLog(
            $"Pointer world position: {pointerWorldPosition}"
        );

        DebugLog(
            $"Exact drag offset: {dragOffset}"
        );

        DebugLog(
            $"Closed folder position: {closedFolder.transform.position}"
        );
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!isActiveAndEnabled || !allowDragging || isOpening || closedFolder == null)
        {
            return;
        }

        if (eventData != null)
            pointerPosition = eventData.position;
        isPointerOver = true;
        UpdateOutlineFocus();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isPointerOver = false;
        UpdateOutlineFocus();
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging || isOpening)
        {
            return;
        }

        pointerPosition = eventData.position;

        lastPointerScreenPosition =
            eventData.position;

        float screenDistance =
            Vector2.Distance(
                pointerDownScreenPosition,
                eventData.position
            );

        if (screenDistance >= minimumDragDistance)
        {
            hasMovedDuringDrag = true;
        }

        UpdateDragTarget(eventData.position);

        DebugLog(
            $"OnDrag detected. Target position: {dragTargetPosition}"
        );
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        DebugLog("OnPointerUp() detected.");

        if (!isDragging || isOpening)
        {
            return;
        }

        pointerPosition = eventData.position;

        lastPointerScreenPosition =
            eventData.position;

        float screenDistance =
            Vector2.Distance(
                pointerDownScreenPosition,
                eventData.position
            );

        UpdateDragTarget(eventData.position);

        if (!hasMovedDuringDrag ||
            screenDistance < minimumDragDistance)
        {
            closedFolder.transform.position = dragStartPosition;
            if (preserveClosedFolderRotation)
                closedFolder.transform.rotation = originalClosedRotation;
            DebugLog(
                "No meaningful drag detected. " +
                "The closed folder remains visible."
            );

            isDragging = false;
            hasDragTarget = false;
            isPointerOver = false;
            UpdateOutlineFocus();

            return;
        }

        bool droppedOnHUD =
            IsPointerInsideHUDIcon(eventData);

        if (!currentDropValid && !droppedOnHUD)
        {
            closedFolder.transform.position = hasDragTarget ? dragTargetPosition : dragStartPosition;
            if (preserveClosedFolderRotation)
                closedFolder.transform.rotation = originalClosedRotation;
            if (deskAnimator != null)
                deskAnimator.PlaySettle(closedFolder.transform);
            isDragging = false;
            hasDragTarget = false;
            isPointerOver = false;
            UpdateOutlineFocus();
            return;
        }

        if (hasDragTarget)
        {
            closedFolder.transform.position =
                dragTargetPosition;
        }

        if (preserveClosedFolderRotation)
        {
            closedFolder.transform.rotation =
                originalClosedRotation;
        }

        isDragging = false;
        hasDragTarget = false;
        isPointerOver = false;
        UpdateOutlineFocus();

        DebugLog(
            $"Closed folder released at: {closedFolder.transform.position}"
        );

        if (droppedOnHUD)
        {
            DebugLog(
                "Closed folder was dropped onto the HUD icon."
            );

            if (Icon != null)
            {
                if (!flyingIntoBar) StartCoroutine(FlyClosedIntoBar(eventData.position));
            }
            else
            {
                Debug.LogWarning(
                    "[CaseFolder3DDrag] FolderToHUDIcon reference is missing."
                );
            }

            return;
        }

        ShowOpenFolder(eventData.position);

        DebugLog("Folder drop process completed.");
    }


    private bool IsPointerInsideHUDIcon(
        PointerEventData eventData
    )
    {
        if (Icon == null)
        {
            return false;
        }

        // T10: the same drop test as the open folder (any hotbar slot when the level uses the hotbar).
        return Icon.IsOverDropTarget(eventData.position);
    }

    /// <summary>
    /// The folder's HUD store. The scene leaves this component's link empty, so it is the open
    /// folder's own (2026-10-26: a closed folder dropped on the bar was opened instead of stored).
    /// </summary>
    private FolderToHUDIcon Icon
    {
        get
        {
            if (folderToHUDIcon == null && openFolder != null) folderToHUDIcon = openFolder.GetComponent<FolderToHUDIcon>();
            return folderToHUDIcon;
        }
    }

    private bool flyingIntoBar;

    /// <summary>
    /// The closed folder dropped on the bar: it flies toward the finger, shrinking into the slot,
    /// then it is stored there (the same store as the open folder). It never opens on the way.
    /// </summary>
    private System.Collections.IEnumerator FlyClosedIntoBar(Vector2 drop)
    {
        flyingIntoBar = true;
        Transform folder = closedFolder.transform;
        Camera view = HeldOnScreen.ActiveCamera(mainCamera);
        Vector3 from = folder.position;
        Quaternion fromRotation = folder.rotation;
        Vector3 restScale = folder.localScale;
        float depth = view != null ? Vector3.Dot(from - view.transform.position, view.transform.forward) : 0.6f;
        const float seconds = 0.3f;
        for (float elapsed = 0f; elapsed < seconds; elapsed += Time.unscaledDeltaTime)
        {
            float t = Mathf.Clamp01(elapsed / seconds);
            float ease = t * t * (3f - 2f * t);
            Vector3 target = view != null
                ? view.ScreenToWorldPoint(new Vector3(drop.x, drop.y, Mathf.Max(view.nearClipPlane + 0.05f, depth * (1f - 0.5f * ease))))
                : from;
            folder.position = Vector3.Lerp(from, target, ease);
            folder.rotation = fromRotation * Quaternion.Euler(0f, 0f, 25f * ease);
            folder.localScale = restScale * Mathf.Lerp(1f, 0.12f, ease * ease);
            yield return null;
        }
        folder.localScale = restScale;
        PlaceClosedFolderAtOrigin();
        closedFolder.SetActive(false);
        flyingIntoBar = false;
        HudSlotDrop.Note(InventoryCatalogue.CaseFolder, drop);
        if (Icon != null) Icon.StoreFolderFromExternalSource();
    }

    private void UpdateDragTarget(Vector2 screenPosition)
    {
        if (mainCamera == null ||
            closedFolder == null)
        {
            return;
        }

        Camera pointerCamera = eventCamera != null ? eventCamera : mainCamera;
        if (pointerCamera == null)
        {
            currentDropValid = false;
            return;
        }
        Ray ray = pointerCamera.ScreenPointToRay(screenPosition);

        if (drawDebugRay)
        {
            Debug.DrawRay(
                ray.origin,
                ray.direction * 20f,
                debugRayColor,
                0.05f
            );
        }

        if (deskSurface != null)
        {
            // Pointer off the desk plane: keep the last safe pose, do not open on release.
            // Project at the visible centre's height, keeping an off-centre pivot
            // at its original resting height when the folder is dropped.
            if (!deskSurface.TryGetPointerPoint(ray, dragStartPosition - dragOffset + deskSurface.DragLiftOffset, out Vector3 surfacePoint))
            {
                currentDropValid = false;
                return;
            }

            heldPointerPosition = surfacePoint + dragOffset - deskSurface.DragLiftOffset;
            if (deskSurface.TryClampPosition(closedFolder.transform, placementCollider,
                    heldPointerPosition, out Vector3 clampedPosition))
            {
                dragTargetPosition = clampedPosition;
            }

            hasDragTarget = true;
            currentDropValid = true;
            return;
        }

        if (!dragPlane.Raycast(ray, out float enter))
        {
            DebugLog("Screen ray did not intersect the drag plane.");
            currentDropValid = false;
            return;
        }

        Vector3 pointerWorldPosition =
            ray.GetPoint(enter);

        /*
         * Direct pointer-following position.
         */
        dragTargetPosition =
            pointerWorldPosition +
            dragOffset;
        heldPointerPosition = dragTargetPosition;

        /*
         * Apply the height offset only to the closed folder.
         */
        dragTargetPosition +=
            Vector3.up *
            closedFolderHeightOffset;

        /*
         * Move the closed folder slightly toward the camera when
         * the pointer approaches the center of the screen.
         *
         * The movement is strongest at the center and becomes zero
         * near the screen edges.
         */
        if (cameraMovementAmount > 0f)
        {
            Vector2 screenCenter =
                new Vector2(
                    Screen.width * 0.5f,
                    Screen.height * 0.5f
                );

            Vector2 screenCenterOffset =
                screenPosition -
                screenCenter;

            float horizontalFactor =
                1f - Mathf.Clamp01(
                    Mathf.Abs(screenCenterOffset.x) /
                    (Screen.width * 0.5f)
                );

            float verticalFactor =
                1f - Mathf.Clamp01(
                    Mathf.Abs(screenCenterOffset.y) /
                    (Screen.height * 0.5f)
                );

            float centerFactor =
                (
                    horizontalFactor +
                    verticalFactor
                ) * 0.5f;

            Vector3 cameraMovement =
                -mainCamera.transform.forward.normalized *
                cameraMovementAmount *
                centerFactor;

            dragTargetPosition +=
                cameraMovement;
        }

        hasDragTarget = true;
        currentDropValid = true;
    }

    /// <summary>
    /// Computes where the open folder should sit so that its
    /// sideways/up-down position matches the given screen point,
    /// while keeping its original distance from the camera
    /// (depth is never changed).
    /// </summary>
    /// <summary>The open folder's resting pose on the desk (the HUD folder view copies it).</summary>
    public Vector3 OpenRestPosition => originalOpenPosition;
    public Quaternion OpenRestRotation => originalOpenRotation;

    /// <summary>Where the desk opens the folder for a screen point (its root, on the open folder's depth plane).</summary>
    public Vector3 OpenPositionFor(Vector2 screenPosition) => GetOpenFolderPositionForScreenPoint(screenPosition);

    private Vector3 GetOpenFolderPositionForScreenPoint(
        Vector2 screenPosition
    )
    {
        Camera mainCamera = HeldOnScreen.ActiveCamera(this.mainCamera);
        if (mainCamera == null)
        {
            return originalOpenPosition;
        }

        /*
         * Plane perpendicular to the camera, passing through the
         * open folder's original position. Any point on this plane
         * shares the same depth-from-camera as the original position,
         * so raycasting onto it only changes sideways/up-down placement.
         */
        Plane openFolderPlane = new Plane(
            mainCamera.transform.forward,
            originalOpenPosition
        );

        Ray ray =
            mainCamera.ScreenPointToRay(
                screenPosition
            );

        if (!openFolderPlane.Raycast(ray, out float enter))
        {
            DebugLog(
                "Could not project drop point onto the open folder's plane. " +
                "Falling back to the original open folder position."
            );

            return originalOpenPosition;
        }

        return ray.GetPoint(enter);
    }

    private void ShowOpenFolder(Vector2 dropScreenPosition, bool centrePaper = false)
    {
        if (closedFolder == null ||
            openFolder == null)
        {
            Debug.LogError(
                "[CaseFolder3DDrag] Cannot show the open folder."
            );

            return;
        }

        isPointerOver = false;
        SetOutlineFocused(false);

        /*
         * Hide the closed folder and show the open folder.
         */
        CompleteDeskMotion();
        openFolder.transform.rotation = originalOpenRotation;
        openFolder.transform.localScale = originalOpenScale;
        closedFolder.SetActive(false);

        if (positionOpenFolderAtDropPoint)
        {
            openFolder.transform.position =
                GetOpenFolderPositionForScreenPoint(dropScreenPosition);

            DebugLog(
                $"Open folder positioned at drop point: " +
                $"{openFolder.transform.position}"
            );
        }

        openFolder.SetActive(true);

        Camera view = HeldOnScreen.ActiveCamera(mainCamera);
        if (centrePaper && caseFolderUI != null && caseFolderUI.PaperRect != null)
            openFolder.transform.position = DeskMotion.CentrePaper(view, openFolder.transform.position,
                caseFolderUI.PaperRect.TransformPoint(caseFolderUI.PaperRect.rect.center));
        else if (positionOpenFolderAtDropPoint && caseFolderUI != null && caseFolderUI.PaperRect != null)
            // The printed page opens exactly where the finger let go (2026-10-26), not the folder's corner.
            HeldOnScreen.CentreUnder(openFolder.transform,
                caseFolderUI.PaperRect.TransformPoint(caseFolderUI.PaperRect.rect.center), view, dropScreenPosition);

        // The whole open folder shows: a drop near an edge is moved in (same depth, never off screen).
        openFolder.transform.position = HeldOnScreen.FitOnScreen(openFolder.transform, view);

        if (deskAnimator != null)
            deskAnimator.PlayOpen(openFolder.transform, closedFolder.transform.position);

        DebugLog(
            "Open folder shown."
        );
        /*
         * Show the Case Folder UI after the open folder appears.
         */
        if (caseFolderUI != null)
        {
            caseFolderUI.Show();

            DebugLog(
                "CaseFolderUI.Show() called after opening the folder."
            );
        }
        else
        {
            Debug.LogError(
                "[CaseFolder3DDrag] CaseFolderUI reference is missing!"
            );
        }
    }

    public void ResetFolder()
    {
        DebugLog("ResetFolder() called.");

        CompleteDeskMotion();

        if (openingCoroutine != null)
        {
            StopCoroutine(openingCoroutine);
            openingCoroutine = null;
        }

        isDragging = false;
        hasDragTarget = false;
        isOpening = false;
        hasMovedDuringDrag = false;
        isPointerOver = false;
        SetOutlineFocused(false);

        if (closedFolder != null)
        {
            closedFolder.transform.position =
                originalClosedPosition;

            closedFolder.transform.rotation =
                originalClosedRotation;

            closedFolder.transform.localScale =
                originalClosedScale;

            closedFolder.SetActive(true);
        }

        if (openFolder != null)
        {
            /*
             * Restore the original open-folder transform only during
             * an explicit reset.
             */
            openFolder.transform.position =
                originalOpenPosition;

            openFolder.transform.rotation =
                originalOpenRotation;

            openFolder.transform.localScale =
                originalOpenScale;

            openFolder.SetActive(false);
        }

        DebugLog("Folder successfully reset.");
    }

    private void DebugLog(string message)
    {
        if (enableDebugLogs)
        {
            Debug.Log(
                $"[CaseFolder3DDrag] {message}"
            );
        }
    }

    public void BeginExternalDrag(Vector2 screenPosition)
    {
        if (!isActiveAndEnabled ||
            !allowExternalHUDDragging ||
            isOpening ||
            mainCamera == null ||
            closedFolder == null ||
            openFolder == null)
        {
            return;
        }

        pointerPosition = screenPosition;

        if (deskAnimator != null)
            deskAnimator.Complete(closedFolder.transform);

        externalHUDDragging = true;

        externalPointerDownPosition =
            screenPosition;

        pointerDownScreenPosition =
            screenPosition;

        lastPointerScreenPosition =
            screenPosition;

        hasMovedDuringDrag = false;
        isDragging = true;
        dragStartPosition = closedFolder.transform.position;
        dragTargetPosition = dragStartPosition;
        heldPointerPosition = dragStartPosition;
        hasDragTarget = true;
        currentDropValid = true;
        eventCamera = mainCamera;

        Ray ray = mainCamera.ScreenPointToRay(screenPosition);
        Vector3 pointerWorldPosition;
        if (deskSurface != null)
        {
            if (!deskSurface.TryGetPointerPoint(ray, dragStartPosition, out pointerWorldPosition))
            {
                externalHUDDragging = false;
                isDragging = false;
                hasDragTarget = false;
                currentDropValid = false;
                UpdateOutlineFocus();
                DebugLog("External HUD drag could not intersect the desk surface.");
                return;
            }
        }
        else
        {
            dragPlane = new Plane(mainCamera.transform.forward, TaxCodeBook3DDrag.VisualCentre(closedFolder));
            if (!dragPlane.Raycast(ray, out float enter))
            {
                externalHUDDragging = false;
                isDragging = false;
                hasDragTarget = false;
                currentDropValid = false;
                UpdateOutlineFocus();
                DebugLog("External HUD drag could not intersect the drag plane.");
                return;
            }
            pointerWorldPosition = ray.GetPoint(enter);
        }

        dragOffset = closedFolder.transform.position - TaxCodeBook3DDrag.VisualCentre(closedFolder);

        dragTargetPosition =
            closedFolder.transform.position;

        hasDragTarget = true;
        UpdateOutlineFocus();

        closedFolder.SetActive(true);
        openFolder.SetActive(false);
        UpdateDragTarget(screenPosition);

        DebugLog(
            "External HUD drag started."
        );
    }


    public void UpdateExternalDrag(Vector2 screenPosition)
    {
        if (!externalHUDDragging ||
            !isDragging ||
            isOpening)
        {
            return;
        }

        pointerPosition = screenPosition;

        lastPointerScreenPosition =
            screenPosition;

        float screenDistance =
            Vector2.Distance(
                externalPointerDownPosition,
                screenPosition
            );

        if (screenDistance >= minimumDragDistance)
        {
            hasMovedDuringDrag = true;
        }

        UpdateDragTarget(screenPosition);
    }


    public bool EndExternalDrag(Vector2 screenPosition)
    {
        if (!externalHUDDragging ||
            !isDragging ||
            isOpening)
        {
            return false;
        }

        pointerPosition = screenPosition;

        lastPointerScreenPosition =
            screenPosition;

        UpdateDragTarget(screenPosition);

        if (!currentDropValid)
        {
            closedFolder.transform.position = hasDragTarget ? dragTargetPosition : dragStartPosition;
            if (preserveClosedFolderRotation)
                closedFolder.transform.rotation = originalClosedRotation;
            externalHUDDragging = false;
            isDragging = false;
            hasDragTarget = false;
            isPointerOver = false;
            UpdateOutlineFocus();
            return false;
        }

        if (hasDragTarget)
        {
            closedFolder.transform.position =
                dragTargetPosition;
        }

        if (preserveClosedFolderRotation)
        {
            closedFolder.transform.rotation =
                originalClosedRotation;
        }

        externalHUDDragging = false;
        isDragging = false;
        hasDragTarget = false;
        isPointerOver = false;
        UpdateOutlineFocus();

        DebugLog(
            $"External HUD drag ended at: " +
            $"{closedFolder.transform.position}"
        );

        return true;
    }

    public void OpenDraggedFolder(bool centrePaper = false)
    {
        ShowOpenFolder(lastPointerScreenPosition, centrePaper);
    }

    /// <summary>
    /// Puts the folder back exactly where it started on the desk: an open
    /// folder closes onto that spot, a moved closed folder slides back.
    /// Does nothing while the folder is stored in the HUD.
    /// </summary>
    public void ReturnToOriginalPlace(bool animate)
    {
        if (!enabled || !allowDragging || closedFolder == null)
            return;
        // A folder that belongs to the HUD (stored, or opened from its icon) goes back to the icon, never
        // to the desk stack: that left it in both places at once (2026-10-26).
        if (FolderToHUDIcon.AnyStoredOrViewing() ||
            (OfficeHudRuntime.Instance != null && OfficeHudRuntime.Instance.CaseFolderViewOut))
            return;

        CompleteDeskMotion();
        isDragging = false;
        hasDragTarget = false;
        externalHUDDragging = false;
        isPointerOver = false;
        SetOutlineFocused(false);

        bool canAnimate = animate && deskAnimator != null;
        if (openFolder != null && openFolder.activeSelf)
        {
            void Finish()
            {
                openFolder.SetActive(false);
                openFolder.transform.SetPositionAndRotation(originalOpenPosition, originalOpenRotation);
                openFolder.transform.localScale = originalOpenScale;
                PlaceClosedFolderAtOrigin();
                closedFolder.SetActive(true);
                if (canAnimate)
                    deskAnimator.PlaySettle(closedFolder.transform);
            }

            if (canAnimate)
                deskAnimator.PlayClose(openFolder.transform, originalClosedPosition, Finish);
            else
                Finish();
            return;
        }

        if (!closedFolder.activeSelf)
            return;

        if (canAnimate)
        {
            closedFolder.transform.localScale = originalClosedScale;
            deskAnimator.PlaySlide(closedFolder.transform, originalClosedPosition, originalClosedRotation);
        }
        else
        {
            PlaceClosedFolderAtOrigin();
        }
    }

    private void PlaceClosedFolderAtOrigin()
    {
        closedFolder.transform.SetPositionAndRotation(originalClosedPosition, originalClosedRotation);
        closedFolder.transform.localScale = originalClosedScale;
    }

    private void CompleteDeskMotion()
    {
        if (deskAnimator == null)
            return;
        if (closedFolder != null)
            deskAnimator.Complete(closedFolder.transform);
        if (openFolder != null)
            deskAnimator.Complete(openFolder.transform);
    }

    /// <summary>
    /// Fully hides the case folder (both closed and open states) and
    /// disables all dragging. Called when the folder has been stored
    /// (e.g. dropped onto a HUD icon) and should disappear for good.
    /// </summary>
    public void HideAndDisableFolder()
    {
        CompleteDeskMotion();
        isDragging = false;
        hasDragTarget = false;
        externalHUDDragging = false;
        allowDragging = false;
        allowExternalHUDDragging = false;
        isPointerOver = false;
        SetOutlineFocused(false);

        if (closedFolder != null)
        {
            closedFolder.SetActive(false);
        }

        if (openFolder != null)
        {
            openFolder.SetActive(false);
        }

        enabled = false;

        DebugLog(
            "Folder fully hidden and disabled after being stored."
        );
    }

    /// <summary>
    /// Inverse of HideAndDisableFolder(). Re-enables dragging and fully
    /// restores the closed folder to its original desk position/rotation/scale,
    /// hides the open folder, per the "retry = complete fresh start" requirement.
    /// Use this (not ResetFolder(), which assumes the component is still enabled)
    /// when coming back from HideAndDisableFolder().
    /// </summary>
    public void RestoreAndEnableFolder()
    {
        CompleteDeskMotion();
        isPointerOver = false;
        SetOutlineFocused(false);
        allowDragging = true;
        allowExternalHUDDragging = true;
        enabled = true;

        isDragging = false;
        hasDragTarget = false;
        externalHUDDragging = false;
        isOpening = false;
        hasMovedDuringDrag = false;

        if (closedFolder != null)
        {
            closedFolder.transform.position = originalClosedPosition;
            closedFolder.transform.rotation = originalClosedRotation;
            closedFolder.transform.localScale = originalClosedScale;
            closedFolder.SetActive(true);
        }

        if (openFolder != null)
        {
            openFolder.transform.position = originalOpenPosition;
            openFolder.transform.rotation = originalOpenRotation;
            openFolder.transform.localScale = originalOpenScale;
            openFolder.SetActive(false);
        }

        DebugLog("Folder restored and re-enabled after a failed audit retry.");
    }

    private void UpdateOutlineFocus()
    {
        bool pointerBlocked = isPointerOver &&
            !isDragging &&
            (DialogueUI.IsAnyDialogueActive ||
             VirtualJoystick.BlocksWorldInput ||
             MobileInputGuard.IsPointerOverUI(pointerPosition));

        SetOutlineFocused(
            isActiveAndEnabled &&
            !isOpening &&
            (externalHUDDragging && isDragging ||
             allowDragging &&
             (isDragging || isPointerOver && !pointerBlocked))
        );
    }

    private void SetOutlineFocused(bool focused)
    {
        if (focused &&
            (!isActiveAndEnabled ||
             isOpening ||
             !(allowDragging || externalHUDDragging)))
        {
            return;
        }

        if (focused && highlightEffect == null && closedFolder != null)
        {
            highlightEffect = HighlightEffect.For(closedFolder.transform);
        }

        if (highlightEffect != null)
        {
            highlightEffect.SetFocused(focused);
        }
    }

}
