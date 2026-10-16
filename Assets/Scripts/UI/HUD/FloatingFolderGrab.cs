using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>Moves the floating Case Folder by its cover; dropping it on the folder icon puts it away.</summary>
public sealed class FloatingFolderGrab : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public CaseFolder3DView Owner;
    private Vector3 offset;

    public void OnBeginDrag(PointerEventData eventData) => offset = Owner != null ? Owner.GrabOffset(eventData) : Vector3.zero;
    public void OnDrag(PointerEventData eventData) => Owner?.Dragged(eventData, offset);
    public void OnEndDrag(PointerEventData eventData) => Owner?.Dropped(eventData);
}
