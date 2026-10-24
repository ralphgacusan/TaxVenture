using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

/// <summary>
/// PURPOSE:
/// Marie's approved Client Copy floating beside the Auditor (Campaign
/// Foundation T08). A visual-only copy of the desk's closed folder model
/// (scripts and colliders stripped, one tap collider added) with a CLIENT
/// COPY stamp on its cover. It pops up and hovers exactly like a plaza
/// profile paper (WorldPickupMotion); a tap within reach flies it into the
/// case-folder HUD icon, then it reports the pickup and removes itself.
/// Presentation and input only: ClientCopyHandover decides what the pickup means.
/// </summary>
public sealed class ClientCopyPickup : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    private const float BobHeight = 0.05f;
    private const float BobSpeed = 2f;
    private const float DropDuration = 0.65f;
    private const float FlightDuration = 0.6f;

    private Action pickedUp;
    private RectTransform icon;
    private Vector3 rest;
    private Quaternion restRotation;
    private Vector3 dropFrom;
    private float dropElapsed = -1f;
    private bool flying;
    private float flightElapsed;
    private Camera flightCamera;
    private Vector2 flightFrom;
    private float flightFromDepth;
    private float flightToDepth;
    private Quaternion flightFromRotation;
    private Vector3 flightFromScale;
    private HighlightEffect highlight;
    private bool hovered;

    /// <summary>
    /// Builds the floating copy from `model` (cloned, never moved) at rest,
    /// popping up from dropFrom (null: already resting, e.g. on resume).
    /// </summary>
    public static ClientCopyPickup Create(Scene scene, GameObject model, Vector3 rest, Quaternion restRotation,
        Vector3? dropFrom, RectTransform icon, Action pickedUp)
    {
        if (model == null) return null;
        var holder = new GameObject("Client Copy (building)");
        holder.SetActive(false);
        SceneManager.MoveGameObjectToScene(holder, scene);
        GameObject visual = Instantiate(model, holder.transform);
        visual.name = "Model";
        visual.SetActive(true);
        foreach (MonoBehaviour script in visual.GetComponentsInChildren<MonoBehaviour>(true)) DestroyImmediate(script);
        foreach (Collider collider in visual.GetComponentsInChildren<Collider>(true)) DestroyImmediate(collider);
        foreach (Rigidbody body in visual.GetComponentsInChildren<Rigidbody>(true)) DestroyImmediate(body);
        foreach (Renderer renderer in visual.GetComponentsInChildren<Renderer>(true)) renderer.enabled = true;

        var root = new GameObject("Client Copy");
        SceneManager.MoveGameObjectToScene(root, scene);
        root.layer = model.layer;
        visual.transform.SetParent(root.transform, false);
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localRotation = Quaternion.identity;
        visual.transform.localScale = model.transform.lossyScale;
        Destroy(holder);

        // One collider around the model for taps; the stamp lies on its top face (the cover).
        Bounds bounds = LocalBounds(root.transform);
        var box = root.AddComponent<BoxCollider>();
        box.center = bounds.center;
        box.size = bounds.size + Vector3.one * 0.04f;
        AddCoverStamp(root.transform, bounds);

        var pickup = root.AddComponent<ClientCopyPickup>();
        pickup.pickedUp = pickedUp;
        pickup.icon = icon;
        pickup.rest = rest;
        pickup.restRotation = restRotation;
        if (dropFrom.HasValue)
        {
            pickup.dropFrom = dropFrom.Value;
            pickup.dropElapsed = 0f;
            root.transform.SetPositionAndRotation(dropFrom.Value, restRotation);
        }
        else root.transform.SetPositionAndRotation(rest, restRotation);
        return pickup;
    }

    private static Bounds LocalBounds(Transform root)
    {
        bool any = false;
        Bounds bounds = new Bounds(Vector3.zero, Vector3.one * 0.2f);
        foreach (MeshFilter filter in root.GetComponentsInChildren<MeshFilter>(true))
        {
            if (filter.sharedMesh == null) continue;
            Bounds mesh = filter.sharedMesh.bounds;
            Matrix4x4 toRoot = root.worldToLocalMatrix * filter.transform.localToWorldMatrix;
            for (int i = 0; i < 8; i++)
            {
                Vector3 corner = mesh.center + Vector3.Scale(mesh.extents,
                    new Vector3((i & 1) == 0 ? -1f : 1f, (i & 2) == 0 ? -1f : 1f, (i & 4) == 0 ? -1f : 1f));
                Vector3 p = toRoot.MultiplyPoint3x4(corner);
                if (!any) { bounds = new Bounds(p, Vector3.zero); any = true; }
                else bounds.Encapsulate(p);
            }
        }
        return bounds;
    }

    private static void AddCoverStamp(Transform root, Bounds bounds)
    {
        var host = new GameObject("Cover Stamp", typeof(RectTransform));
        host.layer = root.gameObject.layer;
        var rect = (RectTransform)host.transform;
        rect.SetParent(root, false);
        rect.localPosition = new Vector3(bounds.center.x, bounds.max.y + 0.003f, bounds.center.z);
        // Lying on the cover (+Y): the canvas faces out of the cover and its "up" is the folder's top edge (+Z).
        rect.localRotation = Quaternion.Euler(90f, 0f, 0f);
        const float unitsPerMetre = 1000f;
        rect.localScale = Vector3.one / unitsPerMetre;
        float width = Mathf.Max(0.05f, Mathf.Min(bounds.size.x, bounds.size.z) * 0.8f) * unitsPerMetre;
        rect.sizeDelta = new Vector2(width, width);
        var canvas = host.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        ClientCopyStamp.Build(rect, width, Vector2.zero);
    }

    private void Awake() => highlight = HighlightEffect.For(this);

    private void Update()
    {
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
            if (progress >= 1f) dropElapsed = -1f;
            return;
        }
        transform.position = WorldPickupMotion.Hover(rest, Time.time, BobSpeed, BobHeight);
        transform.rotation = restRotation;
    }

    private void UpdateFlight()
    {
        if (flightCamera == null || icon == null)
        {
            Finish();
            return;
        }
        flightElapsed += Time.deltaTime;
        float progress = Mathf.Clamp01(flightElapsed / FlightDuration);
        WorldPickupMotion.Fly(transform, flightCamera, flightFrom, IconPoint(), flightFromDepth, flightToDepth,
            flightFromRotation, flightFromScale, progress);
        if (progress >= 1f) Finish();
    }

    private Vector2 IconPoint() => RectTransformUtility.WorldToScreenPoint(null, icon.TransformPoint(icon.rect.center));

    private void Finish()
    {
        if (!enabled) return;
        enabled = false;
        if (highlight != null) highlight.SetFocused(false);
        Action done = pickedUp;
        pickedUp = null;
        Destroy(gameObject);
        done?.Invoke();
    }

    // =========================================================
    // INPUT (same guards as the plaza profile papers)
    // =========================================================

    public void OnPointerEnter(PointerEventData eventData)
    {
        hovered = eventData != null && !DialogueUI.IsAnyDialogueActive && !VirtualJoystick.BlocksWorldInput &&
                  !MobileInputGuard.IsPointerOverUI(eventData.position);
        RefreshHighlight();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        hovered = false;
        RefreshHighlight();
    }

    private void RefreshHighlight()
    {
        bool focused = hovered && !flying && dropElapsed < 0f && !DialogueUI.IsAnyDialogueActive && !VirtualJoystick.BlocksWorldInput;
        if (highlight != null) highlight.SetFocused(focused);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData != null && (eventData.button != PointerEventData.InputButton.Left || MobileInputGuard.IsDragOrCancelled(eventData) ||
            VirtualJoystick.BlocksWorldInput || MobileInputGuard.IsPointerOverUI(eventData.position))) return;
        if (flying || dropElapsed >= 0f || DialogueUI.IsAnyDialogueActive) return;
        if (eventData != null && !InteractionRange.PlayerCanReach(this)) return;
        StartFlight(eventData != null ? eventData.pressEventCamera : null);
    }

    /// <summary>
    /// A tap meant for the copy that landed on the Auditor (his collider and
    /// talk prompt sit right beside it): pick it up the same way, no dialogue.
    /// False while it is still popping out or already flying.
    /// </summary>
    public bool TryPickUpFromAuditor()
    {
        if (flying || dropElapsed >= 0f || DialogueUI.IsAnyDialogueActive) return false;
        StartFlight(null);
        return true;
    }

    private void StartFlight(Camera inputCamera)
    {
        AudioManager.Instance?.QueueInteractionSFX("paper");
        flightCamera = inputCamera != null ? inputCamera : Camera.main;
        if (flightCamera == null || icon == null)
        {
            Finish();
            return;
        }
        Vector3 start = flightCamera.WorldToScreenPoint(transform.position);
        if (start.z <= flightCamera.nearClipPlane)
        {
            Finish();
            return;
        }
        hovered = false;
        flightFrom = new Vector2(start.x, start.y);
        flightFromDepth = start.z;
        flightToDepth = WorldPickupMotion.FlightEndDepth(flightCamera, start.z);
        flightFromRotation = transform.rotation;
        flightFromScale = transform.localScale;
        flightElapsed = 0f;
        flying = true;
    }
}
