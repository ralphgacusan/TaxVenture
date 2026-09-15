using UnityEngine;

/// <summary>
/// Drop target for returning the open case folder to the desk.
///
/// When the open folder enters this trigger:
/// - The open folder is moved to the closed folder's position.
/// - The open folder is hidden.
/// - The closed desk folder is shown.
/// </summary>
public class CaseFolderDropTarget : MonoBehaviour
{
    [Header("Folder References")]

    [Tooltip("The currently open and draggable case folder.")]
    [SerializeField]
    private GameObject openCaseFolder;

    [Tooltip("The separate closed folder displayed on the desk.")]
    [SerializeField]
    private GameObject closedDeskFolder;

    [Header("Drop Settings")]

    [Tooltip("Additional position adjustment after returning the folder.")]
    [SerializeField]
    private Vector3 positionOffset = Vector3.zero;

    [Tooltip("Rotation used by the closed folder after returning.")]
    [SerializeField]
    private bool copyClosedFolderRotation = true;

    [Tooltip("Scale used by the closed folder after returning.")]
    [SerializeField]
    private bool copyClosedFolderScale = true;

    [Header("Debug")]

    [SerializeField]
    private bool enableDebugLogs = true;


    private bool hasReturnedFolder;


    private void Awake()
    {
        if (openCaseFolder == null)
        {
            Debug.LogError(
                "[CaseFolderDropTarget] Open Case Folder is not assigned."
            );
        }

        if (closedDeskFolder == null)
        {
            Debug.LogError(
                "[CaseFolderDropTarget] Closed Desk Folder is not assigned."
            );
        }

        Collider targetCollider = GetComponent<Collider>();

        if (targetCollider == null)
        {
            Debug.LogError(
                "[CaseFolderDropTarget] A Collider is required."
            );
        }
        else if (!targetCollider.isTrigger)
        {
            Debug.LogWarning(
                "[CaseFolderDropTarget] The Collider should be set as Is Trigger."
            );
        }
    }


    private void OnTriggerEnter(Collider other)
    {
        TryReturnFolder(other.gameObject);
    }


    private void OnTriggerStay(Collider other)
    {
        TryReturnFolder(other.gameObject);
    }


    private void TryReturnFolder(GameObject detectedObject)
    {
        if (hasReturnedFolder)
        {
            return;
        }

        if (openCaseFolder == null ||
            closedDeskFolder == null)
        {
            return;
        }

        if (!IsOpenFolderObject(detectedObject))
        {
            return;
        }

        ReturnFolderToDesk();
    }


    private bool IsOpenFolderObject(GameObject detectedObject)
    {
        if (detectedObject == openCaseFolder)
        {
            return true;
        }

        if (detectedObject.transform.IsChildOf(
                openCaseFolder.transform
            ))
        {
            return true;
        }

        return false;
    }


    private void ReturnFolderToDesk()
    {
        hasReturnedFolder = true;

        Transform closedTransform =
            closedDeskFolder.transform;

        openCaseFolder.transform.position =
            closedTransform.position +
            positionOffset;

        if (copyClosedFolderRotation)
        {
            openCaseFolder.transform.rotation =
                closedTransform.rotation;
        }

        if (copyClosedFolderScale)
        {
            openCaseFolder.transform.localScale =
                closedTransform.localScale;
        }

        openCaseFolder.SetActive(false);

        closedDeskFolder.SetActive(true);

        DebugLog(
            "Open case folder returned to the desk."
        );

        DebugLog(
            $"Closed folder position: {closedTransform.position}"
        );
    }


    public void ResetDropTarget()
    {
        hasReturnedFolder = false;
    }


    private void DebugLog(string message)
    {
        if (enableDebugLogs)
        {
            Debug.Log(
                $"[CaseFolderDropTarget] {message}"
            );
        }
    }
}