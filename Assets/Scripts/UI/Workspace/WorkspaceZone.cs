using UnityEngine;

/// <summary>
/// PURPOSE:
/// Represents a defined half of the workspace layout (Left or Right).
/// Enforces that only ONE panel is visible within this zone at a time.
/// All child panels are automatically hidden on startup, so panels only
/// become visible when ShowPanel() is called.
/// </summary>
public class WorkspaceZone : MonoBehaviour
{
    private GameObject currentlyShownPanel;

    private void Awake()
    {
        // Hide every panel under this workspace zone at startup.
        foreach (Transform child in transform)
        {
            child.gameObject.SetActive(false);
        }

        currentlyShownPanel = null;
    }

    /// <summary>
    /// Shows the given panel in this zone, hiding whatever was previously
    /// shown here (if different).
    /// </summary>
    public void ShowPanel(GameObject panel)
    {
        if (currentlyShownPanel != null && currentlyShownPanel != panel)
        {
            currentlyShownPanel.SetActive(false);
        }

        currentlyShownPanel = panel;
        currentlyShownPanel.SetActive(true);
    }

    /// <summary>
    /// Hides the specified panel if it is currently shown.
    /// </summary>
    public void HidePanel(GameObject panel)
    {
        if (currentlyShownPanel == panel)
        {
            currentlyShownPanel.SetActive(false);
            currentlyShownPanel = null;
        }
    }

    /// <summary>
    /// Hides whichever panel is currently shown in this workspace zone.
    /// </summary>
    public void HideCurrent()
    {
        if (currentlyShownPanel != null)
        {
            currentlyShownPanel.SetActive(false);
            currentlyShownPanel = null;
        }
    }
}