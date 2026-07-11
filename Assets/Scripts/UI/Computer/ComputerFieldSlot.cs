using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// PURPOSE:
/// One assignable field slot in the calculator (e.g. "Gross Income" input
/// box). Implements the brief's click-to-assign interaction: the slot shows
/// its current value (or "Click to assign" if empty), and clicking it while
/// a source value is "selected" (see ComputerUI.SelectedSourceValue) fills
/// it in and turns its border green.
///
/// RESPONSIBILITIES:
/// - Display its current value / placeholder text
/// - On click, ask ComputerUI to assign whatever is currently selected into
///   this slot
/// - Show green/red/neutral border state via Image color, matching design
///   doc's "If correct: green border. If incorrect: red border."
///
/// CONNECTS WITH:
/// - ComputerUI: owns the "currently selected source value" and the overall
///   validation/calculate flow; this script just represents ONE slot.
/// </summary>
public class ComputerFieldSlot : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI valueText;
    [SerializeField] private Image borderImage;
    [SerializeField] private Button slotButton;

    public float CurrentValue { get; private set; }
    public bool IsFilled { get; private set; }

    private ComputerUI owner;
    private string slotId;

    public void Initialize(ComputerUI owningUI, string id)
    {
        owner = owningUI;
        slotId = id;
        slotButton.onClick.AddListener(() => owner.OnSlotClicked(slotId));
        Clear();
    }

    public void AssignValue(float value)
    {
        CurrentValue = value;
        IsFilled = true;
        valueText.text = $"\u20b1{value:N0}";
        SetBorder(Color.green);
    }

    public void Clear()
    {
        CurrentValue = 0f;
        IsFilled = false;
        valueText.text = "Click a source, then click here";
        SetBorder(Color.white);
    }

    public void FlashIncorrect()
    {
        SetBorder(Color.red);
    }

    private void SetBorder(Color color)
    {
        if (borderImage != null) borderImage.color = color;
    }
}