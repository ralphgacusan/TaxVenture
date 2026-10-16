using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Presentation-only motion for the desk documents: the open document rises
/// off the desk, closing flies it back to its stack, closed items settle
/// onto the desk, and leaving the desk returns both documents to their
/// original spots. Callers keep ownership of gameplay state; they pass the
/// state change in as a completion callback. Every tween can be completed
/// instantly, so no object is ever left mid-flight.
/// </summary>
[DisallowMultipleComponent]
public sealed class DeskDocumentAnimator : MonoBehaviour
{
    [Header("Documents")]
    [SerializeField] private CaseFolder3DDrag caseFolder;
    [SerializeField] private TaxCodeBook3DDrag taxCodeBook;

    [Tooltip("Return both documents to their original desk spots when the player exits the desk.")]
    [SerializeField] private bool returnDocumentsOnExit = true;

    [Header("Timing (seconds)")]
    [SerializeField, Min(0f)] private float openDuration = 0.38f;
    [SerializeField, Min(0f)] private float closeDuration = 0.26f;
    [SerializeField, Min(0f)] private float settleDuration = 0.2f;
    [SerializeField, Min(0f)] private float slideDuration = 0.32f;

    [Header("Shape")]
    [Tooltip("Open documents grow from this fraction of their size.")]
    [SerializeField, Range(0.1f, 1f)] private float openStartScale = 0.45f;

    [Tooltip("Degrees the open document starts tipped back toward the desk.")]
    [SerializeField, Range(0f, 90f)] private float openStartTilt = 55f;

    [Tooltip("Height closed items drop from when they land on the desk.")]
    [SerializeField, Min(0f)] private float settleDropHeight = 0.04f;

    [Tooltip("Peak height of the arc when an item slides back to its spot.")]
    [SerializeField, Min(0f)] private float slideArcHeight = 0.05f;

    private sealed class Tween
    {
        public Coroutine Routine;
        public Action Complete;
    }

    private readonly Dictionary<Transform, Tween> tweens = new Dictionary<Transform, Tween>();
    private readonly List<Transform> completeBuffer = new List<Transform>();

    private void Awake()
    {
        if (caseFolder == null)
            caseFolder = GetComponentInChildren<CaseFolder3DDrag>(true);
        if (taxCodeBook == null)
            taxCodeBook = GetComponentInChildren<TaxCodeBook3DDrag>(true);
    }

    private void OnEnable()
    {
        CameraController.WorkstationExited += HandleWorkstationExited;
    }

    private void OnDisable()
    {
        CameraController.WorkstationExited -= HandleWorkstationExited;
        CompleteAll();
    }

    /// <summary>
    /// Grows the open document from <paramref name="fromPosition"/> up to the
    /// pose it currently has, which is treated as its final pose.
    /// </summary>
    public void PlayOpen(Transform model, Vector3 fromPosition)
    {
        if (model == null)
            return;

        Complete(model);
        if (openDuration <= 0f || !isActiveAndEnabled)
            return;

        Vector3 toPosition = model.position;
        Quaternion toRotation = model.rotation;
        Vector3 toScale = model.localScale;
        Quaternion fromRotation = Quaternion.AngleAxis(openStartTilt, TiltAxis(toPosition)) * toRotation;
        Vector3 fromScale = toScale * openStartScale;

        Run(model, openDuration, t =>
        {
            float grow = DeskMotion.EaseOutBack(t, 1.2f);
            model.SetPositionAndRotation(
                Vector3.LerpUnclamped(fromPosition, toPosition, DeskMotion.EaseOutCubic(t)),
                Quaternion.Slerp(fromRotation, toRotation, DeskMotion.EaseOutCubic(t)));
            model.localScale = Vector3.LerpUnclamped(fromScale, toScale, grow);
        }, () =>
        {
            model.SetPositionAndRotation(toPosition, toRotation);
            model.localScale = toScale;
        });
    }

    /// <summary>
    /// Shrinks the open document down to <paramref name="toPosition"/>, then
    /// restores its pose and runs <paramref name="onComplete"/>, which should
    /// hide it and show the closed item.
    /// </summary>
    public void PlayClose(Transform model, Vector3 toPosition, Action onComplete)
    {
        if (model == null)
        {
            onComplete?.Invoke();
            return;
        }

        Complete(model);
        if (closeDuration <= 0f || !isActiveAndEnabled)
        {
            onComplete?.Invoke();
            return;
        }

        Vector3 fromPosition = model.position;
        Quaternion fromRotation = model.rotation;
        Vector3 fromScale = model.localScale;
        Quaternion toRotation = Quaternion.AngleAxis(openStartTilt, TiltAxis(fromPosition)) * fromRotation;
        Vector3 toScale = fromScale * openStartScale;

        // Nothing should grab the document while it is flying away.
        Collider modelCollider = model.GetComponent<Collider>();
        bool colliderWasEnabled = modelCollider != null && modelCollider.enabled;
        if (modelCollider != null)
            modelCollider.enabled = false;

        Run(model, closeDuration, t =>
        {
            float e = DeskMotion.EaseInCubic(t);
            model.SetPositionAndRotation(
                Vector3.Lerp(fromPosition, toPosition, e),
                Quaternion.Slerp(fromRotation, toRotation, e));
            model.localScale = Vector3.Lerp(fromScale, toScale, e);
        }, () =>
        {
            model.SetPositionAndRotation(fromPosition, fromRotation);
            model.localScale = fromScale;
            if (modelCollider != null)
                modelCollider.enabled = colliderWasEnabled;
            onComplete?.Invoke();
        });
    }

    /// <summary>Drops a closed item the last few centimetres onto the desk.</summary>
    public void PlaySettle(Transform item)
    {
        if (item == null)
            return;

        Complete(item);
        if (settleDuration <= 0f || settleDropHeight <= 0f || !isActiveAndEnabled)
            return;

        Vector3 rest = item.position;
        Vector3 up = transform.up;
        Run(item, settleDuration, t =>
        {
            item.position = rest + up * (settleDropHeight * (1f - DeskMotion.EaseInQuad(t)));
        }, () => item.position = rest);
    }

    /// <summary>Carries a closed item along a low arc back to a resting pose.</summary>
    public void PlaySlide(Transform item, Vector3 toPosition, Quaternion toRotation)
    {
        if (item == null)
            return;

        Complete(item);
        if (slideDuration <= 0f || !isActiveAndEnabled)
        {
            item.SetPositionAndRotation(toPosition, toRotation);
            return;
        }

        Vector3 fromPosition = item.position;
        Quaternion fromRotation = item.rotation;
        Vector3 up = transform.up;
        Run(item, slideDuration, t =>
        {
            float e = DeskMotion.EaseOutCubic(t);
            Vector3 arc = up * (slideArcHeight * Mathf.Sin(t * Mathf.PI));
            item.SetPositionAndRotation(Vector3.Lerp(fromPosition, toPosition, e) + arc,
                Quaternion.Slerp(fromRotation, toRotation, e));
        }, () => item.SetPositionAndRotation(toPosition, toRotation));
    }

    /// <summary>Snaps a running tween on <paramref name="target"/> to its end.</summary>
    public void Complete(Transform target)
    {
        if (target == null || !tweens.TryGetValue(target, out Tween tween))
            return;

        tweens.Remove(target);
        if (tween.Routine != null)
            StopCoroutine(tween.Routine);
        tween.Complete?.Invoke();
    }

    public void CompleteAll()
    {
        completeBuffer.Clear();
        completeBuffer.AddRange(tweens.Keys);
        for (int i = 0; i < completeBuffer.Count; i++)
            Complete(completeBuffer[i]);
        completeBuffer.Clear();
    }

    private void HandleWorkstationExited()
    {
        if (!returnDocumentsOnExit)
            return;

        CompleteAll();
        if (caseFolder != null)
            caseFolder.ReturnToOriginalPlace(true);
        if (taxCodeBook != null)
            taxCodeBook.ReturnToOriginalPlace(true);
    }

    /// <summary>
    /// Horizontal axis that tips a camera-facing document back toward the
    /// desk when rotated by a positive angle.
    /// </summary>
    private Vector3 TiltAxis(Vector3 documentPosition)
    {
        Vector3 up = transform.up;
        Camera view = Camera.main;
        Vector3 towardViewer = view != null
            ? Vector3.ProjectOnPlane(view.transform.position - documentPosition, up)
            : Vector3.ProjectOnPlane(-transform.forward, up);
        if (towardViewer.sqrMagnitude < 0.0001f)
            towardViewer = Vector3.ProjectOnPlane(-transform.forward, up);
        return Vector3.Cross(towardViewer.normalized, up);
    }

    private void Run(Transform target, float duration, Action<float> step, Action complete)
    {
        Tween tween = new Tween { Complete = complete };
        tweens[target] = tween;
        tween.Routine = StartCoroutine(Animate(target, tween, duration, step));
    }

    private IEnumerator Animate(Transform target, Tween tween, float duration, Action<float> step)
    {
        for (float elapsed = 0f; elapsed < duration; elapsed += Time.unscaledDeltaTime)
        {
            if (target == null)
            {
                tweens.Remove(target);
                yield break;
            }

            step(elapsed / duration);
            yield return null;
        }

        // Complete() may already have finished this tween.
        if (tweens.TryGetValue(target, out Tween current) && current == tween)
        {
            tweens.Remove(target);
            tween.Complete?.Invoke();
        }
    }
}
