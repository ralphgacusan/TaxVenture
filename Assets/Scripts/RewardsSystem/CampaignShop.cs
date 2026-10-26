using System;
using System.Collections.Generic;

/// <summary>
/// One thing the shop sells (T12/T13). kind "item" grants an inventory item
/// (the bookmark set); kind "upgrade" raises a capacity (Satchel: bag, Belt
/// Pouch: hotbar); kind "decor" installs a model at an office display anchor
/// (T13: pencil cup, island print). A product with `requires` that is not
/// met is a teaser: visible with its label, never purchasable.
/// </summary>
[Serializable]
public class ShopProduct
{
    public const string KindItem = "item";
    public const string KindUpgrade = "upgrade";
    public const string KindDecor = "decor";

    public string id = "";
    public string kind = "";
    /// <summary>Catalogue tab: "supplies" or "office" (T13).</summary>
    public string section = "";
    /// <summary>Item kind: the inventory item granted (inventory.json id).</summary>
    public string itemId = "";
    /// <summary>Upgrade kind: "Bag" or "Hotbar".</summary>
    public string area = "";
    /// <summary>Upgrade kind: the capacity it raises the area to.</summary>
    public int capacity;
    /// <summary>Decor kind: display anchor ids, preferred first (the first free one is used).</summary>
    public List<string> anchors = new List<string>();
    public int price;
    public string title = "";
    /// <summary>One line under the title on its card.</summary>
    public string tagline = "";
    /// <summary>What it does, plainly ("Your bag grows from 6 to 9 slots").</summary>
    public string effect = "";
    /// <summary>HudIconArt picture key.</summary>
    public string icon = "";
    /// <summary>"", "chapter:&lt;id&gt;" (that story chapter completed) or "pending:&lt;feature&gt;" (not in this build: always a teaser).</summary>
    public string requires = "";
    /// <summary>Shown on a teaser card ("Requires corrected Level 2").</summary>
    public string requiresLabel = "";
    /// <summary>Optional product that must be owned first (the second Pouch needs the first).</summary>
    public string after = "";
    /// <summary>T14: a supply bought again and again (each with a fresh request); never "owned".</summary>
    public bool consumable;

    public bool IsUpgrade => kind == KindUpgrade;
    public bool IsItem => kind == KindItem;
    public bool IsDecor => kind == KindDecor;
}

/// <summary>
/// Where the Supply Counter stands (T12), as configuration: beside the office
/// entrance on its plaza side. The entrance is the average of the named door
/// objects (or the fallback); candidates step out toward `toward` by each
/// distance and sideways by each offset (metres), and the first spot on open
/// ground with nothing in the counter's footprint wins, so the result is the
/// same on every load. useFixed pins an exact spot instead; flip turns the
/// counter round if its customer side faces the wrong way.
/// </summary>
[Serializable]
public class ShopCounterPlacement
{
    public List<string> entrance = new List<string>();
    public string fallback = "";
    public string toward = "";
    public List<float> distances = new List<float>();
    public List<float> offsets = new List<float>();
    public bool flip;
    public bool useFixed;
    public List<float> fixedPosition = new List<float>();
    public float fixedYaw;
}

/// <summary>
/// An office display anchor (T13). kind "surface": a free spot on top of
/// `target` (under `parent`), on the `side` ("right" / "left") of the desk as
/// seen from its seat, toward the far corner. kind "wall": on the wall behind
/// `target`, `above` metres over its top. Both are resolved the same way on
/// every load (OfficeDecor). kind "floor" (T14): the first clear floor spot
/// beside `target` on the seat's `side` (no anchor uses it since the Desk
/// Drawer unit was removed).
/// </summary>
[Serializable]
public class ShopDecorAnchor
{
    public const string KindSurface = "surface";
    public const string KindWall = "wall";
    public const string KindFloor = "floor";

    public string id = "";
    /// <summary>Where the player is told it went ("the right corner of your desk").</summary>
    public string label = "";
    public string kind = "";
    public string parent = "";
    public string target = "";
    public string side = "";
    public float above;
}

/// <summary>The shop catalogue (Resources/Campaign/shop.json; BuiltIn() is the same data). The counter and the menu read this one catalogue.</summary>
[Serializable]
public class ShopCatalogue
{
    public const string BookmarkSet = "bookmark_set";
    public const string ReviewVoucher = "review_voucher";
    public const string Satchel = "satchel";
    public const string BeltPouch = "belt_pouch";
    public const string BigSatchel = "big_satchel";
    public const string SecondPouch = "second_pouch";
    public const string PencilCup = "pencil_cup";
    public const string IslandPrint = "island_print";
    public const string ComparisonStand = "comparison_stand";
    public const string OfficeComputer = "office_computer";

    public const string SectionSupplies = "supplies";
    public const string SectionOffice = "office";
    public static readonly string[] Sections = { SectionSupplies, SectionOffice };
    /// <summary>One catalogue page holds six cards.</summary>
    public const int CardsPerSection = 6;

    public List<ShopProduct> products = new List<ShopProduct>();
    public ShopCounterPlacement counter = new ShopCounterPlacement();
    public List<ShopDecorAnchor> anchors = new List<ShopDecorAnchor>();

    public ShopProduct Find(string id) => products?.Find(p => p != null && p.id == id);

    public ShopDecorAnchor FindAnchor(string id) => string.IsNullOrEmpty(id) ? null : anchors?.Find(a => a != null && a.id == id);

    /// <summary>The products on one catalogue tab, in catalogue order.</summary>
    public List<ShopProduct> InSection(string section) =>
        (products ?? new List<ShopProduct>()).FindAll(p => p != null && p.section == section);

    public static string SectionTitle(string section) => section == SectionOffice ? "OFFICE" : "SUPPLIES";

    /// <summary>Authoring problems: ids, kinds, prices, unknown items, capacities beyond the hard ceilings, broken chains, sections, anchors.</summary>
    public List<string> Problems(InventoryCatalogue items)
    {
        var problems = new List<string>();
        var ids = new HashSet<string>(StringComparer.Ordinal);
        var anchorIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (ShopDecorAnchor a in anchors ?? new List<ShopDecorAnchor>())
        {
            if (a == null || string.IsNullOrWhiteSpace(a.id)) { problems.Add("a display anchor is missing its id"); continue; }
            if (!anchorIds.Add(a.id)) problems.Add($"duplicate display anchor \"{a.id}\"");
            if (string.IsNullOrWhiteSpace(a.label)) problems.Add($"display anchor \"{a.id}\" has no label");
            if (a.kind != ShopDecorAnchor.KindSurface && a.kind != ShopDecorAnchor.KindWall && a.kind != ShopDecorAnchor.KindFloor)
                problems.Add($"display anchor \"{a.id}\" has unknown kind \"{a.kind}\"");
            if (string.IsNullOrWhiteSpace(a.parent) || string.IsNullOrWhiteSpace(a.target)) problems.Add($"display anchor \"{a.id}\" needs a parent and a target");
            if ((a.kind == ShopDecorAnchor.KindSurface || a.kind == ShopDecorAnchor.KindFloor) && a.side != "right" && a.side != "left")
                problems.Add($"display anchor \"{a.id}\" needs side right or left");
            if (a.kind == ShopDecorAnchor.KindWall && (a.above < 0f || a.above > 1.5f)) problems.Add($"display anchor \"{a.id}\" sits {a.above} m above its target (0-1.5)");
        }
        foreach (ShopProduct p in products ?? new List<ShopProduct>())
        {
            if (p == null || string.IsNullOrWhiteSpace(p.id)) { problems.Add("a product is missing its id"); continue; }
            if (!ids.Add(p.id)) problems.Add($"duplicate product \"{p.id}\"");
            if (string.IsNullOrWhiteSpace(p.title)) problems.Add($"product \"{p.id}\" has no title");
            if (p.price <= 0) problems.Add($"product \"{p.id}\" has price {p.price}");
            if (p.consumable && (!p.IsItem || items?.Find(p.itemId) == null || items.Find(p.itemId).stackLimit <= 1))
                problems.Add($"product \"{p.id}\" is consumable but does not sell a stackable item");
            if (Array.IndexOf(Sections, p.section) < 0) problems.Add($"product \"{p.id}\" is in unknown section \"{p.section}\"");
            bool pending = !string.IsNullOrEmpty(p.requires) && p.requires.StartsWith("pending:", StringComparison.Ordinal);
            if (p.IsItem)
            {
                InventoryItemDefinition item = items?.Find(p.itemId);
                if (item == null) problems.Add($"product \"{p.id}\" sells unknown item \"{p.itemId}\"");
                else if (item.required) problems.Add($"product \"{p.id}\" sells a required item");
            }
            else if (p.IsUpgrade)
            {
                if (p.area == "Bag" && (p.capacity <= CampaignInventoryRecord.StartBag || p.capacity > CampaignInventoryRecord.MaxBag))
                    problems.Add($"product \"{p.id}\" raises the bag to {p.capacity} ({CampaignInventoryRecord.StartBag + 1}-{CampaignInventoryRecord.MaxBag})");
                else if (p.area == "Hotbar" && (p.capacity <= CampaignInventoryRecord.StartHotbar || p.capacity > CampaignInventoryRecord.MaxHotbar))
                    problems.Add($"product \"{p.id}\" raises the bar to {p.capacity} (4-{CampaignInventoryRecord.MaxHotbar})");
                else if (p.area != "Bag" && p.area != "Hotbar") problems.Add($"product \"{p.id}\" upgrades unknown area \"{p.area}\"");
            }
            else if (p.IsDecor)
            {
                if (!pending && (p.anchors == null || p.anchors.Count == 0)) problems.Add($"decor \"{p.id}\" has no display anchor");
                foreach (string anchor in p.anchors ?? new List<string>())
                    if (!anchorIds.Contains(anchor ?? "")) problems.Add($"decor \"{p.id}\" uses unknown display anchor \"{anchor}\"");
            }
            else problems.Add($"product \"{p.id}\" has unknown kind \"{p.kind}\"");
            if (!string.IsNullOrEmpty(p.requires) && !pending && !p.requires.StartsWith("chapter:", StringComparison.Ordinal))
                problems.Add($"product \"{p.id}\" requires \"{p.requires}\" (use chapter:<id> or pending:<feature>)");
            if (!string.IsNullOrEmpty(p.requires) && string.IsNullOrWhiteSpace(p.requiresLabel))
                problems.Add($"product \"{p.id}\" needs a requiresLabel for its teaser card");
        }
        foreach (string section in Sections)
            if (InSection(section).Count > CardsPerSection) problems.Add($"section \"{section}\" has more than {CardsPerSection} products (one page)");
        foreach (ShopProduct p in products ?? new List<ShopProduct>())
            if (p != null && !string.IsNullOrEmpty(p.after) && !ids.Contains(p.after))
                problems.Add($"product \"{p.id}\" comes after unknown product \"{p.after}\"");
        if (counter == null) problems.Add("the counter placement is missing");
        else if (counter.useFixed ? counter.fixedPosition == null || counter.fixedPosition.Count != 3
                 : (counter.entrance == null || counter.entrance.Count == 0) && string.IsNullOrWhiteSpace(counter.fallback))
            problems.Add("the counter placement needs entrance doors (or a fallback), or a fixed position of three numbers");
        else if (!counter.useFixed && (counter.distances == null || counter.distances.Count == 0 || counter.offsets == null || counter.offsets.Count == 0))
            problems.Add("the counter placement needs distances and offsets to try");
        return problems;
    }

    /// <summary>The shipped catalogue (identical to Resources/Campaign/shop.json). Level 1 earns 160 coins: enough for the three Chapter 1 supplies (150), never the whole catalogue.</summary>
    public static ShopCatalogue BuiltIn()
    {
        var c = new ShopCatalogue();
        c.products.Add(new ShopProduct
        {
            id = BookmarkSet, kind = ShopProduct.KindItem, section = SectionSupplies, itemId = InventoryCatalogue.BookmarkSet, price = 40, icon = "bookmarks",
            title = "Bookmark Set", tagline = "Three leather ribbon pins",
            effect = "Pin up to three records to the top of your Bag. Every record stays free to read without them."
        });
        c.products.Add(new ShopProduct
        {
            id = ReviewVoucher, kind = ShopProduct.KindItem, section = SectionSupplies, itemId = InventoryCatalogue.ReviewVoucher, price = 30, icon = "voucher",
            consumable = true, title = "Review Voucher", tagline = "+180 review seconds, one per case",
            effect = "Use it during a case review: the Auditor's cutoff moves 180 active review seconds later. One per case; you can carry two."
        });
        c.products.Add(new ShopProduct
        {
            id = Satchel, kind = ShopProduct.KindUpgrade, section = SectionSupplies, area = "Bag", capacity = 9, price = 50, icon = "bag",
            title = "Satchel", tagline = "A roomier canvas bag",
            effect = "Your bag grows from 6 to 9 slots."
        });
        c.products.Add(new ShopProduct
        {
            id = BeltPouch, kind = ShopProduct.KindUpgrade, section = SectionSupplies, area = "Hotbar", capacity = 4, price = 60, icon = "pouch",
            title = "Belt Pouch", tagline = "One more item at hand",
            effect = "Your bar grows from 3 to 4 slots."
        });
        c.products.Add(new ShopProduct
        {
            id = BigSatchel, kind = ShopProduct.KindUpgrade, section = SectionSupplies, area = "Bag", capacity = 13, price = 90, icon = "bag",
            title = "Large Satchel", tagline = "Room for a whole case file",
            effect = "Your bag grows to 13 slots.", requires = "chapter:Level_02", requiresLabel = "Requires corrected Level 2", after = Satchel
        });
        c.products.Add(new ShopProduct
        {
            id = SecondPouch, kind = ShopProduct.KindUpgrade, section = SectionSupplies, area = "Hotbar", capacity = 5, price = 100, icon = "pouch",
            title = "Second Pouch", tagline = "Five items at hand",
            effect = "Your bar grows to 5 slots.", requires = "chapter:Level_02", requiresLabel = "Requires corrected Level 2", after = BeltPouch
        });
        c.products.Add(new ShopProduct
        {
            id = PencilCup, kind = ShopProduct.KindDecor, section = SectionOffice, price = 30, icon = "pencilcup",
            anchors = new List<string> { "desk_corner" },
            title = "Brass Pencil Cup", tagline = "Three coloured pencils, always at hand",
            effect = "A squat brass mesh cup with three coloured pencils for your desk. Decor only: it changes no score."
        });
        c.products.Add(new ShopProduct
        {
            id = IslandPrint, kind = ShopProduct.KindDecor, section = SectionOffice, price = 60, icon = "islandprint",
            anchors = new List<string> { "wall_above_filing" },
            title = "Framed Island Print", tagline = "The coast, in a dark wood frame",
            effect = "An island print in a dark wood frame for the wall above your filing shelf. Decor only: it changes no score."
        });
        c.products.Add(new ShopProduct
        {
            id = ComparisonStand, kind = ShopProduct.KindDecor, section = SectionOffice, price = 120, icon = "stand",
            title = "Comparison Stand", tagline = "Two records side by side",
            effect = "Holds two records upright at your desk so you can compare them.",
            requires = "pending:comparison_stand", requiresLabel = "Requires ready Level 2"
        });
        c.products.Add(new ShopProduct
        {
            id = OfficeComputer, kind = ShopProduct.KindDecor, section = SectionOffice, price = 250, icon = "computer",
            title = "Office Computer", tagline = "Your own desk computer",
            effect = "A computer for your desk that keeps your records together.",
            requires = "pending:office_computer", requiresLabel = "Requires corrected Level 3"
        });
        c.counter = new ShopCounterPlacement
        {
            entrance = new List<string>
            {
                "Environment/OfficeBuilding/Rooms/Reception/Doors/Office_Entrance_Door_1",
                "Environment/OfficeBuilding/Rooms/Reception/Doors/Office_Entrance_Door_2"
            },
            fallback = "Environment/OfficeBuilding/Rooms/Reception/Doors",
            toward = "Environment/Outside/Plaza",
            distances = new List<float> { 2.6f, 3.4f },
            offsets = new List<float> { 7f, 8f, 6f, 9f, 10f, 5f, 11f, -7f, -8f, -9f, -10f, -6f }
        };
        c.anchors.Add(new ShopDecorAnchor
        {
            id = "desk_corner", label = "the right corner of your desk", kind = ShopDecorAnchor.KindSurface,
            parent = "Environment/OfficeBuilding/Rooms/ConsultantOffice", target = "Desk", side = "right"
        });
        c.anchors.Add(new ShopDecorAnchor
        {
            id = "wall_above_filing", label = "the wall above your filing shelf", kind = ShopDecorAnchor.KindWall,
            parent = "Environment/OfficeBuilding/Rooms/ConsultantOffice/MCRoom/OfficeRoomProps/ShelfProps", target = "SM_Prop_Shelf_06", above = 0.3f
        });
        return c;
    }
}

/// <summary>What the shop says about one product right now.</summary>
public enum ShopStatus { Available, TooExpensive, Owned, Teaser, NeedsPrevious, ShopClosed, BrowseOnly, Unknown }

/// <summary>The outcome of a purchase request, with the facts a receipt shows.</summary>
public sealed class PurchaseResult
{
    public InventoryOutcome Outcome = InventoryOutcome.Rejected;
    public string Message = "";
    public string ProductId = "";
    public int CoinsBefore;
    public int CoinsAfter;
    /// <summary>Item products: where the item landed (its slot, or the hand for Make room).</summary>
    public InventoryResult Item;
    /// <summary>Decor products: the display anchor it was installed at.</summary>
    public string AnchorId = "";
    public bool Ok => Outcome == InventoryOutcome.Applied || Outcome == InventoryOutcome.AlreadyApplied;
}

/// <summary>
/// PURPOSE:
/// The shared shop domain (T12/T13): product status, and one purchase as one
/// transaction on a candidate save. The Supply Counter and the 3D Menu are
/// presenters over these same rules and the same wallet.
///
/// A purchase needs a campaign whose shop has been introduced, a purchasable
/// product (not a teaser, not owned, its predecessor owned) and enough coins.
/// It then debits the price with a ledger line, records the product id as
/// owned, records the request id once (a retry with the same id is
/// AlreadyApplied, not a second charge), and raises the capacity, grants the
/// item or installs the decor at its anchor - all in the same candidate. Any
/// refusal changes nothing. Buying never awards trust or XP and never
/// repeats a story coin reward.
/// </summary>
public static class ShopRules
{
    public const string PurchaseKind = "purchase";
    public const string ShopClosedMessage = "Visit the Supply Counter beside the office first.";

    public static bool IsOwned(PlayerProgressData data, ShopProduct product) =>
        data != null && data.HasCampaign && product != null && !product.consumable && data.campaign.ownedUpgradeIds.Contains(product.id);

    /// <summary>"chapter:Level_02" is met when the story completed that chapter; "pending:..." never is (not in this build).</summary>
    public static bool RequirementMet(PlayerProgressData data, string requires)
    {
        if (string.IsNullOrEmpty(requires)) return true;
        if (data == null || !data.HasCampaign) return false;
        if (requires.StartsWith("chapter:", StringComparison.Ordinal))
            return data.campaign.IsChapterCompleted(requires.Substring("chapter:".Length));
        return false;
    }

    /// <summary>The once-only receipt id of one purchase request (CampaignRewards claim format).</summary>
    public static string PurchaseClaimId(string campaignId, string requestId) =>
        CampaignRewards.ClaimId(campaignId, "shop", requestId, PurchaseKind);

    /// <summary>canSpend: a story run, or the menu between runs; anything else only browses.</summary>
    public static ShopStatus StatusOf(PlayerProgressData data, ShopCatalogue catalogue, ShopProduct product, bool canSpend)
    {
        if (product == null || catalogue?.Find(product.id) == null) return ShopStatus.Unknown;
        if (IsOwned(data, product)) return ShopStatus.Owned;
        if (!RequirementMet(data, product.requires)) return ShopStatus.Teaser;
        if (!string.IsNullOrEmpty(product.after) && !IsOwned(data, catalogue.Find(product.after))) return ShopStatus.NeedsPrevious;
        if (!canSpend) return ShopStatus.BrowseOnly;
        if (data == null || !data.HasCampaign || !data.campaign.shopIntroduced) return ShopStatus.ShopClosed;
        if (data.campaign.coins < product.price) return ShopStatus.TooExpensive;
        return ShopStatus.Available;
    }

    /// <summary>The player-facing reason a product cannot be bought ("" when it can).</summary>
    public static string ReasonFor(ShopStatus status, ShopProduct product, PlayerProgressData data, ShopCatalogue catalogue)
    {
        switch (status)
        {
            case ShopStatus.Owned: return "You already own this.";
            case ShopStatus.Teaser: return product.requiresLabel;
            case ShopStatus.NeedsPrevious: return "Buy the " + (catalogue?.Find(product.after)?.title ?? "earlier one") + " first.";
            case ShopStatus.BrowseOnly: return "Practice run: browsing only. Nothing is spent.";
            case ShopStatus.ShopClosed: return ShopClosedMessage;
            case ShopStatus.TooExpensive:
                int need = product.price - (data != null && data.HasCampaign ? data.campaign.coins : 0);
                return $"You need {need} more coin{(need == 1 ? "" : "s")}.";
            case ShopStatus.Unknown: return "That item is not in the catalogue.";
            default: return "";
        }
    }

    /// <summary>
    /// Applies one purchase to a candidate save (the caller writes it, then
    /// publishes). Refusals return without changing the candidate.
    /// noHand: there is no hand to hold an item while room is made (the
    /// menu), so a full bar and bag refuse the purchase (nothing spent).
    /// </summary>
    public static PurchaseResult Purchase(PlayerProgressData candidate, ShopCatalogue catalogue, InventoryCatalogue items, string productId, string requestId,
        bool noHand = false)
    {
        var result = new PurchaseResult { ProductId = productId ?? "" };
        if (candidate == null || !candidate.HasCampaign) { result.Message = "No story to buy for."; return result; }
        result.CoinsBefore = result.CoinsAfter = candidate.campaign.coins;
        if (string.IsNullOrWhiteSpace(requestId)) { result.Message = "Try again."; return result; }
        string claim = PurchaseClaimId(candidate.campaign.campaignId, requestId.Trim());
        if (candidate.campaign.rewardClaims.Contains(claim))
        {
            result.Outcome = InventoryOutcome.AlreadyApplied;
            result.Message = "Already bought.";
            return result;
        }
        ShopProduct product = catalogue?.Find(productId);
        ShopStatus status = StatusOf(candidate, catalogue, product, canSpend: true);
        if (status != ShopStatus.Available)
        {
            result.Message = ReasonFor(status, product, candidate, catalogue);
            return result;
        }

        // The item, capacity or display spot first (each can refuse), then the money: nothing changes on a refusal.
        CampaignInventoryRecord inventory = candidate.campaign.inventory;
        string action = "shop/" + product.id + "/" + requestId.Trim();
        if (product.IsItem)
        {
            var info = new InventoryItemRecord { itemId = product.itemId, source = "shop:" + product.id };
            InventoryResult granted = noHand
                ? InventoryRules.AcquireWithoutHand(inventory, items, product.itemId, 1, action, info)
                : InventoryRules.Acquire(inventory, items, product.itemId, 1, action, null, info);
            if (!granted.Changed)
            {
                result.Message = granted.Outcome == InventoryOutcome.NoChange && !product.consumable ? "You already own this." :
                    string.IsNullOrEmpty(granted.Message) ? "There is no room for it." : granted.Message + ".";
                return result;
            }
            result.Item = granted;
        }
        else if (product.IsDecor)
        {
            string anchor = DecorRules.FreeAnchor(candidate.campaign, catalogue, product);
            if (anchor.Length == 0)
            {
                result.Message = DecorRules.NoSpotMessage;
                return result;
            }
            candidate.campaign.decor.RemoveAll(d => d != null && d.itemId == product.id);
            candidate.campaign.decor.Add(new DecorPlacement { itemId = product.id, anchorId = anchor });
            result.AnchorId = anchor;
        }
        else
        {
            InventoryArea area = product.area == "Bag" ? InventoryArea.Bag : InventoryArea.Hotbar;
            InventoryResult raised = InventoryRules.UpgradeCapacity(inventory, area, product.capacity, action);
            if (!raised.Changed)
            {
                result.Message = "That upgrade would not add a slot.";
                return result;
            }
        }

        candidate.campaign.coins -= product.price;
        candidate.campaign.ledger.Add(new CampaignLedgerEntry
        {
            claimId = claim, kind = CampaignRewards.CoinsKind, amount = -product.price, balanceAfter = candidate.campaign.coins,
            reason = "Bought " + product.title
        });
        if (!product.consumable) candidate.campaign.ownedUpgradeIds.Add(product.id);
        candidate.campaign.rewardClaims.Add(claim);
        result.Outcome = InventoryOutcome.Applied;
        result.CoinsAfter = candidate.campaign.coins;
        return result;
    }
}

/// <summary>
/// PURPOSE:
/// Office decor placement (T13, pure). A decor purchase takes the first of
/// its anchors that no other installed piece holds; with none free the
/// purchase is refused before any coin moves. Resolve() is what the office
/// shows: every owned piece keeps its saved anchor when that is still valid
/// and free, otherwise it moves to its first free valid anchor, in save
/// order, so the result is the same on every load and nothing is hidden
/// while another spot is open. Decor is installed, never carried or sold.
/// </summary>
public static class DecorRules
{
    public const string NoSpotMessage = "There's no free display spot in your office for it.";

    public static string AnchorOf(CampaignRecord c, string itemId) =>
        c?.decor?.Find(d => d != null && d.itemId == itemId)?.anchorId ?? "";

    /// <summary>The piece installed at an anchor ("" when it is free).</summary>
    public static string ItemAt(CampaignRecord c, string anchorId) =>
        string.IsNullOrEmpty(anchorId) ? "" : c?.decor?.Find(d => d != null && d.anchorId == anchorId)?.itemId ?? "";

    /// <summary>The first of the product's known anchors that no other piece holds ("" when every one is taken).</summary>
    public static string FreeAnchor(CampaignRecord c, ShopCatalogue catalogue, ShopProduct product)
    {
        if (c == null || product?.anchors == null) return "";
        foreach (string anchor in product.anchors)
        {
            if (string.IsNullOrWhiteSpace(anchor) || catalogue?.FindAnchor(anchor) == null) continue;
            string holder = ItemAt(c, anchor);
            if (holder.Length == 0 || holder == product.id) return anchor;
        }
        return "";
    }

    /// <summary>Marks a piece's office arrival as shown (presentation only). True when it changed.</summary>
    public static bool MarkArrived(CampaignRecord c, string itemId)
    {
        DecorPlacement placement = c?.decor?.Find(d => d != null && d.itemId == itemId);
        if (placement == null || placement.arrived) return false;
        placement.arrived = true;
        return true;
    }

    /// <summary>
    /// The desk-corner resting spot (pure, surface space): a footprint
    /// (halfX by halfZ) fully on the surface inside `margin`, strictly on the
    /// seat's `side` (+1 right, -1 left, from the seat's forward direction),
    /// at least `gap` from every obstacle, and nearest the far corner on that
    /// side. Grid order breaks ties, so the answer is the same on every load.
    /// False when nothing fits.
    /// </summary>
    public static bool CornerSpot(PickupRules.Box surface, float halfX, float halfZ, IList<PickupRules.Box> obstacles, float margin, float gap,
        float forwardX, float forwardZ, float side, out float x, out float z)
    {
        float length = (float)Math.Sqrt(forwardX * forwardX + forwardZ * forwardZ);
        if (length < 1e-5f) { forwardX = 0f; forwardZ = 1f; length = 1f; }
        forwardX /= length;
        forwardZ /= length;
        // Right of the seat in a left-handed, Y-up space: cross(up, forward).
        float rightX = forwardZ * Math.Sign(side == 0f ? 1f : side), rightZ = -forwardX * Math.Sign(side == 0f ? 1f : side);
        float centreX = (surface.MinX + surface.MaxX) * 0.5f, centreZ = (surface.MinZ + surface.MaxZ) * 0.5f;
        x = centreX;
        z = centreZ;

        // The far corner on that side: the surface corner furthest along right + forward.
        float cornerX = centreX, cornerZ = centreZ, cornerScore = float.NegativeInfinity;
        foreach (float px in new[] { surface.MinX, surface.MaxX })
            foreach (float pz in new[] { surface.MinZ, surface.MaxZ })
            {
                float s = (px - centreX) * (rightX + forwardX) + (pz - centreZ) * (rightZ + forwardZ);
                if (s > cornerScore) { cornerScore = s; cornerX = px; cornerZ = pz; }
            }

        float minX = surface.MinX + margin + halfX, maxX = surface.MaxX - margin - halfX;
        float minZ = surface.MinZ + margin + halfZ, maxZ = surface.MaxZ - margin - halfZ;
        if (minX > maxX || minZ > maxZ) return false;
        const int steps = 16;
        float best = float.PositiveInfinity;
        bool found = false;
        for (int iz = 0; iz <= steps; iz++)
            for (int ix = 0; ix <= steps; ix++)
            {
                float cx = minX + (maxX - minX) * ix / steps;
                float cz = minZ + (maxZ - minZ) * iz / steps;
                if ((cx - centreX) * rightX + (cz - centreZ) * rightZ <= 0f) continue;
                var foot = new PickupRules.Box(cx - halfX, cz - halfZ, cx + halfX, cz + halfZ);
                bool blocked = false;
                if (obstacles != null)
                    foreach (PickupRules.Box obstacle in obstacles)
                        if (PickupRules.Gap(foot, obstacle) < gap) { blocked = true; break; }
                if (blocked) continue;
                float d = (cx - cornerX) * (cx - cornerX) + (cz - cornerZ) * (cz - cornerZ);
                if (d < best - 1e-7f)
                {
                    best = d;
                    x = cx;
                    z = cz;
                    found = true;
                }
            }
        return found;
    }

    /// <summary>What the office shows, one piece per anchor (see the class summary). unplaced: owned pieces with no free valid anchor.</summary>
    public static List<DecorPlacement> Resolve(CampaignRecord c, ShopCatalogue catalogue, List<string> unplaced = null)
    {
        var shown = new List<DecorPlacement>();
        if (c?.decor == null || catalogue == null) return shown;
        var taken = new HashSet<string>(StringComparer.Ordinal);
        var moving = new List<DecorPlacement>();
        foreach (DecorPlacement placement in c.decor)
        {
            ShopProduct product = placement != null ? catalogue.Find(placement.itemId) : null;
            if (product == null || !product.IsDecor || c.ownedUpgradeIds == null || !c.ownedUpgradeIds.Contains(product.id)) continue;
            bool valid = product.anchors.Contains(placement.anchorId) && catalogue.FindAnchor(placement.anchorId) != null;
            if (valid && taken.Add(placement.anchorId))
                shown.Add(new DecorPlacement { itemId = placement.itemId, anchorId = placement.anchorId, arrived = placement.arrived });
            else moving.Add(placement);
        }
        foreach (DecorPlacement placement in moving)
        {
            ShopProduct product = catalogue.Find(placement.itemId);
            string anchor = product.anchors.Find(a => catalogue.FindAnchor(a) != null && !taken.Contains(a));
            if (anchor == null) { unplaced?.Add(placement.itemId); continue; }
            taken.Add(anchor);
            shown.Add(new DecorPlacement { itemId = placement.itemId, anchorId = anchor, arrived = placement.arrived });
        }
        return shown;
    }
}
