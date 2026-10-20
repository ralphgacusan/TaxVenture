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
    private bool hasFocus = true;
    private bool isPaused;

    [Header("Footstep Audio")]
    [SerializeField] private float footstepInterval = 0.7f;

    private float footstepTimer;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        // This stays enabled when a lesson or workstation holds movement.
        if (GetComponent<AudioEnvironment>() == null)
            gameObject.AddComponent<AudioEnvironment>();

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
                 camRight * inputDir.x).normalized * inputDir.magnitude;
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
        if (!hasFocus || isPaused) return Vector2.zero;
        // Mobile joystick takes priority when it exists.
        if (mobileJoystick != null)
        {
            Vector2 joystickInput = mobileJoystick.Input;

            if (mobileJoystick.IsHeld || joystickInput.sqrMagnitude > 0f)
                return joystickInput;
        }

        // Keep keyboard input temporarily for editor testing.
#if UNITY_EDITOR || UNITY_STANDALONE
        if (useKeyboardInEditor)
        {
            return new Vector2(
                Input.GetAxisRaw("Horizontal"),
                Input.GetAxisRaw("Vertical")
            );
        }
#endif

        return Vector2.zero;
    }

    private void OnDisable()
    {
        moveDirection = Vector3.zero;
        footstepTimer = 0f;
        if (rb != null && !rb.isKinematic)
            rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
    }

    private void OnApplicationFocus(bool focused)
    {
        hasFocus = focused;
        if (!focused) OnDisable();
    }

    private void OnApplicationPause(bool paused)
    {
        isPaused = paused;
        if (paused) OnDisable();
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
