using UnityEngine;
using UnityEngine.EventSystems;

public class PointerBlockDebug : MonoBehaviour
{
    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            bool overUI = EventSystem.current.IsPointerOverGameObject();
            Debug.Log($"[PointerBlockDebug] IsPointerOverGameObject: {overUI}");

            if (overUI)
            {
                var pointerData = new PointerEventData(EventSystem.current)
                {
                    position = Input.mousePosition
                };

                var results = new System.Collections.Generic.List<RaycastResult>();
                EventSystem.current.RaycastAll(pointerData, results);

                foreach (var result in results)
                {
                    Debug.Log($"[PointerBlockDebug] UI hit: {result.gameObject.name}");
                }
            }
        }
    }
}