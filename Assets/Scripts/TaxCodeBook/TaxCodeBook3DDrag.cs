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
    IPointerUpHandler
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

    private Vector3 originalOpenPosition;

    private Vector2 pointerDownScreenPosition;

    private bool isDragging;
    private bool hasDragTarget;
    private bool hasMovedDuringDrag;


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

            openTaxCodeBook.SetActive(false);
        }

        DebugLog("Tax code book initialized.");
    }


    private void Update()
    {
        if (!isDragging ||
            !hasDragTarget ||
            closedTaxCodeBook == null)
        {
            return;
        }

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
                    dragTargetPosition,
                    interpolation
                );
        }
        else
        {
            closedTaxCodeBook.transform.position =
                dragTargetPosition;
        }

        if (preserveClosedBookRotation)
        {
            closedTaxCodeBook.transform.rotation =
                originalClosedRotation;
        }
    }


    public void OnPointerDown(
        PointerEventData eventData
    )
    {
        if (!allowDragging ||
            closedTaxCodeBook == null ||
            mainCamera == null)
        {
            return;
        }

        pointerDownScreenPosition =
            eventData.position;

        hasMovedDuringDrag = false;
        isDragging = true;
        hasDragTarget = false;

        dragPlane =
            new Plane(
                mainCamera.transform.forward,
                closedTaxCodeBook.transform.position
            );

        Ray ray =
            mainCamera.ScreenPointToRay(
                eventData.position
            );

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

        if (!dragPlane.Raycast(
                ray,
                out float enter
            ))
        {
            isDragging = false;
            return;
        }

        Vector3 pointerWorldPosition =
            ray.GetPoint(enter);

        dragOffset =
            closedTaxCodeBook.transform.position -
            pointerWorldPosition;

        dragTargetPosition =
            closedTaxCodeBook.transform.position;

        hasDragTarget = true;

        DebugLog("Tax code book dragging started.");
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
            isDragging = false;
            hasDragTarget = false;

            DebugLog(
                "No meaningful drag detected."
            );

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

        ShowOpenTaxCodeBook(eventData.position);
    }


    private void UpdateDragTarget(
        Vector2 screenPosition
    )
    {
        Ray ray =
            mainCamera.ScreenPointToRay(
                screenPosition
            );

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

        if (!dragPlane.Raycast(
                ray,
                out float enter
            ))
        {
            return;
        }

        Vector3 pointerWorldPosition =
            ray.GetPoint(enter);

        dragTargetPosition =
            pointerWorldPosition +
            dragOffset;

        hasDragTarget = true;
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
        isDragging = false;
        hasDragTarget = false;
        hasMovedDuringDrag = false;

        if (closedTaxCodeBook != null)
        {
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
}