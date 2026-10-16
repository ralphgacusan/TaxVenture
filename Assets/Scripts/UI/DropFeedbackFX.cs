using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Presentation-only "juice" for a value drop on a field: a check or cross
/// badge pops in at the field's right edge, an accepted drop gives a small
/// scale punch, a rejected drop gives a quick head-shake wobble, and the
/// field's edge glows in the result colour. Scale and rotation are used
/// (never position), so layout groups are not disturbed. Everything resets
/// on completion or disable.
/// </summary>
[DisallowMultipleComponent]
public sealed class DropFeedbackFX : MonoBehaviour
{
    private const float MotionDuration = 0.42f;
    private const float BadgeDuration = 0.95f;
    private const float BadgeSize = 46f;

    private Coroutine routine;
    private bool playing;
    private Vector3 restScale;
    private Quaternion restRotation;

    private Outline edge;
    private bool edgeWasEnabled;
    private Color edgeRestColor;
    private Vector2 edgeRestDistance;

    private RectTransform badge;
    private Image badgeFill;
    private CanvasGroup badgeGroup;
    private GameObject checkGlyph;
    private GameObject crossGlyph;

    public static DropFeedbackFX For(Component target)
    {
        DropFeedbackFX fx = target.GetComponent<DropFeedbackFX>();
        return fx != null ? fx : target.gameObject.AddComponent<DropFeedbackFX>();
    }

    public void Play(bool correct, Color color)
    {
        Restore();
        if (!isActiveAndEnabled)
            return;

        restScale = transform.localScale;
        restRotation = transform.localRotation;
        playing = true;

        edge = GetComponent<Outline>();
        if (edge != null)
        {
            edgeWasEnabled = edge.enabled;
            edgeRestColor = edge.effectColor;
            edgeRestDistance = edge.effectDistance;
        }
        else
        {
            edge = gameObject.AddComponent<Outline>();
            edgeWasEnabled = false;
            edgeRestColor = Color.clear;
            edgeRestDistance = new Vector2(2f, -2f);
        }

        EnsureBadge();
        badgeFill.color = color;
        checkGlyph.SetActive(correct);
        crossGlyph.SetActive(!correct);
        badge.gameObject.SetActive(true);

        routine = StartCoroutine(Animate(correct, color));
    }

    public void Restore()
    {
        if (routine != null)
        {
            StopCoroutine(routine);
            routine = null;
        }

        if (playing)
        {
            transform.localScale = restScale;
            transform.localRotation = restRotation;
            if (edge != null)
            {
                edge.effectColor = edgeRestColor;
                edge.effectDistance = edgeRestDistance;
                edge.enabled = edgeWasEnabled;
            }
            playing = false;
        }

        if (badge != null)
            badge.gameObject.SetActive(false);
    }

    private void OnDisable()
    {
        Restore();
    }

    private IEnumerator Animate(bool correct, Color color)
    {
        edge.enabled = true;
        edge.effectDistance = new Vector2(4f, -4f);

        for (float elapsed = 0f; elapsed < BadgeDuration; elapsed += Time.unscaledDeltaTime)
        {
            float t = elapsed / BadgeDuration;
            float m = Mathf.Clamp01(elapsed / MotionDuration);

            if (correct)
            {
                // Quick pop that settles: reads as "accepted".
                float punch = Mathf.Sin(m * Mathf.PI) * (1f - m * 0.4f);
                transform.localScale = restScale * (1f + 0.07f * punch);
                transform.localRotation = restRotation;
            }
            else
            {
                // Decaying side-to-side wobble: reads as "no".
                float wobble = Mathf.Sin(m * Mathf.PI * 6f) * (1f - m);
                transform.localRotation = restRotation * Quaternion.Euler(0f, 0f, 3.5f * wobble);
                transform.localScale = restScale * (1f - 0.03f * Mathf.Sin(m * Mathf.PI));
            }

            // Edge glow fades with the badge.
            Color glow = color;
            glow.a = Mathf.Lerp(0.95f, 0f, Mathf.SmoothStep(0f, 1f, t));
            edge.effectColor = glow;

            // Badge: pop in with overshoot, hold, fade out.
            float pop = Mathf.Clamp01(elapsed / 0.2f);
            float overshoot = 1f + 0.25f * Mathf.Sin(pop * Mathf.PI);
            badge.localScale = Vector3.one * Mathf.Lerp(0.2f, 1f, DeskMotion.EaseOutCubic(pop)) * overshoot;
            badgeGroup.alpha = t < 0.7f ? 1f : 1f - (t - 0.7f) / 0.3f;

            yield return null;
        }

        routine = null;
        Restore();
    }

    private void EnsureBadge()
    {
        if (badge != null)
            return;

        GameObject go = new GameObject("DropFeedbackBadge", typeof(RectTransform), typeof(CanvasGroup),
            typeof(LayoutElement));
        go.layer = gameObject.layer;
        badge = (RectTransform)go.transform;
        badge.SetParent(transform, false);
        badge.SetAsLastSibling();
        badge.anchorMin = badge.anchorMax = new Vector2(1f, 0.5f);
        badge.pivot = new Vector2(0.5f, 0.5f);
        badge.sizeDelta = new Vector2(BadgeSize, BadgeSize);
        badge.anchoredPosition = new Vector2(-BadgeSize * 0.5f - 6f, 0f);
        go.GetComponent<LayoutElement>().ignoreLayout = true;

        badgeGroup = go.GetComponent<CanvasGroup>();
        badgeGroup.blocksRaycasts = false;
        badgeGroup.interactable = false;

        badgeFill = NewBar(badge, "Fill", Vector2.zero, new Vector2(BadgeSize, BadgeSize), 0f, Color.white);
        Outline ring = badgeFill.gameObject.AddComponent<Outline>();
        ring.effectColor = new Color(1f, 1f, 1f, 0.9f);
        ring.effectDistance = new Vector2(3f, -3f);

        checkGlyph = new GameObject("Check", typeof(RectTransform));
        checkGlyph.transform.SetParent(badge, false);
        NewBar((RectTransform)checkGlyph.transform, "Short", new Vector2(-7f, -3f), new Vector2(16f, 6f), 45f, Color.white);
        NewBar((RectTransform)checkGlyph.transform, "Long", new Vector2(5f, 2f), new Vector2(28f, 6f), -50f, Color.white);

        crossGlyph = new GameObject("Cross", typeof(RectTransform));
        crossGlyph.transform.SetParent(badge, false);
        NewBar((RectTransform)crossGlyph.transform, "A", Vector2.zero, new Vector2(28f, 6f), 45f, Color.white);
        NewBar((RectTransform)crossGlyph.transform, "B", Vector2.zero, new Vector2(28f, 6f), -45f, Color.white);

        badge.gameObject.SetActive(false);
    }

    private static Image NewBar(RectTransform parent, string name, Vector2 position, Vector2 size, float angle,
        Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.layer = parent.gameObject.layer;
        RectTransform rect = (RectTransform)go.transform;
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
        rect.localRotation = Quaternion.Euler(0f, 0f, angle);
        Image image = go.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return image;
    }
}
