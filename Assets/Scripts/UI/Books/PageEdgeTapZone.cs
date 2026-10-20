using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// PURPOSE:
/// One invisible/subtle tap zone representing a page-turn edge (left =
/// previous, right = next). Fires a callback on click — used by both the
/// Case Folder and Tax Code Book's Paper, replacing explicit Previous/Next
/// buttons with direct "tap the edge of the page" interaction, per R5's
/// Papers, Please-inspired design.
///
/// The touch area is padded well beyond the round arrow (fingers are wider
/// than the arrow), and the arrow squashes on press and springs back on
/// release, so a tap always feels answered.
/// </summary>
public class PageEdgeTapZone : MonoBehaviour, IPointerClickHandler, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    /// <summary>How much larger than the arrow the invisible touch area is.</summary>
    public const float TouchPadScale = 1.5f;

    public System.Action OnTapped;

    private RectTransform pad;
    private Coroutine punch;

    private void Start() => EnsureTouchPad();

    private void OnEnable()
    {
        transform.localScale = Vector3.one;
    }

    /// <summary>A clear, raycast-only image behind the arrow, larger than it.</summary>
    private void EnsureTouchPad()
    {
        if (pad != null || !(transform is RectTransform rect)) return;
        Transform existing = rect.Find("Touch Pad");
        pad = existing as RectTransform;
        if (pad == null)
        {
            var go = new GameObject("Touch Pad", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.layer = gameObject.layer;
            pad = (RectTransform)go.transform;
            pad.SetParent(rect, false);
            pad.SetAsFirstSibling();
            Image hit = go.GetComponent<Image>();
            hit.color = new Color(1f, 1f, 1f, 0f);
            hit.raycastTarget = true;
            var ignore = go.AddComponent<LayoutElement>();
            ignore.ignoreLayout = true;
        }
        pad.anchorMin = pad.anchorMax = pad.pivot = new Vector2(0.5f, 0.5f);
        pad.anchoredPosition = Vector2.zero;
        Vector2 size = rect.rect.size;
        if (size.x < 1f || size.y < 1f) size = new Vector2(130f, 130f);
        pad.sizeDelta = size * TouchPadScale;
    }

    public void OnPointerDown(PointerEventData eventData) => Punch(0.88f, 0.07f);

    public void OnPointerUp(PointerEventData eventData) => Punch(1f, 0.18f);

    public void OnPointerExit(PointerEventData eventData) => Punch(1f, 0.12f);

    public void OnPointerClick(PointerEventData eventData)
    {
        OnTapped?.Invoke();
    }

    private void Punch(float target, float seconds)
    {
        if (!isActiveAndEnabled) return;
        if (punch != null) StopCoroutine(punch);
        punch = StartCoroutine(ScaleTo(target, seconds));
    }

    private IEnumerator ScaleTo(float target, float seconds)
    {
        float from = transform.localScale.x;
        for (float t = 0f; t < 1f;)
        {
            t = Mathf.Min(1f, t + Time.unscaledDeltaTime / Mathf.Max(0.01f, seconds));
            // Spring back past 1 a little when released.
            float e = target >= 1f ? 1f + 2.2f * Mathf.Pow(t - 1f, 3f) + 1.2f * Mathf.Pow(t - 1f, 2f) : t * (2f - t);
            float s = Mathf.LerpUnclamped(from, target, e);
            transform.localScale = new Vector3(s, s, 1f);
            yield return null;
        }
        transform.localScale = new Vector3(target, target, 1f);
        punch = null;
    }
}
