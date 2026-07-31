using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// PURPOSE:
/// Spawns one PinnedDocument GameObject per entry in
/// CaseManager.Instance.CurrentCase.supportingDocuments, into a FIXED
/// 2-row x 5-column grid (10 slots total), replacing the earlier random
/// scatter layout. Generation happens ONCE per case — re-opening the
/// Corkboard does NOT regenerate or reposition anything, it just leaves
/// existing papers as they are (preserving reviewed-state visuals).
///
/// RESPONSIBILITIES:
/// - Define 10 fixed grid slot positions (2 rows x 5 columns)
/// - On first open only, spawn one paper per document into slots in order
/// - Leave slots empty if fewer than 10 documents exist
/// - Never regenerate on subsequent opens
///
/// CONNECTS WITH:
/// - CorkboardInteractable: calls SpawnDocumentsIfNeeded() on first open
/// - PinnedDocumentInteractable: same as before, unchanged
/// </summary>
public class CorkboardDocumentSpawner : MonoBehaviour
{
    [Header("Prefab")]
    [SerializeField] private GameObject pinnedDocumentPrefab;

    [Header("Grid Layout")]
    [Tooltip("Horizontal spacing between columns.")]
    [SerializeField] private float columnSpacing = 0.35f;
    [Tooltip("Vertical spacing between rows.")]
    [SerializeField] private float rowSpacing = 0.45f;
    [Tooltip("Number of columns (fixed at 5 per design spec).")]

    [SerializeField] private DocumentViewerUI documentViewerUI;
    private const int Columns = 5;
    [Tooltip("Number of rows (fixed at 2 per design spec).")]
    private const int Rows = 2;

    [Header("Slight Randomization (kept minimal — grid stays readable)")]
    [SerializeField] private float maxRotationJitter = 4f;
    [SerializeField] private float maxPositionJitter = 0.02f;

    private readonly List<GameObject> spawnedPapers = new List<GameObject>();
    private bool hasGeneratedThisSession = false;

    /// <summary>
    /// Generates documents into the fixed grid ONLY the first time this is
    /// called for the current case. Subsequent calls do nothing, per spec
    /// ("When the corkboard is opened for the first time, generate every
    /// supporting document").
    /// </summary>
    public void SpawnDocumentsIfNeeded()
    {
        if (hasGeneratedThisSession) return;
        hasGeneratedThisSession = true;

        var documents = CaseManager.Instance.CurrentCase.supportingDocuments;
        List<CorkboardGridSlot> slots = BuildGridSlots();

        int slotCount = Mathf.Min(documents.Count, slots.Count);
        if (documents.Count > slots.Count)
        {
            Debug.LogWarning($"[CorkboardDocumentSpawner] {documents.Count} documents but only {slots.Count} grid slots exist — extra documents will not be shown.");
        }

        for (int i = 0; i < slotCount; i++)
        {
            var doc = documents[i];
            var slot = slots[i];

            Vector3 jitteredPos = slot.localPosition + new Vector3(
                Random.Range(-maxPositionJitter, maxPositionJitter),
                Random.Range(-maxPositionJitter, maxPositionJitter),
                0f);

            Quaternion jitteredRot = slot.localRotation * Quaternion.Euler(0f, 0f, Random.Range(-maxRotationJitter, maxRotationJitter));

            GameObject paperObj = Instantiate(pinnedDocumentPrefab, transform);
            paperObj.transform.localPosition = jitteredPos;
            paperObj.transform.localRotation = jitteredRot;

            PinnedDocumentInteractable interactable = paperObj.GetComponent<PinnedDocumentInteractable>();
            interactable.SetDocumentViewer(documentViewerUI);
            interactable.Initialize(doc.documentName);

            spawnedPapers.Add(paperObj);
        }

        // Remaining slots (if fewer than 10 documents) are simply left empty —
        // no placeholder object spawned, matching "leave remaining slots empty."
    }

    /// <summary>
    /// Builds the 10 fixed slot positions: 2 rows, 5 columns, centered
    /// around this spawner's local origin.
    /// </summary>
    private List<CorkboardGridSlot> BuildGridSlots()
    {
        var slots = new List<CorkboardGridSlot>();

        float totalWidth = (Columns - 1) * columnSpacing;
        float totalHeight = (Rows - 1) * rowSpacing;
        float startX = -totalWidth / 2f;
        float startY = totalHeight / 2f; // top row first, per "[1][2][3][4][5] / [6][7][8][9][10]"

        for (int row = 0; row < Rows; row++)
        {
            for (int col = 0; col < Columns; col++)
            {
                Vector3 pos = new Vector3(
                    startX + col * columnSpacing,
                    startY - row * rowSpacing,
                    0f);

                slots.Add(new CorkboardGridSlot { localPosition = pos });
            }
        }

        return slots;
    }

    public void RefreshAllDocumentVisuals()
    {
        foreach (var paper in spawnedPapers)
        {
            if (paper == null) continue;
            paper.GetComponent<PinnedDocumentInteractable>()?.RefreshVisual();
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.matrix = transform.localToWorldMatrix;
        foreach (var slot in BuildGridSlots())
        {
            Gizmos.DrawWireCube(slot.localPosition, new Vector3(columnSpacing * 0.8f, rowSpacing * 0.8f, 0.01f));
        }
    }
}