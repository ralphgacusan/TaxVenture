using System.Collections;
using UnityEngine;

public class StampTransitionController : MonoBehaviour
{
    [Header("Existing Desk Stamps")]
    [SerializeField] private Transform readyStamp;
    [SerializeField] private Transform notReadyStamp;

    [Header("Highlight Effects")]
    [SerializeField] private HighlightEffect readyHighlight;
    [SerializeField] private HighlightEffect notReadyHighlight;

    [Header("Final Workspace Positions")]
    [SerializeField] private Transform readyTarget;
    [SerializeField] private Transform notReadyTarget;

    [Header("Animation")]
    [SerializeField] private float duration = 0.8f;

    [SerializeField]
    private AnimationCurve movementCurve =
        AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    private bool transitionRunning;

    public bool IsTransitionRunning => transitionRunning;

    // =========================================================
    // ORIGINAL DESK POSITIONS
    // =========================================================

    private Vector3 readyOriginalPosition;
    private Quaternion readyOriginalRotation;

    private Vector3 notReadyOriginalPosition;
    private Quaternion notReadyOriginalRotation;

    private bool originalPositionsSaved;


    // =========================================================
    // UNITY
    // =========================================================

    private void Awake()
    {
        SaveOriginalPositions();
    }


    // =========================================================
    // SAVE ORIGINAL DESK POSITIONS
    // =========================================================

    private void SaveOriginalPositions()
    {
        if (readyStamp == null || notReadyStamp == null)
        {
            Debug.LogError(
                "[StampTransition] Cannot save original positions. " +
                "Stamp references are missing."
            );

            return;
        }

        readyOriginalPosition =
            readyStamp.position;

        readyOriginalRotation =
            readyStamp.rotation;

        notReadyOriginalPosition =
            notReadyStamp.position;

        notReadyOriginalRotation =
            notReadyStamp.rotation;

        originalPositionsSaved = true;

        Debug.Log(
            "[StampTransition] Original stamp positions saved."
        );
    }


    // =========================================================
    // MOVE STAMPS TO WORKSPACE
    // =========================================================

    public void MoveStampsToWorkspace()
    {
        Debug.Log(
            "[StampTransition] MoveStampsToWorkspace() called."
        );

        if (readyStamp == null)
        {
            Debug.LogError(
                "[StampTransition] Ready Stamp is NOT assigned!"
            );

            return;
        }

        if (notReadyStamp == null)
        {
            Debug.LogError(
                "[StampTransition] Not Ready Stamp is NOT assigned!"
            );

            return;
        }

        if (readyTarget == null)
        {
            Debug.LogError(
                "[StampTransition] Ready Target is NOT assigned!"
            );

            return;
        }

        if (notReadyTarget == null)
        {
            Debug.LogError(
                "[StampTransition] Not Ready Target is NOT assigned!"
            );

            return;
        }

        if (readyHighlight != null)
            readyHighlight.Unhighlight();

        if (notReadyHighlight != null)
            notReadyHighlight.Unhighlight();

        // -----------------------------------------------------
        // PAPER SFX
        // -----------------------------------------------------

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayPaperSFX();
        }

        StopAllCoroutines();

        StartCoroutine(MoveStamps());
    }


    // =========================================================
    // MOVE TO WORKSPACE ANIMATION
    // =========================================================

    private IEnumerator MoveStamps()
    {
        transitionRunning = true;

        // -----------------------------------------------------
        // DISABLE DRAGGING WHILE MOVING
        // -----------------------------------------------------

        SetStampDragging(false);

        // -----------------------------------------------------
        // SAVE CURRENT POSITIONS
        // -----------------------------------------------------

        Vector3 readyStartPosition =
            readyStamp.position;

        Quaternion readyStartRotation =
            readyStamp.rotation;

        Vector3 notReadyStartPosition =
            notReadyStamp.position;

        Quaternion notReadyStartRotation =
            notReadyStamp.rotation;

        // -----------------------------------------------------
        // TARGET POSITIONS
        // -----------------------------------------------------

        Vector3 readyEndPosition =
            readyTarget.position;

        Quaternion readyEndRotation =
            readyTarget.rotation;

        Vector3 notReadyEndPosition =
            notReadyTarget.position;

        Quaternion notReadyEndRotation =
            notReadyTarget.rotation;

        Debug.Log(
            $"[StampTransition] Ready movement: " +
            $"{readyStartPosition} -> {readyEndPosition}"
        );

        Debug.Log(
            $"[StampTransition] Not Ready movement: " +
            $"{notReadyStartPosition} -> {notReadyEndPosition}"
        );

        // -----------------------------------------------------
        // ANIMATION
        // -----------------------------------------------------

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            float t =
                Mathf.Clamp01(
                    elapsed / duration
                );

            t = movementCurve.Evaluate(t);

            // Ready Stamp
            readyStamp.position =
                Vector3.Lerp(
                    readyStartPosition,
                    readyEndPosition,
                    t
                );

            readyStamp.rotation =
                Quaternion.Slerp(
                    readyStartRotation,
                    readyEndRotation,
                    t
                );

            // Not Ready Stamp
            notReadyStamp.position =
                Vector3.Lerp(
                    notReadyStartPosition,
                    notReadyEndPosition,
                    t
                );

            notReadyStamp.rotation =
                Quaternion.Slerp(
                    notReadyStartRotation,
                    notReadyEndRotation,
                    t
                );

            yield return null;
        }

        // -----------------------------------------------------
        // FORCE FINAL POSITION
        // -----------------------------------------------------

        readyStamp.position =
            readyEndPosition;

        readyStamp.rotation =
            readyEndRotation;

        notReadyStamp.position =
            notReadyEndPosition;

        notReadyStamp.rotation =
            notReadyEndRotation;

        // -----------------------------------------------------
        // UPDATE DRAG ORIGINS
        // -----------------------------------------------------

        Stamp3DDrag readyDrag =
            readyStamp.GetComponent<Stamp3DDrag>();

        Stamp3DDrag notReadyDrag =
            notReadyStamp.GetComponent<Stamp3DDrag>();

        if (readyDrag != null)
        {
            readyDrag.SetCurrentPositionAsOrigin();

            Debug.Log(
                "[StampTransition] " +
                "Ready Stamp drag origin updated."
            );
        }

        if (notReadyDrag != null)
        {
            notReadyDrag.SetCurrentPositionAsOrigin();

            Debug.Log(
                "[StampTransition] " +
                "Not Ready Stamp drag origin updated."
            );
        }

        // -----------------------------------------------------
        // ENABLE DRAGGING
        // -----------------------------------------------------

        SetStampDragging(true);

        transitionRunning = false;

        Debug.Log(
            "[StampTransition] Stamp movement COMPLETE. " +
            "Stamps are now draggable."
        );
    }


    // =========================================================
    // RETURN STAMPS TO ORIGINAL DESK POSITION
    // =========================================================

    public void ReturnStampsToDesk()
    {
        Debug.Log(
            "[StampTransition] ReturnStampsToDesk() called."
        );

        if (!originalPositionsSaved)
        {
            Debug.LogWarning(
                "[StampTransition] Original positions were not saved."
            );

            SaveOriginalPositions();
        }

        if (readyStamp == null ||
            notReadyStamp == null)
        {
            Debug.LogError(
                "[StampTransition] Cannot return stamps. " +
                "Stamp references are missing."
            );

            return;
        }

        // -----------------------------------------------------
        // PAPER SFX
        // -----------------------------------------------------

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayPaperSFX();
        }

        StopAllCoroutines();

        StartCoroutine(ReturnStamps());
    }


    // =========================================================
    // RETURN ANIMATION
    // =========================================================

    private IEnumerator ReturnStamps()
    {
        transitionRunning = true;

        // -----------------------------------------------------
        // DISABLE DRAGGING
        // -----------------------------------------------------

        SetStampDragging(false);

        // -----------------------------------------------------
        // CURRENT POSITIONS
        // -----------------------------------------------------

        Vector3 readyStartPosition =
            readyStamp.position;

        Quaternion readyStartRotation =
            readyStamp.rotation;

        Vector3 notReadyStartPosition =
            notReadyStamp.position;

        Quaternion notReadyStartRotation =
            notReadyStamp.rotation;

        // -----------------------------------------------------
        // ORIGINAL DESK POSITIONS
        // -----------------------------------------------------

        Vector3 readyEndPosition =
            readyOriginalPosition;

        Quaternion readyEndRotation =
            readyOriginalRotation;

        Vector3 notReadyEndPosition =
            notReadyOriginalPosition;

        Quaternion notReadyEndRotation =
            notReadyOriginalRotation;

        Debug.Log(
            $"[StampTransition] Returning Ready Stamp: " +
            $"{readyStartPosition} -> {readyEndPosition}"
        );

        Debug.Log(
            $"[StampTransition] Returning Not Ready Stamp: " +
            $"{notReadyStartPosition} -> {notReadyEndPosition}"
        );

        // -----------------------------------------------------
        // ANIMATION
        // -----------------------------------------------------

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            float t =
                Mathf.Clamp01(
                    elapsed / duration
                );

            t = movementCurve.Evaluate(t);

            // Ready Stamp
            readyStamp.position =
                Vector3.Lerp(
                    readyStartPosition,
                    readyEndPosition,
                    t
                );

            readyStamp.rotation =
                Quaternion.Slerp(
                    readyStartRotation,
                    readyEndRotation,
                    t
                );

            // Not Ready Stamp
            notReadyStamp.position =
                Vector3.Lerp(
                    notReadyStartPosition,
                    notReadyEndPosition,
                    t
                );

            notReadyStamp.rotation =
                Quaternion.Slerp(
                    notReadyStartRotation,
                    notReadyEndRotation,
                    t
                );

            yield return null;
        }

        // -----------------------------------------------------
        // FORCE ORIGINAL POSITION
        // -----------------------------------------------------

        readyStamp.position =
            readyOriginalPosition;

        readyStamp.rotation =
            readyOriginalRotation;

        notReadyStamp.position =
            notReadyOriginalPosition;

        notReadyStamp.rotation =
            notReadyOriginalRotation;

        // -----------------------------------------------------
        // RESET DRAG ORIGINS
        // -----------------------------------------------------

        Stamp3DDrag readyDrag =
            readyStamp.GetComponent<Stamp3DDrag>();

        Stamp3DDrag notReadyDrag =
            notReadyStamp.GetComponent<Stamp3DDrag>();

        if (readyDrag != null)
        {
            readyDrag.SetCurrentPositionAsOrigin();

            Debug.Log(
                "[StampTransition] " +
                "Ready Stamp origin reset to desk."
            );
        }

        if (notReadyDrag != null)
        {
            notReadyDrag.SetCurrentPositionAsOrigin();

            Debug.Log(
                "[StampTransition] " +
                "Not Ready Stamp origin reset to desk."
            );
        }

        transitionRunning = false;

        Debug.Log(
            "[StampTransition] " +
            "Stamps returned to original desk positions."
        );
    }


    // =========================================================
    // ENABLE / DISABLE STAMP DRAGGING
    // =========================================================

    private void SetStampDragging(bool enabled)
    {
        if (readyStamp != null)
        {
            Stamp3DDrag readyDrag =
                readyStamp.GetComponent<Stamp3DDrag>();

            if (readyDrag != null)
                readyDrag.SetDraggingEnabled(enabled);
        }

        if (notReadyStamp != null)
        {
            Stamp3DDrag notReadyDrag =
                notReadyStamp.GetComponent<Stamp3DDrag>();

            if (notReadyDrag != null)
                notReadyDrag.SetDraggingEnabled(enabled);
        }

        Debug.Log(
            $"[StampTransition] Stamp dragging: {enabled}"
        );
    }


    // =========================================================
    // TEMPORARY MOBILE TEST
    // =========================================================

    public void TestMoveStamps()
    {
        Debug.Log(
            "========== STAMP TEST BUTTON PRESSED =========="
        );

        MoveStampsToWorkspace();

        Debug.Log(
            "========== STAMP TEST COMMAND SENT =========="
        );
    }


    public void TestReturnStamps()
    {
        Debug.Log(
            "========== STAMP RETURN TEST BUTTON PRESSED =========="
        );

        ReturnStampsToDesk();

        Debug.Log(
            "========== STAMP RETURN TEST COMMAND SENT =========="
        );
    }
}