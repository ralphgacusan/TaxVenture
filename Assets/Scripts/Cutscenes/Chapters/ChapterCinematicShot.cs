using UnityEngine;

/// <summary>
/// One authored camera shot of a chapter cutscene: from its start time until
/// the next shot starts, it places the cinematic camera between two pose
/// markers (children "From"/"To" in the scene) with one eased move and a
/// steady field of view. A cut is simply the next shot taking over.
/// Gizmos draw the move and the end frustum so the shot structure is visible
/// in the Scene view without entering Play Mode.
/// </summary>
public sealed class ChapterCinematicShot : ChapterCinematicElement
{
    [SerializeField] private Camera targetCamera;
    [Tooltip("Presentation time this shot cuts in (seconds). It lasts until the next shot's start.")]
    [SerializeField] private float startTime;
    [Tooltip("Editor reference only: the time the next shot cuts in.")]
    [SerializeField] private float endTime = 4.8f;
    [SerializeField] private Transform from;
    [SerializeField] private Transform to;
    [Tooltip("The single camera move runs (smootherstep) between these absolute times; it holds before and after.")]
    [SerializeField] private float moveStart;
    [SerializeField] private float moveEnd = 4.8f;
    [SerializeField, Range(20f, 70f)] private float fieldOfView = 45f;

    public float StartTime => startTime;
    public float EndTime => endTime;
    public float FieldOfView => fieldOfView;

    public override void Evaluate(in ChapterCinematicFrame frame)
    {
        if (targetCamera == null || from == null || to == null) return;
        float t = Ease(Progress(frame.Time, moveStart, moveEnd));
        Vector3 position = Vector3.LerpUnclamped(from.position, to.position, t);
        Quaternion rotation = Quaternion.SlerpUnclamped(from.rotation, to.rotation, t);
        targetCamera.transform.SetPositionAndRotation(position, rotation);
        if (!Mathf.Approximately(targetCamera.fieldOfView, fieldOfView)) targetCamera.fieldOfView = fieldOfView;
    }

    private void OnDrawGizmos()
    {
        if (from == null || to == null) return;
        Gizmos.color = new Color(1f, 0.78f, 0.35f, 0.9f);
        Gizmos.DrawLine(from.position, to.position);
        Gizmos.DrawWireSphere(from.position, 0.04f);
        Matrix4x4 previous = Gizmos.matrix;
        Gizmos.matrix = Matrix4x4.TRS(to.position, to.rotation, Vector3.one);
        Gizmos.DrawFrustum(Vector3.zero, fieldOfView, 1.2f, 0.1f, 16f / 9f);
        Gizmos.matrix = previous;
    }
}
