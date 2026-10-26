using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Forwards a card's press/drag/release to HudProfileCards for the long-press gather,
/// and a plain tap (2026-10-27) so the card's ID turns over.
/// </summary>
public sealed class ProfileCardHold : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler, IPointerClickHandler
{
    public HudProfileCards Owner;
    public FloatingWindow Window;
    public IdCardPortrait Portrait;

    public void OnPointerDown(PointerEventData eventData) => Owner?.CardPressed(Window, eventData);
    public void OnDrag(PointerEventData eventData) => Owner?.CardDragged(Window, eventData);
    public void OnPointerUp(PointerEventData eventData) => Owner?.CardReleased(Window, eventData);

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData == null || eventData.button != PointerEventData.InputButton.Left || MobileInputGuard.IsDragOrCancelled(eventData)) return;
        Owner?.CardTapped(this);
    }
}
