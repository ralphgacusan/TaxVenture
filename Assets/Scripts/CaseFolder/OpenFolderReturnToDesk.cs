using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Allows the open case folder to be dragged and dropped onto
/// a separate desk-folder target.
///
/// The original closed folder and the open folder are not modified.
/// When the open folder is released over the desk target:
/// - The open folder is hidden.
/// - The original closed folder is moved to its saved original position.
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
        Transform deskTransform =
            deskDropFolder.transform;

        /*
         * Restore the original closed folder to its original
         * position, rotation, and scale.
         */
        originalClosedFolder.transform.position =
            originalClosedPosition;

        originalClosedFolder.transform.rotation =
            originalClosedRotation;

        originalClosedFolder.transform.localScale =
            originalClosedScale;

        /*
         * Make sure the original closed folder is positioned
         * slightly above the desk drop folder.
         */
        Vector3 originalPosition =
            originalClosedFolder.transform.position;

        originalClosedFolder.transform.position =
            new Vector3(
                originalPosition.x,
                originalPosition.y + dropHeightOffset,
                originalPosition.z
            );

        /*
         * Hide only the open folder.
         * The original closed folder and desk drop folder
         * remain separate objects.
         */
        gameObject.SetActive(false);

        originalClosedFolder.SetActive(true);

        DebugLog(
            "Open folder successfully returned to the desk."
        );

        DebugLog(
            $"Original closed folder restored at: " +
            $"{originalClosedFolder.transform.position}"
        );
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