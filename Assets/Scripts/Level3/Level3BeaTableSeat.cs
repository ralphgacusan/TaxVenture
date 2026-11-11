using System;
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

    public void Init(Level3BeaTable owner, Transform model)
    {
        table = owner;
        highlight = HighlightEffect.For((Component)this);
        highlight.SetTargetRoot(model);
    }

    public void OnFocus() { if (highlight != null) highlight.Highlight(); }

    public void OnUnfocus() { if (highlight != null) highlight.Unhighlight(); }

    public string GetPromptText() => table != null && table.Content != null ? "Sit at " + table.Content.speakerName + "'s table" : "Sit at the table";

    public void OnInteract()
    {
        if (table != null) table.TrySit();
    }
}

/// <summary>
/// PURPOSE:
/// T04 buildup: one step of Bea's first encounter on her table ("Open Bea's
/// records" on the envelope, "Inspect a record" on the record she brings
/// forward), with the same first-person prompt, tap and highlight as the
/// seat. Its collider is on only while its step is next (Level3BeaTable
/// decides), so the table offers exactly one action at a time. It saves,
/// grades and files nothing; the table owns what the tap does.
/// </summary>
public sealed class Level3BeaTableTarget : MonoBehaviour, IInteractable
{
    private string prompt;
    private Action interact;
    private BoxCollider box;
    private HighlightEffect highlight;

    public void Init(BoxCollider collider, string promptText, Action onInteract)
    {
        box = collider;
        prompt = promptText;
        interact = onInteract;
        highlight = HighlightEffect.For((Component)this);
        Active = false;
    }

    /// <summary>Its step is next: the collider takes the first-person ray. Off also drops the resting outline.</summary>
    public bool Active
    {
        get => box != null && box.enabled;
        set
        {
            if (box != null && box.enabled != value) box.enabled = value;
            if (!value) Affordance(false);
        }
    }

    public void SetVisual(Transform model) => highlight?.SetTargetRoot(model);

    /// <summary>The existing resting outline that marks the next thing to touch (no new marker).</summary>
    public void Affordance(bool on)
    {
        if (highlight != null) highlight.SetAffordance(on && Active);
    }

    public void OnFocus() { if (highlight != null) highlight.Highlight(); }

    public void OnUnfocus() { if (highlight != null) highlight.Unhighlight(); }

    public string GetPromptText() => prompt;

    public void OnInteract()
    {
        if (Active) interact?.Invoke();
    }
}
