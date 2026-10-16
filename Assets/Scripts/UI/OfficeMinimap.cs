using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering.Universal;

/// <summary>Creates the Office_Level1 minimap camera and owns its runtime resources.</summary>
[DefaultExecutionOrder(100)]
[RequireComponent(typeof(Camera))]
public sealed class OfficeMinimap : MonoBehaviour
{
    private const string TargetSceneName = "Office_Level1";
    private const float CameraHeight = 25f;

    private Camera mapCamera;
    private RenderTexture mapTexture;
    private CameraController sceneCameraController;
    private OfficeMinimapHUD hud;
    private float nextResolveTime;
    private OfficeMinimapMarkers markers;
    private SettingsUI settings;
    private CaseResultPopupUI results;
    private GameStateMachine gameState;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void InitializeForLoadedScenes()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;

        for (int i = 0; i < SceneManager.sceneCount; i++)
            EnsureForScene(SceneManager.GetSceneAt(i));
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode) => EnsureForScene(scene);

    private static void EnsureForScene(Scene scene)
    {
        if (!scene.IsValid() || !scene.isLoaded || scene.name != TargetSceneName) return;

        GameObject[] roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
            if (roots[i].GetComponentInChildren<OfficeMinimap>(true) != null)
                return;

        var mapObject = new GameObject("Office Minimap Runtime");
        SceneManager.MoveGameObjectToScene(mapObject, scene);
        mapObject.AddComponent<Camera>();
        mapObject.AddComponent<OfficeMinimap>();
    }

    private void Awake()
    {
        mapCamera = GetComponent<Camera>();
        ConfigureCamera();
        CreateMapTexture();
        mapCamera.enabled = false;
    }

    private void LateUpdate()
    {
        if ((sceneCameraController == null || hud == null) && Time.unscaledTime >= nextResolveTime)
        {
            nextResolveTime = Time.unscaledTime + 0.5f;
            ResolveSceneDependencies();
        }

        Transform playerRoot = sceneCameraController != null ? sceneCameraController.PlayerRoot : null;
        // Stay up while the Office HUD paper windows (Notes, profiles) hold the player.
        bool visible = playerRoot != null &&
                       (sceneCameraController.CanStartDialogue ||
                        sceneCameraController.CurrentMode == CameraController.CameraMode.ProfileViewer) &&
                       !DialogueUI.IsAnyDialogueActive && Time.timeScale > 0f &&
                       (settings == null || !settings.IsVisible) &&
                       (results == null || !results.IsVisible) &&
                       !HudRewardScreen.AnyOpen &&
                       (gameState == null || !gameState.IsPaused) &&
                       hud != null && hud.IsAvailable;

        if (!visible)
        {
            SetVisible(false);
            return;
        }

        transform.SetPositionAndRotation(
            new Vector3(playerRoot.position.x, CameraHeight, playerRoot.position.z),
            Quaternion.Euler(90f, 0f, 0f));

        if (!mapTexture.IsCreated()) mapTexture.Create();
        if (markers == null || !markers.IsValid)
        {
            markers?.Dispose();
            markers = new OfficeMinimapMarkers(hud.MapRect, gameObject.scene);
        }
        markers.Tick(mapCamera, sceneCameraController);
        SetVisible(true);
    }

    private void OnDisable() => SetVisible(false);

    private void OnDestroy()
    {
        SetVisible(false);

        if (mapCamera != null) mapCamera.targetTexture = null;
        if (mapTexture != null)
        {
            mapTexture.Release();
            Destroy(mapTexture);
            mapTexture = null;
        }

        markers?.Dispose();
        markers = null;

        if (hud != null)
        {
            hud.Dispose();
            hud = null;
        }
    }

    private void ConfigureCamera()
    {
        mapCamera.enabled = false;
        mapCamera.tag = "Untagged";
        mapCamera.depth = -20f;
        mapCamera.orthographic = true;
        mapCamera.orthographicSize = 14f;
        mapCamera.aspect = 1f;
        mapCamera.nearClipPlane = 22.2f;
        mapCamera.farClipPlane = 26f;
        mapCamera.clearFlags = CameraClearFlags.SolidColor;
        mapCamera.backgroundColor = new Color32(54, 69, 79, 255);
        mapCamera.cullingMask = LayerMask.GetMask("Default", "Interactable", "3DInteractable");
        mapCamera.useOcclusionCulling = false;
        mapCamera.allowHDR = false;
        mapCamera.allowMSAA = false;

        UniversalAdditionalCameraData cameraData = mapCamera.GetUniversalAdditionalCameraData();
        cameraData.renderType = CameraRenderType.Base;
        cameraData.renderShadows = false;
        cameraData.requiresDepthOption = CameraOverrideOption.Off;
        cameraData.requiresColorOption = CameraOverrideOption.Off;
        cameraData.renderPostProcessing = false;
        cameraData.volumeLayerMask = 0;
        cameraData.antialiasing = AntialiasingMode.None;
    }

    private void CreateMapTexture()
    {
        mapTexture = new RenderTexture(256, 256, 16, RenderTextureFormat.ARGB32)
        {
            name = "Office Minimap",
            antiAliasing = 1,
            useMipMap = false,
            autoGenerateMips = false,
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp,
            anisoLevel = 0
        };
        mapCamera.targetTexture = mapTexture;
    }

    private void ResolveSceneDependencies()
    {
        Scene scene = gameObject.scene;
        if (!scene.IsValid() || scene.name != TargetSceneName) return;

        if (sceneCameraController == null || sceneCameraController.gameObject.scene != scene)
        {
            sceneCameraController = null;
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length && sceneCameraController == null; i++)
                sceneCameraController = roots[i].GetComponentInChildren<CameraController>(true);
        }

        GameObject[] sceneRoots = scene.GetRootGameObjects();
        for (int i = 0; i < sceneRoots.Length; i++)
        {
            if (settings == null) settings = sceneRoots[i].GetComponentInChildren<SettingsUI>(true);
            if (results == null) results = sceneRoots[i].GetComponentInChildren<CaseResultPopupUI>(true);
            if (gameState == null) gameState = sceneRoots[i].GetComponentInChildren<GameStateMachine>(true);
        }

        if (hud == null)
        {
            markers?.Dispose();
            markers = null;
            hud = OfficeMinimapHUD.TryCreate(scene);
            if (hud != null) hud.SetMapTexture(mapTexture);
        }
    }

    private void SetVisible(bool visible)
    {
        if (mapCamera != null) mapCamera.enabled = visible;
        if (hud != null) hud.SetVisible(visible);
    }
}
