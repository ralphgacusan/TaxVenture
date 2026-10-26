using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// PURPOSE:
/// The two Delivery A office decor models (T13), built the same way for the
/// catalogue's turning miniature (ShopPreviewStage) and for the office
/// (OfficeDecor):
/// - pencil_cup: a squat brass mesh cup (brass body, darker mesh bands and
///   slats, a bright rim) holding three pencils - red, blue and green - with
///   sharpened wood tips.
/// - island_print: a dark wood rectangular frame with a cream mat around an
///   island print (sky, sun, sea, a sandy island with a palm, a small sail).
///
/// Solid colours come from one URP Lit material (CampaignShopAssets
/// .solidMaterial) cloned once per colour with a white base map, so the
/// primitives never show a texture atlas. No colliders.
/// Origins: the cup's bottom centre; the frame's back centre, its front
/// facing +Z (hang it with the rotation looking out of the wall).
/// </summary>
public static class DecorModels
{
    public static readonly Vector2 PencilCupFootprint = new Vector2(0.09f, 0.09f);
    public static readonly Vector2 PrintSize = new Vector2(0.42f, 0.32f);

    private static readonly Dictionary<string, Material> materials = new Dictionary<string, Material>();

    private static readonly Color Brass = new Color32(214, 168, 82, 255);
    private static readonly Color BrassDark = new Color32(122, 88, 40, 255);
    private static readonly Color BrassBright = new Color32(240, 206, 128, 255);
    private static readonly Color Wood = new Color32(62, 38, 22, 255);
    private static readonly Color WoodEdge = new Color32(84, 54, 32, 255);

    /// <summary>The catalogue miniature's display plinth: the frame's dark wood with a brass rim.</summary>
    public static Material PlinthWood => Solid(Wood, 0f, 0.45f);
    public static Material PlinthBrass => Solid(BrassBright, 0.4f, 0.65f);

    public static bool Has(string itemId) => itemId == ShopCatalogue.PencilCup || itemId == ShopCatalogue.IslandPrint;

    /// <summary>Builds the model under parent (local origin as described above); null for an item without a model.</summary>
    public static GameObject Build(string itemId, Transform parent)
    {
        if (!Has(itemId)) return null;
        var root = new GameObject("Decor " + itemId);
        root.transform.SetParent(parent, false);
        if (itemId == ShopCatalogue.PencilCup) BuildPencilCup(root.transform);
        else BuildIslandPrint(root.transform);
        return root;
    }

    /// <summary>Puts the model and all its parts (world canvases too) on one layer.</summary>
    public static void SetLayer(GameObject root, int layer)
    {
        if (root == null) return;
        foreach (Transform t in root.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = layer;
    }

    /// <summary>World bounds of every renderer (and world canvas) under root.</summary>
    public static Bounds WorldBounds(GameObject root)
    {
        var bounds = new Bounds(root.transform.position, Vector3.zero);
        bool any = false;
        foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
        {
            if (!any) { bounds = renderer.bounds; any = true; }
            else bounds.Encapsulate(renderer.bounds);
        }
        foreach (RectTransform rect in root.GetComponentsInChildren<RectTransform>(true))
        {
            if (rect.GetComponent<Canvas>() == null) continue;
            var corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            foreach (Vector3 corner in corners)
            {
                if (!any) { bounds = new Bounds(corner, Vector3.zero); any = true; }
                else bounds.Encapsulate(corner);
            }
        }
        return bounds;
    }

    // =========================================================
    // PENCIL CUP
    // =========================================================

    private static void BuildPencilCup(Transform root)
    {
        const float d = 0.072f, h = 0.082f;
        Part(root, PrimitiveType.Cylinder, "Foot", new Vector3(0f, 0.002f, 0f), new Vector3(d + 0.004f, 0.002f, d + 0.004f), BrassDark, 0.35f, 0.5f);
        Part(root, PrimitiveType.Cylinder, "Body", new Vector3(0f, h * 0.5f, 0f), new Vector3(d, h * 0.5f, d), Brass, 0.35f, 0.55f);
        // The mesh: darker bands round the cup and thin slats between them.
        foreach (float y in new[] { 0.022f, 0.046f, 0.068f })
            Part(root, PrimitiveType.Cylinder, "Mesh Band", new Vector3(0f, y, 0f), new Vector3(d + 0.0016f, 0.0016f, d + 0.0016f), BrassDark, 0.3f, 0.4f);
        for (int i = 0; i < 14; i++)
        {
            float a = i * Mathf.PI * 2f / 14f;
            var slat = Part(root, PrimitiveType.Cube, "Mesh Slat", new Vector3(Mathf.Sin(a) * d * 0.5f, h * 0.5f, Mathf.Cos(a) * d * 0.5f),
                new Vector3(0.0026f, h * 0.86f, 0.0016f), BrassDark, 0.3f, 0.4f);
            slat.transform.localRotation = Quaternion.Euler(0f, a * Mathf.Rad2Deg, 0f);
        }
        Part(root, PrimitiveType.Cylinder, "Rim", new Vector3(0f, h, 0f), new Vector3(d + 0.006f, 0.003f, d + 0.006f), BrassBright, 0.4f, 0.65f);
        Part(root, PrimitiveType.Cylinder, "Inside", new Vector3(0f, h + 0.0005f, 0f), new Vector3(d - 0.006f, 0.0012f, d - 0.006f), new Color32(40, 30, 20, 255), 0f, 0.2f);

        Color[] pencils = { new Color32(196, 46, 42, 255), new Color32(42, 92, 184, 255), new Color32(46, 142, 78, 255) };
        float[] tilts = { 9f, 13f, 7f };
        for (int i = 0; i < 3; i++)
        {
            float a = i * 120f + 20f;
            var pivot = new GameObject("Pencil " + (i + 1)).transform;
            pivot.SetParent(root, false);
            pivot.localPosition = new Vector3(Mathf.Sin(a * Mathf.Deg2Rad) * 0.015f, 0.008f, Mathf.Cos(a * Mathf.Deg2Rad) * 0.015f);
            pivot.localRotation = Quaternion.Euler(0f, a, 0f) * Quaternion.Euler(tilts[i], 0f, 0f);
            float length = 0.122f + i * 0.008f;
            Part(pivot, PrimitiveType.Cylinder, "Body", new Vector3(0f, length * 0.5f, 0f), new Vector3(0.0074f, length * 0.5f, 0.0074f), pencils[i], 0f, 0.35f);
            Part(pivot, PrimitiveType.Cylinder, "Wood Tip", new Vector3(0f, length + 0.006f, 0f), new Vector3(0.0058f, 0.006f, 0.0058f), new Color32(232, 204, 152, 255), 0f, 0.2f);
            Part(pivot, PrimitiveType.Sphere, "Lead", new Vector3(0f, length + 0.0125f, 0f), new Vector3(0.0032f, 0.0042f, 0.0032f), new Color32(44, 44, 48, 255), 0.2f, 0.4f);
        }
    }

    // =========================================================
    // ISLAND PRINT
    // =========================================================

    private static void BuildIslandPrint(Transform root)
    {
        float w = PrintSize.x, h = PrintSize.y, bar = 0.03f, depth = 0.024f;
        Part(root, PrimitiveType.Cube, "Backing", new Vector3(0f, 0f, 0.004f), new Vector3(w - 0.01f, h - 0.01f, 0.008f), new Color32(34, 24, 16, 255), 0f, 0.1f);
        Part(root, PrimitiveType.Cube, "Frame Top", new Vector3(0f, h * 0.5f - bar * 0.5f, depth * 0.5f), new Vector3(w, bar, depth), Wood, 0f, 0.45f);
        Part(root, PrimitiveType.Cube, "Frame Bottom", new Vector3(0f, -h * 0.5f + bar * 0.5f, depth * 0.5f), new Vector3(w, bar, depth), Wood, 0f, 0.45f);
        Part(root, PrimitiveType.Cube, "Frame Left", new Vector3(-w * 0.5f + bar * 0.5f, 0f, depth * 0.5f), new Vector3(bar, h - bar * 2f, depth), Wood, 0f, 0.45f);
        Part(root, PrimitiveType.Cube, "Frame Right", new Vector3(w * 0.5f - bar * 0.5f, 0f, depth * 0.5f), new Vector3(bar, h - bar * 2f, depth), Wood, 0f, 0.45f);
        // A thin lighter lip on the inner edge reads as a moulding.
        Part(root, PrimitiveType.Cube, "Lip Top", new Vector3(0f, h * 0.5f - bar - 0.003f, depth * 0.55f), new Vector3(w - bar * 2f, 0.006f, 0.01f), WoodEdge, 0f, 0.5f);
        Part(root, PrimitiveType.Cube, "Lip Bottom", new Vector3(0f, -h * 0.5f + bar + 0.003f, depth * 0.55f), new Vector3(w - bar * 2f, 0.006f, 0.01f), WoodEdge, 0f, 0.5f);

        // The mat and the print, printed on a world-space face just in front of the backing.
        var size = new Vector2(720f, 520f);
        float faceWidth = w - bar * 2f;
        var host = new GameObject("Print Face", typeof(RectTransform));
        var face = (RectTransform)host.transform;
        face.SetParent(root, false);
        host.AddComponent<Canvas>().renderMode = RenderMode.WorldSpace;
        face.sizeDelta = size;
        face.localScale = Vector3.one * (faceWidth / size.x);
        face.localPosition = new Vector3(0f, 0f, 0.0095f);
        face.localRotation = Quaternion.Euler(0f, 180f, 0f);
        BuildPrintFace(face, size);
    }

    /// <summary>A cream mat around the island scene, with a small printed caption.</summary>
    private static void BuildPrintFace(RectTransform face, Vector2 size)
    {
        Box(face, "Mat", 0f, 0f, size.x, size.y, new Color32(240, 232, 212, 255));
        const float m = 46f;
        float pw = size.x - m * 2f, ph = size.y - m * 2f;
        Box(face, "Sky High", m, m, pw, ph * 0.32f, new Color32(126, 192, 236, 255));
        Box(face, "Sky Low", m, m + ph * 0.32f, pw, ph * 0.28f, new Color32(186, 222, 244, 255));
        RectTransform sun = Box(face, "Sun", m + pw * 0.72f, m + ph * 0.12f, 64f, 64f, new Color32(255, 212, 96, 255));
        sun.GetComponent<Image>().sprite = HudTheme.Circle;
        Box(face, "Sea", m, m + ph * 0.6f, pw, ph * 0.4f, new Color32(34, 118, 178, 255));
        Box(face, "Sea Light", m, m + ph * 0.6f, pw, 8f, new Color32(150, 210, 236, 255));
        Box(face, "Island", m + pw * 0.16f, m + ph * 0.54f, pw * 0.5f, ph * 0.16f, new Color32(234, 206, 142, 255), 40f);
        Box(face, "Hill", m + pw * 0.26f, m + ph * 0.46f, pw * 0.26f, ph * 0.14f, new Color32(70, 150, 86, 255), 50f);
        RectTransform trunk = Box(face, "Palm Trunk", m + pw * 0.56f, m + ph * 0.3f, 10f, ph * 0.3f, new Color32(116, 80, 46, 255));
        trunk.localRotation = Quaternion.Euler(0f, 0f, -10f);
        for (int i = 0; i < 5; i++)
        {
            RectTransform leaf = Box(face, "Leaf " + i, m + pw * 0.5f + i * 10f, m + ph * 0.26f + (i % 2) * 8f, 70f, 16f, new Color32(40, 132, 74, 255), 8f);
            leaf.localRotation = Quaternion.Euler(0f, 0f, -40f + i * 22f);
        }
        RectTransform sail = Box(face, "Sail", m + pw * 0.8f, m + ph * 0.5f, 18f, 42f, new Color32(250, 246, 236, 255));
        sail.localRotation = Quaternion.Euler(0f, 0f, -6f);
        Box(face, "Hull", m + pw * 0.78f, m + ph * 0.66f, 44f, 10f, new Color32(150, 60, 44, 255), 4f);
        TextMeshProUGUI caption = HudTheme.CreateLabel("Caption", face, "COAST TALES ISLAND", 22f, new Color32(120, 96, 64, 255), TextAlignmentOptions.Center, FontStyles.Bold);
        HudTheme.PlaceTopLeft(caption.rectTransform, 0f, size.y - m + 6f, size.x, 34f);
        caption.characterSpacing = 6f;
    }

    // =========================================================
    // HELPERS
    // =========================================================

    private static GameObject Part(Transform parent, PrimitiveType type, string name, Vector3 localPosition, Vector3 scale, Color color, float metallic, float smoothness)
    {
        GameObject part = GameObject.CreatePrimitive(type);
        part.name = name;
        Object.Destroy(part.GetComponent<Collider>());
        part.transform.SetParent(parent, false);
        part.transform.localPosition = localPosition;
        part.transform.localScale = scale;
        Material material = Solid(color, metallic, smoothness);
        if (material != null) part.GetComponent<Renderer>().sharedMaterial = material;
        return part;
    }

    /// <summary>One solid-colour copy of the project's URP Lit material per colour (shared, never per part; also the T14 drawer unit's oak and brass).</summary>
    public static Material Solid(Color color, float metallic, float smoothness)
    {
        string key = ColorUtility.ToHtmlStringRGBA(color) + "/" + metallic + "/" + smoothness;
        if (materials.TryGetValue(key, out Material cached) && cached != null) return cached;
        CampaignShopAssets assets = CampaignShopAssets.Load();
        Material source = assets != null ? assets.solidMaterial : null;
        if (source == null) return null;
        var material = new Material(source) { name = "Decor " + key };
        if (material.HasProperty("_BaseMap")) material.SetTexture("_BaseMap", Texture2D.whiteTexture);
        if (material.HasProperty("_MainTex")) material.SetTexture("_MainTex", Texture2D.whiteTexture);
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
        if (material.HasProperty("_Color")) material.SetColor("_Color", color);
        if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", metallic);
        if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", smoothness);
        materials[key] = material;
        return material;
    }

    private static RectTransform Box(RectTransform parent, string name, float x, float y, float w, float h, Color color, float radius = 0f)
    {
        RectTransform rect = HudTheme.PlaceTopLeft(HudTheme.CreateRect(name, parent), x, y, w, h);
        HudTheme.AddImage(rect, radius > 0f ? HudTheme.Rounded : null, color, radius).raycastTarget = false;
        return rect;
    }
}
