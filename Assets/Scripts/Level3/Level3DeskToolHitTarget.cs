using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>Uses the shared document window order to block gestures behind a lifted tool.
/// The calculator controller captures and moves the pointer, like the desk stamps.</summary>
public sealed class Level3DeskToolHitTarget : MonoBehaviour, ICanvasRaycastFilter,
    IPointerDownHandler, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public Level3DeskCalculator Owner;
    public Doc3DLayer Layer;

    public bool IsRaycastLocationValid(Vector2 screen, Camera eventCamera) =>
        Owner != null && Owner.ToolPointerActive && Layer != null && Layer.ShowsAt(screen, out _);

    public void OnPointerDown(PointerEventData eventData) { }
    public void OnPointerClick(PointerEventData eventData) { }
    public void OnBeginDrag(PointerEventData eventData) { }
    public void OnDrag(PointerEventData eventData) { }
    public void OnEndDrag(PointerEventData eventData) { }
}
