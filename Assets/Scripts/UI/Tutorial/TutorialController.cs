using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// PURPOSE:
/// Drives the tutorial popup sequence for a level. Loads step data from
/// tutorial_steps.json (StreamingAssets), auto-starts on scene load, shows
/// each step via TutorialUI, and advances either:
///   - immediately after Continue (if the step has no triggerId), or
///   - only after ReportInteraction(triggerId) is called with a matching id
///     by ANY interactable's OnInteract().
///
/// INTEGRATION — the only thing any existing interactable needs to do:
///
///     TutorialController.Instance.ReportInteraction("receptionist");
///
/// dropped in wherever that interactable's own interaction/dialogue already
/// concludes. If no tutorial step is currently waiting on that id, or no
/// tutorial is running, this call is a harmless no-op. This means adding a
/// new tutorial step for any future interactable requires ONE line in that
/// interactable, plus one JSON entry — no changes to this script.
///
/// This is a MonoBehaviour singleton, not a new FSM state per popup —
/// TutorialCaseState (or scene-load logic) only needs to trigger BeginTutorial()
/// once, or this can auto-start itself (see autoStartOnAwake).
/// </summary>
public class TutorialController : MonoBehaviour
{
    public static TutorialController Instance { get; private set; }

    [Header("View")]
    [SerializeField] private TutorialUI tutorialUI;

    [Header("Data")]
    [Tooltip("Filename inside StreamingAssets/Tutorials/. e.g. 'level1_tutorial.json'")]
    [SerializeField] private string tutorialFileName = "level1_tutorial.json";

    [Header("Behavior")]
    [Tooltip("If true, the tutorial begins automatically as soon as the scene loads.")]
    [SerializeField] private bool autoStartOnAwake = true;

    [Tooltip("Seconds to wait after scene load before the first tutorial popup appears.")]
    [SerializeField] private float startDelaySeconds = 5f;

    [Header("Scene Target References (optional, for arrow)")]
    [Tooltip("Drag in any world Transforms the arrow might need to point at. Name must match a step's targetName.")]
    [SerializeField] private List<TutorialTargetEntry> sceneTargets = new List<TutorialTargetEntry>();

    private List<TutorialStep> steps;
    private int currentStepIndex = -1;
    private TutorialStep activeStep;
    private bool waitingForTrigger;

    [System.Serializable]
    public class TutorialTargetEntry
    {
        public string targetName;
        public Transform transform;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (tutorialUI == null)
            Debug.LogError("[TutorialController] tutorialUI is NOT assigned in Inspector.");
    }

    private void Start()
    {
        if (autoStartOnAwake)
        {
            StartCoroutine(BeginTutorialAfterDelay(startDelaySeconds));
        }
    }

    private System.Collections.IEnumerator BeginTutorialAfterDelay(float delaySeconds)
    {
        if (delaySeconds > 0f)
        {
            Debug.Log($"[TutorialController] Waiting {delaySeconds}s before starting tutorial.");
            yield return new WaitForSeconds(delaySeconds);
        }

        BeginTutorial();
    }

    /// <summary>
    /// Loads tutorial_steps.json and shows the first step.
    /// Can be called manually (e.g. from an FSM state) instead of relying
    /// on autoStartOnAwake, if you want more control over exact timing.
    /// </summary>
    public void BeginTutorial()
    {
        if (!LoadStepsFromJson())
        {
            Debug.LogError("[TutorialController] Failed to load tutorial steps. Tutorial will not start.");
            return;
        }

        Debug.Log($"[TutorialController] Beginning tutorial with {steps.Count} steps.");
        currentStepIndex = -1;
        AdvanceToNextStep();
    }

    private bool LoadStepsFromJson()
    {
        string path = Path.Combine(Application.streamingAssetsPath, "Tutorials", tutorialFileName);

        if (!File.Exists(path))
        {
            Debug.LogError($"[TutorialController] JSON file not found at: {path}");
            return false;
        }

        string json = File.ReadAllText(path);
        TutorialStepList wrapper = JsonUtility.FromJson<TutorialStepList>(json);

        if (wrapper == null || wrapper.steps == null || wrapper.steps.Length == 0)
        {
            Debug.LogError("[TutorialController] JSON parsed but contained no steps.");
            return false;
        }

        steps = new List<TutorialStep>(wrapper.steps);
        return true;
    }

    private void AdvanceToNextStep()
    {
        currentStepIndex++;
        waitingForTrigger = false;

        if (steps == null || currentStepIndex >= steps.Count)
        {
            Debug.Log("[TutorialController] Tutorial sequence complete.");
            activeStep = null;
            return;
        }

        activeStep = steps[currentStepIndex];
        Debug.Log($"[TutorialController] Showing step '{activeStep.id}' (triggerId: '{activeStep.triggerId}')");

        Transform arrowTarget = ResolveTarget(activeStep.targetName);
        tutorialUI.Show(activeStep.speakerName, activeStep.text, OnStepContinuePressed, arrowTarget, activeStep.portraitId);
    }

    private Transform ResolveTarget(string targetName)
    {
        if (string.IsNullOrEmpty(targetName)) return null;

        foreach (var entry in sceneTargets)
        {
            if (entry.targetName == targetName)
                return entry.transform;
        }

        Debug.LogWarning($"[TutorialController] No scene target registered for targetName '{targetName}'. Arrow will not show.");
        return null;
    }

    private void OnStepContinuePressed()
    {
        if (activeStep == null) return;

        if (string.IsNullOrEmpty(activeStep.triggerId))
        {
            // No gameplay action required — move straight on.
            AdvanceToNextStep();
        }
        else
        {
            // Wait for ReportInteraction(triggerId) to be called.
            waitingForTrigger = true;
            Debug.Log($"[TutorialController] Waiting for interaction: '{activeStep.triggerId}'...");
        }
    }

    /// <summary>
    /// THE integration point. Call this from any interactable's OnInteract()
    /// (or wherever its interaction/dialogue concludes) with a string id.
    /// If it matches the currently-awaited step's triggerId, the tutorial
    /// advances. Otherwise this is a no-op — safe to call unconditionally
    /// from every interactable, tutorial-relevant or not.
    /// </summary>
    public void ReportInteraction(string triggerId)
    {
        if (string.IsNullOrEmpty(triggerId)) return;

        if (!waitingForTrigger || activeStep == null)
        {
            Debug.Log($"[TutorialController] ReportInteraction('{triggerId}') ignored — not currently waiting on anything.");
            return;
        }

        if (activeStep.triggerId != triggerId)
        {
            Debug.Log($"[TutorialController] ReportInteraction('{triggerId}') ignored — waiting on '{activeStep.triggerId}' instead.");
            return;
        }

        Debug.Log($"[TutorialController] Trigger '{triggerId}' matched — advancing tutorial.");
        AdvanceToNextStep();
    }

    /// <summary>True while a tutorial sequence is actively running.</summary>
    public bool IsTutorialActive => activeStep != null;
}