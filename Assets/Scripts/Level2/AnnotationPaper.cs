using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// PURPOSE:
/// Builds the market toolkit's 3D parts from Unity primitives (thin boxes,
/// cylinders, world-space TextMeshPro on paper) and owns their runtime
/// materials, so IncomeToolkit can release everything in one place.
/// Every collider made here is a labelled trigger (ToolkitTarget) on one layer.
/// Icons (tick, cross) are drawn strokes, never font glyphs.
/// </summary>
public sealed class ToolkitBuilder
{
    public readonly int Layer;
    private readonly List<Material> materials = new List<Material>();

    public static readonly Quaternion Flat = Quaternion.Euler(90f, 0f, 0f);

    public ToolkitBuilder(int layer) => Layer = layer;

    /// <summary>
    /// 0..1: how much of its own colour a solid part emits on top of the scene
    /// lighting. Papers held in front of the camera catch the scene light at a
    /// grazing angle and read grey; a little self-light keeps their true
    /// colours while the lit shading still gives edges and cards depth.
    /// </summary>
    public float SelfLight { get; set; }

    public Material Lit(Color color, string name)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        var material = new Material(shader) { name = "Level2 Toolkit " + name, color = color };
        if (SelfLight > 0f)
        {
            material.EnableKeyword("_EMISSION");
            material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            material.SetColor("_EmissionColor", color * Mathf.Clamp01(SelfLight));
        }
        materials.Add(material);
        return material;
    }

    /// <summary>See-through colour (ink stroke, glow, shadow). Sprites/Default is always in the build.</summary>
    public Material Clear(Color color, string name)
    {
        var material = new Material(Shader.Find("Sprites/Default")) { name = "Level2 Toolkit " + name, color = color };
        materials.Add(material);
        return material;
    }

    public Transform Node(string name, Transform parent, Vector3 localPosition, Quaternion localRotation)
    {
        var go = new GameObject(name);
        go.layer = Layer;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPosition;
        go.transform.localRotation = localRotation;
        return go.transform;
    }

    public GameObject Shape(PrimitiveType type, string name, Transform parent, Vector3 localPosition, Vector3 size, Material material)
    {
        GameObject shape = GameObject.CreatePrimitive(type);
        shape.name = name;
        shape.layer = Layer;
        Object.DestroyImmediate(shape.GetComponent<Collider>());
        shape.transform.SetParent(parent, false);
        shape.transform.localPosition = localPosition;
        shape.transform.localScale = size;
        MeshRenderer renderer = shape.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        return shape;
    }

    public GameObject Box(string name, Transform parent, Vector3 localPosition, Vector3 size, Material material) =>
        Shape(PrimitiveType.Cube, name, parent, localPosition, size, material);

    /// <summary>A flat disc lying on the parent's surface (Unity's cylinder is 2 units tall).</summary>
    public GameObject Disc(string name, Transform parent, Vector3 localPosition, float diameter, float height, Material material) =>
        Shape(PrimitiveType.Cylinder, name, parent, localPosition, new Vector3(diameter, height * 0.5f, diameter), material);

    /// <summary>A straight flat stroke between two surface points (x, z) at height y.</summary>
    public GameObject Stroke(string name, Transform parent, Vector2 a, Vector2 b, float width, float y, float thickness, Material material)
    {
        Vector2 d = b - a;
        GameObject stroke = Box(name, parent, new Vector3((a.x + b.x) * 0.5f, y, (a.y + b.y) * 0.5f),
            new Vector3(d.magnitude + width * 0.6f, thickness, width), material);
        stroke.transform.localRotation = Quaternion.Euler(0f, -Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg, 0f);
        return stroke;
    }

    /// <summary>A tick mark (drawn, not a font glyph) sized to <paramref name="size"/>.</summary>
    public void CheckIcon(Transform parent, Vector3 center, float size, Material material)
    {
        float y = center.y, w = size * 0.2f;
        Vector2 c = new Vector2(center.x, center.z);
        Stroke("Tick A", parent, c + new Vector2(-0.34f, 0.02f) * size, c + new Vector2(-0.1f, -0.24f) * size, w, y, 0.0006f, material);
        Stroke("Tick B", parent, c + new Vector2(-0.1f, -0.24f) * size, c + new Vector2(0.36f, 0.3f) * size, w, y, 0.0006f, material);
    }

    /// <summary>A cross (drawn, not a font glyph).</summary>
    public void CrossIcon(Transform parent, Vector3 center, float size, Material material)
    {
        float y = center.y, w = size * 0.2f;
        Vector2 c = new Vector2(center.x, center.z);
        Stroke("Cross A", parent, c + new Vector2(-0.28f, -0.28f) * size, c + new Vector2(0.28f, 0.28f) * size, w, y, 0.0006f, material);
        Stroke("Cross B", parent, c + new Vector2(-0.28f, 0.28f) * size, c + new Vector2(0.28f, -0.28f) * size, w, y, 0.0006f, material);
    }

    /// <summary>Dashed rectangle outline lying on the surface; returns the dash transforms.</summary>
    public List<Transform> Dashes(string name, Transform parent, float width, float depth, float y, Material material)
    {
        var list = new List<Transform>();
        const float dash = 0.009f, gap = 0.006f, t = 0.0022f;
        for (int side = 0; side < 4; side++)
        {
            bool horizontal = side < 2;
            float length = horizontal ? width : depth;
            int count = Mathf.Max(2, Mathf.RoundToInt(length / (dash + gap)));
            float step = length / count;
            for (int i = 0; i < count; i++)
            {
                float along = -length * 0.5f + step * (i + 0.5f);
                Vector3 position = horizontal
                    ? new Vector3(along, y, (side == 0 ? 0.5f : -0.5f) * depth)
                    : new Vector3((side == 2 ? -0.5f : 0.5f) * width, y, along);
                Vector3 size = horizontal ? new Vector3(step * 0.62f, 0.0005f, t) : new Vector3(t, 0.0005f, step * 0.62f);
                list.Add(Box(name, parent, position, size, material).transform);
            }
        }
        return list;
    }

    /// <summary>World-space text. Default: lying on the parent's surface (+y up), top toward +z.</summary>
    public TextMeshPro Text(Transform parent, Vector3 localPosition, Vector2 size, string text, Color color, float maxSize,
        TextAlignmentOptions alignment = TextAlignmentOptions.Center, bool bold = false, Quaternion? rotation = null, bool autoSize = true)
    {
        var go = new GameObject("Text", typeof(RectTransform));
        go.layer = Layer;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPosition;
        go.transform.localRotation = rotation ?? Flat;
        var label = go.AddComponent<TextMeshPro>();
        label.rectTransform.sizeDelta = size;
        label.font = HudTheme.Font;
        label.text = bold ? "<b>" + text + "</b>" : text;
        label.color = color;
        label.alignment = alignment;
        label.enableAutoSizing = autoSize;
        label.fontSize = maxSize;
        label.fontSizeMin = 0.05f;
        label.fontSizeMax = maxSize;
        label.richText = true;
        label.textWrappingMode = TextWrappingModes.Normal;
        label.overflowMode = TextOverflowModes.Truncate;
        return label;
    }

    public BoxCollider Hit(Transform parent, Vector3 center, Vector3 size, ToolkitTarget.Kind kind, string id,
        AnnotationSlot slot, AnnotationCard card = null)
    {
        Transform node = Node("Hit " + kind + " " + id, parent, center, Quaternion.identity);
        BoxCollider box = node.gameObject.AddComponent<BoxCollider>();
        box.size = size;
        box.isTrigger = true;
        ToolkitTarget target = node.gameObject.AddComponent<ToolkitTarget>();
        target.kind = kind;
        target.id = id;
        target.slot = slot;
        target.card = card;
        return box;
    }

    public void ReleaseMaterials()
    {
        foreach (Material material in materials)
            if (material != null) Object.Destroy(material);
        materials.Clear();
    }
}

/// <summary>
/// One note card (TYPE / TREATMENT) or record tag (EVIDENCE): a cream card
/// with a coloured header band and word, its label, a soft shadow on the
/// surface under it, a glow when it is picked up and a lock badge once a
/// check confirmed it.
/// </summary>
public sealed class AnnotationCard
{
    public AnnotationSlot kind;
    public string optionId;
    public string label;
    public Transform root;
    public Transform body;
    public BoxCollider hit;
    public Transform shadow;
    public Material shadowMaterial;
    public Material glowMaterial;
    public Transform lockBadge;

    /// <summary>Pose on the desk beside the case (toolkit-local), and tucked in the case.</summary>
    public Vector3 homePosition;
    public Quaternion homeRotation;
    public Vector3 boxPosition;

    /// <summary>On the paper (in a note place) right now.</summary>
    public bool placed;
    /// <summary>Shown in the palette for the current record (false: kept in the case).</summary>
    public bool available = true;
    /// <summary>Confirmed right by a check: stays put.</summary>
    public bool locked;
    public bool selected;
    public float glow;
    public Coroutine motion;
}

/// <summary>
/// PURPOSE:
/// One record laid out for annotation on the toolkit's clipboard: a dark
/// header band with its title, the printed rows (each tappable and
/// swipeable, with a hand-drawn highlighter stroke made of uneven segments
/// that is invisible until the player inks it) and three note places along
/// the bottom: TYPE, TREATMENT and EVIDENCE. Each place has a coloured pill
/// with its word, a neutral dashed outline and a faint "Drop a ..." hint.
/// After a check a place shows a drawn tick or cross badge. Rendered from an
/// AnnotationEntryDefinition, so the tutorial record and the challenge
/// records share one view.
/// </summary>
public sealed class AnnotationPaper
{
    public const float Width = 0.36f, Depth = 0.42f;
    public const float SlotWidth = 0.118f, SlotDepth = 0.078f, SlotZ = -0.164f;
    public const int StrokeSegments = 16;
    public static readonly float[] SlotX = { -0.12f, 0f, 0.12f };
    public static readonly string[] SlotNames = { "TYPE", "TREATMENT", "EVIDENCE" };
    public static readonly string[] SlotHints = { "TYPE card", "TREATMENT card", "record tag" };

    public static readonly Color TypeColor = new Color32(48, 108, 196, 255);
    public static readonly Color TreatmentColor = new Color32(214, 120, 36, 255);
    public static readonly Color EvidenceColor = new Color32(104, 84, 160, 255);
    public static readonly Color FitColor = new Color32(64, 168, 100, 255);
    public static readonly Color WrongColor = new Color32(214, 64, 64, 255);
    public static readonly Color Neutral = new Color(0.25f, 0.23f, 0.21f, 0.35f);
    public static readonly Color InkYellow = new Color(1f, 0.88f, 0.12f, 0.5f);
    // Ink and hover bands draw before the printed text (TextMeshPro is 3000), so a highlight marks a line without covering it.
    private const int UnderTextQueue = 2980;
    private static readonly Color PaperColor = new Color32(252, 249, 240, 255);
    private static readonly Color HeaderColor = new Color32(30, 52, 74, 255);
    private static readonly Color InkColor = new Color32(36, 32, 30, 255);
    private static readonly Color MutedInk = new Color32(96, 90, 82, 255);

    internal static Color CollectionPaperColor => PaperColor;
    internal static Color CollectionHeaderColor => HeaderColor;
    internal static Color CollectionInkColor => InkColor;

    public Transform Hinge { get; private set; }
    public Transform Root { get; private set; }
    public string EntryId { get; private set; }

    private sealed class Line
    {
        public float z;
        public Transform[] segments;
        public Transform sourceTag;
        public Material sourceMaterial;
        public TextMeshPro sourceText;
        public TextMeshPro value;
        public Material hover;
    }

    private sealed class Place
    {
        public Transform anchor;
        public Transform pill;
        public List<Transform> dashes;
        public Material dashMaterial;
        public TextMeshPro hint;
        public TextMeshPro savedValue;
        public Transform tick, cross;
        public Color color;
        public float flashUntil;
        public Color flashColor;
        public int result; // 0 unknown, 1 right, -1 wrong
    }

    private readonly Dictionary<string, Line> lines = new Dictionary<string, Line>();
    private float rowStep = 0.042f;
    private readonly Place[] places = new Place[3];
    private TextMeshPro placedLabel;
    private Material inkMaterial;
    private Transform nibGlint;

    public static Color KindColor(AnnotationSlot kind) =>
        kind == AnnotationSlot.Type ? TypeColor : kind == AnnotationSlot.Treatment ? TreatmentColor : EvidenceColor;

    public static AnnotationPaper Build(ToolkitBuilder b, Transform parent, Vector3 localPosition, AnnotationEntryDefinition entry, string badge)
    {
        var paper = new AnnotationPaper { EntryId = entry.id };
        // Hinged along its top edge so it can flip like a clipboard page.
        paper.Hinge = b.Node("PaperHinge " + entry.id, parent, localPosition + new Vector3(0f, 0f, Depth * 0.5f), Quaternion.identity);
        Transform root = b.Node("AnnotationPaper " + entry.id, paper.Hinge, new Vector3(0f, 0f, -Depth * 0.5f), Quaternion.identity);
        paper.Root = root;
        b.Box("Paper", root, Vector3.zero, new Vector3(Width, 0.002f, Depth), b.Lit(PaperColor, "Paper"));

        const float y = 0.0015f;
        b.Box("Header", root, new Vector3(0f, 0.0011f, 0.18f), new Vector3(Width, 0.0004f, 0.06f), b.Lit(HeaderColor, "Paper Header"));
        b.Text(root, new Vector3(-0.025f, y, 0.188f), new Vector2(Width - 0.1f, 0.03f), entry.title, PaperColor, 0.42f, TextAlignmentOptions.Left, true);
        string meta = string.IsNullOrWhiteSpace(entry.reference) ? entry.issuer : entry.issuer + "  ·  " + entry.reference;
        b.Text(root, new Vector3(-0.025f, y, 0.163f), new Vector2(Width - 0.1f, 0.016f), meta ?? string.Empty, new Color32(190, 204, 216, 255), 0.22f, TextAlignmentOptions.Left);
        if (!string.IsNullOrEmpty(badge))
        {
            b.Disc("Badge", root, new Vector3(Width * 0.5f - 0.035f, 0.0016f, 0.18f), 0.044f, 0.0008f, b.Lit(new Color32(241, 194, 108, 255), "Paper Badge"));
            b.Text(root, new Vector3(Width * 0.5f - 0.035f, 0.0025f, 0.18f), new Vector2(0.04f, 0.022f), badge, HeaderColor, 0.3f, TextAlignmentOptions.Center, true);
        }
        b.Text(root, new Vector3(0f, y, 0.133f), new Vector2(Width - 0.03f, 0.014f), "Swipe the source line, then add its three notes.", MutedInk, 0.16f, TextAlignmentOptions.Left);

        paper.inkMaterial = b.Clear(InkYellow, "Highlighter Ink");
        paper.inkMaterial.renderQueue = UnderTextQueue;
        int count = entry.lines != null ? entry.lines.Count : 0;
        float step = count > 0 ? Mathf.Min(0.042f, 0.222f / count) : 0.042f;
        paper.rowStep = step;
        for (int i = 0; i < count; i++)
        {
            ReviewLineDefinition row = entry.lines[i];
            if (row == null || string.IsNullOrEmpty(row.id)) continue;
            float z = 0.098f - i * step;
            var line = new Line { z = z, segments = new Transform[StrokeSegments] };
            b.Box("Rule", root, new Vector3(0f, 0.0011f, z - step * 0.47f), new Vector3(Width - 0.03f, 0.0002f, 0.0008f), b.Lit(new Color32(226, 220, 206, 255), "Row Rule"));
            // A pale blue band only while the finger is on the row (never yellow before inking).
            line.hover = b.Clear(new Color(0.35f, 0.6f, 1f, 0f), "Row Hover");
            line.hover.renderQueue = UnderTextQueue;
            b.Box("Hover", root, new Vector3(0f, 0.00115f, z), new Vector3(Width - 0.02f, 0.0002f, step * 0.9f), line.hover);
            // Hand-drawn stroke: uneven segments revealed as the marker passes.
            var random = new System.Random(StableHash(row.id));
            float left = -Width * 0.46f, segment = Width * 0.92f / StrokeSegments;
            for (int s = 0; s < StrokeSegments; s++)
            {
                float jitter = ((float)random.NextDouble() - 0.5f) * 0.003f;
                float thick = step * (0.64f + (float)random.NextDouble() * 0.12f);
                Transform node = b.Node("Ink " + s, root, new Vector3(left + segment * s, 0.0012f + s * 0.000004f, z + jitter), Quaternion.identity);
                b.Box("Seg", node, new Vector3(segment * 0.5f, 0f, 0f), new Vector3(segment * 1.08f, 0.0003f, thick), paper.inkMaterial);
                node.localScale = new Vector3(0f, 1f, 1f);
                node.gameObject.SetActive(false);
                line.segments[s] = node;
            }
            // Fixed sizes for every row: long values wrap instead of shrinking to tiny print.
            b.Text(root, new Vector3(-0.105f, y + 0.0005f, z), new Vector2(0.12f, step * 0.99f), row.label, MutedInk, RowFont(count), TextAlignmentOptions.Left, false, null, false);
            line.value = b.Text(root, new Vector3(0.065f, y + 0.0005f, z), new Vector2(0.2f, step * 0.99f), row.value, InkColor, RowFont(count), TextAlignmentOptions.Right, false, null, false);
            // SOURCE flag that pops in at the margin once the line is marked.
            line.sourceTag = b.Node("SourceTag", root, new Vector3(-Width * 0.5f - 0.002f, 0.0018f, z), Quaternion.identity);
            line.sourceMaterial = b.Lit(new Color32(255, 214, 40, 255), "Source Flag");
            b.Box("Flag", line.sourceTag, new Vector3(-0.019f, 0f, 0f), new Vector3(0.038f, 0.0008f, 0.017f), line.sourceMaterial);
            line.sourceText = b.Text(line.sourceTag, new Vector3(-0.019f, 0.0006f, 0f), new Vector2(0.036f, 0.013f), "SOURCE", InkColor, 0.12f, TextAlignmentOptions.Center, true, null, false);
            line.sourceTag.localScale = Vector3.zero;
            b.Hit(root, new Vector3(0f, 0.004f, z), new Vector3(Width * 0.98f, 0.014f, step * 0.98f), ToolkitTarget.Kind.Line, row.id, AnnotationSlot.Type);
            paper.lines[row.id] = line;
        }
        paper.nibGlint = b.Node("Wet Ink", root, Vector3.zero, Quaternion.identity);
        b.Disc("Glint", paper.nibGlint, Vector3.zero, 0.012f, 0.0003f, b.Clear(new Color(1f, 1f, 0.75f, 0.75f), "Wet Ink"));
        paper.nibGlint.gameObject.SetActive(false);

        Material white = b.Lit(Color.white, "Badge Icon");
        for (int s = 0; s < 3; s++)
        {
            var kind = (AnnotationSlot)s;
            var place = new Place { color = KindColor(kind) };
            place.anchor = b.Node("Place " + SlotNames[s], root, new Vector3(SlotX[s], 0.0012f, SlotZ), Quaternion.identity);
            place.dashMaterial = b.Clear(Neutral, "Place Dashes " + s);
            place.dashes = b.Dashes("Dash", place.anchor, SlotWidth, SlotDepth, 0.0004f, place.dashMaterial);
            place.hint = b.Text(place.anchor, new Vector3(0f, 0.0006f, -0.004f), new Vector2(SlotWidth - 0.012f, 0.03f),
                "Drop a\n" + SlotHints[s], new Color(0.45f, 0.42f, 0.38f, 0.65f), 0.18f);
            place.savedValue = b.Text(place.anchor, new Vector3(0f, 0.0012f, -0.004f),
                new Vector2(SlotWidth - 0.012f, SlotDepth - 0.012f), string.Empty, InkColor, 0.11f);
            place.savedValue.gameObject.SetActive(false);
            place.pill = b.Node("Pill", place.anchor, new Vector3(0f, 0.0006f, SlotDepth * 0.5f + 0.011f), Quaternion.identity);
            b.Box("PillBody", place.pill, Vector3.zero, new Vector3(SlotWidth * 0.86f, 0.0008f, 0.016f), b.Lit(place.color, "Pill " + s));
            b.Text(place.pill, new Vector3(0f, 0.0006f, 0f), new Vector2(SlotWidth * 0.82f, 0.013f), SlotNames[s], Color.white, 0.16f, TextAlignmentOptions.Center, true);
            place.tick = b.Node("Right", place.anchor, new Vector3(SlotWidth * 0.5f - 0.004f, 0.007f, SlotDepth * 0.5f - 0.002f), Quaternion.identity);
            b.Disc("Disc", place.tick, Vector3.zero, 0.026f, 0.001f, b.Lit(FitColor, "Right Badge"));
            b.CheckIcon(place.tick, new Vector3(0f, 0.0008f, 0f), 0.02f, white);
            place.tick.gameObject.SetActive(false);
            place.cross = b.Node("Wrong", place.anchor, new Vector3(SlotWidth * 0.5f - 0.004f, 0.007f, SlotDepth * 0.5f - 0.002f), Quaternion.identity);
            b.Disc("Disc", place.cross, Vector3.zero, 0.026f, 0.001f, b.Lit(WrongColor, "Wrong Badge"));
            b.CrossIcon(place.cross, new Vector3(0f, 0.0008f, 0f), 0.02f, white);
            place.cross.gameObject.SetActive(false);
            b.Hit(place.anchor, new Vector3(0f, 0.006f, 0.004f), new Vector3(SlotWidth + 0.02f, 0.016f, SlotDepth + 0.034f), ToolkitTarget.Kind.Slot, SlotNames[s], kind);
            paper.places[s] = place;
        }

        paper.placedLabel = b.Text(root, new Vector3(0f, 0.012f, -0.198f), new Vector2(0.12f, 0.02f), "Placed!", FitColor, 0.26f, TextAlignmentOptions.Center, true);
        paper.placedLabel.gameObject.SetActive(false);
        return paper;
    }

    /// <summary>Row type size: larger when a record has few rows (the short records of Level 2), never below the original 0.17.</summary>
    private static float RowFont(int rows) => rows <= 4 ? 0.205f : 0.17f;

    /// <summary>Applies saved annotation content to this same 3D paper model.</summary>
    public void SetSavedAnnotations(AnnotationDefinition definition, AnnotationEntryDefinition entry,
        AnnotationEntryState saved, bool visible, bool cardsAttached = false)
    {
        string highlight = visible ? saved?.highlightLineId : null;
        ClearStrokes(highlight);
        if (highlight != null && HasLine(highlight))
        {
            SetStroke(highlight, 0f, 1f);
            SetSourceTag(highlight, 1f);
        }

        string[] values =
        {
            visible ? SavedOptionLabel(definition, AnnotationSlot.Type, saved?.typeId, entry) : null,
            visible ? SavedOptionLabel(definition, AnnotationSlot.Treatment, saved?.treatmentId, entry) : null,
            visible ? SavedEvidenceLabel(entry, saved) : null
        };
        for (int i = 0; i < places.Length; i++)
        {
            Place place = places[i];
            bool showValue = visible && !cardsAttached && !string.IsNullOrWhiteSpace(values[i]);
            place.hint.gameObject.SetActive(!showValue && !(cardsAttached && !string.IsNullOrWhiteSpace(values[i])));
            place.savedValue.text = showValue ? values[i] : string.Empty;
            place.savedValue.gameObject.SetActive(showValue);
        }
    }

    private static string SavedOptionLabel(AnnotationDefinition definition, AnnotationSlot slot, string id,
        AnnotationEntryDefinition entry)
    {
        if (string.IsNullOrEmpty(id)) return null;
        AnnotationOptionDefinition option = definition != null ? definition.FindOption(slot, id, entry) : null;
        return option != null && !string.IsNullOrWhiteSpace(option.label) ? option.label : id;
    }

    private static string SavedEvidenceLabel(AnnotationEntryDefinition entry, AnnotationEntryState saved)
    {
        if (saved?.evidenceIds == null || saved.evidenceIds.Count == 0) return null;
        var labels = new List<string>();
        foreach (string id in saved.evidenceIds)
        {
            AnnotationOptionDefinition option = entry?.evidenceOptions?.Find(item => item != null && item.id == id);
            labels.Add(option != null && !string.IsNullOrWhiteSpace(option.label) ? option.label : id);
        }
        return string.Join("\n+ ", labels);
    }

    /// <summary>Builds the actual sidequest 3D paper in a floating HUD window.</summary>
    public static AnnotationPaperCollectionFace BuildCollectionFace(Transform parent, Transform hudCanvas,
        AnnotationEntryDefinition entry, AnnotationEntryState saved, AnnotationDefinition definition, int index = 0) =>
        AnnotationPaperCollectionFace.Build(parent, hudCanvas, entry, saved, definition, index);

    private static int StableHash(string text)
    {
        unchecked
        {
            int hash = 23;
            foreach (char c in text) hash = hash * 31 + c;
            return hash;
        }
    }

    // =========================================================
    // LINES AND HIGHLIGHTER
    // =========================================================

    public bool HasLine(string lineId) => lineId != null && lines.ContainsKey(lineId);

    /// <summary>A printed row's centre (paper-local z, +z toward the header) and row height.</summary>
    public bool TryLineBand(string lineId, out float z, out float height)
    {
        z = height = 0f;
        if (lineId == null || !lines.TryGetValue(lineId, out Line line)) return false;
        z = line.z;
        height = rowStep;
        return true;
    }

    public bool ContainsPoint(Vector3 world)
    {
        Vector3 p = Root.InverseTransformPoint(world);
        return Mathf.Abs(p.x) <= Width * 0.5f && Mathf.Abs(p.z) <= Depth * 0.5f;
    }

    /// <summary>The printed row under a nib point (null between rows or off the paper).</summary>
    public string AimLine(Vector3 world)
    {
        Vector3 p = Root.InverseTransformPoint(world);
        if (Mathf.Abs(p.x) > Width * 0.49f) return null;
        string best = null;
        float nearest = 0.022f;
        foreach (KeyValuePair<string, Line> pair in lines)
        {
            float distance = Mathf.Abs(p.z - pair.Value.z);
            if (distance > nearest) continue;
            nearest = distance;
            best = pair.Key;
        }
        return best;
    }

    /// <summary>World points at the start and end of a line's stroke.</summary>
    public bool TryLineEnds(string lineId, out Vector3 start, out Vector3 end)
    {
        start = end = Vector3.zero;
        if (Root == null || lineId == null || !lines.TryGetValue(lineId, out Line line)) return false;
        start = Root.TransformPoint(new Vector3(-Width * 0.46f, 0.002f, line.z));
        end = Root.TransformPoint(new Vector3(Width * 0.46f, 0.002f, line.z));
        return true;
    }

    /// <summary>0..1 along a line for a world point (clamped).</summary>
    public float ProgressAt(Vector3 world)
    {
        if (Root == null) return 0f;
        Vector3 local = Root.InverseTransformPoint(world);
        return Mathf.Clamp01((local.x + Width * 0.46f) / (Width * 0.92f));
    }

    /// <summary>Shows the stroke of one line between <paramref name="from"/> and <paramref name="to"/> (0..1).</summary>
    public void SetStroke(string lineId, float from, float to)
    {
        if (lineId == null || !lines.TryGetValue(lineId, out Line line)) return;
        float start = Mathf.Clamp01(Mathf.Min(from, to)), end = Mathf.Clamp01(Mathf.Max(from, to));
        int n = StrokeSegments;
        for (int s = 0; s < n; s++)
        {
            float a = (float)s / n, b = (float)(s + 1) / n;
            float cover = Mathf.Clamp01((Mathf.Min(b, end) - Mathf.Max(a, start)) * n);
            bool on = cover > 0.001f;
            Transform node = line.segments[s];
            if (node.gameObject.activeSelf != on) node.gameObject.SetActive(on);
            if (on) node.localScale = new Vector3(cover, 1f, 1f);
        }
    }

    public void SetInkAlpha(float alpha) =>
        inkMaterial.color = new Color(InkYellow.r, InkYellow.g, InkYellow.b, InkYellow.a * Mathf.Clamp01(alpha));

    /// <summary>A bright wet-ink dot at the nib while inking (null hides it).</summary>
    public void SetNib(string lineId, float progress)
    {
        bool on = lineId != null && lines.TryGetValue(lineId, out Line line);
        if (nibGlint.gameObject.activeSelf != on) nibGlint.gameObject.SetActive(on);
        if (!on) return;
        nibGlint.localPosition = new Vector3(-Width * 0.46f + Width * 0.92f * Mathf.Clamp01(progress), 0.0016f, lines[lineId].z);
        nibGlint.localScale = Vector3.one * (0.9f + 0.25f * Mathf.Sin(Time.unscaledTime * 30f));
    }

    /// <summary>Clears every stroke and SOURCE flag except <paramref name="keepLineId"/>.</summary>
    public void ClearStrokes(string keepLineId = null)
    {
        foreach (KeyValuePair<string, Line> pair in lines)
        {
            if (pair.Key == keepLineId) continue;
            SetStroke(pair.Key, 0f, 0f);
            pair.Value.sourceTag.localScale = Vector3.zero;
            pair.Value.value.transform.localScale = Vector3.one;
        }
        SetInkAlpha(1f);
    }

    public void SetSourceTag(string lineId, float scale)
    {
        if (lineId != null && lines.TryGetValue(lineId, out Line line)) line.sourceTag.localScale = Vector3.one * scale;
    }

    /// <summary>Colors the selected source flag with the last challenge result.</summary>
    public void SetHighlightResult(string lineId, int result)
    {
        foreach (KeyValuePair<string, Line> pair in lines)
        {
            bool selected = pair.Key == lineId;
            int value = selected ? result : 0;
            pair.Value.sourceMaterial.color = value > 0 ? FitColor : value < 0 ? WrongColor : new Color32(255, 214, 40, 255);
            pair.Value.sourceText.color = value == 0 ? InkColor : Color.white;
        }
    }

    public Transform HighlightResultBadge(string lineId) =>
        lineId != null && lines.TryGetValue(lineId, out Line line) ? line.sourceTag : null;

    public void BumpValue(string lineId, float scale)
    {
        if (lineId != null && lines.TryGetValue(lineId, out Line line)) line.value.transform.localScale = Vector3.one * scale;
    }

    public void SetHover(string lineId)
    {
        foreach (KeyValuePair<string, Line> pair in lines)
        {
            float a = pair.Key == lineId ? 0.16f : 0f;
            Color c = pair.Value.hover.color;
            if (!Mathf.Approximately(c.a, a)) pair.Value.hover.color = new Color(c.r, c.g, c.b, a);
        }
    }

    // =========================================================
    // NOTE PLACES
    // =========================================================

    /// <summary>World pose of a card in a note place (index/count fan out a two-record bundle).</summary>
    public void SlotPose(AnnotationSlot slot, int index, int count, out Vector3 position, out Quaternion rotation)
    {
        Transform anchor = places[(int)slot].anchor;
        float spread = count > 1 ? (index - (count - 1) * 0.5f) : 0f;
        position = anchor.TransformPoint(new Vector3(spread * 0.018f, 0.003f + index * 0.0016f, -spread * 0.008f));
        rotation = anchor.rotation * Quaternion.Euler(0f, spread * 6f, 0f);
    }

    /// <summary>Where the evidence clip bites (top edge of the EVIDENCE place).</summary>
    public void ClipPose(out Vector3 position, out Quaternion rotation)
    {
        Transform anchor = places[(int)AnnotationSlot.Evidence].anchor;
        position = anchor.TransformPoint(new Vector3(0f, 0.007f, SlotDepth * 0.36f));
        rotation = anchor.rotation;
    }

    public Vector3 SlotCenter(AnnotationSlot slot) => places[(int)slot].anchor.position;

    /// <summary>Shows or hides the faint "Drop a ..." text of a place.</summary>
    public void SetFilled(AnnotationSlot slot, bool filled)
    {
        TextMeshPro hint = places[(int)slot].hint;
        if (hint != null && hint.gameObject.activeSelf == filled) hint.gameObject.SetActive(!filled);
    }

    /// <summary>Result of the last check for a place: 1 right, -1 wrong, 0 unknown (drawn badges).</summary>
    public void SetResult(AnnotationSlot slot, int result)
    {
        Place place = places[(int)slot];
        place.result = result;
        place.tick.gameObject.SetActive(result > 0);
        place.cross.gameObject.SetActive(result < 0);
    }

    public int Result(AnnotationSlot slot) => places[(int)slot].result;

    public Transform ResultBadge(AnnotationSlot slot) =>
        places[(int)slot].result > 0 ? places[(int)slot].tick : places[(int)slot].cross;

    /// <summary>Briefly blinks a place (red: missing or wrong kind).</summary>
    public void Flash(AnnotationSlot slot, Color color, float seconds)
    {
        Place place = places[(int)slot];
        place.flashColor = color;
        place.flashUntil = Time.unscaledTime + seconds;
    }

    /// <summary>
    /// Per-frame look of the places. <paramref name="fitting"/> is the place a
    /// held card fits: a compatible destination only, never a correct answer.
    /// <paramref name="hot"/> brightens it while the card is over it.
    /// </summary>
    public void Animate(AnnotationSlot? fitting, bool hot)
    {
        float now = Time.unscaledTime;
        for (int s = 0; s < 3; s++)
        {
            Place place = places[s];
            bool fit = fitting.HasValue && (int)fitting.Value == s;
            Color c = place.result > 0 ? FitColor : place.result < 0 ? WrongColor : Neutral;
            float scale = 1f;
            if (fit)
            {
                float wave = 0.5f + 0.5f * Mathf.Sin(now * 9f);
                c = hot ? FitColor : Color.Lerp(place.color, Color.white, 0.2f * wave);
                c.a = 1f;
                scale = hot ? 1.07f : 1f + 0.02f * wave;
            }
            if (now < place.flashUntil)
            {
                c = Color.Lerp(c, place.flashColor, Mathf.PingPong(now * 8f, 1f));
                c.a = 1f;
            }
            if (place.dashMaterial.color != c) place.dashMaterial.color = c;
            place.anchor.localScale = Vector3.one * scale;
            place.pill.localPosition = new Vector3(0f, 0.0006f + (fit ? 0.003f * Mathf.Abs(Mathf.Sin(now * 7f)) : 0f), SlotDepth * 0.5f + 0.011f);
            for (int i = 0; i < place.dashes.Count; i++)
            {
                Transform dash = place.dashes[i];
                float grow = fit ? 1f + 0.8f * Mathf.Max(0f, Mathf.Sin(now * 10f - i * 0.6f)) : 1f;
                Vector3 size = dash.localScale;
                if (size.x > size.z) size.z = 0.0022f * grow;
                else size.x = 0.0022f * grow;
                dash.localScale = size;
            }
        }
    }

    /// <summary>Shows the "Placed!" chip under a place (hidden again by ShowPlaced(null)).</summary>
    public void ShowPlaced(AnnotationSlot? slot, float scale = 1f)
    {
        if (placedLabel == null) return;
        bool on = slot.HasValue;
        placedLabel.gameObject.SetActive(on);
        if (!on) return;
        placedLabel.transform.localPosition = new Vector3(SlotX[(int)slot.Value], 0.006f, SlotZ - SlotDepth * 0.5f - 0.01f);
        placedLabel.transform.localScale = Vector3.one * scale;
    }

    public void SetVisible(bool visible)
    {
        if (Hinge != null && Hinge.gameObject.activeSelf != visible) Hinge.gameObject.SetActive(visible);
    }
}
