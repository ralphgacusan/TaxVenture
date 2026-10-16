using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// One drill card in Arthur's tutorial binder: a cream sheet in a clear
/// plastic sleeve, laid out as a wide strip so the whole row reads (and is
/// touched) easily on a phone.
///
///   [ 01 ]  Title                          [ PRACTICE ]
///           Walk · Look around · Follow     or GUIDED / MASTERY
///           Seen in game 2 / 3  ======
///
/// Locked: a grey sheet with a paper-clipped note and a padlock, no button.
/// New: an orange NEW tab on the corner. Mastered: a green MASTERED stamp.
/// Built in board pixels (1 px = 1 mm under the binder's scale); see
/// TutorialHubPresenter for the layout constants.
/// </summary>
public sealed class TrainingBinderCard : MonoBehaviour
{
    public const float Width = 760f;
    public const float Height = 236f;

    private static readonly Color Sleeve = new Color(1f, 1f, 1f, 0.10f);
    private static readonly Color NewOrange = new Color32(240, 140, 52, 255);
    public static readonly Color StampGreen = new Color32(52, 150, 86, 255);
    private static readonly Color GuidedNavy = new Color32(44, 66, 96, 255);

    public event Action<TrainingBinderCard, bool> PracticeRequested;

    public TrainingCardView View { get; private set; }

    private readonly List<Material> materials = new List<Material>();
    private Transform stamp;
    private Transform newTab;
    private float stampSlam = -1f;
    private float pulse = -1f;
    private float hop;
    private Vector3 restPosition;
    private float slideTime = -1f;
    private float slideDelay;
    private float celebrateDelay = -1f;
    private bool celebrateSlam;
    private const float SlideSeconds = 0.42f;

    public static TrainingBinderCard Create(Transform parent, Vector3 center, TrainingCardView view, Color accent)
    {
        Transform root = CareerBoardKit.Node("Card " + view.Def.id, parent, center);
        var card = root.gameObject.AddComponent<TrainingBinderCard>();
        card.restPosition = center;
        card.Build(view, accent);
        return card;
    }

    private void Build(TrainingCardView view, Color accent)
    {
        View = view;
        bool locked = !view.Unlocked;
        Color paper = locked ? CareerBoardKit.PaperGrey : CareerBoardKit.PaperWhite;
        Color ink = locked ? CareerBoardKit.InkSoft : CareerBoardKit.Ink;

        CareerBoardKit.Box(transform, "Shadow", new Vector3(5f, -7f, 4f), new Vector3(Width, Height, 2f),
            new Color32(176, 166, 146, 255), materials);
        CareerBoardKit.Box(transform, "Sheet", Vector3.zero, new Vector3(Width, Height, 3f), paper, materials);
        // Colour edge of the section on the left, like a coded index sheet.
        CareerBoardKit.Box(transform, "Section edge", new Vector3(-Width * 0.5f + 7f, 0f, -2f), new Vector3(14f, Height, 1f),
            locked ? CareerBoardKit.InkSoft : accent, materials);

        TMP_FontAsset hand = HudTheme.HandwritingFont;
        // A mastered card's number moves up to make room for the stamp under it.
        float numberY = view.State == TrainingCardState.Mastered ? 34f : 4f;
        CareerBoardKit.Text(transform, "Number", view.Def.number.ToString("00"), 96f, new Vector3(-Width * 0.5f + 92f, numberY, -3f),
            130f, 110f, locked ? CareerBoardKit.InkSoft : accent, TextAlignmentOptions.Center, FontStyles.Bold, hand);

        float textX = -Width * 0.5f + 170f;
        const float textWidth = 380f;
        CareerBoardKit.Text(transform, "Title", view.Def.title ?? "", 38f, new Vector3(textX + textWidth * 0.5f, 72f, -3f),
            textWidth, 48f, ink, TextAlignmentOptions.Left);

        if (locked)
        {
            BuildLockedNote(textX, textWidth);
        }
        else
        {
            TextMeshPro skills = CareerBoardKit.Text(transform, "Skills", string.Join("  ·  ", view.Def.skills), 23f,
                new Vector3(textX + textWidth * 0.5f, 14f, -3f), textWidth, 64f, CareerBoardKit.InkSoft,
                TextAlignmentOptions.TopLeft, FontStyles.Normal);
            skills.lineSpacing = -8f;
            BuildProgress(textX, textWidth);
            BuildButtons();
        }

        if (view.State == TrainingCardState.New) BuildNewTab();
        if (view.State == TrainingCardState.Mastered) BuildStamp();

        // The clear sleeve over everything (no collider: buttons stay touchable).
        MeshRenderer sleeve = CareerBoardKit.Box(transform, "Sleeve", new Vector3(0f, 0f, -16f), new Vector3(Width + 10f, Height + 10f, 1f),
            Sleeve, materials);
        MakeTransparent(sleeve.sharedMaterial);
        sleeve.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        MeshRenderer shine = CareerBoardKit.Box(transform, "Sleeve shine", new Vector3(-Width * 0.18f, Height * 0.3f, -17f),
            new Vector3(Width * 0.42f, 10f, 0.5f), new Color(1f, 1f, 1f, 0.18f), materials);
        MakeTransparent(shine.sharedMaterial);
        shine.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
    }

    private void BuildLockedNote(float textX, float textWidth)
    {
        CareerBoardKit.Box(transform, "Note", new Vector3(textX + textWidth * 0.5f, -36f, -4f), new Vector3(textWidth, 96f, 1f),
            CareerBoardKit.PaperYellow, materials).transform.localRotation = Quaternion.Euler(0f, 0f, -1.5f);
        CareerBoardKit.Text(transform, "Locked note", View.Def.lockedNote ?? "", 25f, new Vector3(textX + textWidth * 0.5f, -36f, -5f),
            textWidth - 30f, 84f, CareerBoardKit.Ink, TextAlignmentOptions.Center, FontStyles.Normal, HudTheme.HandwritingFont);
        // Paper clip.
        CareerBoardKit.Box(transform, "Clip", new Vector3(textX + 24f, 12f, -6f), new Vector3(14f, 46f, 3f),
            new Color32(150, 156, 164, 255), materials);
        CareerBoardKit.Padlock(transform, new Vector3(Width * 0.5f - 120f, 0f, -8f), 70f, materials);
        CareerBoardKit.Text(transform, "Locked", "LOCKED", 26f, new Vector3(Width * 0.5f - 120f, -64f, -4f), 180f, 34f,
            CareerBoardKit.InkSoft);
    }

    private void BuildProgress(float textX, float textWidth)
    {
        string seen = View.Total > 0 ? $"Seen in game  {View.Seen} / {View.Total}" : "";
        CareerBoardKit.Text(transform, "Seen", seen, 22f, new Vector3(textX + 120f, -66f, -3f), 240f, 30f,
            CareerBoardKit.InkSoft, TextAlignmentOptions.Left, FontStyles.Bold);
        if (View.Total <= 0) return;
        RectTransform fill = CareerBoardKit.Bar(transform, "Seen bar", new Vector3(textX + 320f, -66f, -3f), 120f, 16f,
            View.Seen >= View.Total ? HudTheme.Complete : HudTheme.Active, out _);
        CareerBoardKit.SetFill(fill, View.Total > 0 ? (float)View.Seen / View.Total : 0f);
        if (View.Record != null && View.Record.bestMasterySeconds > 0)
        {
            int s = View.Record.bestMasterySeconds;
            CareerBoardKit.Text(transform, "Best", $"Best mastery  {s / 60}:{s % 60:00}", 22f, new Vector3(textX + 120f, -96f, -3f),
                240f, 30f, CareerBoardKit.InkSoft, TextAlignmentOptions.Left, FontStyles.Bold);
        }
    }

    private void BuildButtons()
    {
        float x = Width * 0.5f - 118f;
        if (View.CanMaster)
        {
            CareerBoardButton guided = CareerBoardButton.Create(transform, "Guided", "GUIDED", new Vector3(x, 50f, -10f),
                new Vector2(196f, 76f), GuidedNavy, HudTheme.TextPrimary, 26f);
            guided.Tapped += _ => PracticeRequested?.Invoke(this, false);
            CareerBoardButton master = CareerBoardButton.Create(transform, "Mastery", "MASTERY", new Vector3(x, -48f, -10f),
                new Vector2(196f, 76f), StampGreen, Color.white, 26f);
            master.Tapped += _ => PracticeRequested?.Invoke(this, true);
        }
        else
        {
            CareerBoardButton practice = CareerBoardButton.Create(transform, "Practice", "PRACTICE", new Vector3(x, 0f, -10f),
                new Vector2(196f, 100f), HudTheme.Brass, HudTheme.PanelInner, 30f);
            practice.Tapped += _ => PracticeRequested?.Invoke(this, false);
        }
    }

    private void BuildNewTab()
    {
        newTab = CareerBoardKit.Node("New tab", transform, new Vector3(-Width * 0.5f + 70f, Height * 0.5f + 6f, -5f));
        newTab.localRotation = Quaternion.Euler(0f, 0f, 6f);
        CareerBoardKit.Box(newTab, "Paper", Vector3.zero, new Vector3(96f, 40f, 1f), NewOrange, materials);
        CareerBoardKit.Text(newTab, "Label", "NEW", 24f, new Vector3(0f, 0f, -1f), 90f, 34f, Color.white);
    }

    private void BuildStamp()
    {
        // Under the card number, clear of the title, skills and buttons.
        stamp = CareerBoardKit.Node("Mastered stamp", transform, new Vector3(-Width * 0.5f + 94f, -66f, -12f));
        stamp.localRotation = Quaternion.Euler(0f, 0f, -9f);
        Color ink = new Color(StampGreen.r, StampGreen.g, StampGreen.b, 0.88f);
        Material inkMaterial = CareerBoardKit.Lit(ink, materials);
        MakeTransparent(inkMaterial);
        const float w = 150f, h = 48f, t = 4f;
        CareerBoardKit.Box(stamp, "Top", new Vector3(0f, h * 0.5f, 0f), new Vector3(w, t, 1f), inkMaterial);
        CareerBoardKit.Box(stamp, "Bottom", new Vector3(0f, -h * 0.5f, 0f), new Vector3(w, t, 1f), inkMaterial);
        CareerBoardKit.Box(stamp, "Left", new Vector3(-w * 0.5f, 0f, 0f), new Vector3(t, h, 1f), inkMaterial);
        CareerBoardKit.Box(stamp, "Right", new Vector3(w * 0.5f, 0f, 0f), new Vector3(t, h, 1f), inkMaterial);
        CareerBoardKit.Text(stamp, "Label", "MASTERED", 25f, new Vector3(0f, 0f, -1f), w - 14f, h - 8f, ink);
    }

    /// <summary>Slides into its sleeve from the right after a delay (page turn, binder intro).</summary>
    public void SlideIn(float delay)
    {
        slideDelay = Mathf.Max(0f, delay);
        slideTime = 0f;
        ApplyPose();
    }

    /// <summary>Plays once on return from a run, after a delay: a hop, and the stamp slam when newly mastered.</summary>
    public void Celebrate(bool slamStamp, float delay)
    {
        celebrateDelay = Mathf.Max(0f, delay);
        celebrateSlam = slamStamp;
        if (slamStamp && stamp != null) stamp.localScale = Vector3.zero;   // appears with the slam
    }

    private void Update()
    {
        float dt = Time.unscaledDeltaTime;
        if (celebrateDelay >= 0f)
        {
            celebrateDelay -= dt;
            if (celebrateDelay < 0f)
            {
                pulse = 0f;
                if (celebrateSlam && stamp != null) stampSlam = 0f;
                if (newTab != null) newTab.gameObject.SetActive(false);
            }
        }
        if (slideTime >= 0f)
        {
            slideTime += dt;
            if (slideTime - slideDelay >= SlideSeconds) slideTime = -1f;
        }
        if (pulse >= 0f)
        {
            pulse += dt;
            hop = Mathf.Sin(Mathf.Clamp01(pulse / 0.5f) * Mathf.PI) * 16f;
            if (pulse > 0.5f) { pulse = -1f; hop = 0f; }
        }
        ApplyPose();
        if (stampSlam >= 0f && stamp != null)
        {
            stampSlam += dt;
            float k = Mathf.Clamp01((stampSlam - 0.25f) / 0.2f);
            stamp.localScale = stampSlam < 0.25f ? Vector3.zero : Vector3.one * Mathf.Lerp(2.6f, 1f, k * k);
            if (stampSlam >= 0.25f && stampSlam - dt < 0.25f && AudioManager.Instance != null)
                AudioManager.Instance.PlayAchievementSFX();
            if (stampSlam > 0.45f) { stampSlam = -1f; stamp.localScale = Vector3.one; }
        }
    }

    private void ApplyPose()
    {
        float slide = 0f;
        float lift = 0f;
        if (slideTime >= 0f)
        {
            float t = Mathf.Clamp01((slideTime - slideDelay) / SlideSeconds);
            float e = 1f - Mathf.Pow(1f - t, 3f);   // ease out
            slide = (1f - e) * 70f;
            lift = Mathf.Sin(t * Mathf.PI) * 10f + (1f - e) * 8f;
        }
        transform.localPosition = restPosition + new Vector3(slide, 0f, -(hop + lift));
    }

    private static void MakeTransparent(Material material)
    {
        if (material == null) return;
        if (material.HasProperty("_Surface"))
        {
            material.SetFloat("_Surface", 1f);
            material.SetFloat("_Blend", 0f);
            material.SetOverrideTag("RenderType", "Transparent");
            material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetInt("_ZWrite", 0);
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        }
    }

    private void OnDestroy()
    {
        foreach (Material material in materials) CareerBoardKit.Dispose(material);
    }
}
