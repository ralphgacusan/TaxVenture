using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// PURPOSE:
/// Scene transitions for Office_Level1 (and level scenes that opt into "fade",
/// see OfficeLevelProfile): the level fades in from black when
/// it starts, and FadeOutThenLoad fades it to black before leaving (Level
/// Complete's Continue). The next scene is not touched.
/// </summary>
public sealed class SceneFade : MonoBehaviour
{
    private const float FadeInSeconds = 0.9f;
    private const float FadeOutSeconds = 0.6f;

    private static SceneFade instance;
    private CanvasGroup black;
    private bool leaving;

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
        if (!scene.IsValid() || !scene.isLoaded || !OfficeLevelProfile.Allows(scene, OfficeFeature.Fade)) return;
        foreach (GameObject root in scene.GetRootGameObjects())
            if (root.GetComponentInChildren<SceneFade>(true) != null) return;
        var host = new GameObject("Scene Fade", typeof(RectTransform));
        SceneManager.MoveGameObjectToScene(host, scene);
        host.AddComponent<SceneFade>();
    }

    /// <summary>Fades the current scene to black, then loads the scene (once).</summary>
    public static void FadeOutThenLoad(string sceneName)
    {
        if (instance == null)
        {
            SceneManager.LoadScene(sceneName);
            return;
        }
        if (instance.leaving) return;
        instance.leaving = true;
        instance.StopAllCoroutines();
        instance.StartCoroutine(instance.FadeAndLoad(sceneName));
    }

    private void Awake()
    {
        instance = this;
        gameObject.layer = LayerMask.NameToLayer("UI");
        Canvas canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 32500;   // above everything, including title cards
        gameObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        gameObject.AddComponent<GraphicRaycaster>();
        gameObject.AddComponent<HudKeepVisibleDuringRewards>();

        RectTransform fill = HudTheme.CreateRect("Black", transform);
        HudTheme.Stretch(fill);
        HudTheme.AddImage(fill, null, Color.black).raycastTarget = true;
        black = gameObject.AddComponent<CanvasGroup>();
        black.alpha = 1f;
        black.blocksRaycasts = true;
        StartCoroutine(FadeIn());
    }

    private void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    private IEnumerator FadeIn()
    {
        // Let the first frames render (and the HUD build) under the black.
        yield return null;
        yield return null;
        float wait = Time.unscaledTime + 0.15f;
        while (Time.unscaledTime < wait) yield return null;
        yield return Fade(1f, 0f, FadeInSeconds);
    }

    private IEnumerator FadeAndLoad(string sceneName)
    {
        yield return Fade(black.alpha, 1f, FadeOutSeconds);
        SceneManager.LoadScene(sceneName);
    }

    private IEnumerator Fade(float from, float to, float seconds)
    {
        black.blocksRaycasts = true;
        float t = 0f;
        while (t < seconds)
        {
            t += Time.unscaledDeltaTime;
            black.alpha = Mathf.Lerp(from, to, Mathf.SmoothStep(0f, 1f, t / seconds));
            yield return null;
        }
        black.alpha = to;
        black.blocksRaycasts = to > 0.01f;
    }
}
