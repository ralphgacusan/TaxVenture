using UnityEngine;

/// <summary>
/// PURPOSE:
/// The physical Corkboard object in the office. On interact, enters
/// first-person mode focused on CorkboardViewpoint, then tells
/// CorkboardDocumentSpawner to populate the board. Does NOT manage any UI
/// panel, document data, or assessment logic itself — purely the entry
/// point, per the "only enters workstation mode" requirement.
///
/// CONNECTS WITH:
/// - HighlightEffect (same GameObject)
/// - CameraController: first-person transition, same system as Desk
/// - CorkboardDocumentSpawner: told to (re)spawn documents once entered
/// </summary>
public class CorkboardInteractable : MonoBehaviour, IInteractable
{
    [SerializeField] private Transform corkboardViewpoint;
    [SerializeField] private CorkboardDocumentSpawner documentSpawner;

    private HighlightEffect highlight;

    private void Awake()
    {
        highlight = GetComponentInChildren<HighlightEffect>(); // was GetComponent<HighlightEffect>()
    }

    public void OnFocus() => highlight.Highlight();
    public void OnUnfocus() => highlight.Unhighlight();

    public void OnInteract()
    {
        CameraController.Instance.EnterFirstPerson(corkboardViewpoint, false);
        documentSpawner.SpawnDocuments();
    }

    public string GetPromptText() => "Click to review evidence at Corkboard";
}