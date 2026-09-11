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

    public void MoveStampsToWorkspace()
    {
        Debug.Log("[StampTransition] MoveStampsToWorkspace() called.");

        // ---------------------------------------------------------
        // VALIDATE STAMP REFERENCES
        // ---------------------------------------------------------

        if (readyStamp == null)
        {
            Debug.LogError("[StampTransition] Ready Stamp is NOT assigned!");
            return;
        }

        if (notReadyStamp == null)
        {
            Debug.LogError(
                "[StampTransition] Not Ready Stamp is NOT assigned!"
            );
            return;
        }

        // ---------------------------------------------------------
        // VALIDATE HIGHLIGHT REFERENCES
        // ---------------------------------------------------------

        if (readyHighlight == null)
        {
            Debug.LogWarning(
                "[StampTransition] Ready Highlight is NOT assigned."
            );
        }

        if (notReadyHighlight == null)
        {
            Debug.LogWarning(
                "[StampTransition] Not Ready Highlight is NOT assigned."
            );
        }

        // ---------------------------------------------------------
        // VALIDATE TARGET REFERENCES
        // ---------------------------------------------------------

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

        Debug.Log(
            "[StampTransition] All required references are assigned."
        );

        Debug.Log(
            $"[StampTransition] Ready Stamp: {readyStamp.name}"
        );

        Debug.Log(
            $"[StampTransition] Not Ready Stamp: {notReadyStamp.name}"
        );

        Debug.Log(
            $"[StampTransition] Ready Target: {readyTarget.name}"
        );

        Debug.Log(
            $"[StampTransition] Not Ready Target: {notReadyTarget.name}"
        );

        // ---------------------------------------------------------
        // RESTORE ORIGINAL MATERIALS
        // ---------------------------------------------------------

        if (readyHighlight != null)
        {
            readyHighlight.Unhighlight();

            Debug.Log(
                "[StampTransition] Ready Stamp original material restored."
            );
        }

        if (notReadyHighlight != null)
        {
            notReadyHighlight.Unhighlight();

            Debug.Log(
                "[StampTransition] Not Ready Stamp original material restored."
            );
        }

        Debug.Log(
            "[StampTransition] Starting stamp movement..."
        );

        StopAllCoroutines();

        StartCoroutine(MoveStamps());
    }

    private IEnumerator MoveStamps()
    {
        // ---------------------------------------------------------
        // SAVE START POSITIONS / ROTATIONS
        // ---------------------------------------------------------

        Vector3 readyStartPosition = readyStamp.position;
        Quaternion readyStartRotation = readyStamp.rotation;

        Vector3 notReadyStartPosition = notReadyStamp.position;
        Quaternion notReadyStartRotation = notReadyStamp.rotation;

        // ---------------------------------------------------------
        // SAVE TARGET POSITIONS / ROTATIONS
        // ---------------------------------------------------------

        Vector3 readyEndPosition = readyTarget.position;
        Quaternion readyEndRotation = readyTarget.rotation;

        Vector3 notReadyEndPosition = notReadyTarget.position;
        Quaternion notReadyEndRotation = notReadyTarget.rotation;

        Debug.Log(
            $"[StampTransition] Ready movement: " +
            $"{readyStartPosition} -> {readyEndPosition}"
        );

        Debug.Log(
            $"[StampTransition] Not Ready movement: " +
            $"{notReadyStartPosition} -> {notReadyEndPosition}"
        );

        // ---------------------------------------------------------
        // ANIMATION
        // ---------------------------------------------------------

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            float t = Mathf.Clamp01(elapsed / duration);

            // Apply animation curve
            t = movementCurve.Evaluate(t);

            // -----------------------------------------------------
            // READY STAMP
            // -----------------------------------------------------

            readyStamp.position = Vector3.Lerp(
                readyStartPosition,
                readyEndPosition,
                t
            );

            readyStamp.rotation = Quaternion.Slerp(
                readyStartRotation,
                readyEndRotation,
                t
            );

            // -----------------------------------------------------
            // NOT READY STAMP
            // -----------------------------------------------------

            notReadyStamp.position = Vector3.Lerp(
                notReadyStartPosition,
                notReadyEndPosition,
                t
            );

            notReadyStamp.rotation = Quaternion.Slerp(
                notReadyStartRotation,
                notReadyEndRotation,
                t
            );

            yield return null;
        }

        // ---------------------------------------------------------
        // FORCE FINAL POSITION / ROTATION
        // ---------------------------------------------------------

        readyStamp.position = readyEndPosition;
        readyStamp.rotation = readyEndRotation;

        notReadyStamp.position = notReadyEndPosition;
        notReadyStamp.rotation = notReadyEndRotation;

        // ---------------------------------------------------------
        // UPDATE DRAG ORIGINS
        // ---------------------------------------------------------
        // The workspace positions are now the new "home" positions.
        // If the player drags a stamp somewhere invalid, it will
        // snap back here instead of returning to the original desk
        // position.

        Stamp3DDrag readyDrag =
            readyStamp.GetComponent<Stamp3DDrag>();

        Stamp3DDrag notReadyDrag =
            notReadyStamp.GetComponent<Stamp3DDrag>();

        if (readyDrag != null)
        {
            readyDrag.SetCurrentPositionAsOrigin();

            Debug.Log(
                "[StampTransition] Ready Stamp drag origin updated."
            );
        }
        else
        {
            Debug.LogWarning(
                "[StampTransition] Stamp3DDrag not found on Ready Stamp."
            );
        }

        if (notReadyDrag != null)
        {
            notReadyDrag.SetCurrentPositionAsOrigin();

            Debug.Log(
                "[StampTransition] Not Ready Stamp drag origin updated."
            );
        }
        else
        {
            Debug.LogWarning(
                "[StampTransition] Stamp3DDrag not found on Not Ready Stamp."
            );
        }

        Debug.Log(
            "[StampTransition] Stamp movement COMPLETE."
        );
    }

    // -------------------------------------------------------------
    // TEMPORARY MOBILE TEST
    // -------------------------------------------------------------

    public void TestMoveStamps()
    {
        Debug.Log(
            "========== STAMP TEST BUTTON PRESSED =========="
        );

        Debug.Log(
            "[StampTransition] TestMoveStamps() called."
        );

        MoveStampsToWorkspace();

        Debug.Log(
            "========== STAMP TEST COMMAND SENT =========="
        );
    }
}