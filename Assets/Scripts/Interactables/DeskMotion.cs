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

    /// <summary>
    /// Flips a page about its own vertical axis: it turns edge-on, the
    /// content is swapped at the midpoint, then it turns back in from the
    /// other side. The page always ends at its rest rotation and scale.
    /// </summary>
    public static IEnumerator TurnPage(RectTransform page, Quaternion restRotation, Vector3 restScale,
        int direction, float duration, Action onMidpoint)
    {
        float half = Mathf.Max(0.01f, duration * 0.5f);
        float sign = direction >= 0 ? 1f : -1f;

        for (float t = 0f; t < half; t += Time.unscaledDeltaTime)
        {
            float angle = 90f * EaseInQuad(t / half);
            page.localRotation = restRotation * Quaternion.Euler(0f, -sign * angle, 0f);
            page.localScale = restScale * Mathf.Lerp(1f, 0.96f, t / half);
            yield return null;
        }

        onMidpoint?.Invoke();

        for (float t = 0f; t < half; t += Time.unscaledDeltaTime)
        {
            float angle = 90f * (1f - EaseOutQuad(t / half));
            page.localRotation = restRotation * Quaternion.Euler(0f, sign * angle, 0f);
            page.localScale = restScale * Mathf.Lerp(0.96f, 1f, t / half);
            yield return null;
        }

        page.localRotation = restRotation;
        page.localScale = restScale;
    }
}
