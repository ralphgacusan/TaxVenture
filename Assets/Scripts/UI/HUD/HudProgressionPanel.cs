using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// PURPOSE:
/// Top-left progression card of the Office HUD: level badge and name, an EXP
/// bar and a hearts bar (each: earned this level out of the level maximum,
/// with a tick at the passing line), the badges earned this level and, when
/// available, the player's career EXP.
///
/// DATA (read-only, never recalculated here):
/// - CaseProgressionManager: level id/name and the level score
///   (LevelScoreLedger: totals, maximums, passing percent, badges; numbers
///   from Resources/Levels/&lt;id&gt;/rewards.json)
/// - PlayerProgressManager (optional, main-menu runs only): career EXP
///
/// UPDATES:
/// Event driven — GameStateMachine.OnStateChanged (level/case loaded) and
/// LevelScoreLedger.EntryAdded (reward earned). Update() only
/// runs while a bar animation is pending, and animations wait until the HUD
/// is visible so the player actually sees the reward land.
/// </summary>
public sealed class HudProgressionPanel : MonoBehaviour
{
    public const float Width = 460f;
    private const float Height = 208f;
    private const float RowCaptionX = 18f;
    private const float BarX = 108f;
    private const float BarWidth = 224f;
    private const float BarHeight = 18f;
    private const float ValueWidth = 104f;
    private const float ExpRowY = 96f;
    private const float RepRowY = 132f;
    private const float RowHeight = 26f;
    private const float FillDuration = 0.9f;
    private const float MedalSize = 52f;

    /// <summary>One animated bar: fill eases toward the target, a bright ghost shows the gain at once.</summary>
    private sealed class AnimatedBar
    {
        public RectTransform Fill;
        public RectTransform Gain;
        public TextMeshProUGUI Value;
        public RectTransform Icon;
        public Image PassMark;
        public int Max = 1;
        public int Target;
        public float Shown;
        private float from;
        private float elapsed = -1f;

        public bool Animating => elapsed >= 0f;

        public void SetTarget(int max, int target, bool animate)
        {
            Max = Mathf.Max(1, max);
            Target = target;
            if (!animate || Mathf.Approximately(Shown, Target))
            {
                if (!animate) Shown = Target;
                elapsed = -1f;
                Apply(Shown, Animating ? Target : Shown);
                return;
            }
            from = Shown;
            elapsed = 0f;
            Apply(Shown, Target);
        }

        public void Tick(float dt)
        {
            if (elapsed < 0f) return;
            elapsed += dt;
            float t = elapsed / FillDuration;
            Shown = Mathf.Lerp(from, Target, HudTheme.EaseOutCubic(t));
            // Small pop on the row icon while the value climbs.
            float pop = t >= 1f ? 1f : 1f + 0.25f * Mathf.Sin(Mathf.Clamp01(t) * Mathf.PI);
            if (Icon != null) Icon.localScale = new Vector3(pop, pop, 1f);
            if (t >= 1f)
            {
                Shown = Target;
                elapsed = -1f;
            }
            Apply(Shown, Animating ? Target : Shown);
        }

        private void Apply(float shown, float gainValue)
        {
            float fill = Fraction(shown, Max);
            float gain = Fraction(gainValue, Max);
            Fill.anchorMax = new Vector2(fill, 1f);
            Gain.anchorMax = new Vector2(Mathf.Max(fill, gain), 1f);
            // Rounded caps cannot shrink below their diameter; hide empty fills.
            Fill.gameObject.SetActive(fill > 0.001f);
            Gain.gameObject.SetActive(gain > fill + 0.001f);
            Value.SetText("{0} / {1}", Mathf.RoundToInt(shown), Max);
            if (PassMark != null)
                PassMark.color = fill >= PassMark.rectTransform.anchorMin.x - 0.0005f ? HudTheme.Complete : HudTheme.BrassSoft;
        }

        /// <summary>Places the passing-line tick at percent of the bar.</summary>
        public void SetPassing(int percent)
        {
            if (PassMark == null) return;
            float x = Mathf.Clamp01(percent / 100f);
            RectTransform mark = PassMark.rectTransform;
            mark.anchorMin = new Vector2(x, 0f);
            mark.anchorMax = new Vector2(x, 1f);
            mark.gameObject.SetActive(percent > 0 && percent < 100);
            Apply(Shown, Animating ? Target : Shown);
        }
    }

    private OfficeHudRuntime host;
    private TextMeshProUGUI badgeText;
    private TextMeshProUGUI headingText;
    private TextMeshProUGUI levelNameText;
    private TextMeshProUGUI careerText;
    private TextMeshProUGUI repCaption;
    private RectTransform walletRow;
    private RectTransform walletIcon;
    private TextMeshProUGUI walletValue;
    private RectTransform medalRow;
    private int medalsShown;
    private readonly AnimatedBar expBar = new AnimatedBar();
    private readonly AnimatedBar repBar = new AnimatedBar();
    private bool subscribed;
    private float holdSeconds;
    private int walletShown;
    private int walletTarget;
    private int walletFrom;
    private float walletElapsed = -1f;

    /// <summary>The card's frame (scaled): HUD pieces that sit under the card read its real bottom edge from here.</summary>
    public RectTransform Frame => (RectTransform)transform;

    private static readonly Vector3[] FrameCorners = new Vector3[4];

    /// <summary>The card's bottom edge in screen pixels (overlay canvas), whatever its height, scale or safe area.</summary>
    public float ScreenBottom
    {
        get
        {
            Frame.GetWorldCorners(FrameCorners);
            return Mathf.Min(FrameCorners[0].y, FrameCorners[3].y);
        }
    }

    public RectTransform ExpBar => expBar.Fill != null ? (RectTransform)expBar.Fill.parent : null;
    public RectTransform ReputationBar => repBar.Fill != null ? (RectTransform)repBar.Fill.parent : null;
    public RectTransform CoinsRow => walletRow;
    /// <summary>The wallet / career line (T09 coin flights land here).</summary>
    public RectTransform WalletLabel => walletIcon;

    // =========================================================
    // BUILD
    // =========================================================

    public static HudProgressionPanel Build(RectTransform parent, OfficeHudRuntime host)
    {
        RectTransform frame = HudTheme.CreateRect("Progression Card", parent);
        HudTheme.PlaceTopLeft(frame, HudTheme.Margin, HudTheme.Margin, Width, Height);
        // Uniform scale keeps proportions; 460x172 x 1.15 = 529x198 reference units.
        frame.localScale = new Vector3(HudTheme.HudScale, HudTheme.HudScale, 1f);
        RectTransform body = HudTheme.CreatePanel("Card", frame, 12f);
        HudTheme.Stretch((RectTransform)body.parent);

        var panel = frame.gameObject.AddComponent<HudProgressionPanel>();
        panel.host = host;
        panel.BuildContents(body);
        return panel;
    }

    private void BuildContents(RectTransform body)
    {
        // Level badge: brass ring with the level number.
        RectTransform ring = HudTheme.PlaceTopLeft(HudTheme.CreateRect("Level Badge", body), 16f, 14f, 68f, 68f);
        HudTheme.AddImage(ring, HudTheme.Circle, HudTheme.Brass);
        RectTransform core = HudTheme.CreateRect("Core", ring);
        HudTheme.Stretch(core, 4f);
        HudTheme.AddImage(core, HudTheme.Circle, HudTheme.PanelInner);
        badgeText = HudTheme.CreateLabel("Number", core, "1", 32f, HudTheme.TextPrimary,
            TextAlignmentOptions.Center, FontStyles.Bold);
        HudTheme.Stretch((RectTransform)badgeText.transform);

        headingText = HudTheme.CreateLabel("Heading", body, "LEVEL 1", HudTheme.HeadingSize, HudTheme.BrassSoft,
            TextAlignmentOptions.MidlineLeft, FontStyles.Bold);
        headingText.characterSpacing = 6f;
        HudTheme.PlaceTopLeft((RectTransform)headingText.transform, 100f, 16f, 170f, 32f);

        careerText = HudTheme.CreateLabel("Career", body, string.Empty, HudTheme.CaptionSize,
            HudTheme.TextMuted, TextAlignmentOptions.MidlineRight);
        // Leaves room for the side-quest medal at the right.
        HudTheme.PlaceTopLeft((RectTransform)careerText.transform, 262f, 16f, 122f, 32f);
        careerText.gameObject.SetActive(false);

        // Campaign coins have their own row and landing point, separate from the career EXP label.
        walletRow = HudTheme.PlaceTopLeft(HudTheme.CreateRect("Wallet", body), 18f, 168f, 424f, RowHeight);
        walletIcon = HudTheme.PlaceTopLeft(HudTheme.CreateRect("Wallet Coin", walletRow), 0f, 1f, 24f, 24f);
        HudTheme.AddImage(walletIcon, HudTheme.Coin, HudTheme.CoinGold);
        TextMeshProUGUI walletCaption = HudTheme.CreateLabel("Wallet Caption", walletRow, "COINS", HudTheme.CaptionSize,
            HudTheme.CoinGold, TextAlignmentOptions.MidlineLeft, FontStyles.Bold);
        walletCaption.characterSpacing = 3f;
        HudTheme.PlaceTopLeft((RectTransform)walletCaption.transform, 34f, 0f, 120f, RowHeight);
        walletValue = HudTheme.CreateLabel("Wallet Value", walletRow, "0", HudTheme.ValueSize,
            HudTheme.TextPrimary, TextAlignmentOptions.MidlineRight, FontStyles.Bold);
        HudTheme.PlaceTopLeft((RectTransform)walletValue.transform, 292f, 0f, 132f, RowHeight);
        walletRow.gameObject.SetActive(false);

        levelNameText = HudTheme.CreateLabel("Level Name", body, string.Empty, HudTheme.ValueSize,
            HudTheme.TextPrimary, TextAlignmentOptions.MidlineLeft);
        HudTheme.PlaceTopLeft((RectTransform)levelNameText.transform, 100f, 48f, 338f, 28f);

        // Badges earned this level: small brass medallions, right-aligned on the name row.
        medalRow = HudTheme.PlaceTopLeft(HudTheme.CreateRect("Badges", body), 444f - MedalSize, 34f,
            MedalSize, MedalSize);

        // EXP row: a star and its clear label before the bar and total.
        RectTransform expIcon = HudTheme.PlaceTopLeft(HudTheme.CreateRect("EXP Icon", body), RowCaptionX, ExpRowY + 2f, 22f, 22f);
        HudTheme.AddImage(expIcon, HudTheme.Star, HudTheme.Exp);
        TextMeshProUGUI expCaption = HudTheme.CreateLabel("EXP Caption", body, "EXP", HudTheme.CaptionSize,
            HudTheme.Exp, TextAlignmentOptions.MidlineLeft, FontStyles.Bold);
        expCaption.characterSpacing = 2f;
        HudTheme.PlaceTopLeft((RectTransform)expCaption.transform, RowCaptionX + 30f, ExpRowY, 50f, RowHeight);
        expBar.Icon = expIcon;
        BuildBar(body, "EXP", ExpRowY, HudTheme.Exp, HudTheme.ExpGain, expBar);

        // Hearts represent reputation in ordinary runs and Public Trust in a campaign.
        RectTransform heart = HudTheme.PlaceTopLeft(HudTheme.CreateRect("Reputation Icon", body),
            RowCaptionX, RepRowY + 1f, 24f, 24f);
        HudTheme.AddImage(heart, HudTheme.Heart, HudTheme.Reputation);
        repCaption = HudTheme.CreateLabel("Reputation Caption", body, "REP", HudTheme.CaptionSize,
            HudTheme.Reputation, TextAlignmentOptions.MidlineLeft, FontStyles.Bold);
        repCaption.characterSpacing = 1f;
        HudTheme.PlaceTopLeft((RectTransform)repCaption.transform, RowCaptionX + 30f, RepRowY, 50f, RowHeight);
        repBar.Icon = heart;
        BuildBar(body, "Reputation", RepRowY, HudTheme.Reputation, HudTheme.ReputationGain, repBar);

        expBar.SetTarget(RewardsCalculator.MaxExp, 0, animate: false);
        repBar.SetTarget(RewardsCalculator.MaxReputation, 0, animate: false);
    }

    private static void BuildBar(RectTransform body, string name, float rowY, Color fillColor, Color gainColor,
        AnimatedBar bar)
    {
        float barY = rowY + (RowHeight - BarHeight) * 0.5f;
        RectTransform track = HudTheme.PlaceTopLeft(HudTheme.CreateRect(name + " Bar", body), BarX, barY, BarWidth, BarHeight);
        HudTheme.AddImage(track, HudTheme.Rounded, HudTheme.Track, BarHeight * 0.5f);
        bar.Gain = CreateFill("Gain", track, gainColor);
        bar.Fill = CreateFill("Fill", track, fillColor);

        // Passing line: a slim tick across the bar that turns green once reached.
        RectTransform mark = HudTheme.CreateRect("Passing Mark", track);
        mark.pivot = new Vector2(0.5f, 0.5f);
        mark.anchorMin = new Vector2(0.8f, 0f);
        mark.anchorMax = new Vector2(0.8f, 1f);
        mark.offsetMin = new Vector2(-1.5f, -4f);
        mark.offsetMax = new Vector2(1.5f, 4f);
        bar.PassMark = HudTheme.AddImage(mark, HudTheme.Rounded, HudTheme.BrassSoft, 1.5f);
        bar.PassMark.raycastTarget = false;

        bar.Value = HudTheme.CreateLabel(name + " Value", body, "0 / 0", HudTheme.ValueSize,
            HudTheme.TextPrimary, TextAlignmentOptions.MidlineRight);
        HudTheme.PlaceTopLeft((RectTransform)bar.Value.transform, BarX + BarWidth + 4f, rowY, ValueWidth, RowHeight);
    }

    private static RectTransform CreateFill(string name, RectTransform track, Color color)
    {
        RectTransform fill = HudTheme.CreateRect(name, track);
        fill.anchorMin = Vector2.zero;
        fill.anchorMax = new Vector2(0f, 1f);
        fill.pivot = new Vector2(0f, 0.5f);
        fill.offsetMin = fill.offsetMax = Vector2.zero;
        HudTheme.AddImage(fill, HudTheme.Rounded, color, BarHeight * 0.5f);
        return fill;
    }

    // =========================================================
    // EVENTS
    // =========================================================

    private void Start()
    {
        Subscribe();
        RefreshFromGame(animate: false);
        enabled = false;
    }

    private void Subscribe()
    {
        if (subscribed) return;
        subscribed = true;
        LevelScoreLedger.EntryAdded += HandleScoreEntry;
        PlayerProgressManager.ProgressChanged += HandleProgressChanged;
        if (GameStateMachine.Instance != null) GameStateMachine.Instance.OnStateChanged += HandleStateChanged;
    }

    private void HandleProgressChanged(PlayerProgressData _) => RefreshFromGame(animate: true);

    private void OnDestroy()
    {
        LevelScoreLedger.EntryAdded -= HandleScoreEntry;
        PlayerProgressManager.ProgressChanged -= HandleProgressChanged;
        if (GameStateMachine.Instance != null) GameStateMachine.Instance.OnStateChanged -= HandleStateChanged;
    }

    private void HandleStateChanged(IGameState state) => RefreshFromGame(animate: true);

    private void HandleScoreEntry(ScoreEntry entry) => RefreshFromGame(animate: true);

    /// <summary>Pulls the current, already-calculated values and animates toward them.</summary>
    public void RefreshFromGame(bool animate)
    {
        CaseProgressionManager progressionManager = CaseProgressionManager.Instance;
        int cases = Mathf.Max(1, progressionManager != null ? progressionManager.TotalCasesInLevel : 0);
        int exp = 0;
        int reputation = 0;
        int maxExp = cases * RewardsCalculator.MaxExp;
        int maxReputation = cases * RewardsCalculator.MaxReputation;
        int passing = 0;

        if (progressionManager != null)
        {
            ApplyLevelLabels(progressionManager.CurrentLevelId, progressionManager.CurrentLevelName);
            LevelScoreLedger score = progressionManager.Score;
            if (score.MaxExp > 0 || score.MaxHearts > 0)
            {
                exp = score.TotalExp;
                reputation = score.TotalHearts;
                maxExp = score.MaxExp;
                maxReputation = score.MaxHearts;
                passing = score.Rules.passingPercent;
            }
            ApplyBadges(score, animate);
        }

        // T08: in a campaign the hearts bar is Public Trust (saved value / 100, with the
        // future support target as its tick), and the story never shows a passing line (T07).
        bool trust = TrustDisplay.Active;
        if (trust)
        {
            reputation = TrustDisplay.PublicTrust;
            maxReputation = CampaignRecord.MaxTrust;
            passing = 0;
        }
        expBar.SetTarget(maxExp, exp, animate);
        repBar.SetTarget(maxReputation, reputation, animate);
        repCaption.SetText(trust ? "TRUST" : "REP");
        expBar.SetPassing(passing);
        repBar.SetPassing(trust ? CampaignRewards.SupportTarget : passing);
        ApplyCareer(animate);
        ApplyPassingCaption(progressionManager, passing);
        enabled = expBar.Animating || repBar.Animating || walletAnimating;
    }

    private void ApplyLevelLabels(string levelId, string levelName)
    {
        int number = ParseLevelNumber(levelId);
        if (number > 0)
        {
            badgeText.SetText("{0}", number);
            headingText.SetText("LEVEL {0}", number);
        }
        levelNameOnly = string.IsNullOrWhiteSpace(levelName) ? string.Empty : levelName;
        levelNameText.text = levelNameOnly;
    }

    private string levelNameOnly;

    /// <summary>
    /// "Tutorial Level · Lv 2 needs 80%" beside the level name, so the
    /// passing tick on the bars has words; "Lv 2 unlocked!" once reached.
    /// </summary>
    private void ApplyPassingCaption(CaseProgressionManager progression, int passing)
    {
        if (levelNameOnly == null) levelNameOnly = levelNameText.text;
        if (progression == null || passing <= 0)
        {
            levelNameText.text = levelNameOnly;
            return;
        }
        levelNameOnly = string.IsNullOrWhiteSpace(progression.CurrentLevelName) ? levelNameOnly : progression.CurrentLevelName;
        int next = ParseLevelNumber(progression.CurrentLevelId) + 1;
        string goal = progression.Score.Passed
            ? $"<color=#6EC98C>Lv {next} unlocked!</color>"
            : $"<color=#DDBE7E>Lv {next} needs {passing}%</color>";
        levelNameText.text = string.IsNullOrEmpty(levelNameOnly) ? goal : $"{levelNameOnly}  <size=85%>·  {goal}</size>";
    }

    /// <summary>"Level_01" -> 1. Returns 0 when the id has no trailing number.</summary>
    public static int ParseLevelNumber(string levelId)
    {
        if (string.IsNullOrEmpty(levelId)) return 0;
        int end = levelId.Length;
        int start = end;
        while (start > 0 && char.IsDigit(levelId[start - 1])) start--;
        if (start == end) return 0;
        return int.TryParse(levelId.Substring(start, end - start), out int value) ? value : 0;
    }

    /// <summary>
    /// One medal slot per side-quest badge of this level: a dark silhouette
    /// until earned, then the bronze / silver / gold medal (popping in).
    /// </summary>
    private void ApplyBadges(LevelScoreLedger score, bool animate)
    {
        LevelRewardRules rules = score.Rules;
        bool hasBadge = rules != null && !string.IsNullOrEmpty(rules.sideQuest.badgeId);
        if (hasBadge && medalImage == null)
        {
            RectTransform medal = HudTheme.CreateRect("Side Quest Badge", medalRow);
            medal.anchorMin = medal.anchorMax = new Vector2(1f, 0.5f);
            medal.pivot = new Vector2(0.5f, 0.5f);
            medal.sizeDelta = new Vector2(MedalSize, MedalSize);
            medal.anchoredPosition = new Vector2(-MedalSize * 0.5f, 0f);
            medalImage = HudTheme.AddImage(medal, HudBadgeArt.For(rules, BadgeTier.None), Color.white);
            medalImage.preserveAspect = true;
            medalsShown = 1;
        }

        if (medalImage != null)
        {
            BadgeTier tier = score.BadgeTiers.Count > 0 ? score.BadgeTiers[0] : BadgeTier.None;
            if (tier != shownTier)
            {
                medalImage.sprite = HudBadgeArt.For(rules, tier);
                if (animate && tier != BadgeTier.None) newMedal = medalImage.rectTransform;
                shownTier = tier;
            }
        }

        // Keep the level name clear of the medal.
        float reserved = medalsShown > 0 ? MedalSize + 8f : 0f;
        RectTransform name = (RectTransform)levelNameText.transform;
        name.sizeDelta = new Vector2(338f - reserved, name.sizeDelta.y);
    }

    private Image medalImage;
    private BadgeTier shownTier = BadgeTier.None;

    /// <summary>The medallion added last (for the HUD's arrival pulse), or null.</summary>
    public RectTransform TakeNewBadge()
    {
        RectTransform medal = newMedal;
        newMedal = null;
        return medal;
    }

    private RectTransform newMedal;

    private void ApplyCareer(bool animate)
    {
        PlayerProgressManager manager = PlayerProgressManager.Instance;
        bool show = manager != null && manager.CurrentProgress != null;
        bool showWallet = show && TrustDisplay.Active;
        careerText.gameObject.SetActive(show && !showWallet);
        walletRow.gameObject.SetActive(showWallet);
        if (!show)
        {
            walletElapsed = -1f;
            return;
        }
        if (showWallet)
        {
            SetWalletTarget(manager.CurrentProgress.campaign.coins, animate);
            return;
        }
        walletElapsed = -1f;
        careerText.SetText("Career {0} EXP", manager.CurrentProgress.totalExp);
    }

    private bool walletAnimating => walletElapsed >= 0f;

    private void SetWalletTarget(int target, bool animate)
    {
        if (!animate || walletValue == null)
        {
            walletShown = walletTarget = target;
            walletElapsed = -1f;
            walletValue.SetText("{0}", walletShown);
            return;
        }
        if (walletAnimating && walletTarget == target) return;
        if (walletShown == target)
        {
            walletTarget = target;
            walletElapsed = -1f;
            walletValue.SetText("{0}", walletShown);
            return;
        }
        walletFrom = walletShown;
        walletTarget = target;
        walletElapsed = 0f;
        enabled = true;
    }

    // =========================================================
    // ANIMATION
    // =========================================================

    private void Update()
    {
        // Hold the reward until the player can see it land.
        if (host != null && !host.IsShown) return;

        // Wait for the flying hearts / sparks to arrive before filling.
        if (holdSeconds > 0f)
        {
            holdSeconds -= Time.unscaledDeltaTime;
            return;
        }

        float dt = Time.unscaledDeltaTime;
        expBar.Tick(dt);
        // Reputation follows the EXP fill slightly later so the two read as a sequence.
        if (!expBar.Animating || expBar.Shown >= expBar.Target * 0.5f) repBar.Tick(dt);
        TickWallet(dt);

        if (!expBar.Animating && !repBar.Animating && !walletAnimating) enabled = false;
    }

    private void TickWallet(float dt)
    {
        if (!walletAnimating) return;
        walletElapsed += dt;
        float t = walletElapsed / FillDuration;
        walletShown = Mathf.RoundToInt(Mathf.Lerp(walletFrom, walletTarget, HudTheme.EaseOutCubic(t)));
        walletValue.SetText("{0}", walletShown);
        if (t >= 1f)
        {
            walletShown = walletTarget;
            walletValue.SetText("{0}", walletShown);
            walletElapsed = -1f;
        }
    }

    /// <summary>
    /// Delays the next bar fill (counted only while the HUD is visible), so
    /// the reward effect flying into the bars lands first.
    /// </summary>
    public void HoldFillFor(float seconds)
    {
        holdSeconds = Mathf.Max(holdSeconds, seconds);
        enabled = true;
    }

    public static float Fraction(float value, int max) => max <= 0 ? 0f : Mathf.Clamp01(value / max);
}
