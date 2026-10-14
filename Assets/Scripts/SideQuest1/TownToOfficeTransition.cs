using UnityEngine;

/// <summary>
/// PURPOSE:
/// Bridges the town intro sequence to the office case-solving loop.
/// Listens for the corkboard sidequest to finish, then unlocks whatever
/// lets the player head back to the office (a door, a prompt, a scene
/// load — pick whichever matches your level structure).
/// </summary>
public class TownToOfficeTransition : MonoBehaviour
{
    [Tooltip("Optional visual, e.g. a door glow or an arrow pointing back to the office.")]
    [SerializeField] private GameObject officeReturnIndicator;

    private void OnEnable()
    {
        TownEvents.OnSideQuestCompleted += HandleSideQuestCompleted;
    }

    private void OnDisable()
    {
        TownEvents.OnSideQuestCompleted -= HandleSideQuestCompleted;
    }

    private void HandleSideQuestCompleted()
    {
        Debug.Log("[TownToOfficeTransition] Side quest complete — office is now accessible.");

        if (officeReturnIndicator != null)
        {
            officeReturnIndicator.SetActive(true);
        }

        // If the office is a separate scene, load it here:
        // UnityEngine.SceneManagement.SceneManager.LoadScene("OfficeScene");

        // If office + town share a scene, you likely just need an office
        // entry trigger that checks a flag (e.g. TownEvents having fired)
        // before calling CaseProgressionManager.Instance.StartLevel("Level_01").
    }
}
