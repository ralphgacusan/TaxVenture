using UnityEngine;
using UnityEngine.EventSystems;

public class RawPointerTest : MonoBehaviour, IPointerClickHandler, IPointerDownHandler
{
    public void OnPointerClick(PointerEventData eventData)
    {
        Debug.Log("[RawPointerTest] OnPointerClick fired!");
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        Debug.Log("[RawPointerTest] OnPointerDown fired!");
    }
}