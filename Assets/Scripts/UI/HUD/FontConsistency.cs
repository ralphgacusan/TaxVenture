using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// PURPOSE:
/// One font across the game. Older UI in the scenes and prefabs still
/// points at "Minnie SDF" (which also shows boxes for some characters, see
/// HUD K11); at runtime every such text switches to the HUD font
/// (HudTheme.Font, LiberationSans, the TMP default). Scenes and prefabs are
/// not rewritten.
///
/// The corkboard side quest keeps its text big and easy to read: text on the
/// board, its cards and slots is enlarged and made bold when it switches.
///
/// Runs in every scene: once on load, then every second for a while (for
/// panels and prefabs created later), then every few seconds.
/// </summary>
public sealed class FontConsistency : MonoBehaviour
{
    private const string OldFontName = "Minnie";
    private const float CorkboardScale = 1.25f;

    private static FontConsistency runner;
    private float nextSweep;
    private float fastUntil;
    private readonly HashSet<TMP_Text> boosted = new HashSet<TMP_Text>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Initialize()
    {
        if (runner != null) return;
        var host = new GameObject("Font Consistency");
        DontDestroyOnLoad(host);
        host.hideFlags = HideFlags.HideInHierarchy;
        runner = host.AddComponent<FontConsistency>();
        SceneManager.sceneLoaded += (scene, mode) => runner.Restart();
        runner.Restart();
    }

    private void Restart()
    {
        nextSweep = 0f;
        fastUntil = Time.unscaledTime + 20f;
    }

    private void Update()
    {
        if (Time.unscaledTime < nextSweep) return;
        nextSweep = Time.unscaledTime + (Time.unscaledTime < fastUntil ? 1f : 4f);
        Sweep();
    }

    private void Sweep()
    {
        TMP_FontAsset font = HudTheme.Font;
        if (font == null) return;
        TMP_Text[] texts = FindObjectsByType<TMP_Text>(FindObjectsInactive.Include);
        for (int i = 0; i < texts.Length; i++)
        {
            TMP_Text text = texts[i];
            if (text == null || text.font == null || text.font == font) continue;
            if (text.font.name.IndexOf(OldFontName, System.StringComparison.OrdinalIgnoreCase) < 0) continue;
            text.font = font;
            if (IsCorkboard(text) && boosted.Add(text)) Enlarge(text);
        }
    }

    /// <summary>Text on the side-quest corkboard, its profile cards or its corner slots.</summary>
    private static bool IsCorkboard(TMP_Text text) =>
        text.GetComponentInParent<CorkboardSideQuestManager>(true) != null ||
        text.GetComponentInParent<ProfileDragCard>(true) != null ||
        text.GetComponentInParent<CorkboardSlot>(true) != null;

    private static void Enlarge(TMP_Text text)
    {
        text.fontStyle |= FontStyles.Bold;
        text.fontSize *= CorkboardScale;
        if (text.enableAutoSizing)
        {
            text.fontSizeMax *= CorkboardScale;
            text.fontSizeMin *= CorkboardScale;
        }
    }
}
