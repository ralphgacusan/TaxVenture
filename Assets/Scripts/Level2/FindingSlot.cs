using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// PURPOSE:
/// One fill-in box of a finding note on the case folder's Findings page
/// ("Entry to check", "Clue", "Reason"). It is a page-1 row (same prefab,
/// paper style and font) turned into a value destination that accepts a
/// whole GROUP of rows (any line of a summary page, any line of a record),
/// so a compatible box never hints at the answer. Tap a row on another page
/// to pick it up, turn to Findings and tap the box (or drag on the same page),
/// exactly like other folder fields. No typing, no precision drawing.
/// </summary>
public sealed class FindingSlot : MonoBehaviour, IDataValueDestination, IPointerDownHandler, IPointerClickHandler, IDropHandler
{
    /// <summary>Which values this box takes.</summary>
    public System.Func<DataValue, bool> Accepts;

    /// <summary>Called with the value the box took.</summary>
    public System.Action<DataValue> Received;
    public System.Action Rejected;

    /// <summary>Optional: tapping the box with nothing picked up empties it (office review boxes).</summary>
    public System.Action Cleared;
    private ValueTypeValidationFeedback feedback;

    /// <summary>Turns a fresh page-1 row object into a finding box with a caption above its value (or "?").</summary>
    public static FindingSlot Make(GameObject rowObject, string label, string value)
    {
        FindingSlot existing = rowObject.GetComponent<FindingSlot>();
        if (existing != null)
        {
            rowObject.GetComponentInChildren<TMPro.TMP_Text>(true).text = PaperInkStyle.FieldTextStacked(label,
                string.IsNullOrEmpty(value) ? "?" : value, string.IsNullOrEmpty(value));
            return existing;
        }
        // The row's own field logic is not used: strip it, keep its look.
        foreach (MonoBehaviour behaviour in rowObject.GetComponents<MonoBehaviour>())
            if (behaviour is CaseFolderFieldRow || behaviour is ValueDragSource ||
                behaviour is ValueSelectionHighlighter)
                DestroyImmediate(behaviour);

        bool blank = string.IsNullOrEmpty(value);
        TMPro.TMP_Text text = rowObject.GetComponentInChildren<TMPro.TMP_Text>(true);
        text.text = PaperInkStyle.FieldTextStacked(label, blank ? "?" : value, blank);
        // Unity's fake null: never use ?? with GetComponent (it can skip the fallback in the Editor).
        Image background = rowObject.GetComponent<Image>();
        if (background == null) background = rowObject.GetComponentInChildren<Image>(true);
        ValueTypeValidationFeedback feedback = rowObject.GetComponent<ValueTypeValidationFeedback>();
        if (feedback == null) feedback = rowObject.AddComponent<ValueTypeValidationFeedback>();
        PaperInkStyle.StyleFieldRow(background, text, true, null, feedback);
        if (background != null) background.raycastTarget = true;

        FindingSlot slot = rowObject.AddComponent<FindingSlot>();
        slot.feedback = feedback;
        if (rowObject.GetComponent<ValueHoverFeedback>() == null) rowObject.AddComponent<ValueHoverFeedback>();
        return slot;
    }

    public bool CanAccept(DataValue value) => value != null && Accepts != null && Accepts(value);

    public bool TryReceiveValue(DataValue value)
    {
        if (!CanAccept(value)) return false;
        Received?.Invoke(value);
        return true;
    }

    public void OnPointerClick(PointerEventData eventData) => Place();

    // Keep the click on the field instead of a draggable paper/window ancestor.
    public void OnPointerDown(PointerEventData eventData) { }

    public void OnDrop(PointerEventData eventData)
    {
        // Moving the sheet itself must not place a previously selected value.
        if (eventData.pointerDrag != null && eventData.pointerDrag.GetComponent<ValueDragSource>() != null) Place();
    }

    private void Place()
    {
        ValueTransferManager manager = ValueTransferManager.Instance;
        if (manager == null || !manager.HasSelection || manager.SelectedValue == null)
        {
            Cleared?.Invoke();
            return;
        }
        if (!CanAccept(manager.SelectedValue))
        {
            feedback?.ShowFeedback(false);
            if (Rejected != null) Rejected();
            else Level2Say.Line("That line doesn't go in this box. Try a line from another page.");
            return;
        }
        manager.TryPlaceOnDestination(this);
        feedback?.ShowFeedback(true);
    }
}
