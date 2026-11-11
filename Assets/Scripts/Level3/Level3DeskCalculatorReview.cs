using UnityEngine;

/// <summary>
/// PURPOSE:
/// The desk calculator's part in stamping Bea's computation (Level 3 T09).
/// T09 revision (5 November 2026): Level 3 stamps with the SHARED stamp set,
/// the one Level 2 uses (Level2DeskStamps / Level2StampDrag: the desk's own
/// stand models, lift, free-position ghost, press, ink and return), bound to
/// Bea's case and computation revision by Level3CaseFolder (its ICaseStamp).
/// The earlier procedural READY / NOT READY bodies that stood in for the stand
/// models here, and their tap-stamp-box press on the retired clipboard leaf,
/// were never built in the live game any more and are removed: one stamp set,
/// one input owner. Lifting the stamps puts this calculator away first
/// (Level3CaseFolder.Opening).
/// Kept here: the guide cue names and what the review cutoff waits for.
/// Nothing here decides the review (Level3Audit / Level3ReviewRules do).
/// </summary>
public sealed partial class Level3DeskCalculator
{
    public const string CueStampPicked = "StampPicked";
    public const string CueSheetStamped = "SheetStamped";

    /// <summary>A tool is moving or a tape is flying (the review cutoff waits for it; the shared stamps report their own press).</summary>
    public bool Busy => movingTool != null || ValueDragSource.IsAnyDragActive || mode == Mode.Lifting || mode == Mode.Returning || flyStart >= 0f;
}
