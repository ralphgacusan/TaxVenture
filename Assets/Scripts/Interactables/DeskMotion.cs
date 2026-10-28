using UnityEngine;

/// <summary>
/// Small easing and motion helpers shared by the desk documents (case
/// folder and tax code book). Presentation only: nothing here changes
/// gameplay state.
/// </summary>
public static class DeskMotion
{
    /// <summary>Changes only a document's final position, keeping its visible paper centre at viewport centre.</summary>
    public static Vector3 CentrePaper(Camera camera, Vector3 rootPosition, Vector3 paperCentre)
    {
        if (camera == null) return rootPosition;
        float depth = Vector3.Dot(paperCentre - camera.transform.position, camera.transform.forward);
        Vector2 centre = camera.pixelRect.center;
        return rootPosition + camera.ScreenToWorldPoint(new Vector3(centre.x, centre.y, depth)) - paperCentre;
    }

    /// <summary>
    /// Unscaled frame time, capped at 1/30 s: a single slow frame (content
    /// rebuilt, a capture) slows an animation for a moment instead of
    /// making it jump.
    /// </summary>
    public static float Step => Mathf.Min(Time.unscaledDeltaTime, 1f / 30f);

    public static float EaseInQuad(float t) => t * t;
    public static float EaseOutQuad(float t) => 1f - (1f - t) * (1f - t);
    public static float EaseInCubic(float t) => t * t * t;
    public static float EaseOutCubic(float t) => 1f - Mathf.Pow(1f - t, 3f);

    public static float EaseOutBack(float t, float overshoot = 1.4f)
    {
        float c3 = overshoot + 1f;
        float u = t - 1f;
        return 1f + c3 * u * u * u + overshoot * u * u;
    }

    /// <summary>
    /// Tilts a held item toward the direction it is being pulled. The lag
    /// between the smoothed item and its pointer target drives the angle, so
    /// the tilt fades out as soon as the item catches up.
    /// </summary>
    public static Quaternion HeldRotation(Quaternion restRotation, Vector3 lag, Vector3 up,
        float maxAngle = 8f, float degreesPerMetre = 320f)
    {
        Vector3 planar = Vector3.ProjectOnPlane(lag, up);
        float distance = planar.magnitude;
        if (distance < 0.0001f)
            return restRotation;

        Vector3 axis = Vector3.Cross(up, planar / distance);
        float angle = Mathf.Min(maxAngle, distance * degreesPerMetre);
        return Quaternion.AngleAxis(angle, axis) * restRotation;
    }

    /// <summary>Length of one page turn (PageTurnStage; the 3D Menu training binder's flip).</summary>
    public const float PageFlipSeconds = 0.5f;
}
