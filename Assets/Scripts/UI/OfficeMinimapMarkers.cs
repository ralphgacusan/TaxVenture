using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Scene-local UI markers. Reads transforms only; never changes NPC state.</summary>
public sealed class OfficeMinimapMarkers
{
    private sealed class Entry
    {
        public NpcStatusMarker Source;
        public TutorialNpcController Guide;
        public RectTransform Icon;
    }

    private readonly Dictionary<NpcStatusMarker, Entry> entries = new Dictionary<NpcStatusMarker, Entry>();
    private readonly List<NpcStatusMarker> removed = new List<NpcStatusMarker>();
    private readonly RectTransform root;
    private readonly RectTransform playerIcon;
    private readonly Scene scene;
    private float nextDiscovery;

    public OfficeMinimapMarkers(RectTransform viewport, Scene ownerScene)
    {
        scene = ownerScene;
        root = NewRect("Live Markers", viewport, Vector2.zero);
        root.anchorMin = Vector2.zero;
        root.anchorMax = Vector2.one;
        root.offsetMin = root.offsetMax = Vector2.zero;
        playerIcon = CreateIcon("Player", true);
    }

    public bool IsValid => root != null;

    public void Tick(Camera camera, CameraController controller)
    {
        if (root == null || controller.PlayerRoot == null) return;
        if (Time.unscaledTime >= nextDiscovery)
        {
            nextDiscovery = Time.unscaledTime + 1f;
            Discover();
        }

        removed.Clear();
        foreach (var pair in entries)
        {
            Entry entry = pair.Value;
            if (entry.Source == null || !entry.Source.isActiveAndEnabled || entry.Source.gameObject.scene != scene)
            {
                if (entry.Icon != null)
                {
                    entry.Icon.gameObject.SetActive(false);
                    Object.Destroy(entry.Icon.gameObject);
                }
                removed.Add(pair.Key);
                continue;
            }
            bool revealed = entry.Guide == null || entry.Guide.HasAppeared;
            entry.Icon.gameObject.SetActive(revealed && Place(camera, entry.Icon, entry.Source.transform.position, 9f));
        }
        for (int i = 0; i < removed.Count; i++) entries.Remove(removed[i]);

        Place(camera, playerIcon, controller.PlayerRoot.position, 12f);
        Quaternion facing = controller.PlayerRoot.rotation;
        if (controller.TryGetFirstPersonView(out _, out Quaternion viewRotation)) facing = viewRotation;
        Vector3 forward = facing * Vector3.forward;
        float yaw = Mathf.Atan2(forward.x, forward.z) * Mathf.Rad2Deg;
        playerIcon.localRotation = Quaternion.Euler(0f, 0f, -yaw);
        playerIcon.SetAsLastSibling();
    }

    private void Discover()
    {
        NpcStatusMarker[] sources = Object.FindObjectsByType<NpcStatusMarker>(FindObjectsInactive.Exclude);
        for (int i = 0; i < sources.Length; i++)
        {
            NpcStatusMarker source = sources[i];
            if (!source.isActiveAndEnabled || source.gameObject.scene != scene || entries.ContainsKey(source)) continue;
            entries.Add(source, new Entry
            {
                Source = source,
                Guide = source.GetComponent<TutorialNpcController>(),
                Icon = CreateIcon("Person", false)
            });
        }
    }

    private bool Place(Camera camera, RectTransform icon, Vector3 world, float inset)
    {
        // The map is a horizontal floor plan: marker height must not affect clipping.
        world.y = 0f;
        Vector3 viewport = camera.WorldToViewportPoint(world);
        Vector2 size = root.rect.size;
        if (size.x <= 0f || size.y <= 0f || viewport.z <= 0f) return false;
        Vector2 position = new Vector2((viewport.x - 0.5f) * size.x, (viewport.y - 0.5f) * size.y);
        if (Mathf.Abs(position.x) > size.x * 0.5f - inset || Mathf.Abs(position.y) > size.y * 0.5f - inset)
            return false;
        icon.anchoredPosition = position;
        return true;
    }

    private RectTransform CreateIcon(string name, bool player)
    {
        RectTransform icon = NewRect(name, root, Vector2.one * (player ? 23f : 15f));
        OfficeMinimapSymbol symbol = icon.gameObject.AddComponent<OfficeMinimapSymbol>();
        symbol.IsPlayer = player;
        symbol.color = player ? new Color32(100, 215, 255, 255) : new Color32(241, 194, 108, 255);
        symbol.raycastTarget = false;
        Outline outline = icon.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color32(12, 22, 33, 255);
        outline.effectDistance = new Vector2(2f, -2f);
        return icon;
    }

    private static RectTransform NewRect(string name, Transform parent, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(LayoutElement));
        go.layer = LayerMask.NameToLayer("UI");
        RectTransform rect = (RectTransform)go.transform;
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        go.GetComponent<LayoutElement>().ignoreLayout = true;
        return rect;
    }

    public void Dispose()
    {
        if (root != null)
        {
            root.gameObject.SetActive(false);
            Object.Destroy(root.gameObject);
        }
        entries.Clear();
        removed.Clear();
    }
}
