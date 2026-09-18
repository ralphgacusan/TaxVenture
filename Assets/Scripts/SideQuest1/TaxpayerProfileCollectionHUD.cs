using System.Collections.Generic;
using UnityEngine;
using TMPro;

/// <summary>
/// PURPOSE:
/// The HUD icon + counter for collected taxpayer profiles. Shows "n/4"
/// on the icon. Clicking the icon no longer opens a flat UI list —
/// instead it toggles CollectedProfilesWorldPanel, which spawns the
/// physical, draggable 3D paper cards directly into the world.
///
/// CONNECTS WITH:
/// - TaxpayerProfileWorldPickup calls CollectProfile() on click
/// - CollectedProfilesWorldPanel.RegisterCollectedProfile() is notified
///   of every new profile so it has something to spawn once opened
/// - TownEvents.OnAllProfilesCollected fires once the 4th is collected
/// - Wire this component's OnHudIconClicked() to the HUD icon Button's
///   onClick in the Inspector.
/// </summary>
public class TaxpayerProfileCollectionHUD : MonoBehaviour
{
    public static TaxpayerProfileCollectionHUD Instance { get; private set; }

    [Header("HUD Icon")]
    [SerializeField] private TextMeshProUGUI countText;

    [Header("Settings")]
    [SerializeField] private int totalProfilesRequired = 4;

    private readonly List<TaxpayerProfile> collectedProfiles = new List<TaxpayerProfile>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[TaxpayerProfileCollectionHUD] Duplicate instance detected. Destroying duplicate.");
            Destroy(gameObject);
            return;
        }

        Instance = this;

        UpdateCountText();
    }

    public void CollectProfile(TaxpayerProfile profile)
    {
        if (profile == null || collectedProfiles.Contains(profile))
        {
            return;
        }

        collectedProfiles.Add(profile);

        if (CollectedProfilesWorldPanel.Instance != null)
        {
            CollectedProfilesWorldPanel.Instance.RegisterCollectedProfile(profile);
        }

        UpdateCountText();

        TownEvents.RaiseProfileCollected(profile.ProfileId);

        Debug.Log(
            $"[TaxpayerProfileCollectionHUD] Collected '{profile.TaxpayerName}'. " +
            $"{collectedProfiles.Count}/{totalProfilesRequired}"
        );

        if (collectedProfiles.Count >= totalProfilesRequired)
        {
            Debug.Log("[TaxpayerProfileCollectionHUD] All profiles collected!");
            TownEvents.RaiseAllProfilesCollected();
        }
    }

    /// <summary>
    /// Wire to the HUD icon Button's onClick in the Inspector. Delegates
    /// entirely to CollectedProfilesWorldPanel — this class no longer
    /// shows any UI of its own.
    /// </summary>
    public void OnHudIconClicked()
    {
        if (CollectedProfilesWorldPanel.Instance != null)
        {
            CollectedProfilesWorldPanel.Instance.OnHudIconClicked();
        }
        else
        {
            Debug.LogWarning("[TaxpayerProfileCollectionHUD] CollectedProfilesWorldPanel.Instance is NULL.");
        }
    }

    private void UpdateCountText()
    {
        if (countText != null)
        {
            int shownTotal = CollectedProfilesWorldPanel.Instance != null && CollectedProfilesWorldPanel.Instance.IsLockedToSingleProfile
                ? 1
                : totalProfilesRequired;

            int shownCount = CollectedProfilesWorldPanel.Instance != null
                ? CollectedProfilesWorldPanel.Instance.CollectedCount
                : collectedProfiles.Count;

            countText.text = $"{shownCount}/{shownTotal}";
        }
    }

    public IReadOnlyList<TaxpayerProfile> CollectedProfiles => collectedProfiles;

    public bool AllProfilesCollected => collectedProfiles.Count >= totalProfilesRequired;

    /// <summary>Call this after the side quest locks to a single profile, to refresh the "n/4" text into "1/1".</summary>
    public void RefreshCountDisplay()
    {
        UpdateCountText();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}