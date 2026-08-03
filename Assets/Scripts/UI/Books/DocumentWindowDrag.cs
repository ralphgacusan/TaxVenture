using UnityEngine;
using UnityEngine.EventSystems;

public class DocumentWindowDrag : MonoBehaviour,
    IPointerDownHandler,
    IDragHandler
{
    [Header("Window")]
    [SerializeField] private RectTransform window;

    private RectTransform parentRect;
    private Canvas canvas;

    private Vector2 dragOffset;
    private Vector2 originalPosition;
    private Vector2 startPointerPosition;

    private void Awake()
    {
        if (window == null)
            window = transform as RectTransform;

        parentRect = window.parent as RectTransform;
        canvas = GetComponentInParent<Canvas>();

        // Remember the original position.
        originalPosition = window.anchoredPosition;
    }

    /// <summary>
    /// Restores the document window to its original position.
    /// </summary>
    public void ResetWindowPosition()
    {
        window.anchoredPosition = originalPosition;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        window.SetAsLastSibling();

        startPointerPosition = eventData.position;

        Camera cam = canvas.renderMode == RenderMode.ScreenSpaceOverlay
            ? null
            : eventData.pressEventCamera;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parentRect,
            eventData.position,
            cam,
            out Vector2 pointerLocalPosition);

        dragOffset = window.anchoredPosition - pointerLocalPosition;
    }

    public void OnDrag(PointerEventData eventData)
    {
        // If the movement is mostly vertical, let SwipeDownToClose handle it.
        Vector2 delta = eventData.position - startPointerPosition;

        if (Mathf.Abs(delta.y) > Mathf.Abs(delta.x))
            return;

        Camera cam = canvas.renderMode == RenderMode.ScreenSpaceOverlay
            ? null
            : eventData.pressEventCamera;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parentRect,
            eventData.position,
            cam,
            out Vector2 pointerLocalPosition);

        window.anchoredPosition = pointerLocalPosition + dragOffset;
    }
}