using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Forwards a tap on the dialogue box to DialogueUI's single gesture path.
/// Taps on the Continue button inside the box are handled by the button, not
/// here, so one press can never reach both. Drags, swipes and long presses
/// that are not taps are ignored. DialogueUI adds and binds this at runtime
/// if the scene does not already have it.
/// </summary>
[DisallowMultipleComponent]
public class DialogueBoxTapRelay : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private DialogueUI dialogueUI;

    public void Bind(DialogueUI owner) => dialogueUI = owner;

    public void OnPointerClick(PointerEventData eventData)
    {
        if (dialogueUI == null || eventData == null) return;
        if (eventData.button != PointerEventData.InputButton.Left) return;
        if (MobileInputGuard.IsDragOrCancelled(eventData)) return;
        dialogueUI.HandleBoxTap();
    }
}
