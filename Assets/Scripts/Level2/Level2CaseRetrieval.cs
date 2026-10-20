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
    private const float FrontDepth = 0.008f;
    private const float BodyDepth = 0.34f;
    private const float Proud = 0.002f;

    // Matches the grey low-poly cabinets; the label card and orange tab make this drawer findable.
    private static readonly Color FrontColor = new Color32(112, 119, 130, 255);
    private static readonly Color SeamColor = new Color32(34, 37, 43, 255);
    private static readonly Color DarkColor = new Color32(52, 56, 64, 255);
    private static readonly Color ChromeColor = new Color32(196, 202, 210, 255);
    private static readonly Color BrassColor = new Color32(186, 150, 82, 255);
    private static readonly Color LabelColor = new Color32(248, 242, 222, 255);
    private static readonly Color TabColor = new Color32(232, 128, 44, 255);
    private static readonly Color FolderColor = new Color32(226, 190, 122, 255);
    private static readonly Color RosaTabColor = new Color32(214, 74, 60, 255);
    private static readonly Color InkColor = new Color32(30, 36, 48, 255);
    private static readonly Color[] OtherFolderColors =
    {
        new Color32(206, 176, 116, 255), new Color32(120, 160, 120, 255), new Color32(110, 140, 186, 255),
        new Color32(214, 186, 130, 255), new Color32(170, 120, 150, 255)
    };

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
        // This authored cabinet has four rows. Fit the second drawer face,
        // instead of placing a small extra drawer across the existing seams.
        float centerY = bounds.min.y + height * 0.625f;
        float width = halfWidth * 2f * 0.90f;
        float faceHeight = height * 0.215f;

        int layer = cabinet.gameObject.layer;
        Vector3 facePoint = new Vector3(bounds.center.x, centerY, bounds.center.z) + front * halfDepth;
        var root = new GameObject(DrawerName);
        root.layer = layer;
        root.transform.SetParent(transform, false);
        root.transform.SetPositionAndRotation(facePoint, Quaternion.LookRotation(front, Vector3.up));

        Material frontMat = MaterialFor(FrontColor, "Drawer Front");
        Material seamMat = MaterialFor(SeamColor, "Drawer Seam");
        Material darkMat = MaterialFor(DarkColor, "Drawer Inside");
        Material chromeMat = MaterialFor(ChromeColor, "Drawer Chrome", 0.85f, 0.6f);
        Material brassMat = MaterialFor(BrassColor, "Label Holder", 0.7f, 0.55f);
        Material labelMat = MaterialFor(LabelColor, "Drawer Label");
        Material tabMat = MaterialFor(TabColor, "Market Tab");
        Material folderMat = MaterialFor(FolderColor, "Rosa Folder");
        Material rosaTabMat = MaterialFor(RosaTabColor, "Rosa Tab");

        // A dark seam just around the front reads as the drawer's cut in the cabinet.
        Box("Seam", root.transform, layer, new Vector3(0f, 0f, Proud * 0.5f), new Vector3(width + 0.012f, faceHeight + 0.012f, 0.002f), seamMat);

        // Everything that slides lives under Slider; local +z points out of the cabinet.
        Transform slider = Child("Slider", root.transform, layer, Vector3.zero);
        float fz = Proud + FrontDepth;
        Box("Front", slider, layer, new Vector3(0f, 0f, Proud + FrontDepth * 0.5f), new Vector3(width, faceHeight, FrontDepth), frontMat);
        // Raised lip along the top edge, like the cabinet's own drawers.
        Box("Lip", slider, layer, new Vector3(0f, faceHeight * 0.5f - 0.006f, fz + 0.003f), new Vector3(width - 0.01f, 0.012f, 0.006f), frontMat);

        // Chrome pull: a bar on two posts.
        float handleY = -faceHeight * 0.22f;
        Box("HandleBar", slider, layer, new Vector3(0f, handleY, fz + 0.026f), new Vector3(0.15f, 0.016f, 0.012f), chromeMat);
        Box("HandlePostL", slider, layer, new Vector3(-0.064f, handleY, fz + 0.012f), new Vector3(0.012f, 0.012f, 0.024f), chromeMat);
        Box("HandlePostR", slider, layer, new Vector3(0.064f, handleY, fz + 0.012f), new Vector3(0.012f, 0.012f, 0.024f), chromeMat);

        // Brass label holder with a cream card and an orange MARKET tab.
        float labelY = faceHeight * 0.2f;
        float labelW = Mathf.Min(0.2f, width - 0.06f);
        Box("LabelHolder", slider, layer, new Vector3(0f, labelY, fz + 0.002f), new Vector3(labelW + 0.014f, 0.072f, 0.004f), brassMat);
        Box("LabelPlate", slider, layer, new Vector3(0f, labelY, fz + 0.0045f), new Vector3(labelW, 0.06f, 0.002f), labelMat);
        Box("MarketTab", slider, layer, new Vector3(-labelW * 0.5f + 0.012f, labelY, fz + 0.006f), new Vector3(0.018f, 0.06f, 0.0015f), tabMat);
        Label(slider, layer, new Vector3(0.008f, labelY, fz + 0.0062f), new Vector2(labelW - 0.03f, 0.054f),
            "<b>MARKET CLIENTS</b>\n<size=70%>R. Santos · Stall 04</size>");

        // Drawer box behind the front (inside the cabinet until it slides out).
        float inner = width - 0.04f;
        float wallHeight = faceHeight * 0.62f;
        float floorY = -faceHeight * 0.42f;
        Box("Bottom", slider, layer, new Vector3(0f, floorY, Proud - BodyDepth * 0.5f), new Vector3(inner, 0.01f, BodyDepth), darkMat);
        Box("SideLeft", slider, layer, new Vector3(-inner * 0.5f, floorY + wallHeight * 0.5f, Proud - BodyDepth * 0.5f), new Vector3(0.01f, wallHeight, BodyDepth), darkMat);
        Box("SideRight", slider, layer, new Vector3(inner * 0.5f, floorY + wallHeight * 0.5f, Proud - BodyDepth * 0.5f), new Vector3(0.01f, wallHeight, BodyDepth), darkMat);
        Box("Back", slider, layer, new Vector3(0f, floorY + wallHeight * 0.5f, Proud - BodyDepth), new Vector3(inner, wallHeight, 0.01f), darkMat);
        // Hanging rails along the sides.
        Box("RailLeft", slider, layer, new Vector3(-inner * 0.5f + 0.012f, floorY + wallHeight, Proud - BodyDepth * 0.5f), new Vector3(0.006f, 0.006f, BodyDepth), chromeMat);
        Box("RailRight", slider, layer, new Vector3(inner * 0.5f - 0.012f, floorY + wallHeight, Proud - BodyDepth * 0.5f), new Vector3(0.006f, 0.006f, BodyDepth), chromeMat);

        // Other clients' hanging folders behind Rosa's, riffled when the drawer opens.
        float folderHeight = Mathf.Min(0.16f, wallHeight + 0.03f);
        float folderY = floorY + folderHeight * 0.5f + 0.006f;
        var others = new List<Transform>();
        for (int i = 0; i < OtherFolderColors.Length; i++)
        {
            Material mat = MaterialFor(OtherFolderColors[i], "Client Folder " + i);
            Transform other = Child("ClientFolder" + i, slider, layer, new Vector3(0f, folderY, Proud - BodyDepth * (0.3f + 0.12f * i)));
            Box("Cover", other, layer, Vector3.zero, new Vector3(inner * 0.86f, folderHeight, 0.012f), mat);
            Box("Tab", other, layer, new Vector3((i % 3 - 1) * inner * 0.28f, folderHeight * 0.5f + 0.01f, 0f), new Vector3(0.06f, 0.022f, 0.012f), mat);
            others.Add(other);
        }

        // Rosa's folder at the front, with a red tab and her name on the cover.
        Transform folder = Child("RosaFolder", slider, layer, new Vector3(0f, folderY, Proud - BodyDepth * 0.16f));
        Box("Cover", folder, layer, Vector3.zero, new Vector3(inner * 0.86f, folderHeight, 0.016f), folderMat);
        Box("Tab", folder, layer, new Vector3(-inner * 0.22f, folderHeight * 0.5f + 0.012f, 0f), new Vector3(0.074f, 0.026f, 0.016f), rosaTabMat);
        Label(folder, layer, new Vector3(-inner * 0.22f, folderHeight * 0.5f + 0.012f, 0.0085f), new Vector2(0.07f, 0.022f), "<b>SANTOS, R.</b>");
        Box("NameCard", folder, layer, new Vector3(0f, folderHeight * 0.12f, 0.0085f), new Vector3(inner * 0.5f, 0.05f, 0.001f), labelMat);
        Label(folder, layer, new Vector3(0f, folderHeight * 0.12f, 0.0095f), new Vector2(inner * 0.48f, 0.046f),
            "<b>ALING ROSA SANTOS</b>\n<size=75%>L2-ROSA-001 · May review</size>");

        // One static tap target over the drawer front (the cabinet behind keeps its own collider).
        BoxCollider hit = root.AddComponent<BoxCollider>();
        hit.center = new Vector3(0f, 0f, Proud + FrontDepth);
        hit.size = new Vector3(width, faceHeight, 0.05f);

        Drawer = root.AddComponent<MarketClientDrawer>();
        Drawer.Init(this, slider, folder, others, labelMat);
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

    private Material MaterialFor(Color color, string name, float metallic = 0f, float smoothness = 0.25f)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        var material = new Material(shader) { name = "Level2 " + name, color = color };
        if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", metallic);
        if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", smoothness);
        if (material.HasProperty("_Glossiness")) material.SetFloat("_Glossiness", smoothness);
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
