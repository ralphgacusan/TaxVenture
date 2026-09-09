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
            RebuildSortingOrders();

            return;
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
        // Reset position to center
        // -----------------------------------------------------

        window.anchoredPosition =
            GetParentCenterPosition();


        // -----------------------------------------------------
        // IMPORTANT:
        //
        // Opening a document makes THAT document topmost.
        // -----------------------------------------------------

        BringWindowToFront();


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


        StopAllCoroutines();


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