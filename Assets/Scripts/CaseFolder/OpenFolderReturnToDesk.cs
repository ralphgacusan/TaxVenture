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

        ReturnFolderToDesk();
    }


    private void ReturnFolderToDesk()
    {
        Transform deskTransform = deskDropFolder.transform;

        originalClosedFolder.transform.position = deskTransform.position;
        originalClosedFolder.transform.localScale = deskTransform.localScale;

        /*
         * Match the target's rotation, then add a slight random
         * tilt so the folder doesn't look perfectly stacked.
         */
        float randomYaw = Random.Range(-sideTiltAngle, sideTiltAngle);
        originalClosedFolder.transform.rotation =
            deskTransform.rotation * Quaternion.Euler(0f, randomYaw, 0f);

        // Stack it slightly above the target so it doesn't z-fight.
        Vector3 stackedPosition = originalClosedFolder.transform.position;
        stackedPosition.y += dropHeightOffset;
        originalClosedFolder.transform.position = stackedPosition;

        gameObject.SetActive(false);
        originalClosedFolder.SetActive(true);

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
}