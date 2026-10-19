using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Turns the supporting-documents cover into a loose, gatherable paper stack.
/// Spread out, the Findings slip (when present) gets its own larger column
/// on the left and the records sit in a neat grid beside it, all scaled as
/// large as the screen allows so every line stays readable.
/// </summary>
public sealed class Level2SupportingStack : MonoBehaviour
{
    private const float HoldSeconds = 3f;
    private const float HoldRingDelay = 0.35f;
    private const float HoldSlop = 22f;
    private const float GatherDuration = 0.28f;
    private const float ScatterDuration = 0.44f;
    private const float PaperGap = 24f;
    private const float FeatureScale = 1.16f;
    private const float MaxLayoutScale = 0.8f;
    private static readonly float[] Tilts = { -2.8f, 1.9f, -1.3f, 2.4f, -2.1f, 1.2f };

    private enum Mode { Idle, Unpacking, Gathering, Stacked, Scattering, Filing }

    private readonly List<Level2FindingsSheet> attachedPapers = new List<Level2FindingsSheet>();
    private readonly List<Level2FindingsSheet> loosePapers = new List<Level2FindingsSheet>();
    private readonly List<Vector2> fromPositions = new List<Vector2>();
    private readonly List<Vector2> targetPositions = new List<Vector2>();
    private readonly List<float> fromTilts = new List<float>();
    private RectTransform cover;
    private CaseFolderUI.PageBuilder page;
    private Mode mode;
    private bool stackOut;
    private bool coverDragging;
    private bool peelComplete;
    private bool ownerVisible = true;
    private bool scatterWhenOwnerShown;
    private Level2FindingsSheet pressedPaper;
    private PointerEventData pressData;
    private bool pressing;
    private float pressTime;
    private Vector2 pressStart;
    private Vector2 pointer;
    private float modeTime;
    private Image holdRing;
    private RectTransform holdRingLayer;

    public bool IsStackOut => stackOut;
    public bool IsAnimating => mode == Mode.Unpacking || mode == Mode.Gathering || mode == Mode.Scattering || mode == Mode.Filing;
    public bool IsGroupMoving => mode == Mode.Unpacking || mode == Mode.Gathering || mode == Mode.Stacked;

    public void BeginPageDraw()
    {
        attachedPapers.Clear();
        cover = null;
        page = null;
    }

    internal void RegisterPaper(Level2FindingsSheet paper)
    {
        if (paper != null && !paper.IsDetached && !attachedPapers.Contains(paper))
            attachedPapers.Add(paper);
    }

    internal void PaperDetached(Level2FindingsSheet paper)
    {
        attachedPapers.Remove(paper);
        if (paper != null && !loosePapers.Contains(paper)) loosePapers.Add(paper);
    }

    internal void PaperDisposed(Level2FindingsSheet paper)
    {
        attachedPapers.Remove(paper);
        loosePapers.Remove(paper);
        if (loosePapers.Count == 0 && mode != Mode.Filing)
        {
            stackOut = false;
            if (mode != Mode.Idle) mode = Mode.Idle;
        }
    }

    internal void OnOwnerVisibilityChanged(bool visible)
    {
        if (ownerVisible == visible) return;
        ownerVisible = visible;
        if (!visible)
        {
            bool interruptedGesture = pressing || coverDragging || pressedPaper != null ||
                mode == Mode.Unpacking || mode == Mode.Gathering || mode == Mode.Stacked ||
                mode == Mode.Scattering || mode == Mode.Filing;
            if (interruptedGesture && stackOut && loosePapers.Count > 0)
                scatterWhenOwnerShown = true;

            StopAllCoroutines();
            for (int i = 0; i < loosePapers.Count; i++)
                if (loosePapers[i] != null) loosePapers[i].CancelGroupFile();
            mode = Mode.Idle;
            pressing = false;
            pressData = null;
            pressedPaper = null;
            coverDragging = false;
            peelComplete = false;
            ShowHoldRing(false);
            return;
        }

        if (scatterWhenOwnerShown)
        {
            scatterWhenOwnerShown = false;
            if (stackOut && loosePapers.Count > 0) StartScatter(pointer);
        }
    }

    public void DrawCover(CaseFolderUI.PageBuilder ownerPage)
    {
        page = ownerPage;
        RectTransform paperRect = ownerPage.Paper;
        cover = HudTheme.CreateRect("Supporting Documents Stack Cover", paperRect);
        cover.sizeDelta = new Vector2(600f, 780f);
        cover.anchorMin = cover.anchorMax = cover.pivot = new Vector2(0.5f, 0.5f);
        cover.anchoredPosition = new Vector2(0f, -8f);
        float scale = Mathf.Min(paperRect.rect.width * 0.86f / 600f, paperRect.rect.height * 0.84f / 780f);
        cover.localScale = Vector3.one * Mathf.Max(0.01f, scale);
        Image background = NotesPaperView.BuildPaperStack(cover,
            new Color32(224, 214, 190, 255), new Color32(253, 247, 226, 255));
        background.raycastTarget = true;
        Shadow shadow = background.GetComponent<Shadow>();
        if (shadow != null)
        {
            shadow.effectColor = new Color(0.08f, 0.07f, 0.05f, 0.28f);
            shadow.effectDistance = new Vector2(8f, -10f);
        }
        RectTransform topRule = HudTheme.PlaceTopLeft(HudTheme.CreateRect("Cover header rule", cover), 54f, 126f, 492f, 2f);
        HudTheme.AddImage(topRule, null, new Color32(41, 53, 67, 255)).raycastTarget = false;
        TextMeshProUGUI title = HudTheme.CreateLabel("Supporting Documents title", cover,
            "Supporting Documents", 39f, new Color32(41, 53, 67, 255), TextAlignmentOptions.Center, FontStyles.Bold);
        HudTheme.PlaceTopLeft(title.rectTransform, 48f, 42f, 504f, 72f);
        TextMeshProUGUI prompt = HudTheme.CreateLabel("Supporting Documents lift prompt", cover,
            "Lift the stack to review the papers inside.", 24f,
            new Color32(110, 112, 106, 255), TextAlignmentOptions.Center);
        prompt.textWrappingMode = TMPro.TextWrappingModes.Normal;
        HudTheme.PlaceTopLeft(prompt.rectTransform, 64f, 150f, 472f, 76f);
        cover.gameObject.AddComponent<Level2SupportingStackCover>().Owner = this;
        ownerPage.AddOverlay(cover.gameObject);
    }

    public void LiftStack(Vector2 screenPosition)
    {
        if (stackOut || attachedPapers.Count == 0) return;
        BeginUnpack(screenPosition, false);
    }

    internal void LiftAdditionalPapers()
    {
        if (!stackOut || attachedPapers.Count == 0) return;
        Level2FindingsSheet[] papers = attachedPapers.FindAll(p => p != null && !p.IsDetached).ToArray();
        if (papers.Length == 0) return;
        Vector2 origin = loosePapers.Count > 0
            ? loosePapers[0].LooseToScreenPosition(loosePapers[0].LoosePosition)
            : new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
        attachedPapers.Clear();
        pointer = origin;
        for (int i = 0; i < papers.Length; i++) papers[i].LiftForStack(origin);
        StartCoroutine(ScatterAfterAdditionalPapers(origin));
    }

    private IEnumerator ScatterAfterAdditionalPapers(Vector2 origin)
    {
        yield return new WaitForSecondsRealtime(0.38f);
        if (loosePapers.Count == 0) yield break;
        PreparePaperScale();
        StartScatter(origin);
    }

    internal void BeginCoverDrag(Vector2 screenPosition)
    {
        if (stackOut)
        {
            coverDragging = mode == Mode.Unpacking || mode == Mode.Stacked;
            pointer = screenPosition;
            return;
        }
        if (attachedPapers.Count == 0) return;
        coverDragging = true;
        BeginUnpack(screenPosition, true);
    }

    internal void DragCover(Vector2 screenPosition)
    {
        pointer = screenPosition;
    }

    internal void EndCoverDrag(Vector2 screenPosition)
    {
        pointer = screenPosition;
        if (!coverDragging) return;
        coverDragging = false;
        if (peelComplete) StartScatter(pointer);
        RefreshFolderPage();
    }

    private void BeginUnpack(Vector2 screenPosition, bool keepStacked)
    {
        Level2FindingsSheet[] papers = attachedPapers.FindAll(p => p != null && !p.IsDetached).ToArray();
        if (papers.Length == 0) return;
        stackOut = true;
        pointer = screenPosition;
        mode = Mode.Unpacking;
        peelComplete = false;
        loosePapers.Clear();
        attachedPapers.Clear();
        for (int i = 0; i < papers.Length; i++) papers[i].LiftForStack(screenPosition);
        if (!keepStacked) RefreshFolderPage();
        StartCoroutine(AfterPeel(keepStacked, screenPosition));
        if (AudioManager.Instance != null) AudioManager.Instance.PlayPaperSFX();
    }

    private IEnumerator AfterPeel(bool keepStacked, Vector2 origin)
    {
        yield return new WaitForSecondsRealtime(0.38f);
        if (loosePapers.Count == 0) yield break;
        peelComplete = true;
        PreparePaperScale();
        if (keepStacked && coverDragging)
        {
            PlaceStack(pointer);
            mode = Mode.Unpacking;
        }
        else
        {
            StartScatter(keepStacked ? pointer : origin);
        }
    }

    private void RefreshFolderPage()
    {
        if (loosePapers.Count > 0) loosePapers[0].RefreshShowingFolders();
        else if (page != null) page.Refresh();
    }

    private void PreparePaperScale()
    {
        if (loosePapers.Count == 0) return;
        float scale = LayoutScale(loosePapers);
        for (int i = 0; i < loosePapers.Count; i++)
        {
            Level2FindingsSheet paper = loosePapers[i];
            if (paper != null) paper.SetLoosePose(paper.LoosePosition, scale * Feature(i), Tilts[i % Tilts.Length] * 0.3f);
        }
    }

    private void Update()
    {
        if (pressData != null && (pressing || mode == Mode.Gathering || mode == Mode.Stacked))
        {
            pointer = pressData.position;
            if (pressing && mode == Mode.Idle && (pointer - pressStart).sqrMagnitude > HoldSlop * HoldSlop)
            {
                pressing = false;
                ShowHoldRing(false);
            }
        }

        if (!pressing || mode != Mode.Idle) return;
        pressTime += Time.unscaledDeltaTime;
        if (pressTime >= HoldRingDelay)
        {
            ShowHoldRing(true);
            PlaceHoldRing(pointer, (pressTime - HoldRingDelay) / (HoldSeconds - HoldRingDelay));
        }
        if (pressTime >= HoldSeconds) BeginGather();
    }

    private void LateUpdate()
    {
        if (mode == Mode.Gathering)
        {
            modeTime += Time.unscaledDeltaTime;
            float t = HudTheme.EaseOutCubic(modeTime / GatherDuration);
            MoveBetweenPositions(t, true);
            if (modeTime >= GatherDuration) mode = Mode.Stacked;
        }
        else if (mode == Mode.Stacked || (mode == Mode.Unpacking && coverDragging))
        {
            PlaceStack(pointer);
        }
        else if (mode == Mode.Scattering)
        {
            modeTime += Time.unscaledDeltaTime;
            // A light toss: the papers glide out and settle with a small overshoot.
            MoveBetweenPositions(DeskMotion.EaseOutBack(Mathf.Clamp01(modeTime / ScatterDuration), 0.7f), false);
            if (modeTime >= ScatterDuration)
            {
                mode = Mode.Idle;
                pressing = false;
                pressData = null;
                ShowHoldRing(false);
            }
        }
    }

    internal void PaperPressed(Level2FindingsSheet paper, PointerEventData data)
    {
        if (!stackOut || mode != Mode.Idle || paper == null || data == null) return;
        pressedPaper = paper;
        pressData = data;
        pressing = true;
        pressTime = 0f;
        pressStart = pointer = data.position;
    }

    internal bool BeginPaperDrag(Level2FindingsSheet paper, PointerEventData data)
    {
        if (mode == Mode.Unpacking || mode == Mode.Gathering || mode == Mode.Stacked)
        {
            pointer = data.position;
            return true;
        }
        return false;
    }

    internal void PaperDragged(Level2FindingsSheet paper, Vector2 screenPosition)
    {
        if (paper != pressedPaper || mode != Mode.Idle) return;
        pointer = screenPosition;
        if (pressing && (pointer - pressStart).sqrMagnitude > HoldSlop * HoldSlop)
        {
            pressing = false;
            ShowHoldRing(false);
        }
    }

    internal void DragGroup(Level2FindingsSheet paper, Vector2 screenPosition)
    {
        pointer = screenPosition;
    }

    internal bool PaperReleased(Level2FindingsSheet paper, Vector2 screenPosition)
    {
        if (paper != pressedPaper && mode == Mode.Idle) return false;
        pointer = screenPosition;
        pressing = false;
        pressData = null;
        ShowHoldRing(false);

        if (mode == Mode.Unpacking)
        {
            coverDragging = false;
            if (peelComplete) StartScatter(screenPosition);
            RefreshFolderPage();
            return true;
        }
        if (mode != Mode.Gathering && mode != Mode.Stacked) return false;

        CaseFolderUI folder = FindFolder(screenPosition);
        if (folder != null) StartCoroutine(FileStack(folder));
        else StartScatter(screenPosition);
        return true;
    }

    private void BeginGather()
    {
        pressing = false;
        ShowHoldRing(false);
        CapturePositions();
        mode = Mode.Gathering;
        modeTime = 0f;
        for (int i = 0; i < loosePapers.Count; i++)
            if (loosePapers[i] != null && loosePapers[i] != pressedPaper) loosePapers[i].FocusLoosePaper();
        pressedPaper?.FocusLoosePaper();
        if (AudioManager.Instance != null) AudioManager.Instance.PlayPaperSFX();
    }

    private void StartScatter(Vector2 origin)
    {
        if (loosePapers.Count == 0)
        {
            mode = Mode.Idle;
            return;
        }
        if (!ownerVisible)
        {
            scatterWhenOwnerShown = true;
            mode = Mode.Idle;
            return;
        }
        pointer = origin;
        BuildScatterTargets(origin);
        CapturePositions();
        mode = Mode.Scattering;
        modeTime = 0f;
        for (int i = 0; i < loosePapers.Count; i++)
        {
            if (loosePapers[i] == null) continue;
            loosePapers[i].FocusLoosePaper();
            loosePapers[i].SetLoosePose(fromPositions[i], LayoutScale(loosePapers) * Feature(i), fromTilts[i]);
        }
        if (AudioManager.Instance != null) AudioManager.Instance.PlayPaperSFX();
    }

    private void BuildScatterTargets(Vector2 screenOrigin)
    {
        targetPositions.Clear();
        if (loosePapers.Count == 0) return;
        Vector2 center = loosePapers[0].ScreenToLoosePosition(screenOrigin);
        Rect bounds = loosePapers[0].LooseCanvasRect;
        Layout layout = Measure(loosePapers);
        float scale = LayoutScale(loosePapers);
        float width = layout.Width * scale;
        float height = layout.Height * scale;
        center.x = Mathf.Clamp(center.x, bounds.xMin + width * 0.5f + 24f, Mathf.Max(bounds.xMin + width * 0.5f + 24f, bounds.xMax - width * 0.5f - 24f));
        center.y = Mathf.Clamp(center.y, bounds.yMin + height * 0.5f + 36f, Mathf.Max(bounds.yMin + height * 0.5f + 36f, bounds.yMax - height * 0.5f - 36f));
        float left = center.x - width * 0.5f;
        float gridLeft = left + (layout.Featured >= 0 ? layout.FeatureSize.x + PaperGap * 2f : 0f) * scale;
        float gridTop = center.y + layout.GridHeight * scale * 0.5f;
        int cell = 0;
        for (int i = 0; i < loosePapers.Count; i++)
        {
            if (i == layout.Featured)
            {
                targetPositions.Add(new Vector2(left + layout.FeatureSize.x * scale * 0.5f, center.y));
                continue;
            }
            int column = cell % layout.Columns;
            int row = cell / layout.Columns;
            cell++;
            targetPositions.Add(new Vector2(
                gridLeft + (column * (layout.Cell.x + PaperGap) + layout.Cell.x * 0.5f) * scale,
                gridTop - (row * (layout.Cell.y + PaperGap) + layout.Cell.y * 0.5f) * scale));
        }
    }

    /// <summary>The spread in unscaled paper units: an optional featured Findings column plus a grid of the rest.</summary>
    private struct Layout
    {
        public int Featured;
        public Vector2 FeatureSize;
        public int Columns, Rows;
        public Vector2 Cell;
        public float GridWidth, GridHeight;
        public float Width, Height;
    }

    private static int FeaturedIndex(List<Level2FindingsSheet> papers)
    {
        if (papers == null || papers.Count < 3) return -1;
        for (int i = 0; i < papers.Count; i++)
            if (papers[i] != null && !papers[i].IsRecord) return i;
        return -1;
    }

    private float Feature(int index) => index == FeaturedIndex(loosePapers) ? FeatureScale : 1f;

    private static Layout Measure(List<Level2FindingsSheet> papers)
    {
        var layout = new Layout { Featured = FeaturedIndex(papers) };
        int gridCount = 0;
        for (int i = 0; i < papers.Count; i++)
        {
            Vector2 size = papers[i] != null ? papers[i].LooseSize : Vector2.zero;
            if (i == layout.Featured)
            {
                layout.FeatureSize = size * FeatureScale;
                continue;
            }
            gridCount++;
            layout.Cell = Vector2.Max(layout.Cell, size);
        }
        layout.Columns = Mathf.Max(1, gridCount <= 4 && layout.Featured >= 0 ? Mathf.Min(2, gridCount) : Mathf.Min(3, gridCount));
        layout.Rows = Mathf.Max(1, Mathf.CeilToInt(gridCount / (float)layout.Columns));
        layout.GridWidth = gridCount > 0 ? layout.Columns * layout.Cell.x + (layout.Columns - 1) * PaperGap : 0f;
        layout.GridHeight = gridCount > 0 ? layout.Rows * layout.Cell.y + (layout.Rows - 1) * PaperGap : 0f;
        layout.Width = layout.GridWidth + (layout.Featured >= 0 ? layout.FeatureSize.x + (gridCount > 0 ? PaperGap * 2f : 0f) : 0f);
        layout.Height = Mathf.Max(layout.GridHeight, layout.FeatureSize.y);
        return layout;
    }

    private void CapturePositions()
    {
        fromPositions.Clear();
        fromTilts.Clear();
        for (int i = 0; i < loosePapers.Count; i++)
        {
            Level2FindingsSheet paper = loosePapers[i];
            fromPositions.Add(paper != null ? paper.LoosePosition : Vector2.zero);
            fromTilts.Add(paper != null ? paper.LooseTilt : Tilts[i % Tilts.Length] * 0.3f);
        }
    }

    private void MoveBetweenPositions(float t, bool gathering)
    {
        if (loosePapers.Count == 0) return;
        Vector2 stackCenter = loosePapers[0].ScreenToLoosePosition(pointer);
        float scale = LayoutScale(loosePapers);
        int depth = 0;
        for (int i = 0; i < loosePapers.Count; i++)
        {
            Level2FindingsSheet paper = loosePapers[i];
            if (paper == null) continue;
            bool onTop = paper == pressedPaper;
            int stackDepth = onTop ? 0 : ++depth;
            Vector2 stackPosition = stackCenter + new Vector2(7f, -7f) * stackDepth;
            Vector2 target = gathering ? stackPosition :
                (targetPositions.Count > i ? targetPositions[i] : stackPosition);
            float tilt = gathering ? Tilts[i % Tilts.Length] * 0.3f : Tilts[i % Tilts.Length];
            paper.SetLoosePose(Vector2.LerpUnclamped(fromPositions[i], target, t), scale * Feature(i), tilt);
        }
    }

    private void PlaceStack(Vector2 screenPosition)
    {
        if (loosePapers.Count == 0) return;
        Vector2 center = loosePapers[0].ScreenToLoosePosition(screenPosition);
        float scale = LayoutScale(loosePapers);
        int depth = 0;
        for (int i = 0; i < loosePapers.Count; i++)
        {
            Level2FindingsSheet paper = loosePapers[i];
            if (paper == null) continue;
            bool onTop = paper == pressedPaper;
            int stackDepth = onTop ? 0 : ++depth;
            paper.SetLoosePose(center + new Vector2(7f, -7f) * stackDepth,
                scale * Feature(i), Tilts[i % Tilts.Length] * 0.3f);
        }
    }

    private float LayoutScale(List<Level2FindingsSheet> papers)
    {
        if (papers == null || papers.Count == 0) return 0.5f;
        Rect bounds = papers[0] != null ? papers[0].LooseCanvasRect : new Rect(0f, 0f, 1920f, 1080f);
        Layout layout = Measure(papers);
        return Mathf.Clamp(Mathf.Min(MaxLayoutScale,
            (bounds.width - 64f) / Mathf.Max(1f, layout.Width),
            (bounds.height - 96f) / Mathf.Max(1f, layout.Height)), 0.32f, MaxLayoutScale);
    }

    private CaseFolderUI FindFolder(Vector2 screenPosition)
    {
        for (int i = 0; i < loosePapers.Count; i++)
        {
            Level2FindingsSheet paper = loosePapers[i];
            if (paper == null) continue;
            CaseFolderUI folder = paper.FindFolderAtForStack(screenPosition, loosePapers);
            if (folder != null) return folder;
        }
        return null;
    }

    private IEnumerator FileStack(CaseFolderUI folder)
    {
        mode = Mode.Filing;
        Level2FindingsSheet[] papers = loosePapers.FindAll(p => p != null && p.CanTransfer).ToArray();
        int pageIndex = folder != null ? folder.FindPageIndex(Level2FindingsPage.PageTitle) : -1;
        if (papers.Length == 0 || pageIndex < 0) { StartScatter(pointer); yield break; }
        bool turned = false;
        folder.TurnToPage(pageIndex, () => turned = true);
        float waited = 0f;
        while (!turned && folder != null && folder.IsShowing && waited < 4f)
        {
            waited += Time.unscaledDeltaTime;
            yield return null;
        }
        if (folder == null || !folder.IsShowing || !turned)
        {
            mode = Mode.Idle;
            StartScatter(pointer);
            yield break;
        }

        for (int i = 0; i < papers.Length; i++) papers[i].BeginGroupFile();
        int completed = 0;
        for (int i = 0; i < papers.Length; i++) papers[i].FileAsPartOfStack(folder, () => completed++);
        waited = 0f;
        while (completed < papers.Length && folder != null && folder.IsShowing && waited < 4f)
        {
            waited += Time.unscaledDeltaTime;
            yield return null;
        }

        bool allReady = completed == papers.Length;
        for (int i = 0; i < papers.Length; i++) allReady &= papers[i] == null || papers[i].ReadyToCompleteGroupFile;
        if (!allReady)
        {
            for (int i = 0; i < papers.Length; i++)
                if (papers[i] != null && papers[i].IsDetached) papers[i].CancelGroupFile();
            mode = Mode.Idle;
            StartScatter(pointer);
            yield break;
        }

        for (int i = 0; i < papers.Length; i++)
            if (papers[i] != null) papers[i].CompleteGroupFile();
        loosePapers.Clear();
        stackOut = false;
        mode = Mode.Idle;
        if (folder != null) folder.RefreshCurrentPage();
    }

    internal void Reset()
    {
        StopAllCoroutines();
        mode = Mode.Idle;
        stackOut = false;
        coverDragging = false;
        pressing = false;
        pressData = null;
        pressedPaper = null;
        peelComplete = false;
        ownerVisible = true;
        scatterWhenOwnerShown = false;
        attachedPapers.Clear();
        loosePapers.Clear();
        ShowHoldRing(false);
    }

    private void BuildHoldRing()
    {
        if (holdRing != null) return;
        holdRingLayer = HudTheme.CreateRect("Supporting Stack Hold Ring", null);
        HudTheme.Stretch(holdRingLayer);
        Canvas canvas = holdRingLayer.gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.overrideSorting = true;
        canvas.sortingOrder = 2650;
        RectTransform ring = HudTheme.CreateRect("Ring", holdRingLayer);
        ring.anchorMin = ring.anchorMax = new Vector2(0.5f, 0.5f);
        ring.sizeDelta = new Vector2(96f, 96f);
        holdRing = HudTheme.AddImage(ring, HudTheme.Circle, new Color(1f, 1f, 1f, 0.85f));
        holdRing.type = Image.Type.Filled;
        holdRing.fillMethod = Image.FillMethod.Radial360;
        holdRing.fillOrigin = (int)Image.Origin360.Top;
        RectTransform hole = HudTheme.CreateRect("Hole", ring);
        HudTheme.Stretch(hole, 9f);
        HudTheme.AddImage(hole, HudTheme.Circle, new Color(0.1f, 0.14f, 0.2f, 0.35f));
        holdRing.gameObject.SetActive(false);
    }

    private void ShowHoldRing(bool show)
    {
        if (show) BuildHoldRing();
        if (holdRing != null && holdRing.gameObject.activeSelf != show) holdRing.gameObject.SetActive(show);
    }

    private void PlaceHoldRing(Vector2 screen, float progress)
    {
        if (holdRing == null || holdRingLayer == null) return;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(holdRingLayer, screen, null, out Vector2 local);
        holdRing.rectTransform.anchoredPosition = local;
        holdRing.fillAmount = Mathf.Clamp01(progress);
    }

    private void OnDestroy()
    {
        if (holdRingLayer != null) Destroy(holdRingLayer.gameObject);
    }

}
