using UnityEngine;

/// <summary>
/// PURPOSE:
/// The upper-right Settings button in scenes without a SettingsUI (Office
/// Level 2). Opens the in-game 3D settings board (InGameSettings), which
/// pauses the game while it is open.
/// </summary>
public class SettingsButtonPlaceholder : MonoBehaviour
{
    public void OnSettingsPressed()
    {
        if (InGameSettings.TryOpen()) return;
        Debug.Log("[SettingsButtonPlaceholder] No in-game settings in this scene (add it to Resources/Settings/in_game_settings.json).");
    }
}
