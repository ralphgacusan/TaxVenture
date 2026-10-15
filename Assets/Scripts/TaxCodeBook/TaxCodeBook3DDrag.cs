using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Handles dragging and opening of the closed 3D tax code book.
///
/// Flow:
/// - Drag the closed book.
/// - Release it after meaningful movement.
/// - Hide the closed book.
/// - Show the open book, positioned sideways/up-down to match
///   the drop point (depth from camera is never changed).
/// - Open the existing 2D tax code UI.
/// </summary>
public class TaxCodeBook3DDrag :
    MonoBehaviour,
    IPointerDownHandler,
    IDragHandler,
    IPointerUpHandler,
    IPointerEnterHandler,
    IPointerExitHandler
{
    [Header("References")]

    [Tooltip("Main Camera used for 3D dragging.")]
    [SerializeField]
    private Camera mainCamera;

    [Tooltip("The closed 3D tax code book.")]
    [SerializeField]
    private GameObject closedTaxCodeBook;

    [Tooltip("The open 3D tax code book.")]
    [SerializeField]
    private GameObject openTaxCodeBook;

    [Tooltip("Existing 2D tax code UI controller.")]
    [SerializeField]
    private TaxCodeBookUI taxCodeBookUI;

    [Tooltip("Optional bounded surface shared by desk documents.")]
    [SerializeField] private DeskPlacementSurface deskSurface;

    [Tooltip("Optional open/close/settle motion shared by desk documents.")]
    [SerializeField] private DeskDocumentAnimator deskAnimator;


    [Header("Drag Settings")]

    [SerializeField]
    private bool allowDragging = true;

    [Tooltip("Preserves the closed book's original rotation.")]
    [SerializeField]
    private bool preserveClosedBookRotation = true;

    [Tooltip("Smoothly moves the closed book while dragging.")]
    [SerializeField]
    private bool animateDragging = true;

    [SerializeField]
    private float dragSmoothness = 18f;

    [Tooltip("Minimum pointer movement required to open the book.")]
    [SerializeField]
    private float minimumDragDistance = 25f;


    [Header("Opening Settings")]

    [Tooltip(
        "When enabled, the open book's position (sideways/up-down only, " +
        "not depth from camera) will match wherever the closed book " +
        "was dropped."
    )]
    [SerializeField]
    private bool positionOpenBookAtDropPoint = true;


    [Header("Debug")]

    [SerializeField]
    private bool enableDebugLogs = true;

    [SerializeField]
    private bool drawDebugRay = true;

    [SerializeField]
    private float debugRayLength = 20f;

    [SerializeField]
    private Color debugRayColor = Color.red;


    private Plane dragPlane;

    private Vector3 dragOffset;
    private Vector3 dragTargetPosition;

    private Quaternion originalClosedRotation;
    private Vector3 originalClosedPosition;
    private Vector3 originalClosedScale;
    private Quaternion originalOpenRotation;
    private Vector3 originalOpenScale;

    private Vector3 originalOpenPosition;

    private Vector2 pointerDownScreenPosition;

    private bool isDragging;
    private bool hasDragTarget;
    private bool hasMovedDuringDrag;

    private bool isPointerOver;

    private Vector2 pointerPosition;
    private Collider placementCollider;
    private Camera eventCamera;
    private Vector3 dragStartPosition;
    private bool currentDropValid;

    private HighlightEffect highlightEffect;


    private void Awake()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        if (closedTaxCodeBook == null)
        {
            closedTaxCodeBook = gameObject;
        }

        if (deskSurface == null)
            deskSurface = GetComponentInParent<DeskPlacementSurface>();
        if (deskAnimator == null)
            deskAnimator = GetComponentInParent<DeskDocumentAnimator>();
        // Scenes saved before the animator was added still get desk motion.
        if (deskAnimator == null && deskSurface != null)
            deskAnimator = deskSurface.gameObject.AddComponent<DeskDocumentAnimator>();
        if (closedTaxCodeBook != null)
            placementCollider = closedTaxCodeBook.GetComponent<Collider>();

        if (openTaxCodeBook == null)
        {
            Debug.LogError(
                "[TaxCodeBook3DDrag] Open tax code book is missing."
            );
        }

        if (mainCamera == null)
        {
            Debug.LogError(
                "[TaxCodeBook3DDrag] Main Camera was not found."
            );
        }

        if (dragSmoothness <= 0f)
        {
            dragSmoothness = 18f;
        }

        if (minimumDragDistance < 0f)
        {
            minimumDragDistance = 25f;
        }

        if (closedTaxCodeBook != null)
        {
            originalClosedRotation =
                closedTaxCodeBook.transform.rotation;
            originalClosedPosition = closedTaxCodeBook.transform.position;
            originalClosedScale = closedTaxCodeBook.transform.localScale;
        }

        if (openTaxCodeBook != null)
        {
            /*
             * Save the open book's original position. This is the
             * depth-from-camera reference used later so the open
             * book only ever shifts sideways/up-down, never in depth.
             */
            originalOpenPosition =
                openTaxCodeBook.transform.position;
            originalOpenRotation = openTaxCodeBook.transform.rotation;
            originalOpenScale = openTaxCodeBook.transform.localScale;

            openTaxCodeBook.SetActive(false);
        }

        DebugLog("Tax code book initialized.");
    }


    private void OnDisable()
    {
        if (isDragging && closedTaxCodeBook != null)
        {
            closedTaxCodeBook.transform.position = hasDragTarget ? dragTargetPosition : dragStartPosition;
            if (preserveClosedBookRotation)
                closedTaxCodeBook.transform.rotation = originalClosedRotation;
        }
        isDragging = false;
        hasDragTarget = false;
        currentDropValid = false;
        isPointerOver = false;
        SetOutlineFocused(false);
    }


    private void Update()
    {
        if (!allowDragging)
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
            closedTaxCodeBook == null)
        {
            return;
        }

        Vector3 heldPosition = dragTargetPosition;
        if (deskSurface != null)
            heldPosition += deskSurface.DragLiftOffset;
        Vector3 dragLag = heldPosition - closedTaxCodeBook.transform.position;

        if (animateDragging)
        {
            float interpolation =
                1f - Mathf.Exp(
                    -dragSmoothness *
                    Time.deltaTime
                );

            closedTaxCodeBook.transform.position =
                Vector3.Lerp(
                    closedTaxCodeBook.transform.position,
                    heldPosition,
                    interpolation
                );
        }
        else
        {
            closedTaxCodeBook.transform.position =
                heldPosition;
        }

        if (preserveClosedBookRotation)
        {
            closedTaxCodeBook.transform.rotation = deskSurface != null
                ? DeskMotion.HeldRotation(originalClosedRotation, dragLag, deskSurface.transform.up)
                : originalClosedRotation;
        }
    }


    public void OnPointerDown(
        PointerEventData eventData
    )
    {
        if (!isActiveAndEnabled ||
            !allowDragging ||
            closedTaxCodeBook == null ||
            mainCamera == null)
        {
            return;
        }

        pointerPosition = eventData.position;

        if (deskAnimator != null)
            deskAnimator.Complete(closedTaxCodeBook.transform);

        pointerDownScreenPosition =
            eventData.position;

        hasMovedDuringDrag = false;
        isDragging = true;
        currentDropValid = true;
        dragStartPosition = closedTaxCodeBook.transform.position;
        dragTargetPosition = dragStartPosition;
        hasDragTarget = true;

        eventCamera = eventData.pressEventCamera != null ? eventData.pressEventCamera : mainCamera;
        Ray ray = eventCamera.ScreenPointToRay(eventData.position);

        if (drawDebugRay)
        {
            Debug.DrawRay(
                ray.origin,
                ray.direction *
                debugRayLength,
                debugRayColor,
                2f
            );
        }

        Vector3 pointerWorldPosition;
        if (deskSurface != null)
        {
            if (!deskSurface.TryGetPointerPoint(ray, dragStartPosition, out pointerWorldPosition))
            {
                isDragging = false;
                hasDragTarget = false;
                currentDropValid = false;
                return;
            }
        }
        else
        {
            dragPlane = new Plane(mainCamera.transform.forward, closedTaxCodeBook.transform.position);
            if (!dragPlane.Raycast(ray, out float enter))
            {
                isDragging = false;
                hasDragTarget = false;
                currentDropValid = false;
                return;
            }
            pointerWorldPosition = ray.GetPoint(enter);
        }

        dragOffset =
            closedTaxCodeBook.transform.position -
            pointerWorldPosition;

        dragTargetPosition =
            closedTaxCodeBook.transform.position;

        hasDragTarget = true;
        UpdateOutlineFocus();

        DebugLog("Tax code book dragging started.");
    }


    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!isActiveAndEnabled || !allowDragging || closedTaxCodeBook == null)
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


    public void OnDrag(
        PointerEventData eventData
    )
    {
        if (!isDragging ||
            closedTaxCodeBook == null)
        {
            return;
        }

        pointerPosition = eventData.position;

        float screenDistance =
            Vector2.Distance(
                pointerDownScreenPosition,
                eventData.position
            );

        if (screenDistance >= minimumDragDistance)
        {
            hasMovedDuringDrag = true;
        }

        UpdateDragTarget(
            eventData.position
        );
    }


    public void OnPointerUp(
        PointerEventData eventData
    )
    {
        if (!isDragging)
        {
            return;
        }

        pointerPosition = eventData.position;

        float screenDistance =
            Vector2.Distance(
                pointerDownScreenPosition,
                eventData.position
            );

        UpdateDragTarget(
            eventData.position
        );

        if (!hasMovedDuringDrag ||
            screenDistance < minimumDragDistance)
        {
            closedTaxCodeBook.transform.position = dragStartPosition;
            if (preserveClosedBookRotation)
                closedTaxCodeBook.transform.rotation = originalClosedRotation;
            isDragging = false;
            hasDragTarget = false;
            currentDropValid = false;
            isPointerOver = false;
            UpdateOutlineFocus();

            DebugLog(
                "No meaningful drag detected."
            );

            return;
        }

        if (!currentDropValid)
        {
            closedTaxCodeBook.transform.position = hasDragTarget ? dragTargetPosition : dragStartPosition;
            if (preserveClosedBookRotation)
                closedTaxCodeBook.transform.rotation = originalClosedRotation;
            if (deskAnimator != null)
                deskAnimator.PlaySettle(closedTaxCodeBook.transform);
            isDragging = false;
            hasDragTarget = false;
            isPointerOver = false;
            UpdateOutlineFocus();
            return;
        }

        if (hasDragTarget)
        {
            closedTaxCodeBook.transform.position =
                dragTargetPosition;
        }

        if (preserveClosedBookRotation)
        {
            closedTaxCodeBook.transform.rotation =
                originalClosedRotation;
        }

        isDragging = false;
        hasDragTarget = false;
        isPointerOver = false;
        UpdateOutlineFocus();

        ShowOpenTaxCodeBook(eventData.position);
    }


    private void UpdateDragTarget(
        Vector2 screenPosition
    )
    {
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
                ray.direction *
                debugRayLength,
                debugRayColor,
                0.05f
            );
        }

        if (deskSurface != null)
        {
            // Pointer off the desk plane: keep the last safe pose, do not open on release.
            if (!deskSurface.TryGetPointerPoint(ray, dragStartPosition, out Vector3 surfacePoint))
            {
                currentDropValid = false;
                return;
            }

            // A blocked move keeps the last valid target, which stays a valid drop.
            if (deskSurface.TryClampPosition(closedTaxCodeBook.transform, placementCollider,
                    surfacePoint + dragOffset, out Vector3 clampedPosition))
            {
                dragTargetPosition = clampedPosition;
            }

            hasDragTarget = true;
            currentDropValid = true;
            return;
        }

        if (!dragPlane.Raycast(ray, out float enter))
        {
            currentDropValid = false;
            return;
        }

        Vector3 pointerWorldPosition =
            ray.GetPoint(enter);

        dragTargetPosition =
            pointerWorldPosition +
            dragOffset;

        hasDragTarget = true;
        currentDropValid = true;
    }


    /// <summary>
    /// Computes where the open book should sit so that its
    /// sideways/up-down position matches the given screen point,
    /// while keeping its original distance from the camera
    /// (depth is never changed).
    /// </summary>
    private Vector3 GetOpenBookPositionForScreenPoint(
        Vector2 screenPosition
    )
    {
        if (mainCamera == null)
        {
            return originalOpenPosition;
        }

        /*
         * Plane perpendicular to the camera, passing through the
         * open book's original position. Any point on this plane
         * shares the same depth-from-camera as the original position,
         * so raycasting onto it only changes sideways/up-down placement.
         */
        Plane openBookPlane = new Plane(
            mainCamera.transform.forward,
            originalOpenPosition
        );

        Ray ray =
            mainCamera.ScreenPointToRay(
                screenPosition
            );

        if (!openBookPlane.Raycast(ray, out float enter))
        {
            DebugLog(
                "Could not project drop point onto the open book's plane. " +
                "Falling back to the original open book position."
            );

            return originalOpenPosition;
        }

        return ray.GetPoint(enter);
    }


    private void ShowOpenTaxCodeBook(Vector2 dropScreenPosition)
    {
        if (closedTaxCodeBook == null ||
            openTaxCodeBook == null)
        {
            return;
        }

        isPointerOver = false;
        SetOutlineFocused(false);

        closedTaxCodeBook.SetActive(false);

        if (positionOpenBookAtDropPoint)
        {
            openTaxCodeBook.transform.position =
                GetOpenBookPositionForScreenPoint(dropScreenPosition);

            DebugLog(
                $"Open book positioned at drop point: " +
                $"{openTaxCodeBook.transform.position}"
            );
        }

        openTaxCodeBook.SetActive(true);

        if (deskAnimator != null)
            deskAnimator.PlayOpen(openTaxCodeBook.transform, closedTaxCodeBook.transform.position);

        // Play paper sound when the tax code book opens.
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayPaperSFX();
        }

        DebugLog(
            "Open tax code book shown."
        );


        if (taxCodeBookUI != null)
        {
            taxCodeBookUI.Show();

            DebugLog(
                "TaxCodeBookUI.Show() called."
            );
        }
        else
        {
            Debug.LogWarning(
                "[TaxCodeBook3DDrag] TaxCodeBookUI is not assigned."
            );
        }
    }


    public void ResetTaxCodeBook()
    {
        CompleteDeskMotion();
        isDragging = false;
        hasDragTarget = false;
        hasMovedDuringDrag = false;
        isPointerOver = false;
        SetOutlineFocused(false);

        if (closedTaxCodeBook != null)
        {
            PlaceClosedBookAtOrigin();
            closedTaxCodeBook.SetActive(true);
        }

        if (openTaxCodeBook != null)
        {
            /*
             * Restore the open book to its original position so the
             * next drop is measured from the same reference depth.
             */
            openTaxCodeBook.transform.position =
                originalOpenPosition;

            openTaxCodeBook.SetActive(false);
        }

        DebugLog(
            "Tax code book reset."
        );
    }


    /// <summary>
    /// Puts the book back exactly where it started on the desk: an open book
    /// closes its page UI and flies back to that spot, a moved closed book
    /// slides back.
    /// </summary>
    public void ReturnToOriginalPlace(bool animate)
    {
        if (!enabled || !allowDragging || closedTaxCodeBook == null)
            return;

        CompleteDeskMotion();
        isDragging = false;
        hasDragTarget = false;
        isPointerOver = false;
        SetOutlineFocused(false);

        bool canAnimate = animate && deskAnimator != null;
        if (openTaxCodeBook != null && openTaxCodeBook.activeSelf)
        {
            void Finish()
            {
                if (taxCodeBookUI != null)
                    taxCodeBookUI.Hide();
                openTaxCodeBook.SetActive(false);
                openTaxCodeBook.transform.SetPositionAndRotation(originalOpenPosition, originalOpenRotation);
                openTaxCodeBook.transform.localScale = originalOpenScale;
                PlaceClosedBookAtOrigin();
                closedTaxCodeBook.SetActive(true);
                if (canAnimate)
                    deskAnimator.PlaySettle(closedTaxCodeBook.transform);
            }

            if (canAnimate)
                deskAnimator.PlayClose(openTaxCodeBook.transform, originalClosedPosition, Finish);
            else
                Finish();
            return;
        }

        if (!closedTaxCodeBook.activeSelf)
            return;

        if (canAnimate)
        {
            closedTaxCodeBook.transform.localScale = originalClosedScale;
            deskAnimator.PlaySlide(closedTaxCodeBook.transform, originalClosedPosition, originalClosedRotation);
        }
        else
        {
            PlaceClosedBookAtOrigin();
        }
    }

    private void PlaceClosedBookAtOrigin()
    {
        closedTaxCodeBook.transform.SetPositionAndRotation(originalClosedPosition, originalClosedRotation);
        closedTaxCodeBook.transform.localScale = originalClosedScale;
    }

    private void CompleteDeskMotion()
    {
        if (deskAnimator == null)
            return;
        if (closedTaxCodeBook != null)
            deskAnimator.Complete(closedTaxCodeBook.transform);
        if (openTaxCodeBook != null)
            deskAnimator.Complete(openTaxCodeBook.transform);
    }


    private void DebugLog(
        string message
    )
    {
        if (enableDebugLogs)
        {
            Debug.Log(
                $"[TaxCodeBook3DDrag] {message}"
            );
        }
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
            allowDragging &&
            (isDragging || isPointerOver && !pointerBlocked)
        );
    }


    private void SetOutlineFocused(bool focused)
    {
        if (focused && (!isActiveAndEnabled || !allowDragging))
        {
            return;
        }

        if (focused && highlightEffect == null && closedTaxCodeBook != null)
        {
            highlightEffect = HighlightEffect.For(closedTaxCodeBook.transform);
        }

        if (highlightEffect != null)
        {
            highlightEffect.SetFocused(focused);
        }
    }
}
