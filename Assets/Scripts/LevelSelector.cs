using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelSelector : MonoBehaviour
{
    private void Update()
    {
        // Check for Android touch
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);

            // Only detect the initial touch
            if (touch.phase == TouchPhase.Began)
            {
                // Use the currently active camera only to create the ray
                Ray ray = Camera.main.ScreenPointToRay(touch.position);

                // Check if the ray hits a collider
                if (Physics.Raycast(ray, out RaycastHit hit))
                {
                    GameObject touchedObject = hit.collider.gameObject;

                    switch (touchedObject.name)
                    {
                        case "level1":
                            SceneManager.LoadScene("Level1");
                            break;

                        case "level2":
                            SceneManager.LoadScene("Level2");
                            break;

                        case "level3":
                            SceneManager.LoadScene("Level3");
                            break;

                        case "level4":
                            SceneManager.LoadScene("Level4");
                            break;

                        case "level5":
                            SceneManager.LoadScene("Level5");
                            break;

                        case "sidequest_button":
                            SceneManager.LoadScene("SideQuest");
                            break;
                    }
                }
            }
        }
    }
}