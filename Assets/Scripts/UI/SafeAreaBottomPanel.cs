using UnityEngine;

/// <summary>
/// Keeps a bottom-anchored panel inside the device safe area: it spans the safe
/// width and sits on the safe bottom edge, keeping its authored height and
/// bottom margin. Recomputes only when the screen or safe area changes.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class SafeAreaBottomPanel : MonoBehaviour
{
    [Tooltip("Gap above the safe bottom edge, in canvas units.")]
    [SerializeField, Min(0f)] private float bottomMargin;

    private RectTransform rect;
    private Rect previousArea;
    private Vector2Int previousSize;

    private void OnEnable()
    {
        rect = (RectTransform)transform;
        previousSize = Vector2Int.zero;
        Apply();
    }

    private void LateUpdate() => Apply();

    private void Apply()
    {
        if (Screen.width <= 0 || Screen.height <= 0) return;
        Rect area = Screen.safeArea;
        Vector2Int size = new Vector2Int(Screen.width, Screen.height);
        if (area == previousArea && size == previousSize) return;

        float bottom = area.yMin / size.y;
        rect.anchorMin = new Vector2(area.xMin / size.x, bottom);
        rect.anchorMax = new Vector2(area.xMax / size.x, bottom);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.sizeDelta = new Vector2(0f, rect.sizeDelta.y);
        rect.anchoredPosition = new Vector2(0f, bottomMargin);

        previousArea = area;
        previousSize = size;
    }
}
