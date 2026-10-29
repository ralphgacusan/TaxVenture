using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// PURPOSE:
/// An ambient Level 1 plaza character (Living Plaza): a tap within reach
/// starts one short, optional small talk through the shared DialogueUI,
/// rotating through its talks in plaza_life.json each time.
///
/// TALK ONLY: it never drops a profile or ID, never awards EXP, hearts,
/// coins or trust, never raises a town, guide or gameplay event, never saves
/// and never changes quest state. Its triangle/check marker tracks whether
/// the player has finished a conversation, just like other plaza people.
/// NpcLife pauses its activity and turns it to the player.
/// </summary>
[RequireComponent(typeof(NpcStateMachine))]
[RequireComponent(typeof(NpcStatusMarker))]
public sealed class AmbientNpcInteractable : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler, INpcAvailabilitySource
{
    [Tooltip("The id of this character in Resources/Levels/Level_01/plaza_life.json.")]
    [SerializeField] private string ambientId;
    [Tooltip("Shown when the file has no name for this id.")]
    [SerializeField] private string fallbackName = "Townsperson";
    [SerializeField] private DialogueUI dialogueUI;

    private NpcStateMachine stateMachine;
    private HighlightEffect pointerHighlight;
    private bool talking;
    private bool hasSpoken;
    private int nextTalk;

    /// <summary>The id in plaza_life.json.</summary>
    public string AmbientId => ambientId;

    /// <summary>A runtime-built townsperson (Level 2): its id in this level's plaza_life.json, set once after AddComponent.</summary>
    public void Configure(string id, string name)
    {
        ambientId = id;
        if (!string.IsNullOrWhiteSpace(name)) fallbackName = name;
        int seed = 0;
        if (ambientId != null) foreach (char c in ambientId) seed += c;
        nextTalk = seed % 3;
    }

    /// <summary>The character's name (from plaza_life.json).</summary>
    public string DisplayName
    {
        get
        {
            AmbientNpcDefinition definition = PlazaLifeContent.Find(ambientId);
            return definition != null && !string.IsNullOrWhiteSpace(definition.name) ? definition.name : fallbackName;
        }
    }

    /// <summary>Conversation discovery only: completion never means a quest reward.</summary>
    public NpcAvailability Availability =>
        ArthurOpening.Active != null && !ArthurOpening.Active.PlazaConversationsOpen
            ? NpcAvailability.Unavailable : NpcAvailabilityRules.Colleague(hasSpoken);

    private void Awake()
    {
        stateMachine = GetComponent<NpcStateMachine>();
        pointerHighlight = HighlightEffect.For(this);
        if (dialogueUI == null) dialogueUI = FindAnyObjectByType<DialogueUI>(FindObjectsInactive.Include);
        // A different first talk per character, so a stroll past them never repeats in step.
        int seed = 0;
        if (ambientId != null) foreach (char c in ambientId) seed += c;
        nextTalk = seed % 3;
        // Existing scene instances also need the marker; RequireComponent covers new ones.
        if (GetComponent<NpcStatusMarker>() == null) gameObject.AddComponent<NpcStatusMarker>();
    }

    private void OnDisable()
    {
        ClearPointerFocus();
        if (talking && dialogueUI != null) dialogueUI.CancelDialogueFor(this);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (pointerHighlight != null) pointerHighlight.SetFocused(CanFocusPointer(eventData));
    }

    public void OnPointerExit(PointerEventData eventData) => ClearPointerFocus();

    private bool CanFocusPointer(PointerEventData eventData) =>
        eventData != null && !talking && InteractionRange.PlayerCanReach(this) &&
        !DialogueUI.IsAnyDialogueActive && !VirtualJoystick.BlocksWorldInput &&
        !MobileInputGuard.IsPointerOverUI(eventData.position);

    private void ClearPointerFocus()
    {
        if (pointerHighlight != null) pointerHighlight.SetFocused(false);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (DialogueUI.IsAnyDialogueActive || talking || dialogueUI == null) return;
        if (eventData.button != PointerEventData.InputButton.Left || MobileInputGuard.IsDragOrCancelled(eventData) ||
            VirtualJoystick.BlocksWorldInput || MobileInputGuard.IsPointerOverUI(eventData.position)) return;
        if (!InteractionRange.PlayerCanReach(this)) return;

        List<DialogueLine> lines = PlazaLifeContent.BuildTalk(PlazaLifeContent.Find(ambientId), nextTalk, fallbackName);
        if (lines == null || lines.Count == 0) return;
        if (!dialogueUI.TryStartDialogue(lines, () => EndTalk(true), () => EndTalk(false), this)) return;

        talking = true;
        AudioManager.Instance?.QueueInteractionSFX("npc");
        if (stateMachine != null)
        {
            stateMachine.ChangeState(new NpcInteractState());
            stateMachine.ChangeState(new NpcDialogueState());
        }
        ClearPointerFocus();
    }

    private void EndTalk(bool completed)
    {
        talking = false;
        // Replayable: a finished talk moves on to the next one; a cancelled one is offered again.
        if (completed)
        {
            hasSpoken = true;
            nextTalk++;
        }
        if (stateMachine != null) stateMachine.ChangeState(new NpcIdleState());
    }
}
