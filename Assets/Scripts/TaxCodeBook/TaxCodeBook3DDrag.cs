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
    private Vector3 heldPointerPosition;

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

    /// <summary>The closed book is being carried by the finger (T10: the hotbar lights the slot under it).</summary>
    public bool IsCarrying => isDragging && hasMovedDuringDrag;

    private bool isPointerOver;

    private Vector2 pointerPosition;
    private Collider placementCollider;
    private Camera eventCamera;
    private Vector3 dragStartPosition;
    private bool currentDropValid;

    private HighlightEffect highlightEffect;
    private bool heldTinted;


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

        ApplyRedCover(closedTaxCodeBook);
        ApplyRedCover(openTaxCodeBook);
        // The open book's slim, stylized hardcover (matches the Case Folder; replaces the thick model's look).
        if (openTaxCodeBook != null && taxCodeBookUI != null)
            TaxCodeBookOpenLook.Apply(openTaxCodeBook, taxCodeBookUI.PaperRoot as RectTransform);

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


    private void ClearHeldTint()
    {
        if (!heldTinted) return;
        heldTinted = false;
        SetHeldTint(closedTaxCodeBook, false);
    }

    private void OnDisable()
    {
        ClearHeldTint();
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

        // A held book reads as "in your hand": red-tinted while it is dragged.
        bool tint = isDragging && hasMovedDuringDrag;
        if (tint != heldTinted && closedTaxCodeBook != null)
        {
            heldTinted = tint;
            SetHeldTint(closedTaxCodeBook, tint);
        }

        if (!isDragging ||
            !hasDragTarget ||
            closedTaxCodeBook == null)
        {
            return;
        }

        Vector3 heldPosition = heldPointerPosition;
        if (deskSurface != null)
            heldPosition += deskSurface.DragLiftOffset;

        // Always a stiff exponential follow: responsive, but finger jitter and
        // uneven frames never shake the held object (it used to snap once moved).
        if (animateDragging)
        {
            float stiffness = hasMovedDuringDrag ? Mathf.Max(dragSmoothness, 28f) : dragSmoothness;
            float interpolation = 1f - Mathf.Exp(-stiffness * Mathf.Min(Time.unscaledDeltaTime, 1f / 30f));
            closedTaxCodeBook.transform.position = Vector3.Lerp(closedTaxCodeBook.transform.position, heldPosition, interpolation);
        }
        else
        {
            closedTaxCodeBook.transform.position = heldPosition;
        }

        if (preserveClosedBookRotation)
        {
            closedTaxCodeBook.transform.rotation = originalClosedRotation;
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
        heldPointerPosition = dragStartPosition;
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
        UpdateDragTarget(eventData.position);
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

        if (hasMovedDuringDrag && TryStoreAt != null && TryStoreAt(this, eventData.position))
        {
            isDragging = false;
            hasDragTarget = false;
            currentDropValid = false;
            isPointerOver = false;
            SetOutlineFocused(false);
            return;
        }

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
            if (!deskSurface.TryGetPointerPoint(ray, dragStartPosition + deskSurface.DragLiftOffset, out Vector3 surfacePoint))
            {
                currentDropValid = false;
                return;
            }

            // The lifted book follows the finger; only its final resting pose is constrained.
            heldPointerPosition = surfacePoint + dragOffset - deskSurface.DragLiftOffset;
            if (deskSurface.TryClampPosition(closedTaxCodeBook.transform, placementCollider,
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
            currentDropValid = false;
            return;
        }

        Vector3 pointerWorldPosition =
            ray.GetPoint(enter);

        dragTargetPosition =
            pointerWorldPosition +
            dragOffset;
        heldPointerPosition = dragTargetPosition;

        hasDragTarget = true;
        currentDropValid = true;
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

        Camera view = HeldOnScreen.ActiveCamera(mainCamera);
        if (positionOpenBookAtDropPoint && view != null)
        {
            // The printed page opens exactly where the finger let go (2026-10-26), at the open
            // book's reading depth; a shortcut open (no finger) uses where the closed book lay.
            Vector2 target = dropScreenPosition != Vector2.zero
                ? dropScreenPosition
                : (Vector2)view.WorldToScreenPoint(VisualCentre(closedTaxCodeBook));
            openTaxCodeBook.transform.SetPositionAndRotation(originalOpenPosition, originalOpenRotation);
            openTaxCodeBook.transform.localScale = originalOpenScale;
            Transform page = taxCodeBookUI != null ? taxCodeBookUI.PaperRoot : null;
            UprightToCamera(openTaxCodeBook.transform, page, view);
            RectTransform pageRect = page as RectTransform;
            Vector3 anchor = pageRect != null ? pageRect.TransformPoint(pageRect.rect.center) : VisualCentre(openTaxCodeBook);
            HeldOnScreen.CentreUnder(openTaxCodeBook.transform, anchor, view, target);

            DebugLog(
                $"Open book positioned at drop point: " +
                $"{openTaxCodeBook.transform.position}"
            );
        }

        openTaxCodeBook.SetActive(true);
        // The whole open book shows: a drop near an edge is moved in (same depth, never off screen).
        if (view != null)
            openTaxCodeBook.transform.position = HeldOnScreen.FitOnScreen(openTaxCodeBook.transform, view);

        if (deskAnimator != null)
            deskAnimator.PlayOpen(openTaxCodeBook.transform, closedTaxCodeBook.transform.position);

        // Match the same opening cue used by the floating book in both levels.
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayEffect("sfx_book_open_01");
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


    /// <summary>
    /// Turns a document about its printed page's own normal so the page reads
    /// upright for this camera (and turns it round first if the page faces
    /// away). The desk tilt is kept; an already upright page is unchanged.
    /// </summary>
    public static void UprightToCamera(Transform model, Transform page, Camera camera)
    {
        if (model == null || page == null || camera == null) return;
        Vector3 normal = page.forward;   // a UI page is read looking along its +z
        Vector3 toPage = page.position - camera.transform.position;
        if (Vector3.Dot(normal, toPage) < 0f)
        {
            model.RotateAround(page.position, page.up, 180f);
            normal = page.forward;
        }
        Vector3 wantedUp = Vector3.ProjectOnPlane(camera.transform.up, normal);
        if (wantedUp.sqrMagnitude < 1e-6f) return;
        float roll = Vector3.SignedAngle(page.up, wantedUp, normal);
        if (Mathf.Abs(roll) > 0.5f) model.RotateAround(page.position, normal, roll);
    }

    /// <summary>The open book model (shown while the book is read).</summary>
    public GameObject OpenBook => openTaxCodeBook;

    /// <summary>The closed book on the desk.</summary>
    public GameObject ClosedBook => closedTaxCodeBook;

    public TaxCodeBookUI BookUI => taxCodeBookUI;

    public bool IsBookOpen => openTaxCodeBook != null && openTaxCodeBook.activeSelf;

    /// <summary>
    /// Optional level hook (Level 2's Tax Code Book icon): a dragged closed
    /// book released at this screen point is stored there instead of being
    /// opened. Return true when the book was taken.
    /// </summary>
    public static System.Func<TaxCodeBook3DDrag, Vector2, bool> TryStoreAt;

    /// <summary>Optional level hook: the book lives in a HUD icon, so desk resets keep it off the desk.</summary>
    public static System.Func<bool> IsStoredElsewhere;

    private static bool StoredElsewhere => IsStoredElsewhere != null && IsStoredElsewhere();

    private static readonly Color CoverRed = new Color(0.62f, 0.12f, 0.13f, 1f);

    /// <summary>
    /// Gives the book's grey cover parts the same red as the rest of the
    /// cover, once and for good (per-slot property blocks; shared materials and
    /// the white page block are untouched), so it never changes colour.
    /// </summary>
    public static void ApplyRedCover(GameObject model)
    {
        if (model == null) return;
        var block = new MaterialPropertyBlock();
        foreach (MeshRenderer renderer in model.GetComponentsInChildren<MeshRenderer>(true))
        {
            if (renderer.GetComponentInParent<Canvas>() != null || renderer.GetComponent<TMPro.TMP_Text>() != null) continue;
            Material[] materials = renderer.sharedMaterials;
            for (int i = 0; i < materials.Length; i++)
            {
                if (materials[i] == null) continue;
                Color.RGBToHSV(materials[i].color, out _, out float saturation, out float value);
                // Grey (not white paper, not already a colour): the cover's side and spine.
                if (saturation > 0.2f || value < 0.15f || value > 0.86f) continue;
                renderer.GetPropertyBlock(block, i);
                Color red = new Color(CoverRed.r, CoverRed.g, CoverRed.b, materials[i].color.a);
                block.SetColor("_BaseColor", red);
                block.SetColor("_Color", red);
                renderer.SetPropertyBlock(block, i);
            }
        }
    }

    /// <summary>
    /// Kept for callers that mark a held book: the book is already red, so
    /// holding it no longer changes its colour (it only reasserts the red cover).
    /// </summary>
    public static void SetHeldTint(GameObject model, bool held) => ApplyRedCover(model);

    /// <summary>Closes an open desk book into the HUD icon: pages hidden, open model reset and hidden, closed book parked.</summary>
    public void ParkOpenBook()
    {
        if (taxCodeBookUI != null && taxCodeBookUI.IsShowing) taxCodeBookUI.Hide();
        if (openTaxCodeBook != null)
        {
            if (deskAnimator != null) deskAnimator.Complete(openTaxCodeBook.transform);
            openTaxCodeBook.transform.SetPositionAndRotation(originalOpenPosition, originalOpenRotation);
            openTaxCodeBook.transform.localScale = originalOpenScale;
            openTaxCodeBook.SetActive(false);
        }
        ParkClosedBook();
    }

    /// <summary>The closed book's resting spot on the desk (world).</summary>
    public Vector3 RestingClosedPosition => originalClosedPosition;

    /// <summary>Brings a carried book back: closed, on its desk spot, visible, with the desk settle motion.</summary>
    public void UnparkClosedBook()
    {
        ClearHeldTint();
        if (closedTaxCodeBook == null) return;
        if (openTaxCodeBook != null && openTaxCodeBook.activeSelf) return;
        PlaceClosedBookAtOrigin();
        closedTaxCodeBook.SetActive(true);
        ApplyRedCover(closedTaxCodeBook);
        // Back from the bar: it drops onto its stack (2026-10-26), like the folder.
        if (deskAnimator != null) deskAnimator.PlayDrop(closedTaxCodeBook.transform);
    }

    /// <summary>Puts the closed book back on its desk spot, hidden (it is now carried in the HUD icon).</summary>
    public void ParkClosedBook()
    {
        ClearHeldTint();
        CompleteDeskMotion();
        isDragging = false;
        hasDragTarget = false;
        isPointerOver = false;
        SetOutlineFocused(false);
        if (closedTaxCodeBook == null) return;
        PlaceClosedBookAtOrigin();
        closedTaxCodeBook.SetActive(false);
    }

    /// <summary>
    /// HUD shortcut (Level 2 Tax Code Book icon): opens the book where it
    /// lies on the desk, exactly as a drag-and-release would.
    /// </summary>
    public bool OpenFromHud()
    {
        if (!allowDragging || isDragging || closedTaxCodeBook == null || openTaxCodeBook == null ||
            !closedTaxCodeBook.activeInHierarchy || openTaxCodeBook.activeSelf)
            return false;
        CompleteDeskMotion();
        ShowOpenTaxCodeBook(Vector2.zero);
        return true;
    }

    public void ResetTaxCodeBook()
    {
        if (StoredElsewhere) return;
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

    internal static Vector3 VisualCentre(GameObject model)
    {
        Bounds bounds = new Bounds(model.transform.position, Vector3.zero);
        bool any = false;
        foreach (Renderer renderer in model.GetComponentsInChildren<Renderer>(true))
        {
            if (!(renderer is MeshRenderer) || !renderer.enabled || renderer.GetComponentInParent<Canvas>() != null ||
                renderer.GetComponentInParent<TMPro.TMP_Text>() != null) continue;
            if (!any) { bounds = renderer.bounds; any = true; }
            else bounds.Encapsulate(renderer.bounds);
        }
        return bounds.center;
    }


    /// <summary>
    /// Puts the book back exactly where it started on the desk: an open book
    /// closes its page UI and flies back to that spot, a moved closed book
    /// slides back.
    /// </summary>
    public void ReturnToOriginalPlace(bool animate)
    {
        if (!enabled || !allowDragging || closedTaxCodeBook == null || StoredElsewhere)
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
