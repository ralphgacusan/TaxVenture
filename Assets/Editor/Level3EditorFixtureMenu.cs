using UnityEditor;

/// <summary>
/// PURPOSE:
/// Tools > TaxVenture > Level 3 > Fixture: chooses how a DIRECT Play of
/// Office_Level3 starts (revised T03). "First Office Calculation" starts at
/// Bea's computation with the calculator not yet taught
/// (Level3EditorFixtures.FirstOfficeUse); "Bea Encounter" starts assigned,
/// before her records are handed over (the T04 buildup plays); "Bea Table
/// Restored" starts with her records handed over (the concise continuation);
/// "Bea Costs Batch" / "Bea Questions Batch" (T05) open her paper machine on
/// a later batch, the earlier ones filed and accepted through the real Check
/// batch (Level3EditorFixtures.MachineBatch); "Bea Earnings Mistake" (T06)
/// has the earnings filed with one wrong choice, not checked yet; "Bea Summary
/// Ready" (T06) has every batch accepted and the summary on the output ledge,
/// not collected; "None" is the normal fresh start.
/// Only direct Editor attempts read it, and those never save; a story,
/// replay or practice run never does. The choice is kept in EditorPrefs on
/// this machine only.
/// </summary>
[InitializeOnLoad]
public static class Level3EditorFixtureMenu
{
    private const string PrefKey = "TaxVenture.Level3.EditorFixture";
    private const string FirstOfficeMenu = "Tools/TaxVenture/Level 3/Fixture: First Office Calculation";
    private const string EncounterMenu = "Tools/TaxVenture/Level 3/Fixture: Bea Encounter";
    private const string RestoredMenu = "Tools/TaxVenture/Level 3/Fixture: Bea Table Restored";
    private const string CostsMenu = "Tools/TaxVenture/Level 3/Fixture: Bea Costs Batch";
    private const string QuestionsMenu = "Tools/TaxVenture/Level 3/Fixture: Bea Questions Batch";
    private const string MistakeMenu = "Tools/TaxVenture/Level 3/Fixture: Bea Earnings Mistake";
    private const string SummaryMenu = "Tools/TaxVenture/Level 3/Fixture: Bea Summary Ready";
    private const string NoneMenu = "Tools/TaxVenture/Level 3/Fixture: None (fresh start)";

    static Level3EditorFixtureMenu() => Apply();

    private static string Chosen => EditorPrefs.GetString(PrefKey, "");

    private static void Apply()
    {
        string chosen = Chosen;
        Level3Checkpointer.EditorFixture = string.IsNullOrEmpty(chosen) ? null
            : (System.Func<CaseDefinition, Level3ChapterCheckpoint>)(definition => Level3EditorFixtures.Build(chosen, definition));
    }

    [MenuItem(FirstOfficeMenu)]
    private static void ChooseFirstOffice()
    {
        EditorPrefs.SetString(PrefKey, Level3EditorFixtures.FirstOfficeUseId);
        Apply();
    }

    [MenuItem(FirstOfficeMenu, true)]
    private static bool FirstOfficeChecked()
    {
        Menu.SetChecked(FirstOfficeMenu, Chosen == Level3EditorFixtures.FirstOfficeUseId);
        return true;
    }

    [MenuItem(EncounterMenu)]
    private static void ChooseEncounter()
    {
        EditorPrefs.SetString(PrefKey, Level3EditorFixtures.BeaEncounterId);
        Apply();
    }

    [MenuItem(EncounterMenu, true)]
    private static bool EncounterChecked()
    {
        Menu.SetChecked(EncounterMenu, Chosen == Level3EditorFixtures.BeaEncounterId);
        return true;
    }

    [MenuItem(RestoredMenu)]
    private static void ChooseRestored()
    {
        EditorPrefs.SetString(PrefKey, Level3EditorFixtures.BeaTableRestoredId);
        Apply();
    }

    [MenuItem(RestoredMenu, true)]
    private static bool RestoredChecked()
    {
        Menu.SetChecked(RestoredMenu, Chosen == Level3EditorFixtures.BeaTableRestoredId);
        return true;
    }

    [MenuItem(CostsMenu)]
    private static void ChooseCosts()
    {
        EditorPrefs.SetString(PrefKey, Level3EditorFixtures.BeaCostsBatchId);
        Apply();
    }

    [MenuItem(CostsMenu, true)]
    private static bool CostsChecked()
    {
        Menu.SetChecked(CostsMenu, Chosen == Level3EditorFixtures.BeaCostsBatchId);
        return true;
    }

    [MenuItem(QuestionsMenu)]
    private static void ChooseQuestions()
    {
        EditorPrefs.SetString(PrefKey, Level3EditorFixtures.BeaQuestionsBatchId);
        Apply();
    }

    [MenuItem(QuestionsMenu, true)]
    private static bool QuestionsChecked()
    {
        Menu.SetChecked(QuestionsMenu, Chosen == Level3EditorFixtures.BeaQuestionsBatchId);
        return true;
    }

    [MenuItem(MistakeMenu)]
    private static void ChooseMistake()
    {
        EditorPrefs.SetString(PrefKey, Level3EditorFixtures.BeaEarningsMistakeId);
        Apply();
    }

    [MenuItem(MistakeMenu, true)]
    private static bool MistakeChecked()
    {
        Menu.SetChecked(MistakeMenu, Chosen == Level3EditorFixtures.BeaEarningsMistakeId);
        return true;
    }

    [MenuItem(SummaryMenu)]
    private static void ChooseSummary()
    {
        EditorPrefs.SetString(PrefKey, Level3EditorFixtures.BeaSummaryReadyId);
        Apply();
    }

    [MenuItem(SummaryMenu, true)]
    private static bool SummaryChecked()
    {
        Menu.SetChecked(SummaryMenu, Chosen == Level3EditorFixtures.BeaSummaryReadyId);
        return true;
    }

    [MenuItem(NoneMenu)]
    private static void ChooseNone()
    {
        EditorPrefs.DeleteKey(PrefKey);
        Apply();
    }

    [MenuItem(NoneMenu, true)]
    private static bool NoneChecked()
    {
        Menu.SetChecked(NoneMenu, string.IsNullOrEmpty(Chosen));
        return true;
    }
}
