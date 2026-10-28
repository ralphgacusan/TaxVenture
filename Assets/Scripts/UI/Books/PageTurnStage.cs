using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// PURPOSE:
/// A page turn shared by the Case Folder and Tax Code Book. Destination
/// contents are never drawn until the leaf lands. Forward turns lift the
/// source print off a blank backing; backward turns bring a blank leaf over
/// the source print. The destination is committed once, after settling.
/// A new input, close or disable completes the pending turn atomically.
/// Copies contain graphics only, never interactive scripts or raycast targets.
/// Attached records follow only the source surface while it remains visible.
/// Timing is unscaled and slow frames are capped by PageTurnMath.
///
/// Presentation only: never changes case data, values or page order.
/// </summary>
public sealed class PageTurnStage
{
    private const float LiftShare = 0.04f;

    private readonly MonoBehaviour host;
    private Coroutine routine;
    private Action pendingCommit;
    private RectTransform paper;
    private RectTransform leaf;
    private RectTransform under;
    private CanvasGroup leafGroup;
    private CanvasGroup paperGroup;
    private float paperAlpha;
    private bool paperBlocks, paperInteractable, paperHidden;
    private readonly List<Graphic> leafPrint = new List<Graphic>();
    private readonly List<Color> leafPrintColors = new List<Color>();
    private bool leafPrintShown = true;
    private Vector3 restPosition, restScale;
    private Quaternion restRotation;

    public PageTurnStage(MonoBehaviour host)
    {
        this.host = host;
    }

    /// <summary>A turn is playing.</summary>
    public bool IsTurning => routine != null;

    /// <summary>The moving copy (null when no turn plays).</summary>
    public RectTransform Leaf => leaf;

    /// <summary>The page index printed on the moving copy (-1 when none).</summary>
    public int LeafPage { get; private set; } = -1;

    /// <summary>The moving copy's front print still has visible opacity.</summary>
    public bool LeafShowsFront => leaf != null && leafPrintShown;

    /// <summary>The still source page under a backward turn, or blank backing under a forward turn.</summary>
    public RectTransform Under => under;

    /// <summary>The page index printed on the still copy (-1 when none).</summary>
    public int UnderPage { get; private set; } = -1;

    /// <summary>The live paper is hidden until the turn settles; attached contents must follow the source copy.</summary>
    public bool PaperHidden => paperHidden;

    /// <summary>
    /// Starts a turn of <paramref name="page"/> (at rest at the given pose).
    /// <paramref name="commit"/> draws the new page on the live paper; it runs
    /// only after the turn settles. Returns false (after committing) when
    /// the turn cannot be animated.
    /// </summary>
    public bool Begin(RectTransform page, Vector3 position, Quaternion rotation, Vector3 scale,
        int direction, int fromPage, int toPage, float duration, Action commit)
    {
        Complete();
        paper = page;
        restPosition = position;
        restRotation = rotation;
        restScale = scale;
        bool canAnimate = page != null && page.parent != null && host != null && host.isActiveAndEnabled &&
                          duration > 0f && page.gameObject.activeInHierarchy;
        if (!canAnimate)
        {
            commit?.Invoke();
            return false;
        }

        pendingCommit = commit;
        // Resolve driven layout first, then keep the turned copies at that
        // exact geometry even after their layout components are removed.
        Canvas.ForceUpdateCanvases();
        // Both copies start from the SOURCE page; the destination is not built yet.
        under = VisualCopy(page, "Paper (under)", page.GetSiblingIndex());
        leaf = VisualCopy(page, "Paper (turning)", page.GetSiblingIndex() + 1);
        LeafPage = direction >= 0 ? fromPage : -1;
        UnderPage = direction < 0 ? fromPage : -1;
        RectTransform blank = direction >= 0 ? under : leaf;
        foreach (Transform child in blank) child.gameObject.SetActive(false);
        HidePaper();

        leafGroup = leaf.GetComponent<CanvasGroup>();
        if (leafGroup == null) leafGroup = leaf.gameObject.AddComponent<CanvasGroup>();
        leafGroup.blocksRaycasts = false;
        leafGroup.interactable = false;
        leafGroup.ignoreParentGroups = false;
        leafPrint.Clear();
        leafPrintColors.Clear();
        Graphics.Clear();
        leaf.GetComponentsInChildren(true, Graphics);
        foreach (Graphic graphic in Graphics)
        {
            // The root graphic is the paper itself; only its printed contents fade.
            if (graphic == null || graphic.transform == leaf) continue;
            leafPrint.Add(graphic);
            leafPrintColors.Add(graphic.color);
        }
        Graphics.Clear();
        leafPrintShown = true;

        Pose(direction, 0f);
        routine = host.StartCoroutine(Play(direction, duration));
        return true;
    }

    /// <summary>Ends the turn now: copies removed, the live paper at rest and shown.</summary>
    public void Complete()
    {
        if (routine != null && host != null) host.StopCoroutine(routine);
        routine = null;
        if (leaf != null)
        {
            leaf.gameObject.SetActive(false);
            UnityEngine.Object.Destroy(leaf.gameObject);
        }
        if (under != null)
        {
            under.gameObject.SetActive(false);
            UnityEngine.Object.Destroy(under.gameObject);
        }
        leaf = under = null;
        leafGroup = null;
        leafPrint.Clear();
        leafPrintColors.Clear();
        LeafPage = UnderPage = -1;
        if (paper != null)
        {
            paper.localPosition = restPosition;
            paper.localRotation = restRotation;
            paper.localScale = restScale;
        }
        Action commit = pendingCommit;
        pendingCommit = null;
        try { commit?.Invoke(); }
        finally { ShowPaper(); }
    }

    private IEnumerator Play(int direction, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            yield return null;
            if (leaf == null) break;
            elapsed += PageTurnMath.Step(Time.unscaledDeltaTime);
            Pose(direction, PageTurnMath.Progress(elapsed, duration));
        }
        routine = null;
        Complete();
    }

    /// <summary>Places the moving copy for progress <paramref name="t"/> (0..1).</summary>
    private void Pose(int direction, float t)
    {
        if (leaf == null) return;
        float angle = PageTurnMath.Angle(direction, t);
        Rect rect = leaf.rect;
        // The hinge (spine) is the left edge at mid height, in the page's scaled local units.
        Vector3 hinge = Vector3.Scale(restScale, new Vector3(rect.xMin, rect.center.y, 0f));
        Quaternion turn = Quaternion.Euler(0f, angle, 0f);
        float lift = rect.height * LiftShare * Mathf.Abs(restScale.z) * Mathf.Sin(angle * Mathf.Deg2Rad);
        leaf.localScale = restScale;
        leaf.localRotation = restRotation * turn;
        leaf.localPosition = restPosition + restRotation * (hinge - turn * hinge) + restRotation * new Vector3(0f, 0f, -lift);

        float printAlpha = direction >= 0 ? PageTurnMath.PrintAlpha(angle) : 0f;
        leafPrintShown = printAlpha > 0f;
        for (int i = 0; i < leafPrint.Count; i++)
        {
            Graphic graphic = leafPrint[i];
            if (graphic == null) continue;
            Color color = leafPrintColors[i];
            color.a *= printAlpha;
            graphic.color = color;
        }

        // The turned-away leaf fades as it lands; a returning leaf appears as it lifts.
        if (leafGroup != null) leafGroup.alpha = PageTurnMath.Alpha(direction, t);
    }

    private void HidePaper()
    {
        if (paper == null || paperHidden) return;
        paperGroup = paper.GetComponent<CanvasGroup>();
        if (paperGroup == null) paperGroup = paper.gameObject.AddComponent<CanvasGroup>();
        paperAlpha = paperGroup.alpha;
        paperBlocks = paperGroup.blocksRaycasts;
        paperInteractable = paperGroup.interactable;
        paperGroup.alpha = 0f;
        paperGroup.blocksRaycasts = false;
        paperGroup.interactable = false;
        paperHidden = true;
    }

    private void ShowPaper()
    {
        if (!paperHidden) return;
        paperHidden = false;
        if (paperGroup == null) return;
        paperGroup.alpha = paperAlpha;
        paperGroup.blocksRaycasts = paperBlocks;
        paperGroup.interactable = paperInteractable;
    }

    // =========================================================
    // VISUAL COPIES
    // =========================================================

    private static readonly List<Component> Components = new List<Component>(256);
    private static readonly List<Graphic> Graphics = new List<Graphic>(128);
    private static readonly List<RectTransform> RectTransforms = new List<RectTransform>(128);
    private static readonly List<RectSnapshot> RectSnapshots = new List<RectSnapshot>(128);

    /// <summary>
    /// A graphics-only copy of <paramref name="source"/> as its sibling at
    /// <paramref name="siblingIndex"/>, with the same rest pose. Built under an
    /// inactive holder so no script on the copy ever wakes up.
    /// </summary>
    private RectTransform VisualCopy(RectTransform source, string name, int siblingIndex)
    {
        Transform parent = source.parent;
        CaptureRectLayout(source);
        var holder = new GameObject("Page Turn Holder", typeof(RectTransform));
        holder.SetActive(false);
        holder.transform.SetParent(parent, false);
        RectTransform copy = UnityEngine.Object.Instantiate(source, holder.transform, false);
        copy.name = name;
        StripToGraphics(copy.gameObject);
        if (copy.GetComponent<Graphic>() == null)
        {
            Image face = copy.gameObject.AddComponent<Image>();
            face.color = new Color(0.988f, 0.968f, 0.925f, 1f);
            face.raycastTarget = false;
        }
        copy.SetParent(parent, false);
        RestoreRectLayout(copy);
        LayoutElement layout = copy.GetComponent<LayoutElement>();
        if (layout == null) layout = copy.gameObject.AddComponent<LayoutElement>();
        layout.ignoreLayout = true;
        copy.SetSiblingIndex(Mathf.Clamp(siblingIndex, 0, parent.childCount - 1));
        copy.localPosition = restPosition;
        copy.localRotation = restRotation;
        copy.localScale = restScale;
        AddPaperShadow(copy);
        UnityEngine.Object.Destroy(holder);
        return copy;
    }

    private static void CaptureRectLayout(RectTransform root)
    {
        RectTransforms.Clear();
        RectSnapshots.Clear();
        root.GetComponentsInChildren(true, RectTransforms);
        foreach (RectTransform rect in RectTransforms)
            RectSnapshots.Add(new RectSnapshot(rect));
        RectTransforms.Clear();
    }

    private static void RestoreRectLayout(RectTransform root)
    {
        RectTransforms.Clear();
        root.GetComponentsInChildren(true, RectTransforms);
        int count = Mathf.Min(RectTransforms.Count, RectSnapshots.Count);
        for (int i = 0; i < count; i++) RectSnapshots[i].Apply(RectTransforms[i]);
        RectTransforms.Clear();
        RectSnapshots.Clear();
    }

    private static void AddPaperShadow(RectTransform root)
    {
        Graphic paperFace = root.GetComponent<Graphic>();
        if (paperFace == null) return;
        Shadow shadow = paperFace.GetComponent<Shadow>();
        if (shadow == null) shadow = paperFace.gameObject.AddComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.14f);
        shadow.effectDistance = new Vector2(0f, -2f);
        shadow.useGraphicAlpha = true;
    }

    private struct RectSnapshot
    {
        private readonly Vector2 anchorMin;
        private readonly Vector2 anchorMax;
        private readonly Vector2 pivot;
        private readonly Vector2 sizeDelta;
        private readonly Vector3 anchoredPosition;
        private readonly Quaternion localRotation;
        private readonly Vector3 localScale;

        public RectSnapshot(RectTransform rect)
        {
            anchorMin = rect.anchorMin;
            anchorMax = rect.anchorMax;
            pivot = rect.pivot;
            sizeDelta = rect.sizeDelta;
            anchoredPosition = rect.anchoredPosition3D;
            localRotation = rect.localRotation;
            localScale = rect.localScale;
        }

        public void Apply(RectTransform rect)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.sizeDelta = sizeDelta;
            rect.anchoredPosition3D = anchoredPosition;
            rect.localRotation = localRotation;
            rect.localScale = localScale;
        }
    }

    private static void StripToGraphics(GameObject root)
    {
        // Several passes: a component another one requires goes after it.
        for (int pass = 0; pass < 3; pass++)
        {
            bool removedAny = false;
            Components.Clear();
            root.GetComponentsInChildren(true, Components);
            for (int i = Components.Count - 1; i >= 0; i--)
            {
                Component component = Components[i];
                if (component == null || Keeps(component)) continue;
                if (RequiredByAnother(component)) continue;
                UnityEngine.Object.DestroyImmediate(component);
                removedAny = true;
            }
            if (!removedAny) break;
        }
        Components.Clear();

        Graphics.Clear();
        root.GetComponentsInChildren(true, Graphics);
        foreach (Graphic graphic in Graphics) graphic.raycastTarget = false;
        Graphics.Clear();
    }

    private static bool Keeps(Component component) =>
        component is Transform || component is CanvasRenderer || component is Graphic ||
        component is Mask || component is RectMask2D || component is BaseMeshEffect ||
        component is CanvasGroup || component is Canvas;

    private static readonly Dictionary<Type, RequireComponent[]> Requirements = new Dictionary<Type, RequireComponent[]>();
    private static readonly List<Component> Siblings = new List<Component>(16);

    /// <summary>True when another removable component on the same object still needs this one this pass.</summary>
    private static bool RequiredByAnother(Component component)
    {
        Type type = component.GetType();
        Siblings.Clear();
        component.GetComponents(Siblings);
        foreach (Component other in Siblings)
        {
            if (other == null || other == component || Keeps(other)) continue;
            foreach (RequireComponent require in RequirementsOf(other.GetType()))
                if ((require.m_Type0 != null && require.m_Type0.IsAssignableFrom(type)) ||
                    (require.m_Type1 != null && require.m_Type1.IsAssignableFrom(type)) ||
                    (require.m_Type2 != null && require.m_Type2.IsAssignableFrom(type)))
                {
                    Siblings.Clear();
                    return true;
                }
        }
        Siblings.Clear();
        return false;
    }

    private static RequireComponent[] RequirementsOf(Type type)
    {
        if (!Requirements.TryGetValue(type, out RequireComponent[] found))
        {
            found = (RequireComponent[])type.GetCustomAttributes(typeof(RequireComponent), true);
            Requirements[type] = found;
        }
        return found;
    }
}
