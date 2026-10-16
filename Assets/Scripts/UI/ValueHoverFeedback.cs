using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

/// <summary>A quiet, raycast-free underline; independent of selection and drop tints.</summary>
[DisallowMultipleComponent]
public sealed class ValueHoverFeedback : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler, IPointerUpHandler
{
    private GameObject cue;

    public static void Ensure(Component owner)
    {
        if (owner.gameObject.scene.name != "Office_Level1")
            return;
        if (owner.transform is RectTransform && owner.GetComponent<ValueHoverFeedback>() == null)
            owner.gameObject.AddComponent<ValueHoverFeedback>();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (cue == null)
        {
            cue = new GameObject("ValueHoverCue", typeof(RectTransform), typeof(CanvasRenderer),
                typeof(Image), typeof(LayoutElement));
            cue.layer = gameObject.layer;
            RectTransform rect = (RectTransform)cue.transform;
            rect.SetParent(transform, false);
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.sizeDelta = new Vector2(-12f, 3f);
            rect.anchoredPosition = new Vector2(0f, 2f);
            cue.GetComponent<LayoutElement>().ignoreLayout = true;
            Image line = cue.GetComponent<Image>();
            line.color = new Color(0.25f, 0.36f, 0.5f, 0.6f);
            line.raycastTarget = false;
        }
        cue.SetActive(true);
    }

    public void OnPointerExit(PointerEventData eventData) => Hide();
    public void OnPointerUp(PointerEventData eventData)
    {
        // Mouse/pen hover persists after a click; touch ends at release.
        bool touch = eventData is ExtendedPointerEventData extended
            ? extended.pointerType == UIPointerType.Touch
            : eventData.pointerId >= 0;
        if (touch)
            Hide();
    }
    private void OnDisable() => Hide();

    private void Hide()
    {
        if (cue != null)
            cue.SetActive(false);
    }
}
