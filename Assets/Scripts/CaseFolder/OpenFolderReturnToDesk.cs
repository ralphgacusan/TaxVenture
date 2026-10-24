using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Allows the open case folder to be dragged and dropped onto
/// a separate desk-folder target.
///
/// The original closed folder and the open folder are not modified.
/// When the open folder is released over the desk target:
/// - The open folder is hidden.
/// - The original closed folder is moved to the desk target's position.
/// - The original closed folder is shown above the desk target.
/// </summary>
public class OpenFolderReturnToDesk :
    MonoBehaviour,
    IPointerUpHandler
{
    [Header("Folder References")]

    [Tooltip("The original closed case folder.")]
    [SerializeField]
    private GameObject originalClosedFolder;

    [Tooltip("The separate closed folder used only as the desk drop target.")]
    [SerializeField]
    private GameObject deskDropFolder;

    [Header("Drop Detection")]

    [Tooltip("Camera used to detect whether the pointer is over the desk target.")]
    [SerializeField]
    private Camera mainCamera;

    [Tooltip("Extra distance added above the desk drop folder.")]
    [SerializeField]
    private float dropHeightOffset = 0.01f;

    [Tooltip("Max random sideways tilt (degrees) applied when stacking the folder, for a more natural look.")]
    [SerializeField]
    private float sideTiltAngle = 6f;

    [Header("Debug")]

    [SerializeField]
    private bool enableDebugLogs = true;


    private Vector3 originalClosedPosition;
    private Quaternion originalClosedRotation;
    private Vector3 originalClosedScale;

    private Collider deskDropCollider;
    private DeskDocumentAnimator deskAnimator;


    private void Awake()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        if (originalClosedFolder == null)
        {
            Debug.LogError(
                "[OpenFolderReturnToDesk] Original closed folder is missing."
            );
        }

        if (deskDropFolder == null)
        {
            Debug.LogError(
                "[OpenFolderReturnToDesk] Desk drop folder is missing."
            );
        }

        if (originalClosedFolder != null)
        {
            originalClosedPosition =
                originalClosedFolder.transform.position;

            originalClosedRotation =
                originalClosedFolder.transform.rotation;

            originalClosedScale =
                originalClosedFolder.transform.localScale;
        }

        if (originalClosedFolder != null)
        {
            deskAnimator =
                originalClosedFolder.GetComponentInParent<DeskDocumentAnimator>(true);
        }

        if (deskDropFolder != null)
        {
            deskDropCollider =
                deskDropFolder.GetComponent<Collider>();

            if (deskDropCollider == null)
            {
                Debug.LogError(
                    "[OpenFolderReturnToDesk] Desk drop folder needs a Collider."
                );
            }
        }
    }


    public void OnPointerUp(PointerEventData eventData)
    {
        if (mainCamera == null ||
            deskDropFolder == null ||
            originalClosedFolder == null ||
            deskDropCollider == null)
        {
            return;
        }

        Ray ray =
            mainCamera.ScreenPointToRay(
                eventData.position
            );

        if (!deskDropCollider.Raycast(
                ray,
                out RaycastHit hit,
                1000f
            ))
        {
            DebugLog(
                "Pointer was released outside the desk drop folder."
            );

            return;
        }

        ReturnFolderToDesk(true);
    }


    private void ReturnFolderToDesk(bool animate)
    {
        Transform deskTransform = deskDropFolder.transform;

        /*
         * Match the target's rotation, then add a slight random
         * tilt so the folder doesn't look perfectly stacked.
         */
        float randomYaw = Random.Range(-sideTiltAngle, sideTiltAngle);
        Quaternion stackedRotation =
            deskTransform.rotation * Quaternion.Euler(0f, randomYaw, 0f);

        // Stack it slightly above the target so it doesn't z-fight.
        Vector3 stackedPosition = deskTransform.position;
        stackedPosition.y += dropHeightOffset;

        bool canAnimate = animate && deskAnimator != null;

        void Finish()
        {
            originalClosedFolder.transform.SetPositionAndRotation(stackedPosition, stackedRotation);
            originalClosedFolder.transform.localScale = deskTransform.localScale;

            gameObject.SetActive(false);
            originalClosedFolder.SetActive(true);

            if (canAnimate)
                deskAnimator.PlaySettle(originalClosedFolder.transform);
        }

        // Play paper sound when the open folder closes.
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayPaperSFX();
        }

        if (canAnimate)
            deskAnimator.PlayClose(transform, stackedPosition, Finish);
        else
            Finish();

        DebugLog("Open folder successfully returned to the desk.");
        DebugLog($"Original closed folder restored at: {originalClosedFolder.transform.position}");
    }

    private void DebugLog(string message)
    {
        if (enableDebugLogs)
        {
            Debug.Log(
                $"[OpenFolderReturnToDesk] {message}"
            );
        }
    }

    /// <summary>
    /// Public entry point so other systems (e.g. audit retry logic) can force
    /// the open folder back onto the desk programmatically, without requiring
    /// an actual pointer-up drop event.
    /// </summary>
    /// <summary>
    /// Resume (Campaign Foundation T05): the folder was in the HUD icon in the
    /// saved run, so the closed desk copy is hidden exactly as opening the
    /// folder hides it (its drag stays enabled, so ForceReturnToDesk works).
    /// </summary>
    public void HideClosedFolderForRestore()
    {
        if (originalClosedFolder != null) originalClosedFolder.SetActive(false);
    }

    public void ForceReturnToDesk()
    {
        if (deskDropFolder == null || originalClosedFolder == null)
        {
            Debug.LogError("[OpenFolderReturnToDesk] Cannot force return — references missing.");
            return;
        }

        if (deskAnimator != null)
        {
            deskAnimator.Complete(transform);
            deskAnimator.Complete(originalClosedFolder.transform);
        }

        ReturnFolderToDesk(false);
    }
}