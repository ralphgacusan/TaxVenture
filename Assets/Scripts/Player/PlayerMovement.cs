using UnityEngine;

/// <summary>
/// PURPOSE:
/// Handles third-person player movement using WASD input (temporary editor input).
/// This script is intentionally decoupled from "how" input is captured — it reads
/// from Unity's legacy Input system for now, but the movement logic itself
/// (direction, speed, rotation) is isolated so we can later swap in a virtual
/// joystick / touch input system without rewriting movement logic.
///
/// RESPONSIBILITIES:
/// - Read horizontal/vertical input
/// - Move the player relative to camera-forward direction
/// - Rotate the player to face movement direction
/// - Expose movement speed as an inspector-tunable value
///
/// DOES NOT:
/// - Handle camera logic (see ThirdPersonCameraFollow.cs)
/// - Handle interaction (see later Interactor.cs in Milestone 2)
///
/// CONNECTS WITH:
/// - Requires a Rigidbody on the same GameObject (for physics-based movement)
/// - Reads Camera.main to determine "forward" relative to view
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    [Tooltip("Movement speed in units per second.")]
    [SerializeField] private float moveSpeed = 5f;

    [Tooltip("How quickly the player rotates to face movement direction (higher = snappier).")]
    [SerializeField] private float rotationSpeed = 10f;

    private Rigidbody rb;
    private Vector3 moveDirection;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        // Prevent the capsule from tipping over during movement/collisions.
        rb.constraints = RigidbodyConstraints.FreezeRotationX |
                        RigidbodyConstraints.FreezeRotationZ;    
    }

    private void Update()
    {
        // --- INPUT ---
        // Temporary keyboard input. In the mobile version, this will be replaced
        // by a virtual joystick that feeds the same horizontal/vertical values.
        float horizontal = Input.GetAxisRaw("Horizontal"); // A/D
        float vertical = Input.GetAxisRaw("Vertical");     // W/S

        Vector3 inputDir = new Vector3(horizontal, 0f, vertical).normalized;

        // --- CAMERA-RELATIVE DIRECTION ---
        // We want "W" to move the player away from the camera, not always along
        // world Z. This makes movement feel correct as the camera orbits later.
        if (Camera.main != null && inputDir.magnitude > 0.01f)
        {
            Vector3 camForward = Camera.main.transform.forward;
            Vector3 camRight = Camera.main.transform.right;
            camForward.y = 0f;
            camRight.y = 0f;
            camForward.Normalize();
            camRight.Normalize();

            moveDirection = (camForward * inputDir.z + camRight * inputDir.x).normalized;
        }
        else
        {
            moveDirection = Vector3.zero;
        }
    }

    private void FixedUpdate()
    {
        // --- MOVEMENT ---
        Vector3 targetVelocity = moveDirection * moveSpeed;
        // Preserve existing vertical velocity (gravity) while overriding horizontal movement.
        rb.linearVelocity = new Vector3(targetVelocity.x, rb.linearVelocity.y, targetVelocity.z);

        // --- ROTATION ---
        // Rotate the player capsule to face the direction it's moving.
        if (moveDirection.sqrMagnitude > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime);
        }
    }
}