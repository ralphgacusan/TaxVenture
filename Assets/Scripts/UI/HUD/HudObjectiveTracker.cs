using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// PURPOSE:
/// Top-center objective tracker of the Office HUD — one step at a time.
/// Shows the current main objective, the clock, the step to do next (with a
/// live counter such as "2/4" when the step has one), a step counter and a
/// segmented bar (one segment per step, grouped per main objective).
///
/// DATA (read-only):
/// - OfficeObjectives: editable objectives + shared progress snapshot
///   (the Notes paper renders the same snapshot, so they always agree)
/// - CaseProgressionManager: case number, shown only for multi-case levels
/// - HudTimer: the existing gameplay clock (its own text is hidden)
///
/// UPDATES:
/// OfficeObjectives.Changed drives everything. When the step being shown is
/// completed, the row plays a check + strike-through, then slides to the
/// next step. Animations wait while the HUD is hidden (dialogue, desk
/// views, results) so the player sees them happen.
/// </summary>
public sealed class HudObjectiveTracker : MonoBehaviour
{
    public const float Width = 470f;
    private const float Height = 112f;
    private const float SegmentsX = 16f;
    private const float SegmentsWidth = 384f;
    private const float SegmentGap = 4f;
    private const float GroupGap = 12f;
    private const float SegmentHeight = 8f;

    private const float CompleteDuration = 0.45f;
    private const float SlideOutDuration = 0.25f;
    private const float SlideInDuration = 0.3f;
    private const float SlideDistance = 14f;

    private enum Phase { Idle, Completing, SlideOut, SlideIn }

    private OfficeHudRuntime host;
    private OfficeObjectives objectives;
    private HudTimer clockSource;
    private RectTransform frame;

    private TextMeshProUGUI captionText;
    private TextMeshProUGUI caseText;
    private RectTransform caseChip;
    private TextMeshProUGUI clockText;
    private TextMeshProUGUI objectiveText;
    private RectTransform objectiveRect;
    private Vector2 objectiveRest;
    private TextMeshProUGUI counterText;
    private Image iconRing;
    private Image iconCore;
    private Image iconCheck;
    private RectTransform iconRect;
    private RectTransform segmentRow;
    private Image[] segments = new Image[0];
    private int[] segmentTaskIndex = new int[0];

    private TaskProgressSnapshot shown;
    private int shownProgress = -1;
    private Phase phase = Phase.Idle;
    private float phaseTime;
    private bool pendingChange;
    private int shownClockSeconds = -1;
    private float fallbackClock;
    private readonly char[] clockBuffer = new char[10];
    private float appliedWidth = -1f;

    // =========================================================
    // BUILD
    // =========================================================

    public static HudObjectiveTracker Build(RectTransform parent, OfficeHudRuntime host,
        OfficeObjectives objectives, HudTimer clockSource)
    {
        RectTransform frame = HudTheme.CreateRect("Objective Tracker", parent);
        frame.anchorMin = frame.anchorMax = new Vector2(0.5f, 1f);
        frame.pivot = new Vector2(0.5f, 1f);
        frame.anchoredPosition = new Vector2(0f, -HudTheme.Margin);
        frame.sizeDelta = new Vector2(Width, Height);
        frame.localScale = new Vector3(HudTheme.HudScale, HudTheme.HudScale, 1f);

        RectTransform body = HudTheme.CreatePanel("Panel", frame, 12f);
        HudTheme.Stretch((RectTransform)body.parent);

        var tracker = frame.gameObject.AddComponent<HudObjectiveTracker>();
        tracker.host = host;
        tracker.objectives = objectives;
        tracker.clockSource = clockSource;
        tracker.frame = frame;
        tracker.BuildContents(body);
        return tracker;
    }

    private void BuildContents(RectTransform body)
    {
        // Header row: main objective title [case chip] ............ clock
        captionText = HudTheme.CreateLabel("Caption", body, "OBJECTIVE", HudTheme.CaptionSize - 1f,
            HudTheme.BrassSoft, TextAlignmentOptions.MidlineLeft, FontStyles.Bold);
        captionText.characterSpacing = 4f;
        HudTheme.PlaceTopLeft((RectTransform)captionText.transform, 16f, 10f, 300f, 24f);

        caseChip = HudTheme.PlaceTopLeft(HudTheme.CreateRect("Case Chip", body), 236f, 11f, 96f, 22f);
        HudTheme.AddImage(caseChip, HudTheme.Rounded, new Color(HudTheme.Brass.r, HudTheme.Brass.g, HudTheme.Brass.b, 0.22f), 11f);
        caseText = HudTheme.CreateLabel("Case", caseChip, "CASE 1 / 1", HudTheme.CaptionSize - 3f,
            HudTheme.BrassSoft, TextAlignmentOptions.Center, FontStyles.Bold);
        HudTheme.Stretch((RectTransform)caseText.transform, 2f);
        caseChip.gameObject.SetActive(false);

        clockText = HudTheme.CreateLabel("Clock", body, "00:00:00", HudTheme.CaptionSize,
            HudTheme.TextMuted, TextAlignmentOptions.MidlineRight);
        HudTheme.PlaceTopLeft((RectTransform)clockText.transform, Width - 16f - 4f - 120f, 10f, 120f, 24f);

        // Objective row: status icon + step text (up to two lines).
        iconRect = HudTheme.CreateRect("Status Icon", body);
        iconRect.anchorMin = iconRect.anchorMax = new Vector2(0f, 1f);
        iconRect.pivot = new Vector2(0.5f, 0.5f);
        iconRect.anchoredPosition = new Vector2(29f, -62f);
        iconRect.sizeDelta = new Vector2(26f, 26f);
        iconRing = HudTheme.AddImage(iconRect, HudTheme.Circle, HudTheme.Active);
        RectTransform core = HudTheme.CreateRect("Core", iconRect);
        HudTheme.Stretch(core, 4f);
        iconCore = HudTheme.AddImage(core, HudTheme.Circle, HudTheme.PanelInner);
        RectTransform check = HudTheme.CreateRect("Check", iconRect);
        HudTheme.Stretch(check, 3f);
        iconCheck = HudTheme.AddImage(check, HudTheme.Check, HudTheme.PanelInner);
        iconCheck.enabled = false;

        objectiveText = HudTheme.CreateLabel("Objective", body, string.Empty, HudTheme.ValueSize - 1f,
            HudTheme.TextPrimary, TextAlignmentOptions.MidlineLeft);
        objectiveText.textWrappingMode = TextWrappingModes.Normal;
        objectiveText.fontSizeMin = 13f;
        objectiveText.maxVisibleLines = 2;
        objectiveRect = HudTheme.PlaceTopLeft((RectTransform)objectiveText.transform, 52f, 40f, Width - 52f - 16f, 44f);
        objectiveRest = objectiveRect.anchoredPosition;

        // Progress row: one segment per step + counter.
        segmentRow = HudTheme.PlaceTopLeft(HudTheme.CreateRect("Steps", body), SegmentsX, 92f, SegmentsWidth, SegmentHeight);
        counterText = HudTheme.CreateLabel("Counter", body, "0 / 0", HudTheme.CaptionSize - 1f,
            HudTheme.TextPrimary, TextAlignmentOptions.MidlineRight, FontStyles.Bold);
        HudTheme.PlaceTopLeft((RectTransform)counterText.transform, SegmentsX + SegmentsWidth + 4f, 84f,
            Width - SegmentsX - SegmentsWidth - 4f - 16f, 24f);
    }

    private void BuildSegments()
    {
        for (int i = segmentRow.childCount - 1; i >= 0; i--)
            Destroy(segmentRow.GetChild(i).gameObject);

        IReadOnlyList<TaskDefinition> tasks = objectives.Tasks;
        var indices = new List<int>();
        int groupBreaks = 0;
        bool seenStep = false;
        for (int i = 0; i < tasks.Count; i++)
        {
            if (tasks[i] == null) continue;
            if (tasks[i].IsHeader) { if (seenStep) groupBreaks++; continue; }
            indices.Add(i);
            seenStep = true;
        }

        segmentTaskIndex = indices.ToArray();
        segments = new Image[segmentTaskIndex.Length];
        if (segments.Length == 0) return;

        float gaps = SegmentGap * (segments.Length - 1 - groupBreaks) + GroupGap * groupBreaks;
        float width = (SegmentsWidth - gaps) / segments.Length;
        float x = 0f;
        for (int s = 0; s < segments.Length; s++)
        {
            if (s > 0) x += IsGroupStart(tasks, segmentTaskIndex[s]) ? GroupGap : SegmentGap;
            RectTransform seg = HudTheme.PlaceTopLeft(HudTheme.CreateRect("Step " + (s + 1), segmentRow),
                x, 0f, width, SegmentHeight);
            segments[s] = HudTheme.AddImage(seg, HudTheme.Rounded, HudTheme.Track, SegmentHeight * 0.5f);
            x += width;
        }
    }

    private static bool IsGroupStart(IReadOnlyList<TaskDefinition> tasks, int index)
        => index > 0 && tasks[index - 1] != null && tasks[index - 1].IsHeader;

    // =========================================================
    // EVENTS
    // =========================================================

    private void Start()
    {
        if (objectives == null)
        {
            enabled = false;
            return;
        }

        BuildSegments();
        objectives.Changed += HandleChanged;
        ShowImmediately(objectives.Snapshot);
    }

    private void OnDestroy()
    {
        if (objectives != null) objectives.Changed -= HandleChanged;
    }

    private void HandleChanged()
    {
        RefreshCase();
        if (phase != Phase.Idle)
        {
            pendingChange = true; // picked up when the running animation finishes
            return;
        }

        TaskProgressSnapshot latest = objectives.Snapshot;
        if (shown == null)
        {
            ShowImmediately(latest);
            return;
        }

        ApplySegments(latest);
        ApplyCounter(latest);

        bool shownStepCompleted = shown.ActiveIndex >= 0 && latest.Status[shown.ActiveIndex] == TaskRowStatus.Done;
        if (shownStepCompleted)
        {
            BeginPhase(Phase.Completing);
        }
        else if (latest.ActiveIndex != shown.ActiveIndex || latest.AllDone != shown.AllDone)
        {
            BeginPhase(Phase.SlideOut);
        }
        else
        {
            shown = latest;
            // Same step: only a live counter (e.g. profiles 2/4) may have changed.
            if (CurrentProgress(shown) != shownProgress) ApplyObjective(shown);
        }
    }

    private void RefreshCase()
    {
        CaseProgressionManager manager = CaseProgressionManager.Instance;
        int total = manager != null ? manager.TotalCasesInLevel : 0;
        // Only worth the space when a level has more than one case.
        caseChip.gameObject.SetActive(total > 1);
        if (total > 1) caseText.SetText("CASE {0} / {1}", Mathf.Min(manager.CurrentCaseNumber, total), total);
    }

    // =========================================================
    // PRESENTATION
    // =========================================================

    private void ShowImmediately(TaskProgressSnapshot snapshot)
    {
        shown = snapshot;
        phase = Phase.Idle;
        RefreshCase();
        ApplyObjective(snapshot);
        ApplySegments(snapshot);
        ApplyCounter(snapshot);
        SetIcon(snapshot.AllDone, 0f);
        SetObjectiveOffset(0f, 1f);
    }

    private int CurrentProgress(TaskProgressSnapshot snapshot)
    {
        if (snapshot == null || snapshot.ActiveIndex < 0) return -1;
        return objectives.TryGetProgress(objectives.Tasks[snapshot.ActiveIndex], out int current, out _) ? current : -1;
    }

    private void ApplyObjective(TaskProgressSnapshot snapshot)
    {
        IReadOnlyList<TaskDefinition> tasks = objectives.Tasks;
        objectiveText.fontStyle = FontStyles.Normal;
        objectiveText.color = HudTheme.TextPrimary;
        shownProgress = CurrentProgress(snapshot);

        if (snapshot.AllDone)
        {
            captionText.text = "ALL OBJECTIVES";
            objectiveText.text = "All objectives complete";
            objectiveText.color = HudTheme.Complete;
            return;
        }

        captionText.text = snapshot.ActiveHeaderIndex >= 0
            ? tasks[snapshot.ActiveHeaderIndex].TaskDescription.ToUpperInvariant()
            : "OBJECTIVE";

        if (snapshot.ActiveIndex < 0)
        {
            objectiveText.text = string.Empty;
            return;
        }

        TaskDefinition step = tasks[snapshot.ActiveIndex];
        objectiveText.text = objectives.TryGetProgress(step, out int current, out int total)
            ? $"{step.TaskDescription}  ({current}/{total})"
            : step.TaskDescription;
    }

    private void ApplyCounter(TaskProgressSnapshot snapshot)
        => counterText.SetText("{0} / {1}", snapshot.DoneCount, snapshot.StepCount);

    private void ApplySegments(TaskProgressSnapshot snapshot)
    {
        for (int s = 0; s < segments.Length; s++)
        {
            int taskIndex = segmentTaskIndex[s];
            TaskRowStatus status = taskIndex < snapshot.Status.Length ? snapshot.Status[taskIndex] : TaskRowStatus.Pending;
            segments[s].color = status == TaskRowStatus.Done ? HudTheme.Complete
                : status == TaskRowStatus.Active ? HudTheme.Active
                : HudTheme.Track;
        }
    }

    /// <summary>done = false shows the active ring; true shows the filled green check.</summary>
    private void SetIcon(bool done, float pop)
    {
        iconRing.color = done ? HudTheme.Complete : HudTheme.Active;
        iconCore.enabled = !done;
        iconCheck.enabled = done;
        float scale = 1f + 0.3f * pop;
        iconRect.localScale = new Vector3(scale, scale, 1f);
    }

    private void SetObjectiveOffset(float x, float alpha)
    {
        objectiveRect.anchoredPosition = objectiveRest + new Vector2(x, 0f);
        objectiveText.alpha = alpha;
    }

    private void BeginPhase(Phase next)
    {
        phase = next;
        phaseTime = 0f;
        if (next == Phase.Completing)
        {
            objectiveText.fontStyle = FontStyles.Strikethrough;
            objectiveText.color = HudTheme.TextMuted;
            SetIcon(true, 0f);
        }
    }

    // =========================================================
    // UPDATE (clock + short animations + active pulse)
    // =========================================================

    private void Update()
    {
        TickClock();

        bool visible = host == null || host.IsShown;
        if (phase != Phase.Idle && visible)
        {
            phaseTime += Time.unscaledDeltaTime;
            switch (phase)
            {
                case Phase.Completing:
                    SetIcon(true, Mathf.Sin(Mathf.Clamp01(phaseTime / CompleteDuration) * Mathf.PI));
                    if (phaseTime >= CompleteDuration) BeginPhase(Phase.SlideOut);
                    break;

                case Phase.SlideOut:
                {
                    float t = HudTheme.EaseOutCubic(phaseTime / SlideOutDuration);
                    SetObjectiveOffset(-SlideDistance * t, 1f - t);
                    if (phaseTime >= SlideOutDuration)
                    {
                        shown = objectives.Snapshot;
                        pendingChange = false;
                        ApplyObjective(shown);
                        ApplySegments(shown);
                        ApplyCounter(shown);
                        SetIcon(shown.AllDone, 0f);
                        BeginPhase(Phase.SlideIn);
                        SetObjectiveOffset(SlideDistance, 0f);
                    }
                    break;
                }

                case Phase.SlideIn:
                {
                    float t = HudTheme.EaseOutCubic(phaseTime / SlideInDuration);
                    SetObjectiveOffset(SlideDistance * (1f - t), t);
                    if (phaseTime >= SlideInDuration)
                    {
                        SetObjectiveOffset(0f, 1f);
                        phase = Phase.Idle;
                        // Something changed mid-animation: catch up now.
                        if (pendingChange)
                        {
                            pendingChange = false;
                            HandleChanged();
                        }
                    }
                    break;
                }
            }
        }

        PulseActiveSegment();
    }

    private void TickClock()
    {
        float seconds;
        if (clockSource != null)
        {
            seconds = clockSource.ElapsedSeconds;
        }
        else
        {
            fallbackClock += Time.deltaTime;
            seconds = fallbackClock;
        }

        int whole = Mathf.FloorToInt(seconds);
        if (whole == shownClockSeconds) return;
        shownClockSeconds = whole;
        clockText.SetCharArray(clockBuffer, 0, HudTheme.FormatClock(clockBuffer, whole));
    }

    private void PulseActiveSegment()
    {
        if (shown == null || shown.ActiveIndex < 0) return;
        for (int s = 0; s < segments.Length; s++)
        {
            if (segmentTaskIndex[s] != shown.ActiveIndex) continue;
            float k = 0.65f + 0.35f * (0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 3.5f));
            segments[s].color = Color.Lerp(HudTheme.Track, HudTheme.Active, k);
            return;
        }
    }

    // =========================================================
    // LAYOUT
    // =========================================================

    /// <summary>
    /// Keeps the tracker between the progression card (left) and the
    /// settings/minimap cluster (right) on narrow screens.
    /// </summary>
    public void ApplyLayout(float safeWidth)
    {
        if (Mathf.Approximately(safeWidth, appliedWidth) || safeWidth <= 0f) return;
        appliedWidth = safeWidth;
        float scale = FitScale(safeWidth);
        frame.localScale = new Vector3(scale, scale, 1f);
    }

    /// <summary>Largest scale (up to HudScale) whose half-width clears the left card.</summary>
    public static float FitScale(float safeWidth)
    {
        float leftReserve = HudTheme.Margin + HudProgressionPanel.Width * HudTheme.HudScale + 16f;
        float halfAvailable = safeWidth * 0.5f - leftReserve;
        float fit = halfAvailable / (Width * 0.5f);
        return Mathf.Clamp(fit, 0.6f, HudTheme.HudScale);
    }
}
