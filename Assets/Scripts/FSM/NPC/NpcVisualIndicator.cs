using UnityEngine;

/// <summary>
/// PURPOSE:
/// Debug-friendly visual feedback for an NPC's current FSM state, using a
/// simple color change on the NPC's material (no animations exist yet per
/// Phase 1 scope). Lets you visually confirm Idle -> Waiting -> Interact ->
/// Dialogue -> Completed transitions during testing.
///
/// RESPONSIBILITIES:
/// - Subscribe to this NPC's NpcStateMachine.OnStateChanged
/// - Swap the NPC's renderer material/color based on state name
///
/// CONNECTS WITH:
/// - NpcStateMachine (same GameObject): subscribes to its OnStateChanged event
///
/// NOTE:
/// This intentionally does NOT use HighlightEffect from Milestone 2 — that
/// component is for interaction-focus highlighting (temporary, while looked
/// at). This is a persistent state indicator layered on top of, and
/// independent from, focus highlighting.
/// </summary>
[RequireComponent(typeof(NpcStateMachine))]
[RequireComponent(typeof(Renderer))]
public class NpcVisualIndicator : MonoBehaviour
{
    [Header("State Colors")]
    [SerializeField] private Color idleColor = Color.gray;
    [SerializeField] private Color waitingColor = Color.yellow;
    [SerializeField] private Color interactColor = new Color(1f, 0.6f, 0f); // orange
    [SerializeField] private Color dialogueColor = Color.cyan;
    [SerializeField] private Color completedColor = Color.green;

    private NpcStateMachine stateMachine;
    private Renderer npcRenderer;

    private void Awake()
    {
        stateMachine = GetComponent<NpcStateMachine>();
        npcRenderer = GetComponent<Renderer>();
    }

    private void OnEnable()
    {
        stateMachine.OnStateChanged += HandleStateChanged;
    }

    private void OnDisable()
    {
        stateMachine.OnStateChanged -= HandleStateChanged;
    }

    private void HandleStateChanged(INpcState newState)
    {
        Color targetColor = newState.StateName switch
        {
            "Idle" => idleColor,
            "Waiting" => waitingColor,
            "Interact" => interactColor,
            "Dialogue" => dialogueColor,
            "Completed" => completedColor,
            _ => idleColor
        };

        npcRenderer.material.color = targetColor;
    }
}