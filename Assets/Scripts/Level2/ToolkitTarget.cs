using UnityEngine;

/// <summary>
/// Marks a collider of the market income toolkit (IncomeToolkit) so a
/// pointer ray can tell what it touched: a printed line to highlight, a note
/// place on the paper, a note card / record tag, the highlighter, a record
/// tab or the CHECK button.
/// </summary>
public sealed class ToolkitTarget : MonoBehaviour
{
    public enum Kind { Line, Slot, Card, Marker, Tab, Button }

    public Kind kind;
    /// <summary>Line id (Line), option id (Card), entry id (Tab) or button id (Button).</summary>
    public string id;
    /// <summary>The note place (Slot) or the card's own kind (Card).</summary>
    public AnnotationSlot slot;
    public AnnotationCard card;
}
