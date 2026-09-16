using UnityEngine;

public class MouseRaycastDebug : MonoBehaviour
{
    [SerializeField] private Camera mainCamera;

    private void Update()
    {
        if (!Input.GetMouseButtonDown(0))
        {
            return;
        }

        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit, 1000f))
        {
            Debug.Log(
                $"[MouseRaycastDebug] Hit object: {hit.collider.gameObject.name}"
            );

            Debug.Log(
                $"[MouseRaycastDebug] Hit layer: " +
                LayerMask.LayerToName(hit.collider.gameObject.layer)
            );
        }
        else
        {
            Debug.Log(
                "[MouseRaycastDebug] Ray did not hit any collider."
            );
        }
    }
}