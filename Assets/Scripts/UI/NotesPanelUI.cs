using System.Collections.Generic;
using System.Text;
using UnityEngine;
using TMPro;

/// <summary>
/// PURPOSE:
/// Dynamic Notes/task panel. Shows task DESCRIPTIONS from
/// TaskListProvider, not raw FSM state names.
///
/// COMPLETION LOGIC:
/// A task is marked DONE once the player reaches its corresponding FSM
/// state. Completed tasks are permanently remembered even if the player
/// revisits previous states.
///
/// FLOATING WINDOW:
/// The Notes panel optionally uses a FloatingWindow component.
/// This does NOT instantiate another window.
/// It uses the FloatingWindow already attached to the Notes panel.
///
/// IMPORTANT:
/// - NotesPanelUI may remain active at startup.
/// - The actual Notes visual panel remains hidden until opened.
/// - Task contents are prepared before the first opening.
/// - Opening Notes refreshes the contents.
/// - Opening Notes DOES NOT lock player movement or camera controls.
/// </summary>
public class NotesPanelUI : MonoBehaviour
{
    // =========================================================
    // PANEL
    // =========================================================

    [Header("Panel")]

    [Tooltip(
        "The actual visible Notes UI panel. " +
        "This should normally be a child of the object containing " +
        "NotesPanelUI and FloatingWindow."
    )]
    [SerializeField]
    private GameObject panelRoot;

    [SerializeField]
    private TextMeshProUGUI objectivesText;


    // =========================================================
    // FLOATING WINDOW
    // =========================================================

    [Header("Floating Window")]

    [Tooltip(
        "FloatingWindow attached to this Notes controller. " +
        "This does NOT instantiate another window."
    )]
    [SerializeField]
    private FloatingWindow floatingWindow;


    // =========================================================
    // SWIPE TO CLOSE
    // =========================================================

    [Header("Swipe to Close")]

    [SerializeField]
    private SwipeDownToClose swipeToClose;


    // =========================================================
    // TASKS
    // =========================================================

    private List<TaskDefinition> tasks;


    // =========================================================
    // COMPLETED STATES
    // =========================================================

    /// <summary>
    /// Permanently stores completed task states during gameplay.
    /// </summary>
    private static HashSet<System.Type> completedStates =
        new HashSet<System.Type>();


    // =========================================================
    // AWAKE
    // =========================================================

    private void Awake()
    {
        // -----------------------------------------------------
        // Load tasks immediately.
        // -----------------------------------------------------

        tasks = TaskListProvider.GetTasks();


        // -----------------------------------------------------
        // Automatically find FloatingWindow if necessary.
        // -----------------------------------------------------

        if (floatingWindow == null)
        {
            floatingWindow = GetComponent<FloatingWindow>();
        }


        // -----------------------------------------------------
        // Validate references.
        // -----------------------------------------------------

        if (panelRoot == null)
        {
            Debug.LogError(
                $"[NotesPanelUI] {name}: " +
                "Panel Root is not assigned!"
            );
        }

        if (objectivesText == null)
        {
            Debug.LogError(
                $"[NotesPanelUI] {name}: " +
                "Objectives Text is not assigned!"
            );
        }


        // -----------------------------------------------------
        // IMPORTANT:
        //
        // Hide ONLY the visual panel.
        //
        // Do NOT disable the NotesPanelUI GameObject.
        // This allows the script to prepare its contents and
        // listen for FSM state changes.
        // -----------------------------------------------------

        if (panelRoot != null)
        {
            panelRoot.SetActive(false);
        }


        // -----------------------------------------------------
        // Prepare the initial task contents.
        //
        // This does NOT open the Notes panel.
        // It only populates the text so the first opening
        // already has the correct values.
        // -----------------------------------------------------

        PrepareContent();


        // -----------------------------------------------------
        // Subscribe to swipe close.
        // -----------------------------------------------------

        if (swipeToClose != null)
        {
            swipeToClose.OnSwipeClosed += Hide;
        }
    }


    // =========================================================
    // START
    // =========================================================

    private void Start()
    {
        // -----------------------------------------------------
        // Refresh once more after other Awake() methods have
        // completed.
        //
        // Still does NOT open the panel.
        // -----------------------------------------------------

        PrepareContent();
    }


    // =========================================================
    // ENABLE
    // =========================================================

    private void OnEnable()
    {
        // -----------------------------------------------------
        // Listen for FSM state changes.
        //
        // IMPORTANT:
        // We do NOT call Show() here.
        // We do NOT open the panel here.
        // -----------------------------------------------------

        if (GameStateMachine.Instance != null)
        {
            GameStateMachine.Instance.OnStateChanged +=
                HandleStateChanged;
        }
    }


    // =========================================================
    // DISABLE
    // =========================================================

    private void OnDisable()
    {
        if (GameStateMachine.Instance != null)
        {
            GameStateMachine.Instance.OnStateChanged -=
                HandleStateChanged;
        }
    }


    // =========================================================
    // DESTROY
    // =========================================================

    private void OnDestroy()
    {
        if (swipeToClose != null)
        {
            swipeToClose.OnSwipeClosed -= Hide;
        }
    }


    // =========================================================
    // STATE CHANGED
    // =========================================================

    private void HandleStateChanged(IGameState newState)
    {
        // -----------------------------------------------------
        // Mark the task corresponding to the new state as done.
        // -----------------------------------------------------

        MarkCurrentTaskDone();


        // -----------------------------------------------------
        // Immediately refresh the Notes content.
        //
        // This happens even while the Notes panel is closed.
        // Therefore, the next time the user opens Notes,
        // the information is already current.
        // -----------------------------------------------------

        RenderTasks();
    }


    // =========================================================
    // PREPARE CONTENT
    // =========================================================

    /// <summary>
    /// Prepares the Notes contents without opening the panel.
    ///
    /// PREPARE CONTENT != SHOW PANEL
    /// </summary>
    private void PrepareContent()
    {
        // -----------------------------------------------------
        // Make sure tasks are loaded.
        // -----------------------------------------------------

        if (tasks == null)
        {
            tasks = TaskListProvider.GetTasks();
        }


        // -----------------------------------------------------
        // Update task completion.
        // -----------------------------------------------------

        MarkCurrentTaskDone();


        // -----------------------------------------------------
        // Render the task list.
        // -----------------------------------------------------

        RenderTasks();
    }


    // =========================================================
    // MARK CURRENT TASK DONE
    // =========================================================

    private void MarkCurrentTaskDone()
    {
        if (GameStateMachine.Instance == null ||
            GameStateMachine.Instance.CurrentState == null)
        {
            return;
        }

        if (tasks == null)
        {
            return;
        }


        System.Type currentType =
            GameStateMachine.Instance
                .CurrentState
                .GetType();


        foreach (TaskDefinition task in tasks)
        {
            if (task.StateType == currentType)
            {
                completedStates.Add(task.StateType);
                break;
            }
        }
    }


    // =========================================================
    // SHOW
    // =========================================================

    public void Show()
    {
        Debug.Log(
            $"[NotesPanelUI] {name}: SHOW"
        );


        // -----------------------------------------------------
        // Refresh content BEFORE opening.
        //
        // This guarantees that the first opening already has
        // the latest values.
        // -----------------------------------------------------

        PrepareContent();


        // -----------------------------------------------------
        // Open the existing FloatingWindow.
        //
        // No new FloatingWindow is created.
        // -----------------------------------------------------

        if (floatingWindow != null)
        {
            floatingWindow.OpenWindow();
        }
        else
        {
            // -------------------------------------------------
            // Fallback if FloatingWindow is not assigned.
            // -------------------------------------------------

            if (panelRoot != null)
            {
                panelRoot.SetActive(true);
            }
        }


        // -----------------------------------------------------
        // Make sure the visual panel is active.
        //
        // This is useful when panelRoot is a child object that
        // was hidden at startup.
        // -----------------------------------------------------

        if (panelRoot != null)
        {
            panelRoot.SetActive(true);
        }


        // -----------------------------------------------------
        // IMPORTANT:
        //
        // NO CameraController.LockPlayerControls() HERE.
        //
        // The player is now allowed to:
        // - Walk
        // - Look around
        // - Move the camera
        // - Continue normal gameplay
        //
        // Notes is only a floating UI window.
        // -----------------------------------------------------


        // -----------------------------------------------------
        // Tutorial.
        // -----------------------------------------------------

        if (TutorialController.Instance != null)
        {
            TutorialController.Instance.ReportInteraction(
                "notes_opened"
            );
        }
    }


    // =========================================================
    // HIDE
    // =========================================================

    public void Hide()
    {
        Debug.Log(
            $"[NotesPanelUI] {name}: HIDE"
        );


        // -----------------------------------------------------
        // Close the existing FloatingWindow.
        // -----------------------------------------------------

        if (floatingWindow != null)
        {
            floatingWindow.CloseWindow();
        }


        // -----------------------------------------------------
        // Hide the visual panel.
        // -----------------------------------------------------

        if (panelRoot != null)
        {
            panelRoot.SetActive(false);
        }


        // -----------------------------------------------------
        // IMPORTANT:
        //
        // NO CameraController.UnlockPlayerControls() HERE.
        //
        // Notes never locked the player, so Notes should never
        // be responsible for unlocking the player either.
        // -----------------------------------------------------
    }


    // =========================================================
    // TOGGLE
    // =========================================================

    /// <summary>
    /// Opens Notes if closed.
    /// Closes Notes if open.
    ///
    /// Connect this method to the Notes icon Button's OnClick.
    /// </summary>
    public void Toggle()
    {
        // -----------------------------------------------------
        // Determine whether the visual panel is currently open.
        // -----------------------------------------------------

        bool isOpen =
            panelRoot != null &&
            panelRoot.activeSelf;


        // -----------------------------------------------------
        // Toggle.
        // -----------------------------------------------------

        if (isOpen)
        {
            Hide();
        }
        else
        {
            Show();
        }
    }


    // =========================================================
    // RENDER TASKS
    // =========================================================

    private void RenderTasks()
    {
        if (objectivesText == null)
        {
            return;
        }

        if (tasks == null)
        {
            return;
        }


        var sb = new StringBuilder();


        sb.AppendLine("Today's Tasks");
        sb.AppendLine();


        foreach (TaskDefinition task in tasks)
        {
            string marker =
                completedStates.Contains(task.StateType)
                    ? "[DONE]"
                    : "[ ]";


            sb.AppendLine(
                $"{marker} {task.TaskDescription}"
            );
        }


        objectivesText.text =
            sb.ToString();


        Debug.Log(
            $"[NotesPanelUI] {name}: " +
            "Task contents refreshed."
        );
    }


    // =========================================================
    // REFRESH
    // =========================================================

    /// <summary>
    /// Refreshes the task contents without opening the panel.
    /// </summary>
    public void Refresh()
    {
        PrepareContent();
    }


    // =========================================================
    // RESET COMPLETED TASKS
    // =========================================================

    /// <summary>
    /// Clears all completed task states.
    ///
    /// Call this when starting a completely new game/case.
    /// </summary>
    public static void ResetCompletedTasks()
    {
        completedStates.Clear();
    }


    // =========================================================
    // CHECK TASK
    // =========================================================

    public static bool IsTaskCompleted(
        System.Type stateType
    )
    {
        if (stateType == null)
        {
            return false;
        }


        return completedStates.Contains(stateType);
    }
}