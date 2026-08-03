using System.Collections.Generic;
using UnityEngine;
using TMPro;

/// <summary>
/// PURPOSE:
/// Dynamic Notes/task panel. Shows task DESCRIPTIONS (from
/// TaskListProvider), not raw FSM state names.
///
/// COMPLETION LOGIC:
/// A task is marked DONE once the player reaches its corresponding FSM
/// state. Completed tasks are permanently remembered even if the player
/// revisits previous states.
/// </summary>
public class NotesPanelUI : MonoBehaviour
{
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private TextMeshProUGUI objectivesText;

    [Header("Swipe to Close")]
    [SerializeField] private SwipeDownToClose swipeToClose;


    private List<TaskDefinition> tasks;


    // Permanently stores completed tasks during gameplay
    private static HashSet<System.Type> completedStates =
        new HashSet<System.Type>();


    private void Awake()
    {
        tasks = TaskListProvider.GetTasks();

        panelRoot.SetActive(false);

        if (swipeToClose != null)
            swipeToClose.OnSwipeClosed += Hide;
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
        MarkCurrentTaskDone();
        RenderTasks();
    }


    private void MarkCurrentTaskDone()
    {
        if (GameStateMachine.Instance == null ||
            GameStateMachine.Instance.CurrentState == null)
            return;


        System.Type currentType =
            GameStateMachine.Instance.CurrentState.GetType();


        foreach (TaskDefinition task in tasks)
        {
            if (task.StateType == currentType)
            {
                completedStates.Add(task.StateType);
                break;
            }
        }
    }


    public void Show()
    {
        if (CameraController.Instance != null)
            CameraController.Instance.LockPlayerControls();


        // Update in case the panel opens after a state change
        MarkCurrentTaskDone();

        RenderTasks();

        panelRoot.SetActive(true);
    }


    public void Hide()
    {
        panelRoot.SetActive(false);

        if (CameraController.Instance != null)
            CameraController.Instance.UnlockPlayerControls();
    }


    private void RenderTasks()
    {
        var sb = new System.Text.StringBuilder();

        sb.AppendLine("Today's Tasks");
        sb.AppendLine();


        foreach (TaskDefinition task in tasks)
        {
            string marker;


            if (completedStates.Contains(task.StateType))
            {
                marker = "[DONE]";
            }
            else
            {
                marker = "[ ]";
            }


            sb.AppendLine($"{marker} {task.TaskDescription}");
        }


        objectivesText.text = sb.ToString();
    }
}