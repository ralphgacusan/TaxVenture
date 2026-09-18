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
/// Hardcoded set of the 4 Level 1 taxpayers. Edit the text here to
/// change names/descriptions without touching any other script.
/// </summary>
public static class TaxpayerProfileDatabase
{
    private static readonly List<TaxpayerProfile> profiles = new List<TaxpayerProfile>
    {
        new TaxpayerProfile(
            "profile_marie",
            "Marie Bautista",
            "A Filipino citizen who works locally and has always lived in the Philippines.",
            TaxpayerClassification.ResidentCitizen,
            "npc_marie"
        ),

        new TaxpayerProfile(
            "profile_jhunjhun",
            "Jhun-jhun Templo",
            "A Filipino citizen who works in Qatar and only visits home briefly.",
            TaxpayerClassification.NonResidentCitizen,
            "npc_jhunjhun"
        ),

        new TaxpayerProfile(
            "profile_milo",
            "Milo Zenn",
            "A foreign citizen who has settled in the Philippines and runs a local restaurant.",
            TaxpayerClassification.ResidentAlien,
            "npc_milo"
        ),

        new TaxpayerProfile(
            "profile_zorbix",
            "Zorbix",
            "A foreign visitor earning Philippine income during a short business stay.",
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
}