using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// PURPOSE:
/// Everything the guide draws on screen except the welcome card:
/// - a dim layer with a spotlight hole and a pulsing frame around the
///   thing being explained (the hole glides from target to target);
/// - the speech box: the guide's portrait in a brass medallion, name
///   plate, lesson chip, typewriter text and a "tap to continue" /
///   "try it!" footer, plus a two-tap "Skip tutorial" button;
/// - a small hint bubble that never blocks play;
/// - the animated hand (GuideHand).
/// The view only draws; GuideDirector decides what to show and when.
/// </summary>
public sealed class GuideCoachView : MonoBehaviour
{
    public enum Placement { Auto, Top, Bottom, Center }

    private const float BoxWidth = 1360f;
    private const float BoxHeight = 300f;
    private const float Medallion = 250f;
    private const float TextLeft = 250f;
    private const float MedallionX = 92f;
    private const float MedallionY = 44f;
    private const float HintWidth = 980f;
    private const float HintHeight = 150f;
    private const float DimAlpha = 0.64f;

    private static readonly Color Paper = new Color32(251, 246, 232, 255);
    private static readonly Color Ink = new Color32(29, 43, 60, 255);
    private static readonly Color InkSoft = new Color32(29, 43, 60, 170);
    private static readonly Color DimColor = new Color(0.02f, 0.035f, 0.07f, 1f);

    public event Action Tapped;
    public event Action SkipRequested;

    private RectTransform root;
    private Color accent = new Color32(100, 215, 255, 255);

    // Spotlight.
    private RectTransform dimLayer;
    private readonly Image[] dims = new Image[4];
    private RectTransform frame;
    private Image frameImage;
    private RectTransform frameGlow;
    private Image frameGlowImage;
    private Rect holeCurrent;
    private Rect holeGoal;
    private bool holeVisible;
    private float holeAlpha;
    private float dimAlpha;
    private bool dimWanted;
    private Image catcher;

    // Speech box.
    private RectTransform box;
    private CanvasGroup boxGroup;
    private RectTransform medallion;
    private Image portrait;
    private TextMeshProUGUI nameLabel;
    private RectTransform chip;
    private TextMeshProUGUI chipLabel;
    private TextMeshProUGUI body;
    private TextMeshProUGUI footer;
    private RectTransform footerArrow;
    private TextMeshProUGUI skipLabel;
    private float skipArmedUntil = -1f;
    private bool boxVisible;
    private float boxAppear;
    private float boxY;
    private float boxYGoal;
    private bool footerIsAction;
    private float medallionBounce;

    // Hint bubble.
    private RectTransform hint;
    private CanvasGroup hintGroup;
    private Image hintPortrait;
    private TextMeshProUGUI hintText;
    private bool hintVisible;

    // Typewriter.
    private TextMeshProUGUI typing;
    private float revealed;
    private int totalChars;
    private float lettersPerSecond = 55f;

    public GuideHand Hand { get; private set; }

    public bool IsTyping => typing != null && revealed < totalChars;

    public static GuideCoachView Create(RectTransform parent)
    {
        RectTransform rect = HudTheme.CreateRect("Guide Coach", parent);
        HudTheme.Stretch(rect);
        var view = rect.gameObject.AddComponent<GuideCoachView>();
        view.root = rect;
        view.Build();
        return view;
    }

    public void Configure(Color accentColor, float letters)
    {
        accent = accentColor;
        lettersPerSecond = Mathf.Max(5f, letters);
        Hand.SetAccent(accent);
        frameImage.color = accent;
    }

    // =========================================================
    // BUILD
    // =========================================================

    private void Build()
    {
        dimLayer = HudTheme.CreateRect("Dim", root);
        HudTheme.Stretch(dimLayer);
        for (int i = 0; i < 4; i++)
        {
            RectTransform r = HudTheme.CreateRect("Dim " + i, dimLayer);
            r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
            r.pivot = Vector2.zero;
            dims[i] = HudTheme.AddImage(r, null, new Color(DimColor.r, DimColor.g, DimColor.b, 0f));
        }

        // Full-screen tap catcher: only raycasts while a page waits for a tap.
        RectTransform catchRect = HudTheme.CreateRect("Tap Catcher", root);
        HudTheme.Stretch(catchRect);
        catcher = catchRect.gameObject.AddComponent<Image>();
        catcher.color = new Color(0f, 0f, 0f, 0f);
        catcher.raycastTarget = false;
        catchRect.gameObject.AddComponent<GuideTapCatcher>().Tapped = () => Tapped?.Invoke();

        frameGlow = HudTheme.CreateRect("Spotlight Glow", root);
        frameGlow.anchorMin = frameGlow.anchorMax = new Vector2(0.5f, 0.5f);
        frameGlowImage = HudTheme.AddImage(frameGlow, GuideArt.Glow, Color.clear);

        frame = HudTheme.CreateRect("Spotlight Frame", root);
        frame.anchorMin = frame.anchorMax = new Vector2(0.5f, 0.5f);
        frameImage = HudTheme.AddImage(frame, GuideArt.Frame, accent);
        frameImage.type = Image.Type.Sliced;
        frameImage.pixelsPerUnitMultiplier = 1.4f;

        Hand = GuideHand.Create(root);

        BuildBox();
        BuildHint();
        SetHole(false, default, true);
    }

    private void BuildBox()
    {
        box = HudTheme.CreateRect("Speech Box", root);
        box.anchorMin = box.anchorMax = new Vector2(0.5f, 0.5f);
        box.sizeDelta = new Vector2(BoxWidth, BoxHeight);
        boxGroup = box.gameObject.AddComponent<CanvasGroup>();
        boxGroup.alpha = 0f;
        boxGroup.blocksRaycasts = false;

        // Tapping the box itself also continues (or finishes the typing).
        Image boxHit = box.gameObject.AddComponent<Image>();
        boxHit.color = Color.clear;
        box.gameObject.AddComponent<GuideTapCatcher>().Tapped = () => Tapped?.Invoke();

        RectTransform shadow = HudTheme.CreateRect("Shadow", box);
        HudTheme.Stretch(shadow);
        shadow.offsetMin += new Vector2(6f, -10f);
        shadow.offsetMax += new Vector2(6f, -10f);
        HudTheme.AddImage(shadow, HudTheme.Rounded, new Color(0f, 0.02f, 0.05f, 0.45f), 22f);

        RectTransform rim = HudTheme.CreateRect("Rim", box);
        HudTheme.Stretch(rim);
        HudTheme.AddImage(rim, HudTheme.Rounded, HudTheme.Brass, 22f);

        RectTransform paper = HudTheme.CreateRect("Paper", rim);
        HudTheme.Stretch(paper, 5f);
        HudTheme.AddImage(paper, HudTheme.Rounded, Paper, 18f);

        // Thin navy inner line like a printed card.
        RectTransform inner = HudTheme.CreateRect("Inner Line", paper);
        HudTheme.Stretch(inner, 9f);
        Image innerImage = HudTheme.AddImage(inner, GuideArt.Frame, new Color(Ink.r, Ink.g, Ink.b, 0.12f));
        innerImage.type = Image.Type.Sliced;
        innerImage.pixelsPerUnitMultiplier = 3f;

        // Medallion with the portrait, overhanging the left edge.
        medallion = HudTheme.CreateRect("Medallion", box);
        medallion.anchorMin = medallion.anchorMax = new Vector2(0f, 0.5f);
        medallion.sizeDelta = new Vector2(Medallion, Medallion);
        medallion.anchoredPosition = new Vector2(MedallionX, MedallionY);
        HudTheme.AddImage(medallion, HudTheme.Circle, new Color(0f, 0.02f, 0.05f, 0.4f));
        RectTransform medRim = HudTheme.CreateRect("Rim", medallion);
        HudTheme.Stretch(medRim, 3f);
        HudTheme.AddImage(medRim, HudTheme.Circle, HudTheme.Brass);
        RectTransform medFill = HudTheme.CreateRect("Fill", medallion);
        HudTheme.Stretch(medFill, 12f);
        HudTheme.AddImage(medFill, HudTheme.Circle, new Color32(36, 76, 116, 255));
        RectTransform mask = HudTheme.CreateRect("Mask", medallion);
        HudTheme.Stretch(mask, 14f);
        Image maskImage = HudTheme.AddImage(mask, HudTheme.Circle, Color.white);
        mask.gameObject.AddComponent<Mask>().showMaskGraphic = false;
        RectTransform portraitRect = HudTheme.CreateRect("Portrait", mask);
        portraitRect.anchorMin = portraitRect.anchorMax = new Vector2(0.5f, 0.5f);
        portraitRect.sizeDelta = new Vector2(340f, 236f);
        portraitRect.anchoredPosition = new Vector2(0f, -18f);
        portrait = HudTheme.AddImage(portraitRect, null, Color.white);
        portrait.preserveAspect = true;
        maskImage.raycastTarget = false;

        // Name plate under the medallion.
        RectTransform plate = HudTheme.CreateRect("Name Plate", box);
        plate.anchorMin = plate.anchorMax = new Vector2(0f, 0.5f);
        plate.sizeDelta = new Vector2(244f, 56f);
        plate.anchoredPosition = new Vector2(MedallionX, -100f);
        HudTheme.AddImage(plate, HudTheme.Rounded, new Color32(22, 34, 50, 255), 12f);
        RectTransform plateRim = HudTheme.CreateRect("Rim", plate);
        HudTheme.Stretch(plateRim, 3f);
        HudTheme.AddImage(plateRim, HudTheme.Rounded, HudTheme.Brass, 10f);
        nameLabel = HudTheme.CreateLabel("Name", plateRim, "ARTHUR", 30f, new Color32(29, 32, 40, 255),
            TextAlignmentOptions.Center, FontStyles.Bold | FontStyles.UpperCase);
        HudTheme.Stretch((RectTransform)nameLabel.transform, 4f);
        nameLabel.characterSpacing = 6f;

        // Lesson chip on the top edge.
        chip = HudTheme.CreateRect("Chip", box);
        chip.anchorMin = chip.anchorMax = new Vector2(0f, 1f);
        chip.pivot = new Vector2(0f, 0.5f);
        chip.anchoredPosition = new Vector2(TextLeft, 0f);
        chip.sizeDelta = new Vector2(300f, 50f);
        HudTheme.AddImage(chip, HudTheme.Rounded, new Color32(22, 34, 50, 255), 25f);
        chipLabel = HudTheme.CreateLabel("Label", chip, "", 25f, accent, TextAlignmentOptions.Center,
            FontStyles.Bold | FontStyles.UpperCase);
        HudTheme.Stretch((RectTransform)chipLabel.transform, 6f);
        chipLabel.characterSpacing = 8f;

        body = HudTheme.CreateLabel("Text", box, "", 40f, Ink, TextAlignmentOptions.TopLeft);
        RectTransform bodyRect = (RectTransform)body.transform;
        HudTheme.Stretch(bodyRect);
        bodyRect.offsetMin = new Vector2(TextLeft, 62f);
        bodyRect.offsetMax = new Vector2(-44f, -42f);
        body.textWrappingMode = TextWrappingModes.Normal;
        body.enableAutoSizing = true;
        body.fontSizeMin = 28f;
        body.fontSizeMax = 40f;
        body.lineSpacing = 4f;
        body.overflowMode = TextOverflowModes.Overflow;

        footer = HudTheme.CreateLabel("Footer", box, "TAP TO CONTINUE", 24f, InkSoft,
            TextAlignmentOptions.MidlineRight, FontStyles.Bold | FontStyles.UpperCase);
        RectTransform footerRect = (RectTransform)footer.transform;
        footerRect.anchorMin = footerRect.anchorMax = new Vector2(1f, 0f);
        footerRect.pivot = new Vector2(1f, 0f);
        footerRect.anchoredPosition = new Vector2(-80f, 18f);
        footerRect.sizeDelta = new Vector2(480f, 40f);
        footer.characterSpacing = 6f;

        footerArrow = HudTheme.CreateRect("Next Arrow", box);
        footerArrow.anchorMin = footerArrow.anchorMax = new Vector2(1f, 0f);
        footerArrow.sizeDelta = new Vector2(32f, 32f);
        footerArrow.anchoredPosition = new Vector2(-48f, 38f);
        footerArrow.localRotation = Quaternion.Euler(0f, 0f, 180f);
        HudTheme.AddImage(footerArrow, GuideArt.Arrow, HudTheme.Brass);

        // Two-tap skip.
        RectTransform skip = HudTheme.CreateRect("Skip", box);
        skip.anchorMin = skip.anchorMax = new Vector2(1f, 1f);
        skip.pivot = new Vector2(1f, 0f);
        skip.anchoredPosition = new Vector2(-18f, 6f);
        skip.sizeDelta = new Vector2(300f, 50f);
        Image skipBg = HudTheme.AddImage(skip, HudTheme.Rounded, new Color32(22, 34, 50, 220), 25f);
        skipBg.raycastTarget = true;
        Button skipButton = skip.gameObject.AddComponent<Button>();
        skipButton.targetGraphic = skipBg;
        skipButton.onClick.AddListener(OnSkipClicked);
        skipLabel = HudTheme.CreateLabel("Label", skip, "SKIP TUTORIAL", 21f, HudTheme.TextMuted,
            TextAlignmentOptions.Center, FontStyles.Bold | FontStyles.UpperCase);
        HudTheme.Stretch((RectTransform)skipLabel.transform, 6f);
        skipLabel.characterSpacing = 4f;
    }

    private void BuildHint()
    {
        hint = HudTheme.CreateRect("Hint Bubble", root);
        hint.anchorMin = hint.anchorMax = new Vector2(0.5f, 1f);
        hint.pivot = new Vector2(0.5f, 1f);
        hint.sizeDelta = new Vector2(HintWidth, HintHeight);
        hint.anchoredPosition = new Vector2(0f, -190f);
        hintGroup = hint.gameObject.AddComponent<CanvasGroup>();
        hintGroup.alpha = 0f;
        hintGroup.blocksRaycasts = false;
        hintGroup.interactable = false;

        RectTransform rim = HudTheme.CreateRect("Rim", hint);
        HudTheme.Stretch(rim);
        HudTheme.AddImage(rim, HudTheme.Rounded, HudTheme.Brass, 20f);
        RectTransform paper = HudTheme.CreateRect("Paper", rim);
        HudTheme.Stretch(paper, 4f);
        HudTheme.AddImage(paper, HudTheme.Rounded, Paper, 17f);

        RectTransform med = HudTheme.CreateRect("Medallion", hint);
        med.anchorMin = med.anchorMax = new Vector2(0f, 0.5f);
        med.sizeDelta = new Vector2(124f, 124f);
        med.anchoredPosition = new Vector2(78f, 0f);
        HudTheme.AddImage(med, HudTheme.Circle, HudTheme.Brass);
        RectTransform fill = HudTheme.CreateRect("Fill", med);
        HudTheme.Stretch(fill, 5f);
        HudTheme.AddImage(fill, HudTheme.Circle, new Color32(36, 76, 116, 255));
        RectTransform mask = HudTheme.CreateRect("Mask", med);
        HudTheme.Stretch(mask, 6f);
        HudTheme.AddImage(mask, HudTheme.Circle, Color.white);
        mask.gameObject.AddComponent<Mask>().showMaskGraphic = false;
        RectTransform pr = HudTheme.CreateRect("Portrait", mask);
        pr.anchorMin = pr.anchorMax = new Vector2(0.5f, 0.5f);
        pr.sizeDelta = new Vector2(170f, 118f);
        pr.anchoredPosition = new Vector2(0f, -10f);
        hintPortrait = HudTheme.AddImage(pr, null, Color.white);
        hintPortrait.preserveAspect = true;

        hintText = HudTheme.CreateLabel("Text", hint, "", 33f, Ink, TextAlignmentOptions.MidlineLeft);
        RectTransform tr = (RectTransform)hintText.transform;
        HudTheme.Stretch(tr);
        tr.offsetMin = new Vector2(158f, 12f);
        tr.offsetMax = new Vector2(-24f, -12f);
        hintText.textWrappingMode = TextWrappingModes.Normal;
        hintText.fontSizeMin = 24f;
    }

    // =========================================================
    // API
    // =========================================================

    public void SetName(string guideName) => nameLabel.text = string.IsNullOrEmpty(guideName) ? "GUIDE" : guideName;

    /// <summary>Shows or hides the two-tap SKIP TUTORIAL button (Training Hub practice hides it).</summary>
    public void SetSkipVisible(bool visible)
    {
        if (skipLabel != null) skipLabel.transform.parent.gameObject.SetActive(visible);
    }

    public void SetPortrait(Sprite sprite, bool bounce)
    {
        portrait.sprite = sprite;
        portrait.enabled = sprite != null;
        hintPortrait.sprite = sprite;
        hintPortrait.enabled = sprite != null;
        if (bounce) medallionBounce = 1f;
    }

    /// <summary>Shows the speech box with a page of text (typewriter).</summary>
    public void ShowBox(string chipText, string text, bool waitsForTap, Placement placement)
    {
        bool wasVisible = boxVisible;
        boxVisible = true;
        boxGroup.blocksRaycasts = true;
        boxGroup.interactable = true;

        chip.gameObject.SetActive(!string.IsNullOrEmpty(chipText));
        if (!string.IsNullOrEmpty(chipText))
        {
            chipLabel.text = chipText;
            chipLabel.color = accent;
            chip.sizeDelta = new Vector2(Mathf.Clamp(chipText.Length * 21f + 64f, 170f, 620f), 50f);
        }

        footerIsAction = !waitsForTap;
        footer.text = waitsForTap ? "TAP TO CONTINUE" : "YOUR TURN!";
        footerArrow.gameObject.SetActive(waitsForTap);

        StartTyping(body, text);

        boxYGoal = PlacementY(placement);
        if (!wasVisible)
        {
            boxY = boxYGoal + (boxYGoal < 0f ? -60f : 60f);
            boxAppear = 0f;
            medallionBounce = 1f;
        }
        ResetSkip();
    }

    public void HideBox()
    {
        boxVisible = false;
        boxGroup.blocksRaycasts = false;
        boxGroup.interactable = false;
        if (typing == body) typing = null;
    }

    public void ShowHint(string text, bool instant = false)
    {
        hintVisible = true;
        StartTyping(hintText, text);
        if (instant) CompleteTyping();
    }

    public void HideHint()
    {
        hintVisible = false;
        if (typing == hintText) typing = null;
    }

    public void CompleteTyping()
    {
        if (typing == null) return;
        revealed = totalChars;
        typing.maxVisibleCharacters = 99999;
    }

    /// <summary>True: the screen dims and every tap continues.</summary>
    public void SetBlocking(bool blocking)
    {
        dimWanted = blocking;
        catcher.raycastTarget = blocking;
    }

    /// <summary>Spotlight on a screen-pixel rect (origin bottom-left). visible=false hides the frame.</summary>
    public void SetHole(bool visible, Rect screenRect, bool instant = false)
    {
        holeVisible = visible;
        if (!visible)
        {
            // Close the hole to a point at the centre so the dim covers everything.
            holeGoal = new Rect(0f, 0f, 0f, 0f);
            if (instant) holeCurrent = holeGoal;
            return;
        }
        Rect local = ScreenToLocal(screenRect);
        const float pad = 14f;
        local.xMin -= pad; local.yMin -= pad; local.xMax += pad; local.yMax += pad;
        bool wasEmpty = holeCurrent.width < 1f;
        holeGoal = local;
        if (instant || wasEmpty) holeCurrent = local;
    }

    public Rect ScreenToLocal(Rect screen)
    {
        Vector2 a = ScreenToLocal(screen.min);
        Vector2 b = ScreenToLocal(screen.max);
        return Rect.MinMaxRect(Mathf.Min(a.x, b.x), Mathf.Min(a.y, b.y), Mathf.Max(a.x, b.x), Mathf.Max(a.y, b.y));
    }

    public Vector2 ScreenToLocal(Vector2 screen)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(root, screen, null, out Vector2 local);
        return local;
    }

    public Rect RootRect => root.rect;

    private float PlacementY(Placement placement)
    {
        Rect r = root.rect;
        float bottom = r.yMin + 24f + BoxHeight * 0.5f + 20f;
        float top = r.yMax - 190f - BoxHeight * 0.5f;
        switch (placement)
        {
            case Placement.Top: return top;
            case Placement.Center: return 0f;
            case Placement.Auto:
                // Opposite half from the spotlight, so the box never covers it.
                if (holeVisible && holeGoal.width > 0f) return holeGoal.center.y < 0f ? top : bottom;
                return bottom;
            default: return bottom;
        }
    }

    private void StartTyping(TextMeshProUGUI label, string text)
    {
        typing = label;
        label.text = text ?? string.Empty;
        label.maxVisibleCharacters = 0;
        label.ForceMeshUpdate();
        totalChars = label.textInfo.characterCount;
        revealed = 0f;
    }

    private void OnSkipClicked()
    {
        if (Time.unscaledTime <= skipArmedUntil)
        {
            ResetSkip();
            SkipRequested?.Invoke();
            return;
        }
        skipArmedUntil = Time.unscaledTime + 3f;
        skipLabel.text = "TAP AGAIN TO SKIP";
        skipLabel.color = HudTheme.Exp;
    }

    private void ResetSkip()
    {
        skipArmedUntil = -1f;
        skipLabel.text = "SKIP TUTORIAL";
        skipLabel.color = HudTheme.TextMuted;
    }

    // =========================================================
    // ANIMATION
    // =========================================================

    private void Update()
    {
        float dt = Time.unscaledDeltaTime;
        float now = Time.unscaledTime;

        // Typewriter.
        if (typing != null && revealed < totalChars)
        {
            revealed = Mathf.Min(totalChars, revealed + lettersPerSecond * dt);
            typing.maxVisibleCharacters = Mathf.FloorToInt(revealed);
            if (revealed >= totalChars) typing.maxVisibleCharacters = 99999;
        }
        if (skipArmedUntil > 0f && now > skipArmedUntil) ResetSkip();

        // Dim + spotlight.
        dimAlpha = Mathf.MoveTowards(dimAlpha, dimWanted ? DimAlpha : 0f, dt * 2.4f);
        holeAlpha = Mathf.MoveTowards(holeAlpha, holeVisible ? 1f : 0f, dt * 4f);
        float follow = 1f - Mathf.Exp(-10f * dt);
        holeCurrent = new Rect(
            Mathf.Lerp(holeCurrent.x, holeGoal.x, follow), Mathf.Lerp(holeCurrent.y, holeGoal.y, follow),
            Mathf.Lerp(holeCurrent.width, holeGoal.width, follow), Mathf.Lerp(holeCurrent.height, holeGoal.height, follow));
        LayoutDims();

        float pulse = 0.5f + 0.5f * Mathf.Sin(now * 4.2f);
        Vector2 grow = new Vector2(10f, 10f) * pulse;
        frame.anchoredPosition = holeCurrent.center;
        frame.sizeDelta = holeCurrent.size + grow;
        frameImage.color = new Color(accent.r, accent.g, accent.b, holeAlpha * (0.75f + 0.25f * pulse));
        frameGlow.anchoredPosition = holeCurrent.center;
        frameGlow.sizeDelta = holeCurrent.size * 1.35f + new Vector2(120f, 120f);
        frameGlowImage.color = new Color(accent.r, accent.g, accent.b, holeAlpha * 0.28f * (0.6f + 0.4f * pulse));
        bool frameOn = holeAlpha > 0.01f && holeCurrent.width > 2f;
        if (frame.gameObject.activeSelf != frameOn)
        {
            frame.gameObject.SetActive(frameOn);
            frameGlow.gameObject.SetActive(frameOn);
        }

        // Speech box.
        boxAppear = Mathf.MoveTowards(boxAppear, boxVisible ? 1f : 0f, dt * (boxVisible ? 3.2f : 5f));
        boxY = Mathf.Lerp(boxY, boxYGoal, 1f - Mathf.Exp(-9f * dt));
        float appear = boxVisible ? HudTheme.EaseOutBack(boxAppear) : boxAppear;
        boxGroup.alpha = Mathf.Clamp01(boxAppear * 1.4f);
        box.localScale = Vector3.one * Mathf.Lerp(0.9f, 1f, appear);
        float width = Mathf.Min(BoxWidth, root.rect.width - 80f);
        box.sizeDelta = new Vector2(width, BoxHeight);
        box.anchoredPosition = new Vector2(0f, boxY);

        medallionBounce = Mathf.MoveTowards(medallionBounce, 0f, dt * 2.2f);
        float bob = Mathf.Sin(now * 1.1f) * 1.5f;
        medallion.localScale = Vector3.one * (1f + 0.05f * Mathf.Sin(medallionBounce * Mathf.PI) * medallionBounce);
        medallion.anchoredPosition = new Vector2(MedallionX, MedallionY + bob);

        float blink = 0.55f + 0.45f * Mathf.Sin(now * 5f);
        if (footerIsAction)
        {
            footer.color = new Color(HudTheme.Complete.r * 0.75f, HudTheme.Complete.g * 0.75f, HudTheme.Complete.b * 0.75f, 0.7f + 0.3f * blink);
            footer.transform.localScale = Vector3.one * (1f + 0.04f * blink);
        }
        else
        {
            footer.color = InkSoft;
            footer.transform.localScale = Vector3.one;
            footerArrow.anchoredPosition = new Vector2(-48f, 38f - 5f * blink);
        }

        // Hint bubble.
        hintGroup.alpha = Mathf.MoveTowards(hintGroup.alpha, hintVisible ? 1f : 0f, dt * 4f);
        float hk = hintVisible ? HudTheme.EaseOutBack(hintGroup.alpha) : hintGroup.alpha;
        hint.localScale = Vector3.one * Mathf.Lerp(0.85f, 1f, hk);
    }

    private void LayoutDims()
    {
        Rect r = root.rect;
        Rect hole = holeCurrent;
        if (hole.width < 1f || hole.height < 1f) hole = new Rect(r.center, Vector2.zero);
        hole.xMin = Mathf.Clamp(hole.xMin, r.xMin, r.xMax);
        hole.xMax = Mathf.Clamp(hole.xMax, r.xMin, r.xMax);
        hole.yMin = Mathf.Clamp(hole.yMin, r.yMin, r.yMax);
        hole.yMax = Mathf.Clamp(hole.yMax, r.yMin, r.yMax);

        // Bottom, top (full width) and left, right (beside the hole).
        Place(0, new Rect(r.xMin, r.yMin, r.width, hole.yMin - r.yMin));
        Place(1, new Rect(r.xMin, hole.yMax, r.width, r.yMax - hole.yMax));
        Place(2, new Rect(r.xMin, hole.yMin, hole.xMin - r.xMin, hole.height));
        Place(3, new Rect(hole.xMax, hole.yMin, r.xMax - hole.xMax, hole.height));
        Color c = new Color(DimColor.r, DimColor.g, DimColor.b, dimAlpha);
        for (int i = 0; i < 4; i++) dims[i].color = c;
        bool on = dimAlpha > 0.001f;
        if (dimLayer.gameObject.activeSelf != on) dimLayer.gameObject.SetActive(on);
    }

    private void Place(int index, Rect rect)
    {
        RectTransform t = dims[index].rectTransform;
        t.anchoredPosition = rect.position;
        t.sizeDelta = new Vector2(Mathf.Max(0f, rect.width), Mathf.Max(0f, rect.height));
    }
}
