using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// The discoverable Level 1 chest that releases the existing paper-style Notes pickup.
/// Opening is checkpointed before the lid or paper changes, so a failed write stays retryable.
/// </summary>
public sealed class NotesOnboardingChest : MonoBehaviour, IInteractable, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    public const string OpenedCue = "NotesChestOpened";

    [SerializeField] private Transform lid;
    [SerializeField] private Vector3 openEuler = new Vector3(-105f, 0f, 0f);
    [SerializeField] private float openSeconds = 0.5f;
    [Tooltip("Chest-local height inside the chest where the paper starts rising.")]
    [SerializeField] private float paperSourceHeight = 0.4f;
    [Tooltip("Chest-local spot and size of the open paper (chest units, so it grows with the chest).")]
    [SerializeField] private Vector3 paperLocalPosition = new Vector3(0f, 0.95f, 0.1f);
    [SerializeField] private Vector3 paperLocalScale = new Vector3(0.42f, 0.47f, 1f);

    private NotesChestPaperPickup paper;
    private Quaternion closedRotation;
    private HighlightEffect highlight;
    private Vector2 pointerPosition;
    private bool pointerHovered;
    private bool interactorFocused;
    private bool opened;
    private bool opening;
    private bool discovered;
    private bool ownershipRestored;

    public bool IsOpen => opened;
    public Vector3 PaperLocalPosition => paperLocalPosition;
    public Vector3 PaperLocalScale => paperLocalScale;

    private void Awake()
    {
        NotesChestPlacement placement = NotesChestPlacement.Find(gameObject.scene);
        if (placement != null) placement.Apply(transform);
        if (lid == null) lid = transform.Find("SM_Gen_Prop_Chest_01_Lid_01");
        if (lid != null) closedRotation = lid.localRotation;
        FindPaper();
        AttachPaper();
        highlight = HighlightEffect.For((IInteractable)this);
    }

    private void Start()
    {
        if (!opened && paper != null) paper.HideForChest();
        RefreshHighlight();
    }

    private void Update()
    {
        HudHotbar hotbar = HudHotbar.Instance;
        if (!ownershipRestored && hotbar != null && hotbar.IsReady)
        {
            ownershipRestored = true;
            // A carried notebook stays carried in the next chapter. Its copied
            // chest is already empty; do not replay an acquisition on resume.
            if (NotesAlreadyOwned()) RestoreOpen(true);
        }
        RefreshHighlight();
        bool available = CanOpenNow();
        if (highlight != null) highlight.SetAffordance(available);
        if (!discovered && available)
        {
            Camera view = Camera.main;
            Vector3 screen = view != null ? view.WorldToViewportPoint(transform.position) : Vector3.back;
            if (screen.z > 0f && screen.x > 0f && screen.x < 1f && screen.y > 0f && screen.y < 1f)
            {
                discovered = true;
                WorldEmote.Show(transform, Emotion.Surprise, 2.2f);
            }
        }
    }

    /// <summary>Quiet checkpoint restore. It never replays the lid motion or pickup drop.</summary>
    public void RestoreOpen(bool wasOpened)
    {
        if (!wasOpened) return;
        opened = true;
        opening = false;
        if (lid != null) lid.localRotation = OpenRotation;
        FindPaper();
        AttachPaper();
        if (paper == null) return;
        if (NotesAlreadyOwned()) paper.MarkCollected();
        else paper.RestoreVisible();
    }

    /// <summary>Called after a committed pickup or when an older source already owned Notes.</summary>
    public void PaperCollected()
    {
        FindPaper();
        AttachPaper();
        if (paper != null) paper.MarkCollected();
    }

    public void OnFocus()
    {
        interactorFocused = true;
        if (CanOpenNow() && highlight != null) highlight.SetFocused(true);
    }

    public void OnUnfocus()
    {
        interactorFocused = false;
        pointerHovered = false;
        if (highlight != null) highlight.SetFocused(false);
    }

    public void OnInteract() => TryOpen();

    public string GetPromptText() => CanOpenNow() ? "Tap to open chest" : "";

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
        if (InteractionRange.PlayerCanReach(this)) TryOpen();
    }

    private bool CanOpenNow()
    {
        if (opened || opening || DialogueUI.IsAnyDialogueActive || !HudHotbar.CanOpenPanels() ||
            !InteractionRange.PlayerCanReach(this)) return false;
        GuideDirector director = GuideRuntime.Instance != null ? GuideRuntime.Instance.Director : null;
        return director == null || director.FinishedLessons().Contains("bag-empty");
    }

    private void TryOpen()
    {
        if (!CanOpenNow()) return;

        // Capture() observes this pending state. Do not expose the lid or paper until it is durable.
        opened = true;
        Level1FieldCheckpointer checkpointer = Level1FieldCheckpointer.Active;
        if (checkpointer != null && !checkpointer.RequestSave())
        {
            opened = false;
            return;
        }

        opening = true;
        pointerHovered = false;
        if (highlight != null) highlight.SetFocused(false);
        if (highlight != null) highlight.SetAffordance(false);
        WorldEmote.Burst(transform, Emotion.Happy, 6);
        AudioManager.Instance?.QueueInteractionSFX("sfx_drawer_open_01");
        StartCoroutine(OpenLidAndReleasePaper());
    }

    private IEnumerator OpenLidAndReleasePaper()
    {
        Quaternion start = lid != null ? lid.localRotation : Quaternion.identity;
        Quaternion end = OpenRotation;
        float elapsed = 0f;
        float duration = Mathf.Max(0.05f, openSeconds);
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            if (lid != null) lid.localRotation = Quaternion.Slerp(start, end, WorldPickupMotion.Smooth(elapsed / duration));
            yield return null;
        }
        if (lid != null) lid.localRotation = end;
        opening = false;
        FindPaper();
        AttachPaper();
        if (paper == null || NotesAlreadyOwned())
        {
            if (paper != null) paper.MarkCollected();
            GameplayEvents.RaiseGuideCue(OpenedCue);
            yield break;
        }
        paper.ShowFrom(transform.TransformPoint(new Vector3(paperLocalPosition.x, paperSourceHeight, paperLocalPosition.z)));
        while (paper != null && paper.IsDropping) yield return null;
        GameplayEvents.RaiseGuideCue(OpenedCue);
    }

    private void FindPaper()
    {
        if (paper != null) return;
        foreach (NotesChestPaperPickup candidate in FindObjectsByType<NotesChestPaperPickup>(FindObjectsInactive.Include))
            if (candidate != null && candidate.gameObject.scene == gameObject.scene) { paper = candidate; break; }
    }

    private void AttachPaper()
    {
        if (paper == null) return;
        if (paper.transform.parent != transform) paper.transform.SetParent(transform, false);
        paper.transform.localPosition = paperLocalPosition;
        // The sheet reads from its local -Z; turn it to the chest's front (+Z, the latch side).
        paper.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
        paper.transform.localScale = paperLocalScale;
        paper.AttachToChest(this);
    }

    private Quaternion OpenRotation => closedRotation * Quaternion.Euler(openEuler);

    private static bool NotesAlreadyOwned()
    {
        HudHotbar hotbar = HudHotbar.Instance;
        return hotbar != null && InventoryRules.Owns(hotbar.Inventory, InventoryCatalogue.Notes);
    }

    private void RefreshHighlight()
    {
        bool focused = (pointerHovered || interactorFocused) && CanOpenNow() &&
                       (!pointerHovered || !MobileInputGuard.IsPointerOverUI(pointerPosition));
        if (highlight != null) highlight.SetFocused(focused);
    }
}
