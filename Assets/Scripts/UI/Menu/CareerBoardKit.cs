using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

/// <summary>
/// PURPOSE:
/// Building blocks for the 3D Menu career corkboards. Everything is real
/// scene geometry in "board pixels" (1 unit = 1 px of a 1500 x 850 layout,
/// +y up, -z toward the camera): corkboard props, paper cards, pushpins,
/// world TextMeshPro lettering, sprite icons and medals, and small
/// world-space canvases used only for progress bars.
///
/// Visual language is borrowed from Office Level 1 so the menu feels like
/// the same game: the Synty office corkboard, white profile-card papers on
/// red pushpins (CorkboardSideQuestManager / ProfileDragCard), the HUD
/// palette, fonts and bar style (HudTheme) and the badge medals (HudBadgeArt).
///
/// Every material created here is added to the caller's list; the caller
/// destroys them (Dispose) when it is destroyed.
/// </summary>
internal static class CareerBoardKit
{
    // Paper and cork palette (Level 1 profile cards are white URP cards).
    internal static readonly Color Ink = new Color(0.12f, 0.13f, 0.17f, 1f);       // PaperInkStyle.PrintInk
    internal static readonly Color InkSoft = new Color32(86, 92, 104, 255);
    // Secondary text on the career cards: close to black so it never blends into the paper.
    internal static readonly Color InkCard = new Color32(38, 41, 50, 255);
    internal static readonly Color PaperWhite = new Color32(246, 242, 230, 255);
    internal static readonly Color PaperYellow = new Color32(247, 226, 132, 255);
    internal static readonly Color PaperGreen = new Color32(196, 230, 176, 255);
    internal static readonly Color PaperGrey = new Color32(184, 190, 192, 255);
    internal static readonly Color PaperBlue = new Color32(190, 214, 236, 255);
    internal static readonly Color PinRed = new Color32(196, 52, 60, 255);
    internal static readonly Color PinBlue = new Color32(52, 120, 196, 255);
    internal static readonly Color PinGold = new Color32(222, 172, 60, 255);
    internal static readonly Color CorkShadow = new Color32(70, 52, 34, 255);
    internal static readonly Color Cream = HudTheme.TextPrimary;
    internal static readonly Color Navy = HudTheme.PanelInner;

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");

    // =========================================================
    // MATERIALS
    // =========================================================

    internal static Material Lit(Color color, List<Material> owned, float smoothness = 0.15f)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        var material = new Material(shader) { name = "Career Board " + ColorUtility.ToHtmlStringRGB(color) };
        SetColor(material, color);
        if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", smoothness);
        if (material.HasProperty("_Glossiness")) material.SetFloat("_Glossiness", smoothness);
        owned.Add(material);
        return material;
    }

    internal static void SetColor(Material material, Color color)
    {
        if (material == null) return;
        if (material.HasProperty(BaseColorId)) material.SetColor(BaseColorId, color);
        if (material.HasProperty(ColorId)) material.SetColor(ColorId, color);
    }

    // =========================================================
    // GEOMETRY
    // =========================================================

    internal static Transform Node(string name, Transform parent, Vector3 localPosition)
    {
        var go = new GameObject(name);
        go.layer = parent != null ? parent.gameObject.layer : 0;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPosition;
        return go.transform;
    }

    internal static MeshRenderer Box(Transform parent, string name, Vector3 position, Vector3 size,
        Color color, List<Material> owned, PrimitiveType type = PrimitiveType.Cube)
    {
        return Box(parent, name, position, size, Lit(color, owned), type);
    }

    internal static MeshRenderer Box(Transform parent, string name, Vector3 position, Vector3 size,
        Material material, PrimitiveType type = PrimitiveType.Cube)
    {
        GameObject go = GameObject.CreatePrimitive(type);
        go.name = name;
        go.layer = parent.gameObject.layer;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = position;
        go.transform.localScale = size;
        Dispose(go.GetComponent<Collider>());
        var renderer = go.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = ShadowCastingMode.On;
        renderer.receiveShadows = true;
        return renderer;
    }

    /// <summary>
    /// Instantiates an art prop (Synty office mesh) and fits it into width x height x depth,
    /// facing the camera. Desk props lie flat (top = +Y), wall props face +Z; the thinnest
    /// mesh axis decides which. Colliders are removed. Returns null when prefab is null.
    /// </summary>
    internal static Transform Prop(GameObject prefab, Transform parent, string name, Vector3 center,
        float width, float height, float depth)
    {
        if (prefab == null) return null;
        Transform wrapper = Node(name, parent, center);
        GameObject instance = Object.Instantiate(prefab, wrapper, false);
        instance.name = prefab.name;
        return Fit(instance, wrapper, parent, width, height, depth);
    }

    /// <summary>Like Prop, from a bare mesh and its materials (no children, no colliders).</summary>
    internal static Transform MeshProp(Mesh mesh, Material[] sharedMaterials, Transform parent, string name, Vector3 center,
        float width, float height, float depth)
    {
        if (mesh == null) return null;
        Transform wrapper = Node(name, parent, center);
        var instance = new GameObject(mesh.name, typeof(MeshFilter), typeof(MeshRenderer));
        instance.transform.SetParent(wrapper, false);
        instance.GetComponent<MeshFilter>().sharedMesh = mesh;
        instance.GetComponent<MeshRenderer>().sharedMaterials = sharedMaterials;
        return Fit(instance, wrapper, parent, width, height, depth);
    }

    private static Transform Fit(GameObject instance, Transform wrapper, Transform parent, float width, float height, float depth)
    {
        foreach (Collider collider in instance.GetComponentsInChildren<Collider>(true)) Dispose(collider);
        foreach (Renderer renderer in instance.GetComponentsInChildren<Renderer>(true))
        {
            renderer.gameObject.layer = parent.gameObject.layer;
            renderer.shadowCastingMode = ShadowCastingMode.On;
            renderer.receiveShadows = true;
        }

        if (!LocalBounds(instance.transform, out Bounds bounds) || bounds.size.sqrMagnitude < 1e-8f)
            return wrapper;

        Vector3 s = bounds.size;
        Quaternion facing;
        if (s.y <= s.x && s.y <= s.z) facing = Quaternion.Euler(-90f, 0f, 0f);      // lying flat: top faces the camera
        else if (s.x <= s.y && s.x <= s.z) facing = Quaternion.Euler(0f, 90f, 0f);  // front is +X
        else facing = Quaternion.Euler(0f, 180f, 0f);                               // wall prop: front is +Z

        instance.transform.localRotation = facing;
        instance.transform.localPosition = -(facing * bounds.center);
        Vector3 turned = facing * s;
        turned = new Vector3(Mathf.Abs(turned.x), Mathf.Abs(turned.y), Mathf.Abs(turned.z));
        wrapper.localScale = new Vector3(width / Mathf.Max(1e-4f, turned.x), height / Mathf.Max(1e-4f, turned.y),
            depth / Mathf.Max(1e-4f, turned.z));
        return wrapper;
    }

    /// <summary>Bounds of all meshes under root, in root's local space.</summary>
    private static bool LocalBounds(Transform root, out Bounds bounds)
    {
        bounds = default;
        bool any = false;
        foreach (MeshFilter filter in root.GetComponentsInChildren<MeshFilter>(true))
        {
            if (filter.sharedMesh == null) continue;
            Bounds mesh = filter.sharedMesh.bounds;
            Matrix4x4 toRoot = root.worldToLocalMatrix * filter.transform.localToWorldMatrix;
            for (int i = 0; i < 8; i++)
            {
                Vector3 corner = mesh.center + Vector3.Scale(mesh.extents,
                    new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                Vector3 point = toRoot.MultiplyPoint3x4(corner);
                if (!any) { bounds = new Bounds(point, Vector3.zero); any = true; }
                else bounds.Encapsulate(point);
            }
        }
        return any;
    }

    /// <summary>Red pushpin like the Level 1 corkboard pins: steel needle, flat collar, round head.</summary>
    internal static Transform Pushpin(Transform parent, Vector3 position, Color color, List<Material> owned, float scale = 1f)
    {
        Transform pin = Node("Pushpin", parent, position);
        pin.localScale = Vector3.one * scale;
        Material plastic = Lit(color, owned, 0.55f);
        MeshRenderer needle = Box(pin, "Needle", new Vector3(0f, 0f, 6f), new Vector3(2.5f, 7f, 2.5f),
            new Color32(150, 150, 150, 255), owned, PrimitiveType.Cylinder);
        needle.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        MeshRenderer collar = Box(pin, "Collar", new Vector3(0f, 0f, -2f), new Vector3(15f, 2.5f, 15f),
            plastic, PrimitiveType.Cylinder);
        collar.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        MeshRenderer stem = Box(pin, "Stem", new Vector3(0f, 0f, -6f), new Vector3(8f, 5f, 8f),
            plastic, PrimitiveType.Cylinder);
        stem.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        Box(pin, "Head", new Vector3(0f, 0f, -12f), new Vector3(16f, 16f, 11f), plastic, PrimitiveType.Sphere);
        Box(pin, "Shine", new Vector3(-3.5f, 3.5f, -16.5f), new Vector3(4f, 4f, 2f), Lit(Color.white, owned, 0.9f),
            PrimitiveType.Sphere);
        return pin;
    }

    /// <summary>Small 3D padlock (locked chapters).</summary>
    internal static Transform Padlock(Transform parent, Vector3 position, float size, List<Material> owned)
    {
        Transform lockRoot = Node("Padlock", parent, position);
        Material metal = Lit(new Color32(92, 100, 112, 255), owned, 0.6f);
        Box(lockRoot, "Body", new Vector3(0f, -size * 0.18f, 0f), new Vector3(size, size * 0.72f, size * 0.32f), metal);
        float t = size * 0.14f;
        Box(lockRoot, "Shackle left", new Vector3(-size * 0.3f, size * 0.3f, 0f), new Vector3(t, size * 0.42f, t), metal);
        Box(lockRoot, "Shackle right", new Vector3(size * 0.3f, size * 0.3f, 0f), new Vector3(t, size * 0.42f, t), metal);
        Box(lockRoot, "Shackle top", new Vector3(0f, size * 0.5f, 0f), new Vector3(size * 0.74f, t, t), metal);
        Box(lockRoot, "Keyhole", new Vector3(0f, -size * 0.16f, -size * 0.17f), new Vector3(t, t * 1.6f, 1f),
            new Color32(30, 34, 40, 255), owned);
        return lockRoot;
    }

    // =========================================================
    // TEXT (world TextMeshPro, not UI)
    // =========================================================

    /// <summary>
    /// 3D lettering centred on (x, y) inside a width x height box. sizePx is the cap
    /// size in board pixels (isOrthographic keeps TMP's 1:1 scale on the board).
    /// </summary>
    internal static TextMeshPro Text(Transform parent, string name, string text, float sizePx, Vector3 center,
        float width, float height, Color color, TextAlignmentOptions alignment = TextAlignmentOptions.Center,
        FontStyles style = FontStyles.Bold, TMP_FontAsset font = null, Material sharedMaterial = null)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.layer = parent.gameObject.layer;
        go.transform.SetParent(parent, false);
        var label = go.AddComponent<TextMeshPro>();
        label.font = font != null ? font : HudTheme.Font;
        if (sharedMaterial != null) label.fontSharedMaterial = sharedMaterial;
        label.isOrthographic = true;
        label.fontSize = sizePx;
        label.enableAutoSizing = true;
        label.fontSizeMax = sizePx;
        label.fontSizeMin = Mathf.Max(6f, sizePx * 0.55f);
        label.fontStyle = style;
        label.color = color;
        label.alignment = alignment;
        label.textWrappingMode = TextWrappingModes.Normal;
        label.overflowMode = TextOverflowModes.Overflow;
        label.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        // Room for ascenders and descenders, so auto-size only shrinks text that is really too long.
        label.rectTransform.sizeDelta = new Vector2(width, Mathf.Max(height, sizePx * 1.35f));
        label.rectTransform.localPosition = center;
        label.text = text;
        MeshRenderer renderer = label.GetComponent<MeshRenderer>();
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        return label;
    }

    /// <summary>A copy of the font's material with a soft drop shadow, so cream lettering reads on cork.</summary>
    internal static Material EmbossedMaterial(TMP_FontAsset font, List<Material> owned)
    {
        if (font == null || font.material == null) return null;
        var material = new Material(font.material) { name = font.name + " Career Emboss" };
        material.EnableKeyword("UNDERLAY_ON");
        if (material.HasProperty("_UnderlayColor")) material.SetColor("_UnderlayColor", new Color(0.08f, 0.05f, 0.02f, 0.85f));
        if (material.HasProperty("_UnderlayOffsetX")) material.SetFloat("_UnderlayOffsetX", 0.6f);
        if (material.HasProperty("_UnderlayOffsetY")) material.SetFloat("_UnderlayOffsetY", -0.6f);
        if (material.HasProperty("_UnderlaySoftness")) material.SetFloat("_UnderlaySoftness", 0.25f);
        if (material.HasProperty("_UnderlayDilate")) material.SetFloat("_UnderlayDilate", 0.15f);
        owned.Add(material);
        return material;
    }

    // =========================================================
    // SPRITES (Level 1 HUD icons and medals as 3D quads)
    // =========================================================

    internal static SpriteRenderer Sprite(Transform parent, string name, Sprite sprite, Vector3 center, float sizePx,
        Color color, int order = 2)
    {
        var go = new GameObject(name);
        go.layer = parent.gameObject.layer;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = center;
        var renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.color = color;
        renderer.sortingOrder = order;
        if (sprite != null)
        {
            float native = Mathf.Max(sprite.bounds.size.x, sprite.bounds.size.y);
            go.transform.localScale = Vector3.one * (sizePx / Mathf.Max(1e-4f, native));
        }
        return renderer;
    }

    // =========================================================
    // PROGRESS BAR (the one place a canvas is used)
    // =========================================================

    /// <summary>Level 1 HUD-style bar on a tiny world-space canvas. Returns the fill rect (anchorMax.x = progress).</summary>
    internal static RectTransform Bar(Transform parent, string name, Vector3 center, float width, float height,
        Color fill, out RectTransform passMark)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Canvas));
        go.layer = parent.gameObject.layer;
        go.transform.SetParent(parent, false);
        var canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 3;
        var rect = (RectTransform)go.transform;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(width, height);
        rect.localPosition = center;
        rect.localScale = Vector3.one;

        float radius = height * 0.5f;
        RectTransform rim = HudTheme.CreateRect("Rim", rect);
        HudTheme.Stretch(rim, -2f);
        HudTheme.AddImage(rim, HudTheme.Rounded, new Color(HudTheme.Brass.r, HudTheme.Brass.g, HudTheme.Brass.b, 0.9f), radius + 2f);
        RectTransform track = HudTheme.CreateRect("Track", rect);
        HudTheme.Stretch(track);
        HudTheme.AddImage(track, HudTheme.Rounded, HudTheme.PanelInner, radius);

        RectTransform fillRect = HudTheme.CreateRect("Fill", track);
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = new Vector2(0f, 1f);
        fillRect.offsetMin = new Vector2(2f, 2f);
        fillRect.offsetMax = new Vector2(-2f, -2f);
        HudTheme.AddImage(fillRect, HudTheme.Rounded, fill, radius - 2f);
        RectTransform shine = HudTheme.CreateRect("Shine", fillRect);
        shine.anchorMin = new Vector2(0f, 0.55f);
        shine.anchorMax = Vector2.one;
        shine.offsetMin = new Vector2(radius * 0.6f, 0f);
        shine.offsetMax = new Vector2(-radius * 0.6f, -1f);
        HudTheme.AddImage(shine, HudTheme.Rounded, new Color(1f, 1f, 1f, 0.28f), radius * 0.4f);

        passMark = HudTheme.CreateRect("Pass mark", rect);
        passMark.anchorMin = passMark.anchorMax = new Vector2(1f, 0.5f);
        passMark.pivot = new Vector2(0.5f, 0.5f);
        passMark.sizeDelta = new Vector2(3f, height + 8f);
        HudTheme.AddImage(passMark, HudTheme.Rounded, HudTheme.Complete, 1.5f);
        passMark.gameObject.SetActive(false);

        foreach (Transform child in go.GetComponentsInChildren<Transform>(true)) child.gameObject.layer = go.layer;
        return fillRect;
    }

    internal static void SetFill(RectTransform fill, float value)
    {
        if (fill == null) return;
        value = Mathf.Clamp01(value);
        fill.anchorMax = new Vector2(value, 1f);
        fill.gameObject.SetActive(value > 0.004f);
    }

    // =========================================================
    // MISC
    // =========================================================

    internal static void SetLayerRecursively(Transform root, int layer)
    {
        root.gameObject.layer = layer;
        for (int i = 0; i < root.childCount; i++) SetLayerRecursively(root.GetChild(i), layer);
    }

    internal static void Dispose(Object value)
    {
        if (value == null) return;
        if (Application.isPlaying) Object.Destroy(value);
        else Object.DestroyImmediate(value);
    }

    /// <summary>Critically damped approach factor for frame-rate independent smoothing.</summary>
    internal static float Approach(float sharpness, float deltaTime) => 1f - Mathf.Exp(-sharpness * deltaTime);
}
