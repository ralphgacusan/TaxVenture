using UnityEngine;

/// <summary>
/// PURPOSE:
/// The motion of a small paper pickup in the world, shared by the plaza
/// profile papers (TaxpayerProfileWorldPickup) and Marie's Client Copy
/// beside the Auditor (ClientCopyPickup, T08): the pop-up drop that flutters
/// down to its rest pose, the idle hover, and the flight into a HUD icon
/// (an arc across the screen, shrinking toward the camera and turning to
/// face it). Pure pose math; the callers own their timing and state.
/// </summary>
public static class WorldPickupMotion
{
    public static float Smooth(float t) => t * t * (3f - 2f * t);

    /// <summary>The idle hover: the rest position plus a gentle bob.</summary>
    public static Vector3 Hover(Vector3 rest, float time, float speed, float height) =>
        rest + Vector3.up * (Mathf.Sin(time * speed) * height);

    /// <summary>The pop-up drop from source toward landing (rest plus its hover) at progress 0..1.</summary>
    public static void Drop(Transform target, Vector3 source, Vector3 rest, Quaternion restRotation, Vector3 landing, float progress)
    {
        float eased = Smooth(progress);
        Vector3 driftDirection = Vector3.Cross(Vector3.up, rest - source);
        if (driftDirection.sqrMagnitude < 0.0001f) driftDirection = Vector3.right;
        driftDirection.Normalize();

        float flutter = Mathf.Sin(progress * Mathf.PI * 4f) * (1f - progress);
        float drift = Mathf.Sin(progress * Mathf.PI) * 0.035f;
        float verticalFlutter = Mathf.Sin(progress * Mathf.PI * 3f) * (1f - progress) * 0.018f;
        target.position = Vector3.LerpUnclamped(source, landing, eased) + driftDirection * drift + Vector3.up * verticalFlutter;
        target.rotation = restRotation * Quaternion.Euler(flutter * 2.5f, 0f, flutter * 5f);
    }

    /// <summary>The depth a flight into the HUD ends at: just in front of the camera, never farther than it started.</summary>
    public static float FlightEndDepth(Camera camera, float startDepth)
    {
        float depth = Mathf.Min(startDepth, camera.nearClipPlane + 0.4f);
        depth = Mathf.Max(camera.nearClipPlane + 0.05f, depth);
        return Mathf.Min(startDepth, depth);
    }

    /// <summary>The flight into a HUD icon at progress 0..1.</summary>
    public static void Fly(Transform target, Camera camera, Vector2 fromScreen, Vector2 toScreen, float fromDepth, float toDepth,
        Quaternion fromRotation, Vector3 fromScale, float progress)
    {
        float eased = Smooth(progress);
        Vector2 screenPoint = Vector2.Lerp(fromScreen, toScreen, eased);
        screenPoint.y += Mathf.Sin(progress * Mathf.PI) * Screen.height * 0.055f;
        screenPoint.x += Mathf.Sin(progress * Mathf.PI * 4f) * (1f - progress) * Screen.width * 0.006f;

        float depth = Mathf.Lerp(fromDepth, toDepth, eased);
        target.position = camera.ScreenToWorldPoint(new Vector3(screenPoint.x, screenPoint.y, depth));
        float screenShrink = Mathf.Lerp(1f, 0.16f, eased);
        float depthScale = camera.orthographic ? 1f : depth / fromDepth;
        target.localScale = fromScale * depthScale * screenShrink;

        Quaternion facingCamera = camera.transform.rotation;
        float flutter = Mathf.Sin(progress * Mathf.PI * 4f) * (1f - progress);
        Quaternion flutterRotation = Quaternion.AngleAxis(flutter * 3f, camera.transform.forward);
        target.rotation = flutterRotation * Quaternion.Slerp(fromRotation, facingCamera, eased);
    }
}
