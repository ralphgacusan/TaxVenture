using TMPro;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// PURPOSE:
/// Builds and owns the RPG-style Office HUD inside the existing HUD_Canvas of
/// Office_Level1 (and of any level scene whose level.json opts into "hud", see
/// OfficeLevelProfile). Same self-bootstrapping pattern as OfficeMinimap: no
/// scene edits are required and no serialized references change.
///
/// RESPONSIBILITIES:
/// - Create a safe-area root placed just above PermanentHUD (so modals still
///   draw over it) and build the HUD clusters into it.
/// - Hide (never delete) the legacy plain-text "Level 1" label and the debug
///   FSM state label. Both are restored if this HUD is destroyed.
/// - Fade the whole HUD out while dialogue, settings, case results, pause or
///   any non-first-person camera mode is active — the same rule the minimap
///   uses — so the HUD never competes with focused interactions.
///
/// DOES NOT:
/// - Read or change rewards, objectives or FSM state beyond observing them.
///   Every cluster is presentation over existing data sources.
/// </summary>
[DefaultExecutionOrder(100)]
public sealed class OfficeHudRuntime : MonoBehaviour
{
    private const float FadeSpeed = 6f;

    public static OfficeHudRuntime Instance { get; private set; }

    private Canvas hudCanvas;
    private RectTransform safeRoot;
    private CanvasGroup group;
    private CanvasGroup legacyTimerHider;

    // Scene HUD buttons (Settings, icon column) fade during dialogue too.
    private readonly System.Collections.Generic.List<CanvasGroup> sceneButtonGroups =
        new System.Collections.Generic.List<CanvasGroup>();
    private readonly System.Collections.Generic.List<CanvasGroup> addedButtonGroups =
        new System.Collections.Generic.List<CanvasGroup>();
    private CanvasGroup settingsButtons;
    private CanvasGroup iconButtons;
    private RectTransform caseFolderIcon;
    private float trustPeekUntil;
    private bool awaitedControlsApplied;
    private readonly Dictionary<Button, bool> awaitedButtonStates = new Dictionary<Button, bool>();
    private readonly Dictionary<HudSubmittableIcon, bool> awaitedIconStates = new Dictionary<HudSubmittableIcon, bool>();
    private readonly Dictionary<Behaviour, bool> awaitedPointerHandlerStates = new Dictionary<Behaviour, bool>();
    private readonly Dictionary<CanvasGroup, AwaitedCanvasGroupState> awaitedSiblingVisualStates =
        new Dictionary<CanvasGroup, AwaitedCanvasGroupState>();
    private readonly System.Collections.Generic.List<Behaviour> disabledNotesHandlers =
        new System.Collections.Generic.List<Behaviour>();

    private CameraController cameraController;
    private SettingsUI settings;
    private CaseResultPopupUI results;
    private float nextResolveTime;

    private HudProgressionPanel progression;
    private OfficeObjectives objectiveProgress;
    private HudObjectiveTracker objectives;
    private NotesPaperView notesPaper;
    private HudProfileCards profileCards;
    private HudSubmittableIcon patchedFolderIcon;
    private CaseFolder3DView caseFolder3D;
    private HudToastQueue toastQueue;
    private HudRewardFlyFX rewardFx;
    private readonly System.Collections.Generic.List<GameObject> addedObjects =
        new System.Collections.Generic.List<GameObject>();
    private readonly System.Collections.Generic.List<Component> addedComponents =
        new System.Collections.Generic.List<Component>();
    private float laidOutWidth = -1f;

    private struct AwaitedCanvasGroupState
    {
        public float Alpha;
        public bool BlocksRaycasts;
        public bool Interactable;
    }

    /// <summary>True once the HUD has fully faded in; animations wait for this.</summary>
    public bool IsShown => group != null && group.alpha >= 0.99f;

    public RectTransform SafeRoot => safeRoot;
    public HudProgressionPanel Progression => progression;
    public HudObjectiveTracker Objectives => objectives;
    public OfficeObjectives ObjectiveProgress => objectiveProgress;
    public RectTransform CaseFolderIcon => caseFolderIcon;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void InitializeForLoadedScenes()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;

        for (int i = 0; i < SceneManager.sceneCount; i++)
            EnsureForScene(SceneManager.GetSceneAt(i));
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode) => EnsureForScene(scene);

    private static void EnsureForScene(Scene scene)
    {
        if (!scene.IsValid() || !scene.isLoaded || !OfficeLevelProfile.Allows(scene, OfficeFeature.Hud)) return;

        GameObject[] roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
            if (roots[i].GetComponentInChildren<OfficeHudRuntime>(true) != null)
                return;

        var host = new GameObject("Office HUD Runtime");
        SceneManager.MoveGameObjectToScene(host, scene);
        host.AddComponent<OfficeHudRuntime>();
    }

    private void Awake()
    {
        Instance = this;
        HudWindowLock.ResetForNewScene();
        hudCanvas = FindHudCanvas(gameObject.scene);
        if (hudCanvas == null)
        {
            Debug.LogWarning("[OfficeHud] HUD_Canvas not found; RPG HUD not built.");
            enabled = false;
            return;
        }

        Transform canvasRoot = hudCanvas.transform;
        Transform permanent = canvasRoot.Find("PermanentHUD");
        Transform level = canvasRoot.Find("PermanentHUD/Level");
        Transform debugLabel = canvasRoot.Find("DebugStateLabel_Text");
        Transform timer = canvasRoot.Find("PermanentHUD/Timer");

        safeRoot = HudTheme.CreateRect("Office HUD Safe Area", canvasRoot);
        HudTheme.Stretch(safeRoot);
        safeRoot.gameObject.AddComponent<MobileControlsSafeArea>();
        if (permanent != null) safeRoot.SetSiblingIndex(permanent.GetSiblingIndex() + 1);

        group = safeRoot.gameObject.AddComponent<CanvasGroup>();
        group.alpha = 0f;
        group.interactable = false;
        group.blocksRaycasts = false;

        progression = HudProgressionPanel.Build(safeRoot, this);

        // The tracker shows the existing HudTimer's clock; the timer object
        // keeps running (so its time is unchanged) but is made invisible.
        HudTimer clock = timer != null ? timer.GetComponent<HudTimer>() : null;
        // One shared objective record for the tracker and the Notes paper.
        objectiveProgress = OfficeObjectives.Create(gameObject, OfficeLevelProfile.LevelIdFor(this));
        objectives = HudObjectiveTracker.Build(safeRoot, this, objectiveProgress, clock);
        AttachNotesPaper();
        AttachPaperWindows(canvasRoot);
        gameObject.AddComponent<HudWindowFocus>();

        // Phase 3: notifications under the tracker and reward fly-in effects.
        toastQueue = HudToastQueue.Create(safeRoot, this, HudTheme.Margin + 112f * HudTheme.HudScale + 12f);
        rewardFx = HudRewardFlyFX.Create(canvasRoot);
        addedObjects.Add(rewardFx.gameObject);
        HudFeedback.Create(gameObject, this, toastQueue, rewardFx, progression, objectiveProgress,
            canvasRoot.Find("PermanentHUD/IconButtons/TaxpayerProfileIcon") as RectTransform);
        if (timer != null && timer.GetComponent<CanvasGroup>() == null)
        {
            legacyTimerHider = timer.gameObject.AddComponent<CanvasGroup>();
            legacyTimerHider.alpha = 0f;
            legacyTimerHider.blocksRaycasts = false;
            legacyTimerHider.interactable = false;
        }

        HideLegacy(level);
        HideLegacy(debugLabel);

        settingsButtons = AddSceneButtonGroup(canvasRoot.Find("PermanentHUD/Settings"));
        iconButtons = AddSceneButtonGroup(canvasRoot.Find("PermanentHUD/IconButtons"));

        // Phase 4: game-style interaction prompt and icon count badges.
        AttachPrompt(canvasRoot);
        AttachBadge(canvasRoot, "PermanentHUD/IconButtons/CaseFolderIconButton", "FolderCountText");
        AttachBadge(canvasRoot, "PermanentHUD/IconButtons/TaxpayerProfileIcon", "CountText");

        // Desk / corkboard exit button in the HUD style.
        SkinExitButton();

        // Game-feel skin for the floating value while dragging, plus a frame on
        // the field under the finger (green = it fits, red = it doesn't).
        DragVisualManager dragVisuals = FindAnyObjectByType<DragVisualManager>(FindObjectsInactive.Include);
        DragGhostSkin.Attach(dragVisuals);
        DropAimIndicator aim = DropAimIndicator.Create(dragVisuals);
        if (aim != null) addedObjects.Add(aim.gameObject);

        // No walking or looking around while a reward/summary card is up.
        PlayerControlHold.ResetForNewScene();
        addedComponents.Add(gameObject.AddComponent<HudModalControlHold>());

        // Matching, distinct HUD icons (same buttons, same behaviour).
        ApplyIcon(canvasRoot, "PermanentHUD/Settings/SettingsButton", "settings");
        ApplyIcon(canvasRoot, "PermanentHUD/IconButtons/NotesIconButton", "notes");
        ApplyIcon(canvasRoot, "PermanentHUD/IconButtons/CaseFolderIconButton", "folder");
        ApplyIcon(canvasRoot, "PermanentHUD/IconButtons/TaxpayerProfileIcon", "profiles");

        // T10: Level 1's icon row becomes a hotbar (same places) with a Bag icon; items keep their icons' behaviour.
        if (HudHotbar.UsesHotbar(OfficeLevelProfile.LevelIdFor(this)))
        {
            HudHotbar hotbar = HudHotbar.Create(gameObject, canvasRoot);
            if (hotbar != null) addedComponents.Add(hotbar);
        }
    }

    /// <summary>The prompt pill sits where the plain PromptPanel was (same layer, never inside the fading HUD).</summary>
    private void AttachPrompt(Transform canvasRoot)
    {
        Transform legacy = FindDeep(canvasRoot, "PromptPanel");
        Transform parent = legacy != null ? legacy.parent : canvasRoot;
        int index = legacy != null ? legacy.GetSiblingIndex() + 1 : -1;
        HudPrompt prompt = HudPrompt.Create(parent, index);
        addedObjects.Add(prompt.gameObject);
    }

    /// <summary>"Exit Desk" / "Exit Corkboard": navy pill, brass rim, cream capitals (same button and click).</summary>
    /// <summary>The EXIT DESK button's own sorting order: over every desk document layer.</summary>
    public const int ExitDeskSortingOrder = 2400;

    private static void SkinExitButton()
    {
        WorkstationUI ui = FindAnyObjectByType<WorkstationUI>(FindObjectsInactive.Include);
        UnityEngine.UI.Button button = ui != null ? ui.GetComponentInChildren<UnityEngine.UI.Button>(true) : null;
        if (button == null) return;
        // Always above the open desk documents (their layers sort 1101-1900), below the Bag (2550) (2026-10-26).
        // A nested canvas added while hidden loses overrideSorting when it is shown, so the order is
        // re-applied every time the button appears (HudSortingKeeper).
        if (button.GetComponent<HudSortingKeeper>() == null)
            button.gameObject.AddComponent<HudSortingKeeper>().Order = ExitDeskSortingOrder;
        if (button.targetGraphic is UnityEngine.UI.Image image)
        {
            float h = Mathf.Max(40f, ((RectTransform)button.transform).rect.height);
            image.sprite = HudTheme.Rounded;
            image.type = UnityEngine.UI.Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = 20f / (h * 0.5f);
            image.color = new Color32(16, 25, 38, 240);
            var rim = image.GetComponent<UnityEngine.UI.Outline>() ?? image.gameObject.AddComponent<UnityEngine.UI.Outline>();
            rim.effectColor = HudTheme.Brass;
            rim.effectDistance = new Vector2(3f, -3f);
        }
        var colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1.1f, 1.1f, 1.1f, 1f);
        colors.pressedColor = new Color(0.75f, 0.75f, 0.8f, 1f);
        button.colors = colors;
        foreach (TMPro.TextMeshProUGUI label in button.GetComponentsInChildren<TMPro.TextMeshProUGUI>(true))
        {
            label.font = HudTheme.Font;
            label.color = HudTheme.TextPrimary;
            label.fontStyle = TMPro.FontStyles.Bold | TMPro.FontStyles.UpperCase;
            label.characterSpacing = 5f;
        }
    }

    /// <summary>Swaps a HUD button's picture for the themed icon (the Image keeps its button and handlers).</summary>
    private static void ApplyIcon(Transform canvasRoot, string path, string icon)
    {
        Transform target = canvasRoot.Find(path);
        UnityEngine.UI.Image image = target != null ? target.GetComponent<UnityEngine.UI.Image>() : null;
        if (image == null)
        {
            Debug.LogWarning($"[OfficeHud] {path} not found; it keeps its old icon.");
            return;
        }
        image.sprite = HudIconArt.Get(icon);
        image.type = UnityEngine.UI.Image.Type.Simple;
        image.preserveAspect = true;
        image.color = Color.white;

        StyleIconButton(target.GetComponent<UnityEngine.UI.Button>());
    }

    /// <summary>Shared feedback for scene icons and Level 2's runtime-created icons.</summary>
    public static void StyleIconButton(UnityEngine.UI.Button button)
    {
        if (button == null) return;
        // Keep the availability fade on the icon's CanvasGroup; avoid
        // multiplying it by Unity's default half-opacity disabled tint.
        var colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1f, 0.94f, 0.8f, 1f);
        colors.pressedColor = new Color(0.72f, 0.78f, 0.88f, 1f);
        colors.selectedColor = Color.white;
        colors.disabledColor = Color.white;
        colors.fadeDuration = 0.1f;
        button.colors = colors;
    }

    private void AttachBadge(Transform canvasRoot, string iconPath, string counterName)
    {
        var icon = canvasRoot.Find(iconPath) as RectTransform;
        Transform counter = icon != null ? icon.Find(counterName) : null;
        TMPro.TextMeshProUGUI text = counter != null ? counter.GetComponent<TMPro.TextMeshProUGUI>() : null;
        if (text == null)
        {
            Debug.LogWarning($"[OfficeHud] {iconPath}/{counterName} not found; that icon keeps its plain counter.");
            return;
        }
        HudIconBadge badge = HudIconBadge.Attach(icon, text);
        if (badge != null) addedComponents.Add(badge);
    }

    private static Transform FindDeep(Transform root, string name)
    {
        if (root.name == name) return root;
        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindDeep(root.GetChild(i), name);
            if (found != null) return found;
        }
        return null;
    }

    private void AttachNotesPaper()
    {
        NotesPanelUI[] panels = FindObjectsByType<NotesPanelUI>(FindObjectsInactive.Include);
        for (int i = 0; i < panels.Length; i++)
        {
            if (panels[i].gameObject.scene != gameObject.scene) continue;
            notesPaper = NotesPaperView.Attach(panels[i], objectiveProgress);
            if (notesPaper != null)
            {
                panels[i].UsePaperView(notesPaper);
                MakeNotesIconSingleHandler();
                FloatingWindow notesWindow = panels[i].GetComponent<FloatingWindow>();
                var notesIcon = hudCanvas.transform.Find("PermanentHUD/IconButtons/NotesIconButton") as RectTransform;
                if (notesWindow != null) notesWindow.UsePaperMotion(-1.6f, notesIcon);
            }
            return;
        }
        Debug.LogWarning("[OfficeHud] NotesPanelUI not found; Notes keeps its plain list.");
    }

    /// <summary>
    /// The Notes icon carries three tap handlers (Button -> NotesPanelUI.Toggle,
    /// FloatingWindowIcon and HudSubmittableIcon, which both toggle the window
    /// directly). One tap ran two or three toggles, so it took two taps to open.
    /// Keep only NotesPanelUI.Toggle, which also refreshes the paper and takes
    /// the movement lock. Restored on destroy.
    /// </summary>
    /// <summary>
    /// Notes, taxpayer profiles and the Case Folder share one window feel:
    /// several open at once, touched one on top, paper open/close motion.
    /// </summary>
    private void AttachPaperWindows(Transform canvasRoot)
    {
        var profileIcon = canvasRoot.Find("PermanentHUD/IconButtons/TaxpayerProfileIcon") as RectTransform;
        var notesIcon = canvasRoot.Find("PermanentHUD/IconButtons/NotesIconButton") as RectTransform;
        var caseFolderIcon = canvasRoot.Find("PermanentHUD/IconButtons/CaseFolderIconButton") as RectTransform;
        this.caseFolderIcon = caseFolderIcon;

        // Taxpayer profiles: paper cards; tap the icon, or drag a stack out.
        profileCards = HudProfileCards.Create(canvasRoot, profileIcon);
        if (profileIcon != null)
        {
            var drag = profileIcon.gameObject.AddComponent<HudIconDragOpen>();
            drag.Visual = HudDragItem.Create(canvasRoot, "Profiles", profileIcon, new Vector2(300f, 188f), 3f,
                HudDragItem.ProfileStackBuilder(HudProfileCards.CurrentProfiles));
            drag.CanDrag = profileCards.CanDragOut;
            drag.OpenAt = profileCards.OpenFromDrop;
            addedComponents.Add(drag);
        }

        // Notes: drag the paper out of its icon; drag the window back to close.
        NotesPanelUI notes = FindNotesPanel();
        if (notes != null && notesIcon != null)
        {
            var drag = notesIcon.gameObject.AddComponent<HudIconDragOpen>();
            drag.Visual = HudDragItem.Create(canvasRoot, "Notes", notesIcon, new Vector2(230f, 270f), -3f,
                HudDragItem.BuildNotesPaper);
            drag.CanDrag = () => !notes.IsOpen;
            drag.OpenAt = notes.ShowAt;
            addedComponents.Add(drag);
            FloatingWindow notesWindow = notes.GetComponent<FloatingWindow>();
            if (notesWindow != null) notesWindow.SetReturnIcon(notesIcon);
        }

        // 3D documents (the desk Case Folder and Tax Code Book) share the
        // paper windows' front-to-back order through Doc3DLayer.
        Scene scene = gameObject.scene;
        Transform openFolder = FindInScene(scene, "Environment/OfficeBuilding/Rooms/ConsultantOffice/InteractiveFolderObjects/OpenCaseFolder");
        Transform openBook = FindInScene(scene, "Environment/OfficeBuilding/Rooms/ConsultantOffice/InteractiveFolderObjects/OpenTaxCodeBook");
        Doc3DLayer folderLayer = Doc3DLayer.Create(canvasRoot, openFolder, "Case Folder");
        Doc3DLayer bookLayer = Doc3DLayer.Create(canvasRoot, openBook, "Tax Code Book");
        // One look for each document (2026-10-26): opened on the desk or from the bar, it is drawn the same way,
        // through its own document layer (the warm manila folder of the bar view), never by the room's lighting.
        if (folderLayer != null) { folderLayer.AlwaysComposite = true; addedObjects.Add(folderLayer.gameObject); }
        if (bookLayer != null) { bookLayer.AlwaysComposite = true; addedObjects.Add(bookLayer.gameObject); }

        // Case Folder icon: the REAL desk folder (3D model with its pages),
        // tapped open anywhere or dragged out (closed) and dropped to open.
        caseFolder3D = CaseFolder3DView.Create(gameObject, scene, caseFolderIcon, folderLayer);
        HudSubmittableIcon folderIcon = caseFolderIcon != null ? caseFolderIcon.GetComponent<HudSubmittableIcon>() : null;
        if (folderIcon != null && caseFolder3D != null)
        {
            CaseFolder3DView view = caseFolder3D;
            folderIcon.ClickOverride = () =>
            {
                if (AuditorInteractable.Current != null && AuditorInteractable.Current.TryIconHandIn()) return true;
                if (ClientCopyHandover.Instance != null && ClientCopyHandover.Instance.TryIconTap()) return true;
                view.Toggle();
                return true;
            };
            folderIcon.DropResolver = screen => AuditorInteractable.ResolveFolderDrop(screen) ??
                (ClientCopyHandover.Instance != null ? ClientCopyHandover.Instance.ResolveDrop(screen) : null);
            folderIcon.DropOpenOverride = screen =>
            {
                view.Open(screen);
                return true; // A rejected 3D open must not start the legacy flat window.
            };
            folderIcon.DragVisual = HudDragItem.Create(canvasRoot, "Case Folder", caseFolderIcon,
                new Vector2(300f, 220f), -6f, (root, size) =>
                {
                    HudDragItem.BuildClosedFolder(root, size);
                    if (ClientCopyHandover.CopyActive)
                        ClientCopyStamp.Build(root, size.x * 0.62f, new Vector2(size.x * 0.16f, -size.y * 0.12f));
                });
            patchedFolderIcon = folderIcon;
        }
        else
        {
            Debug.LogWarning("[OfficeHud] Case Folder icon or desk folder not found; icon keeps its old behaviour.");
        }
    }

    private static Transform FindInScene(Scene scene, string path)
    {
        int slash = path.IndexOf('/');
        string rootName = path.Substring(0, slash);
        string rest = path.Substring(slash + 1);
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root.name != rootName) continue;
            Transform t = root.transform.Find(rest);
            if (t != null) return t;
        }
        Debug.LogWarning($"[OfficeHud] \"{path}\" not found.");
        return null;
    }

    private NotesPanelUI FindNotesPanel()
    {
        NotesPanelUI[] panels = FindObjectsByType<NotesPanelUI>(FindObjectsInactive.Include);
        for (int i = 0; i < panels.Length; i++)
            if (panels[i].gameObject.scene == gameObject.scene) return panels[i];
        return null;
    }

    private void MakeNotesIconSingleHandler()
    {
        Transform icon = hudCanvas.transform.Find("PermanentHUD/IconButtons/NotesIconButton");
        if (icon == null)
        {
            Debug.LogWarning("[OfficeHud] NotesIconButton not found; Notes tap handlers unchanged.");
            return;
        }

        DisableHandler(icon.GetComponent<FloatingWindowIcon>());
        DisableHandler(icon.GetComponent<HudSubmittableIcon>());
    }

    private void DisableHandler(Behaviour handler)
    {
        if (handler == null || !handler.enabled) return;
        handler.enabled = false;
        disabledNotesHandlers.Add(handler);
    }

    private CanvasGroup AddSceneButtonGroup(Transform target)
    {
        if (target == null) return null;
        CanvasGroup group = target.GetComponent<CanvasGroup>();
        if (group == null)
        {
            group = target.gameObject.AddComponent<CanvasGroup>();
            addedButtonGroups.Add(group);
        }
        sceneButtonGroups.Add(group);
        return group;
    }

    /// <summary>Settings and the HUD icons step aside only for conversations and results.</summary>
    private bool SceneButtonsVisible()
    {
        return !DialogueUI.IsAnyDialogueActive && (results == null || !results.IsVisible) && !HudRewardScreen.AnyOpen &&
               !InGameSettings.IsOpen;
    }

    private void FadeSceneButtons()
    {
        if (sceneButtonGroups.Count == 0) return;
        bool visible = SceneButtonsVisible();
        bool awaiting = ClientCopyHandover.AwaitingHandoverAction;
        for (int i = 0; i < sceneButtonGroups.Count; i++)
        {
            CanvasGroup group = sceneButtonGroups[i];
            if (group == null) continue;
            bool show = awaiting ? group == iconButtons : visible;
            group.alpha = Mathf.MoveTowards(group.alpha, show ? 1f : 0f, FadeSpeed * Time.unscaledDeltaTime);
            bool interactive = group.alpha > 0.5f;
            group.blocksRaycasts = interactive;
            group.interactable = interactive;
        }
        GateNonFolderIcons(awaiting);
    }

    private void GateNonFolderIcons(bool active)
    {
        if (active)
        {
            awaitedControlsApplied = true;
            if (iconButtons != null)
            {
                foreach (Transform sibling in iconButtons.transform)
                {
                    // T10: the folder may sit inside a hotbar slot wrapper; that wrapper stays visible too.
                    if (sibling == caseFolderIcon || IsCaseFolderControl(sibling) ||
                        (caseFolderIcon != null && caseFolderIcon.IsChildOf(sibling))) continue;
                    CanvasGroup siblingGroup = sibling.GetComponent<CanvasGroup>();
                    if (siblingGroup == null)
                    {
                        siblingGroup = sibling.gameObject.AddComponent<CanvasGroup>();
                        addedButtonGroups.Add(siblingGroup);
                    }
                    if (!awaitedSiblingVisualStates.ContainsKey(siblingGroup))
                        awaitedSiblingVisualStates.Add(siblingGroup, new AwaitedCanvasGroupState
                        {
                            Alpha = siblingGroup.alpha,
                            BlocksRaycasts = siblingGroup.blocksRaycasts,
                            Interactable = siblingGroup.interactable
                        });
                    siblingGroup.alpha = 0f;
                    siblingGroup.blocksRaycasts = false;
                    siblingGroup.interactable = false;
                }
                foreach (Button button in iconButtons.GetComponentsInChildren<Button>(true))
                {
                    if (button == null || button.transform == caseFolderIcon) continue;
                    if (!awaitedButtonStates.ContainsKey(button)) awaitedButtonStates.Add(button, button.interactable);
                    button.interactable = false;
                    HudSubmittableIcon icon = button.GetComponent<HudSubmittableIcon>();
                    if (icon != null)
                    {
                        if (!awaitedIconStates.ContainsKey(icon)) awaitedIconStates.Add(icon, icon.enabled);
                        icon.enabled = false;
                    }
                }
                foreach (Behaviour handler in iconButtons.GetComponentsInChildren<Behaviour>(true))
                {
                    if (handler == null || handler is HudSubmittableIcon || IsCaseFolderControl(handler.transform) ||
                        !HandlesPointerInput(handler)) continue;
                    if (!awaitedPointerHandlerStates.ContainsKey(handler))
                        awaitedPointerHandlerStates.Add(handler, handler.enabled);
                    handler.enabled = false;
                }
            }
            return;
        }

        if (!awaitedControlsApplied) return;
        foreach (KeyValuePair<Button, bool> saved in awaitedButtonStates)
            if (saved.Key != null) saved.Key.interactable = saved.Value;
        foreach (KeyValuePair<HudSubmittableIcon, bool> saved in awaitedIconStates)
            if (saved.Key != null) saved.Key.enabled = saved.Value;
        foreach (KeyValuePair<Behaviour, bool> saved in awaitedPointerHandlerStates)
            if (saved.Key != null) saved.Key.enabled = saved.Value;
        foreach (KeyValuePair<CanvasGroup, AwaitedCanvasGroupState> saved in awaitedSiblingVisualStates)
            if (saved.Key != null)
            {
                saved.Key.alpha = saved.Value.Alpha;
                saved.Key.blocksRaycasts = saved.Value.BlocksRaycasts;
                saved.Key.interactable = saved.Value.Interactable;
            }
        awaitedButtonStates.Clear();
        awaitedIconStates.Clear();
        awaitedPointerHandlerStates.Clear();
        awaitedSiblingVisualStates.Clear();
        awaitedControlsApplied = false;
        if (caseFolderIcon != null)
        {
            HudIconButton folder = caseFolderIcon.GetComponent<HudIconButton>();
            if (folder != null) folder.RefreshFolderState();
        }
    }

    private bool IsCaseFolderControl(Transform control) => caseFolderIcon != null &&
        (control == caseFolderIcon || control.IsChildOf(caseFolderIcon));

    private static bool HandlesPointerInput(Behaviour handler) =>
        handler is IPointerClickHandler || handler is IPointerDownHandler || handler is IPointerUpHandler ||
        handler is IPointerEnterHandler || handler is IPointerExitHandler || handler is IInitializePotentialDragHandler ||
        handler is IBeginDragHandler || handler is IDragHandler || handler is IEndDragHandler || handler is IDropHandler;

    private static GameObject HideLegacy(Transform legacy)
    {
        if (legacy == null || !legacy.gameObject.activeSelf) return null;
        legacy.gameObject.SetActive(false);
        return legacy.gameObject;
    }

    private static Canvas FindHudCanvas(Scene scene)
    {
        Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include);
        for (int i = 0; i < canvases.Length; i++)
        {
            Canvas candidate = canvases[i];
            if (candidate.gameObject.scene == scene && candidate.name == "HUD_Canvas" &&
                candidate.renderMode == RenderMode.ScreenSpaceOverlay)
                return candidate;
        }
        return null;
    }

    private void LateUpdate()
    {
        // Settings and results are optional; the camera is what gates visibility.
        if (cameraController == null && Time.unscaledTime >= nextResolveTime)
        {
            nextResolveTime = Time.unscaledTime + 0.5f;
            ResolveSceneDependencies();
        }

        float width = safeRoot.rect.width;
        if (!Mathf.Approximately(width, laidOutWidth))
        {
            laidOutWidth = width;
            objectives.ApplyLayout(width);
        }

        FadeSceneButtons();

        float target = ShouldShow() ? 1f : 0f;
        if (!Mathf.Approximately(group.alpha, target))
            group.alpha = Mathf.MoveTowards(group.alpha, target, FadeSpeed * Time.unscaledDeltaTime);
    }

    private bool ShouldShow()
    {
        GameStateMachine gameState = GameStateMachine.Instance;
        // The paper windows (Notes, profile cards) hold the player still via
        // the profile-viewer mode; the HUD stays up while they are open.
        bool dialoguePeek = ClientCopyHandover.AwaitingHandoverAction || Time.unscaledTime < trustPeekUntil;
        bool freeOrReading = cameraController != null &&
            (cameraController.CanStartDialogue ||
             cameraController.CurrentMode == CameraController.CameraMode.ProfileViewer) || dialoguePeek;
        return freeOrReading &&
               (!DialogueUI.IsAnyDialogueActive || dialoguePeek) && (Time.timeScale > 0f || dialoguePeek) &&
               !InGameSettings.IsOpen && (settings == null || !settings.IsVisible) &&
               (results == null || !results.IsVisible) &&
               !HudRewardScreen.AnyOpen &&
               (gameState == null || !gameState.IsPaused);
    }

    /// <summary>Shows the saved Public Trust meter briefly while Marie's dialogue continues.</summary>
    public void PeekDuringDialogue(float seconds)
    {
        trustPeekUntil = Mathf.Max(trustPeekUntil, Time.unscaledTime + Mathf.Max(0f, seconds));
    }

    /// <summary>Closes the floating, read-only 3D view before starting Marie's handover conversation.</summary>
    public void CloseCaseFolderView() => caseFolder3D?.Close();

    /// <summary>The floating Case Folder is out of its icon (the hotbar dims the icon meanwhile).</summary>
    public bool CaseFolderViewOut => caseFolder3D != null && caseFolder3D.IsOut;

    /// <summary>T10: reading the carried Case Folder from the bag opens the same floating 3D view as its icon.</summary>
    public bool OpenCaseFolderView() => caseFolder3D != null && caseFolder3D.Open(null);

    private void ResolveSceneDependencies()
    {
        Scene scene = gameObject.scene;
        GameObject[] roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            if (cameraController == null) cameraController = roots[i].GetComponentInChildren<CameraController>(true);
            if (settings == null) settings = roots[i].GetComponentInChildren<SettingsUI>(true);
        }
        if (results == null) results = CaseResultPopupUI.Instance;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        // The legacy Level / debug labels stay hidden: the HUD only goes away
        // with its scene, and activating objects being destroyed is an error.
        if (legacyTimerHider != null) Destroy(legacyTimerHider);
        for (int i = 0; i < addedButtonGroups.Count; i++)
            if (addedButtonGroups[i] != null) Destroy(addedButtonGroups[i]);
        for (int i = 0; i < sceneButtonGroups.Count; i++)
            if (sceneButtonGroups[i] != null && !addedButtonGroups.Contains(sceneButtonGroups[i]))
            {
                sceneButtonGroups[i].alpha = 1f;
                sceneButtonGroups[i].blocksRaycasts = true;
                sceneButtonGroups[i].interactable = true;
            }
        for (int i = 0; i < disabledNotesHandlers.Count; i++)
            if (disabledNotesHandlers[i] != null) disabledNotesHandlers[i].enabled = true;
        if (notesPaper != null) Destroy(notesPaper.gameObject);
        if (profileCards != null) Destroy(profileCards.gameObject);
        for (int i = 0; i < addedComponents.Count; i++)
            if (addedComponents[i] != null) Destroy(addedComponents[i]);
        for (int i = 0; i < addedObjects.Count; i++)
            if (addedObjects[i] != null) Destroy(addedObjects[i]);
        if (patchedFolderIcon != null)
        {
            patchedFolderIcon.ClickOverride = null;
            patchedFolderIcon.DropResolver = null;
            patchedFolderIcon.DropOpenOverride = null;
            patchedFolderIcon.DragVisual = null;
        }
        GateNonFolderIcons(false);
        if (safeRoot != null) Destroy(safeRoot.gameObject);
    }
}
