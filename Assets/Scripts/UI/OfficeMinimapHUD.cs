using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Builds the non-interactive minimap panel inside the existing safe-area HUD.</summary>
public sealed class OfficeMinimapHUD : MonoBehaviour
{
    private const float PanelWidth = 248f;
    private const float PanelHeight = 294f;
    private const float MapSize = 232f;

    private Canvas hostCanvas;
    private GameObject safeAreaRoot;
    private CanvasGroup canvasGroup;
    private RawImage mapImage;
    private RectTransform mapRect;
    private bool visible;
    private RectTransform panelRect;
    private Vector2 previousSafeSize;
    private RectTransform settingsRoot;
    private RectTransform iconsRoot;
    private Vector3 settingsOriginalPosition;
    private Vector3 iconsOriginalPosition;
    private bool fits = true;
    private bool settingsPlaced;

    public RectTransform MapRect => mapRect;

    public bool IsAvailable => isActiveAndEnabled && gameObject.activeInHierarchy &&
                               hostCanvas != null && hostCanvas.isActiveAndEnabled &&
                               safeAreaRoot != null && safeAreaRoot.activeInHierarchy &&
                               (panelRect == null || panelRect.localScale.x >= 0.55f);

    public static OfficeMinimapHUD TryCreate(Scene scene)
    {
        Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include);
        Canvas hudCanvas = null;
        for (int i = 0; i < canvases.Length; i++)
        {
            Canvas candidate = canvases[i];
            if (candidate.gameObject.scene == scene && candidate.name == "HUD_Canvas" &&
                candidate.renderMode == RenderMode.ScreenSpaceOverlay &&
                candidate.GetComponent<CanvasScaler>() != null)
            {
                hudCanvas = candidate;
                break;
            }
        }

        if (hudCanvas == null) return null;

        GameObject safeAreaObject = CreateRect("Office Minimap Safe Area", hudCanvas.transform);
        RectTransform parent = safeAreaObject.GetComponent<RectTransform>();
        Stretch(parent, 0f, 0f, 0f, 0f);
        safeAreaObject.AddComponent<MobileControlsSafeArea>();

        GameObject panelObject = CreateRect("Office Minimap HUD", parent);
        RectTransform panel = panelObject.GetComponent<RectTransform>();
        panel.anchorMin = Vector2.one;
        panel.anchorMax = Vector2.one;
        panel.pivot = Vector2.one;
        panel.anchoredPosition = new Vector2(-20f, -20f);
        panel.sizeDelta = new Vector2(PanelWidth, PanelHeight);

        Image frame = panelObject.AddComponent<Image>();
        frame.color = new Color32(202, 169, 101, 255);
        Shadow shadow = panelObject.AddComponent<Shadow>();
        shadow.effectColor = new Color(0.015f, 0.025f, 0.04f, 0.55f);
        shadow.effectDistance = new Vector2(4f, -5f);

        CanvasGroup group = panelObject.AddComponent<CanvasGroup>();
        group.alpha = 0f;
        group.interactable = false;
        group.blocksRaycasts = false;

        GameObject bodyObject = CreateRect("Body", panel);
        Stretch(bodyObject.GetComponent<RectTransform>(), 2f, 2f, -2f, -2f);
        Image body = bodyObject.AddComponent<Image>();
        body.color = new Color32(18, 28, 41, 255);

        RectTransform bodyRect = bodyObject.GetComponent<RectTransform>();
        RectTransform header = CreateRect("Header", bodyRect).GetComponent<RectTransform>();
        AnchorTop(header, 26f);
        AddLabel("Office", header, "OFFICE", 18f, TextAlignmentOptions.MidlineLeft,
            new Color32(245, 240, 226, 255), new Vector2(12f, 0f), new Vector2(92f, 0f));
        AddLabel("North", header, "N", 16f, TextAlignmentOptions.Midline,
            new Color32(221, 190, 126, 255), Vector2.zero, new Vector2(28f, 0f), true);
        AddLabel("Floor", header, "1F", 18f, TextAlignmentOptions.MidlineRight,
            new Color32(221, 190, 126, 255), new Vector2(-12f, 0f), new Vector2(38f, 0f), false, true);

        RectTransform mapFrame = CreateRect("Map Frame", bodyRect).GetComponent<RectTransform>();
        mapFrame.anchorMin = mapFrame.anchorMax = new Vector2(0.5f, 1f);
        mapFrame.pivot = new Vector2(0.5f, 1f);
        mapFrame.anchoredPosition = new Vector2(0f, -26f);
        mapFrame.sizeDelta = new Vector2(MapSize + 4f, MapSize + 4f);
        Image mapBorder = mapFrame.gameObject.AddComponent<Image>();
        mapBorder.color = new Color32(201, 171, 112, 255);

        GameObject viewportObject = CreateRect("Map Viewport", mapFrame);
        RectTransform viewport = viewportObject.GetComponent<RectTransform>();
        Stretch(viewport, 2f, 2f, -2f, -2f);
        Image mapBacking = viewportObject.AddComponent<Image>();
        mapBacking.color = new Color32(47, 61, 72, 255);
        viewportObject.AddComponent<RectMask2D>();

        GameObject mapImageObject = CreateRect("Map Texture", viewport);
        Stretch(mapImageObject.GetComponent<RectTransform>(), 0f, 0f, 0f, 0f);
        RawImage rawImage = mapImageObject.AddComponent<RawImage>();
        rawImage.color = Color.white;

        RectTransform footer = CreateRect("Legend", bodyRect).GetComponent<RectTransform>();
        AnchorBottom(footer, 26f);
        AddSwatch("You Swatch", footer, new Color32(100, 215, 255, 255), new Vector2(13f, 0f));
        AddLabel("You", footer, "YOU", 18f, TextAlignmentOptions.MidlineLeft,
            new Color32(228, 235, 239, 255), new Vector2(26f, 0f), new Vector2(60f, 0f));
        AddSwatch("People Swatch", footer, new Color32(241, 194, 108, 255), new Vector2(105f, 0f));
        AddLabel("People", footer, "PEOPLE", 18f, TextAlignmentOptions.MidlineLeft,
            new Color32(228, 235, 239, 255), new Vector2(118f, 0f), new Vector2(90f, 0f));

        Graphic[] graphics = panelObject.GetComponentsInChildren<Graphic>(true);
        for (int i = 0; i < graphics.Length; i++) graphics[i].raycastTarget = false;

        OfficeMinimapHUD hud = panelObject.AddComponent<OfficeMinimapHUD>();
        hud.panelRect = panel;
        hud.hostCanvas = hudCanvas;
        hud.safeAreaRoot = safeAreaObject;
        hud.canvasGroup = group;
        hud.mapImage = rawImage;
        hud.mapRect = viewport;
        hud.settingsRoot = hudCanvas.transform.Find("PermanentHUD/Settings") as RectTransform;
        hud.iconsRoot = hudCanvas.transform.Find("PermanentHUD/IconButtons") as RectTransform;
        if (hud.settingsRoot != null) hud.settingsOriginalPosition = hud.settingsRoot.localPosition;
        if (hud.iconsRoot != null) hud.iconsOriginalPosition = hud.iconsRoot.localPosition;
        hud.UpdateSize();
        return hud;
    }

    public void SetMapTexture(Texture texture)
    {
        if (mapImage != null) mapImage.texture = texture;
    }

    public void SetVisible(bool value)
    {
        if (visible == value) return;
        visible = value;
        if (canvasGroup != null) canvasGroup.alpha = value ? 1f : 0f;
    }

    private void LateUpdate()
    {
        UpdateSize();
        PositionExistingControls();
    }

    private void PositionExistingControls()
    {
        if (panelRect == null || safeAreaRoot == null) return;
        RectTransform safe = (RectTransform)safeAreaRoot.transform;
        float right = safe.rect.xMax - 20f;
        float top = safe.rect.yMax - 20f;
        float scale = panelRect.localScale.x;
        // Translate only the existing containers; retain child sizes, order,
        // spacing, graphics, parents and click/drag bindings.
        if (settingsRoot != null)
        {
            // Map shown: Settings sits left of the map. Map hidden (desk,
            // corkboard, small screens): Settings moves to the top of the icon
            // column, centred on the icons (120 wide, right edge at `right`),
            // while the icons keep their place below.
            bool mapShown = visible && fits;
            Vector3 target = mapShown
                ? new Vector3(right - PanelWidth * scale - 16f, top, 0f)
                : new Vector3(right - 10f, top, 0f);
            Vector3 world = safe.TransformPoint(target);
            settingsRoot.position = settingsPlaced
                ? Vector3.Lerp(settingsRoot.position, world, 1f - Mathf.Exp(-14f * Time.unscaledDeltaTime))
                : world;
            settingsPlaced = true;
        }
        if (iconsRoot != null)
            iconsRoot.position = safe.TransformPoint(new Vector3(right - 10f, top - PanelHeight * scale, 0f));
        // Icons retain their authored x=10/y=-20 inset: right aligned with the
        // map, first icon 20 units below it. Settings remains 100x100.
    }

    private void OnDestroy()
    {
        if (settingsRoot != null) settingsRoot.localPosition = settingsOriginalPosition;
        if (iconsRoot != null) iconsRoot.localPosition = iconsOriginalPosition;
    }

    private void UpdateSize()
    {
        if (safeAreaRoot == null || panelRect == null) return;
        Vector2 available = ((RectTransform)safeAreaRoot.transform).rect.size;
        if (available == previousSafeSize || available.x <= 0f || available.y <= 0f) return;
        previousSafeSize = available;
        // Reserve space left of the map for the unchanged Settings button.
        float scale = Mathf.Min(1f, Mathf.Min((available.x - 156f) / PanelWidth,
            (available.y - 40f) / PanelHeight));
        fits = scale >= 0.55f;
        panelRect.localScale = Vector3.one * Mathf.Max(0.01f, scale);
        if (canvasGroup != null) canvasGroup.alpha = visible && fits ? 1f : 0f;
    }

    public void Dispose()
    {
        if (safeAreaRoot != null) Destroy(safeAreaRoot);
    }

    private static GameObject CreateRect(string name, Transform parent)
    {
        var child = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer));
        child.layer = LayerMask.NameToLayer("UI");
        child.transform.SetParent(parent, false);
        return child;
    }

    private static void Stretch(RectTransform rect, float left, float bottom, float right, float top)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(left, bottom);
        rect.offsetMax = new Vector2(right, top);
    }

    private static void AnchorTop(RectTransform rect, float height)
    {
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(0f, height);
    }

    private static void AnchorBottom(RectTransform rect, float height)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = new Vector2(1f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(0f, height);
    }

    private static TextMeshProUGUI AddLabel(string name, RectTransform parent, string text, float fontSize,
        TextAlignmentOptions alignment, Color color, Vector2 position, Vector2 size, bool centered = false,
        bool rightAligned = false)
    {
        RectTransform rect = CreateRect(name, parent).GetComponent<RectTransform>();
        float anchorX = centered ? 0.5f : rightAligned ? 1f : 0f;
        rect.anchorMin = new Vector2(anchorX, 0f);
        rect.anchorMax = new Vector2(anchorX, 1f);
        rect.pivot = new Vector2(anchorX, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        TextMeshProUGUI label = rect.gameObject.AddComponent<TextMeshProUGUI>();
        label.text = text;
        label.font = TMP_Settings.defaultFontAsset;
        label.fontSize = fontSize;
        label.fontStyle = FontStyles.Bold;
        label.color = color;
        label.alignment = alignment;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.overflowMode = TextOverflowModes.Truncate;
        label.raycastTarget = false;
        return label;
    }

    private static void AddSwatch(string name, RectTransform parent, Color color, Vector2 position)
    {
        RectTransform rect = CreateRect(name, parent).GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0f, 0.5f);
        rect.pivot = new Vector2(0f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = new Vector2(10f, 10f);
        OfficeMinimapSymbol image = rect.gameObject.AddComponent<OfficeMinimapSymbol>();
        image.IsPlayer = name == "You Swatch";
        image.color = color;
        image.raycastTarget = false;
    }
}
