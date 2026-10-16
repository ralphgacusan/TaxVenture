using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// PURPOSE:
/// The HUD button icons, drawn in code so they match the HUD theme: one
/// navy tile with a bevelled brass rim and a soft top gloss, and a distinct,
/// clear picture on each:
///   settings  - a cream gear
///   notes     - a ruled notepad with a red binding and a yellow pencil
///   folder    - a manila case folder with a paper and a red case label
///   profiles  - two stacked blue taxpayer ID cards
/// Render() is pure (no Unity objects) for tests and previews; Get() caches
/// the sprite.
/// </summary>
public static class HudIconArt
{
    public const int Size = 160;
    private const int Samples = 3;

    public static readonly string[] Icons = { "settings", "notes", "folder", "profiles" };
    private static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();

    public static Sprite Get(string icon)
    {
        if (Cache.TryGetValue(icon, out Sprite cached) && cached != null) return cached;
        Color32[] px = Render(Size, icon);
        var tex = new Texture2D(Size, Size, TextureFormat.RGBA32, false)
        {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear,
            name = "HudIcon " + icon
        };
        tex.SetPixels32(px);
        tex.Apply(false, true);
        Sprite sprite = Sprite.Create(tex, new Rect(0, 0, Size, Size), new Vector2(0.5f, 0.5f), 100f);
        sprite.name = "HudIcon " + icon;
        Cache[icon] = sprite;
        return sprite;
    }

    // =========================================================
    // RENDER (pure)
    // =========================================================

    /// <summary>Straight-alpha RGBA pixels, row 0 at the bottom.</summary>
    public static Color32[] Render(int size, string icon)
    {
        var pixels = new Color32[size * size];
        float inv = 1f / (size * Samples);
        for (int py = 0; py < size; py++)
        {
            for (int px = 0; px < size; px++)
            {
                float r = 0, g = 0, b = 0, a = 0;
                for (int sy = 0; sy < Samples; sy++)
                {
                    for (int sx = 0; sx < Samples; sx++)
                    {
                        Color c = Shade((px * Samples + sx + 0.5f) * inv, (py * Samples + sy + 0.5f) * inv, icon);
                        r += c.r * c.a; g += c.g * c.a; b += c.b * c.a; a += c.a;
                    }
                }
                int n = Samples * Samples;
                pixels[py * size + px] = a <= 0f
                    ? new Color32(0, 0, 0, 0)
                    : new Color32(B(r / a), B(g / a), B(b / a), B(a / n));
            }
        }
        return pixels;
    }

    private static byte B(float f) => (byte)Mathf.Clamp(Mathf.RoundToInt(f * 255f), 0, 255);

    private static Color Shade(float u, float v, string icon)
    {
        Color c = new Color(0, 0, 0, 0);

        // Soft drop shadow under the tile.
        if (RoundRect(u, v - (-0.03f), 0.08f, 0.06f, 0.92f, 0.92f, 0.22f, 0.04f, out float sh))
            Over(ref c, new Color(0f, 0f, 0f, 0.38f * sh));

        // Brass rim (bevel: lighter on top) and navy face with a top gloss.
        if (RoundRect(u, v, 0.06f, 0.08f, 0.94f, 0.94f, 0.22f, 0f, out _))
        {
            Over(ref c, Color.Lerp(C(150, 116, 58), C(236, 206, 140), Mathf.Clamp01((v - 0.08f) / 0.86f)));
            if (RoundRect(u, v, 0.1f, 0.12f, 0.9f, 0.9f, 0.18f, 0f, out _))
            {
                float t = Mathf.Clamp01((v - 0.12f) / 0.78f);
                Over(ref c, Color.Lerp(C(14, 22, 34), C(38, 58, 84), t));
                if (v > 0.6f && RoundRect(u, v, 0.13f, 0.6f, 0.87f, 0.87f, 0.15f, 0f, out _))
                    Over(ref c, new Color(1f, 1f, 1f, 0.07f));
            }
        }

        switch (icon)
        {
            case "settings": Gear(u, v, ref c); break;
            case "notes": Notepad(u, v, ref c); break;
            case "folder": Folder(u, v, ref c); break;
            case "profiles": Cards(u, v, ref c); break;
        }
        return c;
    }

    // ---------------- glyphs ----------------

    private static void Gear(float u, float v, ref Color c)
    {
        float x = u - 0.5f, y = v - 0.5f;
        float r = Mathf.Sqrt(x * x + y * y);
        float a = Mathf.Atan2(y, x);
        float tooth = Mathf.Repeat(a / (Mathf.PI * 2f / 8f), 1f);
        bool inTooth = tooth > 0.28f && tooth < 0.72f;
        float outer = inTooth ? 0.3f : 0.235f;
        // Shadow, then body.
        float sx = x + 0.012f, sy = y + 0.018f;
        float sr = Mathf.Sqrt(sx * sx + sy * sy);
        if (sr <= outer && sr >= 0.085f) Over(ref c, new Color(0f, 0f, 0f, 0.35f));
        if (r <= outer && r >= 0.085f)
        {
            Color top = C(250, 246, 234), bottom = C(196, 202, 214);
            Over(ref c, Color.Lerp(bottom, top, Mathf.Clamp01(v * 1.4f - 0.2f)));
            if (r <= 0.16f && r >= 0.13f) Over(ref c, new Color(0.55f, 0.6f, 0.68f, 0.6f));   // inner ring
        }
    }

    private static void Notepad(float u, float v, ref Color c)
    {
        // Shadow + page.
        if (RoundRect(u + 0.012f, v + 0.018f, 0.26f, 0.2f, 0.68f, 0.78f, 0.04f, 0f, out _)) Over(ref c, new Color(0f, 0f, 0f, 0.35f));
        if (RoundRect(u, v, 0.26f, 0.2f, 0.68f, 0.78f, 0.04f, 0f, out _))
        {
            Over(ref c, Color.Lerp(C(232, 224, 200), C(252, 248, 236), (v - 0.2f) / 0.58f));
            for (int i = 0; i < 4; i++)
            {
                float ly = 0.6f - i * 0.1f;
                if (Mathf.Abs(v - ly) < 0.011f && u > 0.32f && u < (i == 3 ? 0.5f : 0.62f)) Over(ref c, C(120, 150, 190));
            }
            if (Mathf.Abs(u - 0.31f) < 0.006f && v < 0.7f) Over(ref c, new Color(0.88f, 0.4f, 0.38f, 0.8f));   // margin
        }
        // Binding with rings.
        if (RoundRect(u, v, 0.25f, 0.7f, 0.69f, 0.8f, 0.03f, 0f, out _)) Over(ref c, C(206, 84, 72));
        for (int i = 0; i < 4; i++)
        {
            float rx = 0.32f + i * 0.1f, ry = 0.8f;
            float d = Mathf.Sqrt((u - rx) * (u - rx) + (v - ry) * (v - ry));
            if (d < 0.03f && d > 0.014f) Over(ref c, C(70, 76, 88));
        }
        // Pencil, diagonal over the page corner.
        float px = u - 0.6f, py = v - 0.38f;
        float along = (px + py) * 0.7071f, across = (py - px) * 0.7071f;
        if (along > -0.2f && along < 0.2f && Mathf.Abs(across) < 0.045f)
        {
            if (along > 0.14f) Over(ref c, C(226, 120, 140));                                   // eraser
            else if (along > 0.11f) Over(ref c, C(190, 190, 200));                              // ferrule
            else Over(ref c, Color.Lerp(C(232, 170, 40), C(255, 210, 80), across > 0 ? 1f : 0.5f)); // body
        }
        if (along <= -0.2f && along > -0.28f && Mathf.Abs(across) < 0.045f * (along + 0.28f) / 0.08f)
            Over(ref c, along > -0.25f ? C(236, 206, 160) : C(60, 60, 70));                      // tip
    }

    private static void Folder(float u, float v, ref Color c)
    {
        if (RoundRect(u + 0.012f, v + 0.018f, 0.2f, 0.24f, 0.8f, 0.72f, 0.04f, 0f, out _)) Over(ref c, new Color(0f, 0f, 0f, 0.35f));
        // Back with tab.
        if (RoundRect(u, v, 0.2f, 0.26f, 0.8f, 0.7f, 0.04f, 0f, out _) || RoundRect(u, v, 0.2f, 0.62f, 0.45f, 0.76f, 0.04f, 0f, out _))
            Over(ref c, C(184, 138, 72));
        // Paper peeking out.
        if (RoundRect(u, v, 0.27f, 0.36f, 0.72f, 0.7f, 0.02f, 0f, out _))
        {
            Over(ref c, C(250, 248, 240));
            if ((Mathf.Abs(v - 0.62f) < 0.01f || Mathf.Abs(v - 0.56f) < 0.01f) && u > 0.33f && u < 0.64f) Over(ref c, C(150, 160, 175));
        }
        // Front flap, slightly lower and lighter.
        if (RoundRect(u, v, 0.18f, 0.24f, 0.82f, 0.56f, 0.05f, 0f, out _))
        {
            Over(ref c, Color.Lerp(C(214, 166, 92), C(242, 200, 128), (v - 0.24f) / 0.32f));
            if (RoundRect(u, v, 0.4f, 0.33f, 0.6f, 0.43f, 0.02f, 0f, out _)) Over(ref c, C(206, 84, 72));  // case label
            if (v > 0.53f) Over(ref c, new Color(1f, 1f, 1f, 0.18f));                                       // edge highlight
        }
    }

    private static void Cards(float u, float v, ref Color c)
    {
        // Back card, tilted.
        float bx = u - 0.54f, by = v - 0.57f;
        float ca = Mathf.Cos(0.21f), sa = Mathf.Sin(0.21f);
        float rx = bx * ca + by * sa, ry = -bx * sa + by * ca;
        if (RoundRect(rx, ry, -0.24f, -0.16f, 0.24f, 0.16f, 0.05f, 0f, out _)) Over(ref c, C(120, 160, 205));
        // Front card shadow + card.
        if (RoundRect(u + 0.012f, v + 0.018f, 0.18f, 0.24f, 0.74f, 0.6f, 0.05f, 0f, out _)) Over(ref c, new Color(0f, 0f, 0f, 0.35f));
        if (RoundRect(u, v, 0.18f, 0.24f, 0.74f, 0.6f, 0.05f, 0f, out _))
        {
            Over(ref c, Color.Lerp(C(150, 200, 242), C(196, 228, 255), (v - 0.24f) / 0.36f));
            if (v > 0.54f) Over(ref c, C(70, 130, 200));                                                     // header band
            float hx = u - 0.32f, hy = v - 0.43f;
            if (hx * hx + hy * hy < 0.055f * 0.055f) Over(ref c, C(30, 52, 84));                             // head
            float ex = (u - 0.32f) / 0.085f, ey = (v - 0.3f) / 0.07f;
            if (v < 0.35f && v > 0.27f && ex * ex + ey * ey < 1f) Over(ref c, C(30, 52, 84));                // shoulders
            for (int i = 0; i < 3; i++)
            {
                float ly = 0.47f - i * 0.07f;
                if (Mathf.Abs(v - ly) < 0.012f && u > 0.44f && u < (i == 2 ? 0.58f : 0.68f)) Over(ref c, C(60, 96, 140));
            }
        }
    }

    // ---------------- helpers ----------------

    private static Color C(int r, int g, int b) => new Color(r / 255f, g / 255f, b / 255f, 1f);

    private static void Over(ref Color dst, Color src)
    {
        float a = src.a + dst.a * (1f - src.a);
        if (a <= 0f) { dst = new Color(0, 0, 0, 0); return; }
        dst = new Color(
            (src.r * src.a + dst.r * dst.a * (1f - src.a)) / a,
            (src.g * src.a + dst.g * dst.a * (1f - src.a)) / a,
            (src.b * src.a + dst.b * dst.a * (1f - src.a)) / a,
            a);
    }

    /// <summary>Rounded rect test; feather &gt; 0 returns a soft edge in `alpha`.</summary>
    private static bool RoundRect(float x, float y, float x0, float y0, float x1, float y1, float r, float feather, out float alpha)
    {
        float cx = Mathf.Clamp(x, x0 + r, x1 - r), cy = Mathf.Clamp(y, y0 + r, y1 - r);
        float dx = x - cx, dy = y - cy;
        float d = Mathf.Sqrt(dx * dx + dy * dy) - r;
        bool inside = x >= x0 - feather && x <= x1 + feather && y >= y0 - feather && y <= y1 + feather;
        if (feather <= 0f)
        {
            alpha = 1f;
            return inside && d <= 0f;
        }
        alpha = Mathf.Clamp01(1f - d / feather);
        return inside && d <= feather;
    }
}
