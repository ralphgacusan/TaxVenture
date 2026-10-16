using System.Collections.Generic;

/// <summary>
/// PURPOSE:
/// The 4 classification "corners" used by the corkboard sidequest.
/// </summary>
public enum TaxpayerClassification
{
    ResidentCitizen,
    NonResidentCitizen,
    ResidentAlien,
    NonResidentAlien
}

/// <summary>
/// One taxpayer's profile data. Currently a plain C# class with a
/// hardcoded list below (TaxpayerProfileDatabase). Swap the database
/// for a ScriptableObject or JSON loader later without touching any
/// other script — everything else only calls GetByNpcId / GetById.
/// </summary>
[System.Serializable]
public class TaxpayerProfile
{
    public string ProfileId;
    public string TaxpayerName;
    public string Description;
    public TaxpayerClassification CorrectClassification;
    public string NpcId;

    public TaxpayerProfile(
        string id,
        string name,
        string description,
        TaxpayerClassification classification,
        string npcId)
    {
        ProfileId = id;
        TaxpayerName = name;
        Description = description;
        CorrectClassification = classification;
        NpcId = npcId;
    }
}

/// <summary>
/// The 4 Level 1 taxpayers. Each description is a short summary of what
/// that person told the player: useful notes for the corkboard (and for
/// Marie's office case), but never the classification itself.
/// Resources/Levels/Level_01/town.json can replace the descriptions
/// (TownContentLoader calls SetDescription at level load).
/// </summary>
public static class TaxpayerProfileDatabase
{
    private static readonly List<TaxpayerProfile> profiles = new List<TaxpayerProfile>
    {
        new TaxpayerProfile(
            "profile_marie",
            "Marie Bautista",
            "Born and raised here, never lived abroad; salaried employee at a local BPO.",
            TaxpayerClassification.ResidentCitizen,
            "npc_marie"
        ),

        new TaxpayerProfile(
            "profile_jhunjhun",
            "Jhun-jhun Templo",
            "Pinoy engineer on a Qatar contract; lives abroad, home only weeks a year.",
            TaxpayerClassification.NonResidentCitizen,
            "npc_jhunjhun"
        ),

        new TaxpayerProfile(
            "profile_milo",
            "Milo Zenn",
            "From very far away; settled here six years with family; runs his restaurant.",
            TaxpayerClassification.ResidentAlien,
            "npc_milo"
        ),

        new TaxpayerProfile(
            "profile_zorbix",
            "Zorbix",
            "Visitor from another planet on a paid 27-day job; flight home booked.",
            TaxpayerClassification.NonResidentAlien,
            "npc_zorbix"
        ),
    };

    public static IReadOnlyList<TaxpayerProfile> All => profiles;

    public static TaxpayerProfile GetById(string profileId)
    {
        return profiles.Find(p => p.ProfileId == profileId);
    }

    public static TaxpayerProfile GetByNpcId(string npcId)
    {
        return profiles.Find(p => p.NpcId == npcId);
    }

    private static readonly Dictionary<string, string> builtInDescriptions = new Dictionary<string, string>();

    /// <summary>Replaces a profile's description (null restores the built-in one).</summary>
    public static void SetDescription(string profileId, string description)
    {
        TaxpayerProfile profile = GetById(profileId);
        if (profile == null) return;
        if (!builtInDescriptions.ContainsKey(profileId)) builtInDescriptions[profileId] = profile.Description;
        profile.Description = string.IsNullOrWhiteSpace(description) ? builtInDescriptions[profileId] : description;
    }

    /// <summary>Back to the built-in descriptions.</summary>
    public static void ClearOverrides()
    {
        foreach (var pair in builtInDescriptions)
        {
            TaxpayerProfile profile = GetById(pair.Key);
            if (profile != null) profile.Description = pair.Value;
        }
    }
}