using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// PURPOSE:
/// Spawns one PinnedDocument GameObject per entry in
/// CaseManager.Instance.CurrentCase.supportingDocuments, scattered randomly
/// (but non-overlapping) within a configurable board area. Does NOT know
/// anything about document field data or review logic — it only places
/// objects and hands each one its document name via
/// PinnedDocumentInteractable.Initialize().
///
/// RESPONSIBILITIES:
/// - Clear previously spawned papers (needed when a new case loads)
/// - Spawn exactly Count papers, never hardcoded
/// - Scatter position/rotation within Inspector-configurable bounds
/// - Keep a list of spawned instances for cleanup/reference
///
/// DOES NOT:
/// - Know document field content (DocumentDataProvider's job)
/// - Know reviewed status logic (CaseData's job, read by PinnedDocumentInteractable)
/// - Handle clicking (PinnedDocumentInteractable's job)
///
/// CONNECTS WITH:
/// - CorkboardInteractable: calls SpawnDocuments() once first-person entry completes
/// - CaseManager.Instance.CurrentCase.supportingDocuments: data source
/// - PinnedDocument prefab: must have PinnedDocumentInteractable on its root
/// </summary>
public class CorkboardDocumentSpawner : MonoBehaviour
{
    [Header("Prefab")]
    [SerializeField] private GameObject pinnedDocumentPrefab;

    [Header("Board Bounds (local space, relative to this spawner)")]
    [Tooltip("Width (local X) and height (local Y) of the pinnable board area.")]
    [SerializeField] private Vector2 boardSize = new Vector2(1f, 0.55f);
    [Tooltip("Depth (local Z) offset range so papers don't perfectly z-fight.")]
    [SerializeField] private float depthJitter = 0.02f;

    [Header("Randomization")]
    [SerializeField] private float maxRotationAngle = 12f;

    [Tooltip("Minimum distance between paper centers, used to avoid heavy overlap.")]
    [SerializeField] private float minSpacing = 0.01f;

    [Tooltip("How many times to retry finding a non-overlapping spot before giving up and placing anyway.")]
    [SerializeField] private int maxPlacementAttempts = 20;

    [SerializeField] private DocumentViewerUI documentViewerUI;

    [SerializeField] float paperWidth = 0.46f;
    [SerializeField] float paperHeight = 0.70f;
    [SerializeField] float gap = 0.02f;

    [SerializeField] private Vector2 clusterCenter = Vector2.zero;

    [SerializeField] private float clusterWidth = 1.2f;
    [SerializeField] private float clusterHeight = 1f;

    private readonly List<GameObject> spawnedPapers = new List<GameObject>();
    private readonly List<Vector2> usedPositions = new List<Vector2>();

    /// <summary>
    /// Clears any existing papers and spawns one PinnedDocument per current
    /// supporting document. Safe to call every time the corkboard is opened —
    /// ensures the board always reflects the current CaseData exactly.
    /// </summary>
    public void SpawnDocuments()
    {
        ClearDocuments();

        var documents = CaseManager.Instance.CurrentCase.supportingDocuments;

        foreach (var doc in documents)
        {
            Vector2 localPos = FindNonOverlappingPosition();
            usedPositions.Add(localPos);

            float depth = Random.Range(-depthJitter, depthJitter);
            Vector3 spawnLocalPos = new Vector3(localPos.x, localPos.y, depth);

            GameObject paperObj = Instantiate(pinnedDocumentPrefab, transform);
            paperObj.transform.localPosition =
                new Vector3(localPos.x, localPos.y, -3f + depth);
            paperObj.transform.localRotation = Quaternion.Euler(
                                  0f, 0f, Random.Range(-maxRotationAngle, maxRotationAngle));

            PinnedDocumentInteractable interactable =
                paperObj.GetComponent<PinnedDocumentInteractable>();

            interactable.SetDocumentViewer(documentViewerUI);
            interactable.Initialize(doc.documentName);

            spawnedPapers.Add(paperObj);
        }
    }

    /// <summary>
    /// Destroys all currently spawned paper GameObjects. Called at the start
    /// of SpawnDocuments() and can also be called externally when a new case
    /// loads (future multi-case support).
    /// </summary>
    public void ClearDocuments()
    {
        foreach (var paper in spawnedPapers)
        {
            if (paper != null) Destroy(paper);
        }
        spawnedPapers.Clear();
        usedPositions.Clear();
    }

    /// <summary>
    /// Picks a random local-space position within board bounds, retrying up
    /// to maxPlacementAttempts times to find one that isn't too close to an
    /// already-used position. Falls back to the last attempt if it never
    /// finds a clean spot (acceptable for greybox — avoids infinite loops).
    /// </summary>
    private Vector2 FindNonOverlappingPosition()
    {
        Vector2 candidate = RandomPointInBoard();

        for (int attempt = 0; attempt < maxPlacementAttempts; attempt++)
        {
            if (IsFarEnoughFromExisting(candidate)) return candidate;
            candidate = RandomPointInBoard();
        }

        return candidate; // best-effort after max attempts
    }

    private Vector2 RandomPointInBoard()
    {
        return new Vector2(
            clusterCenter.x + Random.Range(-clusterWidth * 0.5f, clusterWidth * 0.5f),
            clusterCenter.y + Random.Range(-clusterHeight * 0.5f, clusterHeight * 0.5f)
        );
    }
    private bool IsFarEnoughFromExisting(Vector2 candidate)
    {
        foreach (var used in usedPositions)
        {
            bool overlapX = Mathf.Abs(candidate.x - used.x) < paperWidth + gap;
            bool overlapY = Mathf.Abs(candidate.y - used.y) < paperHeight + gap;

            if (overlapX && overlapY)
                return false;
        }

        return true;
    }

    // Visualize the board bounds in the Scene view for easy tuning.
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawWireCube(Vector3.zero, new Vector3(boardSize.x, boardSize.y, 0.01f));
    }

    /// <summary>
    /// Re-reads reviewed status on every currently spawned paper. Called by
    /// DocumentViewerUI.Hide() so closing the viewer immediately updates
    /// whichever paper was just inspected (and, harmlessly, all others too —
    /// cheap for 7 objects).
    /// </summary>
    public void RefreshAllDocumentVisuals()
    {
        foreach (var paper in spawnedPapers)
        {
            if (paper == null) continue;
            paper.GetComponent<PinnedDocumentInteractable>()?.RefreshVisual();
        }
    }
}