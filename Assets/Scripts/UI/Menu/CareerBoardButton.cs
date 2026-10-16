using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// A chunky 3D push button for the career board (brass like the Level 1
/// reward screen's CONTINUE button): a dark base, a raised face that sinks
/// when pressed, and world lettering on the face.
/// </summary>
public sealed class CareerBoardButton : CareerBoardTarget
{
    private const float FaceRaise = 9f;
    private const float FacePressed = 2.5f;

    private readonly List<Material> materials = new List<Material>();
    private Transform face;
    private Material faceMaterial;
    private Material baseMaterial;
    private TextMeshPro label;
    private Color faceColor;
    private Color textColor;
    private bool interactable = true;
    private float bump;

    public override bool Interactable
    {
        get => interactable;
        set
        {
            interactable = value;
            Refresh();
        }
    }

    public static CareerBoardButton Create(Transform parent, string name, string text, Vector3 center, Vector2 size,
        Color faceColor, Color textColor, float fontSize)
    {
        Transform root = CareerBoardKit.Node(name, parent, center);
        var button = root.gameObject.AddComponent<CareerBoardButton>();
        button.Build(text, size, faceColor, textColor, fontSize);
        return button;
    }

    private void Build(string text, Vector2 size, Color color, Color ink, float fontSize)
    {
        faceColor = color;
        textColor = ink;
        baseMaterial = CareerBoardKit.Lit(Color.Lerp(color, Color.black, 0.45f), materials);
        faceMaterial = CareerBoardKit.Lit(color, materials, 0.45f);
        CareerBoardKit.Box(transform, "Shadow", new Vector3(3f, -5f, 2f), new Vector3(size.x, size.y, 4f),
            CareerBoardKit.CorkShadow, materials);
        CareerBoardKit.Box(transform, "Base", Vector3.zero, new Vector3(size.x, size.y, 8f), baseMaterial);
        face = CareerBoardKit.Node("Face", transform, new Vector3(0f, 0f, -FaceRaise));
        CareerBoardKit.Box(face, "Face plate", Vector3.zero, new Vector3(size.x - 8f, size.y - 8f, 8f), faceMaterial);
        CareerBoardKit.Box(face, "Face shine", new Vector3(0f, size.y * 0.2f, -4.2f),
            new Vector3(size.x - 26f, size.y * 0.22f, 0.5f), CareerBoardKit.Lit(Color.Lerp(color, Color.white, 0.35f), materials));
        label = CareerBoardKit.Text(face, "Label", text, fontSize, new Vector3(0f, 0f, -5f), size.x - 24f, size.y - 10f, ink);
        label.characterSpacing = 2f;
        AddHitBox(transform, new Vector3(0f, 0f, -4f), new Vector3(size.x, size.y, 24f));
        Refresh();
    }

    public void SetLabel(string text)
    {
        if (label != null) label.text = text;
    }

    public void SetColors(Color color, Color ink)
    {
        faceColor = color;
        textColor = ink;
        CareerBoardKit.SetColor(baseMaterial, Color.Lerp(color, Color.black, 0.45f));
        Refresh();
    }

    protected override void OnHoverChanged() => Refresh();
    protected override void OnPressChanged() => Refresh();
    protected override void OnTapped() => bump = 1f;

    private void Refresh()
    {
        if (faceMaterial == null) return;
        Color color = interactable ? faceColor : Color.Lerp(faceColor, new Color(0.55f, 0.58f, 0.6f), 0.75f);
        if (interactable && Hovered) color = Color.Lerp(color, Color.white, 0.18f);
        CareerBoardKit.SetColor(faceMaterial, color);
        if (label != null) label.color = interactable ? textColor : new Color(textColor.r, textColor.g, textColor.b, 0.55f);
    }

    private void Update()
    {
        if (face == null) return;
        float target = interactable && Pressed ? FacePressed : FaceRaise;
        bump = Mathf.MoveTowards(bump, 0f, Time.unscaledDeltaTime * 5f);
        float z = Mathf.Lerp(-face.localPosition.z, target, CareerBoardKit.Approach(28f, Time.unscaledDeltaTime));
        face.localPosition = new Vector3(0f, 0f, -z);
        float pop = 1f + Mathf.Sin(bump * Mathf.PI) * 0.06f;
        transform.localScale = Vector3.one * (interactable && Hovered && !Pressed ? 1.03f * pop : pop);
    }

    private void OnDestroy()
    {
        foreach (Material material in materials) CareerBoardKit.Dispose(material);
    }
}
