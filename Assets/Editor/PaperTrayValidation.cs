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

[InitializeOnLoad]
public static class PaperTrayValidation
{
    private const string SessionKey = "TaxVenture.PaperTrayValidation";
    private const string ReportPath = "Library/PaperTrayValidationReport.txt";
    private const BindingFlags InstanceFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    private static bool running;

    static PaperTrayValidation() => EditorApplication.delayCall += RunIfChanged;

    [MenuItem("Tools/TaxVenture/Run Paper Tray Checks")]
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
        foreach (string path in new[] { "Assets/Scripts/SideQuest1/ProfileDragCard.cs", "Assets/Scripts/SideQuest1/CorkboardSideQuestManager.cs", "Assets/Editor/PaperTrayValidation.cs" })
            source.Append('\n').Append(path).Append('\n').Append(File.Exists(path) ? File.ReadAllText(path) : "missing");
        return Hash128.Compute(source.ToString()).ToString();
    }

    private static void RunChecks(string version, bool force)
    {
        if (running || EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling ||
            (!force && SessionState.GetString(SessionKey, string.Empty) == version)) return;

        running = true;
        var passes = new List<string>();
        var failures = new List<string>();
        Scene preview = default;
        try
        {
            preview = EditorSceneManager.NewPreviewScene();
            RunPreview(preview, passes, failures);
        }
        catch (Exception exception) { failures.Add("Harness exception: " + exception); }
        finally
        {
            if (preview.IsValid()) EditorSceneManager.ClosePreviewScene(preview);
            running = false;
            SessionState.SetString(SessionKey, version);
            Directory.CreateDirectory("Library");
            File.WriteAllText(ReportPath, "Paper tray checks\n" + string.Join("\n", passes) +
                (failures.Count == 0 ? "\nPASS: All checks passed." : "\n" + string.Join("\n", failures)));
        }

        if (failures.Count == 0) Debug.Log("Paper tray checks passed. See " + ReportPath);
        else Debug.LogError("Paper tray checks failed. See " + ReportPath);
    }

    private static void RunPreview(Scene preview, List<string> passes, List<string> failures)
    {
        Camera camera = CreateCamera(preview);
        Vector2 iconPoint = camera.WorldToScreenPoint(new Vector3(-2f, 2f, 3f));
        Vector3 trayPosition = new Vector3(1f, -0.5f, 3f);
        Quaternion trayRotation = Quaternion.Euler(0f, 0f, 13f);
        Vector3 trayScale = new Vector3(1.3f, 0.9f, 0.3f);
        ProfileDragCard card = CreateCard(preview, "Paper tray arrival validation", camera, trayPosition, trayRotation, trayScale);

        Animate(card, iconPoint, 10f);
        Check(State(card) == "Arriving", "HUD animation enters the Arriving state.", passes, failures);
        Vector3 iconScreen = camera.WorldToScreenPoint(card.transform.position);
        float trayDepth = camera.WorldToScreenPoint(trayPosition).z;
        float initialScreenScale = ScreenScaleRatio(card, camera, trayScale, trayDepth);
        Check(Vector2.Distance(new Vector2(iconScreen.x, iconScreen.y), iconPoint) < 1f &&
              initialScreenScale > 0.04f && initialScreenScale < 0.3f &&
              Vector3.Dot(card.transform.forward, camera.transform.forward) > 0.99f &&
              !card.GetComponent<Collider>().enabled,
            "The paper starts at the HUD icon, faces the camera, is small, and has input collider disabled.", passes, failures);

        Set(card, "arrivalElapsed", 0f);
        Invoke(card, "Update");
        Vector3 delayedScreen = camera.WorldToScreenPoint(card.transform.position);
        Check(State(card) == "Arriving" &&
              Vector2.Distance(new Vector2(delayedScreen.x, delayedScreen.y), iconPoint) < 1f,
            "A stagger delay holds the paper at its HUD icon.", passes, failures);

        PointerEventData click = new PointerEventData(null) { button = PointerEventData.InputButton.Left, pointerId = 23, position = iconPoint };
        card.OnPointerDown(click);
        Check(State(card) == "Arriving", "Pointer input cannot drag a card while it is arriving.", passes, failures);

        float delay = Get<float>(card, "arrivalDelay");
        float duration = Get<float>(card, "arrivalDuration");
        Set(card, "arrivalElapsed", delay + duration * 0.5f);
        Invoke(card, "Update");
        float middleScreenScale = ScreenScaleRatio(card, camera, trayScale, trayDepth);
        Check(State(card) == "Arriving" && Vector3.Distance(card.transform.position, trayPosition) > 0.01f &&
              middleScreenScale > initialScreenScale + 0.1f && middleScreenScale < 1f,
            "Mid-flight motion moves toward the tray and grows smoothly.", passes, failures);

        Set(card, "arrivalElapsed", delay + duration + 1f);
        Invoke(card, "Update");
        Check(State(card) != "Arriving" && Vector3.Distance(card.transform.position, trayPosition) < 0.001f &&
              Quaternion.Angle(card.transform.rotation, trayRotation) < 0.01f &&
              Vector3.Distance(card.transform.localScale, trayScale) < 0.001f &&
              card.GetComponent<Collider>().enabled,
            "Arrival restores the exact tray position, rotation, and scale.", passes, failures);

        card.transform.position += Vector3.right;
        card.transform.rotation = Quaternion.identity;
        card.ReturnToOrigin();
        Vector3 returnTarget = Get<Vector3>(card, "transitionTargetPosition");
        Check(Vector3.Distance(returnTarget, trayPosition) < 0.001f,
            "ReturnToOrigin still targets the cached tray position after arrival.", passes, failures);
        Set(card, "transitionStartedAt", Time.unscaledTime - Get<float>(card, "transitionDuration") - 1f);
        Invoke(card, "Update");
        Check(Vector3.Distance(card.transform.position, trayPosition) < 0.001f &&
              Quaternion.Angle(card.transform.rotation, trayRotation) < 0.01f,
            "ReturnToOrigin completes at the cached tray pose.", passes, failures);

        bool enabledColliderRestored = CheckCancellation(preview, camera, iconPoint, true);
        bool disabledColliderRestored = CheckCancellation(preview, camera, iconPoint, false);
        Check(enabledColliderRestored && disabledColliderRestored,
            "Disabling mid-flight restores pose, scale, and both enabled and disabled collider states.", passes, failures);

        if (Camera.main == null)
        {
            ProfileDragCard fallback = CreateCard(preview, "No-camera paper tray fallback", null,
                new Vector3(0f, -1f, 3f), Quaternion.identity, Vector3.one);
            Set(fallback, "mainCamera", null);
            Vector3 fallbackPosition = fallback.transform.position;
            Animate(fallback, iconPoint, 0.1f);
            Check(State(fallback) != "Arriving" && Vector3.Distance(fallback.transform.position, fallbackPosition) < 0.001f,
                "Without a camera the paper appears immediately at its tray pose.", passes, failures);
        }
    }

    private static Camera CreateCamera(Scene scene)
    {
        var go = new GameObject("Paper tray validation camera", typeof(Camera));
        SceneManager.MoveGameObjectToScene(go, scene);
        go.transform.position = new Vector3(0f, 0f, -10f);
        Camera camera = go.GetComponent<Camera>();
        camera.orthographic = false;
        camera.fieldOfView = 60f;
        camera.aspect = 16f / 9f;
        return camera;
    }

    private static bool CheckCancellation(Scene preview, Camera camera, Vector2 iconPoint, bool colliderEnabled)
    {
        Vector3 position = new Vector3(colliderEnabled ? -1f : -2f, -0.5f, 3f);
        Quaternion rotation = Quaternion.Euler(0f, 0f, colliderEnabled ? -8f : 6f);
        Vector3 scale = new Vector3(0.8f, 1.1f, 0.25f);
        ProfileDragCard card = CreateCard(preview, "Cancelled tray arrival", camera, position, rotation, scale, colliderEnabled);
        Animate(card, iconPoint, 0.2f);
        Set(card, "arrivalElapsed", 0.2f + Get<float>(card, "arrivalDuration") * 0.4f);
        Invoke(card, "Update");
        card.gameObject.SetActive(false);
        Invoke(card, "OnDisable");
        return Vector3.Distance(card.transform.position, position) < 0.001f &&
               Quaternion.Angle(card.transform.rotation, rotation) < 0.01f &&
               Vector3.Distance(card.transform.localScale, scale) < 0.001f &&
               card.GetComponent<Collider>().enabled == colliderEnabled;
    }

    private static float ScreenScaleRatio(ProfileDragCard card, Camera camera, Vector3 trayScale, float trayDepth)
    {
        float depth = camera.WorldToScreenPoint(card.transform.position).z;
        return (card.transform.localScale.magnitude / depth) / (trayScale.magnitude / trayDepth);
    }

    private static ProfileDragCard CreateCard(Scene scene, string name, Camera camera, Vector3 position, Quaternion rotation, Vector3 scale, bool colliderEnabled = true)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        SceneManager.MoveGameObjectToScene(go, scene);
        go.transform.SetPositionAndRotation(position, rotation);
        go.transform.localScale = scale;
        go.GetComponent<Collider>().enabled = colliderEnabled;
        ProfileDragCard card = go.AddComponent<ProfileDragCard>();
        Set(card, "cardRenderer", go.GetComponent<Renderer>());
        card.Initialize(new TaxpayerProfile("paper_tray_validation", "Validation", "Preview only.",
            TaxpayerClassification.ResidentCitizen, "validation_npc"), null, camera);
        return card;
    }

    private static void Animate(ProfileDragCard card, Vector2 iconPoint, float delay)
    {
        MethodInfo method = typeof(ProfileDragCard).GetMethod("AnimateFromHud", InstanceFlags, null,
            new[] { typeof(Vector2), typeof(float) }, null);
        if (method == null) throw new MissingMethodException("ProfileDragCard.AnimateFromHud(Vector2, float) was not found.");
        method.Invoke(card, new object[] { iconPoint, delay });
    }

    private static string State(ProfileDragCard card) => Get<object>(card, "state").ToString();
    private static FieldInfo Field(object target, string name) => target.GetType().GetField(name, InstanceFlags) ??
        throw new MissingFieldException(target.GetType().Name, name);
    private static T Get<T>(object target, string name) => (T)Field(target, name).GetValue(target);
    private static void Set(object target, string name, object value) => Field(target, name).SetValue(target, value);
    private static object Invoke(object target, string name) => target.GetType().GetMethod(name, InstanceFlags).Invoke(target, null);
    private static void Check(bool ok, string message, List<string> passes, List<string> failures)
    { if (ok) passes.Add("PASS: " + message); else failures.Add("FAIL: " + message); }
}
