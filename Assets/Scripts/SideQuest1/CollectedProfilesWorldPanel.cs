using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.UI;

/// <summary>
/// Shows collected profile papers in front of the camera while the profile
/// viewer is open. The papers reuse the world pickup prefab for its visuals.
/// </summary>
public class CollectedProfilesWorldPanel : MonoBehaviour
{
    public static CollectedProfilesWorldPanel Instance { get; private set; }

    [Header("Setup")]
    [Tooltip("The camera the papers should arrange themselves in front of.")]
    [SerializeField] private Camera mainCamera;

    [Tooltip("Prefab with a paper mesh and taxpayer profile text fields.")]
    [SerializeField] private GameObject profilePaperPrefab;

    [Tooltip("Optional organizational parent for viewer papers.")]
    [SerializeField] private Transform paperParent;

    [Header("Restriction After Side Quest")]
    [Tooltip("The one profile that remains browsable after the side quest ends.")]
    [SerializeField] private TaxpayerProfileWorldPickup survivingProfileSource;

    private const float ViewerDepth = 0.45f;
    private const float NearPlaneClearance = 0.15f;
    private const float OpenDuration = 0.45f;
    private const float OpenStagger = 0.06f;
    private const float InitialScale = 0.15f;
    private const float MultiCardGap = 0.06f;
    private const float CloseDuration = 0.4f;
    private const float CloseStagger = 0.045f;

    private readonly List<TaxpayerProfile> collectedProfiles = new List<TaxpayerProfile>();
    private readonly List<GameObject> spawnedPapers = new List<GameObject>();

    private CameraController viewerCameraController;
    private HighlightEffect highlightedPaper;
    private bool isOpen;
    private bool isClosing;
    private bool isLockedToSingleProfile;
    private bool profilesOnBoard;
    private int animationVersion;
    private int papersArriving;
    private Transform draggedPaper;
    private TouchControl dragTouch;
    private Vector2 grabOffset;
    private Rect viewerPixelRect;
    private readonly List<RaycastResult> uiHits = new List<RaycastResult>();

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

    private void Update()
    {
        if (isOpen && (mainCamera == null || viewerCameraController == null ||
                       !viewerCameraController.isActiveAndEnabled ||
                       viewerCameraController.CurrentMode != CameraController.CameraMode.ProfileViewer))
        {
            CloseViewer();
        }

        if (!isOpen || isClosing || mainCamera == null || papersArriving > 0) return;
        if (viewerPixelRect != mainCamera.pixelRect)
        {
            viewerPixelRect = mainCamera.pixelRect;
            foreach (GameObject paper in spawnedPapers)
                if (paper != null) KeepPaperOnScreen(paper.transform);
        }
        UpdatePaperInput();
    }

    private void UpdatePaperInput()
    {
        if (draggedPaper != null)
        {
            SetHighlightedPaper(draggedPaper);
            bool held = dragTouch != null ? dragTouch.press.isPressed :
                Mouse.current != null && Mouse.current.leftButton.isPressed;
            if (!held)
            {
                ReleasePaper();
                return;
            }

            Vector2 pointer = dragTouch != null ? dragTouch.position.ReadValue() : Mouse.current.position.ReadValue();
            Vector2 center = pointer + grabOffset;
            float depth = draggedPaper.localPosition.z;
            draggedPaper.position = mainCamera.ScreenToWorldPoint(new Vector3(center.x, center.y, depth));
            KeepPaperOnScreen(draggedPaper);
            return;
        }

        UpdatePaperHover();

        if (Touchscreen.current != null)
        {
            foreach (TouchControl touch in Touchscreen.current.touches)
            {
                if (!touch.press.wasPressedThisFrame) continue;
                TryGrabPaper(touch.position.ReadValue(), touch);
                return;
            }
        }
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            TryGrabPaper(Mouse.current.position.ReadValue(), null);
    }

    private void TryGrabPaper(Vector2 pointer, TouchControl touch)
    {
        // Query UI directly: EventSystem pointer-over state can be a frame behind Update.
        if (EventSystem.current != null)
        {
            uiHits.Clear();
            EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current) { position = pointer }, uiHits);
            foreach (RaycastResult hit in uiHits)
                if (hit.module is GraphicRaycaster) return;
        }

        GameObject paper = GetTopmostPaperAt(pointer);
        if (paper == null) return;

        spawnedPapers.Remove(paper);
        spawnedPapers.Add(paper);
        RefreshPaperDepths();
        draggedPaper = paper.transform;
        dragTouch = touch;
        grabOffset = (Vector2)mainCamera.WorldToScreenPoint(draggedPaper.position) - pointer;
        SetHighlightedPaper(draggedPaper);
    }

    private void UpdatePaperHover()
    {
        if (!TryGetPaperPointerPosition(out Vector2 pointer) || DialogueUI.IsAnyDialogueActive ||
            VirtualJoystick.BlocksWorldInput || MobileInputGuard.IsPointerOverUI(pointer))
        {
            SetHighlightedPaper(null);
            return;
        }

        GameObject paper = GetTopmostPaperAt(pointer);
        SetHighlightedPaper(paper != null ? paper.transform : null);
    }

    private bool TryGetPaperPointerPosition(out Vector2 pointer)
    {
        if (Touchscreen.current != null)
        {
            foreach (TouchControl touch in Touchscreen.current.touches)
            {
                if (!touch.press.isPressed && !touch.press.wasPressedThisFrame) continue;
                pointer = touch.position.ReadValue();
                return true;
            }
        }

        if (Mouse.current != null)
        {
            pointer = Mouse.current.position.ReadValue();
            return true;
        }

        pointer = default;
        return false;
    }

    private GameObject GetTopmostPaperAt(Vector2 pointer)
    {
        for (int i = spawnedPapers.Count - 1; i >= 0; i--)
        {
            GameObject paper = spawnedPapers[i];
            if (paper != null && paper.activeInHierarchy && GetPaperScreenRect(paper.transform).Contains(pointer))
                return paper;
        }

        return null;
    }

    private void SetHighlightedPaper(Transform paper)
    {
        HighlightEffect next = paper != null ? HighlightEffect.For(paper) : null;
        if (highlightedPaper == next) return;

        if (highlightedPaper != null) highlightedPaper.SetFocused(false);
        highlightedPaper = next;
        if (highlightedPaper != null) highlightedPaper.SetFocused(true);
    }

    private Rect GetPaperScreenRect(Transform paper)
    {
        MeshFilter mesh = paper.GetComponent<MeshFilter>();
        Bounds bounds = mesh != null && mesh.sharedMesh != null
            ? mesh.sharedMesh.bounds : new Bounds(Vector3.zero, Vector3.one);
        Vector3 min = mainCamera.WorldToScreenPoint(paper.TransformPoint(bounds.min));
        Vector3 max = mainCamera.WorldToScreenPoint(paper.TransformPoint(bounds.max));
        return Rect.MinMaxRect(Mathf.Min(min.x, max.x), Mathf.Min(min.y, max.y),
            Mathf.Max(min.x, max.x), Mathf.Max(min.y, max.y));
    }

    private void KeepPaperOnScreen(Transform paper)
    {
        Rect card = GetPaperScreenRect(paper);
        Rect screen = mainCamera.pixelRect;
        if (screen.width <= 16f || screen.height <= 16f) return;
        float fit = Mathf.Min(1f, (screen.width - 16f) / card.width, (screen.height - 16f) / card.height);
        if (fit < 1f)
        {
            paper.localScale *= fit;
            card = GetPaperScreenRect(paper);
        }
        Vector2 center = mainCamera.WorldToScreenPoint(paper.position);
        // Keep the whole sheet reachable, including when released against an edge.
        float x = card.width + 16f >= screen.width ? screen.center.x - card.center.x :
            Mathf.Clamp(card.center.x, screen.xMin + card.width * 0.5f + 8f,
                screen.xMax - card.width * 0.5f - 8f) - card.center.x;
        float y = card.height + 16f >= screen.height ? screen.center.y - card.center.y :
            Mathf.Clamp(card.center.y, screen.yMin + card.height * 0.5f + 8f,
                screen.yMax - card.height * 0.5f - 8f) - card.center.y;
        paper.position = mainCamera.ScreenToWorldPoint(new Vector3(center.x + x, center.y + y, paper.localPosition.z));
    }

    private void RefreshPaperDepths()
    {
        float frontDepth = Mathf.Max(ViewerDepth, mainCamera.nearClipPlane + NearPlaneClearance);
        float spacing = 0.03f;
        foreach (GameObject paper in spawnedPapers)
            if (paper != null)
            {
                float scaleAtFront = Mathf.Abs(paper.transform.localScale.z);
                if (!mainCamera.orthographic) scaleAtFront *= frontDepth / paper.transform.localPosition.z;
                spacing = Mathf.Max(spacing, scaleAtFront * 0.03f);
            }

        for (int i = 0; i < spawnedPapers.Count; i++)
        {
            if (spawnedPapers[i] == null) continue;
            Transform paper = spawnedPapers[i].transform;
            Vector3 position = paper.localPosition;
            float depth = frontDepth + (spawnedPapers.Count - 1 - i) * spacing;
            // Real depth orders both the opaque sheet and its text. Compensate
            // perspective so selecting a sheet never changes its screen size/position.
            float ratio = mainCamera.orthographic ? 1f : depth / position.z;
            paper.localScale *= ratio;
            paper.localPosition = new Vector3(position.x * ratio, position.y * ratio, depth);
        }
    }

    private void ReleasePaper()
    {
        SetHighlightedPaper(null);
        draggedPaper = null;
        dragTouch = null;
    }

    private void OnApplicationFocus(bool focused)
    {
        if (!focused) ReleasePaper();
    }

    private void OnApplicationPause(bool paused)
    {
        if (paused) ReleasePaper();
    }

    /// <summary>Mirrors TaxpayerProfileCollectionHUD.CollectProfile.</summary>
    public void RegisterCollectedProfile(TaxpayerProfile profile)
    {
        if (profile == null || collectedProfiles.Contains(profile))
        {
            return;
        }

        collectedProfiles.Add(profile);
        if (isOpen && !isClosing)
        {
            RespawnPapers(GetProfilesToShow());
        }
    }

    /// <summary>Wire this to the same HUD icon Button's onClick.</summary>
    public void OnHudIconClicked()
    {
        // The scene's profile icon calls this directly. In Office_Level1 the
        // Office HUD shows the profiles as paper windows instead (they layer
        // with Notes and the Case Folder), so let it handle the tap first.
        if (!isOpen && TaxpayerProfileCollectionHUD.IconClickOverride != null &&
            TaxpayerProfileCollectionHUD.IconClickOverride())
        {
            return;
        }

        if (!isActiveAndEnabled || isClosing) return;
        if (profilesOnBoard && !isOpen) return;   // the cards are in the corkboard tray
        if (isOpen)
        {
            BeginReturnToFolder();
            return;
        }

        List<TaxpayerProfile> profiles = GetProfilesToShow();
        if (profiles.Count == 0)
        {
            return;
        }

        CameraController controller = CameraController.Instance;
        if (controller == null)
        {
            Debug.LogWarning("[CollectedProfilesWorldPanel] Cannot open profile viewer without a CameraController.");
            return;
        }

        if (!controller.TryBeginProfileViewer())
        {
            return;
        }

        viewerCameraController = controller;
        isOpen = true;

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayPaperSFX();
        }

        RespawnPapers(profiles);
    }

    /// <summary>
    /// Call this once the corkboard side quest completes. From then on,
    /// only the surviving source's profile is shown.
    /// </summary>
    public void LockToSingleSurvivingProfile()
    {
        isLockedToSingleProfile = true;

        if (isOpen && !isClosing)
        {
            RespawnPapers(GetProfilesToShow());
        }

        if (TaxpayerProfileCollectionHUD.Instance != null)
        {
            TaxpayerProfileCollectionHUD.Instance.RefreshCountDisplay();
        }
    }

    /// <summary>Overload for assigning the survivor at call time.</summary>
    public void LockToSingleSurvivingProfile(TaxpayerProfileWorldPickup source)
    {
        survivingProfileSource = source;
        LockToSingleSurvivingProfile();
    }

    private void RespawnPapers(List<TaxpayerProfile> profiles)
    {
        DespawnAllPapers();

        if (profiles == null || profiles.Count == 0)
        {
            CloseViewer();
            return;
        }

        if (profilePaperPrefab == null || mainCamera == null)
        {
            Debug.LogError("[CollectedProfilesWorldPanel] profilePaperPrefab or mainCamera is not assigned.");
            CloseViewer();
            return;
        }

        TaxpayerProfileCollectionHUD hud = TaxpayerProfileCollectionHUD.Instance;
        Vector2 iconScreenPoint = default;
        bool hasIconPoint = hud != null && hud.TryGetCollectionScreenPoint(out iconScreenPoint);
        float depth = Mathf.Max(ViewerDepth, mainCamera.nearClipPlane + NearPlaneClearance);
        Vector3 iconLocal = hasIconPoint
            ? mainCamera.transform.InverseTransformPoint(mainCamera.ScreenToWorldPoint(
                new Vector3(iconScreenPoint.x, iconScreenPoint.y, depth)))
            : new Vector3(0.35f, 0.35f, depth);

        float viewHeight = GetViewportHeightAtDepth(depth);
        int count = profiles.Count;

        for (int i = 0; i < count; i++)
        {
            TaxpayerProfile profile = profiles[i];
            if (profile == null)
            {
                continue;
            }

            GameObject paper = Instantiate(profilePaperPrefab, paperParent);
            paper.transform.SetParent(mainCamera.transform, false);
            paper.transform.localRotation = Quaternion.identity;

            TaxpayerProfileWorldPickup pickup = paper.GetComponent<TaxpayerProfileWorldPickup>();
            if (pickup != null)
            {
                pickup.Initialize(profile);
                pickup.enabled = false;
            }

            FloatingModel3D dragController = paper.GetComponent<FloatingModel3D>();
            if (dragController != null)
            {
                dragController.enabled = false;
            }

            Collider[] paperColliders = paper.GetComponentsInChildren<Collider>();
            foreach (Collider paperCollider in paperColliders)
            {
                if (paperCollider != null)
                {
                    paperCollider.enabled = false;
                }
            }

            HighlightEffect.For(paper.transform).SetFocused(false);

            Vector3 authoredScale = paper.transform.localScale;
            GetPaperDimensions(paper, authoredScale, out float authoredHeight, out float paperAspect);
            GetViewportLayout(i, count, paperAspect, out Vector2 viewportCenter, out float heightFraction);

            float targetHeight = viewHeight * heightFraction;
            Vector3 targetScale = authoredScale * (targetHeight / authoredHeight);
            Vector3 targetLocal = WorldToCameraLocal(mainCamera.ViewportToWorldPoint(
                new Vector3(viewportCenter.x, viewportCenter.y, depth)));
            Vector3 startLocal = iconLocal;
            startLocal.z = targetLocal.z;
            paper.transform.localPosition = startLocal;
            paper.transform.localRotation = Quaternion.identity;
            paper.transform.localScale = targetScale * InitialScale;

            spawnedPapers.Add(paper);
            papersArriving++;
            StartCoroutine(AnimateIntoViewer(paper, startLocal, targetLocal, targetScale,
                OpenStagger * i, viewHeight, animationVersion));
        }
    }

    private void GetViewportLayout(int index, int count, float paperAspect, out Vector2 center, out float height)
    {
        if (count == 1)
        {
            center = new Vector2(0.48f, 0.52f);
            height = Mathf.Min(0.62f, 0.84f * mainCamera.aspect / paperAspect);
            return;
        }

        if (count == 2)
        {
            center = new Vector2(index == 0 ? 0.30f : 0.67f, 0.52f);
            height = Mathf.Min(0.58f, 0.32f * mainCamera.aspect / paperAspect);
            return;
        }

        int rows = Mathf.CeilToInt(count / 2f);
        height = Mathf.Min(0.36f, (0.86f - MultiCardGap * (rows - 1)) / rows);
        height = Mathf.Min(height, 0.32f * mainCamera.aspect / paperAspect);

        int row = index / 2;
        float rowSpacing = height + MultiCardGap;
        float y = 0.5f + (rows - 1) * rowSpacing * 0.5f - row * rowSpacing;
        float x = count == 3 && index == 2 ? 0.485f : (index % 2 == 0 ? 0.30f : 0.67f);
        center = new Vector2(x, y);
    }

    private void GetPaperDimensions(GameObject paper, Vector3 authoredScale, out float height, out float aspect)
    {
        MeshFilter meshFilter = paper.GetComponent<MeshFilter>();
        Vector3 meshSize = meshFilter != null && meshFilter.sharedMesh != null
            ? meshFilter.sharedMesh.bounds.size
            : Vector3.one;

        height = Mathf.Abs(meshSize.y * authoredScale.y);
        float width = Mathf.Abs(meshSize.x * authoredScale.x);
        if (height <= 0.0001f)
        {
            height = 1f;
        }

        aspect = width > 0.0001f ? width / height : 1f;
    }

    private float GetViewportHeightAtDepth(float depth)
    {
        Vector3 bottom = WorldToCameraLocal(mainCamera.ViewportToWorldPoint(new Vector3(0.5f, 0f, depth)));
        Vector3 top = WorldToCameraLocal(mainCamera.ViewportToWorldPoint(new Vector3(0.5f, 1f, depth)));
        return Mathf.Abs(top.y - bottom.y);
    }

    private Vector3 WorldToCameraLocal(Vector3 worldPoint)
    {
        return mainCamera.transform.InverseTransformPoint(worldPoint);
    }

    private IEnumerator AnimateIntoViewer(GameObject paper, Vector3 start, Vector3 target,
        Vector3 targetScale, float delay, float viewHeight, int version)
    {
        if (delay > 0f)
        {
            yield return new WaitForSecondsRealtime(delay);
        }

        float elapsed = 0f;
        float arcHeight = viewHeight * 0.055f;
        while (elapsed < OpenDuration)
        {
            if (paper == null || !isOpen || version != animationVersion)
            {
                yield break;
            }

            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / OpenDuration);
            float eased = progress * progress * (3f - 2f * progress);
            Vector3 position = Vector3.Lerp(start, target, eased);
            position.y += Mathf.Sin(progress * Mathf.PI) * arcHeight;
            paper.transform.localPosition = position;
            paper.transform.localScale = Vector3.Lerp(targetScale * InitialScale, targetScale, eased);
            yield return null;
        }

        if (paper != null && version == animationVersion)
        {
            paper.transform.localPosition = target;
            paper.transform.localScale = targetScale;
            papersArriving--;
            if (papersArriving == 0) RefreshPaperDepths();
        }
    }

    /// <summary>
    /// The corkboard side quest holds the profile cards in its tray (T06,
    /// user request 2026-10-24): while true the icon is empty (0) and nothing
    /// can be opened or dragged out of it. Closes the viewer if it is open.
    /// </summary>
    public void SetProfilesOnBoard(bool onBoard)
    {
        if (profilesOnBoard == onBoard) return;
        profilesOnBoard = onBoard;
        if (onBoard && isOpen && !isClosing) BeginReturnToFolder();
        if (TaxpayerProfileCollectionHUD.Instance != null) TaxpayerProfileCollectionHUD.Instance.RefreshCountDisplay();
    }

    /// <summary>True while the corkboard tray holds the profile cards.</summary>
    public bool ProfilesOnBoard => profilesOnBoard;

    /// <summary>The profiles the viewer would show (read-only use by the Office HUD profile cards).</summary>
    public List<TaxpayerProfile> GetProfilesToShow()
    {
        if (profilesOnBoard) return new List<TaxpayerProfile>();
        if (isLockedToSingleProfile)
        {
            var single = new List<TaxpayerProfile>();
            if (survivingProfileSource != null && survivingProfileSource.Profile != null)
            {
                single.Add(survivingProfileSource.Profile);
            }
            else
            {
                Debug.LogWarning("[CollectedProfilesWorldPanel] Locked to one profile, but survivingProfileSource is unassigned or has no Profile set.");
            }

            return single;
        }

        return new List<TaxpayerProfile>(collectedProfiles);
    }

    private void DespawnAllPapers()
    {
        animationVersion++;
        papersArriving = 0;
        ReleasePaper();
        foreach (GameObject paper in spawnedPapers)
        {
            if (paper != null)
            {
                Destroy(paper);
            }
        }

        spawnedPapers.Clear();
    }

    private void BeginReturnToFolder()
    {
        if (mainCamera == null || spawnedPapers.Count == 0)
        {
            CloseViewer();
            return;
        }

        isClosing = true;
        animationVersion++; // Cancel arrivals, including papers still waiting for their stagger.
        papersArriving = 0;
        ReleasePaper();
        if (AudioManager.Instance != null) AudioManager.Instance.PlayPaperSFX();
        StartCoroutine(AnimateBackToFolder(animationVersion));
    }

    private IEnumerator AnimateBackToFolder(int version)
    {
        int count = spawnedPapers.Count;
        var positions = new Vector3[count];
        var scales = new Vector3[count];
        for (int i = 0; i < count; i++)
        {
            if (spawnedPapers[i] == null) continue;
            positions[i] = spawnedPapers[i].transform.localPosition;
            scales[i] = spawnedPapers[i].transform.localScale;
        }

        Vector2 iconPoint = mainCamera.ViewportToScreenPoint(new Vector3(0.92f, 0.9f, 0f));
        float elapsed = 0f;
        float duration = CloseDuration + CloseStagger * (count - 1);
        while (elapsed < duration)
        {
            if (version != animationVersion || !isOpen || !isClosing) yield break;
            if (mainCamera == null)
            {
                CloseViewer();
                yield break;
            }

            // Re-query the existing HUD anchor so rotation/resizing during closing
            // still lands at the folder's current screen position.
            TaxpayerProfileCollectionHUD hud = TaxpayerProfileCollectionHUD.Instance;
            if (hud != null && hud.TryGetCollectionScreenPoint(out Vector2 currentIconPoint))
                iconPoint = currentIconPoint;

            elapsed += Time.unscaledDeltaTime;
            for (int i = 0; i < count; i++)
            {
                if (spawnedPapers[i] == null) continue;
                float progress = Mathf.Clamp01((elapsed - CloseStagger * (count - 1 - i)) / CloseDuration);
                float eased = progress * progress * (3f - 2f * progress);
                Vector3 target = WorldToCameraLocal(mainCamera.ScreenToWorldPoint(
                    new Vector3(iconPoint.x, iconPoint.y, positions[i].z)));
                Vector3 position = Vector3.Lerp(positions[i], target, eased);
                position.y += Mathf.Sin(progress * Mathf.PI) * GetViewportHeightAtDepth(positions[i].z) * 0.04f;
                Transform paper = spawnedPapers[i].transform;
                paper.localPosition = position;
                paper.localScale = scales[i] * Mathf.Lerp(1f, 0.025f, eased);
                if (progress >= 1f) paper.gameObject.SetActive(false);
            }
            yield return null;
        }

        if (version == animationVersion) CloseViewer();
    }

    // Immediate cleanup is also used on disable/destroy or a camera-mode change.
    private void CloseViewer()
    {
        isOpen = false;
        isClosing = false;
        DespawnAllPapers();

        if (viewerCameraController != null)
        {
            viewerCameraController.EndProfileViewer();
            viewerCameraController = null;
        }
    }

    public int CollectedCount => profilesOnBoard ? 0 : isLockedToSingleProfile
        ? (survivingProfileSource != null && survivingProfileSource.Profile != null ? 1 : 0)
        : collectedProfiles.Count;

    public bool IsLockedToSingleProfile => isLockedToSingleProfile;

    private void OnDisable()
    {
        ReleasePaper();
        if (isOpen || viewerCameraController != null)
        {
            CloseViewer();
        }
    }

    private void OnDestroy()
    {
        if (isOpen || viewerCameraController != null)
        {
            CloseViewer();
        }

        if (Instance == this)
        {
            Instance = null;
        }
    }
}
