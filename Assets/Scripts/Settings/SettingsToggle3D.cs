using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A chunky 3D pill switch for the Settings board: a brass outer ring, a dark
/// outline, a coloured track (mint when on, slate when off) with two gloss
/// slashes, and a cream knob that slides across with a little overshoot and
/// squash. The whole settings row is the touch target, so it is easy to hit
/// on a phone. Shares the tap handling of the career board (CareerBoardTarget).
/// </summary>
public sealed class SettingsToggle3D : CareerBoardTarget
{
    private const float SlideDuration = 0.32f;

    private static readonly Color OffColor = new Color32(70, 84, 102, 255);

    private readonly List<Material> materials = new List<Material>();
    private Material trackMaterial;
    private Material rowMaterial;
    private Color rowColor;
    private Color onColor;
    private Transform pill;
    private Transform knob;
    private Transform[] slashes;
    private Vector2 pillSize;
    private bool isOn;
    private float shown;      // 0 = off, 1 = on (knob position, may overshoot)
    private float from;
    private float elapsed = -1f;
    private float pressScale = 1f;

    public bool IsOn => isOn;

    /// <summary>Raised when the player flips the switch (not when SetOn is called from code).</summary>
    public event Action<SettingsToggle3D, bool> Changed;

    /// <summary>
    /// rowCenter / rowSize: the whole row (touch area, hover tint on rowMaterial).
    /// pillCenter is relative to the row.
    /// </summary>
    public static SettingsToggle3D Create(Transform parent, string name, Vector3 rowCenter, Vector2 rowSize,
        Material rowMaterial, Vector3 pillCenter, Vector2 pillSize, Color onColor)
    {
        Transform root = CareerBoardKit.Node(name, parent, rowCenter);
        var toggle = root.gameObject.AddComponent<SettingsToggle3D>();
        toggle.Build(rowSize, rowMaterial, pillCenter, pillSize, onColor);
        return toggle;
    }

    private void Build(Vector2 rowSize, Material row, Vector3 pillCenter, Vector2 size, Color on)
    {
        rowMaterial = row;
        rowColor = row != null && row.HasProperty("_BaseColor") ? row.GetColor("_BaseColor") : Color.gray;
        onColor = on;
        pillSize = size;
        pill = CareerBoardKit.Node("Switch", transform, pillCenter);

        // Rings from the back forward: brass ring, dark outline, track.
        Pill(pill, "Brass ring", size + new Vector2(22f, 22f), 6f, 10f, CareerBoardKit.Lit(HudTheme.Brass, materials, 0.6f));
        Pill(pill, "Outline", size + new Vector2(10f, 10f), 2f, 10f, CareerBoardKit.Lit(new Color32(14, 18, 26, 255), materials, 0.2f));
        trackMaterial = CareerBoardKit.Lit(OffColor, materials, 0.55f);
        Pill(pill, "Track", size, -2f, 10f, trackMaterial);

        Material gloss = CareerBoardKit.Lit(new Color(1f, 1f, 1f, 1f), materials, 0.9f);
        slashes = new Transform[2];
        for (int i = 0; i < 2; i++)
        {
            MeshRenderer slash = CareerBoardKit.Box(pill, "Gloss " + (i + 1), Vector3.zero,
                new Vector3(5f, size.y * 0.42f, 1f), gloss);
            slash.transform.localRotation = Quaternion.Euler(0f, 0f, -38f);
            slashes[i] = slash.transform;
        }

        float knobSize = size.y - 16f;
        knob = CareerBoardKit.Node("Knob", pill, Vector3.zero);
        Disc(knob, "Knob outline", knobSize + 9f, -9f, 8f, CareerBoardKit.Lit(new Color32(14, 18, 26, 255), materials, 0.2f));
        Disc(knob, "Knob", knobSize, -12f, 8f, CareerBoardKit.Lit(HudTheme.TextPrimary, materials, 0.5f));
        Disc(knob, "Knob shine", knobSize * 0.32f, -16.4f, 1f, CareerBoardKit.Lit(Color.white, materials, 0.95f))
            .transform.localPosition = new Vector3(-knobSize * 0.18f, knobSize * 0.18f, -16.4f);

        AddHitBox(transform, new Vector3(0f, 0f, -6f), new Vector3(rowSize.x, rowSize.y, 24f));
        Apply();
    }

    /// <summary>A capsule facing the camera: a box between two discs.</summary>
    private static void Pill(Transform parent, string name, Vector2 size, float z, float depth, Material material)
    {
        float r = size.y * 0.5f;
        CareerBoardKit.Box(parent, name + " middle", new Vector3(0f, 0f, z), new Vector3(Mathf.Max(0.1f, size.x - size.y), size.y, depth), material);
        Disc(parent, name + " left", size.y, z, depth, material).transform.localPosition = new Vector3(-(size.x * 0.5f - r), 0f, z);
        Disc(parent, name + " right", size.y, z, depth, material).transform.localPosition = new Vector3(size.x * 0.5f - r, 0f, z);
    }

    private static MeshRenderer Disc(Transform parent, string name, float diameter, float z, float depth, Material material)
    {
        MeshRenderer disc = CareerBoardKit.Box(parent, name, new Vector3(0f, 0f, z), new Vector3(diameter, depth * 0.5f, diameter),
            material, PrimitiveType.Cylinder);
        disc.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        return disc;
    }

    /// <summary>Shows a saved value. animate = slide (false snaps, e.g. when the board opens).</summary>
    public void SetOn(bool value, bool animate)
    {
        isOn = value;
        if (animate)
        {
            from = shown;
            elapsed = 0f;
        }
        else
        {
            shown = value ? 1f : 0f;
            elapsed = -1f;
        }
        Apply();
    }

    protected override void OnTapped()
    {
        SetOn(!isOn, animate: true);
        Changed?.Invoke(this, isOn);
    }

    protected override void OnHoverChanged() => Apply();
    protected override void OnPressChanged() => Apply();

    private void Update()
    {
        float dt = Time.unscaledDeltaTime;
        if (elapsed >= 0f)
        {
            elapsed += dt;
            float t = Mathf.Clamp01(elapsed / SlideDuration);
            shown = Mathf.LerpUnclamped(from, isOn ? 1f : 0f, HudTheme.EaseOutBack(t));
            if (t >= 1f) elapsed = -1f;
        }
        pressScale = Mathf.Lerp(pressScale, Interactable && Pressed ? 0.95f : 1f, CareerBoardKit.Approach(24f, dt));
        Apply();
    }

    private void Apply()
    {
        if (knob == null) return;
        float r = pillSize.y * 0.5f;
        float travel = pillSize.x * 0.5f - r;
        float t = Mathf.Clamp01(shown);

        // The knob stretches a little while it travels.
        float moving = elapsed >= 0f ? Mathf.Sin(Mathf.Clamp01(elapsed / SlideDuration) * Mathf.PI) : 0f;
        knob.localPosition = new Vector3(Mathf.Lerp(-travel, travel, shown), 0f, 0f);
        knob.localScale = new Vector3(1f + 0.16f * moving, 1f - 0.08f * moving, 1f);

        // Gloss slashes sit on the empty side of the track.
        float side = Mathf.Lerp(travel * 0.55f, -travel * 0.55f, t);
        for (int i = 0; i < slashes.Length; i++)
            slashes[i].localPosition = new Vector3(side + (i == 0 ? -9f : 9f), 0f, -7.6f);

        Color track = Color.Lerp(OffColor, onColor, t);
        if (Interactable && Hovered) track = Color.Lerp(track, Color.white, 0.12f);
        CareerBoardKit.SetColor(trackMaterial, track);
        if (rowMaterial != null)
            CareerBoardKit.SetColor(rowMaterial, Interactable && Hovered ? Color.Lerp(rowColor, Color.white, 0.06f) : rowColor);
        pill.localScale = Vector3.one * pressScale;
    }

    private void OnDestroy()
    {
        foreach (Material material in materials) CareerBoardKit.Dispose(material);
    }
}
