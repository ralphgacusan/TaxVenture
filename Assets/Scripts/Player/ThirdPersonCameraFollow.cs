using UnityEngine;

/// <summary>
/// PURPOSE:
/// Provides a simple third-person orbit camera that follows the player and
/// can be rotated with the mouse. This is the "exploration" camera mode.
///
/// RESPONSIBILITIES:
/// - Follow the player's position with an offset
/// - Orbit around the player based on mouse X movement
/// - Adjust pitch (up/down) based on mouse Y movement, clamped to avoid flipping
///
/// DOES NOT:
/// - Handle first-person camera switching (that arrives in Milestone 3, via a
///   CameraController that toggles between this script and a first-person rig)
///
/// CONNECTS WITH:
/// - Target: assign the Player transform in the Inspector
/// - Milestone 3 will introduce a CameraController that enables/disables this
///   script when switching between third-person and first-person modes.
/// </summary>
public class ThirdPersonCameraFollow : MonoBehaviour
{
    [Header("Target")]
    [Tooltip("The player transform this camera follows.")]
    [SerializeField] private Transform target;

    [Header("Follow Settings")]
    [Tooltip("Distance from the target.")]
    [SerializeField] private float distance = 6f;

    [Tooltip("Height offset above the target.")]
    [SerializeField] private float height = 2.5f;

    [Tooltip("How quickly the camera catches up to the desired position.")]
    [SerializeField] private float followSmoothness = 10f;

    [Header("Mouse Look Settings")]
    [SerializeField] private float mouseSensitivity = 3f;
    [SerializeField] private float minPitch = 5f;
    [SerializeField] private float maxPitch = 60f;

    private float yaw;
    private float pitch = 20f;

    private void Start()
    {
        // Lock and hide the cursor for a game-like feel during editor testing.
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (target != null)
        {
            yaw = target.eulerAngles.y;
        }
    }

    private void LateUpdate()
    {
        if (target == null) return;

        // --- MOUSE INPUT ---
        yaw += Input.GetAxis("Mouse X") * mouseSensitivity;
        pitch -= Input.GetAxis("Mouse Y") * mouseSensitivity;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        // --- CALCULATE DESIRED POSITION ---
        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 desiredPosition = target.position - (rotation * Vector3.forward * distance) + Vector3.up * height;

        // --- SMOOTH FOLLOW ---
        transform.position = Vector3.Lerp(transform.position, desiredPosition, followSmoothness * Time.deltaTime);
        transform.LookAt(target.position + Vector3.up * (height * 0.5f));
    }

    /// <summary>
    /// Allows other scripts (like a future CameraController) to assign the target
    /// dynamically if needed, rather than only via Inspector.
    /// </summary>
    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }
}