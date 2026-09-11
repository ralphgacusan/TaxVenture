using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;

/// <summary>
/// 3D Ready / Not Ready stamp drag controller.
///
/// RESPONSIBILITIES:
/// - Detect pointer down on the 3D stamp
/// - Move the stamp with the pointer
/// - Detect the existing 2D StampDropZone using EventSystem.RaycastAll
/// - Apply the stamp through StampUI
/// - Snap back when dropped elsewhere
///
/// DOES NOT:
/// - Handle tax/game logic
/// - Handle highlighting
/// - Use Rigidbody physics
/// - Use physics collision with the UI
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

    [SerializeField] private Color dropZoneNormalColor = Color.white;

    [SerializeField]
    private Color dropZoneValidHoverColor =
        new Color(0.6f, 1f, 0.6f);

    [Header("After Successful Drop")]
    [SerializeField] private bool returnAfterSuccessfulDrop = true;

    private Camera mainCamera;

    private Vector3 originalPosition;
    private Quaternion originalRotation;

    private bool isDragging;
    private bool isSnappingBack;

    private float dragDepth;

    private readonly List<RaycastResult> raycastResults =
        new List<RaycastResult>();

    private void Awake()
    {
        mainCamera = Camera.main;

        if (mainCamera == null)
        {
            Debug.LogError(
                $"[{nameof(Stamp3DDrag)}] Main Camera not found."
            );
        }

        originalPosition = transform.position;
        originalRotation = transform.rotation;
    }

    // =========================================================
    // POINTER DOWN
    // =========================================================

    public void OnPointerDown(PointerEventData eventData)
    {
        Debug.Log("========================================");
        Debug.Log($"[STAMP TEST] POINTER DOWN: {gameObject.name}");
        Debug.Log($"[STAMP TEST] Stamp Type: {stampType}");
        Debug.Log($"[STAMP TEST] Collider: {GetComponent<Collider>()}");
        Debug.Log($"[STAMP TEST] Camera: {mainCamera}");
        Debug.Log("========================================");

        BeginDrag(eventData.position);
    }
    private void BeginDrag(Vector2 screenPosition)
    {
        isDragging = true;
        isSnappingBack = false;

        // Preserve the stamp's current distance from the camera.
        dragDepth = Vector3.Dot(
            transform.position -
            mainCamera.transform.position,
            mainCamera.transform.forward
        );

        // Prevent invalid/negative depth.
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
    // STAMP MOVEMENT
    // =========================================================

    private void UpdateStampWorldPosition(
        Vector2 screenPosition)
    {
        if (mainCamera == null)
            return;

        Ray ray =
            mainCamera.ScreenPointToRay(screenPosition);

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
    // UI DROP ZONE
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

            // Supports child Graphics inside the Button.
            if (hitObject.transform.IsChildOf(
                    stampDropZoneUIObject.transform))
            {
                return true;
            }
        }

        return false;
    }

    // =========================================================
    // DROP ZONE FEEDBACK
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
            $"[Stamp3DDrag] RELEASED: {name} | " +
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
            $"[Stamp3DDrag] Applying stamp: {stampType}"
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
            $"[Stamp3DDrag] New drag origin saved for {name}: " +
            $"{originalPosition}"
        );
    }
}

