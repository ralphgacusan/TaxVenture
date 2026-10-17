using UnityEngine;

/// <summary>
/// PURPOSE:
/// Small motion for a stamp impression (ReviewInkMark): Pop = the ink lands
/// (scales in from 1.35 and fades in over ~0.18 s); FadeOut = a rejected
/// press leaves a faint impression that fades away, then is removed.
/// Presentation only; unscaled time so pauses do not freeze it mid-way.
/// </summary>
public sealed class ReviewInkMotion : MonoBehaviour
{
    public enum Mode { Pop, FadeOut }

    private const float PopSeconds = 0.18f;
    private const float FadeHold = 0.35f;
    private const float FadeSeconds = 0.6f;

    private Mode mode;
    private float started;
    private CanvasGroup group;
    private Vector3 baseScale;

    public static void Play(GameObject mark, Mode mode)
    {
        if (mark == null) return;
        // Unity's fake null: never use ?? with GetComponent (the Editor returns a
        // placeholder for a missing component, so the AddComponent would be skipped).
        ReviewInkMotion motion = mark.GetComponent<ReviewInkMotion>();
        if (motion == null) motion = mark.AddComponent<ReviewInkMotion>();
        motion.mode = mode;
        motion.started = Time.unscaledTime;
        motion.group = mark.GetComponent<CanvasGroup>();
        if (motion.group == null) motion.group = mark.AddComponent<CanvasGroup>();
        motion.group.blocksRaycasts = false;
        motion.group.interactable = false;
        motion.baseScale = mark.transform.localScale;
        motion.Update();
    }

    private void Update()
    {
        float t = Time.unscaledTime - started;
        if (mode == Mode.Pop)
        {
            float k = Mathf.Clamp01(t / PopSeconds);
            float e = 1f - (1f - k) * (1f - k);
            transform.localScale = baseScale * Mathf.Lerp(1.35f, 1f, e);
            group.alpha = Mathf.Lerp(0f, 1f, Mathf.Clamp01(k * 1.6f));
            if (k >= 1f) Destroy(this);
            return;
        }

        float f = Mathf.Clamp01((t - FadeHold) / FadeSeconds);
        group.alpha = Mathf.Lerp(0.55f, 0f, f);
        if (f >= 1f) Destroy(gameObject);
    }
}
