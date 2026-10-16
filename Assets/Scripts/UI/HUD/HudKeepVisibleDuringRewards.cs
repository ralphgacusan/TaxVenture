using UnityEngine;

/// <summary>
/// Marker: a root canvas with this component is NOT hidden by HudRewardScreen
/// while a reward/summary card is open (used by the level guide, so Arthur can
/// talk the player through the card).
/// </summary>
public sealed class HudKeepVisibleDuringRewards : MonoBehaviour
{
}
