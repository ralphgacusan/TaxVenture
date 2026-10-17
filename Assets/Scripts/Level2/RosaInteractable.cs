using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// PURPOSE:
/// Talking to Aling Rosa at her stall (ticket T04). Uses the existing
/// DialogueUI (conversation camera frames her through this component) and
/// her lines from the case JSON (review.stall):
/// - before the initial review is stamped: a short greeting, nothing else;
/// - during MarketVisit: the required conversation (referral payment and
///   original records, fixed order: the project has no dialogue choices);
///   when it COMPLETES, the handover happens once: records D1-D6 are
///   collected (review moves on to Tutorial), the conversation heart is
///   granted through the existing score ledger, RosaRecordsCollected fires
///   and the envelope fans the records onto the desk;
/// - afterwards: a short reminder. A cancelled conversation changes nothing.
/// She turns to face the player while talking (transform turn).
/// </summary>
public sealed class RosaInteractable : MonoBehaviour, IInteractable
{
    public const string NpcId = "rosa_santos";

    private Level2RosaStall stall;
    private HighlightEffect highlight;
    private Quaternion restRotation;
    private Coroutine turn;
    private bool talking;

    public void Init(Level2RosaStall owner)
    {
        stall = owner;
        restRotation = transform.rotation;
        highlight = HighlightEffect.For((Component)this);
    }

    public void OnFocus() { if (highlight != null) highlight.Highlight(); }

    public void OnUnfocus() { if (highlight != null) highlight.Unhighlight(); }

    public string GetPromptText() => "Talk to Aling Rosa";

    public void OnInteract()
    {
        if (talking) return;
        CaseReviewState review = Level2OfficeStart.Review;
        CaseDefinition definition = CaseManager.Instance != null ? CaseManager.Instance.CurrentDefinition : null;
        DialogueUI dialogue = FindAnyObjectByType<DialogueUI>(FindObjectsInactive.Include);
        if (review == null || definition == null || !definition.HasReview || dialogue == null) return;

        StallDefinition lines = definition.review.stall;
        string name = string.IsNullOrWhiteSpace(lines.speakerName) ? "Aling Rosa" : lines.speakerName;
        bool handover = review.phase == ReviewPhase.MarketVisit;
        List<DialogueLineDefinition> source =
            handover ? lines.conversationLines :
            review.HandoverDone ? lines.afterLines :
            lines.earlyLines;
        List<DialogueLine> built = CaseFactory.BuildDialogue(source, name);
        if (built == null || built.Count == 0) return;

        bool admitted = dialogue.TryStartDialogue(
            built,
            () => Finish(handover),
            () => Finish(false),
            this);
        if (!admitted) return;

        talking = true;
        FacePlayer(true);
    }

    private void Finish(bool completedHandover)
    {
        talking = false;
        FacePlayer(false);
        if (completedHandover) HandOver();
    }

    /// <summary>The single domain commit of the stall: records, heart, notification, desk animation.</summary>
    private void HandOver()
    {
        CaseReviewState review = Level2OfficeStart.Review;
        CaseDefinition definition = CaseManager.Instance != null ? CaseManager.Instance.CurrentDefinition : null;
        if (review == null || definition == null || !review.TryCollectHandover(definition.review.handoverDocumentIds)) return;

        if (CaseProgressionManager.Instance != null)
            CaseProgressionManager.Instance.AwardConversation(NpcId, "Talked with Aling Rosa", transform.position + Vector3.up * 1.6f);
        Debug.Log($"[Level2] Rosa handed over {definition.review.handoverDocumentIds.Count} records. Review phase: Tutorial.");
        GameplayEvents.RaiseRosaRecordsCollected();
        if (stall != null) stall.ShowRecordsOnDesk(instant: false);
        Level2Say.Line("Rosa's records are on the desk beside her, and in your folder too. Sit at the desk when you're ready.", "wow");
    }

    private void FacePlayer(bool toPlayer)
    {
        Transform player = CameraController.Instance != null ? CameraController.Instance.PlayerRoot : null;
        Quaternion target = restRotation;
        if (toPlayer && player != null)
        {
            Vector3 flat = player.position - transform.position;
            flat.y = 0f;
            if (flat.sqrMagnitude > 0.01f) target = Quaternion.LookRotation(flat.normalized, Vector3.up);
        }
        if (turn != null) StopCoroutine(turn);
        turn = StartCoroutine(TurnTo(target, toPlayer ? 0.3f : 0.6f));
    }

    private IEnumerator TurnTo(Quaternion target, float seconds)
    {
        Quaternion start = transform.rotation;
        for (float t = 0f; t < 1f; t += Time.deltaTime / seconds)
        {
            transform.rotation = Quaternion.Slerp(start, target, Mathf.SmoothStep(0f, 1f, t));
            yield return null;
        }
        transform.rotation = target;
        turn = null;
    }
}
