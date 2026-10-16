using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// PURPOSE:
/// Rewards that travel into the HUD element they update: reputation hearts
/// fly into the reputation bar, EXP stars into the EXP bar, and the target
/// gives a small bump as each one lands. Also a plain "pulse" for an icon
/// (e.g. the profile icon when a collected profile arrives).
///
/// Pooled UI images on their own overlay canvas above the HUD; no
/// allocations after warm-up, never blocks touches, and Update only does
/// work while something is in flight.
/// </summary>
public sealed class HudRewardFlyFX : MonoBehaviour
{
    private const int SortingOrder = 2700;
    private const float BurstDuration = 0.18f;
    private const float Stagger = 0.06f;

    private sealed class Particle
    {
        public Image Image;
        public RectTransform Rect;
        public Vector2 Start, Burst, Control, End;
        public RectTransform Target;
        public float Delay, Elapsed, Flight, Size;
        public bool Active, Arrived;
    }

    private sealed class Pulse
    {
        public RectTransform Target;
        public Vector3 BaseScale;
        public float Delay, Elapsed;
        public bool Active;
    }

    private RectTransform layer;
    private readonly List<Particle> particles = new List<Particle>();
    private readonly List<Pulse> pulses = new List<Pulse>();

    public static HudRewardFlyFX Create(Transform hudCanvas)
    {
        RectTransform layer = HudTheme.CreateRect("Reward Fly FX", hudCanvas);
        HudTheme.Stretch(layer);
        var canvas = layer.gameObject.AddComponent<Canvas>();
        canvas.overrideSorting = true;
        canvas.sortingOrder = SortingOrder;
        var fx = layer.gameObject.AddComponent<HudRewardFlyFX>();
        fx.layer = layer;
        fx.enabled = false;
        return fx;
    }

    /// <summary>
    /// Sends `count` sprites from a screen point into `target`. Returns the
    /// time until the first one lands (for holding the bar fill).
    /// </summary>
    public float Burst(Sprite sprite, Color color, int count, Vector2 fromScreen, RectTransform target, float size)
    {
        if (count <= 0 || target == null) return 0f;

        Vector2 start = ToLocal(fromScreen);
        Vector2 end = ToLocal(RectTransformUtility.WorldToScreenPoint(null, target.TransformPoint(target.rect.center)));
        float firstArrival = float.MaxValue;

        for (int i = 0; i < count; i++)
        {
            Particle p = Take();
            p.Image.sprite = sprite;
            p.Image.color = color;
            p.Size = size;
            p.Target = target;
            float angle = (i / (float)count) * Mathf.PI * 2f + Random.Range(-0.3f, 0.3f);
            float radius = Random.Range(40f, 90f);
            p.Start = start;
            p.Burst = start + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
            p.End = end;
            // Arc: bulge sideways and up between the burst point and the target.
            Vector2 mid = (p.Burst + p.End) * 0.5f;
            Vector2 side = Vector2.Perpendicular((p.End - p.Burst).normalized) * Random.Range(-90f, 90f);
            p.Control = mid + side + Vector2.up * 120f;
            p.Delay = i * Stagger;
            p.Flight = Random.Range(0.5f, 0.65f);
            p.Elapsed = 0f;
            p.Arrived = false;
            p.Active = true;
            p.Rect.anchoredPosition = start;
            p.Rect.localScale = Vector3.zero;
            p.Image.enabled = true;
            firstArrival = Mathf.Min(firstArrival, p.Delay + BurstDuration + p.Flight);
        }

        enabled = true;
        return firstArrival;
    }

    /// <summary>Small scale bump on a HUD element after `delay` seconds.</summary>
    public void PulseAfter(RectTransform target, float delay)
    {
        if (target == null) return;
        Pulse pulse = null;
        for (int i = 0; i < pulses.Count; i++)
        {
            // Restart a pulse already running on the same target (keeps its base scale).
            if (pulses[i].Active && pulses[i].Target == target)
            {
                pulses[i].Delay = delay;
                pulses[i].Elapsed = 0f;
                return;
            }
        }
        for (int i = 0; i < pulses.Count; i++)
            if (!pulses[i].Active) { pulse = pulses[i]; break; }
        if (pulse == null)
        {
            pulse = new Pulse();
            pulses.Add(pulse);
        }
        pulse.Target = target;
        pulse.BaseScale = target.localScale;
        pulse.Delay = delay;
        pulse.Elapsed = 0f;
        pulse.Active = true;
        enabled = true;
    }

    private void Update()
    {
        float dt = Time.unscaledDeltaTime;
        bool busy = false;

        for (int i = 0; i < particles.Count; i++)
        {
            Particle p = particles[i];
            if (!p.Active) continue;
            busy = true;
            p.Elapsed += dt;
            float t = p.Elapsed - p.Delay;
            if (t < 0f) continue;

            if (t < BurstDuration)
            {
                float k = HudTheme.EaseOutCubic(t / BurstDuration);
                p.Rect.anchoredPosition = Vector2.Lerp(p.Start, p.Burst, k);
                float s = p.Size * HudTheme.EaseOutBack(t / BurstDuration);
                p.Rect.localScale = new Vector3(s / 64f, s / 64f, 1f);
                continue;
            }

            float f = Mathf.Clamp01((t - BurstDuration) / p.Flight);
            float e = f * f * (3f - 2f * f);
            Vector2 a = Vector2.Lerp(p.Burst, p.Control, e);
            Vector2 b = Vector2.Lerp(p.Control, p.End, e);
            p.Rect.anchoredPosition = Vector2.Lerp(a, b, e);
            float size = Mathf.Lerp(p.Size, p.Size * 0.55f, e);
            p.Rect.localScale = new Vector3(size / 64f, size / 64f, 1f);

            if (f >= 1f)
            {
                p.Active = false;
                p.Image.enabled = false;
                PulseAfter(p.Target, 0f);
            }
        }

        for (int i = 0; i < pulses.Count; i++)
        {
            Pulse pulse = pulses[i];
            if (!pulse.Active) continue;
            busy = true;
            pulse.Elapsed += dt;
            float t = pulse.Elapsed - pulse.Delay;
            if (t < 0f) continue;
            float k = Mathf.Clamp01(t / 0.22f);
            float scale = 1f + 0.18f * Mathf.Sin(k * Mathf.PI);
            if (pulse.Target != null) pulse.Target.localScale = pulse.BaseScale * scale;
            if (k >= 1f)
            {
                if (pulse.Target != null) pulse.Target.localScale = pulse.BaseScale;
                pulse.Active = false;
            }
        }

        if (!busy) enabled = false;
    }

    private Particle Take()
    {
        for (int i = 0; i < particles.Count; i++)
            if (!particles[i].Active) return particles[i];

        RectTransform rect = HudTheme.CreateRect("Particle", layer);
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(64f, 64f);
        Image image = HudTheme.AddImage(rect, null, Color.white);
        image.enabled = false;
        var p = new Particle { Image = image, Rect = rect };
        particles.Add(p);
        return p;
    }

    private Vector2 ToLocal(Vector2 screen)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(layer, screen, null, out Vector2 local);
        return local;
    }
}
