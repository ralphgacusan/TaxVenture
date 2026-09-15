using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// Allows an open or closed 3D folder to be dragged onto a HUD icon.
/// When dropped successfully:
/// - The folder is hidden.
/// - The HUD folder count increases.
/// - The folder is considered stored for the auditor.
///
/// Requirements:
/// - The folder must have a Collider.
/// - The folder must have FloatingModel3D.
/// - The Main Camera must have a PhysicsRaycaster.
/// - The HUD icon must have a UI Image or Button.
/// - The scene must contain an EventSystem.
/// </summary>
public class FolderToHUDIcon :
    MonoBehaviour,
    IPointerDownHandler,
    IPointerUpHandler
{
    [Header("Folder Settings")]

    [Tooltip("The 3D folder model. Leave empty to use this GameObject.")]
    [SerializeField]
    private GameObject folderObject;

    [Tooltip("Existing FloatingModel3D component on the folder.")]
    [SerializeField]
    private FloatingModel3D floatingModel3D;


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


    private Camera mainCamera;

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

        mainCamera = Camera.main;

        if (folderObject == null)
        {
            Debug.LogError(
                "[FolderToHUDIcon] Folder object is missing."
            );
        }

        if (floatingModel3D == null)
        {
            Debug.LogError(
                "[FolderToHUDIcon] FloatingModel3D was not found."
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


    // private void Update()
    // {
    //     if (!pointerIsDown ||
    //         folderStored ||
    //         folderHUDIcon == null ||
    //         mainCamera == null)
    //     {
    //         return;
    //     }

    //     /*
    //      * The folder is checked continuously while dragging.
    //      * The actual drop is completed in OnPointerUp().
    //      */
    // }


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

        if (floatingModel3D != null)
        {
            floatingModel3D.SetDraggingEnabled(false);
        }

        if (hideFolderAfterDrop &&
            folderObject != null)
        {
            folderObject.SetActive(false);
        }

        DebugLog(
            $"Folder stored successfully. Total folders: {storedFolderCount}"
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
}