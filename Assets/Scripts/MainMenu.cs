using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    public ToggleCamera cameraManager;

    private void Update()
    {
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

                    switch (touchedObject.name)
                    {
                        case "play_button":
                            cameraManager.SwitchToLevelSelector();
                            break;

                        case "tutorial_button":
                            SceneManager.LoadScene("TutorialPage");
                            break;

                        case "settings_button":
                            SceneManager.LoadScene("SettingsPage");
                            break;
                    }
                }
            }
        }
    }
}