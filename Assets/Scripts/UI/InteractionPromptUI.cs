using UnityEngine;
using TMPro;

/// <summary>
/// PURPOSE:
/// Controls the simple on-screen prompt (e.g. "Click to open Desk") that
/// appears when the player is focused on an interactable object.
///
/// RESPONSIBILITIES:
/// - Show/hide a UI panel containing prompt text
/// - Update the text based on what Interactor.cs tells it to display
///
/// DOES NOT:
/// - Decide WHEN to show/hide — that's Interactor's job. This script is a
///   simple "dumb" UI component that just displays what it's told.
///
/// CONNECTS WITH:
/// - Interactor.cs calls ShowPrompt() / HidePrompt() on this script.
/// </summary>
public class InteractionPromptUI : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("The parent panel GameObject containing the prompt (enabled/disabled to show/hide).")]
    [SerializeField] private GameObject promptPanel;

    [Tooltip("The text component displaying the prompt message.")]
    [SerializeField] private TextMeshProUGUI promptText;

    /// <summary>
    /// Optional presenter (the Office HUD's prompt pill). When it accepts a
    /// message, the plain panel stays hidden. Same show/hide timing as before.
    /// </summary>
    public static System.Func<string, bool> ShowOverride;
    public static System.Action HideOverride;

    private void Awake()
    {
        HidePrompt();
    }

    /// <summary>
    /// Displays the prompt panel with the given text.
    /// </summary>
    public void ShowPrompt(string message)
    {
        if (promptText != null) promptText.text = message;
        if (ShowOverride != null && ShowOverride(message))
        {
            if (promptPanel != null) promptPanel.SetActive(false);
            return;
        }
        if (promptPanel != null) promptPanel.SetActive(true);
    }

    /// <summary>
    /// Hides the prompt panel entirely.
    /// </summary>
    public void HidePrompt()
    {
        HideOverride?.Invoke();
        if (promptPanel != null) promptPanel.SetActive(false);
    }
}