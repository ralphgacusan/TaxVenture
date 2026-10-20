using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    public ToggleCamera cameraManager;

    [Tooltip("The settings board in this scene (settings open here; there is no Settings scene).")]
    [SerializeField] private SettingsBoard3D settingsBoard;

    private readonly List<KeyValuePair<GameObject, bool>> careerButtons = new List<KeyValuePair<GameObject, bool>>();
    private Transform settingsMenuButton;
    private Transform tutorialMenuButton;
    private Vector3 settingsMenuPosition;

    private void Awake()
    {
        foreach (GameObject root in gameObject.scene.GetRootGameObjects())
            foreach (Transform node in root.GetComponentsInChildren<Transform>(true))
            {
                if (node.name == "tutorial_button" || node.name == "sidequest_button")
                    careerButtons.Add(new KeyValuePair<GameObject, bool>(node.gameObject, node.gameObject.activeSelf));
                if (node.name == "tutorial_button") tutorialMenuButton = node;
                if (node.name == "settings_button") settingsMenuButton = node;
            }
        if (settingsMenuButton != null) settingsMenuPosition = settingsMenuButton.localPosition;
    }

    private void OnEnable() => PlayerProgressManager.ProgressChanged += RefreshMenu;
    private void OnDisable() => PlayerProgressManager.ProgressChanged -= RefreshMenu;
    private void Start() => RefreshMenu(null);

    private void RefreshMenu(PlayerProgressData _)
    {
        foreach (KeyValuePair<GameObject, bool> entry in careerButtons)
            if (entry.Key != null) entry.Key.SetActive(MenuEntry.HasCareerAccess && entry.Value);
        if (settingsMenuButton != null)
        {
            Vector3 position = settingsMenuPosition;
            if (!MenuEntry.HasCareerAccess && tutorialMenuButton != null)
                position.y = settingsMenuButton.parent != null
                    ? settingsMenuButton.parent.InverseTransformPoint(tutorialMenuButton.position).y
                    : tutorialMenuButton.position.y;
            settingsMenuButton.localPosition = position;
        }
    }

    private void Update()
    {
        if (SceneFade.BlocksInput) return;
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
                            MenuEntry.Play(cameraManager);
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
        if (!MenuEntry.HasCareerAccess) return;
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
