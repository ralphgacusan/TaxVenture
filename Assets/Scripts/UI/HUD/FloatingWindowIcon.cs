using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Controls a HUD icon associated with a FloatingWindow.
///
/// FEATURES:
/// - Click icon while closed → open
/// - Click icon while open → close
///
/// IMPORTANT:
/// The icon remains active independently of the window.
///
/// FloatingWindow is responsible for the actual open/close state.
/// This component simply tells it to toggle.
/// </summary>
public class FloatingWindowIcon :
    MonoBehaviour,
    IPointerClickHandler
{
    // =========================================================
    // WINDOW
    // =========================================================

    [Header("Window")]

    [Tooltip(
        "The FloatingWindow controlled by this HUD icon."
    )]
    [SerializeField] private FloatingWindow window;


    // =========================================================
    // CLICK
    // =========================================================

    public void OnPointerClick(
        PointerEventData eventData)
    {
        if (window == null)
        {
            Debug.LogError(
                $"[FloatingWindowIcon] " +
                $"{gameObject.name} has no FloatingWindow assigned!"
            );

            return;
        }


        Debug.Log(
            $"[FloatingWindowIcon] " +
            $"{gameObject.name} clicked. " +
            $"Window currently active: " +
            $"{window.gameObject.activeSelf}"
        );


        // -----------------------------------------------------
        // ALWAYS TOGGLE
        // -----------------------------------------------------
        //
        // Closed → Open
        // Open → Closed
        //
        // It does not matter how the window reached
        // its current state.
        // -----------------------------------------------------

        window.ToggleWindow();
    }
}