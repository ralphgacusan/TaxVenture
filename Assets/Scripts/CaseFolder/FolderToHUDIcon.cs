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
    private bool outForView;

    // The folders in the HUD icon right now (stored, not out for a view).
    // Kept as the live objects, not a bare number, so the count can never
    // outlive its folders: leaving the scene destroys them and their entries
    // (a static int stayed at 1 after leaving Level 1, and a resumed folder
    // then showed 2 with one real folder).
    private static readonly System.Collections.Generic.HashSet<FolderToHUDIcon> storedFolders =
        new System.Collections.Generic.HashSet<FolderToHUDIcon>();

    private static int storedFolderCount => storedFolders.Count;

    // Safety net for a folder that never woke up (no OnDestroy): a new
    // single-scene load starts with an empty icon, before any folder can be
    // stored (Level 2 also resets it when its office starts).
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ClearOnSceneLoad()
    {
        storedFolders.Clear();
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= HandleSceneLoaded;
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private static void HandleSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        if (mode == UnityEngine.SceneManagement.LoadSceneMode.Single) storedFolders.Clear();
    }


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
        // The floating view owns cover drags and its animated return.
        if (folderStored || GetComponent<FloatingFolderGrab>() != null) return;

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
            folderStored || GetComponent<FloatingFolderGrab>() != null)
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

        bool pointerInsideIcon = IsOverDropTarget(eventData.position);

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
            HudSlotDrop.Note(InventoryCatalogue.CaseFolder, eventData.position);
            StoreFolder();
        }
        else
        {
            DebugLog(
                "Folder was not dropped inside the HUD icon."
            );
        }
    }


    private void StoreFolder() => StoreFolder(restoring: false);

    /// <summary>
    /// Resume (T05): the folder was already in the HUD icon in the saved run.
    /// Stored and hidden exactly like a drop, without the paper sound or the
    /// stored event.
    /// </summary>
    public void RestoreStored()
    {
        if (folderStored) return;
        StoreFolder(restoring: true);
        // Normal play stores the open folder after opening hid the closed desk
        // copy; a restore must hide that copy too, or the folder sits on the
        // desk and in the icon at once (user report 2026-10-23).
        if (openFolderReturnToDesk != null) openFolderReturnToDesk.HideClosedFolderForRestore();
    }

    /// <summary>
    /// T08: the approved case folder leaves the icon for good (it became Marie's
    /// Client Copy, or she has it now): the icon empties, the folder stays
    /// hidden and is not returned to the desk, and no returned event is raised.
    /// RestoreStored puts it back quietly (the copy is picked up).
    /// </summary>
    public void ReleaseQuietly()
    {
        if (!folderStored) return;
        folderStored = false;
        outForView = false;
        storedFolders.Remove(this);
        UpdateFolderCountText();
    }

    private void StoreFolder(bool restoring)
    {
        if (folderStored)
        {
            return;
        }

        folderStored = true;

        // Play paper sound when the folder is dropped onto the HUD icon.
        if (!restoring && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayPaperSFX();
        }

        storedFolders.Add(this);

        UpdateFolderCountText();
        if (!restoring) GameplayEvents.RaiseCaseFolderStored();

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


    private void OnDestroy() => storedFolders.Remove(this);

    private void UpdateFolderCountText()
    {
        if (folderCountText != null)
        {
            folderCountText.text =
                storedFolderCount.ToString();
        }
    }


    /// <summary>True while the folder belongs to the HUD icon, including while it is out for a quick view (T05 checkpoint).</summary>
    public bool IsStoredOrViewing => folderStored;

    /// <summary>
    /// The Case Folder belongs to the HUD now (stored, or out of it for a quick
    /// view): the desk must not put it back on its stack (2026-10-26).
    /// </summary>
    public static bool AnyStoredOrViewing()
    {
        foreach (FolderToHUDIcon folder in FindObjectsByType<FolderToHUDIcon>(FindObjectsInactive.Include))
            if (folder != null && folder.folderStored) return true;
        return false;
    }

    public bool IsFolderStored()
    {
        return folderStored && !outForView;
    }


    /// <summary>Withdraws the badge for an animated view without resetting the real model.</summary>
    public bool TryTakeOutForView()
    {
        if (!folderStored || outForView || !storedFolders.Contains(this)) return false;
        outForView = true;
        storedFolders.Remove(this);
        UpdateFolderCountText();
        GameplayEvents.RaiseCaseFolderReturned();
        return true;
    }

    /// <summary>Called once the view has finished returning to its icon.</summary>
    public void ReturnFromView()
    {
        if (!folderStored || !outForView) return;
        outForView = false;
        storedFolders.Add(this);
        UpdateFolderCountText();
        GameplayEvents.RaiseCaseFolderStored();
    }

    public static int GetStoredFolderCount()
    {
        return storedFolderCount;
    }


    public static void ResetStoredFolderCount()
    {
        storedFolders.Clear();
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
    /// T10: the shared drop test for carried world items. When the level uses
    /// the HUD hotbar, any slot (or the row) takes the folder; otherwise only
    /// this folder's own icon, as before.
    /// </summary>
    public bool IsOverDropTarget(Vector2 screenPosition)
    {
        if (HudSlotDrop.Active) return HudSlotDrop.IsOver(screenPosition);
        return folderHUDIcon != null &&
               RectTransformUtility.RectangleContainsScreenPoint(folderHUDIcon, screenPosition, null);
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

        storedFolders.Remove(this);
        outForView = false;
        UpdateFolderCountText();
        GameplayEvents.RaiseCaseFolderReturned();

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