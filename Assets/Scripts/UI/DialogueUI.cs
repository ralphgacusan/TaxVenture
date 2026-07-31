using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// PURPOSE:
/// THE single, reusable dialogue system for every NPC conversation in the
/// game (Receptionist, Client interview, Client outcome, Auditor review,
/// any future NPC). Replaces InterviewClientUI's question-button interview
/// mode and the separate AuditorDialogueUI entirely — this is now the only
/// dialogue panel in the project.
///
/// LAYOUT (per R4 spec):
/// - Left 25%: circular portrait placeholder (swaps per DialogueLine.PortraitId
///   once real art exists; currently a single placeholder sprite regardless)
/// - Right 75%: dialogue text
/// - Bottom-right: Continue button
///
/// RESPONSIBILITIES:
/// - Show/hide the panel
/// - Play a queue of DialogueLine entries one at a time via Continue
/// - Invoke a completion callback when the queue ends
/// - Lock player controls for the full duration (CameraController)
///
/// DOES NOT:
/// - Know anything about WHICH npc is talking, what the conversation is
///   ABOUT, or what happens after — all of that is supplied by the caller
///   (ReceptionistInteractable, ClientInteractable, AuditorInteractable)
///   via StartDialogue(lines, onConcluded).
///
/// CONNECTS WITH:
/// - CameraController.LockPlayerControls() / UnlockPlayerControls()
/// - Any *Interactable script that needs a conversation
/// </summary>
public class DialogueUI : MonoBehaviour
{
    [Header("Panel")]
    [SerializeField] private GameObject panelRoot;

    [Header("Portrait (left 25%)")]
    [SerializeField] private Image portraitImage;
    [SerializeField] private Sprite defaultPortraitPlaceholder;

    [Header("Dialogue Text (right 75%)")]
    [SerializeField] private TextMeshProUGUI speakerNameText; // optional, e.g. "Client" / "You"
    [SerializeField] private TextMeshProUGUI dialogueText;

    [Header("Continue (bottom-right)")]
    [SerializeField] private Button continueButton;

    private List<DialogueLine> currentLines;
    private int lineIndex;
    private System.Action onConcluded;

    private void Awake()
    {
        Hide();
        continueButton.onClick.AddListener(OnContinuePressed);
    }

    /// <summary>
    /// Starts a sequential conversation. Locks player controls for the
    /// entire duration; unlocks automatically when the queue concludes.
    /// </summary>
    public void StartDialogue(List<DialogueLine> lines, System.Action onConcludedCallback)
    {
        currentLines = lines;
        lineIndex = 0;
        onConcluded = onConcludedCallback;

        CameraController.Instance.LockPlayerControls();

        panelRoot.SetActive(true);
        RenderCurrentLine();
    }

    private void RenderCurrentLine()
    {
        if (lineIndex >= currentLines.Count) return;

        DialogueLine line = currentLines[lineIndex];

        speakerNameText.text = line.Speaker == DialogueSpeaker.Npc ? "" : "You"; // NPC name filled per-call if desired via a future field; "You" for player lines
        dialogueText.text = line.Text;

        // Placeholder portrait swap hook — currently always the same sprite,
        // but reads PortraitId so future art only requires a lookup here.
        portraitImage.sprite = defaultPortraitPlaceholder;
    }

    private void OnContinuePressed()
    {
        lineIndex++;

        if (lineIndex >= currentLines.Count)
        {
            ConcludeDialogue();
            return;
        }

        RenderCurrentLine();
    }

    private void ConcludeDialogue()
    {
        Hide();
        CameraController.Instance.UnlockPlayerControls();
        onConcluded?.Invoke();
    }

    private void Hide()
    {
        panelRoot.SetActive(false);
    }
}