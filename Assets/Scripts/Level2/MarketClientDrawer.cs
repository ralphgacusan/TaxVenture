using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// PURPOSE:
/// The labeled market-client drawer on the file storage cabinet (Level 2).
/// Tapping it while the review waits for the folder plays the retrieval:
/// the drawer unlatches and rolls out with a soft overshoot, the hanging
/// folders behind Rosa's riffle, her folder rises out of the drawer and turns
/// to show its name card, then arcs into the HUD Case Folder icon (stored
/// there, count 0 -> 1) while the drawer rolls shut with a small bump.
/// While the folder is waiting, the label card glows softly. Built at
/// runtime by Level2CaseRetrieval; a separate object from
/// FilingCabinetInteractable, whose archive action is unchanged.
///
/// The domain commit (Level2CaseRetrieval.TryRetrieve) happens once, at the
/// tap, before any animation; the animation never decides anything and a
/// teardown mid-motion just snaps to the end pose.
/// </summary>
public sealed class MarketClientDrawer : MonoBehaviour, IInteractable
{
    // Motion values (metres / seconds), tunable.
    public const float SlideDistance = 0.3f;
    public const float UnlatchSeconds = 0.12f;
    public const float SlideSeconds = 0.5f;
    public const float RiffleSeconds = 0.45f;
    public const float LiftHeight = 0.24f;
    public const float LiftSeconds = 0.42f;
    public const float ShowSeconds = 0.55f;
    public const float ToIconSeconds = 0.5f;
    public const float CloseSeconds = 0.4f;

    public const string BeforeReceptionistMessage = "Talk to the receptionist first.";
    public const string AlreadyTakenMessage = "You already have Rosa's folder.";

    private Level2CaseRetrieval owner;
    private Transform slider;
    private Transform folder;
    private readonly List<Transform> others = new List<Transform>();
    private readonly List<Quaternion> otherRest = new List<Quaternion>();
    private Material label;
    private Color labelRest;
    private Vector3 sliderClosed;
    private Vector3 folderRest;
    private Quaternion folderRestRotation;
    private Vector3 folderScale;
    private HighlightEffect highlight;
    private Coroutine motion;
    private bool shut;

    /// <summary>True while the drawer animation plays.</summary>
    public bool IsPlaying => motion != null;

    public void Init(Level2CaseRetrieval retrieval, Transform drawerSlider, Transform drawerFolder,
        List<Transform> otherFolders = null, Material labelCard = null)
    {
        owner = retrieval;
        slider = drawerSlider;
        folder = drawerFolder;
        sliderClosed = slider.localPosition;
        folderRest = folder.localPosition;
        folderRestRotation = folder.localRotation;
        folderScale = folder.localScale;
        others.Clear();
        otherRest.Clear();
        if (otherFolders != null)
            foreach (Transform other in otherFolders)
                if (other != null)
                {
                    others.Add(other);
                    otherRest.Add(other.localRotation);
                }
        label = labelCard;
        if (label != null) labelRest = label.color;
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

    // The label card breathes while Rosa's folder is waiting in the drawer.
    private void Update()
    {
        if (label == null) return;
        CaseReviewState review = Level2OfficeStart.Review;
        bool waiting = review != null && review.phase == ReviewPhase.RetrieveFolder && !IsPlaying;
        float glow = waiting ? 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 3f) : 0f;
        label.color = Color.Lerp(labelRest, new Color(1f, 0.93f, 0.62f, 1f), glow * 0.55f);
    }

    private IEnumerator PlayRetrieval()
    {
        PlayerControlHold.Acquire(this);
        if (highlight != null) highlight.Unhighlight();

        // Unlatch: a tiny push in, then the drawer rolls out past its stop and settles.
        if (AudioManager.Instance != null) AudioManager.Instance.PlaySelectSFX();
        Vector3 pushed = sliderClosed - Vector3.forward * 0.008f;
        yield return Move(slider, sliderClosed, pushed, UnlatchSeconds, Ease.Smooth);
        Vector3 open = sliderClosed + Vector3.forward * SlideDistance;
        yield return Move(slider, pushed, open, SlideSeconds, Ease.OutBack);

        // Riffle through the hanging folders, front to back.
        if (AudioManager.Instance != null) AudioManager.Instance.PlayPaperSFX();
        yield return Riffle();

        // Rosa's folder rises out of the drawer and turns to show its name card.
        Camera view = Camera.main;
        Vector3 liftedLocal = folderRest + Vector3.up * LiftHeight + Vector3.forward * 0.05f;
        Quaternion fromRot = folder.rotation;
        Vector3 fromPos = folder.position;
        for (float t = 0f; t < 1f;)
        {
            t = Mathf.Min(1f, t + Time.deltaTime / LiftSeconds);
            float e = EaseOutBack(t, 1.2f);
            Vector3 target = slider.TransformPoint(liftedLocal);
            folder.position = Vector3.LerpUnclamped(fromPos, target, e);
            folder.rotation = Quaternion.Slerp(fromRot, FacingCamera(view, folder.position), Mathf.SmoothStep(0f, 1f, t));
            yield return null;
        }

        // A short beat to read the name, bobbing gently.
        Vector3 shown = folder.position;
        for (float t = 0f; t < ShowSeconds; t += Time.deltaTime)
        {
            folder.position = shown + Vector3.up * (0.01f * Mathf.Sin(t * 9f));
            folder.rotation = Quaternion.Slerp(folder.rotation, FacingCamera(view, folder.position), 0.2f);
            yield return null;
        }

        // Into the HUD Case Folder icon on an arc, spinning and shrinking; the drawer starts closing.
        if (AudioManager.Instance != null) AudioManager.Instance.PlayPaperSFX();
        Vector3 start = folder.position;
        Quaternion startRot = folder.rotation;
        shut = false;
        StartCoroutine(Close(open, ToIconSeconds * 0.35f));
        for (float t = 0f; t < 1f;)
        {
            t = Mathf.Min(1f, t + Time.deltaTime / ToIconSeconds);
            float e = t * t * (3f - 2f * t);
            Vector3 end = IconPoint(view, start);
            Vector3 arc = view != null ? view.transform.up * (0.12f * Mathf.Sin(t * Mathf.PI)) : Vector3.zero;
            folder.position = Vector3.Lerp(start, end, e) + arc;
            folder.rotation = startRot * Quaternion.Euler(0f, 0f, -200f * e);
            folder.localScale = Vector3.Lerp(folderScale, folderScale * 0.12f, e * e);
            yield return null;
        }
        folder.gameObject.SetActive(false);
        owner.StoreRetrievedFolder();

        while (!shut) yield return null;
        Finish();
    }

    private IEnumerator Riffle()
    {
        float step = others.Count > 0 ? RiffleSeconds / others.Count : 0f;
        for (int i = 0; i < others.Count; i++)
            StartCoroutine(Flip(others[i], otherRest[i], step * i));
        yield return new WaitForSeconds(RiffleSeconds * 0.6f);
    }

    private static IEnumerator Flip(Transform target, Quaternion rest, float delay)
    {
        if (delay > 0f) yield return new WaitForSeconds(delay);
        const float seconds = 0.32f;
        for (float t = 0f; t < 1f;)
        {
            t = Mathf.Min(1f, t + Time.deltaTime / seconds);
            target.localRotation = rest * Quaternion.Euler(-14f * Mathf.Sin(t * Mathf.PI), 0f, 0f);
            yield return null;
        }
        target.localRotation = rest;
    }

    private IEnumerator Close(Vector3 open, float delay)
    {
        yield return new WaitForSeconds(delay);
        yield return Move(slider, open, sliderClosed, CloseSeconds, Ease.In);
        // A small bump as it hits the stop.
        if (AudioManager.Instance != null) AudioManager.Instance.PlaySelectSFX();
        yield return Move(slider, sliderClosed, sliderClosed + Vector3.forward * 0.01f, 0.06f, Ease.Smooth);
        yield return Move(slider, sliderClosed + Vector3.forward * 0.01f, sliderClosed, 0.08f, Ease.Smooth);
        shut = true;
    }

    /// <summary>Folder cover faces the camera (its name card is on local +z).</summary>
    private static Quaternion FacingCamera(Camera view, Vector3 position)
    {
        if (view == null) return Quaternion.identity;
        Vector3 toCamera = view.transform.position - position;
        return toCamera.sqrMagnitude > 1e-6f ? Quaternion.LookRotation(toCamera.normalized, Vector3.up) : Quaternion.identity;
    }

    /// <summary>A point just in front of the camera under the HUD Case Folder icon.</summary>
    private Vector3 IconPoint(Camera view, Vector3 fallback)
    {
        if (view == null) return fallback;
        return owner != null && owner.TryFolderIconScreenPoint(out Vector2 iconPoint)
            ? view.ScreenPointToRay(iconPoint).GetPoint(0.4f)
            : view.transform.position + view.transform.forward * 0.35f - view.transform.up * 0.15f;
    }

    private enum Ease { Smooth, OutBack, In }

    private static IEnumerator Move(Transform target, Vector3 from, Vector3 to, float seconds, Ease ease)
    {
        float t = 0f;
        while (t < 1f)
        {
            t = Mathf.Min(1f, t + Time.deltaTime / seconds);
            float e = ease == Ease.OutBack ? EaseOutBack(t, 1.1f) : ease == Ease.In ? t * t * t : Mathf.SmoothStep(0f, 1f, t);
            target.localPosition = Vector3.LerpUnclamped(from, to, e);
            yield return null;
        }
        target.localPosition = to;
    }

    private static float EaseOutBack(float t, float overshoot)
    {
        float c = overshoot + 1f;
        float u = t - 1f;
        return 1f + c * u * u * u + overshoot * u * u;
    }

    /// <summary>End pose: drawer closed, folder gone (stored in the icon), controls released.</summary>
    private void Finish()
    {
        motion = null;
        if (owner != null) owner.StoreRetrievedFolder();
        if (slider != null) slider.localPosition = sliderClosed;
        for (int i = 0; i < others.Count; i++)
            if (others[i] != null) others[i].localRotation = otherRest[i];
        if (folder != null)
        {
            folder.localPosition = folderRest;
            folder.localRotation = folderRestRotation;
            folder.localScale = folderScale;
            folder.gameObject.SetActive(false);
        }
        if (label != null) label.color = labelRest;
        PlayerControlHold.Release(this);
    }

    private void OnDisable()
    {
        if (motion != null)
        {
            StopAllCoroutines();
            Finish();
        }
        PlayerControlHold.Release(this);
    }
}
