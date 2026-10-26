using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// PURPOSE:
/// Keeps one HUD element on its own sorting order (e.g. EXIT DESK above the
/// open desk documents, 2026-10-26). A nested Canvas added while its object
/// is hidden loses overrideSorting when the object is shown again, so the
/// order is (re)applied whenever the element is enabled and checked each
/// frame while it is visible. Adds the Canvas and GraphicRaycaster it needs.
/// </summary>
[DisallowMultipleComponent]
public sealed class HudSortingKeeper : MonoBehaviour
{
    public int Order = 2400;

    private Canvas own;

    private void OnEnable() => Apply();

    private void LateUpdate() => Apply();

    private void Apply()
    {
        if (own == null)
        {
            own = GetComponent<Canvas>();
            if (own == null) own = gameObject.AddComponent<Canvas>();
            if (GetComponent<GraphicRaycaster>() == null) gameObject.AddComponent<GraphicRaycaster>();
        }
        if (!own.overrideSorting) own.overrideSorting = true;
        if (own.sortingOrder != Order) own.sortingOrder = Order;
    }
}
