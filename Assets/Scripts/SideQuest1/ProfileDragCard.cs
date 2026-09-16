using UnityEngine;
using UnityEngine.EventSystems;

public class ProfileDragCard : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [Header("References")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private Renderer cardRenderer; // NEW: the card's own visual

    [Header("Drag Settings")]
    [SerializeField] private float dragSmoothness = 20f;

    [Header("Feedback Colors")]
    [SerializeField] private Color neutralColor = Color.white;
    [SerializeField] private Color correctColor = Color.green;
    [SerializeField] private Color wrongColor = Color.red;

    public TaxpayerProfile Profile { get; private set; }
    public bool IsCorrectlyPlaced { get; private set; } // NEW

    private Vector3 originalPosition; // tray position
    private Quaternion originalRotation;

    private Plane dragPlane;
    private Vector3 dragOffset;
    private Vector3 targetPosition;
    private bool isDragging;

    private CorkboardSlot currentSlot; // NEW: which slot (if any) this card currently occupies
    private CorkboardSideQuestManager manager;

    [Header("Card Text")]
    [SerializeField] private TMPro.TMP_Text nameText;
    [SerializeField] private TMPro.TMP_Text descriptionText;

    public void SetCardText(TaxpayerProfile profile)
    {
        if (nameText != null)
        {
            nameText.text = profile.TaxpayerName;
        }

        if (descriptionText != null)
        {
            descriptionText.text = profile.Description;
        }
    }
    public void Initialize(TaxpayerProfile profile, CorkboardSideQuestManager owningManager, Camera activeCamera)
    {
        Profile = profile;
        manager = owningManager;

        if (activeCamera != null) mainCamera = activeCamera;
        else if (mainCamera == null) mainCamera = Camera.main;

        originalPosition = transform.position;
        originalRotation = transform.rotation;

        SetColor(neutralColor);
        SetCardText(profile); // NEW
    }

    private void Update()
    {
        if (!isDragging) return;

        float t = 1f - Mathf.Exp(-dragSmoothness * Time.deltaTime);
        transform.position = Vector3.Lerp(transform.position, targetPosition, t);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (mainCamera == null) return;

        // If this card currently occupies a slot, vacate it — it's being
        // picked back up regardless of whether it was correct or not.
        if (currentSlot != null)
        {
            currentSlot.VacateSlot(this);
            currentSlot = null;
            IsCorrectlyPlaced = false;
            SetColor(neutralColor);
            manager?.NotifyCardRemovedFromSlot();
        }

        dragPlane = new Plane(mainCamera.transform.forward, transform.position);
        Ray ray = mainCamera.ScreenPointToRay(eventData.position);

        if (dragPlane.Raycast(ray, out float enter))
        {
            Vector3 hitPoint = ray.GetPoint(enter);
            dragOffset = transform.position - hitPoint;
            targetPosition = transform.position;
            isDragging = true;
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging) return;

        Ray ray = mainCamera.ScreenPointToRay(eventData.position);
        if (dragPlane.Raycast(ray, out float enter))
        {
            targetPosition = ray.GetPoint(enter) + dragOffset;
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!isDragging) return;
        isDragging = false;

        CorkboardSlot targetSlot = FindSlotUnderPointer(eventData);

        if (targetSlot != null && manager != null)
        {
            bool correct = manager.HandleCardDropped(this, targetSlot);

            transform.position = targetSlot.SnapPosition;
            transform.rotation = targetSlot.SnapRotation;
            currentSlot = targetSlot;
            IsCorrectlyPlaced = correct;
            SetColor(correct ? correctColor : wrongColor);
        }
        else
        {
            ReturnToOrigin();
        }
    }


    private CorkboardSlot FindSlotUnderPointer(PointerEventData eventData)
    {
        if (mainCamera == null) return null;

        Ray ray = mainCamera.ScreenPointToRay(eventData.position);

        if (Physics.Raycast(ray, out RaycastHit hit, 100f))
        {
            return hit.collider.GetComponentInParent<CorkboardSlot>();
        }
        return null;
    }


    public void ReturnToOrigin()
    {
        currentSlot = null;
        IsCorrectlyPlaced = false;
        SetColor(neutralColor);
        transform.position = originalPosition;
        transform.rotation = originalRotation;
    }

    private void SetColor(Color color)
    {
        if (cardRenderer != null)
        {
            cardRenderer.material.color = color;
        }
    }
}