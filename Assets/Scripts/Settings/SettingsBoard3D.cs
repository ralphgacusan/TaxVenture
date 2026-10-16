using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

/// <summary>
/// PURPOSE:
/// The game's settings, shown inside the 3D Menu: tapping the 3D SETTINGS
/// button drops a navy control board with a brass rim on two cables right in
/// front of the main menu camera (same angle and office background as the
/// main menu). It has a SETTINGS name plate, a row per option (Music, SFX)
/// with a round icon badge, world lettering and a chunky 3D pill switch, and
/// the brass BACK button. While it is open the main menu's 3D buttons are
/// hidden; BACK (or Escape / Android back) lifts the board away and brings
/// them back. There is no separate Settings scene any more.
///
/// It shares the look of the career board (HudTheme palette and fonts,
/// navy/brass plates, the 3D push button) but is its own object. The board is
/// drawn by a URP overlay camera on its own layer, so office props never cut
/// through it, with a light dim behind it.
///
/// Behaviour matches the old SettingsPanel: the switches call AudioManager
/// exactly like SettingsUI (the select sound plays before music changes, and
/// SFX turns on before / off after its click), and AudioManager saves them.
/// Other scenes that ask for settings set MenuEntry.OpenSettings and load the
/// 3D Menu, which opens the board on arrival.
///
/// IN-GAME: InGameSettings adds the same board to gameplay scenes and calls
/// ConfigureInGame(). Then it drops in front of the gameplay camera current at
/// open time, hides no menu objects, BACK just closes it, and Opened / Closed
/// let InGameSettings pause and resume the game. Look and behaviour are the same.
/// </summary>
public sealed class SettingsBoard3D : MonoBehaviour
{
    private const float Distance = 10f;
    private const float DropDuration = 0.7f;
    private const float LeaveDuration = 0.4f;
    /// <summary>Unnamed layer for the board (Doc3DLayer uses 31).</summary>
    private const int BoardLayer = 30;

    private static readonly Vector2 BoardSize = new Vector2(820f, 600f);
    private static readonly Vector2 RowSize = new Vector2(720f, 130f);
    private static readonly Vector2 SwitchSize = new Vector2(170f, 76f);
    private static readonly Color BoardNavy = new Color32(24, 34, 50, 255);
    private static readonly Color InsetNavy = new Color32(17, 25, 38, 255);
    private static readonly Color RowNavy = new Color32(32, 46, 66, 255);
    private static readonly Color Mint = new Color32(96, 214, 168, 255);

    [Tooltip("The main menu camera; the board drops in front of it. Empty = Camera.main.")]
    [SerializeField] private Camera viewCamera;
    [Tooltip("The menu's camera manager; settings only open on the main menu view (not the level select or mid-transition).")]
    [SerializeField] private ToggleCamera cameraManager;
    [Tooltip("Main menu objects hidden while settings are open (the 3D menu buttons), restored on close.")]
    [SerializeField] private string[] hideWhileOpen = { "play_button", "tutorial_button", "settings_button", "sidequest_button" };
    [Tooltip("How dark the office gets behind the board (0 = not at all).")]
    [SerializeField, Range(0f, 0.8f)] private float backdropDim = 0.3f;
    [Tooltip("In a gameplay scene (set by InGameSettings): face the gameplay camera at open time and ignore MenuEntry.OpenSettings.")]
    [SerializeField] private bool inGame;

    private readonly List<Material> materials = new List<Material>();
    private readonly List<Transform> rows = new List<Transform>();
    private readonly List<KeyValuePair<GameObject, bool>> hidden = new List<KeyValuePair<GameObject, bool>>();
    private Transform root;
    private Transform board;
    private Material embossed;
    private SettingsToggle3D musicToggle;
    private SettingsToggle3D sfxToggle;
    private TextMeshPro musicState;
    private TextMeshPro sfxState;
    private CareerBoardButton backButton;
    private Camera overlayCamera;
    private Camera stackedOn;
    private Image dim;

    private float boardScale = 0.0136f;
    private float lastAspect = -1f;
    private float elapsed;
    private float leaveElapsed = -1f;
    private bool leaving;
    private bool dropStarted;
    private bool open;
    private bool pendingOpen;

    private CareerBoardTarget hoverTarget;
    private CareerBoardTarget pressTarget;

    public SettingsToggle3D MusicToggle => musicToggle;
    public SettingsToggle3D SfxToggle => sfxToggle;
    public CareerBoardButton BackButton => backButton;

    /// <summary>True from the moment settings open until the board has lifted away.</summary>
    public bool IsOpen => open;

    public bool InGame => inGame;

    /// <summary>Raised when the board starts dropping in.</summary>
    public event System.Action Opened;
    /// <summary>Raised when the lift-away has finished and the board is hidden.</summary>
    public event System.Action Closed;

    /// <summary>
    /// Gameplay scenes: no menu camera manager, no menu buttons to hide, and the
    /// board faces whichever gameplay camera is live when it opens. Call before Start.
    /// </summary>
    public void ConfigureInGame()
    {
        inGame = true;
        cameraManager = null;
        hideWhileOpen = new string[0];
    }

    // =========================================================
    // LIFECYCLE
    // =========================================================

    private void Start()
    {
        if (viewCamera == null) viewCamera = Camera.main;
        Build();
        UseOverlayCamera();
        BuildDim();
        root.gameObject.SetActive(false);
        // Another scene asked for settings: open as soon as the main menu view is ready.
        pendingOpen = !inGame && MenuEntry.ConsumeOpenSettings();
    }

    private void OnDestroy()
    {
        foreach (Material material in materials) CareerBoardKit.Dispose(material);
    }

    private void Update()
    {
        TickButtonPops(Time.unscaledDeltaTime);
        if (pendingOpen && !open && Open()) pendingOpen = false;
        if (!open || root == null) return;
        float dt = Time.unscaledDeltaTime;
        FitToCamera();
        SyncOverlayCamera();
        elapsed += dt;
        Apply(dt);
        if (!open) return;     // closed this frame (lift-away finished)
        if (dim != null)
            dim.color = new Color(0.02f, 0.03f, 0.05f, Mathf.MoveTowards(dim.color.a, leaving ? 0f : backdropDim, dt * 1.2f));

        bool ready = !leaving && elapsed > DropDuration * 0.8f;
        if (musicToggle != null) musicToggle.Interactable = ready;
        if (sfxToggle != null) sfxToggle.Interactable = ready;
        if (backButton != null) backButton.Interactable = ready;
        if (ready)
        {
            HandlePointer();
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) GoBack();
        }
        else ClearPointer();
    }

    // =========================================================
    // OPEN / CLOSE
    // =========================================================

    /// <summary>Drops the settings board in front of the main menu camera and hides the menu buttons.</summary>
    public bool Open()
    {
        if (open) return true;
        if (root == null) Build();
        if (inGame) FollowGameplayCamera();
        if (cameraManager != null && (cameraManager.IsTransitioning || cameraManager.IsLevelSelectorVisible)) return false;
        if (viewCamera == null || !viewCamera.isActiveAndEnabled) return false;

        open = true;
        leaving = false;
        leaveElapsed = -1f;
        dropStarted = true;
        elapsed = 0f;
        HideMenuButtons();
        RefreshFromAudio(animate: false);
        lastAspect = -1f;
        root.gameObject.SetActive(true);
        if (dim != null) dim.color = new Color(0.02f, 0.03f, 0.05f, 0f);
        FitToCamera();
        Apply(0f);
        if (AudioManager.Instance != null) AudioManager.Instance.PlaySelectSFX();
        Opened?.Invoke();
        return true;
    }

    /// <summary>Called when the lift-away animation ends.</summary>
    private void FinishClose()
    {
        leaveElapsed = -1f;
        leaving = false;
        open = false;
        ClearPointer();
        root.gameObject.SetActive(false);
        RestoreMenuButtons();
        Closed?.Invoke();
    }

    /// <summary>Pops each visible menu button away (a small swell, then shrink), one after another.</summary>
    private void HideMenuButtons()
    {
        hidden.Clear();
        if (hideWhileOpen == null || hideWhileOpen.Length == 0) return;
        int order = 0;
        foreach (GameObject sceneRoot in gameObject.scene.GetRootGameObjects())
            foreach (Transform node in sceneRoot.GetComponentsInChildren<Transform>(true))
                if (hideWhileOpen.Contains(node.name) && !node.IsChildOf(transform))
                {
                    GameObject button = node.gameObject;
                    bool wasActive = button.activeSelf;
                    hidden.Add(new KeyValuePair<GameObject, bool>(button, wasActive));
                    if (!wasActive) continue;
                    StartPop(button, showing: false, delay: order++ * PopStagger);
                }
    }

    /// <summary>Pops the buttons back in with a springy overshoot; ones that were hidden before stay hidden.</summary>
    private void RestoreMenuButtons()
    {
        int order = 0;
        foreach (KeyValuePair<GameObject, bool> entry in hidden)
        {
            if (entry.Key == null || !entry.Value) continue;
            StartPop(entry.Key, showing: true, delay: order++ * PopStagger);
        }
        hidden.Clear();
    }

    // =========================================================
    // MENU BUTTON POP ANIMATION
    // =========================================================

    private const float PopOutDuration = 0.24f;
    private const float PopInDuration = 0.42f;
    private const float PopStagger = 0.06f;

    private sealed class ButtonPop
    {
        public GameObject Button;
        public bool Showing;
        public float Delay;
        public float Elapsed;
    }

    private readonly List<ButtonPop> pops = new List<ButtonPop>();
    // Each button's own size, remembered the first time it is animated (a pop can start mid-pop).
    private readonly Dictionary<GameObject, Vector3> buttonScales = new Dictionary<GameObject, Vector3>();

    private void StartPop(GameObject button, bool showing, float delay)
    {
        if (!buttonScales.ContainsKey(button)) buttonScales[button] = button.transform.localScale;
        pops.RemoveAll(p => p.Button == button);
        if (showing)
        {
            button.transform.localScale = Vector3.zero;
            button.SetActive(true);
        }
        pops.Add(new ButtonPop { Button = button, Showing = showing, Delay = delay });
    }

    private void TickButtonPops(float dt)
    {
        for (int i = pops.Count - 1; i >= 0; i--)
        {
            ButtonPop pop = pops[i];
            if (pop.Button == null) { pops.RemoveAt(i); continue; }
            Vector3 size = buttonScales[pop.Button];
            pop.Elapsed += dt;
            float time = pop.Elapsed - pop.Delay;
            if (time < 0f) continue;

            if (pop.Showing)
            {
                float t = Mathf.Clamp01(time / PopInDuration);
                pop.Button.transform.localScale = size * HudTheme.EaseOutBack(t);
                if (t >= 1f) { pop.Button.transform.localScale = size; pops.RemoveAt(i); }
            }
            else
            {
                // Swell to 112% in the first third, then shrink to nothing.
                float t = Mathf.Clamp01(time / PopOutDuration);
                float factor = t < 0.33f ? Mathf.Lerp(1f, 1.12f, HudTheme.EaseOutCubic(t / 0.33f))
                    : Mathf.Lerp(1.12f, 0f, Mathf.Pow((t - 0.33f) / 0.67f, 2f));
                pop.Button.transform.localScale = size * factor;
                if (t >= 1f)
                {
                    pop.Button.SetActive(false);
                    pop.Button.transform.localScale = size;
                    pops.RemoveAt(i);
                }
            }
        }
    }

    /// <summary>Editor captures: builds and shows the board at rest (no menu buttons are touched).</summary>
    public void PreparePreview(bool music, bool sfx)
    {
        if (viewCamera == null) viewCamera = Camera.main;
        Build();
        root.gameObject.SetActive(true);
        ShowValues(music, sfx, animate: false);
        lastAspect = -1f;
        FitToCamera();
        Apply(0f);
    }

    // =========================================================
    // BUILD
    // =========================================================

    /// <summary>Builds the board (also used by the editor capture).</summary>
    public void Build()
    {
        if (root != null) return;
        if (viewCamera == null) viewCamera = Camera.main;
        root = new GameObject("Settings Board 3D").transform;
        root.SetParent(transform, false);
        root.gameObject.layer = BoardLayer;
        FitToCamera();
        embossed = CareerBoardKit.EmbossedMaterial(HudTheme.Font, materials);

        board = CareerBoardKit.Node("Board", root, Vector3.zero);
        float w = BoardSize.x, h = BoardSize.y;

        // Cables up to the ceiling, with brass clamps on the board.
        Material steel = CareerBoardKit.Lit(new Color32(60, 66, 76, 255), materials, 0.7f);
        Material brass = CareerBoardKit.Lit(HudTheme.Brass, materials, 0.65f);
        foreach (float x in new[] { -w * 0.36f, w * 0.36f })
        {
            CareerBoardKit.Box(board, "Cable", new Vector3(x, h * 0.5f + 500f, 4f), new Vector3(3.5f, 500f, 3.5f), steel, PrimitiveType.Cylinder);
            CareerBoardKit.Box(board, "Clamp", new Vector3(x, h * 0.5f + 6f, 0f), new Vector3(26f, 22f, 26f), brass, PrimitiveType.Cylinder);
        }

        // Slab: brass rim, navy body, recessed face, corner bolts.
        CareerBoardKit.Box(board, "Shadow", new Vector3(10f, -16f, 24f), new Vector3(w, h, 4f), new Color32(10, 14, 20, 255), materials);
        CareerBoardKit.Box(board, "Rim", new Vector3(0f, 0f, 6f), new Vector3(w + 18f, h + 18f, 28f), brass);
        CareerBoardKit.Box(board, "Body", Vector3.zero, new Vector3(w, h, 34f), BoardNavy, materials);
        CareerBoardKit.Box(board, "Face", new Vector3(0f, 0f, -17.5f), new Vector3(w - 40f, h - 40f, 2f), InsetNavy, materials);
        foreach (Vector2 corner in new[] { new Vector2(-1, 1), new Vector2(1, 1), new Vector2(-1, -1), new Vector2(1, -1) })
            CareerBoardKit.Box(board, "Bolt", new Vector3(corner.x * (w * 0.5f - 20f), corner.y * (h * 0.5f - 20f), -19f),
                new Vector3(12f, 12f, 6f), HudTheme.BrassSoft, materials, PrimitiveType.Sphere);

        BuildPlate(new Vector3(0f, h * 0.5f, -22f), 340f);

        TextMeshPro section = CareerBoardKit.Text(board, "Section", "AUDIO", 17f, new Vector3(-w * 0.5f + 40f + 100f, 222f, -19f),
            200f, 24f, HudTheme.BrassSoft, TextAlignmentOptions.MidlineLeft);
        section.characterSpacing = 6f;

        musicToggle = BuildRow("Music", "MUSIC", "Background music", 120f, HudTheme.Exp, MusicIcon, out musicState);
        sfxToggle = BuildRow("SFX", "SOUND FX", "Taps, paper and rewards", -40f, HudTheme.Active, SpeakerIcon, out sfxState);
        musicToggle.Changed += (_, on) => OnMusicChanged(on);
        sfxToggle.Changed += (_, on) => OnSfxChanged(on);

        CareerBoardKit.Text(board, "Footnote", "Changes are saved automatically", 15f, new Vector3(0f, -138f, -19f), 600f, 22f,
            HudTheme.TextMuted, TextAlignmentOptions.Center, FontStyles.Normal);
        backButton = CareerBoardButton.Create(board, "Back Button", "<  BACK", new Vector3(0f, -214f, -26f),
            new Vector2(240f, 72f), HudTheme.Brass, new Color32(40, 30, 14, 255), 26f);
        backButton.Tapped += _ => GoBack();
        CareerBoardKit.SetLayerRecursively(root, BoardLayer);
    }

    private void BuildPlate(Vector3 center, float width)
    {
        Transform plate = CareerBoardKit.Node("Title Plate", board, center);
        CareerBoardKit.Box(plate, "Shadow", new Vector3(4f, -6f, 8f), new Vector3(width, 70f, 4f), new Color32(10, 14, 20, 255), materials);
        CareerBoardKit.Box(plate, "Rim", Vector3.zero, new Vector3(width, 70f, 12f), CareerBoardKit.Lit(HudTheme.Brass, materials, 0.65f));
        CareerBoardKit.Box(plate, "Face", new Vector3(0f, 0f, -5f), new Vector3(width - 12f, 58f, 6f), HudTheme.PanelInner, materials);
        CareerBoardKit.Box(plate, "Screw left", new Vector3(-width * 0.5f + 20f, 0f, -8.5f), new Vector3(8f, 8f, 2f),
            HudTheme.BrassSoft, materials, PrimitiveType.Sphere);
        CareerBoardKit.Box(plate, "Screw right", new Vector3(width * 0.5f - 20f, 0f, -8.5f), new Vector3(8f, 8f, 2f),
            HudTheme.BrassSoft, materials, PrimitiveType.Sphere);
        TextMeshPro label = CareerBoardKit.Text(plate, "Label", "SETTINGS", 34f, new Vector3(0f, 0f, -8.5f), width - 60f, 50f, HudTheme.BrassSoft);
        label.characterSpacing = 10f;
    }

    private delegate void IconBuilder(Transform parent, Material ink);

    private SettingsToggle3D BuildRow(string id, string title, string subtitle, float y, Color badge, IconBuilder icon,
        out TextMeshPro state)
    {
        Transform row = CareerBoardKit.Node(id + " Row", board, new Vector3(0f, y, -19f));
        rows.Add(row);
        Material rowMaterial = CareerBoardKit.Lit(RowNavy, materials, 0.2f);
        CareerBoardKit.Box(row, "Panel", Vector3.zero, new Vector3(RowSize.x, RowSize.y, 4f), rowMaterial);
        CareerBoardKit.Box(row, "Edge", new Vector3(0f, -RowSize.y * 0.5f + 1.5f, -2.2f), new Vector3(RowSize.x - 24f, 2f, 0.5f),
            new Color(HudTheme.Brass.r, HudTheme.Brass.g, HudTheme.Brass.b, 1f) * 0.75f, materials);

        float left = -RowSize.x * 0.5f;
        // Round icon badge.
        Transform iconRoot = CareerBoardKit.Node("Icon", row, new Vector3(left + 70f, 0f, -4f));
        Disc(iconRoot, "Outline", 92f, 0f, 6f, CareerBoardKit.Lit(new Color32(14, 18, 26, 255), materials, 0.2f));
        Disc(iconRoot, "Badge", 82f, -3f, 6f, CareerBoardKit.Lit(badge, materials, 0.45f));
        Transform glyph = CareerBoardKit.Node("Glyph", iconRoot, new Vector3(0f, 0f, -7f));
        icon(glyph, CareerBoardKit.Lit(new Color32(20, 26, 36, 255), materials, 0.3f));

        TextMeshPro label = CareerBoardKit.Text(row, "Title", title, 34f, new Vector3(left + 130f + 160f, 18f, -3f), 320f, 44f,
            HudTheme.TextPrimary, TextAlignmentOptions.MidlineLeft, FontStyles.Bold, null, embossed);
        label.characterSpacing = 3f;
        state = CareerBoardKit.Text(row, "State", subtitle, 17f, new Vector3(left + 130f + 160f, -22f, -3f), 320f, 24f,
            HudTheme.TextMuted, TextAlignmentOptions.MidlineLeft, FontStyles.Normal);

        return SettingsToggle3D.Create(row, id + " Switch", Vector3.zero, RowSize, rowMaterial,
            new Vector3(RowSize.x * 0.5f - 130f, 0f, -4f), SwitchSize, Mint);
    }

    private static MeshRenderer Disc(Transform parent, string name, float diameter, float z, float depth, Material material)
    {
        MeshRenderer disc = CareerBoardKit.Box(parent, name, new Vector3(0f, 0f, z), new Vector3(diameter, depth * 0.5f, diameter),
            material, PrimitiveType.Cylinder);
        disc.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        return disc;
    }

    /// <summary>A beamed pair of eighth notes.</summary>
    private static void MusicIcon(Transform parent, Material ink)
    {
        foreach (float x in new[] { -12f, 12f })
        {
            MeshRenderer head = CareerBoardKit.Box(parent, "Note head", new Vector3(x - 5f, -14f, 0f), new Vector3(17f, 13f, 6f), ink, PrimitiveType.Sphere);
            head.transform.localRotation = Quaternion.Euler(0f, 0f, 20f);
            CareerBoardKit.Box(parent, "Stem", new Vector3(x + 2.5f, 4f, 0f), new Vector3(4f, 36f, 5f), ink);
        }
        MeshRenderer beam = CareerBoardKit.Box(parent, "Beam", new Vector3(2.5f, 22f, 0f), new Vector3(30f, 7f, 5f), ink);
        beam.transform.localRotation = Quaternion.Euler(0f, 0f, 8f);
    }

    /// <summary>A speaker with two sound waves.</summary>
    private static void SpeakerIcon(Transform parent, Material ink)
    {
        CareerBoardKit.Box(parent, "Body", new Vector3(-16f, 0f, 0f), new Vector3(10f, 16f, 6f), ink);
        MeshRenderer cone = CareerBoardKit.Box(parent, "Cone", new Vector3(-6f, 0f, 0f), new Vector3(17f, 17f, 6f), ink);
        cone.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
        CareerBoardKit.Box(parent, "Cone back", new Vector3(-9f, 0f, 0.5f), new Vector3(10f, 26f, 6f), ink);
        for (int i = 0; i < 2; i++)
        {
            float x = 10f + i * 9f, length = 14f + i * 12f;
            CareerBoardKit.Box(parent, "Wave top", new Vector3(x, length * 0.25f, 0f), new Vector3(4f, length * 0.55f, 5f), ink)
                .transform.localRotation = Quaternion.Euler(0f, 0f, -28f);
            CareerBoardKit.Box(parent, "Wave bottom", new Vector3(x, -length * 0.25f, 0f), new Vector3(4f, length * 0.55f, 5f), ink)
                .transform.localRotation = Quaternion.Euler(0f, 0f, 28f);
        }
    }

    // =========================================================
    // SETTINGS (same behaviour as SettingsUI)
    // =========================================================

    /// <summary>Shows the saved values (AudioManager owns them).</summary>
    public void RefreshFromAudio(bool animate)
    {
        bool music = AudioManager.Instance == null || AudioManager.Instance.IsMusicEnabled();
        bool sfx = AudioManager.Instance == null || AudioManager.Instance.IsSFXEnabled();
        ShowValues(music, sfx, animate);
    }

    /// <summary>Editor captures: shows the given values without touching audio.</summary>
    public void ShowValues(bool music, bool sfx, bool animate)
    {
        if (musicToggle == null) return;
        musicToggle.SetOn(music, animate);
        sfxToggle.SetOn(sfx, animate);
        UpdateStates();
    }

    private void OnMusicChanged(bool enabled)
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySelectSFX();
            AudioManager.Instance.SetMusicEnabled(enabled);
        }
        UpdateStates();
    }

    private void OnSfxChanged(bool enabled)
    {
        if (AudioManager.Instance != null)
        {
            if (enabled)
            {
                // Turn SFX on first so this click can be heard.
                AudioManager.Instance.SetSFXEnabled(true);
                AudioManager.Instance.PlaySelectSFX();
            }
            else
            {
                // Click while SFX is still on, then turn it off.
                AudioManager.Instance.PlaySelectSFX();
                AudioManager.Instance.SetSFXEnabled(false);
            }
        }
        UpdateStates();
    }

    private void UpdateStates()
    {
        musicState.text = $"Background music  ·  <color=#{Hex(musicToggle.IsOn)}>{(musicToggle.IsOn ? "ON" : "OFF")}</color>";
        sfxState.text = $"Taps, paper and rewards  ·  <color=#{Hex(sfxToggle.IsOn)}>{(sfxToggle.IsOn ? "ON" : "OFF")}</color>";
    }

    private static string Hex(bool on) => ColorUtility.ToHtmlStringRGB(on ? Mint : HudTheme.TextMuted);

    private void GoBack()
    {
        if (leaving) return;
        leaving = true;
        leaveElapsed = 0f;
        ClearPointer();
        if (AudioManager.Instance != null) AudioManager.Instance.PlaySelectSFX();
    }

    // =========================================================
    // MOTION
    // =========================================================

    private void Apply(float dt)
    {
        if (board == null) return;
        float time = Time.unscaledTime;

        // Drop in on the cables, then a damped swing and a very slow idle sway.
        float drop = !Application.isPlaying ? 1f : dropStarted ? Mathf.Clamp01(elapsed / DropDuration) : 0f;
        float y = Mathf.LerpUnclamped(980f, 0f, HudTheme.EaseOutBack(drop));
        float settle = Mathf.Max(0f, elapsed - DropDuration);
        float swing = drop >= 1f ? 2.2f * Mathf.Exp(-settle * 2.6f) * Mathf.Sin(settle * 7f) : 0f;
        float idle = Mathf.Sin(time * 1.25f) * 0.3f;

        if (leaveElapsed >= 0f)
        {
            leaveElapsed += dt;
            float t = Mathf.Clamp01(leaveElapsed / LeaveDuration);
            y += 1100f * t * t;
            if (t >= 1f && Application.isPlaying)
            {
                leaveElapsed = -1f;
                FinishClose();
            }
        }

        board.localPosition = new Vector3(0f, y, 0f);
        board.localRotation = Quaternion.Euler(0f, 0f, swing + (Application.isPlaying ? idle : 0f));

        // Rows pop in one after the other as the board lands.
        for (int i = 0; i < rows.Count; i++)
        {
            float t = Application.isPlaying ? Mathf.Clamp01((elapsed - DropDuration * 0.6f - i * 0.1f) / 0.3f) : 1f;
            rows[i].localScale = Vector3.one * Mathf.LerpUnclamped(0.85f, 1f, HudTheme.EaseOutBack(t));
        }
    }

    /// <summary>Keeps the board in front of the camera and sized to fit any screen shape.</summary>
    private void FitToCamera()
    {
        if (root == null || viewCamera == null) return;
        float aspect = viewCamera.aspect > 0f ? viewCamera.aspect : 16f / 9f;
        Transform cam = viewCamera.transform;
        root.SetPositionAndRotation(cam.position + cam.forward * Distance, cam.rotation);
        if (Mathf.Abs(aspect - lastAspect) < 0.001f) return;
        lastAspect = aspect;
        float visibleHeight = 2f * Distance * Mathf.Tan(viewCamera.fieldOfView * 0.5f * Mathf.Deg2Rad);
        boardScale = Mathf.Min(visibleHeight / 860f, visibleHeight * aspect / 1000f);
        root.localScale = Vector3.one * boardScale;
    }

    // =========================================================
    // OVERLAY CAMERA
    // =========================================================

    /// <summary>Draws the board with a URP overlay camera on its own layer, so the office can never cover it.</summary>
    private void UseOverlayCamera()
    {
        if (viewCamera == null) return;
        UniversalAdditionalCameraData baseData = viewCamera.GetUniversalAdditionalCameraData();
        if (baseData == null || baseData.renderType != CameraRenderType.Base) return;

        if (overlayCamera == null)
        {
            var go = new GameObject("Settings Board Camera");
            overlayCamera = go.AddComponent<Camera>();
            overlayCamera.cullingMask = 1 << BoardLayer;
            overlayCamera.clearFlags = CameraClearFlags.Depth;
            UniversalAdditionalCameraData overlayData = overlayCamera.GetUniversalAdditionalCameraData();
            overlayData.renderType = CameraRenderType.Overlay;
            overlayData.renderShadows = false;
        }
        // Moving to another base camera (in-game): leave the old stack first.
        if (stackedOn != null && stackedOn != viewCamera)
        {
            UniversalAdditionalCameraData oldData = stackedOn.GetUniversalAdditionalCameraData();
            if (oldData != null) oldData.cameraStack.Remove(overlayCamera);
        }
        overlayCamera.transform.SetParent(viewCamera.transform, false);
        // Last in the stack, so it draws over any other overlay.
        baseData.cameraStack.Remove(overlayCamera);
        baseData.cameraStack.Add(overlayCamera);
        stackedOn = viewCamera;
        viewCamera.cullingMask &= ~(1 << BoardLayer);
        SyncOverlayCamera();
    }

    /// <summary>In-game: drop in front of the camera the player is looking through right now.</summary>
    private void FollowGameplayCamera()
    {
        CameraController controller = CameraController.Instance;
        Camera current = controller != null && controller.MainCamera != null ? controller.MainCamera : Camera.main;
        if (current == null) return;
        if (current != viewCamera) lastAspect = -1f;
        viewCamera = current;
        UseOverlayCamera();
    }

    private void SyncOverlayCamera()
    {
        if (overlayCamera == null || viewCamera == null) return;
        overlayCamera.fieldOfView = viewCamera.fieldOfView;
        overlayCamera.nearClipPlane = viewCamera.nearClipPlane;
        overlayCamera.farClipPlane = viewCamera.farClipPlane;
    }

    /// <summary>A soft dark veil behind the board (on the board layer) once the office is showing.</summary>
    private void BuildDim()
    {
        if (root == null) return;
        var go = new GameObject("Backdrop Dim", typeof(RectTransform), typeof(Canvas));
        go.layer = BoardLayer;
        go.transform.SetParent(root, false);
        var canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = -50;
        var rect = (RectTransform)go.transform;
        rect.sizeDelta = new Vector2(6000f, 4000f);
        rect.localPosition = new Vector3(0f, 0f, 300f);
        RectTransform fill = HudTheme.CreateRect("Dim", rect);
        HudTheme.Stretch(fill);
        fill.gameObject.layer = BoardLayer;
        dim = fill.gameObject.AddComponent<Image>();
        dim.color = new Color(0.02f, 0.03f, 0.05f, 0f);
        dim.raycastTarget = false;
    }

    // =========================================================
    // INPUT
    // =========================================================

    private void HandlePointer()
    {
        Pointer pointer = Pointer.current;
        if (pointer == null || viewCamera == null) return;
        Vector2 position = pointer.position.ReadValue();
        bool pressed = pointer.press.wasPressedThisFrame;
        bool released = pointer.press.wasReleasedThisFrame;
        bool canHover = pointer is Mouse || pointer is Pen;

        CareerBoardTarget target = RaycastTarget(position);
        SetHover(canHover || pointer.press.isPressed ? target : null);
        if (pressed)
        {
            pressTarget = target;
            if (pressTarget != null) pressTarget.SetPressed(true);
        }
        if (released)
        {
            CareerBoardTarget pressedTarget = pressTarget;
            if (pressedTarget != null) pressedTarget.SetPressed(false);
            if (pressedTarget != null && pressedTarget == target) target.Tap();
            pressTarget = null;
            if (!canHover) SetHover(null);
        }
    }

    private CareerBoardTarget RaycastTarget(Vector2 screen)
    {
        Ray ray = viewCamera.ScreenPointToRay(screen);
        // Only the board layer: office furniture may stand between the camera and the board in world space.
        if (!Physics.Raycast(ray, out RaycastHit hit, Distance * 3f, 1 << BoardLayer, QueryTriggerInteraction.Collide)) return null;
        CareerBoardTarget target = hit.collider.GetComponentInParent<CareerBoardTarget>();
        return target != null && target.transform.IsChildOf(root) ? target : null;
    }

    private void SetHover(CareerBoardTarget target)
    {
        if (hoverTarget == target) return;
        if (hoverTarget != null) hoverTarget.SetHover(false);
        hoverTarget = target;
        if (hoverTarget != null) hoverTarget.SetHover(true);
    }

    private void ClearPointer()
    {
        SetHover(null);
        if (pressTarget != null) pressTarget.SetPressed(false);
        pressTarget = null;
    }

}
