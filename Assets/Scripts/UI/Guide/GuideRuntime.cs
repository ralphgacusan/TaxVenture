using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// PURPOSE:
/// Builds the level guide for Office_Level1 at runtime (same self-bootstrap
/// pattern as OfficeHudRuntime and OfficeMinimap: no scene edits, no
/// serialized references): its own overlay canvas above the HUD, the
/// objective beacon layer, the coach view, the lesson director and the
/// objective guide (ground arrows + minimap pin).
///
/// Content lives in Resources/Tutorials/&lt;levelId&gt;/guide.json (Level_01 for
/// Office_Level1; other level scenes opt in with "guide" in level.json
/// officeFeatures, see OfficeLevelProfile). With the file
/// missing or "enabled": false, nothing is built. During a Tutorial Hub
/// practice run (TrainingSession) the drill's script plays instead, without
/// the SKIP TUTORIAL button.
/// </summary>
[DefaultExecutionOrder(110)]
public sealed class GuideRuntime : MonoBehaviour
{
    // The objective marker sits under the HUD paper windows (1100-1900);
    // the coach sits above everything (windows, profile cards 2650, drags),
    // so Arthur's box is never covered by Notes, Profiles or the Case Folder.
    private const int ObjectiveSortingOrder = 400;
    private const int CoachSortingOrder = 5000;

    public static GuideRuntime Instance { get; private set; }

    public GuideDirector Director { get; private set; }
    public ObjectiveGuide Objectives { get; private set; }
    /// <summary>The practice drill being played (null in a normal run).</summary>
    public TrainingDrillScript.Result Practice { get; private set; }

    private GameObject canvasObject;
    private GameObject objectiveCanvasObject;

    private void OnEnable() => Instance = this;

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
        if (!scene.IsValid() || !scene.isLoaded || !OfficeLevelProfile.Allows(scene, OfficeFeature.Guide)) return;
        GameObject[] roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
            if (roots[i].GetComponentInChildren<GuideRuntime>(true) != null)
                return;

        var host = new GameObject("Level Guide Runtime");
        SceneManager.MoveGameObjectToScene(host, scene);
        host.AddComponent<GuideRuntime>();
    }

    private void Awake()
    {
        Instance = this;
        string levelId = OfficeLevelProfile.LevelIdFor(this);
        Practice = TrainingSession.ComposeFor(levelId);
        GuideScript script = Practice != null ? Practice.Script : GuideScript.Load(levelId);
        if (script == null || !script.enabled)
        {
            enabled = false;
            return;
        }

        RectTransform beaconLayer = CreateCanvas("Guide Objective Canvas", ObjectiveSortingOrder, false, out objectiveCanvasObject);
        Objectives = ObjectiveGuide.Create(gameObject, script, beaconLayer);

        RectTransform safe = CreateCanvas("Guide Canvas", CoachSortingOrder, true, out canvasObject);
        // Reward cards hide every other overlay canvas; Arthur stays to explain them.
        canvasObject.AddComponent<HudKeepVisibleDuringRewards>();
        GuideCoachView view = GuideCoachView.Create(safe);
        Director = GuideDirector.Create(gameObject, script, levelId, safe, view);
        if (Practice != null) view.SetSkipVisible(false);
    }

    private RectTransform CreateCanvas(string name, int order, bool raycasts, out GameObject host)
    {
        host = new GameObject(name, typeof(RectTransform));
        SceneManager.MoveGameObjectToScene(host, gameObject.scene);
        host.layer = LayerMask.NameToLayer("UI");
        Canvas canvas = host.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = order;
        CanvasScaler scaler = host.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        if (raycasts) host.AddComponent<GraphicRaycaster>();

        RectTransform safe = HudTheme.CreateRect("Safe Area", host.transform);
        HudTheme.Stretch(safe);
        safe.gameObject.AddComponent<MobileControlsSafeArea>();
        return safe;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        if (canvasObject != null) Destroy(canvasObject);
        if (objectiveCanvasObject != null) Destroy(objectiveCanvasObject);
    }
}
