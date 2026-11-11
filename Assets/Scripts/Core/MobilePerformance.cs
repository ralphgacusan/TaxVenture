using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

/// <summary>
/// The Android frame budget. Runs only on phones, so the Editor and Play Mode are untouched.
/// Without this Android caps the game at 30 fps. It asks for 60, and when a scene cannot hold
/// that on the current phone it steps down: slightly lower 3D resolution first, then a steady
/// 30 fps at the normal resolution. Overlay UI always draws at full resolution.
/// Each scene remembers its step for the session.
/// </summary>
public sealed class MobilePerformance : MonoBehaviour
{
    /// <summary>Tallest 3D image worth drawing: a 1080p phone keeps its 0.8 scale, sharper screens stop here.</summary>
    private const float MaxRenderHeight = 864f;
    private const float ReducedScale = 0.875f;

    private static readonly Dictionary<string, int> sceneLevels = new Dictionary<string, int>();
    private UniversalRenderPipelineAsset pipeline;
    private float baseScale = 1f;
    private FrameBudget budget = new FrameBudget(0);
    private string sceneName;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Boot()
    {
        if (!Application.isMobilePlatform) return;
#if !DEVELOPMENT_BUILD
        // Hundreds of log lines each build a stack trace and write to logcat in a release APK.
        Debug.unityLogger.filterLogType = LogType.Error;
#endif
        QualitySettings.asyncUploadTimeSlice = 4;
        QualitySettings.asyncUploadBufferSize = 32;
        var host = new GameObject("Mobile Performance") { hideFlags = HideFlags.HideInHierarchy };
        DontDestroyOnLoad(host);
        host.AddComponent<MobilePerformance>();
    }

    private void Awake()
    {
        pipeline = UniversalRenderPipeline.asset;
        if (pipeline != null)
        {
            float shortSide = Mathf.Max(1, Mathf.Min(Screen.width, Screen.height));
            baseScale = Mathf.Min(pipeline.renderScale, MaxRenderHeight / shortSide);
        }
        SceneManager.sceneLoaded += HandleSceneLoaded;
        BeginScene(SceneManager.GetActiveScene().name);
    }

    private void OnDestroy() => SceneManager.sceneLoaded -= HandleSceneLoaded;

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (mode != LoadSceneMode.Single) return;
        Application.backgroundLoadingPriority = ThreadPriority.Normal;
        BeginScene(scene.name);
    }

    private void BeginScene(string name)
    {
        sceneName = name;
        sceneLevels.TryGetValue(name, out int level);
        budget = new FrameBudget(level);
        Apply();
    }

    private void Update()
    {
        if (!budget.Sample(Time.unscaledDeltaTime)) return;
        sceneLevels[sceneName] = budget.Level;
        Apply();
    }

    private void Apply()
    {
        Application.targetFrameRate = budget.TargetFrameRate;
        if (pipeline != null) pipeline.renderScale = baseScale * (budget.ReducesResolution ? ReducedScale : 1f);
    }

    /// <summary>Loads a scene without freezing the main thread (a long freeze can trigger Android's "not responding").</summary>
    public static void LoadScene(string name)
    {
        Application.backgroundLoadingPriority = ThreadPriority.High;
        if (SceneManager.LoadSceneAsync(name) == null)
            Application.backgroundLoadingPriority = ThreadPriority.Normal;
    }
}

/// <summary>Decides the frame-rate step from measured frame times. Plain C# so it can be tested without Unity.</summary>
public sealed class FrameBudget
{
    public const int LowestLevel = 2;
    private const float WindowSeconds = 4f;
    private const float SettleSeconds = 3f;
    private const float HitchSeconds = 0.25f;
    private const float FullRateFloor = 50f;
    private const float ReducedRateFloor = 42f;

    private float settle = SettleSeconds;
    private float elapsed;
    private int frames;

    public FrameBudget(int level) => Level = level < 0 ? 0 : level > LowestLevel ? LowestLevel : level;

    public int Level { get; private set; }
    public int TargetFrameRate => Level >= LowestLevel ? 30 : 60;
    public bool ReducesResolution => Level == 1;

    /// <summary>Feeds one frame's time; true when the level changed. Load and resume hitches do not count.</summary>
    public bool Sample(float deltaSeconds)
    {
        if (Level >= LowestLevel) return false;
        if (settle > 0f)
        {
            settle -= deltaSeconds;
            return false;
        }
        if (deltaSeconds > HitchSeconds)
        {
            elapsed = 0f;
            frames = 0;
            return false;
        }
        elapsed += deltaSeconds;
        frames++;
        if (elapsed < WindowSeconds) return false;
        float rate = frames / elapsed;
        elapsed = 0f;
        frames = 0;
        if (rate >= (Level == 0 ? FullRateFloor : ReducedRateFloor)) return false;
        Level++;
        settle = SettleSeconds;
        return true;
    }
}
