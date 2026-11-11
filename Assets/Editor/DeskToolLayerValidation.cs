using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

/// <summary>Batch-only render regression for overlapping desk tool captures. Never saves a scene.</summary>
public static class DeskToolLayerValidation
{
    private static Scene preview;
    private static Doc3DLayer previewPaper, previewCalculator, previewReceipt;
    public static void RunBatch() => Run(true);

    [MenuItem("Tools/TaxVenture/Run Desk Tool Layer Check")]
    public static void RunInEditor() => Run(false);

    private static void Run(bool batch)
    {
        Scene original = SceneManager.GetActiveScene();
        Scene test = default;
        Doc3DLayer cardLayer = null, calcLayer = null, receiptLayer = null;
        try
        {
            test = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            var camera = new GameObject("Layer test camera", typeof(Camera)).GetComponent<Camera>();
            camera.tag = "MainCamera";
            camera.transform.SetPositionAndRotation(new Vector3(0f, 1f, 0f), Quaternion.Euler(90f, 0f, 0f));
            camera.orthographic = true;
            camera.orthographicSize = 0.15f;
            camera.nearClipPlane = 0.01f;
            camera.targetTexture = new RenderTexture(960, 540, 24);
            camera.GetUniversalAdditionalCameraData().renderPostProcessing = false;
            new GameObject("Light", typeof(Light)).GetComponent<Light>().type = LightType.Directional;
            RenderSettings.ambientLight = Color.white;
            var props = new Level3DeskProps(Level2DeskStamps.DragLayer);
            Transform host = new GameObject("Tools").transform;
            var card = Level3PracticeCardModel.Build(host, props, Level3PracticeRules.LessonCard);
            var calculator = Level3CalculatorModel.Build(host, null, props);
            calculator.Root.position = new Vector3(0.04f, 0.02f, 0f);
            foreach (TMPro.TMP_Text text in host.GetComponentsInChildren<TMPro.TMP_Text>()) text.ForceMeshUpdate();
            cardLayer = Doc3DLayer.Create(null, card.Root, "Test paper");
            calcLayer = Doc3DLayer.Create(null, calculator.Root, "Test calculator");
            cardLayer.AlwaysComposite = calcLayer.AlwaysComposite = true;
            Directory.CreateDirectory("Temp/DeskToolLayerValidation");
            Texture2D paper = Capture(cardLayer, camera, "paper");
            Texture2D calc = Capture(calcLayer, camera, "calculator");
            Vector3 sample = camera.WorldToScreenPoint(card.Root.TransformPoint(new Vector3(0.03f, 0.005f, 0.04f)));
            Color pixel = paper.GetPixel((int)sample.x, (int)sample.y);
            Debug.Log($"[DeskToolLayerValidation] Paper surface RGBA: {pixel}; pipeline {GraphicsSettings.currentRenderPipeline.name}");
            if (pixel.a < 0.99f) throw new Exception("Opaque paper capture must hide the calculator when brought to front.");
            Debug.Log("[DeskToolLayerValidation] PASS: opaque paper capture.");
            AssertForegroundLifetime(calcLayer, cardLayer);
            calculator.SetTape(new[] { "1,250.00", "- 480.00", "= 770.00" }, animate: false);
            Transform receipt = calculator.TapeStrip;
            receipt.SetParent(host, true); // the original strip, as in the runtime drag
            receipt.position = card.Root.position + Vector3.up * 0.04f;
            foreach (Collider collider in receipt.GetComponentsInChildren<Collider>()) collider.enabled = false;
            receiptLayer = Doc3DLayer.Create(null, receipt, "Test held receipt");
            receiptLayer.AlwaysComposite = true;
            receiptLayer.RefreshTextBeforeCapture = true;
            AssertForegroundLifetime(receiptLayer, cardLayer, calcLayer);
            Capture(receiptLayer, camera, "held-receipt");
            Debug.Log("[DeskToolLayerValidation] PASS: calculator/receipt foreground reservations survive competing focus and release.");
            if (!batch)
            {
                cardLayer.AssignSortingOrder(1200);
                calcLayer.AssignSortingOrder(1202);
                receiptLayer.AssignSortingOrder(1204);
                typeof(Doc3DLayer).GetMethod("SetComposited", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(cardLayer, new object[] { true });
                typeof(Doc3DLayer).GetMethod("SetComposited", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(calcLayer, new object[] { true });
                typeof(Doc3DLayer).GetMethod("SetComposited", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(receiptLayer, new object[] { true });
                camera.targetTexture = null;
                camera.depth = 999f;
                preview = test;
                previewPaper = cardLayer;
                previewCalculator = calcLayer;
                previewReceipt = receiptLayer;
                test = default;
                cardLayer = calcLayer = receiptLayer = null;
            }
            if (batch) EditorApplication.Exit(0);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            if (batch) EditorApplication.Exit(1);
        }
        finally
        {
            if (receiptLayer != null) UnityEngine.Object.DestroyImmediate(receiptLayer.gameObject);
            if (cardLayer != null) UnityEngine.Object.DestroyImmediate(cardLayer.gameObject);
            if (calcLayer != null) UnityEngine.Object.DestroyImmediate(calcLayer.gameObject);
            if (test.IsValid()) EditorSceneManager.CloseScene(test, true);
            if (original.IsValid()) SceneManager.SetActiveScene(original);
        }
    }

    [MenuItem("Tools/TaxVenture/Close Desk Tool Layer Preview")]
    public static void ClosePreview()
    {
        if (previewReceipt != null) UnityEngine.Object.DestroyImmediate(previewReceipt.gameObject);
        if (previewPaper != null) UnityEngine.Object.DestroyImmediate(previewPaper.gameObject);
        if (previewCalculator != null) UnityEngine.Object.DestroyImmediate(previewCalculator.gameObject);
        if (preview.IsValid()) EditorSceneManager.CloseScene(preview, true);
    }

    // Native-only regression: deliberately reorder the destination/other tool
    // after the press, then run the same reservation enforcement as LateUpdate.
    private static void AssertForegroundLifetime(Doc3DLayer held, params Doc3DLayer[] covered)
    {
        MethodInfo maintain = typeof(Doc3DLayer).GetMethod("UpdateForegroundOrder", BindingFlags.Instance | BindingFlags.NonPublic);
        held.KeepInFront = true;
        foreach (Doc3DLayer other in covered)
        {
            other.BringToFront(false);
            // Simulate the native canvas reset after enable/reparent as well.
            held.GetComponent<Canvas>().overrideSorting = false;
            held.GetComponent<Canvas>().sortingOrder = 0;
            maintain.Invoke(held, null);
            if (!held.GetComponent<Canvas>().overrideSorting || held.SortingOrder <= other.SortingOrder ||
                held.GetComponent<Canvas>().sortingOrder <= other.GetComponent<Canvas>().sortingOrder)
                throw new Exception("Held visual must remain above a document focused after pointer-down.");
        }
        held.KeepInFront = false;
        covered[0].BringToFront(false);
        maintain.Invoke(held, null);
        if (held.SortingOrder >= covered[0].SortingOrder)
            throw new Exception("Released visual must give up its foreground reservation.");
    }

    private static Texture2D Capture(Doc3DLayer layer, Camera camera, string name)
    {
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
        typeof(Doc3DLayer).GetMethod("RenderDocument", flags).Invoke(layer, new object[] { camera });
        var texture = (RenderTexture)typeof(Doc3DLayer).GetField("texture", flags).GetValue(layer);
        RenderTexture.active = texture;
        var result = new Texture2D(texture.width, texture.height, TextureFormat.RGBA32, false);
        result.ReadPixels(new Rect(0, 0, texture.width, texture.height), 0, 0);
        result.Apply();
        RenderTexture.active = null;
        File.WriteAllBytes("Temp/DeskToolLayerValidation/" + name + ".png", result.EncodeToPNG());
        return result;
    }
}
