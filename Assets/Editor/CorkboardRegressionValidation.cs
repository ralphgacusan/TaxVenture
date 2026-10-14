using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Lightweight edit-mode regression checks for the corkboard minigame.
/// Runs once for each change to the three corkboard runtime scripts and uses a
/// preview scene so it never edits the user's open scene.
/// </summary>
[InitializeOnLoad]
public static class CorkboardRegressionValidation
{
    private const int ValidationVersion = 1;
    private const string SessionKey = "TaxVenture.CorkboardRegressionValidation";
    private const string ReportPath = "Library/CorkboardValidationReport.txt";

    private static readonly string[] SourceFiles =
    {
        "Assets/Scripts/SideQuest1/CorkboardSlot.cs",
        "Assets/Scripts/SideQuest1/CorkboardSideQuestManager.cs",
        "Assets/Scripts/SideQuest1/ProfileDragCard.cs"
    };

    private static bool running;

    static CorkboardRegressionValidation()
    {
        EditorApplication.delayCall += RunIfNeeded;
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
    }

    [MenuItem("Tools/TaxVenture/Run Corkboard Regression Checks")]
    private static void RunManually()
    {
        RunChecks(ComputeSourceVersion(), true);
    }

    private static void OnPlayModeChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredEditMode)
        {
            EditorApplication.delayCall += RunIfNeeded;
        }
    }

    private static void RunIfNeeded()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || running)
        {
            return;
        }

        string sourceVersion = ComputeSourceVersion();
        if (SessionState.GetString(SessionKey, string.Empty) == sourceVersion)
        {
            return;
        }

        RunChecks(sourceVersion, false);
    }

    private static string ComputeSourceVersion()
    {
        var source = new StringBuilder(ValidationVersion.ToString());
        string projectRoot = Directory.GetCurrentDirectory();
        foreach (string relativePath in SourceFiles)
        {
            string path = Path.Combine(projectRoot, relativePath);
            source.Append('\n').Append(relativePath).Append('\n');
            if (File.Exists(path))
            {
                source.Append(File.ReadAllText(path));
            }
        }

        return Hash128.Compute(source.ToString()).ToString();
    }

    private static void RunChecks(string sourceVersion, bool force)
    {
        if (running || EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
        {
            return;
        }

        if (!force && SessionState.GetString(SessionKey, string.Empty) == sourceVersion)
        {
            return;
        }

        running = true;
        var assertions = new List<string>();
        var failures = new List<string>();
        Scene preview = default;
        try
        {
            preview = EditorSceneManager.NewPreviewScene();
            RunGeometryChecks(preview, assertions, failures);
            RunSlotStateChecks(preview, assertions, failures);
        }
        catch (Exception exception)
        {
            failures.Add("Harness exception: " + exception);
        }
        finally
        {
            if (preview.IsValid())
            {
                foreach (GameObject root in preview.GetRootGameObjects())
                {
                    foreach (CorkboardSideQuestManager fixtureManager in root.GetComponentsInChildren<CorkboardSideQuestManager>(true))
                    {
                        fixtureManager.CancelInvoke();
                        SetField(fixtureManager, "sideQuestActive", false);
                        SetField(fixtureManager, "sideQuestCompleted", false);
                    }
                }

                EditorSceneManager.ClosePreviewScene(preview);
            }

            running = false;
            SessionState.SetString(SessionKey, sourceVersion);
            WriteReport(sourceVersion, assertions, failures);
        }
    }

    private static void RunGeometryChecks(Scene preview, List<string> assertions, List<string> failures)
    {
        BuildFixture(preview, out GameObject board, out CorkboardSideQuestManager manager,
            out List<CorkboardSlot> slots, out Camera camera);

        Assert(InvokeBool(manager, "BuildBoardGeometry"), "Four configured slot colliders produce board geometry.", assertions, failures);
        SetField(manager, "sideQuestActive", true);

        ProfileDragCard sampleCard = CreateCard(preview, "Geometry sample card", manager, camera,
            new TaxpayerProfile("sample", "Sample", "", TaxpayerClassification.ResidentCitizen, "sample"),
            new Vector3(0f, -3f, -1f));

        CheckDrop(manager, camera, sampleCard, new Vector3(-1f, 1f, -1f), TaxpayerClassification.ResidentCitizen,
            "upper-left quadrant", assertions, failures);
        CheckDrop(manager, camera, sampleCard, new Vector3(1f, 1f, -1f), TaxpayerClassification.NonResidentCitizen,
            "upper-right quadrant", assertions, failures);
        CheckDrop(manager, camera, sampleCard, new Vector3(-1f, -1f, -1f), TaxpayerClassification.ResidentAlien,
            "lower-left quadrant", assertions, failures);
        CheckDrop(manager, camera, sampleCard, new Vector3(1f, -1f, -1f), TaxpayerClassification.NonResidentAlien,
            "lower-right quadrant", assertions, failures);

        // Lower-left papers retain their release location, including near an inner divider.
        float pinnedDepth = GetField<float>(manager, "boardPlaneZ") - 0.008f;
        foreach (Vector2 point in new[] { new Vector2(-1.3f, -0.7f), new Vector2(-0.6f, -1.4f), new Vector2(-0.05f, -0.8f) })
        {
            Vector3 target = board.transform.TransformPoint(new Vector3(point.x, point.y, pinnedDepth));
            Ray release = camera.ScreenPointToRay(camera.WorldToScreenPoint(target));
            bool accepted = manager.TryGetDrop(release, sampleCard.transform, out CorkboardSlot area,
                out Vector3 pinned, out _);
            Assert(accepted && area.ExpectedClassification == TaxpayerClassification.ResidentAlien &&
                   Vector3.Distance(pinned, target) < 0.001f,
                "Resident Alien placement keeps the release position " + point + ".", assertions, failures);
        }

        camera.orthographic = false;
        Vector3 perspectiveTarget = board.transform.TransformPoint(new Vector3(-0.9f, -0.9f, pinnedDepth));
        Ray perspectiveRay = camera.ScreenPointToRay(camera.WorldToScreenPoint(perspectiveTarget));
        Assert(manager.TryGetDrop(perspectiveRay, sampleCard.transform, out _, out Vector3 perspectivePinned, out _) &&
               Vector2.Distance(camera.WorldToScreenPoint(perspectivePinned), camera.WorldToScreenPoint(perspectiveTarget)) < 0.1f,
            "Perspective pinning stays at the release screen position without a depth jump.", assertions, failures);
        camera.orthographic = true;

        CheckDrop(manager, camera, sampleCard, new Vector3(0f, 1f, -1f), TaxpayerClassification.NonResidentCitizen,
            "vertical seam deterministically selects the right area", assertions, failures);

        Vector3 edgePoint = new Vector3(1.95f, 1f, -1f);
        Ray edgeRay = camera.ScreenPointToRay(camera.WorldToScreenPoint(edgePoint));
        bool edgeAccepted = manager.TryGetDrop(edgeRay, sampleCard.transform, out CorkboardSlot edgeSlot,
            out Vector3 edgePosition, out _);
        Assert(edgeAccepted && edgeSlot != null, "A pointer near the board edge is accepted and assigned to an area.", assertions, failures);
        if (edgeAccepted)
        {
            Bounds boardBounds = GetField<Bounds>(manager, "boardBounds");
            Vector3 localPosition = board.transform.InverseTransformPoint(edgePosition);
            GetCardHalfExtents(sampleCard.transform, board.transform, out float halfWidth, out float halfHeight);
            Assert(localPosition.x - halfWidth >= boardBounds.min.x - 0.001f &&
                   localPosition.x + halfWidth <= boardBounds.max.x + 0.001f &&
                   localPosition.y - halfHeight >= boardBounds.min.y - 0.001f &&
                   localPosition.y + halfHeight <= boardBounds.max.y + 0.001f &&
                   localPosition.x < edgePoint.x,
                "The card is clamped inward and remains fully inside the board near its outer edge.", assertions, failures);
        }

        Vector3 outsidePoint = new Vector3(2.1f, 1f, -1f);
        Ray outsideRay = camera.ScreenPointToRay(camera.WorldToScreenPoint(outsidePoint));
        Assert(!manager.TryGetDrop(outsideRay, sampleCard.transform, out _, out _, out _),
            "A release outside the board is rejected.", assertions, failures);
    }

    private static void RunSlotStateChecks(Scene preview, List<string> assertions, List<string> failures)
    {
        BuildFixture(preview, out _, out CorkboardSideQuestManager manager,
            out List<CorkboardSlot> slots, out Camera camera);
        Assert(InvokeBool(manager, "BuildBoardGeometry"), "Card fixture board geometry is ready.", assertions, failures);

        CorkboardSlot residentCitizenSlot = FindSlot(slots, TaxpayerClassification.ResidentCitizen);
        TaxpayerProfile resident = Profile(TaxpayerClassification.ResidentCitizen);
        TaxpayerProfile alien = Profile(TaxpayerClassification.ResidentAlien);
        ProfileDragCard first = CreateCard(preview, "First placement", manager, camera, resident, new Vector3(0f, -3f, -1f));
        ProfileDragCard replacement = CreateCard(preview, "Wrong replacement", manager, camera, alien, new Vector3(0.6f, -3f, -1f));

        Assert(residentCitizenSlot.TryPlaceCard(first), "A matching profile solves its classification area.", assertions, failures);
        Assert(residentCitizenSlot.IsSolved && residentCitizenSlot.OccupyingCard == first,
            "The area records the correctly placed card.", assertions, failures);
        Assert(!residentCitizenSlot.TryPlaceCard(replacement), "A mismatched replacement is reported as incorrect.", assertions, failures);
        Assert(!residentCitizenSlot.IsSolved && residentCitizenSlot.OccupyingCard == replacement,
            "Replacing a card updates the occupant and clears the solved state.", assertions, failures);
        residentCitizenSlot.VacateSlot(replacement);
        Assert(!residentCitizenSlot.IsSolved && residentCitizenSlot.OccupyingCard == null,
            "Picking a placed card up vacates the area.", assertions, failures);
        Assert(residentCitizenSlot.TryPlaceCard(first) && residentCitizenSlot.IsSolved,
            "A previously displaced correct profile can be placed again.", assertions, failures);
        residentCitizenSlot.VacateSlot(first);
        residentCitizenSlot.ResetRuntimeState();
        Assert(residentCitizenSlot.PlacementAttempts == 0 && residentCitizenSlot.OccupyingCard == null,
            "Reset clears attempt and occupancy state.", assertions, failures);
    }

    private static void BuildFixture(Scene preview, out GameObject board, out CorkboardSideQuestManager manager,
        out List<CorkboardSlot> slots, out Camera camera)
    {
        board = new GameObject("Validation Corkboard");
        SceneManager.MoveGameObjectToScene(board, preview);

        slots = new List<CorkboardSlot>();
        AddSlot(preview, board.transform, slots, TaxpayerClassification.ResidentCitizen, new Vector2(-1f, 1f));
        AddSlot(preview, board.transform, slots, TaxpayerClassification.NonResidentCitizen, new Vector2(1f, 1f));
        AddSlot(preview, board.transform, slots, TaxpayerClassification.ResidentAlien, new Vector2(-1f, -1f));
        AddSlot(preview, board.transform, slots, TaxpayerClassification.NonResidentAlien, new Vector2(1f, -1f));

        GameObject managerObject = new GameObject("Validation Corkboard Manager");
        SceneManager.MoveGameObjectToScene(managerObject, preview);
        manager = managerObject.AddComponent<CorkboardSideQuestManager>();
        SetField(manager, "sideQuestRoot", board);
        SetField(manager, "slots", slots);
        GameObject cameraObject = new GameObject("Validation Camera");
        SceneManager.MoveGameObjectToScene(cameraObject, preview);
        camera = cameraObject.AddComponent<Camera>();
        camera.transform.position = new Vector3(0f, 0f, -6f);
        camera.orthographic = true;
        camera.orthographicSize = 4.5f;
        camera.aspect = 16f / 9f;
        camera.pixelRect = new Rect(0f, 0f, 1280f, 720f);
    }

    private static void AddSlot(Scene preview, Transform board, List<CorkboardSlot> slots,
        TaxpayerClassification classification, Vector2 center)
    {
        var slotObject = new GameObject("Validation " + classification);
        SceneManager.MoveGameObjectToScene(slotObject, preview);
        slotObject.transform.SetParent(board, false);
        slotObject.transform.localPosition = new Vector3(center.x, center.y, 0f);

        BoxCollider collider = slotObject.AddComponent<BoxCollider>();
        collider.size = new Vector3(2f, 2f, 0.1f);

        var snapObject = new GameObject("Snap Point");
        snapObject.transform.SetParent(slotObject.transform, false);
        snapObject.transform.localPosition = new Vector3(0f, 0f, -0.12f);

        CorkboardSlot slot = slotObject.AddComponent<CorkboardSlot>();
        SetField(slot, "expectedClassification", classification);
        SetField(slot, "snapPoint", snapObject.transform);
        SetField(slot, "dropArea", collider);
        slots.Add(slot);
    }

    private static ProfileDragCard CreateCard(Scene preview, string name, CorkboardSideQuestManager manager,
        Camera camera, TaxpayerProfile profile, Vector3 position)
    {
        GameObject cardObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cardObject.name = name;
        SceneManager.MoveGameObjectToScene(cardObject, preview);
        cardObject.transform.position = position;
        cardObject.transform.localScale = new Vector3(0.4f, 0.6f, 0.04f);
        ProfileDragCard card = cardObject.AddComponent<ProfileDragCard>();
        SetField(card, "cardRenderer", cardObject.GetComponent<Renderer>());
        card.Initialize(profile, manager, camera);
        return card;
    }

    private static void CheckDrop(CorkboardSideQuestManager manager, Camera camera, ProfileDragCard card,
        Vector3 target, TaxpayerClassification expected, string description,
        List<string> assertions, List<string> failures)
    {
        Ray ray = camera.ScreenPointToRay(camera.WorldToScreenPoint(target));
        bool accepted = manager.TryGetDrop(ray, card.transform, out CorkboardSlot slot, out Vector3 position, out _);
        Assert(accepted && slot != null && slot.ExpectedClassification == expected,
            "Drop at " + description + " chooses " + expected + ".", assertions, failures);
        if (accepted)
        {
            Transform board = GetField<GameObject>(manager, "sideQuestRoot").transform;
            Bounds bounds = slot.TryGetBoardLocalBounds(board, out Bounds result) ? result : default;
            Vector3 local = board.InverseTransformPoint(position);
            GetCardHalfExtents(card.transform, board, out float halfWidth, out float halfHeight);
            bool insideCell = local.x - halfWidth >= bounds.min.x - 0.001f && local.x + halfWidth <= bounds.max.x + 0.001f &&
                              local.y - halfHeight >= bounds.min.y - 0.001f && local.y + halfHeight <= bounds.max.y + 0.001f;
            Assert(insideCell, "The card bounds stay inside the selected " + description + " area.", assertions, failures);
        }
    }

    private static TaxpayerProfile Profile(TaxpayerClassification classification)
    {
        return new TaxpayerProfile(classification.ToString(), classification.ToString(), "Validation profile", classification, "test");
    }

    private static CorkboardSlot FindSlot(List<CorkboardSlot> slots, TaxpayerClassification classification)
    {
        foreach (CorkboardSlot slot in slots)
        {
            if (slot.ExpectedClassification == classification) return slot;
        }
        throw new InvalidOperationException("Missing test slot for " + classification);
    }

    private static void GetCardHalfExtents(Transform card, Transform board, out float halfWidth, out float halfHeight)
    {
        Bounds bounds = default;
        bool hasBounds = false;
        foreach (Renderer renderer in card.GetComponentsInChildren<Renderer>())
        {
            Vector3 center = renderer.bounds.center;
            Vector3 extents = renderer.bounds.extents;
            for (int x = -1; x <= 1; x += 2)
            for (int y = -1; y <= 1; y += 2)
            for (int z = -1; z <= 1; z += 2)
            {
                Vector3 point = board.InverseTransformPoint(center + Vector3.Scale(extents, new Vector3(x, y, z)));
                if (hasBounds) bounds.Encapsulate(point);
                else { bounds = new Bounds(point, Vector3.zero); hasBounds = true; }
            }
        }

        halfWidth = hasBounds ? bounds.extents.x : 0f;
        halfHeight = hasBounds ? bounds.extents.y : 0f;
    }

    private static bool InvokeBool(object target, string methodName)
    {
        return (bool)Invoke(target, methodName);
    }

    private static object Invoke(object target, string methodName)
    {
        MethodInfo method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        if (method == null) throw new MissingMethodException(target.GetType().Name, methodName);
        return method.Invoke(target, null);
    }

    private static void SetField(object target, string fieldName, object value)
    {
        FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        if (field == null) throw new MissingFieldException(target.GetType().Name, fieldName);
        field.SetValue(target, value);
    }

    private static T GetField<T>(object target, string fieldName)
    {
        FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        if (field == null) throw new MissingFieldException(target.GetType().Name, fieldName);
        return (T)field.GetValue(target);
    }

    private static void Assert(bool condition, string message, List<string> assertions, List<string> failures)
    {
        if (condition) assertions.Add("PASS: " + message);
        else failures.Add("FAIL: " + message);
    }

    private static void WriteReport(string sourceVersion, List<string> assertions, List<string> failures)
    {
        string report = "Corkboard regression checks - " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + Environment.NewLine +
                        "Source version: " + sourceVersion + Environment.NewLine +
                        "Assertions: " + assertions.Count + " passed, " + failures.Count + " failed" + Environment.NewLine + Environment.NewLine +
                        string.Join(Environment.NewLine, assertions) + Environment.NewLine + Environment.NewLine +
                        string.Join(Environment.NewLine, failures);

        string absolutePath = Path.Combine(Directory.GetCurrentDirectory(), ReportPath);
        try
        {
            File.WriteAllText(absolutePath, report);
        }
        catch (Exception exception)
        {
            Debug.LogWarning("Could not write corkboard validation report: " + exception.Message);
        }

        if (failures.Count == 0)
        {
            Debug.Log("Corkboard regression checks passed: " + assertions.Count + " assertions. Report: " + ReportPath);
        }
        else
        {
            Debug.LogError("Corkboard regression checks failed: " + failures.Count + " of " + assertions.Count + " assertions. Report: " + ReportPath + "\n" + string.Join("\n", failures));
        }
    }
}
