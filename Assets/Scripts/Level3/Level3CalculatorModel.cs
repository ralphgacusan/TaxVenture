using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// PURPOSE:
/// The physical Level 3 desk calculator (T03), built once at runtime from
/// supplied art: a scaled instance of Synty's SM_Prop_Calculator_01 as the
/// body (the source prefab is never edited), plus readable parts of real
/// thickness in the office palette:
///
/// - a navy faceplate over the body's top, a dark display with mint digits
///   and a caption line (source in use, the expression, or an input problem);
/// - twenty raised keycaps (cream digits, navy operators, brass =, terracotta
///   clears), each with printed label and its own tap target;
/// - a paper roll with brass caps behind the display, and the tape strip it
///   feeds onto the desk beside the calculator (curled lip, visible thickness),
///   printed with the draft computation.
///
/// Local frame: +Y up out of the keys, +Z toward the display / roll (away
/// from the seated player), body base at y = 0. Sizes are meters at rest; the
/// owner scales the whole assembly when it lifts it toward the camera.
/// Presentation only: Level3Calculator decides every value.
/// </summary>
public sealed class Level3CalculatorModel
{
    /// <summary>Body length (along Z) the source mesh is fitted to.</summary>
    public const float BodyLength = 0.19f;
    public const float TapeWidth = 0.105f;
    public const float TapeGap = 0.012f;
    private const float FallbackWidth = 0.115f;
    private const float FallbackHeight = 0.026f;
    private const int TapeLinesShown = 3;
    public const float TapePoints = 136.5f;
    public const float TapeMinPoints = 70f;
    public const float TapePrintWidth = TapeWidth * 0.88f;
    public const float TapePageHeight = 0.085f - 0.018f;

    public static readonly Level3CalcKey[,] Layout =
    {
        { Level3CalcKey.Clear, Level3CalcKey.ClearEntry, Level3CalcKey.Backspace, Level3CalcKey.Divide },
        { Level3CalcKey.D7, Level3CalcKey.D8, Level3CalcKey.D9, Level3CalcKey.Multiply },
        { Level3CalcKey.D4, Level3CalcKey.D5, Level3CalcKey.D6, Level3CalcKey.Subtract },
        { Level3CalcKey.D1, Level3CalcKey.D2, Level3CalcKey.D3, Level3CalcKey.Add },
        { Level3CalcKey.D0, Level3CalcKey.Point, Level3CalcKey.Percent, Level3CalcKey.Equals }
    };

    private sealed class KeyCap
    {
        public Level3CalcKey Key;
        public Transform Cap;
        public Vector3 Rest;
        public float PressedUntil;
    }

    public Transform Root { get; private set; }
    /// <summary>Body width (X) after fitting the source mesh.</summary>
    public float Width { get; private set; }
    /// <summary>Body height (Y) after fitting.</summary>
    public float Height { get; private set; }
    /// <summary>True when the Synty body was used (false: a plain navy body stands in; logged).</summary>
    public bool UsesSourceBody { get; private set; }
    /// <summary>The whole calculator's tap target (resting on the desk: tap to pick it up).</summary>
    public Collider BodyTarget { get; private set; }
    public Collider MoveHandle { get; private set; }
    public Collider[] MoveBorders { get; private set; }
    public bool IsMoveHandle(Collider target) => target == BodyTarget || target == MoveHandle || (MoveBorders != null && System.Array.IndexOf(MoveBorders, target) >= 0);

    /// <summary>Local bounds of calculator + tape + roll (for on-screen fitting).</summary>
    public Bounds UnitBounds { get; private set; }

    private readonly Dictionary<Collider, KeyCap> keys = new Dictionary<Collider, KeyCap>();
    private readonly Level3DeskProps props;
    private TextMeshPro display;
    private TextMeshPro caption;
    private Vector3 inputRestScale;
    private float inputPopAt = -1f;
    public const float PlacementLift = 0.0015f;
    public static Vector3 PlacementOffset(float t) => Vector3.up * (PlacementLift * (1f - Mathf.SmoothStep(0f, 1f, t)));
    public static Vector3 PlacementScale(Vector3 rest, float t) => rest * (1f + 0.045f * Mathf.Sin(Mathf.Clamp01(t) * Mathf.PI));
    public void PopInput()
    {
        FinishInputPop();
        inputRestScale = display.transform.localScale;
        inputPopAt = Time.unscaledTime;
        display.color = Level3DeskProps.BrassSoft;
    }
    public void FinishInputPop()
    {
        if (inputPopAt >= 0f && display != null) display.transform.localScale = inputRestScale;
        inputPopAt = -1f;
        if (display != null) display.color = Level3DeskProps.DisplayInk;
    }
    private Transform tapeStrip;
    private TextMeshPro tapePrint;
    private float tapeLength;
    private Transform tapePaper;
    private TextMeshPro tapePageLabel;
    private BoxCollider tapePageTarget;
    private readonly List<string> printedLines = new List<string>();
    private int tapePage;
    public BoxCollider InputTarget { get; private set; }
    public GameObject InputFrame { get; private set; }
    public GameObject TapeSelectionFrame { get; private set; }
    public BoxCollider TapeTarget { get; private set; }
    private float tapeFeedStart = -1f;
    private float tapeFeedSeconds;

    private Level3CalculatorModel(Level3DeskProps props)
    {
        this.props = props;
    }

    public static Level3CalculatorModel Build(Transform parent, GameObject sourceBody, Level3DeskProps props)
    {
        var model = new Level3CalculatorModel(props);
        model.Create(parent, sourceBody);
        return model;
    }

    private void Create(Transform parent, GameObject sourceBody)
    {
        var root = new GameObject("Calculator");
        root.layer = parent.gameObject.layer;
        root.transform.SetParent(parent, false);
        Root = root.transform;

        UsesSourceBody = sourceBody != null && FitSourceBody(sourceBody);
        if (!UsesSourceBody)
        {
            Width = FallbackWidth;
            Height = FallbackHeight;
            props.Box("Body", Root, new Vector3(0f, Height * 0.5f, 0f), new Vector3(Width, Height, BodyLength), Level3DeskProps.Navy);
        }

        BuildFace();
        BuildRollAndTape();
        // Below the key targets, so a key is always the nearest hit; the whole body lifts the calculator from the desk.
        BodyTarget = props.Target("Calculator Target", Root, new Vector3(0f, Height * 0.5f, 0f), new Vector3(Width, Height, BodyLength + 0.03f));
        MoveBorders = new Collider[]
        {
            props.Target("Move calculator left rim", Root, new Vector3(-Width * 0.5f, Height + 0.002f, 0f), new Vector3(0.006f, 0.01f, BodyLength * 0.9f)),
            props.Target("Move calculator right rim", Root, new Vector3(Width * 0.5f, Height + 0.002f, 0f), new Vector3(0.006f, 0.01f, BodyLength * 0.9f)),
            props.Target("Move calculator bottom rim", Root, new Vector3(0f, Height + 0.002f, -BodyLength * 0.5f + 0.001f), new Vector3(Width, 0.01f, 0.006f))
        };

        float minX = -Width * 0.5f - TapeGap - TapeWidth - 0.002f;
        float maxZ = BodyLength * 0.5f + 0.03f;
        var bounds = new Bounds();
        bounds.SetMinMax(new Vector3(minX, 0f, -BodyLength * 0.5f), new Vector3(Width * 0.5f, Height + 0.04f, maxZ));
        UnitBounds = bounds;
        SetDisplay("0", "");
    }

    /// <summary>Instance the supplied prop, lay its long side along Z, scale it to BodyLength, base on y = 0. No colliders.</summary>
    private bool FitSourceBody(GameObject prefab)
    {
        GameObject body = Object.Instantiate(prefab, Root, false);
        body.name = "Body (" + prefab.name + ")";
        foreach (Collider collider in body.GetComponentsInChildren<Collider>(true)) Object.Destroy(collider);
        foreach (Transform t in body.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = Root.gameObject.layer;

        if (!TryLocalBounds(body, out Bounds raw)) { Object.Destroy(body); return false; }
        bool longX = raw.size.x > raw.size.z;
        if (longX) body.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
        float length = longX ? raw.size.x : raw.size.z;
        if (length <= 0.0001f) { Object.Destroy(body); return false; }
        float scale = BodyLength / length;
        body.transform.localScale = Vector3.one * scale;
        if (!TryLocalBounds(body, out Bounds fitted)) { Object.Destroy(body); return false; }
        body.transform.localPosition -= new Vector3(fitted.center.x, fitted.min.y, fitted.center.z);
        Width = fitted.size.x;
        Height = Mathf.Max(0.012f, fitted.size.y);
        foreach (Renderer renderer in body.GetComponentsInChildren<Renderer>(true))
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        return true;
    }

    /// <summary>The renderers' bounds in Root's local space (Root is at identity scale while building).</summary>
    private bool TryLocalBounds(GameObject body, out Bounds bounds)
    {
        bounds = new Bounds();
        bool any = false;
        foreach (MeshFilter filter in body.GetComponentsInChildren<MeshFilter>(true))
        {
            if (filter.sharedMesh == null) continue;
            Bounds mesh = filter.sharedMesh.bounds;
            Vector3 c = mesh.center, e = mesh.extents;
            for (int i = 0; i < 8; i++)
            {
                Vector3 corner = c + new Vector3((i & 1) == 0 ? -e.x : e.x, (i & 2) == 0 ? -e.y : e.y, (i & 4) == 0 ? -e.z : e.z);
                Vector3 local = Root.InverseTransformPoint(filter.transform.TransformPoint(corner));
                if (!any) { bounds = new Bounds(local, Vector3.zero); any = true; }
                else bounds.Encapsulate(local);
            }
        }
        return any;
    }

    // =========================================================
    // FACE: faceplate, display, keys
    // =========================================================

    private void BuildFace()
    {
        float plateW = Width * 0.92f, plateL = BodyLength * 0.94f, top = Height + 0.0015f;
        props.Box("Faceplate", Root, new Vector3(0f, top, 0f), new Vector3(plateW, 0.003f, plateL), Level3DeskProps.Navy);
        float surface = top + 0.0015f;

        // Display window: the far 24% of the plate, a dark recessed panel with a brass rim.
        float displayL = plateL * 0.22f, displayZ = plateL * 0.5f - displayL * 0.5f - plateL * 0.03f;
        props.Box("Display Rim", Root, new Vector3(0f, surface + 0.0005f, displayZ), new Vector3(plateW * 0.9f, 0.002f, displayL + 0.004f), Level3DeskProps.Brass);
        props.Box("Display", Root, new Vector3(0f, surface + 0.001f, displayZ), new Vector3(plateW * 0.86f, 0.0025f, displayL), Level3DeskProps.Display);
        float printY = surface + 0.0028f;
        display = props.Print("Display Digits", Root, new Vector3(0f, printY, displayZ + displayL * 0.12f), plateW * 0.8f, displayL * 0.56f, "0",
            Level3DeskProps.DisplayInk, TextAlignmentOptions.Right, bold: true);
        caption = props.Print("Display Caption", Root, new Vector3(0f, printY, displayZ - displayL * 0.32f), plateW * 0.8f, displayL * 0.26f, "",
            Level3DeskProps.DisplayInk, TextAlignmentOptions.Right);

        InputTarget = props.Target("Place selected amount", Root, new Vector3(0f, printY + 0.003f, displayZ),
            new Vector3(plateW * 0.9f, 0.008f, displayL + 0.004f));
        InputFrame = props.Frame("Input selected", Root, new Vector3(0f, printY, displayZ), plateW * 0.9f,
            displayL + 0.004f, 0.0012f, Level3DeskProps.Highlight);
        InputFrame.SetActive(false);

        // Keys: 4 columns x 5 rows below the display.
        float keysTop = displayZ - displayL * 0.5f - plateL * 0.04f;
        float keysBottom = -plateL * 0.5f + plateL * 0.03f;
        int rows = Layout.GetLength(0), cols = Layout.GetLength(1);
        float cellW = plateW * 0.94f / cols, cellL = (keysTop - keysBottom) / rows;
        for (int r = 0; r < rows; r++)
            for (int c = 0; c < cols; c++)
            {
                Level3CalcKey key = Layout[r, c];
                var center = new Vector3(-plateW * 0.47f + cellW * (c + 0.5f), surface + 0.0035f, keysTop - cellL * (r + 0.5f));
                AddKey(key, center, new Vector3(cellW * 0.82f, 0.006f, cellL * 0.8f));
            }
    }

    private void AddKey(Level3CalcKey key, Vector3 center, Vector3 size)
    {
        KeyStyle(key, out Color cap, out Color ink);
        GameObject capObject = props.Box("Key " + key, Root, center, size, cap);
        Transform capTransform = capObject.transform;
        // The label rides on the cap (a child of a scaled box would distort, so it is parented to a holder).
        var holder = new GameObject("Key " + key + " Top");
        holder.layer = Root.gameObject.layer;
        holder.transform.SetParent(Root, false);
        holder.transform.localPosition = center;
        capTransform.SetParent(holder.transform, true);
        props.Print("Label", holder.transform, new Vector3(0f, size.y * 0.5f + 0.0004f, 0f), size.x * 0.8f, size.z * 0.62f, Label(key), ink,
            TextAlignmentOptions.Center, bold: true);
        BoxCollider target = props.Target("Target", holder.transform, Vector3.zero, new Vector3(size.x / 0.82f, size.y + 0.01f, size.z / 0.8f));
        keys[target] = new KeyCap { Key = key, Cap = holder.transform, Rest = holder.transform.localPosition };
    }

    public static string Label(Level3CalcKey key)
    {
        switch (key)
        {
            case Level3CalcKey.Point: return ".";
            case Level3CalcKey.Add: return "+";
            case Level3CalcKey.Subtract: return "−";
            case Level3CalcKey.Multiply: return "×";
            case Level3CalcKey.Divide: return "÷";
            case Level3CalcKey.Percent: return "%";
            case Level3CalcKey.Equals: return "=";
            case Level3CalcKey.Clear: return "C";
            case Level3CalcKey.ClearEntry: return "CE";
            case Level3CalcKey.Backspace: return "DEL";
            default: return ((int)key - (int)Level3CalcKey.D0).ToString();
        }
    }

    private static void KeyStyle(Level3CalcKey key, out Color cap, out Color ink)
    {
        switch (key)
        {
            case Level3CalcKey.Equals: cap = Level3DeskProps.Brass; ink = Level3DeskProps.Ink; return;
            case Level3CalcKey.Add:
            case Level3CalcKey.Subtract:
            case Level3CalcKey.Multiply:
            case Level3CalcKey.Divide:
            case Level3CalcKey.Percent: cap = Level3DeskProps.NavyLight; ink = Level3DeskProps.Cream; return;
            case Level3CalcKey.Clear:
            case Level3CalcKey.ClearEntry:
            case Level3CalcKey.Backspace: cap = Level3DeskProps.Terracotta; ink = Level3DeskProps.Cream; return;
            default: cap = Level3DeskProps.Cream; ink = Level3DeskProps.Ink; return;
        }
    }

    public bool TryKey(Collider collider, out Level3CalcKey key)
    {
        key = default;
        if (collider == null || !keys.TryGetValue(collider, out KeyCap cap)) return false;
        key = cap.Key;
        return true;
    }

    /// <summary>Holds a key down while the finger is on it (released by Release or after a short press).</summary>
    public void Press(Collider collider, float seconds)
    {
        if (collider != null && keys.TryGetValue(collider, out KeyCap cap)) cap.PressedUntil = Time.unscaledTime + seconds;
    }

    /// <summary>Follow-up (Arthur's demonstrations): presses a key by its kind, as a finger would (presentation only).</summary>
    public void PressKey(Level3CalcKey key, float seconds)
    {
        foreach (KeyCap cap in keys.Values)
            if (cap.Key == key) cap.PressedUntil = Time.unscaledTime + seconds;
    }

    public void ReleaseAll()
    {
        foreach (KeyCap cap in keys.Values) cap.PressedUntil = 0f;
    }

    public void SetDisplay(string number, string line)
    {
        if (display != null && display.text != number) display.text = number;
        if (caption != null && caption.text != (line ?? "")) caption.text = line ?? "";
    }

    // =========================================================
    // ROLL AND TAPE
    // =========================================================

    private void BuildRollAndTape()
    {
        float rollZ = BodyLength * 0.5f + 0.011f, rollY = Height * 0.75f, rollLength = Width * 0.72f;
        props.Box("Roll Housing", Root, new Vector3(0f, rollY * 0.5f, rollZ + 0.004f), new Vector3(rollLength + 0.012f, rollY, 0.026f), Level3DeskProps.Navy);
        props.Roll("Paper Roll", Root, new Vector3(0f, rollY + 0.006f, rollZ), rollLength, 0.026f, Level3DeskProps.Paper);
        props.Roll("Roll Cap L", Root, new Vector3(-rollLength * 0.5f - 0.002f, rollY + 0.006f, rollZ), 0.004f, 0.03f, Level3DeskProps.Brass);
        props.Roll("Roll Cap R", Root, new Vector3(rollLength * 0.5f + 0.002f, rollY + 0.006f, rollZ), 0.004f, 0.03f, Level3DeskProps.Brass);
        MoveHandle = props.Target("Move calculator by roll", Root, new Vector3(0f, rollY + 0.012f, rollZ),
            new Vector3(rollLength, 0.02f, 0.028f));

        // The tape leaves the roll toward the player's left and lies on the desk beside the body:
        // a short curled lip down from the roll, then a flat strip that grows as lines are printed.
        float tapeX = -Width * 0.5f - TapeGap - TapeWidth * 0.5f;
        tapeLength = 0.085f;
        var lip = props.Box("Tape Lip", Root, new Vector3(tapeX + 0.004f, rollY * 0.5f + 0.003f, rollZ - 0.004f), new Vector3(TapeWidth, 0.0012f, rollY + 0.01f), Level3DeskProps.Paper);
        lip.transform.localRotation = Quaternion.Euler(-62f, 0f, 0f);

        var strip = new GameObject("Tape Strip");
        strip.layer = Root.gameObject.layer;
        strip.transform.SetParent(Root, false);
        // Anchored at the roll end: scaling Z feeds the strip toward the player.
        strip.transform.localPosition = new Vector3(tapeX, 0.0008f, BodyLength * 0.5f);
        tapeStrip = strip.transform;
        tapePaper = props.Box("Paper", tapeStrip, new Vector3(0f, 0f, -tapeLength * 0.5f), new Vector3(TapeWidth, 0.0012f, tapeLength), Level3DeskProps.Paper).transform;
        TapeTarget = props.Target("Drag printed tape", tapeStrip, new Vector3(0f, 0.003f, -tapeLength * 0.5f), new Vector3(TapeWidth + 0.008f, 0.012f, tapeLength));
        tapePrint = props.FixedPrint("Print", tapeStrip, new Vector3(0f, 0.0008f, -tapeLength * 0.5f), TapeWidth * 0.88f, tapeLength * 0.94f, "",
            Level3DeskProps.Ink, TapePoints, TextAlignmentOptions.Center);
        tapePrint.enableAutoSizing = true;
        tapePrint.fontSizeMin = TapeMinPoints;
        tapePrint.fontSizeMax = TapePoints;
        tapePageLabel = props.FixedPrint("Working page", tapeStrip, Vector3.zero, TapeWidth * 0.88f, 0.009f, "",
            Level3DeskProps.InkMuted, 45f, TextAlignmentOptions.Center);
        tapePageTarget = props.Target("Next working page", tapeStrip, Vector3.zero, new Vector3(TapeWidth, 0.014f, 0.012f));
        TapeSelectionFrame = props.Frame("Tape selected", tapeStrip, Vector3.zero, TapeWidth + 0.002f,
            tapeLength, 0.001f, Level3DeskProps.Highlight);
        TapeSelectionFrame.SetActive(false);
        SetTape(null, animate: false);
    }

    /// <summary>Prints the draft lines on the strip (null/empty: a blank stub at the roll). Animated feed when asked.</summary>
    public void SetTape(IList<string> lines, bool animate, float seconds = 0.2f)
    {
        bool empty = lines == null || lines.Count == 0;
        printedLines.Clear();
        if (!empty) foreach (string line in lines)
            // The baked HUD atlas contains ASCII minus; avoid a dynamic fallback
            // changing the measured advance on the tiny strip (payload is unchanged).
            printedLines.Add(System.Text.RegularExpressions.Regex.Replace(line, @"\s+[A-Z][A-Z ]*$", "").Replace('−', '-'));
        tapePage = LastTapePage(printedLines.Count);
        PrintTapePage();
        if (TapeTarget != null) TapeTarget.enabled = !empty;
        if (tapeStrip == null) return;
        float length = Mathf.Clamp((empty ? 0 : Mathf.Min(lines.Count, TapeLinesShown)) * 0.016f + 0.012f, 0.042f, tapeLength);
        tapePaper.localPosition = new Vector3(0f, 0f, -length * 0.5f);
        tapePaper.localScale = new Vector3(TapeWidth, 0.0012f, length);
        TapeTarget.center = Vector3.zero;
        TapeTarget.transform.localPosition = new Vector3(0f, 0.003f, -length * 0.5f);
        TapeTarget.size = new Vector3(TapeWidth + 0.008f, 0.012f, length);
        TapeSelectionFrame.transform.localPosition = new Vector3(0f, 0.0015f, -length * 0.5f);
        TapeSelectionFrame.transform.localScale = new Vector3(1f, 1f, length / tapeLength);
        if (empty) TapeSelectionFrame.SetActive(false);
        tapePrint.transform.localPosition = new Vector3(0f, 0.0008f, -length * 0.5f);
        bool pages = printedLines.Count > TapeLinesShown;
        tapePrint.transform.localPosition += Vector3.forward * (pages ? 0.005f : 0f);
        tapePrint.rectTransform.sizeDelta = new Vector2(TapeWidth * 0.88f, length - (pages ? 0.018f : 0.006f)) / Level3DeskProps.PrintScale;
        tapePageLabel.transform.localPosition = new Vector3(0f, 0.0009f, -length + 0.006f);
        tapePageTarget.transform.localPosition = new Vector3(0f, 0.004f, -length + 0.006f);
        tapePageLabel.gameObject.SetActive(pages);
        tapePageTarget.enabled = pages;
        if (empty)
        {
            tapeFeedStart = -1f;
            tapeStrip.localScale = new Vector3(1f, 1f, 0.08f);
            return;
        }
        if (animate)
        {
            // The print appears once the strip has fed out (it would squash while the strip grows).
            if (tapePrint != null) tapePrint.enabled = false;
            tapePageLabel.enabled = false;
            tapeFeedStart = Time.unscaledTime;
            tapeFeedSeconds = Mathf.Max(0.05f, seconds);
            tapeStrip.localScale = new Vector3(1f, 1f, 0.08f);
        }
        else
        {
            tapeFeedStart = -1f;
            tapeStrip.localScale = Vector3.one;
            if (tapePrint != null) tapePrint.enabled = true;
            tapePageLabel.enabled = true;
        }
    }

    // Long optional client chains use readable pages on the same short strip.
    // No discarded first lines, huge overflow, or permanently growing scroll.
    public bool NextTapePage(Collider target)
    {
        if (target != tapePageTarget || !tapePageTarget.enabled || IsFeeding) return false;
        tapePage = (tapePage + 1) % ((printedLines.Count + TapeLinesShown - 1) / TapeLinesShown);
        PrintTapePage();
        return true;
    }
    public static int LastTapePage(int lineCount) => System.Math.Max(0, (lineCount - 1) / TapeLinesShown);
    // Overlap a short final page so its operation and operands remain beside the latest result.
    public static int TapePageStart(int page, int lineCount) =>
        System.Math.Min(System.Math.Max(0, page) * TapeLinesShown, System.Math.Max(0, lineCount - TapeLinesShown));

    private void PrintTapePage()
    {
        int first = TapePageStart(tapePage, printedLines.Count);
        int end = Mathf.Min(first + TapeLinesShown, printedLines.Count);
        var text = new System.Text.StringBuilder();
        for (int i = first; i < end; i++)
            text.Append(i == printedLines.Count - 1 ? "<b>" + printedLines[i] + "</b>" : printedLines[i]).Append('\n');
        tapePrint.text = text.ToString().TrimEnd();
        tapePageLabel.text = (tapePage + 1) + "/" + ((printedLines.Count + TapeLinesShown - 1) / TapeLinesShown) + "  ›";
    }

    /// <summary>World position/rotation of the printed strip (the attach flight starts here).</summary>
    public Transform TapeStrip => tapeStrip;
    public Vector2 PrintedTapeSize => new Vector2(TapeWidth, tapePaper.localScale.z);
    public Vector3 PrintedTapeCenter => tapePaper.localPosition;
    public bool IsFeeding => tapeFeedStart >= 0f;

    /// <summary>Key press depth and tape feed; call every frame while visible (cheap: no allocations).</summary>
    public void Tick()
    {
        float now = Time.unscaledTime;
        if (inputPopAt >= 0f)
        {
            float t = Mathf.Clamp01((now - inputPopAt) / 0.42f);
            display.transform.localScale = inputRestScale * (1f + 0.12f * Mathf.Sin(t * Mathf.PI));
            display.color = Color.Lerp(Level3DeskProps.BrassSoft, Level3DeskProps.DisplayInk, t);
            if (t >= 1f) FinishInputPop();
        }
        foreach (KeyCap cap in keys.Values)
        {
            float depth = now < cap.PressedUntil ? -0.0028f : 0f;
            Vector3 target = cap.Rest + new Vector3(0f, depth, 0f);
            if (cap.Cap.localPosition != target) cap.Cap.localPosition = target;
        }
        if (tapeFeedStart >= 0f && tapeStrip != null)
        {
            float t = Mathf.Clamp01((now - tapeFeedStart) / tapeFeedSeconds);
            tapeStrip.localScale = new Vector3(1f, 1f, Mathf.Lerp(0.08f, 1f, 1f - (1f - t) * (1f - t)));
            if (t >= 1f)
            {
                tapeFeedStart = -1f;
                if (tapePrint != null) tapePrint.enabled = true;
                tapePageLabel.enabled = true;
            }
        }
    }
}
