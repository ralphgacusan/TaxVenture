using System;
using System.IO;
using System.Linq;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Tools > TaxVenture > Settings > Capture Settings Board: renders the 3D
/// Settings board in front of the 3D Menu main camera, in a disposable preview scene (no scene or PlayerPrefs are
/// written) and checks that touches land on the switches and BACK.
/// Output: Library/SettingsBoardPreview. Library/SettingsBoardSetup.request
/// runs it from outside the editor on the next focused editor update.
/// </summary>
[InitializeOnLoad]
public static class SettingsBoardSetup
{
    private const string ScenePath = "Assets/Scenes/3D Menu.unity";
    private const string RequestPath = "Library/SettingsBoardSetup.request";
    private const string Output = "Library/SettingsBoardPreview";
    private static bool running;

    static SettingsBoardSetup() { EditorApplication.update += CheckRequest; }

    private static void CheckRequest()
    {
        if (running || EditorApplication.isCompiling || EditorApplication.isUpdating ||
            EditorApplication.isPlayingOrWillChangePlaymode || !File.Exists(RequestPath)) return;
        File.Delete(RequestPath);
        try { Capture(); }
        catch (Exception error)
        {
            Directory.CreateDirectory(Output);
            File.WriteAllText(Output + "/error.txt", error.ToString());
            Debug.LogException(error);
        }
    }

    [MenuItem("Tools/TaxVenture/Settings/Capture Settings Board")]
    public static void Capture()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Exit play mode before capturing the settings preview.");
        running = true;
        Directory.CreateDirectory(Output);
        foreach (string old in Directory.GetFiles(Output, "*.png")) File.Delete(old);
        Scene preview = EditorSceneManager.OpenPreviewScene(ScenePath);
        var log = new StringBuilder();
        void Check(bool ok, string message)
        {
            if (!ok) throw new InvalidOperationException("Settings board check failed: " + message);
            log.AppendLine("PASS: " + message);
        }

        try
        {
            // Settings live in the 3D Menu: render from the main menu camera, where the board drops in.
            ToggleCamera cameras = All<ToggleCamera>(preview).Single();
            Camera camera = cameras.mainCam;
            camera.gameObject.SetActive(true);
            camera.overrideSceneCullingMask = EditorSceneManager.GetSceneCullingMask(preview);
            SettingsBoard3D board = All<SettingsBoard3D>(preview).Single();

            board.PreparePreview(music: true, sfx: false);
            Render(camera, "settings-1280x720", 1280, 720);
            Render(camera, "settings-2960x1140", 2960, 1140);
            Render(camera, "settings-1024x768", 1024, 768);
            board.PreparePreview(music: false, sfx: true);
            Render(camera, "settings-flipped-1280x720", 1280, 720);

            Check(board.MusicToggle != null && board.SfxToggle != null && board.BackButton != null, "music, SFX and BACK are built");
            Check(!board.MusicToggle.IsOn && board.SfxToggle.IsOn, "switches show the given values");
            Physics.SyncTransforms();
            PhysicsScene physics = preview.GetPhysicsScene();
            foreach (CareerBoardTarget target in new CareerBoardTarget[] { board.MusicToggle, board.SfxToggle, board.BackButton })
            {
                Vector3 screen = camera.WorldToScreenPoint(target.transform.position + target.transform.forward * -0.01f);
                Check(screen.z > 0f && screen.x > 0f && screen.y > 0f && screen.x < camera.pixelWidth && screen.y < camera.pixelHeight,
                    target.name + " is on screen");
                Ray ray = camera.ScreenPointToRay(screen);
                Check(physics.Raycast(ray.origin, ray.direction, out RaycastHit hit, 100f) &&
                      hit.collider.GetComponentInParent<CareerBoardTarget>() == target, target.name + " is the first touch hit");
            }
            foreach (TextMeshPro text in board.GetComponentsInChildren<TextMeshPro>(true))
                Check(text.font != null, text.name + " has a font");

            File.WriteAllText(Output + "/interaction-checks.txt", log.ToString());
            if (File.Exists(Output + "/error.txt")) File.Delete(Output + "/error.txt");
            File.WriteAllText(Output + "/complete.txt", DateTime.UtcNow.ToString("O"));
        }
        finally
        {
            EditorSceneManager.ClosePreviewScene(preview);
            running = false;
        }
    }

    private static T[] All<T>(Scene scene) where T : Component => scene.GetRootGameObjects()
        .SelectMany(root => root.GetComponentsInChildren<T>(true)).ToArray();

    private static void Render(Camera camera, string name, int width, int height)
    {
        RenderTexture previous = RenderTexture.active;
        RenderTexture previousTarget = camera.targetTexture;
        var target = new RenderTexture(width, height, 24);
        var texture = new Texture2D(width, height, TextureFormat.RGB24, false);
        try
        {
            camera.targetTexture = target;
            camera.aspect = (float)width / height;
            foreach (SettingsBoard3D board in All<SettingsBoard3D>(camera.gameObject.scene))
                board.PreparePreview(board.MusicToggle == null || board.MusicToggle.IsOn, board.SfxToggle == null || board.SfxToggle.IsOn);
            Canvas.ForceUpdateCanvases();
            foreach (TMP_Text text in All<TMP_Text>(camera.gameObject.scene))
                if (text.gameObject.activeInHierarchy) text.ForceMeshUpdate();
            camera.Render();
            RenderTexture.active = target;
            texture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            texture.Apply();
            File.WriteAllBytes(Output + "/" + name + ".png", texture.EncodeToPNG());
        }
        finally
        {
            camera.targetTexture = previousTarget;
            camera.ResetAspect();
            RenderTexture.active = previous;
            UnityEngine.Object.DestroyImmediate(texture);
            target.Release();
            UnityEngine.Object.DestroyImmediate(target);
        }
    }
}
