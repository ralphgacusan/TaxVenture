using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// PURPOSE:
/// The item under the finger while dragging out of a HUD icon: a closed
/// Case Folder, a Notes paper or a stack of taxpayer profiles (the content
/// is built by the caller). It lifts out of the icon, follows the finger
/// with a little lag and swing, and on release either blooms open where it
/// was dropped (the real window takes over), is absorbed by a drop target,
/// or flies back into its icon.
///
/// Lives on its own overlay canvas above the document windows and never
/// blocks raycasts. Update only runs while the item is visible.
/// </summary>
public sealed class HudDragItem : MonoBehaviour, IHudDragVisual
{
    private const int SortingOrder = 2600;
    private const float LiftDuration = 0.16f;
    private const float FollowSharpness = 18f;
    private const float OpenDuration = 0.14f;
    private const float ConsumeDuration = 0.2f;
    private const float ReturnDuration = 0.28f;

    private enum State { Idle, Following, Dropping }

    private RectTransform layer;
    private RectTransform item;
    private CanvasGroup group;
    private RectTransform icon;
    private Action<RectTransform, Vector2> buildContent;
    private Vector2 size;
    private float restTilt;

    private State state = State.Idle;
    private HudDragOutcome outcome;
    private Vector2 position;
    private Vector2 target;
    private float tilt;
    private float elapsed;
    private Vector2 dropFrom;

    public static HudDragItem Create(Transform hudCanvas, string name, RectTransform icon, Vector2 size,
        float restTilt, Action<RectTransform, Vector2> buildContent)
    {
        RectTransform layer = HudTheme.CreateRect("Drag Item " + name, hudCanvas);
        HudTheme.Stretch(layer);
        var canvas = layer.gameObject.AddComponent<Canvas>();
        canvas.overrideSorting = true;
        canvas.sortingOrder = SortingOrder;

        RectTransform item = HudTheme.CreateRect("Item", layer);
        item.anchorMin = item.anchorMax = new Vector2(0.5f, 0.5f);
        item.pivot = new Vector2(0.5f, 0.5f);
        item.sizeDelta = size;
        var group = item.gameObject.AddComponent<CanvasGroup>();
        group.blocksRaycasts = false;
        group.interactable = false;

        var drag = layer.gameObject.AddComponent<HudDragItem>();
        drag.layer = layer;
        drag.item = item;
        drag.group = group;
        drag.icon = icon;
        drag.size = size;
        drag.restTilt = restTilt;
        drag.buildContent = buildContent;
        item.gameObject.SetActive(false);
        drag.enabled = false;
        return drag;
    }

    // =========================================================
    // IHudDragVisual
    // =========================================================

    public void Begin(Vector2 screenPosition)
    {
        // Rebuild so the item always shows current content (e.g. profiles).
        for (int i = item.childCount - 1; i >= 0; i--) Destroy(item.GetChild(i).gameObject);
        buildContent?.Invoke(item, size);

        position = IconLocal();
        target = ToLocal(screenPosition);
        tilt = restTilt;
        elapsed = 0f;
        state = State.Following;
        item.gameObject.SetActive(true);
        Apply(position, 0.25f, tilt, 1f);
        enabled = true;

        if (AudioManager.Instance != null) AudioManager.Instance.PlayEffect("sfx_paper_pickup_01", 0.55f);
    }

    public void Move(Vector2 screenPosition)
    {
        if (state == State.Following) target = ToLocal(screenPosition);
    }

    public void Drop(Vector2 screenPosition, HudDragOutcome dropOutcome)
    {
        if (state == State.Idle) return;
        outcome = dropOutcome;
        dropFrom = position;
        elapsed = 0f;
        state = State.Dropping;
        enabled = true;
    }

    // =========================================================
    // ANIMATION
    // =========================================================

    private void Update()
    {
        float dt = Time.unscaledDeltaTime;
        elapsed += dt;

        if (state == State.Following)
        {
            Vector2 previous = position;
            position = Vector2.Lerp(position, target, 1f - Mathf.Exp(-FollowSharpness * dt));
            float velocityX = dt > 0f ? (position.x - previous.x) / dt : 0f;
            float swing = Mathf.Clamp(-velocityX * 0.012f, -12f, 12f);
            tilt = Mathf.Lerp(tilt, restTilt + swing, 1f - Mathf.Exp(-10f * dt));
            float lift = HudTheme.EaseOutBack(elapsed / LiftDuration);
            Apply(position, Mathf.LerpUnclamped(0.25f, 1f, lift), tilt, 1f);
            return;
        }

        if (state != State.Dropping) return;

        switch (outcome)
        {
            case HudDragOutcome.Opened:
            {
                float t = Mathf.Clamp01(elapsed / OpenDuration);
                Apply(dropFrom, Mathf.Lerp(1f, 1.25f, t), tilt, 1f - t);
                if (t >= 1f) Finish();
                break;
            }
            case HudDragOutcome.Consumed:
            {
                float t = Mathf.Clamp01(elapsed / ConsumeDuration);
                Apply(dropFrom, Mathf.Lerp(1f, 0.3f, t * t), tilt, 1f - t);
                if (t >= 1f) Finish();
                break;
            }
            default:
            {
                float t = Mathf.Clamp01(elapsed / ReturnDuration);
                float ease = t * t;
                Vector2 home = IconLocal();
                Vector2 p = Vector2.Lerp(dropFrom, home, ease);
                p.y += Mathf.Sin(t * Mathf.PI) * 40f;
                Apply(p, Mathf.Lerp(1f, 0.2f, ease), restTilt - 20f * ease, 1f - Mathf.Clamp01((t - 0.6f) / 0.4f));
                if (t >= 1f) Finish();
                break;
            }
        }
    }

    private void Finish()
    {
        if (outcome == HudDragOutcome.Returned && AudioManager.Instance != null)
            AudioManager.Instance.PlayEffect("sfx_paper_place_01", 0.55f);
        state = State.Idle;
        item.gameObject.SetActive(false);
        enabled = false;
    }

    private void Apply(Vector2 at, float scale, float rotation, float alpha)
    {
        item.anchoredPosition = at;
        item.localScale = new Vector3(scale, scale, 1f);
        item.localRotation = Quaternion.Euler(0f, 0f, rotation);
        group.alpha = alpha;
    }

    private Vector2 ToLocal(Vector2 screen)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(layer, screen, null, out Vector2 local);
        return local;
    }

    private Vector2 IconLocal()
    {
        if (icon == null) return target;
        Vector2 screen = RectTransformUtility.WorldToScreenPoint(null, icon.TransformPoint(icon.rect.center));
        return ToLocal(screen);
    }

    // =========================================================
    // CONTENT BUILDERS
    // =========================================================

    private static readonly Color FolderBack = new Color32(196, 156, 92, 255);
    private static readonly Color FolderFront = new Color32(228, 192, 122, 255);
    private static readonly Color FolderEdge = new Color32(160, 122, 66, 255);
    private static readonly Color NotesPaper = new Color32(246, 238, 216, 255);
    private static readonly Color NotesBack = new Color32(226, 214, 186, 255);

    /// <summary>A closed manila case folder with a tab, papers peeking out and a label.</summary>
    /// <summary>The closed Case Folder (reviewed: Arthur's Reviewed File, the same drawing with the teal cover).</summary>
    public static void BuildClosedFolder(RectTransform root, Vector2 size, bool reviewed = false)
    {
        float u = size.x / 300f;
        Color folderEdge = reviewed ? ReviewedFileLook.Teal(FolderEdge) : FolderEdge;
        Color folderBack = reviewed ? ReviewedFileLook.Teal(FolderBack) : FolderBack;
        Color folderFront = reviewed ? ReviewedFileLook.Teal(FolderFront) : FolderFront;
        RectTransform thickness = HudTheme.CreateRect("Thickness", root);
        HudTheme.Stretch(thickness);
        thickness.offsetMin += new Vector2(5f, -7f) * u;
        thickness.offsetMax += new Vector2(5f, -7f) * u;
        HudTheme.AddImage(thickness, HudTheme.Rounded, folderEdge, 10f * u);

        RectTransform back = HudTheme.CreateRect("Back Cover", root);
        HudTheme.Stretch(back);
        Image backImage = HudTheme.AddImage(back, HudTheme.Rounded, folderBack, 10f * u);
        var shadow = backImage.gameObject.AddComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.3f);
        shadow.effectDistance = new Vector2(8f, -10f) * u;

        RectTransform tab = HudTheme.PlaceTopLeft(HudTheme.CreateRect("Tab", root), 16f * u, -24f * u, 112f * u, 40f * u);
        HudTheme.AddImage(tab, HudTheme.Rounded, folderBack, 8f * u);

        RectTransform paper = HudTheme.PlaceTopLeft(HudTheme.CreateRect("Papers", root), 14f * u, 8f * u, size.x - 28f * u, 60f * u);
        paper.localRotation = Quaternion.Euler(0f, 0f, 1.2f);
        HudTheme.AddImage(paper, HudTheme.Rounded, new Color32(250, 248, 240, 255), 4f * u);

        RectTransform front = HudTheme.PlaceTopLeft(HudTheme.CreateRect("Front Cover", root), 0f, 30f * u, size.x, size.y - 30f * u);
        HudTheme.AddImage(front, HudTheme.Rounded, folderFront, 10f * u);
        RectTransform lip = HudTheme.PlaceTopLeft(HudTheme.CreateRect("Lip", front), 0f, 0f, size.x, 4f * u);
        HudTheme.AddImage(lip, null, folderEdge);

        RectTransform label = HudTheme.CreateRect("Label", front);
        label.anchorMin = label.anchorMax = new Vector2(0.5f, 0.5f);
        label.sizeDelta = new Vector2(170f * u, 54f * u);
        label.anchoredPosition = new Vector2(0f, 6f * u);
        HudTheme.AddImage(label, HudTheme.Rounded, new Color32(252, 250, 242, 255), 6f * u);
        var text = HudTheme.CreateLabel("Text", label, reviewed ? ReviewedFileLook.Label : "CASE FOLDER", 22f * u, ProfileCardFace.Ink,
            TMPro.TextAlignmentOptions.Center, TMPro.FontStyles.Bold);
        text.characterSpacing = 4f;
        HudTheme.Stretch((RectTransform)text.transform, 4f * u);
    }

    /// <summary>A small Notes page: paper stack, tape, title and a few ruled, ticked lines.</summary>
    public static void BuildNotesPaper(RectTransform root, Vector2 size)
    {
        float u = size.x / 230f;
        NotesPaperView.BuildPaperStack(root, NotesBack, NotesPaper, 0.45f * u);

        RectTransform tape = HudTheme.CreateRect("Tape", root);
        tape.anchorMin = tape.anchorMax = new Vector2(0.5f, 1f);
        tape.sizeDelta = new Vector2(80f * u, 22f * u);
        tape.anchoredPosition = new Vector2(0f, -2f * u);
        tape.localRotation = Quaternion.Euler(0f, 0f, -4f);
        HudTheme.AddImage(tape, null, new Color32(236, 222, 170, 215));

        var title = HudTheme.CreateLabel("Title", root, "Objectives", 28f * u, ProfileCardFace.Ink,
            TMPro.TextAlignmentOptions.MidlineLeft, TMPro.FontStyles.Bold);
        title.font = HudTheme.HandwritingFont;
        HudTheme.PlaceTopLeft((RectTransform)title.transform, 26f * u, 22f * u, size.x - 40f * u, 40f * u);

        for (int i = 0; i < 5; i++)
        {
            float y = 78f * u + i * 34f * u;
            RectTransform line = HudTheme.PlaceTopLeft(HudTheme.CreateRect("Line", root), 16f * u, y + 22f * u, size.x - 32f * u, 2f);
            HudTheme.AddImage(line, null, new Color32(150, 180, 214, 120));
            RectTransform box = HudTheme.PlaceTopLeft(HudTheme.CreateRect("Box", root), 24f * u, y + 2f * u, 16f * u, 16f * u);
            HudTheme.AddImage(box, HudTheme.Rounded, ProfileCardFace.Ink, 3f * u);
            RectTransform inner = HudTheme.CreateRect("Inner", box);
            HudTheme.Stretch(inner, 2f);
            HudTheme.AddImage(inner, HudTheme.Rounded, NotesPaper, 2f * u);
            RectTransform scribble = HudTheme.PlaceTopLeft(HudTheme.CreateRect("Text", root), 50f * u, y + 8f * u,
                (size.x - 80f * u) * (i % 2 == 0 ? 0.9f : 0.65f), 5f * u);
            HudTheme.AddImage(scribble, HudTheme.Rounded, new Color32(38, 44, 70, 120), 2f * u);
        }
    }

    /// <summary>
    /// A neat stack of the clients' ID cards (2026-10-27): the top one is the real 3D card
    /// (IdCardPortrait), one more peeking out underneath.
    /// </summary>
    public static Action<RectTransform, Vector2> ProfileStackBuilder(Func<System.Collections.Generic.List<TaxpayerProfile>> profiles)
    {
        return (root, size) =>
        {
            var list = profiles();
            if (list.Count > 1)
            {
                RectTransform under = HudTheme.CreateRect("Card Under", root);
                HudTheme.Stretch(under, size.y * 0.06f);
                under.offsetMin += new Vector2(10f, -10f);
                under.offsetMax += new Vector2(10f, -10f);
                under.localRotation = Quaternion.Euler(0f, 0f, 4f);
                HudTheme.AddImage(under, HudTheme.Rounded, IdCardModel.CardStock, 12f);
                RectTransform band = HudTheme.CreateRect("Band", under);
                band.anchorMin = new Vector2(0f, 0.8f);
                band.anchorMax = Vector2.one;
                band.offsetMin = band.offsetMax = Vector2.zero;
                HudTheme.AddImage(band, HudTheme.Rounded, SupportingDocumentFace.IdentityBlue, 12f);
            }

            RectTransform top = HudTheme.CreateRect("Top Card", root);
            HudTheme.Stretch(top);
            TaxpayerProfile first = list.Count > 0 ? list[0] : null;
            if (first != null) IdCardPortrait.Create(top, CaseRecordRules.FromProfile(first));
        };
    }
}
