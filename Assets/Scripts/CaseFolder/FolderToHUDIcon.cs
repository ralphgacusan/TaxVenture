using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// Allows an open or closed 3D folder to be dropped onto a HUD icon.
///
/// Supports:
/// - Open folders using FloatingModel3D
/// - Closed folders using CaseFolder3DDrag
///
/// When dropped successfully:
/// - The folder is hidden.
/// - The folder count increases.
/// - The folder can no longer be dragged.
/// - The folder is considered stored.
/// </summary>
public class FolderToHUDIcon :
    MonoBehaviour,
    IPointerDownHandler,
    IPointerUpHandler
{
    [Header("Folder Settings")]

    [Tooltip(
        "The folder object to hide after storage. " +
        "Leave empty to use this GameObject."
    )]
    [SerializeField]
    private GameObject folderObject;


    [Tooltip(
        "Optional FloatingModel3D component used by an open folder."
    )]
    [SerializeField]
    private FloatingModel3D floatingModel3D;


    [Tooltip(
        "Optional CaseFolder3DDrag component used by a closed folder."
    )]
    [SerializeField]
    private CaseFolder3DDrag caseFolder3DDrag;


    [Header("HUD Destination")]

    [Tooltip("The HUD icon that receives the folder.")]
    [SerializeField]
    private RectTransform folderHUDIcon;


    [Tooltip("The UI object containing the folder count text.")]
    [SerializeField]
    private TextMeshProUGUI folderCountText;


    [Header("Drop Settings")]

    [Tooltip("Hide the folder after it is successfully stored.")]
    [SerializeField]
    private bool hideFolderAfterDrop = true;


    [Header("Debug")]

    [SerializeField]
    private bool enableDebugLogs = true;


    [Tooltip("Optional. If this folder is the OPEN folder, assign the component that knows how to return it to the desk as a closed folder.")]
    [SerializeField]
    private OpenFolderReturnToDesk openFolderReturnToDesk;


    private bool pointerIsDown;
    private bool folderStored;

    private static int storedFolderCount = 0;


    private void Awake()
    {
        if (folderObject == null)
        {
            folderObject = gameObject;
        }

        if (floatingModel3D == null)
        {
            floatingModel3D =
                GetComponent<FloatingModel3D>();
        }

        if (openFolderReturnToDesk == null)
        {
            openFolderReturnToDesk = GetComponent<OpenFolderReturnToDesk>();
        }

        if (caseFolder3DDrag == null)
        {
            caseFolder3DDrag =
                GetComponent<CaseFolder3DDrag>();
        }

        if (folderObject == null)
        {
            Debug.LogError(
                "[FolderToHUDIcon] Folder object is missing."
            );
        }

        if (floatingModel3D == null &&
            caseFolder3DDrag == null)
        {
            Debug.LogWarning(
                "[FolderToHUDIcon] Neither FloatingModel3D nor " +
                "CaseFolder3DDrag was found. Make sure one of them " +
                "is assigned."
            );
        }

        if (folderHUDIcon == null)
        {
            Debug.LogError(
                "[FolderToHUDIcon] HUD icon is not assigned."
            );
        }

        UpdateFolderCountText();
    }


    public void OnPointerDown(
        PointerEventData eventData
    )
    {
        if (folderStored)
        {
            return;
        }

        pointerIsDown = true;

        DebugLog(
            "Folder pointer down detected."
        );
    }


    public void OnPointerUp(
        PointerEventData eventData
    )
    {
        if (!pointerIsDown ||
            folderStored)
        {
            return;
        }

        pointerIsDown = false;

        if (folderHUDIcon == null)
        {
            DebugLog(
                "HUD icon is not assigned."
            );

            return;
        }

        bool pointerInsideIcon =
            RectTransformUtility.RectangleContainsScreenPoint(
                folderHUDIcon,
                eventData.position,
                null
            );

        DebugLog(
            $"Pointer screen position: {eventData.position}"
        );

        DebugLog(
            $"HUD icon position: {folderHUDIcon.position}"
        );

        DebugLog(
            $"HUD icon size: {folderHUDIcon.rect.size}"
        );

        DebugLog(
            $"Pointer inside HUD icon: {pointerInsideIcon}"
        );

        if (pointerInsideIcon)
        {
            StoreFolder();
        }
        else
        {
            DebugLog(
                "Folder was not dropped inside the HUD icon."
            );
        }
    }


    private void StoreFolder()
    {
        if (folderStored)
        {
            return;
        }

        folderStored = true;

        storedFolderCount++;

        UpdateFolderCountText();

        /*
         * Disable the open folder's drag system.
         */
        if (floatingModel3D != null)
        {
            floatingModel3D.SetDraggingEnabled(false);

            DebugLog(
                "FloatingModel3D dragging disabled."
            );
        }

        /*
         * Fully hide and disable the closed/open folder pair.
         * This handles both possible active states (closed or open)
         * so the folder disappears completely.
         */
        if (caseFolder3DDrag != null)
        {
            caseFolder3DDrag.HideAndDisableFolder();

            DebugLog(
                "CaseFolder3DDrag hidden and disabled."
            );
        }

        /*
         * Hide the folder after it has been stored.
         * Only relevant for the FloatingModel3D (open folder) case,
         * since CaseFolder3DDrag already hides itself above.
         */
        if (hideFolderAfterDrop &&
            folderObject != null &&
            caseFolder3DDrag == null)
        {
            folderObject.SetActive(false);

            DebugLog(
                "Folder object hidden."
            );
        }

        DebugLog(
            $"Folder stored successfully. " +
            $"Total folders: {storedFolderCount}"
        );
    }


    private void UpdateFolderCountText()
    {
        if (folderCountText != null)
        {
            folderCountText.text =
                storedFolderCount.ToString();
        }
    }


    public bool IsFolderStored()
    {
        return folderStored;
    }


    public static int GetStoredFolderCount()
    {
        return storedFolderCount;
    }


    public static void ResetStoredFolderCount()
    {
        storedFolderCount = 0;
    }


    private void DebugLog(
        string message
    )
    {
        if (enableDebugLogs)
        {
            Debug.Log(
                $"[FolderToHUDIcon] {message}"
            );
        }
    }

    public void StoreFolderFromExternalSource()
    {
        if (folderStored)
        {
            return;
        }

        StoreFolder();
    }

    public RectTransform GetHUDIcon()
    {
        return folderHUDIcon;
    }

    /// <summary>
    /// Inverse of StoreFolder(), for the "audit failed, try again" loop.
    /// Brings the folder back into play, decrements the shared stored count,
    /// and re-enables dragging on whichever drag component this folder uses.
    /// </summary>
    public void ResetToUnstored()
    {
        if (!folderStored)
        {
            return;
        }

        folderStored = false;

        storedFolderCount = Mathf.Max(0, storedFolderCount - 1);
        UpdateFolderCountText();

        // Always re-enable the open folder's own dragging, regardless of
        // which desk-return path is used below — StoreFolder() disabled it
        // and nothing else turns it back on.
        if (floatingModel3D != null)
        {
            floatingModel3D.SetDraggingEnabled(true);
        }

        if (openFolderReturnToDesk != null)
        {
            openFolderReturnToDesk.ForceReturnToDesk();
        }
        else if (caseFolder3DDrag != null)
        {
            caseFolder3DDrag.RestoreAndEnableFolder();
        }
        else if (floatingModel3D != null)
        {
            floatingModel3D.ResetModel();
        }

        if (folderObject != null && caseFolder3DDrag == null && openFolderReturnToDesk == null)
        {
            folderObject.SetActive(true);
        }

        DebugLog("Folder reset to unstored state for retry.");
    }
}