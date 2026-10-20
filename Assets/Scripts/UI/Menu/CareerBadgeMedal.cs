using UnityEngine;

/// <summary>
/// A side-quest medal hanging on the progress board. The face is the exact
/// Level 1 medal (HudBadgeArt: same hexagon, metal tier, emblem and ribbon),
/// drawn as a world sprite with a few darker layers behind it for thickness,
/// a soft tier-coloured glow when earned, a slow swing on its pin, and a
/// pop-and-spin when a new or better tier was earned since the last visit.
/// </summary>
public sealed class CareerBadgeMedal : MonoBehaviour
{
    private const int DepthLayers = 4;
    private const float PopDuration = 0.9f;

    private SpriteRenderer face;
    private readonly SpriteRenderer[] depth = new SpriteRenderer[DepthLayers];
    private SpriteRenderer glow;
    private Transform swing;
    private float size;
    private float phase;
    private float popElapsed = -1f;
    private float popDelay;
    private bool earned;
    private bool popSounded;

    public static CareerBadgeMedal Create(Transform parent, string name, Vector3 hangPoint, float sizePx)
    {
        Transform root = CareerBoardKit.Node(name, parent, hangPoint);
        var medal = root.gameObject.AddComponent<CareerBadgeMedal>();
        medal.Build(sizePx);
        return medal;
    }

    private void Build(float sizePx)
    {
        size = sizePx;
        phase = Random.Range(0f, Mathf.PI * 2f);
        // Swing around the hang point at the top of the ribbon.
        swing = CareerBoardKit.Node("Swing", transform, Vector3.zero);
        Vector3 centre = new Vector3(0f, -sizePx * 0.5f, 0f);
        glow = CareerBoardKit.Sprite(swing, "Glow", HudTheme.Circle, centre + new Vector3(0f, sizePx * 0.1f, 1.5f),
            sizePx * 1.25f, Color.clear, 1);
        for (int i = 0; i < DepthLayers; i++)
            depth[i] = CareerBoardKit.Sprite(swing, "Edge " + i, null, centre + new Vector3(0.6f * (i + 1), -0.8f * (i + 1), 0.8f * (i + 1)),
                sizePx, new Color(0.18f, 0.13f, 0.08f, 1f), 2);
        face = CareerBoardKit.Sprite(swing, "Medal", null, centre, sizePx, Color.white, 4);
    }

    /// <summary>Shows the medal art for this tier (0 = the dark "not earned" silhouette).</summary>
    public void SetBadge(string emblem, int tier, string ribbon)
    {
        BadgeTier badgeTier = (BadgeTier)Mathf.Clamp(tier, 0, PlayerProgressData.MaxBadgeTier);
        Sprite sprite = HudBadgeArt.Get(emblem, badgeTier, ribbon);
        earned = tier > 0;
        face.sprite = sprite;
        Rescale(face);
        foreach (SpriteRenderer layer in depth)
        {
            layer.sprite = sprite;
            Rescale(layer);
        }
        Color tierGlow = HudBadgeArt.TierColor(badgeTier);
        glow.color = earned ? new Color(tierGlow.r, tierGlow.g, tierGlow.b, 0.32f) : Color.clear;
        face.color = earned ? Color.white : new Color(1f, 1f, 1f, 0.92f);
    }

    /// <summary>Pop and spin (new or upgraded badge).</summary>
    public void Celebrate(float delay)
    {
        popDelay = Mathf.Max(0f, delay);
        popElapsed = 0f;
        popSounded = false;
    }

    private void Rescale(SpriteRenderer renderer)
    {
        if (renderer.sprite == null) return;
        float native = Mathf.Max(renderer.sprite.bounds.size.x, renderer.sprite.bounds.size.y);
        renderer.transform.localScale = Vector3.one * (size / Mathf.Max(1e-4f, native));
    }

    private void Update()
    {
        float time = Time.unscaledTime;
        float swingAngle = Mathf.Sin(time * 1.6f + phase) * (earned ? 3f : 1.5f);
        float scale = 1f, spin = 0f;
        if (popElapsed >= 0f)
        {
            popElapsed += Time.unscaledDeltaTime;
            float t = (popElapsed - popDelay) / PopDuration;
            if (t < 0f) scale = 0.001f;
            else if (t >= 1f)
            {
                popElapsed = -1f;
            }
            else
            {
                scale = HudTheme.EaseOutBack(Mathf.Clamp01(t * 1.6f));
                spin = 360f * HudTheme.EaseOutCubic(t);
                if (!popSounded && AudioManager.Instance != null) AudioManager.Instance.PlayEffect("sfx_reward_badge_01", 0.7f);
                popSounded = true;
            }
        }
        swing.localRotation = Quaternion.Euler(0f, spin, swingAngle);
        swing.localScale = Vector3.one * scale;
        if (glow != null && earned)
        {
            float breathe = 1f + 0.06f * Mathf.Sin(time * 2.4f + phase);
            glow.transform.localScale = Vector3.one * (size * 1.25f / Mathf.Max(1e-4f, glow.sprite.bounds.size.x)) * breathe;
        }
    }
}
