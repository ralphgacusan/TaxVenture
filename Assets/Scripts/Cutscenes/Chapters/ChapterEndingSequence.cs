using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Owns only the additive ending/result handoff. Domain service/rewards remain with existing owners.</summary>
public sealed class ChapterEndingSequence : MonoBehaviour
{
    private static ChapterEndingSequence active;
    public static bool BlocksGameplay => active != null && (active.running || Time.frameCount <= active.releaseFrame);
    /// <summary>True until the cutscene scene has fully unloaded (the result rebuilds under its curtain).</summary>
    public static bool CinematicLoaded => active != null && active.cinematic.IsValid() && active.cinematic.isLoaded;
    private bool running, requested, received, retry, shuttingDown;
    private int releaseFrame = -1;
    private string chapter;
    private Action continuation;
    private ChapterCinematicResult result;
    private Scene cinematic;
    private readonly List<Behaviour> hidden = new List<Behaviour>();
    private readonly List<Renderer> renderers = new List<Renderer>();
    private GameObject retryPanel;
    private string bypassedChapter; // This run's movie failed and could not be saved as handled.
    private const float ReadyWaitLimit = 20f, PlaybackGrace = 30f;
    private const int SaveAttempts = 3;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reset() => active = null;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Initialize()
    {
        SceneManager.sceneLoaded -= Loaded;
        SceneManager.sceneLoaded += Loaded;
        for (int i = 0; i < SceneManager.sceneCount; i++) Ensure(SceneManager.GetSceneAt(i));
    }

    private static void Loaded(Scene scene, LoadSceneMode mode) => Ensure(scene);
    private static void Ensure(Scene scene)
    {
        if (!scene.IsValid() || !scene.isLoaded || (scene.name != "Office_Level1" && scene.name != "Office_Level2" && scene.name != "Office_Level3")) return;
        foreach (GameObject root in scene.GetRootGameObjects())
            if (root.GetComponent<ChapterEndingSequence>() != null) return;
        var host = new GameObject("Chapter ending handoff");
        SceneManager.MoveGameObjectToScene(host, scene);
        host.AddComponent<ChapterEndingSequence>();
    }

    private void Awake() => active = this;

    public static bool HasHandled(string id)
    {
        if (id != null && active != null && active.bypassedChapter == id) return true;
        PlayerProgressManager manager = PlayerProgressManager.Instance;
        return manager != null && ChapterEndingRules.Pending(manager.CurrentProgress, id) && manager.CurrentProgress.campaign.ending.consumed;
    }

    /// <summary>True when this sequence owns the continuation; false preserves the caller's old result path.</summary>
    public static bool TryPlay(string id, Action next)
    {
        PlayerProgressManager manager = PlayerProgressManager.Instance;
        if (active == null || manager == null || PlayerProgressManager.CurrentRunMode != CampaignRunMode.Story ||
            TrainingSession.IsPracticing || manager.CurrentProgress == null || !manager.CurrentProgress.HasCampaign) return false;
        if (active.bypassedChapter == id) return false;
        if (!ChapterEndingRules.Pending(manager.CurrentProgress, id) &&
            (id == "Level_02" || manager.CurrentProgress.campaign.IsChapterCompleted(id) || !ChapterEndingRules.Eligible(manager.CurrentProgress, id))) return false;
        if (active.running) return true;
        if (HasHandled(id)) return false;
        active.requested = true;
        active.chapter = id;
        active.continuation = next;
        active.running = true;
        active.StartCoroutine(active.Run());
        return true;
    }

    private void Update()
    {
        if (running) { PlayerControlHold.Refresh(); return; }
        // L2 delivery already commits chapter completion and clears its gameplay checkpoint.
        // The saved result handoff must therefore resume independently of Rosa's actor state.
        if (requested || gameObject.scene.name != "Office_Level2" || PlayerProgressManager.CurrentRunMode != CampaignRunMode.Story ||
            LevelCompleteUI.Instance == null) return;
        PlayerProgressManager manager = PlayerProgressManager.Instance;
        if (manager == null || !ChapterEndingRules.Pending(manager.CurrentProgress, "Level_02")) return;
        string json = manager.CurrentProgress.campaign.ending.resultJson;
        LevelTotalResult saved = string.IsNullOrEmpty(json) ? null : JsonUtility.FromJson<LevelTotalResult>(json);
        if (saved == null) return;
        requested = true;
        LevelCompleteUI.Instance.ShowAfterHandover(saved);
    }

    private static bool Ready() => !DialogueUI.IsAnyDialogueActive &&
        (DialogueUI.Active == null || !DialogueUI.Active.IsPanelShowing) &&
        (CameraController.Instance == null || !CameraController.Instance.IsTransitioning) &&
        !InGameSettings.IsOpen && !HudRewardScreen.AnyOpen && !SceneFade.BlocksInput;

    private bool ReadyToPresent() => Ready() && (chapter != "Level_02" ||
        (Level2Checkpointer.Active != null && Level2Checkpointer.Active.IsReady &&
         Level2OfficeStart.Instance != null && Level2OfficeStart.Instance.PlayerPlaced));

    /// <summary>
    /// Always ends at the result card. A failed, missing or stalled movie is logged and
    /// treated as skipped: nothing here may leave the player on a held, half-hidden scene.
    /// </summary>
    private IEnumerator Run()
    {
        yield return null; // The conversation's end-of-dialogue callbacks finish before gameplay is hidden.
        yield return ChapterEndingRules.Guarded(Present(), Debug.LogException);
        yield return ChapterEndingRules.Guarded(RecordHandled(), Debug.LogException);
        try { Uncover(); }
        catch (Exception error) { Debug.LogException(error); }
        running = false;
        releaseFrame = Time.frameCount + 1; // Skip/Back must not activate the result's Continue.
        try { continuation?.Invoke(); }
        catch (Exception error) { Debug.LogException(error); }
        yield return ChapterEndingRules.Guarded(Unload(), Debug.LogException);
        PlayerControlHold.Release(this);
        while (Time.frameCount <= releaseFrame) yield return null;
        AudioManager.Instance?.RefreshCinematicAudio();
    }

    private IEnumerator Present()
    {
        float waited = 0f;
        while (!ReadyToPresent())
        {
            if (waited >= ReadyWaitLimit)
            {
                Debug.LogWarning($"[ChapterEnding] {chapter}: still not ready after {ReadyWaitLimit:0} s; presenting anyway.");
                break;
            }
            if (!InGameSettings.IsOpen) waited += Time.unscaledDeltaTime;
            yield return null;
        }
        PlayerControlHold.Acquire(this);
        HideGameplay();
        AudioManager.Instance?.RefreshCinematicAudio();
        for (int attempt = 1; !ChapterEndingRules.Pending(PlayerProgressManager.Instance.CurrentProgress, chapter) &&
            !PlayerProgressManager.Instance.PreparePendingEnding(chapter); attempt++)
        {
            if (attempt >= SaveAttempts) break;
            yield return Retry("Couldn't save the ending handoff. Your work is safe.");
        }
        string sceneName = ChapterEndingRules.SceneFor(chapter);
        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogWarning($"[ChapterEnding] {sceneName} is not in the build; showing the result without the movie.");
            yield break;
        }
        PlayerProgressData progress = PlayerProgressManager.Instance.CurrentProgress;
        ChapterCinematicPlayer.PendingContext = new ChapterCinematicContext(
            ChapterEndingRules.Pending(progress, chapter) ? progress.campaign.ending.variant : "");
        ChapterCinematicPlayer.Finished += Finished;
        AsyncOperation load = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
        if (load != null) yield return load;
        cinematic = SceneManager.GetSceneByName(sceneName);
        if (!cinematic.IsValid() || !cinematic.isLoaded)
        {
            Debug.LogWarning($"[ChapterEnding] {sceneName} did not load; showing the result without the movie.");
            yield break;
        }
        ChapterCinematicPlayer player = ChapterCinematicPlayer.Current;
        if (player == null || player.gameObject.scene != cinematic || player.Id != ExpectedId)
        {
            Debug.LogWarning($"[ChapterEnding] {sceneName} has no playable {ExpectedId}; showing the result without the movie.");
            yield break;
        }
        SceneManager.SetActiveScene(cinematic);
        float played = 0f, limit = player.Script.duration + PlaybackGrace;
        while (!received && player != null)
        {
            // Suspended playback pauses its own clock, so only focused time counts.
            if (Application.isFocused) played += Mathf.Min(Time.unscaledDeltaTime, 0.25f);
            if (played > limit)
            {
                Debug.LogWarning($"[ChapterEnding] {ExpectedId} did not finish within {limit:0} s; showing the result.");
                player.Abort();
                break;
            }
            yield return null;
        }
    }

    /// <summary>Writes the handled movie before exposing its result; if it cannot, the result still shows this run.</summary>
    private IEnumerator RecordHandled()
    {
        ChapterCinematicPlayer.Finished -= Finished;
        ChapterCinematicPlayer.PendingContext = null;
        ChapterCinematicOutcome outcome = received && result.Watched ? result.Outcome : ChapterCinematicOutcome.Skipped;
        for (int attempt = 1; !PlayerProgressManager.Instance.CommitEndingConsumed(chapter, outcome); attempt++)
        {
            if (attempt >= SaveAttempts)
            {
                Debug.LogWarning($"[ChapterEnding] {chapter}: couldn't save the finished ending; continuing to the result.");
                bypassedChapter = chapter;
                PlayerProgressManager.Instance.MarkEndingHandledUnsaved(chapter);
                yield break;
            }
            yield return Retry("Couldn't save the ending handoff. Tap Retry to continue.");
        }
    }

    /// <summary>Rebuild the existing result under the cinematic's opaque curtain, then reveal it.</summary>
    private void Uncover()
    {
        // A failure before RecordHandled finished must not replay the movie when the continuation re-enters TryPlay.
        if (!HasHandled(chapter)) bypassedChapter = chapter;
        Camera camera = ChapterCinematicPlayer.Current != null ? ChapterCinematicPlayer.Current.CinematicCamera : null;
        if (camera != null)
        {
            camera.enabled = false;
            AudioListener listener = camera.GetComponent<AudioListener>();
            if (listener != null) listener.enabled = false;
        }
        RestoreGameplay();
        if (gameObject.scene.IsValid() && gameObject.scene.isLoaded) SceneManager.SetActiveScene(gameObject.scene);
    }

    private string ExpectedId => chapter == "Level_01" ? "chapter01.end" : chapter == "Level_02" ? "chapter02.end" : "chapter03.end";
    private void Finished(ChapterCinematicResult finished)
    {
        if (received || finished.Id != ExpectedId) return;
        result = finished;
        received = true;
    }

    private IEnumerator Unload()
    {
        if (gameObject.scene.IsValid() && gameObject.scene.isLoaded) SceneManager.SetActiveScene(gameObject.scene);
        if (cinematic.IsValid() && cinematic.isLoaded)
        {
            AsyncOperation unload = SceneManager.UnloadSceneAsync(cinematic);
            if (unload != null) yield return unload;
        }
        cinematic = default;
    }

    private void HideGameplay()
    {
        foreach (GameObject root in gameObject.scene.GetRootGameObjects())
        {
            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
                if (renderer != null && renderer.enabled) { renderers.Add(renderer); renderer.enabled = false; }
            // A component whose script is missing comes back as a null entry (every Office scene's Client has one).
            foreach (Behaviour behaviour in root.GetComponentsInChildren<Behaviour>(true))
                if (behaviour != null && behaviour.enabled && (behaviour is Camera || behaviour is Light || behaviour is Canvas || behaviour is AudioListener ||
                    behaviour is Interactor || behaviour is PlayerMovement || behaviour is FirstPersonCameraController))
                { hidden.Add(behaviour); behaviour.enabled = false; }
        }
    }

    private void RestoreGameplay()
    {
        foreach (Renderer renderer in renderers) if (renderer != null) renderer.enabled = true;
        foreach (Behaviour behaviour in hidden) if (behaviour != null) behaviour.enabled = true;
        renderers.Clear();
        hidden.Clear();
    }

    private IEnumerator Retry(string message)
    {
        Debug.LogWarning("[ChapterEnding] " + message);
        retry = false;
        retryPanel = new GameObject("Ending retry", typeof(RectTransform));
        retryPanel.transform.SetParent(transform, false);
        Canvas canvas = retryPanel.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 32700;
        var scaler = retryPanel.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        retryPanel.AddComponent<GraphicRaycaster>();
        RectTransform panel = HudTheme.CreateRect("Panel", retryPanel.transform);
        panel.anchorMin = panel.anchorMax = new Vector2(.5f, .5f);
        panel.sizeDelta = new Vector2(800, 260);
        HudTheme.AddImage(panel, null, new Color(0, 0, 0, .95f));
        TextMeshProUGUI label = HudTheme.CreateLabel("Message", panel, message, 32, HudTheme.TextPrimary, TextAlignmentOptions.Center);
        var labelRect = (RectTransform)label.transform;
        labelRect.anchorMin = new Vector2(.05f, .45f); labelRect.anchorMax = new Vector2(.95f, .95f);
        labelRect.offsetMin = labelRect.offsetMax = Vector2.zero;
        RectTransform button = HudTheme.CreateRect("Retry", panel);
        button.anchorMin = button.anchorMax = new Vector2(.5f, .22f);
        button.sizeDelta = new Vector2(260, 80);
        Image image = HudTheme.AddImage(button, null, HudTheme.BrassSoft);
        HudTheme.Stretch((RectTransform)HudTheme.CreateLabel("Text", button, "RETRY", 32, Color.black, TextAlignmentOptions.Center).transform);
        Button tap = button.gameObject.AddComponent<Button>();
        tap.targetGraphic = image;
        tap.onClick.AddListener(() => retry = true);
        while (!retry) yield return null;
        Destroy(retryPanel);
        retryPanel = null;
    }

    private void OnApplicationQuit() => shuttingDown = true;
    private void OnDestroy()
    {
        ChapterCinematicPlayer.Finished -= Finished;
        ChapterCinematicPlayer.PendingContext = null;
        RestoreGameplay();
        PlayerControlHold.Release(this);
        if (!shuttingDown && cinematic.IsValid() && cinematic.isLoaded) SceneManager.UnloadSceneAsync(cinematic);
        if (active == this) active = null;
        if (!shuttingDown) AudioManager.Instance?.RefreshCinematicAudio();
    }
}
