using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// PURPOSE:
/// Little reaction bubbles over people and things in the world: a cheer
/// (green "!" with a star), a shrug (amber "?"), a wait (slate "...") and a
/// worry (coral "!"). A bubble pops in, bobs, follows its target on screen
/// and fades out; it hides while its target is off screen or behind the
/// camera. Screen-space overlay, never blocks touches. Self-contained:
/// call WorldEmote.Show(...) from anywhere in Office_Level1.
/// </summary>
public sealed class WorldEmote : MonoBehaviour
{
    public enum Kind { Cheer, Shrug, Wait, Worry }

    private const int SortingOrder = 420;
    private const int PoolSize = 8;

    private sealed class Bubble
    {
        public RectTransform Root;
        public CanvasGroup Group;
        public Image Fill;
        public Image Tail;
        public Image Star;
        public TextMeshProUGUI Glyph;
        public Transform Follow;
        public Vector3 WorldPoint;
        public float Age;
        public float Life;
        public bool Active;
    }

    private static WorldEmote instance;
    private RectTransform root;
    private readonly List<Bubble> pool = new List<Bubble>();
    private readonly List<Renderer> renderers = new List<Renderer>();

    /// <summary>Shows a bubble above a target (its renderers' top) for a few seconds.</summary>
    public static void Show(Transform target, Kind kind, float seconds = 2.5f)
    {
        if (target == null) return;
        WorldEmote e = Ensure(target.gameObject.scene);
        if (e != null) e.Spawn(target, Vector3.zero, kind, seconds);
    }

    /// <summary>Shows a bubble at a fixed world point.</summary>
    public static void ShowAt(Vector3 world, Scene scene, Kind kind, float seconds = 2.5f)
    {
        WorldEmote e = Ensure(scene);
        if (e != null) e.Spawn(null, world, kind, seconds);
    }

    private static WorldEmote Ensure(Scene scene)
    {
        if (instance != null) return instance;
        if (!scene.IsValid()) return null;
        var host = new GameObject("World Emotes", typeof(RectTransform));
        SceneManager.MoveGameObjectToScene(host, scene);
        host.layer = LayerMask.NameToLayer("UI");
        Canvas canvas = host.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = SortingOrder;
        CanvasScaler scaler = host.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        instance = host.AddComponent<WorldEmote>();
        instance.root = (RectTransform)host.transform;
        return instance;
    }

    private void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    private void Spawn(Transform follow, Vector3 world, Kind kind, float seconds)
    {
        Bubble b = null;
        for (int i = 0; i < pool.Count; i++)
        {
            if (pool[i].Active && pool[i].Follow == follow && follow != null) { b = pool[i]; break; } // replace on same target
            if (!pool[i].Active && b == null) b = pool[i];
        }
        if (b == null)
        {
            if (pool.Count >= PoolSize) b = pool[0];
            else
            {
                b = Build();
                pool.Add(b);
            }
        }

        b.Follow = follow;
        b.WorldPoint = world;
        b.Age = 0f;
        b.Life = Mathf.Max(0.8f, seconds);
        b.Active = true;
        Style(b, kind);
        b.Root.gameObject.SetActive(true);
    }

    private Bubble Build()
    {
        var b = new Bubble();
        b.Root = HudTheme.CreateRect("Emote", root);
        b.Root.anchorMin = b.Root.anchorMax = Vector2.zero;
        b.Root.pivot = new Vector2(0.5f, 0f);
        b.Root.sizeDelta = new Vector2(76f, 66f);
        b.Group = b.Root.gameObject.AddComponent<CanvasGroup>();
        b.Group.blocksRaycasts = false;
        b.Group.interactable = false;

        RectTransform tail = HudTheme.CreateRect("Tail", b.Root);
        tail.anchorMin = tail.anchorMax = new Vector2(0.5f, 0f);
        tail.sizeDelta = new Vector2(20f, 20f);
        tail.anchoredPosition = new Vector2(0f, 8f);
        tail.localRotation = Quaternion.Euler(0f, 0f, 45f);
        b.Tail = HudTheme.AddImage(tail, null, Color.white);

        RectTransform bubble = HudTheme.CreateRect("Bubble", b.Root);
        bubble.anchorMin = new Vector2(0f, 0f);
        bubble.anchorMax = new Vector2(1f, 1f);
        bubble.offsetMin = new Vector2(0f, 10f);
        bubble.offsetMax = Vector2.zero;
        HudTheme.AddImage(bubble, HudTheme.Rounded, new Color32(22, 34, 50, 255), 22f);
        RectTransform fill = HudTheme.CreateRect("Fill", bubble);
        HudTheme.Stretch(fill, 3f);
        b.Fill = HudTheme.AddImage(fill, HudTheme.Rounded, Color.white, 19f);

        RectTransform star = HudTheme.CreateRect("Star", b.Root);
        star.anchorMin = star.anchorMax = new Vector2(1f, 1f);
        star.sizeDelta = new Vector2(26f, 26f);
        star.anchoredPosition = new Vector2(-4f, -4f);
        b.Star = HudTheme.AddImage(star, HudTheme.Star, HudTheme.Exp);

        b.Glyph = HudTheme.CreateLabel("Glyph", fill, "!", 40f, Color.white, TextAlignmentOptions.Center, FontStyles.Bold);
        HudTheme.Stretch(b.Glyph.rectTransform);
        b.Root.gameObject.SetActive(false);
        return b;
    }

    private static void Style(Bubble b, Kind kind)
    {
        Color fill;
        string glyph;
        bool star = false;
        switch (kind)
        {
            case Kind.Cheer: fill = HudTheme.Complete; glyph = "!"; star = true; break;
            case Kind.Shrug: fill = HudTheme.Exp; glyph = "?"; break;
            case Kind.Wait: fill = new Color32(120, 136, 152, 255); glyph = "..."; break;
            default: fill = HudTheme.Reputation; glyph = "!"; break;
        }
        b.Fill.color = fill;
        b.Tail.color = fill;
        b.Glyph.text = glyph;
        b.Glyph.color = new Color32(22, 34, 50, 255);
        b.Star.gameObject.SetActive(star);
    }

    private void LateUpdate()
    {
        Camera cam = GuideTargets.ViewCamera;
        float dt = Time.unscaledDeltaTime;
        float scale = root.lossyScale.x > 0f ? root.lossyScale.x : 1f;
        for (int i = 0; i < pool.Count; i++)
        {
            Bubble b = pool[i];
            if (!b.Active) continue;
            b.Age += dt;
            if (b.Age >= b.Life || (b.Follow == null && b.WorldPoint == Vector3.zero) || cam == null)
            {
                b.Active = false;
                b.Root.gameObject.SetActive(false);
                continue;
            }

            Vector3 world = b.Follow != null ? Top(b.Follow) : b.WorldPoint;
            Vector3 sp = cam.WorldToScreenPoint(world);
            bool visible = sp.z > 0.05f && sp.x > -50f && sp.x < Screen.width + 50f && sp.y > -50f && sp.y < Screen.height + 50f;
            float pop = HudTheme.EaseOutBack(Mathf.Clamp01(b.Age / 0.3f));
            float fadeOut = Mathf.Clamp01((b.Life - b.Age) / 0.4f);
            b.Group.alpha = visible ? Mathf.Min(1f, b.Age / 0.12f) * fadeOut : 0f;
            float rise = Mathf.Min(1f, b.Age / b.Life) * 18f + Mathf.Sin(b.Age * 5f) * 3f;
            b.Root.anchoredPosition = new Vector2(sp.x, sp.y) / scale + new Vector2(0f, rise);
            b.Root.localScale = Vector3.one * Mathf.Lerp(0.4f, 1f, pop);
            b.Star.rectTransform.localRotation = Quaternion.Euler(0f, 0f, b.Age * 90f);
        }
    }

    private Vector3 Top(Transform t)
    {
        renderers.Clear();
        t.GetComponentsInChildren(false, renderers);
        bool any = false;
        Bounds bounds = default;
        for (int i = 0; i < renderers.Count; i++)
        {
            Renderer r = renderers[i];
            if (!r.enabled || r is ParticleSystemRenderer) continue;
            if (!any) { bounds = r.bounds; any = true; }
            else bounds.Encapsulate(r.bounds);
        }
        return any ? new Vector3(bounds.center.x, bounds.max.y + 0.08f, bounds.center.z) : t.position + Vector3.up * 2f;
    }
}
