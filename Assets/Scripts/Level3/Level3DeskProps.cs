using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// PURPOSE:
/// Shared low-poly building blocks for Level 3's desk tools (T03): the
/// office's cream / navy / brass palette as URP Lit materials, bevel-free
/// boxes and cylinders with real thickness, and flat 3D TextMeshPro print
/// lying on a surface. Same construction as Level 2's hand stamps and
/// toolkit papers (primitives + world TextMeshPro); materials are created
/// once per owner and destroyed with it.
/// </summary>
public sealed class Level3DeskProps
{
    // The HUD palette (HudTheme) on physical objects.
    public static readonly Color Navy = new Color32(24, 36, 52, 255);
    public static readonly Color NavyLight = new Color32(52, 66, 82, 255);
    public static readonly Color Display = new Color32(14, 22, 32, 255);
    public static readonly Color DisplayInk = new Color32(170, 226, 196, 255);
    public static readonly Color Cream = new Color32(245, 240, 226, 255);
    public static readonly Color Paper = new Color32(250, 247, 238, 255);
    public static readonly Color Brass = new Color32(202, 169, 101, 255);
    public static readonly Color BrassSoft = new Color32(221, 190, 126, 255);
    public static readonly Color Terracotta = new Color32(176, 98, 74, 255);
    public static readonly Color Ink = new Color32(28, 38, 52, 255);
    public static readonly Color InkMuted = new Color32(96, 106, 118, 255);
    public static readonly Color Highlight = new Color32(100, 215, 255, 255);

    private readonly Dictionary<Color, Material> materials = new Dictionary<Color, Material>();
    private readonly int layer;

    public Level3DeskProps(int layer)
    {
        this.layer = layer;
    }

    public Material Lit(Color color)
    {
        if (materials.TryGetValue(color, out Material existing) && existing != null) return existing;
        Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        var material = new Material(shader) { name = "Level3 Desk " + ColorUtility.ToHtmlStringRGB(color), color = color };
        if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", 0.25f);
        materials[color] = material;
        return material;
    }

    /// <summary>A box of the given size (meters, local) centered at localPosition; no collider.</summary>
    public GameObject Box(string name, Transform parent, Vector3 localPosition, Vector3 size, Color color) =>
        Shape(PrimitiveType.Cube, name, parent, localPosition, size, color);

    /// <summary>A cylinder lying along local X (length = size.x, diameter = size.y).</summary>
    public GameObject Roll(string name, Transform parent, Vector3 localPosition, float length, float diameter, Color color)
    {
        GameObject roll = Shape(PrimitiveType.Cylinder, name, parent, localPosition, new Vector3(diameter, length * 0.5f, diameter), color);
        roll.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
        return roll;
    }

    public GameObject Shape(PrimitiveType type, string name, Transform parent, Vector3 localPosition, Vector3 size, Color color)
    {
        GameObject part = GameObject.CreatePrimitive(type);
        part.name = name;
        part.layer = layer;
        Object.Destroy(part.GetComponent<Collider>());
        part.transform.SetParent(parent, false);
        part.transform.localPosition = localPosition;
        part.transform.localScale = size;
        MeshRenderer renderer = part.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = Lit(color);
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        return part;
    }

    /// <summary>
    /// Flat print lying on a surface (facing local +Y, reading toward local +Z),
    /// fitted into width × height meters. Auto-sized, never wrapped unless asked.
    /// </summary>
    public TextMeshPro Print(string name, Transform parent, Vector3 localPosition, float width, float height, string text,
        Color color, TextAlignmentOptions alignment = TextAlignmentOptions.Center, bool bold = false, bool wrap = false)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.layer = layer;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPosition;
        go.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        var label = go.AddComponent<TextMeshPro>();
        label.rectTransform.sizeDelta = new Vector2(width, height);
        label.font = HudTheme.Font;
        label.text = text ?? "";
        label.color = color;
        label.alignment = alignment;
        label.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
        label.enableAutoSizing = true;
        label.fontSizeMin = 0.01f;
        label.fontSizeMax = 1f;
        label.textWrappingMode = wrap ? TextWrappingModes.Normal : TextWrappingModes.NoWrap;
        label.overflowMode = TextOverflowModes.Ellipsis;
        label.margin = Vector4.zero;
        MeshRenderer renderer = label.GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }
        return label;
    }

    /// <summary>A tap target (trigger-free box collider on the owner's pointer layer) covering a local area.</summary>
    public BoxCollider Target(string name, Transform parent, Vector3 localPosition, Vector3 size)
    {
        var go = new GameObject(name);
        go.layer = layer;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPosition;
        var box = go.AddComponent<BoxCollider>();
        box.size = size;
        return box;
    }

    /// <summary>Four thin bars around a local rectangle (selection / armed slot frame).</summary>
    public GameObject Frame(string name, Transform parent, Vector3 center, float width, float depth, float bar, Color color)
    {
        var frame = new GameObject(name);
        frame.layer = layer;
        frame.transform.SetParent(parent, false);
        frame.transform.localPosition = center;
        Box("Top", frame.transform, new Vector3(0f, 0f, depth * 0.5f), new Vector3(width + bar, bar * 0.4f, bar), color);
        Box("Bottom", frame.transform, new Vector3(0f, 0f, -depth * 0.5f), new Vector3(width + bar, bar * 0.4f, bar), color);
        Box("Left", frame.transform, new Vector3(-width * 0.5f, 0f, 0f), new Vector3(bar, bar * 0.4f, depth), color);
        Box("Right", frame.transform, new Vector3(width * 0.5f, 0f, 0f), new Vector3(bar, bar * 0.4f, depth), color);
        return frame;
    }

    public void Destroy()
    {
        foreach (Material material in materials.Values)
            if (material != null) Object.Destroy(material);
        materials.Clear();
    }
}
