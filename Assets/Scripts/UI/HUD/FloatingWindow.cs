using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;


// TO DO : fix the layering of the document viewer as it appears in the back
// fix the issue where only one document can be open at a time

/// <summary>
/// Reusable floating window/panel controller.
///
/// FEATURES:
/// - Freely drag the window around the screen
/// - Whichever window is touched becomes the topmost window
/// - Works with multiple FloatingWindow objects
/// - Works even when windows have different parents
/// - Uses Canvas sorting order for reliable layering
/// - HUD icon remains above floating windows
/// - Drag window onto HUD icon to close
/// - Normal click opens centered
/// - Dragging a HUD icon opens the window near the icon
/// - Adjustable opening offset
/// - Supports mouse and touch
///
/// OPENING BEHAVIOR:
/// - Every window starts at its configured baseSortingOrder.
/// - Opening a window places it at the first/default layer.
/// - Clicking/touching a window can still bring it to the front.
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
        "Canvas containing the window."
    )]
    [SerializeField] private Canvas canvas;


    // =========================================================
    // HUD ICON
    // =========================================================

    [Header("HUD Icon")]

    [Tooltip(
        "HUD icon associated with this window."
    )]
    [SerializeField] private RectTransform returnIcon;

    [Tooltip(
        "Distance from the HUD icon required to close the window."
    )]
    [SerializeField] private float returnIconDistance = 120f;


    // =========================================================
    // OPENING POSITION
    // =========================================================

    [Header("Opening Position")]

    [Tooltip(
        "Horizontal offset applied when opening from the HUD icon. " +
        "Positive = RIGHT, Negative = LEFT."
    )]
    [SerializeField] private float openingOffsetX = 450f;

    [Tooltip(
        "Vertical offset applied when opening from the HUD icon. " +
        "Positive = UP, Negative = DOWN."
    )]
    [SerializeField] private float openingOffsetY = 0f;


    // =========================================================
    // DRAG SETTINGS
    // =========================================================

    [Header("Dragging")]

    [SerializeField] private bool allowDragging = true;

    [SerializeField] private bool bringToFront = true;


    // =========================================================
    // OPEN / CLOSE
    // =========================================================

    [Header("Open / Close")]

    [Tooltip(
        "Kept for compatibility."
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
        "Default sorting order used when the window opens."
    )]
    [SerializeField] private int baseSortingOrder = 100;

    [Tooltip(
        "Sorting order used by the HUD icon. " +
        "Floating windows will always stay below this."
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
    // GLOBAL SORTING ORDER
    // =========================================================

    /*
     * Used only when a window is explicitly brought
     * to the front by touching/clicking it.
     *
     * Opening a window does NOT increment this value.
     *
     * This means:
     *
     * Open document:
     *     Document = baseSortingOrder
     *
     * Touch document:
     *     Document = higher layer
     *
     * Open document again:
     *     Document = baseSortingOrder
     */

    private static int currentSortingOrder = 100;


    // =========================================================
    // UNITY - AWAKE
    // =========================================================

    private void Awake()
    {
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
        // Find canvas
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
        // Find parent
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
        // Find Canvas directly attached to the window.
        // -----------------------------------------------------

        windowCanvas =
            window.GetComponent<Canvas>();


        // -----------------------------------------------------
        // Create Canvas if necessary.
        // -----------------------------------------------------

        if (windowCanvas == null)
        {
            windowCanvas =
                window.gameObject.AddComponent<Canvas>();
        }


        // -----------------------------------------------------
        // Enable independent sorting.
        // -----------------------------------------------------

        windowCanvas.overrideSorting = true;


        // -----------------------------------------------------
        // IMPORTANT:
        //
        // Always start at the configured base layer.
        //
        // We do NOT increment currentSortingOrder here.
        // -----------------------------------------------------

        windowCanvas.sortingOrder =
            baseSortingOrder;


        // Keep the global counter at least above
        // the base layer so BringWindowToFront()
        // can move windows above it.

        currentSortingOrder =
            Mathf.Max(
                currentSortingOrder,
                baseSortingOrder
            );


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
        // Touching the window brings it to front.
        // -----------------------------------------------------

        if (bringToFront)
        {
            BringWindowToFront();
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
        // Increase global layer.
        //
        // This only happens when the user actually
        // touches/clicks the window.
        // -----------------------------------------------------

        currentSortingOrder++;


        // -----------------------------------------------------
        // Prevent window from reaching HUD icon layer.
        // -----------------------------------------------------

        if (currentSortingOrder >= iconSortingOrder)
        {
            currentSortingOrder =
                iconSortingOrder - 1;
        }


        // -----------------------------------------------------
        // Assign new sorting order.
        // -----------------------------------------------------

        windowCanvas.sortingOrder =
            currentSortingOrder;


        // -----------------------------------------------------
        // Also move this GameObject to the end of its
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
        // Check HUD icon.
        // -----------------------------------------------------

        if (returnIcon != null &&
            IsOverReturnIcon(
                eventData.position
            ))
        {
            Debug.Log(
                $"[FloatingWindow] " +
                $"{gameObject.name}: " +
                "DROPPED ON HUD ICON"
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
    // CHECK HUD ICON
    // =========================================================

    private bool IsOverReturnIcon(
        Vector2 screenPosition
    )
    {
        if (returnIcon == null)
            return false;


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


            yield return null;
        }


        window.anchoredPosition =
            targetPosition;


        CloseWindow();
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
        // ALWAYS RESET TO CENTER.
        // -----------------------------------------------------

        window.anchoredPosition =
            GetParentCenterPosition();


        // -----------------------------------------------------
        // ALWAYS START AT BASE LAYER.
        //
        // This is the important change.
        // Opening does NOT make it topmost.
        // -----------------------------------------------------

        if (windowCanvas != null)
        {
            windowCanvas.sortingOrder =
                baseSortingOrder;
        }


        // -----------------------------------------------------
        // Put this window at the end of its hierarchy.
        // -----------------------------------------------------

        window.SetAsLastSibling();


        Debug.Log(
            $"[FloatingWindow] " +
            $"{gameObject.name}: " +
            $"OPENED | Sorting Order = " +
            $"{baseSortingOrder}"
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
            // Apply opening offset.
            // -------------------------------------------------

            localPosition +=
                new Vector2(
                    openingOffsetX,
                    openingOffsetY
                );


            // -------------------------------------------------
            // Put visual center at desired position.
            // -------------------------------------------------

            window.anchoredPosition =
                GetAnchoredPositionForCenter(
                    localPosition
                );
        }
        else
        {
            // -------------------------------------------------
            // Fallback.
            // -------------------------------------------------

            window.anchoredPosition =
                GetParentCenterPosition();
        }


        // -----------------------------------------------------
        // ALWAYS START AT BASE LAYER.
        // -----------------------------------------------------

        if (windowCanvas != null)
        {
            windowCanvas.sortingOrder =
                baseSortingOrder;
        }


        // -----------------------------------------------------
        // Put this window at the end of its hierarchy.
        // -----------------------------------------------------

        window.SetAsLastSibling();


        Debug.Log(
            $"[FloatingWindow] " +
            $"{gameObject.name}: " +
            $"OPENED AT POSITION | " +
            $"Sorting Order = {baseSortingOrder}"
        );
    }


    // =========================================================
    // GET PARENT CENTER POSITION
    // =========================================================

    private Vector2 GetParentCenterPosition()
    {
        if (parentRect == null)
            return Vector2.zero;


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
            return desiredCenter;


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
            return null;


        // -----------------------------------------------------
        // Screen Space Overlay.
        // -----------------------------------------------------

        if (canvas.renderMode ==
            RenderMode.ScreenSpaceOverlay)
        {
            return null;
        }


        // -----------------------------------------------------
        // Camera / World Space.
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
            return;


        StopAllCoroutines();


        isClosing = false;

        isDragging = false;


        gameObject.SetActive(false);


        Debug.Log(
            $"[FloatingWindow] " +
            $"{gameObject.name}: CLOSED"
        );
    }


    // =========================================================
    // TOGGLE
    // =========================================================

    public void ToggleWindow()
    {
        bool currentlyOpen =
            gameObject.activeSelf;


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
        return gameObject.activeSelf;
    }


    // =========================================================
    // RESET POSITION
    // =========================================================

    public void ResetWindowPosition()
    {
        if (window == null)
            return;


        window.anchoredPosition =
            originalPosition;


        Debug.Log(
            $"[FloatingWindow] " +
            $"{gameObject.name}: " +
            "POSITION RESET"
        );
    }
}