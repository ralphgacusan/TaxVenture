using UnityEngine;
using TMPro;

/// <summary>
/// PURPOSE:
/// One read-only row on the Tax Return Viewer, showing a single field's
/// label and the value that was actually confirmed/printed onto the form.
/// Purely display — no interaction, no source/destination role (the
/// SUBMITTABLE version of the Tax Return is the HUD icon itself, per R11;
/// this panel is just for the player to REVIEW what they filed).
/// </summary>
public class TaxReturnViewerRow : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI labelText;
    [SerializeField] private TextMeshProUGUI valueText;

    public void Initialize(string label, string value)
    {
        labelText.text = label;
        valueText.text = string.IsNullOrEmpty(value) ? "-" : value;
    }
}