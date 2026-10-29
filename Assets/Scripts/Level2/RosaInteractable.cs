using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// PURPOSE:
/// Talking to Aling Rosa at her stall (ticket T04). Uses the existing
/// DialogueUI (conversation camera frames her through this component) and
/// her lines from the case JSON (review.stall):
/// - before the initial review is stamped: a short greeting, nothing else;
/// - during MarketVisit: the required conversation (paid laundry job and
///   original records, fixed order: the project has no dialogue choices);
///   when it COMPLETES, the handover happens once: records D1-D6 are
///   collected (review moves on to Tutorial), the conversation heart is
///   granted through the existing score ledger, RosaRecordsCollected fires
///   and the envelope fans the records onto the desk;
/// - afterwards: a short reminder. A cancelled conversation changes nothing.
/// She turns to face the player while talking (transform turn).
/// Level 2 consistency pass: once her records are handed over she waits in
/// the office conference room (Level2RosaConsultation places this same
/// actor; never a second Rosa) and taps there go to her consultation or the
/// Client Copy handover through TryStartVisit.
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
        life = GetComponent<NpcLife>();
    }

    // T32: with Level 1's NpcLife she has one body owner (it turns her to the player and back, and walks her).
    private NpcLife life;

    /// <summary>Her behaviour owner (null without the shared controller: she is then turned here and placed, not walked).</summary>
    public NpcLife Life => life;

    public void OnFocus() { if (highlight != null) highlight.Highlight(); }

    public void OnUnfocus() { if (highlight != null) highlight.Unhighlight(); }

    public string GetPromptText() => "Talk to Aling Rosa";

    /// <summary>A conversation is running with her (stall talk, a consultation or the Client Copy).</summary>
    public bool Talking => talking;

    /// <summary>Where she faces when not talking (her stall, or the conference room's client spot after the move).</summary>
    public void SetRest(Quaternion rotation)
    {
        restRotation = rotation;
        if (!talking)
        {
            if (turn != null) StopCoroutine(turn);
            turn = null;
            transform.rotation = rotation;
        }
    }

    /// <summary>
    /// A conference-room visit (Level 2 consistency pass) through the shared dialogue flow with her as
    /// its owner, as Marie's TryStartVisit: she turns to the player and back. Talk only.
    /// </summary>
    public bool TryStartVisit(List<DialogueLine> lines, System.Action completed, System.Action cancelled)
    {
        if (talking || lines == null || lines.Count == 0) return false;
        DialogueUI dialogue = FindAnyObjectByType<DialogueUI>(FindObjectsInactive.Include);
        if (dialogue == null) return false;
        bool admitted = dialogue.TryStartDialogue(lines,
            () => { EndVisit(); completed?.Invoke(); },
            () => { EndVisit(); cancelled?.Invoke(); },
            this);
        if (!admitted) return false;
        AudioManager.Instance?.QueueInteractionSFX("npc");
        talking = true;
        FacePlayer(true);
        return true;
    }

    private void OnDisable()
    {
        // Actor removal/scene interruption cancels its dialogue, never a handover milestone.
        if (DialogueUI.Active != null) DialogueUI.Active.CancelDialogueFor(this);
        talking = false;
        if (turn != null) StopCoroutine(turn);
        turn = null;
    }

    private void EndVisit()
    {
        talking = false;
        FacePlayer(false);
    }

    public void OnInteract()
    {
        if (talking) return;
        // In the conference room her visits (consultation, Client Copy) belong to Level2RosaConsultation.
        if (Level2RosaConsultation.Instance != null && Level2RosaConsultation.Instance.HandleTap(this)) return;
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

        Level2Town.Instance?.OnRosaDialogueStarted(this);
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
        if (life != null) return;   // NpcLife turns her for her own conversation and back afterwards
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
