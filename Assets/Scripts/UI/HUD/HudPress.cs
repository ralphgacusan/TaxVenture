using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// PURPOSE:
/// Soft touch feedback for runtime-built HUD buttons and cards: a squash
/// while pressed, a slight grow under the pointer (the Editor's mouse), and
/// an eased return. A disabled button stays still. Sits on the object that
/// receives the touch; scales Target (itself by default) so it never fights
/// another animation on a child.
///
/// Presentation only - nothing here reads or changes gameplay state.
/// </summary>
public sealed class HudPress : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    public RectTransform Target;
    public float HoverScale = 1.035f;
    public float PressedScale = 0.94f;

    private Selectable selectable;
    private bool over, down;
    private float scale = 1f;

    /// <summary>Adds feedback to rect (scaling target, or rect itself).</summary>
    public static HudPress Add(RectTransform rect, RectTransform target = null, float hover = 1.035f, float pressed = 0.94f)
    {
        HudPress press = rect.gameObject.AddComponent<HudPress>();
        press.Target = target != null ? target : rect;
        press.HoverScale = hover;
        press.PressedScale = pressed;
        return press;
    }

    private void Awake() => selectable = GetComponent<Selectable>();

    public void OnPointerEnter(PointerEventData eventData) => over = true;
    public void OnPointerExit(PointerEventData eventData) { over = false; down = false; }
    public void OnPointerDown(PointerEventData eventData) => down = true;
    public void OnPointerUp(PointerEventData eventData) => down = false;

    private void Update()
    {
        if (Target == null) return;
        bool live = selectable == null || selectable.IsInteractable();
        float goal = !live ? 1f : down ? PressedScale : over ? HoverScale : 1f;
        scale = Mathf.Lerp(scale, goal, 1f - Mathf.Exp(-22f * Time.unscaledDeltaTime));
        if (Mathf.Abs(scale - goal) < 0.0005f) scale = goal;
        Target.localScale = new Vector3(scale, scale, 1f);
    }

    private void OnDisable()
    {
        over = down = false;
        scale = 1f;
        if (Target != null) Target.localScale = Vector3.one;
    }
}
