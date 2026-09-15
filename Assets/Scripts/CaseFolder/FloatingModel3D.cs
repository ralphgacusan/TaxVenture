using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Reusable 3D floating model drag controller.
///
/// Features:
/// - Works with any 3D model that has a Collider.
/// - Supports mouse and touch through Unity's EventSystem.
/// - Allows movement left, right, up, and down relative to the camera.
/// - Keeps the model on a camera-facing drag plane.
/// - Preserves the original rotation and scale.
/// - Can be reused for the open case folder and other 3D models.
///
/// Requirements:
/// - Attach this script to the same GameObject as the Collider.
/// - Main Camera must have a PhysicsRaycaster.
/// - Scene must contain an EventSystem.
/// </summary>
public class FloatingModel3D :
    MonoBehaviour,
    IPointerDownHandler,
    IDragHandler,
    IPointerUpHandler
{
    [Header("Model")]

    [Tooltip(
        "Leave empty to use this GameObject's Transform."
    )]
    [SerializeField]
    private Transform model;


    [Header("Camera")]

    [SerializeField]
    private Camera mainCamera;


    [Header("Dragging")]

    [SerializeField]
    private bool allowDragging = true;

    [Tooltip(
        "Smoothly moves the model toward the pointer."
    )]
    [SerializeField]
    private bool animateDragging = true;

    [Tooltip(
        "Higher values make the model follow the pointer faster."
    )]
    [SerializeField]
    private float dragSmoothness = 18f;

    [Tooltip(
        "Preserves the model's original rotation while dragging."
    )]
    [SerializeField]
    private bool preserveOriginalRotation = true;


    [Header("Frontmost Model")]

    [Tooltip(
        "Moves the selected model slightly toward the camera when clicked."
    )]
    [SerializeField]
    private bool moveSelectedModelForward = false;

    [Tooltip(
        "Distance to move the selected model toward the camera."
    )]
    [SerializeField]
    private float frontmostOffset = 0.01f;


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
        if (model == null)
        {
            model = transform;
        }

        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        if (model == null)
        {
            Debug.LogError(
                "[FloatingModel3D] Model reference is missing."
            );

            enabled = false;
            return;
        }

        if (mainCamera == null)
        {
            Debug.LogError(
                "[FloatingModel3D] Main Camera was not found."
            );

            enabled = false;
            return;
        }

        if (dragSmoothness <= 0f)
        {
            dragSmoothness = 18f;
        }

        originalRotation = model.rotation;
        originalScale = model.localScale;

        Collider modelCollider =
            GetComponent<Collider>();

        if (modelCollider == null)
        {
            Debug.LogError(
                "[FloatingModel3D] A Collider must be attached " +
                "to the same GameObject as this script."
            );
        }

        DebugLog(
            $"Initialized on {gameObject.name}."
        );
    }


    private void Update()
    {
        if (!isDragging ||
            !hasDragTarget ||
            model == null)
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

            model.position =
                Vector3.Lerp(
                    model.position,
                    dragTargetPosition,
                    interpolation
                );
        }
        else
        {
            model.position =
                dragTargetPosition;
        }

        if (preserveOriginalRotation)
        {
            model.rotation =
                originalRotation;
        }

        model.localScale =
            originalScale;
    }


    public void OnPointerDown(
        PointerEventData eventData
    )
    {
        DebugLog(
            $"OnPointerDown detected on {gameObject.name}."
        );

        if (!allowDragging)
        {
            DebugLog(
                "Dragging is disabled."
            );

            return;
        }

        if (model == null ||
            mainCamera == null)
        {
            Debug.LogError(
                "[FloatingModel3D] Model or Main Camera is missing."
            );

            return;
        }

        eventCamera =
            eventData.pressEventCamera != null
                ? eventData.pressEventCamera
                : mainCamera;

        /*
         * Create a plane parallel to the camera.
         * The plane passes through the model's current position.
         */
        dragPlane =
            new Plane(
                mainCamera.transform.forward,
                model.position
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

        dragOffset =
            model.position -
            pointerWorldPosition;

        dragTargetPosition =
            model.position;

        isDragging = true;
        hasDragTarget = true;

        if (preserveOriginalRotation)
        {
            model.rotation =
                originalRotation;
        }

        model.localScale =
            originalScale;

        if (moveSelectedModelForward)
        {
            model.position +=
                -mainCamera.transform.forward.normalized *
                frontmostOffset;

            dragTargetPosition =
                model.position;
        }

        DebugLog(
            $"Dragging started on {gameObject.name}."
        );

        DebugLog(
            $"Model position: {model.position}"
        );

        DebugLog(
            $"Pointer world position: {pointerWorldPosition}"
        );

        DebugLog(
            $"Drag offset: {dragOffset}"
        );
    }


    public void OnDrag(
        PointerEventData eventData
    )
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


    private void UpdateDragTarget(
        Vector2 screenPosition
    )
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
            DebugLog(
                "Screen ray did not intersect the drag plane."
            );

            return;
        }

        Vector3 pointerWorldPosition =
            ray.GetPoint(enter);

        dragTargetPosition =
            pointerWorldPosition +
            dragOffset;

        hasDragTarget = true;
    }


    public void OnPointerUp(
        PointerEventData eventData
    )
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
            model.position =
                dragTargetPosition;
        }

        if (preserveOriginalRotation)
        {
            model.rotation =
                originalRotation;
        }

        model.localScale =
            originalScale;

        isDragging = false;
        hasDragTarget = false;

        DebugLog(
            $"Dragging finished on {gameObject.name}."
        );

        DebugLog(
            $"Final position: {model.position}"
        );
    }


    public void SetDraggingEnabled(
        bool enabled
    )
    {
        allowDragging = enabled;

        DebugLog(
            $"Dragging enabled: {allowDragging}"
        );
    }


    public bool IsDragging()
    {
        return isDragging;
    }


    public void ResetModel()
    {
        if (model == null)
        {
            return;
        }

        isDragging = false;
        hasDragTarget = false;

        model.position =
            dragTargetPosition;

        model.rotation =
            originalRotation;

        model.localScale =
            originalScale;

        DebugLog(
            "Model reset to its current drag target."
        );
    }


    private void DebugLog(
        string message
    )
    {
        if (enableDebugLogs)
        {
            Debug.Log(
                $"[FloatingModel3D] {message}"
            );
        }
    }
}