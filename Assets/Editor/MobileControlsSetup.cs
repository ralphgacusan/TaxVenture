using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Applies T01 wiring to the user's open scene with Undo, without external scene edits.</summary>
public static class MobileControlsSetup
{
    [MenuItem("Tools/TaxVenture/Configure Mobile Controls")]
    private static void Configure()
    {
        if (Application.isPlaying)
        {
            Debug.LogError("Stop Play mode before configuring mobile controls.");
            return;
        }
        Scene scene = SceneManager.GetActiveScene();
        CameraController owner = null;
        foreach (CameraController candidate in Object.FindObjectsByType<CameraController>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (candidate.gameObject.scene != scene) continue;
            if (owner != null) { Debug.LogError("Multiple CameraControllers in the active scene; configure one owner first."); return; }
            owner = candidate;
        }
        if (owner == null) { Debug.LogError("Open Office_Level1 before configuring controls (CameraController missing)."); return; }
        var ownerData = new SerializedObject(owner);
        var move = ownerData.FindProperty("virtualJoystick").objectReferenceValue as VirtualJoystick;
        var camera = ownerData.FindProperty("mainCamera").objectReferenceValue as Camera;
        if (move == null || camera == null || move.transform.parent == null ||
            move.GetComponent<Image>() == null || move.GetComponent<RectTransform>() == null)
        {
            Debug.LogError("CameraController must reference the existing movement joystick and Main Camera. No changes applied.");
            return;
        }
        var moveData = new SerializedObject(move);
        if (moveData.FindProperty("handle").objectReferenceValue == null)
        {
            Debug.LogError("Movement joystick handle reference is missing. No changes applied.");
            return;
        }
        var look = ownerData.FindProperty("lookJoystick").objectReferenceValue as VirtualJoystick;
        Transform existing = move.transform.parent.Find("LookJoystick");
        if (look == null && existing != null) look = existing.GetComponent<VirtualJoystick>();
        if (camera.GetComponent<FirstPersonCameraController>() == null && camera.GetComponent<ThirdPersonCameraFollow>() == null)
        {
            Debug.LogError("Main Camera has no supported look controller. No changes applied.");
            return;
        }
        if (look != null && (look == move || look.transform.parent != move.transform.parent))
        {
            Debug.LogError("Legacy look joystick must be a separate sibling. No changes applied.");
            return;
        }

        Undo.IncrementCurrentGroup();
        int group = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Configure TaxVenture mobile controls");
        GameObject obsoleteLook = look != null ? look.gameObject : existing != null ? existing.gameObject : null;
        if (obsoleteLook != null)
        {
            Undo.RecordObject(obsoleteLook, "Disable obsolete look joystick");
            obsoleteLook.SetActive(false);
        }
        Style(move);
        Wire(owner, "lookJoystick", null);
        var firstPerson = camera.GetComponent<FirstPersonCameraController>();
        if (firstPerson != null) ConfigureSwipe(firstPerson);
        var thirdPerson = camera.GetComponent<ThirdPersonCameraFollow>();
        if (thirdPerson != null) ConfigureSwipe(thirdPerson);
        Transform legacyArea = move.transform.parent.Find("MobileLookArea");
        if (legacyArea != null)
        {
            Undo.RecordObject(legacyArea.gameObject, "Disable unused swipe area");
            legacyArea.gameObject.SetActive(false);
        }
        if (move.transform.parent.GetComponent<MobileControlsSafeArea>() == null)
            Undo.AddComponent<MobileControlsSafeArea>(move.transform.parent.gameObject);
        Undo.CollapseUndoOperations(group);
        EditorSceneManager.MarkSceneDirty(scene);
        Selection.activeGameObject = move.gameObject;
        Debug.Log("Mobile controls configured: larger MOVE joystick shifted inward/up; camera swipe uses the right 75% (excluding UI). LOOK joystick disabled. Save the scene (Ctrl+S), then test in Play mode.");
    }

    private static void Wire(Object target, string field, Object value)
    {
        var data = new SerializedObject(target);
        data.FindProperty(field).objectReferenceValue = value;
        data.ApplyModifiedProperties();
    }

    private static void ConfigureSwipe(Object target)
    {
        var data = new SerializedObject(target);
        data.FindProperty("touchLookScreenWidth").floatValue = 0.75f;
        data.FindProperty("disableTouchLook").boolValue = false;
        data.ApplyModifiedProperties();
    }

    private static void Style(VirtualJoystick stick)
    {
        RectTransform rect = stick.GetComponent<RectTransform>();
        Undo.RecordObject(rect, "Position joystick");
        rect.anchorMin = rect.anchorMax = Vector2.zero;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(280f, 280f);
        rect.anchoredPosition = new Vector2(260f, 240f);
        rect.localScale = Vector3.one;
        Image image = stick.GetComponent<Image>();
        Undo.RecordObject(image, "Style joystick base");
        image.color = new Color(0.08f, 0.12f, 0.15f, 0.6f);
        image.raycastTarget = true;
        Outline outline = image.GetComponent<Outline>();
        if (outline == null) outline = Undo.AddComponent<Outline>(image.gameObject);
        Undo.RecordObject(outline, "Style joystick rim");
        outline.effectColor = new Color(0.85f, 0.92f, 0.94f, 0.55f);
        outline.effectDistance = new Vector2(2f, -2f);

        var data = new SerializedObject(stick);
        data.FindProperty("background").objectReferenceValue = rect;
        data.FindProperty("handleRange").floatValue = 80f;
        data.FindProperty("deadZone").floatValue = 0.12f;
        data.FindProperty("sensitivity").floatValue = 1f;
        RectTransform handle = data.FindProperty("handle").objectReferenceValue as RectTransform;
        data.ApplyModifiedProperties();
        Undo.RecordObject(handle, "Style joystick thumb");
        handle.anchorMin = handle.anchorMax = handle.pivot = new Vector2(0.5f, 0.5f);
        handle.anchoredPosition = Vector2.zero;
        handle.sizeDelta = new Vector2(112f, 112f);
        handle.localScale = Vector3.one;
        Image thumb = handle.GetComponent<Image>();
        if (thumb != null)
        {
            Undo.RecordObject(thumb, "Color joystick thumb");
            thumb.color = new Color(0.95f, 0.8f, 0.43f, 0.94f);
            thumb.raycastTarget = false; // The base owns presses; labels/thumb cannot steal pointer routing.
        }
        Transform oldLabel = stick.transform.Find("ControlLabel");
        TextMeshProUGUI label;
        if (oldLabel != null) label = oldLabel.GetComponent<TextMeshProUGUI>();
        else
        {
            var labelObject = new GameObject("ControlLabel", typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(labelObject, "Label joystick");
            Undo.SetTransformParent(labelObject.transform, stick.transform, "Parent joystick label");
            label = Undo.AddComponent<TextMeshProUGUI>(labelObject);
        }
        if (label != null)
        {
            Undo.RecordObject(label, "Style joystick label");
            Undo.RecordObject(label.rectTransform, "Position joystick label");
            label.text = "MOVE";
            label.font = TMP_Settings.defaultFontAsset;
            label.fontSize = 23f;
            label.alignment = TextAlignmentOptions.Center;
            label.color = new Color(0.88f, 0.93f, 0.96f, 0.9f);
            label.raycastTarget = false;
            label.rectTransform.anchorMin = label.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            label.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            label.rectTransform.localScale = Vector3.one;
            label.rectTransform.sizeDelta = new Vector2(220f, 36f);
            label.rectTransform.anchoredPosition = new Vector2(0f, -164f);
        }
    }
}
