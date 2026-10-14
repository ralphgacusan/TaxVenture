using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[InitializeOnLoad]
public static class PaperCollectionValidation
{
    private const string SessionKey = "TaxVenture.PaperCollectionValidation";
    private const string ReportPath = "Library/PaperCollectionValidationReport.txt";
    private static bool running;
    private const BindingFlags InstanceFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    private const BindingFlags StaticFlags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

    static PaperCollectionValidation() => EditorApplication.delayCall += RunIfChanged;

    [MenuItem("Tools/TaxVenture/Run Paper Collection Checks")]
    private static void RunManually() => RunChecks(SourceVersion(), true);

    private static void RunIfChanged()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || running) return;
        string version = SourceVersion();
        if (SessionState.GetString(SessionKey, string.Empty) != version) RunChecks(version, false);
    }

    private static string SourceVersion()
    {
        var source = new StringBuilder("1");
        foreach (string path in new[] { "Assets/Scripts/SideQuest1/TaxpayerProfileWorldPickup.cs", "Assets/Scripts/SideQuest1/TaxpayerProfileCollectionHUD.cs", "Assets/Editor/PaperCollectionValidation.cs" })
            source.Append('\n').Append(path).Append('\n').Append(File.Exists(path) ? File.ReadAllText(path) : "missing");
        return Hash128.Compute(source.ToString()).ToString();
    }

    private static void RunChecks(string version, bool force)
    {
        if (running || EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || (!force && SessionState.GetString(SessionKey, string.Empty) == version)) return;
        running = true;
        var passes = new List<string>();
        var failures = new List<string>();
        Scene preview = default;
        var originalHud = GetSingleton(typeof(TaxpayerProfileCollectionHUD)); var originalPanel = GetSingleton(typeof(CollectedProfilesWorldPanel));
        var originalAudio = GetSingleton(typeof(AudioManager));
        FieldInfo profileEvent = typeof(TownEvents).GetField("OnProfileCollected", StaticFlags), allProfilesEvent = typeof(TownEvents).GetField("OnAllProfilesCollected", StaticFlags);
        Delegate originalProfileEvent = profileEvent?.GetValue(null) as Delegate, originalAllProfilesEvent = allProfilesEvent?.GetValue(null) as Delegate;
        try
        {
            if (profileEvent == null || allProfilesEvent == null) throw new MissingFieldException("TownEvents event backing fields were not found.");
            profileEvent.SetValue(null, null); allProfilesEvent.SetValue(null, null);
            SetSingleton(typeof(TaxpayerProfileCollectionHUD), null); SetSingleton(typeof(CollectedProfilesWorldPanel), null);
            SetSingleton(typeof(AudioManager), null);
            preview = EditorSceneManager.NewPreviewScene(); RunPreview(preview, passes, failures);
        }
        catch (Exception exception) { failures.Add("Harness exception: " + exception); }
        finally
        {
            if (profileEvent != null) profileEvent.SetValue(null, originalProfileEvent);
            if (allProfilesEvent != null) allProfilesEvent.SetValue(null, originalAllProfilesEvent);
            SetSingleton(typeof(TaxpayerProfileCollectionHUD), originalHud); SetSingleton(typeof(CollectedProfilesWorldPanel), originalPanel);
            SetSingleton(typeof(AudioManager), originalAudio);
            if (preview.IsValid()) EditorSceneManager.ClosePreviewScene(preview);
            running = false;
            SessionState.SetString(SessionKey, version);
            Directory.CreateDirectory("Library");
            File.WriteAllText(ReportPath, "Paper collection checks\n" + string.Join("\n", passes) + (failures.Count == 0 ? "\nPASS: All checks passed." : "\n" + string.Join("\n", failures)));
        }
        if (failures.Count == 0) Debug.Log("Paper collection checks passed. See " + ReportPath);
        else Debug.LogError("Paper collection checks failed. See " + ReportPath);
    }
    private static void RunPreview(Scene preview, List<string> passes, List<string> failures)
    {
        var canvasObject = new GameObject("Paper collection validation canvas", typeof(RectTransform), typeof(Canvas)); SceneManager.MoveGameObjectToScene(canvasObject, preview);
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        RectTransform canvasRect = (RectTransform)canvasObject.transform; canvasRect.sizeDelta = new Vector2(1280f, 720f);
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        Camera camera = CreateCamera(preview);
        RectTransform target = CreateTarget(canvasRect);
        TaxpayerProfileCollectionHUD hud = target.gameObject.AddComponent<TaxpayerProfileCollectionHUD>();
        SetSingleton(typeof(TaxpayerProfileCollectionHUD), hud);

        CheckProjection(hud, target, null, passes, failures, "Overlay canvas");
        canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = 5f;
        Canvas.ForceUpdateCanvases();
        CheckProjection(hud, target, camera, passes, failures, "Screen Space Camera canvas");

        TaxpayerProfile profile = new TaxpayerProfile("validation_profile", "Validation", "Preview only.", TaxpayerClassification.ResidentCitizen, "validation_npc");
        TaxpayerProfileWorldPickup pickup = CreatePickup(preview, camera, "Collect animation pickup", profile, new Vector3(-1.5f, -0.7f, 2f), Vector3.one);
        Vector3 start = pickup.transform.position;
        Vector3 startScale = pickup.transform.localScale;
        PointerEventData click = CreateClick(camera, pickup.transform.position);
        pickup.OnPointerClick(click);
        Check(pickup.IsCollecting && hud.CollectedProfiles.Count == 0 && pickup.gameObject.activeSelf,
            "Click starts collection while the HUD count remains unchanged.", passes, failures);

        Set(pickup, "collectElapsed", 0.15f);
        Invoke(pickup, "Update");
        float elapsedBeforeRepeat = Get<float>(pickup, "collectElapsed");
        pickup.OnPointerClick(click);
        Check(pickup.IsCollecting && Mathf.Abs(Get<float>(pickup, "collectElapsed") - elapsedBeforeRepeat) < 0.0001f &&
              hud.CollectedProfiles.Count == 0,
            "A repeated click during flight does not restart or complete collection.", passes, failures);

        Set(pickup, "collectElapsed", 0.25f);
        Invoke(pickup, "Update");
        Vector3 quarter = pickup.transform.position;
        Vector3 quarterScale = pickup.transform.localScale;
        Set(pickup, "collectElapsed", Get<float>(pickup, "collectionDuration") * 0.55f);
        Invoke(pickup, "Update");
        Vector3 middle = pickup.transform.position;
        Check(Vector3.Distance(start, quarter) > 0.01f && Vector3.Distance(quarter, middle) > 0.01f &&
              quarterScale.magnitude < startScale.magnitude && pickup.transform.localScale.magnitude <= quarterScale.magnitude &&
              hud.CollectedProfiles.Count == 0,
            "Mid-flight updates move the paper continuously and shrink it before the count changes.", passes, failures);

        Set(pickup, "collectElapsed", Get<float>(pickup, "collectionDuration"));
        Invoke(pickup, "Update");
        Check(!pickup.IsCollecting && !pickup.gameObject.activeSelf && hud.CollectedProfiles.Count == 1,
            "Arrival hides the paper and grants its profile once.", passes, failures);
        pickup.OnPointerClick(click);
        Check(hud.CollectedProfiles.Count == 1, "Clicks after arrival cannot grant the profile again.", passes, failures);

        TaxpayerProfile cancelProfile = new TaxpayerProfile("cancel_profile", "Cancel", "Preview only.", TaxpayerClassification.NonResidentCitizen, "cancel_npc");
        TaxpayerProfileWorldPickup cancelled = CreatePickup(preview, camera, "Cancelled collection pickup", cancelProfile, new Vector3(1f, -0.4f, 2f), new Vector3(1.2f, 0.8f, 1f));
        Vector3 cancelPosition = cancelled.transform.position;
        Vector3 cancelScale = cancelled.transform.localScale;
        cancelled.OnPointerClick(CreateClick(camera, cancelled.transform.position));
        Set(cancelled, "collectElapsed", 0.3f);
        Invoke(cancelled, "Update");
        cancelled.gameObject.SetActive(false);
        Invoke(cancelled, "OnDisable");
        Check(!cancelled.IsCollecting && Vector3.Distance(cancelled.transform.position, cancelPosition) < 0.001f &&
              Vector3.Distance(cancelled.transform.localScale, cancelScale) < 0.001f && hud.CollectedProfiles.Count == 1,
            "Disabling in flight cancels collection, restores the original pose and scale, and grants nothing.", passes, failures);
    }
    private static void CheckProjection(TaxpayerProfileCollectionHUD hud, RectTransform target, Camera camera, List<string> passes, List<string> failures, string label)
    {
        MethodInfo method = typeof(TaxpayerProfileCollectionHUD).GetMethod("TryGetCollectionScreenPoint", InstanceFlags);
        if (method == null) throw new MissingMethodException("TaxpayerProfileCollectionHUD.TryGetCollectionScreenPoint was not found.");
        object[] args = { Vector2.zero };
        bool valid = (bool)method.Invoke(hud, args);
        Vector2 actual = (Vector2)args[0];
        Vector2 expected = RectTransformUtility.WorldToScreenPoint(camera, target.TransformPoint(target.rect.center));
        Vector2 pivot = RectTransformUtility.WorldToScreenPoint(camera, target.position);
        Check(valid && Vector2.Distance(actual, expected) < 0.5f && Vector2.Distance(actual, pivot) > 5f,
            label + " collection destination uses the icon rectangle center with the canvas camera.", passes, failures);
    }
    private static RectTransform CreateTarget(RectTransform parent)
    {
        var go = new GameObject("TaxpayerProfileIcon validation target", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)); go.transform.SetParent(parent, false);
        var target = (RectTransform)go.transform; target.anchorMin = target.anchorMax = target.pivot = Vector2.one;
        target.anchoredPosition = new Vector2(-40f, -45f); target.sizeDelta = new Vector2(120f, 100f); return target;
    }
    private static Camera CreateCamera(Scene scene)
    {
        var go = new GameObject("Paper collection validation camera", typeof(Camera), typeof(PhysicsRaycaster)); SceneManager.MoveGameObjectToScene(go, scene);
        go.transform.position = new Vector3(0f, 0f, -10f);
        Camera camera = go.GetComponent<Camera>(); camera.orthographic = true; camera.orthographicSize = 5f;
        camera.nearClipPlane = 0.1f; camera.farClipPlane = 100f; return camera;
    }
    private static PointerEventData CreateClick(Camera camera, Vector3 worldPosition)
    {
        var data = new PointerEventData(null);
        data.pointerPressRaycast = new RaycastResult { module = camera.GetComponent<PhysicsRaycaster>(), worldPosition = worldPosition, screenPosition = camera.WorldToScreenPoint(worldPosition) };
        return data;
    }
    private static TaxpayerProfileWorldPickup CreatePickup(Scene scene, Camera camera, string name, TaxpayerProfile profile, Vector3 position, Vector3 scale)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube); go.name = name; SceneManager.MoveGameObjectToScene(go, scene);
        go.transform.SetPositionAndRotation(position, Quaternion.identity); go.transform.localScale = scale;
        TaxpayerProfileWorldPickup pickup = go.AddComponent<TaxpayerProfileWorldPickup>();
        foreach (FieldInfo field in typeof(TaxpayerProfileWorldPickup).GetFields(InstanceFlags))
            if (field.FieldType == typeof(Camera)) field.SetValue(pickup, camera);
        pickup.Initialize(profile);
        return pickup;
    }

    private static object GetSingleton(Type type) => type.GetProperty("Instance", StaticFlags)?.GetValue(null);
    private static void SetSingleton(Type type, object value)
    {
        MethodInfo setter = type.GetProperty("Instance", StaticFlags)?.GetSetMethod(true);
        setter?.Invoke(null, new[] { value });
    }
    private static FieldInfo Field(object target, string name) => target.GetType().GetField(name, InstanceFlags);
    private static T Get<T>(object target, string name) => (T)Field(target, name).GetValue(target);
    private static void Set(object target, string name, object value) => Field(target, name).SetValue(target, value);
    private static object Invoke(object target, string name) => target.GetType().GetMethod(name, InstanceFlags).Invoke(target, null);
    private static void Check(bool ok, string message, List<string> passes, List<string> failures)
    { if (ok) passes.Add("PASS: " + message); else failures.Add("FAIL: " + message); }
}
