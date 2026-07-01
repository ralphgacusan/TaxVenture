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

    private void Awake()
    {
        HidePrompt();
    }

    /// <summary>
    /// Displays the prompt panel with the given text.
    /// </summary>
    public void ShowPrompt(string message)
    {
        if (promptPanel != null) promptPanel.SetActive(true);
        if (promptText != null) promptText.text = message;
    }

    /// <summary>
    /// Hides the prompt panel entirely.
    /// </summary>
    public void HidePrompt()
    {
        if (promptPanel != null) promptPanel.SetActive(false);
    }
}