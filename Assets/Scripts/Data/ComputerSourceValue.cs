/// <summary>
/// PURPOSE:
/// Represents one draggable/clickable source value in the Computer UI,
/// now carrying which destination slot it's actually correct for — needed
/// for real validation (green/red borders) instead of universal acceptance.
///
/// CONNECTS WITH:
/// - ComputerUI: builds a list of these instead of raw (label, float) pairs
/// - ComputerFieldSlot: compares its own slotId against CorrectSlotId
/// </summary>
public class ComputerSourceValue
{
    public string Label;
    public float Value;
    public string CorrectSlotId;

    public ComputerSourceValue(string label, float value, string correctSlotId)
    {
        Label = label;
        Value = value;
        CorrectSlotId = correctSlotId;
    }
}