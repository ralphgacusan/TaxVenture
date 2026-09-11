using UnityEngine;
using UnityEngine.EventSystems;

public class MobileLookArea : MonoBehaviour,
    IPointerDownHandler,
    IPointerUpHandler,
    IDragHandler
{
    [Header("Look Settings")]
    [SerializeField] private float sensitivity = 1f;

    public Vector2 LookDelta { get; private set; }

    private bool isTouching;

    public void OnPointerDown(PointerEventData eventData)
    {
        isTouching = true;
        LookDelta = Vector2.zero;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isTouching)
            return;

        LookDelta += eventData.delta * sensitivity;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        isTouching = false;
    }

    public Vector2 ConsumeDelta()
    {
        Vector2 delta = LookDelta;
        LookDelta = Vector2.zero;
        return delta;
    }
}