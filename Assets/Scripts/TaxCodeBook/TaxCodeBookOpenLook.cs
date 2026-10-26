using UnityEngine;

/// <summary>
/// PURPOSE:
/// The open Tax Code Book's look, built at runtime around its printed page
/// so it matches the Case Folder: a slim, stylized hardcover instead of the
/// thick imported model. The imported model's renderers are switched off
/// (kept in the scene, so nothing that references it breaks) and these parts
/// are added under the open book, aligned to the printed page:
///
/// - a red hardcover board, a little larger than the page, with a rounded
///   darker spine along the left edge (the cover stays visibly 3D),
/// - a slim cream page block behind the page, with page lines on its right
///   and bottom edges,
/// - a gold ribbon bookmark hanging out of the bottom.
///
/// Sizes follow the page (any level, any scale). The parts have no colliders:
/// the open book keeps its own collider for dragging. Everything is drawn by
/// the book's Doc3DLayer like the rest of the model (it gathers renderers
/// every frame).
/// </summary>
public static class TaxCodeBookOpenLook
{
    public const string RootName = "Stylized Book (runtime)";

    private static readonly Color Cover = new Color32(176, 40, 42, 255);
    private static readonly Color Spine = new Color32(126, 24, 30, 255);
    private static readonly Color Pages = new Color32(242, 234, 212, 255);
    private static readonly Color PageLine = new Color32(214, 200, 168, 255);
    private static readonly Color Ribbon = new Color32(214, 168, 64, 255);

    /// <summary>Builds the look once on openBook around paper (the printed page). Safe to call again.</summary>
    public static void Apply(GameObject openBook, RectTransform paper)
    {
        if (openBook == null || paper == null || openBook.transform.Find(RootName) != null) return;
        Rect rect = paper.rect;
        if (rect.width <= 0f || rect.height <= 0f) return;

        // Hide the imported model (not the page canvas, not anything already hidden).
        foreach (Renderer renderer in openBook.GetComponentsInChildren<Renderer>(true))
            if (renderer.GetComponentInParent<Canvas>() == null) renderer.enabled = false;

        // A frame on the page: x right, y up, +z behind the page (a UI page faces -z).
        var root = new GameObject(RootName).transform;
        root.SetParent(openBook.transform, false);
        root.SetPositionAndRotation(paper.TransformPoint(rect.center), paper.rotation);
        float unit = Mathf.Max(1e-6f, Mathf.Abs(root.lossyScale.x));
        float w = rect.width * Mathf.Abs(paper.lossyScale.x) / unit;
        float h = rect.height * Mathf.Abs(paper.lossyScale.y) / unit;
        root.localScale = Vector3.one;

        float block = h * 0.03f;        // slim page block
        float board = h * 0.022f;       // hardcover board
        float coverW = w * 1.09f, coverH = h * 1.07f;
        float pageW = w * 1.03f, pageH = h * 1.03f;
        float behind = h * 0.002f;      // a hair behind the printed page

        // Page block with page lines on the right and bottom faces.
        Box(root, "Page Block", new Vector3(0f, 0f, behind + block * 0.5f), new Vector3(pageW, pageH, block), Pages);
        for (int i = 1; i <= 3; i++)
        {
            float z = behind + block * (i / 4f);
            Box(root, "Page Line R" + i, new Vector3(pageW * 0.5f + h * 0.0006f, 0f, z), new Vector3(h * 0.0015f, pageH * 0.98f, h * 0.0018f), PageLine);
            Box(root, "Page Line B" + i, new Vector3(0f, -pageH * 0.5f - h * 0.0006f, z), new Vector3(pageW * 0.98f, h * 0.0015f, h * 0.0018f), PageLine);
        }

        // The hardcover board behind the pages, offset toward the spine side.
        float boardZ = behind + block + board * 0.5f;
        Box(root, "Cover Board", new Vector3(-(coverW - pageW) * 0.25f, 0f, boardZ), new Vector3(coverW, coverH, board), Cover);

        // A rounded spine along the left edge, wrapping the page block.
        float spineRadius = (block + board) * 0.62f;
        Transform spine = Part(PrimitiveType.Cylinder, root, "Spine", Spine);
        spine.localPosition = new Vector3(-coverW * 0.5f - (coverW - pageW) * 0.25f + spineRadius * 0.35f, 0f, behind + (block + board) * 0.5f);
        spine.localRotation = Quaternion.identity;   // a cylinder's axis is its local Y: along the spine
        spine.localScale = new Vector3(spineRadius * 2f, coverH * 0.5f, spineRadius * 2f);

        // A gold ribbon bookmark hanging out of the bottom, slightly turned.
        Transform ribbon = Box(root, "Ribbon", new Vector3(w * 0.28f, -pageH * 0.5f - h * 0.07f, behind + block * 0.35f),
            new Vector3(w * 0.045f, h * 0.16f, h * 0.002f), Ribbon);
        ribbon.localRotation = Quaternion.Euler(0f, 0f, 7f);

        SetLayer(root, openBook.layer);
    }

    private static Transform Box(Transform parent, string name, Vector3 position, Vector3 size, Color color)
    {
        Transform part = Part(PrimitiveType.Cube, parent, name, color);
        part.localPosition = position;
        part.localRotation = Quaternion.identity;
        part.localScale = size;
        return part;
    }

    private static Transform Part(PrimitiveType type, Transform parent, string name, Color color)
    {
        GameObject part = GameObject.CreatePrimitive(type);
        part.name = name;
        Collider collider = part.GetComponent<Collider>();
        if (collider != null) Object.Destroy(collider);
        part.transform.SetParent(parent, false);
        MeshRenderer renderer = part.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = Material(color);
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        return part.transform;
    }

    private static Material Material(Color color)
    {
        Material solid = DecorModels.Solid(color, 0f, color == Ribbon ? 0.55f : 0.25f);
        if (solid != null) return solid;
        Shader lit = Shader.Find("Universal Render Pipeline/Lit");
        var fallback = new Material(lit != null ? lit : Shader.Find("Standard"));
        if (fallback.HasProperty("_BaseColor")) fallback.SetColor("_BaseColor", color);
        fallback.color = color;
        return fallback;
    }

    private static void SetLayer(Transform root, int layer)
    {
        root.gameObject.layer = layer;
        foreach (Transform child in root) SetLayer(child, layer);
    }
}
