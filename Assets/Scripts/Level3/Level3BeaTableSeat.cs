using UnityEngine;

/// <summary>
/// PURPOSE:
/// The tap target of Bea's worktable (T04): the same first-person prompt and
/// highlight as the office desk. After the handover it sits the player at the
/// table (Level3BeaTable.TrySit); before it, one plain line says to talk to Bea.
/// </summary>
public sealed class Level3BeaTableSeat : MonoBehaviour, IInteractable
{
    private Level3BeaTable table;
    private HighlightEffect highlight;

    public void Init(Level3BeaTable owner)
    {
        table = owner;
        highlight = HighlightEffect.For((Component)this);
    }

    public void OnFocus() { if (highlight != null) highlight.Highlight(); }

    public void OnUnfocus() { if (highlight != null) highlight.Unhighlight(); }

    public string GetPromptText() => table != null && table.Content != null ? "Sit at " + table.Content.speakerName + "'s table" : "Sit at the table";

    public void OnInteract()
    {
        if (table != null) table.TrySit();
    }
}
