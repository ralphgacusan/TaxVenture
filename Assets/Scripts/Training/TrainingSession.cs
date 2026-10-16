using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// PURPOSE:
/// The hand-off between the Tutorial Hub (TrainingBinder3D in the 3D Menu)
/// and a practice run (same idea as MenuEntry): which drill card is being
/// practised, GUIDED or MASTERY, and which tab to reopen afterwards.
///
/// While IsPracticing is true:
/// - GuideRuntime plays the drill instead of the level's own tutorial,
/// - PracticeModeRuntime sets the level up for the drill,
/// - PlayerProgressManager ignores level results (nothing reaches the career),
/// - TrainingRecorder ignores finished lessons.
/// Loading any scene other than the drill's ends the session, so a practice
/// flag can never leak into a real run.
///
/// The in-game lessons the player finishes are recorded by TrainingRecorder
/// (subscribed here, before the first scene loads).
/// </summary>
public static class TrainingSession
{
    public static bool IsPracticing => Drill != null;
    public static TrainingDrill Drill { get; private set; }
    public static TrainingCardDef Card { get; private set; }
    public static bool Mastery { get; private set; }

    /// <summary>The tab the hub opens on next time (kept after the run ends).</summary>
    public static string ReturnTab { get; set; }

    /// <summary>The card the hub focuses on and celebrates once (null = none).</summary>
    public static string ReturnCard { get; private set; }
    public static bool ReturnNewlyMastered { get; private set; }

    private static bool openBinder;

    /// <summary>The 3D Menu should open straight on the binder (back from a drill). Returns the request once.</summary>
    public static bool ConsumeOpenBinder()
    {
        bool open = openBinder;
        openBinder = false;
        return open;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;
        TrainingRecorder.Initialize();
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (mode != LoadSceneMode.Single || Drill == null) return;
        if (scene.name != Drill.scene) End();
    }

    /// <summary>Starts a practice run: loads the drill's scene.</summary>
    public static bool Begin(TrainingCardDef card, TrainingDrill drill, bool mastery)
    {
        if (card == null || drill == null || string.IsNullOrWhiteSpace(drill.scene)) return false;
        Card = card;
        Drill = drill;
        Mastery = mastery;
        ReturnTab = card.tab;
        ReturnCard = null;
        ReturnNewlyMastered = false;
        SceneManager.LoadScene(drill.scene);
        return true;
    }

    /// <summary>Plays the same drill again (or switches between guided and mastery).</summary>
    public static void Restart(bool mastery)
    {
        if (Drill == null) return;
        Mastery = mastery;
        SceneManager.LoadScene(Drill.scene);
    }

    /// <summary>Ends the run and goes back to the binder in the 3D Menu (finished = the card is celebrated).</summary>
    public static void ReturnToHub(bool finished, bool newlyMastered)
    {
        ReturnCard = Card != null && finished ? Card.id : null;
        ReturnNewlyMastered = finished && newlyMastered;
        if (Card != null) ReturnTab = Card.tab;
        openBinder = true;
        End();
        SceneManager.LoadScene(MenuEntry.MenuSceneName);
    }

    /// <summary>The hub read the return state; it is cleared so it plays once.</summary>
    public static void ConsumeReturn()
    {
        ReturnCard = null;
        ReturnNewlyMastered = false;
    }

    public static void End()
    {
        Drill = null;
        Card = null;
        Mastery = false;
    }

    /// <summary>The script GuideRuntime plays in this level while practising (null = the level's own).</summary>
    public static TrainingDrillScript.Result ComposeFor(string levelId)
    {
        if (Drill == null || Drill.levelId != levelId) return null;
        TrainingDrillScript.Result result = TrainingDrillScript.Compose(GuideScript.Load(levelId), Drill, Mastery);
        if (!string.IsNullOrEmpty(result.Problems))
            Debug.LogWarning($"[Training] Drill \"{Drill.id}\" ({(Mastery ? "mastery" : "guided")}):\n{result.Problems}");
        return result;
    }
}

/// <summary>
/// Records every in-game tutorial lesson the player finishes into the save
/// ("Level_01/move"), which opens the matching Tutorial Hub cards. Practice
/// runs are not recorded. Observation only.
/// </summary>
public static class TrainingRecorder
{
    private static bool initialized;

    public static void Initialize()
    {
        if (initialized) return;
        initialized = true;
        GuideDirector.LessonFinished += HandleLessonFinished;
    }

    private static void HandleLessonFinished(string levelId, string lessonId)
    {
        if (TrainingSession.IsPracticing) return;
        string key = PlayerProgressData.TutorialKey(levelId, lessonId);
        if (key == null || PlayerProgressManager.Instance == null) return;
        if (PlayerProgressManager.Instance.RecordTutorialSeen(key))
            Debug.Log($"[Training] Lesson seen: {key}");
    }
}
