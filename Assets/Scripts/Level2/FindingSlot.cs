using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// PURPOSE:
/// One fill-in box of a finding note on the case folder's Findings page
/// ("Concern", "Related record", "Reason"). It is a page-1 row (same prefab,
/// paper style and font) turned into a value destination that accepts a
/// whole GROUP of rows (any line of a summary page, any line of a record),
/// so a compatible box never hints at the answer. Tap a row on another page
/// to pick it up, turn to Findings and tap the box (or drag on the same page),
/// exactly like other folder fields. No typing, no precision drawing.
/// </summary>
public sealed class FindingSlot : MonoBehaviour, IDataValueDestination, IPointerClickHandler, IDropHandler
{
    /// <summary>Which values this box takes.</summary>
    public System.Func<DataValue, bool> Accepts;

    /// <summary>Called with the value the box took.</summary>
    public System.Action<DataValue> Received;

    /// <summary>Turns a fresh page-1 row object into a finding box showing "label: value" (or "?").</summary>
    public static FindingSlot Make(GameObject rowObject, string label, string value)
    {
        // The row's own field logic is not used: strip it, keep its look.
        foreach (MonoBehaviour behaviour in rowObject.GetComponents<MonoBehaviour>())
            if (behaviour is CaseFolderFieldRow || behaviour is ValueDragSource ||
                behaviour is ValueSelectionHighlighter || behaviour is ValueTypeValidationFeedback)
                DestroyImmediate(behaviour);

        bool blank = string.IsNullOrEmpty(value);
        TMPro.TMP_Text text = rowObject.GetComponentInChildren<TMPro.TMP_Text>(true);
        PaperInkStyle.SetFieldText(text, label, blank ? "?" : value, blank);
        // Unity's fake null: never use ?? with GetComponent (it can skip the fallback in the Editor).
        Image background = rowObject.GetComponent<Image>();
        if (background == null) background = rowObject.GetComponentInChildren<Image>(true);
        PaperInkStyle.StyleFieldRow(background, text, true, null, null);
        if (background != null) background.raycastTarget = true;

        return rowObject.AddComponent<FindingSlot>();
    }

    public bool CanAccept(DataValue value) => value != null && Accepts != null && Accepts(value);

    public bool TryReceiveValue(DataValue value)
    {
        if (!CanAccept(value)) return false;
        Received?.Invoke(value);
        return true;
    }

    public void OnPointerClick(PointerEventData eventData) => Place();

    public void OnDrop(PointerEventData eventData) => Place();

    private void Place()
    {
        ValueTransferManager manager = ValueTransferManager.Instance;
        if (manager == null || !manager.HasSelection || manager.SelectedValue == null) return;
        if (!CanAccept(manager.SelectedValue))
        {
            Level2Say.Line("That line doesn't go in this box. Try a line from another page.");
            return;
        }
        manager.TryPlaceOnDestination(this);
    }
}
