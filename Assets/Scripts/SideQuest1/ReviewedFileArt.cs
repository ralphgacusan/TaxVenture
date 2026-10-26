using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// PURPOSE:
/// Arthur's Reviewed File's one look (Campaign Foundation T30, revised
/// 2026-10-26): it IS the in-game Case Folder, copied, with a muted teal cover.
/// Only the cover changes: the copies keep the desk folder's models, sizes,
/// paper, red tab and print. The open model uses its recoloured baked texture;
/// the closed Synty mesh uses a solid cover colour sampled from that texture
/// (its palette UVs cannot use the open model's atlas). Every form uses this:
///   - the floating pickup: a copy of the desk's closed folder model;
///   - reading: a runtime copy of the desk's open folder (ReviewedFileViewer);
///   - the bar / Bag icon (HudIconArt "reviewed") and the drag ghost
///     (HudDragItem.BuildClosedFolder): the Case Folder's own drawings with
///     Teal() applied to their tan colours from the same CoverColor.
/// Marie's folder is never touched (the copies get their own material instances).
/// </summary>
public static class ReviewedFileLook
{
    public const string Icon = "reviewed";
    public const string Label = "REVIEWED FILE";
    public const string OpenTexture = "Campaign/ReviewedFile/ReviewedFileOpen_BaseColor";
    public const string ClosedTexture = "Campaign/ReviewedFile/ReviewedFileClosed_BaseColor";
    public const string ClosedModel = "Campaign/ReviewedFile/ReviewedFileClosedModel";
    // Median cover colour of the open model's teal atlas; the Synty closed
    // model has different UVs, so it must not receive that unrelated atlas.
    public static readonly Color CoverColor = new Color32(85, 131, 132, 255);
    public const string DeskOpenFolderPath = "Environment/OfficeBuilding/Rooms/ConsultantOffice/InteractiveFolderObjects/OpenCaseFolder";
    public const string DeskClosedFolderPath = "Environment/OfficeBuilding/Rooms/ConsultantOffice/Desk/StackedCaseFolder/CaseFolder";
    public const string DeskStoredFolderPath = "Environment/OfficeBuilding/Rooms/ConsultantOffice/Desk/StackedCaseFolder/DeskFolder";

    /// <summary>The common cover colour with the icon's existing light/dark shading preserved.</summary>
    public static Color Teal(Color tan)
    {
        Color.RGBToHSV(tan, out _, out _, out float v);
        Color teal = CoverColor * Mathf.Clamp(v / 0.85f, 0.35f, 1.15f);
        teal.a = tan.a;
        return teal;
    }

    /// <summary>Gives every mesh under root its own material with the teal base texture (Resources path).</summary>
    public static bool Paint(Transform root, string texturePath)
    {
        bool closed = texturePath == ClosedTexture;
        var teal = closed ? null : Resources.Load<Texture2D>(texturePath);
        if (root == null || (!closed && teal == null))
        {
            Debug.LogWarning($"[ReviewedFile] Teal texture '{texturePath}' not found; the copy keeps the manila cover.");
            return false;
        }
        foreach (MeshRenderer renderer in root.GetComponentsInChildren<MeshRenderer>(true))
        {
            if (renderer.GetComponentInParent<Canvas>(true) != null) continue;
            // Closed copies replace the material without allocating disposable
            // clones of the palette shader. Open copies need texture instances.
            Material[] materials = closed ? renderer.sharedMaterials : renderer.materials;
            if (closed)
            {
                // PolygonOffice uses _Albedo_Map, not the usual URP texture
                // slots. Give this independent copy a known lit cover material.
                for (int i = 0; i < materials.Length; i++)
                    materials[i] = SupportingDocumentMesh.Matte(CoverColor);
                renderer.sharedMaterials = materials;
                continue;
            }
            foreach (Material material in materials)
            {
                if (material == null) continue;
                if (material.HasProperty("_BaseMap")) material.SetTexture("_BaseMap", teal);
                if (material.HasProperty("_MainTex")) material.SetTexture("_MainTex", teal);
                if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", Color.white);
                if (material.HasProperty("_Color")) material.SetColor("_Color", Color.white);
            }
            renderer.materials = materials;
        }
        return true;
    }

    /// <summary>
    /// A clean copy of the existing closed folder mesh: teal, real size, cover up (+Y), its
    /// middle at parent's origin. Returns the copy (null without the model asset) and its size.
    /// </summary>
    public static Transform BuildClosed(Transform parent, Scene scene, out Vector3 size)
    {
        size = new Vector3(0.24f, 0.02f, 0.32f);
        // Use the same mesh as the Auditor, from a clean prefab. A live desk
        // hierarchy can be hidden, animated, scaled or batched at collection time.
        GameObject model = Resources.Load<GameObject>(ClosedModel);
        Transform desk = model != null ? model.transform : null;
        if (desk == null || parent == null)
        {
            Debug.LogWarning("[ReviewedFile] The closed Case Folder model asset was not found; the pickup has no model.");
            return null;
        }
        // The desk hides its Visual when its folder is open/stored. Build asleep,
        // as ClientCopyPickup does, then reveal only the independent model.
        var holder = new GameObject("Reviewed File (building)");
        holder.SetActive(false);
        SceneManager.MoveGameObjectToScene(holder, scene);
        GameObject copy = Object.Instantiate(desk.gameObject, holder.transform);
        copy.name = "Reviewed File (closed)";
        Strip<MonoBehaviour>(copy);
        Strip<Collider>(copy);
        Strip<Rigidbody>(copy);
        foreach (Renderer renderer in copy.GetComponentsInChildren<Renderer>(true))
        {
            renderer.enabled = true;
            // Include hidden mesh ancestors, without waking any copied scripts.
            for (Transform t = renderer.transform; t != holder.transform; t = t.parent)
                t.gameObject.SetActive(true);
            renderer.gameObject.SetActive(true);
        }
        copy.SetActive(true);
        copy.transform.SetParent(parent, false);
        Object.Destroy(holder);
        // As it lies on the desk, without the desk's turn: the cover faces up.
        copy.transform.localRotation = Quaternion.Inverse(Quaternion.Euler(0f, desk.eulerAngles.y, 0f)) * desk.rotation;
        copy.transform.localScale = desk.lossyScale;
        copy.transform.localPosition = Vector3.zero;
        int layer = parent.gameObject.layer;
        foreach (Transform t in copy.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = layer;
        Paint(copy.transform, ClosedTexture);

        bool any = false;
        var bounds = new Bounds();
        // Local mesh corners work even when the source renderer was hidden;
        // world renderer bounds can be stale until Unity updates rendering.
        foreach (MeshFilter filter in copy.GetComponentsInChildren<MeshFilter>(true))
        {
            if (filter.sharedMesh == null) continue;
            Bounds mesh = filter.sharedMesh.bounds;
            Matrix4x4 toRoot = parent.worldToLocalMatrix * filter.transform.localToWorldMatrix;
            for (int i = 0; i < 8; i++)
            {
                Vector3 corner = mesh.center + Vector3.Scale(mesh.extents,
                    new Vector3((i & 1) == 0 ? -1f : 1f, (i & 2) == 0 ? -1f : 1f, (i & 4) == 0 ? -1f : 1f));
                Vector3 point = toRoot.MultiplyPoint3x4(corner);
                if (!any) { bounds = new Bounds(point, Vector3.zero); any = true; }
                else bounds.Encapsulate(point);
            }
        }
        if (!any)
        {
            Debug.LogWarning("[ReviewedFile] The closed Case Folder has no mesh; pickup was not created.");
            Object.Destroy(copy);
            return null;
        }
        copy.transform.localPosition -= bounds.center;
        size = bounds.size;
        return copy.transform;
    }

    /// <summary>
    /// A hidden copy of the desk's open Case Folder (scene root, at the desk folder's pose): model, canvas,
    /// pages and page controls, without the parts that belong to Marie's folder (desk tap, desk stack,
    /// HUD store, desk dragging and outline). Built inside an inactive holder so none of those ever wake.
    /// </summary>
    public static Transform BuildOpenCopy(Scene scene, int layer)
    {
        Transform desk = Find(scene, DeskOpenFolderPath);
        if (desk == null)
        {
            Debug.LogWarning("[ReviewedFile] The desk's open Case Folder was not found; Arthur's file cannot be read.");
            return null;
        }
        var holder = new GameObject("Reviewed File (build)");
        holder.SetActive(false);
        SceneManager.MoveGameObjectToScene(holder, scene);
        GameObject copy = Object.Instantiate(desk.gameObject, desk.position, desk.rotation, holder.transform);
        copy.name = "Arthur's Reviewed File (open)";
        copy.transform.localScale = desk.lossyScale;
        Strip<FolderToHUDIcon>(copy);
        Strip<OpenFolderReturnToDesk>(copy);
        Strip<CaseFolderInteractable>(copy);
        Strip<DeskItemHighlight>(copy);
        Strip<FloatingModel3D>(copy);
        Strip<HighlightEffect>(copy);
        Strip<CaseFolder3DDrag>(copy);
        // Marie's attached records (T25) belong to her folder only: none come along into the copy.
        foreach (AttachedRecordsRoot records in copy.GetComponentsInChildren<AttachedRecordsRoot>(true))
            if (records != null) Object.DestroyImmediate(records.gameObject);
        copy.layer = layer;
        Paint(copy.transform, OpenTexture);
        copy.SetActive(false);
        copy.transform.SetParent(null, true);
        SceneManager.MoveGameObjectToScene(copy, scene);
        Object.Destroy(holder);
        return copy.transform;
    }

    private static void Strip<T>(GameObject root) where T : Component
    {
        foreach (T component in root.GetComponentsInChildren<T>(true)) Object.DestroyImmediate(component);
    }

    public static Transform Find(Scene scene, string path)
    {
        int slash = path.IndexOf('/');
        string rootName = path.Substring(0, slash);
        string rest = path.Substring(slash + 1);
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root.name != rootName) continue;
            Transform t = root.transform.Find(rest);
            if (t != null) return t;
        }
        return null;
    }
}
