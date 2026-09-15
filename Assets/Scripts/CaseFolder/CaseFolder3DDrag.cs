using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

public class CaseFolder3DDrag :
    MonoBehaviour,
    IPointerDownHandler,
    IDragHandler,
    IPointerUpHandler
{
    [Header("References")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private Transform deskViewpoint;
    [SerializeField] private GameObject closedFolder;

    [Tooltip(
        "Assign the open case folder object here. " +
        "Its position, rotation, and scale will never be changed."
    )]
    [SerializeField] private GameObject openFolder;

    [Header("Case Folder UI")]
    [SerializeField] private CaseFolderUI caseFolderUI;

    [Header("Drag Settings")]
    [SerializeField] private bool allowDragging = true;

    [Tooltip("Keeps the closed folder at its original rotation.")]
    [SerializeField] private bool preserveClosedFolderRotation = true;

    [Tooltip("Makes the closed folder smoothly follow the pointer.")]
    [SerializeField] private bool animateDragging = true;

    [Tooltip("Higher values make the closed folder follow the pointer faster.")]
    [SerializeField] private float dragSmoothness = 18f;

    [Tooltip(
        "Moves the closed folder toward the camera while the pointer " +
        "moves toward the center of the screen."
    )]
    [SerializeField] private float cameraMovementAmount = 0.15f;

    [Tooltip(
        "Moves the closed folder downward or upward while dragging. " +
        "Negative values move it downward, while positive values move it upward."
    )]
    [SerializeField] private float closedFolderHeightOffset = 0f;

    [Tooltip(
        "Minimum screen movement required before the folder opens."
    )]
    [SerializeField] private float minimumDragDistance = 25f;

    [Header("Opening Settings")]
    [SerializeField] private bool animateOpening = false;

    [Header("Debug Settings")]
    [SerializeField] private bool enableDebugLogs = true;

    [SerializeField] private bool drawDebugRay = true;

    [SerializeField] private Color debugRayColor = Color.red;

    private Plane dragPlane;

    private bool isDragging;
    private bool hasDragTarget;
    private bool isOpening;
    private bool hasMovedDuringDrag;

    private Vector3 dragTargetPosition;
    private Vector3 dragOffset;

    private Vector2 pointerDownScreenPosition;

    private Vector3 originalClosedPosition;
    private Quaternion originalClosedRotation;
    private Vector3 originalClosedScale;

    private Vector3 originalOpenPosition;
    private Quaternion originalOpenRotation;
    private Vector3 originalOpenScale;

    private Coroutine openingCoroutine;

    private void Awake()
    {
        DebugLog("Awake() started.");

        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        if (closedFolder == null)
        {
            closedFolder = gameObject;
        }

        if (deskViewpoint == null)
        {
            Debug.LogError(
                "[CaseFolder3DDrag] Desk Viewpoint is not assigned."
            );

            return;
        }

        if (openFolder == null)
        {
            Debug.LogError(
                "[CaseFolder3DDrag] Open Folder is not assigned."
            );

            return;
        }

        if (mainCamera == null)
        {
            Debug.LogError(
                "[CaseFolder3DDrag] Main Camera was not found."
            );

            return;
        }

        if (dragSmoothness <= 0f)
        {
            dragSmoothness = 18f;
        }

        if (cameraMovementAmount < 0f)
        {
            cameraMovementAmount = 0f;
        }

        if (minimumDragDistance < 0f)
        {
            minimumDragDistance = 25f;
        }

        // Save the closed folder's original transform.
        originalClosedPosition =
            closedFolder.transform.position;

        originalClosedRotation =
            closedFolder.transform.rotation;

        originalClosedScale =
            closedFolder.transform.localScale;

        /*
         * Save the open folder's transform only for ResetFolder().
         * The open folder is never repositioned, rotated, or scaled
         * during dragging or opening.
         */
        originalOpenPosition =
            openFolder.transform.position;

        originalOpenRotation =
            openFolder.transform.rotation;

        originalOpenScale =
            openFolder.transform.localScale;

        // Hide the open folder without changing its transform.
        openFolder.SetActive(false);

        DebugLog("References successfully initialized.");
        DebugLog(
            $"Original Closed Folder Position: {originalClosedPosition}"
        );
        DebugLog(
            $"Original Open Folder Position: {originalOpenPosition}"
        );
    }

    private void Update()
    {
        if (!isDragging ||
            !hasDragTarget ||
            isOpening ||
            closedFolder == null)
        {
            return;
        }

        if (animateDragging)
        {
            float interpolation =
                1f - Mathf.Exp(
                    -dragSmoothness * Time.deltaTime
                );

            closedFolder.transform.position =
                Vector3.Lerp(
                    closedFolder.transform.position,
                    dragTargetPosition,
                    interpolation
                );
        }
        else
        {
            closedFolder.transform.position =
                dragTargetPosition;
        }

        if (preserveClosedFolderRotation)
        {
            closedFolder.transform.rotation =
                originalClosedRotation;
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        DebugLog("OnPointerDown() detected.");

        if (!allowDragging || isOpening)
        {
            return;
        }

        if (mainCamera == null ||
            closedFolder == null ||
            openFolder == null)
        {
            Debug.LogError(
                "[CaseFolder3DDrag] A required reference is missing."
            );

            return;
        }

        pointerDownScreenPosition =
            eventData.position;

        hasMovedDuringDrag = false;
        isDragging = true;
        hasDragTarget = false;

        /*
         * The plane is parallel to the camera and passes directly
         * through the closed folder's current position.
         */
        dragPlane = new Plane(
            mainCamera.transform.forward,
            closedFolder.transform.position
        );

        Ray ray =
            mainCamera.ScreenPointToRay(
                eventData.position
            );

        if (!dragPlane.Raycast(ray, out float enter))
        {
            DebugLog(
                "The pointer did not intersect the drag plane."
            );

            isDragging = false;
            return;
        }

        Vector3 pointerWorldPosition =
            ray.GetPoint(enter);

        /*
         * Calculate the exact grab offset.
         */
        dragOffset =
            closedFolder.transform.position -
            pointerWorldPosition;

        dragTargetPosition =
            closedFolder.transform.position;

        hasDragTarget = true;

        if (preserveClosedFolderRotation)
        {
            closedFolder.transform.rotation =
                originalClosedRotation;
        }

        DebugLog(
            $"Pointer position: {eventData.position}"
        );

        DebugLog(
            $"Pointer world position: {pointerWorldPosition}"
        );

        DebugLog(
            $"Exact drag offset: {dragOffset}"
        );

        DebugLog(
            $"Closed folder position: {closedFolder.transform.position}"
        );
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging || isOpening)
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

        UpdateDragTarget(eventData.position);

        DebugLog(
            $"OnDrag detected. Target position: {dragTargetPosition}"
        );
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        DebugLog("OnPointerUp() detected.");

        if (!isDragging || isOpening)
        {
            return;
        }

        float screenDistance =
            Vector2.Distance(
                pointerDownScreenPosition,
                eventData.position
            );

        UpdateDragTarget(eventData.position);

        if (!hasMovedDuringDrag ||
            screenDistance < minimumDragDistance)
        {
            DebugLog(
                "No meaningful drag detected. " +
                "The closed folder remains visible."
            );

            isDragging = false;
            hasDragTarget = false;

            return;
        }

        if (hasDragTarget)
        {
            closedFolder.transform.position =
                dragTargetPosition;
        }

        if (preserveClosedFolderRotation)
        {
            closedFolder.transform.rotation =
                originalClosedRotation;
        }

        isDragging = false;
        hasDragTarget = false;

        DebugLog(
            $"Closed folder released at: {closedFolder.transform.position}"
        );

        ShowOpenFolder();

        DebugLog("Folder drop process completed.");
    }

    private void UpdateDragTarget(Vector2 screenPosition)
    {
        if (mainCamera == null ||
            closedFolder == null)
        {
            return;
        }

        Ray ray =
            mainCamera.ScreenPointToRay(
                screenPosition
            );

        if (drawDebugRay)
        {
            Debug.DrawRay(
                ray.origin,
                ray.direction * 20f,
                debugRayColor,
                0.05f
            );
        }

        if (!dragPlane.Raycast(ray, out float enter))
        {
            DebugLog(
                "Screen ray did not intersect the drag plane."
            );

            return;
        }

        Vector3 pointerWorldPosition =
            ray.GetPoint(enter);

        /*
         * Direct pointer-following position.
         */
        dragTargetPosition =
            pointerWorldPosition +
            dragOffset;

        /*
         * Apply the height offset only to the closed folder.
         */
        dragTargetPosition +=
            Vector3.up *
            closedFolderHeightOffset;

        /*
         * Move the closed folder slightly toward the camera when
         * the pointer approaches the center of the screen.
         *
         * The movement is strongest at the center and becomes zero
         * near the screen edges.
         */
        if (cameraMovementAmount > 0f)
        {
            Vector2 screenCenter =
                new Vector2(
                    Screen.width * 0.5f,
                    Screen.height * 0.5f
                );

            Vector2 screenCenterOffset =
                screenPosition -
                screenCenter;

            float horizontalFactor =
                1f - Mathf.Clamp01(
                    Mathf.Abs(screenCenterOffset.x) /
                    (Screen.width * 0.5f)
                );

            float verticalFactor =
                1f - Mathf.Clamp01(
                    Mathf.Abs(screenCenterOffset.y) /
                    (Screen.height * 0.5f)
                );

            float centerFactor =
                (
                    horizontalFactor +
                    verticalFactor
                ) * 0.5f;

            Vector3 cameraMovement =
                -mainCamera.transform.forward.normalized *
                cameraMovementAmount *
                centerFactor;

            dragTargetPosition +=
                cameraMovement;
        }

        hasDragTarget = true;
    }

    private void ShowOpenFolder()
    {
        if (closedFolder == null ||
            openFolder == null)
        {
            Debug.LogError(
                "[CaseFolder3DDrag] Cannot show the open folder."
            );

            return;
        }

        /*
         * Hide the closed folder and show the open folder.
         * The open folder's transform is not modified.
         */
        closedFolder.SetActive(false);
        openFolder.SetActive(true);

        DebugLog(
            "Open folder shown without changing its transform."
        );

        /*
         * Show the Case Folder UI after the open folder appears.
         */
        if (caseFolderUI != null)
        {
            caseFolderUI.Show();

            DebugLog(
                "CaseFolderUI.Show() called after opening the folder."
            );
        }
        else
        {
            Debug.LogError(
                "[CaseFolder3DDrag] CaseFolderUI reference is missing!"
            );
        }
    }

    public void ResetFolder()
    {
        DebugLog("ResetFolder() called.");

        if (openingCoroutine != null)
        {
            StopCoroutine(openingCoroutine);
            openingCoroutine = null;
        }

        isDragging = false;
        hasDragTarget = false;
        isOpening = false;
        hasMovedDuringDrag = false;

        if (closedFolder != null)
        {
            closedFolder.transform.position =
                originalClosedPosition;

            closedFolder.transform.rotation =
                originalClosedRotation;

            closedFolder.transform.localScale =
                originalClosedScale;

            closedFolder.SetActive(true);
        }

        if (openFolder != null)
        {
            /*
             * Restore the original open-folder transform only during
             * an explicit reset.
             */
            openFolder.transform.position =
                originalOpenPosition;

            openFolder.transform.rotation =
                originalOpenRotation;

            openFolder.transform.localScale =
                originalOpenScale;

            openFolder.SetActive(false);
        }

        DebugLog("Folder successfully reset.");
    }

    private void DebugLog(string message)
    {
        if (enableDebugLogs)
        {
            Debug.Log(
                $"[CaseFolder3DDrag] {message}"
            );
        }
    }
}

