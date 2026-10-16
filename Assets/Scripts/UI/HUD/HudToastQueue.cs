using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Kinds of HUD notification (sets the icon and accent colour).</summary>
public enum HudToastKind
{
    Objective,
    MainObjective,
    Exp,
    Reputation,
    Profile,
    Badge,
    Deduction,
    Locked,
    Passing
}

/// <summary>
/// PURPOSE:
/// Small, restrained notifications just below the objective tracker:
/// "Objective complete", "+85 EXP", "+1 Heart", "Profile collected 2/4",
/// "Badge earned", deductions ("Auditor found 2 mistakes"), "Locked" notices
/// and "Passing score reached". At most two are visible; more wait their turn.
/// They slide in, hold, then lift out; the second moves up when the first
/// leaves. A new toast of the same kind while one is still showing merges
/// into it (values add up) instead of stacking duplicates.
///
/// Lives inside the Office HUD's fading root, and its timers only run while
/// the HUD is shown — so notices raised during dialogue, desk views or
/// popups wait and are seen afterwards. Never blocks touches.
/// Presentation only: callers pass the real values from game events.
/// </summary>
public sealed class HudToastQueue : MonoBehaviour
{
    private const float Width = 380f;
    private const float Height = 62f;
    private const float Gap = 10f;
    private const int MaxVisible = 2;
    private const float EnterDuration = 0.25f;
    private const float ExitDuration = 0.3f;
    private const float HoldSeconds = 2.4f;
    private const float MainHoldSeconds = 3.2f;
    private const float EnterPop = 0.06f;
    private static readonly Color DeductionColor = new Color32(214, 120, 96, 255);
    private const float SlideDistance = 18f;

    private sealed class Toast
    {
        public HudToastKind Kind;
        public string Key;
        public string Title;
        public string Value;
        public Sprite Icon;
        public int Amount;
        public RectTransform Root;
        public CanvasGroup Group;
        public TextMeshProUGUI ValueText;
        public float Age;
        public float Hold;
        public bool Exiting;
        public float ExitAge;
        public float Y;
    }

    private OfficeHudRuntime host;
    private RectTransform area;
    private readonly List<Toast> visible = new List<Toast>();
    private readonly Queue<Toast> waiting = new Queue<Toast>();

    public static HudToastQueue Create(RectTransform parent, OfficeHudRuntime host, float topOffset)
    {
        RectTransform area = HudTheme.CreateRect("Toasts", parent);
        area.anchorMin = area.anchorMax = new Vector2(0.5f, 1f);
        area.pivot = new Vector2(0.5f, 1f);
        area.anchoredPosition = new Vector2(0f, -topOffset);
        area.sizeDelta = new Vector2(Width, (Height + Gap) * MaxVisible);
        area.localScale = new Vector3(HudTheme.HudScale, HudTheme.HudScale, 1f);

        var queue = area.gameObject.AddComponent<HudToastQueue>();
        queue.host = host;
        queue.area = area;
        return queue;
    }

    // =========================================================
    // API
    // =========================================================

    /// <summary>Shows a notice. amount > 0 merges with a showing toast of the same key.</summary>
    public void Show(HudToastKind kind, string title, string value, string mergeKey = null, int amount = 0,
        Sprite icon = null)
    {
        if (mergeKey != null)
        {
            for (int i = 0; i < visible.Count; i++)
            {
                Toast t = visible[i];
                if (t.Key != mergeKey || t.Exiting) continue;
                t.Amount += amount;
                t.Value = FormatAmount(kind, t.Amount, value);
                t.ValueText.text = t.Value;
                t.Age = Mathf.Min(t.Age, EnterDuration);
                return;
            }
            foreach (Toast t in waiting)
            {
                if (t.Key != mergeKey) continue;
                t.Amount += amount;
                t.Value = FormatAmount(kind, t.Amount, value);
                return;
            }
        }

        waiting.Enqueue(new Toast
        {
            Icon = icon,
            Kind = kind,
            Key = mergeKey,
            Title = title,
            Value = amount != 0 ? FormatAmount(kind, amount, value) : value,
            Amount = amount,
            Hold = kind == HudToastKind.MainObjective || kind == HudToastKind.Badge || kind == HudToastKind.Passing
                ? MainHoldSeconds
                : HoldSeconds
        });
    }

    private static string FormatAmount(HudToastKind kind, int amount, string fallback)
    {
        switch (kind)
        {
            case HudToastKind.Exp: return "+" + amount + " EXP";
            case HudToastKind.Reputation: return "+" + amount + (amount == 1 ? " Heart" : " Hearts");
            default: return fallback;
        }
    }

    // =========================================================
    // UPDATE
    // =========================================================

    private void Update()
    {
        // Notices wait while the HUD is hidden (dialogue, desk view, popups).
        if (host != null && !host.IsShown) return;

        float dt = Time.unscaledDeltaTime;

        while (waiting.Count > 0 && CountActive() < MaxVisible)
        {
            Toast t = waiting.Dequeue();
            Build(t);
            t.Y = SlotY(visible.Count);
            visible.Add(t);
        }

        for (int i = visible.Count - 1; i >= 0; i--)
        {
            Toast t = visible[i];
            if (!t.Exiting)
            {
                t.Age += dt;
                if (t.Age >= EnterDuration + t.Hold)
                {
                    t.Exiting = true;
                    t.ExitAge = 0f;
                }
            }
            else
            {
                t.ExitAge += dt;
                if (t.ExitAge >= ExitDuration)
                {
                    Destroy(t.Root.gameObject);
                    visible.RemoveAt(i);
                }
            }
        }

        int slot = 0;
        for (int i = 0; i < visible.Count; i++)
        {
            Toast t = visible[i];
            float targetY = SlotY(t.Exiting ? i : slot);
            if (!t.Exiting) slot++;
            t.Y = Mathf.Lerp(t.Y, targetY, 1f - Mathf.Exp(-12f * dt));

            float alpha;
            float offset;
            float scale = 1f;
            if (t.Exiting)
            {
                float k = Mathf.Clamp01(t.ExitAge / ExitDuration);
                alpha = 1f - k;
                offset = SlideDistance * k * k;
            }
            else
            {
                float k = HudTheme.EaseOutCubic(t.Age / EnterDuration);
                alpha = k;
                offset = -SlideDistance * (1f - k);
                // Small settle pop as it lands (also replays when a merge bumps the value).
                float p = Mathf.Clamp01(t.Age / (EnterDuration * 1.6f));
                scale = 1f + EnterPop * Mathf.Sin(p * Mathf.PI);
            }

            t.Group.alpha = alpha;
            t.Root.anchoredPosition = new Vector2(0f, t.Y + offset);
            t.Root.localScale = new Vector3(scale, scale, 1f);
        }
    }

    private int CountActive()
    {
        int n = 0;
        for (int i = 0; i < visible.Count; i++)
            if (!visible[i].Exiting) n++;
        return n;
    }

    private static float SlotY(int slot) => -slot * (Height + Gap);

    // =========================================================
    // BUILD
    // =========================================================

    private void Build(Toast t)
    {
        RectTransform frame = HudTheme.CreateRect("Toast " + t.Kind, area);
        frame.anchorMin = frame.anchorMax = new Vector2(0.5f, 1f);
        frame.pivot = new Vector2(0.5f, 1f);
        frame.sizeDelta = new Vector2(Width, Height);
        t.Group = frame.gameObject.AddComponent<CanvasGroup>();
        t.Group.blocksRaycasts = false;
        t.Group.interactable = false;
        t.Group.alpha = 0f;

        RectTransform body = HudTheme.CreatePanel("Panel", frame, 12f);
        HudTheme.Stretch((RectTransform)body.parent);

        Color accent = AccentOf(t.Kind);

        // Thin accent edge on the left so the kind reads at a glance.
        RectTransform edge = HudTheme.PlaceTopLeft(HudTheme.CreateRect("Accent", body), 0f, 10f, 4f, Height - 4f - 20f);
        HudTheme.AddImage(edge, HudTheme.Rounded, new Color(accent.r, accent.g, accent.b, 0.85f), 2f);

        RectTransform badge = HudTheme.PlaceTopLeft(HudTheme.CreateRect("Badge", body), 12f, (Height - 4f - 40f) * 0.5f, 40f, 40f);
        string glyphText = GlyphTextOf(t.Kind);
        if (t.Icon != null)
        {
            // A picture of its own (the earned medal), drawn a little larger.
            HudTheme.PlaceTopLeft(badge, 6f, (Height - 4f - 54f) * 0.5f, 54f, 54f);
            HudTheme.AddImage(badge, t.Icon, Color.white).preserveAspect = true;
        }
        else if (glyphText != null)
        {
            TextMeshProUGUI mark = HudTheme.CreateLabel("Glyph", badge, glyphText, 28f, HudTheme.PanelInner,
                TextAlignmentOptions.Center, FontStyles.Bold);
            HudTheme.Stretch((RectTransform)mark.transform);
        }
        else
        {
            HudTheme.AddImage(badge, HudTheme.Circle, accent);
            RectTransform glyph = HudTheme.CreateRect("Glyph", badge);
            HudTheme.Stretch(glyph, t.Kind == HudToastKind.Profile ? 11f : 8f);
            HudTheme.AddImage(glyph, GlyphOf(t.Kind), HudTheme.PanelInner);
        }

        bool brassTitle = t.Kind == HudToastKind.MainObjective || t.Kind == HudToastKind.Badge || t.Kind == HudToastKind.Passing;
        TextMeshProUGUI title = HudTheme.CreateLabel("Title", body, t.Title.ToUpperInvariant(), HudTheme.CaptionSize - 3f,
            brassTitle ? HudTheme.BrassSoft : HudTheme.TextMuted,
            TextAlignmentOptions.MidlineLeft, FontStyles.Bold);
        title.characterSpacing = 3f;
        HudTheme.PlaceTopLeft((RectTransform)title.transform, 64f, 6f, Width - 64f - 14f, 22f);

        t.ValueText = HudTheme.CreateLabel("Value", body, t.Value, HudTheme.ValueSize - 2f, HudTheme.TextPrimary,
            TextAlignmentOptions.MidlineLeft,
            t.Kind == HudToastKind.Objective || t.Kind == HudToastKind.Locked ? FontStyles.Normal : FontStyles.Bold);
        t.ValueText.enableAutoSizing = true;
        t.ValueText.fontSizeMin = HudTheme.CaptionSize - 4f;
        t.ValueText.fontSizeMax = HudTheme.ValueSize - 2f;
        t.ValueText.overflowMode = TextOverflowModes.Overflow;
        HudTheme.PlaceTopLeft((RectTransform)t.ValueText.transform, 64f, 27f, Width - 64f - 14f, 26f);

        foreach (Graphic g in frame.GetComponentsInChildren<Graphic>(true)) g.raycastTarget = false;
        t.Root = frame;
    }

    private static Color AccentOf(HudToastKind kind)
    {
        switch (kind)
        {
            case HudToastKind.Objective: return HudTheme.Complete;
            case HudToastKind.MainObjective: return HudTheme.Brass;
            case HudToastKind.Exp: return HudTheme.Exp;
            case HudToastKind.Reputation: return HudTheme.Reputation;
            case HudToastKind.Badge: return HudTheme.Brass;
            case HudToastKind.Deduction: return DeductionColor;
            case HudToastKind.Locked: return HudTheme.TextMuted;
            case HudToastKind.Passing: return HudTheme.Complete;
            default: return HudTheme.Active;
        }
    }

    /// <summary>Kinds drawn with a text mark instead of a sprite.</summary>
    private static string GlyphTextOf(HudToastKind kind)
    {
        switch (kind)
        {
            case HudToastKind.Deduction: return "-";
            case HudToastKind.Locked: return "!";
            default: return null;
        }
    }

    private static Sprite GlyphOf(HudToastKind kind)
    {
        switch (kind)
        {
            case HudToastKind.Objective:
            case HudToastKind.MainObjective:
            case HudToastKind.Passing: return HudTheme.Check;
            case HudToastKind.Exp:
            case HudToastKind.Badge: return HudTheme.Star;
            case HudToastKind.Reputation: return HudTheme.Heart;
            default: return HudTheme.Rounded;
        }
    }
}
