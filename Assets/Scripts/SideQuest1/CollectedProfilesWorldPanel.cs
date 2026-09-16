using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// PURPOSE:
/// Replaces the old flat UI panel in TaxpayerProfileCollectionHUD with a
/// set of physical, draggable 3D "paper" cards — one per collected
/// taxpayer profile. Clicking the HUD icon spawns/shows all of them at
/// once, positioned around the camera so the player can see and drag
/// each one anywhere on screen; clicking again hides/despawns them all
/// together.
///
/// Reuses the same paper prefab as the corkboard/pickup cards (must have
/// a Collider + a FloatingModel3D component + name/description TMP
/// fields), so visuals stay identical everywhere in the game.
///
/// AFTER THE SIDEQUEST ENDS:
/// Only one profile should remain browsable this way. Assign that single
/// surviving profile's source (whichever TaxpayerProfileWorldPickup or
/// NPC interactable owns it) to "survivingProfileSource" in the
/// Inspector, and call LockToSingleSurvivingProfile() once the sidequest
/// completes (e.g. from TutorialNpcController.HandleSideQuestCompleted or
/// SideQuestCompleteUI.OnContinuePressed). After that call, opening the
/// icon will only ever show that one paper, regardless of what was
/// collected before.
/// </summary>
public class CollectedProfilesWorldPanel : MonoBehaviour
{
    public static CollectedProfilesWorldPanel Instance { get; private set; }

    [Header("Setup")]
    [Tooltip("The camera the papers should arrange themselves in front of.")]
    [SerializeField] private Camera mainCamera;

    [Tooltip("Prefab with Collider + FloatingModel3D + name/description TMP text fields (e.g. ProfileWorldPickup_TEMP).")]
    [SerializeField] private GameObject profilePaperPrefab;

    [Tooltip("Parent to spawn papers under. Purely organizational, can be an empty GameObject.")]
    [SerializeField] private Transform paperParent;

    [Header("Layout")]
    [Tooltip("Distance in front of the camera the papers appear at.")]
    [SerializeField] private float spawnDistance = 1.2f;

    [Tooltip("Horizontal spacing between papers when spawned in a row.")]
    [SerializeField] private float horizontalSpacing = 0.6f;

    [Tooltip("Small random offset applied per paper so a fanned-out stack doesn't look too perfectly aligned.")]
    [SerializeField] private float scatterAmount = 0.05f;

    [Header("Restriction After Side Quest")]
    [Tooltip(
        "Optional. Assign the ONE TaxpayerProfileWorldPickup (or any " +
        "component exposing a TaxpayerProfile) that should be the only " +
        "profile browsable after the side quest ends. Leave empty until " +
        "the side quest actually completes, or assign ahead of time and " +
        "just call LockToSingleSurvivingProfile() when ready."
    )]
    [SerializeField] private TaxpayerProfileWorldPickup survivingProfileSource;

    private readonly List<TaxpayerProfile> collectedProfiles = new List<TaxpayerProfile>();
    private readonly List<GameObject> spawnedPapers = new List<GameObject>();

    private bool isOpen;
    private bool isLockedToSingleProfile;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[CollectedProfilesWorldPanel] Duplicate instance detected. Destroying duplicate.");
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }
    }

    /// <summary>
    /// Mirrors TaxpayerProfileCollectionHUD.CollectProfile — call this
    /// alongside (or instead of, depending on how you wire it) the HUD's
    /// own collection call so this panel knows what to spawn later.
    /// </summary>
    public void RegisterCollectedProfile(TaxpayerProfile profile)
    {
        if (profile == null || collectedProfiles.Contains(profile))
        {
            return;
        }

        collectedProfiles.Add(profile);

        // If the panel happens to be open while a new profile comes in,
        // refresh immediately so it appears without needing a re-toggle.
        if (isOpen)
        {
            RespawnPapers();
        }
    }

    /// <summary>Wire this to the same HUD icon Button's onClick.</summary>
    public void OnHudIconClicked()
    {
        isOpen = !isOpen;

        if (isOpen)
        {
            // Play paper SFX when the profile papers are opened.
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayPaperSFX();
            }

            RespawnPapers();
        }
        else
        {
            DespawnAllPapers();
        }
    }

    /// <summary>
    /// Call this once the corkboard side quest completes. From then on,
    /// only survivingProfileSource's profile will ever be shown/spawned,
    /// regardless of how many were collected before.
    /// </summary>
    public void LockToSingleSurvivingProfile()
    {
        isLockedToSingleProfile = true;

        if (isOpen)
        {
            RespawnPapers();
        }

        if (TaxpayerProfileCollectionHUD.Instance != null)
        {
            TaxpayerProfileCollectionHUD.Instance.RefreshCountDisplay();
        }
    }

    /// <summary>
    /// Overload in case you'd rather pass the surviving source at call
    /// time instead of pre-assigning it in the Inspector.
    /// </summary>
    public void LockToSingleSurvivingProfile(TaxpayerProfileWorldPickup source)
    {
        survivingProfileSource = source;
        LockToSingleSurvivingProfile();
    }

    private void RespawnPapers()
    {
        DespawnAllPapers();

        if (profilePaperPrefab == null || mainCamera == null)
        {
            Debug.LogError("[CollectedProfilesWorldPanel] profilePaperPrefab or mainCamera is not assigned.");
            return;
        }

        List<TaxpayerProfile> profilesToShow = GetProfilesToShow();

        for (int i = 0; i < profilesToShow.Count; i++)
        {
            SpawnPaper(profilesToShow[i], i, profilesToShow.Count);
        }
    }

    private List<TaxpayerProfile> GetProfilesToShow()
    {
        if (isLockedToSingleProfile)
        {
            var single = new List<TaxpayerProfile>();

            if (survivingProfileSource != null && survivingProfileSource.Profile != null)
            {
                single.Add(survivingProfileSource.Profile);
            }
            else
            {
                Debug.LogWarning(
                    "[CollectedProfilesWorldPanel] Locked to a single profile, " +
                    "but survivingProfileSource is unassigned or has no Profile set."
                );
            }

            return single;
        }

        return collectedProfiles;
    }

    private void SpawnPaper(TaxpayerProfile profile, int index, int totalCount)
    {
        GameObject paperObj = Instantiate(profilePaperPrefab, paperParent);

        // Fan the papers out in a horizontal row centered in front of the camera.
        float centeredOffset = (index - (totalCount - 1) / 2f) * horizontalSpacing;

        Vector3 scatter = new Vector3(
            Random.Range(-scatterAmount, scatterAmount),
            Random.Range(-scatterAmount, scatterAmount),
            0f
        );

        Vector3 worldPos =
            mainCamera.transform.position +
            mainCamera.transform.forward * spawnDistance +
            mainCamera.transform.right * centeredOffset +
            scatter;

        paperObj.transform.position = worldPos;
        paperObj.transform.rotation = Quaternion.LookRotation(
            paperObj.transform.position - mainCamera.transform.position
        );

        // Populate text the same way the corkboard cards do.
        TaxpayerProfileWorldPickup pickupText = paperObj.GetComponent<TaxpayerProfileWorldPickup>();
        if (pickupText != null)
        {
            pickupText.Initialize(profile);
        }

        // Wire up dragging via the reusable FloatingModel3D.
        FloatingModel3D dragController = paperObj.GetComponent<FloatingModel3D>();
        if (dragController == null)
        {
            dragController = paperObj.AddComponent<FloatingModel3D>();
        }

        spawnedPapers.Add(paperObj);
    }

    private void DespawnAllPapers()
    {
        foreach (GameObject paper in spawnedPapers)
        {
            if (paper != null)
            {
                Destroy(paper);
            }
        }

        spawnedPapers.Clear();
    }

    public int CollectedCount => isLockedToSingleProfile
        ? (survivingProfileSource != null && survivingProfileSource.Profile != null ? 1 : 0)
        : collectedProfiles.Count;

    public bool IsLockedToSingleProfile => isLockedToSingleProfile;

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}