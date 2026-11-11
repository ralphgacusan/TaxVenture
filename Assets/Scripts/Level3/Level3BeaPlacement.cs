using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Scene-authored group position/facing for Bea's outdoor table and starting place.</summary>
[DisallowMultipleComponent]
public sealed class Level3BeaPlacement : MonoBehaviour
{
    public static Level3BeaPlacement Find(Scene scene)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
            foreach (Level3BeaPlacement placement in root.GetComponentsInChildren<Level3BeaPlacement>())
                if (placement.isActiveAndEnabled) return placement;
        return null;
    }

    private void OnDrawGizmos()
    {
        if (Application.isPlaying) return;
        Level3SceneAssets assets = Level3SceneAssets.Load();
        if (assets == null || assets.worktablePrefab == null) return;
        Transform table = assets.worktablePrefab.transform;
        Bounds body = SupplyCounter.LocalBounds(table, table);
        if (body.size.y <= 0f) return;
        float scale = Level3BeaTable.TableTop / body.size.y;
        Matrix4x4 pose = Matrix4x4.TRS(transform.position, Quaternion.Euler(0f, transform.eulerAngles.y, 0f), Vector3.one);
        Matrix4x4 previous = Gizmos.matrix;
        Color color = Gizmos.color;
        Gizmos.color = new Color(.75f, .65f, .4f, 1f);
        Draw(table, pose * Matrix4x4.TRS(new Vector3(-body.center.x, -body.min.y, -body.center.z) * scale,
            Quaternion.identity, Vector3.one * scale));
        if (assets.beaPrefab != null)
        {
            Transform bea = assets.beaPrefab.transform;
            Bounds person = SupplyCounter.LocalBounds(bea, bea);
            float height = person.size.y > 0f ? 1.6f / person.size.y : 1f;
            Matrix4x4 stand = pose * Matrix4x4.TRS(new Vector3(0f, 0f, body.extents.z * scale + .38f),
                Quaternion.Euler(0f, 180f, 0f), Vector3.one);
            Draw(bea, stand * Matrix4x4.TRS(new Vector3(0f, -person.min.y * height, 0f), Quaternion.identity, Vector3.one * height));
        }
        Gizmos.matrix = pose;
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(new Vector3(0f, Level3BeaTable.TableTop + .01f, 0f), new Vector3(body.size.x * scale, .02f, body.size.z * scale));
        Gizmos.DrawLine(Vector3.up * .1f, new Vector3(0f, .1f, -1.2f));
        Gizmos.matrix = previous;
        Gizmos.color = color;
    }

    private static void Draw(Transform prefab, Matrix4x4 pose)
    {
        foreach (MeshFilter mesh in prefab.GetComponentsInChildren<MeshFilter>(true))
        {
            if (mesh.sharedMesh == null) continue;
            Gizmos.matrix = pose * prefab.worldToLocalMatrix * mesh.transform.localToWorldMatrix;
            Gizmos.DrawMesh(mesh.sharedMesh);
        }
        foreach (SkinnedMeshRenderer skin in prefab.GetComponentsInChildren<SkinnedMeshRenderer>(true))
        {
            if (skin.sharedMesh == null) continue;
            Gizmos.matrix = pose * prefab.worldToLocalMatrix * skin.transform.localToWorldMatrix;
            Gizmos.DrawMesh(skin.sharedMesh);
        }
    }
}
