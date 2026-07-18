using UnityEngine;

/// <summary>
/// PURPOSE:
/// Controls visibility of simple placeholder "hands" (two capsules/cubes) that
/// appear in first-person workstation view, per the design doc requirement:
/// "The player's hands are visible on screen to reinforce the first-person
/// experience."
///
/// RESPONSIBILITIES:
/// - Show/hide the hands GameObject(s) on command
///
/// DOES NOT:
/// - Handle any animation (no animations exist yet per Phase 1 scope) — just
///   simple show/hide of static greybox placeholders.
///
/// CONNECTS WITH:
/// - CameraController calls Show()/Hide() when entering/exiting first-person mode.
/// - Should be parented under the Camera so the hands move with it.
/// </summary>
public class FirstPersonHands : MonoBehaviour
{
    [Tooltip("Parent object containing the placeholder hand meshes. Assign the object that holds both hand capsules.")]
    [SerializeField] private GameObject handsRoot;

    [Header("Carried Document (left hand)")]
    [SerializeField] private GameObject carriedDocumentVisual; // paper mesh, child of left hand position

    private void Awake()
    {
        if (handsRoot != null)
            handsRoot.SetActive(true);

        HideCarriedDocument();
    }

    public void Show()
    {
        if (handsRoot != null) handsRoot.SetActive(true);
    }

    public void Hide()
    {
        if (handsRoot != null) handsRoot.SetActive(false);
    }


    public void ShowCarriedDocument()
    {
        if (carriedDocumentVisual != null) carriedDocumentVisual.SetActive(true);
    }

    public void HideCarriedDocument()
    {
        if (carriedDocumentVisual != null) carriedDocumentVisual.SetActive(false);
    }
}