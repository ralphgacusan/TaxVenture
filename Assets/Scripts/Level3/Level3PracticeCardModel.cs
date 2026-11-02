using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// PURPOSE:
/// The protected practice budget (T03) as a physical desk object: a navy
/// clipboard with a brass clip holding a cream card. Printed on it:
/// PRACTICE · OFFICE BUDGET, "Not your money", three amounts the calculator
/// can take (Budget 100.00, Paper order 20.00, Courier 8.00), two slots where
/// tapes are clipped, and one line of plain guidance.
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

    private sealed class Slot
    {
        public string DestinationId;
        public Vector3 Center;
        public GameObject Frame;
        public GameObject Tape;
        public TextMeshPro Print;
    }

    public Transform Root { get; private set; }
    /// <summary>The whole card's tap target (resting on the desk: tap to pick up the tools).</summary>
    public Collider BodyTarget { get; private set; }
    /// <summary>Local bounds (for on-screen fitting).</summary>
    public Bounds UnitBounds { get; private set; }

    private readonly Level3DeskProps props;
    private readonly Dictionary<Collider, string> sourceTargets = new Dictionary<Collider, string>();
    private readonly Dictionary<string, GameObject> sourceFrames = new Dictionary<string, GameObject>();
    private readonly Dictionary<Collider, Slot> slotTargets = new Dictionary<Collider, Slot>();
    private readonly Dictionary<string, Slot> slots = new Dictionary<string, Slot>();
    private TextMeshPro message;
    private TextMeshPro status;
    private float surface;

    private Level3PracticeCardModel(Level3DeskProps props)
    {
        this.props = props;
    }

    public static Level3PracticeCardModel Build(Transform parent, Level3DeskProps props)
    {
        var model = new Level3PracticeCardModel(props);
        model.Create(parent);
        return model;
    }

    private void Create(Transform parent)
    {
        var root = new GameObject("Practice Card");
        root.layer = parent.gameObject.layer;
        root.transform.SetParent(parent, false);
        Root = root.transform;

        // Clipboard board, card, brass clip.
        props.Box("Clipboard", Root, new Vector3(0f, 0.002f, 0f), new Vector3(CardWidth + 0.014f, 0.004f, CardLength + 0.022f), Level3DeskProps.Navy);
        props.Box("Card", Root, new Vector3(0f, 0.00475f, -0.004f), new Vector3(CardWidth, 0.0015f, CardLength - 0.008f), Level3DeskProps.Paper);
        props.Box("Clip Base", Root, new Vector3(0f, 0.0065f, CardLength * 0.5f + 0.002f), new Vector3(0.05f, 0.004f, 0.016f), Level3DeskProps.Brass);
        props.Box("Clip Lever", Root, new Vector3(0f, 0.0095f, CardLength * 0.5f - 0.004f), new Vector3(0.034f, 0.0025f, 0.012f), Level3DeskProps.BrassSoft);
        surface = 0.0056f;
        float printY = surface + 0.0003f;
        float innerW = CardWidth * 0.88f;

        // Header band and note (card top edge is at z = 0.097).
        props.Box("Header", Root, new Vector3(0f, surface + 0.0002f, 0.085f), new Vector3(CardWidth * 0.96f, 0.0006f, 0.028f), Level3DeskProps.Navy);
        props.Print("Title", Root, new Vector3(0f, surface + 0.0007f, 0.085f), innerW, 0.025f, "PRACTICE\nOFFICE BUDGET", Level3DeskProps.Cream, bold: true);
        props.Print("Note", Root, new Vector3(0f, printY, 0.064f), innerW, 0.011f, "Practice only · No score", Level3DeskProps.Ink);

        // Amount rows (sources): 0.049, 0.029, 0.009.
        float z = 0.049f;
        foreach (Level3PracticeSource source in Level3PracticeRules.CardSources)
        {
            var center = new Vector3(0f, printY, z);
            props.Print(source.SourceId + " Label", Root, center + new Vector3(-innerW * 0.24f, 0f, 0f), innerW * 0.5f, 0.017f, source.Label,
                Level3DeskProps.Ink, TextAlignmentOptions.Left);
            props.Print(source.SourceId + " Amount", Root, center + new Vector3(innerW * 0.25f, 0f, 0f), innerW * 0.48f, 0.017f,
                Level3Calculator.Format(source.Amount), Level3DeskProps.Ink, TextAlignmentOptions.Right, bold: true);
            GameObject frame = props.Frame(source.SourceId + " Selected", Root, center, innerW + 0.004f, 0.017f, 0.0018f, Level3DeskProps.Highlight);
            frame.SetActive(false);
            sourceFrames[source.SourceId] = frame;
            sourceTargets[props.Target(source.SourceId + " Target", Root, center, new Vector3(innerW + 0.006f, 0.012f, 0.019f))] = source.SourceId;
            z -= 0.02f;
        }

        props.Box("Divider", Root, new Vector3(0f, surface + 0.0002f, -0.003f), new Vector3(innerW, 0.0005f, 0.0012f), Level3DeskProps.Brass);

        // Tape slots (destinations): labels at -0.012 / -0.044, slots 0.015 below their labels.
        for (int i = 0; i < Level3PracticeRules.Slots.Length; i++)
        {
            Level3PracticeSlot definition = Level3PracticeRules.Slots[i];
            float labelZ = -0.012f - i * 0.032f;
            props.Print(definition.DestinationId + " Label", Root, new Vector3(0f, printY, labelZ), innerW, 0.014f,
                (i + 1) + "  " + definition.Label, Level3DeskProps.Ink, TextAlignmentOptions.Left, bold: true);
            var center = new Vector3(0f, printY, labelZ - 0.015f);
            props.Frame(definition.DestinationId + " Outline", Root, center, innerW * 0.92f, 0.016f, 0.001f, Level3DeskProps.InkMuted);
            GameObject armed = props.Frame(definition.DestinationId + " Armed", Root, center, innerW * 0.96f, 0.02f, 0.0022f, Level3DeskProps.Brass);
            armed.SetActive(false);
            var slot = new Slot { DestinationId = definition.DestinationId, Center = center, Frame = armed };
            slots[definition.DestinationId] = slot;
            slotTargets[props.Target(definition.DestinationId + " Target", Root, center + new Vector3(0f, 0f, 0.007f), new Vector3(innerW + 0.006f, 0.014f, 0.03f))] = slot;
        }

        status = props.Print("Status", Root, new Vector3(0f, printY, -0.074f), innerW, 0.012f, "", Level3DeskProps.Ink, TextAlignmentOptions.Center, bold: true);
        message = props.Print("Message", Root, new Vector3(0f, printY, -0.089f), innerW, 0.019f, "",
            Level3DeskProps.Ink, TextAlignmentOptions.Center, wrap: true);

        foreach (TextMeshPro label in Root.GetComponentsInChildren<TextMeshPro>()) label.fontSizeMax = 1.4f;

        // Below the row/slot targets, so those are always the nearest hit.
        BodyTarget = props.Target("Card Target", Root, new Vector3(0f, 0.0025f, 0f), new Vector3(CardWidth + 0.014f, 0.005f, CardLength + 0.022f));
        UnitBounds = new Bounds(new Vector3(0f, 0.006f, 0f), new Vector3(CardWidth + 0.014f, 0.012f, CardLength + 0.03f));
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

    /// <summary>The slot waiting for a second tap to confirm (destination named first); null clears.</summary>
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
            float width = CardWidth * 0.82f;
            props.Box("Paper", slot.Tape.transform, Vector3.zero, new Vector3(width, 0.0012f, 0.017f), Level3DeskProps.Paper);
            props.Box("Shadow", slot.Tape.transform, new Vector3(0.0015f, -0.0007f, -0.0015f), new Vector3(width, 0.0004f, 0.017f), Level3DeskProps.InkMuted);
            props.Box("Clip", slot.Tape.transform, new Vector3(-width * 0.5f + 0.006f, 0.0018f, 0f), new Vector3(0.009f, 0.003f, 0.02f), Level3DeskProps.Brass);
            slot.Print = props.Print("Print", slot.Tape.transform, new Vector3(0.004f, 0.0008f, 0f), width - 0.016f, 0.013f, "",
                Level3DeskProps.Ink, TextAlignmentOptions.Right);
            slot.Print.fontSizeMax = 1.4f;
        }
        string working = lines != null && lines.Count > 1 ? string.Join("  ", Trim(lines)) : "";
        slot.Print.text = (working.Length > 0 ? working + "  " : "") + "<b>= " + Level3Calculator.Format(result) + "</b>";
        return slot.Tape.transform;
    }

    /// <summary>The working lines without the final "= result" line (the strip prints that in bold).</summary>
    private static IEnumerable<string> Trim(IList<string> lines)
    {
        for (int i = 0; i < lines.Count; i++)
            if (!lines[i].StartsWith("= ", System.StringComparison.Ordinal)) yield return lines[i];
    }

    /// <summary>Where a slot's tape lands (the attach flight ends here).</summary>
    public Vector3 SlotWorldPosition(string destinationId) =>
        slots.TryGetValue(destinationId, out Slot slot) ? Root.TransformPoint(slot.Center) : Root.position;
}
