using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Handles dragging of the open 3D tax code book.
///
/// Features:
/// - Supports mouse and touch through Unity's EventSystem.
/// - Keeps the book on a camera-facing drag plane.
/// - Preserves the exact pointer-to-book offset.
/// - Preserves the original rotation and scale.
/// - Supports smooth or direct dragging.
/// </summary>
public class TaxCodeBookFloatingModel3D :
    MonoBehaviour,
    IPointerDownHandler,
    IDragHandler,
    IPointerUpHandler
{
    [Header("Tax Code Book")]

    [Tooltip("Leave empty to use this GameObject's Transform.")]
    [SerializeField]
    private Transform taxCodeBook;


    [Header("Camera")]

    [SerializeField]
    private Camera mainCamera;


    [Header("Dragging")]

    [SerializeField]
    private bool allowDragging = true;

    [Tooltip("Smoothly moves the book toward the pointer.")]
    [SerializeField]
    private bool animateDragging = true;

    [Tooltip("Higher values make the book follow the pointer faster.")]
    [SerializeField]
    private float dragSmoothness = 30f;

    [Tooltip("Preserves the book's original rotation.")]
    [SerializeField]
    private bool preserveOriginalRotation = true;

    [Tooltip("Preserves the book's original scale.")]
    [SerializeField]
    private bool preserveOriginalScale = true;


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

    private Quaternion originalRotation;
    private Vector3 originalScale;

    private bool isDragging;
    private bool hasDragTarget;

    private Camera eventCamera;


    private void Awake()
    {
        if (taxCodeBook == null)
        {
            taxCodeBook = transform;
        }

        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        if (taxCodeBook == null)
        {
            Debug.LogError(
                "[TaxCodeBookFloatingModel3D] " +
                "Tax code book reference is missing."
            );

            enabled = false;
            return;
        }

        if (mainCamera == null)
        {
            Debug.LogError(
                "[TaxCodeBookFloatingModel3D] " +
                "Main Camera was not found."
            );

            enabled = false;
            return;
        }

        if (dragSmoothness <= 0f)
        {
            dragSmoothness = 30f;
        }

        originalRotation =
            taxCodeBook.rotation;

        originalScale =
            taxCodeBook.localScale;

        Collider bookCollider =
            GetComponent<Collider>();

        if (bookCollider == null)
        {
            Debug.LogError(
                "[TaxCodeBookFloatingModel3D] " +
                "A Collider must be attached to the same " +
                "GameObject as this script."
            );
        }

        DebugLog(
            "Initialized on " +
            gameObject.name
        );
    }


    private void Update()
    {
        if (!isDragging ||
            !hasDragTarget ||
            taxCodeBook == null)
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

            taxCodeBook.position =
                Vector3.Lerp(
                    taxCodeBook.position,
                    dragTargetPosition,
                    interpolation
                );
        }
        else
        {
            taxCodeBook.position =
                dragTargetPosition;
        }

        RestoreBookAppearance();
    }


    public void OnPointerDown(PointerEventData eventData)
    {
        if (!allowDragging ||
            taxCodeBook == null ||
            mainCamera == null)
        {
            return;
        }

        eventCamera =
            eventData.pressEventCamera != null
                ? eventData.pressEventCamera
                : mainCamera;

        /*
         * Create a plane passing through the book's current position.
         * The book stays in its original position until the pointer
         * actually moves.
         */
        dragPlane =
            new Plane(
                mainCamera.transform.forward,
                taxCodeBook.position
            );

        Ray ray =
            eventCamera.ScreenPointToRay(
                eventData.position
            );

        if (drawDebugRay)
        {
            Debug.DrawRay(
                ray.origin,
                ray.direction * debugRayLength,
                debugRayColor,
                2f
            );
        }

        if (!dragPlane.Raycast(
                ray,
                out float enter
            ))
        {
            DebugLog(
                "Pointer ray did not intersect the drag plane."
            );

            isDragging = false;
            hasDragTarget = false;

            return;
        }

        Vector3 pointerWorldPosition =
            ray.GetPoint(enter);

        /*
         * Preserve the exact point where the user grabbed the book.
         * This prevents the book from jumping when dragging starts.
         */
        dragOffset =
            taxCodeBook.position -
            pointerWorldPosition;

        dragTargetPosition =
            taxCodeBook.position;

        isDragging = true;
        hasDragTarget = true;

        RestoreBookAppearance();

        DebugLog(
            "Tax code book dragging started."
        );

        DebugLog(
            "Book position: " +
            taxCodeBook.position
        );

        DebugLog(
            "Pointer world position: " +
            pointerWorldPosition
        );

        DebugLog(
            "Drag offset: " +
            dragOffset
        );
    }


    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging ||
            !hasDragTarget)
        {
            return;
        }

        UpdateDragTarget(
            eventData.position
        );
    }


    private void UpdateDragTarget(Vector2 screenPosition)
    {
        if (eventCamera == null)
        {
            eventCamera = mainCamera;
        }

        Ray ray =
            eventCamera.ScreenPointToRay(
                screenPosition
            );

        if (drawDebugRay)
        {
            Debug.DrawRay(
                ray.origin,
                ray.direction * debugRayLength,
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

        /*
         * Move the book according to the pointer while preserving
         * the initial grab offset.
         */
        dragTargetPosition =
            pointerWorldPosition +
            dragOffset;

        hasDragTarget = true;
    }


    public void OnPointerUp(PointerEventData eventData)
    {
        if (!isDragging)
        {
            return;
        }

        UpdateDragTarget(
            eventData.position
        );

        if (hasDragTarget)
        {
            taxCodeBook.position =
                dragTargetPosition;
        }

        RestoreBookAppearance();

        isDragging = false;
        hasDragTarget = false;

        DebugLog(
            "Tax code book dragging finished."
        );

        DebugLog(
            "Final position: " +
            taxCodeBook.position
        );
    }


    private void RestoreBookAppearance()
    {
        if (taxCodeBook == null)
        {
            return;
        }

        if (preserveOriginalRotation)
        {
            taxCodeBook.rotation =
                originalRotation;
        }

        if (preserveOriginalScale)
        {
            taxCodeBook.localScale =
                originalScale;
        }
    }


    public void SetDraggingEnabled(bool enabled)
    {
        allowDragging = enabled;

        DebugLog(
            "Dragging enabled: " +
            allowDragging
        );
    }


    public bool IsDragging()
    {
        return isDragging;
    }


    public void ResetBook()
    {
        if (taxCodeBook == null)
        {
            return;
        }

        isDragging = false;
        hasDragTarget = false;

        RestoreBookAppearance();

        DebugLog(
            "Tax code book rotation and scale restored."
        );
    }


    private void DebugLog(string message)
    {
        if (enableDebugLogs)
        {
            Debug.Log(
                "[TaxCodeBookFloatingModel3D] " +
                message
            );
        }
    }
}