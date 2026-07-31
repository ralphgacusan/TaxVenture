using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// PURPOSE:
/// Detects a downward drag gesture on the book/folder panel and triggers a
/// close callback once the drag exceeds a distance threshold, playing a
/// simple slide-down animation first. Built entirely on Unity's
/// IPointerDownHandler/IDragHandler/IPointerUpHandler interfaces, which
/// already abstract over mouse vs. touch pointer data — per current-phase
/// direction, mouse-drag IS the swipe gesture for now, and this script
/// requires zero changes when real touch arrives later.
///
/// RESPONSIBILITIES:
/// - Track drag start position and current delta
/// - If released past the downward distance threshold, play a slide-down
///   animation then invoke OnSwipeClosed
/// - If released before the threshold, snap back to original position
///   (treated as "not a real close gesture")
/// </summary>
public class SwipeDownToClose : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [Header("Target to Animate")]
    [SerializeField] private RectTransform panelToAnimate;

    [Header("Gesture Settings")]
    [SerializeField] private float closeDistanceThreshold = 150f;
    [SerializeField] private float slideOutDistance = 800f;
    [SerializeField] private float slideAnimationDuration = 0.25f;

    public System.Action OnSwipeClosed;

    private Vector2 dragStartPos;
    private Vector2 panelOriginalPos;
    private bool isDragging = false;

    private void OnEnable()
    {
        if (panelToAnimate != null) panelOriginalPos = panelToAnimate.anchoredPosition;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        dragStartPos = eventData.position;
        panelOriginalPos = panelToAnimate.anchoredPosition;
        isDragging = true;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging) return;

        float deltaY = eventData.position.y - dragStartPos.y;
        // Only allow dragging downward (negative screen delta moves panel down);
        // clamp so dragging upward does nothing.
        float clampedDeltaY = Mathf.Min(0f, deltaY);

        panelToAnimate.anchoredPosition = panelOriginalPos + new Vector2(0f, clampedDeltaY);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!isDragging) return;
        isDragging = false;

        float deltaY = dragStartPos.y - eventData.position.y; // positive = dragged down

        if (deltaY >= closeDistanceThreshold)
        {
            StartCoroutine(SlideOutAndClose());
        }
        else
        {
            StartCoroutine(SnapBack());
        }
    }

    private System.Collections.IEnumerator SlideOutAndClose()
    {
        Vector2 start = panelToAnimate.anchoredPosition;
        Vector2 end = panelOriginalPos + new Vector2(0f, -slideOutDistance);
        float elapsed = 0f;

        while (elapsed < slideAnimationDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / slideAnimationDuration;
            panelToAnimate.anchoredPosition = Vector2.Lerp(start, end, t);
            yield return null;
        }

        panelToAnimate.anchoredPosition = panelOriginalPos; // reset for next open
        OnSwipeClosed?.Invoke();
    }

    private System.Collections.IEnumerator SnapBack()
    {
        Vector2 start = panelToAnimate.anchoredPosition;
        float elapsed = 0f;
        float duration = 0.15f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            panelToAnimate.anchoredPosition = Vector2.Lerp(start, panelOriginalPos, t);
            yield return null;
        }

        panelToAnimate.anchoredPosition = panelOriginalPos;
    }
}