using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// PURPOSE:
/// Rosa in the office conference room (Level 2 consistency pass), under
/// Level2Gameplay/RosaConsultation. The Level 2 counterpart of Marie's
/// ClientConsultation, on the same foundation: the shared dialogue flow with
/// her as its owner, Level 1's consultation rules (ClientConsultationRules:
/// clues that fit, a moment after a returned audit or a new review day, the
/// visit ladder) and the same record (Level1ConsultationRecord, saved in her
/// review), plus a short line once per review stage (case JSON
/// review.roomMoments). From approval on, taps belong to the Client Copy
/// handover (ClientCopyHandover).
///
/// HER WALK (T32, Marie's trip on Level 1's NpcLife): she keeps her stall, the
/// records conversation and the toolkit tutorial beside her. Once her explicit
/// choice is recorded (or a legacy first check exists) and a short beat has passed, the
/// SAME Rosa walks from her stall through the front doors to the front desk
/// (town_life.json "rosa"), waits there a fixed receptionWaitSeconds, then walks
/// on to the conference room's client spot (case JSON clientFinale.spotPath),
/// where the stand-in client steps aside. No route indicator. Legs follow
/// RosaTripRules, read from conditions, never one-shot events; she never sets
/// off mid-conversation. A tap on the way gets one friendly line
/// (review.travelLines) while NpcLife pauses and then resumes the walk. An
/// approval while she is on her way sends her straight on (no remaining wait).
/// A failed route ends with NpcLife's out-of-view placement, never a visible
/// jump. Her leg is saved in her review (rosaLeg): a resume places her at the
/// front desk (her wait starts again) or in the room, without a walk. Without
/// the shared controller or route data she is placed once out of view instead.
/// Her stall, records desk and the folder's Supporting Documents stay usable
/// wherever she is.
///
/// Visiting is talk only: it never calls a trust, coin, XP, score, clock,
/// objective, review or story command, and never names an answer or changes
/// an amount.
/// </summary>
public sealed class Level2RosaConsultation : MonoBehaviour
{
    public const string ChildName = "RosaConsultation";
    public const string StageArrival = "arrival";
    public const string StageReviewing = "reviewing";
    public const string StageOfficeReview = "officeReview";
    public const string StageReadyToSubmit = "readyToSubmit";
    public const string StageHold = "hold";
    public const string StageCorrection = "correction";
    private const float CheckEvery = 0.25f;
    private const string EthicsChoiceAction = "rosa-ethics-choice";
    private const string EthicsConfirmAction = "rosa-ethics-confirm";

    public static Level2RosaConsultation Instance { get; private set; }

    private Level2RosaStall stall;
    private Transform spot;
    private Renderer[] renderers;
    private bool started;
    private bool hurry;
    private bool inRoom;
    private MarieTripRules.Leg leg = MarieTripRules.Leg.Plaza;
    private float leaveAt = -1f;
    private float waitUntil;
    private bool inVisit;
    private bool warned;
    private float nextCheck;
    private DialogueUI ethicsDialogue;
    private RectTransform ethicsChoicePanel;
    private float promptReadyAt;
    private Coroutine deskDiscussion;
    private bool deskDialogueFinished;

    /// <summary>Rosa stands at the conference room's client spot.</summary>
    public bool InRoom => inRoom;

    /// <summary>The one Rosa (built by her stall).</summary>
    public RosaInteractable Rosa => stall != null ? stall.Rosa : null;

    private void OnEnable() => Instance = this;

    private void OnDisable()
    {
        if (deskDiscussion != null) StopCoroutine(deskDiscussion);
        deskDiscussion = null;
        PlayerControlHold.Release(this);
        if (ethicsDialogue != null) ethicsDialogue.CancelDialogueFor(Rosa);
        ClearEthicsPrompt();
        UnhookEthicsDialogue();
        if (Instance == this) Instance = null;
    }

    private static CaseReviewState Review => Level2OfficeStart.Review;

    private static CaseDefinition Definition => CaseManager.Instance != null ? CaseManager.Instance.CurrentDefinition : null;

    // =========================================================
    // PLACEMENT (single owner)
    // =========================================================

    /// <summary>Where the review says she belongs: the office once Mark the Money! has begun (her stall before).</summary>
    public static bool BelongsInRoom(CaseReviewState review) => RosaTripRules.MarkTheMoneyStarted(review);

    /// <summary>Her trip's leg now (presentation; saved as the place it leads to).</summary>
    public MarieTripRules.Leg CurrentLeg => leg;

    private void Update()
    {
        if (ethicsChoicePanel != null && (ethicsDialogue == null || ethicsDialogue.IsReturning || !ethicsDialogue.IsDialogueActive))
            ClearEthicsPrompt();
        // The desk handoff owns the transition until the existing dialogue has returned its camera.
        if (deskDiscussion != null)
        {
            PlayerControlHold.Refresh();
            if (ethicsDialogue != null && !ethicsDialogue.IsReturning &&
                UnityEngine.InputSystem.Keyboard.current != null &&
                UnityEngine.InputSystem.Keyboard.current.escapeKey.wasPressedThisFrame)
                ethicsDialogue.CancelDialogueFor(Rosa);
            return;
        }
        if (inRoom || Time.unscaledTime < nextCheck) return;
        nextCheck = Time.unscaledTime + CheckEvery;
        if (!Resolve()) return;
        CaseReviewState review = Review;
        RosaInteractable rosa = Rosa;
        if (!started)
        {
            // Read the review once the checkpoint is back: a resumed review places her without a walk.
            if (Level2Checkpointer.Active != null && !Level2Checkpointer.Active.IsReady) return;
            started = true;
            ApplyResume(review);
            if (inRoom) return;
        }
        // On her way: NpcLife owns the walk (a conversation pauses it); arrival moves her on.
        if (leg == MarieTripRules.Leg.ToReception || leg == MarieTripRules.Leg.ToRoom) return;
        bool begun = RosaTripRules.MarkTheMoneyStarted(review);
        if (begun && leaveAt < 0f) leaveAt = Time.time + Mathf.Max(0f, Walk.leaveDelaySeconds);
        // Never set off mid-conversation (hers or anyone's on screen).
        if (rosa == null || rosa.Talking || inVisit || DialogueUI.IsAnyDialogueActive) return;
        if (rosa.Life == null || !HasRoute)
        {
            // No walk possible (no controller or route data): the old placement, never in view.
            if ((begun || hurry) && !Visible()) PlaceInRoomNow();
            return;
        }
        MarieTripRules.Leg next = RosaTripRules.Next(leg, begun, leaveAt >= 0f && Time.time >= leaveAt, Time.time >= waitUntil, hurry);
        if (next != leg) StartLeg(next);
    }

    /// <summary>
    /// Asks her to the conference room: at once under a fade (the Client Copy restore), or on foot
    /// (an approval while she is still on her way: she skips the rest of her front-desk wait).
    /// </summary>
    public void MoveToRoom(bool immediate)
    {
        if (inRoom) return;
        hurry = true;
        if (immediate && Resolve() && Rosa != null && !Rosa.Talking) PlaceInRoomNow();
    }

    private RosaWalkDefinition Walk => Level2Town.Instance != null ? Level2Town.Instance.Rosa : FallbackWalk;
    private static readonly RosaWalkDefinition FallbackWalk = new RosaWalkDefinition();
    private bool HasRoute => Walk.toReception != null && Walk.toReception.Count > 0 && Walk.toRoom != null && Walk.toRoom.Count > 0;

    private void StartLeg(MarieTripRules.Leg next)
    {
        RosaInteractable rosa = Rosa;
        NpcLife life = rosa.Life;
        RosaWalkDefinition walk = Walk;
        var stops = new List<Vector3>();
        if (next == MarieTripRules.Leg.ToReception)
        {
            foreach (TownPoint point in walk.toReception) stops.Add(NpcLife.Ground(point.Position));
            TownPoint wait = walk.toReception[walk.toReception.Count - 1];
            leg = MarieTripRules.Leg.ToReception;
            Save();
            // A small cheerful beat as she sets off (no route indicator: she simply walks).
            WorldEmote.Burst(rosa.transform, Emotion.Cheerful, 3);
            Debug.Log("[Level2] Rosa leaves her stall for the office front desk.");
            life.TravelForQuest(stops, wait.Facing, () =>
            {
                leg = MarieTripRules.Leg.AtReception;
                waitUntil = Time.time + Mathf.Max(0f, walk.receptionWaitSeconds);
                Debug.Log("[Level2] Rosa waits at the front desk.");
            });
            return;
        }
        if (next != MarieTripRules.Leg.ToRoom) return;
        // From her stall (an approval before she set off) she crosses the front doors first.
        if (leg == MarieTripRules.Leg.Plaza)
            for (int i = 0; i < walk.toReception.Count - 1; i++) stops.Add(NpcLife.Ground(walk.toReception[i].Position));
        foreach (TownPoint point in walk.toRoom) stops.Add(NpcLife.Ground(point.Position));
        stops.Add(RoomFeet());
        leg = MarieTripRules.Leg.ToRoom;
        Save();
        Debug.Log("[Level2] Rosa walks to the conference room.");
        life.TravelForQuest(stops, RoomFacing(), ArriveInRoom);
    }

    private void ArriveInRoom()
    {
        ClientConsultation.StandInStepsAside(spot);
        inRoom = true;
        leg = MarieTripRules.Leg.InRoom;
        Save();
        Debug.Log("[Level2] Rosa waits in the conference room.");
    }

    /// <summary>A resumed review: she stands where her saved leg (or the review) puts her. No walk, no line, no save.</summary>
    private void ApplyResume(CaseReviewState review)
    {
        MarieTripRules.Leg resumed = RosaTripRules.Resume(review != null ? review.rosaLeg : null, review);
        if (resumed == MarieTripRules.Leg.InRoom) PlaceInRoomNow();
        else if (resumed == MarieTripRules.Leg.AtReception && HasRoute && Rosa != null)
        {
            TownPoint wait = Walk.toReception[Walk.toReception.Count - 1];
            Place(NpcLife.Ground(wait.Position), wait.Facing);
            leg = MarieTripRules.Leg.AtReception;
            leaveAt = Time.time;
            waitUntil = Time.time + Mathf.Max(0f, Walk.receptionWaitSeconds);
        }
    }

    private bool Visible()
    {
        if (renderers == null)
        {
            RosaInteractable rosa = Rosa;
            renderers = rosa != null ? rosa.GetComponentsInChildren<Renderer>(true) : new Renderer[0];
        }
        foreach (Renderer r in renderers)
            if (r != null && r.enabled && r.isVisible) return true;
        return false;
    }

    /// <summary>Puts her at the room's client spot at once (a resume under the fade, a Client Copy restore, or no walk data).</summary>
    private void PlaceInRoomNow()
    {
        if (inRoom || Rosa == null || spot == null) return;
        ClientConsultation.StandInStepsAside(spot);
        Place(RoomFeet(), RoomFacing());
        inRoom = true;
        leg = MarieTripRules.Leg.InRoom;
        Debug.Log("[Level2] Rosa waits in the conference room.");
    }

    private void Place(Vector3 feet, Quaternion facing)
    {
        RosaInteractable rosa = Rosa;
        if (rosa.Life != null) rosa.Life.PlaceForQuest(feet, facing);
        else
        {
            rosa.transform.position = feet;
            rosa.SetRest(facing);
        }
        Physics.SyncTransforms();
    }

    private Vector3 RoomFeet()
    {
        Vector3 feet = spot.position;
        if (Physics.Raycast(spot.position + Vector3.up * 1f, Vector3.down, out RaycastHit floor, 2.5f,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            feet.y = floor.point.y;
        return feet;
    }

    private Quaternion RoomFacing() => Quaternion.Euler(0f, spot.eulerAngles.y, 0f);

    private void Save()
    {
        CaseReviewState review = Review;
        if (review == null) return;
        review.rosaLeg = RosaTripRules.ToSaved(leg);
        if (Level2Checkpointer.Active != null) Level2Checkpointer.Active.RequestSave();
    }

    // =========================================================
    // VISITS
    // =========================================================

    /// <summary>A tap on Rosa (RosaInteractable): true when this owner answered it (she is in the room).</summary>
    public bool HandleTap(RosaInteractable rosa)
    {
        if (rosa == null || rosa != Rosa || inVisit) return false;
        CaseReviewState review = Review;
        if (EthicsChoiceDue(review)) return StartEthicsChoice();
        // On her way (or waiting at the front desk): one friendly line; NpcLife pauses the walk and resumes it.
        if (!inRoom && leg != MarieTripRules.Leg.Plaza) return StartTravelAside();
        if (!inRoom) return false;
        if (review == null) return false;
        ClientCopyHandover handover = ClientCopyHandover.Instance;
        if (handover != null && handover.Phase != ClientCopyPhase.None)
        {
            // From approval on the Client Copy owns her taps (a busy one is simply ignored).
            handover.HandleRosaVisit(rosa);
            return true;
        }
        if (review.audit != null && review.audit.approved)
        {
            // Approved, the Auditor has not handed the copy over yet.
            return StartVisit(Build(new List<DialogueLineDefinition>
            {
                new DialogueLineDefinition { speaker = "Npc", text = "The Auditor approved it? Thank you. I'll wait here for my copy.", portraitId = "Rosa_Default", emote = "heart" }
            }), null);
        }
        return StartConsultation(review);
    }

    private static bool EthicsChoiceDue(CaseReviewState review)
    {
        CaseDefinition definition = Definition;
        if (review == null || definition?.review?.annotation == null || review.audit?.approved == true ||
            review.phase < ReviewPhase.Challenge || review.challenge == null || review.challenge.firstChecked ||
            Level2Checkpoint.EthicsChoiceCommitted(review, definition.caseId)) return false;
        return IncomeChallenge.Missing(review.challenge, definition.review.annotation).Count == 0;
    }

    /// <summary>A tap on her on the way: one friendly line by where she is (talk only, nothing saved or awarded).</summary>
    private bool StartTravelAside()
    {
        MarieTravelDefinition travel = Definition?.review?.travelLines;
        List<DialogueLineDefinition> source = travel == null ? null
            : leg == MarieTripRules.Leg.AtReception ? travel.atReception
            : leg == MarieTripRules.Leg.ToRoom ? travel.toRoom : travel.toOffice;
        List<DialogueLine> lines = Build(source);
        if (lines == null || lines.Count == 0) lines = Build(Neutral);
        return StartVisit(lines, null);
    }

    /// <summary>Complete first CHECK opens the existing Rosa choice, never a grade or a new choice owner.</summary>
    public bool TryDiscussFromDesk(MarketRecordsDesk desk)
    {
        CameraController camera = CameraController.Instance;
        if (deskDiscussion != null || !Resolve() || desk == null || desk != stall.Desk ||
            desk.Toolkit == null || !desk.Toolkit.IsOpen || desk.Viewpoint == null || camera == null ||
            camera.CurrentMode != CameraController.CameraMode.Workstation || camera.IsTransitioning ||
            IncomeToolkit.TeachingPaused || !EthicsChoiceDue(Review)) return false;
        // Save the annotations/current tab before changing presentation. Failure leaves the desk usable.
        if (Level2Checkpointer.Active == null || !Level2Checkpointer.Active.RequestSave()) return false;
        deskDiscussion = StartCoroutine(DiscussFromDesk(desk, Review));
        return true;
    }

    private IEnumerator DiscussFromDesk(MarketRecordsDesk desk, CaseReviewState review)
    {
        CameraController camera = CameraController.Instance;
        desk.Toolkit.SuspendForRosaDiscussion();
        camera.ExitFirstPerson();
        PlayerControlHold.Acquire(this);
        try
        {
            yield return null; // the toolkit is parked before the conversation takes camera ownership
            float until = Time.unscaledTime + 3f;
            while (camera != null && !camera.CanStartDialogue && Time.unscaledTime < until) yield return null;
            if (Review != review || camera == null || !camera.CanStartDialogue) yield break;
            deskDialogueFinished = false;
            bool admitted = StartEthicsChoice();
            if (admitted)
                while (!deskDialogueFinished && Review == review && Rosa != null && Rosa.isActiveAndEnabled)
                    yield return null;
            // Completion/cancellation callbacks run after DialogueUI restores its original camera.
            while (Review == review && (DialogueUI.IsAnyDialogueActive || InGameSettings.IsOpen)) yield return null;
            if (Review != review || desk == null || !desk.isActiveAndEnabled || camera == null ||
                camera.CurrentMode != CameraController.CameraMode.FirstPerson || camera.IsTransitioning) yield break;
            PlayerControlHold.Release(this); // release while still in FirstPerson, before the workstation locks input
            desk.OnInteract();
            bool committed = Level2Checkpoint.EthicsChoiceCommitted(review, Definition != null ? Definition.caseId : "");
            Level2Say.Line(committed ? "Your conversation is recorded. Check your work when ready."
                : "Your marks are kept. CHECK will let you talk with Rosa when you're ready.", null, 6f);
        }
        finally
        {
            PlayerControlHold.Release(this);
            deskDiscussion = null;
        }
    }

    private bool StartEthicsChoice()
    {
        string facts = "You reviewed the May figures: ₱12,000 for clothes washing and ₱3,000 for renting the spare washer. D3 shows another ₱5,000 paid for blanket washing; D5 says the ₱20,000 loan must be repaid.";
        var lines = new List<DialogueLine>
        {
            new DialogueLine(DialogueSpeaker.Npc, facts, "Rosa_Default", "Aling Rosa"),
            new DialogueLine(DialogueSpeaker.Npc, "The blanket payment is not in the current total. What will you tell me before we check the case?", "Rosa_Default", "Aling Rosa")
            {
                AwaitAction = EthicsChoiceAction,
                ActionPrompt = "Choose what to tell Rosa"
            }
        };
        if (!StartVisit(lines, EndEthicsDialogue)) return false;
        ethicsDialogue = DialogueUI.Active;
        if (ethicsDialogue == null)
        {
            EndEthicsDialogue();
            return false;
        }
        ethicsDialogue.LineRevealed += OnEthicsLineRevealed;
        if (ethicsDialogue.IsAwaitingAction && !ethicsDialogue.IsLineTyping) ShowEthicsChoices();
        return true;
    }

    private void OnEthicsLineRevealed(DialogueLine line)
    {
        if (line == null || ethicsDialogue == null || DialogueUI.Active != ethicsDialogue) return;
        if (line.AwaitAction == EthicsChoiceAction) ShowEthicsChoices();
        else if (line.AwaitAction == EthicsConfirmAction) ShowEthicsConfirmation();
        else ClearEthicsPrompt();
    }

    private void EndEthicsDialogue()
    {
        deskDialogueFinished = true;
        ClearEthicsPrompt();
        UnhookEthicsDialogue();
        // A disabled transition may already have released its hold while dialogue owned the controls.
        PlayerControlHold.Refresh();
    }

    private void UnhookEthicsDialogue()
    {
        if (ethicsDialogue != null) ethicsDialogue.LineRevealed -= OnEthicsLineRevealed;
        ethicsDialogue = null;
    }

    private void ShowEthicsChoices(string saveError = null)
    {
        string intro = saveError ?? "Choose one clear answer. Your review still needs its supporting records, correction, approval, and Client Copy.";
        ShowEthicsPrompt(intro, new[]
        {
            Level2CampaignRules.ResponsibleChoiceText,
            Level2CampaignRules.UnsupportedChoiceText,
            Level2CampaignRules.DismissiveChoiceText
        }, new System.Action[]
        {
            () => SelectEthicsChoice(Level2EthicsChoice.Responsible),
            () => SelectEthicsChoice(Level2EthicsChoice.UnsupportedPromise),
            () => SelectEthicsChoice(Level2EthicsChoice.DismissiveHold)
        }, 430f);
    }

    private void ShowEthicsConfirmation(string saveError = null)
    {
        ShowEthicsPrompt(saveError ?? "The records leave one income amount unresolved. Calling this case ready now would be unsupported. Do you still want to make that promise?",
            new[] { "Confirm: I’ll call it ready now", "Go back to my choices" },
            new System.Action[]
            {
                () => CommitEthicsChoice(Level2EthicsChoice.UnsupportedPromise, EthicsConfirmAction),
                ReturnToEthicsChoices
            }, 340f);
    }

    private void ReturnToEthicsChoices()
    {
        ClearEthicsPrompt();
        if (ethicsDialogue == null || ethicsDialogue.ContinueWith(EthicsConfirmAction, new List<DialogueLine>
            {
                new DialogueLine(DialogueSpeaker.Npc, "Choose the words you want to use.", "Rosa_Default", "Aling Rosa")
                {
                    AwaitAction = EthicsChoiceAction,
                    ActionPrompt = "Choose what to tell Rosa"
                }
            }) < 0)
            ShowEthicsChoices();
    }

    private void SelectEthicsChoice(Level2EthicsChoice choice)
    {
        if (choice == Level2EthicsChoice.UnsupportedPromise)
        {
            ClearEthicsPrompt();
            var warning = new List<DialogueLine>
            {
                new DialogueLine(DialogueSpeaker.Npc,
                    "The records leave one income amount unresolved. Calling the case ready would be unsupported.", "Rosa_Default", "Aling Rosa"),
                new DialogueLine(DialogueSpeaker.Npc, "Confirm that you still want to call it ready.", "Rosa_Default", "Aling Rosa")
                {
                    AwaitAction = EthicsConfirmAction,
                    ActionPrompt = "Confirm the unsupported promise or go back"
                }
            };
            if (ethicsDialogue == null || ethicsDialogue.ContinueWith(EthicsChoiceAction, warning) < 0)
                ShowEthicsChoices();
            return;
        }

        CommitEthicsChoice(choice, EthicsChoiceAction);
    }

    private void CommitEthicsChoice(Level2EthicsChoice choice, string actionId)
    {
        CaseDefinition definition = Definition;
        if (definition == null || ethicsDialogue == null || ethicsDialogue.AwaitingActionId != actionId) return;
        PlayerProgressManager manager = PlayerProgressManager.Instance;
        CampaignRewards.Change trust = default;
        bool saved = manager != null
            ? manager.CommitLevelTwoEthicsChoice(choice, definition.caseId, out trust)
            : Level2Checkpointer.Active != null && Level2Checkpointer.Active.CommitLocalEthicsChoice(choice, definition.caseId);
        if (manager == null) trust = default;
        if (!saved)
        {
            if (actionId == EthicsConfirmAction) ShowEthicsConfirmation("It did not save. No promise was recorded. Retry this confirmation or go back.");
            else ShowEthicsChoices("It did not save. No choice was recorded. Choose again when ready.");
            return;
        }

        ClearEthicsPrompt();
        if (choice == Level2EthicsChoice.DismissiveHold)
            foreach (CaseFolderUI folder in FindObjectsByType<CaseFolderUI>(FindObjectsInactive.Include))
                if (!folder.IsReference && folder.IsShowing) folder.Hide();
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayPaperSFX();
        }
        StartCoroutine(PlayEthicsResponseCue());
        if (trust.Outcome == CampaignRewards.Outcome.Applied)
            HudFeedback.AnnounceCampaign(trust, default, null);

        List<DialogueLine> response = new List<DialogueLine>
        {
            new DialogueLine(DialogueSpeaker.Player, Level2CampaignRules.ChoiceText(choice), "Auditor_Default", "You")
        };
        string rosaResponse = choice == Level2EthicsChoice.Responsible
            ? "I hoped you could promise today. All right — let’s check the payment together."
            : choice == Level2EthicsChoice.UnsupportedPromise
                ? "I want to move forward. I'll wait for you to sort out the amount."
                : "I brought what I have. I’ll leave the records available so the review can still be finished.";
        response.Add(new DialogueLine(DialogueSpeaker.Npc, rosaResponse, "Rosa_Default", "Aling Rosa"));
        if (ethicsDialogue == null || ethicsDialogue.ContinueWith(actionId, response) < 0)
            Debug.LogWarning("[Level2] Rosa's choice was saved, but its response could not be inserted into the active conversation.");
    }

    // Presentation only: advancing or leaving skips the cue; no save or progression waits for it.
    private IEnumerator PlayEthicsResponseCue()
    {
        yield return new WaitForSecondsRealtime(0.4f);
        if (ethicsDialogue != null && ethicsDialogue.IsDialogueActive && !ethicsDialogue.IsReturning)
            AudioManager.Instance?.QueueInteractionSFX("npc");
    }

    private void ShowEthicsPrompt(string message, string[] labels, System.Action[] actions, float height)
    {
        ClearEthicsPrompt();
        if (ethicsDialogue == null) return;
        Canvas canvas = ethicsDialogue.GetComponentInParent<Canvas>();
        if (canvas == null) return;
        RectTransform body = HudTheme.CreatePanel("Rosa choice note", canvas.rootCanvas.transform, 18f);
        ethicsChoicePanel = body.parent as RectTransform;
        ethicsChoicePanel.anchorMin = ethicsChoicePanel.anchorMax = new Vector2(0.5f, 0.5f);
        ethicsChoicePanel.pivot = new Vector2(0.5f, 0.5f);
        ethicsChoicePanel.sizeDelta = new Vector2(980f, height);
        promptReadyAt = Time.unscaledTime + 0.2f;
        ethicsChoicePanel.SetAsLastSibling();
        AddPromptLabel(body, "ROSA'S RECORDS", 18f, 30f, FontStyles.Bold);
        AddPromptLabel(body, message, 58f, 66f, FontStyles.Normal);
        RectTransform later = HudTheme.CreateRect("Talk later", body);
        HudTheme.PlaceTopLeft(later, 756f, 14f, 160f, 38f);
        Image laterFace = HudTheme.AddImage(later, HudTheme.Rounded, HudTheme.Track, 8f);
        laterFace.raycastTarget = true;
        Button laterButton = later.gameObject.AddComponent<Button>();
        laterButton.targetGraphic = laterFace;
        laterButton.onClick.AddListener(() =>
        {
            if (Time.unscaledTime >= promptReadyAt && ethicsDialogue != null)
            {
                ClearEthicsPrompt();
                ethicsDialogue.CancelDialogueFor(Rosa);
            }
        });
        TextMeshProUGUI laterText = HudTheme.CreateLabel("Talk later text", later, "Talk later", 20f,
            HudTheme.TextPrimary, TextAlignmentOptions.Center, FontStyles.Normal);
        HudTheme.Stretch(laterText.rectTransform, 3f);

        float rowHeight = labels.Length == 3 ? 86f : 92f;
        float gap = 10f;
        float startY = labels.Length == 3 ? 132f : 142f;
        Button first = null;
        for (int i = 0; i < labels.Length; i++)
        {
            int choiceIndex = i;
            RectTransform rect = HudTheme.CreateRect("Rosa choice", body);
            HudTheme.PlaceTopLeft(rect, 30f, startY + i * (rowHeight + gap), 916f, rowHeight);
            Image face = HudTheme.AddImage(rect, HudTheme.Rounded, new Color32(34, 48, 61, 255), 12f);
            face.raycastTarget = true;
            Button button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = face;
            button.onClick.AddListener(() =>
            {
                if (Time.unscaledTime < promptReadyAt || ethicsChoicePanel == null) return;
                AudioManager.Instance?.QueueInteractionSFX("continue");
                actions[choiceIndex]?.Invoke();
            });
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 0.96f, 0.84f, 1f);
            colors.pressedColor = new Color(0.8f, 0.84f, 0.9f, 1f);
            colors.fadeDuration = 0.1f;
            button.colors = colors;
            TextMeshProUGUI label = HudTheme.CreateLabel("Choice text", rect, labels[i], 32f, HudTheme.TextPrimary,
                TextAlignmentOptions.Left, FontStyles.Normal);
            HudTheme.Stretch(label.rectTransform, 16f);
            label.textWrappingMode = TextWrappingModes.Normal;
            label.enableAutoSizing = true;
            label.fontSizeMin = 24f;
            label.fontSizeMax = 32f;
            if (first == null) first = button;
        }
        if (EventSystem.current != null && first != null) EventSystem.current.SetSelectedGameObject(first.gameObject);
        ethicsDialogue.DockChoices(ethicsChoicePanel);
    }

    private static TextMeshProUGUI AddPromptLabel(RectTransform parent, string text, float top, float height, FontStyles style)
    {
        RectTransform rect = HudTheme.CreateRect("Choice heading", parent);
        HudTheme.PlaceTopLeft(rect, 30f, top, 916f, height);
        TextMeshProUGUI label = rect.gameObject.AddComponent<TextMeshProUGUI>();
        label.font = HudTheme.Font;
        label.text = text;
        label.fontSize = style == FontStyles.Bold ? 24f : 22f;
        label.fontSizeMin = 18f;
        label.fontSizeMax = label.fontSize;
        label.enableAutoSizing = true;
        label.textWrappingMode = TextWrappingModes.Normal;
        label.overflowMode = TextOverflowModes.Overflow;
        label.color = style == FontStyles.Bold ? HudTheme.BrassSoft : HudTheme.TextPrimary;
        label.fontStyle = style;
        label.alignment = TextAlignmentOptions.Left;
        label.raycastTarget = false;
        return label;
    }

    private void ClearEthicsPrompt()
    {
        if (ethicsChoicePanel != null)
        {
            if (ethicsDialogue != null && ethicsDialogue.isActiveAndEnabled)
                ethicsDialogue.ReleaseChoices(ethicsChoicePanel);
            else Destroy(ethicsChoicePanel.gameObject);
        }
        ethicsChoicePanel = null;
    }

    private bool StartConsultation(CaseReviewState review)
    {
        CaseDefinition definition = Definition;
        if (review.consultation == null) review.consultation = new Level1ConsultationRecord();
        if (review.roomMomentsSaid == null) review.roomMomentsSaid = new List<string>();
        Level1ConsultationRecord candidate = JsonUtility.FromJson<Level1ConsultationRecord>(JsonUtility.ToJson(review.consultation));
        ConsultationContext context = Context(review);
        bool special = context.FailedAudits > candidate.seenFailedAudits || context.Day > candidate.seenDay ||
            !string.IsNullOrEmpty(candidate.pendingSpecial);
        string stage = special ? null : NextStage(review);
        List<DialogueLine> lines;
        if (stage != null) lines = Build(MomentLines(definition, stage));
        else
        {
            ConsultationPick pick = ClientConsultationRules.Next(Effective(definition), candidate, context);
            lines = pick.Lines != null ? Build(pick.Lines) : null;
        }
        if (lines == null || lines.Count == 0) lines = Build(Neutral);
        RosaInteractable rosa = Rosa;
        if (rosa == null) return false;
        inVisit = rosa.TryStartVisit(lines, () =>
        {
            inVisit = false;
            if (Review != review) return;
            Level1ConsultationRecord before = review.consultation;
            review.consultation = candidate;
            if (stage != null) review.roomMomentsSaid.Add(stage);
            if (Level2Checkpointer.Active != null && !Level2Checkpointer.Active.RequestSave())
            {
                review.consultation = before;
                if (stage != null) review.roomMomentsSaid.Remove(stage);
            }
        }, () => { inVisit = false; });
        return inVisit;
    }

    private bool StartVisit(List<DialogueLine> lines, System.Action done)
    {
        RosaInteractable rosa = Rosa;
        if (rosa == null) return false;
        inVisit = rosa.TryStartVisit(lines, () => { inVisit = false; done?.Invoke(); }, () => { inVisit = false; done?.Invoke(); });
        return inVisit;
    }

    /// <summary>The review stage's line still to say (arrival first), or null.</summary>
    public static string NextStage(CaseReviewState review)
    {
        if (review == null) return null;
        List<string> said = review.roomMomentsSaid ?? new List<string>();
        if (review.phase < ReviewPhase.OfficeReconciliation && !said.Contains(StageArrival)) return StageArrival;
        string now = StageOf(review);
        return now != null && !said.Contains(now) ? now : null;
    }

    /// <summary>Where the review stands for Rosa's lines (never an answer): reviewing, office review, or stamped and ready to hand in.</summary>
    public static string StageOf(CaseReviewState review)
    {
        if (review == null || (review.audit != null && review.audit.approved)) return null;
        if (review.phase < ReviewPhase.OfficeReconciliation) return StageReviewing;
        if (review.phase == ReviewPhase.Correction && review.audit != null && review.audit.lastRevision == review.packetRevision)
            return review.audit.lastOutcome == FinalAudit.Outcome.SupportedHold.ToString() ? StageHold : StageCorrection;
        bool stamped = review.finalStamp != null && !string.IsNullOrEmpty(review.finalStamp.verdict) && FinalAssessment.IsCurrent(review);
        return stamped ? StageReadyToSubmit : StageOfficeReview;
    }

    private static List<DialogueLineDefinition> MomentLines(CaseDefinition definition, string stage)
    {
        List<RoomMomentDefinition> moments = definition?.review?.roomMoments;
        if (moments != null)
            foreach (RoomMomentDefinition moment in moments)
                if (moment != null && moment.stage == stage && moment.lines != null && moment.lines.Count > 0) return moment.lines;
        return null;
    }

    /// <summary>The review as the consultation sees it: counters, document reads and empty entries (never answers or correctness).</summary>
    private static ConsultationContext Context(CaseReviewState review)
    {
        CaseData data = CaseManager.Instance != null ? CaseManager.Instance.CurrentCase : null;
        return new ConsultationContext
        {
            FailedAudits = review.audit != null ? review.audit.failedSubmissions : 0,
            Day = review.clock != null ? Mathf.Max(1, review.clock.day) : 1,
            CaseOpen = true,
            DocumentUnread = name => Unread(data, name),
            Field = key => data != null && data.folderTransferredFields != null && data.folderTransferredFields.Contains(key)
                ? ConsultationFieldState.Filled : ConsultationFieldState.Empty
        };
    }

    private static bool Unread(CaseData data, string documentName)
    {
        if (data?.supportingDocuments == null) return false;
        foreach (SupportingDocument document in data.supportingDocuments)
            if (document != null && document.documentName == documentName) return !document.isReviewed;
        return false;
    }

    private static ClientConsultationDefinition Effective(CaseDefinition def)
    {
        ClientConsultationDefinition source = def?.consultation ?? new ClientConsultationDefinition { enabled = false };
        return new ClientConsultationDefinition
        {
            enabled = source.enabled,
            clues = source.clues ?? new List<ConsultationClue>(),
            failedAudit = source.failedAudit ?? new List<ConsultationVariant>(),
            laterDay = source.laterDay ?? new List<ConsultationVariant>(),
            ladder = source.ladder ?? new List<ConsultationStep>(),
            fallback = source.fallback ?? new List<DialogueLineDefinition>()
        };
    }

    private static readonly List<DialogueLineDefinition> Neutral = new List<DialogueLineDefinition>
    {
        new DialogueLineDefinition { speaker = "Npc", text = "I'm right here if you need anything about my records.", portraitId = "Rosa_Default" }
    };

    private static string ClientName
    {
        get
        {
            CaseDefinition def = Definition;
            string name = def?.review?.stall != null ? def.review.stall.speakerName : null;
            return string.IsNullOrWhiteSpace(name) ? "Aling Rosa" : name;
        }
    }

    private static List<DialogueLine> Build(List<DialogueLineDefinition> lines) =>
        lines != null && lines.Count > 0 ? CaseFactory.BuildDialogue(lines, ClientName) : null;

    // =========================================================
    // DATA
    // =========================================================

    private bool Resolve()
    {
        if (stall != null && spot != null) return true;
        CaseDefinition definition = Definition;
        if (definition == null || !definition.HasReview || Level2OfficeStart.Instance == null) return false;
        if (stall == null) stall = Level2OfficeStart.Instance.GetComponentInChildren<Level2RosaStall>();
        if (stall == null || !stall.Built || stall.Rosa == null) return false;
        string path = definition.clientFinale != null ? definition.clientFinale.spotPath : null;
        spot = string.IsNullOrWhiteSpace(path) ? null : PlayerPlacement.Find(gameObject.scene, path);
        if (spot == null)
        {
            if (!warned) Debug.LogWarning($"[Level2] The conference-room client spot '{path}' was not found; Rosa stays at her stall.");
            warned = true;
            return false;
        }
        WarnProblems(definition);
        return true;
    }

    private static void WarnProblems(CaseDefinition definition)
    {
        ClientConsultationDefinition consultation = Effective(definition);
        if (!ClientConsultationRules.HasContent(consultation)) return;
        var documents = new List<string>();
        if (definition.documents != null)
            foreach (DocumentDefinition document in definition.documents)
                if (document != null && !string.IsNullOrWhiteSpace(document.documentName)) documents.Add(document.documentName);
        var answers = new List<string>();
        foreach (RequiredFieldDefinition field in definition.GradedFields())
        {
            string value = CaseFieldRules.AnswerValue(definition.answerKey, field.key);
            if (string.IsNullOrWhiteSpace(value)) continue;
            answers.Add(value.Trim());
            answers.Add(System.Text.RegularExpressions.Regex.Replace(value.Trim(), "(?<=[a-z])(?=[A-Z])", " "));
        }
        List<string> problems = ClientConsultationRules.Problems(consultation, documents, Level1CaseCheckpoint.FieldKeys, answers);
        if (problems.Count > 0)
            Debug.LogWarning($"[Level2] {problems.Count} of Rosa's consultation entries were skipped or need a look: {string.Join("; ", problems)}");
    }
}
