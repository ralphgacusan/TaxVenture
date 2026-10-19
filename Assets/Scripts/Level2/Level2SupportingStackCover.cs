using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>Routes cover taps and drags to the supporting-paper stack.</summary>
public sealed class Level2SupportingStackCover : MonoBehaviour, IPointerClickHandler, IBeginDragHandler,
    IDragHandler, IEndDragHandler
{
    public Level2SupportingStack Owner;

    public void OnPointerClick(PointerEventData e)
    {
        if (!e.dragging && e.button == PointerEventData.InputButton.Left)
            Owner?.LiftStack(e.position);
    }

    public void OnBeginDrag(PointerEventData e)
    {
        if (e.button == PointerEventData.InputButton.Left) Owner?.BeginCoverDrag(e.position);
    }

    public void OnDrag(PointerEventData e) => Owner?.DragCover(e.position);
    public void OnEndDrag(PointerEventData e) => Owner?.EndCoverDrag(e.position);
}
