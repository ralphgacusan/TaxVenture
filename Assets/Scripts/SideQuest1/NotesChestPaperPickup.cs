using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// The paper that rises from the Level 1 onboarding chest and flies into the
/// existing Notes hotbar slot after an atomic inventory/pickup commit.
/// </summary>
public sealed class NotesChestPaperPickup : MonoBehaviour, IInteractable, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [SerializeField] private float bobHeight = 0.05f;
    [SerializeField] private float bobSpeed = 2f;

    // The sheet is drawn at the Notes panel's own size, then fitted to the quad.
    private const float SheetWidth = 900f;
    private const float SheetHeight = 1000f;

    private NotesOnboardingChest chest;
    private Collider pickupCollider;
    private HighlightEffect highlight;
    private Vector3 restPosition;
    private Quaternion restRotation;
    private Vector3 restScale;
    private bool hasRestPose;
    private bool dropping;
    private float dropElapsed;
    private const float DropSeconds = 0.65f;
    private Vector3 dropStart;
    private bool collecting;
    private float collectElapsed;
    private const float CollectSeconds = 0.45f;
    private const float LiftShare = 0.25f;
    private const float LiftHeight = 0.08f;
    private Vector3 liftStart;
    private Quaternion liftRotation;
    private Vector3 liftedPosition;
    private Camera collectionCamera;
    private Vector2 collectionStartScreen;
    private float collectionStartDepth;
    private float collectionTargetDepth;
    private Quaternion collectionStartRotation;
    private Vector3 collectionStartScale;
    private Vector2 pointerPosition;
    private bool pointerHovered;
    private bool interactorFocused;
    private bool collected;

    public bool IsDropping => dropping;
    public bool IsCollecting => collecting;
    public bool IsCollected => collected;

    private void Awake()
    {
        chest = GetComponentInParent<NotesOnboardingChest>();
        pickupCollider = GetComponent<Collider>();
        BuildSheet();
        highlight = HighlightEffect.For((IInteractable)this);
        CacheRestPose();
        if (pickupCollider != null) pickupCollider.enabled = false;
    }

    /// <summary>Called when the chest takes ownership of the scene-authored paper instance.</summary>
    public void AttachToChest(NotesOnboardingChest owner)
    {
        chest = owner;
        // The chest sets the local pose before this call. Re-cache after that
        // reparent so hover, restore and the flight all use the chest pose.
        hasRestPose = false;
        CacheRestPose();
    }

    private void Update()
    {
        RefreshHighlight();
        if (collected) return;
        if (collecting) { UpdateCollection(); return; }
        if (dropping) { UpdateDrop(); return; }
        transform.position = WorldPickupMotion.Hover(restPosition, Time.time, bobSpeed, bobHeight);
    }

    public void ShowFrom(Vector3 source)
    {
        if (collected) return;
        gameObject.SetActive(true);
        CacheRestPose();
        dropStart = source;
        dropElapsed = 0f;
        dropping = true;
        if (pickupCollider != null) pickupCollider.enabled = false;
        transform.SetPositionAndRotation(source, restRotation);
    }

    public void RestoreVisible()
    {
        if (collected) return;
        dropping = false;
        collecting = false;
        gameObject.SetActive(true);
        CacheRestPose();
        transform.SetPositionAndRotation(restPosition, restRotation);
        transform.localScale = restScale;
        if (pickupCollider != null) pickupCollider.enabled = true;
    }

    public void HideForChest()
    {
        dropping = false;
        collecting = false;
        pointerHovered = false;
        interactorFocused = false;
        if (pickupCollider != null) pickupCollider.enabled = false;
        if (highlight != null) highlight.SetFocused(false);
        if (gameObject.activeSelf) gameObject.SetActive(false);
    }

    public void MarkCollected()
    {
        collected = true;
        HideForChest();
    }

    public void OnFocus()
    {
        interactorFocused = true;
        if (CanCollect() && highlight != null) highlight.SetFocused(true);
    }

    public void OnUnfocus()
    {
        interactorFocused = false;
        pointerHovered = false;
        if (highlight != null) highlight.SetFocused(false);
    }

    public void OnInteract() => TryCollect(null);

    public string GetPromptText() => CanCollect() ? "Tap to collect Notes" : "";

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (eventData == null) return;
        pointerPosition = eventData.position;
        pointerHovered = true;
        RefreshHighlight();
    }

    public void OnPointerExit(PointerEventData eventData) => OnUnfocus();

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData == null || eventData.button != PointerEventData.InputButton.Left ||
            MobileInputGuard.IsDragOrCancelled(eventData) || VirtualJoystick.BlocksWorldInput ||
            MobileInputGuard.IsPointerOverUI(eventData.position)) return;
        if (InteractionRange.PlayerCanReach(this)) TryCollect(eventData.pressEventCamera);
    }

    private bool CanCollect()
    {
        return isActiveAndEnabled && !collected && !dropping && !collecting &&
               !DialogueUI.IsAnyDialogueActive && HudHotbar.CanOpenPanels() &&
               HudHotbar.Instance != null && HudHotbar.Instance.IsReady && InteractionRange.PlayerCanReach(this);
    }

    private void TryCollect(Camera inputCamera)
    {
        if (collected || dropping || collecting || !CanCollect()) return;
        HudHotbar hotbar = HudHotbar.Instance;
        if (hotbar == null || !hotbar.IsReady) return;
        if (InventoryRules.Owns(hotbar.Inventory, InventoryCatalogue.Notes))
        {
            if (chest != null) chest.PaperCollected();
            else HideForChest();
            return;
        }

        // Ownership and the once-per-campaign pickup claim are durable before
        // any sound or flight; a failed write leaves this paper in the world.
        InventoryResult result = hotbar.CollectNotesFromChest();
        if (result == null || !result.Changed)
        {
            if (result != null && result.Outcome == InventoryOutcome.SaveFailed)
                hotbar.Notice("Notes could not be saved. Your paper is still here; tap it to try again.");
            else if (result != null && result.Outcome == InventoryOutcome.Rejected)
                hotbar.Notice(result.Message);
            return;
        }

        AudioManager.Instance?.QueueInteractionSFX("paper");
        WorldEmote.Show(chest != null ? chest.transform : transform, Emotion.Idea, 1.6f);
        collectionCamera = inputCamera != null ? inputCamera : Camera.main;
        if (collectionCamera == null || !hotbar.TryGetNotesLandingScreenPoint(out _))
        {
            FinishCollection();
            return;
        }

        liftStart = transform.position;
        liftRotation = transform.rotation;
        liftedPosition = liftStart + Vector3.up * LiftHeight;
        Vector3 startScreen = collectionCamera.WorldToScreenPoint(liftedPosition);
        if (startScreen.z <= collectionCamera.nearClipPlane)
        {
            FinishCollection();
            return;
        }

        collectionStartScreen = new Vector2(startScreen.x, startScreen.y);
        collectionStartDepth = startScreen.z;
        collectionTargetDepth = WorldPickupMotion.FlightEndDepth(collectionCamera, collectionStartDepth);
        collectionStartRotation = Quaternion.Slerp(liftRotation, collectionCamera.transform.rotation, 0.6f);
        collectionStartScale = transform.localScale;
        collectElapsed = 0f;
        collecting = true;
        dropping = false;
        if (pickupCollider != null) pickupCollider.enabled = true;
        ClearPointerFocus();
    }

    private void UpdateDrop()
    {
        dropElapsed += Time.deltaTime;
        float progress = Mathf.Clamp01(dropElapsed / DropSeconds);
        Vector3 landing = WorldPickupMotion.Hover(restPosition, Time.time, bobSpeed, bobHeight);
        WorldPickupMotion.Drop(transform, dropStart, restPosition, restRotation, landing, progress);
        if (progress < 1f) return;
        dropping = false;
        dropElapsed = 0f;
        transform.SetPositionAndRotation(landing, restRotation);
        if (pickupCollider != null) pickupCollider.enabled = true;
    }

    private void UpdateCollection()
    {
        HudHotbar hotbar = HudHotbar.Instance;
        if (hotbar == null || collectionCamera == null || !hotbar.TryGetNotesLandingScreenPoint(out Vector2 targetScreen))
        {
            FinishCollection();
            return;
        }

        collectElapsed += Time.deltaTime;
        float progress = Mathf.Clamp01(collectElapsed / CollectSeconds);
        if (progress < LiftShare)
        {
            float lift = WorldPickupMotion.Smooth(progress / LiftShare);
            transform.SetPositionAndRotation(Vector3.Lerp(liftStart, liftedPosition, lift),
                Quaternion.Slerp(liftRotation, collectionStartRotation, lift));
            return;
        }
        WorldPickupMotion.Fly(transform, collectionCamera, collectionStartScreen, targetScreen,
            collectionStartDepth, collectionTargetDepth, collectionStartRotation, collectionStartScale,
            (progress - LiftShare) / (1f - LiftShare));
        if (progress >= 1f) FinishCollection();
    }

    private void FinishCollection()
    {
        if (collected) return;
        collected = true;
        collecting = false;
        dropping = false;
        ClearPointerFocus();
        if (chest != null) chest.PaperCollected();
        else gameObject.SetActive(false);
    }

    /// <summary>
    /// The same page as the Notes panel (NotesPaperView), on a world canvas
    /// just in front of the quad. The quad stays as the paper's backing and
    /// outline; the root's scale is the sheet's size in metres.
    /// </summary>
    private void BuildSheet()
    {
        if (transform.Find("Notes Sheet") != null) return;
        Renderer backing = GetComponent<Renderer>();
        if (backing != null) backing.material.color = new Color32(226, 214, 186, 255);

        var host = new GameObject("Notes Sheet", typeof(RectTransform));
        host.layer = gameObject.layer;
        var rect = (RectTransform)host.transform;
        rect.SetParent(transform, false);
        rect.localPosition = new Vector3(0f, 0f, -0.01f);
        rect.localRotation = Quaternion.identity;
        rect.localScale = new Vector3(1f / SheetWidth, 1f / SheetHeight, 1f);
        rect.sizeDelta = new Vector2(SheetWidth, SheetHeight);
        host.AddComponent<Canvas>().renderMode = RenderMode.WorldSpace;

        var headings = new System.Collections.Generic.List<string>();
        OfficeObjectives objectives = OfficeObjectives.Instance;
        if (objectives != null && objectives.Tasks != null)
            foreach (TaskDefinition task in objectives.Tasks)
                if (task != null && task.IsHeader && !string.IsNullOrEmpty(task.TaskDescription)) headings.Add(task.TaskDescription);
        NotesPaperView.BuildSheet(rect, SheetWidth, SheetHeight, headings);
    }

    private void CacheRestPose()
    {
        if (hasRestPose) return;
        restPosition = transform.position;
        restRotation = transform.rotation;
        restScale = transform.localScale;
        hasRestPose = true;
    }

    private void RefreshHighlight()
    {
        bool focused = (pointerHovered || interactorFocused) && CanCollect() &&
                       (!pointerHovered || !MobileInputGuard.IsPointerOverUI(pointerPosition));
        if (highlight != null) highlight.SetFocused(focused);
    }

    private void ClearPointerFocus()
    {
        pointerHovered = false;
        if (highlight != null) highlight.SetFocused(false);
    }
}
