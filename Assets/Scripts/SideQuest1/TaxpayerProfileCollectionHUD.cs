using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

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
    private CanvasGroup iconGroup;
    private Button iconButton;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[TaxpayerProfileCollectionHUD] Duplicate instance detected. Destroying duplicate.");
            Destroy(gameObject);
            return;
        }

        Instance = this;

        iconButton = GetComponent<Button>();
        iconGroup = GetComponent<CanvasGroup>();
        if (iconGroup == null) iconGroup = gameObject.AddComponent<CanvasGroup>();

        UpdateCountText();
    }

    private void Start()
    {
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
    /// Resume (T04): a profile collected in the saved run is back in the
    /// collection and the 3D profile viewer, without the pickup sound or the
    /// collection events (nothing is awarded twice).
    /// </summary>
    public void RestoreCollected(TaxpayerProfile profile)
    {
        if (profile == null || collectedProfiles.Contains(profile)) return;
        collectedProfiles.Add(profile);
        if (CollectedProfilesWorldPanel.Instance != null)
            CollectedProfilesWorldPanel.Instance.RegisterCollectedProfile(profile);
        UpdateCountText();
    }

    public bool TryGetCollectionScreenPoint(out Vector2 screenPoint)
    {
        screenPoint = default;

        // T10: the binder's hotbar slot, the slot it will take, or the Bag icon.
        Vector2? landing = CollectionPointOverride != null ? CollectionPointOverride() : null;
        if (landing.HasValue)
        {
            screenPoint = landing.Value;
            return true;
        }

        RectTransform icon = transform as RectTransform;
        if (!isActiveAndEnabled || !gameObject.activeInHierarchy || icon == null || !icon.gameObject.activeInHierarchy)
        {
            return false;
        }

        Canvas canvas = icon.GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            return false;
        }

        Canvas rootCanvas = canvas.rootCanvas;
        if (rootCanvas == null || !rootCanvas.isActiveAndEnabled)
        {
            return false;
        }

        for (Transform parent = icon; parent != null; parent = parent.parent)
        {
            CanvasGroup group = parent.GetComponent<CanvasGroup>();
            if (group != null && group.alpha <= 0.001f)
            {
                return false;
            }

            if (parent == rootCanvas.transform)
            {
                break;
            }
        }

        CanvasRenderer renderer = icon.GetComponent<CanvasRenderer>();
        if ((renderer != null && renderer.cull) || icon.rect.width <= 0f || icon.rect.height <= 0f)
        {
            return false;
        }

        Camera canvasCamera = rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : rootCanvas.worldCamera;
        if (rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay && canvasCamera == null)
        {
            return false;
        }

        screenPoint = RectTransformUtility.WorldToScreenPoint(canvasCamera, icon.TransformPoint(icon.rect.center));
        return !float.IsNaN(screenPoint.x) && !float.IsInfinity(screenPoint.x)
            && !float.IsNaN(screenPoint.y) && !float.IsInfinity(screenPoint.y);
    }

    /// <summary>
    /// Wire to the HUD icon Button's onClick in the Inspector. Delegates
    /// entirely to CollectedProfilesWorldPanel — this class no longer
    /// shows any UI of its own.
    /// </summary>
    public void OnHudIconClicked()
    {
        // Office HUD: the profiles open as paper windows instead of the 3D
        // viewer, so they layer with Notes and the Case Folder.
        if (IconClickOverride != null && IconClickOverride())
        {
            return;
        }

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
        int shownCount = CollectedProfilesWorldPanel.Instance != null
            ? CollectedProfilesWorldPanel.Instance.CollectedCount
            : collectedProfiles.Count;
        bool available = shownCount > 0;
        if (iconGroup != null) iconGroup.alpha = available ? 1f : 0.4f;
        if (iconButton != null) iconButton.interactable = available;

        if (countText != null)
        {
            int shownTotal = CollectedProfilesWorldPanel.Instance != null && CollectedProfilesWorldPanel.Instance.IsLockedToSingleProfile
                ? 1
                : totalProfilesRequired;

            countText.text = $"{shownCount}/{shownTotal}";
        }
    }

    public IReadOnlyList<TaxpayerProfile> CollectedProfiles => collectedProfiles;

    /// <summary>
    /// Optional handler for the HUD icon tap (set by the Office HUD).
    /// Returns true when it handled the tap.
    /// </summary>
    public static System.Func<bool> IconClickOverride;

    /// <summary>
    /// Optional landing point for profiles flying in (set by the Level 1 HUD
    /// hotbar, T10): the Profile Binder's slot, the slot it will take, or the
    /// Bag icon. Null result = this icon, as before.
    /// </summary>
    public static System.Func<Vector2?> CollectionPointOverride;

    public bool AllProfilesCollected => collectedProfiles.Count >= totalProfilesRequired;

    /// <summary>How many profiles the plaza step needs (read-only, for HUD objectives).</summary>
    public int RequiredProfiles => totalProfilesRequired;

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
