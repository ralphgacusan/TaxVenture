using UnityEngine;
using UnityEngine.EventSystems;

public class VirtualJoystick : MonoBehaviour,
    IPointerDownHandler,
    IPointerUpHandler,
    IDragHandler
{
    [Header("References")]
    [SerializeField] private RectTransform background;
    [SerializeField] private RectTransform handle;

    [Header("Settings")]
    [SerializeField] private float handleRange = 100f;

    public Vector2 Input { get; private set; }

    public float Horizontal => Input.x;
    public float Vertical => Input.y;

    private Camera uiCamera;

    private void Awake()
    {
        if (background == null)
            background = GetComponent<RectTransform>();

        Canvas canvas = GetComponentInParent<Canvas>();

        if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            uiCamera = canvas.worldCamera;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        OnDrag(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (background == null || handle == null)
            return;

        Vector2 localPoint;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                background,
                eventData.position,
                uiCamera,
                out localPoint))
        {
            return;
        }

        Vector2 halfSize = background.rect.size * 0.5f;

        Vector2 normalized = new Vector2(
            localPoint.x / halfSize.x,
            localPoint.y / halfSize.y
        );

        normalized = Vector2.ClampMagnitude(normalized, 1f);

        Input = normalized;

        handle.anchoredPosition = new Vector2(
            normalized.x * handleRange,
            normalized.y * handleRange
        );
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        ResetJoystick();
    }

    public void ResetJoystick()
    {
        Input = Vector2.zero;

        if (handle != null)
            handle.anchoredPosition = Vector2.zero;
    }

    public void Show()
    {
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        ResetJoystick();
        gameObject.SetActive(false);
    }

    public void SetVisible(bool visible)
    {
        if (visible)
            Show();
        else
            Hide();
    }
}