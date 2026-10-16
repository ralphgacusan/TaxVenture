using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// PURPOSE:
/// Plays the lessons of guide.json. Listens to game events (observation
/// only), decides which lesson starts next, shows its pages through
/// GuideCoachView / GuideWelcomeCard, points the spotlight and hand, and
/// waits for the action each page asks for.
///
/// While a page waits for a tap, the player is held still (movement, look
/// and interaction components are paused and restored afterwards; the
/// camera mode is NOT changed, so the HUD stays visible for the tour).
///
/// DOES NOT change gameplay state, rewards, objectives or the FSM.
/// </summary>
public sealed class GuideDirector : MonoBehaviour
{
    private const float PollInterval = 0.2f;

    private GuideScript script;
    private string levelId;
    private GuideCoachView view;
    private RectTransform canvasRoot;
    private GuideTargets targets;
    private bool touch;

    // Event counters ("event:X", "dialogue:start", "lesson:id" ...).
    private readonly Dictionary<string, int> counts = new Dictionary<string, int>();
    private readonly HashSet<string> finished = new HashSet<string>();
    private readonly Dictionary<string, Sprite> portraits = new Dictionary<string, Sprite>();
    private readonly Dictionary<GuideLesson, List<GuideTriggers.Trigger>> whenCache = new Dictionary<GuideLesson, List<GuideTriggers.Trigger>>();
    private readonly Dictionary<GuideLesson, float> pendingSince = new Dictionary<GuideLesson, float>();

    // Current lesson/page.
    private GuideLesson lesson;
    private int pageIndex;
    private GuidePage page;
    private List<GuideTriggers.Trigger> pageWait;
    private Dictionary<string, int> pageCounts;
    private float pageStart;
    private float moved;
    private float looked;
    private Vector3 lastPlayerPos;
    private float lastYaw;
    private bool hasLastPose;
    private bool suspended;
    private bool collapsed;
    private bool asideActive;
    private float asideUntil;
    private float typedDoneAt = -1f;
    private CameraController.CameraMode? pageStartMode;
    private GuideWelcomeCard welcomeCard;
    private float advanceAt = -1f;
    private bool skippedAll;

    // Startup.
    private bool started;
    private float readySince = -1f;

    // Polled observations.
    private float nextPoll;
    private bool wasDialogue;
    private ValueTransferManager transfers;
    private GameStateMachine stateMachine;
    private CorkboardSideQuestManager corkboard;
    private int lastWrong;
    private int lastOccupied;

    // Control hold while a page waits for a tap.

    public bool IsShowingLesson => lesson != null && !suspended;
    public bool IsBlocking { get; private set; }

    public static GuideDirector Create(GameObject host, GuideScript script, string levelId, RectTransform canvasRoot,
        GuideCoachView view)
    {
        var director = host.AddComponent<GuideDirector>();
        director.script = script;
        director.levelId = levelId;
        director.canvasRoot = canvasRoot;
        director.view = view;
        director.targets = new GuideTargets(host.scene);
        director.touch = ResolveTouch(script.platform);
        director.view.Configure(script.trail.ParsedColor, script.lettersPerSecond);
        director.view.SetName(script.guide.name);
        director.view.Tapped += director.Tap;
        director.view.SkipRequested += director.SkipAll;
        director.LoadFinished();
        return director;
    }

    public static bool ResolveTouch(string platform)
    {
        if (string.Equals(platform, "touch", StringComparison.OrdinalIgnoreCase)) return true;
        if (string.Equals(platform, "desktop", StringComparison.OrdinalIgnoreCase)) return false;
        // "auto": the Editor counts as the phone (TaxVenture is a mobile game).
        return Application.isMobilePlatform || Application.isEditor;
    }

    // =========================================================
    // EVENTS
    // =========================================================

    private void OnEnable()
    {
        GuideVoice.Handler = Say;
        TownEvents.OnProfileCollected += HandleProfileCollected;
        TownEvents.OnAllProfilesCollected += HandleAllProfiles;
        TownEvents.OnTutorialNpcReady += HandleGuideReady;
        TownEvents.OnTutorialIntroCompleted += HandleGuideTalked;
        TownEvents.OnSideQuestCompleted += HandleSideQuest;
        GameplayEvents.OnNotesUnlockRequested += HandleReceptionist;
        GameplayEvents.OnCaseFolderOpened += HandleFolderOpened;
        GameplayEvents.OnTaxCodeBookFirstOpened += HandleBookOpened;
        GameplayEvents.OnTaxReturnCollected += HandleReturnCollected;
        GameplayEvents.OnCaseCompleted += HandleCaseCompleted;
        GameplayEvents.OnCaseFolderStored += HandleFolderStored;
        GameplayEvents.OnCaseFolderReturned += HandleFolderReturned;
    }

    private void OnDisable()
    {
        if (GuideVoice.Handler == (System.Func<string, string, float, bool>)Say) GuideVoice.Handler = null;
        HudRewardScreen.HoldAutoContinue = false;
        TownEvents.OnProfileCollected -= HandleProfileCollected;
        TownEvents.OnAllProfilesCollected -= HandleAllProfiles;
        TownEvents.OnTutorialNpcReady -= HandleGuideReady;
        TownEvents.OnTutorialIntroCompleted -= HandleGuideTalked;
        TownEvents.OnSideQuestCompleted -= HandleSideQuest;
        GameplayEvents.OnNotesUnlockRequested -= HandleReceptionist;
        GameplayEvents.OnCaseFolderOpened -= HandleFolderOpened;
        GameplayEvents.OnTaxCodeBookFirstOpened -= HandleBookOpened;
        GameplayEvents.OnTaxReturnCollected -= HandleReturnCollected;
        GameplayEvents.OnCaseCompleted -= HandleCaseCompleted;
        GameplayEvents.OnCaseFolderStored -= HandleFolderStored;
        GameplayEvents.OnCaseFolderReturned -= HandleFolderReturned;
        if (stateMachine != null) stateMachine.OnStateChanged -= HandleState;
        stateMachine = null;
        UnhookTransfers();
        ReleaseControls();
    }

    private void HandleProfileCollected(string id) => Fire(ObjectiveTriggers.EventKey(ObjectiveTriggers.ProfileCollected));
    private void HandleAllProfiles() => Fire(ObjectiveTriggers.EventKey(ObjectiveTriggers.AllProfilesCollected));
    private void HandleGuideReady() => Fire(ObjectiveTriggers.EventKey(ObjectiveTriggers.TutorialGuideAppeared));
    private void HandleGuideTalked() => Fire(ObjectiveTriggers.EventKey(ObjectiveTriggers.TutorialGuideTalkedTo));
    private void HandleSideQuest() => Fire(ObjectiveTriggers.EventKey(ObjectiveTriggers.SideQuestCompleted));
    private void HandleReceptionist() => Fire(ObjectiveTriggers.EventKey(ObjectiveTriggers.ReceptionistTalkedTo));
    private void HandleFolderOpened() => Fire(ObjectiveTriggers.EventKey(ObjectiveTriggers.CaseFolderOpened));
    private void HandleBookOpened() => Fire(ObjectiveTriggers.EventKey(ObjectiveTriggers.TaxCodeBookOpened));
    private void HandleReturnCollected() => Fire(ObjectiveTriggers.EventKey(ObjectiveTriggers.TaxReturnCollected));
    private void HandleCaseCompleted() => Fire(ObjectiveTriggers.EventKey(ObjectiveTriggers.CaseCompleted));
    private void HandleFolderStored() => Fire(ObjectiveTriggers.EventKey(ObjectiveTriggers.CaseFolderStored));
    private void HandleFolderReturned() => Fire(ObjectiveTriggers.EventKey(ObjectiveTriggers.CaseFolderReturned));
    private void HandleState(IGameState state) { if (state != null) Fire(ObjectiveTriggers.StateKey(state.GetType())); }

    private void HandleSelection(object source)
    {
        if (transfers != null && transfers.HasSelection) Fire(GuideTriggers.ValueSelected);
    }

    private void HandlePlaced(DataValue value) => Fire(GuideTriggers.ValuePlaced);
    private void HandleRejected(DataValue value, string reason) => Fire(GuideTriggers.ValueRejected);

    private void UnhookTransfers()
    {
        if (transfers == null) return;
        transfers.OnSelectionChanged -= HandleSelection;
        transfers.OnTransferSucceeded -= HandlePlaced;
        transfers.OnTransferFailed -= HandleRejected;
        transfers = null;
    }

    /// <summary>Counts an event (public so other systems can report extra guide events).</summary>
    public void Fire(string key)
    {
        if (string.IsNullOrEmpty(key)) return;
        counts.TryGetValue(key, out int n);
        counts[key] = n + 1;
    }

    private int Count(string key) => counts.TryGetValue(key, out int n) ? n : 0;

    // =========================================================
    // LOOP
    // =========================================================

    private void Update()
    {
        float now = Time.unscaledTime;
        if (now >= nextPoll)
        {
            nextPoll = now + PollInterval;
            Poll();
        }

        PlayerControlHold.Refresh();
        TickAside(Time.unscaledTime);
        if (!started) TryStart(now);
        if (skippedAll) return;

        if (lesson == null)
        {
            if (started) PickLesson(now);
            return;
        }

        UpdateLesson(now);
    }

    private void Poll()
    {
        if (stateMachine == null && GameStateMachine.Instance != null)
        {
            stateMachine = GameStateMachine.Instance;
            stateMachine.OnStateChanged += HandleState;
            if (stateMachine.CurrentState != null) HandleState(stateMachine.CurrentState);
        }

        if (transfers != ValueTransferManager.Instance)
        {
            UnhookTransfers();
            transfers = ValueTransferManager.Instance;
            if (transfers != null)
            {
                transfers.OnSelectionChanged += HandleSelection;
                transfers.OnTransferSucceeded += HandlePlaced;
                transfers.OnTransferFailed += HandleRejected;
            }
        }

        bool dialogue = DialogueUI.IsAnyDialogueActive;
        if (dialogue != wasDialogue)
        {
            Fire(dialogue ? GuideTriggers.DialogueStart : GuideTriggers.DialogueEnd);
            wasDialogue = dialogue;
        }

        if (CurrentMode == CameraController.CameraMode.CorkboardSideQuest)
        {
            if (corkboard == null) corkboard = FindAnyObjectByType<CorkboardSideQuestManager>(FindObjectsInactive.Include);
            if (corkboard != null && corkboard.WrongPlacements > lastWrong)
            {
                lastWrong = corkboard.WrongPlacements;
                Fire(GuideTriggers.CorkboardWrong);
            }
            int occupied = 0;
            CorkboardSlot[] slots = FindObjectsByType<CorkboardSlot>(FindObjectsInactive.Exclude);
            for (int i = 0; i < slots.Length; i++) if (slots[i].OccupyingCard != null) occupied++;
            if (occupied > lastOccupied) Fire(GuideTriggers.CorkboardPlaced);
            lastOccupied = occupied;
        }
    }

    private static CameraController.CameraMode? CurrentMode =>
        CameraController.Instance != null ? CameraController.Instance.CurrentMode : (CameraController.CameraMode?)null;

    private void TryStart(float now)
    {
        OfficeHudRuntime hud = OfficeHudRuntime.Instance;
        bool ready = (hud == null || hud.IsShown) && CurrentMode == CameraController.CameraMode.FirstPerson &&
                     !DialogueUI.IsAnyDialogueActive && Calm();
        if (!ready)
        {
            readySince = -1f;
            return;
        }
        if (readySince < 0f) readySince = now;
        if (now - readySince < script.startDelay) return;
        started = true;
        Fire(GuideTriggers.Start);
    }

    /// <summary>No result card, settings or pause in the way.</summary>
    private static bool Calm() => Calm(allowResults: false);

    private static bool Calm(bool allowResults)
    {
        if ((HudRewardScreen.AnyOpen && !allowResults) || Time.timeScale <= 0f) return false;
        if (SideQuestSplash.IsShowing) return false;
        if (!allowResults && CaseResultPopupUI.Instance != null && CaseResultPopupUI.Instance.IsVisible) return false;
        if (GameStateMachine.Instance != null && GameStateMachine.Instance.IsPaused) return false;
        return true;
    }

    private void PickLesson(float now)
    {
        GuideLesson[] lessons = script.lessons;
        // While a card lesson waits out its delay, the card's auto-continue waits too.
        bool cardLessonPending = false;
        for (int i = 0; i < lessons.Length; i++)
        {
            GuideLesson candidate = lessons[i];
            if (finished.Contains(candidate.id)) continue;
            if (!string.IsNullOrEmpty(candidate.after) && !finished.Contains(candidate.after)) continue;
            if (!WhenSatisfied(candidate))
            {
                pendingSince.Remove(candidate);
                continue;
            }
            if (!CanShowNow(candidate)) continue;

            if (!pendingSince.TryGetValue(candidate, out float since))
            {
                pendingSince[candidate] = now;
                since = now;
            }
            if (now - since < candidate.delay)
            {
                if (candidate.duringResults) cardLessonPending = true;
                continue;
            }

            pendingSince.Remove(candidate);
            BeginLesson(candidate);
            return;
        }
        HudRewardScreen.HoldAutoContinue = cardLessonPending;
    }

    private bool CanShowNow(GuideLesson candidate)
    {
        if (!Calm(candidate.duringResults)) return false;
        if (DialogueUI.IsAnyDialogueActive && !candidate.duringDialogue) return false;
        if (!string.IsNullOrEmpty(candidate.onlyIn) && !ConditionsMet(candidate.onlyIn)) return false;
        return true;
    }

    private bool WhenSatisfied(GuideLesson candidate)
    {
        if (!whenCache.TryGetValue(candidate, out List<GuideTriggers.Trigger> list))
        {
            list = GuideTriggers.ParseAll(candidate.when);
            whenCache[candidate] = list;
        }
        for (int i = 0; i < list.Count; i++)
        {
            GuideTriggers.Trigger t = list[i];
            if (t.Kind == GuideTriggers.Kind.Event ? Count(t.Key) >= t.Count : Condition(t)) return true;
        }
        return false;
    }

    private bool ConditionsMet(string text)
    {
        List<GuideTriggers.Trigger> list = GuideTriggers.ParseAll(text);
        for (int i = 0; i < list.Count; i++)
        {
            GuideTriggers.Trigger t = list[i];
            if (t.Kind == GuideTriggers.Kind.Event ? Count(t.Key) >= t.Count : Condition(t)) return true;
        }
        return list.Count == 0;
    }

    private bool Condition(GuideTriggers.Trigger t) => RawCondition(t) != t.Negate;

    private bool RawCondition(GuideTriggers.Trigger t)
    {
        switch (t.Key)
        {
            case GuideTriggers.ModePrefix:
                return CurrentMode.HasValue && CurrentMode.Value.ToString() == t.Arg;
            case GuideTriggers.StepPrefix:
            {
                OfficeObjectives objectives = OfficeObjectives.Instance;
                if (objectives == null || objectives.Snapshot == null || objectives.Snapshot.ActiveIndex < 0) return false;
                TaskDefinition active = objectives.Tasks[objectives.Snapshot.ActiveIndex];
                return ObjectiveTriggers.Normalize(active.TriggerKey) == t.Arg;
            }
            case GuideTriggers.NearPrefix:
            {
                Transform player = GuideTargets.Player;
                if (player == null) return false;
                GuideTargets.Hit hit = targets.ResolveNearPlayer(t.Arg);
                if (!hit.Found || !hit.HasWorld) return false;
                Vector3 d = hit.World - player.position;
                d.y = 0f;
                return d.magnitude <= t.Number;
            }
            case GuideTriggers.VisiblePrefix:
                return targets.Resolve(t.Arg).OnScreen;
            case GuideTriggers.ProfilesPrefix:
            {
                TaxpayerProfileCollectionHUD profiles = TaxpayerProfileCollectionHUD.Instance;
                return profiles != null && profiles.CollectedProfiles.Count >= t.Number;
            }
            case GuideTriggers.OpenPrefix:
                return WindowOpen(t.Arg);
            case GuideTriggers.ActivePrefix:
                return targets.Resolve(t.Arg).Found;
            case GuideTriggers.CardPrefix:
                return CardOpen(t.Arg);
            case GuideTriggers.DialogueActive:
                return DialogueUI.IsAnyDialogueActive;
            case GuideTriggers.CaseFilled:
            {
                CaseData data = CaseManager.Instance != null ? CaseManager.Instance.CurrentCase : null;
                return data != null && data.residencyStatus.HasValue && data.taxpayerType.HasValue && data.incomeSource.HasValue;
            }
        }
        return false;
    }

    private NotesPanelUI notesPanel;
    private HudProfileCards profileCards;
    private CaseFolder3DView folderView;

    private static bool CardOpen(string kind)
    {
        HudRewardScreen top = HudRewardScreen.Top;
        if (top == null || !top.IsRevealed) return false;
        string title = (top.Title ?? "").ToLowerInvariant();
        switch (kind)
        {
            case "any": return true;
            case "audit": return title.Contains("audit");
            case "sidequest": return title.Contains("side quest");
            case "level": return title.Contains("level complete");
            case "result": return title.Contains("result");
        }
        return false;
    }

    private bool WindowOpen(string window)
    {
        switch (window)
        {
            case "notes":
                if (notesPanel == null) notesPanel = FindAnyObjectByType<NotesPanelUI>(FindObjectsInactive.Include);
                return notesPanel != null && notesPanel.IsOpen;
            case "profiles":
                if (profileCards == null) profileCards = FindAnyObjectByType<HudProfileCards>(FindObjectsInactive.Include);
                return profileCards != null && profileCards.IsShowingCards;
            case "folder":
                if (folderView == null) folderView = FindAnyObjectByType<CaseFolder3DView>(FindObjectsInactive.Include);
                return folderView != null && folderView.IsOpen;
        }
        return false;
    }

    // =========================================================
    // LESSONS AND PAGES
    // =========================================================

    private void BeginLesson(GuideLesson next)
    {
        lesson = next;
        suspended = false;
        pageIndex = -1;
        NextPage();
    }

    private void NextPage()
    {
        advanceAt = -1f;
        pageIndex++;
        while (lesson != null && pageIndex < lesson.pages.Length && !PageFitsPlatform(lesson.pages[pageIndex]))
            pageIndex++;

        if (lesson == null || pageIndex >= lesson.pages.Length)
        {
            EndLesson();
            return;
        }

        page = lesson.pages[pageIndex];
        pageWait = page.WaitsForTap && !page.IsHint ? null : GuideTriggers.ParseAll(
            page.IsHint && string.IsNullOrWhiteSpace(page.waitFor) ? "seconds:5" : page.waitFor);
        pageCounts = new Dictionary<string, int>(counts);
        pageStart = Time.unscaledTime;
        pageStartMode = CurrentMode;
        moved = looked = 0f;
        hasLastPose = false;
        ShowPage();
    }

    private bool PageFitsPlatform(GuidePage p)
    {
        if (string.IsNullOrEmpty(p.onlyOn)) return true;
        return string.Equals(p.onlyOn, touch ? "touch" : "desktop", StringComparison.OrdinalIgnoreCase);
    }

    private void ShowPage()
    {
        if (page.revealArrows) RevealArrows();
        collapsed = false;
        typedDoneAt = -1f;
        Sprite portrait = Portrait(page.mood);
        view.SetPortrait(portrait, true);

        if (page.IsWelcomeCard)
        {
            view.HideBox();
            view.HideHint();
            view.Hand.Hide();
            view.SetHole(false, default);
            SetBlocking(true, dim: false);
            welcomeCard = GuideWelcomeCard.Show(canvasRoot, script.welcome, WelcomeObjectives(),
                Portrait("default") ?? portrait, script.guide.name, script.guide.role, script.trail.ParsedColor,
                () =>
                {
                    welcomeCard = null;
                    if (page != null && page.IsWelcomeCard) NextPage();
                });
            return;
        }

        string text = Format(page.TextFor(touch));
        if (page.IsHint)
        {
            view.HideBox();
            view.ShowHint(text);
            SetBlocking(false, dim: false);
        }
        else
        {
            if (!asideActive) view.HideHint();
            string chip = !string.IsNullOrEmpty(page.title) ? page.title : lesson.title;
            bool tap = page.WaitsForTap;
            SetBlocking(tap, dim: tap);
            // Resolve the spotlight before placing the box ("auto" avoids it).
            UpdatePointer(true);
            view.ShowBox(chip, text, tap, ParsePlacement(page.box));
        }
        UpdatePointer(true);
    }

    private void UpdateLesson(float now)
    {
        // Pause (hide) while the lesson's context is gone, e.g. the player left the desk.
        bool inContext = string.IsNullOrEmpty(lesson.onlyIn) || ConditionsMet(lesson.onlyIn);
        bool allowed = Calm(lesson.duringResults) && (lesson.duringDialogue || !DialogueUI.IsAnyDialogueActive) && inContext;
        if (welcomeCard != null) allowed = true;
        // A card that explains itself keeps its auto-continue waiting while the guide talks.
        HudRewardScreen.HoldAutoContinue = allowed && lesson.duringResults;
        if (!allowed && lesson.endIfGone && !inContext)
        {
            EndLesson();
            return;
        }
        if (!allowed)
        {
            if (!suspended) Suspend();
            // The goal can happen while hidden (e.g. "start a conversation"):
            // finish quietly instead of flashing the page back afterwards.
            if (pageWait != null && advanceAt < 0f && WaitSatisfied(now))
            {
                if (pageIndex >= lesson.pages.Length - 1) EndLesson();
                else advanceAt = now;
            }
            return;
        }
        if (suspended)
        {
            suspended = false;
            if (advanceAt < 0f) ShowPage();
        }

        TrackMotion();
        UpdatePointer(false);
        UpdateCollapse(now);

        if (IsBlocking && welcomeCard == null && Keyboard.current != null &&
            (Keyboard.current.spaceKey.wasPressedThisFrame || Keyboard.current.enterKey.wasPressedThisFrame))
            Tap();

        if (advanceAt > 0f)
        {
            if (now >= advanceAt) NextPage();
            return;
        }

        if (pageWait != null && WaitSatisfied(now))
        {
            // A short beat so the player sees their action land.
            advanceAt = now + 0.45f;
            view.Hand.Hide();
        }
    }

    /// <summary>
    /// A short line from Arthur in his top bubble (with portrait), e.g. his
    /// reaction to a corkboard placement. Works even after the tutorial was
    /// skipped: it's gameplay feedback, not a lesson.
    /// </summary>
    private bool Say(string text, string mood, float seconds)
    {
        if (string.IsNullOrWhiteSpace(text)) return false;
        view.SetPortrait(Portrait(string.IsNullOrEmpty(mood) ? "default" : mood), true);
        view.ShowHint(Format(text));
        asideActive = true;
        asideUntil = Time.unscaledTime + Mathf.Max(1.5f, seconds);
        return true;
    }

    private void TickAside(float now)
    {
        if (!asideActive || now < asideUntil) return;
        asideActive = false;
        // Hand the bubble back to a page that uses it, otherwise hide it.
        if (page != null && !suspended && (page.IsHint || collapsed))
        {
            view.SetPortrait(Portrait(page.mood), false);
            view.ShowHint(Format(page.TextFor(touch)), instant: true);
        }
        else
        {
            view.HideHint();
        }
    }

    /// <summary>
    /// "Your turn" pages step out of the way: once the text has been read
    /// (or the player starts doing it, or opens a window) the speech box
    /// folds into the small bubble at the top, so Notes, Profiles, the Case
    /// Folder and the world stay free to use. The hand and frame remain.
    /// </summary>
    private void UpdateCollapse(float now)
    {
        if (collapsed || page == null || page.IsHint || page.IsWelcomeCard || page.WaitsForTap ||
            welcomeCard != null || advanceAt > 0f || asideActive)
            return;

        if (view.IsTyping) typedDoneAt = -1f;
        else if (typedDoneAt < 0f) typedDoneAt = now;

        bool acting = moved > 0.4f || looked > 10f || CurrentMode != pageStartMode ||
                      WindowOpen("notes") || WindowOpen("profiles") || WindowOpen("folder");
        bool read = typedDoneAt > 0f && now - typedDoneAt > 3f;
        if (!acting && !read) return;

        collapsed = true;
        view.HideBox();
        view.ShowHint(Format(page.TextFor(touch)), instant: true);
    }

    private void Suspend()
    {
        suspended = true;
        view.HideBox();
        view.HideHint();
        view.Hand.Hide();
        view.SetHole(false, default);
        SetBlocking(false, dim: false);
    }

    private bool WaitSatisfied(float now)
    {
        for (int i = 0; i < pageWait.Count; i++)
        {
            GuideTriggers.Trigger t = pageWait[i];
            switch (t.Kind)
            {
                case GuideTriggers.Kind.Event:
                    pageCounts.TryGetValue(t.Key, out int before);
                    if (Count(t.Key) - before >= t.Count) return true;
                    break;
                case GuideTriggers.Kind.Condition:
                    if (Condition(t)) return true;
                    break;
                case GuideTriggers.Kind.Move:
                    if (moved >= t.Number) return true;
                    break;
                case GuideTriggers.Kind.Look:
                    if (looked >= t.Number) return true;
                    break;
                case GuideTriggers.Kind.Seconds:
                    if (now - pageStart >= t.Number) return true;
                    break;
            }
        }
        return false;
    }

    private void TrackMotion()
    {
        CameraController controller = CameraController.Instance;
        Transform player = GuideTargets.Player;
        if (controller == null || player == null) return;
        Vector3 pos = player.position;
        Quaternion rot = player.rotation;
        if (controller.TryGetFirstPersonView(out _, out Quaternion view3)) rot = view3;
        Camera cam = GuideTargets.ViewCamera;
        if (cam != null && controller.CurrentMode == CameraController.CameraMode.FirstPerson) rot = cam.transform.rotation;
        float yaw = rot.eulerAngles.y;
        if (hasLastPose)
        {
            Vector3 step = pos - lastPlayerPos;
            step.y = 0f;
            if (step.magnitude < 3f) moved += step.magnitude;
            looked += Mathf.Abs(Mathf.DeltaAngle(lastYaw, yaw));
        }
        lastPlayerPos = pos;
        lastYaw = yaw;
        hasLastPose = true;
    }

    private void UpdatePointer(bool fresh)
    {
        if (page == null || page.IsWelcomeCard || suspended) return;

        GuideTargets.Hit target = targets.Resolve(page.point);
        bool show = target.Found && target.OnScreen;
        if (page.IsHint) show = false;
        view.SetHole(show, target.ScreenRect);

        GuideHand.Gesture gesture = ParseGesture(page.gesture, page.point);
        if (!show || gesture == GuideHand.Gesture.None || advanceAt > 0f)
        {
            view.Hand.Hide();
            return;
        }

        Rect from = view.ScreenToLocal(target.ScreenRect);
        Rect to = from;
        if (gesture == GuideHand.Gesture.Drag)
        {
            GuideTargets.Hit dest = targets.Resolve(page.to);
            if (!dest.Found || !dest.OnScreen)
            {
                view.Hand.Hide();
                return;
            }
            to = view.ScreenToLocal(dest.ScreenRect);
        }

        if (fresh || !view.Hand.IsVisible) view.Hand.Show(gesture, from, to);
        else view.Hand.Track(from, to);
    }

    private static GuideHand.Gesture ParseGesture(string gesture, string point)
    {
        if (string.IsNullOrEmpty(point)) return GuideHand.Gesture.None;
        switch ((gesture ?? "tap").Trim().ToLowerInvariant())
        {
            case "none": return GuideHand.Gesture.None;
            case "drag": return GuideHand.Gesture.Drag;
            case "swipe": return GuideHand.Gesture.Swipe;
            case "joystick": return GuideHand.Gesture.Joystick;
            case "hold": return GuideHand.Gesture.Hold;
            default: return GuideHand.Gesture.Tap;
        }
    }

    private static GuideCoachView.Placement ParsePlacement(string box)
    {
        switch ((box ?? "auto").Trim().ToLowerInvariant())
        {
            case "top": return GuideCoachView.Placement.Top;
            case "bottom": return GuideCoachView.Placement.Bottom;
            case "center": return GuideCoachView.Placement.Center;
            default: return GuideCoachView.Placement.Auto;
        }
    }

    private void Tap()
    {
        if (page == null || suspended || welcomeCard != null) return;
        if (view.IsTyping)
        {
            view.CompleteTyping();
            return;
        }
        if (page.WaitsForTap && !page.IsHint && advanceAt < 0f) NextPage();
    }

    private void EndLesson()
    {
        if (lesson != null)
        {
            finished.Add(lesson.id);
            Fire(GuideTriggers.LessonKey(lesson.id));
            SaveFinished();
        }
        lesson = null;
        page = null;
        HudRewardScreen.HoldAutoContinue = false;
        view.HideBox();
        view.HideHint();
        view.Hand.Hide();
        view.SetHole(false, default);
        SetBlocking(false, dim: false);
    }

    /// <summary>Ends the whole tutorial for this run (the objective arrows stay).</summary>
    public void SkipAll()
    {
        RevealArrows();
        for (int i = 0; i < script.lessons.Length; i++) finished.Add(script.lessons[i].id);
        SaveFinished();
        if (welcomeCard != null) welcomeCard.Close();
        lesson = null;
        page = null;
        skippedAll = true;
        Suspend();
        suspended = false;
    }

    // =========================================================
    // HOLDING THE PLAYER STILL
    // =========================================================

    private void SetBlocking(bool blocking, bool dim)
    {
        IsBlocking = blocking;
        view.SetBlocking(blocking && dim);
        if (blocking) HoldControls();
        else ReleaseControls();
    }

    // The shared PlayerControlHold pauses walking/looking/interaction and
    // freezes the body (so it cannot slide); it waits for conversations.
    private void HoldControls() => PlayerControlHold.Acquire(this);

    private void ReleaseControls() => PlayerControlHold.Release(this);

    // =========================================================
    // CONTENT HELPERS
    // =========================================================

    private Sprite Portrait(string mood)
    {
        // Someone else speaking in the bubble (e.g. the client): a portrait id.
        if (mood != null && mood.StartsWith("portrait:", StringComparison.Ordinal))
        {
            string id = mood.Substring("portrait:".Length);
            Sprite other = DialoguePortraitDatabase.Instance != null ? DialoguePortraitDatabase.Instance.GetPortrait(id) : null;
            if (other != null) return other;
            mood = "default";
        }
        string file = script.PortraitFile(mood);
        if (string.IsNullOrEmpty(file)) return null;
        if (portraits.TryGetValue(file, out Sprite cached)) return cached;
        string folder = string.IsNullOrEmpty(script.guide.portraitFolder) ? "" : script.guide.portraitFolder.TrimEnd('/') + "/";
        Sprite sprite = Resources.Load<Sprite>(folder + file);
        if (sprite == null)
        {
            Texture2D tex = Resources.Load<Texture2D>(folder + file);
            if (tex != null)
                sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
        }
        if (sprite == null && DialoguePortraitDatabase.Instance != null)
            sprite = DialoguePortraitDatabase.Instance.GetPortrait(file);
        if (sprite == null) Debug.LogWarning($"[Guide] Portrait \"{folder}{file}\" not found in Resources.");
        portraits[file] = sprite;
        return sprite;
    }

    private string Format(string text)
    {
        if (string.IsNullOrEmpty(text)) return string.Empty;
        if (text.Contains("{guide}")) text = text.Replace("{guide}", script.guide.name);
        if (text.Contains("{objective}"))
        {
            OfficeObjectives objectives = OfficeObjectives.Instance;
            string current = objectives != null && objectives.Snapshot != null && objectives.Snapshot.ActiveIndex >= 0
                ? objectives.Tasks[objectives.Snapshot.ActiveIndex].TaskDescription : "";
            text = text.Replace("{objective}", current);
        }
        return text;
    }

    private List<GuideWelcomeCard.ObjectiveLine> WelcomeObjectives()
    {
        var lines = new List<GuideWelcomeCard.ObjectiveLine>();
        OfficeObjectives objectives = OfficeObjectives.Instance;
        IReadOnlyList<TaskDefinition> tasks = objectives != null ? objectives.Tasks : TaskListProvider.LoadObjectives(levelId);
        for (int i = 0; i < tasks.Count; i++)
        {
            if (!tasks[i].IsHeader) continue;
            int steps = 0;
            string first = null;
            for (int j = i + 1; j < tasks.Count && !tasks[j].IsHeader; j++)
            {
                steps++;
                if (first == null) first = tasks[j].TaskDescription;
            }
            lines.Add(new GuideWelcomeCard.ObjectiveLine
            {
                Title = tasks[i].TaskDescription,
                Detail = steps > 0 ? $"{steps} step{(steps == 1 ? "" : "s")}  ·  First: {first}" : ""
            });
        }
        return lines;
    }

    private string SaveKey => $"TaxVenture.Guide.{levelId}.finished";

    private void LoadFinished()
    {
        if (script.rememberFinishedLessons)
        {
            string saved = PlayerPrefs.GetString(SaveKey, "");
            foreach (string id in saved.Split(','))
                if (!string.IsNullOrEmpty(id)) finished.Add(id);
        }

        // The arrows wait for the guide to introduce them; if no unfinished
        // lesson will ever do that, show them right away.
        bool willReveal = false;
        for (int i = 0; i < script.lessons.Length && !willReveal; i++)
        {
            if (finished.Contains(script.lessons[i].id)) continue;
            foreach (GuidePage p in script.lessons[i].pages)
                if (p.revealArrows) willReveal = true;
        }
        if (!willReveal) RevealArrows();
    }

    private static void RevealArrows()
    {
        if (ObjectiveGuide.Instance != null) ObjectiveGuide.Instance.Reveal();
    }

    private void SaveFinished()
    {
        if (!script.rememberFinishedLessons) return;
        PlayerPrefs.SetString(SaveKey, string.Join(",", finished));
        PlayerPrefs.Save();
    }
}
