using UnityEngine;

/// <summary>
/// Supplies only T02's read-only presentation context before the shared player
/// consumes it. The Inspector selection is an isolated preview fallback; an
/// explicitly supplied PendingContext always wins. No campaign state is read.
/// </summary>
[DefaultExecutionOrder(-1000)]
public sealed class Chapter02CinematicContext : MonoBehaviour
{
    public enum RosaPreview
    {
        Neutral = 0,
        ResponsibleCompleted = 1,
        PromiseResolved = 2,
        DismissiveCompleted = 3
    }

    public const string Neutral = "rosa.neutral";
    public const string ResponsibleCompleted = "rosa.responsible_completed";
    public const string PromiseResolved = "rosa.promise_resolved";
    public const string DismissiveCompleted = "rosa.dismissive_completed";

    [Tooltip("Used only when no PendingContext was supplied. Stop Play before changing the preview and play again.")]
    [SerializeField] private RosaPreview preview = RosaPreview.Neutral;

    public static ChapterCinematicContext Resolve(ChapterCinematicContext supplied, RosaPreview fallback)
    {
        string variant = supplied != null ? supplied.Variant : fallback switch
        {
            RosaPreview.ResponsibleCompleted => ResponsibleCompleted,
            RosaPreview.PromiseResolved => PromiseResolved,
            RosaPreview.DismissiveCompleted => DismissiveCompleted,
            _ => Neutral
        };
        // Missing, legacy or unrecognized history must never invent follow-through.
        return new ChapterCinematicContext(variant switch
        {
            ResponsibleCompleted => ResponsibleCompleted,
            PromiseResolved => PromiseResolved,
            DismissiveCompleted => DismissiveCompleted,
            _ => Neutral
        });
    }

    private void Awake()
    {
        ChapterCinematicPlayer.PendingContext = Resolve(ChapterCinematicPlayer.PendingContext, preview);
    }
}
