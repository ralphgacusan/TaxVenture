using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;

/// <summary>
/// Reusable floating window/panel controller.
///
/// FEATURES:
/// - Multiple floating windows can exist simultaneously
/// - Each window can be dragged independently
/// - The window being interacted with becomes the TOPMOST window
/// - Opening a window makes it the active/topmost document
/// - Windows can exist under different parents
/// - Uses an independent Canvas for reliable UI layering
/// - HUD / Case Folder icon stays below document windows
/// - Dragging a document onto its HUD icon closes that document
/// - Supports mouse and touch
///
/// LAYERING:
///
///     Drag Visual / Ghost
///             ↑
///     Active Document
///             ↑
///     Other Documents
///             ↑
///     HUD / Case Folder
///
/// IMPORTANT:
/// This script does NOT close other FloatingWindows when
/// another window is opened.
///
/// IMPORTANT INSPECTOR SETUP:
/// You can drag the Case Folder Icon from the Hierarchy
/// directly into the "Return Icon" field below.
///
/// The Case Folder Icon should have a RectTransform.
/// </summary>
public class FloatingWindow :
    MonoBehaviour,
    IPointerDownHandler,
    IDragHandler,
    IPointerUpHandler
{
    // =========================================================
    // WINDOW
    // =========================================================

    [Header("Window")]

    [Tooltip(
        "Leave empty to automatically use this GameObject."
    )]
    [SerializeField] private RectTransform window;

    [Tooltip(
        "Canvas containing this window. " +
        "Leave empty to automatically find the parent Canvas."
    )]
    [SerializeField] private Canvas canvas;


    // =========================================================
    // HUD / CASE FOLDER ICON
    // =========================================================

    [Header("HUD / Case Folder Icon")]

    [Tooltip(
        "Drag the Case Folder Icon from the Hierarchy here. " +
        "The icon must have a RectTransform. " +
        "Dragging this document onto the icon closes the document."
    )]
    [SerializeField] private RectTransform returnIcon;

    [Tooltip(
        "Distance from the Case Folder icon required to close the window."
    )]
    [SerializeField] private float returnIconDistance = 120f;


    // =========================================================
    // OPENING POSITION
    // =========================================================

    [Header("Opening Position")]

    [Tooltip(
        "Horizontal offset applied when opening from a HUD icon."
    )]
    [SerializeField] private float openingOffsetX = 450f;

    [Tooltip(
        "Vertical offset applied when opening from a HUD icon."
    )]
    [SerializeField] private float openingOffsetY = 0f;


    // =========================================================
    // DRAG SETTINGS
    // =========================================================

    [Header("Dragging")]

    [SerializeField] private bool allowDragging = true;

    [Tooltip(
        "When enabled, touching/clicking the window brings it "
        + "to the front."
    )]
    [SerializeField] private bool bringToFront = true;


    // =========================================================
    // OPEN / CLOSE
    // =========================================================

    [Header("Open / Close")]

    [Tooltip(
        "Kept for compatibility with previous versions."
    )]
    [SerializeField] private bool resetPositionOnOpen = false;

    [Tooltip(
        "Animation duration when closing to the HUD icon."
    )]
    [SerializeField] private float closeAnimationDuration = 0.2f;


    // =========================================================
    // LAYERING
    // =========================================================

    [Header("Layering")]

    [Tooltip(
        "Starting sorting order for document windows. " +
        "Must be higher than the Case Folder HUD."
    )]
    [SerializeField] private int baseSortingOrder = 1100;

    [Tooltip(
        "Maximum sorting order available to document windows. " +
        "Keep this below the drag visual / ghost canvas."
    )]
    [SerializeField] private int maximumWindowSortingOrder = 1900;

    [Tooltip(
        "Sorting order used by the Case Folder HUD. " +
        "Documents will always be above this value."
    )]
    [SerializeField] private int iconSortingOrder = 1000;


    // =========================================================
    // INTERNAL
    // =========================================================

    private RectTransform parentRect;

    private Vector2 originalPosition;

    private Vector2 dragOffset;

    private bool isDragging;

    private bool isClosing;

    private Camera eventCamera;

    private Canvas windowCanvas;


    // =========================================================
    // PAPER MOTION (opt-in, Office HUD paper windows)
    // =========================================================

    private const float PaperOpenDuration = 0.42f;
    private const float PaperCloseDuration = 0.3f;
    private const float PaperIconScale = 0.12f;
    private const float PaperArc = 60f;

    private bool paperMotion;
    private float paperTilt;
    private float paperRestScale = 1f;
    private CanvasGroup paperGroup;
    private bool paperShown;
    private bool paperOpening;
    private RectTransform paperIcon;
    private Vector2 paperTarget;
    private float paperOpenDelay;
    private Vector2 pendingOpenOffset;
    private Vector2? paperOpenOrigin;

    // Registry so 3D document layers can sort and hit-test against windows.
    public static readonly System.Collections.Generic.List<FloatingWindow> Instances =
        new System.Collections.Generic.List<FloatingWindow>();

    private const float PopDuration = 0.18f;
    private Coroutine popRoutine;

    /// <summary>Raised whenever this window finishes closing (any way).</summary>
    public event System.Action Closed;



    public void SetReturnIcon(RectTransform icon)
    {
        returnIcon = icon;
    }

    public RectTransform GetReturnIcon()
    {
        return returnIcon;
    }
    // =========================================================
    // GLOBAL DOCUMENT LAYER
    // =========================================================

    /*
     * Shared by ALL FloatingWindow instances.
     *
     * Example:
     *
     * Document A = 1101
     * Document B = 1102
     * Document C = 1103
     *
     * User touches Document A:
     *
     * Document A = 1104
     *
     * Therefore A becomes the topmost document.
     *
     * This means:
     *
     * "Whatever document I interact with
     * becomes the first/top layer."
     */

    private static int currentSortingOrder = 1100;


    // =========================================================
    // UNITY - AWAKE
    // =========================================================

    private void Awake()
    {
        if (!Instances.Contains(this))
        {
            Instances.Add(this);
        }

        // -----------------------------------------------------
        // Find window
        // -----------------------------------------------------

        if (window == null)
        {
            window =
                GetComponent<RectTransform>();
        }


        if (window == null)
        {
            Debug.LogError(
                $"[FloatingWindow] {gameObject.name} " +
                "does not have a RectTransform!"
            );

            return;
        }


        // -----------------------------------------------------
        // Find parent Canvas
        // -----------------------------------------------------

        if (canvas == null)
        {
            canvas =
                GetComponentInParent<Canvas>();
        }


        if (canvas == null)
        {
            Debug.LogError(
                $"[FloatingWindow] Could not find Canvas " +
                $"for {gameObject.name}!"
            );

            return;
        }


        // -----------------------------------------------------
        // Find parent RectTransform
        // -----------------------------------------------------

        parentRect =
            window.parent as RectTransform;


        if (parentRect == null)
        {
            Debug.LogError(
                $"[FloatingWindow] {gameObject.name} " +
                "must have a RectTransform parent!"
            );

            return;
        }


        // -----------------------------------------------------
        // Save original position
        // -----------------------------------------------------

        originalPosition =
            window.anchoredPosition;


        // -----------------------------------------------------
        // Get Canvas directly attached to this window
        // -----------------------------------------------------

        windowCanvas =
            window.GetComponent<Canvas>();


        // -----------------------------------------------------
        // Create independent Canvas if needed
        // -----------------------------------------------------

        if (windowCanvas == null)
        {
            windowCanvas =
                window.gameObject.AddComponent<Canvas>();
        }


        // -----------------------------------------------------
        // Enable independent sorting
        // -----------------------------------------------------

        windowCanvas.overrideSorting = true;


        // -----------------------------------------------------
        // Make sure the global counter is above the HUD
        // -----------------------------------------------------

        currentSortingOrder =
            Mathf.Max(
                currentSortingOrder,
                baseSortingOrder
            );


        // -----------------------------------------------------
        // Initial layer
        // -----------------------------------------------------

        windowCanvas.sortingOrder =
            baseSortingOrder;


        Debug.Log(
            $"[FloatingWindow] Initialized: " +
            $"{gameObject.name} | " +
            $"Sorting Order = " +
            $"{windowCanvas.sortingOrder}"
        );
    }


    // =========================================================
    // POINTER DOWN
    // =========================================================

    public void OnPointerDown(
        PointerEventData eventData
    )
    {
        if (window == null ||
            canvas == null ||
            isClosing)
        {
            return;
        }


        // Grabbing a paper window mid-flight lands it immediately.
        if (paperOpening)
        {
            FinishPaperOpen();
        }


        // -----------------------------------------------------
        // Determine event camera
        // -----------------------------------------------------

        eventCamera =
            canvas.renderMode ==
            RenderMode.ScreenSpaceOverlay
                ? null
                : eventData.pressEventCamera;


        // -----------------------------------------------------
        // Calculate drag offset
        // -----------------------------------------------------

        if (RectTransformUtility
            .ScreenPointToLocalPointInRectangle(
                parentRect,
                eventData.position,
                eventCamera,
                out Vector2 localPointer
            ))
        {
            dragOffset =
                window.anchoredPosition -
                localPointer;
        }


        // -----------------------------------------------------
        // Bring THIS document to front
        // -----------------------------------------------------

        if (bringToFront)
        {
            BringToFront();
        }


        isDragging = true;


        Debug.Log(
            $"[FloatingWindow] " +
            $"{gameObject.name}: " +
            "TOUCHED -> FRONT"
        );
    }


    // =========================================================
    // BRING WINDOW TO FRONT
    // =========================================================

    private void BringWindowToFront()
    {
        if (windowCanvas == null)
        {
            return;
        }


        // -----------------------------------------------------
        // Make sure current layer is valid
        // -----------------------------------------------------

        if (currentSortingOrder <
            iconSortingOrder)
        {
            currentSortingOrder =
                iconSortingOrder;
        }


        // -----------------------------------------------------
        // Increase global document layer
        // -----------------------------------------------------

        currentSortingOrder++;


        // -----------------------------------------------------
        // Prevent exceeding the maximum layer
        // -----------------------------------------------------

        if (currentSortingOrder >=
            maximumWindowSortingOrder)
        {
            // Renumber every window from the bottom, keeping their order,
            // then give this one the next (top) layer. The old reset
            // returned without assigning a layer, so windows touched after
            // it could stay stuck below the others.
            RebuildSortingOrders(this);
        }


        // -----------------------------------------------------
        // Make absolutely sure the document remains
        // above the Case Folder HUD.
        // -----------------------------------------------------

        currentSortingOrder =
            Mathf.Max(
                currentSortingOrder,
                iconSortingOrder + 1
            );


        // -----------------------------------------------------
        // Assign sorting order
        // -----------------------------------------------------

        windowCanvas.overrideSorting = true;

        windowCanvas.sortingOrder =
            currentSortingOrder;


        // -----------------------------------------------------
        // Also move this object to the end of its hierarchy.
        //
        // This is useful when multiple windows share a
        // hierarchy.
        // -----------------------------------------------------

        window.SetAsLastSibling();


        Debug.Log(
            $"[FloatingWindow] " +
            $"{gameObject.name}: " +
            $"NOW FRONT | Sorting Order = " +
            $"{windowCanvas.sortingOrder}"
        );
    }


    // =========================================================
    // REBUILD SORTING ORDERS
    // =========================================================

    private static void RebuildSortingOrders(FloatingWindow except)
    {
        // Windows and 3D document layers share one order: renumber both
        // from the bottom, keeping their relative order.
        var layered = new System.Collections.Generic.List<(int order, System.Action<int> assign)>();
        for (int i = 0; i < Instances.Count; i++)
        {
            FloatingWindow w = Instances[i];
            if (w == null || w == except || w.windowCanvas == null)
            {
                continue;
            }
            Canvas c = w.windowCanvas;
            layered.Add((c.sortingOrder, order => c.sortingOrder = order));
        }
        for (int i = 0; i < Doc3DLayer.All.Count; i++)
        {
            Doc3DLayer layer = Doc3DLayer.All[i];
            if (layer != null)
            {
                layered.Add((layer.SortingOrder, layer.AssignSortingOrder));
            }
        }

        layered.Sort((a, b) => a.order.CompareTo(b.order));
        int next = 1101;
        for (int i = 0; i < layered.Count; i++)
        {
            layered[i].assign(next++);
        }

        currentSortingOrder = next;
    }

    private static void RebuildSortingOrders()
    {
        /*
         * We intentionally do not try to search the entire
         * scene here.
         *
         * Instead, reset the global counter to just above
         * the HUD layer.
         *
         * The next interaction will assign a fresh top layer.
         *
         * Existing windows keep their current layers until
         * they are interacted with again.
         */

        currentSortingOrder = 1101;
    }


    // =========================================================
    // DRAG
    // =========================================================

    public void OnDrag(
        PointerEventData eventData
    )
    {
        if (!isDragging ||
            !allowDragging ||
            isClosing)
        {
            return;
        }


        if (!RectTransformUtility
            .ScreenPointToLocalPointInRectangle(
                parentRect,
                eventData.position,
                eventCamera,
                out Vector2 localPointer
            ))
        {
            return;
        }


        // -----------------------------------------------------
        // Move window with pointer
        // -----------------------------------------------------

        window.anchoredPosition =
            localPointer +
            dragOffset;
    }


    // =========================================================
    // POINTER UP
    // =========================================================

    public void OnPointerUp(
        PointerEventData eventData
    )
    {
        if (!isDragging ||
            isClosing)
        {
            return;
        }


        isDragging = false;


        // -----------------------------------------------------
        // Check Case Folder icon
        // -----------------------------------------------------

        if (returnIcon != null &&
            IsOverReturnIcon(
                eventData.position
            ))
        {
            Debug.Log(
                $"[FloatingWindow] " +
                $"{gameObject.name}: " +
                "DROPPED ON CASE FOLDER ICON"
            );


            StartCoroutine(
                CloseToIcon()
            );


            return;
        }


        Debug.Log(
            $"[FloatingWindow] " +
            $"{gameObject.name}: " +
            "DRAG FINISHED"
        );
    }


    // =========================================================
    // CHECK RETURN ICON
    // =========================================================

    private bool IsOverReturnIcon(
        Vector2 screenPosition
    )
    {
        if (returnIcon == null)
        {
            return false;
        }


        Vector3[] corners =
            new Vector3[4];


        returnIcon.GetWorldCorners(
            corners
        );


        Vector2 iconCenter =
            (corners[0] + corners[2]) * 0.5f;


        float distance =
            Vector2.Distance(
                screenPosition,
                iconCenter
            );


        return distance <=
            returnIconDistance;
    }


    // =========================================================
    // CLOSE TO ICON
    // =========================================================

    private IEnumerator CloseToIcon()
    {
        isClosing = true;


        Vector2 start =
            window.anchoredPosition;


        Vector2 targetPosition =
            start;


        if (returnIcon != null)
        {
            Camera closeCamera =
                GetCanvasEventCamera();


            Vector2 iconScreenPosition =
                RectTransformUtility.WorldToScreenPoint(
                    closeCamera,
                    returnIcon.position
                );


            if (RectTransformUtility
                .ScreenPointToLocalPointInRectangle(
                    parentRect,
                    iconScreenPosition,
                    closeCamera,
                    out Vector2 localPosition
                ))
            {
                targetPosition =
                    GetAnchoredPositionForCenter(
                        localPosition
                    );
            }
        }


        float elapsed = 0f;


        while (elapsed <
               closeAnimationDuration)
        {
            elapsed +=
                Time.deltaTime;


            float t =
                Mathf.Clamp01(
                    elapsed /
                    closeAnimationDuration
                );


            window.anchoredPosition =
                Vector2.Lerp(
                    start,
                    targetPosition,
                    t
                );


            // Paper windows shrink and fade into the icon.
            if (paperMotion)
            {
                float scale = paperRestScale * Mathf.Lerp(1f, PaperIconScale, t * t);
                window.localScale = new Vector3(scale, scale, 1f);
                if (paperGroup != null)
                {
                    paperGroup.alpha = 1f - Mathf.Clamp01((t - 0.5f) / 0.5f);
                }
            }


            yield return null;
        }


        window.anchoredPosition =
            targetPosition;


        CloseWindowImmediate();
    }


    // =========================================================
    // OPEN - CENTER
    // =========================================================

    public void OpenWindow()
    {
        if (window == null ||
            parentRect == null)
        {
            return;
        }


        StopAllCoroutines();


        isClosing = false;

        isDragging = false;


        // -----------------------------------------------------
        // Activate
        // -----------------------------------------------------

        gameObject.SetActive(true);


        // -----------------------------------------------------
        // Reset position to center
        // -----------------------------------------------------

        window.anchoredPosition =
            GetParentCenterPosition() + pendingOpenOffset;

        pendingOpenOffset = Vector2.zero;


        // -----------------------------------------------------
        // IMPORTANT:
        //
        // Opening a document makes THAT document topmost.
        // -----------------------------------------------------

        BringWindowToFront();

        StartPaperOpen();


        Debug.Log(
            $"[FloatingWindow] " +
            $"{gameObject.name}: " +
            $"OPENED | Sorting Order = " +
            $"{windowCanvas.sortingOrder}"
        );
    }


    // =========================================================
    // OPEN AT SCREEN POSITION
    // =========================================================

    public void OpenWindowAtScreenPosition(
        Vector2 screenPosition
    )
    {
        if (window == null ||
            parentRect == null)
        {
            return;
        }


        StopAllCoroutines();


        isClosing = false;

        isDragging = false;


        // -----------------------------------------------------
        // Activate
        // -----------------------------------------------------

        gameObject.SetActive(true);


        // -----------------------------------------------------
        // Determine camera
        // -----------------------------------------------------

        Camera positionCamera =
            GetCanvasEventCamera();


        // -----------------------------------------------------
        // Convert screen position to parent local position
        // -----------------------------------------------------

        if (RectTransformUtility
            .ScreenPointToLocalPointInRectangle(
                parentRect,
                screenPosition,
                positionCamera,
                out Vector2 localPosition
            ))
        {
            // -------------------------------------------------
            // Apply opening offset
            // -------------------------------------------------

            localPosition +=
                new Vector2(
                    openingOffsetX,
                    openingOffsetY
                );


            // -------------------------------------------------
            // Put visual center at desired position
            // -------------------------------------------------

            window.anchoredPosition =
                GetAnchoredPositionForCenter(
                    localPosition
                );
        }
        else
        {
            // -------------------------------------------------
            // Fallback to center
            // -------------------------------------------------

            window.anchoredPosition =
                GetParentCenterPosition();
        }


        // -----------------------------------------------------
        // Opening from a position also makes this document
        // the active/topmost document.
        // -----------------------------------------------------

        BringWindowToFront();

        StartPaperOpen();


        Debug.Log(
            $"[FloatingWindow] " +
            $"{gameObject.name}: " +
            $"OPENED AT POSITION | " +
            $"Sorting Order = " +
            $"{windowCanvas.sortingOrder}"
        );
    }


    // =========================================================
    // GET PARENT CENTER POSITION
    // =========================================================

    private Vector2 GetParentCenterPosition()
    {
        if (parentRect == null)
        {
            return Vector2.zero;
        }


        Vector3 parentCenterWorld =
            parentRect.TransformPoint(
                parentRect.rect.center
            );


        Vector2 parentCenterLocal =
            parentRect.InverseTransformPoint(
                parentCenterWorld
            );


        return GetAnchoredPositionForCenter(
            parentCenterLocal
        );
    }


    // =========================================================
    // GET WINDOW POSITION FROM CENTER
    // =========================================================

    private Vector2 GetAnchoredPositionForCenter(
        Vector2 desiredCenter
    )
    {
        if (window == null)
        {
            return desiredCenter;
        }


        Rect rect =
            window.rect;


        Vector2 pivotOffset =
            new Vector2(
                (0.5f - window.pivot.x) *
                rect.width,

                (0.5f - window.pivot.y) *
                rect.height
            );


        return desiredCenter +
               pivotOffset;
    }


    // =========================================================
    // GET CANVAS EVENT CAMERA
    // =========================================================

    private Camera GetCanvasEventCamera()
    {
        if (canvas == null)
        {
            return null;
        }


        // -----------------------------------------------------
        // Screen Space Overlay
        // -----------------------------------------------------

        if (canvas.renderMode ==
            RenderMode.ScreenSpaceOverlay)
        {
            return null;
        }


        // -----------------------------------------------------
        // Camera / World Space
        // -----------------------------------------------------

        if (canvas.worldCamera != null)
        {
            return canvas.worldCamera;
        }


        return Camera.main;
    }


    // =========================================================
    // CLOSE
    // =========================================================

    public void CloseWindow()
    {
        if (window == null)
        {
            return;
        }


        // Paper windows tip away and fade before hiding.
        if (paperMotion &&
            paperShown &&
            gameObject.activeInHierarchy)
        {
            if (!isClosing)
            {
                StopAllCoroutines();

                isClosing = true;

                isDragging = false;

                StartCoroutine(PaperClose());
            }

            return;
        }


        CloseWindowImmediate();
    }


    private void CloseWindowImmediate()
    {
        StopAllCoroutines();

        paperShown = false;

        paperOpening = false;

        paperOpenOrigin = null;

        ResetPaperVisual();


        isClosing = false;

        isDragging = false;


        // -----------------------------------------------------
        // Hide ONLY this document.
        //
        // Other documents remain open.
        // -----------------------------------------------------

        gameObject.SetActive(false);


        Debug.Log(
            $"[FloatingWindow] " +
            $"{gameObject.name}: CLOSED"
        );

        Closed?.Invoke();
    }


    private void OnDestroy()
    {
        Instances.Remove(this);
    }


    // =========================================================
    // TOGGLE
    // =========================================================

    public void ToggleWindow()
    {
        bool currentlyOpen =
            IsOpen();


        if (currentlyOpen)
        {
            CloseWindow();
        }
        else
        {
            OpenWindow();
        }
    }


    // =========================================================
    // IS OPEN
    // =========================================================

    public bool IsOpen()
    {
        // A paper window that is animating closed already counts as closed.
        return gameObject.activeSelf &&
               !(paperMotion && isClosing && paperShown);
    }


    // =========================================================
    // PAPER MOTION
    // =========================================================

    /// <summary>
    /// Opt-in open/close motion used by the Office HUD paper windows
    /// (Notes, taxpayer profiles, Case Folder). The window rests at
    /// restTiltDegrees around Z (null keeps its current tilt). With an
    /// icon, it flies out of that HUD icon when opened and back into it
    /// when closed; without one it pops in place. Other windows keep the
    /// original instant behaviour.
    /// </summary>
    public void UsePaperMotion(float? restTiltDegrees, RectTransform icon = null)
    {
        if (window == null)
        {
            window = GetComponent<RectTransform>();
        }

        paperMotion = true;
        paperIcon = icon;

        paperTilt = restTiltDegrees ??
                    (window != null ? Mathf.DeltaAngle(0f, window.localEulerAngles.z) : 0f);

        paperGroup = GetComponent<CanvasGroup>();
        if (paperGroup == null)
        {
            paperGroup = gameObject.AddComponent<CanvasGroup>();
        }

        ResetPaperVisual();
    }

    /// <summary>Delay before the next paper open starts (used to stagger cards).</summary>
    public void SetPaperOpenDelay(float seconds)
    {
        paperOpenDelay = Mathf.Max(0f, seconds);
    }

    /// <summary>
    /// The next paper open grows out of this screen point (e.g. where a
    /// dragged HUD item was dropped) instead of flying from the icon.
    /// </summary>
    public void SetPaperOpenOrigin(Vector2 screenPoint)
    {
        paperOpenOrigin = screenPoint;
    }

    /// <summary>Opens centred, shifted by offset (parent units).</summary>
    public void OpenWindowWithOffset(Vector2 offset)
    {
        pendingOpenOffset = offset;
        OpenWindow();
    }

    public bool UsesPaperMotion => paperMotion;

    /// <summary>
    /// Makes this window the top layer, with a small "pop" for paper windows
    /// so coming to the front is felt.
    /// </summary>
    public void BringToFront()
    {
        bool alreadyTop = windowCanvas != null && windowCanvas.sortingOrder >= currentSortingOrder;
        BringWindowToFront();

        if (paperMotion && !alreadyTop && !paperOpening && !isClosing && gameObject.activeInHierarchy)
        {
            if (popRoutine != null)
            {
                StopCoroutine(popRoutine);
            }
            popRoutine = StartCoroutine(PaperPop());
        }
    }

    private IEnumerator PaperPop()
    {
        float elapsed = 0f;
        while (elapsed < PopDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / PopDuration);
            float scale = paperRestScale * (1f + 0.045f * Mathf.Sin(t * Mathf.PI));
            window.localScale = new Vector3(scale, scale, 1f);
            yield return null;
        }

        window.localScale = Vector3.one * paperRestScale;
        popRoutine = null;
    }

    /// <summary>Sets the resting scale for paper windows that use a scaled presentation.</summary>
    public void SetPaperRestScale(float scale)
    {
        paperRestScale = Mathf.Max(0.01f, scale);
        if (paperMotion && window != null && !paperOpening && !isClosing && popRoutine == null)
            window.localScale = Vector3.one * paperRestScale;
    }

    /// <summary>Current layer of this window (higher = in front).</summary>
    public int SortingOrder => windowCanvas != null ? windowCanvas.sortingOrder : 0;

    public RectTransform WindowRect => window;

    /// <summary>Opt-in for papers that filter 3D coverage at each hit point.</summary>
    public bool UsesPointRaycastFiltering { get; set; }

    public bool ContainsScreenPoint(Vector2 point)
    {
        if (!RectTransformUtility.RectangleContainsScreenPoint(window, point, null)) return false;
        return !UsesPointRaycastFiltering || !(GetComponent<ICanvasRaycastFilter>() is ICanvasRaycastFilter filter) ||
            filter.IsRaycastLocationValid(point, null);
    }

    /// <summary>
    /// Blocks or passes touches. A paper under a 3D document that is in
    /// front of it must let touches through to the document.
    /// </summary>
    public void SetBlocksRaycasts(bool blocks)
    {
        if (paperGroup != null && paperGroup.blocksRaycasts != blocks)
        {
            paperGroup.blocksRaycasts = blocks;
        }
    }

    /// <summary>
    /// Next top layer for something that is not a FloatingWindow (the 3D
    /// desk documents), so everything shares one front-to-back order.
    /// </summary>
    public static int TakeTopSortingOrder()
    {
        currentSortingOrder = Mathf.Max(currentSortingOrder + 1, 1101);
        if (currentSortingOrder >= 1900)
        {
            RebuildSortingOrders(null);
            currentSortingOrder++;
        }
        return currentSortingOrder;
    }

    private void OnEnable()
    {
        // Shown by other code without OpenWindow(): never leave it invisible.
        if (paperMotion && !paperShown)
        {
            ResetPaperVisual();
        }
    }

    private void ResetPaperVisual()
    {
        if (!paperMotion || window == null)
        {
            return;
        }

        window.localScale = Vector3.one * paperRestScale;
        window.localRotation = Quaternion.Euler(0f, 0f, paperTilt);
        if (paperGroup != null)
        {
            paperGroup.alpha = 1f;
        }
    }

    private void StartPaperOpen()
    {
        if (!paperMotion || !gameObject.activeInHierarchy)
        {
            return;
        }

        paperShown = true;
        paperTarget = window.anchoredPosition;
        StartCoroutine(PaperOpen(paperOpenDelay));
        paperOpenDelay = 0f;
    }

    private void FinishPaperOpen()
    {
        StopAllCoroutines();
        paperOpening = false;
        window.anchoredPosition = paperTarget;
        ResetPaperVisual();
    }

    /// <summary>The window position (anchored) that centres it on the HUD icon.</summary>
    private bool TryGetIconPosition(out Vector2 position)
    {
        position = paperTarget;
        if (paperIcon == null || parentRect == null || !paperIcon.gameObject.activeInHierarchy)
        {
            return false;
        }

        Camera camera = GetCanvasEventCamera();
        Vector2 screen = RectTransformUtility.WorldToScreenPoint(camera, paperIcon.TransformPoint(paperIcon.rect.center));
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, screen, camera, out Vector2 local))
        {
            return false;
        }

        position = GetAnchoredPositionForCenter(local);
        return true;
    }

    private bool TryScreenToAnchored(Vector2 screen, out Vector2 position)
    {
        position = paperTarget;
        if (parentRect == null)
        {
            return false;
        }

        Camera camera = GetCanvasEventCamera();
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, screen, camera, out Vector2 local))
        {
            return false;
        }

        position = GetAnchoredPositionForCenter(local);
        return true;
    }

    /// <summary>Point on a soft arc between a and b (the arc bulges upward).</summary>
    private static Vector2 Arc(Vector2 a, Vector2 b, float t)
    {
        Vector2 point = Vector2.LerpUnclamped(a, b, t);
        point.y += Mathf.Sin(Mathf.Clamp01(t) * Mathf.PI) * PaperArc;
        return point;
    }

    private void SetPaperPose(Vector2 position, float scale, float tilt, float alpha)
    {
        window.anchoredPosition = position;
        window.localScale = new Vector3(scale, scale, 1f);
        window.localRotation = Quaternion.Euler(0f, 0f, tilt);
        if (paperGroup != null)
        {
            paperGroup.alpha = alpha;
        }
    }

    private IEnumerator PaperOpen(float delay)
    {
        paperOpening = true;
        bool fromIcon;
        Vector2 start;
        float startScale;
        if (paperOpenOrigin.HasValue && TryScreenToAnchored(paperOpenOrigin.Value, out start))
        {
            // Dropped item: grow out of the drop point.
            fromIcon = true;
            startScale = paperRestScale * 0.35f;
        }
        else
        {
            fromIcon = TryGetIconPosition(out start);
            if (!fromIcon)
            {
                start = paperTarget;
            }
            startScale = paperRestScale * (fromIcon ? PaperIconScale : 0.82f);
        }
        paperOpenOrigin = null;

        float startTilt = paperTilt + (fromIcon ? 28f : 7f);
        SetPaperPose(start, startScale, startTilt, 0f);

        float wait = 0f;
        while (wait < delay)
        {
            wait += Time.unscaledDeltaTime;
            yield return null;
        }

        float elapsed = 0f;
        while (elapsed < PaperOpenDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / PaperOpenDuration);

            // Travel eases out; size and swing overshoot slightly, then settle.
            float travel = 1f - (1f - t) * (1f - t) * (1f - t);
            float u = t - 1f;
            float pop = 1f + u * u * (2.2f * u + 1.2f);

            Vector2 position = fromIcon ? Arc(start, paperTarget, travel) : paperTarget;
            float scale = Mathf.LerpUnclamped(startScale, paperRestScale, pop);
            float tilt = Mathf.LerpUnclamped(startTilt, paperTilt, pop);
            SetPaperPose(position, scale, tilt, Mathf.Clamp01(t * 3f));

            yield return null;
        }

        paperOpening = false;
        window.anchoredPosition = paperTarget;
        ResetPaperVisual();
    }

    private IEnumerator PaperClose()
    {
        paperOpening = false;
        Vector2 start = window.anchoredPosition;
        bool toIcon = TryGetIconPosition(out Vector2 end);
        float startScale = window.localScale.x;

        float elapsed = 0f;
        while (elapsed < PaperCloseDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / PaperCloseDuration);
            float ease = t * t;

            Vector2 position = toIcon ? Arc(start, end, ease) : start;
            float scale = Mathf.Lerp(startScale, paperRestScale * (toIcon ? PaperIconScale : 0.86f), ease);
            float tilt = paperTilt - (toIcon ? 24f : 6f) * ease;
            float alpha = toIcon ? 1f - Mathf.Clamp01((t - 0.55f) / 0.45f) : 1f - ease;
            SetPaperPose(position, scale, tilt, alpha);

            yield return null;
        }

        // Next open starts from the same resting spot (centre) as before.
        window.anchoredPosition = start;
        CloseWindowImmediate();
    }


    // =========================================================
    // RESET POSITION
    // =========================================================

    public void ResetWindowPosition()
    {
        if (window == null)
        {
            return;
        }


        window.anchoredPosition =
            originalPosition;


        Debug.Log(
            $"[FloatingWindow] " +
            $"{gameObject.name}: " +
            "POSITION RESET"
        );
    }
}
