using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;

public class SwipeDownToClose : MonoBehaviour,
    IPointerDownHandler,
    IDragHandler,
    IPointerUpHandler
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
    private bool isDragging;

    private void OnEnable()
    {
        if (panelToAnimate != null)
            panelOriginalPos = panelToAnimate.anchoredPosition;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (panelToAnimate == null)
            return;

        dragStartPos = eventData.position;
        panelOriginalPos = panelToAnimate.anchoredPosition;
        isDragging = true;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging || panelToAnimate == null)
            return;

        Vector2 delta = eventData.position - dragStartPos;

        // Ignore mostly-horizontal drags.
        if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
            return;

        // Only move downward.
        float offsetY = Mathf.Min(0f, delta.y);

        panelToAnimate.anchoredPosition =
            panelOriginalPos + new Vector2(0f, offsetY);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!isDragging || panelToAnimate == null)
            return;

        isDragging = false;

        Vector2 delta = eventData.position - dragStartPos;

        // If this wasn't mostly vertical, let the drag script handle it.
        if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
        {
            StartCoroutine(SnapBack());
            return;
        }

        float downwardDistance = -delta.y;

        if (downwardDistance >= closeDistanceThreshold)
            StartCoroutine(SlideOutAndClose());
        else
            StartCoroutine(SnapBack());
    }

    private IEnumerator SlideOutAndClose()
    {
        Vector2 start = panelToAnimate.anchoredPosition;
        Vector2 end = panelOriginalPos + Vector2.down * slideOutDistance;

        float elapsed = 0f;

        while (elapsed < slideAnimationDuration)
        {
            elapsed += Time.deltaTime;

            panelToAnimate.anchoredPosition =
                Vector2.Lerp(start, end, elapsed / slideAnimationDuration);

            yield return null;
        }

        panelToAnimate.anchoredPosition = panelOriginalPos;

        OnSwipeClosed?.Invoke();
    }

    private IEnumerator SnapBack()
    {
        Vector2 start = panelToAnimate.anchoredPosition;

        float duration = 0.15f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            panelToAnimate.anchoredPosition =
                Vector2.Lerp(start, panelOriginalPos, elapsed / duration);

            yield return null;
        }

        panelToAnimate.anchoredPosition = panelOriginalPos;
    }
}