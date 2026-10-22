using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Merge missing authored notes into an already-open menu after script import.
/// Keeps the user's existing duplicates/placement, including unsaved scene edits.
/// Never saves/reloads the scene or touches player progress.</summary>
public static class MainMenuButtonAuthoring
{
    [InitializeOnLoadMethod]
    private static void QueueOpenSceneBinding() => EditorApplication.delayCall += BindOpenScenes;

    [MenuItem("Tools/TaxVenture/3D Menu/Bind Editable Story Buttons")]
    private static void BindOpenScenes()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene scene = SceneManager.GetSceneAt(i);
            if (!scene.isLoaded || scene.name != MenuEntry.MenuSceneName) continue;
            GameObject settings = Find(scene, "settings_button");
            if (settings == null) continue;
            GameObject newGame = Find(scene, MenuEntry.NewGameButtonName) ?? Find(scene, MenuEntry.LegacyNewGameButtonName);
            GameObject resume = Find(scene, MenuEntry.ContinueButtonName);
            if (newGame == null) newGame = Duplicate(settings, MenuEntry.NewGameButtonName, 24.5f);
            if (resume == null) resume = Duplicate(settings, MenuEntry.ContinueButtonName, 65.7f);
            RemoveInheritedSettingsAction(newGame);
            RemoveInheritedSettingsAction(resume);
            MainMenuCampaignPanel panel = settings.transform.parent.GetComponent<MainMenuCampaignPanel>();
            if (panel == null) panel = Undo.AddComponent<MainMenuCampaignPanel>(settings.transform.parent.gameObject);
            var serialized = new SerializedObject(panel);
            serialized.FindProperty("playNote").objectReferenceValue = Find(scene, "play_button");
            serialized.FindProperty("newGameNote").objectReferenceValue = newGame;
            serialized.FindProperty("continueNote").objectReferenceValue = resume;
            serialized.ApplyModifiedProperties();
            panel.ApplyAuthoredLabels();
            // Edit Mode previews the returning row. Play is an alternative
            // runtime state, not a third note behind New Game and Continue.
            GameObject play = Find(scene, "play_button");
            if (play != null && play.activeSelf)
            {
                Undo.RecordObject(play, "Preview returning menu");
                play.SetActive(false);
                PrefabUtility.RecordPrefabInstancePropertyModifications(play);
                EditorSceneManager.MarkSceneDirty(scene);
            }
        }
    }

    private static GameObject Duplicate(GameObject settings, string name, float x)
    {
        GameObject note = Object.Instantiate(settings, settings.transform.parent);
        Undo.RegisterCreatedObjectUndo(note, "Create editable story button");
        note.name = name;
        note.SetActive(true);
        note.transform.localPosition = new Vector3(x, 35.22227f, 10.25278f);
        note.transform.localScale = settings.transform.localScale * 0.48f;
        foreach (UnityEngine.UI.Button button in note.GetComponentsInChildren<UnityEngine.UI.Button>(true))
            Undo.DestroyObjectImmediate(button); // MainMenu's existing physics-touch dispatcher owns input.
        EditorSceneManager.MarkSceneDirty(note.scene);
        return note;
    }

    private static void RemoveInheritedSettingsAction(GameObject note)
    {
        foreach (UnityEngine.UI.Button button in note.GetComponentsInChildren<UnityEngine.UI.Button>(true))
            for (int i = 0; i < button.onClick.GetPersistentEventCount(); i++)
                if (button.onClick.GetPersistentMethodName(i) == "OnSettingsPressed")
                {
                    Undo.DestroyObjectImmediate(button);
                    break;
                }
    }

    private static GameObject Find(Scene scene, string name)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
            foreach (Transform node in root.GetComponentsInChildren<Transform>(true))
                if (node.name == name) return node.gameObject;
        return null;
    }
}
