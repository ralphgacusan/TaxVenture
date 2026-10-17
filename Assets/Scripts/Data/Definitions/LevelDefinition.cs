using System;
using System.Collections.Generic;

/// <summary>
/// PURPOSE:
/// Lists which case files make up one level, in play order. The gameplay
/// layer never hardcodes "how many cases" — it always reads
/// caseIds.Count. A level with exactly one case naturally results in
/// "Level Complete" firing immediately after that one case finishes,
/// with zero special-casing anywhere in code.
/// </summary>
[Serializable]
public class LevelDefinition
{
    public string levelId;
    public string levelName;
    public List<string> caseIds = new List<string>();

    /// <summary>
    /// Optional: shared office systems this level's scene opts into
    /// ("hud", "minimap", "guide", "fade"). Office_Level1 always has all of
    /// them; see OfficeLevelCatalog.
    /// </summary>
    public List<string> officeFeatures = new List<string>();
}