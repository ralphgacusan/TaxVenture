using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

/// <summary>
/// Third-person orbit camera supporting both:
/// - Mouse look in the Unity Editor / desktop
/// - Touch swipe look on Android/mobile
///
/// Mobile touch behavior:
/// - Touching empty gameplay space rotates the camera.
/// - Touching UI does NOT rotate the camera.
/// - Joystick touches do NOT rotate the camera.
/// - HUD buttons remain clickable.
/// - Dialogue buttons remain clickable.
///
/// The camera follows the player and supports temporary
/// first-person mode for debugging.
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

    [Tooltip("Look height offset above the target.")]
    [SerializeField] private float lookHeight = 1.2f;

    [Header("Mouse Look")]
    [Tooltip("Mouse look sensitivity for Editor/Desktop testing.")]
    [SerializeField] private float mouseSensitivity = 3f;

    [Header("Touch Look")]
    [Tooltip("Touch swipe sensitivity for mobile.")]
    [SerializeField] private float touchSensitivity = 0.15f;

    [Tooltip("If enabled, touch look will be disabled.")]
    [SerializeField] private bool disableTouchLook = false;

    [Header("Pitch Limits")]
    [SerializeField] private float minPitch = -15f;
    [SerializeField] private float maxPitch = 60f;

    [Header("DEBUG - Temporary First Person Toggle")]
    [SerializeField] private bool firstPersonMode = false;

    [SerializeField] private KeyCode toggleKey = KeyCode.Q;

    [SerializeField] private float firstPersonHeight = 0.8f;

    [SerializeField] private FirstPersonHands firstPersonHands;

    [Header("Cursor Toggle (Editor)")]
    [SerializeField] private KeyCode freeCursorKey = KeyCode.Tab;

    private float yaw;
    private float pitch = 0f;

    private bool isCursorFreed = false;

    // Used so one finger can remain on the joystick
    // while another finger controls the camera.
    private int activeLookFingerId = -1;

    private void Start()
    {
        // Cursor behavior is only relevant for desktop/editor testing.
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (target != null)
        {
            yaw = target.eulerAngles.y;
        }
    }

    private void LateUpdate()
    {
        if (target == null)
            return;

        // =========================================================
        // LOOK INPUT
        // =========================================================

        HandleMouseLook();
        HandleTouchLook();

        // =========================================================
        // CAMERA ROTATION
        // =========================================================

        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);

        // =========================================================
        // CAMERA MODE
        // =========================================================

        if (firstPersonMode)
        {
            // -----------------------------------------------------
            // FIRST PERSON
            // -----------------------------------------------------

            transform.position =
                target.position +
                Vector3.up * firstPersonHeight;

            transform.rotation =
                Quaternion.Euler(pitch, yaw, 0f);
        }
        else
        {
            // -----------------------------------------------------
            // THIRD PERSON
            // -----------------------------------------------------

            if (firstPersonHands != null)
                firstPersonHands.Hide();

            Vector3 desiredPosition =
                target.position
                - (rotation * Vector3.forward * distance)
                + Vector3.up * height;

            transform.position = Vector3.Lerp(
                transform.position,
                desiredPosition,
                followSmoothness * Time.deltaTime
            );

            transform.LookAt(
                target.position +
                Vector3.up * lookHeight
            );
        }
    }

    // =============================================================
    // MOUSE LOOK
    // =============================================================

    private void HandleMouseLook()
    {
        // Only process mouse input when a mouse exists.
        if (Mouse.current == null)
            return;

        // Read mouse movement using the NEW Input System.
        Vector2 mouseDelta = Mouse.current.delta.ReadValue();

        // Apply sensitivity.
        yaw += mouseDelta.x * mouseSensitivity;

        pitch -= mouseDelta.y * mouseSensitivity;

        // Prevent camera from flipping upside down.
        pitch = Mathf.Clamp(
            pitch,
            minPitch,
            maxPitch
        );
    }

    // =============================================================
    // TOUCH LOOK
    // =============================================================

    private void HandleTouchLook()
    {
        if (disableTouchLook)
            return;

        if (Touchscreen.current == null)
            return;

        var touches = Touchscreen.current.touches;

        // =========================================================
        // FIND A NEW LOOK FINGER
        // =========================================================

        if (activeLookFingerId == -1)
        {
            for (int i = 0; i < touches.Count; i++)
            {
                var touch = touches[i];

                // Only consider newly pressed fingers.
                if (!touch.press.wasPressedThisFrame)
                    continue;

                int fingerId = touch.touchId.ReadValue();

                // IMPORTANT:
                // If this finger started on ANY UI element
                // (joystick, HUD button, dialogue button, etc.),
                // it belongs to the UI and must NOT control the camera.
                if (IsTouchOverUI(fingerId))
                    continue;

                // This finger started on empty gameplay space.
                // It becomes our camera-look finger.
                activeLookFingerId = fingerId;

                break;
            }
        }

        // =========================================================
        // PROCESS ACTIVE LOOK FINGER
        // =========================================================

        if (activeLookFingerId == -1)
            return;

        for (int i = 0; i < touches.Count; i++)
        {
            var touch = touches[i];

            int fingerId = touch.touchId.ReadValue();

            if (fingerId != activeLookFingerId)
                continue;

            // Finger released.
            if (!touch.press.isPressed)
            {
                activeLookFingerId = -1;
                return;
            }

            // Get movement of THIS finger only.
            Vector2 touchDelta = touch.delta.ReadValue();

            yaw += touchDelta.x * touchSensitivity;

            pitch -= touchDelta.y * touchSensitivity;

            pitch = Mathf.Clamp(
                pitch,
                minPitch,
                maxPitch
            );

            return;
        }

        // Finger no longer exists.
        activeLookFingerId = -1;
    }

    // =============================================================
    // UI DETECTION
    // =============================================================

    private bool IsTouchOverUI(int fingerId)
    {
        if (EventSystem.current == null)
            return false;

        return EventSystem.current.IsPointerOverGameObject(fingerId);
    }

    // =============================================================
    // TARGET
    // =============================================================

    /// <summary>
    /// Allows other scripts to assign the player target.
    /// </summary>
    public void SetTarget(Transform newTarget)
    {
        target = newTarget;

        if (target != null)
        {
            yaw = target.eulerAngles.y;
        }
    }

    // =============================================================
    // FIRST PERSON TOGGLE
    // =============================================================

    private void ToggleFirstPerson()
    {
        firstPersonMode = !firstPersonMode;

        if (firstPersonHands != null)
        {
            if (firstPersonMode)
                firstPersonHands.Show();
            else
                firstPersonHands.Hide();
        }
    }

    // =============================================================
    // CURSOR
    // =============================================================

    private void ToggleCursorFree()
    {
        isCursorFreed = !isCursorFreed;

        if (isCursorFreed)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    // =============================================================
    // KEYBOARD DEBUG INPUT
    // =============================================================

    private void Update()
    {
        // ---------------------------------------------------------
        // FIRST PERSON TOGGLE
        // ---------------------------------------------------------
        //
        // This is desktop/editor testing only.
        // Your mobile version can later have a UI button if needed.
        //

        if (Keyboard.current != null &&
            Keyboard.current[GetKeyControl(toggleKey)].wasPressedThisFrame)
        {
            ToggleFirstPerson();
        }

        // ---------------------------------------------------------
        // FREE CURSOR
        // ---------------------------------------------------------

        if (Keyboard.current != null &&
            Keyboard.current[GetKeyControl(freeCursorKey)].wasPressedThisFrame)
        {
            ToggleCursorFree();
        }
    }

    // =============================================================
    // KEYCODE → INPUT SYSTEM KEY
    // =============================================================

    private Key GetKeyControl(KeyCode keyCode)
    {
        switch (keyCode)
        {
            case KeyCode.Q:
                return Key.Q;

            case KeyCode.Tab:
                return Key.Tab;

            case KeyCode.E:
                return Key.E;

            case KeyCode.Escape:
                return Key.Escape;

            case KeyCode.Space:
                return Key.Space;

            default:
                return Key.None;
        }
    }
}