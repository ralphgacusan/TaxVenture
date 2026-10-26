using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Scene-authored position, facing and size for the Level 1 Notes chest.</summary>
[DisallowMultipleComponent]
public sealed class NotesChestPlacement : MonoBehaviour
{
    [Tooltip("The chest's size in the world (1 = the Synty prop's own size). Rotation uses only the yaw.")]
    [SerializeField, Min(0.1f)] private float scale = 1f;

    public float Scale => scale;

    public static NotesChestPlacement Find(Scene scene)
    {
        // Called from the chest's Awake, before this marker's OnEnable may have run.
        foreach (GameObject root in scene.GetRootGameObjects())
            foreach (NotesChestPlacement placement in root.GetComponentsInChildren<NotesChestPlacement>())
                if (placement.enabled && placement.gameObject.activeInHierarchy) return placement;
        return null;
    }

    /// <summary>Moves the chest here, at the scene root, upright and facing this marker's yaw.</summary>
    public void Apply(Transform chest)
    {
        chest.SetParent(null, false);
        chest.SetPositionAndRotation(transform.position, Quaternion.Euler(0f, transform.eulerAngles.y, 0f));
        chest.localScale = Vector3.one * scale;
    }

    private void OnDrawGizmos()
    {
        if (Application.isPlaying) return;
        NotesOnboardingChest chest = FindAnyObjectByType<NotesOnboardingChest>(FindObjectsInactive.Include);
        if (chest == null || chest.gameObject.scene != gameObject.scene) return;
        Transform source = chest.transform;
        Matrix4x4 previous = Gizmos.matrix;
        Color previousColor = Gizmos.color;
        Matrix4x4 pose = Matrix4x4.TRS(transform.position, Quaternion.Euler(0f, transform.eulerAngles.y, 0f), Vector3.one * scale);
        Gizmos.color = new Color(0.75f, 0.55f, 0.3f, 1f);
        foreach (MeshFilter mesh in source.GetComponentsInChildren<MeshFilter>(true))
        {
            if (mesh.sharedMesh == null || mesh.GetComponentInParent<NotesChestPaperPickup>(true) != null) continue;
            Gizmos.matrix = pose * source.worldToLocalMatrix * mesh.transform.localToWorldMatrix;
            Gizmos.DrawMesh(mesh.sharedMesh);
        }
        // The paper's spot when open, and the side the player opens it from (+Z).
        Gizmos.matrix = pose;
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(chest.PaperLocalPosition, new Vector3(chest.PaperLocalScale.x, chest.PaperLocalScale.y, 0.01f));
        Gizmos.DrawLine(new Vector3(0f, 0.05f, 0.6f), new Vector3(0f, 0.05f, 1.6f));
        Gizmos.matrix = previous;
        Gizmos.color = previousColor;
    }
}
