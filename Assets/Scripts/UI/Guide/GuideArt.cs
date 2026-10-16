using UnityEngine;

/// <summary>
/// PURPOSE:
/// Procedural art for the guide (same idea as HudTheme's sprites: no
/// imported images, built once and cached): the cartoon pointing glove,
/// ripple ring, spotlight frame, soft glow, goal pin, edge arrow and the
/// flat ground-arrow mesh used by the objective trail.
/// </summary>
public static class GuideArt
{
    private static Sprite hand;
    private static Sprite ring;
    private static Sprite frame;
    private static Sprite glow;
    private static Sprite pin;
    private static Sprite arrow;
    private static Sprite dot;
    private static Sprite chevron;

    /// <summary>The glove's fingertip, in sprite fractions (used as its pivot).</summary>
    public static readonly Vector2 HandTip = new Vector2(0.40f, 0.965f);

    private static readonly Color32 Outline = new Color32(22, 34, 52, 255);
    private static readonly Color32 GloveLight = new Color32(255, 255, 255, 255);
    private static readonly Color32 GloveShade = new Color32(214, 226, 240, 255);
    private static readonly Color32 CuffColor = new Color32(196, 214, 234, 255);

    public static Sprite Hand
    {
        get
        {
            if (hand == null)
            {
                Texture2D tex = Paint(256, HandPixel, "GuideHand");
                hand = Sprite.Create(tex, new Rect(0, 0, 256, 256), HandTip, 100f);
                hand.name = "GuideHand";
            }
            return hand;
        }
    }

    public static Sprite Ring
    {
        get
        {
            if (ring == null) ring = Make(Paint(128, RingPixel, "GuideRing"), "GuideRing");
            return ring;
        }
    }

    /// <summary>9-sliced rounded outline (spotlight frame); border 24 px.</summary>
    public static Sprite Frame
    {
        get
        {
            if (frame == null)
            {
                Texture2D tex = Paint(64, FramePixel, "GuideFrame");
                frame = Sprite.Create(tex, new Rect(0, 0, 64, 64), new Vector2(0.5f, 0.5f), 100f, 0,
                    SpriteMeshType.FullRect, new Vector4(24, 24, 24, 24));
                frame.name = "GuideFrame";
            }
            return frame;
        }
    }

    public static Sprite Glow
    {
        get
        {
            if (glow == null) glow = Make(Paint(128, GlowPixel, "GuideGlow"), "GuideGlow");
            return glow;
        }
    }

    /// <summary>Map-pin goal marker (white; tint it).</summary>
    public static Sprite Pin
    {
        get
        {
            if (pin == null)
            {
                Texture2D tex = Paint(128, PinPixel, "GuidePin");
                pin = Sprite.Create(tex, new Rect(0, 0, 128, 128), new Vector2(0.5f, 0.04f), 100f);
                pin.name = "GuidePin";
            }
            return pin;
        }
    }

    /// <summary>Rounded triangle pointing up (edge arrow, "next" chevron).</summary>
    public static Sprite Arrow
    {
        get
        {
            if (arrow == null) arrow = Make(Paint(64, ArrowPixel, "GuideArrow"), "GuideArrow");
            return arrow;
        }
    }

    public static Sprite Dot
    {
        get
        {
            if (dot == null) dot = Make(Paint(32, DotPixel, "GuideDot"), "GuideDot");
            return dot;
        }
    }

    /// <summary>Chevron "^" pointing up, for UI use.</summary>
    public static Sprite Chevron
    {
        get
        {
            if (chevron == null) chevron = Make(Paint(64, ChevronPixel, "GuideChevron"), "GuideChevron");
            return chevron;
        }
    }

    private static Sprite Make(Texture2D tex, string name)
    {
        Sprite s = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
        s.name = name;
        return s;
    }

    private delegate Color PixelFn(float u, float v, float px);

    /// <summary>2x2 supersampled painter; u,v in 0..1 (v up), px = one pixel in uv units.</summary>
    private static Texture2D Paint(int size, PixelFn fn, string name)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear,
            name = name
        };
        var pixels = new Color32[size * size];
        float px = 1f / size;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Color acc = Color.clear;
                for (int sy = 0; sy < 2; sy++)
                for (int sx = 0; sx < 2; sx++)
                {
                    Color c = fn((x + 0.25f + sx * 0.5f) * px, (y + 0.25f + sy * 0.5f) * px, px);
                    // Premultiply while averaging so edges do not darken.
                    acc.r += c.r * c.a;
                    acc.g += c.g * c.a;
                    acc.b += c.b * c.a;
                    acc.a += c.a;
                }
                if (acc.a > 0f)
                {
                    acc.r /= acc.a;
                    acc.g /= acc.a;
                    acc.b /= acc.a;
                }
                acc.a *= 0.25f;
                pixels[y * size + x] = acc;
            }
        }
        tex.SetPixels32(pixels);
        tex.Apply(false, true);
        return tex;
    }

    // =========================================================
    // SIGNED DISTANCE HELPERS
    // =========================================================

    private static float Capsule(float u, float v, float ax, float ay, float bx, float by, float r)
    {
        float pax = u - ax, pay = v - ay, bax = bx - ax, bay = by - ay;
        float h = Mathf.Clamp01((pax * bax + pay * bay) / (bax * bax + bay * bay));
        float dx = pax - bax * h, dy = pay - bay * h;
        return Mathf.Sqrt(dx * dx + dy * dy) - r;
    }

    private static float Box(float u, float v, float cx, float cy, float hx, float hy, float r)
    {
        float qx = Mathf.Abs(u - cx) - hx + r;
        float qy = Mathf.Abs(v - cy) - hy + r;
        float outside = new Vector2(Mathf.Max(qx, 0f), Mathf.Max(qy, 0f)).magnitude;
        return outside + Mathf.Min(Mathf.Max(qx, qy), 0f) - r;
    }

    private static float CircleSd(float u, float v, float cx, float cy, float r)
    {
        float dx = u - cx, dy = v - cy;
        return Mathf.Sqrt(dx * dx + dy * dy) - r;
    }

    private static float Smooth(float d, float px) => Mathf.Clamp01(0.5f - d / px);

    // =========================================================
    // SHAPES
    // =========================================================

    private static Color HandPixel(float u, float v, float px)
    {
        // Pointing glove: index finger up, curled fingers to the right, thumb
        // to the left and a puffy cuff at the bottom (cartoon style).
        float finger = Capsule(u, v, 0.40f, 0.52f, 0.40f, 0.885f, 0.082f);
        float palm = Box(u, v, 0.51f, 0.40f, 0.175f, 0.15f, 0.085f);
        float knuckle1 = CircleSd(u, v, 0.615f, 0.505f, 0.072f);
        float knuckle2 = CircleSd(u, v, 0.655f, 0.415f, 0.07f);
        float knuckle3 = CircleSd(u, v, 0.64f, 0.325f, 0.066f);
        float thumb = Capsule(u, v, 0.37f, 0.36f, 0.255f, 0.475f, 0.062f);
        float cuff = Box(u, v, 0.53f, 0.175f, 0.16f, 0.075f, 0.055f);

        float glove = Mathf.Min(Mathf.Min(finger, palm), Mathf.Min(Mathf.Min(knuckle1, knuckle2), Mathf.Min(knuckle3, thumb)));
        float all = Mathf.Min(glove, cuff);

        const float outline = 0.022f;
        float coverage = Smooth(all - outline, px);
        if (coverage <= 0f) return Color.clear;

        Color fill;
        if (cuff < 0f && cuff < glove + 0.02f)
            fill = Color.Lerp(CuffColor, GloveShade, Mathf.Clamp01((0.175f - v) * 6f));
        else
        {
            // Light from the upper left.
            float shade = Mathf.Clamp01((u - 0.42f) * 2.2f + (0.45f - v) * 0.8f);
            fill = Color.Lerp(GloveLight, GloveShade, shade);
        }

        // Dark outline (outside the shape), plus short crease lines between
        // the curled fingers and along the top of the cuff.
        float inside = Smooth(all, px * 1.5f);
        float creaseInk = 0f;
        if (all < -0.006f)
        {
            if (u > 0.53f && Mathf.Min(knuckle1, knuckle2) < 0f && Mathf.Abs(knuckle1 - knuckle2) < 0.03f)
                creaseInk = Mathf.Max(creaseInk, Smooth(Mathf.Abs(knuckle1 - knuckle2) - 0.008f, px));
            if (u > 0.53f && Mathf.Min(knuckle2, knuckle3) < 0f && Mathf.Abs(knuckle2 - knuckle3) < 0.03f)
                creaseInk = Mathf.Max(creaseInk, Smooth(Mathf.Abs(knuckle2 - knuckle3) - 0.008f, px));
            if (v > 0.2f && Mathf.Abs(cuff) < 0.03f)
                creaseInk = Mathf.Max(creaseInk, Smooth(Mathf.Abs(cuff) - 0.006f, px));
        }
        float ink = Mathf.Max(1f - inside, creaseInk * 0.85f);
        Color c = Color.Lerp(fill, Outline, ink);
        c.a = coverage;
        return c;
    }

    private static Color RingPixel(float u, float v, float px)
    {
        float d = Mathf.Abs(CircleSd(u, v, 0.5f, 0.5f, 0.42f)) - 0.05f;
        float a = Smooth(d, px);
        return new Color(1f, 1f, 1f, a);
    }

    private static Color FramePixel(float u, float v, float px)
    {
        // Rounded-rect outline: radius 20 px, stroke 6 px, on a 64 px sprite.
        float r = 20f / 64f, stroke = 6f / 64f;
        float d = Box(u, v, 0.5f, 0.5f, 0.5f - stroke * 0.5f - px, 0.5f - stroke * 0.5f - px, r);
        float a = Smooth(Mathf.Abs(d) - stroke * 0.5f, px);
        return new Color(1f, 1f, 1f, a);
    }

    private static Color GlowPixel(float u, float v, float px)
    {
        float dx = u - 0.5f, dy = v - 0.5f;
        float d = Mathf.Sqrt(dx * dx + dy * dy) * 2f;
        float a = Mathf.Clamp01(1f - d);
        return new Color(1f, 1f, 1f, a * a);
    }

    private static Color PinPixel(float u, float v, float px)
    {
        // Teardrop pin: a circle on top of a point at the bottom.
        float head = CircleSd(u, v, 0.5f, 0.64f, 0.3f);
        float tipX = Mathf.Abs(u - 0.5f);
        // Triangle from the circle's sides down to the tip at v=0.05.
        float t = Mathf.InverseLerp(0.05f, 0.62f, v);
        float halfWidth = Mathf.Lerp(0f, 0.27f, t);
        float tri = v < 0.05f || v > 0.62f ? 1f : tipX - halfWidth;
        float body = Mathf.Min(head, tri);
        float a = Smooth(body, px);
        if (a <= 0f) return Color.clear;
        float hole = CircleSd(u, v, 0.5f, 0.64f, 0.12f);
        float rim = Smooth(-body - 0.035f, px);
        // White body, darker rim, dark centre dot.
        Color c = Color.Lerp(new Color(0.55f, 0.6f, 0.68f), Color.white, rim);
        c = Color.Lerp(c, new Color(0.09f, 0.14f, 0.2f), Smooth(hole, px));
        c.a = a;
        return c;
    }

    private static Color ArrowPixel(float u, float v, float px)
    {
        // Rounded triangle pointing up.
        float d = Mathf.Max(Mathf.Max(
            (u - 0.5f) * 0.894f + (v - 0.86f) * 0.447f,
            -(u - 0.5f) * 0.894f + (v - 0.86f) * 0.447f), 0.16f - v);
        float a = Smooth(d - 0.02f, px * 1.5f);
        return new Color(1f, 1f, 1f, a);
    }

    private static Color DotPixel(float u, float v, float px)
    {
        return new Color(1f, 1f, 1f, Smooth(CircleSd(u, v, 0.5f, 0.5f, 0.42f), px));
    }

    private static Color ChevronPixel(float u, float v, float px)
    {
        float a1 = Capsule(u, v, 0.2f, 0.32f, 0.5f, 0.68f, 0.09f);
        float a2 = Capsule(u, v, 0.8f, 0.32f, 0.5f, 0.68f, 0.09f);
        return new Color(1f, 1f, 1f, Smooth(Mathf.Min(a1, a2), px));
    }

    // =========================================================
    // GROUND ARROW MESH SHAPE
    // =========================================================

    /// <summary>
    /// Writes one flat chevron (pointing +forward) into the vertex lists:
    /// a soft glow copy underneath and the solid arrow on top.
    /// </summary>
    public static void AddGroundChevron(System.Collections.Generic.List<Vector3> verts,
        System.Collections.Generic.List<Color32> colors, System.Collections.Generic.List<int> tris,
        Vector3 center, Vector3 forward, float size, Color color, float alpha)
    {
        Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
        AddChevronShape(verts, colors, tris, center, forward, right, size * 1.45f, 0.42f,
            new Color(color.r, color.g, color.b, alpha * 0.32f));
        AddChevronShape(verts, colors, tris, center, forward, right, size, 0.3f,
            new Color(Mathf.Lerp(color.r, 1f, 0.35f), Mathf.Lerp(color.g, 1f, 0.35f), Mathf.Lerp(color.b, 1f, 0.35f), alpha));
    }

    private static void AddChevronShape(System.Collections.Generic.List<Vector3> verts,
        System.Collections.Generic.List<Color32> colors, System.Collections.Generic.List<int> tris,
        Vector3 c, Vector3 f, Vector3 r, float size, float thickness, Color color)
    {
        float half = size * 0.5f;
        float back = size * 0.42f;
        float t = size * thickness;
        // Outer edge: left-back, tip, right-back. Inner edge: the same shifted back.
        Vector3 tip = c + f * (back * 0.5f);
        Vector3 left = tip - f * back - r * half;
        Vector3 rightP = tip - f * back + r * half;
        Vector3 tipIn = tip - f * t;
        Vector3 leftIn = left - f * t;
        Vector3 rightIn = rightP - f * t;

        int i = verts.Count;
        verts.Add(left); verts.Add(tip); verts.Add(tipIn); verts.Add(leftIn);
        verts.Add(tip); verts.Add(rightP); verts.Add(rightIn); verts.Add(tipIn);
        Color32 c32 = color;
        for (int k = 0; k < 8; k++) colors.Add(c32);
        tris.Add(i); tris.Add(i + 1); tris.Add(i + 2);
        tris.Add(i); tris.Add(i + 2); tris.Add(i + 3);
        tris.Add(i + 4); tris.Add(i + 5); tris.Add(i + 6);
        tris.Add(i + 4); tris.Add(i + 6); tris.Add(i + 7);
    }
}
