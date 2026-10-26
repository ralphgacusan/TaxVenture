using System;
using System.Collections;
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

    /// <summary>Length of one page turn (the 3D Menu training binder's flip).</summary>
    public const float PageFlipSeconds = 0.5f;

    /// <summary>
    /// Turns a page like the 3D Menu training binder: the page hinges on its
    /// left edge (the spine), lifts a little toward the reader, eases in to
    /// edge-on, the content is swapped exactly there, then it eases out flat
    /// from the other side. Forward turns lift the free edge toward the
    /// reader first; backward turns lay the new page down toward the reader.
    /// The page always ends at its rest position, rotation and scale (the
    /// rest position is the page's position when the turn starts).
    /// </summary>
    public static IEnumerator TurnPage(RectTransform page, Quaternion restRotation, Vector3 restScale,
        int direction, float duration, Action onMidpoint)
    {
        float half = Mathf.Max(0.01f, duration * 0.5f);
        float sign = direction >= 0 ? 1f : -1f;
        Vector3 restPosition = page.localPosition;
        Rect rect = page.rect;
        // The hinge (left edge, mid height) and the lift, in the page's scaled local units.
        Vector3 hinge = Vector3.Scale(restScale, new Vector3(rect.xMin, rect.center.y, 0f));
        float lift = rect.height * 0.04f * restScale.z;

        void Pose(float angle, float t)
        {
            Quaternion turn = Quaternion.Euler(0f, angle, 0f);
            page.localRotation = restRotation * turn;
            page.localScale = restScale;
            page.localPosition = restPosition + restRotation * (hinge - turn * hinge)
                                 + restRotation * new Vector3(0f, 0f, -lift * Mathf.Sin(Mathf.Clamp01(t) * Mathf.PI));
        }

        for (float t = 0f; t < half; t += Step)
        {
            float a = t / half;
            Pose(sign * 90f * a * a, a * 0.5f);
            yield return null;
        }

        // Finish exactly edge-on before changing content; low frame rates must
        // not expose the next page before it reaches the spine.
        Pose(sign * 90f, 0.5f);
        onMidpoint?.Invoke();
        // The new page's rows were just built: skip that (possibly slow) frame so
        // the second half starts from the spine instead of jumping ahead.
        yield return null;

        for (float t = 0f; t < half; t += Step)
        {
            float b = 1f - t / half;
            Pose(-sign * 90f * b * b, 0.5f + (t / half) * 0.5f);
            yield return null;
        }

        page.localRotation = restRotation;
        page.localScale = restScale;
        page.localPosition = restPosition;
    }
}
