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
    [SerializeField] private float distance = 3.8f;
    [Tooltip("Height offset above the target.")]
    [SerializeField] private float height = 1.6f;
    [Tooltip("How quickly the camera catches up to the desired position.")]
    [SerializeField] private float followSmoothness = 10f;
    [Tooltip("Look Height offset above the target's position for the camera to look at.")]
    [SerializeField] private float lookHeight = 1.2f;

    [Header("Mouse Look Settings")]
    [SerializeField] private float mouseSensitivity = 3f;
    [SerializeField] private float minPitch = -15f;
    [SerializeField] private float maxPitch = 60f;

    [Header("DEBUG - Temporary First Person Toggle")]
    [SerializeField] private bool firstPersonMode = false;

    [SerializeField] private KeyCode toggleKey = KeyCode.Q;

    [SerializeField] private float firstPersonHeight = 0.8f;

    private float yaw;
    private float pitch = 0f;

    private void Start()
    {

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (target != null)
        {
            yaw = target.eulerAngles.y;
        }
    }

    // Test: Third Person
    // private void LateUpdate()
    // {
    //     if (target == null) return;

    //     // --- MOUSE INPUT ---
    //     yaw += Input.GetAxis("Mouse X") * mouseSensitivity;
    //     pitch -= Input.GetAxis("Mouse Y") * mouseSensitivity;
    //     pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

    //     // --- CALCULATE DESIRED POSITION ---
    //     Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
    //     Vector3 desiredPosition = target.position - (rotation * Vector3.forward * distance) + Vector3.up * height;

    //     // --- SMOOTH FOLLOW ---
    //     transform.position = Vector3.Lerp(transform.position, desiredPosition, followSmoothness * Time.deltaTime);
    //     transform.LookAt(target.position + Vector3.up * (height * 0.5f));
    // }



    /// <summary>
    /// Allows other scripts (like a future CameraController) to assign the target
    /// dynamically if needed, rather than only via Inspector.
    /// </summary>
    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }


    // Test: Third and First Person Toggle
    private void LateUpdate()
    {
        if (target == null) return;

        // Mouse look
        yaw += Input.GetAxis("Mouse X") * mouseSensitivity;
        pitch -= Input.GetAxis("Mouse Y") * mouseSensitivity;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);

        if (firstPersonMode)
        {
            // FIRST PERSON
            transform.position = target.position + Vector3.up * firstPersonHeight;
            transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
        }
        else
        {
            // THIRD PERSON
            Vector3 desiredPosition =
                target.position
                - (rotation * Vector3.forward * distance)
                + Vector3.up * height;

            transform.position = Vector3.Lerp(
                transform.position,
                desiredPosition,
                followSmoothness * Time.deltaTime);

            transform.LookAt(target.position + Vector3.up * lookHeight);
        }
    }
    private void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            firstPersonMode = !firstPersonMode;
        }
    }
}