using UnityEngine;

/// <summary>
/// PURPOSE:
/// The catalogue's live miniature (T13): a tiny display stage far from the
/// scene, with its own camera rendering one decor model on a round wooden
/// plinth into a texture that the book's right page shows. The model turns
/// slowly to face the player and back, never more than a quarter turn
/// (+/- 40 degrees); choosing it lifts it a little, and a committed purchase
/// plays a short 0.3 s lift-and-settle. The camera only renders while a
/// model is shown and the catalogue is open.
///
/// The stage sits 5 km away on an unnamed layer that only its camera draws,
/// so no other camera, search or placement ever meets it.
/// </summary>
public sealed class ShopPreviewStage : MonoBehaviour
{
    private const int TextureSize = 512;
    private const float SwingDegrees = 40f;
    private const float SwingSpeed = 0.5f;
    private static readonly Vector3 StagePosition = new Vector3(5000f, -40f, 5000f);

    private Camera stageCamera;
    private RenderTexture texture;
    private Transform turntable, lift;
    private GameObject model;
    private string shown = "";
    private int layer;
    private float liftAt = -10f, settleAt = -10f;

    public Texture Texture => texture;
    public bool HasModel => model != null;

    public static ShopPreviewStage Create(Color background)
    {
        var host = new GameObject("Shop Preview Stage (T13)");
        host.transform.position = StagePosition;
        ShopPreviewStage stage = host.AddComponent<ShopPreviewStage>();
        stage.layer = FreeLayer();
        stage.Build(background);
        return stage;
    }

    /// <summary>An unnamed, unused layer (the highest free one) so only the stage camera draws the stage.</summary>
    private static int FreeLayer()
    {
        for (int i = 31; i >= 8; i--)
            if (string.IsNullOrEmpty(LayerMask.LayerToName(i))) return i;
        return 31;
    }

    private void Build(Color background)
    {
        texture = new RenderTexture(TextureSize, TextureSize, 16, RenderTextureFormat.ARGB32) { name = "Shop Preview", antiAliasing = 2 };
        texture.Create();

        var cameraHost = new GameObject("Stage Camera");
        cameraHost.transform.SetParent(transform, false);
        stageCamera = cameraHost.AddComponent<Camera>();
        stageCamera.clearFlags = CameraClearFlags.SolidColor;
        stageCamera.backgroundColor = background;
        stageCamera.cullingMask = 1 << layer;
        stageCamera.fieldOfView = 22f;
        stageCamera.nearClipPlane = 0.02f;
        stageCamera.farClipPlane = 6f;
        stageCamera.allowHDR = false;
        stageCamera.allowMSAA = false;
        stageCamera.targetTexture = texture;
        stageCamera.enabled = false;

        // A warm key light beside the camera (the scene's sun also reaches the stage).
        var lightHost = new GameObject("Stage Light");
        lightHost.transform.SetParent(transform, false);
        lightHost.transform.localPosition = new Vector3(-0.35f, 0.6f, 0.55f);
        Light key = lightHost.AddComponent<Light>();
        key.type = LightType.Point;
        key.range = 3f;
        key.intensity = 1.4f;
        key.color = new Color(1f, 0.95f, 0.86f);
        key.cullingMask = 1 << layer;

        turntable = new GameObject("Turntable").transform;
        turntable.SetParent(transform, false);
        lift = new GameObject("Lift").transform;
        lift.SetParent(turntable, false);
    }

    /// <summary>Shows a decor model (rebuilding only when it changes). False when the item has no model.</summary>
    public bool Show(string itemId)
    {
        if (itemId == shown && model != null) { SetRendering(true); return true; }
        if (model != null) Destroy(model);
        foreach (Transform child in turntable) if (child != lift) Destroy(child.gameObject);
        model = null;
        shown = itemId ?? "";
        if (!DecorModels.Has(itemId)) { SetRendering(false); return false; }

        model = DecorModels.Build(itemId, lift);
        // The frame stands on the plinth leaning back a touch, like a print on a shop easel.
        bool frame = itemId == ShopCatalogue.IslandPrint;
        if (frame)
        {
            model.transform.localRotation = Quaternion.Euler(-8f, 0f, 0f);
            model.transform.localPosition = new Vector3(0f, DecorModels.PrintSize.y * 0.5f + 0.004f, -0.02f);
        }
        float radius = frame ? DecorModels.PrintSize.x * 0.42f : DecorModels.PencilCupFootprint.x * 0.95f;
        Plinth(radius);
        DecorModels.SetLayer(turntable.gameObject, layer);
        FrameCamera();
        Lift();
        SetRendering(true);
        return true;
    }

    /// <summary>Choosing it: the miniature rises a little and settles.</summary>
    public void Lift() => liftAt = Time.unscaledTime;

    /// <summary>A committed purchase: a short 0.3 s lift-and-settle (as it goes to the office).</summary>
    public void Settle() => settleAt = Time.unscaledTime;

    public void SetRendering(bool on)
    {
        if (stageCamera != null) stageCamera.enabled = on && model != null;
    }

    private void Plinth(float radius)
    {
        GameObject disc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        disc.name = "Plinth";
        Destroy(disc.GetComponent<Collider>());
        disc.transform.SetParent(turntable, false);
        disc.transform.localPosition = new Vector3(0f, -0.006f, 0f);
        disc.transform.localScale = new Vector3(radius * 2f, 0.006f, radius * 2f);
        GameObject rim = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        rim.name = "Plinth Rim";
        Destroy(rim.GetComponent<Collider>());
        rim.transform.SetParent(turntable, false);
        rim.transform.localPosition = new Vector3(0f, -0.0135f, 0f);
        rim.transform.localScale = new Vector3(radius * 2f + 0.01f, 0.0025f, radius * 2f + 0.01f);
        if (DecorModels.PlinthWood != null) disc.GetComponent<Renderer>().sharedMaterial = DecorModels.PlinthWood;
        if (DecorModels.PlinthBrass != null) rim.GetComponent<Renderer>().sharedMaterial = DecorModels.PlinthBrass;
    }

    /// <summary>Fits the whole miniature in view from slightly above and in front.</summary>
    private void FrameCamera()
    {
        Quaternion saved = turntable.localRotation;
        turntable.localRotation = Quaternion.identity;
        Bounds bounds = DecorModels.WorldBounds(turntable.gameObject);
        turntable.localRotation = saved;
        float radius = Mathf.Max(0.05f, bounds.extents.magnitude);
        float distance = radius / Mathf.Sin(stageCamera.fieldOfView * 0.5f * Mathf.Deg2Rad) * 1.04f;
        Vector3 direction = new Vector3(0f, 0.36f, 1f).normalized;
        stageCamera.transform.position = bounds.center + direction * distance;
        stageCamera.transform.LookAt(bounds.center);
        stageCamera.farClipPlane = distance + radius * 3f;
    }

    private void Update()
    {
        if (model == null) return;
        float now = Time.unscaledTime;
        // Slowly turns to face the player and back: at most a quarter turn in all.
        turntable.localRotation = Quaternion.Euler(0f, Mathf.Sin(now * SwingSpeed) * SwingDegrees, 0f);
        float rise = 0f;
        float l = (now - liftAt) / 0.45f;
        if (l >= 0f && l < 1f) rise += Mathf.Sin(l * Mathf.PI) * 0.012f;
        float s = (now - settleAt) / 0.3f;
        if (s >= 0f && s < 1f) rise += (s < 0.45f ? HudTheme.EaseOutCubic(s / 0.45f) : 1f - HudTheme.EaseOutBack((s - 0.45f) / 0.55f)) * 0.03f;
        lift.localPosition = new Vector3(0f, rise, 0f);
    }

    private void OnDestroy()
    {
        if (stageCamera != null) stageCamera.targetTexture = null;
        if (texture != null)
        {
            texture.Release();
            Destroy(texture);
        }
    }
}
