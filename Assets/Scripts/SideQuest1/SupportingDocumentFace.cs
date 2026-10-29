using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// PURPOSE:
/// The printed faces of supporting records (Documentation/SupportingDocuments.md),
/// built from UI pieces on a world canvas so the same face rests on a sleeve
/// and reads large in a close-up. Fixed layout per type, only the values
/// change: header (icon + title), owner/context, labelled evidence, footer.
/// T24 builds Types 1 and 2 (now Arthur's file); T25 adds Type 3, the
/// compensation certificate (2316) on Marie's case; later tickets add the
/// other types here. Warm ivory paper, charcoal text,
/// colour only marks the type (never whether anything is right).
/// Sizes are canvas units of the face (ID card 800 x 500, sheet 700 x 980).
/// </summary>
public static class SupportingDocumentFace
{
    public static readonly Vector2 CardSize = new Vector2(800f, 500f);
    public static readonly Vector2 SheetSize = new Vector2(700f, 980f);

    public static readonly Color Paper = new Color32(246, 240, 224, 255);
    /// <summary>The ID card's printed face (a cooler card stock than paper).</summary>
    public static readonly Color CardFace = new Color32(244, 240, 231, 255);
    public static readonly Color Edge = new Color32(208, 194, 164, 255);
    public static readonly Color Ink = new Color32(46, 40, 34, 255);
    public static readonly Color LabelInk = new Color32(94, 84, 70, 255);
    public static readonly Color IdentityBlue = new Color32(91, 127, 166, 255);
    public static readonly Color BackgroundOchre = new Color32(196, 146, 62, 255);
    /// <summary>Type 3 (2316): a restrained blue, deeper than the ID card's so the two read apart.</summary>
    public static readonly Color CompensationBlue = new Color32(64, 98, 140, 255);
    private static readonly Color HeaderInk = new Color32(255, 251, 240, 255);
    private static readonly Color Portrait = new Color32(219, 227, 235, 255);
    private static readonly Color Silhouette = new Color32(138, 152, 168, 255);
    private static readonly Color BlockFill = new Color32(251, 247, 237, 255);
    private static readonly Color BlockLabel = new Color32(146, 100, 34, 255);
    private static readonly Color Shadow = new Color(0f, 0f, 0f, 0.26f);

    /// <summary>Type 5's narrow transaction slip (1 : 1.8 portrait).</summary>
    public static readonly Vector2 SlipSize = new Vector2(600f, 1080f);
    /// <summary>Level 2 accents (the standard's restrained sage, ochre and plum; colour marks the type only).</summary>
    public static readonly Color IncomeSage = new Color32(126, 156, 122, 255);
    public static readonly Color PaymentOchre = new Color32(200, 151, 62, 255);
    public static readonly Color AgreementPlum = new Color32(134, 96, 126, 255);
    public static readonly Color PlainGrey = new Color32(110, 112, 106, 255);

    public static Vector2 SizeOf(string type) =>
        type == ClientRecord.Identity ? CardSize : type == ClientRecord.Payment ? SlipSize : SheetSize;

    /// <summary>The type's header colour (also its sleeve tab).</summary>
    public static Color TypeColor(string type) =>
        type == ClientRecord.Identity ? IdentityBlue : type == ClientRecord.Compensation ? CompensationBlue :
        type == ClientRecord.Income ? IncomeSage : type == ClientRecord.Payment ? PaymentOchre :
        type == ClientRecord.Agreement ? AgreementPlum : type == ClientRecord.Plain ? PlainGrey : BackgroundOchre;

    /// <summary>Fills root (sized to SizeOf(record.type)) with the record's face.</summary>
    public static void Build(RectTransform root, ClientRecord record, bool physical = false)
    {
        if (root == null || record == null) return;
        if (record.type == ClientRecord.Identity) BuildIdentity(root, record, physical);
        else if (record.type == ClientRecord.Compensation) BuildCompensation(root, record, physical);
        else if (record.type == ClientRecord.Background) BuildBackground(root, record, physical);
        else BuildTransaction(root, record, physical);
    }

    /// <summary>The printed back: the ID's taxpayer profile, or a paper's continued rows.</summary>
    public static void BuildBack(RectTransform root, ClientRecord record)
    {
        if (root == null || record == null) return;
        if (record.IsCard) BuildIdentityBack(root, record);
        else BuildContinued(root, record);
    }

    // =========================================================
    // TYPE 1: IDENTITY CARD
    // =========================================================

    private static void BuildIdentity(RectTransform root, ClientRecord r, bool physical)
    {
        Vector2 s = CardSize;
        if (!physical) Paperback(root, s, 40f);
        else Backing(root, s, 40f, CardFace);
        // Header band (rounded top, straight bottom edge).
        Box(root, "Header", 0f, 0f, s.x, 104f, IdentityBlue, 40f);
        Box(root, "Header Base", 0f, 64f, s.x, 40f, CardFace);
        Icon(root, ClientRecord.Identity, 30f, 22f, 58f);
        Label(root, "Title", Title(r, "IDENTITY CARD"), 104f, 20f, 660f, 62f, 44f, HeaderInk, TextAlignmentOptions.Left, FontStyles.Bold);

        // Neutral portrait: the same bust for everyone (no faces, no photos).
        RectTransform portrait = Box(root, "Portrait", 34f, 120f, 190f, 250f, Portrait, 16f);
        portrait.gameObject.AddComponent<RectMask2D>();
        RectTransform head = Box(portrait, "Head", 49f, 42f, 92f, 98f, Silhouette);
        head.GetComponent<Image>().sprite = HudTheme.Circle;
        Box(portrait, "Shoulders", 15f, 150f, 160f, 170f, Silhouette, 76f);

        // 2026-10-27: larger print, one line each (long values shrink a little instead of wrapping into the next row).
        Fit(Label(root, "Name", Upper(r.name), 248f, 110f, 524f, 60f, 50f, Ink, TextAlignmentOptions.Left, FontStyles.Bold), 34f);
        Row(root, "Citizenship", r.citizenship, 248f, 176f, 524f);
        Row(root, "Born", r.born, 248f, 252f, 524f);
        Row(root, "Address", r.address, 248f, 328f, 524f);
        Row(root, "ID", r.reference, 248f, 404f, 524f);
    }

    /// <summary>
    /// The back of an ID card (user revision 2026-10-27): the client's taxpayer profile in the ID's
    /// own style (same blue band, ink and margins), one fixed layout for every client, in large print.
    /// Blank card stock when the record has no profile.
    /// </summary>
    public static void BuildIdentityBack(RectTransform root, ClientRecord r)
    {
        if (root == null || r == null) return;
        Vector2 s = CardSize;
        Backing(root, s, 40f, CardFace);
        if (string.IsNullOrWhiteSpace(r.profileText)) return;
        Box(root, "Header", 0f, 0f, s.x, 96f, IdentityBlue, 40f);
        Box(root, "Header Base", 0f, 56f, s.x, 40f, CardFace);
        Label(root, "Title", "TAXPAYER PROFILE", 40f, 18f, 520f, 60f, 40f, HeaderInk, TextAlignmentOptions.Left, FontStyles.Bold)
            .characterSpacing = 3f;
        Label(root, "Number", Value(r.profileNumber), 540f, 22f, 220f, 52f, 34f, HeaderInk, TextAlignmentOptions.Right);
        Fit(Label(root, "Name", Upper(r.name), 40f, 106f, 720f, 58f, 46f, Ink, TextAlignmentOptions.Left, FontStyles.Bold), 32f);
        Box(root, "Rule", 40f, 168f, 720f, 3f, Edge);
        TextMeshProUGUI about = Label(root, "Profile", r.profileText.Trim(), 40f, 184f, 720f, 296f, 46f, Ink, TextAlignmentOptions.TopLeft);
        about.textWrappingMode = TextWrappingModes.Normal;
        about.lineSpacing = 0f;
        about.enableAutoSizing = true;
        about.fontSizeMin = 32f;
        about.fontSizeMax = 46f;
    }

    /// <summary>A small label over a large value, both left-aligned.</summary>
    private static void Row(RectTransform root, string label, string value, float x, float y, float width)
    {
        Label(root, label + " Label", label, x, y, width, 28f, 26f, LabelInk, TextAlignmentOptions.Left);
        TextMeshProUGUI text = Label(root, label + " Value", Value(value), x, y + 28f, width, 48f,
            label == "Citizenship" ? 46f : 42f, Ink, TextAlignmentOptions.Left);
        Fit(text, 28f);
    }

    /// <summary>One line that shrinks (down to min) rather than wrapping into the next row.</summary>
    private static TextMeshProUGUI Fit(TextMeshProUGUI text, float min)
    {
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.enableAutoSizing = true;
        text.fontSizeMax = text.fontSize;
        text.fontSizeMin = min;
        return text;
    }

    // =========================================================
    // TYPE 2: CLIENT BACKGROUND SHEET (office interview record)
    // =========================================================

    private static void BuildBackground(RectTransform root, ClientRecord r, bool physical)
    {
        Vector2 s = SheetSize;
        if (!physical) Paperback(root, s, 18f);
        else Backing(root, s, 18f, Paper);
        Box(root, "Header", 0f, 0f, s.x, 122f, BackgroundOchre, 18f);
        Box(root, "Header Base", 0f, 104f, s.x, 18f, Paper);
        Icon(root, ClientRecord.Background, 32f, 24f, 56f);
        Label(root, "Title", Title(r, "CLIENT BACKGROUND"), 104f, 22f, 570f, 60f, 40f, HeaderInk, TextAlignmentOptions.Left, FontStyles.Bold);

        Label(root, "Name", Value(r.name), 40f, 128f, 620f, 52f, 42f, Ink, TextAlignmentOptions.Left, FontStyles.Bold);
        string context = Value(r.period);
        Label(root, "Context", context, 40f, 184f, 620f, 36f, 28f, LabelInk, TextAlignmentOptions.Left);
        Box(root, "Divider", 40f, 232f, 620f, 3f, Edge);

        Block(root, "HOME", r.living, 252f, 222f);
        Block(root, "WORK", r.work, 492f, 168f);
        Block(root, "INCOME", r.income, 678f, 150f);

        Box(root, "Footer Line", 40f, 856f, 620f, 2f, Edge);
        // Keep the small-print reference/footer in the record data, not on the reading face.
    }

    /// <summary>One clearly separated block: a small heading over the wrapped fact.</summary>
    private static void Block(RectTransform root, string heading, string text, float y, float height)
    {
        RectTransform block = Box(root, heading + " Block", 32f, y, 636f, height, Edge, 14f);
        RectTransform inner = Box(block, "Fill", 2f, 2f, 632f, height - 4f, BlockFill, 12f);
        TextMeshProUGUI label = Label(inner, "Heading", heading, 22f, 14f, 590f, 34f, 30f, BlockLabel, TextAlignmentOptions.Left, FontStyles.Bold);
        label.characterSpacing = 6f;
        TextMeshProUGUI value = Label(inner, "Text", Value(text), 22f, 54f, 590f, height - 62f, 48f, Ink, TextAlignmentOptions.TopLeft);
        value.textWrappingMode = TextWrappingModes.Normal;
    }

    // =========================================================
    // TYPE 3: COMPENSATION CERTIFICATE (2316; T25, Marie's case)
    // =========================================================

    /// <summary>Portrait sheet, blue header, the employee and year, an employer block above two amount rows.</summary>
    private static void BuildCompensation(RectTransform root, ClientRecord r, bool physical)
    {
        Vector2 s = SheetSize;
        if (!physical) Paperback(root, s, 18f);
        else Backing(root, s, 18f, Paper);
        Box(root, "Header", 0f, 0f, s.x, 122f, CompensationBlue, 18f);
        Box(root, "Header Base", 0f, 104f, s.x, 18f, Paper);
        Icon(root, ClientRecord.Compensation, 32f, 24f, 56f);
        Label(root, "Title", Title(r, "COMPENSATION 2316"), 104f, 22f, 570f, 60f, 40f, HeaderInk, TextAlignmentOptions.Left, FontStyles.Bold);

        Label(root, "Name", Value(r.name), 40f, 128f, 620f, 52f, 42f, Ink, TextAlignmentOptions.Left, FontStyles.Bold);
        Label(root, "Context", Value(r.period), 40f, 184f, 620f, 36f, 28f, LabelInk, TextAlignmentOptions.Left);
        Box(root, "Divider", 40f, 232f, 620f, 3f, Edge);

        // Who paid her: the employer and its (masked) TIN.
        RectTransform employer = Box(root, "EMPLOYER Block", 32f, 252f, 636f, 196f, Edge, 14f);
        RectTransform inner = Box(employer, "Fill", 2f, 2f, 632f, 192f, BlockFill, 12f);
        TextMeshProUGUI heading = Label(inner, "Heading", "EMPLOYER", 22f, 14f, 590f, 34f, 30f, CompensationBlue, TextAlignmentOptions.Left, FontStyles.Bold);
        heading.characterSpacing = 6f;
        TextMeshProUGUI name = Label(inner, "Text", Value(r.employer), 22f, 54f, 590f, 84f, 40f, Ink, TextAlignmentOptions.TopLeft);
        name.textWrappingMode = TextWrappingModes.Normal;
        Label(inner, "TIN", "TIN " + Value(r.employerTin), 22f, 142f, 590f, 36f, 28f, LabelInk, TextAlignmentOptions.Left);

        Amount(root, "COMPENSATION PAID", r.compensationAmount, 482f);
        Amount(root, "TAX WITHHELD", r.withheldAmount, 640f);

        Box(root, "Footer Line", 40f, 856f, 620f, 2f, Edge);
        Label(root, "Reference", Value(r.reference), 40f, 872f, 620f, 34f, 26f, LabelInk, TextAlignmentOptions.Left);
    }

    /// <summary>One amount row: a small label over a large right-aligned amount on a light band.</summary>
    private static void Amount(RectTransform root, string label, string amount, float y)
    {
        RectTransform band = Box(root, label + " Row", 32f, y, 636f, 140f, BlockFill, 14f);
        Box(band, "Rule", 0f, 136f, 636f, 4f, Edge);
        TextMeshProUGUI caption = Label(band, "Label", label, 22f, 16f, 590f, 32f, 28f, LabelInk, TextAlignmentOptions.Left, FontStyles.Bold);
        caption.characterSpacing = 4f;
        Label(band, "Amount", Value(amount), 22f, 54f, 592f, 72f, 60f, Ink, TextAlignmentOptions.Right, FontStyles.Bold);
    }

    // =========================================================
    // TYPES 4, 5, 7: TRANSACTION PAPERS (Level 2, Rosa's records)
    // =========================================================

    /// <summary>
    /// Header (icon + title) -> owner/context (issuer, reference, date; Type 7 the second party) -> evidence
    /// rows -> footer (Type 4 total band, Type 5 large amount and status, Type 7 two signature strokes).
    /// Same reading order as Types 1-3; only the values change between clients.
    /// </summary>
    private static void BuildTransaction(RectTransform root, ClientRecord r, bool physical)
    {
        bool slip = r.type == ClientRecord.Payment;
        Vector2 s = SizeOf(r.type);
        Color accent = TypeColor(r.type);
        if (!physical) Paperback(root, s, 18f);
        else Backing(root, s, 18f, Paper);
        float inset = slip ? 34f : 40f;
        float width = s.x - inset * 2f;
        Box(root, "Header", 0f, 0f, s.x, 122f, accent, 18f);
        Box(root, "Header Base", 0f, 104f, s.x, 18f, Paper);
        Icon(root, r.type, 30f, 24f, 56f);
        TextMeshProUGUI title = Label(root, "Title", Title(r, CaseRecordRules.TypeTitle(r.type)), 100f, 22f, s.x - 120f, 60f, slip ? 34f : 38f,
            HeaderInk, TextAlignmentOptions.Left, FontStyles.Bold);
        title.textWrappingMode = TextWrappingModes.NoWrap;
        title.overflowMode = TextOverflowModes.Ellipsis;

        // Owner / context.
        float y = 132f;
        if (r.type == ClientRecord.Agreement)
        {
            Party(root, "PARTY", r.issuer, inset, y, width * 0.5f - 8f, accent);
            Party(root, "WITH", r.party, inset + width * 0.5f + 8f, y, width * 0.5f - 8f, accent);
            y += 124f;
        }
        else
        {
            TextMeshProUGUI issuer = Label(root, "Issuer", Value(r.issuer), inset, y, width, 48f, slip ? 34f : 38f, Ink, TextAlignmentOptions.Left, FontStyles.Bold);
            issuer.textWrappingMode = TextWrappingModes.Normal;
            y += slip ? 52f : 54f;
        }
        Label(root, "Context", Value(r.reference) + "   ·   " + Value(r.date), inset, y, width, 36f, 27f, LabelInk, TextAlignmentOptions.Left);
        y += 46f;
        Box(root, "Divider", inset, y, width, 3f, Edge);
        y += 18f;

        // Evidence rows: a small label over a readable value (long values wrap; nothing shrinks).
        float footerTop = s.y - (slip ? 300f : r.type == ClientRecord.Income ? 214f : r.type == ClientRecord.Agreement ? 196f : 90f);
        int count = r.rows != null ? r.rows.Count : 0;
        float rowHeight = count > 0 ? Mathf.Min(118f, (footerTop - y) / count) : 0f;
        for (int i = 0; i < count; i++) Row(root, r.rows[i], inset, y + i * rowHeight, width, rowHeight, i);
        if (count == 0) Label(root, "Empty", "Not applicable", inset, y, width, 40f, 30f, LabelInk, TextAlignmentOptions.Left);

        if (slip) SlipFooter(root, r, inset, footerTop, width, accent);
        else if (r.type == ClientRecord.Income) TotalBand(root, r, inset, footerTop, width, accent);
        else if (r.type == ClientRecord.Agreement) Signatures(root, inset, footerTop, width);
        if (r.backRows != null && r.backRows.Count > 0)
            Label(root, "Turn Over", "Continued on the back ›", inset, s.y - 60f, width, 34f, 24f, LabelInk, TextAlignmentOptions.Right, FontStyles.Italic);
    }

    /// <summary>The back of a paper whose table did not fit one face: the same header band and the remaining rows.</summary>
    private static void BuildContinued(RectTransform root, ClientRecord r)
    {
        Vector2 s = SizeOf(r.type);
        Color accent = TypeColor(r.type);
        Backing(root, s, 18f, Paper);
        Box(root, "Header", 0f, 0f, s.x, 96f, accent, 18f);
        Box(root, "Header Base", 0f, 80f, s.x, 16f, Paper);
        Label(root, "Title", Title(r, CaseRecordRules.TypeTitle(r.type)) + " (continued)", 34f, 18f, s.x - 68f, 56f, 32f, HeaderInk,
            TextAlignmentOptions.Left, FontStyles.Bold);
        float inset = r.type == ClientRecord.Payment ? 34f : 40f;
        float y = 124f;
        int count = r.backRows != null ? r.backRows.Count : 0;
        float rowHeight = count > 0 ? Mathf.Min(118f, (s.y - 80f - y) / count) : 0f;
        for (int i = 0; i < count; i++) Row(root, r.backRows[i], inset, y + i * rowHeight, s.x - inset * 2f, rowHeight, i);
    }

    private static void Row(RectTransform root, ClientRecordRow row, float x, float y, float w, float h, int index)
    {
        if (row == null) return;
        if (index % 2 == 1) Box(root, "Row Band " + index, x - 10f, y, w + 20f, h - 6f, BlockFill, 10f);
        TextMeshProUGUI label = Label(root, "Row Label " + index, string.IsNullOrWhiteSpace(row.label) ? "—" : row.label.Trim().ToUpperInvariant(),
            x, y + 8f, w, 30f, 22f, LabelInk, TextAlignmentOptions.Left, FontStyles.Bold);
        label.characterSpacing = 3f;
        TextMeshProUGUI value = Label(root, "Row Value " + index, Value(row.value), x, y + 40f, w, Mathf.Max(40f, h - 48f), 34f, Ink, TextAlignmentOptions.TopLeft);
        value.textWrappingMode = TextWrappingModes.Normal;
    }

    /// <summary>Type 4: the stated total on a band (the client's claim, never a confirmation).</summary>
    private static void TotalBand(RectTransform root, ClientRecord r, float x, float y, float w, Color accent)
    {
        RectTransform band = Box(root, "Total Band", x - 8f, y, w + 16f, 150f, BlockFill, 14f);
        Box(band, "Rule", 0f, 0f, w + 16f, 5f, accent);
        TextMeshProUGUI caption = Label(band, "Label", string.IsNullOrWhiteSpace(r.totalLabel) ? "RECORDED TOTAL" : r.totalLabel.Trim().ToUpperInvariant(),
            22f, 18f, w - 28f, 32f, 24f, LabelInk, TextAlignmentOptions.Left, FontStyles.Bold);
        caption.characterSpacing = 4f;
        Label(band, "Amount", Value(r.totalValue), 22f, 60f, w - 28f, 72f, 58f, Ink, TextAlignmentOptions.Right, FontStyles.Bold);
    }

    /// <summary>Type 5: the large amount and, when the record says so, a clear status label.</summary>
    private static void SlipFooter(RectTransform root, ClientRecord r, float x, float y, float w, Color accent)
    {
        RectTransform band = Box(root, "Amount Band", x - 8f, y, w + 16f, 170f, BlockFill, 14f);
        TextMeshProUGUI caption = Label(band, "Label", string.IsNullOrWhiteSpace(r.totalLabel) ? "AMOUNT" : r.totalLabel.Trim().ToUpperInvariant(),
            20f, 16f, w - 24f, 32f, 23f, LabelInk, TextAlignmentOptions.Left, FontStyles.Bold);
        caption.characterSpacing = 3f;
        Label(band, "Amount", Value(r.totalValue), 20f, 60f, w - 24f, 84f, 66f, Ink, TextAlignmentOptions.Right, FontStyles.Bold);
        if (string.IsNullOrWhiteSpace(r.status)) return;
        RectTransform stamp = Box(root, "Status", x + w - 220f, y + 190f, 220f, 72f, accent, 12f);
        RectTransform fill = Box(stamp, "Fill", 4f, 4f, 212f, 64f, Paper, 9f);
        TextMeshProUGUI status = Label(fill, "Text", r.status, 0f, 6f, 212f, 52f, 38f, accent, TextAlignmentOptions.Center, FontStyles.Bold);
        status.characterSpacing = 8f;
        stamp.localRotation = Quaternion.Euler(0f, 0f, 4f);
    }

    /// <summary>Type 7: the two parties' signature lines with simple strokes.</summary>
    private static void Signatures(RectTransform root, float x, float y, float w)
    {
        float half = w * 0.5f - 12f;
        for (int i = 0; i < 2; i++)
        {
            float left = x + i * (half + 24f);
            RectTransform stroke = Box(root, "Signature " + i, left + 18f, y + 52f, half - 60f, 7f, Ink, 3.5f);
            stroke.localRotation = Quaternion.Euler(0f, 0f, i == 0 ? 6f : -5f);
            Box(root, "Signature Line " + i, left, y + 92f, half, 3f, Edge);
            Label(root, "Signature Caption " + i, i == 0 ? "PARTY" : "WITH", left, y + 104f, half, 30f, 21f, LabelInk, TextAlignmentOptions.Left, FontStyles.Bold);
        }
    }

    private static void Party(RectTransform root, string heading, string name, float x, float y, float w, Color accent)
    {
        RectTransform block = Box(root, heading + " Block", x, y, w, 112f, Edge, 12f);
        RectTransform inner = Box(block, "Fill", 2f, 2f, w - 4f, 108f, BlockFill, 10f);
        TextMeshProUGUI label = Label(inner, "Heading", heading, 16f, 10f, w - 36f, 30f, 22f, accent, TextAlignmentOptions.Left, FontStyles.Bold);
        label.characterSpacing = 5f;
        TextMeshProUGUI value = Label(inner, "Name", Value(name), 16f, 44f, w - 36f, 60f, 30f, Ink, TextAlignmentOptions.TopLeft, FontStyles.Bold);
        value.textWrappingMode = TextWrappingModes.Normal;
    }

    // =========================================================
    // SHARED PIECES
    // =========================================================

    /// <summary>
    /// The printed face's own paper (physical records, 2026-10-27): an unlit sheet of the paper colour
    /// under the print, inset inside the 3D body so its lit bevel and edge show around it. The print
    /// never turns grey in shade (corkboard, studio renders); depth comes from the 3D body and shadow.
    /// </summary>
    private static void Backing(RectTransform root, Vector2 size, float radius, Color color) =>
        Box(root, "Print Paper", 0f, 0f, size.x, size.y, color, radius);

    /// <summary>Soft drop shadow, edge and paper, the size of the face.</summary>
    private static void Paperback(RectTransform root, Vector2 size, float radius)
    {
        Box(root, "Shadow", 10f, 14f, size.x, size.y, Shadow, radius);
        Box(root, "Edge", 0f, 0f, size.x, size.y, Edge, radius);
        Box(root, "Paper", 3f, 3f, size.x - 6f, size.y - 6f, Paper, radius - 3f);
    }

    /// <summary>The type's header icon in light ink: a small card with a portrait dot (Identity) or a page of lines (Background).</summary>
    public static void Icon(RectTransform parent, string type, float x, float y, float size)
    {
        RectTransform icon = HudTheme.PlaceTopLeft(HudTheme.CreateRect("Icon", parent), x, y, size, size);
        if (type == ClientRecord.Identity)
        {
            Box(icon, "Card", 0f, size * 0.16f, size, size * 0.68f, HeaderInk, size * 0.12f);
            Box(icon, "Card Fill", size * 0.06f, size * 0.22f, size * 0.88f, size * 0.56f, IdentityBlue, size * 0.08f);
            RectTransform dot = Box(icon, "Dot", size * 0.14f, size * 0.32f, size * 0.26f, size * 0.26f, HeaderInk);
            dot.GetComponent<Image>().sprite = HudTheme.Circle;
            for (int i = 0; i < 2; i++) Box(icon, "Line " + i, size * 0.48f, size * (0.36f + i * 0.14f), size * 0.36f, size * 0.07f, HeaderInk);
        }
        else if (type == ClientRecord.Compensation)
        {
            // A page with two amount lines and a coin: payments recorded.
            Box(icon, "Page", size * 0.14f, 0f, size * 0.72f, size, HeaderInk, size * 0.1f);
            for (int i = 0; i < 2; i++) Box(icon, "Line " + i, size * 0.26f, size * (0.2f + i * 0.17f), size * 0.48f, size * 0.07f, CompensationBlue);
            RectTransform coin = Box(icon, "Coin", size * 0.4f, size * 0.58f, size * 0.3f, size * 0.3f, CompensationBlue);
            coin.GetComponent<Image>().sprite = HudTheme.Circle;
        }
        else if (type == ClientRecord.Payment)
        {
            // A narrow slip with a coin: one payment.
            Box(icon, "Slip", size * 0.24f, 0f, size * 0.52f, size, HeaderInk, size * 0.08f);
            Box(icon, "Line", size * 0.33f, size * 0.18f, size * 0.34f, size * 0.07f, PaymentOchre);
            RectTransform coin = Box(icon, "Coin", size * 0.35f, size * 0.52f, size * 0.3f, size * 0.3f, PaymentOchre);
            coin.GetComponent<Image>().sprite = HudTheme.Circle;
        }
        else if (type == ClientRecord.Agreement)
        {
            // A page with two lines and a signature stroke.
            Box(icon, "Page", size * 0.14f, 0f, size * 0.72f, size, HeaderInk, size * 0.1f);
            for (int i = 0; i < 2; i++) Box(icon, "Line " + i, size * 0.26f, size * (0.2f + i * 0.17f), size * 0.48f, size * 0.07f, AgreementPlum);
            RectTransform stroke = Box(icon, "Signature", size * 0.26f, size * 0.7f, size * 0.42f, size * 0.08f, AgreementPlum);
            stroke.localRotation = Quaternion.Euler(0f, 0f, 12f);
        }
        else
        {
            // A page of lines; an income record ends in a total bar.
            Color ink = TypeColor(type);
            Box(icon, "Page", size * 0.14f, 0f, size * 0.72f, size, HeaderInk, size * 0.1f);
            for (int i = 0; i < 4; i++) Box(icon, "Line " + i, size * 0.26f, size * (0.2f + i * 0.17f), size * (i == 3 ? 0.3f : 0.48f), size * 0.07f, ink);
            if (type == ClientRecord.Income) Box(icon, "Total", size * 0.26f, size * 0.84f, size * 0.48f, size * 0.09f, ink);
        }
    }

    private static RectTransform Box(RectTransform parent, string name, float x, float y, float w, float h, Color color, float radius = 0f)
    {
        RectTransform rect = HudTheme.PlaceTopLeft(HudTheme.CreateRect(name, parent), x, y, w, h);
        HudTheme.AddImage(rect, radius > 0f ? HudTheme.Rounded : null, color, radius);
        return rect;
    }

    private static TextMeshProUGUI Label(RectTransform parent, string name, string text, float x, float y, float w, float h, float size, Color color,
        TextAlignmentOptions align, FontStyles style = FontStyles.Normal)
    {
        TextMeshProUGUI label = HudTheme.CreateLabel(name, parent, text, size, color, align, style);
        label.enableAutoSizing = false;
        label.fontSharedMaterial = HudTheme.Font.material;
        // Truncate can discard an entire line when font ascenders exceed a
        // compact row's height. Keep the authored, bounded text visible.
        label.overflowMode = TextOverflowModes.Overflow;
        label.extraPadding = true;
        HudTheme.PlaceTopLeft(label.rectTransform, x, y, w, h);
        return label;
    }

    private static string Title(ClientRecord r, string fallback) => string.IsNullOrWhiteSpace(r.title) ? fallback : r.title.Trim();
    private static string Value(string value) => string.IsNullOrWhiteSpace(value) ? "—" : value.Trim();
    private static string Upper(string value) => Value(value).ToUpperInvariant();
}

/// <summary>
/// The paper bodies of supporting records: one rounded, shallow slab mesh per
/// size (an ID card with soft corners, a full sheet with barely rounded ones),
/// lying flat with its top face at +Y. Shared and cached; no texture (the face
/// canvas carries the print).
/// </summary>
public static class SupportingDocumentMesh
{
    private static readonly Dictionary<string, Mesh> meshes = new Dictionary<string, Mesh>();
    private static readonly Dictionary<Color, Material> materials = new Dictionary<Color, Material>();

    /// <summary>A rounded slab width x depth (X x Z), thickness tall, bottom at y = 0.</summary>
    public static Mesh RoundedSlab(float width, float depth, float thickness, float radius, int cornerSegments = 6)
    {
        string key = $"{width:F4}x{depth:F4}x{thickness:F4}r{radius:F4}s{cornerSegments}";
        if (meshes.TryGetValue(key, out Mesh cached) && cached != null) return cached;

        radius = Mathf.Clamp(radius, 0f, Mathf.Min(width, depth) * 0.5f);
        cornerSegments = Mathf.Max(1, cornerSegments);
        // Outline in increasing angle (counter-clockwise seen from above, X right / Z up).
        var outline = new List<Vector2>();
        float hx = width * 0.5f - radius, hz = depth * 0.5f - radius;
        Vector2[] centres = { new Vector2(hx, hz), new Vector2(-hx, hz), new Vector2(-hx, -hz), new Vector2(hx, -hz) };
        for (int c = 0; c < 4; c++)
            for (int i = 0; i <= cornerSegments; i++)
            {
                float a = (c * 90f + 90f * i / cornerSegments) * Mathf.Deg2Rad;
                outline.Add(centres[c] + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * radius);
            }

        var vertices = new List<Vector3>();
        var normals = new List<Vector3>();
        var triangles = new List<int>();
        int n = outline.Count;
        // Top (+Y) and bottom (-Y) as fans from the centre.
        int top = vertices.Count;
        vertices.Add(new Vector3(0f, thickness, 0f)); normals.Add(Vector3.up);
        foreach (Vector2 p in outline) { vertices.Add(new Vector3(p.x, thickness, p.y)); normals.Add(Vector3.up); }
        for (int i = 0; i < n; i++) { triangles.Add(top); triangles.Add(top + 1 + (i + 1) % n); triangles.Add(top + 1 + i); }
        int bottom = vertices.Count;
        vertices.Add(Vector3.zero); normals.Add(Vector3.down);
        foreach (Vector2 p in outline) { vertices.Add(new Vector3(p.x, 0f, p.y)); normals.Add(Vector3.down); }
        for (int i = 0; i < n; i++) { triangles.Add(bottom); triangles.Add(bottom + 1 + i); triangles.Add(bottom + 1 + (i + 1) % n); }
        // Sides: one quad per outline edge, facing outward.
        for (int i = 0; i < n; i++)
        {
            Vector2 a = outline[i], b = outline[(i + 1) % n];
            Vector2 edge = b - a;
            var normal = new Vector3(edge.y, 0f, -edge.x).normalized;
            int s = vertices.Count;
            vertices.Add(new Vector3(a.x, thickness, a.y)); vertices.Add(new Vector3(b.x, thickness, b.y));
            vertices.Add(new Vector3(b.x, 0f, b.y)); vertices.Add(new Vector3(a.x, 0f, a.y));
            for (int k = 0; k < 4; k++) normals.Add(normal);
            triangles.Add(s); triangles.Add(s + 1); triangles.Add(s + 2);
            triangles.Add(s); triangles.Add(s + 2); triangles.Add(s + 3);
        }

        var mesh = new Mesh { name = "Record Slab " + key };
        mesh.SetVertices(vertices);
        mesh.SetNormals(normals);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateBounds();
        meshes[key] = mesh;
        return mesh;
    }

    /// <summary>
    /// T31 physical card: a rounded slab whose top edge is softly bevelled (a chamfer ring of width
    /// bevel between the top face and the sides), bottom at y = 0, top face at y = thickness.
    /// </summary>
    public static Mesh BeveledSlab(float width, float depth, float thickness, float radius, float bevel, int cornerSegments = 6)
    {
        string key = $"bevel {width:F4}x{depth:F4}x{thickness:F4}r{radius:F4}b{bevel:F4}s{cornerSegments}";
        if (meshes.TryGetValue(key, out Mesh cached) && cached != null) return cached;

        bevel = Mathf.Clamp(bevel, 0f, Mathf.Min(thickness * 0.8f, Mathf.Min(width, depth) * 0.2f));
        List<Vector2> outer = Outline(width, depth, radius, cornerSegments);
        List<Vector2> inner = Outline(width - 2f * bevel, depth - 2f * bevel, Mathf.Max(0.0001f, radius - bevel), cornerSegments);
        int n = outer.Count;
        float shoulder = thickness - bevel;
        var vertices = new List<Vector3>();
        var normals = new List<Vector3>();
        var triangles = new List<int>();

        int top = vertices.Count;
        vertices.Add(new Vector3(0f, thickness, 0f)); normals.Add(Vector3.up);
        foreach (Vector2 p in inner) { vertices.Add(new Vector3(p.x, thickness, p.y)); normals.Add(Vector3.up); }
        for (int i = 0; i < n; i++) { triangles.Add(top); triangles.Add(top + 1 + (i + 1) % n); triangles.Add(top + 1 + i); }
        int bottom = vertices.Count;
        vertices.Add(Vector3.zero); normals.Add(Vector3.down);
        foreach (Vector2 p in outer) { vertices.Add(new Vector3(p.x, 0f, p.y)); normals.Add(Vector3.down); }
        for (int i = 0; i < n; i++) { triangles.Add(bottom); triangles.Add(bottom + 1 + i); triangles.Add(bottom + 1 + (i + 1) % n); }
        for (int i = 0; i < n; i++)
        {
            int j = (i + 1) % n;
            Vector2 edge = outer[j] - outer[i];
            var outward = new Vector3(edge.y, 0f, -edge.x).normalized;
            // Chamfer: inner top edge down to the outer shoulder, facing up and out.
            Vector3 slope = (outward + Vector3.up).normalized;
            int s = vertices.Count;
            vertices.Add(new Vector3(inner[i].x, thickness, inner[i].y)); vertices.Add(new Vector3(inner[j].x, thickness, inner[j].y));
            vertices.Add(new Vector3(outer[j].x, shoulder, outer[j].y)); vertices.Add(new Vector3(outer[i].x, shoulder, outer[i].y));
            for (int k = 0; k < 4; k++) normals.Add(slope);
            triangles.Add(s); triangles.Add(s + 1); triangles.Add(s + 2);
            triangles.Add(s); triangles.Add(s + 2); triangles.Add(s + 3);
            // Side wall below the shoulder.
            s = vertices.Count;
            vertices.Add(new Vector3(outer[i].x, shoulder, outer[i].y)); vertices.Add(new Vector3(outer[j].x, shoulder, outer[j].y));
            vertices.Add(new Vector3(outer[j].x, 0f, outer[j].y)); vertices.Add(new Vector3(outer[i].x, 0f, outer[i].y));
            for (int k = 0; k < 4; k++) normals.Add(outward);
            triangles.Add(s); triangles.Add(s + 1); triangles.Add(s + 2);
            triangles.Add(s); triangles.Add(s + 2); triangles.Add(s + 3);
        }
        return Store(key, vertices, normals, triangles);
    }

    /// <summary>
    /// T31 physical sheet: a thin paper sheet (X x Z, top edge toward +Z) whose two free bottom
    /// corners lift within the print-free outer margins, with a visible thin edge all round.
    /// </summary>
    public static Mesh LiftedSheet(float width, float depth, float thickness, float cornerLift, int segX = 40, int segZ = 40)
    {
        string key = $"sheet {width:F4}x{depth:F4}x{thickness:F4}l{cornerLift:F4}s{segX}x{segZ}";
        if (meshes.TryGetValue(key, out Mesh cached) && cached != null) return cached;
        float hx = width * 0.5f, hz = depth * 0.5f;
        float Lift(float x, float z)
        {
            float u = Mathf.Abs(x) / hx;              // 0 centre .. 1 side
            float v = (-z / hz + 1f) * 0.5f;          // 0 top (clipped) .. 1 bottom (free)
            float su = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.945f, 1f, u));
            float sv = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.975f, 1f, v));
            return cornerLift * su * sv;
        }
        var vertices = new List<Vector3>();
        var triangles = new List<int>();
        int cols = segX + 1;
        // Top and bottom surfaces.
        for (int face = 0; face < 2; face++)
        {
            int start = vertices.Count;
            for (int iz = 0; iz <= segZ; iz++)
                for (int ix = 0; ix <= segX; ix++)
                {
                    float x = -hx + width * ix / segX, z = -hz + depth * iz / segZ;
                    vertices.Add(new Vector3(x, Lift(x, z) + (face == 0 ? thickness : 0f), z));
                }
            for (int iz = 0; iz < segZ; iz++)
                for (int ix = 0; ix < segX; ix++)
                {
                    int a = start + iz * cols + ix, b = a + 1, c = a + cols, d = c + 1;
                    if (face == 0) { triangles.Add(a); triangles.Add(c); triangles.Add(b); triangles.Add(b); triangles.Add(c); triangles.Add(d); }
                    else { triangles.Add(a); triangles.Add(b); triangles.Add(c); triangles.Add(b); triangles.Add(d); triangles.Add(c); }
                }
        }
        // The thin edge: a strip along each of the four sides.
        void Edge(Vector2 from, Vector2 to, int steps)
        {
            int start = vertices.Count;
            for (int i = 0; i <= steps; i++)
            {
                Vector2 p = Vector2.Lerp(from, to, (float)i / steps);
                float y = Lift(p.x, p.y);
                vertices.Add(new Vector3(p.x, y + thickness, p.y));
                vertices.Add(new Vector3(p.x, y, p.y));
            }
            for (int i = 0; i < steps; i++)
            {
                int a = start + i * 2;
                triangles.Add(a); triangles.Add(a + 2); triangles.Add(a + 1);
                triangles.Add(a + 1); triangles.Add(a + 2); triangles.Add(a + 3);
            }
        }
        Edge(new Vector2(hx, -hz), new Vector2(hx, hz), segZ);
        Edge(new Vector2(hx, hz), new Vector2(-hx, hz), segX);
        Edge(new Vector2(-hx, hz), new Vector2(-hx, -hz), segZ);
        Edge(new Vector2(-hx, -hz), new Vector2(hx, -hz), segX);
        var mesh = new Mesh { name = "Record Sheet " + key };
        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        meshes[key] = mesh;
        return mesh;
    }

    /// <summary>A photo-corner holder: a right-triangle prism with legs along +X and +Z from the origin.</summary>
    public static Mesh CornerWedge(float leg, float thickness)
    {
        string key = $"wedge {leg:F4}x{thickness:F4}";
        if (meshes.TryGetValue(key, out Mesh cached) && cached != null) return cached;
        var p = new[] { Vector2.zero, new Vector2(leg, 0f), new Vector2(0f, leg) };
        var vertices = new List<Vector3>();
        var normals = new List<Vector3>();
        var triangles = new List<int>();
        int s = vertices.Count;
        foreach (Vector2 q in p) { vertices.Add(new Vector3(q.x, thickness, q.y)); normals.Add(Vector3.up); }
        triangles.Add(s); triangles.Add(s + 2); triangles.Add(s + 1);
        for (int i = 0; i < 3; i++)
        {
            Vector2 a = p[i], b = p[(i + 1) % 3];
            Vector2 edge = b - a;
            var outward = new Vector3(edge.y, 0f, -edge.x).normalized;
            int k = vertices.Count;
            vertices.Add(new Vector3(a.x, thickness, a.y)); vertices.Add(new Vector3(b.x, thickness, b.y));
            vertices.Add(new Vector3(b.x, 0f, b.y)); vertices.Add(new Vector3(a.x, 0f, a.y));
            for (int j = 0; j < 4; j++) normals.Add(outward);
            triangles.Add(k); triangles.Add(k + 1); triangles.Add(k + 2);
            triangles.Add(k); triangles.Add(k + 2); triangles.Add(k + 3);
        }
        return Store(key, vertices, normals, triangles);
    }

    private static List<Vector2> Outline(float width, float depth, float radius, int cornerSegments)
    {
        radius = Mathf.Clamp(radius, 0f, Mathf.Min(width, depth) * 0.5f);
        cornerSegments = Mathf.Max(1, cornerSegments);
        var outline = new List<Vector2>();
        float hx = width * 0.5f - radius, hz = depth * 0.5f - radius;
        Vector2[] centres = { new Vector2(hx, hz), new Vector2(-hx, hz), new Vector2(-hx, -hz), new Vector2(hx, -hz) };
        for (int c = 0; c < 4; c++)
            for (int i = 0; i <= cornerSegments; i++)
            {
                float a = (c * 90f + 90f * i / cornerSegments) * Mathf.Deg2Rad;
                outline.Add(centres[c] + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * radius);
            }
        return outline;
    }

    private static Mesh Store(string key, List<Vector3> vertices, List<Vector3> normals, List<int> triangles)
    {
        var mesh = new Mesh { name = "Record " + key };
        mesh.SetVertices(vertices);
        mesh.SetNormals(normals);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateBounds();
        meshes[key] = mesh;
        return mesh;
    }

    /// <summary>A matte lit material of this colour that renders in this pipeline (shared per colour).</summary>
    public static Material Matte(Color color)
    {
        if (materials.TryGetValue(color, out Material cached) && cached != null) return cached;
        Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        var material = new Material(shader) { name = "Record Paper", color = color };
        if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", 0.1f);
        if (material.HasProperty("_Glossiness")) material.SetFloat("_Glossiness", 0.1f);
        materials[color] = material;
        return material;
    }
}
