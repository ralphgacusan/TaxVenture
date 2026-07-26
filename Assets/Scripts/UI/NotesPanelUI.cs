using System.Collections.Generic;
using UnityEngine;
using TMPro;

/// <summary>
/// PURPOSE:
/// Dynamic Notes/task panel. Shows task DESCRIPTIONS (from
/// TaskListProvider), not raw FSM state names, with each task marked as
/// Done / In Progress / Pending based on how far GameStateMachine has
/// actually progressed. Reuses the existing GameStateMachine.OnStateChanged
/// event — same subscription pattern as HudStateLabel/DebugStateLabel.
///
/// COMPLETION LOGIC:
/// Tasks are ordered (per TaskListProvider). Any task whose state appears
/// BEFORE the current state in that order is marked Done. The task matching
/// the CURRENT state is marked In Progress. Everything after is Pending.
/// This means completion is derived purely from FSM position, never
/// tracked/duplicated separately — consistent with the project's existing
/// "CaseData/FSM is the single source of truth" principle.
/// </summary>
public class NotesPanelUI : MonoBehaviour
{
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private TextMeshProUGUI objectivesText;

    private List<TaskDefinition> tasks;

    private void Awake()
    {
        tasks = TaskListProvider.GetTasks();
        Hide();
    }

    private void OnEnable()
    {
        if (GameStateMachine.Instance != null)
        {
            GameStateMachine.Instance.OnStateChanged += HandleStateChanged;
        }
    }

    private void OnDisable()
    {
        if (GameStateMachine.Instance != null)
        {
            GameStateMachine.Instance.OnStateChanged -= HandleStateChanged;
        }
    }

    private void HandleStateChanged(IGameState newState)
    {
        // Refresh text live even while the panel might be closed, so it's
        // correct the instant it's opened next.
        RenderTasks();
    }

    public void Show()
    {
        RenderTasks();
        panelRoot.SetActive(true);
    }

    public void Hide()
    {
        panelRoot.SetActive(false);
    }

    private void RenderTasks()
    {
        if (GameStateMachine.Instance == null || GameStateMachine.Instance.CurrentState == null) return;

        System.Type currentType = GameStateMachine.Instance.CurrentState.GetType();
        int currentIndex = tasks.FindIndex(t => t.StateType == currentType);

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("Today's Tasks");
        sb.AppendLine();

        for (int i = 0; i < tasks.Count; i++)
        {
            string marker;
            if (currentIndex < 0)
            {
                marker = "[ ]";
            }
            else if (i < currentIndex)
            {
                marker = "[DONE]";
            }
            else if (i == currentIndex)
            {
                marker = "[NOW]";
            }
            else
            {
                marker = "[ ]";
            }

            sb.AppendLine($"{marker} {tasks[i].TaskDescription}");
        }

        objectivesText.text = sb.ToString();
    }
}