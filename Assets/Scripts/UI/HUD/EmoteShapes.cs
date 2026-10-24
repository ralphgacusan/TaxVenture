using System;

/// <summary>
/// PURPOSE:
/// The emote icons as pure coverage tests (T22): Covers(shape, u, v) says
/// whether a point of the unit square (v up) is inside the icon. EmoteSprites
/// rasterizes these once into textures; the tests rasterize them to check
/// that every shape is really different. Shapes stay inside 0.08..0.92 so the
/// soft outline fits. Extra parts that move on their own (the three thinking
/// dots, the bulb's glow, the cloud's raindrop) are drawn separately.
/// </summary>
public static class EmoteShapes
{
    public static bool Covers(EmoteShape shape, float u, float v)
    {
        switch (shape)
        {
            case EmoteShape.Heart: return Heart(u, v);
            case EmoteShape.Sparkles: return Sparkles(u, v);
            case EmoteShape.Exclaim: return Exclaim(u, v);
            case EmoteShape.Question: return Question(u, v);
            case EmoteShape.Dots: return Dot(u, v, 0.2f) || Dot(u, v, 0.5f) || Dot(u, v, 0.8f);
            case EmoteShape.Bulb: return Bulb(u, v);
            case EmoteShape.SweatDrop: return Drop(u, v);
            case EmoteShape.RainCloud: return Cloud(u, v);
            case EmoteShape.AngerPuff: return AngerPuff(u, v);
            case EmoteShape.MusicNote: return MusicNote(u, v);
            default: return false;
        }
    }

    /// <summary>One thinking dot (the Dots shape is three of them in a row).</summary>
    public static bool SingleDot(float u, float v) => Circle(u, v, 0.5f, 0.5f, 0.36f);

    /// <summary>Soft glow behind the bulb: 1 at the centre, 0 at the edge.</summary>
    public static float Glow(float u, float v)
    {
        float dx = u - 0.5f, dy = v - 0.5f;
        float r = (float)Math.Sqrt(dx * dx + dy * dy) / 0.5f;
        float t = Math.Max(0f, 1f - r);
        return t * t;
    }

    private static bool Dot(float u, float v, float x) => Circle(u, v, x, 0.5f, 0.11f);

    private static bool Circle(float u, float v, float cx, float cy, float r)
    {
        float dx = u - cx, dy = v - cy;
        return dx * dx + dy * dy <= r * r;
    }

    private static bool Ring(float u, float v, float cx, float cy, float r, float half)
    {
        float dx = u - cx, dy = v - cy;
        float d = (float)Math.Sqrt(dx * dx + dy * dy);
        return Math.Abs(d - r) <= half;
    }

    private static float Segment(float px, float py, float ax, float ay, float bx, float by)
    {
        float dx = bx - ax, dy = by - ay;
        float t = ((px - ax) * dx + (py - ay) * dy) / (dx * dx + dy * dy);
        t = Math.Max(0f, Math.Min(1f, t));
        float cx = ax + t * dx - px, cy = ay + t * dy - py;
        return (float)Math.Sqrt(cx * cx + cy * cy);
    }

    // (x^2 + y^2 - 1)^3 - x^2 y^3 <= 0, fitted to the square.
    private static bool Heart(float u, float v)
    {
        float x = (u - 0.5f) * 2.7f;
        float y = (v - 0.47f) * 2.7f;
        float a = x * x + y * y - 1f;
        return a * a * a - x * x * y * y * y <= 0f;
    }

    // Four-point star: |x|^0.5 + |y|^0.5 <= r^0.5.
    private static bool Spark(float u, float v, float cx, float cy, float r)
    {
        float x = Math.Abs(u - cx) / r, y = Math.Abs(v - cy) / r;
        return (float)Math.Sqrt(x) + (float)Math.Sqrt(y) <= 1f;
    }

    private static bool Sparkles(float u, float v) =>
        Spark(u, v, 0.4f, 0.44f, 0.34f) || Spark(u, v, 0.76f, 0.76f, 0.15f) || Spark(u, v, 0.78f, 0.22f, 0.1f);

    private static bool Exclaim(float u, float v)
    {
        // Tapered bar (wide at the top) and a round dot.
        if (v >= 0.36f && v <= 0.9f)
        {
            float half = 0.065f + (v - 0.36f) / 0.54f * 0.045f;
            if (Math.Abs(u - 0.5f) <= half) return v <= 0.84f || Circle(u, v, 0.5f, 0.84f, half);
        }
        return Circle(u, v, 0.5f, 0.17f, 0.085f);
    }

    private static bool Question(float u, float v)
    {
        // Hook: a ring around (0.5, 0.66) without its lower-left quarter, then a stem and a dot.
        const float cx = 0.5f, cy = 0.66f;
        if (Ring(u, v, cx, cy, 0.19f, 0.065f))
        {
            double angle = Math.Atan2(v - cy, u - cx) * 180.0 / Math.PI; // -180..180
            if (!(angle < -100.0 && angle > -180.0) && !(angle > 150.0)) return true;
        }
        if (Segment(u, v, 0.5f, 0.47f, 0.5f, 0.35f) <= 0.065f) return true;
        return Circle(u, v, 0.5f, 0.15f, 0.08f);
    }

    private static bool Bulb(float u, float v)
    {
        if (Circle(u, v, 0.5f, 0.6f, 0.28f)) return true;
        // Neck narrowing to the screw base.
        if (v >= 0.3f && v <= 0.42f && Math.Abs(u - 0.5f) <= 0.13f + (v - 0.3f) * 0.5f) return true;
        // Screw base: two bands and a rounded cap.
        if (v >= 0.2f && v <= 0.3f && Math.Abs(u - 0.5f) <= 0.13f && !(v > 0.245f && v < 0.26f)) return true;
        return Circle(u, v, 0.5f, 0.17f, 0.075f);
    }

    // Teardrop: round bottom, pointed top.
    private static bool Drop(float u, float v)
    {
        const float cx = 0.5f, cy = 0.36f, r = 0.24f, tip = 0.9f;
        if (Circle(u, v, cx, cy, r)) return true;
        if (v < cy || v > tip) return false;
        float half = r * (1f - (v - cy) / (tip - cy));
        return Math.Abs(u - cx) <= half;
    }

    private static bool Cloud(float u, float v)
    {
        if (Circle(u, v, 0.32f, 0.5f, 0.17f) || Circle(u, v, 0.53f, 0.6f, 0.22f) || Circle(u, v, 0.72f, 0.48f, 0.15f)) return true;
        // Flat underside.
        return v >= 0.33f && v <= 0.5f && u >= 0.3f && u <= 0.75f;
    }

    // Four short curved strokes bowing toward the centre (the cartoon "vein" mark).
    private static bool AngerPuff(float u, float v)
    {
        for (int q = 0; q < 4; q++)
        {
            float sx = (q & 1) == 0 ? -1f : 1f;
            float sy = (q & 2) == 0 ? -1f : 1f;
            float x = (u - 0.5f) * sx, y = (v - 0.5f) * sy; // into this quadrant
            if (x < 0.05f || y < 0.05f || x > 0.42f || y > 0.42f) continue;
            if (Ring(x, y, 0.4f, 0.4f, 0.27f, 0.055f)) return true;
        }
        return false;
    }

    private static bool MusicNote(float u, float v)
    {
        // Tilted oval head.
        float hx = u - 0.36f, hy = v - 0.24f;
        const float cos = 0.906f, sin = 0.423f; // -25 degrees
        float rx = hx * cos + hy * sin, ry = -hx * sin + hy * cos;
        if ((rx * rx) / (0.15f * 0.15f) + (ry * ry) / (0.1f * 0.1f) <= 1f) return true;
        // Stem and flag.
        if (u >= 0.45f && u <= 0.51f && v >= 0.25f && v <= 0.86f) return true;
        return Segment(u, v, 0.48f, 0.84f, 0.72f, 0.62f) <= 0.05f || Segment(u, v, 0.72f, 0.62f, 0.7f, 0.5f) <= 0.04f;
    }
}
