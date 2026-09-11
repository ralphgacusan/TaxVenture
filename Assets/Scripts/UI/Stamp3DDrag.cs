using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;

/// <summary>
/// Handles dragging a physical 3D Ready / Not Ready stamp.
///
/// Flow:
/// 1. Player touches/clicks the 3D stamp.
/// 2. Stamp follows the pointer.
/// 3. Pointer is checked against the Case Folder drop zone.
/// 4. On valid drop, StampUI applies the stamp to CaseData.
/// 5. Stamp returns to its workspace position.
/// </summary>
[RequireComponent(typeof(Collider))]
public class Stamp3DDrag : MonoBehaviour, IPointerDownHandler
{
    [Header("Identity")]
    [SerializeField] private StampType stampType;

    [Header("Drop Target")]
    [SerializeField] private GameObject stampDropZoneUIObject;

    [Header("Movement")]
    [SerializeField] private float snapBackSpeed = 10f;

    [Header("Optional Drop Zone Highlight")]
    [SerializeField] private UnityEngine.UI.Graphic dropZoneHighlightGraphic;

    [SerializeField]
    private Color dropZoneNormalColor = Color.white;

    [SerializeField]
    private Color dropZoneValidHoverColor =
        new Color(0.6f, 1f, 0.6f);

    [Header("After Successful Drop")]
    [SerializeField]
    private bool returnAfterSuccessfulDrop = true;

    private Camera mainCamera;

    private bool draggingEnabled;
    private bool isDragging;
    private bool isSnappingBack;

    private Vector3 originalPosition;
    private Quaternion originalRotation;

    private float dragDepth;

    private readonly List<RaycastResult> raycastResults =
        new List<RaycastResult>();

    private void Awake()
    {
        mainCamera = Camera.main;

        originalPosition = transform.position;
        originalRotation = transform.rotation;

        draggingEnabled = false;

        if (mainCamera == null)
        {
            Debug.LogError(
                "[Stamp3DDrag] Main Camera not found."
            );
        }
    }

    // =========================================================
    // POINTER DOWN
    // =========================================================

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!draggingEnabled)
            return;

        if (mainCamera == null)
            return;

        Debug.Log(
            $"[Stamp3DDrag] Drag started: {name} | Type: {stampType}"
        );

        BeginDrag(eventData.position);
    }

    // =========================================================
    // ENABLE / DISABLE
    // =========================================================

    public void SetDraggingEnabled(bool enabled)
    {
        draggingEnabled = enabled;

        if (!enabled)
        {
            isDragging = false;
            isSnappingBack = false;
        }

        Debug.Log(
            $"[Stamp3DDrag] {name} dragging enabled: {enabled}"
        );
    }

    // =========================================================
    // BEGIN DRAG
    // =========================================================

    private void BeginDrag(Vector2 screenPosition)
    {
        isDragging = true;
        isSnappingBack = false;

        dragDepth = Vector3.Dot(
            transform.position -
            mainCamera.transform.position,
            mainCamera.transform.forward
        );

        if (dragDepth <= 0f)
        {
            dragDepth = 1f;
        }
    }

    // =========================================================
    // UPDATE
    // =========================================================

    private void Update()
    {
        if (isSnappingBack)
        {
            SnapBackTowardOrigin();
            return;
        }

        if (!isDragging)
            return;

        Vector2 pointerPosition =
            GetPointerScreenPosition();

        UpdateStampWorldPosition(pointerPosition);

        UpdateDropZoneHighlight(pointerPosition);

        if (WasReleasedThisFrame())
        {
            EndDrag(pointerPosition);
        }
    }

    // =========================================================
    // POINTER POSITION
    // =========================================================

    private Vector2 GetPointerScreenPosition()
    {
        if (Input.touchCount > 0)
        {
            return Input.GetTouch(0).position;
        }

        return Input.mousePosition;
    }

    private bool WasReleasedThisFrame()
    {
        if (Input.touchCount > 0)
        {
            TouchPhase phase =
                Input.GetTouch(0).phase;

            return phase == TouchPhase.Ended ||
                   phase == TouchPhase.Canceled;
        }

        return Input.GetMouseButtonUp(0);
    }

    // =========================================================
    // MOVE STAMP
    // =========================================================

    private void UpdateStampWorldPosition(
        Vector2 screenPosition)
    {
        Ray ray =
            mainCamera.ScreenPointToRay(
                screenPosition
            );

        Plane dragPlane =
            new Plane(
                -mainCamera.transform.forward,
                mainCamera.transform.position +
                mainCamera.transform.forward *
                dragDepth
            );

        if (dragPlane.Raycast(
                ray,
                out float distance))
        {
            transform.position =
                ray.GetPoint(distance);
        }
    }

    // =========================================================
    // DROP ZONE
    // =========================================================

    private bool IsPointerOverDropZone(
        Vector2 screenPosition)
    {
        if (EventSystem.current == null)
            return false;

        if (stampDropZoneUIObject == null)
            return false;

        PointerEventData pointerData =
            new PointerEventData(
                EventSystem.current
            );

        pointerData.position =
            screenPosition;

        raycastResults.Clear();

        EventSystem.current.RaycastAll(
            pointerData,
            raycastResults
        );

        foreach (RaycastResult result in raycastResults)
        {
            GameObject hitObject =
                result.gameObject;

            if (hitObject ==
                stampDropZoneUIObject)
            {
                return true;
            }

            if (hitObject.transform.IsChildOf(
                    stampDropZoneUIObject.transform))
            {
                return true;
            }
        }

        return false;
    }

    // =========================================================
    // HIGHLIGHT
    // =========================================================

    private void UpdateDropZoneHighlight(
        Vector2 screenPosition)
    {
        if (dropZoneHighlightGraphic == null)
            return;

        bool valid =
            IsPointerOverDropZone(
                screenPosition
            );

        dropZoneHighlightGraphic.color =
            valid
                ? dropZoneValidHoverColor
                : dropZoneNormalColor;
    }

    // =========================================================
    // RELEASE
    // =========================================================

    private void EndDrag(
        Vector2 screenPosition)
    {
        isDragging = false;

        if (dropZoneHighlightGraphic != null)
        {
            dropZoneHighlightGraphic.color =
                dropZoneNormalColor;
        }

        bool validDrop =
            IsPointerOverDropZone(
                screenPosition
            );

        Debug.Log(
            $"[Stamp3DDrag] Released {name}. " +
            $"Valid Drop = {validDrop}"
        );

        if (validDrop)
        {
            ApplyStamp();

            HandleSuccessfulDrop();
        }
        else
        {
            isSnappingBack = true;
        }
    }

    // =========================================================
    // APPLY STAMP
    // =========================================================

    private void ApplyStamp()
    {
        if (StampUI.Instance == null)
        {
            Debug.LogError(
                "[Stamp3DDrag] StampUI.Instance is NULL."
            );

            return;
        }

        Debug.Log(
            $"[Stamp3DDrag] Applying stamp type: {stampType}"
        );

        StampUI.Instance.ApplyStampType(
            stampType
        );
    }

    // =========================================================
    // SUCCESSFUL DROP
    // =========================================================

    private void HandleSuccessfulDrop()
    {
        if (returnAfterSuccessfulDrop)
        {
            isSnappingBack = true;
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    // =========================================================
    // SNAP BACK
    // =========================================================

    private void SnapBackTowardOrigin()
    {
        transform.position =
            Vector3.Lerp(
                transform.position,
                originalPosition,
                Time.deltaTime *
                snapBackSpeed
            );

        transform.rotation =
            Quaternion.Lerp(
                transform.rotation,
                originalRotation,
                Time.deltaTime *
                snapBackSpeed
            );

        if (Vector3.Distance(
                transform.position,
                originalPosition) < 0.01f)
        {
            transform.position =
                originalPosition;

            transform.rotation =
                originalRotation;

            isSnappingBack = false;
        }
    }

    // =========================================================
    // TRANSITION SUPPORT
    // =========================================================

    public void SetCurrentPositionAsOrigin()
    {
        originalPosition =
            transform.position;

        originalRotation =
            transform.rotation;

        Debug.Log(
            $"[Stamp3DDrag] Origin updated for {name}"
        );
    }
}