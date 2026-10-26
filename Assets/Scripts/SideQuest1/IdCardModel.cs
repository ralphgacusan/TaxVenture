using System;
using UnityEngine;

/// <summary>
/// PURPOSE:
/// The one physical ID card (user revision 2026-10-27; Type 1 of
/// Documentation/SupportingDocuments.md) used everywhere a client's ID
/// appears: on Arthur's and Marie's supporting pages, as the copy each plaza
/// client hands over (world drop, binder, corkboard tray and pins). A
/// rounded, bevelled matte card with a darker laminated core, bevelled on
/// both sides; the printed front (SupportingDocumentFace identity layout) on
/// top and, when the record has one, the taxpayer profile printed on its back.
///
/// Built lying flat: X right, +Y out of the front, +Z the card's top edge.
/// Everything that turns over lives under one flip pivot at the card's
/// middle, so a flip never moves the card's place, holder or touch area.
/// </summary>
public static class IdCardModel
{
    public static readonly Color CardStock = new Color32(240, 236, 226, 255);
    public static readonly Color CoreColor = new Color32(171, 181, 183, 255);
    /// <summary>A sheet's stock and its darker core band (2026-10-27: sheets get the ID's depth too).</summary>
    public static readonly Color SheetStock = new Color32(246, 240, 224, 255);
    public static readonly Color SheetCore = new Color32(196, 184, 158, 255);

    /// <summary>Card thickness for a card this wide (visible when tilted or flipping).</summary>
    public static float ThicknessFor(Vector2 size) => Mathf.Max(0.002f, size.x * 0.03f);

    /// <summary>A record's thickness: the ID's, or a sheet's (a little thinner for its width, still clearly visible).</summary>
    public static float ThicknessFor(ClientRecord record, Vector2 size) =>
        record == null || record.IsCard ? ThicknessFor(size) : Mathf.Max(0.0016f, size.x * 0.02f);

    /// <summary>How far a printed face sits above the body: enough that it never flickers with the surface at a distance.</summary>
    public static float FaceLift(Vector2 size) => Mathf.Max(0.00025f, size.x * 0.003f);

    /// <summary>
    /// Builds the card under parent (its bottom at y = 0, its front face at y = thickness) and
    /// returns the flip that turns it over (also when the record has no back: then the back is blank stock).
    /// </summary>
    public static CardFlip Build(Transform parent, ClientRecord record, Vector2 size, int layer, out Canvas frontCanvas, out Canvas backCanvas)
    {
        bool card = record == null || record.IsCard;
        float thickness = ThicknessFor(record, size);
        float corner = size.x * (card ? 0.06f : 0.015f);
        var pivot = new GameObject("Card Flip");
        pivot.layer = layer;
        pivot.transform.SetParent(parent, false);
        pivot.transform.localPosition = new Vector3(0f, thickness * 0.5f, 0f);

        // Two mirrored bevelled halves (a front and a back bevel) around a darker core.
        Mesh half = SupportingDocumentMesh.BeveledSlab(size.x, size.y, thickness * 0.5f, corner, thickness * 0.22f);
        Material stock = SupportingDocumentMesh.Matte(card ? CardStock : SheetStock);
        Transform front = Part("Body Front", pivot.transform, layer, half, stock);
        front.localPosition = Vector3.zero;
        Transform back = Part("Body Back", pivot.transform, layer, half, stock);
        back.localPosition = Vector3.zero;
        back.localScale = new Vector3(1f, -1f, 1f);
        Transform core = Part("Laminate Core", pivot.transform, layer,
            SupportingDocumentMesh.RoundedSlab(size.x * 0.996f, size.y * 0.996f, thickness * 0.34f, corner * 0.96f),
            SupportingDocumentMesh.Matte(card ? CoreColor : SheetCore));
        core.localPosition = new Vector3(0f, -thickness * 0.17f, 0f);

        frontCanvas = Face(pivot.transform, "Face", record, size, layer, thickness * 0.5f + FaceLift(size), false);
        backCanvas = card ? Face(pivot.transform, "Back Face", record, size, layer, -(thickness * 0.5f + FaceLift(size)), true) : null;

        CardFlip flip = pivot.AddComponent<CardFlip>();
        flip.Configure(size, record != null && record.HasBack);
        return flip;
    }

    /// <summary>
    /// A soft drop shadow under the paper (2026-10-27: depth for a record out of its folder, a dropped
    /// ID, a corkboard card, the binder's cards): a blurred-looking dark rounded sheet just behind the
    /// body, offset down and to the right. Transparent UI, so it reads as shade, never as a border.
    /// </summary>
    public static GameObject AddDropShadow(Transform parent, ClientRecord record, Vector2 size, int layer)
    {
        bool card = record == null || record.IsCard;
        var host = new GameObject("Drop Shadow", typeof(RectTransform));
        host.layer = layer;
        var rect = (RectTransform)host.transform;
        rect.SetParent(parent, false);
        const float units = 1000f;
        rect.sizeDelta = new Vector2(units, units * size.y / size.x);
        rect.localScale = Vector3.one * (size.x / units);
        rect.localRotation = Quaternion.Euler(90f, 0f, 0f);
        rect.localPosition = new Vector3(size.x * 0.025f, -FaceLift(size) * 2f, -size.y * 0.04f);
        var canvas = host.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        float radius = units * (card ? 0.07f : 0.025f);
        for (int i = 0; i < 3; i++)
        {
            // Three soft layers, widest and faintest first: a gentle falloff instead of a hard edge.
            RectTransform layerRect = HudTheme.CreateRect("Shade " + i, rect);
            layerRect.gameObject.layer = layer;
            HudTheme.Stretch(layerRect, -units * 0.012f * (2 - i));
            UnityEngine.UI.Image shade = HudTheme.AddImage(layerRect, HudTheme.Rounded, new Color(0f, 0f, 0f, 0.1f), radius + units * 0.012f * (2 - i));
            shade.raycastTarget = false;
        }
        return host;
    }

    /// <summary>A printed face just outside the body; the back one reads upright once the card is turned over.</summary>
    private static Canvas Face(Transform pivot, string name, ClientRecord record, Vector2 size, int layer, float height, bool back)
    {
        Vector2 units = SupportingDocumentFace.SizeOf(record != null ? record.type : ClientRecord.Identity);
        var host = new GameObject(name, typeof(RectTransform));
        host.layer = layer;
        var face = (RectTransform)host.transform;
        face.SetParent(pivot, false);
        face.localPosition = new Vector3(0f, height, 0f);
        Quaternion up = Quaternion.Euler(90f, 0f, 0f);
        face.localRotation = back ? CardFlip.TurnOver * up : up;
        face.sizeDelta = units;
        face.localScale = Vector3.one * (size.x * (record == null || record.IsCard ? 0.94f : 0.96f) / units.x);
        var canvas = host.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.additionalShaderChannels |= AdditionalCanvasShaderChannels.TexCoord1;
        if (back) SupportingDocumentFace.BuildIdentityBack(face, record);
        else SupportingDocumentFace.Build(face, record, physical: true);
        foreach (UnityEngine.UI.Graphic graphic in face.GetComponentsInChildren<UnityEngine.UI.Graphic>(true)) graphic.raycastTarget = false;
        return canvas;
    }

    private static Transform Part(string name, Transform parent, int layer, Mesh mesh, Material material)
    {
        var part = new GameObject(name);
        part.layer = layer;
        part.transform.SetParent(parent, false);
        part.AddComponent<MeshFilter>().sharedMesh = mesh;
        var renderer = part.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        return part.transform;
    }
}

/// <summary>
/// Turns an ID card over (front: the ID; back: the taxpayer profile). A short,
/// physical flip: the card lifts a little toward the reader, turns 180 degrees
/// about its top edge's axis with a gentle ease, and settles flat again.
/// Purely visual: it changes no record, answer, save or reward. Changed fires
/// every frame while it turns (a cached document capture redraws).
/// </summary>
public sealed class CardFlip : MonoBehaviour
{
    /// <summary>Half a turn about the card's top axis (+Z): the back faces the reader.</summary>
    public static readonly Quaternion TurnOver = Quaternion.AngleAxis(180f, Vector3.forward);

    public const float FlipSeconds = 0.5f;
    private const float LiftShare = 0.22f;   // of the card's height, at mid-turn
    private const float Pop = 0.05f;

    private Vector3 rest;
    private float liftHeight;
    private bool hasBack;
    private float from, to, elapsed = -1f;

    /// <summary>Showing the back now (or turning toward it).</summary>
    public bool ShowingBack => to > 0.5f;
    public bool IsTurning => elapsed >= 0f;
    public bool HasBack => hasBack;

    /// <summary>Every frame while turning, and once at the end.</summary>
    public event Action Changed;
    /// <summary>A flip finished (true = the back is up).</summary>
    public event Action<bool> Flipped;

    public void Configure(Vector2 size, bool back)
    {
        rest = transform.localPosition;
        liftHeight = size.y * LiftShare;
        hasBack = back;
    }

    /// <summary>Turns the card over (no-op for a card with nothing on its back). True when it started.</summary>
    public bool Toggle()
    {
        if (!hasBack) return false;
        from = Current;
        to = ShowingBack ? 0f : 1f;
        elapsed = 0f;
        if (AudioManager.Instance != null) AudioManager.Instance.PlayEffect("sfx_card_pickup_01", 0.45f);
        return true;
    }

    /// <summary>Front up again at once (closing, gathering, resetting a view).</summary>
    public void ShowFront()
    {
        if (to == 0f && elapsed < 0f) return;
        from = to = 0f;
        elapsed = -1f;
        Apply(0f, 0f);
        Changed?.Invoke();
    }

    private float Current => elapsed < 0f ? to : Mathf.Lerp(from, to, Ease(Mathf.Clamp01(elapsed / FlipSeconds)));

    private static float Ease(float t) => t * t * t * (t * (t * 6f - 15f) + 10f);

    private void Update()
    {
        if (elapsed < 0f) return;
        elapsed += Time.unscaledDeltaTime;
        float t = Mathf.Clamp01(elapsed / FlipSeconds);
        float turn = Mathf.Lerp(from, to, Ease(t));
        // Lifted and slightly larger at mid-turn, flat and still at both ends.
        float arc = Mathf.Sin(Mathf.PI * t) * Mathf.Abs(to - from);
        Apply(turn, arc);
        Changed?.Invoke();
        if (t < 1f) return;
        elapsed = -1f;
        Apply(to, 0f);
        Changed?.Invoke();
        Flipped?.Invoke(ShowingBack);
        if (AudioManager.Instance != null) AudioManager.Instance.PlayEffect("sfx_card_place_01", 0.4f);
    }

    private void Apply(float turn, float arc)
    {
        transform.localRotation = Quaternion.AngleAxis(180f * turn, Vector3.forward);
        transform.localPosition = rest + Vector3.up * (liftHeight * arc);
        transform.localScale = Vector3.one * (1f + Pop * arc);
    }
}
