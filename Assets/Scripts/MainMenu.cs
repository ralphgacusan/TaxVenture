using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    public ToggleCamera cameraManager;

    [Tooltip("The settings board in this scene (settings open here; there is no Settings scene).")]
    [SerializeField] private SettingsBoard3D settingsBoard;

    private void Update()
    {
        // While settings are open the menu buttons are hidden and the board takes the touches.
        if (settingsBoard != null && settingsBoard.IsOpen) return;
        // Same while the training binder is up (or the camera is leaning to it).
        if (TrainingBinder3D.Instance != null && TrainingBinder3D.Instance.IsOpen) return;

        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);

            if (touch.phase == TouchPhase.Began)
            {
                Camera activeCamera = Camera.main;

                if (activeCamera == null)
                {
                    Debug.LogError("No Main Camera found!");
                    return;
                }

                Ray ray = activeCamera.ScreenPointToRay(touch.position);

                if (Physics.Raycast(ray, out RaycastHit hit))
                {
                    GameObject touchedObject = hit.collider.gameObject;

                    Debug.Log("Menu touch hit: " + touchedObject.name);

                    switch (touchedObject.name)
                    {
                        case "play_button":
                            cameraManager.SwitchToLevelSelector();
                            break;

                        case "tutorial_button":
                            OpenTraining();
                            break;

                        case "settings_button":
                            OpenSettings();
                            break;
                    }
                }
            }
        }
    }

    /// <summary>Tutorial Hub: the camera leans down to the desk and Arthur's training binder comes up.</summary>
    private void OpenTraining()
    {
        if (TrainingBinder3D.Instance != null) TrainingBinder3D.Instance.Open();
        else Debug.LogWarning("[MainMenu] No TrainingBinder3D in this scene.");
    }

    private void OpenSettings()
    {
        if (settingsBoard == null) settingsBoard = FindAnyObjectByType<SettingsBoard3D>();
        if (settingsBoard != null) settingsBoard.Open();
        else Debug.LogWarning("[MainMenu] No SettingsBoard3D in this scene.");
    }
}
