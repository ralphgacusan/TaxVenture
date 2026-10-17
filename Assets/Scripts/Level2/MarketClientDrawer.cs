using System.Collections;
using UnityEngine;

/// <summary>
/// PURPOSE:
/// The labeled market-client drawer on the file storage cabinet (Level 2).
/// Tapping it while the review waits for the folder slides the drawer out,
/// lifts Rosa's folder and flies it into the HUD Case Folder icon (stored
/// there, count 0 -> 1), then closes the drawer. Built at
/// runtime by Level2CaseRetrieval; a separate object from
/// FilingCabinetInteractable, whose archive action is unchanged.
///
/// The domain commit (Level2CaseRetrieval.TryRetrieve) happens once, at the
/// tap, before any animation; the animation never decides anything and a
/// teardown mid-motion just snaps to the end pose.
/// </summary>
public sealed class MarketClientDrawer : MonoBehaviour, IInteractable
{
    // Initial motion values (WORLD_AND_FEEDBACK: slide 0.15-0.25 m).
    public const float SlideDistance = 0.2f;
    public const float SlideSeconds = 0.35f;
    public const float LiftHeight = 0.22f;
    public const float LiftSeconds = 0.3f;
    public const float ToPlayerSeconds = 0.35f;
    public const float CloseSeconds = 0.35f;

    public const string BeforeReceptionistMessage = "Talk to the receptionist first.";
    public const string AlreadyTakenMessage = "You already have Rosa's folder.";

    private Level2CaseRetrieval owner;
    private Transform slider;
    private Transform folder;
    private Vector3 sliderClosed;
    private Vector3 folderRest;
    private Vector3 folderScale;
    private HighlightEffect highlight;
    private Coroutine motion;

    /// <summary>True while the drawer animation plays.</summary>
    public bool IsPlaying => motion != null;

    public void Init(Level2CaseRetrieval retrieval, Transform drawerSlider, Transform drawerFolder)
    {
        owner = retrieval;
        slider = drawerSlider;
        folder = drawerFolder;
        sliderClosed = slider.localPosition;
        folderRest = folder.localPosition;
        folderScale = folder.localScale;
        highlight = HighlightEffect.For((Component)this);
    }

    /// <summary>Shows the drawer as already emptied (e.g. a resumed review).</summary>
    public void ShowEmpty()
    {
        if (folder != null) folder.gameObject.SetActive(false);
    }

    public void OnFocus() { if (highlight != null) highlight.Highlight(); }

    public void OnUnfocus() { if (highlight != null) highlight.Unhighlight(); }

    public void OnInteract()
    {
        if (IsPlaying || owner == null) return;
        CaseReviewState review = Level2OfficeStart.Review;
        if (review == null) return;
        if (review.FolderRetrieved)
        {
            Level2Say.Line(AlreadyTakenMessage);
            return;
        }
        if (review.phase < ReviewPhase.RetrieveFolder)
        {
            Level2Say.Line(BeforeReceptionistMessage);
            return;
        }
        if (!owner.TryRetrieve()) return;
        motion = StartCoroutine(PlayRetrieval());
    }

    public string GetPromptText()
    {
        CaseReviewState review = Level2OfficeStart.Review;
        if (review == null || IsPlaying) return "Market-client drawer";
        if (review.FolderRetrieved) return "Market-client drawer (empty)";
        if (review.phase < ReviewPhase.RetrieveFolder) return "Market-client drawer";
        return "Take Rosa's folder";
    }

    private IEnumerator PlayRetrieval()
    {
        PlayerControlHold.Acquire(this);
        if (highlight != null) highlight.Unhighlight();

        Vector3 open = sliderClosed + Vector3.forward * SlideDistance;
        yield return Move(slider, sliderClosed, open, SlideSeconds);

        if (AudioManager.Instance != null) AudioManager.Instance.PlayPaperSFX();
        Vector3 lifted = folderRest + Vector3.up * LiftHeight;
        yield return Move(folder, folderRest, lifted, LiftSeconds);

        // Into the HUD Case Folder icon (a point just in front of the camera
        // under the icon), shrinking as it goes.
        Camera view = Camera.main;
        Vector3 start = folder.position;
        Vector3 end = start;
        if (view != null)
            end = owner.TryFolderIconScreenPoint(out Vector2 iconPoint)
                ? view.ScreenPointToRay(iconPoint).GetPoint(0.4f)
                : view.transform.position + view.transform.forward * 0.35f - view.transform.up * 0.15f;
        float t = 0f;
        while (t < 1f)
        {
            t = Mathf.Min(1f, t + Time.deltaTime / ToPlayerSeconds);
            float e = Mathf.SmoothStep(0f, 1f, t);
            folder.position = Vector3.Lerp(start, end, e);
            folder.localScale = Vector3.Lerp(folderScale, folderScale * 0.15f, e);
            yield return null;
        }
        folder.gameObject.SetActive(false);
        owner.StoreRetrievedFolder();

        yield return Move(slider, open, sliderClosed, CloseSeconds);
        Finish();
    }

    private static IEnumerator Move(Transform target, Vector3 from, Vector3 to, float seconds)
    {
        float t = 0f;
        while (t < 1f)
        {
            t = Mathf.Min(1f, t + Time.deltaTime / seconds);
            target.localPosition = Vector3.Lerp(from, to, Mathf.SmoothStep(0f, 1f, t));
            yield return null;
        }
    }

    /// <summary>End pose: drawer closed, folder gone (stored in the icon), controls released.</summary>
    private void Finish()
    {
        motion = null;
        if (owner != null) owner.StoreRetrievedFolder();
        if (slider != null) slider.localPosition = sliderClosed;
        if (folder != null)
        {
            folder.localPosition = folderRest;
            folder.localScale = folderScale;
            folder.gameObject.SetActive(false);
        }
        PlayerControlHold.Release(this);
    }

    private void OnDisable()
    {
        if (motion != null)
        {
            StopCoroutine(motion);
            Finish();
        }
        PlayerControlHold.Release(this);
    }
}
