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
    // PAPER VIEW (Office HUD)
    // =========================================================

    /// <summary>
    /// When set (Office_Level1, by OfficeHudRuntime), Notes renders as a
    /// paper page of the level objectives and, like the taxpayer profile
    /// viewer, locks walking/looking and hides the joysticks while open.
    /// Other scenes keep the original text list and free movement.
    /// </summary>
    private NotesPaperView paperView;

    // Set by ShowAt(): open where a dragged Notes paper was dropped.
    private Vector2? pendingOpenAt;


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

        // The panel deactivates itself on Hide (panelRoot is this object),
        // so this also covers any other way the panel gets disabled.
        ReleaseViewerLock();
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
        // Paper view: hold the player still while reading,
        // exactly like the taxpayer profile viewer (movement,
        // look and interaction off, joysticks hidden).
        // At the desk/computer the player is already still.
        // -----------------------------------------------------

        if (paperView != null)
        {
            HudWindowLock.Acquire(this);
        }


        // -----------------------------------------------------
        // Open the existing FloatingWindow.
        //
        // No new FloatingWindow is created.
        // -----------------------------------------------------

        if (floatingWindow != null)
        {
            if (pendingOpenAt.HasValue)
            {
                floatingWindow.SetPaperOpenOrigin(pendingOpenAt.Value);
                floatingWindow.OpenWindowAtScreenPosition(pendingOpenAt.Value);
            }
            else
            {
                floatingWindow.OpenWindow();
            }
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

        pendingOpenAt = null;


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
        // Without the paper view (other scenes), Notes is only a
        // floating UI window: the player can keep walking.
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
        // Hide the visual panel. The paper view lets the
        // FloatingWindow animate out and deactivate itself.
        // -----------------------------------------------------

        if (panelRoot != null && (paperView == null || floatingWindow == null))
        {
            panelRoot.SetActive(false);
        }


        // -----------------------------------------------------
        // Give back only the hold Notes itself took (paper view).
        // -----------------------------------------------------

        ReleaseViewerLock();
    }


    private void ReleaseViewerLock()
    {
        HudWindowLock.Release(this);
    }


    /// <summary>True while Notes is open (a closing paper counts as closed).</summary>
    public bool IsOpen =>
        floatingWindow != null ? floatingWindow.IsOpen() : panelRoot != null && panelRoot.activeSelf;

    /// <summary>
    /// Opens Notes where a Notes paper dragged out of its HUD icon was
    /// dropped. Returns false when Notes is already open.
    /// </summary>
    public bool ShowAt(Vector2 screenPosition)
    {
        if (IsOpen)
        {
            return false;
        }

        pendingOpenAt = screenPosition;
        Show();
        return true;
    }

    /// <summary>
    /// Switches Notes to the paper objectives page (called by OfficeHudRuntime).
    /// </summary>
    public void UsePaperView(NotesPaperView view)
    {
        paperView = view;
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
            paperView != null && floatingWindow != null
                ? floatingWindow.IsOpen()
                : panelRoot != null && panelRoot.activeSelf;


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
        // Paper view renders the level objectives itself.
        if (paperView != null)
        {
            paperView.Render();
            return;
        }

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


        // Shared presentation rule (TaskProgress): a task is done once the
        // game has entered its state; the first unfinished task is "now".
        TaskProgressSnapshot progress = TaskProgress.Evaluate(
            tasks,
            task => task.StateType != null && completedStates.Contains(task.StateType)
        );


        for (int i = 0; i < tasks.Count; i++)
        {
            TaskDefinition task = tasks[i];

            if (task.IsHeader)
            {
                sb.AppendLine(
                    $"<b><color=#7A5A1E>{task.TaskDescription}</color></b>"
                );
                sb.AppendLine();
                continue;
            }

            sb.AppendLine(FormatTaskRow(progress.Status[i], task.TaskDescription));
        }


        objectivesText.text =
            sb.ToString();


        Debug.Log(
            $"[NotesPanelUI] {name}: " +
            "Task contents refreshed."
        );
    }


    /// <summary>
    /// One Notes row: a small coloured status tag, then the task text.
    /// Colours are dark enough to read on the white Notes paper.
    /// </summary>
    private static string FormatTaskRow(TaskRowStatus status, string text)
    {
        switch (status)
        {
            case TaskRowStatus.Done:
                return $"<size=70%><b><color=#2E8B57>DONE</color></b></size><pos=150><color=#8A8A8A><s>{text}</s></color>";

            case TaskRowStatus.Active:
                return $"<size=70%><b><color=#1F6FB2>NOW</color></b></size><pos=150><b>{text}</b>";

            default:
                return $"<size=70%><b><color=#8A8A8A>TO DO</color></b></size><pos=150>{text}";
        }
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