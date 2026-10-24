using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// PURPOSE:
/// Level 1's desk has two folder models under StackedCaseFolder: the real
/// closed CaseFolder and DeskFolder, the drop target where the open folder is
/// put back on the desk. DeskFolder's model was always visible, so with the
/// real folder in the HUD icon (or open in the hands) the desk still showed a
/// folder, which read as a duplicate case folder.
///
/// Now the DeskFolder model is shown only while the real closed folder is on
/// the desk. Its collider (the drop target) is never touched, so dropping the
/// open folder back onto the desk works exactly as before.
///
/// Added at runtime to Office_Level1 (no scene change). Presentation only.
/// </summary>
public sealed class DeskFolderStackSync : MonoBehaviour
{
    private const string StackPath = "Environment/OfficeBuilding/Rooms/ConsultantOffice/Desk/StackedCaseFolder";

    private GameObject caseFolder;
    private GameObject deskFolderVisual;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Install()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != OfficeLevelCatalog.Level1Scene) return;
        Transform stack = Find(scene, StackPath);
        if (stack == null)
        {
            Debug.LogWarning($"[DeskFolderStackSync] {StackPath} not found in {scene.name}; the desk drop folder keeps its old visibility.");
            return;
        }
        if (stack.GetComponent<DeskFolderStackSync>() != null) return;
        Transform closed = stack.Find("CaseFolder");
        Transform visual = stack.Find("DeskFolder/Visual");
        if (closed == null || visual == null)
        {
            Debug.LogWarning("[DeskFolderStackSync] CaseFolder or DeskFolder/Visual is missing under StackedCaseFolder.");
            return;
        }
        var sync = stack.gameObject.AddComponent<DeskFolderStackSync>();
        sync.caseFolder = closed.gameObject;
        sync.deskFolderVisual = visual.gameObject;
        sync.Apply();
    }

    private static Transform Find(Scene scene, string path)
    {
        int slash = path.IndexOf('/');
        string rootName = slash < 0 ? path : path.Substring(0, slash);
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root.name != rootName) continue;
            if (slash < 0) return root.transform;
            Transform found = root.transform.Find(path.Substring(slash + 1));
            if (found != null) return found;
        }
        return null;
    }

    private void LateUpdate() => Apply();

    /// <summary>The drop folder model is visible exactly while the real closed folder is on the desk.</summary>
    private void Apply()
    {
        if (caseFolder == null || deskFolderVisual == null) return;
        bool onDesk = caseFolder.activeInHierarchy;
        if (deskFolderVisual.activeSelf != onDesk) deskFolderVisual.SetActive(onDesk);
    }
}
