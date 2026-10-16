using TMPro;
using UnityEngine;

/// <summary>
/// PURPOSE:
/// Font references for the runtime-built Office HUD, loaded from
/// Resources/HUD/HudFonts so the HUD needs no scene references.
///
/// WHY NOT THE SCENE HUD FONT:
/// Minnie SDF (the scene's HUD font) has no real glyphs for
/// ! " # $ % ' ( ) * + - / &lt; = &gt; ? @ ` and the digits 3 and 8, which
/// would show as boxes in values like "38 / 100". The HUD uses a
/// full-coverage UI font, and the Notes paper a full-coverage handwriting font.
/// </summary>
[CreateAssetMenu(fileName = "HudFonts", menuName = "TaxVenture/HUD Font Set")]
public sealed class HudFontSet : ScriptableObject
{
    public const string ResourcePath = "HUD/HudFonts";

    [Tooltip("Clean font for HUD values, labels and the objective tracker.")]
    public TMP_FontAsset uiFont;

    [Tooltip("Handwriting font for the Notes paper.")]
    public TMP_FontAsset handwritingFont;

    private static HudFontSet loaded;
    private static bool attempted;

    public static HudFontSet Load()
    {
        if (loaded == null && !attempted)
        {
            attempted = true;
            loaded = Resources.Load<HudFontSet>(ResourcePath);
            if (loaded == null)
                Debug.LogWarning($"[OfficeHud] Resources/{ResourcePath} not found; using the TMP default font.");
        }
        return loaded;
    }
}
