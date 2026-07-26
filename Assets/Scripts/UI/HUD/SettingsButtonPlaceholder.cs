using UnityEngine;

/// <summary>
/// PURPOSE:
/// Placeholder for the upper-right Settings button. No functionality yet —
/// exists purely so the HUD layout is complete and the button is wired to
/// something, avoiding an empty/unwired button in the hierarchy.
/// </summary>
public class SettingsButtonPlaceholder : MonoBehaviour
{
    public void OnSettingsPressed()
    {
        Debug.Log("[SettingsButtonPlaceholder] Settings clicked — no functionality implemented yet.");
    }
}