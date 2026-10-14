using UnityEngine;

public class CorkboardSlot : MonoBehaviour
{
    [Header("Setup")]
    [SerializeField] private TaxpayerClassification expectedClassification;

    [Header("Snap Point")]
    [SerializeField] private Transform snapPoint;
    [SerializeField] private Collider dropArea;

    public bool IsSolved { get; private set; }
    public ProfileDragCard OccupyingCard { get; private set; }
    public int PlacementAttempts { get; private set; }
    public TaxpayerClassification ExpectedClassification => expectedClassification;
    public Vector3 SnapPosition => snapPoint != null ? snapPoint.position : transform.position;
    public Quaternion SnapRotation => snapPoint != null ? snapPoint.rotation : transform.rotation;
    public Collider DropArea => dropArea != null ? dropArea : GetComponent<Collider>();

    private void Awake()
    {
        if (dropArea == null)
        {
            dropArea = GetComponent<Collider>();
        }
    }

    /// <summary>Returns this area's collider bounds in the board's local XY plane.</summary>
    public bool TryGetBoardLocalBounds(Transform board, out Bounds bounds)
    {
        bounds = default;
        Collider area = DropArea;
        if (board == null || area == null || !area.enabled)
        {
            return false;
        }

        bool hasPoint = false;
        if (area is BoxCollider box)
        {
            Vector3 half = box.size * 0.5f;
            for (int x = -1; x <= 1; x += 2)
            for (int y = -1; y <= 1; y += 2)
            for (int z = -1; z <= 1; z += 2)
            {
                Vector3 boxPoint = box.center + Vector3.Scale(half, new Vector3(x, y, z));
                AddBoardPoint(board.InverseTransformPoint(box.transform.TransformPoint(boxPoint)), ref bounds, ref hasPoint);
            }
        }
        else
        {
            Bounds worldBounds = area.bounds;
            Vector3 half = worldBounds.extents;
            for (int x = -1; x <= 1; x += 2)
            for (int y = -1; y <= 1; y += 2)
            for (int z = -1; z <= 1; z += 2)
            {
                Vector3 point = worldBounds.center + Vector3.Scale(half, new Vector3(x, y, z));
                AddBoardPoint(board.InverseTransformPoint(point), ref bounds, ref hasPoint);
            }
        }

        return hasPoint;
    }

    /// <summary>
    /// Called after the card's pin animation is visible. One area holds one card;
    /// replacing it returns the previous card to its tray origin.
    /// </summary>
    public bool TryPlaceCard(ProfileDragCard card)
    {
        if (card == null || card.Profile == null)
        {
            return false;
        }

        if (OccupyingCard != null && OccupyingCard != card)
        {
            ProfileDragCard previous = OccupyingCard;
            OccupyingCard = null;
            IsSolved = false;
            previous.ReturnToOrigin();
        }

        OccupyingCard = card;
        PlacementAttempts++;
        IsSolved = card.Profile.CorrectClassification == expectedClassification;
        return IsSolved;
    }

    /// <summary>Called when a placed card is picked back up.</summary>
    public void VacateSlot(ProfileDragCard card)
    {
        if (OccupyingCard == card)
        {
            OccupyingCard = null;
            IsSolved = false;
        }
    }

    public void ResetRuntimeState()
    {
        OccupyingCard = null;
        IsSolved = false;
        PlacementAttempts = 0;
    }

    private static void AddBoardPoint(Vector3 point, ref Bounds bounds, ref bool hasPoint)
    {
        if (hasPoint)
        {
            bounds.Encapsulate(point);
        }
        else
        {
            bounds = new Bounds(point, Vector3.zero);
            hasPoint = true;
        }
    }
}
