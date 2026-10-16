using System;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>Forwards a click/tap on its (transparent) graphic to a callback. Used by the guide.</summary>
public sealed class GuideTapCatcher : MonoBehaviour, IPointerClickHandler
{
    public Action Tapped;

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) return;
        Tapped?.Invoke();
    }
}
