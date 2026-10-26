using UnityEngine;

/// <summary>
/// PURPOSE:
/// Smoothly turns the first-person view toward a world point (T12): the
/// guide's "face" page field (the Supply Counter introduction, then back
/// toward the plaza) and the counter itself when it opens. It eases yaw and
/// pitch over a short time, writes the camera rotation directly while the
/// player is held (guide pages, the shop's window hold), and hands the
/// final angles to FirstPersonCameraController so control resumes from the
/// new view without a snap. Presentation only: it never moves the player.
/// </summary>
[DefaultExecutionOrder(500)]
public sealed class ViewTurn : MonoBehaviour
{
    private const float DefaultSeconds = 0.7f;

    private static ViewTurn running;

    private FirstPersonCameraController view;
    private Transform cameraTransform;
    private float fromYaw, fromPitch, toYaw, toPitch;
    private float started, seconds;

    /// <summary>True while a turn is easing.</summary>
    public static bool Turning => running != null && running.enabled;

    /// <summary>Starts (or retargets) a turn toward point. False when there is no first-person view to turn.</summary>
    public static bool Toward(Vector3 point, float duration = DefaultSeconds)
    {
        CameraController controller = CameraController.Instance;
        if (controller == null) return false;
        CameraController.CameraMode mode = controller.CurrentMode;
        if (mode != CameraController.CameraMode.FirstPerson && mode != CameraController.CameraMode.ProfileViewer) return false;
        Camera main = controller.MainCamera != null ? controller.MainCamera : Camera.main;
        // Level 1 also has a look controller on a separate helper object.
        // Keep the angles on the controller that actually writes this camera.
        FirstPersonCameraController fpc = main != null ? main.GetComponent<FirstPersonCameraController>() : null;
        if (fpc == null || main == null) return false;

        Vector3 direction = point - main.transform.position;
        float flat = new Vector2(direction.x, direction.z).magnitude;
        if (flat < 0.05f && Mathf.Abs(direction.y) < 0.05f) return false;
        float yaw = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
        float pitch = -Mathf.Atan2(direction.y, Mathf.Max(0.01f, flat)) * Mathf.Rad2Deg;

        if (running == null)
        {
            var host = new GameObject("View Turn");
            running = host.AddComponent<ViewTurn>();
        }
        running.Begin(fpc, main.transform, yaw, pitch, Mathf.Max(0.05f, duration));
        return true;
    }

    /// <summary>Ends a turn at once (the angles reached so far are kept).</summary>
    public static void Stop()
    {
        if (running != null) running.enabled = false;
    }

    private void Begin(FirstPersonCameraController fpc, Transform cam, float yaw, float pitch, float duration)
    {
        view = fpc;
        cameraTransform = cam;
        Vector3 euler = cam.rotation.eulerAngles;
        fromYaw = euler.y;
        fromPitch = euler.x > 180f ? euler.x - 360f : euler.x;
        toYaw = fromYaw + Mathf.DeltaAngle(fromYaw, yaw);
        toPitch = Mathf.Clamp(pitch, -60f, 60f);
        started = Time.unscaledTime;
        seconds = duration;
        enabled = true;
    }

    // After the first-person controller's LateUpdate (it writes the same angles when it runs).
    private void LateUpdate()
    {
        if (view == null || cameraTransform == null)
        {
            enabled = false;
            return;
        }
        CameraController controller = CameraController.Instance;
        if (controller != null && controller.CurrentMode != CameraController.CameraMode.FirstPerson &&
            controller.CurrentMode != CameraController.CameraMode.ProfileViewer)
        {
            enabled = false; // a desk, interview or board view took over
            return;
        }
        float t = Mathf.Clamp01((Time.unscaledTime - started) / seconds);
        float eased = t * t * (3f - 2f * t);
        float yaw = Mathf.Lerp(fromYaw, toYaw, eased);
        float pitch = Mathf.Lerp(fromPitch, toPitch, eased);
        view.SetLook(yaw, pitch);
        cameraTransform.rotation = Quaternion.Euler(pitch, yaw, 0f);
        if (t >= 1f) enabled = false;
    }

    private void OnDestroy()
    {
        if (running == this) running = null;
    }
}
