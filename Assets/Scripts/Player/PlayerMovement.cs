using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;

    [Tooltip("How quickly the player rotates to face movement direction.")]
    [SerializeField] private float rotationSpeed = 10f;

    [Header("Mobile Input")]
    [SerializeField] private VirtualJoystick mobileJoystick;

    [Header("Editor Testing")]
    [SerializeField] private bool useKeyboardInEditor = true;

    private Rigidbody rb;
    private Vector3 moveDirection;

    [Header("Footstep Audio")]
    [SerializeField] private float footstepInterval = 0.7f;

    private float footstepTimer;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        rb.constraints =
            RigidbodyConstraints.FreezeRotationX |
            RigidbodyConstraints.FreezeRotationZ;
    }

    private void Update()
    {
        Vector2 input = GetMovementInput();

        Vector3 inputDir = new Vector3(
            input.x,
            0f,
            input.y
        );

        inputDir = Vector3.ClampMagnitude(inputDir, 1f);

        if (Camera.main != null && inputDir.sqrMagnitude > 0.0001f)
        {
            Vector3 camForward = Camera.main.transform.forward;
            Vector3 camRight = Camera.main.transform.right;

            camForward.y = 0f;
            camRight.y = 0f;

            camForward.Normalize();
            camRight.Normalize();

            moveDirection =
                (camForward * inputDir.z +
                 camRight * inputDir.x).normalized;
        }
        else
        {
            moveDirection = Vector3.zero;
        }

        // =========================================================
        // FOOTSTEP AUDIO
        // =========================================================

        if (moveDirection.sqrMagnitude > 0.01f)
        {
            footstepTimer -= Time.deltaTime;

            if (footstepTimer <= 0f)
            {
                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.PlayFootstepSFX();
                }

                footstepTimer = footstepInterval;
            }
        }
        else
        {
            footstepTimer = 0f;
        }
    }

    private Vector2 GetMovementInput()
    {
        // Mobile joystick takes priority when it exists.
        if (mobileJoystick != null)
        {
            Vector2 joystickInput = mobileJoystick.Input;

            if (joystickInput.sqrMagnitude > 0.001f)
                return joystickInput;
        }

        // Keep keyboard input temporarily for editor testing.
        if (useKeyboardInEditor)
        {
            return new Vector2(
                Input.GetAxisRaw("Horizontal"),
                Input.GetAxisRaw("Vertical")
            );
        }

        return Vector2.zero;
    }

    private void FixedUpdate()
    {
        Vector3 targetVelocity = moveDirection * moveSpeed;

        rb.linearVelocity = new Vector3(
            targetVelocity.x,
            rb.linearVelocity.y,
            targetVelocity.z
        );

        if (moveDirection.sqrMagnitude > 0.01f)
        {
            Quaternion targetRotation =
                Quaternion.LookRotation(moveDirection);

            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                rotationSpeed * Time.fixedDeltaTime
            );
        }
    }
}