/// <summary>
/// PURPOSE:
/// How the 3D Menu scene should open next time it loads. The main menu and
/// the progress / level select live in one scene ("3D Menu"); a finished
/// level asks it to open directly on the level select instead of the title
/// view. ToggleCamera reads (and clears) the request in Start.
/// </summary>
public static class MenuEntry
{
    public const string MenuSceneName = "3D Menu";

    /// <summary>True: the next 3D Menu load opens on the level select (progress) view.</summary>
    public static bool OpenLevelSelect;

    /// <summary>Returns the pending request once, then clears it.</summary>
    public static bool ConsumeOpenLevelSelect()
    {
        bool open = OpenLevelSelect;
        OpenLevelSelect = false;
        return open;
    }
}
