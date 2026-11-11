using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// A 3D volume fader for the Settings board, built like SettingsToggle3D: a brass
/// ring, a dark outline, a dark track that fills with the row's colour (mint when
/// on, slate when off), brass marks under it and the same cream knob. Press
/// anywhere on it to jump there, then drag left or right; the board keeps calling
/// DragTo while the press lasts, so the finger can drift off the row. A readout
/// above the right end shows the percentage, or OFF. Runs on unscaled time.
/// </summary>
public sealed class SettingsSlider3D : CareerBoardTarget
{
    private const float TrackHeight = 20f;
    private const float KnobSize = 38f;
    private const float EndSnap = 0.02f;
    private const int NotchCount = 10;

    private static readonly Color EmptyColor = new Color32(14, 20, 30, 255);
    private static readonly Color OffFillColor = new Color32(70, 84, 102, 255);
    private static readonly Color OffKnobColor = new Color32(166, 172, 182, 255);

    private readonly List<Material> materials = new List<Material>();
    private Material fillMaterial;
    private Material knobMaterial;
    private Transform fillMiddle;
    private Transform fillCap;
    private Transform knob;
    private TextMeshPro readout;
    private Color onColor;
    private float width;
    private float travel;
    private float value;
    private float shown;
    private float lit = 1f;
    private float knobScale = 1f;
    private bool channelOn = true;
    private bool dragging;

    /// <summary>0-1, snapped to whole percents and to the two ends.</summary>
    public float Value => value;

    /// <summary>Raised while the player moves the knob (not when SetValue is called from code).</summary>
    public event Action<SettingsSlider3D, float> Dragged;
    /// <summary>Raised each time a drag crosses a 10% step.</summary>
    public event Action<SettingsSlider3D> Notched;
    /// <summary>Raised when the press that moved the knob ends, including when the board takes the pointer away.</summary>
    public event Action<SettingsSlider3D> Released;

    public static SettingsSlider3D Create(Transform parent, string name, Vector3 center, float width,
        Vector3 hitCenter, Vector3 hitSize, Color onColor)
    {
        Transform root = CareerBoardKit.Node(name, parent, center);
        var slider = root.gameObject.AddComponent<SettingsSlider3D>();
        slider.Build(width, hitCenter, hitSize, onColor);
        return slider;
    }

    private void Build(float size, Vector3 hitCenter, Vector3 hitSize, Color on)
    {
        width = size;
        onColor = on;
        travel = size * 0.5f - 12f;
        Vector2 track = new Vector2(size, TrackHeight);

        // Rings from the back forward, at the depths the switch uses.
        Color ink = new Color32(14, 18, 26, 255);
        SettingsToggle3D.Pill(transform, "Brass ring", track + new Vector2(14f, 14f), 6f, 10f, CareerBoardKit.Lit(HudTheme.Brass, materials, 0.6f));
        SettingsToggle3D.Pill(transform, "Outline", track + new Vector2(6f, 6f), 2f, 10f, CareerBoardKit.Lit(ink, materials, 0.2f));
        SettingsToggle3D.Pill(transform, "Track", track, -2f, 10f, CareerBoardKit.Lit(EmptyColor, materials, 0.35f));

        // The fill runs from the left end of the track to the knob's centre.
        fillMaterial = CareerBoardKit.Lit(on, materials, 0.55f);
        fillMiddle = CareerBoardKit.Box(transform, "Fill", new Vector3(0f, 0f, -2.6f), new Vector3(1f, TrackHeight, 10f), fillMaterial).transform;
        fillCap = SettingsToggle3D.Disc(transform, "Fill cap", TrackHeight, -2.6f, 10f, fillMaterial).transform;
        fillCap.localPosition = new Vector3(-(size * 0.5f - TrackHeight * 0.5f), 0f, -2.6f);

        // Brass marks at 0, 25, 50, 75 and 100%.
        Material brass = CareerBoardKit.Lit(HudTheme.BrassSoft, materials, 0.6f);
        for (int i = 0; i <= 4; i++)
            CareerBoardKit.Box(transform, "Mark " + i, new Vector3(Mathf.Lerp(-travel, travel, i / 4f), -21f, 0f),
                new Vector3(i % 2 == 0 ? 3f : 2f, i % 2 == 0 ? 8f : 5f, 1f), brass);

        knob = CareerBoardKit.Node("Knob", transform, Vector3.zero);
        SettingsToggle3D.Disc(knob, "Knob outline", KnobSize + 8f, -9f, 8f, CareerBoardKit.Lit(ink, materials, 0.2f));
        knobMaterial = CareerBoardKit.Lit(HudTheme.TextPrimary, materials, 0.5f);
        SettingsToggle3D.Disc(knob, "Knob", KnobSize, -12f, 8f, knobMaterial);
        SettingsToggle3D.Disc(knob, "Knob shine", KnobSize * 0.32f, -16.4f, 1f, CareerBoardKit.Lit(Color.white, materials, 0.95f))
            .transform.localPosition = new Vector3(-KnobSize * 0.18f, KnobSize * 0.18f, -16.4f);

        readout = CareerBoardKit.Text(transform, "Readout", "", 25f, new Vector3(size * 0.5f - 50f, 63f, 1f), 100f, 34f,
            on, TextAlignmentOptions.MidlineRight);

        AddHitBox(transform, hitCenter, hitSize);
        Apply();
    }

    /// <summary>Shows a saved value. animate = glide (false snaps, e.g. when the board opens).</summary>
    public void SetValue(float newValue, bool animate)
    {
        value = Mathf.Clamp01(newValue);
        if (!animate) shown = value;
        RefreshReadout();
        Apply();
    }

    /// <summary>Whether the row's switch is on: fills mint or slate, and the readout shows the percent or OFF.</summary>
    public void ShowEnabled(bool on)
    {
        channelOn = on;
        if (!Application.isPlaying) lit = on ? 1f : 0f;
        RefreshReadout();
        Apply();
    }

    /// <summary>Moves the knob to where this ray crosses the slider. Called each frame of a press.</summary>
    public void DragTo(Ray ray)
    {
        if (!Interactable) return;
        if (!new Plane(transform.forward, transform.position).Raycast(ray, out float distance)) return;
        dragging = true;

        float x = transform.InverseTransformPoint(ray.GetPoint(distance)).x;
        float next = Mathf.InverseLerp(-travel, travel, x);
        next = next < EndSnap ? 0f : next > 1f - EndSnap ? 1f : Mathf.Round(next * 100f) / 100f;
        if (Mathf.Approximately(next, value)) return;

        bool notched = Mathf.FloorToInt(next * NotchCount) != Mathf.FloorToInt(value * NotchCount);
        value = next;
        Dragged?.Invoke(this, value);
        RefreshReadout();
        if (notched) Notched?.Invoke(this);
    }

    protected override void OnPressChanged()
    {
        if (Pressed || !dragging) return;
        dragging = false;
        Released?.Invoke(this);
    }

    private void RefreshReadout()
    {
        if (readout == null) return;
        readout.text = channelOn ? Mathf.RoundToInt(value * 100f) + "%" : "OFF";
        readout.color = channelOn ? onColor : HudTheme.TextMuted;
    }

    private void Update()
    {
        float dt = Time.unscaledDeltaTime;
        shown = Mathf.Lerp(shown, value, CareerBoardKit.Approach(dragging ? 40f : 14f, dt));
        lit = Mathf.MoveTowards(lit, channelOn ? 1f : 0f, dt * 5f);
        knobScale = Mathf.Lerp(knobScale, dragging ? 1.14f : 1f, CareerBoardKit.Approach(24f, dt));
        Apply();
    }

    private void Apply()
    {
        if (knob == null) return;
        float knobX = Mathf.Lerp(-travel, travel, shown);
        knob.localPosition = new Vector3(knobX, 0f, 0f);
        knob.localScale = Vector3.one * knobScale;

        float start = fillCap.localPosition.x;
        float length = Mathf.Max(0.1f, knobX - start);
        fillMiddle.localPosition = new Vector3(start + length * 0.5f, 0f, -2.6f);
        fillMiddle.localScale = new Vector3(length, TrackHeight, 10f);

        Color fill = Color.Lerp(OffFillColor, onColor, lit);
        if (Interactable && (Hovered || dragging)) fill = Color.Lerp(fill, Color.white, 0.12f);
        CareerBoardKit.SetColor(fillMaterial, fill);
        CareerBoardKit.SetColor(knobMaterial, Color.Lerp(OffKnobColor, HudTheme.TextPrimary, lit));
    }

    private void OnDestroy()
    {
        foreach (Material material in materials) CareerBoardKit.Dispose(material);
    }
}
