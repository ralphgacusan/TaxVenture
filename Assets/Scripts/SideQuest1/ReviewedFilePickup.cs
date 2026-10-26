using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

/// <summary>
/// PURPOSE:
/// Arthur's Reviewed File floating beside him after his opening talk
/// (Campaign Foundation T30). The Auditor's Client Copy choreography (T08):
/// it pops up from about his hands, settles and hovers (WorldPickupMotion);
/// a tap within reach (or the centre-screen prompt) asks its owner to commit
/// the collection first. Only a committed collection makes it fly, toward
/// the item's place in the HUD (the Bag icon), then it reports the landing
/// and removes itself. A failed save leaves it hovering, tappable again.
/// The model is a teal copy of the desk's closed Case Folder (ReviewedFileLook: one look everywhere).
/// </summary>
public sealed class ReviewedFilePickup : MonoBehaviour, IInteractable, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    private const float BobHeight = 0.05f;
    private const float BobSpeed = 2f;
    private const float DropDuration = 0.65f;
    private const float FlightDuration = 0.6f;

    private Func<RectTransform> commit;
    private Action landed;
    private RectTransform target;
    private Vector3 rest;
    private Quaternion restRotation;
    private Vector3 dropFrom;
    private float dropElapsed = -1f;
    private bool flying;
    private float flightElapsed;
    private Camera flightCamera;
    private Vector2 flightFrom;
    private Vector2 flightTo;
    private float flightFromDepth, flightToDepth;
    private Quaternion flightFromRotation;
    private Vector3 flightFromScale;
    private HighlightEffect highlight;
    private bool hovered, interactorFocused;
    private bool announce;

    /// <summary>
    /// Builds the floating file at rest, popping up from dropFrom (null: already
    /// resting, e.g. on resume). commit returns where it flies once the
    /// collection is saved (null: not collected, it stays).
    /// </summary>
    public static ReviewedFilePickup Create(Scene scene, Vector3 rest, Quaternion restRotation, Vector3? dropFrom,
        Func<RectTransform> commit, Action landed)
    {
        var root = new GameObject("Arthur's Reviewed File (pickup)");
        SceneManager.MoveGameObjectToScene(root, scene);
        int layer = LayerMask.NameToLayer("Interactable");
        root.layer = layer < 0 ? 3 : layer;
        // The real closed Case Folder model, teal, centred on the root so it turns and hovers about its middle.
        if (ReviewedFileLook.BuildClosed(root.transform, scene, out Vector3 size) == null)
        {
            Destroy(root);
            return null;
        }
        var box = root.AddComponent<BoxCollider>();
        box.center = Vector3.zero;
        box.size = new Vector3(size.x + 0.06f, Mathf.Max(0.08f, size.y + 0.04f), size.z + 0.08f);

        var pickup = root.AddComponent<ReviewedFilePickup>();
        pickup.commit = commit;
        pickup.landed = landed;
        pickup.rest = rest;
        pickup.restRotation = restRotation;
        if (dropFrom.HasValue)
        {
            pickup.dropFrom = dropFrom.Value;
            pickup.dropElapsed = 0f;
            root.transform.SetPositionAndRotation(dropFrom.Value, restRotation);
        }
        else
        {
            root.transform.SetPositionAndRotation(rest, restRotation);
            // Resumed already resting: the guide still hears that it is out to collect.
            pickup.announce = true;
        }
        return pickup;
    }

    /// <summary>Still popping out: not tappable yet.</summary>
    public bool IsDropping => dropElapsed >= 0f;
    public bool IsFlying => flying;

    private void Awake() => highlight = HighlightEffect.For((IInteractable)this);

    private void Update()
    {
        if (announce)
        {
            announce = false;
            GameplayEvents.RaiseGuideCue(ArthurOpening.CuePresented);
        }
        RefreshHighlight();
        if (flying)
        {
            UpdateFlight();
            return;
        }
        if (dropElapsed >= 0f)
        {
            dropElapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(dropElapsed / DropDuration);
            WorldPickupMotion.Drop(transform, dropFrom, rest, restRotation, WorldPickupMotion.Hover(rest, Time.time, BobSpeed, BobHeight), progress);
            if (progress >= 1f)
            {
                dropElapsed = -1f;
                GameplayEvents.RaiseGuideCue(ArthurOpening.CuePresented);
            }
            return;
        }
        transform.position = WorldPickupMotion.Hover(rest, Time.time, BobSpeed, BobHeight);
        transform.rotation = restRotation;
    }

    private void UpdateFlight()
    {
        if (flightCamera == null || target == null)
        {
            Finish();
            return;
        }
        flightElapsed += Time.unscaledDeltaTime;
        float progress = Mathf.Clamp01(flightElapsed / FlightDuration);
        WorldPickupMotion.FlyFolder(transform, flightCamera, flightFrom, flightTo, flightFromDepth, flightToDepth, flightFromRotation, flightFromScale, progress);
        if (progress >= 1f) Finish();
    }

    private void Finish()
    {
        if (!enabled) return;
        enabled = false;
        if (highlight != null) highlight.SetFocused(false);
        if (AudioManager.Instance != null) AudioManager.Instance.PlayEffect("sfx_card_place_01", 0.8f);
        Action done = landed;
        landed = null;
        Destroy(gameObject);
        done?.Invoke();
    }

    // =========================================================
    // INPUT (same guards as the plaza papers)
    // =========================================================

    private bool CanTake() => !flying && dropElapsed < 0f && !DialogueUI.IsAnyDialogueActive && !VirtualJoystick.BlocksWorldInput &&
                              InteractionRange.PlayerCanReach(this);

    public void OnFocus()
    {
        interactorFocused = true;
        RefreshHighlight();
    }

    public void OnUnfocus()
    {
        interactorFocused = false;
        hovered = false;
        RefreshHighlight();
    }

    public void OnInteract() => TryTake(null);

    public string GetPromptText() => CanTake() ? "Click to take Arthur's file" : "";

    public void OnPointerEnter(PointerEventData eventData)
    {
        hovered = eventData != null && !MobileInputGuard.IsPointerOverUI(eventData.position);
        RefreshHighlight();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        hovered = false;
        RefreshHighlight();
    }

    private void RefreshHighlight()
    {
        if (highlight != null) highlight.SetFocused((hovered || interactorFocused) && CanTake());
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData == null || eventData.button != PointerEventData.InputButton.Left || MobileInputGuard.IsDragOrCancelled(eventData) ||
            MobileInputGuard.IsPointerOverUI(eventData.position)) return;
        TryTake(eventData.pressEventCamera);
    }

    /// <summary>A committed collection only: the flight never starts before its owner saved it.</summary>
    private void TryTake(Camera inputCamera)
    {
        if (!CanTake() || commit == null) return;
        RectTransform landing = commit();
        if (landing == null) return;
        target = landing;
        Canvas.ForceUpdateCanvases();
        flightTo = RectTransformUtility.WorldToScreenPoint(null, target.TransformPoint(target.rect.center));
        AudioManager.Instance?.QueueInteractionSFX("paper");
        flightCamera = inputCamera != null ? inputCamera : HeldOnScreen.ActiveCamera(Camera.main);
        Vector3 start = flightCamera != null ? flightCamera.WorldToScreenPoint(transform.position) : Vector3.zero;
        if (flightCamera == null || start.z <= flightCamera.nearClipPlane)
        {
            Finish();
            return;
        }
        hovered = interactorFocused = false;
        flightFrom = new Vector2(start.x, start.y);
        flightFromDepth = start.z;
        flightToDepth = WorldPickupMotion.FlightEndDepth(flightCamera, start.z);
        flightFromRotation = transform.rotation;
        flightFromScale = transform.localScale;
        flightElapsed = 0f;
        flying = true;
        GetComponent<Collider>().enabled = false;
    }
}
