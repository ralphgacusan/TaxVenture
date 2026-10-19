using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>Blocks underlying paper gestures only over the visible lifted stamp.
/// The stamp controller owns pointer capture and motion for both touch and mouse.</summary>
public sealed class Level2StampHitTarget : MonoBehaviour, ICanvasRaycastFilter,
    IPointerDownHandler, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public Level2StampDrag Owner;
    public bool IncludeStand;

    public bool IsRaycastLocationValid(Vector2 screen, Camera eventCamera)
    {
        Camera view = CameraController.Instance != null ? CameraController.Instance.MainCamera : Camera.main;
        return Owner != null && (!IncludeStand || !Owner.IsUp) && Owner.ScreenHit(view, screen, out _, IncludeStand);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (IncludeStand && Owner != null) Owner.TapStand();
    }
    public void OnPointerClick(PointerEventData eventData) { }
    public void OnBeginDrag(PointerEventData eventData) { }
    public void OnDrag(PointerEventData eventData) { }
    public void OnEndDrag(PointerEventData eventData) { }
}
