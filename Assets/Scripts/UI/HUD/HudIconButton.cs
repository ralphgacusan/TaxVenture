using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// PURPOSE:
/// One HUD icon button (Notes / Tax Code Book / Case Folder) that starts
/// locked (disabled, low opacity) and permanently unlocks once its
/// corresponding GameplayEvents signal fires. Once unlocked, it never
/// re-locks for the rest of the level, per spec.
///
/// RESPONSIBILITIES:
/// - Start in a locked visual/interactable state
/// - Subscribe to exactly one GameplayEvents signal (chosen in Inspector)
/// - On that event: enable Button.interactable, restore full opacity
///
/// DOES NOT:
/// - Know what happens when clicked (Button.onClick is wired separately in
///   the Inspector to whatever panel this icon opens — e.g. NotesPanelUI,
///   TaxCodeBookUI, CaseFolderUI — this script only manages lock state)
/// </summary>
public class HudIconButton : MonoBehaviour
{
    public enum UnlockTrigger
    {
        CaseFolderFirstOpened,
        TaxCodeBookFirstOpened,
        NotesUnlockRequested,
        TaxReturnCollected
    }
    [SerializeField] private UnlockTrigger unlockTrigger;
    [SerializeField] private Button button;
    [SerializeField] private CanvasGroup canvasGroup; // controls opacity via alpha

    [Header("Visuals")]
    [SerializeField] private float lockedAlpha = 0.4f;
    [SerializeField] private float unlockedAlpha = 1f;

    [SerializeField] private HudSubmittableIcon submittableIcon;
    private void Awake()
    {
        SetLocked();
    }

    private void OnEnable()
    {
        switch (unlockTrigger)
        {
            case UnlockTrigger.CaseFolderFirstOpened:
                GameplayEvents.OnCaseFolderFirstOpened += Unlock;
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
            case UnlockTrigger.CaseFolderFirstOpened:
                GameplayEvents.OnCaseFolderFirstOpened -= Unlock;
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
        Debug.Log($"{name}: Locked");

        button.interactable = false;
        canvasGroup.alpha = lockedAlpha;

        submittableIcon?.SetAvailable(false);
    }

    private void Unlock()
    {
        Debug.Log($"{name}: Unlocked");

        button.interactable = true;
        canvasGroup.alpha = unlockedAlpha;

        submittableIcon?.SetAvailable(true);
    }
}