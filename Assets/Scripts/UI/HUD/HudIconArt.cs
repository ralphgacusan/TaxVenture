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
///   reviewed  - Arthur's Reviewed File (T30): the Case Folder icon with its tan cover turned muted teal (ReviewedFileLook.Teal)
///   profiles  - the clients' ID cards (2026-10-27): a cream ID card with its blue band and portrait, a second one turned over behind
///   papers    - the corrected annotated records collected in Level 2
///   bag       - a leather satchel with a brass buckle (T10 Bag icon)
///   slot      - an EMPTY hotbar slot: dim face, dashed rim and a "+" (T10)
///   locked    - a locked slot: dim face, plain grey rim (a padlock is drawn over it)
///   postcard  - a blue-bordered postcard with sea, sand and a palm (T11 keepsake)
///   voucher   - a folded green ticket with a clock badge (T11 review voucher)
///   bookmarks - three leather ribbons on a brass bar (T12 bookmark set)
///   pouch     - a small leather belt pouch with a brass stud (T12 Belt Pouch)
///   pencilcup - a squat brass mesh cup with red, blue and green pencils (T13 decor)
///   islandprint - a dark wood frame around an island print (T13 decor)
///   stand     - two records held upright on a wooden stand (T13 teaser)
///   computer  - a desk monitor and keyboard (T13 teaser)
/// Render() is pure (no Unity objects) for tests and previews; Get() caches
/// the sprite.
/// </summary>
public static class HudIconArt
{
    public const int Size = 160;
    private const int Samples = 3;

    public static readonly string[] Icons = { "settings", "notes", "folder", "reviewed", "profiles", "papers", "book", "bag", "slot", "locked", "postcard", "voucher", "bookmarks", "pouch", "pencilcup", "islandprint", "stand", "computer" };
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
        if (icon == "slot" || icon == "locked") return EmptyTile(u, v, icon == "slot");

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
            case "folder": Folder(u, v, ref c, false); break;
            case "reviewed": Folder(u, v, ref c, true); break;
            case "profiles": Cards(u, v, ref c); break;
            case "papers": Papers(u, v, ref c); break;
            case "book": Book(u, v, ref c); break;
            case "bag": Satchel(u, v, ref c); break;
            case "postcard": Postcard(u, v, ref c); break;
            case "voucher": Voucher(u, v, ref c); break;
            case "bookmarks": Bookmarks(u, v, ref c); break;
            case "pouch": Pouch(u, v, ref c); break;
            case "pencilcup": PencilCup(u, v, ref c); break;
            case "islandprint": IslandPrint(u, v, ref c); break;
            case "stand": ComparisonStand(u, v, ref c); break;
            case "computer": Computer(u, v, ref c); break;
        }
        return c;
    }

    /// <summary>An empty slot (dashed rim and "+") or a locked one (plain rim): the dim face of a locked icon, no picture.</summary>
    private static Color EmptyTile(float u, float v, bool dashed)
    {
        Color c = new Color(0, 0, 0, 0);
        if (RoundRect(u, v - (-0.03f), 0.08f, 0.06f, 0.92f, 0.92f, 0.22f, 0.04f, out float sh))
            Over(ref c, new Color(0f, 0f, 0f, 0.18f * sh));
        bool outer = RoundRect(u, v, 0.06f, 0.08f, 0.94f, 0.94f, 0.22f, 0f, out _);
        bool inner = RoundRect(u, v, 0.1f, 0.12f, 0.9f, 0.9f, 0.18f, 0f, out _);
        if (inner) Over(ref c, new Color(14f / 255f, 22f / 255f, 34f / 255f, 0.42f));
        if (outer && !inner)
        {
            if (!dashed) Over(ref c, new Color(0.55f, 0.6f, 0.66f, 0.55f));
            else
            {
                // Dashes along each edge (the coordinate running along that edge).
                bool horizontal = v < 0.16f || v > 0.86f;
                float along = horizontal ? u : v;
                if (Mathf.Repeat(along * 7f, 1f) < 0.55f) Over(ref c, new Color(236f / 255f, 206f / 255f, 140f / 255f, 0.75f));
            }
        }
        if (dashed && ((Mathf.Abs(u - 0.5f) < 0.028f && Mathf.Abs(v - 0.51f) < 0.13f) || (Mathf.Abs(v - 0.51f) < 0.028f && Mathf.Abs(u - 0.5f) < 0.13f)))
            Over(ref c, new Color(245f / 255f, 240f / 255f, 226f / 255f, 0.6f));
        return c;
    }

    private static void Postcard(float u, float v, ref Color c)
    {
        // Tilted card: coastal blue border, sky, sea, sand and one palm.
        float x = u - 0.5f, y = v - 0.5f;
        float ca = Mathf.Cos(-0.12f), sa = Mathf.Sin(-0.12f);
        float px = x * ca - y * sa + 0.5f, py = x * sa + y * ca + 0.5f;
        if (RoundRect(px + 0.014f, py + 0.02f, 0.2f, 0.26f, 0.8f, 0.72f, 0.03f, 0f, out _)) Over(ref c, new Color(0f, 0f, 0f, 0.35f));
        if (!RoundRect(px, py, 0.2f, 0.26f, 0.8f, 0.72f, 0.03f, 0f, out _)) return;
        Over(ref c, C(58, 132, 196));                                                               // border
        if (!RoundRect(px, py, 0.24f, 0.3f, 0.76f, 0.68f, 0.015f, 0f, out _)) return;
        if (py > 0.48f) Over(ref c, Color.Lerp(C(170, 220, 246), C(120, 190, 236), (py - 0.48f) / 0.2f));   // sky
        else if (py > 0.4f) Over(ref c, C(38, 124, 186));                                           // sea
        else Over(ref c, C(236, 210, 150));                                                          // sand
        float sx = px - 0.64f, sy = py - 0.6f;
        if (sx * sx + sy * sy < 0.035f * 0.035f) Over(ref c, C(255, 214, 90));                      // sun
        if (Mathf.Abs(px - 0.36f - (py - 0.32f) * 0.15f) < 0.012f && py > 0.32f && py < 0.56f) Over(ref c, C(120, 84, 48)); // trunk
        float lx = px - 0.39f, ly = py - 0.57f;
        if (Mathf.Abs(ly + lx * lx * 3f) < 0.012f && Mathf.Abs(lx) < 0.09f) Over(ref c, C(46, 140, 82)); // leaves
    }

    private static void Voucher(float u, float v, ref Color c)
    {
        // A folded green ticket with notched ends and a clock badge.
        if (RoundRect(u + 0.014f, v + 0.02f, 0.18f, 0.3f, 0.82f, 0.66f, 0.03f, 0f, out _)) Over(ref c, new Color(0f, 0f, 0f, 0.35f));
        bool body = RoundRect(u, v, 0.18f, 0.3f, 0.82f, 0.66f, 0.03f, 0f, out _);
        float nl = (u - 0.18f) * (u - 0.18f) + (v - 0.48f) * (v - 0.48f);
        float nr = (u - 0.82f) * (u - 0.82f) + (v - 0.48f) * (v - 0.48f);
        if (!body || nl < 0.05f * 0.05f || nr < 0.05f * 0.05f) return;
        Over(ref c, Color.Lerp(C(46, 140, 84), C(96, 190, 118), (v - 0.3f) / 0.36f));
        if (Mathf.Abs(u - 0.6f) < 0.006f && Mathf.Repeat(v * 30f, 1f) < 0.5f) Over(ref c, C(220, 240, 220));     // perforation
        if (Mathf.Abs(v - 0.4f) < 0.01f && u > 0.25f && u < 0.54f) Over(ref c, C(220, 240, 220));
        float cx = u - 0.4f, cy = v - 0.53f;
        float r = Mathf.Sqrt(cx * cx + cy * cy);
        if (r < 0.075f) Over(ref c, C(250, 248, 236));                                                            // clock face
        if (r < 0.075f && ((Mathf.Abs(cx) < 0.008f && cy > 0f && cy < 0.055f) || (Mathf.Abs(cy) < 0.008f && cx > 0f && cx < 0.04f)))
            Over(ref c, C(40, 70, 50));                                                                            // hands
        if (u > 0.64f && u < 0.76f && Mathf.Abs(v - 0.48f) < 0.012f) Over(ref c, C(250, 248, 236));              // "+"
        if (Mathf.Abs(u - 0.7f) < 0.012f && v > 0.42f && v < 0.54f) Over(ref c, C(250, 248, 236));
    }

    private static void Bookmarks(float u, float v, ref Color c)
    {
        // Three leather ribbons with V-notched tails hanging from a brass bar.
        Color[] leather = { C(150, 40, 46), C(46, 112, 74), C(44, 70, 128) };
        for (int i = 0; i < 3; i++)
        {
            float cx = 0.36f + i * 0.14f, half = 0.045f;
            float bottom = 0.24f + i * 0.05f;
            if (u + 0.012f > cx - half && u + 0.012f < cx + half && v + 0.018f > bottom && v + 0.018f < 0.72f) Over(ref c, new Color(0f, 0f, 0f, 0.3f));
            bool inStrip = u > cx - half && u < cx + half && v > bottom && v < 0.72f;
            bool notch = v < bottom + 0.06f && Mathf.Abs(u - cx) < (bottom + 0.06f - v) * 0.8f;
            if (!inStrip || notch) continue;
            Over(ref c, Color.Lerp(leather[i] * 0.8f, leather[i], (v - bottom) / (0.72f - bottom)));
            if (Mathf.Abs(u - (cx - half + 0.012f)) < 0.004f || Mathf.Abs(u - (cx + half - 0.012f)) < 0.004f)
                Over(ref c, new Color(1f, 0.92f, 0.8f, 0.45f));                                        // stitching
        }
        if (RoundRect(u, v, 0.26f, 0.68f, 0.74f, 0.78f, 0.04f, 0f, out _))
        {
            Over(ref c, Color.Lerp(C(150, 116, 58), C(236, 206, 140), (v - 0.68f) / 0.1f));            // brass bar
            if (v > 0.75f) Over(ref c, new Color(1f, 1f, 1f, 0.2f));
        }
    }

    private static void Pouch(float u, float v, ref Color c)
    {
        // Belt strap behind, the pouch body, its flap and a brass stud.
        if (v > 0.6f && v < 0.68f && u > 0.14f && u < 0.86f) Over(ref c, C(70, 46, 28));
        if (RoundRect(u + 0.012f, v + 0.018f, 0.28f, 0.2f, 0.72f, 0.66f, 0.1f, 0f, out _)) Over(ref c, new Color(0f, 0f, 0f, 0.35f));
        if (RoundRect(u, v, 0.28f, 0.2f, 0.72f, 0.66f, 0.1f, 0f, out _))
        {
            Over(ref c, Color.Lerp(C(132, 82, 44), C(176, 116, 64), (v - 0.2f) / 0.46f));
            if (Mathf.Abs(v - 0.26f) < 0.007f && u > 0.33f && u < 0.67f) Over(ref c, new Color(1f, 0.9f, 0.7f, 0.35f));   // stitching
        }
        if (RoundRect(u, v, 0.26f, 0.44f, 0.74f, 0.7f, 0.08f, 0f, out _))
        {
            Over(ref c, Color.Lerp(C(102, 60, 30), C(146, 92, 50), (v - 0.44f) / 0.26f));            // flap
            if (v > 0.66f) Over(ref c, new Color(1f, 1f, 1f, 0.12f));
        }
        float sx = u - 0.5f, sy = v - 0.47f;
        if (sx * sx + sy * sy < 0.035f * 0.035f) Over(ref c, C(236, 206, 140));                        // stud
        if (sx * sx + sy * sy < 0.016f * 0.016f) Over(ref c, C(150, 116, 58));
    }

    private static void PencilCup(float u, float v, ref Color c)
    {
        // Three pencils behind the rim (red, blue, green) with wood tips and dark leads.
        Color[] colours = { C(196, 46, 42), C(42, 92, 184), C(46, 142, 78) };
        float[] tops = { 0.8f, 0.74f, 0.78f };
        for (int i = 0; i < 3; i++)
        {
            float cx = 0.4f + i * 0.1f, lean = (i - 1) * 0.12f;
            float x = u - cx - (v - 0.5f) * lean;
            if (Mathf.Abs(x) < 0.028f && v > 0.5f && v < tops[i] - 0.06f) Over(ref c, Color.Lerp(colours[i] * 0.8f, colours[i], 0.5f + x * 10f));
            float tipBase = tops[i] - 0.06f;
            if (v >= tipBase && v < tops[i] && Mathf.Abs(x) < 0.028f * (tops[i] - v) / 0.06f)
                Over(ref c, v > tops[i] - 0.02f ? C(44, 44, 48) : C(232, 204, 152));
        }
        // The squat brass mesh cup: shadow, body with a soft side shade, dark mesh bands and slats, bright rim.
        if (RoundRect(u + 0.012f, v + 0.018f, 0.28f, 0.2f, 0.72f, 0.56f, 0.05f, 0f, out _)) Over(ref c, new Color(0f, 0f, 0f, 0.35f));
        if (RoundRect(u, v, 0.28f, 0.2f, 0.72f, 0.56f, 0.05f, 0f, out _))
        {
            Over(ref c, Color.Lerp(C(176, 132, 58), C(236, 196, 112), 1f - Mathf.Abs(u - 0.44f) * 2.4f));
            bool band = Mathf.Abs(v - 0.3f) < 0.012f || Mathf.Abs(v - 0.4f) < 0.012f || Mathf.Abs(v - 0.48f) < 0.012f;
            bool slat = Mathf.Abs(Mathf.Repeat(u * 22f, 1f) - 0.5f) < 0.12f && v > 0.24f && v < 0.52f;
            if (band || slat) Over(ref c, new Color(0.36f, 0.26f, 0.12f, 0.55f));
        }
        if (RoundRect(u, v, 0.26f, 0.53f, 0.74f, 0.59f, 0.03f, 0f, out _)) Over(ref c, C(244, 214, 140));
    }

    private static void IslandPrint(float u, float v, ref Color c)
    {
        // Dark wood frame, cream mat and a small island scene.
        if (RoundRect(u + 0.012f, v + 0.018f, 0.2f, 0.24f, 0.8f, 0.76f, 0.02f, 0f, out _)) Over(ref c, new Color(0f, 0f, 0f, 0.35f));
        if (!RoundRect(u, v, 0.2f, 0.24f, 0.8f, 0.76f, 0.02f, 0f, out _)) return;
        Over(ref c, Color.Lerp(C(54, 32, 18), C(92, 60, 36), (v - 0.24f) / 0.52f));                // frame
        if (!RoundRect(u, v, 0.25f, 0.29f, 0.75f, 0.71f, 0.01f, 0f, out _)) return;
        Over(ref c, C(240, 232, 212));                                                              // mat
        if (!RoundRect(u, v, 0.29f, 0.33f, 0.71f, 0.67f, 0f, 0f, out _)) return;
        if (v > 0.47f) Over(ref c, Color.Lerp(C(186, 222, 244), C(126, 192, 236), (v - 0.47f) / 0.2f)); // sky
        else Over(ref c, C(34, 118, 178));                                                          // sea
        float ix = (u - 0.47f) / 0.14f, iy = (v - 0.45f) / 0.06f;
        if (ix * ix + iy * iy < 1f && v > 0.43f) Over(ref c, C(234, 206, 142));                      // island
        float hx = (u - 0.47f) / 0.07f, hy = (v - 0.48f) / 0.05f;
        if (hx * hx + hy * hy < 1f && v > 0.47f) Over(ref c, C(70, 150, 86));                        // hill
        float sx = u - 0.64f, sy = v - 0.6f;
        if (sx * sx + sy * sy < 0.028f * 0.028f) Over(ref c, C(255, 212, 96));                       // sun
        if (Mathf.Abs(u - 0.53f - (v - 0.47f) * 0.2f) < 0.008f && v > 0.47f && v < 0.6f) Over(ref c, C(116, 80, 46)); // palm
        float lx = u - 0.555f, ly = v - 0.6f;
        if (Mathf.Abs(ly + lx * lx * 4f) < 0.01f && Mathf.Abs(lx) < 0.06f) Over(ref c, C(40, 132, 74));
    }

    private static void ComparisonStand(float u, float v, ref Color c)
    {
        // A wooden base with two upright records side by side.
        if (RoundRect(u + 0.012f, v + 0.018f, 0.2f, 0.22f, 0.8f, 0.3f, 0.03f, 0f, out _)) Over(ref c, new Color(0f, 0f, 0f, 0.35f));
        for (int i = 0; i < 2; i++)
        {
            float x0 = 0.25f + i * 0.27f;
            if (RoundRect(u + 0.01f, v + 0.014f, x0, 0.3f, x0 + 0.23f, 0.76f, 0.02f, 0f, out _)) Over(ref c, new Color(0f, 0f, 0f, 0.3f));
            if (!RoundRect(u, v, x0, 0.3f, x0 + 0.23f, 0.76f, 0.02f, 0f, out _)) continue;
            Over(ref c, C(250, 246, 234));
            for (int line = 0; line < 5; line++)
                if (Mathf.Abs(v - (0.68f - line * 0.07f)) < 0.008f && u > x0 + 0.03f && u < x0 + 0.2f - (line % 2) * 0.05f) Over(ref c, C(150, 160, 176));
        }
        if (RoundRect(u, v, 0.2f, 0.22f, 0.8f, 0.31f, 0.03f, 0f, out _)) Over(ref c, Color.Lerp(C(110, 70, 40), C(156, 104, 62), (v - 0.22f) / 0.09f));
    }

    private static void Computer(float u, float v, ref Color c)
    {
        // A monitor on a short neck and a keyboard in front.
        if (RoundRect(u + 0.012f, v + 0.018f, 0.22f, 0.4f, 0.78f, 0.78f, 0.04f, 0f, out _)) Over(ref c, new Color(0f, 0f, 0f, 0.35f));
        if (RoundRect(u, v, 0.22f, 0.4f, 0.78f, 0.78f, 0.04f, 0f, out _))
        {
            Over(ref c, C(48, 54, 64));                                                             // bezel
            if (RoundRect(u, v, 0.26f, 0.44f, 0.74f, 0.74f, 0.02f, 0f, out _))
                Over(ref c, Color.Lerp(C(56, 120, 168), C(120, 190, 226), (v - 0.44f) / 0.3f));     // screen
        }
        if (u > 0.46f && u < 0.54f && v > 0.32f && v < 0.4f) Over(ref c, C(70, 76, 88));             // neck
        if (RoundRect(u, v, 0.36f, 0.3f, 0.64f, 0.34f, 0.015f, 0f, out _)) Over(ref c, C(70, 76, 88)); // foot
        if (RoundRect(u, v, 0.24f, 0.17f, 0.76f, 0.26f, 0.02f, 0f, out _))
        {
            Over(ref c, C(214, 216, 222));                                                          // keyboard
            if (Mathf.Repeat(u * 18f, 1f) < 0.18f || Mathf.Abs(v - 0.215f) < 0.006f) Over(ref c, new Color(0.45f, 0.47f, 0.52f, 0.6f));
        }
    }

    private static void Satchel(float u, float v, ref Color c)
    {
        // Strap arc over the bag.
        float sx = u - 0.5f, sy = v - 0.6f;
        float sr = Mathf.Sqrt(sx * sx + sy * sy);
        if (sy > 0f && sr > 0.19f && sr < 0.235f) Over(ref c, C(120, 72, 38));
        // Shadow + body.
        if (RoundRect(u + 0.012f, v + 0.018f, 0.2f, 0.2f, 0.8f, 0.6f, 0.07f, 0f, out _)) Over(ref c, new Color(0f, 0f, 0f, 0.35f));
        if (RoundRect(u, v, 0.2f, 0.2f, 0.8f, 0.6f, 0.07f, 0f, out _))
        {
            Over(ref c, Color.Lerp(C(140, 86, 44), C(186, 122, 66), (v - 0.2f) / 0.4f));
            if (Mathf.Abs(v - 0.27f) < 0.008f && u > 0.25f && u < 0.75f) Over(ref c, new Color(1f, 0.9f, 0.7f, 0.35f));   // stitching
        }
        // Flap with a rounded lower edge.
        if (RoundRect(u, v, 0.18f, 0.4f, 0.82f, 0.64f, 0.08f, 0f, out _))
        {
            Over(ref c, Color.Lerp(C(110, 64, 32), C(156, 98, 52), (v - 0.4f) / 0.24f));
            if (v > 0.6f) Over(ref c, new Color(1f, 1f, 1f, 0.12f));
        }
        // Brass buckle on the flap.
        if (RoundRect(u, v, 0.44f, 0.36f, 0.56f, 0.47f, 0.02f, 0f, out _))
        {
            Over(ref c, C(236, 206, 140));
            if (RoundRect(u, v, 0.475f, 0.39f, 0.525f, 0.44f, 0.01f, 0f, out _)) Over(ref c, C(120, 92, 44));
        }
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

    /// <summary>The Case Folder; reviewed: Arthur's Reviewed File, the same folder with only its tan cover turned teal (ReviewedFileLook.Teal).</summary>
    private static void Folder(float u, float v, ref Color c, bool reviewed)
    {
        Color Cover(Color tan) => reviewed ? ReviewedFileLook.Teal(tan) : tan;
        if (RoundRect(u + 0.012f, v + 0.018f, 0.2f, 0.24f, 0.8f, 0.72f, 0.04f, 0f, out _)) Over(ref c, new Color(0f, 0f, 0f, 0.35f));
        // Back with tab.
        if (RoundRect(u, v, 0.2f, 0.26f, 0.8f, 0.7f, 0.04f, 0f, out _) || RoundRect(u, v, 0.2f, 0.62f, 0.45f, 0.76f, 0.04f, 0f, out _))
            Over(ref c, Cover(C(184, 138, 72)));
        // Paper peeking out.
        if (RoundRect(u, v, 0.27f, 0.36f, 0.72f, 0.7f, 0.02f, 0f, out _))
        {
            Over(ref c, C(250, 248, 240));
            if ((Mathf.Abs(v - 0.62f) < 0.01f || Mathf.Abs(v - 0.56f) < 0.01f) && u > 0.33f && u < 0.64f) Over(ref c, C(150, 160, 175));
        }
        // Front flap, slightly lower and lighter.
        if (RoundRect(u, v, 0.18f, 0.24f, 0.82f, 0.56f, 0.05f, 0f, out _))
        {
            Over(ref c, Color.Lerp(Cover(C(214, 166, 92)), Cover(C(242, 200, 128)), (v - 0.24f) / 0.32f));
            if (RoundRect(u, v, 0.4f, 0.33f, 0.6f, 0.43f, 0.02f, 0f, out _)) Over(ref c, C(206, 84, 72));  // case label
            if (v > 0.53f) Over(ref c, new Color(1f, 1f, 1f, 0.18f));                                       // edge highlight
        }
    }

    private static void Cards(float u, float v, ref Color c)
    {
        // 2026-10-27: the clients' ID cards (IdCardModel's look): a second card turned over behind,
        // showing its blue profile band, and the front card: cream stock, blue header band,
        // a portrait box with the neutral bust, and printed lines.
        float bx = u - 0.55f, by = v - 0.56f;
        float ca = Mathf.Cos(0.24f), sa = Mathf.Sin(0.24f);
        float rx = bx * ca + by * sa, ry = -bx * sa + by * ca;
        if (RoundRect(rx, ry, -0.25f, -0.16f, 0.25f, 0.16f, 0.05f, 0f, out _))
        {
            Over(ref c, C(222, 214, 196));
            if (ry > 0.07f) Over(ref c, C(91, 127, 166));
            for (int i = 0; i < 2; i++)
                if (Mathf.Abs(ry - (0.0f - i * 0.07f)) < 0.012f && rx > -0.19f && rx < 0.17f) Over(ref c, C(150, 140, 120));
        }
        // Front card: a soft shadow, a darker edge (its thickness), then the card.
        if (RoundRect(u + 0.014f, v + 0.02f, 0.16f, 0.22f, 0.76f, 0.6f, 0.055f, 0f, out _)) Over(ref c, new Color(0f, 0f, 0f, 0.38f));
        if (RoundRect(u, v + 0.012f, 0.16f, 0.22f, 0.76f, 0.6f, 0.055f, 0f, out _)) Over(ref c, C(171, 181, 183));
        if (RoundRect(u, v, 0.16f, 0.235f, 0.76f, 0.6f, 0.055f, 0f, out _))
        {
            Over(ref c, Color.Lerp(C(236, 231, 219), C(248, 245, 236), (v - 0.235f) / 0.365f));
            if (v > 0.515f) Over(ref c, C(91, 127, 166));                                                     // header band
            if (v > 0.53f && v < 0.575f && u > 0.27f && u < 0.56f && Mathf.Abs(v - 0.552f) < 0.012f) Over(ref c, C(255, 251, 240)); // title
            if (RoundRect(u, v, 0.21f, 0.29f, 0.38f, 0.49f, 0.02f, 0f, out _))
            {
                Over(ref c, C(219, 227, 235));                                                               // portrait box
                float hx = u - 0.295f, hy = v - 0.415f;
                if (hx * hx + hy * hy < 0.04f * 0.04f) Over(ref c, C(138, 152, 168));                          // head
                float ex = (u - 0.295f) / 0.07f, ey = (v - 0.3f) / 0.06f;
                if (v < 0.36f && ex * ex + ey * ey < 1f) Over(ref c, C(138, 152, 168));                      // shoulders
            }
            for (int i = 0; i < 3; i++)
            {
                float ly = 0.46f - i * 0.065f;
                if (Mathf.Abs(v - ly) < 0.013f && u > 0.42f && u < (i == 0 ? 0.7f : i == 1 ? 0.64f : 0.56f)) Over(ref c, C(46, 40, 34));
            }
        }
    }

    private static void Papers(float u, float v, ref Color c)
    {
        // A small stack of corrected records, with a green check on the top sheet.
        if (RoundRect(u + 0.025f, v - 0.015f, 0.24f, 0.22f, 0.69f, 0.72f, 0.035f, 0f, out _))
            Over(ref c, new Color(0f, 0f, 0f, 0.35f));
        if (RoundRect(u - 0.045f, v + 0.015f, 0.24f, 0.29f, 0.7f, 0.78f, 0.035f, 0f, out _))
            Over(ref c, C(220, 215, 199));
        if (RoundRect(u, v, 0.25f, 0.22f, 0.72f, 0.73f, 0.035f, 0f, out _))
        {
            Over(ref c, C(250, 248, 238));
            if (RoundRect(u, v, 0.25f, 0.61f, 0.72f, 0.73f, 0.025f, 0f, out _))
                Over(ref c, C(46, 158, 106));
            for (int i = 0; i < 3; i++)
            {
                float lineY = 0.53f - i * 0.075f;
                if (Mathf.Abs(v - lineY) < 0.009f && u > 0.32f && u < 0.64f)
                    Over(ref c, C(124, 145, 153));
            }
            // Green confirmation tick on the lower corner.
            float ax = 0.48f, ay = 0.34f;
            if (DistanceToSegment(new Vector2(u, v), new Vector2(ax - 0.055f, ay), new Vector2(ax - 0.01f, ay - 0.045f)) < 0.014f ||
                DistanceToSegment(new Vector2(u, v), new Vector2(ax - 0.01f, ay - 0.045f), new Vector2(ax + 0.075f, ay + 0.05f)) < 0.014f)
                Over(ref c, C(46, 158, 106));
        }
    }

    private static void Book(float u, float v, ref Color c)
    {
        // A closed reference book: cream page block, deep red cover (like the desk book), spine bands and a bookmark ribbon.
        if (RoundRect(u + 0.014f, v + 0.02f, 0.24f, 0.2f, 0.76f, 0.8f, 0.04f, 0f, out _)) Over(ref c, new Color(0f, 0f, 0f, 0.35f));
        if (RoundRect(u, v, 0.3f, 0.21f, 0.75f, 0.76f, 0.025f, 0f, out _))
        {
            Over(ref c, C(246, 240, 222));
            if (u > 0.7f && Mathf.Repeat(v * 60f, 1f) < 0.35f) Over(ref c, C(214, 204, 180));          // page edges
        }
        if (RoundRect(u, v, 0.24f, 0.24f, 0.7f, 0.8f, 0.04f, 0f, out _))
        {
            Over(ref c, Color.Lerp(C(128, 26, 30), C(196, 52, 52), (v - 0.24f) / 0.56f));
            if (u < 0.31f) Over(ref c, C(98, 18, 22));                                                 // spine
            if ((Mathf.Abs(v - 0.7f) < 0.012f || Mathf.Abs(v - 0.34f) < 0.012f) && u < 0.31f) Over(ref c, C(236, 206, 140));
            if (RoundRect(u, v, 0.38f, 0.5f, 0.62f, 0.64f, 0.02f, 0f, out _))                            // title plate
            {
                Over(ref c, C(236, 206, 140));
                if (Mathf.Abs(v - 0.57f) < 0.01f && u > 0.42f && u < 0.58f) Over(ref c, C(120, 92, 44));
            }
            if (v > 0.74f) Over(ref c, new Color(1f, 1f, 1f, 0.14f));                                  // top highlight
        }
        if (u > 0.56f && u < 0.62f && v > 0.12f && v < 0.3f && v > 0.12f + Mathf.Abs(u - 0.59f) * 1.4f)
            Over(ref c, C(236, 206, 140));                                                              // ribbon
    }

    private static float DistanceToSegment(Vector2 point, Vector2 start, Vector2 end)
    {
        Vector2 delta = end - start;
        float t = Mathf.Clamp01(Vector2.Dot(point - start, delta) / Mathf.Max(0.0001f, delta.sqrMagnitude));
        return Vector2.Distance(point, start + delta * t);
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
