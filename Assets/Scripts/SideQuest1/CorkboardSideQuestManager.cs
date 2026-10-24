using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Runs the four-profile corkboard sorting sidequest.
///
/// T06 (Campaign Foundation): drafting, then a check once the board is full.
/// - Placing or moving a card is only a draft: no correctness sound, tint,
///   reaction, emote or guide hint, and nothing is scored.
/// - The board checks itself as soon as all four areas hold a card (user
///   decision 2026-10-24, replacing the CHECK BOARD stamp), after a short beat
///   so the last pin lands. A re-check needs a changed board (a card moved).
///   At 3/4 before the first check the guide warns once that the next card
///   starts the check.
/// - The FIRST complete check freezes the correct count, the tier (Gold 4/4,
///   Silver 3/4, Bronze 0-2) and the settled quest XP. It marks each card and
///   keeps right cards pinned; checks 1-2 give clues for the red cards, check 3
///   on shows their corners. Corrections never change the frozen result. The
///   quest completes only when all four match. The reward
///   (TutorialNpcController) uses the frozen wrong count and tier.
/// - While the board is open the profiles live in its tray: the HUD profiles
///   icon shows 0/4 and cannot be opened or dragged (CollectedProfilesWorldPanel.ProfilesOnBoard).
/// </summary>
public class CorkboardSideQuestManager : MonoBehaviour
{
    [Header("Setup")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private GameObject sideQuestRoot;
    [SerializeField] private Transform corkboardViewpoint;
    [SerializeField] private Transform cardTrayParent;
    [SerializeField] private GameObject profileCardPrefab;
    [SerializeField] private List<CorkboardSlot> slots;

    [Header("Reward")]
    [SerializeField] private int rewardExp = 50;
    [SerializeField] private int rewardReputation = 10;

    private readonly List<ProfileDragCard> spawnedCards = new List<ProfileDragCard>();
    private readonly HashSet<CorkboardSlot> firstTrySolvedSlots = new HashSet<CorkboardSlot>();
    private readonly List<LineRenderer> dividerLines = new List<LineRenderer>();
    private Camera cachedCamera;
    private Material dividerMaterial;
    private Canvas feedbackCanvas;
    private TextMeshProUGUI feedbackText;

    // Reveal (Side Quest 1 title card): while the card shows, the board is
    // bare; the corner labels, dividers, prompt and the profile cards only
    // appear (with a pop) once it closes.
    private bool revealHeld;
    private bool cameraArrived;
    private bool revealStarted;
    private float revealTimeout;
    private readonly List<KeyValuePair<Transform, Vector3>> hiddenLabels = new List<KeyValuePair<Transform, Vector3>>();
    private Bounds boardBounds;
    private float splitX;
    private float splitY;
    private float boardPlaneZ;
    private int placementAttempts;
    private int wrongPlacements;
    private bool sideQuestActive;
    private bool sideQuestCompleted;
    private bool completionEventRaised;
    private bool boardGeometryReady;

    public bool CanInteract => sideQuestActive && !sideQuestCompleted;

    /// <summary>
    /// Notification: a card's verdict from a board CHECK (correct?, locked is
    /// always false now). Raised only by a committed check, never by a draft drop.
    /// </summary>
    public static event System.Action<ProfileDragCard, bool, bool> CardPlaced;

    /// <summary>The draft changed (a card was placed, moved or taken off). Carries no verdict.</summary>
    public static event System.Action DraftChanged;

    /// <summary>A check was committed: how many of the four are right now (the first one is frozen).</summary>
    public static event System.Action<int> BoardChecked;

    // T06 first-check state (frozen at the first complete check).
    private bool checkSubmitted;
    private int frozenCorrect = -1;
    private int settledExp = -1;
    private readonly List<string> frozenWrongProfileIds = new List<string>();
    private bool checking;
    private bool lastCardWarned;
    private int checkCount;
    // Auto-check: when the board is full it checks after AutoCheckDelay (the pin lands first).
    private const float AutoCheckDelay = 0.45f;
    private float autoCheckAt = -1f;
    private string lastCheckedDraft;

    // Tray layout (user request 2026-10-24): four cards in the left column,
    // vertically centred with equal top/bottom padding and clear gaps between.
    private const float TrayViewX = 0.11f;
    private const float TrayCardHeight = 0.21f;
    private const float TrayCardGap = 0.04f;
    private RectTransform feedbackPanel;

    /// <summary>True once the first complete check was made.</summary>
    public bool CheckSubmitted => checkSubmitted;
    /// <summary>Right answers on the first complete check (-1 before it).</summary>
    public int FrozenCorrect => frozenCorrect;
    /// <summary>Quest XP settled from the first check (-1 before it).</summary>
    public int SettledExp => settledExp;
    /// <summary>The frozen tier as BadgeTier (None before the first check).</summary>
    public BadgeTier FrozenTier => checkSubmitted ? (BadgeTier)ClassificationCheck.TierFor(frozenCorrect) : BadgeTier.None;
    /// <summary>Complete checks made so far (1 = the first check).</summary>
    public int CheckCount => checkCount;
    /// <summary>Areas currently holding a card (the draft count).</summary>
    public int PlacedCount => CountOccupied();

    /// <summary>Wrong drops in a row for one card before it is locked for LockSeconds.</summary>
    public const int MissesBeforeLock = 2;
    public const float LockSeconds = 3f;
    private readonly System.Collections.Generic.Dictionary<ProfileDragCard, int> missesByCard =
        new System.Collections.Generic.Dictionary<ProfileDragCard, int>();
    public bool IsCompleted => sideQuestCompleted;
    /// <summary>Profiles wrong on the FIRST complete check (each one costs EXP; see rewards.json). 0 before it.</summary>
    public int WrongPlacements => wrongPlacements;

    private void Awake()
    {
        if (sideQuestRoot != null)
        {
            sideQuestRoot.SetActive(false);
        }
    }

    private void OnDisable()
    {
        CancelInvoke();
        if (sideQuestCompleted && !completionEventRaised)
        {
            FinishSideQuest();
        }
        if (sideQuestActive)
        {
            sideQuestActive = false;
            if (CameraController.Instance != null)
            {
                CameraController.Instance.ExitCorkboardSideQuest();
            }
        }

        ClearRuntimeObjects();
    }

    public void BeginSideQuest() => Open(null);

    /// <summary>
    /// Resume (T04): reopens the saved board straight away (no title card),
    /// with the saved counters and every saved card pinned in its area again.
    /// Correctness is recomputed from the profiles; no drop is replayed, so
    /// no sound, reaction, score or event happens. False if it cannot open.
    /// </summary>
    public bool ResumeSideQuest(Level1FieldCheckpoint saved)
    {
        if (saved == null) return false;
        Open(saved);
        return CanInteract;
    }

    /// <summary>Resume (T04): the board was already solved in the saved run (counters kept for the reward).</summary>
    public void RestoreCompleted(int attempts, int wrong, int correctOnFirstCheck = -1)
    {
        if (sideQuestActive || sideQuestCompleted) return;
        placementAttempts = Mathf.Max(0, attempts);
        if (correctOnFirstCheck >= 0)
        {
            checkSubmitted = true;
            frozenCorrect = Mathf.Clamp(correctOnFirstCheck, 0, ClassificationCheck.Profiles);
            wrongPlacements = ClassificationCheck.Profiles - frozenCorrect;
        }
        else wrongPlacements = Mathf.Clamp(wrong, 0, ClassificationCheck.Profiles);
        sideQuestCompleted = true;
        completionEventRaised = true;
    }

    /// <summary>The board's saveable state (T04): phase, counters, stable pins and per-area attempts.</summary>
    public void CaptureState(Level1FieldCheckpoint into)
    {
        if (into == null) return;
        into.boardPhase = sideQuestCompleted ? Level1FieldCheckpoint.PhaseSolved
            : sideQuestActive ? Level1FieldCheckpoint.PhaseActive : Level1FieldCheckpoint.PhaseNone;
        into.placementAttempts = placementAttempts;
        into.wrongPlacements = wrongPlacements;
        into.checkSubmitted = checkSubmitted;
        into.checkCorrectCount = checkSubmitted ? frozenCorrect : -1;
        into.checkSettledExp = checkSubmitted ? settledExp : -1;
        into.checkCount = checkSubmitted ? checkCount : 0;
        into.checkWrongProfileIds.Clear();
        if (checkSubmitted) into.checkWrongProfileIds.AddRange(frozenWrongProfileIds);
        into.placements.Clear();
        into.slotAttempts.Clear();
        if (!sideQuestActive || slots == null) return;
        if (resuming != null)
        {
            // Reopened but the cards are not pinned yet: the saved pins still stand.
            foreach (Level1Placement p in resuming.placements)
                into.placements.Add(new Level1Placement { profileId = p.profileId, slotId = p.slotId });
            foreach (Level1SlotAttempts a in resuming.slotAttempts)
                into.slotAttempts.Add(new Level1SlotAttempts { slotId = a.slotId, attempts = a.attempts, firstTrySolved = a.firstTrySolved });
            return;
        }
        foreach (CorkboardSlot slot in slots)
        {
            if (slot == null) continue;
            string id = Level1FieldCheckpointRules.SlotId(slot.ExpectedClassification);
            if (slot.OccupyingCard != null && slot.OccupyingCard.Profile != null)
                into.placements.Add(new Level1Placement { profileId = slot.OccupyingCard.Profile.ProfileId, slotId = id });
            if (slot.PlacementAttempts > 0)
                into.slotAttempts.Add(new Level1SlotAttempts { slotId = id, attempts = slot.PlacementAttempts, firstTrySolved = firstTrySolvedSlots.Contains(slot) });
        }
    }

    /// <summary>The saved run being resumed by the current opening (null = a normal start).</summary>
    private Level1FieldCheckpoint resuming;

    private void Open(Level1FieldCheckpoint saved)
    {
        if (sideQuestActive || sideQuestCompleted)
        {
            return;
        }

        cachedCamera = mainCamera != null ? mainCamera : Camera.main;
        if (cachedCamera == null || corkboardViewpoint == null || !HasValidSlots())
        {
            Debug.LogError("[CorkboardSideQuestManager] A camera, viewpoint, and four configured slots are required.");
            return;
        }

        if (profileCardPrefab == null || cardTrayParent == null || TaxpayerProfileCollectionHUD.Instance == null ||
            TaxpayerProfileCollectionHUD.Instance.CollectedProfiles.Count < 4)
        {
            Debug.LogError("[CorkboardSideQuestManager] The card prefab, tray, and four collected profiles are required.");
            return;
        }

        if (!BuildBoardGeometry())
        {
            Debug.LogError("[CorkboardSideQuestManager] Could not read the four corkboard drop areas.");
            return;
        }

        ClearRuntimeObjects();
        // Profile windows opened from the HUD close: the cards are about to fly into the tray.
        HudProfileCards hudCards = FindAnyObjectByType<HudProfileCards>(FindObjectsInactive.Exclude);
        if (hudCards != null) hudCards.CloseCards();
        resuming = saved;
        placementAttempts = saved != null ? Mathf.Max(0, saved.placementAttempts) : 0;
        firstTrySolvedSlots.Clear();
        checkSubmitted = saved != null && saved.checkSubmitted && saved.checkCorrectCount >= 0;
        frozenCorrect = checkSubmitted ? Mathf.Clamp(saved.checkCorrectCount, 0, ClassificationCheck.Profiles) : -1;
        wrongPlacements = checkSubmitted ? ClassificationCheck.Profiles - frozenCorrect : 0;
        settledExp = checkSubmitted ? saved.checkSettledExp : -1;
        frozenWrongProfileIds.Clear();
        if (checkSubmitted && saved.checkWrongProfileIds != null) frozenWrongProfileIds.AddRange(saved.checkWrongProfileIds);
        checkCount = checkSubmitted ? Mathf.Max(1, saved.checkCount) : 0;
        checking = false;
        lastCardWarned = false;
        autoCheckAt = -1f;
        lastCheckedDraft = null;
        sideQuestActive = true;

        if (sideQuestRoot != null && !sideQuestRoot.activeSelf)
        {
            sideQuestRoot.SetActive(true);
        }

        revealHeld = saved == null && SideQuestSplash.Active;
        revealStarted = false;
        cameraArrived = false;
        if (revealHeld)
        {
            // Only the bare corkboard behind the title card.
            HideSlotLabels();
            revealTimeout = Time.unscaledTime + 30f;
        }
        else
        {
            CreateDividers();
            SetFeedback(saved != null ? "Welcome back. Your files are where you pinned them."
                : "Your files are ready. Place each profile on a corkboard area, then check the board.");
        }

        if (CameraController.Instance != null)
        {
            CameraController.Instance.EnterCorkboardSideQuest(corkboardViewpoint, OnCameraArrived);
        }
        else
        {
            OnCameraArrived();
        }
    }

    private void OnCameraArrived()
    {
        cameraArrived = true;
        if (!revealHeld) SpawnCards();
    }

    /// <summary>
    /// Called when the Side Quest 1 title card has closed: the board fills in
    /// with a subtle pop (labels, dividers, prompt), then the profile cards
    /// fly in as before.
    /// </summary>
    public void ReleaseReveal()
    {
        if (!revealHeld || revealStarted) return;
        revealStarted = true;
        StartCoroutine(Reveal());
    }

    private void Update()
    {
        // Safety: never leave the board bare if the title card never closes.
        if (revealHeld && !revealStarted && sideQuestActive && Time.unscaledTime > revealTimeout) ReleaseReveal();
        UpdateAutoCheck();
    }

    /// <summary>
    /// The board checks itself once all four areas hold a card and the board
    /// differs from the last check. Waits while a card is moving; a card taken
    /// off before the beat ends cancels it.
    /// </summary>
    private void UpdateAutoCheck()
    {
        if (autoCheckAt < 0f) return;
        if (!CanInteract || !ClassificationCheck.CanCheck(CountOccupied())) { autoCheckAt = -1f; return; }
        if (Time.unscaledTime < autoCheckAt) return;
        foreach (ProfileDragCard card in spawnedCards)
            if (card != null && card.IsBeingMoved) return;   // let the drag or pin finish
        autoCheckAt = -1f;
        if (DraftSignature() == lastCheckedDraft) return;
        RequestCheck();
    }

    /// <summary>Which card is in which area (compares a board with the last check).</summary>
    private string DraftSignature()
    {
        var parts = new List<string>();
        if (slots != null)
            foreach (CorkboardSlot slot in slots)
                parts.Add(slot != null && slot.OccupyingCard != null && slot.OccupyingCard.Profile != null
                    ? slot.OccupyingCard.Profile.ProfileId : "-");
        return string.Join("|", parts);
    }

    private System.Collections.IEnumerator Reveal()
    {
        while (!cameraArrived) yield return null;
        if (!sideQuestActive) yield break;

        // Corner labels pop in one by one.
        for (int i = 0; i < hiddenLabels.Count; i++)
        {
            StartCoroutine(Pop(hiddenLabels[i].Key, hiddenLabels[i].Value, 0.32f));
            yield return Wait(0.08f);
        }
        hiddenLabels.Clear();

        // Dividers grow out from the centre of the board.
        CreateDividers();
        StartCoroutine(GrowDividers(0.4f));
        yield return Wait(0.25f);

        // The prompt panel pops up.
        SetFeedback("Your files are ready. Match each profile to a corkboard area.");
        Transform panel = feedbackCanvas != null ? feedbackCanvas.transform.Find("FeedbackPanel") : null;
        if (panel != null) StartCoroutine(Pop(panel, panel.localScale, 0.3f));
        yield return Wait(0.2f);

        revealHeld = false;
        SpawnCards();   // the existing fly-in from the profiles icon
    }

    private static System.Collections.IEnumerator Wait(float seconds)
    {
        float end = Time.unscaledTime + seconds;
        while (Time.unscaledTime < end) yield return null;
    }

    /// <summary>Subtle pop: from 60% with a small overshoot to full size.</summary>
    private static System.Collections.IEnumerator Pop(Transform target, Vector3 fullScale, float seconds)
    {
        if (target == null) yield break;
        float t = 0f;
        while (t < seconds && target != null)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / seconds) - 1f;
            float back = 1f + k * k * (2.2f * k + 1.2f);   // ease-out-back, gentle overshoot
            target.localScale = fullScale * Mathf.LerpUnclamped(0.6f, 1f, back);
            yield return null;
        }
        if (target != null) target.localScale = fullScale;
    }

    private System.Collections.IEnumerator GrowDividers(float seconds)
    {
        var ends = new List<KeyValuePair<Vector3, Vector3>>();
        foreach (LineRenderer line in dividerLines)
            ends.Add(new KeyValuePair<Vector3, Vector3>(line.GetPosition(0), line.GetPosition(1)));
        float t = 0f;
        while (t < seconds)
        {
            t += Time.unscaledDeltaTime;
            float k = 1f - Mathf.Pow(1f - Mathf.Clamp01(t / seconds), 3f);
            for (int i = 0; i < dividerLines.Count && i < ends.Count; i++)
            {
                if (dividerLines[i] == null) continue;
                Vector3 mid = (ends[i].Key + ends[i].Value) * 0.5f;
                dividerLines[i].SetPosition(0, Vector3.Lerp(mid, ends[i].Key, k));
                dividerLines[i].SetPosition(1, Vector3.Lerp(mid, ends[i].Value, k));
            }
            yield return null;
        }
    }

    private void HideSlotLabels()
    {
        hiddenLabels.Clear();
        if (slots == null) return;
        foreach (CorkboardSlot slot in slots)
        {
            if (slot == null) continue;
            foreach (TMP_Text label in slot.GetComponentsInChildren<TMP_Text>(true))
            {
                hiddenLabels.Add(new KeyValuePair<Transform, Vector3>(label.transform, label.transform.localScale));
                label.transform.localScale = Vector3.zero;
            }
        }
    }

    private void RestoreSlotLabels()
    {
        foreach (var pair in hiddenLabels)
            if (pair.Key != null) pair.Key.localScale = pair.Value;
        hiddenLabels.Clear();
    }

    /// <summary>Maps a pointer ray to a quadrant and clamps the whole card inside it.</summary>
    public bool TryGetDrop(Ray ray, Transform card, out CorkboardSlot slot, out Vector3 position, out Quaternion rotation)
    {
        slot = null;
        position = default;
        rotation = Quaternion.identity;
        if (!CanInteract || card == null || !boardGeometryReady || !HasValidSlots())
        {
            return false;
        }

        Transform board = BoardTransform;
        float pinnedDepth = boardPlaneZ - 0.008f;
        Vector3 planePoint = board.TransformPoint(new Vector3(0f, 0f, pinnedDepth));
        Plane plane = new Plane(board.forward, planePoint);
        if (!plane.Raycast(ray, out float distance) || distance < 0f)
        {
            return false;
        }

        Vector3 boardPoint = board.InverseTransformPoint(ray.GetPoint(distance));
        if (boardPoint.x < boardBounds.min.x || boardPoint.x > boardBounds.max.x ||
            boardPoint.y < boardBounds.min.y || boardPoint.y > boardBounds.max.y)
        {
            return false;
        }

        slot = FindSlotForQuadrant(boardPoint.x >= splitX, boardPoint.y >= splitY);
        // T06: an area whose card a check marked right is settled; the dropped card goes back to the tray.
        if (slot != null && slot.OccupyingCard != null && slot.OccupyingCard.transform != card && slot.OccupyingCard.IsLockedCorrect)
            slot = null;
        if (slot == null)
        {
            slot = null;
            return false;
        }

        GetCardHalfExtents(card, board, out float halfWidth, out float halfHeight);
        const float edgeGap = 0.015f;
        float minX = boardBounds.min.x + halfWidth + edgeGap;
        float maxX = boardBounds.max.x - halfWidth - edgeGap;
        float minY = boardBounds.min.y + halfHeight + edgeGap;
        float maxY = boardBounds.max.y - halfHeight - edgeGap;
        if (minX > maxX || minY > maxY)
        {
            slot = null;
            return false;
        }

        float x = Mathf.Clamp(boardPoint.x, minX, maxX);
        float y = Mathf.Clamp(boardPoint.y, minY, maxY);
        position = board.TransformPoint(new Vector3(x, y, pinnedDepth));
        rotation = board.rotation;
        return true;
    }

    /// <summary>Called after the card's short pin animation has finished.</summary>
    public bool HandleCardDropped(ProfileDragCard card, CorkboardSlot slot)
    {
        if (!CanInteract || card == null || card.Profile == null || !spawnedCards.Contains(card) ||
            slot == null || slots == null || !slots.Contains(slot))
        {
            return false;
        }

        foreach (CorkboardSlot existingSlot in slots)
        {
            if (existingSlot != null && existingSlot != slot && existingSlot.OccupyingCard == card)
            {
                existingSlot.VacateSlot(card);
            }
        }

        // T06: a drop is only a draft. The area records the card (its hidden
        // match is read only by a CHECK); no sound, reaction, emote or score.
        placementAttempts++;
        slot.TryPlaceCard(card);
        SetFeedback(checkSubmitted
            ? $"First check: {frozenCorrect}/4 ({FrozenTier}). {CountOccupied()}/4 placed. The board checks again when all four are pinned."
            : ClassificationCheck.DraftMessage(CountOccupied()));
        HandleDraftChanged();
        return false;
    }

    public void NotifyCardRemovedFromSlot()
    {
        if (!CanInteract) return;
        SetFeedback(checkSubmitted
            ? $"First check: {frozenCorrect}/4 ({FrozenTier}). Pin the card again and the board checks again."
            : ClassificationCheck.DraftMessage(CountOccupied()));
        HandleDraftChanged();
    }

    public void CancelSideQuest()
    {
        if (sideQuestCompleted)
        {
            return;
        }

        CancelInvoke();
        sideQuestActive = false;
        ClearRuntimeObjects();
        if (sideQuestRoot != null && sideQuestRoot.activeSelf)
        {
            sideQuestRoot.SetActive(false);
        }
        if (CameraController.Instance != null)
        {
            CameraController.Instance.ExitCorkboardSideQuest();
        }
    }

    private Transform BoardTransform => sideQuestRoot != null ? sideQuestRoot.transform : transform;

    private void SpawnCards()
    {
        if (!CanInteract || TaxpayerProfileCollectionHUD.Instance == null)
        {
            return;
        }

        IReadOnlyList<TaxpayerProfile> profiles = TaxpayerProfileCollectionHUD.Instance.CollectedProfiles;
        if (profiles.Count < 4)
        {
            CancelSideQuest();
            Debug.LogError("[CorkboardSideQuestManager] Four collected profiles are required.");
            return;
        }

        Vector2 collectionScreenPoint = default;
        bool animateFromHud = resuming == null && cachedCamera != null &&
            TaxpayerProfileCollectionHUD.Instance.TryGetCollectionScreenPoint(out collectionScreenPoint);

        if (!TryGetTrayView(out Plane trayPlane, out float viewHeight))
        {
            CancelSideQuest();
            Debug.LogError("[CorkboardSideQuestManager] Could not frame the card tray in the game camera.");
            return;
        }

        // Left tray column: four cards centred vertically (equal top/bottom padding) with clear gaps.
        float cardHeight = TrayCardHeight;
        float[] cardViewY = ClassificationCheck.TrayCentres(ClassificationCheck.Profiles, cardHeight, TrayCardGap);
        for (int i = 0; i < 4; i++)
        {
            GameObject cardObject = Instantiate(profileCardPrefab, cardTrayParent);
            ProfileDragCard card = cardObject.GetComponent<ProfileDragCard>();
            if (card == null)
            {
                Destroy(cardObject);
                CancelSideQuest();
                Debug.LogError("[CorkboardSideQuestManager] The card prefab is missing ProfileDragCard.");
                return;
            }

            card.transform.rotation = BoardTransform.rotation;
            GetCardHalfExtents(card.transform, BoardTransform, out _, out float halfHeight);
            float baseHeight = Mathf.Max(halfHeight * 2f, 0.01f);
            float scale = viewHeight * cardHeight / baseHeight;
            card.transform.localScale *= scale;
            if (!TryGetTrayPoint(trayPlane, TrayViewX, cardViewY[i], out Vector3 trayPosition))
            {
                Destroy(cardObject);
                CancelSideQuest();
                Debug.LogError("[CorkboardSideQuestManager] Could not place a profile card in the visible tray.");
                return;
            }
            card.transform.position = trayPosition;
            card.Initialize(profiles[i], this, cachedCamera);
            if (animateFromHud) card.AnimateFromHud(collectionScreenPoint, i * 0.09f);
            spawnedCards.Add(card);
        }

        // The profiles are in the tray now: the HUD profiles icon is empty and locked.
        if (CollectedProfilesWorldPanel.Instance != null) CollectedProfilesWorldPanel.Instance.SetProfilesOnBoard(true);
        LayoutFeedbackUnderBoard();
        if (resuming != null)
        {
            RestorePins(resuming);
            resuming = null;
            if (checkSubmitted)
            {
                // The check already happened: show its marks again quietly (no sound, reaction or event).
                MarkPlacedCards(raiseEvents: false);
                lastCheckedDraft = DraftSignature();
                SetFeedback($"Welcome back. First check: {frozenCorrect}/4 ({FrozenTier}). Move the red cards and the board checks again.");
            }
            else
            {
                SetFeedback("Welcome back. " + ClassificationCheck.DraftMessage(CountOccupied()));
                // Left with a full board before its check: check it once the cards are back.
                if (ClassificationCheck.CanCheck(CountOccupied())) autoCheckAt = Time.unscaledTime + AutoCheckDelay;
            }
            return;
        }
        SetFeedback(ClassificationCheck.DraftMessage(0));
    }

    // =========================================================
    // CHECK BOARD (T06)
    // =========================================================

    /// <summary>The feedback line spans the board's width under it (centred on the board, not the screen).</summary>
    private void LayoutFeedbackUnderBoard()
    {
        if (feedbackPanel == null || cachedCamera == null) return;
        Transform board = BoardTransform;
        float minX = 1f, maxX = 0f;
        for (int i = 0; i < 4; i++)
        {
            float x = (i & 1) == 0 ? boardBounds.min.x : boardBounds.max.x;
            float y = (i & 2) == 0 ? boardBounds.min.y : boardBounds.max.y;
            Vector3 viewport = cachedCamera.WorldToViewportPoint(board.TransformPoint(new Vector3(x, y, boardPlaneZ)));
            if (viewport.z <= 0f) return;
            minX = Mathf.Min(minX, viewport.x);
            maxX = Mathf.Max(maxX, viewport.x);
        }
        minX = Mathf.Clamp01(minX + 0.015f);
        maxX = Mathf.Clamp01(maxX - 0.015f);
        if (maxX - minX < 0.3f) return;
        feedbackPanel.anchorMin = new Vector2(minX, 0f);
        feedbackPanel.anchorMax = new Vector2(maxX, 0f);
        feedbackPanel.sizeDelta = new Vector2(0f, feedbackPanel.sizeDelta.y);
    }

    /// <summary>
    /// Checks the full board (started by UpdateAutoCheck). The first time, the
    /// result is frozen (count, tier, settled XP); every check marks the cards
    /// and gives clues (answers from check 3); all four right completes the quest.
    /// </summary>
    public void RequestCheck()
    {
        if (!CanInteract || checking) return;
        foreach (ProfileDragCard card in spawnedCards)
            if (card != null && card.IsBeingMoved) return;   // finish the drag first
        int placed = CountOccupied();
        if (!ClassificationCheck.CanCheck(placed))
        {
            SetFeedback(ClassificationCheck.RemainingMessage(placed));
            if (AudioManager.Instance != null) AudioManager.Instance.PlayEffect("sfx_card_return_01", 0.5f);
            return;
        }

        checking = true;
        autoCheckAt = -1f;
        lastCheckedDraft = DraftSignature();
        checkCount++;
        int correct = CountSolvedSlots();
        if (!checkSubmitted)
        {
            checkSubmitted = true;
            frozenCorrect = correct;
            wrongPlacements = ClassificationCheck.Profiles - correct;
            frozenWrongProfileIds.Clear();
            foreach (CorkboardSlot slot in slots)
                if (slot != null && slot.OccupyingCard != null && !slot.IsSolved && slot.OccupyingCard.Profile != null)
                    frozenWrongProfileIds.Add(slot.OccupyingCard.Profile.ProfileId);
            settledExp = SettleExp(wrongPlacements);
            Debug.Log($"[CorkboardSideQuestManager] First check frozen: {frozenCorrect}/4, tier {FrozenTier}, {settledExp} XP.");
        }

        MarkPlacedCards(raiseEvents: true);
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayEffect(correct == ClassificationCheck.Profiles ? "sfx_classification_correct_01" : "sfx_classification_incorrect_01");
        try { BoardChecked?.Invoke(correct); }
        catch (System.Exception error) { Debug.LogException(error); }

        if (correct == ClassificationCheck.Profiles)
        {
            checking = false;
            CheckForCompletion();
            return;
        }
        // Hint ladder: clues on checks 1-2, the right corners from check 3 (ClassificationCheck).
        bool reveal = ClassificationCheck.RevealsAnswers(checkCount);
        List<ProfileDragCard> misses = RedCards();
        string guide = string.Join(" ", misses.ConvertAll(card => reveal ? AnswerLine(card) : ClueLine(card)));
        bool spoken = misses.Count > 0 && GuideVoice.TrySay(guide, reveal ? null : "confused", 4f + 3f * misses.Count);
        SetFeedback(CorrectionText(correct, reveal, misses, spoken || misses.Count == 0 ? null : (reveal ? AnswerLine(misses[0]) : ClueLine(misses[0]))));
        checking = false;
    }

    /// <summary>Marks every pinned card with the live verdict (right ones stay pinned).</summary>
    private void MarkPlacedCards(bool raiseEvents)
    {
        foreach (CorkboardSlot slot in slots)
        {
            if (slot == null || slot.OccupyingCard == null) continue;
            slot.OccupyingCard.MarkChecked(slot.IsSolved);
            if (!raiseEvents) continue;
            try { CardPlaced?.Invoke(slot.OccupyingCard, slot.IsSolved, false); }
            catch (System.Exception error) { Debug.LogException(error); }
        }
    }

    /// <summary>Pinned cards the last check marked wrong, in board order.</summary>
    private List<ProfileDragCard> RedCards()
    {
        var red = new List<ProfileDragCard>();
        foreach (CorkboardSlot slot in slots)
        {
            ProfileDragCard card = slot != null ? slot.OccupyingCard : null;
            if (card != null && card.Profile != null && !slot.IsSolved) red.Add(card);
        }
        return red;
    }

    /// <summary>
    /// The board line after a check that is not all right. Checks 1-2 name the
    /// red cards only (the clues come from Arthur, or the first clue is added
    /// here when he cannot speak); from check 3 the right corners are listed.
    /// </summary>
    private string CorrectionText(int correct, bool reveal, List<ProfileDragCard> misses, string inlineHint)
    {
        string head = $"First check: {frozenCorrect}/4 ({FrozenTier}).";
        if (checkCount > 1) head += $" Check {checkCount}: {correct}/4.";
        if (reveal)
        {
            var answers = misses.ConvertAll(card =>
                $"{FirstName(card.Profile.TaxpayerName)} -> {EnumDisplayFormatter.Format(card.Profile.CorrectClassification.ToString())}");
            return $"{head} Answers: {string.Join(", ", answers)}. Move them and the board checks again.";
        }
        string names = string.Join(", ", misses.ConvertAll(card => FirstName(card.Profile.TaxpayerName)));
        string hint = string.IsNullOrEmpty(inlineHint) ? "Use Arthur's clues" : inlineHint;
        return $"{head} Red: {names}. {hint} Move them and the board checks again.";
    }

    /// <summary>A clue for a red card: which of the two questions it missed (never the corner).</summary>
    private static string ClueLine(ProfileDragCard card)
    {
        CorkboardSlot slot = card.CurrentSlot;
        return slot != null ? CorkboardTeaching.ReactionFor(card.Profile, slot.ExpectedClassification) : "";
    }

    /// <summary>The answer for a red card (check 3 on): its corner and why.</summary>
    private static string AnswerLine(ProfileDragCard card)
    {
        TaxpayerClassification right = card.Profile.CorrectClassification;
        return $"{FirstName(card.Profile.TaxpayerName)} belongs in {EnumDisplayFormatter.Format(right.ToString())}. " +
               CorkboardTeaching.ReactionFor(card.Profile, right);
    }

    private static string FirstName(string full)
    {
        if (string.IsNullOrWhiteSpace(full)) return "This profile";
        int space = full.IndexOf(' ');
        return space > 0 ? full.Substring(0, space) : full;
    }

    /// <summary>Quest XP from the frozen wrong count with the level's rewards.json (same maths as the reward).</summary>
    private int SettleExp(int wrong)
    {
        CaseProgressionManager progression = CaseProgressionManager.Instance;
        LevelRewardRules rules = progression != null ? progression.Score.Rules : null;
        if (rules != null && rules.sideQuest != null) return LevelScoring.SideQuest(rules, wrong).Exp;
        return ClassificationCheck.SettledExp(rewardExp, 5, 0, wrong);
    }

    private void HandleDraftChanged()
    {
        int placed = CountOccupied();
        // One heads-up before the first check: the next card starts it.
        if (placed == ClassificationCheck.Profiles - 1 && !checkSubmitted && !lastCardWarned)
        {
            lastCardWarned = true;
            GuideVoice.TrySay(ClassificationCheck.LastCardPrompt, null, 4.5f);
        }
        // Full board: check after a short beat (UpdateAutoCheck); a card taken off cancels it.
        autoCheckAt = ClassificationCheck.CanCheck(placed) ? Time.unscaledTime + AutoCheckDelay : -1f;
        try { DraftChanged?.Invoke(); }
        catch (System.Exception error) { Debug.LogException(error); }
    }

    /// <summary>Pins each saved card in the middle of its saved area (kept inside the area like a drop).</summary>
    private void RestorePins(Level1FieldCheckpoint saved)
    {
        Transform board = BoardTransform;
        foreach (Level1Placement placement in saved.placements)
        {
            ProfileDragCard card = spawnedCards.Find(c => c != null && c.Profile != null && c.Profile.ProfileId == placement.profileId);
            CorkboardSlot slot = slots.Find(a => a != null && Level1FieldCheckpointRules.SlotId(a.ExpectedClassification) == placement.slotId);
            if (card == null || slot == null || slot.OccupyingCard != null || !slot.TryGetBoardLocalBounds(board, out Bounds area)) continue;

            GetCardHalfExtents(card.transform, board, out float halfWidth, out float halfHeight);
            const float edgeGap = 0.015f;
            Vector3 min = Vector3.Max(area.min, boardBounds.min) + new Vector3(halfWidth + edgeGap, halfHeight + edgeGap, 0f);
            Vector3 max = Vector3.Min(area.max, boardBounds.max) - new Vector3(halfWidth + edgeGap, halfHeight + edgeGap, 0f);
            float x = min.x <= max.x ? (min.x + max.x) * 0.5f : area.center.x;
            float y = min.y <= max.y ? (min.y + max.y) * 0.5f : area.center.y;
            Vector3 position = board.TransformPoint(new Vector3(x, y, boardPlaneZ - 0.008f));
            card.RestorePinned(slot, position, board.rotation);
            slot.RestoreCard(card);
        }
        foreach (Level1SlotAttempts record in saved.slotAttempts)
        {
            CorkboardSlot slot = slots.Find(a => a != null && Level1FieldCheckpointRules.SlotId(a.ExpectedClassification) == record.slotId);
            if (slot == null) continue;
            slot.RestoreAttempts(record.attempts);
            if (record.firstTrySolved) firstTrySolvedSlots.Add(slot);
        }
    }

    private bool TryGetTrayView(out Plane trayPlane, out float viewHeight)
    {
        Transform board = BoardTransform;
        trayPlane = new Plane(board.forward, cardTrayParent.position);
        viewHeight = 0f;
        if (cachedCamera == null || !TryGetTrayPoint(trayPlane, 0.5f, 0f, out Vector3 bottom) ||
            !TryGetTrayPoint(trayPlane, 0.5f, 1f, out Vector3 top))
        {
            return false;
        }

        viewHeight = Mathf.Abs(board.InverseTransformPoint(top).y - board.InverseTransformPoint(bottom).y);
        return viewHeight > 0.01f;
    }

    private bool TryGetTrayPoint(Plane trayPlane, float viewportX, float viewportY, out Vector3 point)
    {
        point = default;
        if (cachedCamera == null)
        {
            return false;
        }

        Ray ray = cachedCamera.ViewportPointToRay(new Vector3(viewportX, viewportY, 0f));
        if (!trayPlane.Raycast(ray, out float distance) || distance < 0f)
        {
            return false;
        }

        point = ray.GetPoint(distance);
        return true;
    }

    private bool BuildBoardGeometry()
    {
        boardGeometryReady = false;
        Transform board = BoardTransform;
        bool hasBounds = false;
        float snapZTotal = 0f;
        foreach (CorkboardSlot area in slots)
        {
            if (area == null || !area.TryGetBoardLocalBounds(board, out Bounds areaBounds))
            {
                return false;
            }

            if (hasBounds)
            {
                boardBounds.Encapsulate(areaBounds);
            }
            else
            {
                boardBounds = areaBounds;
                hasBounds = true;
            }

            snapZTotal += board.InverseTransformPoint(area.SnapPosition).z;
        }

        if (!hasBounds)
        {
            return false;
        }

        // Drop colliders can extend past the board; keep the playable cross on its face.
        Transform boardVisual = board.Find("Corkboard");
        if (boardVisual != null)
        {
            Bounds faceBounds = default;
            bool hasFaceBounds = false;
            foreach (MeshFilter mesh in boardVisual.GetComponentsInChildren<MeshFilter>(true))
            {
                if (mesh.sharedMesh == null) continue;
                Bounds meshBounds = mesh.sharedMesh.bounds;
                for (int x = -1; x <= 1; x += 2)
                for (int y = -1; y <= 1; y += 2)
                for (int z = -1; z <= 1; z += 2)
                {
                    Vector3 corner = meshBounds.center + Vector3.Scale(meshBounds.extents, new Vector3(x, y, z));
                    Vector3 point = board.InverseTransformPoint(mesh.transform.TransformPoint(corner));
                    if (hasFaceBounds) faceBounds.Encapsulate(point);
                    else { faceBounds = new Bounds(point, Vector3.zero); hasFaceBounds = true; }
                }
            }
            if (hasFaceBounds)
            {
                // Leave room for the frame and the rounded ends of the divider strokes.
                const float frameInset = 0.035f;
                Vector3 min = boardBounds.min;
                Vector3 max = boardBounds.max;
                min.x = Mathf.Max(min.x, faceBounds.min.x + frameInset);
                min.y = Mathf.Max(min.y, faceBounds.min.y + frameInset);
                max.x = Mathf.Min(max.x, faceBounds.max.x - frameInset);
                max.y = Mathf.Min(max.y, faceBounds.max.y - frameInset);
                if (min.x >= max.x || min.y >= max.y) return false;
                boardBounds.SetMinMax(min, max);
            }
        }

        splitX = boardBounds.center.x;
        splitY = boardBounds.center.y;
        // The serialized snap points define the visible card face plane in front of the board.
        boardPlaneZ = snapZTotal / slots.Count;
        boardGeometryReady = true;
        return true;
    }

    private bool HasValidSlots()
    {
        if (slots == null || slots.Count != 4)
        {
            return false;
        }
        var uniqueSlots = new HashSet<CorkboardSlot>();
        var categories = new HashSet<TaxpayerClassification>();
        foreach (CorkboardSlot slot in slots)
        {
            if (slot == null || slot.DropArea == null || !uniqueSlots.Add(slot) || !categories.Add(slot.ExpectedClassification))
            {
                return false;
            }
        }
        return categories.Count == 4;
    }

    private CorkboardSlot FindSlotForQuadrant(bool right, bool upper)
    {
        foreach (CorkboardSlot candidate in slots)
        {
            if (!candidate.TryGetBoardLocalBounds(BoardTransform, out Bounds bounds))
            {
                continue;
            }
            if ((bounds.center.x >= splitX) == right && (bounds.center.y >= splitY) == upper)
            {
                return candidate;
            }
        }
        return null;
    }

    private void GetCardHalfExtents(Transform card, Transform board, out float halfWidth, out float halfHeight)
    {
        Bounds cardBounds = default;
        bool hasBounds = false;
        foreach (Renderer visual in card.GetComponentsInChildren<Renderer>())
        {
            Bounds localBounds = visual.localBounds;
            Vector3 ext = localBounds.extents;
            for (int x = -1; x <= 1; x += 2)
            for (int y = -1; y <= 1; y += 2)
            for (int z = -1; z <= 1; z += 2)
            {
                Vector3 localPoint = localBounds.center + Vector3.Scale(ext, new Vector3(x, y, z));
                Vector3 local = board.InverseTransformPoint(visual.transform.TransformPoint(localPoint));
                if (hasBounds) cardBounds.Encapsulate(local);
                else { cardBounds = new Bounds(local, Vector3.zero); hasBounds = true; }
            }
        }

        halfWidth = hasBounds ? cardBounds.extents.x : 0.15f;
        halfHeight = hasBounds ? cardBounds.extents.y : 0.15f;
    }

    private void CreateDividers()
    {
        DestroyDividers();
        dividerMaterial = CreateLineMaterial();
        if (dividerMaterial == null)
        {
            return;
        }

        float z = boardPlaneZ - 0.002f;
        CreateDivider("BoardDividerVertical", new Vector3(splitX, boardBounds.min.y, z), new Vector3(splitX, boardBounds.max.y, z), dividerMaterial);
        CreateDivider("BoardDividerHorizontal", new Vector3(boardBounds.min.x, splitY, z), new Vector3(boardBounds.max.x, splitY, z), dividerMaterial);
    }

    private void CreateDivider(string name, Vector3 start, Vector3 end, Material material)
    {
        var lineObject = new GameObject(name);
        lineObject.transform.SetParent(BoardTransform, false);
        LineRenderer line = lineObject.AddComponent<LineRenderer>();
        line.useWorldSpace = false;
        line.positionCount = 2;
        line.SetPosition(0, start);
        line.SetPosition(1, end);
        line.startWidth = line.endWidth = 0.009f;
        line.numCapVertices = 2;
        line.sharedMaterial = material;
        line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        line.receiveShadows = false;
        line.sortingOrder = 2;
        dividerLines.Add(line);
    }

    private Material CreateLineMaterial()
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
        {
            Debug.LogWarning("[CorkboardSideQuestManager] Could not find an unlit shader for board dividers.");
            return null;
        }

        var material = new Material(shader) { color = new Color(0.94f, 0.82f, 0.55f, 0.9f) };
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", material.color);
        return material;
    }

    private void SetFeedback(string message)
    {
        if (feedbackText == null)
        {
            CreateFeedbackCanvas();
        }
        if (feedbackText != null)
        {
            feedbackText.text = message;
        }
    }

    private void CreateFeedbackCanvas()
    {
        var canvasObject = new GameObject("CorkboardFeedback", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        feedbackCanvas = canvasObject.GetComponent<Canvas>();
        feedbackCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        feedbackCanvas.sortingOrder = 30;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);
        scaler.matchWidthOrHeight = 0.5f;

        var panelObject = new GameObject("FeedbackPanel", typeof(RectTransform), typeof(Image));
        panelObject.transform.SetParent(canvasObject.transform, false);
        RectTransform panel = panelObject.GetComponent<RectTransform>();
        panel.anchorMin = panel.anchorMax = new Vector2(0.5f, 0f);
        panel.pivot = new Vector2(0.5f, 0f);
        panel.anchoredPosition = new Vector2(0f, 10f);
        panel.sizeDelta = new Vector2(650f, 62f);
        feedbackPanel = panel;
        Image background = panelObject.GetComponent<Image>();
        background.color = new Color(0.07f, 0.055f, 0.04f, 0.92f);
        background.raycastTarget = false;

        var textObject = new GameObject("FeedbackText", typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(panel, false);
        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(18f, 8f);
        textRect.offsetMax = new Vector2(-18f, -8f);
        feedbackText = textObject.GetComponent<TextMeshProUGUI>();
        feedbackText.font = TMP_Settings.defaultFontAsset;
        // Big and easy to read on the board (same font as the rest of the game).
        feedbackText.fontSize = 22f;
        feedbackText.enableAutoSizing = true;
        feedbackText.fontSizeMin = 15f;
        feedbackText.fontSizeMax = 22f;
        feedbackText.fontStyle = FontStyles.Bold;
        feedbackText.color = new Color(1f, 0.94f, 0.78f);
        feedbackText.alignment = TextAlignmentOptions.Center;
        feedbackText.enableWordWrapping = true;
        feedbackText.raycastTarget = false;
    }

    private void CheckForCompletion()
    {
        if (slots == null || slots.Count != 4 || CountSolvedSlots() != 4)
        {
            return;
        }
        foreach (CorkboardSlot slot in slots)
        {
            if (slot == null || slot.OccupyingCard == null || !slot.IsSolved)
            {
                return;
            }
        }
        CompleteSideQuest();
    }

    /// <summary>Areas holding a card, right or not (the draft count; reveals nothing).</summary>
    private int CountOccupied()
    {
        int count = 0;
        if (slots == null) return 0;
        foreach (CorkboardSlot slot in slots)
            if (slot != null && slot.OccupyingCard != null) count++;
        return count;
    }

    private int CountSolvedSlots()
    {
        int count = 0;
        if (slots == null) return 0;
        foreach (CorkboardSlot slot in slots)
        {
            if (slot != null && slot.OccupyingCard != null && slot.IsSolved) count++;
        }
        return count;
    }

    private void CompleteSideQuest()
    {
        if (sideQuestCompleted)
        {
            return;
        }

        sideQuestCompleted = true;
        sideQuestActive = false;
        SetFeedback($"All four files are correct! First check: {frozenCorrect}/4, {FrozenTier} tier.");
        GrantRewards();
        Invoke(nameof(HideSideQuestRoot), 2f);
    }

    private void GrantRewards()
    {
        // The level score is awarded by the tutorial NPC (TutorialNpcController)
        // from WrongPlacements and rewards.json; this only logs the run.
        Debug.Log($"[CorkboardSideQuestManager] Side quest finished: first check {frozenCorrect}/4 ({FrozenTier}), {settledExp} XP settled, {placementAttempts} placements.");
    }

    private void HideSideQuestRoot()
    {
        if (sideQuestRoot != null)
        {
            sideQuestRoot.SetActive(false);
        }

        FinishSideQuest();
    }

    private void FinishSideQuest()
    {
        if (completionEventRaised)
        {
            return;
        }

        completionEventRaised = true;
        if (CollectedProfilesWorldPanel.Instance != null) CollectedProfilesWorldPanel.Instance.SetProfilesOnBoard(false);
        if (CameraController.Instance != null)
        {
            CameraController.Instance.ExitCorkboardSideQuest();
        }

        TownEvents.RaiseSideQuestCompleted();
    }

    private void ClearRuntimeObjects()
    {
        RestoreSlotLabels();
        revealHeld = false;
        DestroyDividers();
        if (feedbackCanvas != null)
        {
            Destroy(feedbackCanvas.gameObject);
            feedbackCanvas = null;
            feedbackText = null;
            feedbackPanel = null;
        }
        foreach (ProfileDragCard card in spawnedCards)
        {
            if (card != null)
            {
                Destroy(card.gameObject);
            }
        }
        spawnedCards.Clear();
        autoCheckAt = -1f;
        // The cards leave the tray: the HUD profiles icon holds them again.
        if (CollectedProfilesWorldPanel.Instance != null) CollectedProfilesWorldPanel.Instance.SetProfilesOnBoard(false);

        if (slots != null)
        {
            foreach (CorkboardSlot slot in slots)
            {
                if (slot != null) slot.ResetRuntimeState();
            }
        }
    }

    private void DestroyDividers()
    {
        foreach (LineRenderer line in dividerLines)
        {
            if (line != null)
            {
                Destroy(line.gameObject);
            }
        }
        dividerLines.Clear();
        if (dividerMaterial != null)
        {
            Destroy(dividerMaterial);
            dividerMaterial = null;
        }
    }

    /// <summary>
    /// The side-quest reward. Uses the level score entry when one was awarded
    /// (rewards.json values); otherwise the legacy inspector values.
    /// </summary>
    public SideQuestResult BuildResult(ScoreEntry entry = null)
    {
        if (entry == null) return new SideQuestResult(rewardExp, rewardReputation);
        return new SideQuestResult(entry.Exp, entry.Hearts)
        {
            MaxExp = entry.MaxExp,
            MaxReputation = entry.MaxHearts,
            WrongPlacements = wrongPlacements,
            BadgeName = entry.BadgeName,
            BadgeTier = entry.BadgeTier
        };
    }
}
