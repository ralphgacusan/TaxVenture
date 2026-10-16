using UnityEngine;

/// <summary>Insets the full-screen mobile-controls root inside the device safe area.</summary>
[RequireComponent(typeof(RectTransform))]
public class MobileControlsSafeArea : MonoBehaviour
{
    private RectTransform controls;
    private Rect previousArea;
    private Vector2Int previousSize;

    private void OnEnable()
    {
        controls = (RectTransform)transform;
        previousSize = Vector2Int.zero;
        LateUpdate();
    }

    private void LateUpdate()
    {
        if (Screen.width <= 0 || Screen.height <= 0) return;
        Rect area = Screen.safeArea;
        Vector2Int size = new Vector2Int(Screen.width, Screen.height);
        if (area == previousArea && size == previousSize) return;
        controls.anchorMin = new Vector2(area.xMin / size.x, area.yMin / size.y);
        controls.anchorMax = new Vector2(area.xMax / size.x, area.yMax / size.y);
        controls.offsetMin = controls.offsetMax = Vector2.zero;
        previousArea = area;
        previousSize = size;
    }
}
