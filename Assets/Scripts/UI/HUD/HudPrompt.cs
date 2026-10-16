using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// PURPOSE:
/// The Office HUD's interaction prompt (mobile): a rounded navy plate at the
/// bottom centre with a round tap button that softly pulses, and two lines
/// of text, like a game action prompt:
///
///   "Click to talk to the Auditor"  ->  (tap)  TAP TO TALK
///                                              Auditor
///   "Click to sit at desk"          ->  (tap)  TAP TO SIT
///                                              Desk
///   "Printer"                       ->  Printer               (name only)
///   "Finish Side Quest 1 first"     ->  (lock) LOCKED
///                                              Finish Side Quest 1 first
///
/// TaxVenture is a phone game, so the glyph is always the tap glyph.
/// Presentation only: InteractionPromptUI still decides when a prompt shows
/// (same Interactor timing); this replaces its plain panel through
/// InteractionPromptUI.ShowOverride / HideOverride. Fades and pops in over
/// 0.15 s, never blocks touches, and does no work while hidden.
/// </summary>
public sealed class HudPrompt : MonoBehaviour
{
    public enum Kind
    {
        Action,
        Name,
        Locked
    }

    /// <summary>A prompt string split into what the prompt shows.</summary>
    public struct Parts
    {
        public Kind Kind;
        public string Verb;
        public string Target;
    }

    private const string ClickPrefix = "Click to ";
    private const float Height = 84f;
    private const float ButtonSize = 62f;
    private const float Pad = 11f;
    private const float Gap = 16f;
    private const float EndPad = 28f;
    private const float CaptionSize = 18f;
    private const float TitleSize = 30f;
    private const float MaxTextWidth = 560f;
    private const float ShowDuration = 0.15f;
    private const float HideDuration = 0.12f;
    private static readonly Color LockedRed = new Color32(226, 112, 96, 255);

    private RectTransform root;
    private CanvasGroup group;
    private RectTransform frame;
    private RectTransform button;
    private Image buttonImage;
    private Image glyph;
    private RectTransform pulseRing;
    private Image pulseImage;
    private TextMeshProUGUI captionText;
    private TextMeshProUGUI titleText;
    private string shownMessage;
    private Kind shownKind;
    private bool visible;
    private float t;

    // =========================================================
    // PARSING (pure, tested)
    // =========================================================

    /// <summary>Splits a prompt string into kind, verb and target.</summary>
    public static Parts Parse(string message)
    {
        string text = string.IsNullOrWhiteSpace(message) ? string.Empty : message.Trim();

        if (text == LevelGate.SideQuestFirstMessage)
            return new Parts { Kind = Kind.Locked, Target = text };

        if (!text.StartsWith(ClickPrefix, System.StringComparison.OrdinalIgnoreCase))
            return new Parts { Kind = Kind.Name, Target = text };

        string rest = text.Substring(ClickPrefix.Length).Trim();
        int space = rest.IndexOf(' ');
        if (space < 0)
            return new Parts { Kind = Kind.Action, Verb = Capitalize(rest), Target = string.Empty };

        string verb = rest.Substring(0, space);
        string target = rest.Substring(space + 1).Trim();

        // "talk to X" -> Talk · X ; "present findings to X" -> Present findings · X
        // "sit at desk" -> Sit · Desk
        if (target.StartsWith("to ", System.StringComparison.OrdinalIgnoreCase) ||
            target.StartsWith("at ", System.StringComparison.OrdinalIgnoreCase))
        {
            target = target.Substring(3).Trim();
        }
        else
        {
            int to = target.IndexOf(" to ", System.StringComparison.OrdinalIgnoreCase);
            if (to > 0 && target.Substring(0, to).IndexOf(' ') < 0)
            {
                verb += " " + target.Substring(0, to);
                target = target.Substring(to + 4).Trim();
            }
        }

        if (target.StartsWith("the ", System.StringComparison.OrdinalIgnoreCase))
            target = target.Substring(4).Trim();

        return new Parts { Kind = Kind.Action, Verb = Capitalize(verb), Target = Capitalize(target) };
    }

    private static string Capitalize(string s) =>
        string.IsNullOrEmpty(s) ? s : char.ToUpperInvariant(s[0]) + s.Substring(1);

    /// <summary>The small caption line above the name ("TAP TO TALK", "LOCKED"); null for a plain name.</summary>
    public static string CaptionOf(Parts parts)
    {
        switch (parts.Kind)
        {
            case Kind.Action: return ("Tap to " + (parts.Verb ?? string.Empty).ToLowerInvariant()).ToUpperInvariant();
            case Kind.Locked: return "LOCKED";
            default: return null;
        }
    }

    // =========================================================
    // BUILD
    // =========================================================

    /// <summary>Builds the prompt where the plain prompt panel was and takes over its display.</summary>
    public static HudPrompt Create(Transform parent, int siblingIndex)
    {
        RectTransform root = HudTheme.CreateRect("HUD Prompt", parent);
        root.anchorMin = root.anchorMax = new Vector2(0.5f, 0f);
        root.pivot = new Vector2(0.5f, 0f);
        root.anchoredPosition = new Vector2(0f, 70f);
        root.sizeDelta = new Vector2(10f, Height);
        if (siblingIndex >= 0) root.SetSiblingIndex(siblingIndex);

        var prompt = root.gameObject.AddComponent<HudPrompt>();
        prompt.root = root;
        prompt.Build();
        InteractionPromptUI.ShowOverride = prompt.Show;
        InteractionPromptUI.HideOverride = prompt.Hide;
        return prompt;
    }

    private void Build()
    {
        group = root.gameObject.AddComponent<CanvasGroup>();
        group.alpha = 0f;
        group.blocksRaycasts = false;
        group.interactable = false;

        RectTransform body = HudTheme.CreatePanel("Plate", root, Height * 0.5f);
        frame = (RectTransform)body.parent;
        HudTheme.Stretch(frame);
        body.GetComponent<Image>().color = new Color32(16, 25, 38, 238);
        RectTransform sheen = HudTheme.CreateRect("Sheen", body);
        sheen.anchorMin = new Vector2(0f, 0.5f);
        sheen.anchorMax = Vector2.one;
        sheen.offsetMin = new Vector2(Height * 0.3f, 0f);
        sheen.offsetMax = new Vector2(-Height * 0.3f, -3f);
        HudTheme.AddImage(sheen, HudTheme.Rounded, new Color(1f, 1f, 1f, 0.05f), 14f);

        // Round tap button with a soft pulse ring.
        button = HudTheme.CreateRect("Tap Button", body);
        button.anchorMin = button.anchorMax = new Vector2(0f, 0.5f);
        button.pivot = new Vector2(0.5f, 0.5f);
        button.sizeDelta = new Vector2(ButtonSize, ButtonSize);
        button.anchoredPosition = new Vector2(Pad + ButtonSize * 0.5f, 0f);
        pulseRing = HudTheme.CreateRect("Pulse", button);
        HudTheme.Stretch(pulseRing);
        pulseImage = HudTheme.AddImage(pulseRing, HudTheme.Circle, new Color(HudTheme.Active.r, HudTheme.Active.g, HudTheme.Active.b, 0f));
        buttonImage = HudTheme.AddImage(HudTheme.CreateRect("Face", button), HudTheme.Circle, HudTheme.Active);
        HudTheme.Stretch(buttonImage.rectTransform);
        RectTransform rim = HudTheme.CreateRect("Rim", buttonImage.rectTransform);
        HudTheme.Stretch(rim, 3f);
        HudTheme.AddImage(rim, HudTheme.Circle, new Color(1f, 1f, 1f, 0.12f));
        RectTransform mark = HudTheme.CreateRect("Glyph", buttonImage.rectTransform);
        HudTheme.Stretch(mark, 12f);
        glyph = HudTheme.AddImage(mark, HudTheme.Tap, HudTheme.PanelInner);

        captionText = HudTheme.CreateLabel("Caption", body, string.Empty, CaptionSize, HudTheme.BrassSoft,
            TextAlignmentOptions.BottomLeft, FontStyles.Bold);
        captionText.characterSpacing = 5f;
        captionText.enableAutoSizing = false;
        captionText.overflowMode = TextOverflowModes.Overflow;

        titleText = HudTheme.CreateLabel("Title", body, string.Empty, TitleSize, HudTheme.TextPrimary,
            TextAlignmentOptions.TopLeft, FontStyles.Bold);

        root.localScale = new Vector3(HudTheme.HudScale, HudTheme.HudScale, 1f);
        enabled = false;
    }

    // =========================================================
    // SHOW / HIDE
    // =========================================================

    private bool Show(string message)
    {
        if (this == null || root == null) return false;
        if (!visible || message != shownMessage)
        {
            Layout(Parse(message));
            shownMessage = message;
        }
        if (!visible)
        {
            visible = true;
            t = 0f;
            root.gameObject.SetActive(true);
        }
        else
        {
            // New text while showing: a short settle pop.
            t = Mathf.Min(t, ShowDuration * 0.5f);
        }
        enabled = true;
        return true;
    }

    private void Hide()
    {
        if (this == null || !visible) return;
        visible = false;
        t = 0f;
        enabled = true;
    }

    private void Layout(Parts parts)
    {
        shownKind = parts.Kind;
        bool hasButton = parts.Kind != Kind.Name;
        string caption = CaptionOf(parts);

        button.gameObject.SetActive(hasButton);
        if (hasButton)
        {
            bool locked = parts.Kind == Kind.Locked;
            buttonImage.color = locked ? new Color32(96, 108, 124, 255) : HudTheme.Active;
            // Mobile game: always the tap glyph (a padlock when locked).
            glyph.sprite = locked ? HudTheme.Lock : HudTheme.Tap;
            HudTheme.Stretch(glyph.rectTransform, locked ? 15f : 12f);
        }

        float x = hasButton ? Pad + ButtonSize + Gap : EndPad + 4f;
        string title = string.IsNullOrEmpty(parts.Target) ? (parts.Verb ?? string.Empty) : parts.Target;

        captionText.gameObject.SetActive(caption != null);
        captionText.text = caption ?? string.Empty;
        captionText.color = parts.Kind == Kind.Locked ? LockedRed : HudTheme.BrassSoft;

        titleText.text = title;
        titleText.color = parts.Kind == Kind.Locked ? HudTheme.TextMuted : HudTheme.TextPrimary;
        titleText.enableAutoSizing = false;
        titleText.fontSize = parts.Kind == Kind.Locked ? TitleSize - 4f : TitleSize;
        float titleWidth = title.Length == 0 ? 0f : titleText.GetPreferredValues(title).x;
        float captionWidth = caption == null ? 0f : captionText.GetPreferredValues(caption).x;
        if (titleWidth > MaxTextWidth)
        {
            // Very long names shrink to fit rather than clip.
            titleText.enableAutoSizing = true;
            titleText.fontSizeMax = titleText.fontSize;
            titleText.fontSizeMin = TitleSize * 0.6f;
            titleWidth = MaxTextWidth;
        }
        float textWidth = Mathf.Max(titleWidth, captionWidth);

        RectTransform captionRect = (RectTransform)captionText.transform;
        RectTransform titleRect = (RectTransform)titleText.transform;
        captionRect.anchorMin = captionRect.anchorMax = new Vector2(0f, 0.5f);
        titleRect.anchorMin = titleRect.anchorMax = new Vector2(0f, 0.5f);
        captionRect.pivot = new Vector2(0f, 0f);
        titleRect.pivot = new Vector2(0f, caption == null ? 0.5f : 1f);
        captionRect.sizeDelta = new Vector2(textWidth + 4f, 24f);
        titleRect.sizeDelta = new Vector2(textWidth + 4f, 38f);
        captionRect.anchoredPosition = new Vector2(x, 2f);
        titleRect.anchoredPosition = new Vector2(x, caption == null ? 0f : 4f);
        titleText.alignment = caption == null ? TextAlignmentOptions.MidlineLeft : TextAlignmentOptions.TopLeft;

        root.sizeDelta = new Vector2(x + textWidth + EndPad, Height);
    }

    private void Update()
    {
        t += Time.unscaledDeltaTime;
        if (visible)
        {
            float k = Mathf.Clamp01(t / ShowDuration);
            group.alpha = HudTheme.EaseOutCubic(k);
            float s = Mathf.LerpUnclamped(0.9f, 1f, HudTheme.EaseOutBack(k));
            frame.localScale = new Vector3(s, s, 1f);

            // The tap button breathes: a ring grows out of it and fades.
            if (shownKind == Kind.Action)
            {
                float p = Mathf.Repeat(Time.unscaledTime, 1.4f) / 1.4f;
                float ring = 1f + 0.55f * HudTheme.EaseOutCubic(p);
                pulseRing.localScale = new Vector3(ring, ring, 1f);
                Color c = pulseImage.color;
                c.a = 0.45f * (1f - p);
                pulseImage.color = c;
            }
            else
            {
                Color c = pulseImage.color;
                c.a = 0f;
                pulseImage.color = c;
            }
        }
        else
        {
            float k = Mathf.Clamp01(t / HideDuration);
            group.alpha = 1f - k;
            if (k >= 1f)
            {
                group.alpha = 0f;
                enabled = false;
            }
        }
    }

    private void OnDestroy()
    {
        if (InteractionPromptUI.ShowOverride == (System.Func<string, bool>)Show) InteractionPromptUI.ShowOverride = null;
        if (InteractionPromptUI.HideOverride == (System.Action)Hide) InteractionPromptUI.HideOverride = null;
    }
}
