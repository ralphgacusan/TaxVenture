using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// PURPOSE:
/// Game-feel skin for the shared floating value shown while dragging a value
/// (DragVisualManager's DraggedValueVisual): a navy value tag with a brass
/// rim, a soft lifted shadow and cream print text; it pops up when picked
/// up, floats just above the finger (so the thumb never hides it) and tilts
/// with the drag direction. DragVisualManager still owns text, size, show /
/// hide and position; this only restyles and animates the visual.
/// </summary>
[DisallowMultipleComponent]
public sealed class DragGhostSkin : MonoBehaviour
{
    private const float PopDuration = 0.2f;
    private const float MaxTilt = 10f;

    private RectTransform rect;
    private Vector3 lastPosition;
    private float tilt;
    private float t;
    private bool hasLast;

    /// <summary>Skins the manager's drag visual once (Office HUD).</summary>
    public static void Attach(DragVisualManager manager)
    {
        GameObject visual = manager != null ? manager.Visual : null;
        if (visual == null || visual.GetComponent<DragGhostSkin>() != null) return;
        visual.AddComponent<DragGhostSkin>().Setup();
    }

    private void Setup()
    {
        rect = (RectTransform)transform;
        // Just above the touch point (the thumb never hides it), with a small
        // pointer whose tip is exactly where the value will be dropped.
        rect.pivot = new Vector2(0.5f, -0.3f);

        Image image = GetComponent<Image>();
        if (image != null)
        {
            image.sprite = HudTheme.Rounded;
            image.type = Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = 20f / 22f;
            image.color = new Color(0.07f, 0.11f, 0.17f, 0.97f);
            image.raycastTarget = false;
            var rim = gameObject.AddComponent<Outline>();
            rim.effectColor = HudTheme.Brass;
            rim.effectDistance = new Vector2(3f, -3f);
            var lift = gameObject.AddComponent<Shadow>();
            lift.effectColor = new Color(0f, 0f, 0f, 0.4f);
            lift.effectDistance = new Vector2(6f, -12f);
        }

        TextMeshProUGUI text = GetComponentInChildren<TextMeshProUGUI>(true);
        if (text != null)
        {
            text.font = HudTheme.Font;
            text.fontStyle = FontStyles.Bold;
            text.color = HudTheme.TextPrimary;
            text.characterSpacing = 1f;
            text.raycastTarget = false;
        }

        // A small brass "tag hole" on the left edge.
        RectTransform hole = HudTheme.CreateRect("Tag Hole", rect);
        hole.anchorMin = hole.anchorMax = new Vector2(0f, 0.5f);
        hole.sizeDelta = new Vector2(16f, 16f);
        hole.anchoredPosition = new Vector2(20f, 0f);
        HudTheme.AddImage(hole, HudTheme.Circle, HudTheme.Brass);
        hole.SetAsFirstSibling();

        RectTransform pointer = HudTheme.CreateRect("Drop Pointer", rect);
        pointer.anchorMin = pointer.anchorMax = new Vector2(0.5f, 0f);
        pointer.pivot = new Vector2(0.5f, 1f);
        pointer.sizeDelta = new Vector2(26f, 22f);
        pointer.anchoredPosition = new Vector2(0f, 2f);
        pointer.localRotation = Quaternion.Euler(0f, 0f, 180f);
        pointer.pivot = new Vector2(0.5f, 0f);
        HudTheme.AddImage(pointer, GuideArt.Arrow, HudTheme.Brass);
    }

    private void OnEnable()
    {
        t = 0f;
        tilt = 0f;
        hasLast = false;
        if (rect != null) rect.localScale = Vector3.one * 0.6f;
    }

    private void LateUpdate()
    {
        if (rect == null) return;
        float dt = Time.unscaledDeltaTime;

        // Pick-up pop.
        t += dt;
        float k = Mathf.Clamp01(t / PopDuration);
        float s = Mathf.LerpUnclamped(0.6f, 1f, HudTheme.EaseOutBack(k));
        rect.localScale = new Vector3(s, s, 1f);

        // Lean into the drag direction, settle when still.
        Vector3 p = rect.position;
        float dx = hasLast && dt > 0f ? (p.x - lastPosition.x) / dt : 0f;
        lastPosition = p;
        hasLast = true;
        float target = Mathf.Clamp(-dx * 0.012f, -MaxTilt, MaxTilt);
        tilt = Mathf.Lerp(tilt, target, 1f - Mathf.Exp(-12f * dt));
        rect.localRotation = Quaternion.Euler(0f, 0f, tilt);
    }
}
