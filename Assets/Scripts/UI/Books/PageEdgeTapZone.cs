using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// PURPOSE:
/// One invisible/subtle tap zone representing a page-turn edge (left =
/// previous, right = next). Fires a callback on click — used by both the
/// Case Folder and Tax Code Book's Paper, replacing explicit Previous/Next
/// buttons with direct "tap the edge of the page" interaction, per R5's
/// Papers, Please-inspired design.
/// </summary>
public class PageEdgeTapZone : MonoBehaviour, IPointerClickHandler
{
    public System.Action OnTapped;

    public void OnPointerClick(PointerEventData eventData)
    {
        OnTapped?.Invoke();
    }
}