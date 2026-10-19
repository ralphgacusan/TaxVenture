using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// PURPOSE:
/// The Office Hours countdown: a small navy chip under the progress card
/// (top-left; it slides to the corner when the HUD steps aside at the desk)
/// with a ticking clock face, a deadline caption and the minutes left.
/// It turns amber, then pulsing red, as time runs short. Shown only
/// after the receptionist has announced the deadline and until the case is
/// done. Purely visual; never blocks touches.
/// </summary>
public sealed class OfficeHoursChip : MonoBehaviour
{
    private const float Width = 430f;
    private const float Height = 78f;

    private OfficeHours hours;
    private Level2ReviewClock reviewClock;
    private RectTransform chip;
    private CanvasGroup group;
    private Image rim;
    private Image face;
    private RectTransform hourHand;
    private RectTransform minuteHand;
    private TextMeshProUGUI caption;
    private TextMeshProUGUI value;
    private float alpha;
    private float pop;
    private float y;
    private int shownMinutes = -1;
    private int shownDay = -1;

    public static OfficeHoursChip Create(OfficeHours owner)
    {
        return Create(owner, null, owner.gameObject.scene);
    }

    public static OfficeHoursChip Create(Level2ReviewClock owner)
    {
        return Create(null, owner, owner.gameObject.scene);
    }

    private static OfficeHoursChip Create(OfficeHours hours, Level2ReviewClock reviewClock, UnityEngine.SceneManagement.Scene scene)
    {
        var host = new GameObject("Office Hours Canvas", typeof(RectTransform));
        SceneManager.MoveGameObjectToScene(host, scene);
        host.layer = LayerMask.NameToLayer("UI");
        Canvas canvas = host.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 450;
        CanvasScaler scaler = host.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        RectTransform safe = HudTheme.CreateRect("Safe Area", host.transform);
        HudTheme.Stretch(safe);
        safe.gameObject.AddComponent<MobileControlsSafeArea>();

        var view = host.AddComponent<OfficeHoursChip>();
        view.hours = hours;
        view.reviewClock = reviewClock;
        view.Build(safe);
        return view;
    }

    private void Build(RectTransform parent)
    {
        chip = HudTheme.CreateRect("Office Hours Chip", parent);
        chip.anchorMin = chip.anchorMax = new Vector2(0f, 1f);
        chip.pivot = new Vector2(0f, 1f);
        chip.sizeDelta = new Vector2(Width, Height);
        group = chip.gameObject.AddComponent<CanvasGroup>();
        group.alpha = 0f;
        group.blocksRaycasts = false;
        group.interactable = false;

        rim = HudTheme.AddImage(chip, HudTheme.Rounded, HudTheme.Brass, 18f);
        RectTransform body = HudTheme.CreateRect("Body", chip);
        HudTheme.Stretch(body, 3f);
        HudTheme.AddImage(body, HudTheme.Rounded, new Color32(16, 25, 38, 235), 16f);

        // Clock face with two hands.
        RectTransform clock = HudTheme.CreateRect("Clock", body);
        clock.anchorMin = clock.anchorMax = new Vector2(0f, 0.5f);
        clock.sizeDelta = new Vector2(54f, 54f);
        clock.anchoredPosition = new Vector2(40f, 0f);
        face = HudTheme.AddImage(clock, HudTheme.Circle, HudTheme.Brass);
        RectTransform inner = HudTheme.CreateRect("Inner", clock);
        HudTheme.Stretch(inner, 4f);
        HudTheme.AddImage(inner, HudTheme.Circle, new Color32(245, 240, 226, 255));
        hourHand = Hand(clock, "Hour", 15f, 4f);
        minuteHand = Hand(clock, "Minute", 21f, 3f);

        caption = HudTheme.CreateLabel("Caption", body, "", 17f, HudTheme.BrassSoft, TextAlignmentOptions.BottomLeft,
            FontStyles.Bold | FontStyles.UpperCase);
        RectTransform cr = caption.rectTransform;
        cr.anchorMin = new Vector2(0f, 0.5f);
        cr.anchorMax = new Vector2(1f, 1f);
        cr.offsetMin = new Vector2(80f, 0f);
        cr.offsetMax = new Vector2(-12f, -8f);
        caption.characterSpacing = 3f;

        value = HudTheme.CreateLabel("Value", body, "", 27f, HudTheme.TextPrimary, TextAlignmentOptions.TopLeft, FontStyles.Bold);
        RectTransform vr = value.rectTransform;
        vr.anchorMin = new Vector2(0f, 0f);
        vr.anchorMax = new Vector2(1f, 0.5f);
        vr.offsetMin = new Vector2(80f, 6f);
        vr.offsetMax = new Vector2(-12f, 0f);
    }

    private static RectTransform Hand(RectTransform clock, string name, float length, float thickness)
    {
        RectTransform hand = HudTheme.CreateRect(name, clock);
        hand.anchorMin = hand.anchorMax = new Vector2(0.5f, 0.5f);
        hand.pivot = new Vector2(0.5f, 0f);
        hand.sizeDelta = new Vector2(thickness, length);
        HudTheme.AddImage(hand, null, new Color32(29, 43, 60, 255));
        return hand;
    }

    /// <summary>A short attention pop (warnings).</summary>
    public void Pop() => pop = 1f;

    private void LateUpdate()
    {
        if (hours == null && reviewClock == null) return;
        float dt = Time.unscaledDeltaTime;
        bool level2 = reviewClock != null;
        bool show = level2
            ? reviewClock.Announced && !reviewClock.CaseDone && !reviewClock.InNextMorning && !HudRewardScreen.AnyOpen
            : hours.Announced && !hours.CaseDone && !hours.InNextMorning && !HudRewardScreen.AnyOpen;
        alpha = Mathf.MoveTowards(alpha, show ? 1f : 0f, dt * 4f);
        group.alpha = alpha;
        if (alpha <= 0f) return;

        // Under the progress card while the HUD is up; in the corner when it steps aside.
        OfficeHudRuntime hud = OfficeHudRuntime.Instance;
        float goal = hud != null && hud.IsShown ? -(HudTheme.Margin + 172f * HudTheme.HudScale + 14f) : -HudTheme.Margin;
        y = Mathf.Lerp(y == 0f ? goal : y, goal, 1f - Mathf.Exp(-10f * dt));
        chip.anchoredPosition = new Vector2(HudTheme.Margin, y);

        int minutes = level2 ? reviewClock.MinutesLeft : hours.MinutesLeft;
        int day = level2 ? reviewClock.Day : hours.Day;
        if (minutes != shownMinutes || day != shownDay)
        {
            shownMinutes = minutes;
            shownDay = day;
            caption.text = level2
                ? (day > 1 ? $"Day {day} · " : "") + "Review cutoff today " + reviewClock.DeadlineText
                : (day > 1 ? $"Day {day} · " : "") + "Auditor leaves " + hours.DeadlineText;
            value.text = minutes == 1 ? "1 min left" : $"{minutes} min left";
        }

        // Clock hands follow the in-game time.
        double t = level2 ? reviewClock.GameSeconds : hours.GameSeconds;
        minuteHand.localRotation = Quaternion.Euler(0f, 0f, -(float)(t / 60.0 % 60.0) * 6f);
        hourHand.localRotation = Quaternion.Euler(0f, 0f, -(float)(t / 3600.0 % 12.0) * 30f);

        float real = level2 ? reviewClock.RealSecondsLeft : hours.RealSecondsLeft;
        float amber = level2 ? reviewClock.AmberSeconds : hours.Config.amberRealSeconds;
        float red = level2 ? reviewClock.RedSeconds : hours.Config.redRealSeconds;
        Color state = real <= red ? HudTheme.Reputation : real <= amber ? HudTheme.Exp : HudTheme.Brass;
        float pulse = real <= red ? 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 9f) : 0f;
        rim.color = Color.Lerp(state, Color.white, pulse * 0.35f);
        face.color = state;
        value.color = real <= amber ? state : HudTheme.TextPrimary;

        pop = Mathf.MoveTowards(pop, 0f, dt * 1.6f);
        float scale = 1f + 0.08f * Mathf.Sin(pop * Mathf.PI) + 0.03f * pulse;
        chip.localScale = new Vector3(scale, scale, 1f);
    }
}
