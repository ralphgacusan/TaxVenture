using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Tracks an interactable for the screen-space outline renderer feature.
/// This component never changes renderer materials.
/// </summary>
public class HighlightEffect : MonoBehaviour
{
    private const float FadeDuration = 0.12f;

    private static readonly List<HighlightEffect> activeEffects = new List<HighlightEffect>();
    private static readonly IReadOnlyList<HighlightEffect> activeEffectsView = activeEffects.AsReadOnly();

    public static IReadOnlyList<HighlightEffect> ActiveEffects => activeEffectsView;

    public Renderer[] TargetRenderers { get; private set; } = new Renderer[0];

    public float Strength { get; private set; }

    private bool focused;
    private bool requested;
    private bool affordance;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetActiveEffects()
    {
        activeEffects.Clear();
    }

    public static HighlightEffect For(IInteractable target)
    {
        return For(target as Component);
    }

    public static HighlightEffect For(Component target)
    {
        if (target == null) return null;

        HighlightEffect effect = target.GetComponent<HighlightEffect>();
        if (effect == null)
            effect = target.gameObject.AddComponent<HighlightEffect>();
        else if (!effect.enabled)
            effect.enabled = true;

        return effect;
    }

    private void Awake()
    {
        CacheTargetRenderers();
    }

    private void OnEnable()
    {
        if (IsWanted) Register();
    }

    private void OnDisable()
    {
        // Focus can become stale while its interactable is disabled. A legacy
        // request can also become stale while disabled, so clear both owners.
        focused = false;
        requested = false;
        affordance = false;
        Strength = 0f;
        Unregister();
    }

    private void OnDestroy()
    {
        Unregister();
    }

    private void Update()
    {
        if (IsWanted) Register();

        float target = IsWanted ? 1f : 0f;
        if (Mathf.Approximately(Strength, target))
        {
            if (target == 0f) Unregister();
            return;
        }

        Strength = Mathf.MoveTowards(Strength, target, Time.unscaledDeltaTime / FadeDuration);
        if (Strength == 0f && target == 0f) Unregister();
    }

    /// <summary>Sets transient focus ownership, independently of legacy requests.</summary>
    public void SetFocused(bool value)
    {
        focused = value;
        RefreshRegistration();
    }

    /// <summary>Requests a persistent highlight until Unhighlight is called.</summary>
    public void Highlight()
    {
        requested = true;
        if (!enabled) enabled = true;
        RefreshRegistration();
    }

    public void Unhighlight()
    {
        requested = false;
        RefreshRegistration();
    }

    /// <summary>Persistent interaction hint, independent of hover and legacy highlight requests.</summary>
    public void SetAffordance(bool value)
    {
        affordance = value;
        if (value && !enabled) enabled = true;
        RefreshRegistration();
    }

    /// <summary>Refresh after a model is spawned or replaced beneath this interaction root.</summary>
    public void RefreshTargets() => CacheTargetRenderers();

    private bool IsWanted => focused || requested || affordance;

    private void RefreshRegistration()
    {
        if (IsWanted && isActiveAndEnabled)
            Register();
        // Keep the entry while fading out; Update removes it at zero.
    }

    private void Register()
    {
        if (!activeEffects.Contains(this)) activeEffects.Add(this);
    }

    private void Unregister()
    {
        activeEffects.Remove(this);
    }

    private void CacheTargetRenderers()
    {
        Renderer[] candidates = GetComponentsInChildren<Renderer>(true);
        var eligible = new List<Renderer>(candidates.Length);

        foreach (Renderer candidate in candidates)
        {
            if (!(candidate is MeshRenderer) && !(candidate is SkinnedMeshRenderer)) continue;
            if (candidate.GetComponent<TMP_Text>() != null) continue;
            if (IsExcludedLayer(candidate.transform)) continue;
            if (BelongsToNestedInteractiveObject(candidate.transform)) continue;
            eligible.Add(candidate);
        }

        TargetRenderers = eligible.ToArray();
    }

    private bool IsExcludedLayer(Transform candidate)
    {
        if (candidate.GetComponentInParent<Canvas>(true) != null) return true;

        int uiLayer = LayerMask.NameToLayer("UI");

        for (Transform current = candidate; current != null; current = current.parent)
        {
            if (current.gameObject.layer == uiLayer)
                return true;
            if (current == transform) break;
        }

        return false;
    }

    private bool BelongsToNestedInteractiveObject(Transform candidate)
    {
        for (Transform current = candidate; current != null && current != transform; current = current.parent)
        {
            MonoBehaviour[] behaviours = current.GetComponents<MonoBehaviour>();
            foreach (MonoBehaviour behaviour in behaviours)
            {
                if (behaviour is IInteractable ||
                    behaviour is IPointerClickHandler ||
                    behaviour is IPointerDownHandler ||
                    behaviour is IBeginDragHandler ||
                    behaviour is IDragHandler)
                    return true;
            }
        }

        return false;
    }
}
