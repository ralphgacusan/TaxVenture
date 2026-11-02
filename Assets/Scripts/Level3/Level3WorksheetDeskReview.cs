using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// PURPOSE:
/// The worksheet's deliberate check, hints and collection at Bea's table
/// (Level 3 T06), the second half of Level3WorksheetDesk (one component,
/// one input owner).
///
/// CHECK: filling in the last field never checks anything. CHECK first looks
/// for missing work: an incomplete worksheet circles what is missing in
/// pencil and says so; nothing is graded or saved. The first complete check
/// asks once ("CHECK NOW?": it sets the grade, corrections stay possible),
/// then Level3Checkpointer.CheckWorksheet saves it, and only then a pencil
/// ticks every place in turn (brass tick = confirmed, graphite "?" = look
/// again) and the first-check receipt counts up to the frozen grade before
/// it folds into the records column. Later checks only refresh the marks.
/// Any tap skips the reveal; the saved result is already complete.
///
/// HINT: after the first check, for the selected (or first) marked place:
/// three steps from where to look (and the Tax Code Book section) to
/// specific help. Hints are free and never saved.
///
/// COLLECT: once every decision checks out, COLLECT WORKSHEET saves the
/// collection, the 60 coins, +2 Public Trust and the mastery award in one
/// write (Level3Checkpointer.CollectWorksheet). Only after that save does the
/// CHECKED board fly into the Case Folder, the XP/badge and trust/coin
/// rewards announce themselves and the Side Quest Complete card shows; the
/// player then stands up from the table. A failed save keeps the worksheet
/// on the table, ready to collect again. Leaving mid-flight finishes the
/// presentation at once (the save already happened).
/// </summary>
public sealed partial class Level3WorksheetDesk
{
    public const string CueChecked = "WorksheetChecked";
    public const string CueHint = "WorksheetHint";

    private enum RevealStage { None, Pass, Count, Hold, Fold }

    private const float PassStep = 0.11f;
    private const float CountStep = 0.065f;
    private const float HoldSeconds = 1.8f;
    private const float FoldSeconds = 0.38f;
    private const float NeededSeconds = 6f;
    private const float StampHoldSeconds = 0.5f;
    private const float FlySeconds = 0.62f;
    private const float CardDelaySeconds = 0.8f;

    private bool checkArmed;
    private string hintFocus;
    private readonly Dictionary<string, int> hintLevels = new Dictionary<string, int>();

    private RevealStage reveal;
    private float revealStart;
    private int revealStep;
    private bool revealFirst;
    private Level3WorksheetEvaluation revealEval;

    private int shownReviewRevision = -1, shownReviewPhase = -1, shownReviewChecks = -1, shownReviewTapes = -1;

    private bool collecting;
    private float collectStart;
    private bool awardShown;
    private bool flightSoundPlayed;
    private float cardAt = -1f;
    private Level3WorksheetGrants collectGrants;
    private Pose collectFrom;
    private Vector3 collectOrigin;

    /// <summary>The check's reveal is playing (a tap skips it).</summary>
    private bool Revealing => reveal != RevealStage.None;

    private static LevelRewardRules Rules =>
        CaseProgressionManager.Instance != null && CaseProgressionManager.Instance.Score != null
            ? CaseProgressionManager.Instance.Score.Rules : LevelRewardRules.Load(Level3ChapterRules.LevelId);

    private static Sprite MedalFor(int tier) => tier > 0 ? HudBadgeArt.For(Rules, (BadgeTier)tier) : null;

    // =========================================================
    // REVIEW STATE ON THE BOARD (quiet: restores never animate or announce)
    // =========================================================

    private bool ReviewChanged(Level3ChapterCheckpoint chapter)
    {
        int revision = chapter?.worksheet != null ? chapter.worksheet.revision : 0;
        int phase = (int)Level3ChapterRules.PhaseOf(chapter);
        int checks = chapter?.worksheetCheck != null ? chapter.worksheetCheck.checks : 0;
        int tapes = chapter?.tapes != null ? chapter.tapes.Count : 0;
        return revision != shownReviewRevision || phase != shownReviewPhase || checks != shownReviewChecks || tapes != shownReviewTapes;
    }

    /// <summary>Tapes, marks, the first-check stub, the stamp and the key row from the published chapter.</summary>
    private void RefreshReview(Level3ChapterCheckpoint chapter, bool animate)
    {
        if (model == null) return;
        shownReviewRevision = chapter?.worksheet != null ? chapter.worksheet.revision : 0;
        shownReviewPhase = (int)Level3ChapterRules.PhaseOf(chapter);
        shownReviewChecks = chapter?.worksheetCheck != null ? chapter.worksheetCheck.checks : 0;
        shownReviewTapes = chapter?.tapes != null ? chapter.tapes.Count : 0;
        model.SyncSubtotals(chapter, flySection, animate);
        if (!Revealing)
        {
            model.SetMarks(id => Level3WorksheetCheck.MarkFor(chapter, id), animate: false);
            model.ShowStub(chapter?.worksheetCheck, MedalFor(chapter?.worksheetCheck != null ? chapter.worksheetCheck.firstTier : 0));
            model.SetStamp(Level3ChapterRules.PhaseOf(chapter) == Level3Phase.WorksheetReady || Level3WorksheetCheck.Collected(chapter), animate: false);
        }
        RefreshKeys();
    }

    private void RefreshKeys()
    {
        if (model == null) return;
        Level3ChapterCheckpoint chapter = Chapter;
        Level3Phase phase = Level3ChapterRules.PhaseOf(chapter);
        bool editable = Level3WorksheetRules.Editable(chapter);
        bool first = chapter?.worksheetCheck != null && chapter.worksheetCheck.firstChecked;
        Level3WorksheetModel.CheckKey key = phase == Level3Phase.WorksheetReady ? Level3WorksheetModel.CheckKey.Collect
            : !editable ? Level3WorksheetModel.CheckKey.Hidden
            : checkArmed ? Level3WorksheetModel.CheckKey.Confirm
            : first ? Level3WorksheetModel.CheckKey.Again : Level3WorksheetModel.CheckKey.Check;
        model.SetKeys(key, editable && first && FirstMarked(chapter) != null, editable && chapter?.worksheet?.undo != null && chapter.worksheet.undo.Count > 0);
    }

    private void DisarmCheck()
    {
        if (!checkArmed) return;
        checkArmed = false;
        RefreshKeys();
    }

    // =========================================================
    // CHECK
    // =========================================================

    private void TapCheck()
    {
        Level3Checkpointer owner = Level3Checkpointer.Active;
        Level3ChapterCheckpoint chapter = Chapter;
        if (owner == null || chapter == null) return;
        if (!Level3WorksheetRules.Editable(chapter)) { Refused("The worksheet cannot be checked right now."); return; }
        bool first = chapter.worksheetCheck == null || !chapter.worksheetCheck.firstChecked;
        if (first && !checkArmed)
        {
            // Completeness first: an incomplete worksheet never reaches the scoring question.
            Level3WorksheetEvaluation preview = Level3WorksheetCheck.Evaluate(chapter, Definition);
            if (!preview.Complete) { ShowIncomplete(preview); return; }
            checkArmed = true;
            RefreshKeys();
            model.SetMessage("Your first complete check sets Bea's worksheet grade (12 decisions). You can still fix things afterwards, but the grade stays. Tap CHECK NOW? to check.");
            if (AudioManager.Instance != null) AudioManager.Instance.PlaySelectSFX();
            return;
        }
        checkArmed = false;
        Level3Checkpointer.CheckCommit outcome = owner.CheckWorksheet(out Level3WorksheetEvaluation eval, out string hint);
        switch (outcome)
        {
            case Level3Checkpointer.CheckCommit.Checked:
                StartReveal(eval, first);
                break;
            case Level3Checkpointer.CheckCommit.Incomplete:
                ShowIncomplete(eval);
                break;
            case Level3Checkpointer.CheckCommit.Unchanged:
                model.SetMessage(hint + " Change a place marked ?, then CHECK AGAIN.");
                if (AudioManager.Instance != null) AudioManager.Instance.PlayMouseClickSelectSFX();
                RefreshKeys();
                break;
            default:
                RefreshKeys();
                Refused(hint);
                break;
        }
    }

    /// <summary>Not checked: what is missing, circled in pencil and in one plain line. Nothing was graded.</summary>
    private void ShowIncomplete(Level3WorksheetEvaluation eval)
    {
        if (eval == null) return;
        model.ShowNeeded(eval.MissingPlaces, NeededSeconds);
        string first = eval.MissingLines.Count > 0 ? eval.MissingLines[0] : "";
        string more = eval.MissingLines.Count > 1 ? " (and " + (eval.MissingLines.Count - 1) + " more, circled in pencil)" : " (circled in pencil)";
        model.SetMessage("Not checked yet, so nothing was graded. Still needed: " + first + more);
        if (AudioManager.Instance != null) AudioManager.Instance.PlayMouseClickSelectSFX();
        RefreshKeys();
    }

    private void StartReveal(Level3WorksheetEvaluation eval, bool first)
    {
        if (eval == null || eval.Decisions.Count == 0) { FinishReveal(); return; }
        ClosePicker();
        Select(null, quiet: true);
        model.ClearNeeded();
        model.SetMarks(null, animate: false);
        reveal = RevealStage.Pass;
        revealStart = Time.unscaledTime;
        revealStep = 0;
        revealFirst = first;
        revealEval = eval;
        hintLevels.Clear();
        model.SetMessage(first ? "Checking all 12 decisions..." : "Checking again...");
        if (AudioManager.Instance != null) AudioManager.Instance.PlayPaperSFX();
    }

    /// <summary>The saved check, revealed: the pencil ticks each place, then (first check) the receipt counts up and folds away.</summary>
    private void TickReview()
    {
        if (!Revealing || model == null) return;
        float now = Time.unscaledTime;
        float elapsed = now - revealStart;
        List<Level3WorksheetDecision> decisions = revealEval.Decisions;
        switch (reveal)
        {
            case RevealStage.Pass:
            {
                // Each step: the pencil travels to the next place (60%), dips, and its mark appears.
                int step = Mathf.Min(decisions.Count - 1, (int)(elapsed / PassStep));
                float u = Mathf.Clamp01((elapsed - step * PassStep) / PassStep);
                Vector3 target = model.MarkLocal(Level3WorksheetCheck.PlaceOf(decisions[step].Id));
                Vector3 previous = step > 0 ? model.MarkLocal(Level3WorksheetCheck.PlaceOf(decisions[step - 1].Id)) : target + new Vector3(0.03f, 0f, 0.03f);
                float travel = Mathf.Clamp01(u / 0.6f);
                travel = travel * travel * (3f - 2f * travel);
                model.SetPencil(Vector3.Lerp(previous, target, travel), u < 0.6f ? 0f : Mathf.Sin((u - 0.6f) / 0.4f * Mathf.PI));
                int due = elapsed >= decisions.Count * PassStep ? decisions.Count : step + (u >= 0.75f ? 1 : 0);
                while (revealStep < due)
                {
                    Level3WorksheetDecision d = decisions[revealStep];
                    model.SetMark(d.Id, d.Correct ? Level3WorksheetCheck.Mark.Confirmed : Level3WorksheetCheck.Mark.LookAgain, animate: true);
                    if (AudioManager.Instance != null && revealStep % 2 == 0) AudioManager.Instance.PlayMouseClickSelectSFX();
                    revealStep++;
                }
                if (elapsed < decisions.Count * PassStep + 0.25f) return;
                model.SetPencil(null, 0f);
                if (!revealFirst) { FinishReveal(); return; }
                Level3WorksheetResult result = Chapter?.worksheetCheck;
                model.OpenReceipt(CategoryLines(revealEval), MedalFor(result != null ? result.firstTier : 0));
                reveal = RevealStage.Count;
                revealStart = now;
                revealStep = 0;
                return;
            }
            case RevealStage.Count:
            {
                int target = revealEval.Correct;
                int due = Mathf.Min(target, (int)(elapsed / CountStep));
                while (revealStep < due)
                {
                    revealStep++;
                    model.SetReceiptCount(revealStep);
                    if (AudioManager.Instance != null) AudioManager.Instance.PlayKeyboardSFX();
                }
                if (elapsed < target * CountStep + 0.2f) return;
                Level3WorksheetResult result = Chapter?.worksheetCheck;
                int tier = result != null ? result.firstTier : Level3WorksheetCheck.TierFor(revealEval.Correct);
                model.SetReceiptCount(revealEval.Correct);
                model.RevealReceiptTier(tier, tier >= 3 ? "Every decision right on the first check." : "Corrections never change this grade.");
                if (AudioManager.Instance != null) AudioManager.Instance.PlayAchievementSFX();
                reveal = RevealStage.Hold;
                revealStart = now;
                return;
            }
            case RevealStage.Hold:
                if (elapsed < HoldSeconds) return;
                reveal = RevealStage.Fold;
                revealStart = now;
                if (AudioManager.Instance != null) AudioManager.Instance.PlayPaperSFX();
                return;
            case RevealStage.Fold:
                model.FoldReceipt(Mathf.Clamp01(elapsed / FoldSeconds));
                if (elapsed >= FoldSeconds) FinishReveal();
                return;
        }
    }

    /// <summary>Skips what is left of a reveal (a tap, leaving the table): the saved result shows at once.</summary>
    private void SkipReveal()
    {
        if (!Revealing) return;
        FinishReveal();
    }

    private void FinishReveal()
    {
        bool first = revealFirst;
        Level3WorksheetEvaluation eval = revealEval;
        reveal = RevealStage.None;
        revealEval = null;
        if (model == null) return;
        model.SetPencil(null, 0f);
        model.CloseReceipt();
        Level3ChapterCheckpoint chapter = Chapter;
        bool ready = Level3ChapterRules.PhaseOf(chapter) == Level3Phase.WorksheetReady;
        model.SetMarks(id => Level3WorksheetCheck.MarkFor(chapter, id), animate: false);
        model.ShowStub(chapter?.worksheetCheck, MedalFor(chapter?.worksheetCheck != null ? chapter.worksheetCheck.firstTier : 0));
        model.Sync(chapter?.worksheet, snap: false);
        bool stamped = model.StampVisible;
        model.SetStamp(ready, animate: true);
        if (ready && !stamped && AudioManager.Instance != null) AudioManager.Instance.PlayEffect("sfx_stamp_press_01", 0.8f);
        RefreshKeys();
        if (eval == null || chapter?.worksheetCheck == null) return;
        Level3WorksheetResult result = chapter.worksheetCheck;
        string tier = Level3WorksheetCheck.TierName(result.firstTier);
        string say;
        if (first)
            say = result.firstTier >= 3 ? "12 out of 12 on your first check: Gold! Every decision is right."
                : result.firstCorrect + "/" + result.firstTotal + " on your first check: " + tier + ". Places marked ? need another look.";
        else
            say = ready ? "Every decision checks out now. Your first-check grade stays " + tier + "."
                : eval.Wrong + (eval.Wrong == 1 ? " place still needs" : " places still need") + " another look. Your first-check grade stays " + tier + ".";
        model.SetMessage(say + " " + SkillNote(eval, ready));
        if (!GuideVoice.TrySay(say, first && result.firstTier >= 3 ? "wow" : null, 6f)) LevelGate.NotifyBlocked(say);
        GameplayEvents.RaiseGuideCue(CueChecked);
    }

    /// <summary>Keeps treatment and arithmetic apart in plain words (a matching tape is never another treatment error).</summary>
    private static string SkillNote(Level3WorksheetEvaluation eval, bool ready)
    {
        if (ready) return "Tap COLLECT WORKSHEET.";
        int sections = eval.CorrectIn("dest-", out int sectionTotal);
        int tapes = eval.CorrectIn("subtotal-", out int tapeTotal);
        if (tapes == tapeTotal && sections < sectionTotal)
            return "Your tapes add up exactly what is in their sections; the strips marked ? need another look. Tap one, then HINT.";
        if (tapes < tapeTotal)
            return "A tape marked ? doesn't add exactly the strips in its section, each once. Tap HINT for help.";
        return "Tap HINT for help with a marked place.";
    }

    private static string CategoryLines(Level3WorksheetEvaluation eval)
    {
        int d = eval.CorrectIn("dest-", out int dt);
        int r = eval.CorrectIn("reason-", out int rt);
        int l = eval.CorrectIn("related-", out int lt);
        int s = eval.CorrectIn("subtotal-", out int st);
        return $"Sections {d}/{dt}   Reasons {r}/{rt}\nLink {l}/{lt}   Subtotals {s}/{st}";
    }

    // =========================================================
    // HINTS (free, never saved)
    // =========================================================

    private static string FirstMarked(Level3ChapterCheckpoint chapter)
    {
        foreach (string id in Level3WorksheetCheck.DecisionIds)
            if (Level3WorksheetCheck.MarkFor(chapter, id) == Level3WorksheetCheck.Mark.LookAgain) return id;
        return null;
    }

    private void TapHint()
    {
        Level3ChapterCheckpoint chapter = Chapter;
        if (chapter?.worksheetCheck == null || !chapter.worksheetCheck.firstChecked)
        {
            model.SetMessage("Hints open after your first check, for the places it marks with ?.");
            if (AudioManager.Instance != null) AudioManager.Instance.PlayMouseClickSelectSFX();
            return;
        }
        string id = hintFocus != null && Level3WorksheetCheck.MarkFor(chapter, hintFocus) == Level3WorksheetCheck.Mark.LookAgain ? hintFocus : null;
        if (id != null && hintLevels.TryGetValue(id, out int used) && used >= Level3WorksheetHints.Levels)
        {
            // This place has had every hint: move on to the next marked one.
            string next = NextMarked(chapter, id);
            if (next != null) id = next;
        }
        if (id == null) id = FirstMarked(chapter);
        if (id == null)
        {
            model.SetMessage("Nothing is marked right now. Change a marked place and CHECK AGAIN, or collect when it all checks out.");
            if (AudioManager.Instance != null) AudioManager.Instance.PlayMouseClickSelectSFX();
            return;
        }
        int level = Mathf.Min(Level3WorksheetHints.Levels, (hintLevels.TryGetValue(id, out int had) ? had : 0) + 1);
        hintLevels[id] = level;
        hintFocus = id;
        model.PopMark(id);
        if (id.StartsWith("dest-")) model.SetSelectedStrip(id.Substring(5));
        model.SetMessage("Hint " + level + "/" + Level3WorksheetHints.Levels + " · " + PlaceName(id) + ": " + Level3WorksheetHints.For(id, level));
        if (AudioManager.Instance != null) AudioManager.Instance.PlayPaperSFX();
        GameplayEvents.RaiseGuideCue(CueHint);
    }

    private static string NextMarked(Level3ChapterCheckpoint chapter, string after)
    {
        int start = System.Array.IndexOf(Level3WorksheetCheck.DecisionIds, after);
        for (int i = 1; i <= Level3WorksheetCheck.DecisionIds.Length; i++)
        {
            string id = Level3WorksheetCheck.DecisionIds[(start + i) % Level3WorksheetCheck.DecisionIds.Length];
            if (id != after && Level3WorksheetCheck.MarkFor(chapter, id) == Level3WorksheetCheck.Mark.LookAgain) return id;
        }
        return null;
    }

    private string PlaceName(string decisionId)
    {
        if (decisionId.StartsWith("dest-")) return model.Row(decisionId.Substring(5))?.Label ?? decisionId;
        switch (decisionId)
        {
            case "reason-R6": return "Party purchases note";
            case "reason-R7": return "Printing bill note";
            case "related-R7": return "Printing bill note, related entry";
            case "subtotal-income": return "Income tape";
            case "subtotal-expenses": return "Allowable expenses tape";
            default: return decisionId;
        }
    }

    // =========================================================
    // COLLECT (saved first; every effect after)
    // =========================================================

    private void TapCollect()
    {
        if (collecting) return;
        Level3Checkpointer owner = Level3Checkpointer.Active;
        if (owner == null) return;
        Level3Checkpointer.CollectCommit outcome = owner.CollectWorksheet(out Level3WorksheetGrants grants, out string hint);
        switch (outcome)
        {
            case Level3Checkpointer.CollectCommit.Collected:
                StartCollect(grants);
                break;
            case Level3Checkpointer.CollectCommit.AlreadyCollected:
                model.SetMessage("Bea's worksheet is already in her Case Folder.");
                break;
            default:
                Refused(hint);
                RefreshKeys();
                break;
        }
    }

    private void StartCollect(Level3WorksheetGrants grants)
    {
        collecting = true;
        collectGrants = grants;
        collectStart = Time.unscaledTime;
        awardShown = false;
        flightSoundPlayed = false;
        cardAt = -1f;
        ReleasePointer();
        ClosePicker();
        SkipReveal();
        if (calcModel != null)
        {
            // The calculator goes back to the office with the worksheet's totals; it leaves the table now.
            calcMode = CalcMode.Resting;
            calcModel.Root.gameObject.SetActive(false);
        }
        model.SetAdding(null);
        model.SetStamp(true, animate: true);
        model.SetKeys(Level3WorksheetModel.CheckKey.Hidden, false, false);
        model.SetMessage("Collected. Bea's checked worksheet goes into her Case Folder.");
        collectFrom = Capture(model.Root);
        collectOrigin = model.Root.position;
        if (AudioManager.Instance != null) AudioManager.Instance.PlayEffect("sfx_stamp_press_01", 0.9f);
    }

    private void TickCollect()
    {
        if (!collecting) return;
        float now = Time.unscaledTime;
        if (!awardShown)
        {
            float t = now - collectStart;
            if (t < StampHoldSeconds)
            {
                // A small lift as the stamp lands, like a sheet picked up to file.
                Pose lifted = collectFrom;
                lifted.Position += model.Root.up * (0.012f * Mathf.Sin(Mathf.Clamp01(t / StampHoldSeconds) * Mathf.PI * 0.5f));
                Apply(model.Root, lifted);
                return;
            }
            if (!flightSoundPlayed)
            {
                flightSoundPlayed = true;
                if (AudioManager.Instance != null) AudioManager.Instance.PlayEffect("sfx_folder_close_01", 0.8f);
            }
            float f = Mathf.Clamp01((t - StampHoldSeconds) / FlySeconds);
            float e = f < 0.5f ? 4f * f * f * f : 1f - Mathf.Pow(-2f * f + 2f, 3f) * 0.5f;
            Pose end = FolderPose(collectFrom);
            Pose start = collectFrom;
            start.Position += model.Root.up * 0.012f;
            Pose pose = Pose.Lerp(start, end, e);
            pose.Position += (end.Rotation * Vector3.up) * (0.06f * Mathf.Sin(e * Mathf.PI));
            Apply(model.Root, pose);
            if (f >= 1f) LandCollect();
            return;
        }
        if (cardAt >= 0f && now >= cardAt) ShowCard();
    }

    /// <summary>Where the board shrinks to: just in front of the Case Folder icon (or the Bag) on the HUD.</summary>
    private Pose FolderPose(Pose start)
    {
        Camera view = CameraController.Instance != null ? CameraController.Instance.MainCamera : Camera.main;
        Vector2 screen = new Vector2(Screen.width * 0.08f, Screen.height * 0.12f);
        RectTransform icon = HudHotbar.Instance != null ? HudHotbar.Instance.ItemTarget(InventoryCatalogue.CaseFolder) : null;
        if (icon != null)
        {
            Canvas canvas = icon.GetComponentInParent<Canvas>();
            if (canvas != null) canvas = canvas.rootCanvas;
            Camera uiCamera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
            screen = RectTransformUtility.WorldToScreenPoint(uiCamera, icon.TransformPoint(icon.rect.center));
        }
        if (view == null) return new Pose { Position = start.Position, Rotation = start.Rotation, Scale = start.Scale * 0.05f };
        Vector3 at = view.ScreenToWorldPoint(new Vector3(screen.x, screen.y, 0.42f));
        return new Pose { Position = at, Rotation = Quaternion.LookRotation(view.transform.up, -view.transform.forward), Scale = start.Scale * 0.06f };
    }

    /// <summary>The worksheet is in the folder: the board leaves the table and the saved rewards announce themselves (once).</summary>
    private void LandCollect()
    {
        if (awardShown) return;
        awardShown = true;
        if (model != null) model.Root.gameObject.SetActive(false);
        if (calcModel != null) calcModel.Root.gameObject.SetActive(false);
        mode = Mode.Gone;
        if (table != null) table.HoldPapers(false);
        if (putDownGroup != null) { putDownGroup.alpha = 0f; putDownGroup.interactable = putDownGroup.blocksRaycasts = false; }

        Level3WorksheetResult result = Chapter?.worksheetCheck;
        int wrong = result?.firstWrong != null ? result.firstWrong.Count : 0;
        CaseProgressionManager progression = CaseProgressionManager.Instance;
        // The level score shows the frozen first-check XP and badge (the toast and flight are the HUD's own).
        if (progression != null && progression.Score != null) progression.Score.AddSideQuest(wrong, collectOrigin, (BadgeTier)collectGrants.Tier);
        // Public Trust and coins: only what the save actually applied, with their named reasons.
        HudFeedback.AnnounceCampaign(collectGrants.Trust, collectGrants.Coins, collectOrigin);
        if (HudHotbar.Instance != null) HudHotbar.Instance.Notice("Bea's checked worksheet is in the Case Folder.");
        cardAt = Time.unscaledTime + CardDelaySeconds;
    }

    private void ShowCard()
    {
        cardAt = -1f;
        collecting = false;
        Level3WorksheetResult result = Chapter?.worksheetCheck;
        LevelRewardRules rules = Rules;
        var tier = (BadgeTier)collectGrants.Tier;
        var data = new RewardScreenData
        {
            Title = "Side Quest Complete",
            Subtitle = rules != null && !string.IsNullOrEmpty(rules.sideQuest.badgeName) ? rules.sideQuest.badgeName : "Worth the Tax?",
            Exp = collectGrants.Xp,
            MaxExp = Level3WorksheetCheck.MaxXp,
            Summary = "Every total on Bea's worksheet traces back to a record: what she earned, what supports her costs, and what stays out. Her computation is next.",
            MedalLabel = tier + " badge",
            Accent = HudBadgeArt.TierColor(tier)
        };
        Sprite medal = MedalFor(collectGrants.Tier);
        if (medal != null) data.Medals.Add(medal);
        if (result != null) data.Fact("First check", result.firstCorrect + "/" + result.firstTotal);
        data.Fact("Badge", tier.ToString());
        if (tier != BadgeTier.Gold) data.Footer = "Gold needs 12/12 on the first check.";
        if (collectGrants.Trust.Applied)
            data.RewardTile("Public Trust", HudTheme.Heart, HudTheme.Reputation, collectGrants.Trust.Amount, "Trust", collectGrants.Trust.After, CampaignRecord.MaxTrust);
        if (collectGrants.Coins.Applied)
            data.RewardTile("Coins", HudTheme.Coin, HudTheme.CoinGold, collectGrants.Coins.Amount, "Wallet", collectGrants.Coins.After);
        System.Action afterContinue = () => { if (isActiveAndEnabled) StartCoroutine(LeaveTableAfterCard()); };
        if (SideQuestCompleteUI.Instance != null) SideQuestCompleteUI.Instance.ShowScreen(data, afterContinue);
        else afterContinue();
    }

    /// <summary>After Continue: wait for the card and flights to finish, then stand up from the table (the side quest is over).</summary>
    private IEnumerator LeaveTableAfterCard()
    {
        yield return null;
        for (float waited = 0f; waited < 8f; waited += Time.unscaledDeltaTime)
        {
            if (!HudRewardScreen.AnyOpen && !SideQuestSplash.IsShowing) break;
            yield return null;
        }
        yield return new WaitForSecondsRealtime(0.25f);
        CameraController camera = CameraController.Instance;
        if (table != null && table.Seated && camera != null && camera.CurrentMode == CameraController.CameraMode.Workstation) camera.ExitFirstPerson();
    }

    /// <summary>Leaving or disabling mid-collection: the save already happened, so the presentation finishes at once (once).</summary>
    private void FinishCollectNow()
    {
        if (!collecting) return;
        if (!awardShown) LandCollect();
        if (cardAt >= 0f) ShowCard();
    }
}
