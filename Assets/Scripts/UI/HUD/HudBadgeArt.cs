using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// PURPOSE:
/// Draws the collectible side-quest medals in code: a bevelled hexagon in the
/// tier's metal (bronze / silver / gold, or a dark silhouette when not yet
/// earned), a two-tailed ribbon in the side quest's own colour, and the side
/// quest's own emblem in the centre ("taxpayer_id": an ID card under a
/// magnifying glass for Know Your Taxpayer; "receipt"; "star").
///
/// Each side quest picks its emblem and ribbon in rewards.json
/// (badgeEmblem / badgeRibbon). Sprites are generated once per
/// emblem + tier + ribbon and cached. Render() is pure (no Unity objects) so
/// it can be tested and previewed outside the engine.
/// </summary>
public static class HudBadgeArt
{
    public const int Size = 192;
    private const int Samples = 3;

    // Layout in unit space (x right, y up, 0..1).
    private const float HexX = 0.5f;
    private const float HexY = 0.61f;
    private const float HexOuter = 0.34f;   // inradius of the outer rim
    private const float HexInner = 0.255f;  // inradius of the face
    private const float Outline = 0.022f;

    private static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();

    private struct Palette
    {
        public Color RimLight, RimMid, RimDark, FaceTop, FaceBottom, Emblem, EmblemShade;
    }

    // =========================================================
    // SPRITES
    // =========================================================

    /// <summary>The medal sprite (cached). tier None draws the "not yet earned" silhouette.</summary>
    public static Sprite Get(string emblem, BadgeTier tier, string ribbonHex)
    {
        string key = (emblem ?? "star") + "|" + tier + "|" + ribbonHex;
        if (Cache.TryGetValue(key, out Sprite cached) && cached != null) return cached;

        Color ribbon = ParseColor(ribbonHex, new Color32(47, 143, 216, 255));
        Color32[] pixels = Render(Size, emblem, tier, ribbon);
        var tex = new Texture2D(Size, Size, TextureFormat.RGBA32, false)
        {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear,
            name = "Badge " + key
        };
        tex.SetPixels32(pixels);
        tex.Apply(false, true);
        Sprite sprite = Sprite.Create(tex, new Rect(0, 0, Size, Size), new Vector2(0.5f, 0.5f), 100f);
        sprite.name = "Badge " + key;
        Cache[key] = sprite;
        return sprite;
    }

    /// <summary>The medal for the current level's side-quest rules.</summary>
    public static Sprite For(LevelRewardRules rules, BadgeTier tier)
    {
        LevelRewardRules.SideQuestRules sq = rules != null ? rules.sideQuest : new LevelRewardRules.SideQuestRules();
        return Get(sq.badgeEmblem, tier, sq.badgeRibbon);
    }

    public static Color ParseColor(string hex, Color fallback) =>
        !string.IsNullOrEmpty(hex) && ColorUtility.TryParseHtmlString(hex, out Color c) ? c : fallback;

    /// <summary>Glow colour of a tier (reward screens).</summary>
    public static Color TierColor(BadgeTier tier)
    {
        switch (tier)
        {
            case BadgeTier.Gold: return new Color32(255, 214, 102, 255);
            case BadgeTier.Silver: return new Color32(210, 222, 238, 255);
            case BadgeTier.Bronze: return new Color32(236, 150, 92, 255);
            default: return new Color32(120, 132, 148, 255);
        }
    }

    public static string TierLabel(BadgeTier tier) =>
        tier == BadgeTier.None ? "Not earned" : tier.ToString();

    // =========================================================
    // RENDER (pure)
    // =========================================================

    /// <summary>Straight-alpha RGBA pixels, row 0 at the bottom (Texture2D order).</summary>
    public static Color32[] Render(int size, string emblem, BadgeTier tier, Color ribbon)
    {
        Palette pal = PaletteOf(tier);
        bool locked = tier == BadgeTier.None;
        if (locked) ribbon = new Color(0.22f, 0.27f, 0.33f, 1f);
        System.Func<float, float, bool> shape = EmblemShape(emblem);

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
                        float u = (px * Samples + sx + 0.5f) * inv;
                        float v = (py * Samples + sy + 0.5f) * inv;
                        Color c = Shade(u, v, pal, ribbon, shape, locked);
                        r += c.r * c.a; g += c.g * c.a; b += c.b * c.a; a += c.a;
                    }
                }
                int n = Samples * Samples;
                pixels[py * size + px] = a <= 0f
                    ? new Color32(0, 0, 0, 0)
                    : new Color32(To8(r / a), To8(g / a), To8(b / a), To8(a / n));
            }
        }
        return pixels;
    }

    private static byte To8(float f) => (byte)Mathf.Clamp(Mathf.RoundToInt(f * 255f), 0, 255);

    private static Color Shade(float u, float v, Palette pal, Color ribbon, System.Func<float, float, bool> emblem, bool locked)
    {
        float x = u - HexX, y = v - HexY;
        float d = HexDistance(x, y, out Vector2 facet);
        Color outline = new Color(0.06f, 0.08f, 0.13f, 1f);

        if (d <= HexOuter + Outline)
        {
            if (d > HexOuter) return outline;

            // Light comes from the upper left.
            float light = Vector2.Dot(facet, new Vector2(-0.45f, 0.89f));
            if (d > HexInner + 0.018f)
            {
                // Outer bevel: facets facing the light are bright.
                float rim = (d - HexInner) / (HexOuter - HexInner);
                Color c = light >= 0f ? Color.Lerp(pal.RimMid, pal.RimLight, light) : Color.Lerp(pal.RimMid, pal.RimDark, -light);
                if (rim > 0.82f) c = Color.Lerp(c, pal.RimLight, 0.35f);            // bright outer lip
                return c;
            }
            if (d > HexInner)
            {
                // Inner bevel: lit the opposite way, so the face looks sunk in.
                return light >= 0f ? Color.Lerp(pal.RimMid, pal.RimDark, light * 0.8f) : Color.Lerp(pal.RimMid, pal.RimLight, -light * 0.6f);
            }

            // Face: vertical gradient, soft gloss on the upper half.
            float t = Mathf.Clamp01((y + HexInner) / (2f * HexInner));
            Color face = Color.Lerp(pal.FaceBottom, pal.FaceTop, t);
            if (y > 0.02f && y - x * 0.35f > 0.04f) face = Color.Lerp(face, Color.white, 0.07f);

            // Emblem in face space (-1..1), with a drop shadow.
            float ex = x / (HexInner * 0.84f), ey = y / (HexInner * 0.84f);
            if (emblem(ex, ey))
            {
                Color top = pal.Emblem;
                Color bottom = Color.Lerp(pal.Emblem, pal.EmblemShade, 0.55f);
                return Color.Lerp(bottom, top, Mathf.Clamp01(ey * 0.5f + 0.5f));
            }
            if (emblem(ex + 0.07f, ey - 0.09f)) return Color.Lerp(face, Color.black, locked ? 0.25f : 0.38f);

            // Small sparkle on gold.
            return face;
        }

        // Ribbon tails below the hexagon (behind it).
        Color tail;
        if (RibbonTail(u, v, 0.4f, -1f, out float edge) || RibbonTail(u, v, 0.6f, 1f, out edge))
        {
            bool left = u < 0.5f;
            tail = Color.Lerp(ribbon, Color.black, left ? 0.18f : 0.0f);
            tail = Color.Lerp(tail, Color.white, 0.12f * Mathf.Clamp01((v - 0.12f) / 0.3f));
            if (edge < 0.012f) return outline;
            return tail;
        }
        return new Color(0, 0, 0, 0);
    }

    /// <summary>Inradius distance for a pointy-top hexagon, and the outward normal of the nearest side.</summary>
    private static float HexDistance(float x, float y, out Vector2 facet)
    {
        // Side normals of a pointy-top hexagon: 0, 60, 120 degrees (and opposites).
        float best = float.MinValue;
        facet = Vector2.right;
        for (int i = 0; i < 3; i++)
        {
            float ang = i * Mathf.PI / 3f;
            var n = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang));
            float dot = x * n.x + y * n.y;
            if (Mathf.Abs(dot) > best)
            {
                best = Mathf.Abs(dot);
                facet = dot >= 0f ? n : -n;
            }
        }
        return best;
    }

    /// <summary>One ribbon tail: a slanted band with a V-notch at the bottom.</summary>
    private static bool RibbonTail(float u, float v, float centre, float side, out float edge)
    {
        edge = 0f;
        const float top = 0.46f, halfWidth = 0.112f;
        if (v > top) return false;
        float slant = (top - v) * 0.3f * side;          // tails spread outward
        float cx = centre + slant;
        float dx = Mathf.Abs(u - cx);
        if (dx > halfWidth) return false;
        float notch = 0.02f + 0.1f * (1f - dx / halfWidth); // V cut: middle higher
        if (v < notch) return false;
        edge = Mathf.Min(halfWidth - dx, v - notch);
        return true;
    }

    // =========================================================
    // PALETTES
    // =========================================================

    private static Palette PaletteOf(BadgeTier tier)
    {
        switch (tier)
        {
            case BadgeTier.Gold:
                return new Palette
                {
                    RimLight = C(255, 233, 140), RimMid = C(238, 184, 56), RimDark = C(168, 108, 22),
                    FaceTop = C(214, 136, 36), FaceBottom = C(150, 82, 18),
                    Emblem = C(255, 246, 214), EmblemShade = C(250, 200, 110)
                };
            case BadgeTier.Silver:
                return new Palette
                {
                    RimLight = C(248, 251, 255), RimMid = C(190, 200, 212), RimDark = C(112, 124, 140),
                    FaceTop = C(132, 146, 164), FaceBottom = C(80, 92, 110),
                    Emblem = C(250, 252, 255), EmblemShade = C(196, 208, 222)
                };
            case BadgeTier.Bronze:
                return new Palette
                {
                    RimLight = C(246, 176, 116), RimMid = C(204, 118, 64), RimDark = C(128, 64, 34),
                    FaceTop = C(156, 72, 42), FaceBottom = C(104, 42, 26),
                    Emblem = C(255, 226, 196), EmblemShade = C(236, 160, 110)
                };
            default:
                return new Palette
                {
                    RimLight = C(92, 104, 120), RimMid = C(66, 76, 90), RimDark = C(40, 48, 60),
                    FaceTop = C(44, 52, 64), FaceBottom = C(30, 36, 46),
                    Emblem = C(82, 94, 110), EmblemShade = C(64, 74, 88)
                };
        }
    }

    private static Color C(int r, int g, int b) => new Color(r / 255f, g / 255f, b / 255f, 1f);

    // =========================================================
    // EMBLEMS (face space: -1..1, y up)
    // =========================================================

    public static readonly string[] Emblems = { "taxpayer_id", "receipt", "star" };

    private static System.Func<float, float, bool> EmblemShape(string emblem)
    {
        switch (emblem)
        {
            case "taxpayer_id": return TaxpayerId;
            case "receipt": return Receipt;
            default: return Star;
        }
    }

    /// <summary>Know Your Taxpayer: an ID card (photo + text lines) under a magnifying glass.</summary>
    private static bool TaxpayerId(float x, float y)
    {
        // Magnifying glass, bottom-right, over the card.
        float mx = x - 0.36f, my = y + 0.26f;
        float mr = Mathf.Sqrt(mx * mx + my * my);
        if (mr >= 0.2f && mr <= 0.3f) return true;
        // Handle at 45 degrees down-right.
        float hx = (mx + my * -1f) * 0.7071f;   // along the handle
        float hy = (mx + my) * 0.7071f;        // across the handle
        if (hx >= 0.27f && hx <= 0.62f && Mathf.Abs(hy) <= 0.075f) return true;
        if (mr < 0.2f) return false;            // lens shows the face through it

        // Card with cut-outs.
        if (!RoundRect(x, y, -0.7f, -0.42f, 0.52f, 0.46f, 0.1f)) return false;
        float hdx = x + 0.38f, hdy = y - 0.12f;
        if (hdx * hdx + hdy * hdy <= 0.0225f) return false;                        // head
        float sdx = (x + 0.38f) / 0.22f, sdy = (y + 0.22f) / 0.16f;
        if (y >= -0.3f && sdx * sdx + sdy * sdy <= 1f) return false;               // shoulders
        if (x >= -0.08f && x <= 0.4f && Mathf.Abs(y - 0.24f) <= 0.045f) return false;  // name line
        if (x >= -0.08f && x <= 0.3f && Mathf.Abs(y - 0.07f) <= 0.04f) return false;   // detail line
        return true;
    }

    /// <summary>A receipt with a torn zig-zag bottom and printed lines.</summary>
    private static bool Receipt(float x, float y)
    {
        if (x < -0.42f || x > 0.42f || y > 0.62f) return false;
        float zig = -0.58f + 0.08f * Mathf.Abs(Mathf.Repeat((x + 0.42f) / 0.14f, 2f) - 1f);
        if (y < zig) return false;
        if (x > -0.28f && x < 0.28f)
            for (int i = 0; i < 4; i++)
                if (Mathf.Abs(y - (0.4f - i * 0.22f)) <= 0.035f && x < (i == 3 ? 0.05f : 0.28f)) return false;
        return true;
    }

    private static bool Star(float x, float y)
    {
        float r = Mathf.Sqrt(x * x + y * y);
        float a = Mathf.Atan2(y, x) - Mathf.PI * 0.5f;
        float sector = Mathf.Repeat(a, Mathf.PI * 2f / 5f) / (Mathf.PI * 2f / 5f);
        float toward = Mathf.Abs(sector * 2f - 1f);
        return r <= Mathf.Lerp(0.36f, 0.82f, toward * toward);
    }

    private static bool RoundRect(float x, float y, float x0, float y0, float x1, float y1, float r)
    {
        if (x < x0 || x > x1 || y < y0 || y > y1) return false;
        float cx = Mathf.Clamp(x, x0 + r, x1 - r), cy = Mathf.Clamp(y, y0 + r, y1 - r);
        float dx = x - cx, dy = y - cy;
        return dx * dx + dy * dy <= r * r;
    }
}
