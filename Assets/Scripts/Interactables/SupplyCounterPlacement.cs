using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Scene-authored position and facing for the runtime supply stall.</summary>
[DisallowMultipleComponent]
public sealed class SupplyCounterPlacement : MonoBehaviour
{
    public static SupplyCounterPlacement Find(Scene scene)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
            foreach (SupplyCounterPlacement placement in root.GetComponentsInChildren<SupplyCounterPlacement>())
                if (placement.isActiveAndEnabled) return placement;
        return null;
    }

    private void OnDrawGizmos()
    {
        if (Application.isPlaying) return;
        CampaignShopAssets assets = CampaignShopAssets.Load();
        if (assets == null || assets.counterPrefab == null) return;
        Transform prefab = assets.counterPrefab.transform;
        Bounds body = SupplyCounter.LocalBounds(prefab, prefab);
        float yaw = body.size.z > body.size.x ? 90f : 0f;
        if (ShopCatalogueAsset.Current.counter.flip) yaw += 180f;
        Matrix4x4 turn = Matrix4x4.Rotate(Quaternion.Euler(0f, yaw, 0f));
        Vector3 center = turn.MultiplyPoint3x4(body.center);
        Vector3 size = turn.MultiplyVector(body.size);
        size = new Vector3(Mathf.Abs(size.x), Mathf.Abs(size.y), Mathf.Abs(size.z));
        Vector3 offset = new Vector3(-center.x, -body.min.y, -center.z);
        Matrix4x4 previous = Gizmos.matrix;
        Color previousColor = Gizmos.color;
        Matrix4x4 pose = Matrix4x4.TRS(transform.position,
            Quaternion.Euler(0f, transform.eulerAngles.y, 0f), Vector3.one);
        Gizmos.color = new Color(0.75f, 0.65f, 0.4f, 1f);
        foreach (MeshFilter mesh in prefab.GetComponentsInChildren<MeshFilter>(true))
        {
            if (mesh.sharedMesh == null) continue;
            Gizmos.matrix = pose * Matrix4x4.Translate(offset) * turn * prefab.worldToLocalMatrix * mesh.transform.localToWorldMatrix;
            Gizmos.DrawMesh(mesh.sharedMesh);
        }
        Gizmos.matrix = pose;
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(new Vector3(0f, size.y + 1f, -size.z * 0.32f),
            new Vector3(size.x * 0.95f, size.x * 0.95f / 4f, 0.04f));
        Gizmos.DrawLine(new Vector3(0f, 0.1f, size.z * 0.5f), new Vector3(0f, 0.1f, size.z * 0.5f + 1f));
        Gizmos.matrix = previous;
        Gizmos.color = previousColor;
    }
}
