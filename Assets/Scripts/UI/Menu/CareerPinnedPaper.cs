using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// A paper pinned to the career corkboard, built and animated like the Level 1
/// profile cards: a thin card on a red pushpin that sways gently around the
/// pin (ProfileDragCard.UpdateSway), drops in and gets pinned when the board
/// opens, peels toward the player on hover, wobbles when tapped, and can take
/// a rubber stamp (PASSED / TRY AGAIN) that slams down when newly earned.
///
/// The object's origin is the pin, so every rotation turns around it. The
/// presenter writes the paper's content into Body.
/// </summary>
public sealed class CareerPinnedPaper : CareerBoardTarget
{
    private const float IntroDuration = 0.5f;
    private const float PinDuration = 0.16f;
    private const float StampDuration = 0.3f;
    private const float PaperThickness = 4f;

    private readonly List<Material> materials = new List<Material>();
    private Material paperMaterial;
    private Material stripMaterial;
    private Transform pin;
    private Transform stamp;
    private TextMeshPro stampLabel;
    private readonly List<Material> stampMaterials = new List<Material>();
    private BoxCollider hitBox;

    private Vector3 restPosition;
    private float tilt;
    private float swayAmplitude = 0.8f;
    private float swayPeriod = 3.5f;
    private float swayPhase;
    private float hoverLift = 14f;
    private bool interactive;

    private float introDelay = -1f;
    private float introElapsed;
    private bool introSound;
    private float wobble;
    private float wobbleTime = 99f;
    private float stampElapsed;
    private bool stampAnimating;
    private bool stampSound;
    private float pulse;
    private bool hidden;
    private float shownLift;
    private float shownPeel;

    public Transform Body { get; private set; }
    public Vector2 Size { get; private set; }
    public object Data { get; set; }
    public bool IntroPlaying => introDelay >= 0f;

    public override bool Interactable
    {
        get => interactive && !hidden && !IntroPlaying;
        set => interactive = value;
    }

    /// <summary>pinPosition is where the pin goes (top centre of the paper, slightly below its edge).</summary>
    public static CareerPinnedPaper Create(Transform parent, string name, Vector3 pinPosition, Vector2 size,
        float tiltDegrees, Color paperColor, Color pinColor, bool interactive)
    {
        Transform root = CareerBoardKit.Node(name, parent, pinPosition);
        var paper = root.gameObject.AddComponent<CareerPinnedPaper>();
        paper.Build(size, tiltDegrees, paperColor, pinColor, interactive);
        return paper;
    }

    private void Build(Vector2 size, float tiltDegrees, Color paperColor, Color pinColor, bool isInteractive)
    {
        Size = size;
        tilt = tiltDegrees;
        interactive = isInteractive;
        restPosition = transform.localPosition;
        swayPhase = Random.Range(0f, Mathf.PI * 2f);
        swayPeriod = Random.Range(3.1f, 4.2f);

        // The pin sits 18 px below the top edge, so the body hangs from it.
        Body = CareerBoardKit.Node("Body", transform, new Vector3(0f, -(size.y * 0.5f - 18f), 0f));
        CareerBoardKit.Box(Body, "Cork shadow", new Vector3(5f, -7f, 3.2f), new Vector3(size.x, size.y, 1f),
            CareerBoardKit.CorkShadow, materials);
        paperMaterial = CareerBoardKit.Lit(paperColor, materials, 0.08f);
        CareerBoardKit.Box(Body, "Paper", Vector3.zero, new Vector3(size.x, size.y, PaperThickness), paperMaterial);
        // Sticky strip along the top and a lifted bottom lip give the sheet a real paper feel.
        stripMaterial = CareerBoardKit.Lit(Color.Lerp(paperColor, Color.black, 0.08f), materials, 0.08f);
        CareerBoardKit.Box(Body, "Top strip", new Vector3(0f, size.y * 0.5f - 15f, -PaperThickness * 0.5f - 0.3f),
            new Vector3(size.x, 30f, 0.6f), stripMaterial);
        MeshRenderer lip = CareerBoardKit.Box(Body, "Curled lip", new Vector3(0f, -size.y * 0.5f + 5f, -2.6f),
            new Vector3(size.x, 11f, 2.5f), stripMaterial);
        lip.transform.localRotation = Quaternion.Euler(-24f, 0f, 0f);

        pin = CareerBoardKit.Pushpin(transform, new Vector3(0f, 0f, -PaperThickness * 0.5f - 1f), pinColor, materials);

        if (interactive)
            hitBox = AddHitBox(transform, new Vector3(0f, Body.localPosition.y, -6f), new Vector3(size.x, size.y, 16f));
        Apply(0f);
    }

    // =========================================================
    // LOOK
    // =========================================================

    public void SetPaperColor(Color color)
    {
        CareerBoardKit.SetColor(paperMaterial, color);
        CareerBoardKit.SetColor(stripMaterial, Color.Lerp(color, Color.black, 0.08f));
    }

    public void SetPinColor(Color color)
    {
        if (pin == null) return;
        foreach (Renderer renderer in pin.GetComponentsInChildren<Renderer>())
            if (renderer.name != "Needle" && renderer.name != "Shine") CareerBoardKit.SetColor(renderer.sharedMaterial, color);
    }

    public void SetSway(float amplitudeDegrees) => swayAmplitude = amplitudeDegrees;

    /// <summary>Gentle "look at me" pulse for the chapter the player should play next.</summary>
    public void SetPulse(bool on) => pulse = on ? 1f : 0f;

    /// <summary>A rubber stamp across the paper. slam = it thumps down now (new result).</summary>
    public void ShowStamp(string text, Color color, bool slam, float delay = 0f)
    {
        if (stamp == null) BuildStamp();
        stampLabel.text = text;
        stampLabel.color = color;
        foreach (Material material in stampMaterials) CareerBoardKit.SetColor(material, color);
        stamp.gameObject.SetActive(true);
        stampElapsed = slam ? -Mathf.Max(0f, delay) : 0f;
        stampAnimating = slam;
        stampSound = slam;
        if (!slam) { stamp.localScale = Vector3.one; stamp.localPosition = StampRest; }
    }

    public void HideStamp()
    {
        if (stamp != null) stamp.gameObject.SetActive(false);
        stampAnimating = false;
    }

    private Vector3 StampRest => new Vector3(Size.x * 0.03f, -Size.y * 0.385f, -PaperThickness * 0.5f - 1.2f);

    private void BuildStamp()
    {
        float w = Mathf.Min(Size.x - 70f, 128f), h = 34f, t = 3.5f;
        stamp = CareerBoardKit.Node("Stamp", Body, StampRest);
        stamp.localRotation = Quaternion.Euler(0f, 0f, -11f);
        Material ink = CareerBoardKit.Lit(Color.red, materials, 0.02f);
        stampMaterials.Add(ink);
        CareerBoardKit.Box(stamp, "Frame top", new Vector3(0f, h * 0.5f, 0f), new Vector3(w, t, 0.6f), ink);
        CareerBoardKit.Box(stamp, "Frame bottom", new Vector3(0f, -h * 0.5f, 0f), new Vector3(w, t, 0.6f), ink);
        CareerBoardKit.Box(stamp, "Frame left", new Vector3(-w * 0.5f, 0f, 0f), new Vector3(t, h + t, 0.6f), ink);
        CareerBoardKit.Box(stamp, "Frame right", new Vector3(w * 0.5f, 0f, 0f), new Vector3(t, h + t, 0.6f), ink);
        stampLabel = CareerBoardKit.Text(stamp, "Stamp text", "", 21f, new Vector3(0f, 0f, -0.6f), w - 12f, h - 6f, Color.red);
        stampLabel.characterSpacing = 3f;
    }

    // =========================================================
    // MOTION
    // =========================================================

    /// <summary>Drops the paper in and pins it, after delay seconds.</summary>
    public void PlayIntro(float delay, bool sound)
    {
        introDelay = Mathf.Max(0f, delay);
        introElapsed = 0f;
        introSound = sound;
        Apply(0f);
    }

    /// <summary>A flick: the paper swings on its pin and settles.</summary>
    public void Pluck(float strength = 1f)
    {
        wobble = Mathf.Max(wobble * Mathf.Exp(-wobbleTime * 3f), strength);
        wobbleTime = 0f;
    }

    /// <summary>Hides the paper (it is "held" by the chapter sheet); the pin stays on the board.</summary>
    public void SetLifted(bool lifted)
    {
        if (hidden == lifted) return;
        hidden = lifted;
        if (Body != null) Body.gameObject.SetActive(!lifted);
        if (!lifted) Pluck(0.9f);
    }

    public void SnapToRest()
    {
        introDelay = -1f;
        wobble = 0f;
        Apply(0f);
    }

    protected override void OnTapped() => Pluck(1f);

    private void Update() => Apply(Time.unscaledDeltaTime);

    private void Apply(float dt)
    {
        float time = Time.unscaledTime;
        Vector3 introOffset = Vector3.zero;
        float introSpin = 0f, introScale = 1f, pinDepth = 0f;
        bool showing = true;

        if (introDelay >= 0f)
        {
            introElapsed += dt;
            float t = introElapsed - introDelay;
            if (t < 0f)
            {
                showing = false;
            }
            else
            {
                float drop = Mathf.Clamp01(t / IntroDuration);
                float eased = HudTheme.EaseOutCubic(drop);
                introOffset = new Vector3(0f, 26f, -240f) * (1f - eased);
                introSpin = 12f * (1f - eased);
                introScale = Mathf.Lerp(1.1f, 1f, eased);
                float pinT = Mathf.Clamp01((t - IntroDuration * 0.7f) / PinDuration);
                pinDepth = Mathf.Lerp(-70f, 0f, pinT * pinT);
                if (pinT >= 1f)
                {
                    introDelay = -1f;
                    Pluck(0.75f);
                    if (introSound) PlayPaperSound();
                }
            }
        }
        SetRenderersVisible(showing);

        wobbleTime += dt;
        float wobbleAngle = wobble * 6f * Mathf.Exp(-wobbleTime * 3f) * Mathf.Sin(wobbleTime * 14f);
        if (wobbleTime > 3f) wobble = 0f;

        float sway = Mathf.Sin(time * (2f * Mathf.PI / swayPeriod) + swayPhase) * swayAmplitude;
        bool hover = Interactable && Hovered;
        float targetLift = hover ? (Pressed ? hoverLift * 0.4f : hoverLift) : 0f;
        float targetPeel = hover && !Pressed ? -7f : 0f;
        float k = dt > 0f ? CareerBoardKit.Approach(14f, dt) : 1f;
        shownLift = Mathf.Lerp(shownLift, targetLift, k);
        shownPeel = Mathf.Lerp(shownPeel, targetPeel, k);
        float pulseScale = pulse > 0f ? 1f + 0.018f * (0.5f + 0.5f * Mathf.Sin(time * 3.2f)) : 1f;
        float pressScale = Interactable && Pressed ? 0.98f : 1f;

        transform.localPosition = restPosition + introOffset + Vector3.back * shownLift;
        transform.localRotation = Quaternion.Euler(shownPeel, 0f, tilt + sway + wobbleAngle + introSpin);
        transform.localScale = Vector3.one * (introScale * pulseScale * pressScale);
        if (pin != null) pin.localPosition = new Vector3(0f, 0f, -PaperThickness * 0.5f - 1f + pinDepth);

        if (stampAnimating && stamp != null)
        {
            stampElapsed += dt;
            stamp.gameObject.SetActive(stampElapsed >= 0f);
            float s = Mathf.Clamp01(stampElapsed / StampDuration);
            float eased = s * s;
            stamp.localScale = Vector3.one * Mathf.Lerp(2.6f, 1f, eased);
            stamp.localPosition = StampRest + Vector3.back * Mathf.Lerp(140f, 0f, eased);
            if (s >= 1f)
            {
                stampAnimating = false;
                Pluck(0.8f);
                if (stampSound && AudioManager.Instance != null) AudioManager.Instance.PlayAchievementSFX();
            }
        }

        if (hitBox != null) hitBox.enabled = Interactable;
    }

    private bool renderersVisible = true;

    private void SetRenderersVisible(bool visible)
    {
        if (renderersVisible == visible && visible) return;
        renderersVisible = visible;
        foreach (Renderer renderer in GetComponentsInChildren<Renderer>(true)) renderer.enabled = visible;
    }

    private static void PlayPaperSound()
    {
        if (Application.isPlaying && AudioManager.Instance != null) AudioManager.Instance.PlayPaperSFX();
    }

    private void OnDestroy()
    {
        foreach (Material material in materials) CareerBoardKit.Dispose(material);
    }
}
