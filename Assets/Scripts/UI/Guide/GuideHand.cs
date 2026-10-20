using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// PURPOSE:
/// The animated pointing glove of the guide. It never just appears: it
/// glides from where it was to the new target, then acts out the gesture
/// the player should make (tap, hold, drag, swipe or joystick push) with
/// ripple rings where the finger presses. Purely visual; never blocks
/// touches. Coordinates are local to the guide canvas root.
/// </summary>
public sealed class GuideHand : MonoBehaviour
{
    public enum Gesture { None, Tap, Hold, Drag, Swipe, Joystick }

    private const float TravelSeconds = 0.5f;
    private const float HandSize = 150f;
    private const float Tilt = -16f;

    private RectTransform root;
    private RectTransform handRect;
    private RectTransform shadowRect;
    private Image handImage;
    private Image shadowImage;
    private RectTransform carryRect;
    private Image carryImage;
    [SerializeField] private RectTransform[] ripples = new RectTransform[3];
    [SerializeField] private Image[] rippleImages = new Image[3];
    [SerializeField] private float[] rippleAge = { 9f, 9f, 9f };
    private int nextRipple;
    private Color accent = new Color32(100, 215, 255, 255);

    private Gesture gesture;
    private Rect from;
    private Rect to;
    private float clock;
    private float lastCycleT;
    private bool visible;
    private float alpha;

    private Vector2 position;
    private Vector2 travelStart;
    private float travelAge = 99f;
    private bool hasPosition;

    public static GuideHand Create(RectTransform parent)
    {
        RectTransform rect = HudTheme.CreateRect("Guide Hand", parent);
        HudTheme.Stretch(rect);
        var hand = rect.gameObject.AddComponent<GuideHand>();
        hand.root = rect;
        hand.Build();
        return hand;
    }

    private void Build()
    {
        for (int i = 0; i < ripples.Length; i++)
        {
            ripples[i] = HudTheme.CreateRect("Ripple " + i, root);
            ripples[i].sizeDelta = new Vector2(120f, 120f);
            rippleImages[i] = HudTheme.AddImage(ripples[i], GuideArt.Ring, Color.clear);
            ripples[i].gameObject.SetActive(false);
        }

        carryRect = HudTheme.CreateRect("Carry Glow", root);
        carryRect.sizeDelta = new Vector2(110f, 110f);
        carryImage = HudTheme.AddImage(carryRect, GuideArt.Glow, Color.clear);

        shadowRect = HudTheme.CreateRect("Hand Shadow", root);
        shadowRect.sizeDelta = new Vector2(HandSize, HandSize);
        shadowRect.pivot = GuideArt.HandTip;
        shadowImage = HudTheme.AddImage(shadowRect, GuideArt.Hand, new Color(0f, 0.02f, 0.06f, 0f));

        handRect = HudTheme.CreateRect("Hand", root);
        handRect.sizeDelta = new Vector2(HandSize, HandSize);
        handRect.pivot = GuideArt.HandTip;
        handImage = HudTheme.AddImage(handRect, GuideArt.Hand, new Color(1f, 1f, 1f, 0f));

        SetAlpha(0f);
    }

    public void SetAccent(Color color) => accent = color;

    public bool IsVisible => visible;

    /// <summary>Shows a gesture on a target (and optional destination) rect in root-local units.</summary>
    public void Show(Gesture newGesture, Rect target, Rect destination)
    {
        if (newGesture == Gesture.None)
        {
            Hide();
            return;
        }

        bool sameGesture = visible && newGesture == gesture &&
                           (target.center - from.center).sqrMagnitude < 4f &&
                           (destination.center - to.center).sqrMagnitude < 4f;
        from = target;
        to = destination;
        if (sameGesture) return;

        gesture = newGesture;
        if (!visible || !hasPosition)
        {
            // Enter from just below-right of the first point.
            position = GesturePoint(0f, out _, out _) + new Vector2(140f, -160f);
            hasPosition = true;
        }
        travelStart = position;
        travelAge = 0f;
        clock = 0f;
        lastCycleT = 0f;
        visible = true;
    }

    /// <summary>Keeps the gesture but follows a moving target (no restart).</summary>
    public void Track(Rect target, Rect destination)
    {
        from = target;
        to = destination;
    }

    public void Hide() => visible = false;

    private void Update()
    {
        float dt = Time.unscaledDeltaTime;
        alpha = Mathf.MoveTowards(alpha, visible ? 1f : 0f, dt * (visible ? 5f : 7f));
        if (alpha <= 0f && !visible)
        {
            SetAlpha(0f);
            TickRipples(dt);
            return;
        }

        clock += dt;
        travelAge += dt;
        Vector2 goal = GesturePoint(clock, out float press, out float gestureAlpha);
        if (travelAge < TravelSeconds)
        {
            float k = HudTheme.EaseOutCubic(travelAge / TravelSeconds);
            // Glide along a gentle arc to the new place.
            Vector2 mid = (travelStart + goal) * 0.5f + new Vector2(0f, 60f);
            Vector2 a = Vector2.Lerp(travelStart, mid, k);
            Vector2 b = Vector2.Lerp(mid, goal, k);
            position = Vector2.Lerp(a, b, k);
            press = 0f;
            gestureAlpha = 1f;
        }
        else
        {
            position = goal;
        }

        float scale = 1f - 0.14f * press;
        handRect.anchoredPosition = position;
        handRect.localScale = new Vector3(scale, scale, 1f);
        handRect.localRotation = Quaternion.Euler(0f, 0f, Tilt + 6f * press);
        shadowRect.anchoredPosition = position + new Vector2(10f, -12f) * (1f - 0.6f * press);
        shadowRect.localScale = handRect.localScale;
        shadowRect.localRotation = handRect.localRotation;

        bool carrying = (gesture == Gesture.Drag || gesture == Gesture.Swipe || gesture == Gesture.Joystick) &&
                        press > 0.5f && travelAge >= TravelSeconds;
        carryRect.anchoredPosition = position;
        carryImage.color = new Color(accent.r, accent.g, accent.b, carrying ? 0.55f * alpha * gestureAlpha : 0f);

        SetAlpha(alpha * gestureAlpha);
        TickRipples(dt);
    }

    /// <summary>
    /// Where the fingertip should be at time t for the current gesture, how
    /// pressed it is (0..1) and the gesture's own fade. Spawns ripples on presses.
    /// </summary>
    private Vector2 GesturePoint(float t, out float press, out float fade)
    {
        press = 0f;
        fade = 1f;
        Vector2 a = from.center;
        float prev = lastCycleT;
        Vector2 point = GestureSwitch(t, prev, a, ref press, ref fade);
        return point;
    }

    private Vector2 GestureSwitch(float t, float prev, Vector2 a, ref float press, ref float fade)
    {
        switch (gesture)
        {
            case Gesture.Tap:
            {
                const float cycle = 1.3f;
                float c = Mathf.Repeat(t, cycle);
                press = Bump(c, 0.55f, 0.68f, 0.86f);
                CycleEvent(prev, c, 0.66f, a);
                Vector2 hover = new Vector2(16f, -22f) * (1f - press) + new Vector2(0f, Mathf.Sin(t * 3.2f) * 4f);
                return a + hover;
            }
            case Gesture.Hold:
            {
                const float cycle = 2.2f;
                float c = Mathf.Repeat(t, cycle);
                press = Bump(c, 0.3f, 0.45f, 1.6f, 1.8f);
                CycleEvent(prev, c, 0.44f, a);
                CycleEvent(prev, c, 1.0f, a);
                return a + new Vector2(16f, -22f) * (1f - press);
            }
            case Gesture.Drag:
            case Gesture.Swipe:
            {
                Vector2 start = a, end = to.center;
                float cycle = 2.5f, move0 = 0.45f, move1 = 1.55f;
                if (gesture == Gesture.Swipe)
                {
                    float reach = Mathf.Max(80f, from.width * 0.28f);
                    start = a - new Vector2(reach, 0f);
                    end = a + new Vector2(reach, 0f);
                    cycle = 1.9f;
                    move1 = 1.15f;
                }
                float c = Mathf.Repeat(t, cycle);
                fade = Mathf.Clamp01(c / 0.2f) * (1f - Mathf.Clamp01((c - (move1 + 0.3f)) / 0.25f));
                press = Bump(c, 0.22f, 0.36f, move1, move1 + 0.15f);
                CycleEvent(prev, c, 0.35f, start);
                CycleEvent(prev, c, move1 + 0.02f, end);
                float k = SmoothStep(Mathf.InverseLerp(move0, move1, c));
                // Arc away from the straight line so the motion reads as a drag.
                Vector2 dir = end - start;
                Vector2 normal = dir.sqrMagnitude > 1f ? new Vector2(-dir.y, dir.x).normalized : Vector2.up;
                if (normal.y < 0f) normal = -normal;
                float arc = gesture == Gesture.Drag ? Mathf.Min(90f, dir.magnitude * 0.15f) : 0f;
                Vector2 p = Vector2.Lerp(start, end, k) + normal * Mathf.Sin(k * Mathf.PI) * arc;
                return p + new Vector2(14f, -18f) * (1f - press);
            }
            case Gesture.Joystick:
            {
                const float cycle = 3f;
                float c = Mathf.Repeat(t, cycle);
                float radius = Mathf.Clamp(Mathf.Min(from.width, from.height) * 0.32f, 40f, 110f);
                press = Bump(c, 0.25f, 0.4f, 2.4f, 2.6f);
                CycleEvent(prev, c, 0.38f, a);
                // Push up, then sweep around, then let go.
                float push = Mathf.Clamp01((c - 0.45f) / 0.35f) * (1f - Mathf.Clamp01((c - 2.25f) / 0.3f));
                float angle = Mathf.PI * 0.5f + Mathf.Clamp01((c - 0.9f) / 1.3f) * Mathf.PI * 1.5f;
                Vector2 offset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius * SmoothStep(push);
                return a + offset + new Vector2(14f, -18f) * (1f - press);
            }
        }
        return a;
    }

    /// <summary>0 before a, rises to 1 by b, holds until c, falls to 0 by d (d defaults to c + 0.16).</summary>
    private static float Bump(float t, float a, float b, float c, float d = -1f)
    {
        if (d < 0f) d = c + 0.16f;
        if (t <= a || t >= d) return 0f;
        if (t < b) return SmoothStep((t - a) / (b - a));
        if (t <= c) return 1f;
        return 1f - SmoothStep((t - c) / (d - c));
    }

    private static float SmoothStep(float x)
    {
        x = Mathf.Clamp01(x);
        return x * x * (3f - 2f * x);
    }

    // Spawns a ripple the frame the cycle passes `at` (and the hand has arrived).
    private void CycleEvent(float prev, float cycleT, float at, Vector2 where)
    {
        lastCycleT = cycleT;
        if (travelAge < TravelSeconds) return;
        if (prev < at && cycleT >= at) SpawnRipple(where);
    }

    private void SpawnRipple(Vector2 where)
    {
        int i = nextRipple;
        nextRipple = (nextRipple + 1) % ripples.Length;
        rippleAge[i] = 0f;
        ripples[i].anchoredPosition = where;
        ripples[i].gameObject.SetActive(true);
    }

    private void TickRipples(float dt)
    {
        for (int i = 0; i < ripples.Length; i++)
        {
            if (rippleAge[i] > 0.7f)
            {
                if (ripples[i].gameObject.activeSelf) ripples[i].gameObject.SetActive(false);
                continue;
            }
            rippleAge[i] += dt;
            float k = Mathf.Clamp01(rippleAge[i] / 0.7f);
            float s = Mathf.Lerp(0.25f, 1.5f, HudTheme.EaseOutCubic(k));
            ripples[i].localScale = new Vector3(s, s, 1f);
            rippleImages[i].color = new Color(accent.r, accent.g, accent.b, (1f - k) * 0.9f * alpha);
        }
    }

    private void SetAlpha(float a)
    {
        handImage.color = new Color(1f, 1f, 1f, a);
        shadowImage.color = new Color(0f, 0.02f, 0.06f, 0.35f * a);
        bool on = a > 0.001f;
        if (handRect.gameObject.activeSelf != on)
        {
            handRect.gameObject.SetActive(on);
            shadowRect.gameObject.SetActive(on);
            carryRect.gameObject.SetActive(on);
        }
    }
}
