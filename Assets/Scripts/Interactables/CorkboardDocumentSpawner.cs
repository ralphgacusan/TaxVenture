using System.Collections.Generic;
using UnityEngine;

public class CorkboardDocumentSpawner : MonoBehaviour
{
    [Header("Prefab")]
    [SerializeField] private GameObject pinnedDocumentPrefab;

    [SerializeField] private DocumentViewerUI documentViewerUI;

    [Header("Random Look")]
    [SerializeField] private float positionJitter = 0.01f;
    [SerializeField] private float maxRotationAngle = 6f;
    [SerializeField] private float depthJitter = 0.02f;

    private readonly List<GameObject> spawnedPapers = new();

    // Compact layouts for a 5 x 2.3 corkboard
    private static readonly Vector2[][] Layouts =
    {
        // 1
        new[]
        {
            new Vector2(0f, 0f)
        },

        // 2
        new[]
        {
            new Vector2(-0.22f, 0f),
            new Vector2( 0.22f, 0f)
        },

        // 3
        new[]
        {
            new Vector2(-0.24f, 0.14f),
            new Vector2( 0.24f, 0.14f),
            new Vector2( 0f,-0.18f)
        },

        // 4
        new[]
        {
            new Vector2(-0.24f, 0.18f),
            new Vector2( 0.24f, 0.18f),
            new Vector2(-0.24f,-0.18f),
            new Vector2( 0.24f,-0.18f)
        },

        // 5
        new[]
        {
            new Vector2(-0.30f, 0.20f),
            new Vector2( 0.30f, 0.20f),
            new Vector2( 0f,    0f),
            new Vector2(-0.30f,-0.20f),
            new Vector2( 0.30f,-0.20f)
        },

        // 6
        new[]
        {
            new Vector2(-0.34f, 0.20f),
            new Vector2( 0f,    0.20f),
            new Vector2( 0.34f, 0.20f),

            new Vector2(-0.34f,-0.20f),
            new Vector2( 0f,   -0.20f),
            new Vector2( 0.34f,-0.20f)
        },

        // 7
        new[]
        {
            new Vector2(-0.36f, 0.22f),
            new Vector2( 0f,    0.22f),
            new Vector2( 0.36f, 0.22f),

            new Vector2(-0.36f,-0.02f),
            new Vector2( 0.36f,-0.02f),

            new Vector2(-0.18f,-0.26f),
            new Vector2( 0.18f,-0.26f)
        },

        // 8
        new[]
        {
            new Vector2(-0.36f, 0.22f),
            new Vector2(-0.12f, 0.22f),
            new Vector2( 0.12f, 0.22f),
            new Vector2( 0.36f, 0.22f),

            new Vector2(-0.36f,-0.22f),
            new Vector2(-0.12f,-0.22f),
            new Vector2( 0.12f,-0.22f),
            new Vector2( 0.36f,-0.22f)
        }
    };
    public void SpawnDocuments()
    {
        ClearDocuments();

        var documents = CaseManager.Instance.CurrentCase.supportingDocuments;

        int count = Mathf.Clamp(documents.Count, 1, 8);

        Vector2[] layout = Layouts[count - 1];

        for (int i = 0; i < count; i++)
        {
            Vector2 position = layout[i];

            position += new Vector2(
                Random.Range(-positionJitter, positionJitter),
                Random.Range(-positionJitter, positionJitter));

            float depth = Random.Range(-depthJitter, depthJitter);

            GameObject paper = Instantiate(pinnedDocumentPrefab, transform);

            paper.transform.localPosition =
                new Vector3(position.x, position.y, -3f + depth);

            paper.transform.localRotation =
                Quaternion.Euler(
                    0f,
                    0f,
                    Random.Range(-maxRotationAngle, maxRotationAngle));

            var interactable = paper.GetComponent<PinnedDocumentInteractable>();

            interactable.SetDocumentViewer(documentViewerUI);
            interactable.Initialize(documents[i].documentName);

            spawnedPapers.Add(paper);
        }
    }

    public void ClearDocuments()
    {
        foreach (var paper in spawnedPapers)
        {
            if (paper != null)
                Destroy(paper);
        }

        spawnedPapers.Clear();
    }

    public void RefreshAllDocumentVisuals()
    {
        foreach (var paper in spawnedPapers)
        {
            if (paper == null)
                continue;

            paper.GetComponent<PinnedDocumentInteractable>()?.RefreshVisual();
        }
    }
}