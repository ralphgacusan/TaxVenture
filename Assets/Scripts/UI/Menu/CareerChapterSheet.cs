using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The chapter sheet: tapping a pinned paper lifts it off the cork and it
/// flies toward the player as a large clipped sheet (like the printed
/// documents in Office Level 1) with the chapter's story, status stamp, best
/// result bars, pass line and a 3D START / REPLAY button. Tapping outside,
/// CLOSE or Back puts it back on its pin.
/// </summary>
public sealed class CareerChapterSheet : MonoBehaviour
{
    public const float Width = 640f;
    public const float Height = 500f;
    private const float OpenDuration = 0.42f;
    private const float CloseDuration = 0.24f;
    private const float FrontDepth = -300f;
    private const float BarDuration = 0.8f;

    private readonly List<Material> materials = new List<Material>();
    private Transform sheet;
    private Image backdrop;
    private Material paperMaterial;
    private TextMeshPro chapterLabel, titleLabel, descriptionLabel, attemptsLabel, expValue, heartsValue;
    private TextMeshPro passLine, timeLabel, emptyLabel, sideQuestLabel, stampLabel;
    private Transform stamp;
    private readonly List<Material> stampMaterials = new List<Material>();
    private Transform resultRows;
    private RectTransform expFill, heartsFill, expPass, heartsPass;
    private SpriteRenderer sideQuestMedal;
    private CareerBoardButton startButton;
    private CareerBoardButton closeButton;

    private Vector3 fromPosition;
    private float fromTilt;
    private float fromScale = 0.4f;
    private float elapsed;
    private bool opening;
    private bool closing;
    private float expTarget, heartsTarget;
    private float barElapsed = -1f;

    public CareerChapterView Chapter { get; private set; }
    public bool IsOpen => sheet != null && sheet.gameObject.activeSelf && !closing;
    public bool IsAnimating => opening || closing;

    public event Action<CareerChapterView> StartRequested;
    public event Action Closed;

    public static CareerChapterSheet Create(Transform boardRoot)
    {
        Transform root = CareerBoardKit.Node("Chapter Sheet", boardRoot, Vector3.zero);
        var view = root.gameObject.AddComponent<CareerChapterSheet>();
        view.Build();
        return view;
    }

    private void Build()
    {
        BuildBackdrop();
        sheet = CareerBoardKit.Node("Sheet", transform, new Vector3(0f, 0f, FrontDepth));

        CareerBoardKit.Box(sheet, "Shadow", new Vector3(10f, -14f, 8f), new Vector3(Width, Height, 2f),
            new Color32(20, 22, 28, 255), materials);
        paperMaterial = CareerBoardKit.Lit(CareerBoardKit.PaperWhite, materials, 0.06f);
        CareerBoardKit.Box(sheet, "Paper", Vector3.zero, new Vector3(Width, Height, 6f), paperMaterial);
        // Faint ruled lines, like the Level 1 notes paper.
        Material rule = CareerBoardKit.Lit(new Color32(214, 222, 232, 255), materials, 0.05f);
        for (int i = 0; i < 7; i++)
            CareerBoardKit.Box(sheet, "Rule " + i, new Vector3(0f, 120f - i * 44f, -3.2f), new Vector3(Width - 60f, 1.4f, 0.4f), rule);
        Material brass = CareerBoardKit.Lit(HudTheme.Brass, materials, 0.6f);
        CareerBoardKit.Box(sheet, "Clip", new Vector3(0f, Height * 0.5f - 4f, -6f), new Vector3(130f, 36f, 10f), brass);
        CareerBoardKit.Box(sheet, "Clip lever", new Vector3(0f, Height * 0.5f + 12f, -9f), new Vector3(70f, 14f, 6f), brass);

        var blocker = sheet.gameObject.AddComponent<CareerBoardTarget>();
        blocker.Interactable = false;
        blocker.AddHitBox(sheet, Vector3.zero, new Vector3(Width, Height, 12f));

        const float left = -Width * 0.5f + 34f;
        const float z = -3.6f;
        chapterLabel = Label("Chapter", 17f, new Vector3(left + 200f, 196f, z), 400f, 26f, CareerBoardKit.InkSoft,
            TextAlignmentOptions.MidlineLeft);
        chapterLabel.characterSpacing = 4f;
        titleLabel = Label("Title", 40f, new Vector3(left + 200f, 158f, z), 400f, 50f, CareerBoardKit.Ink,
            TextAlignmentOptions.MidlineLeft);
        descriptionLabel = Label("Description", 20f, new Vector3(0f, 92f, z), Width - 68f, 64f, CareerBoardKit.InkSoft,
            TextAlignmentOptions.TopLeft, FontStyles.Normal);
        BuildStamp(new Vector3(Width * 0.5f - 120f, 166f, z));

        CareerBoardKit.Box(sheet, "Divider", new Vector3(0f, 50f, -3.4f), new Vector3(Width - 68f, 2f, 0.6f),
            new Color32(190, 182, 160, 255), materials);
        TextMeshPro bestHeading = Label("Best heading", 16f, new Vector3(left + 120f, 26f, z), 240f, 24f,
            CareerBoardKit.InkSoft, TextAlignmentOptions.MidlineLeft);
        bestHeading.text = "BEST RESULT";
        bestHeading.characterSpacing = 3f;
        attemptsLabel = Label("Attempts", 16f, new Vector3(Width * 0.5f - 34f - 120f, 26f, z), 240f, 24f,
            CareerBoardKit.InkSoft, TextAlignmentOptions.MidlineRight, FontStyles.Normal);

        resultRows = CareerBoardKit.Node("Result rows", sheet, Vector3.zero);
        BuildRow(resultRows, "EXP", HudTheme.Star, HudTheme.Exp, -16f, out expFill, out expPass, out expValue);
        BuildRow(resultRows, "Hearts", HudTheme.Heart, HudTheme.Reputation, -58f, out heartsFill, out heartsPass, out heartsValue);
        emptyLabel = Label("No result", 19f, new Vector3(0f, -37f, z), Width - 68f, 60f, CareerBoardKit.InkSoft,
            TextAlignmentOptions.Center, FontStyles.Normal);
        passLine = Label("Pass line", 16f, new Vector3(0f, -94f, z), Width - 68f, 24f, CareerBoardKit.InkSoft,
            TextAlignmentOptions.MidlineLeft, FontStyles.Normal);
        timeLabel = Label("Time", 16f, new Vector3(0f, -120f, z), Width - 68f, 24f, CareerBoardKit.Ink,
            TextAlignmentOptions.MidlineLeft);

        sideQuestMedal = CareerBoardKit.Sprite(sheet, "Side quest medal", null, new Vector3(left + 18f, -150f, z), 40f, Color.white, 5);
        sideQuestLabel = Label("Side quest", 16f, new Vector3(left + 44f + 270f, -150f, z), 540f, 28f, CareerBoardKit.InkSoft,
            TextAlignmentOptions.MidlineLeft, FontStyles.Normal);

        closeButton = CareerBoardButton.Create(sheet, "Close Button", "CLOSE", new Vector3(left + 95f, -206f, -6f),
            new Vector2(190f, 62f), HudTheme.Track, HudTheme.TextPrimary, 22f);
        closeButton.Tapped += _ => Close();
        startButton = CareerBoardButton.Create(sheet, "Start Button", "START", new Vector3(Width * 0.5f - 34f - 150f, -206f, -6f),
            new Vector2(300f, 70f), HudTheme.Complete, new Color32(18, 36, 28, 255), 28f);
        startButton.Tapped += _ => { if (Chapter != null && Chapter.Launchable) StartRequested?.Invoke(Chapter); };

        sheet.gameObject.SetActive(false);
        backdrop.transform.parent.gameObject.SetActive(false);
    }

    private void BuildBackdrop()
    {
        var go = new GameObject("Backdrop", typeof(RectTransform), typeof(Canvas));
        go.layer = gameObject.layer;
        go.transform.SetParent(transform, false);
        var canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 6;
        var rect = (RectTransform)go.transform;
        rect.sizeDelta = new Vector2(2600f, 1600f);
        rect.localPosition = new Vector3(0f, 0f, FrontDepth + 60f);
        RectTransform fill = HudTheme.CreateRect("Dim", rect);
        HudTheme.Stretch(fill);
        backdrop = fill.gameObject.AddComponent<Image>();
        backdrop.color = new Color(0.02f, 0.03f, 0.05f, 0f);
        backdrop.raycastTarget = false;
        CareerBoardKit.SetLayerRecursively(go.transform, gameObject.layer);
    }

    private TextMeshPro Label(string name, float size, Vector3 center, float width, float height, Color color,
        TextAlignmentOptions alignment, FontStyles style = FontStyles.Bold) =>
        CareerBoardKit.Text(sheet, name, "", size, center, width, height, color, alignment, style);

    private void BuildStamp(Vector3 center)
    {
        stamp = CareerBoardKit.Node("Stamp", sheet, center);
        stamp.localRotation = Quaternion.Euler(0f, 0f, -9f);
        const float w = 190f, h = 54f, t = 4.5f;
        Material ink = CareerBoardKit.Lit(Color.red, materials, 0.02f);
        stampMaterials.Add(ink);
        CareerBoardKit.Box(stamp, "Frame top", new Vector3(0f, h * 0.5f, 0f), new Vector3(w, t, 0.6f), ink);
        CareerBoardKit.Box(stamp, "Frame bottom", new Vector3(0f, -h * 0.5f, 0f), new Vector3(w, t, 0.6f), ink);
        CareerBoardKit.Box(stamp, "Frame left", new Vector3(-w * 0.5f, 0f, 0f), new Vector3(t, h + t, 0.6f), ink);
        CareerBoardKit.Box(stamp, "Frame right", new Vector3(w * 0.5f, 0f, 0f), new Vector3(t, h + t, 0.6f), ink);
        stampLabel = CareerBoardKit.Text(stamp, "Stamp text", "", 28f, new Vector3(0f, 0f, -0.6f), w - 16f, h - 10f, Color.red);
        stampLabel.characterSpacing = 4f;
    }

    private void BuildRow(Transform parent, string name, Sprite icon, Color color, float y,
        out RectTransform fill, out RectTransform passMark, out TextMeshPro value)
    {
        const float left = -Width * 0.5f + 34f;
        CareerBoardKit.Sprite(parent, name + " icon", icon, new Vector3(left + 14f, y, -3.6f), 28f, color, 5);
        fill = CareerBoardKit.Bar(parent, name + " bar", new Vector3(left + 40f + 200f, y, -3.8f), 400f, 20f, color, out passMark);
        value = CareerBoardKit.Text(parent, name + " value", "", 20f, new Vector3(left + 450f + 70f, y, -3.6f), 140f, 28f,
            CareerBoardKit.Ink, TextAlignmentOptions.MidlineLeft);
    }

    // =========================================================
    // OPEN / CLOSE
    // =========================================================

    /// <summary>Shows the chapter, flying out from a paper at fromLocal (board space) with its tilt and size.</summary>
    public void Open(CareerChapterView chapter, CareerBadgeView badge, bool progressAvailable,
        Vector3 fromLocal, float tilt, float paperWidth)
    {
        Chapter = chapter;
        Fill(chapter, badge, progressAvailable);
        fromPosition = fromLocal;
        fromTilt = tilt;
        fromScale = Mathf.Clamp(paperWidth / Width, 0.2f, 1f);
        elapsed = 0f;
        opening = true;
        closing = false;
        sheet.gameObject.SetActive(true);
        backdrop.transform.parent.gameObject.SetActive(true);
        KitSetFill(expFill, 0f);
        KitSetFill(heartsFill, 0f);
        barElapsed = 0f;
        Apply(0f);
        if (AudioManager.Instance != null) AudioManager.Instance.PlayPaper2SFX();
    }

    public void Close()
    {
        if (!IsOpen) return;
        opening = false;
        closing = true;
        elapsed = 0f;
        if (AudioManager.Instance != null) AudioManager.Instance.PlayPaperSFX();
    }

    /// <summary>Skips the fly-in (editor captures).</summary>
    public void SnapOpen()
    {
        if (sheet == null || !sheet.gameObject.activeSelf) return;
        opening = closing = false;
        sheet.localPosition = new Vector3(0f, 0f, FrontDepth);
        sheet.localRotation = Quaternion.identity;
        sheet.localScale = Vector3.one;
        backdrop.color = new Color(0.02f, 0.03f, 0.05f, 0.55f);
        barElapsed = -1f;
        KitSetFill(expFill, expTarget);
        KitSetFill(heartsFill, heartsTarget);
    }

    /// <summary>Hides immediately (camera left the board).</summary>
    public void HideNow()
    {
        if (sheet == null || !sheet.gameObject.activeSelf) return;
        opening = closing = false;
        sheet.gameObject.SetActive(false);
        backdrop.transform.parent.gameObject.SetActive(false);
        Closed?.Invoke();
    }

    public void SetInputEnabled(bool enabled)
    {
        if (closeButton != null) closeButton.Interactable = enabled;
        if (startButton != null) startButton.Interactable = enabled && Chapter != null && Chapter.Launchable;
    }

    private void Fill(CareerChapterView chapter, CareerBadgeView badge, bool progressAvailable)
    {
        chapterLabel.text = $"CHAPTER {chapter.Def.number}";
        titleLabel.text = chapter.Title;
        descriptionLabel.text = progressAvailable
            ? chapter.Def.description ?? ""
            : "Career progress could not be read. Your saved results are safe, but chapters cannot start right now.";

        CareerChapterStatus status = chapter.Status;
        // A replay states what it can change (T03 run modes).
        if (progressAvailable && chapter.Launchable && CareerProgress.LaunchMode(chapter) == CampaignRunMode.MasteryReplay)
            descriptionLabel.text += "\n<size=72%>" + CampaignRun.WhatChanges(CampaignRunMode.MasteryReplay) + "</size>";
        stampLabel.text = status == CareerChapterStatus.Available ? "OPEN" : CareerProgress.StatusLabel(chapter);
        Color stampColor = StampColor(status);
        stampLabel.color = stampColor;
        foreach (Material material in stampMaterials) CareerBoardKit.SetColor(material, stampColor);

        bool hasResult = chapter.HasResult && progressAvailable;
        resultRows.gameObject.SetActive(hasResult);
        // T08: hearts mean Public Trust in a campaign, so the score-hearts row is not shown there.
        foreach (string part in new[] { "Hearts icon", "Hearts bar", "Hearts value" })
        {
            Transform row = resultRows.Find(part);
            if (row != null) row.gameObject.SetActive(!chapter.Campaign);
        }
        emptyLabel.gameObject.SetActive(!hasResult);
        attemptsLabel.text = hasResult ? $"{chapter.Attempts} attempt{(chapter.Attempts == 1 ? "" : "s")}" : "";
        if (hasResult)
        {
            expValue.text = $"{chapter.BestExp:N0} / {chapter.MaxExp:N0}";
            heartsValue.text = $"{chapter.BestHearts:N0} / {chapter.MaxHearts:N0}";
            expTarget = chapter.MaxExp > 0 ? chapter.BestExp / (float)chapter.MaxExp : 0f;
            heartsTarget = chapter.MaxHearts > 0 ? chapter.BestHearts / (float)chapter.MaxHearts : 0f;
            PlacePassMark(expPass, chapter.PassingPercent);
            PlacePassMark(heartsPass, chapter.PassingPercent);
        }
        emptyLabel.text = chapter.PendingIntegration ? CareerProgress.PendingIntegrationText
            : status == CareerChapterStatus.ComingSoon ? "This chapter is still being written. Check back soon!"
            : status == CareerChapterStatus.Locked ? CareerProgress.LockedText(chapter)
            : "No result yet. Finish the chapter to record your best run.";

        bool showPass = status != CareerChapterStatus.ComingSoon && chapter.TargetExp > 0;
        passLine.gameObject.SetActive(showPass);
        passLine.text = CareerProgress.MasteryLine(chapter);
        timeLabel.gameObject.SetActive(hasResult);
        timeLabel.text = $"Best time {CareerProgress.FormatTime(chapter.BestSeconds)}    Last run {CareerProgress.FormatTime(chapter.LastSeconds)}    Total {CareerProgress.FormatTime(chapter.TotalSeconds)}";

        bool hasBadge = badge != null && status != CareerChapterStatus.ComingSoon;
        sideQuestMedal.gameObject.SetActive(hasBadge);
        sideQuestLabel.gameObject.SetActive(hasBadge);
        if (hasBadge)
        {
            sideQuestMedal.sprite = HudBadgeArt.Get(badge.Emblem, (BadgeTier)badge.Tier, badge.Ribbon);
            float native = Mathf.Max(sideQuestMedal.sprite.bounds.size.x, sideQuestMedal.sprite.bounds.size.y);
            sideQuestMedal.transform.localScale = Vector3.one * (40f / native);
            string sideTime = badge.BestSeconds > 0 ? $"  ·  best {CareerProgress.FormatTime(badge.BestSeconds)}" : "";
            sideQuestLabel.text = $"Side quest {badge.Number}: {badge.Name}  ·  {CareerProgress.TierLabel(badge.Tier)}{sideTime}";
        }

        switch (status)
        {
            case CareerChapterStatus.Available:
                startButton.SetLabel("START");
                startButton.SetColors(HudTheme.Complete, new Color32(18, 36, 28, 255));
                break;
            case CareerChapterStatus.Finished:
            case CareerChapterStatus.Passed:
                startButton.SetLabel("REPLAY");
                startButton.SetColors(HudTheme.Brass, new Color32(40, 30, 14, 255));
                break;
            case CareerChapterStatus.Locked:
                startButton.SetLabel("LOCKED");
                startButton.SetColors(HudTheme.Track, HudTheme.TextPrimary);
                break;
            default:
                startButton.SetLabel(chapter.PendingIntegration ? "NOT YET" : "COMING SOON");
                startButton.SetColors(HudTheme.Track, HudTheme.TextPrimary);
                break;
        }
        if (!progressAvailable) startButton.SetLabel("UNAVAILABLE");
        startButton.Interactable = chapter.Launchable;
    }

    private static void PlacePassMark(RectTransform mark, int percent)
    {
        if (mark == null) return;
        float x = Mathf.Clamp01(percent / 100f);
        mark.anchorMin = mark.anchorMax = new Vector2(x, 0.5f);
        mark.anchoredPosition = Vector2.zero;
        mark.gameObject.SetActive(percent > 0 && percent < 100);
    }

    internal static Color StampColor(CareerChapterStatus status)
    {
        switch (status)
        {
            case CareerChapterStatus.Passed: return new Color32(46, 150, 84, 255);
            case CareerChapterStatus.Finished: return new Color32(214, 112, 40, 255);
            case CareerChapterStatus.Available: return new Color32(40, 110, 190, 255);
            default: return new Color32(110, 116, 126, 255);
        }
    }

    private static void KitSetFill(RectTransform fill, float value) => CareerBoardKit.SetFill(fill, value);

    private void Update() => Apply(Time.unscaledDeltaTime);

    private void Apply(float dt)
    {
        if (sheet == null || !sheet.gameObject.activeSelf) return;
        Vector3 front = new Vector3(0f, 0f, FrontDepth);
        float dim = 0.55f;

        if (opening)
        {
            elapsed += dt;
            float t = Mathf.Clamp01(elapsed / OpenDuration);
            float move = HudTheme.EaseOutCubic(t);
            float grow = HudTheme.EaseOutBack(t);
            sheet.localPosition = Vector3.LerpUnclamped(fromPosition, front, move);
            sheet.localRotation = Quaternion.Euler(Mathf.Sin(t * Mathf.PI) * -10f, 0f, Mathf.LerpUnclamped(fromTilt, 0f, grow));
            sheet.localScale = Vector3.one * Mathf.LerpUnclamped(fromScale, 1f, grow);
            backdrop.color = new Color(0.02f, 0.03f, 0.05f, dim * move);
            if (t >= 1f) opening = false;
        }
        else if (closing)
        {
            elapsed += dt;
            float t = Mathf.Clamp01(elapsed / CloseDuration);
            float back = t * t;
            sheet.localPosition = Vector3.Lerp(front, fromPosition, back);
            sheet.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(0f, fromTilt, back));
            sheet.localScale = Vector3.one * Mathf.Lerp(1f, fromScale, back);
            backdrop.color = new Color(0.02f, 0.03f, 0.05f, dim * (1f - t));
            if (t >= 1f)
            {
                closing = false;
                sheet.gameObject.SetActive(false);
                backdrop.transform.parent.gameObject.SetActive(false);
                Closed?.Invoke();
                return;
            }
        }
        else
        {
            // Held in the hands: a slow drift instead of a dead-still panel.
            float time = Time.unscaledTime;
            sheet.localPosition = front + new Vector3(0f, Mathf.Sin(time * 1.3f) * 2.5f, 0f);
            sheet.localRotation = Quaternion.Euler(Mathf.Sin(time * 0.9f) * 0.8f, Mathf.Sin(time * 0.7f) * 1.2f,
                Mathf.Sin(time * 1.1f) * 0.4f);
        }

        if (barElapsed >= 0f)
        {
            barElapsed += dt;
            float b = Mathf.Clamp01((barElapsed - 0.25f) / BarDuration);
            float eased = HudTheme.EaseOutCubic(b);
            KitSetFill(expFill, expTarget * eased);
            KitSetFill(heartsFill, heartsTarget * eased);
            if (b >= 1f) barElapsed = -1f;
        }
    }

    private void OnDestroy()
    {
        foreach (Material material in materials) CareerBoardKit.Dispose(material);
    }
}
