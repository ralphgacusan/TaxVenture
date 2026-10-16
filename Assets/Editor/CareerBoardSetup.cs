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
/// Tools > TaxVenture > 3D Menu > Capture Career Board: renders the 3D career
/// corkboards from the level-select camera in a disposable preview scene with
/// sample progress (no scene or save is written) and checks that touch
/// raycasts land on the right papers and buttons.
///
/// Output: Library/CareerBoardPreview (*.png, interaction-checks.txt, error.txt
/// on failure). Creating Library/CareerBoardSetup.request runs it from outside
/// the editor (picked up on the next editor update).
/// </summary>
[InitializeOnLoad]
public static class CareerBoardSetup
{
    private const string ScenePath = "Assets/Scenes/3D Menu.unity";
    private const string RequestPath = "Library/CareerBoardSetup.request";
    private const string Output = "Library/CareerBoardPreview";
    private static bool running;

    static CareerBoardSetup() { EditorApplication.update += CheckRequest; }

    private static void CheckRequest()
    {
        if (running || EditorApplication.isCompiling || EditorApplication.isUpdating ||
            EditorApplication.isPlayingOrWillChangePlaymode || !File.Exists(RequestPath)) return;
        File.Delete(RequestPath);
        try { CaptureInstalledBoard(); }
        catch (Exception error)
        {
            Directory.CreateDirectory(Output);
            File.WriteAllText(Output + "/error.txt", error.ToString());
            Debug.LogException(error);
        }
    }

    [MenuItem("Tools/TaxVenture/3D Menu/Capture Career Board")]
    public static void CaptureInstalledBoard()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Exit play mode before capturing the menu preview.");
        running = true;
        Directory.CreateDirectory(Output);
        foreach (string old in Directory.GetFiles(Output, "*.png")) File.Delete(old);
        Scene preview = EditorSceneManager.OpenPreviewScene(ScenePath);
        try
        {
            ToggleCamera cameras = All<ToggleCamera>(preview).Single();
            Camera camera = cameras.levelSelectorCam;
            camera.transform.SetPositionAndRotation(cameras.levelSelectorEndPoint.position, cameras.levelSelectorEndPoint.rotation);
            camera.gameObject.SetActive(true);
            camera.overrideSceneCullingMask = EditorSceneManager.GetSceneCullingMask(preview);
            CareerBoardPresenter presenter = All<CareerBoardPresenter>(preview).Single();

            presenter.BuildBoard();
            var log = new StringBuilder();

            presenter.RefreshPreview(new PlayerProgressData());
            Capture(camera, "fresh-1280x720", 1280, 720);
            ValidateFresh(presenter, camera, preview, log);

            PlayerProgressData career = SampleCareer(passLevel1: true, badgeTier: 3);
            presenter.RefreshPreview(career);
            Capture(camera, "career-1280x720", 1280, 720);
            Capture(camera, "career-1920x1080", 1920, 1080);
            Capture(camera, "career-1024x768", 1024, 768);
            Capture(camera, "career-2400x1080", 2400, 1080);
            ValidateCareer(presenter, log);

            presenter.RefreshPreview(SampleCareer(passLevel1: false, badgeTier: 1));
            Capture(camera, "failed-run-1280x720", 1280, 720);

            presenter.RefreshPreview(career, 1);
            Capture(camera, "sheet-replay-1280x720", 1280, 720);
            presenter.RefreshPreview(career, 3);
            Capture(camera, "sheet-coming-soon-1280x720", 1280, 720);

            File.WriteAllText(Output + "/interaction-checks.txt", log.ToString());
            if (File.Exists(Output + "/error.txt")) File.Delete(Output + "/error.txt");
            File.WriteAllText(Output + "/complete.txt", "Preview captures completed using disposable sample progress.\n" + DateTime.UtcNow.ToString("O"));
        }
        finally
        {
            EditorSceneManager.ClosePreviewScene(preview);
            running = false;
        }
    }

    private static PlayerProgressData SampleCareer(bool passLevel1, int badgeTier)
    {
        var sample = new PlayerProgressData();
        if (passLevel1) sample.RecordLevelResult("Level_01", 150, 12, 150, 12, true, null);
        else sample.RecordLevelResult("Level_01", 95, 7, 150, 12, false, null);
        sample.RecordLevelTime("Level_01", 754f, 212f);
        sample.RecordBadge("know_your_taxpayer_1", badgeTier);
        return sample;
    }

    private static void ValidateFresh(CareerBoardPresenter presenter, Camera camera, Scene scene, StringBuilder log)
    {
        void Check(bool ok, string message)
        {
            if (!ok) throw new InvalidOperationException("Career board check failed: " + message);
            log.AppendLine("PASS: " + message);
        }

        var papers = presenter.ChapterPapers;
        Check(papers.Count == 5, "five chapter papers are pinned");
        Check(!presenter.Sheet.IsOpen, "the chapter sheet starts closed");

        PhysicsScene physics = scene.GetPhysicsScene();
        Physics.SyncTransforms();
        foreach (CareerPinnedPaper paper in papers)
        {
            Vector3 screen = camera.WorldToScreenPoint(paper.Body.position);
            Check(screen.z > 0f && screen.x > 0f && screen.y > 0f && screen.x < camera.pixelWidth && screen.y < camera.pixelHeight,
                paper.name + " is on screen");
            Ray ray = camera.ScreenPointToRay(screen);
            bool hit = physics.Raycast(ray.origin, ray.direction, out RaycastHit info, 200f);
            Check(hit && info.collider.GetComponentInParent<CareerBoardTarget>() == paper, paper.name + " is the first touch hit at its centre");
        }

        var chapters = presenter.Snapshot.Chapters;
        Check(chapters[0].Status == CareerChapterStatus.Available && chapters[0].Launchable, "fresh Chapter 1 is playable");
        Check(chapters[1].Status == CareerChapterStatus.Locked && !chapters[1].Launchable, "fresh Chapter 2 is locked");
        Check(chapters[2].Status == CareerChapterStatus.ComingSoon && !chapters[2].Launchable, "Chapter 3 is coming soon");
        Check(presenter.StringCount == 0, "no red string while only Chapter 1 is open");
        Check(presenter.BadgeSlotCount == 5 && presenter.BadgePaper(0) != null && presenter.BadgePaper(1) == null,
            "five badge spaces: the side quest paper, then empty spaces");
        CareerPinnedPaper badgePaper = presenter.BadgePaper(0);
        Vector3 badgeScreen = camera.WorldToScreenPoint(badgePaper.Body.position + badgePaper.Body.up * -30f * badgePaper.Body.lossyScale.y);
        Ray badgeRay = camera.ScreenPointToRay(badgeScreen);
        Check(physics.Raycast(badgeRay.origin, badgeRay.direction, out RaycastHit badgeHit, 200f) &&
              badgeHit.collider.GetComponentInParent<CareerBoardTarget>() == badgePaper, "the badge paper is tappable");

        presenter.RefreshPreview(new PlayerProgressData(), 2);
        Check(presenter.Sheet.IsOpen && presenter.Sheet.Chapter != null && presenter.Sheet.Chapter.Def.id == "Level_02",
            "tapping Chapter 2 opens its sheet");
        Button("Start Button", presenter, start => Check(!start.Interactable, "a locked chapter's START cannot be pressed"));
        presenter.RefreshPreview(new PlayerProgressData(), 1);
        Button("Start Button", presenter, start => Check(start.Interactable, "Chapter 1 START can be pressed"));
        presenter.RefreshPreview(new PlayerProgressData(), 3);
        Button("Start Button", presenter, start => Check(!start.Interactable, "a coming-soon chapter can never start"));
        presenter.RefreshPreview(new PlayerProgressData());
        Check(!presenter.Sheet.IsOpen, "the sheet closes when the board is refreshed");
    }

    private static void ValidateCareer(CareerBoardPresenter presenter, StringBuilder log)
    {
        void Check(bool ok, string message)
        {
            if (!ok) throw new InvalidOperationException("Career board check failed: " + message);
            log.AppendLine("PASS: " + message);
        }

        CareerSnapshot snapshot = presenter.Snapshot;
        Check(snapshot.Passed == 1 && snapshot.Chapters[1].Status == CareerChapterStatus.Available,
            "passing Chapter 1 unlocks Chapter 2");
        Check(snapshot.Badges.Count == 1 && snapshot.Badges[0].Tier == 3, "the gold Know Your Taxpayer badge is shown");
        Check(snapshot.Goal == snapshot.Chapters[1], "the next goal moves to Chapter 2");
        Check(presenter.StringCount == 1, "a red string links Chapter 1 to the unlocked Chapter 2");
        Check(snapshot.Chapters[0].BestSeconds == 754, "the chapter best time is shown from the save");
        foreach (TextMeshPro text in presenter.GetComponentsInChildren<TextMeshPro>(true))
            Check(text.font != null, text.name + " has a font");
    }

    private static void Button(string name, CareerBoardPresenter presenter, Action<CareerBoardButton> check)
    {
        CareerBoardButton button = presenter.Sheet.GetComponentsInChildren<CareerBoardButton>(true).First(b => b.name == name);
        check(button);
    }

    private static T[] All<T>(Scene scene) where T : Component => scene.GetRootGameObjects()
        .SelectMany(root => root.GetComponentsInChildren<T>(true)).ToArray();

    private static void Capture(Camera camera, string name, int width, int height)
    {
        Directory.CreateDirectory(Output);
        RenderTexture previous = RenderTexture.active;
        RenderTexture previousTarget = camera.targetTexture;
        var target = new RenderTexture(width, height, 24);
        var texture = new Texture2D(width, height, TextureFormat.RGB24, false);
        try
        {
            camera.targetTexture = target;
            camera.aspect = (float)width / height;
            // The presenter scales the boards for narrow screens from the camera aspect.
            foreach (CareerBoardPresenter presenter in All<CareerBoardPresenter>(camera.gameObject.scene))
                typeof(CareerBoardPresenter).GetMethod("ApplyAspect", System.Reflection.BindingFlags.NonPublic |
                    System.Reflection.BindingFlags.Instance)?.Invoke(presenter, null);
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
