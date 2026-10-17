using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// PURPOSE:
/// Level 2 folder retrieval (ticket T02), under Level2Gameplay/CaseRetrieval.
///
/// - Builds the labeled "market-client drawer" on the file storage cabinet
///   from Unity primitives: drawer front with label and handle, a shallow
///   drawer box and Rosa's manila folder inside. It sits on the cabinet face
///   that has the most open floor in front of it; FilingCabinetInteractable
///   (archiving) is not touched.
/// - Keeps the desk Case Folder hidden until the folder is retrieved. The
///   drawer then flies the folder into the HUD Case Folder icon and stores
///   it there (StoreRetrievedFolder, count 0 -> 1); Level2FolderHandoff lets
///   the player put it on the desk from the icon.
/// - TryRetrieve() is the single domain commit: review phase RetrieveFolder
///   -> PreliminaryReview and the CaseFolderRetrieved notification. It never
///   archives, audits, rewards, changes the game state or the case index.
/// </summary>
public sealed class Level2CaseRetrieval : MonoBehaviour
{
    public const string ChildName = "CaseRetrieval";
    public const string CabinetPath = "Environment/OfficeBuilding/Rooms/FileStorageRoom/Filing_Cabinet_1";
    public const string DrawerName = "MarketClientDrawer";
    private const float WaitForCaseSeconds = 6f;

    // Drawer layout (metres). Tunable; see PROGRESS for the user's visual check.
    private const float FrontHeight = 0.2f;
    private const float FrontDepth = 0.03f;
    private const float BodyDepth = 0.34f;
    private const float Proud = 0.012f;

    private static readonly Color FrontColor = new Color32(84, 101, 118, 255);
    private static readonly Color DarkColor = new Color32(38, 42, 48, 255);
    private static readonly Color LabelColor = new Color32(244, 236, 214, 255);
    private static readonly Color FolderColor = new Color32(222, 186, 120, 255);
    private static readonly Color InkColor = new Color32(30, 36, 48, 255);

    public MarketClientDrawer Drawer { get; private set; }

    /// <summary>True once the desk folder was hidden waiting for retrieval.</summary>
    public bool DeskFolderHidden { get; private set; }

    private readonly List<Material> materials = new List<Material>();
    private CaseFolder3DDrag deskFolder;

    private void Awake()
    {
        Build();
    }

    private IEnumerator Start()
    {
        float until = Time.unscaledTime + WaitForCaseSeconds;
        while (Level2OfficeStart.Review == null && Time.unscaledTime < until) yield return null;

        CaseReviewState review = Level2OfficeStart.Review;
        if (review == null)
        {
            Debug.LogWarning("[Level2] Rosa's review state is missing; the desk folder is left as it is.");
            yield break;
        }

        deskFolder = FindDeskFolder();
        if (review.FolderRetrieved)
        {
            if (Drawer != null) Drawer.ShowEmpty();
            yield break;
        }
        if (deskFolder != null)
        {
            deskFolder.HideAndDisableFolder();
            DeskFolderHidden = true;
        }
        else
        {
            Debug.LogWarning("[Level2] No desk CaseFolder3DDrag found; nothing to hide before retrieval.");
        }
    }

    /// <summary>Takes Rosa's folder out of the drawer, once. False when it is not the moment.</summary>
    public bool TryRetrieve()
    {
        CaseReviewState review = Level2OfficeStart.Review;
        if (review == null || !review.TryRetrieveFolder()) return false;

        Debug.Log("[Level2] Rosa's folder retrieved from the market-client drawer. Review phase: PreliminaryReview.");
        GameplayEvents.RaiseCaseFolderRetrieved();
        return true;
    }

    /// <summary>True once the retrieved folder is in the HUD folder icon.</summary>
    public bool FolderInIcon { get; private set; }

    /// <summary>
    /// Puts the retrieved folder into the HUD Case Folder icon (count 0 -> 1),
    /// once, when the drawer animation reaches the icon (or is cut short).
    /// </summary>
    public void StoreRetrievedFolder()
    {
        if (FolderInIcon) return;
        CaseReviewState review = Level2OfficeStart.Review;
        if (review == null || !review.AwaitingDeskPlacement) return;
        FolderToHUDIcon store = Level2FolderHandoff.FindStore(gameObject.scene);
        if (store == null)
        {
            Debug.LogWarning("[Level2] No HUD folder store found; Rosa's folder stays hidden.");
            return;
        }
        store.StoreFolderFromExternalSource();
        FolderInIcon = FolderToHUDIcon.GetStoredFolderCount() > 0;
        DeskFolderHidden = false;
    }

    /// <summary>Screen point of the HUD Case Folder icon (where the folder flies), if any.</summary>
    public bool TryFolderIconScreenPoint(out Vector2 point)
    {
        point = default;
        FolderToHUDIcon store = Level2FolderHandoff.FindStore(gameObject.scene);
        RectTransform icon = store != null ? store.GetHUDIcon() : null;
        if (icon == null || !icon.gameObject.activeInHierarchy) return false;
        Canvas canvas = icon.GetComponentInParent<Canvas>();
        Camera uiCamera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
        point = RectTransformUtility.WorldToScreenPoint(uiCamera, icon.position);
        return true;
    }

    private CaseFolder3DDrag FindDeskFolder()
    {
        foreach (GameObject root in gameObject.scene.GetRootGameObjects())
        {
            CaseFolder3DDrag found = root.GetComponentInChildren<CaseFolder3DDrag>(true);
            if (found != null) return found;
        }
        return null;
    }

    // =========================================================
    // BUILD
    // =========================================================

    private void Build()
    {
        Transform cabinet = PlayerPlacement.Find(gameObject.scene, CabinetPath);
        if (cabinet == null)
        {
            Debug.LogWarning($"[Level2] Cabinet '{CabinetPath}' not found; the market-client drawer was not built.");
            return;
        }
        if (!TryBounds(cabinet, out Bounds bounds))
        {
            Debug.LogWarning("[Level2] The cabinet has no renderers; the market-client drawer was not built.");
            return;
        }

        Vector3 front = ChooseFront(cabinet, bounds, out float halfDepth, out float halfWidth);
        float height = bounds.size.y;
        float centerY = bounds.min.y + Mathf.Clamp(height * 0.62f, 0.55f, Mathf.Max(0.55f, height - 0.15f));
        float width = Mathf.Clamp(halfWidth * 2f * 0.8f, 0.3f, 0.6f);

        int layer = cabinet.gameObject.layer;
        Vector3 facePoint = new Vector3(bounds.center.x, centerY, bounds.center.z) + front * halfDepth;
        var root = new GameObject(DrawerName);
        root.layer = layer;
        root.transform.SetParent(transform, false);
        root.transform.SetPositionAndRotation(facePoint, Quaternion.LookRotation(front, Vector3.up));

        // Everything that slides lives under Slider; local +z points out of the cabinet.
        Transform slider = Child("Slider", root.transform, layer, Vector3.zero);
        Material frontMat = MaterialFor(FrontColor, "Drawer Front");
        Material darkMat = MaterialFor(DarkColor, "Drawer Dark");
        Material labelMat = MaterialFor(LabelColor, "Drawer Label");
        Material folderMat = MaterialFor(FolderColor, "Rosa Folder");

        Box("Front", slider, layer, new Vector3(0f, 0f, Proud + FrontDepth * 0.5f), new Vector3(width, FrontHeight, FrontDepth), frontMat);
        Box("Handle", slider, layer, new Vector3(0f, -0.045f, Proud + FrontDepth + 0.012f), new Vector3(0.12f, 0.018f, 0.024f), darkMat);
        Box("LabelPlate", slider, layer, new Vector3(0f, 0.035f, Proud + FrontDepth + 0.002f), new Vector3(Mathf.Min(0.22f, width - 0.04f), 0.07f, 0.004f), labelMat);
        Label(slider, layer, new Vector3(0f, 0.035f, Proud + FrontDepth + 0.0045f), new Vector2(Mathf.Min(0.21f, width - 0.05f), 0.062f),
            "<b>MARKET CLIENTS</b>\nR. Santos · Stall 04");

        // Drawer box behind the front (inside the cabinet until it slides out).
        float inner = width - 0.04f;
        Box("Bottom", slider, layer, new Vector3(0f, -FrontHeight * 0.4f, Proud - BodyDepth * 0.5f), new Vector3(inner, 0.01f, BodyDepth), darkMat);
        Box("SideLeft", slider, layer, new Vector3(-inner * 0.5f, -FrontHeight * 0.15f, Proud - BodyDepth * 0.5f), new Vector3(0.01f, FrontHeight * 0.5f, BodyDepth), darkMat);
        Box("SideRight", slider, layer, new Vector3(inner * 0.5f, -FrontHeight * 0.15f, Proud - BodyDepth * 0.5f), new Vector3(0.01f, FrontHeight * 0.5f, BodyDepth), darkMat);

        // Rosa's folder standing in the drawer, with a small tab label.
        Transform folder = Child("RosaFolder", slider, layer, new Vector3(0f, -FrontHeight * 0.05f, Proud - BodyDepth * 0.45f));
        Box("Cover", folder, layer, Vector3.zero, new Vector3(inner * 0.85f, 0.16f, 0.018f), folderMat);
        Box("Tab", folder, layer, new Vector3(-inner * 0.2f, 0.09f, 0f), new Vector3(0.07f, 0.025f, 0.018f), folderMat);
        Label(folder, layer, new Vector3(-inner * 0.2f, 0.09f, 0.0105f), new Vector2(0.065f, 0.02f), "ROSA S.");

        // One static tap target over the drawer front (the cabinet behind keeps its own collider).
        BoxCollider hit = root.AddComponent<BoxCollider>();
        hit.center = new Vector3(0f, 0f, Proud + FrontDepth);
        hit.size = new Vector3(width + 0.04f, FrontHeight + 0.04f, 0.08f);

        Drawer = root.AddComponent<MarketClientDrawer>();
        Drawer.Init(this, slider, folder);
        Debug.Log($"[Level2] Market-client drawer at {facePoint:F2}, facing {front:F2}, width {width:F2} m.");
    }

    private static bool TryBounds(Transform target, out Bounds bounds)
    {
        bounds = default;
        Renderer[] renderers = target.GetComponentsInChildren<Renderer>(true);
        bool any = false;
        foreach (Renderer r in renderers)
        {
            if (r is ParticleSystemRenderer) continue;
            if (!any) { bounds = r.bounds; any = true; }
            else bounds.Encapsulate(r.bounds);
        }
        return any;
    }

    /// <summary>The cabinet side with the most open space in front of it (the side people stand at).</summary>
    private static Vector3 ChooseFront(Transform cabinet, Bounds bounds, out float halfDepth, out float halfWidth)
    {
        Vector3 f = Flat(cabinet.forward), r = Flat(cabinet.right);
        Vector3[] sides = { f, -f, r, -r };
        Vector3 best = f;
        float bestClear = -1f;
        float probeY = bounds.min.y + Mathf.Min(1f, bounds.size.y * 0.6f);
        foreach (Vector3 side in sides)
        {
            float half = Extent(bounds, side);
            Vector3 origin = new Vector3(bounds.center.x, probeY, bounds.center.z) + side * (half + 0.02f);
            float clear = 3f;
            foreach (RaycastHit hit in Physics.RaycastAll(origin, side, 3f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                if (hit.collider.transform.IsChildOf(cabinet)) continue;
                clear = Mathf.Min(clear, hit.distance);
            }
            if (clear > bestClear + 0.01f)
            {
                bestClear = clear;
                best = side;
            }
        }
        halfDepth = Extent(bounds, best);
        halfWidth = Extent(bounds, Vector3.Cross(Vector3.up, best));
        return best;
    }

    private static Vector3 Flat(Vector3 v)
    {
        v.y = 0f;
        return v.sqrMagnitude > 1e-6f ? v.normalized : Vector3.forward;
    }

    private static float Extent(Bounds b, Vector3 direction) =>
        Mathf.Abs(direction.x) * b.extents.x + Mathf.Abs(direction.z) * b.extents.z;

    private static Transform Child(string name, Transform parent, int layer, Vector3 localPosition)
    {
        var go = new GameObject(name);
        go.layer = layer;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPosition;
        return go.transform;
    }

    private static void Box(string name, Transform parent, int layer, Vector3 localPosition, Vector3 size, Material material)
    {
        GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
        box.name = name;
        box.layer = layer;
        Object.Destroy(box.GetComponent<Collider>());
        box.transform.SetParent(parent, false);
        box.transform.localPosition = localPosition;
        box.transform.localScale = size;
        box.GetComponent<MeshRenderer>().sharedMaterial = material;
    }

    private static void Label(Transform parent, int layer, Vector3 localPosition, Vector2 size, string text)
    {
        var go = new GameObject("Label", typeof(RectTransform));
        go.layer = layer;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPosition;
        // TextMeshPro reads from its -z side; the player looks at the drawer along -z.
        go.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
        var label = go.AddComponent<TextMeshPro>();
        label.rectTransform.sizeDelta = size;
        label.font = HudTheme.Font;
        label.text = text;
        label.color = InkColor;
        label.alignment = TextAlignmentOptions.Center;
        label.enableAutoSizing = true;
        label.fontSizeMin = 0.04f;
        label.fontSizeMax = 0.3f;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.richText = true;
    }

    private Material MaterialFor(Color color, string name)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        var material = new Material(shader) { name = "Level2 " + name, color = color };
        materials.Add(material);
        return material;
    }

    private void OnDestroy()
    {
        foreach (Material material in materials)
            if (material != null) Destroy(material);
        materials.Clear();
    }
}
