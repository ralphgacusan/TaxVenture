using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Read-only presentation of one saved record exactly as Mark the Money!
/// left it: the toolkit's actual AnnotationPaper mesh with the player's
/// highlight and the same TYPE / TREATMENT / RECORD cards attached in their
/// places (built by IncomeToolkit.BuildCardVisual), lit like the desk with a
/// little self-light and a soft drop shadow, composited by the shared 3D
/// document layer. Only the front is shown (no flip). During the office
/// review its highlighted line is also a value source: tap (or drag) the
/// highlighted amount and place it on a Reviewed Schedule line through the
/// existing value-transfer system.
/// </summary>
public sealed class AnnotationPaperCollectionFace : MonoBehaviour, ICanvasRaycastFilter
{
    private const float FlipSeconds = 0.16f;

    private RectTransform card;
    private Transform modelRoot;
    private AnnotationPaper paper;
    private ToolkitBuilder builder;
    private Doc3DLayer documentLayer;
    private Canvas controlsCanvas;
    private AnnotationDefinition definition;
    private AnnotationEntryDefinition entry;
    private AnnotationEntryState saved;
    private Vector3 paperScale = Vector3.one;
    private Action close;
    private Coroutine flip;
    private bool showingAnnotations;
    private bool inputLocked;
    private float flipScale = 1f;
    private AnnotatedValueRow valueRow;
    private int paperLayer = -1;
    private Camera excludedFrom;

    public bool ShowingAnnotations => showingAnnotations;

    /// <summary>
    /// Spare unnamed layers (TagManager 23-26; 28-31 belong to stamps, the
    /// training binder, the settings board and Doc3DLayer). Each paper keeps
    /// its own, so its capture camera renders only that paper and no
    /// per-frame layer swapping is needed; the main camera skips them.
    /// </summary>
    public static readonly int[] PaperLayers = { 23, 24, 25, 26 };

    public static AnnotationPaperCollectionFace Build(Transform parent, Transform hudCanvas,
        AnnotationEntryDefinition entry, AnnotationEntryState saved, AnnotationDefinition definition, int index = 0)
    {
        int layer = PaperLayers[Mathf.Abs(index) % PaperLayers.Length];
        if (parent == null || hudCanvas == null || entry == null) return null;

        var view = parent.gameObject.AddComponent<AnnotationPaperCollectionFace>();
        view.card = parent as RectTransform;
        view.entry = entry;
        view.saved = saved;
        view.definition = definition;
        // Same model as the market desk; a little self-light keeps the paper cream in front of the camera.
        view.builder = new ToolkitBuilder(layer) { SelfLight = 0.5f };

        var model = new GameObject("Floating Annotation Paper " + entry.id);
        model.layer = layer;
        view.modelRoot = model.transform;
        view.paper = AnnotationPaper.Build(view.builder, view.modelRoot, Vector3.zero, entry, entry.id);
        view.paper.SetSavedAnnotations(definition, entry, saved, true, true);
        view.showingAnnotations = true;
        view.AttachCards();
        view.AddDropShadow();
        view.paper.SetVisible(true);
        foreach (Collider hit in model.GetComponentsInChildren<Collider>(true)) hit.enabled = false;
        model.SetActive(false);

        view.documentLayer = Doc3DLayer.Create(hudCanvas, model.transform, "Annotated Paper " + entry.id);
        if (view.documentLayer != null)
        {
            view.documentLayer.CompositeOverDocuments = true;
            // Always drawn in the window order: never behind the HUD or the desk, even when it overlaps nothing.
            view.documentLayer.AlwaysComposite = true;
            // Smooth dragging: its own layer (no swaps), its window as footprint, and a capture only when it moves.
            view.documentLayer.PersistentRenderLayer = layer;
            view.documentLayer.FootprintRect = view.card;
            view.documentLayer.RenderOnlyWhenChanged = true;
            view.paperLayer = layer;
        }

        // No close control: hold any paper to stack them all, then drop the stack on the icon.
        return view;
    }

    public void SetCloseAction(System.Action action) => close = action;

    public void SetInputLocked(bool locked) => inputLocked = locked;

    public bool IsRaycastLocationValid(Vector2 screenPosition, Camera eventCamera)
    {
        if (inputLocked) return false;
        foreach (Doc3DLayer layer in Doc3DLayer.All)
        {
            if (layer == null || layer == documentLayer || !layer.IsVisible ||
                layer.SortingOrder <= (documentLayer != null ? documentLayer.SortingOrder : 0)) continue;
            if (layer.TryGetFootprint(out Rect footprint) && footprint.Contains(screenPosition)) return false;
        }
        return true;
    }

    /// <summary>Only the annotated front is shown: kept for older callers, it never hides or flips the paper.</summary>
    public void SetAnnotations(bool show, bool animate) { }

    /// <summary>The player's saved cards, attached in their places as the market desk placed them.</summary>
    private void AttachCards()
    {
        if (saved == null || paper == null) return;
        bool mastered = Level2OfficeStart.Review?.challenge?.mastered == true;
        Attach(AnnotationSlot.Type, string.IsNullOrEmpty(saved.typeId) ? null : new[] { saved.typeId }, mastered);
        Attach(AnnotationSlot.Treatment, string.IsNullOrEmpty(saved.treatmentId) ? null : new[] { saved.treatmentId }, mastered);
        Attach(AnnotationSlot.Evidence, saved.evidenceIds != null ? saved.evidenceIds.ToArray() : null, mastered);
        if (mastered && paper.HasLine(saved.highlightLineId)) paper.SetHighlightResult(saved.highlightLineId, 1);
    }

    private void Attach(AnnotationSlot slot, string[] ids, bool mastered)
    {
        if (ids == null || ids.Length == 0) return;
        float scale = IncomeToolkit.PlacedScale / IncomeToolkit.PaperScale;
        for (int i = 0; i < ids.Length; i++)
        {
            AnnotationOptionDefinition option = slot == AnnotationSlot.Evidence
                ? entry.evidenceOptions?.Find(o => o != null && o.id == ids[i])
                : definition?.FindOption(slot, ids[i], entry);
            if (option == null) continue;
            paper.SlotPose(slot, i, ids.Length, out Vector3 position, out Quaternion rotation);
            var attached = new AnnotationCard { kind = slot, optionId = option.id, label = option.label, placed = true, locked = mastered };
            attached.root = builder.Node("Attached Card " + option.id, paper.Root, paper.Root.InverseTransformPoint(position),
                Quaternion.Inverse(paper.Root.rotation) * rotation);
            attached.root.localScale = Vector3.one * scale;
            IncomeToolkit.BuildCardVisual(builder, attached, slot, option);
            if (attached.lockBadge != null) attached.lockBadge.gameObject.SetActive(mastered);
        }
        paper.SetFilled(slot, true);
        if (mastered) paper.SetResult(slot, 1);
    }

    /// <summary>A soft two-step shadow under the sheet, offset down-right, so it reads as paper lifted off the screen.</summary>
    private void AddDropShadow()
    {
        Transform root = paper.Root;
        builder.Box("Drop Shadow Soft", root, new Vector3(0.011f, -0.0035f, -0.014f),
            new Vector3(AnnotationPaper.Width + 0.016f, 0.0004f, AnnotationPaper.Depth + 0.016f), builder.Clear(new Color(0f, 0f, 0f, 0.12f), "Paper Shadow Soft"));
        builder.Box("Drop Shadow", root, new Vector3(0.006f, -0.0025f, -0.008f),
            new Vector3(AnnotationPaper.Width, 0.0004f, AnnotationPaper.Depth), builder.Clear(new Color(0f, 0f, 0f, 0.22f), "Paper Shadow"));
    }

    private void LateUpdate()
    {
        if (card == null || modelRoot == null || paper == null) return;
        FloatingWindow window = GetComponent<FloatingWindow>();
        Camera camera = CameraController.Instance != null ? CameraController.Instance.MainCamera : Camera.main;
        if (!card.gameObject.activeInHierarchy || window == null || camera == null)
        {
            if (modelRoot.gameObject.activeSelf) modelRoot.gameObject.SetActive(false);
            return;
        }

        if (!modelRoot.gameObject.activeSelf) modelRoot.gameObject.SetActive(true);
        // Only the paper's own capture draws it: keep the world camera from drawing (and occluding) a second copy.
        if (paperLayer >= 0 && (camera != excludedFrom || (camera.cullingMask & (1 << paperLayer)) != 0))
        {
            camera.cullingMask &= ~(1 << paperLayer);
            excludedFrom = camera;
        }
        card.GetWorldCorners(Corners);
        Vector2 bottomLeft = RectTransformUtility.WorldToScreenPoint(null, Corners[0]);
        Vector2 bottomRight = RectTransformUtility.WorldToScreenPoint(null, Corners[3]);
        Vector2 topLeft = RectTransformUtility.WorldToScreenPoint(null, Corners[1]);
        Vector2 topRight = RectTransformUtility.WorldToScreenPoint(null, Corners[2]);
        float screenWidth = Vector2.Distance(bottomLeft, bottomRight);
        float screenHeight = Vector2.Distance(bottomLeft, topLeft);
        Vector2 screenCenter = (bottomLeft + topRight) * 0.5f;
        float screenAngle = Mathf.Atan2(bottomRight.y - bottomLeft.y, bottomRight.x - bottomLeft.x) * Mathf.Rad2Deg;

        float depth = Mathf.Max(camera.nearClipPlane + 0.05f, 0.8f);
        float worldHeight = 2f * depth * Mathf.Tan(camera.fieldOfView * 0.5f * Mathf.Deg2Rad) *
                            screenHeight / Mathf.Max(1f, camera.pixelHeight);
        float worldWidth = 2f * depth * Mathf.Tan(camera.fieldOfView * 0.5f * Mathf.Deg2Rad) *
                           screenWidth / Mathf.Max(1f, camera.pixelHeight);
        modelRoot.position = camera.ScreenToWorldPoint(new Vector3(screenCenter.x, screenCenter.y, depth));

        Quaternion faceCamera = Quaternion.LookRotation(camera.transform.up, -camera.transform.forward);
        modelRoot.rotation = faceCamera * Quaternion.AngleAxis(-screenAngle, Vector3.up);
        paperScale = new Vector3(worldWidth / AnnotationPaper.Width, 1f,
            worldHeight / AnnotationPaper.Depth);
        paper.Hinge.localScale = new Vector3(paperScale.x * flipScale, paperScale.y, paperScale.z);

        UpdateValueRow(camera);

        // Keep the captured 3D face in exactly the same global order as its
        // transparent FloatingWindow hit area.
        if (documentLayer != null)
        {
            documentLayer.AssignSortingOrder(window.SortingOrder);
            if (controlsCanvas != null) controlsCanvas.sortingOrder = window.SortingOrder + 1;
        }
        // The highlighted-amount band sits in this paper's own layer just above its face:
        // over its own paper, under any paper brought in front of it.
        if (valueRow != null && valueRow.gameObject.activeInHierarchy)
        {
            Canvas band = valueRow.GetComponent<Canvas>();
            if (band == null)
            {
                band = valueRow.gameObject.AddComponent<Canvas>();
                valueRow.gameObject.AddComponent<GraphicRaycaster>();
            }
            band.overrideSorting = true;
            band.sortingOrder = window.SortingOrder + 1;
        }
    }

    /// <summary>
    /// Lays the value source over the highlighted line while the office
    /// review is open and the annotations are showing; hidden otherwise.
    /// </summary>
    private void UpdateValueRow(Camera camera)
    {
        string lineId = saved?.highlightLineId;
        ScheduleSourceDefinition source = null;
        float z = 0f, height = 0f;
        bool active = showingAnnotations && !inputLocked && flip == null && entry != null &&
                      paper.TryLineBand(lineId, out z, out height) &&
                      Level2OfficeReview.AnnotatedSource(entry.id, out source);
        if (!active)
        {
            if (valueRow != null && valueRow.gameObject.activeSelf) valueRow.gameObject.SetActive(false);
            return;
        }
        if (valueRow == null)
        {
            ReviewLineDefinition line = entry.lines?.Find(l => l != null && l.id == lineId);
            valueRow = AnnotatedValueRow.Create(card, source.semanticKey, source.amount,
                line != null && !string.IsNullOrWhiteSpace(line.value) ? line.value : Money(source.amount));
        }
        if (!valueRow.gameObject.activeSelf) valueRow.gameObject.SetActive(true);

        // Follow the real printed row (the 3D sheet is hinged, so measure it).
        Vector2 left = ToCard(camera, paper.Root.TransformPoint(new Vector3(-AnnotationPaper.Width * 0.47f, 0.002f, z)));
        Vector2 right = ToCard(camera, paper.Root.TransformPoint(new Vector3(AnnotationPaper.Width * 0.47f, 0.002f, z)));
        Vector2 top = ToCard(camera, paper.Root.TransformPoint(new Vector3(0f, 0.002f, z + height * 0.55f)));
        Vector2 bottom = ToCard(camera, paper.Root.TransformPoint(new Vector3(0f, 0.002f, z - height * 0.55f)));
        RectTransform rect = (RectTransform)valueRow.transform;
        rect.anchoredPosition = (left + right) * 0.5f;
        rect.sizeDelta = new Vector2(Mathf.Max(40f, Vector2.Distance(left, right)), Mathf.Max(34f, Vector2.Distance(top, bottom)));
        rect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(right.y - left.y, right.x - left.x) * Mathf.Rad2Deg);
        valueRow.SetScheduled(Level2OfficeReview.IsScheduled(source.semanticKey));
    }

    private Vector2 ToCard(Camera camera, Vector3 world)
    {
        Vector2 screen = camera.WorldToScreenPoint(world);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(card, screen, null, out Vector2 local);
        return local;
    }

    private static string Money(float amount) => "₱" + amount.ToString("N0", System.Globalization.CultureInfo.InvariantCulture);

    private void OnDisable()
    {
        if (valueRow != null) valueRow.gameObject.SetActive(false);
        if (flip != null) StopCoroutine(flip);
        flip = null;
        flipScale = 1f;
        inputLocked = false;
        if (paper != null) paper.Hinge.localScale = paperScale;
        if (modelRoot != null) modelRoot.gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        if (documentLayer != null) Destroy(documentLayer.gameObject);
        if (modelRoot != null) Destroy(modelRoot.gameObject);
        if (builder != null) builder.ReleaseMaterials();
    }

    private void BuildCloseButton()
    {
        RectTransform closeRoot = HudTheme.CreateRect("Paper Close Control", transform);
        closeRoot.anchorMin = closeRoot.anchorMax = new Vector2(1f, 1f);
        closeRoot.pivot = new Vector2(1f, 1f);
        closeRoot.sizeDelta = new Vector2(28f, 28f);
        closeRoot.anchoredPosition = new Vector2(-8f, -8f);
        controlsCanvas = closeRoot.gameObject.AddComponent<Canvas>();
        controlsCanvas.overrideSorting = true;
        closeRoot.gameObject.AddComponent<AnnotationPaperCloseRaycastFilter>().Initialize(this);
        closeRoot.gameObject.AddComponent<GraphicRaycaster>();

        Image image = HudTheme.AddImage(closeRoot, HudTheme.Circle, AnnotationPaper.CollectionPaperColor);
        image.raycastTarget = true;
        TextMeshProUGUI text = HudTheme.CreateLabel("Close Label", closeRoot, "×", 19f,
            AnnotationPaper.CollectionHeaderColor, TextAlignmentOptions.Center, FontStyles.Bold);
        HudTheme.Stretch(text.rectTransform);
        text.raycastTarget = false;
        Button button = closeRoot.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(() =>
        {
            if (!inputLocked) close?.Invoke();
        });
    }

    /// <summary>Compact four-sheet drag ghost for the permanent HUD icon.</summary>
    public static void BuildStack(RectTransform root, Vector2 size)
    {
        float unit = size.x / 280f;
        for (int i = 3; i >= 0; i--)
        {
            RectTransform sheet = HudTheme.CreateRect("Sheet " + (i + 1), root);
            sheet.anchorMin = sheet.anchorMax = new Vector2(0.5f, 0.5f);
            sheet.pivot = new Vector2(0.5f, 0.5f);
            sheet.sizeDelta = size * (1f - i * 0.035f);
            sheet.anchoredPosition = new Vector2(i * 4f * unit, -i * 4f * unit);
            sheet.localRotation = Quaternion.Euler(0f, 0f, (i - 1.5f) * 2f);
            HudTheme.AddImage(sheet, HudTheme.Rounded, AnnotationPaper.CollectionPaperColor, 8f * unit);
            if (i != 0) continue;
            RectTransform header = HudTheme.PlaceTopLeft(HudTheme.CreateRect("Header", sheet), 0f, 0f,
                size.x, 32f * unit);
            HudTheme.AddImage(header, null, AnnotationPaper.CollectionHeaderColor);
            TextMeshProUGUI title = HudTheme.CreateLabel("Title", header, "ANNOTATED RECORDS", 14f * unit,
                Color.white, TextAlignmentOptions.Center, FontStyles.Bold);
            HudTheme.Stretch(title.rectTransform);
            TextMeshProUGUI count = HudTheme.CreateLabel("Count", sheet, "E1  E2  E3  E4", 11f * unit,
                AnnotationPaper.CollectionInkColor, TextAlignmentOptions.Center, FontStyles.Bold);
            count.rectTransform.anchorMin = count.rectTransform.anchorMax = new Vector2(0.5f, 0.45f);
            count.rectTransform.sizeDelta = new Vector2(size.x * 0.82f, 22f * unit);
        }
    }

    private static readonly Vector3[] Corners = new Vector3[4];
}

/// <summary>Forwards point coverage through the close control's override-sorting Canvas.</summary>
internal sealed class AnnotationPaperCloseRaycastFilter : MonoBehaviour, ICanvasRaycastFilter
{
    private AnnotationPaperCollectionFace owner;

    public void Initialize(AnnotationPaperCollectionFace face) => owner = face;

    public bool IsRaycastLocationValid(Vector2 screenPosition, Camera eventCamera) =>
        owner != null && owner.IsRaycastLocationValid(screenPosition, eventCamera);
}

/// <summary>
/// The highlighted amount of an annotated paper as a value source for the
/// Reviewed Schedule. Tap selects it (then tap a schedule line); a drag
/// carries it. A soft pulsing band shows it can be used; it turns green once
/// that payment is on the schedule, amber while it is picked up.
/// </summary>
internal sealed class AnnotatedValueRow : MonoBehaviour, IDataValueSource, IPointerClickHandler, IBeginDragHandler
{
    private static readonly Color Ready = new Color(0.25f, 0.55f, 1f, 1f);
    private static readonly Color Chosen = new Color(1f, 0.72f, 0.12f, 1f);
    private static readonly Color Used = new Color(0.2f, 0.62f, 0.38f, 1f);

    private string semanticKey;
    private float amount;
    private string display;
    private Image band;
    private Image frame;
    private bool selected;
    private bool scheduled;
    private bool subscribed;

    public static AnnotatedValueRow Create(RectTransform card, string semanticKey, float amount, string display)
    {
        RectTransform rect = HudTheme.CreateRect("Annotated value " + semanticKey, card);
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        var row = rect.gameObject.AddComponent<AnnotatedValueRow>();
        row.semanticKey = semanticKey;
        row.amount = amount;
        row.display = display;
        row.band = HudTheme.AddImage(rect, HudTheme.Rounded, new Color(Ready.r, Ready.g, Ready.b, 0.12f), 10f);
        row.band.raycastTarget = true;
        RectTransform outline = HudTheme.CreateRect("Frame", rect);
        HudTheme.Stretch(outline, -3f);
        row.frame = HudTheme.AddImage(outline, HudTheme.Rounded, new Color(Ready.r, Ready.g, Ready.b, 0.5f), 12f);
        row.frame.raycastTarget = false;
        row.frame.fillCenter = false;
        rect.gameObject.AddComponent<ValueDragSource>().Initialize(row, row);
        return row;
    }

    public DataValue GetDataValue() => new DataValue(amount, display, DataValueType.Number, semanticKey);

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.dragging || ValueTransferManager.Instance == null) return;
        if (Level2AnnotatedPapers.Instance != null && (Level2AnnotatedPapers.Instance.SwallowTap || Level2AnnotatedPapers.Instance.IsStacked)) return;
        ValueTransferManager.Instance.SelectValue(this, this);
        if (ReferenceEquals(ValueTransferManager.Instance.SelectedSourceComponent, this)) Level2OfficeReview.PresentSchedule();
    }

    // Runs beside ValueDragSource on the same row: the schedule comes forward for the drop.
    public void OnBeginDrag(PointerEventData eventData) => Level2OfficeReview.PresentSchedule();

    public void SetScheduled(bool value)
    {
        // Just landed on the schedule: a quick pop as it turns green.
        if (value && !scheduled && wasShown) popUntil = Time.unscaledTime + PopSeconds;
        scheduled = value;
        wasShown = true;
    }

    private const float PopSeconds = 0.35f;
    private float popUntil = -1f;
    private bool wasShown;

    private void OnEnable() => Subscribe();

    private void Start() => Subscribe();

    private void Subscribe()
    {
        if (subscribed || ValueTransferManager.Instance == null) return;
        ValueTransferManager.Instance.OnSelectionChanged += HandleSelection;
        subscribed = true;
        HandleSelection(ValueTransferManager.Instance.SelectedSourceComponent);
    }

    private void OnDisable()
    {
        if (subscribed && ValueTransferManager.Instance != null) ValueTransferManager.Instance.OnSelectionChanged -= HandleSelection;
        subscribed = false;
        selected = false;
    }

    private void HandleSelection(object owner) => selected = ReferenceEquals(owner, this);

    private void Update()
    {
        if (!subscribed) Subscribe();
        float wave = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 4f);
        Color tint = selected ? Chosen : scheduled ? Used : Ready;
        // A frame marks the value; the fill stays faint so the highlighted text is never covered.
        band.color = new Color(tint.r, tint.g, tint.b, selected ? 0.1f : scheduled ? 0.05f : 0.02f + 0.03f * wave);
        frame.color = new Color(tint.r, tint.g, tint.b, selected ? 1f : scheduled ? 0.7f : 0.45f + 0.4f * wave);
        float pop = selected ? 1.04f : 1f;
        float landed = popUntil > Time.unscaledTime ? 1f - (popUntil - Time.unscaledTime) / PopSeconds : 1f;
        if (landed < 1f) pop *= 1f + 0.14f * Mathf.Sin(landed * Mathf.PI);
        transform.localScale = Vector3.Lerp(transform.localScale, Vector3.one * pop, 1f - Mathf.Exp(-18f * Mathf.Min(Time.unscaledDeltaTime, 1f / 30f)));
    }
}
