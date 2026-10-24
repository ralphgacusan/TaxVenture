using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    public ToggleCamera cameraManager;

    [Tooltip("The settings board in this scene (settings open here; there is no Settings scene).")]
    [SerializeField] private SettingsBoard3D settingsBoard;

    private readonly List<KeyValuePair<GameObject, bool>> careerButtons = new List<KeyValuePair<GameObject, bool>>();
    private Transform settingsMenuButton;
    private Transform tutorialMenuButton;
    private Vector3 settingsMenuPosition;
    private bool actionPending;

    private void Awake()
    {
        foreach (GameObject root in gameObject.scene.GetRootGameObjects())
            foreach (Transform node in root.GetComponentsInChildren<Transform>(true))
            {
                if (node.name == "tutorial_button" || node.name == "sidequest_button")
                    careerButtons.Add(new KeyValuePair<GameObject, bool>(node.gameObject, node.gameObject.activeSelf));
                if (node.name == "tutorial_button") tutorialMenuButton = node;
                if (node.name == "settings_button") settingsMenuButton = node;
            }
        if (settingsMenuButton != null) settingsMenuPosition = settingsMenuButton.localPosition;
    }

    private void OnEnable()
    {
        PlayerProgressManager.ProgressChanged += RefreshMenu;
        // The main camera (and this component) is disabled by Continue and
        // enabled again by Back, so restore the overlay subscription too.
        if (settingsBoard != null)
        {
            settingsBoard.Closed -= OnSettingsClosed;
            settingsBoard.Closed += OnSettingsClosed;
        }
    }
    private void OnDisable()
    {
        PlayerProgressManager.ProgressChanged -= RefreshMenu;
        if (settingsBoard != null) settingsBoard.Closed -= OnSettingsClosed;
    }

    private void OnSettingsClosed() => RefreshMenu(null);
    private void Start()
    {
        // Back in the menu: no run is in progress and a new launch may start (T03).
        MenuEntry.MenuOpened();
        if (settingsBoard == null) settingsBoard = FindAnyObjectByType<SettingsBoard3D>();
        if (settingsBoard != null)
        {
            settingsBoard.Closed -= OnSettingsClosed;
            settingsBoard.Closed += OnSettingsClosed;
        }
        campaignPanel = MainMenuCampaignPanel.Ensure(gameObject.scene, settingsBoard);
        actionPending = false;
        // Physics touch/mouse input below is the single owner. In particular,
        // Settings' legacy UGUI receiver must not bypass or duplicate the press.
        foreach (GameObject root in gameObject.scene.GetRootGameObjects())
            foreach (Transform node in root.GetComponentsInChildren<Transform>(true))
                if (IsMenuNote(node.name))
                    foreach (UnityEngine.UI.Button button in node.GetComponentsInChildren<UnityEngine.UI.Button>(true))
                    {
                        button.onClick = new UnityEngine.UI.Button.ButtonClickedEvent();
                        button.enabled = false;
                    }
        RefreshMenu(null);
    }

    /// <summary>NEW GAME / CONTINUE notes in PLAY's row after saved Level 1 completion (T03).</summary>
    private MainMenuCampaignPanel campaignPanel;

    /// <summary>
    /// Before saved Level 1 completion: PLAY and Settings (Settings moves up into the Tutorials slot).
    /// After saved Level 1 completion: NEW GAME + CONTINUE, then Tutorials and Settings.
    /// Runs on load and whenever the save changes (back from gameplay, after a New Game).
    /// </summary>
    private void RefreshMenu(PlayerProgressData _)
    {
        if (actionPending || MenuEntry.IsStoryLaunching || (settingsBoard != null && settingsBoard.MenuButtonsHidden)) return;
        if (campaignPanel != null) campaignPanel.Apply(MenuEntry.CurrentModel());
        foreach (KeyValuePair<GameObject, bool> entry in careerButtons)
            if (entry.Key != null) entry.Key.SetActive(MenuEntry.HasCareerAccess && entry.Value);
        if (settingsMenuButton != null)
        {
            Vector3 position = settingsMenuPosition;
            if (!MenuEntry.HasCareerAccess && tutorialMenuButton != null)
                position.y = settingsMenuButton.parent != null
                    ? settingsMenuButton.parent.InverseTransformPoint(tutorialMenuButton.position).y
                    : tutorialMenuButton.position.y;
            settingsMenuButton.localPosition = position;
        }
    }

    private static bool IsMenuNote(string name) => name == "play_button" || name == "settings_button" ||
        name == "tutorial_button" || name == MenuEntry.NewGameButtonName ||
        name == MenuEntry.LegacyNewGameButtonName || name == MenuEntry.ContinueButtonName;

    private void Update()
    {
        if (actionPending || MenuEntry.IsStoryLaunching || SceneFade.BlocksInput ||
            (cameraManager != null && (cameraManager.IsTransitioning || cameraManager.IsLevelSelectorVisible))) return;
        if (settingsBoard != null && (settingsBoard.IsOpen || settingsBoard.MenuButtonsAnimating)) return;
        if (TrainingBinder3D.Instance != null && TrainingBinder3D.Instance.IsOpen) return;

        Vector2 position;
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            if (touch.phase != TouchPhase.Began) return;
            position = touch.position;
        }
        else
        {
            if (!Input.GetMouseButtonDown(0)) return;
            position = Input.mousePosition;
        }
        Camera view = Camera.main;
        if (view == null) return;
        if (!Physics.Raycast(view.ScreenPointToRay(position), out RaycastHit hit)) return;
        Transform target = hit.collider.transform;
        while (target != null && !IsMenuNote(target.name)) target = target.parent;
        if (target == null || !target.gameObject.activeInHierarchy) return;
        GameObject note = target.gameObject;
        switch (note.name)
        {
            case "play_button":
                Press(note, () => MenuEntry.Play(cameraManager));
                break;
            case MenuEntry.NewGameButtonName:
            case MenuEntry.LegacyNewGameButtonName:
                if (MenuEntry.HasCareerAccess) Press(note, OpenNewGameConfirmation);
                break;
            case MenuEntry.ContinueButtonName:
                if (MenuEntry.HasCareerAccess) Press(note, () => MenuEntry.Continue(cameraManager));
                break;
            case "tutorial_button":
                if (MenuEntry.HasCareerAccess) Press(note, OpenTraining);
                break;
            case "settings_button":
                Press(note, OpenSettings);
                break;
        }
    }

    /// <summary>Tutorial Hub: the camera leans down to the desk and Arthur's training binder comes up.</summary>
    private void OpenTraining()
    {
        if (!MenuEntry.HasCareerAccess) return;
        if (TrainingBinder3D.Instance != null) TrainingBinder3D.Instance.Open();
        else Debug.LogWarning("[MainMenu] No TrainingBinder3D in this scene.");
    }

    private void Press(GameObject note, System.Action action)
    {
        if (actionPending || campaignPanel == null) return;
        actionPending = true;
        if (!campaignPanel.PressNote(note, () =>
        {
            if ((note.name == "play_button" || note.name == MenuEntry.ContinueButtonName) && settingsBoard != null)
            {
                if (!settingsBoard.HideMenuForTransition(() => RunAction(action))) actionPending = false;
            }
            else RunAction(action);
        })) actionPending = false;
    }

    private void RunAction(System.Action action)
    {
        try { action(); }
        finally
        {
            actionPending = false;
            // Overlay/transition owners keep notes hidden until their own
            // close/Back flow. A failed start can restore and retry.
            if ((settingsBoard == null || (!settingsBoard.IsOpen && !settingsBoard.MenuButtonsHidden)) &&
                (TrainingBinder3D.Instance == null || !TrainingBinder3D.Instance.IsOpen)) RefreshMenu(null);
        }
    }

    /// <summary>NEW GAME: the existing confirmation card, shown without the settings board.</summary>
    private void OpenNewGameConfirmation()
    {
        if (!MenuEntry.HasCareerAccess) return;
        if (settingsBoard == null) settingsBoard = FindAnyObjectByType<SettingsBoard3D>();
        if (settingsBoard != null) settingsBoard.OpenNewGameConfirmation();
        else Debug.LogWarning("[MainMenu] No SettingsBoard3D in this scene for the New Game confirmation.");
    }

    private void OpenSettings()
    {
        if (settingsBoard == null) settingsBoard = FindAnyObjectByType<SettingsBoard3D>();
        if (settingsBoard != null) settingsBoard.Open();
        else Debug.LogWarning("[MainMenu] No SettingsBoard3D in this scene.");
    }
}
