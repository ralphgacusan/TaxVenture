using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// PURPOSE:
/// While a value is being dragged (DragVisualManager's floating tag is up),
/// frames the drop field under the finger, the same spot the drop itself
/// uses: a soft pulsing outline with a small tag, green "DROP HERE" when the
/// field takes this kind of value, red "WON'T FIT" when it doesn't. Purely
/// visual; never blocks touches. Built by OfficeHudRuntime.
/// </summary>
public sealed class DropAimIndicator : MonoBehaviour
{
    // Above every paper window and 3D document layer (1101-1900), under the dragged tag.
    private const int SortingOrder = 29000;

    private RectTransform visual;
    private RectTransform root;
    private RectTransform frame;
    private Image outline;
    private Image fill;
    private RectTransform tagRect;
    private Image tagImage;
    private TextMeshProUGUI tagLabel;
    private Image tagCheck;
    private RectTransform checkBadge;
    private float fitPop;
    private CanvasGroup group;
    private readonly List<RaycastResult> results = new List<RaycastResult>();
    private readonly Vector3[] corners = new Vector3[4];
    private PointerEventData pointer;
    private EventSystem pointerOwner;
    private Component lastTarget;
    private bool lastFits;
    private DataValue lastValue;
    private float alpha;

    public static DropAimIndicator Create(DragVisualManager manager)
    {
        if (manager == null || manager.Visual == null) return null;
        var host = new GameObject("Drop Aim Indicator", typeof(RectTransform));
        host.layer = LayerMask.NameToLayer("UI");
        Canvas canvas = host.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = SortingOrder;
        var indicator = host.AddComponent<DropAimIndicator>();
        indicator.visual = manager.Visual.transform as RectTransform;
        indicator.root = (RectTransform)host.transform;
        indicator.Build();
        return indicator;
    }

    private void Build()
    {
        frame = HudTheme.CreateRect("Frame", root);
        frame.anchorMin = frame.anchorMax = Vector2.zero;
        frame.pivot = new Vector2(0.5f, 0.5f);
        group = frame.gameObject.AddComponent<CanvasGroup>();
        group.alpha = 0f;
        group.blocksRaycasts = false;
        group.interactable = false;

        fill = HudTheme.AddImage(frame, HudTheme.Rounded, Color.clear, 10f);
        RectTransform line = HudTheme.CreateRect("Outline", frame);
        HudTheme.Stretch(line, -6f);
        outline = HudTheme.AddImage(line, GuideArt.Frame, Color.white);
        outline.type = Image.Type.Sliced;
        outline.pixelsPerUnitMultiplier = 2.2f;

        tagRect = HudTheme.CreateRect("Tag", frame);
        tagRect.anchorMin = tagRect.anchorMax = new Vector2(1f, 1f);
        tagRect.pivot = new Vector2(1f, 0f);
        tagRect.anchoredPosition = new Vector2(4f, 6f);
        tagRect.sizeDelta = new Vector2(150f, 34f);
        tagImage = HudTheme.AddImage(tagRect, HudTheme.Rounded, Color.white, 17f);
        tagLabel = HudTheme.CreateLabel("Label", tagRect, "DROP HERE", 18f, Color.white, TextAlignmentOptions.Center,
            FontStyles.Bold | FontStyles.UpperCase);
        HudTheme.Stretch(tagLabel.rectTransform, 4f);

        // A round green check on the field's corner when this kind of value fits.
        checkBadge = HudTheme.CreateRect("Fits Check", frame);
        checkBadge.anchorMin = checkBadge.anchorMax = new Vector2(1f, 0.5f);
        checkBadge.pivot = new Vector2(0.5f, 0.5f);
        checkBadge.anchoredPosition = new Vector2(-26f, 0f);
        checkBadge.sizeDelta = new Vector2(40f, 40f);
        Image badge = HudTheme.AddImage(checkBadge, HudTheme.Circle, HudTheme.Complete);
        badge.raycastTarget = false;
        RectTransform tick = HudTheme.CreateRect("Tick", checkBadge);
        HudTheme.Stretch(tick, 8f);
        tagCheck = HudTheme.AddImage(tick, HudTheme.Check, Color.white);
        tagCheck.preserveAspect = true;
        tagCheck.raycastTarget = false;
    }

    private void LateUpdate()
    {
        float dt = Time.unscaledDeltaTime;
        bool dragging = visual != null && visual.gameObject.activeInHierarchy;
        Component target = dragging ? FindDestination(DragVisualManager.PointerScreen) : null;
        bool show = target != null && TryScreenRect(target.transform as RectTransform, out Rect rect) && Place(rect);

        alpha = Mathf.MoveTowards(alpha, show ? 1f : 0f, dt * (show ? 12f : 8f));
        group.alpha = alpha;
        if (!show) return;

        DataValue value = ValueTransferManager.Instance != null ? ValueTransferManager.Instance.SelectedValue : null;
        if (target != lastTarget || value != lastValue)
        {
            lastTarget = target;
            lastValue = value;
            bool fits = Fits(target, value);
            if (fits && (!lastFits || target != lastTarget)) fitPop = 1f;
            lastFits = fits;
        }
        fitPop = Mathf.MoveTowards(fitPop, 0f, dt * 4f);
        checkBadge.gameObject.SetActive(lastFits);
        checkBadge.localScale = Vector3.one * (1f + 0.35f * HudTheme.EaseOutBack(fitPop) * fitPop);

        Color c = lastFits ? HudTheme.Complete : HudTheme.Reputation;
        float pulse = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 8f);
        outline.color = new Color(c.r, c.g, c.b, 0.75f + 0.25f * pulse);
        fill.color = new Color(c.r, c.g, c.b, 0.12f + 0.06f * pulse);
        tagImage.color = new Color(c.r * 0.8f, c.g * 0.8f, c.b * 0.8f, 0.95f);
        tagLabel.text = lastFits ? "FITS" : "WON'T FIT";
    }

    private static bool Fits(Component target, DataValue value)
    {
        if (value == null) return false;
        if (target is CaseFolderFieldRow field) return field.WouldAccept(value);
        return target is IDataValueDestination destination && destination.CanAccept(value);
    }

    /// <summary>The drop target under a screen point, using the same UI raycast as the drop.</summary>
    private Component FindDestination(Vector2 screen)
    {
        EventSystem events = EventSystem.current;
        if (events == null) return null;
        if (pointer == null || pointerOwner != events)
        {
            pointer = new PointerEventData(events);
            pointerOwner = events;
        }
        pointer.position = screen;
        results.Clear();
        events.RaycastAll(pointer, results);
        for (int i = 0; i < results.Count; i++)
        {
            GameObject hit = results[i].gameObject;
            if (hit == null || hit.transform.IsChildOf(visual)) continue;
            IDataValueDestination destination = hit.GetComponentInParent<IDataValueDestination>();
            if (destination is Component component && component.transform is RectTransform) return component;
            // The first real UI under the finger decides (no seeing through windows).
            if (hit.GetComponent<Graphic>() is Graphic g && g.raycastTarget) return null;
        }
        return null;
    }

    private bool TryScreenRect(RectTransform rect, out Rect screenRect)
    {
        screenRect = default;
        if (rect == null) return false;
        Canvas canvas = rect.GetComponentInParent<Canvas>();
        if (canvas == null) return false;
        Canvas top = canvas.rootCanvas;
        Camera cam = top.renderMode == RenderMode.ScreenSpaceOverlay ? null
            : top.worldCamera != null ? top.worldCamera : Camera.main;
        rect.GetWorldCorners(corners);
        Vector2 min = new Vector2(float.MaxValue, float.MaxValue);
        Vector2 max = new Vector2(float.MinValue, float.MinValue);
        for (int i = 0; i < 4; i++)
        {
            Vector2 p = RectTransformUtility.WorldToScreenPoint(cam, corners[i]);
            min = Vector2.Min(min, p);
            max = Vector2.Max(max, p);
        }
        screenRect = Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        return screenRect.width > 2f && screenRect.height > 2f;
    }

    private bool Place(Rect screenRect)
    {
        // Overlay canvas without a scaler: local units are screen pixels.
        float scale = root.lossyScale.x > 0f ? root.lossyScale.x : 1f;
        frame.anchoredPosition = screenRect.center / scale;
        frame.sizeDelta = screenRect.size / scale + new Vector2(8f, 8f);
        return true;
    }
}
