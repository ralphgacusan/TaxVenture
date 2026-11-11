using UnityEngine;

/// <summary>
/// One small physical prop event in a chapter cutscene: the target travels
/// from its "From" pose to its "To" pose between two times with an ease-out
/// (it decelerates to rest, as a folder slid across a desk does), and holds
/// the end pose afterwards. Both poses are scene markers, so the move is
/// visible and editable in the Scene view; the prop never leaves the surface
/// the markers rest on.
/// </summary>
public sealed class ChapterCinematicPropMove : ChapterCinematicElement
{
    [SerializeField] private Transform target;
    [SerializeField] private Transform from;
    [SerializeField] private Transform to;
    [SerializeField] private float start = 10.45f;
    [SerializeField] private float end = 11.15f;

    public float Start => start;
    public float End => end;

    public override void Evaluate(in ChapterCinematicFrame frame)
    {
        if (target == null || from == null || to == null) return;
        float t = EaseOut(Progress(frame.Time, start, end));
        target.SetPositionAndRotation(
            Vector3.LerpUnclamped(from.position, to.position, t),
            Quaternion.SlerpUnclamped(from.rotation, to.rotation, t));
    }

    private void OnDrawGizmos()
    {
        if (from == null || to == null) return;
        Gizmos.color = new Color(0.45f, 0.75f, 1f, 0.9f);
        Gizmos.DrawLine(from.position, to.position);
        Gizmos.DrawWireCube(to.position, new Vector3(0.05f, 0.01f, 0.05f));
    }
}
