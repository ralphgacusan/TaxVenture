using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// PURPOSE:
/// The corkboard's cards react to the board's check (Level 1 Phase 3):
/// sparkles when a card is right, a "?" when it's wrong, a "..." while it's
/// locked. (T22: the WorldEmote emotion set.)
///
/// 2026-10-26 (user request): people show no icons outside a conversation.
/// The plaza clients' sparkles after the side quest, Marie's worried bubble
/// after a failed audit and her thank-you bubble over the closing cards are
/// removed; emotes now only appear on dialogue lines.
/// Observation only: listens to events, shows bubbles; changes nothing.
/// </summary>
public sealed class ConsequenceReactions : MonoBehaviour
{
    private const string TargetSceneName = "Office_Level1";

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
        if (!scene.IsValid() || !scene.isLoaded || scene.name != TargetSceneName) return;
        foreach (GameObject root in scene.GetRootGameObjects())
            if (root.GetComponentInChildren<ConsequenceReactions>(true) != null) return;
        var host = new GameObject("Consequence Reactions");
        SceneManager.MoveGameObjectToScene(host, scene);
        host.AddComponent<ConsequenceReactions>();
    }

    private void OnEnable()
    {
        CorkboardSideQuestManager.CardPlaced += HandleCardPlaced;
    }

    private void OnDisable()
    {
        CorkboardSideQuestManager.CardPlaced -= HandleCardPlaced;
    }

    private void HandleCardPlaced(ProfileDragCard card, bool correct, bool locked)
    {
        if (card == null) return;
        Emotion emotion = correct ? Emotion.Happy : locked ? Emotion.Thinking : Emotion.Confused;
        WorldEmote.Show(card.transform, emotion, locked ? CorkboardSideQuestManager.LockSeconds : 2.2f);
    }
}
