using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// PURPOSE:
/// Shared visual language for the runtime-built Office HUD (progression card,
/// objective tracker, toasts). Palette matches the Ticket 06 minimap so every
/// HUD cluster reads as one system: navy panels, brass edges, cream values.
///
/// Sprites are generated procedurally once and cached, so the HUD needs no
/// imported art. Cached textures are re-created if Unity unloads them.
///
/// Presentation only — nothing here reads or changes gameplay state.
/// </summary>
public static class HudTheme
{
    // =========================================================
    // PALETTE
    // =========================================================

    public static readonly Color PanelFill = new Color32(18, 28, 41, 220);
    public static readonly Color PanelInner = new Color32(12, 19, 29, 255);
    public static readonly Color Brass = new Color32(202, 169, 101, 255);
    public static readonly Color BrassSoft = new Color32(221, 190, 126, 255);
    public static readonly Color TextPrimary = new Color32(245, 240, 226, 255);
    public static readonly Color TextMuted = new Color32(168, 180, 192, 255);
    public static readonly Color Track = new Color32(52, 66, 82, 255);
    public static readonly Color Empty = new Color32(60, 72, 86, 255);
    public static readonly Color Exp = new Color32(241, 194, 108, 255);
    public static readonly Color ExpGain = new Color32(255, 236, 186, 255);
    public static readonly Color Reputation = new Color32(236, 93, 98, 255);
    public static readonly Color ReputationGain = new Color32(255, 196, 198, 255);
    public static readonly Color Complete = new Color32(110, 201, 140, 255);
    public static readonly Color Active = new Color32(100, 215, 255, 255);
    public static readonly Color ShadowColor = new Color(0.015f, 0.025f, 0.04f, 0.55f);

    // =========================================================
    // TYPE SCALE (reference resolution 1920x1080)
    // =========================================================

    public const float HeadingSize = 26f;
    public const float ValueSize = 21f;
    public const float CaptionSize = 18f;

    /// <summary>Uniform scale applied to every HUD cluster (user-approved size).</summary>
    public const float HudScale = 1.15f;

    public const float Margin = 20f;
    public const float Padding = 16f;
    public const float CornerRadius = 10f;

    // =========================================================
    // FONT
    // =========================================================

    /// <summary>Full-coverage UI font (see HudFontSet for why not the scene's Minnie font).</summary>
    public static TMP_FontAsset Font
    {
        get
        {
            HudFontSet set = HudFontSet.Load();
            return set != null && set.uiFont != null ? set.uiFont : TMP_Settings.defaultFontAsset;
        }
    }

    /// <summary>Handwriting font for the Notes paper; falls back to the UI font.</summary>
    public static TMP_FontAsset HandwritingFont
    {
        get
        {
            HudFontSet set = HudFontSet.Load();
            return set != null && set.handwritingFont != null ? set.handwritingFont : Font;
        }
    }

    // =========================================================
    // SPRITES
    // =========================================================

    private const int RoundedSize = 64;
    private const int RoundedRadius = 20;

    private static Sprite rounded;
    private static Sprite circle;
    private static Sprite heart;
    private static Sprite check;
    private static Sprite star;
    private static Sprite tap;
    private static Sprite mouse;
    private static Sprite padlock;

    /// <summary>9-sliced rounded rectangle; scale corners with pixelsPerUnitMultiplier.</summary>
    public static Sprite Rounded
    {
        get
        {
            if (rounded == null)
            {
                Texture2D tex = Rasterize(RoundedSize, RoundedRoundRectCoverage);
                float b = RoundedRadius;
                rounded = Sprite.Create(tex, new Rect(0, 0, RoundedSize, RoundedSize),
                    new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(b, b, b, b));
                rounded.name = "HudRounded";
            }
            return rounded;
        }
    }

    public static Sprite Circle
    {
        get
        {
            if (circle == null) circle = MakeSprite(Rasterize(RoundedSize, CircleCoverage), "HudCircle");
            return circle;
        }
    }

    public static Sprite Heart
    {
        get
        {
            if (heart == null) heart = MakeSprite(Rasterize(RoundedSize, HeartCoverage), "HudHeart");
            return heart;
        }
    }

    /// <summary>Five-point star (EXP sparks and EXP toasts).</summary>
    public static Sprite Star
    {
        get
        {
            if (star == null) star = MakeSprite(Rasterize(RoundedSize, StarCoverage), "HudStar");
            return star;
        }
    }

    public static Sprite Check
    {
        get
        {
            if (check == null) check = MakeSprite(Rasterize(RoundedSize, CheckCoverage), "HudCheck");
            return check;
        }
    }

    /// <summary>Tap glyph: a fingertip dot inside a ripple ring (touch prompts).</summary>
    public static Sprite Tap
    {
        get
        {
            if (tap == null) tap = MakeSprite(Rasterize(RoundedSize, TapCoverage), "HudTap");
            return tap;
        }
    }

    /// <summary>Mouse glyph with the left button filled (desktop prompts).</summary>
    public static Sprite Mouse
    {
        get
        {
            if (mouse == null) mouse = MakeSprite(Rasterize(RoundedSize, MouseCoverage), "HudMouse");
            return mouse;
        }
    }

    /// <summary>Padlock glyph (locked prompts).</summary>
    public static Sprite Lock
    {
        get
        {
            if (padlock == null) padlock = MakeSprite(Rasterize(RoundedSize, LockCoverage), "HudLock");
            return padlock;
        }
    }

    private static Sprite MakeSprite(Texture2D tex, string name)
    {
        Sprite sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
        sprite.name = name;
        return sprite;
    }

    private delegate bool Inside(float x, float y);

    /// <summary>White texture whose alpha is the 4x4 supersampled coverage of the shape.</summary>
    private static Texture2D Rasterize(int size, Inside inside)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear,
            name = "HudShape"
        };
        var pixels = new Color32[size * size];
        const int samples = 4;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                int hits = 0;
                for (int sy = 0; sy < samples; sy++)
                    for (int sx = 0; sx < samples; sx++)
                        if (inside((x + (sx + 0.5f) / samples) / size, (y + (sy + 0.5f) / samples) / size))
                            hits++;
                byte a = (byte)(255 * hits / (samples * samples));
                pixels[y * size + x] = new Color32(255, 255, 255, a);
            }
        }
        tex.SetPixels32(pixels);
        tex.Apply(false, true);
        return tex;
    }

    private static bool RoundedRoundRectCoverage(float u, float v)
    {
        float r = (float)RoundedRadius / RoundedSize;
        float cx = Mathf.Clamp(u, r, 1f - r);
        float cy = Mathf.Clamp(v, r, 1f - r);
        float dx = u - cx, dy = v - cy;
        return dx * dx + dy * dy <= r * r;
    }

    private static bool CircleCoverage(float u, float v)
    {
        float dx = u - 0.5f, dy = v - 0.5f;
        return dx * dx + dy * dy <= 0.25f;
    }

    // Two thick strokes: short down-stroke then long up-stroke.
    private static bool StarCoverage(float u, float v)
    {
        float x = u - 0.5f, y = v - 0.5f;
        float r = Mathf.Sqrt(x * x + y * y);
        float a = Mathf.Atan2(y, x) - Mathf.PI * 0.5f;
        // Within each 72-degree wedge: 1 at a point (wedge edge), 0 between points.
        float sector = Mathf.Repeat(a, Mathf.PI * 2f / 5f) / (Mathf.PI * 2f / 5f);
        float towardPoint = Mathf.Abs(sector * 2f - 1f);
        return r <= Mathf.Lerp(0.21f, 0.48f, towardPoint * towardPoint);
    }

    private static bool TapCoverage(float u, float v)
    {
        float dx = u - 0.5f, dy = v - 0.5f;
        float r = Mathf.Sqrt(dx * dx + dy * dy);
        return r <= 0.16f || (r >= 0.32f && r <= 0.42f);
    }

    // Rounded body outline, a split between the buttons, the left button filled.
    private static bool MouseCoverage(float u, float v)
    {
        const float x0 = 0.26f, x1 = 0.74f, y0 = 0.06f, y1 = 0.94f, radius = 0.24f, stroke = 0.075f;
        if (!InRoundRect(u, v, x0, y0, x1, y1, radius)) return false;
        bool inner = InRoundRect(u, v, x0 + stroke, y0 + stroke, x1 - stroke, y1 - stroke, radius - stroke);
        if (!inner) return true;
        const float split = 0.56f;
        if (v >= split && v <= split + 0.05f) return true;
        if (v > split && Mathf.Abs(u - 0.5f) <= 0.025f) return true;
        return v > split && u < 0.5f;
    }

    // Body with a shackle arch on top.
    private static bool LockCoverage(float u, float v)
    {
        if (InRoundRect(u, v, 0.2f, 0.06f, 0.8f, 0.56f, 0.08f)) return true;
        float dx = u - 0.5f, dy = v - 0.62f;
        float r = Mathf.Sqrt(dx * dx + dy * dy);
        bool ring = r >= 0.14f && r <= 0.23f;
        return (ring && v >= 0.62f) || (Mathf.Abs(dx) >= 0.14f && Mathf.Abs(dx) <= 0.23f && v >= 0.5f && v < 0.62f);
    }

    private static bool InRoundRect(float u, float v, float x0, float y0, float x1, float y1, float r)
    {
        if (u < x0 || u > x1 || v < y0 || v > y1) return false;
        float cx = Mathf.Clamp(u, x0 + r, x1 - r);
        float cy = Mathf.Clamp(v, y0 + r, y1 - r);
        float dx = u - cx, dy = v - cy;
        return dx * dx + dy * dy <= r * r;
    }

    private static bool CheckCoverage(float u, float v)
    {
        const float thickness = 0.085f;
        return SegmentDistance(u, v, 0.18f, 0.52f, 0.41f, 0.28f) <= thickness ||
               SegmentDistance(u, v, 0.41f, 0.28f, 0.84f, 0.74f) <= thickness;
    }

    private static float SegmentDistance(float px, float py, float ax, float ay, float bx, float by)
    {
        float dx = bx - ax, dy = by - ay;
        float t = Mathf.Clamp01(((px - ax) * dx + (py - ay) * dy) / (dx * dx + dy * dy));
        float cx = ax + t * dx - px, cy = ay + t * dy - py;
        return Mathf.Sqrt(cx * cx + cy * cy);
    }

    // Classic implicit heart (x^2 + y^2 - 1)^3 - x^2 y^3 <= 0, fitted to the square.
    private static bool HeartCoverage(float u, float v)
    {
        float x = (u - 0.5f) * 2.6f;
        float y = (v - 0.45f) * 2.6f;
        float a = x * x + y * y - 1f;
        return a * a * a - x * x * y * y * y <= 0f;
    }

    // =========================================================
    // FACTORY
    // =========================================================

    public static RectTransform CreateRect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.layer = LayerMask.NameToLayer("UI");
        var rect = (RectTransform)go.transform;
        rect.SetParent(parent, false);
        return rect;
    }

    /// <summary>Anchors a rect to its parent's top-left, positioned in reference units.</summary>
    public static RectTransform PlaceTopLeft(RectTransform rect, float x, float y, float width, float height)
    {
        rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(x, -y);
        rect.sizeDelta = new Vector2(width, height);
        return rect;
    }

    /// <summary>Like PlaceTopLeft (same top-left coordinates), but pivoted on its centre so it scales and turns in place.</summary>
    public static RectTransform PlaceCentered(RectTransform rect, float x, float y, float width, float height)
    {
        rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(x + width * 0.5f, -y - height * 0.5f);
        rect.sizeDelta = new Vector2(width, height);
        return rect;
    }

    public static void Stretch(RectTransform rect, float inset = 0f)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(inset, inset);
        rect.offsetMax = new Vector2(-inset, -inset);
    }

    public static Image AddImage(RectTransform rect, Sprite sprite, Color color, float cornerRadius = 0f)
    {
        var image = rect.gameObject.AddComponent<Image>();
        image.sprite = sprite;
        image.color = color;
        image.raycastTarget = false;
        if (sprite == Rounded)
        {
            image.type = Image.Type.Sliced;
            // Map the sprite's 20px corner onto the requested radius.
            image.pixelsPerUnitMultiplier = RoundedRadius / Mathf.Max(1f, cornerRadius);
        }
        return image;
    }

    /// <summary>Brass-edged navy rounded panel with a soft drop shadow.</summary>
    public static RectTransform CreatePanel(string name, Transform parent, float cornerRadius = CornerRadius)
    {
        RectTransform frame = CreateRect(name, parent);
        Image edge = AddImage(frame, Rounded, Brass, cornerRadius);
        var shadow = frame.gameObject.AddComponent<Shadow>();
        shadow.effectColor = ShadowColor;
        shadow.effectDistance = new Vector2(3f, -4f);
        edge.color = new Color(Brass.r, Brass.g, Brass.b, 0.85f);

        RectTransform body = CreateRect("Body", frame);
        Stretch(body, 2f);
        AddImage(body, Rounded, PanelFill, cornerRadius - 1f);
        return body;
    }

    public static TextMeshProUGUI CreateLabel(string name, Transform parent, string text, float size,
        Color color, TextAlignmentOptions alignment, FontStyles style = FontStyles.Normal)
    {
        RectTransform rect = CreateRect(name, parent);
        var label = rect.gameObject.AddComponent<TextMeshProUGUI>();
        label.font = Font;
        label.text = text;
        label.fontSize = size;
        label.color = color;
        label.alignment = alignment;
        label.fontStyle = style;
        label.raycastTarget = false;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        // The scene HUD font (Minnie SDF) is wide and has no ellipsis glyph:
        // shrink to fit instead of clipping, and never draw an ellipsis.
        label.enableAutoSizing = true;
        label.fontSizeMax = size;
        label.fontSizeMin = Mathf.Max(8f, size * 0.6f);
        label.overflowMode = TextOverflowModes.Truncate;
        return label;
    }

    // =========================================================
    // CLOCK
    // =========================================================

    /// <summary>
    /// Writes "HH:MM:SS" into buffer (length >= 9 for 3-digit hours) without
    /// allocating. Returns the number of characters written.
    /// </summary>
    public static int FormatClock(char[] buffer, int totalSeconds)
    {
        if (totalSeconds < 0) totalSeconds = 0;
        int hours = Mathf.Min(totalSeconds / 3600, 999);
        int minutes = (totalSeconds / 60) % 60;
        int seconds = totalSeconds % 60;

        int n = 0;
        if (hours >= 100) buffer[n++] = (char)('0' + hours / 100);
        buffer[n++] = (char)('0' + (hours / 10) % 10);
        buffer[n++] = (char)('0' + hours % 10);
        buffer[n++] = ':';
        buffer[n++] = (char)('0' + minutes / 10);
        buffer[n++] = (char)('0' + minutes % 10);
        buffer[n++] = ':';
        buffer[n++] = (char)('0' + seconds / 10);
        buffer[n++] = (char)('0' + seconds % 10);
        return n;
    }

    // =========================================================
    // EASING
    // =========================================================

    public static float EaseOutCubic(float t)
    {
        t = 1f - Mathf.Clamp01(t);
        return 1f - t * t * t;
    }

    public static float EaseOutBack(float t)
    {
        t = Mathf.Clamp01(t) - 1f;
        const float s = 1.70158f;
        return 1f + t * t * ((s + 1f) * t + s);
    }
}
