using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>Forwards a card's press/drag/release to HudProfileCards for the long-press gather.</summary>
public sealed class ProfileCardHold : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    public HudProfileCards Owner;
    public FloatingWindow Window;

    public void OnPointerDown(PointerEventData eventData) => Owner?.CardPressed(Window, eventData);
    public void OnDrag(PointerEventData eventData) => Owner?.CardDragged(Window, eventData);
    public void OnPointerUp(PointerEventData eventData) => Owner?.CardReleased(Window, eventData);
}
