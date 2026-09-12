
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// PURPOSE:
/// The single, reusable dialogue system for every NPC conversation in the
/// game (Receptionist, Client interview, Client outcome, Auditor review,
/// any future NPC).
///
/// RESPONSIBILITIES:
/// - Show/hide the dialogue panel
/// - Play a queue of DialogueLine entries one at a time via Continue
/// - Display the correct speaker and portrait
/// - Play NPC dialogue SFX whenever a new NPC line appears
/// - Invoke a completion callback when the queue ends
/// - Lock player controls for the full duration
///
/// DOES NOT:
/// - Know anything about which NPC is talking
/// - Know what the conversation is about
/// - Know what happens after the conversation
///
/// All of that is supplied by the caller through:
/// StartDialogue(lines, onConcluded).
///
/// CONNECTS WITH:
/// - CameraController.LockPlayerControls() / UnlockPlayerControls()
/// - AudioManager.PlayNPCDialogueSFX()
/// - DialoguePortraitDatabase
/// - Any *Interactable script that needs a conversation
/// </summary>
public class DialogueUI : MonoBehaviour
{
    // =========================================================
    // PANEL
    // =========================================================

    [Header("Panel")]
    [SerializeField]
    private GameObject panelRoot;


    // =========================================================
    // PORTRAIT
    // =========================================================

    [Header("Portrait (left 25%)")]

    [SerializeField]
    private Image portraitImage;

    [SerializeField]
    private Sprite defaultPortraitPlaceholder;


    // =========================================================
    // DIALOGUE TEXT
    // =========================================================

    [Header("Dialogue Text (right 75%)")]

    [SerializeField]
    private TextMeshProUGUI speakerNameText;

    [SerializeField]
    private TextMeshProUGUI dialogueText;


    // =========================================================
    // CONTINUE BUTTON
    // =========================================================

    [Header("Continue (bottom-right)")]

    [SerializeField]
    private Button continueButton;


    // =========================================================
    // INTERNAL STATE
    // =========================================================

    private List<DialogueLine> currentLines;

    private int lineIndex;

    private System.Action onConcluded;


    // =========================================================
    // UNITY
    // =========================================================

    private void Awake()
    {
        Hide();


        if (continueButton != null)
        {
            continueButton.onClick.AddListener(
                OnContinuePressed
            );
        }
        else
        {
            Debug.LogWarning(
                "[DialogueUI] Continue Button is not assigned."
            );
        }
    }


    private void OnDestroy()
    {
        if (continueButton != null)
        {
            continueButton.onClick.RemoveListener(
                OnContinuePressed
            );
        }
    }


    // =========================================================
    // START DIALOGUE
    // =========================================================

    /// <summary>
    /// Starts a sequential conversation.
    ///
    /// Player controls are locked for the entire duration.
    /// </summary>
    public void StartDialogue(
        List<DialogueLine> lines,
        System.Action onConcludedCallback
    )
    {
        // -----------------------------------------------------
        // Validate dialogue data.
        // -----------------------------------------------------

        if (lines == null ||
            lines.Count == 0)
        {
            Debug.LogWarning(
                "[DialogueUI] StartDialogue() received " +
                "an empty dialogue list."
            );

            onConcludedCallback?.Invoke();

            return;
        }


        // -----------------------------------------------------
        // Store dialogue.
        // -----------------------------------------------------

        currentLines =
            lines;

        lineIndex =
            0;

        onConcluded =
            onConcludedCallback;


        // -----------------------------------------------------
        // Lock player controls.
        // -----------------------------------------------------

        if (CameraController.Instance != null)
        {
            CameraController.Instance
                .LockPlayerControls();
        }
        else
        {
            Debug.LogWarning(
                "[DialogueUI] " +
                "CameraController.Instance is NULL."
            );
        }


        // -----------------------------------------------------
        // Show panel.
        // -----------------------------------------------------

        if (panelRoot != null)
        {
            panelRoot.SetActive(true);
        }
        else
        {
            Debug.LogError(
                "[DialogueUI] " +
                "Panel Root is NOT assigned."
            );
        }


        // -----------------------------------------------------
        // Render first line.
        // -----------------------------------------------------

        RenderCurrentLine();
    }


    // =========================================================
    // RENDER CURRENT LINE
    // =========================================================

    private void RenderCurrentLine()
    {
        // -----------------------------------------------------
        // Safety checks.
        // -----------------------------------------------------

        if (currentLines == null ||
            currentLines.Count == 0)
        {
            return;
        }


        if (lineIndex < 0 ||
            lineIndex >= currentLines.Count)
        {
            return;
        }


        DialogueLine line =
            currentLines[lineIndex];


        // -----------------------------------------------------
        // SPEAKER NAME
        // -----------------------------------------------------

        if (speakerNameText != null)
        {
            speakerNameText.text =
                line.Speaker == DialogueSpeaker.Player
                    ? "You"
                    : line.SpeakerName;
        }


        // -----------------------------------------------------
        // DIALOGUE TEXT
        // -----------------------------------------------------

        if (dialogueText != null)
        {
            dialogueText.text =
                line.Text;
        }


        // -----------------------------------------------------
        // PORTRAIT
        // -----------------------------------------------------

        if (portraitImage != null)
        {
            Sprite portrait = null;


            if (DialoguePortraitDatabase.Instance != null)
            {
                portrait =
                    DialoguePortraitDatabase.Instance
                        .GetPortrait(
                            line.PortraitId
                        );
            }


            portraitImage.sprite =
                portrait != null
                    ? portrait
                    : defaultPortraitPlaceholder;
        }


        // =====================================================
        // NPC DIALOGUE SFX
        // =====================================================
        //
        // Play the sound ONLY for NPC dialogue.
        //
        // Player lines do not trigger the NPC voice sound.
        //
        // This means every NPC automatically gets the sound:
        //
        // Receptionist → 🔊
        // Client       → 🔊
        // Auditor      → 🔊
        // Future NPC   → 🔊
        //
        // No changes are required in their Interactable scripts.
        // =====================================================

        if (line.Speaker != DialogueSpeaker.Player)
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance
                    .PlayNPCDialogueSFX();
            }
            else
            {
                Debug.LogWarning(
                    "[DialogueUI] " +
                    "AudioManager.Instance is NULL. " +
                    "NPC dialogue SFX could not be played."
                );
            }
        }
    }


    // =========================================================
    // CONTINUE
    // =========================================================

    private void OnContinuePressed()
    {
        // -----------------------------------------------------
        // Safety check.
        // -----------------------------------------------------

        if (currentLines == null ||
            currentLines.Count == 0)
        {
            ConcludeDialogue();

            return;
        }


        // -----------------------------------------------------
        // Move to next line.
        // -----------------------------------------------------

        lineIndex++;


        // -----------------------------------------------------
        // End of dialogue.
        // -----------------------------------------------------

        if (lineIndex >= currentLines.Count)
        {
            ConcludeDialogue();

            return;
        }


        // -----------------------------------------------------
        // Display next line.
        // -----------------------------------------------------

        RenderCurrentLine();
    }


    // =========================================================
    // CONCLUDE DIALOGUE
    // =========================================================

    private void ConcludeDialogue()
    {
        Hide();


        // -----------------------------------------------------
        // Unlock player controls.
        // -----------------------------------------------------

        if (CameraController.Instance != null)
        {
            CameraController.Instance
                .UnlockPlayerControls();
        }


        // -----------------------------------------------------
        // Save callback locally before clearing it.
        // -----------------------------------------------------

        System.Action callback =
            onConcluded;

        onConcluded =
            null;

        currentLines =
            null;


        // -----------------------------------------------------
        // Notify caller.
        // -----------------------------------------------------

        callback?.Invoke();
    }


    // =========================================================
    // HIDE
    // =========================================================

    private void Hide()
    {
        if (panelRoot != null)
        {
            panelRoot.SetActive(false);
        }
    }
}
