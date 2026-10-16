using UnityEditor;
using UnityEngine;

/// <summary>
/// Tools > TaxVenture > Tutorial Hub > Check Content: validates training.json
/// and composes every drill (guided and mastery) against the level's
/// guide.json, with a Console report. The binder itself needs no setup: it
/// adds itself to the 3D Menu at runtime (TrainingBinder3D).
/// </summary>
public static class TutorialHubSetup
{
    [MenuItem("Tools/TaxVenture/Tutorial Hub/Check Content")]
    public static void CheckContent()
    {
        TrainingCatalogFile catalog = TrainingCatalogFile.Load();
        int problems = 0;
        foreach (TrainingCardDef card in catalog.cards)
        {
            TrainingDrill drill = TrainingDrill.Load(card.drill);
            if (drill == null)
            {
                Debug.LogWarning($"[TutorialHubSetup] Card \"{card.id}\": drill \"{card.drill}\" missing.");
                problems++;
                continue;
            }
            GuideScript guide = GuideScript.Load(drill.levelId);
            foreach (bool mastery in new[] { false, true })
            {
                TrainingDrillScript.Result run = TrainingDrillScript.Compose(guide, drill, mastery);
                string name = $"{drill.id} ({(mastery ? "mastery" : "guided")})";
                if (!string.IsNullOrEmpty(run.Problems)) { Debug.LogWarning($"[TutorialHubSetup] {name}:\n{run.Problems}"); problems++; }
                else Debug.Log($"[TutorialHubSetup] {name}: {run.Steps.Count} steps, ends with \"{run.FinalStep}\".");
            }
        }
        Debug.Log(problems == 0 ? "[TutorialHubSetup] Content OK." : $"[TutorialHubSetup] {problems} problem(s); see above.");
    }
}
