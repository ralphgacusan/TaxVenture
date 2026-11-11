using UnityEditor;
using UnityEditor.Android;
using UnityEditor.Build;
using UnityEngine;

/// <summary>
/// Points Android Player Settings at the icon and splash images in Assets/Images/AppBranding.
/// Goes through the PlayerSettings API because the open Editor overwrites outside edits to
/// ProjectSettings.asset. Applies itself once while the icon and logo slots are still empty.
/// </summary>
public static class AppBrandingSetup
{
    private const string Folder = "Assets/Images/AppBranding/";
    private static readonly Color SplashBackground = new Color32(0xF9, 0xC9, 0x3F, 0xFF);
    private const float LogoSeconds = 2f;

    [InitializeOnLoadMethod]
    private static void ApplyIfUnset()
    {
        if (Application.isBatchMode) return;
        EditorApplication.delayCall += () =>
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || !IsUnset()) return;
            // Textures can import after this script; a later domain reload retries.
            if (AssetDatabase.LoadAssetAtPath<Sprite>(Folder + "splash_logo.png") != null) Apply();
        };
    }

    private static bool IsUnset()
    {
        PlatformIcon[] adaptive = PlayerSettings.GetPlatformIcons(NamedBuildTarget.Android, AndroidPlatformIconKind.Adaptive);
        return PlayerSettings.SplashScreen.logos.Length == 0
            && adaptive.Length > 0
            && adaptive[0].GetTexture(0) == null;
    }

    [MenuItem("Tools/TaxVenture/Apply App Icon And Splash")]
    private static void Apply()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogError("Stop Play mode before applying the app icon and splash.");
            return;
        }

        Sprite logo = AssetDatabase.LoadAssetAtPath<Sprite>(Folder + "splash_logo.png");
        Texture2D staticSplash = Load("splash_static.png");
        if (logo == null || staticSplash == null)
        {
            Debug.LogError("App branding images are missing from " + Folder);
            return;
        }

        // Deprecated by Unity, but the minimum SDK of 25 predates adaptive icons, so API 25 still uses these.
#pragma warning disable CS0618
        if (!AssignIcons(AndroidPlatformIconKind.Adaptive, "adaptive_background_", "adaptive_foreground_")
            || !AssignIcons(AndroidPlatformIconKind.Round, "round_")
            || !AssignIcons(AndroidPlatformIconKind.Legacy, "legacy_"))
        {
            return;
        }
#pragma warning restore CS0618

        PlayerSettings.SplashScreen.show = true;
        PlayerSettings.SplashScreen.showUnityLogo = false;
        PlayerSettings.SplashScreen.logos = new[] { PlayerSettings.SplashScreenLogo.Create(LogoSeconds, logo) };
        PlayerSettings.SplashScreen.backgroundColor = SplashBackground;
        PlayerSettings.SplashScreen.overlayOpacity = 0f;
        PlayerSettings.Android.splashScreenScale = AndroidSplashScreenScale.ScaleToFill;

        // Player Settings has no public API for the Android static splash image.
        SerializedObject settings = new SerializedObject(Unsupported.GetSerializedAssetInterfaceSingleton("PlayerSettings"));
        settings.FindProperty("androidSplashScreen").objectReferenceValue = staticSplash;
        settings.ApplyModifiedPropertiesWithoutUndo();

        AssetDatabase.SaveAssets();
        Debug.Log("Applied the TaxVenture Android icons and splash to Player Settings.");
    }

    /// <summary>Layers are given in Unity's order, so adaptive icons take background then foreground.</summary>
    private static bool AssignIcons(PlatformIconKind kind, params string[] layerPrefixes)
    {
        PlatformIcon[] icons = PlayerSettings.GetPlatformIcons(NamedBuildTarget.Android, kind);
        foreach (PlatformIcon icon in icons)
        {
            Texture2D[] layers = new Texture2D[layerPrefixes.Length];
            for (int i = 0; i < layers.Length; i++)
            {
                string file = layerPrefixes[i] + icon.width + ".png";
                layers[i] = Load(file);
                if (layers[i] == null)
                {
                    Debug.LogError("Missing app icon image " + Folder + file);
                    return false;
                }
            }
            icon.SetTextures(layers);
        }
        PlayerSettings.SetPlatformIcons(NamedBuildTarget.Android, kind, icons);
        return true;
    }

    private static Texture2D Load(string file) => AssetDatabase.LoadAssetAtPath<Texture2D>(Folder + file);
}
