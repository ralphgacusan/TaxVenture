/// <summary>Fresh chapter entry plays the opening; a saved Story checkpoint resumes gameplay.</summary>
public static class ChapterOpeningRules
{
    public static string SceneForOffice(string office) => office == "Office_Level2" ? "CS_Chapter02_Start"
        : office == "Office_Level3" ? "CS_Chapter03_Start" : null;

    public static string IdForScene(string scene) => scene == "CS_Chapter02_Start" ? "chapter02.start"
        : scene == "CS_Chapter03_Start" ? "chapter03.start" : null;

    public static bool ShouldPlay(PlayerProgressData data, CampaignRunMode mode, string office, bool practicing)
    {
        if (practicing || SceneForOffice(office) == null || ChapterEndingRules.Pending(data)) return false;
        if (mode == CampaignRunMode.MasteryReplay) return true;
        if (mode != CampaignRunMode.Story || data == null) return false;
        string chapter = office == "Office_Level2" ? "Level_02" : "Level_03";
        if (data.HasCampaign && data.campaign.IsChapterCompleted(chapter)) return false;
        if (data.CheckpointFor(chapter) != null) return false;
        return chapter != "Level_03" || data.campaign?.levelThree == null || !data.campaign.levelThree.IsSet;
    }
}
