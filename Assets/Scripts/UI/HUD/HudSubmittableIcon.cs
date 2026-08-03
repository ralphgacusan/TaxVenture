using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// PURPOSE:
/// Dual-mode HUD icon button: a quick click opens the associated panel
/// (Case Folder / Tax Return), while a press-and-hold-then-drag submits
/// the icon's document as an IDataValueSource, per R11's revised
/// submission flow (no more standalone 3D world tokens — the HUD icons
/// themselves are now the draggable submission sources).
///
/// GESTURE DISAMBIGUATION:
/// Same movement-threshold approach used for the 3D document dragging in
/// R6 (OnMouseDown/Drag/Up), applied here via Unity's UI pointer
/// interfaces since this is a screen-space UI element, not a 3D object.
/// A pointer-down that releases before crossing the threshold is treated
/// as a click; crossing the threshold switches to drag mode and suppresses
/// the click entirely.
///
/// REUSES R8 ARCHITECTURE DIRECTLY:
/// Implements IDataValueSource exactly like every other source in the
/// game (DocumentFieldRow, CaseFolderFieldRow, TaxCodeValueChip,
/// ComputedResultSource). AuditorSubmissionTray (IDataValueDestination)
/// requires zero changes — it already only knows about the interface.
///
/// CONNECTS WITH:
/// - ValueTransferManager: SelectValue()/TryPlaceOnDestination() (called
///   manually here rather than through ValueClickSource, since this needs
///   custom click-vs-drag gating first)
/// - DragGhostIcon: the visual that follows the cursor while dragging
/// - Whatever Button/UI opens the panel on a genuine click (wired via a
///   UnityEvent, so this component doesn't need a direct reference to
///   CaseFolderUI/TaxReturnViewerUI)
/// </summary>
public class HudSubmittableIcon : MonoBehaviour, IDataValueSource, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    public enum SubmittableKind { CaseFolder, TaxReturn }

    [Header("Identity")]
    [SerializeField] private SubmittableKind kind;

    [Header("Click Behavior (quick press-release)")]
    [SerializeField] private UnityEngine.Events.UnityEvent onClickOpenPanel;

    [Header("Drag Behavior (press-hold-move)")]
    [SerializeField] private float dragThresholdPixels = 12f;
    [SerializeField] private DragGhostIcon ghostPrefab;
    [SerializeField] private Sprite ghostSprite; // paper/folder icon shown while dragging
    [SerializeField] private Canvas rootCanvas;

    [Header("Availability")]
    [Tooltip("Icon is only interactable once this is true (e.g. Tax Return only after printing).")]
    [SerializeField] private bool isAvailable = true;

    private Vector2 pointerDownScreenPos;
    private bool isDragging = false;
    private DragGhostIcon activeGhost;

    public void SetAvailable(bool available)
    {
        isAvailable = available;
        GetComponent<Image>().raycastTarget = available; // fully disables interaction when unavailable
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!isAvailable) return;
        pointerDownScreenPos = eventData.position;
        isDragging = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isAvailable) return;

        float moved = Vector2.Distance(pointerDownScreenPos, eventData.position);

        if (!isDragging && moved > dragThresholdPixels)
        {
            BeginDrag(eventData);
        }

        if (isDragging)
        {
            activeGhost.FollowPointer(eventData.position, rootCanvas);
        }
    }

    private void BeginDrag(PointerEventData eventData)
    {
        isDragging = true;
        activeGhost = Instantiate(ghostPrefab, rootCanvas.transform);
        activeGhost.SetSprite(ghostSprite);
        activeGhost.FollowPointer(eventData.position, rootCanvas);

        ValueTransferManager.Instance.SelectValue(this, this);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!isAvailable) return;

        if (!isDragging)
        {
            // Quick click, no meaningful drag -> open the panel.
            onClickOpenPanel?.Invoke();
            return;
        }

        Debug.Log($"Hovered Count: {eventData.hovered.Count}");

        foreach (var obj in eventData.hovered)
        {
            Debug.Log($"Hovered: {obj.name}");
        }

        // Was dragging -- resolve drop against whatever's under the pointer.
        IDataValueDestination destination = FindDestinationUnderPointer(eventData);

        if (destination != null)
        {
            Debug.Log("Found destination!");
            ValueTransferManager.Instance.TryPlaceOnDestination(destination);
        }
        else
        {
            Debug.Log("No IDataValueDestination found.");
            ValueTransferManager.Instance.ClearSelection();
        }

        Destroy(activeGhost.gameObject);
        activeGhost = null;
        isDragging = false;
    }

    private IDataValueDestination FindDestinationUnderPointer(PointerEventData eventData)
    {
        Camera cam = Camera.main;

        if (cam == null)
        {
            Debug.LogError("HudSubmittableIcon: No Main Camera found.");
            return null;
        }

        Ray ray = cam.ScreenPointToRay(eventData.position);

        Debug.DrawRay(ray.origin, ray.direction * 100f, Color.green, 2f);

        if (Physics.Raycast(ray, out RaycastHit hit, 100f))
        {
            Debug.Log($"Physics Hit: {hit.collider.name}");

            IDataValueDestination destination =
                hit.collider.GetComponent<IDataValueDestination>();

            if (destination == null)
            {
                destination = hit.collider.GetComponentInParent<IDataValueDestination>();
            }

            if (destination != null)
            {
                Debug.Log($"Found IDataValueDestination on {hit.collider.name}");
                return destination;
            }

            Debug.Log("Hit object is not an IDataValueDestination.");
        }
        else
        {
            Debug.Log("Physics raycast hit nothing.");
        }

        return null;
    }
    public DataValue GetDataValue()
    {
        string key = kind == SubmittableKind.CaseFolder ? "Submit_CaseFolder" : "Submit_TaxReturn";
        return new DataValue(kind, kind.ToString(), DataValueType.Text, key);
    }
}