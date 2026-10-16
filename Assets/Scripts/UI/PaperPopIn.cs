using UnityEngine;

/// <summary>
/// Small "lands on the page" entrance for a value tag or form row: each
/// time it is shown it scales up from slightly small with a little
/// overshoot, staggered by its place in the list, then rests at exactly its
/// original scale. Presentation only; layout groups ignore scale.
/// </summary>
[DisallowMultipleComponent]
public sealed class PaperPopIn : MonoBehaviour
{
    private const float Duration = 0.32f;
    private const float Stagger = 0.045f;

    private Vector3 rest = Vector3.one;
    private bool hasRest;
    private float t = -1f;
    private float delay;

    public static void Attach(Component target)
    {
        if (target == null) return;
        if (target.GetComponent<PaperPopIn>() == null) target.gameObject.AddComponent<PaperPopIn>();
    }

    private void OnEnable()
    {
        if (!hasRest)
        {
            rest = transform.localScale;
            hasRest = true;
        }
        delay = Mathf.Min(8, transform.GetSiblingIndex()) * Stagger;
        t = 0f;
        transform.localScale = rest * 0.86f;
    }

    private void OnDisable()
    {
        if (hasRest) transform.localScale = rest;
        t = -1f;
    }

    private void Update()
    {
        if (t < 0f) return;
        t += Time.unscaledDeltaTime;
        float k = Mathf.Clamp01((t - delay) / Duration);
        if (k <= 0f) return;
        // Ease-out-back for a soft overshoot.
        float x = k - 1f;
        float e = 1f + x * x * (2.2f * x + 1.2f);
        transform.localScale = rest * Mathf.LerpUnclamped(0.86f, 1f, e);
        if (k >= 1f)
        {
            transform.localScale = rest;
            t = -1f;
        }
    }
}
