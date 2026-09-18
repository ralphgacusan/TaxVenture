using UnityEngine;

public class CorkboardSlot : MonoBehaviour
{
    [Header("Setup")]
    [SerializeField] private TaxpayerClassification expectedClassification;

    [Header("Snap Point")]
    [SerializeField] private Transform snapPoint;

    public bool IsSolved { get; private set; }
    public ProfileDragCard OccupyingCard { get; private set; } // NEW
    public TaxpayerClassification ExpectedClassification => expectedClassification;
    public Vector3 SnapPosition => snapPoint != null ? snapPoint.position : transform.position;
    public Quaternion SnapRotation => snapPoint != null ? snapPoint.rotation : transform.rotation;

    /// <summary>
    /// Called by CorkboardSideQuestManager when a card is dropped here.
    /// A slot can only hold one card at a time; dropping a new one bumps
    /// the old one back to the tray.
    /// </summary>
    public bool TryPlaceCard(ProfileDragCard card)
    {
        if (OccupyingCard != null && OccupyingCard != card)
        {
            OccupyingCard.ReturnToOrigin();
        }

        OccupyingCard = card;
        IsSolved = card.Profile.CorrectClassification == expectedClassification;

        return IsSolved;
    }

    /// <summary>Called by ProfileDragCard when it's picked back up off this slot.</summary>
    public void VacateSlot(ProfileDragCard card)
    {
        if (OccupyingCard == card)
        {
            OccupyingCard = null;
            IsSolved = false;
        }
    }
}