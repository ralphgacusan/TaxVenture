using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// PURPOSE:
/// The level opening card: "WELCOME TO / TaxVenture!" with a letter-by-
/// letter pop, the level subtitle, the main objectives (read from the live
/// objectives list, so it always matches objectives.json) sliding in one by
/// one, the guide's portrait peeking in, twinkling stars and a pulsing
/// "Let's go!" button. Text comes from guide.json "welcome".
/// </summary>
public sealed class GuideWelcomeCard : MonoBehaviour
{
    private const float CardWidth = 1120f;
    private const float RowHeight = 92f;

    public struct ObjectiveLine
    {
        public string Title;
        public string Detail;
    }

    private const float StageWidth = 1760f;
    private const float CardX = StageWidth * 0.5f - CardWidth * 0.5f;

    private RectTransform root;
    private RectTransform stage;
    private CanvasGroup group;
    private RectTransform card;
    private TextMeshProUGUI title;
    private RectTransform portraitRect;
    private RectTransform button;
    private CanvasGroup buttonGroup;
    private readonly List<RectTransform> rows = new List<RectTransform>();
    private readonly List<CanvasGroup> rowGroups = new List<CanvasGroup>();
    private readonly List<RectTransform> stars = new List<RectTransform>();
    private readonly List<Image> starImages = new List<Image>();
    private float age;
    private bool closing;
    private float closeAge;
    private Action onClosed;
    private Color accent;

    public static GuideWelcomeCard Show(RectTransform parent, GuideWelcome data, IList<ObjectiveLine> objectives,
        Sprite guidePortrait, string guideName, string guideRole, Color accent, Action onClosed)
    {
        RectTransform rect = HudTheme.CreateRect("Guide Welcome", parent);
        HudTheme.Stretch(rect);
        var welcome = rect.gameObject.AddComponent<GuideWelcomeCard>();
        welcome.root = rect;
        welcome.onClosed = onClosed;
        welcome.accent = accent;
        welcome.Build(data ?? new GuideWelcome(), objectives, guidePortrait, guideName, guideRole);
        return welcome;
    }

    private void Build(GuideWelcome data, IList<ObjectiveLine> objectives, Sprite guidePortrait, string guideName, string guideRole)
    {
        group = root.gameObject.AddComponent<CanvasGroup>();
        group.alpha = 0f;

        // Backdrop blocks the game; tapping it does nothing (use the button).
        Image backdrop = root.gameObject.AddComponent<Image>();
        backdrop.color = new Color(0.02f, 0.035f, 0.07f, 0.8f);
        backdrop.raycastTarget = true;

        int count = data.listObjectives && objectives != null ? objectives.Count : 0;
        float cardHeight = 330f + (count > 0 ? 56f + count * RowHeight : 0f) + 110f;

        // Card on the right, the guide standing beside it on the left; both
        // scale together so they fit any screen.
        stage = HudTheme.CreateRect("Stage", root);
        stage.anchorMin = stage.anchorMax = new Vector2(0.5f, 0.5f);
        stage.sizeDelta = new Vector2(StageWidth, cardHeight);

        RectTransform frame = HudTheme.CreateRect("Card", stage);
        frame.anchorMin = frame.anchorMax = new Vector2(0.5f, 0.5f);
        frame.sizeDelta = new Vector2(CardWidth, cardHeight);
        frame.anchoredPosition = new Vector2(guidePortrait != null ? CardX : 0f, 0f);
        card = frame;
        RectTransform body = HudTheme.CreatePanel("Panel", frame, 22f);
        HudTheme.Stretch((RectTransform)body.parent);

        // Top band.
        RectTransform band = HudTheme.CreateRect("Band", body);
        band.anchorMin = new Vector2(0f, 1f);
        band.anchorMax = new Vector2(1f, 1f);
        band.pivot = new Vector2(0.5f, 1f);
        band.sizeDelta = new Vector2(-8f, 300f);
        band.anchoredPosition = new Vector2(0f, -4f);
        HudTheme.AddImage(band, HudTheme.Rounded, new Color32(28, 46, 68, 255), 18f);

        TextMeshProUGUI kicker = HudTheme.CreateLabel("Kicker", band, data.kicker, 30f, HudTheme.BrassSoft,
            TextAlignmentOptions.Center, FontStyles.Bold | FontStyles.UpperCase);
        Place(kicker.rectTransform, 0f, -36f, 800f, 44f);
        kicker.characterSpacing = 18f;

        title = HudTheme.CreateLabel("Title", band, data.title, 104f, Color.white, TextAlignmentOptions.Center, FontStyles.Bold);
        Place(title.rectTransform, 0f, -84f, 1000f, 130f);
        title.enableVertexGradient = true;
        title.colorGradient = new VertexGradient(new Color32(255, 240, 196, 255), new Color32(255, 240, 196, 255),
            new Color32(241, 184, 82, 255), new Color32(241, 184, 82, 255));
        title.outlineWidth = 0.18f;
        title.outlineColor = new Color32(40, 26, 10, 255);

        TextMeshProUGUI subtitle = HudTheme.CreateLabel("Subtitle", band, data.subtitle, 26f, accent,
            TextAlignmentOptions.Center, FontStyles.Bold | FontStyles.UpperCase);
        Place(subtitle.rectTransform, 0f, -216f, 900f, 36f);
        subtitle.characterSpacing = 10f;

        if (!string.IsNullOrEmpty(data.tagline))
        {
            TextMeshProUGUI tagline = HudTheme.CreateLabel("Tagline", band, data.tagline, 22f, HudTheme.TextMuted,
                TextAlignmentOptions.Center);
            Place(tagline.rectTransform, 0f, -254f, 960f, 32f);
        }

        // Twinkling stars around the title.
        Vector2[] starSpots =
        {
            new Vector2(-470f, -90f), new Vector2(-400f, -190f), new Vector2(455f, -70f),
            new Vector2(410f, -200f), new Vector2(-300f, -40f), new Vector2(330f, -30f)
        };
        for (int i = 0; i < starSpots.Length; i++)
        {
            RectTransform s = HudTheme.CreateRect("Star " + i, band);
            s.anchorMin = s.anchorMax = new Vector2(0.5f, 1f);
            s.anchoredPosition = starSpots[i];
            float size = i % 2 == 0 ? 34f : 22f;
            s.sizeDelta = new Vector2(size, size);
            starImages.Add(HudTheme.AddImage(s, HudTheme.Star, HudTheme.Exp));
            stars.Add(s);
        }

        // Objectives.
        float y = -322f;
        if (count > 0)
        {
            TextMeshProUGUI heading = HudTheme.CreateLabel("Objectives Heading", body, data.objectivesHeading, 24f,
                HudTheme.Brass, TextAlignmentOptions.Center, FontStyles.Bold | FontStyles.UpperCase);
            Place(heading.rectTransform, 0f, y, 900f, 34f);
            heading.characterSpacing = 10f;
            y -= 52f;

            for (int i = 0; i < count; i++)
            {
                RectTransform row = HudTheme.CreateRect("Objective " + (i + 1), body);
                row.anchorMin = row.anchorMax = new Vector2(0.5f, 1f);
                row.pivot = new Vector2(0.5f, 1f);
                row.sizeDelta = new Vector2(CardWidth - 120f, RowHeight - 12f);
                row.anchoredPosition = new Vector2(0f, y);
                HudTheme.AddImage(row, HudTheme.Rounded, new Color32(24, 38, 56, 255), 14f);
                rowGroups.Add(row.gameObject.AddComponent<CanvasGroup>());
                rows.Add(row);

                RectTransform badge = HudTheme.CreateRect("Badge", row);
                badge.anchorMin = badge.anchorMax = new Vector2(0f, 0.5f);
                badge.sizeDelta = new Vector2(56f, 56f);
                badge.anchoredPosition = new Vector2(48f, 0f);
                HudTheme.AddImage(badge, HudTheme.Circle, HudTheme.Brass);
                TextMeshProUGUI number = HudTheme.CreateLabel("Number", badge, (i + 1).ToString(), 30f,
                    new Color32(29, 32, 40, 255), TextAlignmentOptions.Center, FontStyles.Bold);
                HudTheme.Stretch(number.rectTransform);

                TextMeshProUGUI rowTitle = HudTheme.CreateLabel("Title", row, objectives[i].Title, 30f,
                    HudTheme.TextPrimary, TextAlignmentOptions.MidlineLeft, FontStyles.Bold);
                RectTransform tr = rowTitle.rectTransform;
                tr.anchorMin = new Vector2(0f, 0.5f);
                tr.anchorMax = new Vector2(1f, 1f);
                tr.offsetMin = new Vector2(98f, -2f);
                tr.offsetMax = new Vector2(-24f, -6f);

                TextMeshProUGUI detail = HudTheme.CreateLabel("Detail", row, objectives[i].Detail, 21f,
                    HudTheme.TextMuted, TextAlignmentOptions.MidlineLeft);
                RectTransform dr = detail.rectTransform;
                dr.anchorMin = new Vector2(0f, 0f);
                dr.anchorMax = new Vector2(1f, 0.5f);
                dr.offsetMin = new Vector2(98f, 6f);
                dr.offsetMax = new Vector2(-24f, 2f);

                y -= RowHeight;
            }
        }

        // Button.
        button = HudTheme.CreateRect("Go Button", body);
        button.anchorMin = button.anchorMax = new Vector2(0.5f, 0f);
        button.pivot = new Vector2(0.5f, 0f);
        button.sizeDelta = new Vector2(340f, 78f);
        button.anchoredPosition = new Vector2(0f, 26f);
        buttonGroup = button.gameObject.AddComponent<CanvasGroup>();
        buttonGroup.alpha = 0f;
        Image buttonRim = HudTheme.AddImage(button, HudTheme.Rounded, HudTheme.Brass, 39f);
        buttonRim.raycastTarget = true;
        RectTransform buttonFill = HudTheme.CreateRect("Fill", button);
        HudTheme.Stretch(buttonFill, 4f);
        HudTheme.AddImage(buttonFill, HudTheme.Rounded, new Color32(62, 160, 98, 255), 35f);
        TextMeshProUGUI buttonLabel = HudTheme.CreateLabel("Label", buttonFill, data.button, 34f, Color.white,
            TextAlignmentOptions.Center, FontStyles.Bold | FontStyles.UpperCase);
        HudTheme.Stretch(buttonLabel.rectTransform, 6f);
        buttonLabel.characterSpacing = 8f;
        Button go = button.gameObject.AddComponent<Button>();
        go.targetGraphic = buttonRim;
        go.onClick.AddListener(Close);

        // The guide peeks in at the bottom-left of the card.
        if (guidePortrait != null)
        {
            portraitRect = HudTheme.CreateRect("Guide Portrait", stage);
            portraitRect.anchorMin = portraitRect.anchorMax = new Vector2(0.5f, 0.5f);
            portraitRect.pivot = new Vector2(0.5f, 0f);
            portraitRect.sizeDelta = new Vector2(570f, 396f);
            Image p = HudTheme.AddImage(portraitRect, guidePortrait, Color.white);
            p.preserveAspect = true;

            RectTransform plate = HudTheme.CreateRect("Guide Plate", portraitRect);
            plate.anchorMin = plate.anchorMax = new Vector2(0.5f, 0f);
            plate.pivot = new Vector2(0.5f, 1f);
            plate.sizeDelta = new Vector2(400f, 88f);
            plate.anchoredPosition = new Vector2(0f, 6f);
            HudTheme.AddImage(plate, HudTheme.Rounded, HudTheme.Brass, 12f);
            TextMeshProUGUI who = HudTheme.CreateLabel("Name", plate, guideName, 34f, new Color32(29, 32, 40, 255),
                TextAlignmentOptions.Top, FontStyles.Bold | FontStyles.UpperCase);
            HudTheme.Stretch(who.rectTransform, 4f);
            who.characterSpacing = 6f;
            TextMeshProUGUI role = HudTheme.CreateLabel("Role", plate, guideRole, 22f, new Color32(60, 48, 30, 255),
                TextAlignmentOptions.Bottom);
            HudTheme.Stretch(role.rectTransform, 5f);
        }

    }

    private static void Place(RectTransform rect, float x, float y, float width, float height)
    {
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(x, y);
        rect.sizeDelta = new Vector2(width, height);
    }

    public void Close()
    {
        if (closing) return;
        closing = true;
        closeAge = 0f;
        group.blocksRaycasts = false;
    }

    private void LateUpdate()
    {
        if (root == null || card == null) return;
        Rect r = root.rect;
        float stageWidth = portraitRect != null ? StageWidth : CardWidth;
        float fit = Mathf.Min(1f, (r.width - 60f) / stageWidth, (r.height - 60f) / card.sizeDelta.y);
        float baseScale = Mathf.Max(0.35f, fit);
        stage.localScale = Vector3.one * baseScale;

        float dt = Time.unscaledDeltaTime;
        age += dt;
        float t;
        if (closing)
        {
            closeAge += dt;
            t = 1f - Mathf.Clamp01(closeAge / 0.3f);
            group.alpha = t;
            card.localScale = Vector3.one * Mathf.Lerp(0.9f, 1f, t);
            if (closeAge >= 0.3f)
            {
                Action done = onClosed;
                onClosed = null;
                Destroy(gameObject);
                done?.Invoke();
            }
            return;
        }

        group.alpha = Mathf.Clamp01(age / 0.25f);
        float pop = HudTheme.EaseOutBack(Mathf.Clamp01(age / 0.45f));
        card.localScale = Vector3.one * Mathf.Lerp(0.82f, 1f, pop);

        AnimateTitle();

        for (int i = 0; i < rows.Count; i++)
        {
            float k = Mathf.Clamp01((age - 1.0f - i * 0.22f) / 0.4f);
            rowGroups[i].alpha = k;
            Vector2 p = rows[i].anchoredPosition;
            rows[i].anchoredPosition = new Vector2(Mathf.Lerp(160f, 0f, HudTheme.EaseOutCubic(k)), p.y);
        }

        // A calm fade-in and a barely-there breathing; no bouncing.
        float buttonIn = Mathf.Clamp01((age - 1.2f - rows.Count * 0.22f) / 0.35f);
        float breathe = 1f + 0.012f * Mathf.Sin(age * 2.4f) * buttonIn;
        button.localScale = Vector3.one * Mathf.Lerp(0.96f, 1f, HudTheme.EaseOutCubic(buttonIn)) * breathe;
        buttonGroup.alpha = buttonIn;

        for (int i = 0; i < stars.Count; i++)
        {
            float tw = 0.5f + 0.5f * Mathf.Sin(age * (2.2f + i * 0.37f) + i * 1.7f);
            float appear = Mathf.Clamp01((age - 0.6f - i * 0.08f) / 0.3f);
            stars[i].localScale = Vector3.one * (0.6f + 0.5f * tw) * appear;
            stars[i].localRotation = Quaternion.Euler(0f, 0f, age * (i % 2 == 0 ? 40f : -55f));
            starImages[i].color = new Color(HudTheme.Exp.r, HudTheme.Exp.g, HudTheme.Exp.b, 0.45f + 0.55f * tw);
        }

        if (portraitRect != null)
        {
            // Glides in once, then stays calm: only a slow, tiny breathing bob.
            float slide = HudTheme.EaseOutCubic(Mathf.Clamp01((age - 0.4f) / 0.6f));
            float bottom = -card.sizeDelta.y * 0.5f + 104f;
            portraitRect.anchoredPosition = new Vector2(-StageWidth * 0.5f + 290f + Mathf.Lerp(-200f, 0f, slide),
                bottom + Mathf.Sin(age * 1.1f) * 1.5f);
            portraitRect.localRotation = Quaternion.identity;
        }
    }

    /// <summary>Each letter of the title pops in after the previous one.</summary>
    private void AnimateTitle()
    {
        title.ForceMeshUpdate();
        TMP_TextInfo info = title.textInfo;
        for (int i = 0; i < info.characterCount; i++)
        {
            TMP_CharacterInfo ch = info.characterInfo[i];
            if (!ch.isVisible) continue;
            float k = Mathf.Clamp01((age - 0.35f - i * 0.055f) / 0.38f);
            float s = HudTheme.EaseOutBack(k);
            float wave = k >= 1f ? Mathf.Sin(age * 3f - i * 0.5f) * 4f : 0f;
            Vector3[] v = info.meshInfo[ch.materialReferenceIndex].vertices;
            int vi = ch.vertexIndex;
            Vector3 center = (v[vi] + v[vi + 2]) * 0.5f;
            for (int j = 0; j < 4; j++)
                v[vi + j] = center + (v[vi + j] - center) * s + new Vector3(0f, wave + (1f - k) * 30f, 0f);
        }
        title.UpdateVertexData(TMP_VertexDataUpdateFlags.Vertices);
    }
}
