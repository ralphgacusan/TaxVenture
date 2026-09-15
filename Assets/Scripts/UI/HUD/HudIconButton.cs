
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// PURPOSE:
/// Controls the locked/unlocked state of one HUD icon.
///
/// Case Folder:
/// - Starts locked
/// - Unlocks when the current Case Folder is opened
/// - Locks again when the current case is completed
/// - Unlocks again when the next Case Folder is opened
///
/// Other icons:
/// - Unlock once through their corresponding GameplayEvents signal
/// - Remain unlocked for the rest of the level
///
/// IMPORTANT:
/// The HUD GameObject itself stays ACTIVE throughout the level.
/// Only the Button interaction and visual availability are changed.
///
/// DOES NOT:
/// - Know what happens when clicked
/// - Open or close the associated panel
/// - Manage gameplay progression
///
/// Button.onClick is wired separately in the Inspector.
/// </summary>
public class HudIconButton : MonoBehaviour
{
    public enum UnlockTrigger
    {
        CaseFolderOpened,
        TaxCodeBookFirstOpened,
        NotesUnlockRequested,
        TaxReturnCollected
    }


    [SerializeField]
    private UnlockTrigger unlockTrigger;


    [SerializeField]
    private Button button;


    [SerializeField]
    private CanvasGroup canvasGroup;


    [Header("Visuals")]

    [SerializeField]
    private float lockedAlpha = 0.4f;

    [SerializeField]
    private float unlockedAlpha = 1f;


    [SerializeField]
    private HudSubmittableIcon submittableIcon;


    private void Awake()
    {
        Unlock();
    }


    private void OnEnable()
    {
        switch (unlockTrigger)
        {
            case UnlockTrigger.CaseFolderOpened:

                GameplayEvents.OnCaseFolderOpened += Unlock;
                GameplayEvents.OnCaseCompleted += RelockCaseFolder;

                break;


            case UnlockTrigger.TaxCodeBookFirstOpened:

                GameplayEvents.OnTaxCodeBookFirstOpened += Unlock;

                break;


            case UnlockTrigger.NotesUnlockRequested:

                GameplayEvents.OnNotesUnlockRequested += Unlock;

                break;


            case UnlockTrigger.TaxReturnCollected:

                GameplayEvents.OnTaxReturnCollected += Unlock;

                break;
        }
    }


    private void OnDisable()
    {
        switch (unlockTrigger)
        {
            case UnlockTrigger.CaseFolderOpened:

                GameplayEvents.OnCaseFolderOpened -= Unlock;
                GameplayEvents.OnCaseCompleted -= RelockCaseFolder;

                break;


            case UnlockTrigger.TaxCodeBookFirstOpened:

                GameplayEvents.OnTaxCodeBookFirstOpened -= Unlock;

                break;


            case UnlockTrigger.NotesUnlockRequested:

                GameplayEvents.OnNotesUnlockRequested -= Unlock;

                break;


            case UnlockTrigger.TaxReturnCollected:

                GameplayEvents.OnTaxReturnCollected -= Unlock;

                break;
        }
    }


    private void SetLocked()
    {
        Debug.Log(
            $"{name}: Locked"
        );


        if (button != null)
        {
            button.interactable = false;
        }


        if (canvasGroup != null)
        {
            canvasGroup.alpha = lockedAlpha;
        }


        if (submittableIcon != null)
        {
            submittableIcon.SetAvailable(false);
        }
    }


    private void Unlock()
    {
        Debug.Log(
            $"{name}: Unlocked"
        );


        if (button != null)
        {
            button.interactable = true;
        }


        if (canvasGroup != null)
        {
            canvasGroup.alpha = unlockedAlpha;
        }


        if (submittableIcon != null)
        {
            submittableIcon.SetAvailable(true);
        }
    }


    private void RelockCaseFolder()
    {
        if (unlockTrigger !=
            UnlockTrigger.CaseFolderOpened)
        {
            return;
        }


        Debug.Log(
            $"{name}: Case completed → " +
            "locking Case Folder icon."
        );


        SetLocked();
    }
}

