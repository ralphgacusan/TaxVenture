using System;
using System.Collections.Generic;

/// <summary>
/// Where a world pickup or display rests (T11): on top of a scene object,
/// found at runtime by name under a parent path. When several children share
/// the name, the one nearest the object named in `near` is used. The exact
/// spot on its top is chosen by PickupRules.BestSpot (most clearance from
/// the props already there), so placement is the same on every load.
/// </summary>
[Serializable]
public class PickupSpot
{
    /// <summary>Scene path of the parent ("Environment/OfficeBuilding/Rooms/Reception/WaitingArea").</summary>
    public string parent = "";
    /// <summary>Name of the object whose top is the surface ("SM_Prop_CoffeeTable_01").</summary>
    public string surface = "";
    /// <summary>Optional: a prop on the wanted surface, to tell same-named surfaces apart.</summary>
    public string near = "";
}

/// <summary>One optional world pickup (T11): a stable id, the item it gives, when it appears and what it reads.</summary>
[Serializable]
public class PickupDefinition
{
    public string id = "";
    public string itemId = "";
    public string levelId = "";
    public int quantity = 1;
    /// <summary>"" = from the level start, or an objectives key such as "event:ReceptionistTalkedTo" (the introduction it waits for).</summary>
    public string availableAfter = "";
    /// <summary>Printed on the object and its inspect card.</summary>
    public string title = "";
    /// <summary>Short neutral text on the back / the inspect card (never an answer or a case value).</summary>
    public List<string> lines = new List<string>();
    /// <summary>The plaque beside it ("COAST TALES", "CONSULTATION SUPPLIES").</summary>
    public string plaque = "";
    public PickupSpot spot = new PickupSpot();
}

/// <summary>An office display anchor for keepsakes (T11), e.g. the stand at the player's desk.</summary>
[Serializable]
public class DisplayDefinition
{
    public string id = "";
    public string levelId = "";
    /// <summary>The inventory category it accepts (Keepsakes).</summary>
    public string category = "";
    public string title = "";
    public PickupSpot spot = new PickupSpot();
}

/// <summary>World pickups and display anchors (Resources/Campaign/pickups.json; BuiltIn() is the same data).</summary>
[Serializable]
public class PickupCatalogue
{
    public const string PostcardPickup = "coast_tales_postcard";
    public const string VoucherPickup = "level1_review_voucher";
    public const string DeskDisplay = "desk_keepsake_stand";

    public List<PickupDefinition> pickups = new List<PickupDefinition>();
    public List<DisplayDefinition> displays = new List<DisplayDefinition>();

    public PickupDefinition Find(string id) => pickups?.Find(p => p != null && p.id == id);
    public DisplayDefinition FindDisplay(string id) => displays?.Find(d => d != null && d.id == id);

    /// <summary>Authoring problems: ids, unknown items, quantities, spots, and text that must stay neutral.</summary>
    public List<string> Problems(InventoryCatalogue items)
    {
        var problems = new List<string>();
        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (PickupDefinition pickup in pickups ?? new List<PickupDefinition>())
        {
            if (pickup == null || string.IsNullOrWhiteSpace(pickup.id)) { problems.Add("a pickup is missing its id"); continue; }
            if (!ids.Add(pickup.id)) problems.Add($"duplicate pickup id \"{pickup.id}\"");
            InventoryItemDefinition item = items?.Find(pickup.itemId);
            if (item == null) problems.Add($"pickup \"{pickup.id}\" gives unknown item \"{pickup.itemId}\"");
            else if (item.required) problems.Add($"pickup \"{pickup.id}\" gives a required item; required records come from their own handover");
            if (pickup.quantity < 1) problems.Add($"pickup \"{pickup.id}\" has quantity {pickup.quantity}");
            if (string.IsNullOrWhiteSpace(pickup.levelId)) problems.Add($"pickup \"{pickup.id}\" has no level");
            if (string.IsNullOrWhiteSpace(pickup.title)) problems.Add($"pickup \"{pickup.id}\" has no title");
            if (!string.IsNullOrEmpty(pickup.availableAfter) && !pickup.availableAfter.Contains(":"))
                problems.Add($"pickup \"{pickup.id}\" waits for \"{pickup.availableAfter}\" (use an objectives key such as event:ReceptionistTalkedTo)");
            problems.AddRange(SpotProblems("pickup " + pickup.id, pickup.spot));
        }
        foreach (DisplayDefinition display in displays ?? new List<DisplayDefinition>())
        {
            if (display == null || string.IsNullOrWhiteSpace(display.id)) { problems.Add("a display is missing its id"); continue; }
            if (!ids.Add(display.id)) problems.Add($"duplicate display id \"{display.id}\"");
            if (Array.IndexOf(InventoryCatalogue.Categories, display.category) < 0) problems.Add($"display \"{display.id}\" accepts unknown category \"{display.category}\"");
            problems.AddRange(SpotProblems("display " + display.id, display.spot));
        }
        return problems;
    }

    private static IEnumerable<string> SpotProblems(string owner, PickupSpot spot)
    {
        if (spot == null || string.IsNullOrWhiteSpace(spot.parent) || string.IsNullOrWhiteSpace(spot.surface))
            yield return $"{owner} needs a spot (parent path and surface name)";
    }

    /// <summary>The shipped catalogue (identical to Resources/Campaign/pickups.json), used when the file is missing or broken.</summary>
    public static PickupCatalogue BuiltIn()
    {
        var catalogue = new PickupCatalogue();
        catalogue.pickups.Add(new PickupDefinition
        {
            id = PostcardPickup, itemId = InventoryCatalogue.Postcard, levelId = "Level_01", title = "COAST TALES",
            plaque = "COAST TALES - TAKE ONE",
            lines = new List<string>
            {
                "The ferry horn still sounds at six, and the fish market opens before the sun is up.",
                "Someone from the Coast Tales club left this here to welcome you to the island."
            },
            spot = new PickupSpot { parent = "Environment/OfficeBuilding/Rooms/Reception/WaitingArea", surface = "SM_Prop_CoffeeTable_01", near = "SM_Prop_Plant_Flowers_01" }
        });
        catalogue.pickups.Add(new PickupDefinition
        {
            id = VoucherPickup, itemId = InventoryCatalogue.ReviewVoucher, levelId = "Level_01", title = "REVIEW VOUCHER",
            availableAfter = "event:ReceptionistTalkedTo", plaque = "CONSULTATION SUPPLIES",
            lines = new List<string>
            {
                "+180 active review seconds on one case, before the Auditor leaves. One per case.",
                "Free for new consultants: one per person."
            },
            spot = new PickupSpot { parent = "Environment/OfficeBuilding/Rooms/Reception/WaitingArea", surface = "SM_Prop_CoffeeTable_01", near = "SM_Prop_Book_Magazine_01" }
        });
        catalogue.displays.Add(new DisplayDefinition
        {
            id = DeskDisplay, levelId = "Level_01", category = InventoryCatalogue.Keepsakes, title = "KEEPSAKE",
            spot = new PickupSpot { parent = "Environment/OfficeBuilding/Rooms/ConsultantOffice", surface = "Desk" }
        });
        return catalogue;
    }
}

/// <summary>
/// PURPOSE:
/// Pure rules for T11 world pickups: the once-per-campaign claim id, when a
/// pickup may appear, and the deterministic resting spot on a surface. No
/// Unity types, so the regression suite runs them directly.
/// </summary>
public static class PickupRules
{
    public const string ClaimKind = "pickup";

    /// <summary>Campaign + level + collectible: claimed once, never again (CampaignRewards claim format).</summary>
    public static string ClaimId(string campaignId, string levelId, string pickupId) =>
        CampaignRewards.ClaimId(campaignId, levelId, pickupId, ClaimKind);

    /// <summary>A profile card's claim: the same format with the profile id ("profile_marie").</summary>
    public static string ProfileClaimId(string campaignId, string levelId, string profileId) =>
        CampaignRewards.ClaimId(campaignId, levelId, "profile/" + (profileId ?? "").Trim(), ClaimKind);

    /// <summary>Shown when not claimed and its introduction (an objectives key) has happened.</summary>
    public static bool IsAvailable(PickupDefinition pickup, bool claimed, ICollection<string> reachedKeys) =>
        pickup != null && !claimed &&
        (string.IsNullOrEmpty(pickup.availableAfter) || (reachedKeys != null && reachedKeys.Contains(pickup.availableAfter)));

    public struct Box
    {
        public float MinX, MinZ, MaxX, MaxZ;
        public Box(float minX, float minZ, float maxX, float maxZ) { MinX = minX; MinZ = minZ; MaxX = maxX; MaxZ = maxZ; }
    }

    /// <summary>
    /// The resting spot for a footprint (halfX by halfZ) on a surface top:
    /// fully inside the surface (with a margin), clear of every obstacle, and
    /// with the most room to the nearest obstacle (ties: nearer the surface
    /// centre, then grid order). False when nothing fits.
    /// </summary>
    public static bool BestSpot(Box surface, float halfX, float halfZ, IList<Box> obstacles, float margin, out float x, out float z)
    {
        x = (surface.MinX + surface.MaxX) * 0.5f;
        z = (surface.MinZ + surface.MaxZ) * 0.5f;
        float minX = surface.MinX + margin + halfX, maxX = surface.MaxX - margin - halfX;
        float minZ = surface.MinZ + margin + halfZ, maxZ = surface.MaxZ - margin - halfZ;
        if (minX > maxX || minZ > maxZ) return false;
        const int steps = 12;
        float centreX = x, centreZ = z;
        float bestScore = float.NegativeInfinity;
        bool found = false;
        for (int iz = 0; iz <= steps; iz++)
            for (int ix = 0; ix <= steps; ix++)
            {
                float cx = minX + (maxX - minX) * ix / steps;
                float cz = minZ + (maxZ - minZ) * iz / steps;
                var foot = new Box(cx - halfX, cz - halfZ, cx + halfX, cz + halfZ);
                float clearance = 1f;
                bool blocked = false;
                if (obstacles != null)
                    foreach (Box obstacle in obstacles)
                    {
                        float gap = Gap(foot, obstacle);
                        if (gap <= 0f) { blocked = true; break; }
                        clearance = Math.Min(clearance, gap);
                    }
                if (blocked) continue;
                float fromCentre = (float)Math.Sqrt((cx - centreX) * (cx - centreX) + (cz - centreZ) * (cz - centreZ));
                float score = clearance - fromCentre * 0.05f;
                if (score > bestScore + 1e-5f)
                {
                    bestScore = score;
                    x = cx;
                    z = cz;
                    found = true;
                }
            }
        return found;
    }

    /// <summary>Distance between two boxes (0 or less when they touch or overlap).</summary>
    public static float Gap(Box a, Box b)
    {
        float dx = Math.Max(b.MinX - a.MaxX, a.MinX - b.MaxX);
        float dz = Math.Max(b.MinZ - a.MaxZ, a.MinZ - b.MaxZ);
        if (dx <= 0f && dz <= 0f) return Math.Max(dx, dz);
        if (dx <= 0f) return dz;
        if (dz <= 0f) return dx;
        return (float)Math.Sqrt(dx * dx + dz * dz);
    }
}
