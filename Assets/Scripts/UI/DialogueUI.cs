using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Shared conversation admission, presentation and completion/cancellation.
///
/// T02 lifecycle: one admitted session at a time, owner-scoped cancellation,
/// once-only terminal callbacks.
///
/// T03 presentation:
/// - Progressive TMP reveal over VISIBLE characters (rich-text tags are never
///   typed), with sentence/clause pauses.
/// - Box taps, the Continue button, submit and AdvanceDialogue() all go through
///   one DialogueAdvanceGate: a gesture while typing reveals only the current
///   line; a distinct later gesture advances or, on the last line, closes. The
///   opening interaction and duplicate/near-simultaneous events are rejected.
/// - An optional IDialoguePresentation (DialogueCameraDirector) frames shots.
///   On end or cancel the panel hides, the presentation blends back to first
///   person, and ONLY THEN are controls released and the completion or
///   cancellation callback run (DialogueTerminalHandoff). While that return
///   is running the conversation still counts as active, so no other
///   conversation can start.
///
/// T22: a line tagged with an emote shows it over its speaker from the moment
/// the line starts typing until the line ends (DialogueEmotes); untagged lines
/// show none.
///
/// T08: a line may AWAIT AN ACTION (DialogueLine.AwaitAction). Once it is
/// revealed, taps, the Continue button and submit no longer advance it; the
/// Continue button is replaced by a prompt ("Drag the client copy to
/// Marie"). The owner completes it with CompleteAwaitedAction(id), and the
/// conversation then moves on by itself. Ordinary lines behave as before.
/// </summary>
public class DialogueUI : MonoBehaviour
{
    [Header("Panel")]
    [SerializeField] private GameObject panelRoot;
    [Header("Portrait (left 25%)")]
    [SerializeField] private Image portraitImage;
    [SerializeField] private Sprite defaultPortraitPlaceholder;
    [Header("Dialogue Text (right 75%)")]
    [SerializeField] private TextMeshProUGUI speakerNameText;
    [SerializeField] private TextMeshProUGUI dialogueText;
    [Header("Continue (bottom-right)")]
    [SerializeField] private Button continueButton;

    [Header("Typewriter (T03)")]
    [SerializeField] private bool useTypewriter = true;
    [SerializeField, Range(5f, 120f)] private float charactersPerSecond = 40f;
    [SerializeField, Range(0f, 1f)] private float sentencePause = 0.22f;
    [SerializeField, Range(0f, 0.5f)] private float clausePause = 0.08f;

    [Header("Backdrop (T03)")]
    [Tooltip("Adds a rectangle behind the dialogue box so it stands out from the scene. The box, fonts and colours are unchanged.")]
    [SerializeField] private bool showBackdrop = true;
    [Tooltip("Backdrop colour. White frames the box; lower alpha makes it more transparent.")]
    [SerializeField] private Color backdropColor = new Color(1f, 1f, 1f, 0.85f);
    [Tooltip("How far the backdrop extends beyond the box on every side (canvas units).")]
    [SerializeField, Range(0f, 60f)] private float backdropPadding = 10f;
    [SerializeField] private bool backdropShadow = true;

    [Header("Gestures (T03)")]
    [Tooltip("Seconds after a conversation opens during which taps are ignored, so the opening interaction cannot skip the first line.")]
    [SerializeField, Range(0f, 0.6f)] private float openingGrace = 0.15f;
    [Tooltip("Minimum seconds between two accepted gestures (tap + button, repeated submit).")]
    [SerializeField, Range(0.02f, 0.5f)] private float minimumGestureInterval = 0.12f;

    private const int AllCharactersVisible = 99999;

    private readonly DialogueSession session = new DialogueSession();
    private readonly DialogueAdvanceGate gate = new DialogueAdvanceGate();
    private readonly DialogueRevealSchedule reveal = new DialogueRevealSchedule();
    private readonly DialogueTerminalHandoff handoff = new DialogueTerminalHandoff();
    private static DialogueUI activeUI;
    private UnityEngine.Object sessionOwner;
    private bool hasSessionOwner;
    private bool initialized;
    private bool hidingSelf;
    private Action releaseControls;
    private int sessionVersion;
    private IDialoguePresentation presentation;
    private IDialoguePresentation sessionPresentation;
    private IDialoguePresentation returningPresentation;
    private char[] revealBuffer = new char[128];
    private RectTransform backdrop;
    private Image backdropImage;
    private readonly Vector3[] boxCorners = new Vector3[4];
    private RectTransform actionPrompt;
    private TextMeshProUGUI actionPromptText;

    /// <summary>True from admission until controls are released after the return.</summary>
    public bool IsDialogueActive => session.IsActive || handoff.IsPending;
    public static bool IsAnyDialogueActive => activeUI != null && activeUI.IsDialogueActive;
    /// <summary>The dialogue box running the current conversation, or null.</summary>
    public static DialogueUI Active => activeUI;

    /// <summary>This dialogue box is still on screen (it can outlive the session for a moment while it closes).</summary>
    public bool IsPanelShowing => panelRoot != null && panelRoot.activeInHierarchy;
    public bool CanStartDialogue => !IsAnyDialogueActive && CanShowPanel() &&
        (CameraController.Instance == null || CameraController.Instance.CanStartDialogue) &&
        (GameStateMachine.Instance == null || !GameStateMachine.Instance.IsPaused);
    public DialogueLine CurrentLine => session.CurrentLine;
    public UnityEngine.Object CurrentOwner => sessionOwner;
    public int CurrentLineIndex => session.LineIndex;
    /// <summary>True while the current line is still being typed out.</summary>
    public bool IsLineTyping => session.IsActive && reveal.IsTyping;
    /// <summary>True after the last line or a cancel, while the view returns to first person.</summary>
    public bool IsReturning => handoff.IsPending;
    public event Action DialogueStarted;
    public event Action<DialogueLine> LineChanged;
    /// <summary>Raised when the current line is fully visible (typed out or revealed by a tap).</summary>
    public event Action<DialogueLine> LineRevealed;
    public event Action<DialogueEndReason> DialogueEnded;

    /// <summary>T08: the current line waits for an action (taps do not advance it).</summary>
    public bool IsAwaitingAction => session.IsAwaitingAction;
    /// <summary>T08: the action the current line waits for, or null.</summary>
    public string AwaitingActionId => session.AwaitingActionId;
    /// <summary>T08: the action the active conversation waits for, or null (for HUD owners).</summary>
    public static string ActiveAwaitingAction => activeUI != null && activeUI.session.IsActive ? activeUI.session.AwaitingActionId : null;

    private void Awake() => Initialize();

    private void Initialize()
    {
        if (initialized) return;
        initialized = true;
        if (continueButton != null) continueButton.onClick.AddListener(OnContinuePressed);
        EnsureBoxTapRelay();
        SyncBackdrop();
        HideSelf();
    }

    // The original scene puts this component on panelRoot itself, which is
    // inactive between conversations. A direct start must be allowed to show it.
    private bool CanShowPanel()
    {
        if (!enabled || panelRoot == null || continueButton == null || dialogueText == null) return false;
        return gameObject.activeInHierarchy || (panelRoot == gameObject &&
            (transform.parent == null || transform.parent.gameObject.activeInHierarchy));
    }

    // ------------------------------------------------------------------
    // Presentation registration (DialogueCameraDirector)
    // ------------------------------------------------------------------

    public void RegisterPresentation(IDialoguePresentation value)
    {
        if (value != null) presentation = value;
    }

    public void UnregisterPresentation(IDialoguePresentation value)
    {
        if (ReferenceEquals(presentation, value)) presentation = null;
    }

    // ------------------------------------------------------------------
    // Admission
    // ------------------------------------------------------------------

    public bool StartDialogue(List<DialogueLine> lines, Action onConcludedCallback,
        Action onCancelled = null, UnityEngine.Object owner = null)
        => TryStartDialogue(lines, onConcludedCallback, onCancelled, owner);

    public bool TryStartDialogue(List<DialogueLine> lines, Action onCompleted,
        Action onCancelled = null, UnityEngine.Object owner = null)
    {
        if (!CanStartDialogue) return false;
        Initialize();
        if (!CanShowPanel() || !session.TryStart(lines, onCompleted, onCancelled)) return false;
        activeUI = this;
        sessionOwner = owner;
        hasSessionOwner = !ReferenceEquals(owner, null);
        int startingVersion = ++sessionVersion;
        gate.OpeningGrace = openingGrace;
        gate.MinimumInterval = minimumGestureInterval;
        gate.Open(Time.frameCount, Time.unscaledTime);
        try
        {
            if (CameraController.Instance != null)
                releaseControls = CameraController.Instance.AcquireDialogueControls();

            sessionPresentation = IsAlive(presentation) ? presentation : null;
            if (sessionPresentation != null)
            {
                try { sessionPresentation.BeginConversation(owner); }
                catch (Exception exception)
                {
                    // A broken presentation must never block the conversation itself.
                    Debug.LogException(exception, this);
                    SafeAbort(sessionPresentation);
                    sessionPresentation = null;
                }
            }
            if (!session.IsActive || sessionVersion != startingVersion) return false;

            SyncBackdrop();
            if (!panelRoot.activeSelf) panelRoot.SetActive(true);
            if (!session.IsActive || sessionVersion != startingVersion) return false;
            RenderCurrentLine();
            if (!session.IsActive || sessionVersion != startingVersion) return false;
            DialogueStarted?.Invoke();
            return session.IsActive && sessionVersion == startingVersion;
        }
        catch (Exception exception)
        {
            Debug.LogException(exception, this);
            if (sessionVersion == startingVersion) Finish(DialogueEndReason.Cancelled);
            return false;
        }
    }

    private void Update()
    {
        if (session.IsActive && hasSessionOwner && sessionOwner == null)
        {
            CancelDialogue();
            return;
        }

        if (session.IsActive && reveal.IsTyping)
        {
            int visible = reveal.Tick(Time.unscaledDeltaTime);
            if (dialogueText != null) dialogueText.maxVisibleCharacters = visible;
            if (!reveal.IsTyping) OnLineFullyRevealed();
        }
    }

    private void RenderCurrentLine()
    {
        DialogueLine line = session.CurrentLine;
        if (line == null) return;
        ShowActionPrompt(null);
        if (speakerNameText != null)
            speakerNameText.text = line.Speaker == DialogueSpeaker.Player ? "You" : line.SpeakerName;
        dialogueText.text = line.Text;
        if (portraitImage != null)
        {
            Sprite portrait = DialoguePortraitDatabase.Instance != null
                ? DialoguePortraitDatabase.Instance.GetPortrait(line.PortraitId) : null;
            portraitImage.sprite = portrait != null ? portrait : defaultPortraitPlaceholder;
        }

        BeginReveal();

        if (IsAlive(sessionPresentation))
        {
            try { sessionPresentation.ShowLine(line, session.LineIndex); }
            catch (Exception exception) { Debug.LogException(exception, this); }
        }

        // T22: a tagged line's emote starts with its typing and lasts for the line.
        try { DialogueEmotes.Play(line, sessionOwner, BoxTopScreenY()); }
        catch (Exception exception) { Debug.LogException(exception, this); }
        LineChanged?.Invoke(line);
    }

    // ------------------------------------------------------------------
    // Typewriter
    // ------------------------------------------------------------------

    private void BeginReveal()
    {
        if (!useTypewriter || dialogueText == null)
        {
            reveal.Clear();
            if (dialogueText != null) dialogueText.maxVisibleCharacters = AllCharactersVisible;
            return;
        }

        // TMP parses rich text; characterInfo holds only what is rendered, so
        // tags are never typed and maxVisibleCharacters maps 1:1 to the schedule.
        dialogueText.maxVisibleCharacters = 0;
        dialogueText.ForceMeshUpdate();
        TMP_TextInfo info = dialogueText.textInfo;
        int count = info != null ? info.characterCount : 0;
        if (revealBuffer.Length < count) revealBuffer = new char[Math.Max(count, revealBuffer.Length * 2)];
        for (int i = 0; i < count; i++) revealBuffer[i] = info.characterInfo[i].character;
        reveal.Begin(revealBuffer, count, charactersPerSecond, sentencePause, clausePause);
        if (!reveal.IsTyping) OnLineFullyRevealed();
    }

    private void OnLineFullyRevealed()
    {
        if (dialogueText != null) dialogueText.maxVisibleCharacters = AllCharactersVisible;
        DialogueLine line = session.CurrentLine;
        if (line != null && session.IsAwaitingAction) ShowActionPrompt(line.ActionPrompt);
        if (line != null) LineRevealed?.Invoke(line);
    }

    // ------------------------------------------------------------------
    // Gestures: every route converges here
    // ------------------------------------------------------------------

    private void OnContinuePressed() => HandleGesture();

    /// <summary>Called by DialogueBoxTapRelay for a tap on the dialogue box.</summary>
    public void HandleBoxTap() => HandleGesture();

    /// <summary>
    /// One advance gesture. While the line is typing it reveals the line; otherwise
    /// it advances, or ends the conversation on the last line.
    /// </summary>
    public void AdvanceDialogue() => HandleGesture();

    private void HandleGesture()
    {
        if (!session.IsActive) return;
        // T08: a revealed line that waits for an action ignores taps (only the action moves it on).
        if (session.IsAwaitingAction && !reveal.IsTyping) return;
        DialogueGesture gesture = gate.Evaluate(Time.frameCount, Time.unscaledTime, reveal.IsTyping);
        if (gesture == DialogueGesture.RevealLine || gesture == DialogueGesture.Advance)
            AudioManager.Instance?.QueueInteractionSFX("continue");

        switch (gesture)
        {
            case DialogueGesture.RevealLine:
                reveal.RevealAll();
                OnLineFullyRevealed();
                break;
            case DialogueGesture.Advance:
                if (!session.TryAdvance(out bool completed)) return;
                if (completed) Finish(DialogueEndReason.Completed);
                else RenderCurrentLine();
                break;
        }
    }

    // ------------------------------------------------------------------
    // Awaited actions (T08)
    // ------------------------------------------------------------------

    /// <summary>
    /// The owner reports the awaited action done: the line is complete and the
    /// conversation moves on to the next line (or ends on the last one).
    /// False when the current line does not wait for this action.
    /// </summary>
    public bool CompleteAwaitedAction(string actionId)
    {
        if (!session.IsActive) return false;
        if (reveal.IsTyping)
        {
            reveal.RevealAll();
            OnLineFullyRevealed();
        }
        if (!session.TryCompleteAction(actionId)) return false;
        ShowActionPrompt(null);
        if (!session.TryAdvance(out bool completed)) return false;
        if (completed) Finish(DialogueEndReason.Completed);
        else RenderCurrentLine();
        return true;
    }

    /// <summary>Replaces the waiting line's prompt (e.g. a retry message after a failed save); null restores the line's own.</summary>
    public void SetActionPrompt(string text)
    {
        if (!session.IsAwaitingAction || reveal.IsTyping) return;
        ShowActionPrompt(text ?? session.CurrentLine.ActionPrompt);
    }

    /// <summary>The prompt pill above the box while a line waits; the Continue button comes back with ordinary lines.</summary>
    private void ShowActionPrompt(string text)
    {
        bool show = text != null;
        if (continueButton != null) continueButton.gameObject.SetActive(!show);
        if (!show)
        {
            if (actionPrompt != null) actionPrompt.gameObject.SetActive(false);
            return;
        }
        RectTransform box = dialogueText != null ? dialogueText.transform.parent as RectTransform : null;
        if (box == null) return;
        if (actionPrompt == null)
        {
            actionPrompt = HudTheme.CreatePanel("Dialogue Action Prompt", box, 22f).parent as RectTransform;
            actionPrompt.anchorMin = actionPrompt.anchorMax = new Vector2(0.5f, 1f);
            actionPrompt.pivot = new Vector2(0.5f, 0f);
            actionPrompt.anchoredPosition = new Vector2(0f, 14f);
            actionPrompt.sizeDelta = new Vector2(620f, 64f);
            actionPromptText = HudTheme.CreateLabel("Text", actionPrompt.GetChild(0), "", 30f, HudTheme.TextPrimary,
                TextAlignmentOptions.Center, FontStyles.Bold);
            HudTheme.Stretch(actionPromptText.rectTransform, 10f);
            foreach (Graphic graphic in actionPrompt.GetComponentsInChildren<Graphic>(true)) graphic.raycastTarget = false;
        }
        actionPromptText.text = text;
        actionPrompt.gameObject.SetActive(true);
    }

    // ------------------------------------------------------------------
    // Termination: hide → presentation return → release → callback
    // ------------------------------------------------------------------

    public void CancelDialogue() => Finish(DialogueEndReason.Cancelled);

    public bool CancelDialogueFor(UnityEngine.Object owner)
    {
        if (!session.IsActive || !hasSessionOwner || !ReferenceEquals(sessionOwner, owner)) return false;
        Finish(DialogueEndReason.Cancelled);
        return true;
    }

    private void Finish(DialogueEndReason reason)
    {
        if (!session.TryEnd(reason, out Action callback)) return;
        int version = sessionVersion;
        sessionOwner = null;
        hasSessionOwner = false;
        gate.Close();
        reveal.Clear();
        DialogueEmotes.Clear();
        ShowActionPrompt(null);
        Action release = releaseControls;
        releaseControls = null;
        IDialoguePresentation returning = sessionPresentation;
        sessionPresentation = null;
        HideSelf();

        if (!handoff.Begin(version, () => CompleteFinish(reason, release, callback)))
        {
            // Defensive: never strand controls or callbacks.
            CompleteFinish(reason, release, callback);
            return;
        }

        bool waiting = false;
        if (IsAlive(returning))
        {
            returningPresentation = returning;
            try { waiting = returning.BeginReturn(reason, () => handoff.Complete(version)); }
            catch (Exception exception)
            {
                Debug.LogException(exception, this);
                SafeAbort(returning);
                waiting = false;
            }
        }
        if (!waiting) handoff.Complete(version);
    }

    private void CompleteFinish(DialogueEndReason reason, Action release, Action callback)
    {
        returningPresentation = null;
        if (activeUI == this) activeUI = null;
        release?.Invoke();
        try { callback?.Invoke(); }
        finally { DialogueEnded?.Invoke(reason); }
    }

    // ------------------------------------------------------------------
    // Visibility and teardown
    // ------------------------------------------------------------------

    private void HideSelf()
    {
        // Skip while Unity is already deactivating the panel (activeSelf is false
        // then); calling SetActive again from OnDisable is not allowed.
        if (panelRoot == null || !panelRoot.activeSelf) return;
        hidingSelf = true;
        try { panelRoot.SetActive(false); }
        finally { hidingSelf = false; }
    }

    /// <summary>
    /// Keeps a plain rectangle directly behind the dialogue box, matching its
    /// layout plus padding. Created at runtime (no scene edit), raycast-transparent,
    /// and hidden with the panel because it is the box's sibling.
    /// </summary>
    private void SyncBackdrop()
    {
        RectTransform box = dialogueText != null ? dialogueText.transform.parent as RectTransform : null;
        bool usable = showBackdrop && box != null && box.parent != null &&
                      (panelRoot == null || box != panelRoot.transform);
        if (!usable)
        {
            if (backdrop != null) backdrop.gameObject.SetActive(false);
            return;
        }

        if (backdrop == null)
        {
            var go = new GameObject("DialogueBackdrop", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.layer = box.gameObject.layer;
            backdrop = (RectTransform)go.transform;
            backdrop.SetParent(box.parent, false);
            backdropImage = go.GetComponent<Image>();
            backdropImage.raycastTarget = false;
            Image boxImage = box.GetComponent<Image>();
            if (boxImage != null)
            {
                backdropImage.sprite = boxImage.sprite;
                backdropImage.type = boxImage.type;
                backdropImage.pixelsPerUnitMultiplier = boxImage.pixelsPerUnitMultiplier;
            }
            if (backdropShadow)
            {
                Shadow shadow = go.AddComponent<Shadow>();
                shadow.effectColor = new Color(0f, 0f, 0f, 0.35f);
                shadow.effectDistance = new Vector2(4f, -4f);
            }
        }

        backdrop.gameObject.SetActive(true);
        int boxIndex = box.GetSiblingIndex();
        int backdropIndex = backdrop.GetSiblingIndex();
        if (backdropIndex != boxIndex - 1)
            backdrop.SetSiblingIndex(backdropIndex < boxIndex ? boxIndex - 1 : boxIndex);

        backdrop.anchorMin = box.anchorMin;
        backdrop.anchorMax = box.anchorMax;
        backdrop.pivot = box.pivot;
        backdrop.localScale = box.localScale;
        backdrop.localRotation = box.localRotation;
        backdrop.sizeDelta = box.sizeDelta + Vector2.one * (2f * backdropPadding);
        backdrop.anchoredPosition = box.anchoredPosition + (box.pivot * 2f - Vector2.one) * backdropPadding;
        backdropImage.color = backdropColor;
    }

    /// <summary>Screen-pixel top of the dialogue box (its backdrop when shown), so emotes stay above it; 0 if unknown.</summary>
    private float BoxTopScreenY()
    {
        RectTransform box = backdrop != null && backdrop.gameObject.activeInHierarchy
            ? backdrop : dialogueText != null ? dialogueText.transform.parent as RectTransform : null;
        if (box == null) return 0f;
        Canvas canvas = box.GetComponentInParent<Canvas>();
        if (canvas == null) return 0f;
        Camera cam = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        box.GetWorldCorners(boxCorners);
        float top = 0f;
        for (int i = 0; i < 4; i++)
            top = Mathf.Max(top, RectTransformUtility.WorldToScreenPoint(cam, boxCorners[i]).y);
        return top;
    }

    private void EnsureBoxTapRelay()
    {
        if (dialogueText == null) return;
        Transform box = dialogueText.transform.parent;
        if (box == null || box.GetComponent<Graphic>() == null)
            box = panelRoot != null ? panelRoot.transform : null;
        if (box == null) return;
        DialogueBoxTapRelay relay = box.GetComponent<DialogueBoxTapRelay>();
        if (relay == null) relay = box.gameObject.AddComponent<DialogueBoxTapRelay>();
        relay.Bind(this);
    }

    private void OnDisable()
    {
        // Our own hide disables this component (it lives on panelRoot); that is
        // not a cancellation. Any other disable cancels the active conversation;
        // the presentation still returns the view before callbacks run.
        if (hidingSelf) return;
        CancelDialogue();
    }

    private void OnDestroy()
    {
        CancelDialogue();
        if (handoff.IsPending)
        {
            // Teardown: the blend cannot be trusted to finish.
            SafeAbort(returningPresentation);
            returningPresentation = null;
            handoff.ForceComplete();
        }
        if (activeUI == this) activeUI = null;
        if (initialized && continueButton != null)
            continueButton.onClick.RemoveListener(OnContinuePressed);
    }

    private static bool IsAlive(IDialoguePresentation value) =>
        value != null && !(value is UnityEngine.Object unityObject && unityObject == null);

    private void SafeAbort(IDialoguePresentation value)
    {
        if (!IsAlive(value)) return;
        try { value.AbortConversation(); }
        catch (Exception exception) { Debug.LogException(exception, this); }
    }
}
