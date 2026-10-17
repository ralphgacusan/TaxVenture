using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// PURPOSE:
/// The "PUT STAMPS AWAY" button shown while Level 2's desk stamps are out
/// (Level2DeskStamps). Same look as the desk's EXIT DESK pill (navy, brass
/// rim, cream capitals), bottom right, inside the safe area, with a touch
/// target well over 44 points. Tapping it sets both stamps back on the stand.
/// Lives on the HUD canvas but outside the HUD's fading group (the HUD fades
/// at the desk).
/// </summary>
public sealed class PutAwayButton : MonoBehaviour
{
    private static readonly Color Navy = new Color32(18, 28, 41, 235);
    private static readonly Color Cream = new Color32(245, 240, 226, 255);

    private Level2DeskStamps owner;
    private CanvasGroup group;
    private GameObject safeArea;
    private float alpha;

    public static PutAwayButton Create(Level2DeskStamps desk)
    {
        if (OfficeHudRuntime.Instance == null || OfficeHudRuntime.Instance.SafeRoot == null) return null;
        Transform canvasRoot = OfficeHudRuntime.Instance.SafeRoot.parent;
        if (canvasRoot == null) return null;

        RectTransform safe = HudTheme.CreateRect("Put Stamps Away Safe Area", canvasRoot);
        HudTheme.Stretch(safe);
        safe.gameObject.AddComponent<MobileControlsSafeArea>();
        safe.SetAsLastSibling();

        RectTransform frame = HudTheme.CreateRect("Put Stamps Away", safe);
        frame.anchorMin = frame.anchorMax = new Vector2(1f, 0f);
        frame.pivot = new Vector2(1f, 0f);
        frame.anchoredPosition = new Vector2(-HudTheme.Margin * 2f, HudTheme.Margin * 2f);
        frame.sizeDelta = new Vector2(330f, 84f);

        Image rim = HudTheme.AddImage(frame, HudTheme.Rounded, HudTheme.Brass, 40f);
        rim.raycastTarget = true;
        RectTransform body = HudTheme.CreateRect("Body", frame);
        HudTheme.Stretch(body, 3f);
        HudTheme.AddImage(body, HudTheme.Rounded, Navy, 38f);
        TextMeshProUGUI label = HudTheme.CreateLabel("Label", body, "PUT STAMPS AWAY", HudTheme.ValueSize, Cream,
            TextAlignmentOptions.Center, FontStyles.Bold);
        HudTheme.Stretch(label.rectTransform, 8f);

        PutAwayButton button = frame.gameObject.AddComponent<PutAwayButton>();
        button.owner = desk;
        button.safeArea = safe.gameObject;
        button.group = frame.gameObject.AddComponent<CanvasGroup>();
        button.group.alpha = 0f;
        button.group.interactable = button.group.blocksRaycasts = false;
        Button click = frame.gameObject.AddComponent<Button>();
        click.targetGraphic = rim;
        click.onClick.AddListener(button.Press);
        return button;
    }

    public void SetVisible(bool visible)
    {
        alpha = Mathf.MoveTowards(alpha, visible ? 1f : 0f, Time.unscaledDeltaTime * 8f);
        group.alpha = alpha;
        group.interactable = group.blocksRaycasts = visible;
    }

    private void Press()
    {
        if (AudioManager.Instance != null) AudioManager.Instance.PlaySelectSFX();
        if (owner != null) owner.PutAway();
    }

    private void OnDestroy()
    {
        if (safeArea != null) Destroy(safeArea);
    }
}
