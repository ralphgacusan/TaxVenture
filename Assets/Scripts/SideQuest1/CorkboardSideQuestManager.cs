using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Runs the four-profile corkboard sorting sidequest.</summary>
public class CorkboardSideQuestManager : MonoBehaviour
{
    [Header("Setup")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private GameObject sideQuestRoot;
    [SerializeField] private Transform corkboardViewpoint;
    [SerializeField] private Transform cardTrayParent;
    [SerializeField] private GameObject profileCardPrefab;
    [SerializeField] private float cardTraySpacing = 0.5f;
    [SerializeField] private List<CorkboardSlot> slots;

    [Header("Reward")]
    [SerializeField] private int rewardExp = 50;
    [SerializeField] private int rewardReputation = 10;

    private readonly List<ProfileDragCard> spawnedCards = new List<ProfileDragCard>();
    private readonly HashSet<CorkboardSlot> firstTrySolvedSlots = new HashSet<CorkboardSlot>();
    private readonly List<LineRenderer> dividerLines = new List<LineRenderer>();
    private Camera cachedCamera;
    private Material dividerMaterial;
    private Canvas feedbackCanvas;
    private TextMeshProUGUI feedbackText;
    private Bounds boardBounds;
    private float splitX;
    private float splitY;
    private float boardPlaneZ;
    private int placementAttempts;
    private int wrongPlacements;
    private bool sideQuestActive;
    private bool sideQuestCompleted;
    private bool completionEventRaised;
    private bool boardGeometryReady;

    public bool CanInteract => sideQuestActive && !sideQuestCompleted;
    public bool IsCompleted => sideQuestCompleted;
    /// <summary>Placements that were wrong this run (each one costs EXP; see rewards.json).</summary>
    public int WrongPlacements => wrongPlacements;

    private void Awake()
    {
        if (sideQuestRoot != null)
        {
            sideQuestRoot.SetActive(false);
        }
    }

    private void OnDisable()
    {
        CancelInvoke();
        if (sideQuestCompleted && !completionEventRaised)
        {
            FinishSideQuest();
        }
        if (sideQuestActive)
        {
            sideQuestActive = false;
            if (CameraController.Instance != null)
            {
                CameraController.Instance.ExitCorkboardSideQuest();
            }
        }

        ClearRuntimeObjects();
    }

    public void BeginSideQuest()
    {
        if (sideQuestActive || sideQuestCompleted)
        {
            return;
        }

        cachedCamera = mainCamera != null ? mainCamera : Camera.main;
        if (cachedCamera == null || corkboardViewpoint == null || !HasValidSlots())
        {
            Debug.LogError("[CorkboardSideQuestManager] A camera, viewpoint, and four configured slots are required.");
            return;
        }

        if (profileCardPrefab == null || cardTrayParent == null || TaxpayerProfileCollectionHUD.Instance == null ||
            TaxpayerProfileCollectionHUD.Instance.CollectedProfiles.Count < 4)
        {
            Debug.LogError("[CorkboardSideQuestManager] The card prefab, tray, and four collected profiles are required.");
            return;
        }

        if (!BuildBoardGeometry())
        {
            Debug.LogError("[CorkboardSideQuestManager] Could not read the four corkboard drop areas.");
            return;
        }

        ClearRuntimeObjects();
        placementAttempts = 0;
        wrongPlacements = 0;
        firstTrySolvedSlots.Clear();
        sideQuestActive = true;

        if (sideQuestRoot != null && !sideQuestRoot.activeSelf)
        {
            sideQuestRoot.SetActive(true);
        }

        CreateDividers();
        SetFeedback("Your files are ready. Match each profile to a corkboard area.");

        if (CameraController.Instance != null)
        {
            CameraController.Instance.EnterCorkboardSideQuest(corkboardViewpoint, SpawnCards);
        }
        else
        {
            SpawnCards();
        }
    }

    /// <summary>Maps a pointer ray to a quadrant and clamps the whole card inside it.</summary>
    public bool TryGetDrop(Ray ray, Transform card, out CorkboardSlot slot, out Vector3 position, out Quaternion rotation)
    {
        slot = null;
        position = default;
        rotation = Quaternion.identity;
        if (!CanInteract || card == null || !boardGeometryReady || !HasValidSlots())
        {
            return false;
        }

        Transform board = BoardTransform;
        float pinnedDepth = boardPlaneZ - 0.008f;
        Vector3 planePoint = board.TransformPoint(new Vector3(0f, 0f, pinnedDepth));
        Plane plane = new Plane(board.forward, planePoint);
        if (!plane.Raycast(ray, out float distance) || distance < 0f)
        {
            return false;
        }

        Vector3 boardPoint = board.InverseTransformPoint(ray.GetPoint(distance));
        if (boardPoint.x < boardBounds.min.x || boardPoint.x > boardBounds.max.x ||
            boardPoint.y < boardBounds.min.y || boardPoint.y > boardBounds.max.y)
        {
            return false;
        }

        slot = FindSlotForQuadrant(boardPoint.x >= splitX, boardPoint.y >= splitY);
        if (slot == null)
        {
            slot = null;
            return false;
        }

        GetCardHalfExtents(card, board, out float halfWidth, out float halfHeight);
        const float edgeGap = 0.015f;
        float minX = boardBounds.min.x + halfWidth + edgeGap;
        float maxX = boardBounds.max.x - halfWidth - edgeGap;
        float minY = boardBounds.min.y + halfHeight + edgeGap;
        float maxY = boardBounds.max.y - halfHeight - edgeGap;
        if (minX > maxX || minY > maxY)
        {
            slot = null;
            return false;
        }

        float x = Mathf.Clamp(boardPoint.x, minX, maxX);
        float y = Mathf.Clamp(boardPoint.y, minY, maxY);
        position = board.TransformPoint(new Vector3(x, y, pinnedDepth));
        rotation = board.rotation;
        return true;
    }

    /// <summary>Called after the card's short pin animation has finished.</summary>
    public bool HandleCardDropped(ProfileDragCard card, CorkboardSlot slot)
    {
        if (!CanInteract || card == null || card.Profile == null || !spawnedCards.Contains(card) ||
            slot == null || slots == null || !slots.Contains(slot))
        {
            return false;
        }

        foreach (CorkboardSlot existingSlot in slots)
        {
            if (existingSlot != null && existingSlot != slot && existingSlot.OccupyingCard == card)
            {
                existingSlot.VacateSlot(card);
            }
        }

        placementAttempts++;
        bool firstAttempt = slot.PlacementAttempts == 0;
        bool correct = slot.TryPlaceCard(card);
        if (!correct) wrongPlacements++;
        if (correct && firstAttempt)
        {
            firstTrySolvedSlots.Add(slot);
        }

        int solvedCount = CountSolvedSlots();
        string count = $"{solvedCount}/4 areas sorted";
        if (correct)
        {
            string firstTry = firstAttempt ? " First try!" : " Nice recovery!";
            SetFeedback($"{card.Profile.TaxpayerName} is filed correctly.{firstTry} {count} • {placementAttempts} placements • {firstTrySolvedSlots.Count} first-try areas.");
        }
        else
        {
            SetFeedback($"Try again: compare citizenship and usual residence for {card.Profile.TaxpayerName}. {count} • {placementAttempts} placements.");
        }

        CheckForCompletion();
        return correct;
    }

    public void NotifyCardRemovedFromSlot()
    {
        if (CanInteract)
        {
            SetFeedback($"Move the files around until each area is correct. {CountSolvedSlots()}/4 sorted • {placementAttempts} placements.");
        }
    }

    public void CancelSideQuest()
    {
        if (sideQuestCompleted)
        {
            return;
        }

        CancelInvoke();
        sideQuestActive = false;
        ClearRuntimeObjects();
        if (sideQuestRoot != null && sideQuestRoot.activeSelf)
        {
            sideQuestRoot.SetActive(false);
        }
        if (CameraController.Instance != null)
        {
            CameraController.Instance.ExitCorkboardSideQuest();
        }
    }

    private Transform BoardTransform => sideQuestRoot != null ? sideQuestRoot.transform : transform;

    private void SpawnCards()
    {
        if (!CanInteract || TaxpayerProfileCollectionHUD.Instance == null)
        {
            return;
        }

        IReadOnlyList<TaxpayerProfile> profiles = TaxpayerProfileCollectionHUD.Instance.CollectedProfiles;
        if (profiles.Count < 4)
        {
            CancelSideQuest();
            Debug.LogError("[CorkboardSideQuestManager] Four collected profiles are required.");
            return;
        }

        Vector2 collectionScreenPoint = default;
        bool animateFromHud = cachedCamera != null &&
            TaxpayerProfileCollectionHUD.Instance.TryGetCollectionScreenPoint(out collectionScreenPoint);

        if (!TryGetTrayView(out Plane trayPlane, out float viewHeight))
        {
            CancelSideQuest();
            Debug.LogError("[CorkboardSideQuestManager] Could not frame the card tray in the game camera.");
            return;
        }

        float step = Mathf.Clamp(cardTraySpacing / viewHeight, 0.235f, 0.24f);
        float cardHeight = 0.22f;
        float[] cardViewY = { 0.85f, 0.85f - step, 0.85f - step * 2f, 0.85f - step * 3f };
        for (int i = 0; i < 4; i++)
        {
            GameObject cardObject = Instantiate(profileCardPrefab, cardTrayParent);
            ProfileDragCard card = cardObject.GetComponent<ProfileDragCard>();
            if (card == null)
            {
                Destroy(cardObject);
                CancelSideQuest();
                Debug.LogError("[CorkboardSideQuestManager] The card prefab is missing ProfileDragCard.");
                return;
            }

            card.transform.rotation = BoardTransform.rotation;
            GetCardHalfExtents(card.transform, BoardTransform, out _, out float halfHeight);
            float baseHeight = Mathf.Max(halfHeight * 2f, 0.01f);
            float scale = viewHeight * cardHeight / baseHeight;
            card.transform.localScale *= scale;
            if (!TryGetTrayPoint(trayPlane, 0.11f, cardViewY[i], out Vector3 trayPosition))
            {
                Destroy(cardObject);
                CancelSideQuest();
                Debug.LogError("[CorkboardSideQuestManager] Could not place a profile card in the visible tray.");
                return;
            }
            card.transform.position = trayPosition;
            card.Initialize(profiles[i], this, cachedCamera);
            if (animateFromHud) card.AnimateFromHud(collectionScreenPoint, i * 0.09f);
            spawnedCards.Add(card);
        }

        SetFeedback("Drag a profile into its matching area. Cards can be moved again.");
    }

    private bool TryGetTrayView(out Plane trayPlane, out float viewHeight)
    {
        Transform board = BoardTransform;
        trayPlane = new Plane(board.forward, cardTrayParent.position);
        viewHeight = 0f;
        if (cachedCamera == null || !TryGetTrayPoint(trayPlane, 0.5f, 0f, out Vector3 bottom) ||
            !TryGetTrayPoint(trayPlane, 0.5f, 1f, out Vector3 top))
        {
            return false;
        }

        viewHeight = Mathf.Abs(board.InverseTransformPoint(top).y - board.InverseTransformPoint(bottom).y);
        return viewHeight > 0.01f;
    }

    private bool TryGetTrayPoint(Plane trayPlane, float viewportX, float viewportY, out Vector3 point)
    {
        point = default;
        if (cachedCamera == null)
        {
            return false;
        }

        Ray ray = cachedCamera.ViewportPointToRay(new Vector3(viewportX, viewportY, 0f));
        if (!trayPlane.Raycast(ray, out float distance) || distance < 0f)
        {
            return false;
        }

        point = ray.GetPoint(distance);
        return true;
    }

    private bool BuildBoardGeometry()
    {
        boardGeometryReady = false;
        Transform board = BoardTransform;
        bool hasBounds = false;
        float snapZTotal = 0f;
        foreach (CorkboardSlot area in slots)
        {
            if (area == null || !area.TryGetBoardLocalBounds(board, out Bounds areaBounds))
            {
                return false;
            }

            if (hasBounds)
            {
                boardBounds.Encapsulate(areaBounds);
            }
            else
            {
                boardBounds = areaBounds;
                hasBounds = true;
            }

            snapZTotal += board.InverseTransformPoint(area.SnapPosition).z;
        }

        if (!hasBounds)
        {
            return false;
        }

        // Drop colliders can extend past the board; keep the playable cross on its face.
        Transform boardVisual = board.Find("Corkboard");
        if (boardVisual != null)
        {
            Bounds faceBounds = default;
            bool hasFaceBounds = false;
            foreach (MeshFilter mesh in boardVisual.GetComponentsInChildren<MeshFilter>(true))
            {
                if (mesh.sharedMesh == null) continue;
                Bounds meshBounds = mesh.sharedMesh.bounds;
                for (int x = -1; x <= 1; x += 2)
                for (int y = -1; y <= 1; y += 2)
                for (int z = -1; z <= 1; z += 2)
                {
                    Vector3 corner = meshBounds.center + Vector3.Scale(meshBounds.extents, new Vector3(x, y, z));
                    Vector3 point = board.InverseTransformPoint(mesh.transform.TransformPoint(corner));
                    if (hasFaceBounds) faceBounds.Encapsulate(point);
                    else { faceBounds = new Bounds(point, Vector3.zero); hasFaceBounds = true; }
                }
            }
            if (hasFaceBounds)
            {
                // Leave room for the frame and the rounded ends of the divider strokes.
                const float frameInset = 0.035f;
                Vector3 min = boardBounds.min;
                Vector3 max = boardBounds.max;
                min.x = Mathf.Max(min.x, faceBounds.min.x + frameInset);
                min.y = Mathf.Max(min.y, faceBounds.min.y + frameInset);
                max.x = Mathf.Min(max.x, faceBounds.max.x - frameInset);
                max.y = Mathf.Min(max.y, faceBounds.max.y - frameInset);
                if (min.x >= max.x || min.y >= max.y) return false;
                boardBounds.SetMinMax(min, max);
            }
        }

        splitX = boardBounds.center.x;
        splitY = boardBounds.center.y;
        // The serialized snap points define the visible card face plane in front of the board.
        boardPlaneZ = snapZTotal / slots.Count;
        boardGeometryReady = true;
        return true;
    }

    private bool HasValidSlots()
    {
        if (slots == null || slots.Count != 4)
        {
            return false;
        }
        var uniqueSlots = new HashSet<CorkboardSlot>();
        var categories = new HashSet<TaxpayerClassification>();
        foreach (CorkboardSlot slot in slots)
        {
            if (slot == null || slot.DropArea == null || !uniqueSlots.Add(slot) || !categories.Add(slot.ExpectedClassification))
            {
                return false;
            }
        }
        return categories.Count == 4;
    }

    private CorkboardSlot FindSlotForQuadrant(bool right, bool upper)
    {
        foreach (CorkboardSlot candidate in slots)
        {
            if (!candidate.TryGetBoardLocalBounds(BoardTransform, out Bounds bounds))
            {
                continue;
            }
            if ((bounds.center.x >= splitX) == right && (bounds.center.y >= splitY) == upper)
            {
                return candidate;
            }
        }
        return null;
    }

    private void GetCardHalfExtents(Transform card, Transform board, out float halfWidth, out float halfHeight)
    {
        Bounds cardBounds = default;
        bool hasBounds = false;
        foreach (Renderer visual in card.GetComponentsInChildren<Renderer>())
        {
            Bounds localBounds = visual.localBounds;
            Vector3 ext = localBounds.extents;
            for (int x = -1; x <= 1; x += 2)
            for (int y = -1; y <= 1; y += 2)
            for (int z = -1; z <= 1; z += 2)
            {
                Vector3 localPoint = localBounds.center + Vector3.Scale(ext, new Vector3(x, y, z));
                Vector3 local = board.InverseTransformPoint(visual.transform.TransformPoint(localPoint));
                if (hasBounds) cardBounds.Encapsulate(local);
                else { cardBounds = new Bounds(local, Vector3.zero); hasBounds = true; }
            }
        }

        halfWidth = hasBounds ? cardBounds.extents.x : 0.15f;
        halfHeight = hasBounds ? cardBounds.extents.y : 0.15f;
    }

    private void CreateDividers()
    {
        DestroyDividers();
        dividerMaterial = CreateLineMaterial();
        if (dividerMaterial == null)
        {
            return;
        }

        float z = boardPlaneZ - 0.002f;
        CreateDivider("BoardDividerVertical", new Vector3(splitX, boardBounds.min.y, z), new Vector3(splitX, boardBounds.max.y, z), dividerMaterial);
        CreateDivider("BoardDividerHorizontal", new Vector3(boardBounds.min.x, splitY, z), new Vector3(boardBounds.max.x, splitY, z), dividerMaterial);
    }

    private void CreateDivider(string name, Vector3 start, Vector3 end, Material material)
    {
        var lineObject = new GameObject(name);
        lineObject.transform.SetParent(BoardTransform, false);
        LineRenderer line = lineObject.AddComponent<LineRenderer>();
        line.useWorldSpace = false;
        line.positionCount = 2;
        line.SetPosition(0, start);
        line.SetPosition(1, end);
        line.startWidth = line.endWidth = 0.009f;
        line.numCapVertices = 2;
        line.sharedMaterial = material;
        line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        line.receiveShadows = false;
        line.sortingOrder = 2;
        dividerLines.Add(line);
    }

    private Material CreateLineMaterial()
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
        {
            Debug.LogWarning("[CorkboardSideQuestManager] Could not find an unlit shader for board dividers.");
            return null;
        }

        var material = new Material(shader) { color = new Color(0.94f, 0.82f, 0.55f, 0.9f) };
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", material.color);
        return material;
    }

    private void SetFeedback(string message)
    {
        if (feedbackText == null)
        {
            CreateFeedbackCanvas();
        }
        if (feedbackText != null)
        {
            feedbackText.text = message;
        }
    }

    private void CreateFeedbackCanvas()
    {
        var canvasObject = new GameObject("CorkboardFeedback", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        feedbackCanvas = canvasObject.GetComponent<Canvas>();
        feedbackCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        feedbackCanvas.sortingOrder = 30;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);
        scaler.matchWidthOrHeight = 0.5f;

        var panelObject = new GameObject("FeedbackPanel", typeof(RectTransform), typeof(Image));
        panelObject.transform.SetParent(canvasObject.transform, false);
        RectTransform panel = panelObject.GetComponent<RectTransform>();
        panel.anchorMin = panel.anchorMax = new Vector2(0.5f, 0f);
        panel.pivot = new Vector2(0.5f, 0f);
        panel.anchoredPosition = new Vector2(0f, 18f);
        panel.sizeDelta = new Vector2(650f, 72f);
        Image background = panelObject.GetComponent<Image>();
        background.color = new Color(0.07f, 0.055f, 0.04f, 0.92f);
        background.raycastTarget = false;

        var textObject = new GameObject("FeedbackText", typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(panel, false);
        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(18f, 8f);
        textRect.offsetMax = new Vector2(-18f, -8f);
        feedbackText = textObject.GetComponent<TextMeshProUGUI>();
        feedbackText.font = TMP_Settings.defaultFontAsset;
        feedbackText.fontSize = 20f;
        feedbackText.color = new Color(1f, 0.94f, 0.78f);
        feedbackText.alignment = TextAlignmentOptions.Center;
        feedbackText.enableWordWrapping = true;
        feedbackText.raycastTarget = false;
    }

    private void CheckForCompletion()
    {
        if (slots == null || slots.Count != 4 || CountSolvedSlots() != 4)
        {
            return;
        }
        foreach (CorkboardSlot slot in slots)
        {
            if (slot == null || slot.OccupyingCard == null || !slot.IsSolved)
            {
                return;
            }
        }
        CompleteSideQuest();
    }

    private int CountSolvedSlots()
    {
        int count = 0;
        if (slots == null) return 0;
        foreach (CorkboardSlot slot in slots)
        {
            if (slot != null && slot.OccupyingCard != null && slot.IsSolved) count++;
        }
        return count;
    }

    private void CompleteSideQuest()
    {
        if (sideQuestCompleted)
        {
            return;
        }

        sideQuestCompleted = true;
        sideQuestActive = false;
        SetFeedback($"Case closed! All four files are correct. {placementAttempts} placements • {firstTrySolvedSlots.Count}/4 first-try areas.");
        GrantRewards();
        Invoke(nameof(HideSideQuestRoot), 2f);
    }

    private void GrantRewards()
    {
        // The level score is awarded by the tutorial NPC (TutorialNpcController)
        // from WrongPlacements and rewards.json; this only logs the run.
        Debug.Log($"[CorkboardSideQuestManager] Side quest finished: {placementAttempts} placements, {wrongPlacements} wrong.");
    }

    private void HideSideQuestRoot()
    {
        if (sideQuestRoot != null)
        {
            sideQuestRoot.SetActive(false);
        }

        FinishSideQuest();
    }

    private void FinishSideQuest()
    {
        if (completionEventRaised)
        {
            return;
        }

        completionEventRaised = true;
        if (CameraController.Instance != null)
        {
            CameraController.Instance.ExitCorkboardSideQuest();
        }

        TownEvents.RaiseSideQuestCompleted();
    }

    private void ClearRuntimeObjects()
    {
        DestroyDividers();
        if (feedbackCanvas != null)
        {
            Destroy(feedbackCanvas.gameObject);
            feedbackCanvas = null;
            feedbackText = null;
        }
        foreach (ProfileDragCard card in spawnedCards)
        {
            if (card != null)
            {
                Destroy(card.gameObject);
            }
        }
        spawnedCards.Clear();
        if (slots != null)
        {
            foreach (CorkboardSlot slot in slots)
            {
                if (slot != null) slot.ResetRuntimeState();
            }
        }
    }

    private void DestroyDividers()
    {
        foreach (LineRenderer line in dividerLines)
        {
            if (line != null)
            {
                Destroy(line.gameObject);
            }
        }
        dividerLines.Clear();
        if (dividerMaterial != null)
        {
            Destroy(dividerMaterial);
            dividerMaterial = null;
        }
    }

    /// <summary>
    /// The side-quest reward. Uses the level score entry when one was awarded
    /// (rewards.json values); otherwise the legacy inspector values.
    /// </summary>
    public SideQuestResult BuildResult(ScoreEntry entry = null)
    {
        if (entry == null) return new SideQuestResult(rewardExp, rewardReputation);
        return new SideQuestResult(entry.Exp, entry.Hearts)
        {
            MaxExp = entry.MaxExp,
            MaxReputation = entry.MaxHearts,
            WrongPlacements = wrongPlacements,
            BadgeName = entry.BadgeName,
            BadgeTier = entry.BadgeTier
        };
    }
}
