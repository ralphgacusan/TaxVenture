using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// PURPOSE:
/// The one emotion system for people and things in the world (T22 extends
/// the original reaction bubbles). Ten emotions from EmoteCatalogue, each a
/// different drawn shape (heart, sparkles, "!", "?", "...", light bulb, sweat
/// drop, rain cloud, anger puff, music note) with its own idle motion:
/// - a BUBBLE (cream speech bubble with a tail) for most uses, timed or held
///   for as long as a dialogue line lasts (Hold / Release);
/// - a BURST of 5-8 free-floating icons for peak moments only.
/// Placement: above the target's head bone (else its renderers' top), a
/// little to the side so it never covers the face, kept above a given screen
/// line (the dialogue box) and inside the screen; constant on-screen size;
/// hidden while the target is off screen, behind the camera or not drawn.
/// Screen-space overlay, never blocks touches, unscaled time, pooled.
/// The original kinds (Cheer, Shrug, Wait, Worry) still work and map onto
/// the new set (EmoteCatalogue.FromLegacyKind). Self-contained: call
/// WorldEmote.Show(...) from anywhere in a scene.
/// Reduced motion (PlayerPrefs "ReducedMotion" = 1, no settings row yet,
/// KNOWN_ISSUES CF-45): icons stay, idle motion stops, a burst becomes one
/// still bubble.
/// </summary>
public sealed class WorldEmote : MonoBehaviour
{
    public enum Kind { Cheer, Shrug, Wait, Worry }

    public const string ReducedMotionKey = "ReducedMotion";

    private const int SortingOrder = 420;
    private const int BubblePoolSize = 8;
    private const int SparkPoolSize = 16;
    private const float EnterSeconds = 0.2f;
    private const float ExitSeconds = 0.25f;
    private const float BubbleWidth = 84f;
    private const float BubbleHeight = 80f;
    private const float SideOffset = 40f;
    private const float ScreenMargin = 16f;
    private const float SparkSize = 46f;

    private static readonly Color BubbleFill = new Color32(245, 240, 226, 255);  // HudTheme cream
    private static readonly Color Ink = new Color32(22, 34, 50, 255);
    private static readonly Color IconOutline = new Color32(22, 34, 50, 235);
    private static readonly Color DropShadow = new Color(0f, 0f, 0f, 0.3f);

    /// <summary>Where an emote sits: a followed transform (head bone if any) or a fixed world point.</summary>
    private sealed class Anchor
    {
        public Transform Follow;
        public bool HasFollow;
        public Transform Head;
        public float HeadLift;
        public Vector3 WorldPoint;
        public readonly List<Renderer> Renderers = new List<Renderer>();
    }

    private sealed class Bubble
    {
        public RectTransform Root;
        public CanvasGroup Group;
        public Image Fill;
        public Image Tail;
        public RectTransform Icon;
        public Image IconShadow;
        public Image IconOutline;
        public Image IconFill;
        public Image Glow;
        public RectTransform[] Dots;
        public Image[] DotFills;
        public RectTransform Drop;
        public Image DropFill;
        public readonly Anchor Anchor = new Anchor();
        public EmoteStyle Style;
        public bool Reduced;
        public float Age;
        public float Life;          // PositiveInfinity while held
        public bool Releasing;
        public float ReleaseAt;
        public float MinScreenY;
        public float Side = 1f;
        public int Token;
        public bool Active;
    }

    private sealed class Spark
    {
        public RectTransform Root;
        public CanvasGroup Group;
        public Image Shadow;
        public Image Outline;
        public Image Fill;
        public readonly Anchor Anchor = new Anchor();
        public float Age;
        public float Delay;
        public float Life;
        public Vector2 Drift;
        public float Rise;
        public float Spin;
        public bool Active;
    }

    private static WorldEmote instance;
    private RectTransform root;
    private readonly List<Bubble> bubbles = new List<Bubble>();
    private readonly List<Spark> sparks = new List<Spark>();
    private readonly List<Transform> boneSearch = new List<Transform>();
    private int nextToken;
    private int warmShape;

    // =========================================================
    // PUBLIC API
    // =========================================================

    /// <summary>Original kinds: a bubble above a target for a few seconds.</summary>
    public static void Show(Transform target, Kind kind, float seconds = 2.5f) =>
        Show(target, EmoteCatalogue.FromLegacyKind((int)kind), seconds);

    /// <summary>Original kinds: a bubble at a fixed world point.</summary>
    public static void ShowAt(Vector3 world, Scene scene, Kind kind, float seconds = 2.5f)
    {
        WorldEmote e = Ensure(scene);
        if (e != null) e.SpawnBubble(null, world, EmoteCatalogue.FromLegacyKind((int)kind), Mathf.Max(0.8f, seconds), 0f);
    }

    /// <summary>A bubble above a target (head bone or renderers' top) for a few seconds.</summary>
    public static void Show(Transform target, Emotion emotion, float seconds = 2.5f)
    {
        if (target == null) return;
        WorldEmote e = Ensure(target.gameObject.scene);
        if (e != null) e.SpawnBubble(target, Vector3.zero, emotion, Mathf.Max(0.8f, seconds), 0f);
    }

    /// <summary>
    /// A bubble that stays until Release(token) (a dialogue line). minScreenY:
    /// screen pixels the bubble must stay above (the dialogue box's top). Returns 0 if nothing shows.
    /// </summary>
    public static int Hold(Transform target, Emotion emotion, float minScreenY = 0f)
    {
        if (target == null) return 0;
        WorldEmote e = Ensure(target.gameObject.scene);
        return e != null ? e.SpawnBubble(target, Vector3.zero, emotion, float.PositiveInfinity, minScreenY) : 0;
    }

    /// <summary>Lets a held bubble leave (fade and drift up).</summary>
    public static void Release(int token)
    {
        if (token == 0 || instance == null) return;
        foreach (Bubble b in instance.bubbles)
            if (b.Active && b.Token == token) StartExit(b);
    }

    /// <summary>Peak moments only: 5-8 icons rise and drift apart over about 1.2-1.6 seconds.</summary>
    public static void Burst(Transform target, Emotion emotion, int count = 6)
    {
        if (target == null) return;
        WorldEmote e = Ensure(target.gameObject.scene);
        if (e != null) e.SpawnBurst(target, emotion, EmoteCatalogue.BurstCount(count));
    }

    public static bool ReducedMotion
    {
        get
        {
            try { return PlayerPrefs.GetInt(ReducedMotionKey, 0) == 1; }
            catch (Exception) { return false; }
        }
    }

    // =========================================================
    // SETUP
    // =========================================================

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

    // =========================================================
    // SPAWNING
    // =========================================================

    private int SpawnBubble(Transform follow, Vector3 world, Emotion emotion, float life, float minScreenY)
    {
        EmoteStyle style = EmoteCatalogue.Get(emotion);
        if (!style.IsValid) return 0;

        Bubble b = null;
        for (int i = 0; i < bubbles.Count && follow != null; i++)
            if (bubbles[i].Active && bubbles[i].Anchor.Follow == follow) { b = bubbles[i]; break; } // replace on same target
        if (b == null)
            for (int i = 0; i < bubbles.Count; i++)
                if (!bubbles[i].Active) { b = bubbles[i]; break; }
        if (b == null && bubbles.Count < BubblePoolSize)
        {
            b = BuildBubble();
            bubbles.Add(b);
        }
        if (b == null)
            for (int i = 0; i < bubbles.Count; i++)
                if (!float.IsPositiveInfinity(bubbles[i].Life)) { b = bubbles[i]; break; }
        if (b == null) b = bubbles[0];

        SetAnchor(b.Anchor, follow, world);
        b.Style = style;
        b.Reduced = ReducedMotion;
        b.Age = 0f;
        b.Life = life;
        b.Releasing = false;
        b.MinScreenY = minScreenY;
        b.Side = 1f;
        b.Token = ++nextToken;
        if (nextToken == int.MaxValue) nextToken = 0;
        b.Active = true;
        Style(b);
        b.Root.gameObject.SetActive(true);
        PlaySound(style);
        return b.Token;
    }

    private void SpawnBurst(Transform follow, Emotion emotion, int count)
    {
        EmoteStyle style = EmoteCatalogue.Get(emotion);
        if (!style.IsValid) return;
        if (ReducedMotion)
        {
            SpawnBubble(follow, Vector3.zero, emotion, 1.4f, 0f);
            return;
        }

        Sprite fill = EmoteSprites.Fill(style.Shape);
        Sprite outline = EmoteSprites.Outline(style.Shape);
        Color color = new Color32(style.R, style.G, style.B, 255);
        for (int i = 0; i < count; i++)
        {
            Spark s = null;
            for (int k = 0; k < sparks.Count; k++)
                if (!sparks[k].Active) { s = sparks[k]; break; }
            if (s == null)
            {
                if (sparks.Count >= SparkPoolSize) break;
                s = BuildSpark();
                sparks.Add(s);
            }

            SetAnchor(s.Anchor, follow, Vector3.zero);
            float side = (i % 2 == 0 ? 1f : -1f) * (0.35f + 0.65f * (i + 1) / count);
            s.Age = 0f;
            s.Delay = i * 0.06f + UnityEngine.Random.Range(0f, 0.04f);
            s.Life = UnityEngine.Random.Range(0.9f, 1.1f);
            s.Drift = new Vector2(side * UnityEngine.Random.Range(50f, 95f), 0f);
            s.Rise = UnityEngine.Random.Range(110f, 165f);
            s.Spin = UnityEngine.Random.Range(-14f, 14f);
            s.Fill.sprite = fill;
            s.Fill.color = color;
            s.Outline.sprite = outline;
            s.Shadow.sprite = outline;
            s.Group.alpha = 0f;
            s.Active = true;
            s.Root.gameObject.SetActive(true);
        }
        PlaySound(style);
    }

    private void SetAnchor(Anchor a, Transform follow, Vector3 world)
    {
        a.Follow = follow;
        a.HasFollow = follow != null;
        a.WorldPoint = world;
        a.Head = null;
        a.HeadLift = 0f;
        a.Renderers.Clear();
        if (follow == null) return;

        follow.GetComponentsInChildren(false, a.Renderers);
        for (int i = a.Renderers.Count - 1; i >= 0; i--)
        {
            Renderer r = a.Renderers[i];
            if (!(r is MeshRenderer || r is SkinnedMeshRenderer) || r.GetComponentInParent<Canvas>() != null ||
                r.GetComponentInParent<TaxpayerProfileWorldPickup>() != null)
                a.Renderers.RemoveAt(i);
        }

        a.Head = FindHead(follow);
        if (a.Head != null && TryBounds(a.Renderers, out Bounds bounds))
            a.HeadLift = Mathf.Max(0.12f, bounds.max.y - a.Head.position.y) + 0.06f;
        else if (a.Head != null)
            a.HeadLift = 0.28f;
    }

    private Transform FindHead(Transform root)
    {
        Animator animator = root.GetComponentInChildren<Animator>();
        if (animator != null && animator.isHuman)
        {
            Transform head = animator.GetBoneTransform(HumanBodyBones.Head);
            if (head != null) return head;
        }
        boneSearch.Clear();
        root.GetComponentsInChildren(true, boneSearch);
        Transform found = null;
        for (int i = 0; i < boneSearch.Count && found == null; i++)
        {
            string n = boneSearch[i].name;
            if (n.IndexOf("head", StringComparison.OrdinalIgnoreCase) >= 0 &&
                n.IndexOf("end", StringComparison.OrdinalIgnoreCase) < 0 &&
                n.IndexOf("top", StringComparison.OrdinalIgnoreCase) < 0) found = boneSearch[i];
        }
        boneSearch.Clear();
        return found;
    }

    private static void PlaySound(EmoteStyle style)
    {
        // Through the SFX setting: AudioManager plays nothing while SFX is off.
        if (!string.IsNullOrEmpty(style.SoundId) && AudioManager.Instance != null)
            AudioManager.Instance.PlayEffect(style.SoundId, style.SoundVolume);
    }

    private static void StartExit(Bubble b)
    {
        if (b.Releasing) return;
        b.Releasing = true;
        b.ReleaseAt = b.Age;
    }

    // =========================================================
    // BUILDING (pooled; built once)
    // =========================================================

    private Bubble BuildBubble()
    {
        var b = new Bubble();
        b.Root = HudTheme.CreateRect("Emote", root);
        b.Root.anchorMin = b.Root.anchorMax = Vector2.zero;
        b.Root.pivot = new Vector2(0.5f, 0f);
        b.Root.sizeDelta = new Vector2(BubbleWidth, BubbleHeight);
        b.Group = b.Root.gameObject.AddComponent<CanvasGroup>();
        b.Group.blocksRaycasts = false;
        b.Group.interactable = false;

        // Drop shadow, dark rim, cream body; the tail is a rotated square under the body.
        RectTransform shadow = HudTheme.CreateRect("Shadow", b.Root);
        shadow.anchorMin = Vector2.zero;
        shadow.anchorMax = Vector2.one;
        shadow.offsetMin = new Vector2(3f, 7f);
        shadow.offsetMax = new Vector2(3f, -3f);
        HudTheme.AddImage(shadow, HudTheme.Rounded, DropShadow, 24f);

        RectTransform tailRim = HudTheme.CreateRect("TailRim", b.Root);
        tailRim.anchorMin = tailRim.anchorMax = new Vector2(0.5f, 0f);
        tailRim.sizeDelta = new Vector2(24f, 24f);
        tailRim.anchoredPosition = new Vector2(0f, 12f);
        tailRim.localRotation = Quaternion.Euler(0f, 0f, 45f);
        HudTheme.AddImage(tailRim, null, Ink);

        RectTransform rim = HudTheme.CreateRect("Rim", b.Root);
        rim.anchorMin = Vector2.zero;
        rim.anchorMax = Vector2.one;
        rim.offsetMin = new Vector2(0f, 10f);
        rim.offsetMax = Vector2.zero;
        HudTheme.AddImage(rim, HudTheme.Rounded, Ink, 24f);

        RectTransform tail = HudTheme.CreateRect("Tail", b.Root);
        tail.anchorMin = tail.anchorMax = new Vector2(0.5f, 0f);
        tail.sizeDelta = new Vector2(18f, 18f);
        tail.anchoredPosition = new Vector2(0f, 14f);
        tail.localRotation = Quaternion.Euler(0f, 0f, 45f);
        b.Tail = HudTheme.AddImage(tail, null, BubbleFill);

        RectTransform body = HudTheme.CreateRect("Body", rim);
        HudTheme.Stretch(body, 3f);
        b.Fill = HudTheme.AddImage(body, HudTheme.Rounded, BubbleFill, 21f);

        b.Icon = HudTheme.CreateRect("Icon", body);
        b.Icon.anchorMin = b.Icon.anchorMax = new Vector2(0.5f, 0.5f);
        b.Icon.sizeDelta = new Vector2(56f, 56f);

        RectTransform glow = HudTheme.CreateRect("Glow", b.Icon);
        glow.anchorMin = glow.anchorMax = new Vector2(0.5f, 0.56f);
        glow.sizeDelta = new Vector2(78f, 78f);
        b.Glow = HudTheme.AddImage(glow, null, Color.white);

        b.IconShadow = AddLayer(b.Icon, "Shadow", new Vector2(2f, -2f));
        b.IconOutline = AddLayer(b.Icon, "Outline", Vector2.zero);
        b.IconFill = AddLayer(b.Icon, "Fill", Vector2.zero);

        b.Dots = new RectTransform[3];
        b.DotFills = new Image[3];
        for (int i = 0; i < 3; i++)
        {
            b.Dots[i] = HudTheme.CreateRect("Dot", b.Icon);
            b.Dots[i].anchorMin = b.Dots[i].anchorMax = new Vector2(0.2f + 0.3f * i, 0.5f);
            b.Dots[i].sizeDelta = new Vector2(18f, 18f);
            Image dotOutline = AddLayer(b.Dots[i], "Outline", Vector2.zero);
            dotOutline.sprite = EmoteSprites.DotOutline;
            dotOutline.color = IconOutline;
            b.DotFills[i] = AddLayer(b.Dots[i], "Fill", Vector2.zero);
            b.DotFills[i].sprite = EmoteSprites.Dot;
        }

        b.Drop = HudTheme.CreateRect("Drop", b.Icon);
        b.Drop.anchorMin = b.Drop.anchorMax = new Vector2(0.5f, 0.2f);
        b.Drop.sizeDelta = new Vector2(16f, 16f);
        Image dropOutline = AddLayer(b.Drop, "Outline", Vector2.zero);
        dropOutline.sprite = EmoteSprites.Outline(EmoteShape.SweatDrop);
        dropOutline.color = IconOutline;
        b.DropFill = AddLayer(b.Drop, "Fill", Vector2.zero);
        b.DropFill.sprite = EmoteSprites.Fill(EmoteShape.SweatDrop);

        b.Root.gameObject.SetActive(false);
        return b;
    }

    private Spark BuildSpark()
    {
        var s = new Spark();
        s.Root = HudTheme.CreateRect("EmoteSpark", root);
        s.Root.anchorMin = s.Root.anchorMax = Vector2.zero;
        s.Root.pivot = new Vector2(0.5f, 0.5f);
        s.Root.sizeDelta = new Vector2(SparkSize, SparkSize);
        s.Group = s.Root.gameObject.AddComponent<CanvasGroup>();
        s.Group.blocksRaycasts = false;
        s.Group.interactable = false;
        s.Shadow = AddLayer(s.Root, "Shadow", new Vector2(2f, -3f));
        s.Shadow.color = DropShadow;
        s.Outline = AddLayer(s.Root, "Outline", Vector2.zero);
        s.Outline.color = IconOutline;
        s.Fill = AddLayer(s.Root, "Fill", Vector2.zero);
        s.Root.gameObject.SetActive(false);
        return s;
    }

    private static Image AddLayer(RectTransform parent, string name, Vector2 offset)
    {
        RectTransform rect = HudTheme.CreateRect(name, parent);
        HudTheme.Stretch(rect);
        rect.anchoredPosition = offset;
        return HudTheme.AddImage(rect, null, Color.white);
    }

    private static void Style(Bubble b)
    {
        EmoteStyle style = b.Style;
        Color color = new Color32(style.R, style.G, style.B, 255);
        bool dots = style.Shape == EmoteShape.Dots;
        b.IconFill.sprite = EmoteSprites.Fill(style.Shape);
        b.IconFill.color = color;
        b.IconOutline.sprite = EmoteSprites.Outline(style.Shape);
        b.IconOutline.color = IconOutline;
        b.IconShadow.sprite = b.IconOutline.sprite;
        b.IconShadow.color = DropShadow;
        b.IconFill.enabled = b.IconOutline.enabled = b.IconShadow.enabled = !dots;
        for (int i = 0; i < 3; i++)
        {
            b.Dots[i].gameObject.SetActive(dots);
            b.Dots[i].localScale = Vector3.one;
            b.DotFills[i].color = color;
            b.DotFills[i].canvasRenderer.SetAlpha(1f);
        }
        bool glow = style.Shape == EmoteShape.Bulb;
        b.Glow.gameObject.SetActive(glow);
        if (glow)
        {
            b.Glow.sprite = EmoteSprites.Glow;
            b.Glow.color = new Color(1f, 0.9f, 0.45f, 0.45f);
            b.Glow.canvasRenderer.SetAlpha(1f);
            b.Glow.rectTransform.localScale = Vector3.one;
        }
        bool drop = style.Shape == EmoteShape.RainCloud;
        b.Drop.gameObject.SetActive(drop);
        if (drop)
        {
            b.DropFill.color = new Color32(100, 170, 235, 255);
            b.DropFill.canvasRenderer.SetAlpha(1f);
            b.Drop.anchoredPosition = Vector2.zero;
        }
        b.Icon.anchoredPosition = Vector2.zero;
        b.Icon.localScale = Vector3.one;
        b.Icon.localRotation = Quaternion.identity;
        b.Root.localScale = Vector3.zero;
        b.Group.alpha = 0f;
    }

    // =========================================================
    // PER FRAME (no allocations)
    // =========================================================

    private void LateUpdate()
    {
        // Draw one icon per frame in the background so the first emote of a shape never hitches.
        if (warmShape < EmoteCatalogue.All.Count)
            EmoteSprites.Fill(EmoteCatalogue.All[warmShape++].Shape);

        Camera cam = GuideTargets.ViewCamera;
        float dt = Time.unscaledDeltaTime;
        float scale = root.lossyScale.x > 0f ? root.lossyScale.x : 1f;
        Vector2 canvas = root.rect.size;

        for (int i = 0; i < bubbles.Count; i++)
        {
            Bubble b = bubbles[i];
            if (!b.Active) continue;
            b.Age += dt;
            if (b.Anchor.HasFollow && b.Anchor.Follow == null) StartExit(b);
            if (!b.Releasing && b.Age >= b.Life - ExitSeconds) StartExit(b);
            float exit = b.Releasing ? Mathf.Clamp01((b.Age - b.ReleaseAt) / ExitSeconds) : 0f;
            if (exit >= 1f || cam == null)
            {
                b.Active = false;
                b.Root.gameObject.SetActive(false);
                continue;
            }

            bool visible = TryScreen(b.Anchor, cam, out Vector3 sp);
            Vector2 pos = new Vector2(sp.x, sp.y) / scale;
            float center = canvas.x * 0.5f;
            if (pos.x < center - canvas.x * 0.12f) b.Side = -1f;
            else if (pos.x > center + canvas.x * 0.12f) b.Side = 1f;
            pos = Place(pos, b.Side, b.MinScreenY / scale, canvas);
            pos.y += 28f * HudTheme.EaseOutCubic(exit);

            b.Root.anchoredPosition = pos;
            b.Root.localScale = Vector3.one * EnterScale(b.Age);
            b.Group.alpha = visible ? Mathf.Min(1f, b.Age / 0.08f) * (1f - exit) : 0f;
            Animate(b);
        }

        for (int i = 0; i < sparks.Count; i++)
        {
            Spark s = sparks[i];
            if (!s.Active) continue;
            s.Age += dt;
            float t = (s.Age - s.Delay) / s.Life;
            if (t >= 1f || cam == null || (s.Anchor.HasFollow && s.Anchor.Follow == null))
            {
                s.Active = false;
                s.Root.gameObject.SetActive(false);
                continue;
            }
            if (t < 0f)
            {
                s.Group.alpha = 0f;
                continue;
            }

            bool visible = TryScreen(s.Anchor, cam, out Vector3 sp);
            float rise = HudTheme.EaseOutCubic(t);
            Vector2 pos = new Vector2(sp.x, sp.y) / scale + new Vector2(0f, 24f) + s.Drift * rise + new Vector2(0f, s.Rise * rise);
            s.Root.anchoredPosition = pos;
            float age = t * s.Life;
            float size = EnterScale(age) * (t > 0.75f ? Mathf.Lerp(1f, 0.7f, (t - 0.75f) / 0.25f) : 1f);
            s.Root.localScale = Vector3.one * size;
            s.Root.localRotation = Quaternion.Euler(0f, 0f, s.Spin * Mathf.Sin(age * 6f));
            s.Group.alpha = visible ? (t > 0.65f ? 1f - (t - 0.65f) / 0.35f : 1f) : 0f;
        }
    }

    // Entrance: 0 -> 1.15 -> 1 in EnterSeconds.
    private static float EnterScale(float age)
    {
        const float peakAt = EnterSeconds * 0.65f;
        if (age <= 0f) return 0f;
        if (age < peakAt) return 1.15f * HudTheme.EaseOutCubic(age / peakAt);
        if (age < EnterSeconds) return Mathf.Lerp(1.15f, 1f, (age - peakAt) / (EnterSeconds - peakAt));
        return 1f;
    }

    /// <summary>Beside and above the anchor, above minY, inside the screen; beside the head if the top is too close.</summary>
    private static Vector2 Place(Vector2 head, float side, float minY, Vector2 canvas)
    {
        Vector2 pos = head + new Vector2(side * SideOffset, 6f);
        float top = canvas.y - ScreenMargin - BubbleHeight;
        if (pos.y > top)
        {
            // Close-up: the head top is near the screen top, so sit beside the head instead of on the face.
            pos.y = top;
            pos.x += side * (BubbleWidth * 0.9f);
        }
        if (pos.y < minY + 8f) pos.y = minY + 8f;
        float half = BubbleWidth * 0.5f + ScreenMargin;
        pos.x = Mathf.Clamp(pos.x, half, Mathf.Max(half, canvas.x - half));
        return pos;
    }

    private static bool TryScreen(Anchor a, Camera cam, out Vector3 sp)
    {
        Vector3 world;
        if (a.HasFollow)
        {
            if (a.Follow == null)
            {
                sp = Vector3.zero;
                return false;
            }
            if (a.Head != null) world = a.Head.position + Vector3.up * a.HeadLift;
            else if (TryBounds(a.Renderers, out Bounds bounds)) world = new Vector3(bounds.center.x, bounds.max.y + 0.08f, bounds.center.z);
            else world = a.Follow.position + Vector3.up * 2f;
        }
        else world = a.WorldPoint;

        sp = cam.WorldToScreenPoint(world);
        bool onScreen = sp.z > 0.05f && sp.x > -50f && sp.x < Screen.width + 50f && sp.y > -50f && sp.y < Screen.height + 50f;
        return onScreen && (!a.HasFollow || Drawn(a.Renderers));
    }

    // A target whose renderers are all hidden (e.g. the conversation-only Elijah between conversations) shows no emote.
    private static bool Drawn(List<Renderer> renderers)
    {
        if (renderers.Count == 0) return true;
        for (int i = 0; i < renderers.Count; i++)
        {
            Renderer r = renderers[i];
            if (r != null && r.enabled && r.gameObject.activeInHierarchy) return true;
        }
        return false;
    }

    private static bool TryBounds(List<Renderer> renderers, out Bounds bounds)
    {
        bounds = default;
        bool any = false;
        for (int i = 0; i < renderers.Count; i++)
        {
            Renderer r = renderers[i];
            if (r == null || !r.enabled || !r.gameObject.activeInHierarchy) continue;
            if (!any) { bounds = r.bounds; any = true; }
            else bounds.Encapsulate(r.bounds);
        }
        return any;
    }

    /// <summary>The per-emotion idle motion (none under reduced motion).</summary>
    private static void Animate(Bubble b)
    {
        float t = b.Age;
        Vector2 offset = Vector2.zero;
        float scale = 1f;
        float angle = 0f;
        bool still = b.Reduced;

        switch (b.Style.Motion)
        {
            case EmoteMotion.Pulse:
                if (!still) scale = 1f + 0.09f * Mathf.Max(0f, Mathf.Sin(t * 7.5f));
                break;
            case EmoteMotion.Twinkle:
                if (!still)
                {
                    float pop = Mathf.Max(0f, 1f - Mathf.Repeat(t, 1.2f) / 0.18f);
                    scale = 1f + 0.05f * Mathf.Sin(t * 9f) + 0.12f * pop;
                    angle = 7f * Mathf.Sin(t * 3f);
                }
                break;
            case EmoteMotion.PopShake:
                if (!still) offset.x = 4f * Mathf.Sin(t * 48f) * Mathf.Max(0f, 1f - (t - EnterSeconds) / 0.4f) * (t > EnterSeconds ? 1f : 0f);
                break;
            case EmoteMotion.Wobble:
                if (!still) angle = 12f * Mathf.Sin(t * 2.4f);
                break;
            case EmoteMotion.DotsInTurn:
                for (int i = 0; i < 3; i++)
                {
                    float shown = still ? 1f : Mathf.Clamp01((Mathf.Repeat(t, 1.4f) - i * 0.3f) / 0.12f);
                    b.DotFills[i].canvasRenderer.SetAlpha(shown);
                    b.Dots[i].localScale = Vector3.one * (0.6f + 0.4f * shown);
                }
                break;
            case EmoteMotion.GlowPulse:
                {
                    float glow = still ? 0.5f : 0.5f + 0.5f * Mathf.Sin(t * 4f);
                    b.Glow.canvasRenderer.SetAlpha(0.35f + 0.65f * glow);
                    b.Glow.rectTransform.localScale = Vector3.one * (0.9f + 0.2f * glow);
                }
                break;
            case EmoteMotion.SlideDown:
                if (!still)
                {
                    float slide = Mathf.Repeat(t, 1.5f) / 1.5f;
                    offset = new Vector2(6f, 9f - 18f * HudTheme.EaseOutCubic(slide));
                }
                break;
            case EmoteMotion.Drip:
                {
                    float drip = still ? 0.3f : Mathf.Repeat(t, 1.1f) / 1.1f;
                    b.Drop.anchoredPosition = new Vector2(4f, -14f * drip);
                    b.DropFill.canvasRenderer.SetAlpha(still ? 1f : 1f - drip * drip);
                }
                break;
            case EmoteMotion.DoublePulse:
                if (!still)
                {
                    float c = Mathf.Repeat(t, 1.5f);
                    float p = Mathf.Max(0f, 1f - Mathf.Abs(c - 0.08f) / 0.08f) + Mathf.Max(0f, 1f - Mathf.Abs(c - 0.3f) / 0.08f);
                    scale = 1f + 0.16f * p;
                }
                break;
            case EmoteMotion.SwayUp:
                if (!still)
                {
                    angle = 10f * Mathf.Sin(t * 3f);
                    offset.y = 3f + 4f * Mathf.Sin(t * 3f + 1f);
                }
                break;
        }

        b.Icon.anchoredPosition = offset;
        b.Icon.localScale = Vector3.one * scale;
        b.Icon.localRotation = Quaternion.Euler(0f, 0f, angle);
    }
}
