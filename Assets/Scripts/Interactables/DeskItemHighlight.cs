using UnityEngine;

/// <summary>
/// Controls the highlight effect of a workstation item.
/// </summary>
[RequireComponent(typeof(HighlightEffect))]
public class DeskItemHighlight : MonoBehaviour
{
    private HighlightEffect highlight;
    private bool showRequested;

    private void Awake()
    {
        highlight = GetComponent<HighlightEffect>();

        if (highlight == null)
        {
            Debug.LogError(
                "[DeskItemHighlight] HighlightEffect is missing on: "
                + gameObject.name,
                this
            );
        }
    }

    private void OnEnable()
    {
        if (showRequested)
            ShowHighlight();
    }

    public void ShowHighlight()
    {
        showRequested = true;
        highlight = HighlightEffect.For(this);

        if (highlight == null)
        {
            Debug.LogWarning(
                "[DeskItemHighlight] Cannot show highlight. "
                + "HighlightEffect is NULL on: "
                + gameObject.name,
                this
            );

            return;
        }

        highlight.RefreshTargets();
        highlight.SetAffordance(true);
    }

    public void HideHighlight()
    {
        showRequested = false;

        if (highlight == null)
        {
            Debug.LogWarning(
                "[DeskItemHighlight] Cannot hide highlight. "
                + "HighlightEffect is NULL on: "
                + gameObject.name,
                this
            );

            return;
        }

        highlight.SetAffordance(false);
    }
}
