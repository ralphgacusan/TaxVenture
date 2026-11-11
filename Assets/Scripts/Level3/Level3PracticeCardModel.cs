using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// PURPOSE:
/// A desk card (T03) as a physical object: a navy clipboard with a brass clip
/// holding a cream card. Printed from its Level3CardSpec: a header, a short
/// note, the amounts the calculator can take, the slots where tapes are
/// clipped, and one line of plain guidance. Revision: the first-use lesson's
/// example (Level3PracticeRules.LessonCard) uses it; the retired budget
/// practice (PracticeCard) only shows version 1 tapes kept for reference.
///
/// Each amount row and each slot has its own tap target. An attached tape is
/// a real paper strip with a brass clip, printed with its working and result;
/// tapping it offers its result to the calculator. Presentation only:
/// Level3PracticeRules and the chapter command decide what is attached.
/// Local frame as the calculator: lying flat, +Y up, +Z away from the player.
/// </summary>
public sealed class Level3PracticeCardModel
{
    public const float CardWidth = 0.15f;
    public const float CardLength = 0.21f;
    private const float AttachedTapeDepth = 0.026f;
    public static Vector2 AttachedTapeSize => new Vector2(CardWidth * 0.82f, AttachedTapeDepth);
    public const float AmountPoints = 140f;
    public const float AmountWidth = CardWidth * 0.88f;
    public const float AmountHeight = 0.022f;
    public const float ResultPoints = 150f;
    public const float ResultWidth = CardWidth * 0.82f - 0.016f;
    public const float ResultHeight = 0.022f;
    public const float LabelPoints = 112f;
    public const float LabelHeight = 0.018f;
    public const float NotePoints = 82f;
    public const float NoteHeight = 0.012f;
    public const float MessagePoints = 82f;
    public const float MessageHeight = 0.033f;
    /// <summary>Lesson rows: two amounts keep the original spacing; three or four use a tighter pitch and a longer card.</summary>
    public const float LessonPitch = 0.038f;
    public const float CompactPitch = 0.032f;

    /// <summary>A card's printed length (the original 0.21 m unless a lesson lists more than two amounts).</summary>
    public static float LengthOf(Level3CardSpec spec)
    {
        int n = spec?.Sources != null ? spec.Sources.Length : 0;
        if (spec == null || !spec.IsLesson || n <= 2) return CardLength;
        return CardLength + n * CompactPitch - 2f * LessonPitch;
    }

    /// <summary>How an amount prints on the card (a rate as a percentage).</summary>
    public static string Printed(Level3PracticeSource source) =>
        source.Percent && Level3Calculator.TryParse(source.Amount, out decimal fraction)
            ? Level3ComputationRules.Percent(Level3Calculator.Canonical(fraction)) : Level3Calculator.Format(source.Amount);

    private sealed class Slot
    {
        public string DestinationId;
        public Vector3 Center;
        public GameObject Frame;
        public GameObject Tape;
        public TextMeshPro Print;
    }

    public Transform Root { get; private set; }
    /// <summary>The card this model prints (rebuilt, never edited, when the desk needs another).</summary>
    public Level3CardSpec Spec { get; private set; }
    /// <summary>The whole card's tap target (resting on the desk: tap to pick up the tools).</summary>
    public Collider BodyTarget { get; private set; }
    public Collider MoveHandle { get; private set; }
    public Collider[] MoveBorders { get; private set; }
    public bool IsMoveHandle(Collider target) => target == BodyTarget || target == MoveHandle || (MoveBorders != null && System.Array.IndexOf(MoveBorders, target) >= 0);
    /// <summary>Local bounds (for on-screen fitting).</summary>
    public Bounds UnitBounds { get; private set; }

    private readonly Level3DeskProps props;
    private readonly Dictionary<Collider, string> sourceTargets = new Dictionary<Collider, string>();
    private readonly Dictionary<string, GameObject> sourceFrames = new Dictionary<string, GameObject>();
    private readonly Dictionary<Collider, Slot> slotTargets = new Dictionary<Collider, Slot>();
    private readonly Dictionary<string, Slot> slots = new Dictionary<string, Slot>();
    private TextMeshPro message;
    private TextMeshPro status;
    private TextMeshPro note;
    private float surface;
    private Transform settling;
    private float settleAt;
    private Vector3 settleScale, settlePosition;
    public void FinishSettle()
    {
        if (settling != null)
        {
            settling.localScale = settleScale;
            settling.localPosition = settlePosition;
        }
        settling = null;
    }
    public void PopSlot(string destinationId)
    {
        if (!slots.TryGetValue(destinationId, out Slot slot) || slot.Tape == null) return;
        FinishSettle();
        settling = slot.Tape.transform; settleScale = settling.localScale; settleAt = Time.unscaledTime;
        settlePosition = settling.localPosition;
        settling.localPosition = settlePosition + Level3CalculatorModel.PlacementOffset(0f);
    }
    public void Tick()
    {
        if (settling == null) return;
        float t = Mathf.Clamp01((Time.unscaledTime - settleAt) / 0.22f);
        settling.localScale = Level3CalculatorModel.PlacementScale(settleScale, t);
        settling.localPosition = settlePosition + Level3CalculatorModel.PlacementOffset(t);
        if (t >= 1f) FinishSettle();
    }

    private Level3PracticeCardModel(Level3DeskProps props, Level3CardSpec spec)
    {
        this.props = props;
        Spec = spec;
    }

    public static Level3PracticeCardModel Build(Transform parent, Level3DeskProps props, Level3CardSpec spec)
    {
        var model = new Level3PracticeCardModel(props, spec ?? Level3PracticeRules.PracticeCard);
        model.Create(parent);
        return model;
    }

    /// <summary>Removes the card's objects (the desk builds the next card in its place).</summary>
    public void Destroy()
    {
        if (Root != null) Object.Destroy(Root.gameObject);
        Root = null;
    }

    private void Create(Transform parent)
    {
        var root = new GameObject(Spec.IsLesson ? "Example Card" : "Practice Card");
        root.layer = parent.gameObject.layer;
        root.transform.SetParent(parent, false);
        Root = root.transform;
        bool lesson = Spec.IsLesson;
        float length = LengthOf(Spec);
        // Everything is placed from the card's top or bottom edge, so the original 0.21 m cards print exactly as before.
        float top = length * 0.5f, bottom = -length * 0.5f;

        // Clipboard board, card, brass clip.
        props.Box("Clipboard", Root, new Vector3(0f, 0.002f, 0f), new Vector3(CardWidth + 0.014f, 0.004f, length + 0.022f), Level3DeskProps.Navy);
        props.Box("Card", Root, new Vector3(0f, 0.00475f, -0.004f), new Vector3(CardWidth, 0.0015f, length - 0.008f), Level3DeskProps.Paper);
        props.Box("Clip Base", Root, new Vector3(0f, 0.0065f, top + 0.002f), new Vector3(0.05f, 0.004f, 0.016f), Level3DeskProps.Brass);
        props.Box("Clip Lever", Root, new Vector3(0f, 0.0095f, top - 0.004f), new Vector3(0.034f, 0.0025f, 0.012f), Level3DeskProps.BrassSoft);
        surface = 0.0056f;
        float printY = surface + 0.0003f;
        float innerW = CardWidth * 0.88f;

        // Header band and note (the 0.21 m card's top edge is at z = 0.097).
        props.Box("Header", Root, new Vector3(0f, surface + 0.0002f, top - 0.020f), new Vector3(CardWidth * 0.96f, 0.0006f, 0.028f), Level3DeskProps.Navy);
        props.FixedPrint("Title", Root, new Vector3(0f, surface + 0.0007f, top - 0.020f), innerW, 0.025f, Spec.Title, Level3DeskProps.Cream, 105f, TextAlignmentOptions.Center, bold: true, wrap: true);
        note = props.FixedPrint("Note", Root, new Vector3(0f, printY, top - 0.040f), innerW, NoteHeight,
            Spec.Note, Level3DeskProps.Ink, NotePoints, TextAlignmentOptions.Center);
        MoveHandle = props.Target("Move paper by clip", Root, new Vector3(0f, 0.012f, top - 0.003f), new Vector3(0.058f, 0.012f, 0.022f));
        MoveBorders = new Collider[]
        {
            props.Target("Move paper left edge", Root, new Vector3(-CardWidth * 0.5f - 0.004f, 0.005f, 0f), new Vector3(0.008f, 0.012f, length + 0.01f)),
            props.Target("Move paper right edge", Root, new Vector3(CardWidth * 0.5f + 0.004f, 0.005f, 0f), new Vector3(0.008f, 0.012f, length + 0.01f)),
            props.Target("Move paper bottom edge", Root, new Vector3(0f, 0.005f, bottom - 0.006f), new Vector3(CardWidth + 0.014f, 0.012f, 0.01f))
        };

        // Amount rows (sources): from 0.049 on the 0.21 m card; a lesson with more than two amounts packs them closer.
        bool compact = lesson && Spec.Sources.Length > 2;
        float pitch = !lesson ? 0.02f : compact ? CompactPitch : LessonPitch;
        float labelOffset = compact ? 0.0065f : 0.007f, amountOffset = compact ? -0.0085f : -0.010f;
        float z = top - 0.056f;
        foreach (Level3PracticeSource source in Spec.Sources)
        {
            var center = new Vector3(0f, printY, z);
            props.FixedPrint(source.SourceId + " Label", Root, center + new Vector3(0f, 0f, labelOffset), innerW, LabelHeight, source.Label,
                Level3DeskProps.Ink, LabelPoints, TextAlignmentOptions.Left);
            props.FixedPrint(source.SourceId + " Amount", Root, center + new Vector3(0f, 0f, amountOffset), AmountWidth, AmountHeight,
                Printed(source), Level3DeskProps.Ink, AmountPoints, TextAlignmentOptions.Right, bold: true);
            GameObject frame = props.Frame(source.SourceId + " Selected", Root, center, innerW + 0.004f, lesson ? pitch - (compact ? 0.002f : 0f) : 0.038f, 0.0018f, Level3DeskProps.Highlight);
            frame.SetActive(false);
            sourceFrames[source.SourceId] = frame;
            sourceTargets[props.Target(source.SourceId + " Target", Root, center, new Vector3(innerW + 0.006f, 0.012f, lesson ? pitch : 0.019f))] = source.SourceId;
            z -= pitch;
        }

        float divider = lesson ? z + 0.009f : -0.003f;
        props.Box("Divider", Root, new Vector3(0f, surface + 0.0002f, divider), new Vector3(innerW, 0.0005f, 0.0012f), Level3DeskProps.Brass);

        // Tape slots (destinations): labels at -0.012 / -0.044, slots 0.015 below their labels.
        for (int i = 0; i < Spec.Slots.Length; i++)
        {
            Level3PracticeSlot definition = Spec.Slots[i];
            float labelZ = (lesson ? divider - 0.016f : -0.012f) - i * 0.032f;
            props.FixedPrint(definition.DestinationId + " Label", Root, new Vector3(0f, printY, labelZ), innerW, LabelHeight,
                (lesson ? "" : (i + 1) + "  ") + definition.Label, Level3DeskProps.Ink, LabelPoints, TextAlignmentOptions.Left, bold: true);
            var center = new Vector3(0f, printY, labelZ - 0.021f);
            props.Frame(definition.DestinationId + " Outline", Root, center, innerW * 0.96f, 0.03f, 0.001f, Level3DeskProps.InkMuted);
            GameObject armed = props.Frame(definition.DestinationId + " Armed", Root, center, innerW, 0.034f, 0.0022f, Level3DeskProps.Brass);
            armed.SetActive(false);
            var slot = new Slot { DestinationId = definition.DestinationId, Center = center, Frame = armed };
            slots[definition.DestinationId] = slot;
            slotTargets[props.Target(definition.DestinationId + " Target", Root, center + new Vector3(0f, 0.002f, 0f), new Vector3(innerW + 0.006f, 0.014f, 0.04f))] = slot;
        }

        // Status replaces the example note, leaving room for readable guidance.
        status = props.FixedPrint("Status", Root, new Vector3(0f, printY, top - 0.040f), innerW, NoteHeight, "",
            Level3DeskProps.Ink, NotePoints, TextAlignmentOptions.Center, bold: true);
        message = props.FixedPrint("Message", Root, new Vector3(0f, printY, bottom + 0.018f), innerW, MessageHeight, "",
            Level3DeskProps.Ink, MessagePoints, TextAlignmentOptions.Center, wrap: true);

        foreach (TextMeshPro label in Root.GetComponentsInChildren<TextMeshPro>())
            if (label.enableAutoSizing) label.fontSizeMax = 1.4f;

        // Below the row/slot targets, so those are always the nearest hit.
        BodyTarget = props.Target("Card Target", Root, new Vector3(0f, 0.0025f, 0f), new Vector3(CardWidth + 0.014f, 0.005f, length + 0.022f));
        UnitBounds = new Bounds(new Vector3(0f, 0.006f, 0f), new Vector3(CardWidth + 0.014f, 0.012f, length + 0.03f));
    }

    public IEnumerable<KeyValuePair<Collider, string>> SourceTargets => sourceTargets;

    public IEnumerable<KeyValuePair<Collider, string>> TapeTargets
    {
        get { foreach (var target in slotTargets) yield return new KeyValuePair<Collider, string>(target.Key, target.Value.DestinationId); }
    }

    public bool TrySource(Collider collider, out string sourceId) => sourceTargets.TryGetValue(collider, out sourceId);

    public bool TrySlot(Collider collider, out string destinationId)
    {
        destinationId = null;
        if (collider == null || !slotTargets.TryGetValue(collider, out Slot slot)) return false;
        destinationId = slot.DestinationId;
        return true;
    }

    /// <summary>Highlights the amount in use (a card row, or an attached tape by its slot); null clears.</summary>
    public void SetSelected(string sourceId)
    {
        foreach (KeyValuePair<string, GameObject> frame in sourceFrames) frame.Value.SetActive(frame.Key == sourceId);
    }

    /// <summary>Neutral drop/placement hover; null clears.</summary>
    public void SetArmed(string destinationId)
    {
        foreach (Slot slot in slots.Values) slot.Frame.SetActive(slot.DestinationId == destinationId);
    }

    public void SetMessage(string text)
    {
        if (message != null && message.text != (text ?? "")) message.text = text ?? "";
    }

    public void SetStatus(string text)
    {
        if (status != null && status.text != (text ?? "")) status.text = text ?? "";
        if (note != null) note.gameObject.SetActive(string.IsNullOrEmpty(text));
    }

    public bool HasTape(string destinationId) => slots.TryGetValue(destinationId, out Slot slot) && slot.Tape != null;

    /// <summary>Clips a tape into its slot (a paper strip with a brass clip), printed with its working and result.</summary>
    public Transform ShowTape(string destinationId, IList<string> lines, string result)
    {
        if (!slots.TryGetValue(destinationId, out Slot slot)) return null;
        if (slot.Tape == null)
        {
            slot.Tape = new GameObject(destinationId + " Tape");
            slot.Tape.layer = Root.gameObject.layer;
            slot.Tape.transform.SetParent(Root, false);
            slot.Tape.transform.localPosition = slot.Center + new Vector3(0f, 0.001f, 0f);
            float width = AttachedTapeSize.x;
            props.Box("Paper", slot.Tape.transform, Vector3.zero, new Vector3(width, 0.0012f, AttachedTapeDepth), Level3DeskProps.Paper);
            props.Box("Shadow", slot.Tape.transform, new Vector3(0.0015f, -0.0007f, -0.0015f), new Vector3(width, 0.0004f, AttachedTapeDepth), Level3DeskProps.InkMuted);
            props.Box("Clip", slot.Tape.transform, new Vector3(-width * 0.5f + 0.006f, 0.0018f, 0f), new Vector3(0.009f, 0.003f, 0.02f), Level3DeskProps.Brass);
            slot.Print = props.FixedPrint("Print", slot.Tape.transform, new Vector3(0.004f, 0.0008f, 0f), ResultWidth, ResultHeight, "",
                Level3DeskProps.Ink, ResultPoints, TextAlignmentOptions.Right, bold: true);
        }
        slot.Print.text = "<b>" + Level3Calculator.Format(result) + "</b>";
        return slot.Tape.transform;
    }

    /// <summary>Takes a clipped tape off its slot again (Arthur's demonstration puts the example back for the player).</summary>
    public void RemoveTape(string destinationId)
    {
        if (!slots.TryGetValue(destinationId ?? "", out Slot slot) || slot.Tape == null) return;
        if (settling == slot.Tape.transform) settling = null;
        Object.Destroy(slot.Tape);
        slot.Tape = null;
        slot.Print = null;
    }

    /// <summary>The working lines without the final "= result" line (the strip prints that in bold).</summary>
    private static IEnumerable<string> Trim(IList<string> lines)
    {
        for (int i = 0; i < lines.Count; i++)
            if (!lines[i].StartsWith("= ", System.StringComparison.Ordinal)) yield return lines[i];
    }

    /// <summary>Where a slot's tape lands (the attach flight ends here).</summary>
    public Vector3 SlotWorldPosition(string destinationId) =>
        slots.TryGetValue(destinationId, out Slot slot) ? Root.TransformPoint(slot.Center + Vector3.up * 0.001f) : Root.position;
}
