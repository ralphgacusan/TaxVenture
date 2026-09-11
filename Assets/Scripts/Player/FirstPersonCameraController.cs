
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

/// <summary>
/// FIRST-PERSON CAMERA CONTROLLER
///
/// Controls the Main Camera ONLY during normal FirstPerson mode.
///
/// When CameraController changes to:
/// - Workstation
/// - Interview
///
/// this script stops controlling the camera even if the component
/// accidentally gets re-enabled by another script or Unity.
/// </summary>
public class FirstPersonCameraController : MonoBehaviour
{
    [Header("Player")]
    [Tooltip("Player transform used to position the first-person camera.")]
    [SerializeField] private Transform player;

    [Header("Camera Position")]
    [Tooltip("Height of the camera above the player's position.")]
    [SerializeField] private float cameraHeight = 0.8f;

    [Header("Look Settings")]
    [Tooltip("Touch look sensitivity.")]
    [SerializeField] private float sensitivity = 0.15f;

    [Header("Vertical Look")]
    [SerializeField] private float minPitch = -60f;
    [SerializeField] private float maxPitch = 60f;

    [Header("Touch Look")]
    [SerializeField] private bool disableTouchLook = false;

    [Header("Mouse Look")]
    [Tooltip("Mouse look sensitivity for Editor/Desktop testing.")]
    [SerializeField] private float mouseSensitivity = 3f;

    private float yaw;
    private float pitch;

    private int activeLookFingerId = -1;

    private void Start()
    {
        if (player == null)
        {
            Debug.LogError(
                "[FirstPersonCameraController] Player is not assigned."
            );

            return;
        }

        yaw = player.eulerAngles.y;
        pitch = 0f;

#if UNITY_EDITOR || UNITY_STANDALONE
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
#endif
    }

    private void LateUpdate()
    {
        if (player == null)
            return;

        // ============================================================
        // IMPORTANT:
        // This controller is allowed to control the camera ONLY
        // during normal FirstPerson mode.
        // ============================================================

        if (CameraController.Instance != null &&
            CameraController.Instance.CurrentMode !=
            CameraController.CameraMode.FirstPerson)
        {
            return;
        }

        HandleMouseLook();
        HandleTouchLook();

        transform.position =
            player.position +
            Vector3.up * cameraHeight;

        transform.rotation =
            Quaternion.Euler(
                pitch,
                yaw,
                0f
            );
    }

    private void HandleMouseLook()
    {
        if (Mouse.current == null)
            return;

        Vector2 mouseDelta =
            Mouse.current.delta.ReadValue();

        if (mouseDelta.sqrMagnitude < 0.000001f)
            return;

        yaw +=
            mouseDelta.x *
            mouseSensitivity;

        pitch -=
            mouseDelta.y *
            mouseSensitivity;

        pitch =
            Mathf.Clamp(
                pitch,
                minPitch,
                maxPitch
            );
    }

    private void HandleTouchLook()
    {
        if (disableTouchLook)
            return;

        if (Touchscreen.current == null)
            return;

        var touches =
            Touchscreen.current.touches;

        // Find a new look finger
        if (activeLookFingerId == -1)
        {
            for (int i = 0; i < touches.Count; i++)
            {
                var touch = touches[i];

                if (!touch.press.wasPressedThisFrame)
                    continue;

                int fingerId =
                    touch.touchId.ReadValue();

                // Ignore UI touches
                if (IsTouchOverUI(fingerId))
                    continue;

                activeLookFingerId =
                    fingerId;

                break;
            }
        }

        if (activeLookFingerId == -1)
            return;

        // Process active look finger
        for (int i = 0; i < touches.Count; i++)
        {
            var touch = touches[i];

            int fingerId =
                touch.touchId.ReadValue();

            if (fingerId != activeLookFingerId)
                continue;

            // Finger released
            if (!touch.press.isPressed)
            {
                activeLookFingerId = -1;
                return;
            }

            Vector2 touchDelta =
                touch.delta.ReadValue();

            yaw +=
                touchDelta.x *
                sensitivity;

            pitch -=
                touchDelta.y *
                sensitivity;

            pitch =
                Mathf.Clamp(
                    pitch,
                    minPitch,
                    maxPitch
                );

            return;
        }

        activeLookFingerId = -1;
    }

    private bool IsTouchOverUI(int fingerId)
    {
        if (EventSystem.current == null)
            return false;

        return EventSystem.current
            .IsPointerOverGameObject(fingerId);
    }

    public void SetPlayer(Transform newPlayer)
    {
        player = newPlayer;

        if (player != null)
        {
            yaw =
                player.eulerAngles.y;

            pitch = 0f;
        }
    }

    public void ResetLook()
    {
        if (player == null)
            return;

        yaw =
            player.eulerAngles.y;

        pitch = 0f;
    }
}

