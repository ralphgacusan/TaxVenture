using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// PURPOSE:
/// People react to what the player does (Level 1 Phase 3):
/// - Corkboard: every placed card gets a bubble: a cheer when it's right,
///   a shrug when it's wrong, a "..." while it's locked after repeated misses.
/// - Plaza: once the side quest is solved and the player is back on their
///   feet, the four plaza clients cheer.
/// - The case client (Marie): after a failed audit, a worried bubble with
///   her sad portrait (her NPC marker then shows she has something to say
///   again in the conference room); after approval, a thank-you with her
///   happy portrait.
/// Observation only: listens to events, shows bubbles; changes nothing.
/// </summary>
public sealed class ConsequenceReactions : MonoBehaviour
{
    private const string TargetSceneName = "Office_Level1";

    private bool plazaCheerPending;
    private bool clientWorryPending;
    private bool clientThanksPending;

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
        TownEvents.OnSideQuestCompleted += HandleSideQuestCompleted;
        CaseProgressionManager.OnAuditFailed += HandleAuditFailed;
        GameplayEvents.OnCaseCompleted += HandleCaseCompleted;
    }

    private void OnDisable()
    {
        CorkboardSideQuestManager.CardPlaced -= HandleCardPlaced;
        TownEvents.OnSideQuestCompleted -= HandleSideQuestCompleted;
        CaseProgressionManager.OnAuditFailed -= HandleAuditFailed;
        GameplayEvents.OnCaseCompleted -= HandleCaseCompleted;
    }

    private void HandleCardPlaced(ProfileDragCard card, bool correct, bool locked)
    {
        if (card == null) return;
        WorldEmote.Kind kind = correct ? WorldEmote.Kind.Cheer : locked ? WorldEmote.Kind.Wait : WorldEmote.Kind.Shrug;
        WorldEmote.Show(card.transform, kind, locked ? CorkboardSideQuestManager.LockSeconds : 2.2f);
    }

    private void HandleSideQuestCompleted() => plazaCheerPending = true;
    private void HandleAuditFailed(int mistakes, LevelScoring.Award cost) => clientWorryPending = true;

    private void HandleCaseCompleted()
    {
        clientWorryPending = false;
        clientThanksPending = true;
    }

    private static bool PlayerFree()
    {
        CameraController controller = CameraController.Instance;
        return controller != null && controller.CurrentMode == CameraController.CameraMode.FirstPerson &&
               !DialogueUI.IsAnyDialogueActive && !HudRewardScreen.AnyOpen;
    }

    private void Update()
    {
        // The plaza cheers once the board is closed and the player is walking again.
        if (plazaCheerPending && PlayerFree())
        {
            plazaCheerPending = false;
            foreach (TaxpayerNpcInteractable npc in FindObjectsByType<TaxpayerNpcInteractable>(FindObjectsInactive.Exclude))
                if (npc.gameObject.scene == gameObject.scene) WorldEmote.Show(npc.transform, WorldEmote.Kind.Cheer, 5f);
        }

        CaseDefinition def = CaseManager.Instance != null ? CaseManager.Instance.CurrentDefinition : null;

        // Marie hears about the failed audit once the Audit Summary closes.
        if (clientWorryPending && !DialogueUI.IsAnyDialogueActive && !HudRewardScreen.AnyOpen)
        {
            clientWorryPending = false;
            if (def != null && !string.IsNullOrWhiteSpace(def.clientFailedBubble))
                GuideVoice.TrySay(def.clientFailedBubble, "portrait:" + Portrait(def.clientFailedPortrait, "Client1_Marie_Sad"), 6f);
        }

        // Approved: her thanks shows over the closing cards.
        if (clientThanksPending && !DialogueUI.IsAnyDialogueActive)
        {
            clientThanksPending = false;
            // The end-of-level scene has her say it in person instead.
            if (CaseFinale.Instance != null && CaseFinale.Instance.Running) return;
            if (def != null && !string.IsNullOrWhiteSpace(def.clientApprovedBubble))
                GuideVoice.TrySay(def.clientApprovedBubble, "portrait:" + Portrait(def.clientApprovedPortrait, "Client1_Marie_Happy"), 6f);
        }
    }

    private static string Portrait(string configured, string fallback) =>
        string.IsNullOrWhiteSpace(configured) ? fallback : configured;
}
