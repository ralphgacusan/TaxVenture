using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class PaperDropValidation
{
    private const string SessionKey = "TaxVenture.PaperDropValidation";
    private const string ReportPath = "Library/PaperDropValidationReport.txt";
    private static bool running;

    static PaperDropValidation() => EditorApplication.delayCall += RunIfChanged;

    [MenuItem("Tools/TaxVenture/Run Paper Drop Checks")]
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
        foreach (string path in new[] { "Assets/Scripts/SideQuest1/TaxpayerProfileWorldPickup.cs",
                     "Assets/Scripts/SideQuest1/TaxpayerNpcInteractable.cs",
                     "Assets/Editor/PaperDropValidation.cs" })
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
            File.WriteAllText(ReportPath, "Paper drop checks\n" + string.Join("\n", passes) +
                (failures.Count == 0 ? "\nPASS: All checks passed." : "\n" + string.Join("\n", failures)));
        }
        if (failures.Count == 0) Debug.Log("Paper drop checks passed. See " + ReportPath);
        else Debug.LogError("Paper drop checks failed. See " + ReportPath);
    }

    private static void RunPreview(Scene preview, List<string> passes, List<string> failures)
    {
        TaxpayerProfile profile = TaxpayerProfileDatabase.GetByNpcId("npc_marie");
        var paperObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
        paperObject.name = "Paper drop validation pickup";
        SceneManager.MoveGameObjectToScene(paperObject, preview);
        paperObject.transform.SetPositionAndRotation(new Vector3(4f, 2f, 1f), Quaternion.Euler(0f, 0f, 12f));
        paperObject.SetActive(false);
        TaxpayerProfileWorldPickup paper = paperObject.AddComponent<TaxpayerProfileWorldPickup>();

        var npcObject = new GameObject("Paper drop validation NPC");
        SceneManager.MoveGameObjectToScene(npcObject, preview);
        npcObject.transform.position = new Vector3(1f, 5f, 1f);
        npcObject.SetActive(false);
        TaxpayerNpcInteractable npc = npcObject.AddComponent<TaxpayerNpcInteractable>();
        Set(npc, "npcId", "npc_marie");
        Set(npc, "profilePickup", paper);
        Invoke(npc, "DropTaxpayerProfile");

        Vector3 source = npcObject.transform.position + Vector3.up;
        Vector3 target = Get<Vector3>(paper, "basePosition");
        Quaternion targetRotation = Get<Quaternion>(paper, "baseRotation");
        Check(paperObject.activeSelf && paper.IsDropping && Near(paper.transform.position, source),
            "NPC pickup flow starts the active paper at the NPC source after caching its target.", passes, failures);
        paper.OnPointerClick(null);
        Check(paperObject.activeSelf && paper.IsDropping && paper.Profile == profile,
            "Clicking during the drop leaves the paper active and uncollected.", passes, failures);

        float duration = Get<float>(paper, "dropDuration");
        Set(paper, "dropElapsed", Mathf.Max(0f, duration * 0.5f - Time.deltaTime));
        Invoke(paper, "Update");
        Vector3 middle = paper.transform.position;
        Check(middle.y < source.y - 0.2f && middle.y > target.y + 0.2f &&
              Vector3.Distance(middle, source) > 0.25f && Vector3.Distance(middle, target) > 0.25f,
            "Mid-flight pose descends between source and cached floor target without teleporting.", passes, failures);

        Set(paper, "dropElapsed", duration);
        Vector3 landed = target + Vector3.up * Mathf.Sin(Time.time * Get<float>(paper, "bobSpeed")) * Get<float>(paper, "bobHeight");
        Invoke(paper, "Update");
        Check(!paper.IsDropping && Near(paper.transform.position, landed),
            "Landing reaches the cached target with the existing bob offset.", passes, failures);
        Check(Quaternion.Angle(paper.transform.rotation, targetRotation) < 0.01f,
            "Landing restores the cached paper rotation.", passes, failures);

        paper.DropFrom(source);
        Set(paper, "dropElapsed", duration * 0.5f);
        Invoke(paper, "Update");
        paperObject.SetActive(false);
        Invoke(paper, "OnDisable");
        Check(!paper.IsDropping && Near(paper.transform.position, target) &&
              Quaternion.Angle(paper.transform.rotation, targetRotation) < 0.01f,
            "Disabling mid-drop restores the cached pose and clears the drop.", passes, failures);
        paperObject.SetActive(true);
        paper.Initialize(profile);
        Check(!paper.IsDropping && Near(paper.transform.position, target),
            "Reinitializing the same pickup does not accumulate position drift.", passes, failures);

        TaxpayerProfileWorldPickup reused = CreatePickup(preview, "Initialize-only reused HUD paper", new Vector3(-3f, 1f, 0f));
        Vector3 reusedTarget = reused.transform.position;
        reused.Initialize(profile);
        Invoke(reused, "Update");
        reused.Initialize(profile);
        Invoke(reused, "Update");
        Vector3 bobbed = reusedTarget + Vector3.up * Mathf.Sin(Time.time * Get<float>(reused, "bobSpeed")) * Get<float>(reused, "bobHeight");
        Check(!reused.IsDropping && Near(reused.transform.position, bobbed),
            "Initialize-only reused HUD paper keeps its idle pose without starting a drop.", passes, failures);
    }

    private static TaxpayerProfileWorldPickup CreatePickup(Scene scene, string name, Vector3 position)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        SceneManager.MoveGameObjectToScene(go, scene);
        go.transform.position = position;
        return go.AddComponent<TaxpayerProfileWorldPickup>();
    }

    private static bool Near(Vector3 a, Vector3 b) => Vector3.Distance(a, b) < 0.001f;
    private static void Check(bool ok, string message, List<string> passes, List<string> failures)
    { if (ok) passes.Add("PASS: " + message); else failures.Add("FAIL: " + message); }
    private static FieldInfo Field(object target, string name) => target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
    private static T Get<T>(object target, string name) => (T)Field(target, name).GetValue(target);
    private static void Set(object target, string name, object value) => Field(target, name).SetValue(target, value);
    private static object Invoke(object target, string name) => target.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public).Invoke(target, null);
}
