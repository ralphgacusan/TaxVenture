using UnityEngine;

/// <summary>
/// PURPOSE:
/// While a reward or summary card is up (Audit Summary, Side Quest Complete,
/// Level Complete, Case Result: anything shown through HudRewardScreen),
/// the player cannot walk, look around or interact with the world. Uses the
/// shared PlayerControlHold, so it combines safely with other holders.
/// Built by OfficeHudRuntime.
/// </summary>
public sealed class HudModalControlHold : MonoBehaviour
{
    private bool holding;

    private void LateUpdate()
    {
        bool modal = HudRewardScreen.AnyOpen ||
                     (CaseResultPopupUI.Instance != null && CaseResultPopupUI.Instance.IsVisible);
        if (modal != holding)
        {
            holding = modal;
            if (modal) PlayerControlHold.Acquire(this);
            else PlayerControlHold.Release(this);
        }
        PlayerControlHold.Refresh();
    }

    private void OnDisable()
    {
        if (!holding) return;
        holding = false;
        PlayerControlHold.Release(this);
    }
}
