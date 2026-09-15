using UnityEngine;

/// <summary>
/// Controls the highlight effect of a workstation item.
/// </summary>
[RequireComponent(typeof(HighlightEffect))]
public class DeskItemHighlight : MonoBehaviour
{
    private HighlightEffect highlight;

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

    public void ShowHighlight()
    {
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

        highlight.Highlight();
    }

    public void HideHighlight()
    {
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

        highlight.Unhighlight();
    }
}