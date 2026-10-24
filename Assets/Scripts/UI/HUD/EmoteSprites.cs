using UnityEngine;

/// <summary>
/// PURPOSE:
/// Runtime sprites for the emote icons (T22), drawn once from EmoteShapes:
/// a white fill (tinted per emotion) and a slightly larger white outline of
/// the same shape (tinted dark for the soft outline, black for the drop
/// shadow). No art files; built on first use and kept for the session.
/// </summary>
public static class EmoteSprites
{
    private const int Size = 96;
    private const int Samples = 3;
    private const int OutlinePixels = 5;

    private static readonly int shapeCount = System.Enum.GetValues(typeof(EmoteShape)).Length;
    private static Sprite[] fills;
    private static Sprite[] outlines;
    private static Sprite dot, dotOutline, glow;

    public static Sprite Fill(EmoteShape shape)
    {
        EnsureShape(shape);
        return fills[(int)shape];
    }

    public static Sprite Outline(EmoteShape shape)
    {
        EnsureShape(shape);
        return outlines[(int)shape];
    }

    /// <summary>One thinking dot (and the cloud's falling raindrop uses the sweat drop).</summary>
    public static Sprite Dot
    {
        get
        {
            if (dot == null) Build(EmoteShapes.SingleDot, "EmoteDot", out dot, out dotOutline);
            return dot;
        }
    }

    public static Sprite DotOutline
    {
        get
        {
            if (dotOutline == null) Build(EmoteShapes.SingleDot, "EmoteDot", out dot, out dotOutline);
            return dotOutline;
        }
    }

    /// <summary>Soft round glow (behind the light bulb).</summary>
    public static Sprite Glow
    {
        get
        {
            if (glow == null)
            {
                var alpha = new float[Size * Size];
                for (int y = 0; y < Size; y++)
                    for (int x = 0; x < Size; x++)
                        alpha[y * Size + x] = EmoteShapes.Glow((x + 0.5f) / Size, (y + 0.5f) / Size);
                glow = ToSprite(alpha, "EmoteGlow");
            }
            return glow;
        }
    }

    private static void EnsureShape(EmoteShape shape)
    {
        if (fills == null)
        {
            fills = new Sprite[shapeCount];
            outlines = new Sprite[shapeCount];
        }
        int i = (int)shape;
        if (fills[i] != null) return;
        Build((u, v) => EmoteShapes.Covers(shape, u, v), "Emote" + shape, out fills[i], out outlines[i]);
    }

    private static void Build(System.Func<float, float, bool> inside, string name, out Sprite fill, out Sprite outline)
    {
        var coverage = new float[Size * Size];
        const float step = 1f / Samples;
        for (int y = 0; y < Size; y++)
        {
            for (int x = 0; x < Size; x++)
            {
                int hits = 0;
                for (int sy = 0; sy < Samples; sy++)
                    for (int sx = 0; sx < Samples; sx++)
                        if (inside((x + (sx + 0.5f) * step) / Size, (y + (sy + 0.5f) * step) / Size)) hits++;
                coverage[y * Size + x] = hits / (float)(Samples * Samples);
            }
        }

        // Outline: the coverage grown by a disc (a max filter), so the rim keeps its soft edge.
        var grown = new float[Size * Size];
        const int r = OutlinePixels;
        for (int y = 0; y < Size; y++)
        {
            for (int x = 0; x < Size; x++)
            {
                float best = 0f;
                for (int dy = -r; dy <= r && best < 1f; dy++)
                {
                    int yy = y + dy;
                    if (yy < 0 || yy >= Size) continue;
                    for (int dx = -r; dx <= r; dx++)
                    {
                        int xx = x + dx;
                        if (xx < 0 || xx >= Size || dx * dx + dy * dy > r * r) continue;
                        float c = coverage[yy * Size + xx];
                        if (c > best) best = c;
                    }
                }
                grown[y * Size + x] = best;
            }
        }

        fill = ToSprite(coverage, name);
        outline = ToSprite(grown, name + "Outline");
    }

    private static Sprite ToSprite(float[] alpha, string name)
    {
        var tex = new Texture2D(Size, Size, TextureFormat.RGBA32, false)
        {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear,
            name = name
        };
        var pixels = new Color32[Size * Size];
        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(Mathf.Clamp01(alpha[i]) * 255f));
        tex.SetPixels32(pixels);
        tex.Apply(false, true);
        Sprite sprite = Sprite.Create(tex, new Rect(0, 0, Size, Size), new Vector2(0.5f, 0.5f), 100f);
        sprite.name = name;
        return sprite;
    }
}
