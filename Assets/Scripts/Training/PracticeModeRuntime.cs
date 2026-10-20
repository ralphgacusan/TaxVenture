using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// PURPOSE:
/// A Tutorial Hub practice run inside the real level (Office_Level1). It
/// self-bootstraps only while TrainingSession.IsPracticing, like the other
/// runtime layers (no scene edits):
///
/// - Setup (after the level has started): office drills mark the side quest
///   done (door opens), mark their objectives.json steps done, swap in the
///   practice case, hand over the client's profile and put the player in the
///   reception. Every drill switches off the scene parts it never uses
///   ("hide"), so the run is lighter and feels like a quiet, separate place.
/// - A slim PRACTICE ribbon (card, GUIDED / MASTERY, step n / m, mastery
///   time) with a two-tap LEAVE, and a soft blue-grey vignette.
/// - When the drill's last step ends: the run is saved to the Training Hub
///   (never the career) and a result card offers the next run or the binder.
///
/// GuideRuntime plays the drill's lessons (TrainingSession.ComposeFor);
/// PlayerProgressManager ignores level results while practising.
/// </summary>
[DefaultExecutionOrder(120)]
public sealed class PracticeModeRuntime : MonoBehaviour
{
    private const int SortingOrder = 5200;
    private const float SetupTimeout = 6f;
    private static readonly Color Ribbon = new Color32(14, 24, 38, 230);
    private static readonly Color Vignette = new Color32(60, 86, 120, 255);
    private static readonly Color StampGreen = new Color32(66, 160, 98, 255);

    public static PracticeModeRuntime Instance { get; private set; }

    private TrainingDrill drill;
    private TrainingCardDef card;
    private bool mastery;
    private TrainingDrillScript.Result practice;
    private readonly HashSet<string> stepsDone = new HashSet<string>();
    private float startedAt = -1f;
    private bool finished;
    private float leaveArmedUntil = -1f;

    private GameObject canvasObject;
    private RectTransform safe;
    private TextMeshProUGUI stepLabel;
    private TextMeshProUGUI timeLabel;
    private TextMeshProUGUI leaveLabel;
    private RectTransform resultCard;
    private CanvasGroup resultGroup;
    private RectTransform stamp;
    private float resultShownAt;
    private bool requestedCompletionMusic;

    // =========================================================
    // BOOTSTRAP
    // =========================================================

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
        if (!TrainingSession.IsPracticing || !scene.IsValid() || !scene.isLoaded) return;
        if (scene.name != TrainingSession.Drill.scene) return;
        foreach (GameObject root in scene.GetRootGameObjects())
            if (root.GetComponentInChildren<PracticeModeRuntime>(true) != null) return;
        var host = new GameObject("Practice Mode Runtime");
        SceneManager.MoveGameObjectToScene(host, scene);
        host.AddComponent<PracticeModeRuntime>();
    }

    private void Awake()
    {
        Instance = this;
        drill = TrainingSession.Drill;
        card = TrainingSession.Card;
        mastery = TrainingSession.Mastery;
        if (drill == null || card == null)
        {
            enabled = false;
            return;
        }
        BuildOverlay();
    }

    private void OnEnable() => GuideDirector.LessonFinished += HandleLessonFinished;

    private void OnDisable() => GuideDirector.LessonFinished -= HandleLessonFinished;

    private IEnumerator Start()
    {
        if (drill == null) yield break;
        float until = Time.unscaledTime + SetupTimeout;
        while (Time.unscaledTime < until &&
               (CaseProgressionManager.Instance == null || !CaseProgressionManager.Instance.LevelStarted ||
                OfficeObjectives.Instance == null || CameraController.Instance == null))
            yield return null;
        yield return null;

        practice = GuideRuntime.Instance != null ? GuideRuntime.Instance.Practice : null;
        if (practice == null || practice.FinalStep == null)
            Debug.LogWarning($"[Training] Drill \"{drill.id}\" has no playable steps; LEAVE returns to the binder.");

        SetUpLevel();
        HideUnusedParts();
        startedAt = Time.unscaledTime;
        RefreshRibbon();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        PlayerControlHold.Release(this);
        if (requestedCompletionMusic && AudioManager.Instance != null)
            AudioManager.Instance.SetCompletionMusic(false);
        if (canvasObject != null) Destroy(canvasObject);
    }

    // =========================================================
    // SETUP
    // =========================================================

    private void SetUpLevel()
    {
        if (drill.StartsInOffice) LevelGate.MarkSideQuestDone();

        OfficeObjectives objectives = OfficeObjectives.Instance;
        if (objectives != null)
            foreach (string step in drill.objectivesDone)
                objectives.MarkDone(step);

        if (!string.IsNullOrWhiteSpace(drill.practiceCase) && CaseManager.Instance != null)
            CaseManager.Instance.LoadCase(drill.levelId, drill.practiceCase.Trim());

        TrainingProfile profile = drill.profile;
        if (profile != null && !string.IsNullOrWhiteSpace(profile.name) && TaxpayerProfileCollectionHUD.Instance != null)
        {
            if (!System.Enum.TryParse(profile.classification, out TaxpayerClassification classification))
                classification = TaxpayerClassification.ResidentCitizen;
            TaxpayerProfileCollectionHUD.Instance.CollectProfile(new TaxpayerProfile(
                string.IsNullOrWhiteSpace(profile.id) ? "profile_practice" : profile.id,
                profile.name, profile.description ?? "", classification, profile.npcId));
        }

        if (drill.StartsInOffice) PlacePlayer();
    }

    /// <summary>Stands the player on the floor between "spawn" and "face", looking at "face".</summary>
    private void PlacePlayer() => PlayerPlacement.PlaceBetween(gameObject.scene, drill.spawn, drill.face);

    /// <summary>Switches off the scene parts this drill never uses (after every Start has run).</summary>
    private void HideUnusedParts()
    {
        int hidden = 0;
        foreach (string path in drill.hide)
        {
            Transform t = Find(path);
            if (t == null)
            {
                Debug.LogWarning($"[Training] Drill \"{drill.id}\": hide \"{path}\" not found.");
                continue;
            }
            if (!t.gameObject.activeSelf) continue;
            t.gameObject.SetActive(false);
            hidden++;
        }
        if (hidden > 0) Debug.Log($"[Training] Drill \"{drill.id}\": switched off {hidden} unused part(s) of the level.");
    }

    private Transform Find(string path) => PlayerPlacement.Find(gameObject.scene, path);

    // =========================================================
    // PROGRESS
    // =========================================================

    private void HandleLessonFinished(string levelId, string lessonId)
    {
        if (finished) return;
        if (practice == null) practice = GuideRuntime.Instance != null ? GuideRuntime.Instance.Practice : null;
        if (practice == null) return;
        if (practice.Steps.Contains(lessonId)) stepsDone.Add(lessonId);
        RefreshRibbon();
        if (lessonId == practice.FinalStep) Finish();
    }

    private void Update()
    {
        if (drill == null) return;
        if (!finished && startedAt >= 0f && mastery && timeLabel != null)
            timeLabel.text = Clock(Time.unscaledTime - startedAt);
        if (leaveArmedUntil > 0f && Time.unscaledTime > leaveArmedUntil) DisarmLeave();
        AnimateResult();
    }

    private void LateUpdate()
    {
        // Nothing underneath (a reward card) may move on while the result is up.
        if (finished) HudRewardScreen.HoldAutoContinue = true;
    }

    private void Finish()
    {
        finished = true;
        float seconds = startedAt >= 0f ? Time.unscaledTime - startedAt : 0f;
        StopGuide();
        PlayerControlHold.Acquire(this);

        bool newlyMastered = false;
        TrainingRecord record = null;
        PlayerProgressManager progress = PlayerProgressManager.Instance;
        if (progress != null)
        {
            newlyMastered = progress.RecordTrainingRun(card.id, mastery, seconds);
            record = progress.CurrentProgress != null ? progress.CurrentProgress.GetTrainingRecord(card.id) : null;
        }
        if (AudioManager.Instance != null) AudioManager.Instance.PlayAchievementSFX();
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetCompletionMusic(true);
            requestedCompletionMusic = true;
        }
        ShowResult(seconds, record, newlyMastered);
    }

    private static void StopGuide()
    {
        GuideRuntime runtime = GuideRuntime.Instance;
        GuideDirector director = runtime != null ? runtime.Director : null;
        if (director == null) return;
        director.enabled = false;
        GuideCoachView view = director.View;
        if (view == null) return;
        view.HideBox();
        view.HideHint();
        view.Hand.Hide();
        view.SetHole(false, default);
        view.SetBlocking(false);
    }

    // =========================================================
    // OVERLAY (ribbon, vignette, result card)
    // =========================================================

    private void BuildOverlay()
    {
        canvasObject = new GameObject("Practice Canvas", typeof(RectTransform));
        SceneManager.MoveGameObjectToScene(canvasObject, gameObject.scene);
        canvasObject.layer = LayerMask.NameToLayer("UI");
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = SortingOrder;
        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        canvasObject.AddComponent<GraphicRaycaster>();
        canvasObject.AddComponent<HudKeepVisibleDuringRewards>();

        BuildVignette((RectTransform)canvasObject.transform);
        safe = HudTheme.CreateRect("Safe Area", canvasObject.transform);
        HudTheme.Stretch(safe);
        safe.gameObject.AddComponent<MobileControlsSafeArea>();
        BuildRibbon();
    }

    private static void BuildVignette(RectTransform parent)
    {
        // Four soft edges, never touchable: "this is practice, not the real day".
        float[,] edges = { { 0f, 0f, 1f, 0.06f }, { 0f, 0.94f, 1f, 1f }, { 0f, 0f, 0.035f, 1f }, { 0.965f, 0f, 1f, 1f } };
        for (int i = 0; i < 4; i++)
        {
            RectTransform edge = HudTheme.CreateRect("Vignette", parent);
            edge.anchorMin = new Vector2(edges[i, 0], edges[i, 1]);
            edge.anchorMax = new Vector2(edges[i, 2], edges[i, 3]);
            edge.offsetMin = edge.offsetMax = Vector2.zero;
            HudTheme.AddImage(edge, null, new Color(Vignette.r, Vignette.g, Vignette.b, 0.16f));
        }
    }

    private void BuildRibbon()
    {
        RectTransform ribbon = HudTheme.CreateRect("Practice Ribbon", safe);
        ribbon.anchorMin = ribbon.anchorMax = new Vector2(0.5f, 0f);
        ribbon.pivot = new Vector2(0.5f, 0f);
        // Under the interaction prompt (bottom centre, 70 up), between the two thumb areas.
        ribbon.sizeDelta = new Vector2(760f, 52f);
        ribbon.anchoredPosition = new Vector2(0f, 8f);
        HudTheme.AddImage(ribbon, HudTheme.Rounded, new Color(HudTheme.Brass.r, HudTheme.Brass.g, HudTheme.Brass.b, 0.9f), 26f);
        RectTransform body = HudTheme.CreateRect("Body", ribbon);
        HudTheme.Stretch(body, 2f);
        HudTheme.AddImage(body, HudTheme.Rounded, Ribbon, 24f);

        TrainingTabDef tab = null;
        TrainingCatalogFile catalog = TrainingCatalogFile.Load();
        if (catalog != null) tab = catalog.FindTab(card.tab);
        Color accent = tab != null ? tab.ParsedColor : HudTheme.Active;

        RectTransform chip = HudTheme.CreateRect("Chip", body);
        chip.anchorMin = chip.anchorMax = new Vector2(0f, 0.5f);
        chip.pivot = new Vector2(0f, 0.5f);
        chip.sizeDelta = new Vector2(150f, 36f);
        chip.anchoredPosition = new Vector2(8f, 0f);
        HudTheme.AddImage(chip, HudTheme.Rounded, accent, 18f);
        TextMeshProUGUI chipLabel = HudTheme.CreateLabel("Label", chip, mastery ? "MASTERY" : "PRACTICE", 22f,
            HudTheme.PanelInner, TextAlignmentOptions.Center, FontStyles.Bold);
        HudTheme.Stretch((RectTransform)chipLabel.transform, 4f);
        chipLabel.characterSpacing = 3f;

        TextMeshProUGUI title = HudTheme.CreateLabel("Title", body, (card.title ?? "").ToUpperInvariant(), 24f,
            HudTheme.TextPrimary, TextAlignmentOptions.MidlineLeft, FontStyles.Bold);
        RectTransform tr = (RectTransform)title.transform;
        tr.anchorMin = new Vector2(0f, 0f);
        tr.anchorMax = new Vector2(1f, 1f);
        tr.offsetMin = new Vector2(172f, 0f);
        tr.offsetMax = new Vector2(-330f, 0f);

        stepLabel = HudTheme.CreateLabel("Steps", body, "", 22f, HudTheme.BrassSoft, TextAlignmentOptions.Center, FontStyles.Bold);
        RectTransform sr = (RectTransform)stepLabel.transform;
        sr.anchorMin = sr.anchorMax = new Vector2(1f, 0.5f);
        sr.pivot = new Vector2(1f, 0.5f);
        sr.sizeDelta = new Vector2(110f, 40f);
        sr.anchoredPosition = new Vector2(-218f, 0f);

        timeLabel = HudTheme.CreateLabel("Time", body, "", 22f, HudTheme.TextMuted, TextAlignmentOptions.Center, FontStyles.Bold);
        RectTransform timeRect = (RectTransform)timeLabel.transform;
        timeRect.anchorMin = timeRect.anchorMax = new Vector2(1f, 0.5f);
        timeRect.pivot = new Vector2(1f, 0.5f);
        timeRect.sizeDelta = new Vector2(80f, 40f);
        timeRect.anchoredPosition = new Vector2(-136f, 0f);
        timeLabel.gameObject.SetActive(mastery);

        Button leave = MakeButton(body, "Leave", "LEAVE", new Color32(48, 62, 80, 255), HudTheme.TextPrimary,
            new Vector2(118f, 38f), 20f, out leaveLabel);
        RectTransform lr = (RectTransform)leave.transform;
        lr.anchorMin = lr.anchorMax = new Vector2(1f, 0.5f);
        lr.pivot = new Vector2(1f, 0.5f);
        lr.anchoredPosition = new Vector2(-8f, 0f);
        leave.onClick.AddListener(OnLeave);
    }

    private void RefreshRibbon()
    {
        if (stepLabel == null) return;
        int total = practice != null ? practice.Steps.Count : 0;
        stepLabel.text = total > 0 ? $"STEP {Mathf.Min(stepsDone.Count + 1, total)}/{total}" : "";
        if (finished) stepLabel.text = "DONE";
    }

    private void OnLeave()
    {
        if (Time.unscaledTime <= leaveArmedUntil)
        {
            TrainingSession.ReturnToHub(false, false);
            return;
        }
        leaveArmedUntil = Time.unscaledTime + 3f;
        leaveLabel.text = "SURE?";
        leaveLabel.color = HudTheme.Exp;
    }

    private void DisarmLeave()
    {
        leaveArmedUntil = -1f;
        if (leaveLabel == null) return;
        leaveLabel.text = "LEAVE";
        leaveLabel.color = HudTheme.TextPrimary;
    }

    private void ShowResult(float seconds, TrainingRecord record, bool newlyMastered)
    {
        RefreshRibbon();
        RectTransform dim = HudTheme.CreateRect("Result Dim", safe.parent);
        HudTheme.Stretch(dim);
        Image dimImage = HudTheme.AddImage(dim, null, new Color(0.02f, 0.04f, 0.07f, 0.62f));
        dimImage.raycastTarget = true;   // nothing underneath can be touched now
        dim.SetAsLastSibling();

        resultCard = HudTheme.CreateRect("Result Card", dim);
        resultCard.anchorMin = resultCard.anchorMax = new Vector2(0.5f, 0.5f);
        resultCard.sizeDelta = new Vector2(820f, 560f);
        resultGroup = resultCard.gameObject.AddComponent<CanvasGroup>();
        resultGroup.alpha = 0f;
        RectTransform body = HudTheme.CreatePanel("Panel", resultCard, 28f);
        HudTheme.Stretch((RectTransform)body.parent);

        string heading = mastery ? "DRILL MASTERED" : "DRILL COMPLETE";
        TextMeshProUGUI head = HudTheme.CreateLabel("Heading", body, heading, 54f, HudTheme.Exp,
            TextAlignmentOptions.Center, FontStyles.Bold);
        Place(head.rectTransform, 0f, 200f, 720f, 70f);
        head.characterSpacing = 4f;

        TextMeshProUGUI name = HudTheme.CreateLabel("Card", body, card.title ?? "", 34f, HudTheme.TextPrimary,
            TextAlignmentOptions.Center, FontStyles.Bold);
        Place(name.rectTransform, 0f, 132f, 720f, 48f);

        string detail;
        if (mastery)
        {
            int best = record != null ? record.bestMasterySeconds : 0;
            detail = $"Time {Clock(seconds)}" + (best > 0 ? $"   ·   Best {Clock(best)}" : "");
        }
        else
        {
            detail = "Nicely done! Ready to try it without my hand?";
        }
        TextMeshProUGUI info = HudTheme.CreateLabel("Detail", body, detail, 28f, HudTheme.TextMuted, TextAlignmentOptions.Center);
        Place(info.rectTransform, 0f, 78f, 720f, 44f);

        if (mastery) BuildStamp(body, newlyMastered);
        else
        {
            TextMeshProUGUI tip = HudTheme.CreateLabel("Tip", body,
                "A MASTERY run has goals only: no hand, no spotlight.\nFinish it to earn the MASTERED stamp.", 26f,
                HudTheme.TextPrimary, TextAlignmentOptions.Center);
            tip.textWrappingMode = TextWrappingModes.Normal;
            Place(tip.rectTransform, 0f, -20f, 700f, 110f);
        }

        Button primary, secondary;
        if (mastery)
        {
            primary = MakeButton(body, "Back", "BACK TO BINDER", HudTheme.Brass, HudTheme.PanelInner, new Vector2(330f, 82f), 28f, out _);
            secondary = MakeButton(body, "Again", "TRY AGAIN", new Color32(48, 62, 80, 255), HudTheme.TextPrimary, new Vector2(250f, 82f), 26f, out _);
            primary.onClick.AddListener(() => TrainingSession.ReturnToHub(true, newlyMastered));
            secondary.onClick.AddListener(() => TrainingSession.Restart(true));
        }
        else
        {
            primary = MakeButton(body, "Mastery", "MASTERY RUN", StampGreen, Color.white, new Vector2(330f, 82f), 28f, out _);
            secondary = MakeButton(body, "Back", "BACK TO BINDER", new Color32(48, 62, 80, 255), HudTheme.TextPrimary, new Vector2(290f, 82f), 24f, out _);
            primary.onClick.AddListener(() => TrainingSession.Restart(true));
            secondary.onClick.AddListener(() => TrainingSession.ReturnToHub(true, false));
        }
        Place((RectTransform)primary.transform, 170f, -200f, 0f, 0f, keepSize: true);
        Place((RectTransform)secondary.transform, -180f, -200f, 0f, 0f, keepSize: true);
        resultShownAt = Time.unscaledTime;
    }

    private void BuildStamp(RectTransform body, bool fresh)
    {
        stamp = HudTheme.CreateRect("Mastered Stamp", body);
        stamp.anchorMin = stamp.anchorMax = new Vector2(0.5f, 0.5f);
        stamp.sizeDelta = new Vector2(420f, 120f);
        stamp.anchoredPosition = new Vector2(0f, -40f);
        stamp.localRotation = Quaternion.Euler(0f, 0f, 8f);
        HudTheme.AddImage(stamp, HudTheme.Rounded, StampGreen, 18f);
        RectTransform inner = HudTheme.CreateRect("Inner", stamp);
        HudTheme.Stretch(inner, 7f);
        HudTheme.AddImage(inner, HudTheme.Rounded, HudTheme.PanelFill, 13f);
        TextMeshProUGUI label = HudTheme.CreateLabel("Label", inner, "MASTERED", 64f, StampGreen, TextAlignmentOptions.Center, FontStyles.Bold);
        HudTheme.Stretch((RectTransform)label.transform, 6f);
        label.characterSpacing = 8f;
        if (!fresh) stamp.localScale = Vector3.one;
        else stamp.localScale = Vector3.one * 2.4f;   // slammed in by AnimateResult
    }

    private void AnimateResult()
    {
        if (resultGroup == null) return;
        float t = Time.unscaledTime - resultShownAt;
        resultGroup.alpha = Mathf.Clamp01(t / 0.25f);
        float pop = HudTheme.EaseOutBack(t / 0.35f);
        resultCard.localScale = Vector3.one * Mathf.Lerp(0.86f, 1f, pop);
        if (stamp != null && stamp.localScale.x > 1.001f)
        {
            float k = Mathf.Clamp01((t - 0.35f) / 0.22f);
            stamp.localScale = Vector3.one * Mathf.Lerp(2.4f, 1f, k * k);
        }
    }

    private static Button MakeButton(RectTransform parent, string name, string text, Color face, Color ink, Vector2 size,
        float fontSize, out TextMeshProUGUI label)
    {
        RectTransform rect = HudTheme.CreateRect(name, parent);
        rect.sizeDelta = size;
        Image image = HudTheme.AddImage(rect, HudTheme.Rounded, face, size.y * 0.5f);
        image.raycastTarget = true;
        Button button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        label = HudTheme.CreateLabel("Label", rect, text, fontSize, ink, TextAlignmentOptions.Center, FontStyles.Bold);
        HudTheme.Stretch((RectTransform)label.transform, 8f);
        label.characterSpacing = 2f;
        return button;
    }

    private static void Place(RectTransform rect, float x, float y, float width, float height, bool keepSize = false)
    {
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        if (!keepSize) rect.sizeDelta = new Vector2(width, height);
        rect.anchoredPosition = new Vector2(x, y);
    }

    /// <summary>"1:23" (drills are short).</summary>
    private static string Clock(float seconds)
    {
        int total = Mathf.Max(0, Mathf.RoundToInt(seconds));
        return $"{total / 60}:{total % 60:00}";
    }
}
