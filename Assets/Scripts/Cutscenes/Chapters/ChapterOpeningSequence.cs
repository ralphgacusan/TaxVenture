using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Menu-owned continuation for the existing standalone opening scenes. No save writes.</summary>
public sealed class ChapterOpeningSequence : MonoBehaviour
{
    private static string pendingOffice, pendingScene;
    private string office, expectedId;
    private bool returning;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        pendingOffice = pendingScene = null;
        SceneManager.sceneLoaded -= Loaded;
    }

    public static void LoadChapter(string destination)
    {
        if (pendingOffice != null) return;
        PlayerProgressData data = PlayerProgressManager.Instance != null ? PlayerProgressManager.Instance.CurrentProgress : null;
        if (ChapterEndingRules.Pending(data))
        {
            MenuEntry.Continue(null);
            return;
        }
        string opening = ChapterOpeningRules.SceneForOffice(destination);
        if (!Application.CanStreamedLevelBeLoaded(destination) ||
            !ChapterOpeningRules.ShouldPlay(data, CampaignRun.Chosen, destination, TrainingSession.IsPracticing))
        {
            LoadOffice(destination);
            return;
        }
        if (!Application.CanStreamedLevelBeLoaded(opening))
        {
            Debug.LogWarning("[ChapterOpening] " + opening + " unavailable; continuing to " + destination + ".");
            LoadOffice(destination);
            return;
        }
        pendingOffice = destination;
        pendingScene = opening;
        SceneManager.sceneLoaded -= Loaded;
        SceneManager.sceneLoaded += Loaded;
        ChapterCinematicPlayer.PendingContext = ChapterCinematicContext.Default;
        MobilePerformance.LoadScene(opening);
    }

    private static void Loaded(Scene scene, LoadSceneMode mode)
    {
        if (mode != LoadSceneMode.Single) return;
        SceneManager.sceneLoaded -= Loaded;
        string destination = pendingOffice, opening = pendingScene;
        pendingOffice = pendingScene = null;
        if (destination == null || scene.name != opening) return;
        var host = new GameObject("Chapter opening handoff");
        SceneManager.MoveGameObjectToScene(host, scene);
        var handoff = host.AddComponent<ChapterOpeningSequence>();
        handoff.office = destination;
        handoff.expectedId = ChapterOpeningRules.IdForScene(opening);
    }

    private void OnEnable() => ChapterCinematicPlayer.Finished += Finished;
    private void OnDestroy() => ChapterCinematicPlayer.Finished -= Finished;

    private IEnumerator Start()
    {
        // A broken/missing player must not strand normal level entry. Preview scenes have no handoff.
        float waited = 0f;
        while (!returning && waited < 10f)
        {
            var player = ChapterCinematicPlayer.Current;
            if (player != null && player.gameObject.scene == gameObject.scene && player.Id == expectedId) yield break;
            if (Application.isFocused) waited += Mathf.Min(Time.unscaledDeltaTime, 0.25f);
            yield return null;
        }
        if (returning) yield break;
        Debug.LogWarning("[ChapterOpening] Opening player unavailable; continuing to " + office + ".");
        ContinueToOffice();
    }

    private void Finished(ChapterCinematicResult result)
    {
        if (result.Id == expectedId && result.Watched) ContinueToOffice();
    }

    private void ContinueToOffice()
    {
        if (returning) return;
        returning = true;
        ChapterCinematicPlayer.Finished -= Finished;
        // The movie already holds black; the office's existing arrival fade reveals gameplay.
        LoadOffice(office);
    }

    private static void LoadOffice(string destination)
    {
        if (Application.CanStreamedLevelBeLoaded(destination))
        {
            MobilePerformance.LoadScene(destination);
            return;
        }
        Debug.LogError("[ChapterOpening] " + destination + " unavailable; returning to Career.");
        MenuEntry.OpenLevelSelect = true;
        MobilePerformance.LoadScene(MenuEntry.MenuSceneName);
    }
}
