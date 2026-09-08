using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// PURPOSE:
/// Drives the tutorial popup sequence for a level.
///
/// Tutorial data is loaded from:
///
/// Resources/Tutorials/{tutorialFileName}.json
///
/// Example:
///
/// Assets/Resources/Tutorials/level1_tutorial.json
///
/// The tutorial:
/// - loads its steps from JSON
/// - automatically starts after the configured delay
/// - displays each step through TutorialUI
/// - advances immediately when no triggerId is specified
/// - waits for ReportInteraction(triggerId) when a gameplay interaction
///   is required
///
/// ANDROID:
/// Uses Resources.Load<TextAsset>() instead of File.ReadAllText(),
/// allowing the JSON file to work when packaged inside the Android APK.
/// </summary>
public class TutorialController : MonoBehaviour
{
    public static TutorialController Instance { get; private set; }


    [Header("View")]
    [SerializeField] private TutorialUI tutorialUI;


    [Header("Data")]
    [Tooltip(
        "JSON filename inside Resources/Tutorials/. " +
        "Example: level1_tutorial.json"
    )]
    [SerializeField]
    private string tutorialFileName = "level1_tutorial.json";


    [Header("Behavior")]
    [Tooltip(
        "If true, the tutorial begins automatically as soon as " +
        "the scene loads."
    )]
    [SerializeField]
    private bool autoStartOnAwake = true;


    [Tooltip(
        "Seconds to wait after scene load before the first " +
        "tutorial popup appears."
    )]
    [SerializeField]
    private float startDelaySeconds = 5f;


    [Header("Scene Target References (optional, for arrow)")]
    [Tooltip(
        "Drag in any world Transforms the arrow might need to point at. " +
        "Name must match a step's targetName."
    )]
    [SerializeField]
    private List<TutorialTargetEntry> sceneTargets =
        new List<TutorialTargetEntry>();


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
        {
            Debug.LogError(
                "[TutorialController] tutorialUI is NOT assigned in Inspector."
            );
        }
    }


    private void Start()
    {
        if (autoStartOnAwake)
        {
            StartCoroutine(
                BeginTutorialAfterDelay(startDelaySeconds)
            );
        }
    }


    private System.Collections.IEnumerator BeginTutorialAfterDelay(
        float delaySeconds)
    {
        if (delaySeconds > 0f)
        {
            Debug.Log(
                $"[TutorialController] Waiting {delaySeconds}s " +
                $"before starting tutorial."
            );

            yield return new WaitForSeconds(delaySeconds);
        }

        BeginTutorial();
    }


    /// <summary>
    /// Loads the tutorial JSON and shows the first step.
    /// </summary>
    public void BeginTutorial()
    {
        if (!LoadStepsFromJson())
        {
            Debug.LogError(
                "[TutorialController] Failed to load tutorial steps. " +
                "Tutorial will not start."
            );

            return;
        }

        Debug.Log(
            $"[TutorialController] Beginning tutorial with " +
            $"{steps.Count} steps."
        );

        currentStepIndex = -1;

        AdvanceToNextStep();
    }


    /// <summary>
    /// Loads tutorial steps from:
    ///
    /// Resources/Tutorials/{tutorialFileName}.json
    ///
    /// Resources.Load does not require the .json extension.
    /// </summary>
    private bool LoadStepsFromJson()
    {
        if (string.IsNullOrWhiteSpace(tutorialFileName))
        {
            Debug.LogError(
                "[TutorialController] tutorialFileName is empty."
            );

            return false;
        }

        // Remove ".json" because Resources.Load expects
        // the resource path without the file extension.
        string resourceFileName =
            Path.GetFileNameWithoutExtension(tutorialFileName);

        string resourcePath =
            $"Tutorials/{resourceFileName}";

        TextAsset jsonFile =
            Resources.Load<TextAsset>(resourcePath);

        if (jsonFile == null)
        {
            Debug.LogError(
                "[TutorialController] JSON file not found at " +
                $"Resources/{resourcePath}.json"
            );

            return false;
        }

        if (string.IsNullOrWhiteSpace(jsonFile.text))
        {
            Debug.LogError(
                "[TutorialController] JSON file is empty at " +
                $"Resources/{resourcePath}.json"
            );

            return false;
        }

        TutorialStepList wrapper;

        try
        {
            wrapper =
                JsonUtility.FromJson<TutorialStepList>(
                    jsonFile.text
                );
        }
        catch (System.Exception e)
        {
            Debug.LogError(
                "[TutorialController] Failed to parse tutorial JSON: " +
                e.Message
            );

            return false;
        }

        if (wrapper == null)
        {
            Debug.LogError(
                "[TutorialController] JSON parsed to null."
            );

            return false;
        }

        if (wrapper.steps == null ||
            wrapper.steps.Length == 0)
        {
            Debug.LogError(
                "[TutorialController] JSON parsed but contained no steps."
            );

            return false;
        }

        steps = new List<TutorialStep>(wrapper.steps);

        Debug.Log(
            $"[TutorialController] Successfully loaded " +
            $"{steps.Count} tutorial step(s) from " +
            $"Resources/{resourcePath}.json"
        );

        return true;
    }


    private void AdvanceToNextStep()
    {
        currentStepIndex++;

        waitingForTrigger = false;

        if (steps == null ||
            currentStepIndex >= steps.Count)
        {
            Debug.Log(
                "[TutorialController] Tutorial sequence complete."
            );

            activeStep = null;

            return;
        }

        activeStep = steps[currentStepIndex];

        if (activeStep == null)
        {
            Debug.LogError(
                $"[TutorialController] Tutorial step at index " +
                $"{currentStepIndex} is null."
            );

            AdvanceToNextStep();

            return;
        }

        Debug.Log(
            $"[TutorialController] Showing step " +
            $"'{activeStep.id}' " +
            $"(triggerId: '{activeStep.triggerId}')"
        );

        Transform arrowTarget =
            ResolveTarget(activeStep.targetName);

        if (tutorialUI == null)
        {
            Debug.LogError(
                "[TutorialController] Cannot show tutorial because " +
                "tutorialUI is NULL."
            );

            return;
        }

        tutorialUI.Show(
            activeStep.speakerName,
            activeStep.text,
            OnStepContinuePressed,
            arrowTarget,
            activeStep.portraitId
        );
    }


    private Transform ResolveTarget(string targetName)
    {
        if (string.IsNullOrEmpty(targetName))
        {
            return null;
        }

        foreach (var entry in sceneTargets)
        {
            if (entry == null)
            {
                continue;
            }

            if (entry.targetName == targetName)
            {
                return entry.transform;
            }
        }

        Debug.LogWarning(
            $"[TutorialController] No scene target registered for " +
            $"targetName '{targetName}'. Arrow will not show."
        );

        return null;
    }


    private void OnStepContinuePressed()
    {
        if (activeStep == null)
        {
            Debug.LogWarning(
                "[TutorialController] Continue pressed but " +
                "activeStep is NULL."
            );

            return;
        }

        if (string.IsNullOrEmpty(activeStep.triggerId))
        {
            // No gameplay action is required.
            // Immediately move to the next tutorial step.
            AdvanceToNextStep();
        }
        else
        {
            // Wait for ReportInteraction(triggerId).
            waitingForTrigger = true;

            Debug.Log(
                $"[TutorialController] Waiting for interaction: " +
                $"'{activeStep.triggerId}'..."
            );
        }
    }


    /// <summary>
    /// Integration point for gameplay interactables.
    ///
    /// Example:
    ///
    /// TutorialController.Instance.ReportInteraction("receptionist");
    ///
    /// If the supplied triggerId matches the currently awaited
    /// tutorial step, the tutorial advances.
    /// </summary>
    public void ReportInteraction(string triggerId)
    {
        if (string.IsNullOrEmpty(triggerId))
        {
            return;
        }

        if (!waitingForTrigger ||
            activeStep == null)
        {
            Debug.Log(
                $"[TutorialController] ReportInteraction(" +
                $"'{triggerId}') ignored — " +
                "not currently waiting on anything."
            );

            return;
        }

        if (activeStep.triggerId != triggerId)
        {
            Debug.Log(
                $"[TutorialController] ReportInteraction(" +
                $"'{triggerId}') ignored — " +
                $"waiting on '{activeStep.triggerId}' instead."
            );

            return;
        }

        Debug.Log(
            $"[TutorialController] Trigger '{triggerId}' matched — " +
            "advancing tutorial."
        );

        AdvanceToNextStep();
    }


    /// <summary>
    /// True while a tutorial sequence is actively running.
    /// </summary>
    public bool IsTutorialActive =>
        activeStep != null;
}