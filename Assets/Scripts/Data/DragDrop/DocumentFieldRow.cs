using UnityEngine;
using TMPro;

/// <summary>
/// PURPOSE:
/// One displayed, selectable field row inside DocumentViewerUI.
///
/// Source-only — documents never receive values.
///
/// Supports:
/// - Click-to-select
/// - Drag-and-drop
///
/// NORMAL DOCUMENT DISPLAY:
///     Label: Value
///
/// DRAG VISUAL:
///     Value
///
/// IMPORTANT:
/// The DataValue itself is NOT changed.
/// Only the visual representation of the drag is different.
/// </summary>
public class DocumentFieldRow : MonoBehaviour, IDataValueSource
{
    [Header("UI")]
    [SerializeField] private TextMeshProUGUI fieldText;

    [Header("Interaction")]
    [SerializeField] private ValueClickSource clickSource;
    [SerializeField] private ValueDragSource dragSource;

    private DocumentField field;

    // ---------------------------------------------------------
    // Initialization
    // ---------------------------------------------------------

    public void Initialize(DocumentField documentField)
    {
        field = documentField;

        if (field == null)
        {
            Debug.LogError(
                $"DocumentFieldRow on {gameObject.name} " +
                "received a NULL DocumentField!"
            );

            return;
        }

        // -----------------------------------------------------
        // NORMAL DOCUMENT DISPLAY
        // -----------------------------------------------------
        //
        // The actual document keeps its original appearance:
        //
        // Residency Status: Alien
        // Gross Income: ₱500,000
        // TIN: 123-456-789
        //
        // This does NOT affect dragging.
        // -----------------------------------------------------

        if (fieldText != null)
        {
            fieldText.text =
                $"{field.Label}: {field.DisplayValue}";
        }
        else
        {
            Debug.LogError(
                $"DocumentFieldRow on {gameObject.name} " +
                "has NO fieldText assigned!"
            );
        }

        // -----------------------------------------------------
        // CLICK SOURCE
        // -----------------------------------------------------

        if (clickSource != null)
        {
            clickSource.Initialize(this, this);
        }
        else
        {
            Debug.LogError(
                $"DocumentFieldRow on {gameObject.name} " +
                "has NO ValueClickSource assigned!"
            );
        }

        // -----------------------------------------------------
        // DRAG SOURCE
        // -----------------------------------------------------

        if (dragSource != null)
        {
            dragSource.Initialize(this, this);
        }
        else
        {
            Debug.LogError(
                $"DocumentFieldRow on {gameObject.name} " +
                "has NO ValueDragSource assigned!"
            );
        }

        Debug.Log(
            $"DocumentFieldRow initialized: " +
            $"{field.Label} = {field.DisplayValue}"
        );
    }

    // ---------------------------------------------------------
    // Data Value
    // ---------------------------------------------------------

    public DataValue GetDataValue()
    {
        if (field == null)
        {
            Debug.LogError(
                $"DocumentFieldRow on {gameObject.name} " +
                "has NO DocumentField!"
            );

            return null;
        }

        /*
         * IMPORTANT:
         *
         * Do NOT change this.
         *
         * ValueTransferManager and the validation system
         * still receive the original DataValue.
         */

        return field.ToDataValue();
    }

    // ---------------------------------------------------------
    // Drag Display Value
    // ---------------------------------------------------------

    /// <summary>
    /// Gets the visual text that should appear while this
    /// document field is being dragged.
    ///
    /// This is VISUAL ONLY.
    ///
    /// Example:
    ///
    /// Normal document:
    ///     Residency Status: Alien
    ///
    /// Drag visual:
    ///     Alien
    ///
    /// The actual DataValue remains unchanged.
    /// </summary>
    public string GetDragDisplayValue()
    {
        if (field == null)
        {
            return "?";
        }

        if (string.IsNullOrEmpty(field.DisplayValue))
        {
            return "?";
        }

        return field.DisplayValue;
    }
}