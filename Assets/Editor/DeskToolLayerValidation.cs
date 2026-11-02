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
    private static Doc3DLayer previewPaper, previewCalculator;
    public static void RunBatch() => Run(true);

    [MenuItem("Tools/TaxVenture/Run Desk Tool Layer Check")]
    public static void RunInEditor() => Run(false);

    private static void Run(bool batch)
    {
        Scene original = SceneManager.GetActiveScene();
        Scene test = default;
        Doc3DLayer cardLayer = null, calcLayer = null;
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
            var card = Level3PracticeCardModel.Build(host, props);
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
            if (!batch)
            {
                cardLayer.AssignSortingOrder(1202);
                calcLayer.AssignSortingOrder(1200);
                typeof(Doc3DLayer).GetMethod("SetComposited", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(cardLayer, new object[] { true });
                typeof(Doc3DLayer).GetMethod("SetComposited", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(calcLayer, new object[] { true });
                camera.targetTexture = null;
                camera.depth = 999f;
                preview = test;
                previewPaper = cardLayer;
                previewCalculator = calcLayer;
                test = default;
                cardLayer = calcLayer = null;
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
            if (cardLayer != null) UnityEngine.Object.DestroyImmediate(cardLayer.gameObject);
            if (calcLayer != null) UnityEngine.Object.DestroyImmediate(calcLayer.gameObject);
            if (test.IsValid()) EditorSceneManager.CloseScene(test, true);
            if (original.IsValid()) SceneManager.SetActiveScene(original);
        }
    }

    [MenuItem("Tools/TaxVenture/Close Desk Tool Layer Preview")]
    public static void ClosePreview()
    {
        if (previewPaper != null) UnityEngine.Object.DestroyImmediate(previewPaper.gameObject);
        if (previewCalculator != null) UnityEngine.Object.DestroyImmediate(previewCalculator.gameObject);
        if (preview.IsValid()) EditorSceneManager.CloseScene(preview, true);
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
