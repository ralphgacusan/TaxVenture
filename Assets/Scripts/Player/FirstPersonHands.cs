using UnityEngine;

/// <summary>
/// PURPOSE:
/// Controls visibility of simple placeholder "hands" (two capsules/cubes) that
/// appear in first-person workstation view, per the design doc requirement:
/// "The player's hands are visible on screen to reinforce the first-person
/// experience."
///
/// RESPONSIBILITIES:
/// - Show/hide the hands GameObject(s) on command
///
/// DOES NOT:
/// - Handle any animation (no animations exist yet per Phase 1 scope) — just
///   simple show/hide of static greybox placeholders.
///
/// CONNECTS WITH:
/// - CameraController calls Show()/Hide() when entering/exiting first-person mode.
/// - Should be parented under the Camera so the hands move with it.
///
/// Milestone T03: in the outdoor third-person view the camera is behind
/// Elijah, so camera-mounted hands must not float in the shot. Show/Hide still
/// record what every caller wants (IsShowing reports it, so snapshots and
/// restores keep working), but the hands only appear in first person. The
/// SAME carried-document object moves to Elijah's left hand in third person
/// and back to its own place in first person (one carried-paper authority).
/// </summary>
public class FirstPersonHands : MonoBehaviour
{
    // ADD THIS
    public static FirstPersonHands Instance { get; private set; }

    [Tooltip("Parent object containing the placeholder hand meshes. Assign the object that holds both hand capsules.")]
    [SerializeField] private GameObject handsRoot;

    [Header("Carried Document (left hand)")]
    [SerializeField] private GameObject carriedDocumentVisual;

    private bool handsWanted = true;
    private bool thirdPerson;
    private Transform carriedHome;
    private Vector3 carriedLocalPosition;
    private Quaternion carriedLocalRotation;
    private Vector3 carriedLocalScale;
    private bool carriedHomeKnown;

    private void Awake()
    {
        // ADD THIS
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (handsRoot != null)
            handsRoot.SetActive(true);

        HideCarriedDocument();
    }

    /// <summary>Whether the hands are currently visible (captured before a conversation hides them).</summary>
    public bool IsShowing => handsRoot != null && handsWanted;

    /// <summary>Whether the carried document is currently visible.</summary>
    public bool IsShowingCarriedDocument => carriedDocumentVisual != null && carriedDocumentVisual.activeSelf;

    /// <summary>T03: the outdoor third-person view is on (hands hidden, carried paper in Elijah's hand).</summary>
    public bool ThirdPersonView => thirdPerson;

    public void Show()
    {
        handsWanted = true;
        if (handsRoot != null) handsRoot.SetActive(!thirdPerson);
    }

    public void Hide()
    {
        handsWanted = false;
        if (handsRoot != null) handsRoot.SetActive(false);
    }


    public void ShowCarriedDocument()
    {
        if (carriedDocumentVisual != null) carriedDocumentVisual.SetActive(true);
    }

    public void HideCarriedDocument()
    {
        if (carriedDocumentVisual != null) carriedDocumentVisual.SetActive(false);
    }

    /// <summary>
    /// T03: switch between first person (hands as wanted, paper in its own place) and the outdoor
    /// third-person view (no camera hands; the same paper rides <paramref name="carryHand"/> if given).
    /// </summary>
    public void SetThirdPersonView(bool on, Transform carryHand)
    {
        thirdPerson = on;
        if (handsRoot != null) handsRoot.SetActive(handsWanted && !on);
        if (carriedDocumentVisual == null) return;
        Transform paper = carriedDocumentVisual.transform;
        if (!carriedHomeKnown)
        {
            carriedHome = paper.parent;
            carriedLocalPosition = paper.localPosition;
            carriedLocalRotation = paper.localRotation;
            carriedLocalScale = paper.localScale;
            carriedHomeKnown = true;
        }
        if (on && carryHand != null)
        {
            // Held flat against the palm side, small enough for a hand, at world scale.
            paper.SetParent(carryHand, false);
            paper.localPosition = new Vector3(0f, 0.08f, 0.04f) / Mathf.Max(1e-4f, carryHand.lossyScale.y);
            paper.localRotation = Quaternion.Euler(0f, 0f, 90f);
            float parent = Mathf.Max(1e-4f, carryHand.lossyScale.y);
            paper.localScale = carriedLocalScale * (0.6f / parent);
        }
        else if (carriedHome != null || !on)
        {
            paper.SetParent(carriedHome, false);
            paper.localPosition = carriedLocalPosition;
            paper.localRotation = carriedLocalRotation;
            paper.localScale = carriedLocalScale;
        }
    }
}
