using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections.Generic;

public class WorldSpaceBackButton : MonoBehaviour
{
    public ToggleCamera cameraManager;
    public Canvas canvas;

    private GraphicRaycaster raycaster;

    private void Start()
    {
        raycaster = canvas.GetComponent<GraphicRaycaster>();

        if (raycaster == null)
        {
            Debug.LogError("Graphic Raycaster is missing from the Canvas!");
        }
    }

    private void Update()
    {
        // Android touch
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);

            // Only detect the initial touch
            if (touch.phase == TouchPhase.Began)
            {
                CheckTouch(touch.position, touch.fingerId);
            }
        }
    }

    private void CheckTouch(Vector2 screenPosition, int fingerId)
    {
        if (raycaster == null)
            return;

        // Create pointer data
        PointerEventData pointerData =
            new PointerEventData(EventSystem.current);

        pointerData.position = screenPosition;
        pointerData.pointerId = fingerId;

        // Store objects hit by the UI raycast
        List<RaycastResult> results =
            new List<RaycastResult>();

        raycaster.Raycast(pointerData, results);

        // Check everything touched
        foreach (RaycastResult result in results)
        {
            if (result.gameObject == gameObject ||
                result.gameObject.transform.IsChildOf(transform))
            {
                Debug.Log("BACK BUTTON TOUCHED!");

                if (cameraManager != null)
                {
                    Debug.Log("Calling SwitchToMainMenu!");

                    cameraManager.SwitchToMainMenu();
                }
                else
                {
                    Debug.LogError(
                        "Camera Manager is NOT assigned!"
                    );
                }

                return;
            }
        }
    }
}