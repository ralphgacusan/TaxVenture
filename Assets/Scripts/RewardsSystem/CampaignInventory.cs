using System;
using System.Collections.Generic;

/// <summary>Where an item sits (T10): a hotbar or bag slot, the "Make room" hand, or (save repair only) the old Desk Drawer list.</summary>
public enum InventoryArea { None, Hotbar, Bag, Hand, Drawer, Display }

/// <summary>One place in the inventory: an area and (for slots) its index.</summary>
public struct SlotRef : IEquatable<SlotRef>
{
    public InventoryArea Area;
    public int Index;

    public SlotRef(InventoryArea area, int index)
    {
        Area = area;
        Index = index;
    }

    public static SlotRef None => new SlotRef(InventoryArea.None, -1);
    public static SlotRef Hand => new SlotRef(InventoryArea.Hand, 0);
    public static SlotRef Drawer => new SlotRef(InventoryArea.Drawer, 0);
    public static SlotRef Display => new SlotRef(InventoryArea.Display, 0);
    public static SlotRef Hotbar(int index) => new SlotRef(InventoryArea.Hotbar, index);
    public static SlotRef Bag(int index) => new SlotRef(InventoryArea.Bag, index);

    public bool IsSlot => Area == InventoryArea.Hotbar || Area == InventoryArea.Bag;
    public bool IsNone => Area == InventoryArea.None;

    public bool Equals(SlotRef other) => Area == other.Area && (!IsSlot || Index == other.Index);
    public override bool Equals(object obj) => obj is SlotRef other && Equals(other);
    public override int GetHashCode() => ((int)Area * 397) ^ (IsSlot ? Index : 0);
    public static bool operator ==(SlotRef a, SlotRef b) => a.Equals(b);
    public static bool operator !=(SlotRef a, SlotRef b) => !a.Equals(b);

    /// <summary>"hotbar:0", "bag:3", "hand", "drawer" or "".</summary>
    public override string ToString()
    {
        switch (Area)
        {
            case InventoryArea.Hotbar: return "hotbar:" + Index;
            case InventoryArea.Bag: return "bag:" + Index;
            case InventoryArea.Hand: return "hand";
            case InventoryArea.Drawer: return "drawer";
            case InventoryArea.Display: return "display";
            default: return "";
        }
    }

    public static bool TryParse(string text, out SlotRef slot)
    {
        slot = None;
        if (string.IsNullOrWhiteSpace(text)) return false;
        string value = text.Trim();
        if (value == "hand") { slot = Hand; return true; }
        if (value == "drawer") { slot = Drawer; return true; }
        int colon = value.IndexOf(':');
        if (colon <= 0 || !int.TryParse(value.Substring(colon + 1), out int index) || index < 0) return false;
        string area = value.Substring(0, colon);
        if (area == "hotbar") { slot = Hotbar(index); return true; }
        if (area == "bag") { slot = Bag(index); return true; }
        return false;
    }
}

/// <summary>
/// One kind of item the campaign can own (T10). Data only: the view/action
/// adapter is chosen by id in the HUD (no item scripting language).
/// </summary>
[Serializable]
public class InventoryItemDefinition
{
    public string id = "";
    public string label = "";
    /// <summary>One of InventoryCatalogue.Categories.</summary>
    public string category = "";
    /// <summary>HudIconArt picture key ("notes", "folder", "profiles", "book", ...).</summary>
    public string icon = "";
    /// <summary>What it does, for the press-and-hold details card.</summary>
    public string description = "";
    /// <summary>Never stored in the drawer, sold, discarded or left behind.</summary>
    public bool required;
    public int stackLimit = 1;
    /// <summary>One slot holds everything of this kind (profiles in the binder, case papers in the folder).</summary>
    public bool container;
    /// <summary>The story level the item belongs to ("Level_01"); "" = the whole campaign.</summary>
    public string scope = "";
    /// <summary>Closing "Make room" with it in hand can put it back where it came from (the desk book).</summary>
    public bool returnsToWorld;
    /// <summary>T11: at most this many carried (bar, bag and hand together); more are refused. 0 = no limit.</summary>
    public int carryLimit;
    /// <summary>T30: a read-only reference kept for good (Arthur's Reviewed File): never edited, submitted or needed by a case.</summary>
    public bool reference;
}

/// <summary>A neutral story fact the evidence journal can list once learned, with its source record.</summary>
[Serializable]
public class InventoryJournalFact
{
    public string id = "";
    public string text = "";
    public string sourceItemId = "";
}

/// <summary>Item definitions and journal facts (Resources/Campaign/inventory.json; BuiltIn() is the same data).</summary>
[Serializable]
public class InventoryCatalogue
{
    public const string Profiles = "Profiles";
    public const string CaseFiles = "Case Files";
    public const string ToolsAndSupplies = "Tools & Supplies";
    public const string Keepsakes = "Keepsakes";
    public static readonly string[] Categories = { Profiles, CaseFiles, ToolsAndSupplies, Keepsakes };

    public const string Notes = "notes";
    public const string CaseFolder = "case_folder";
    public const string ProfileBinder = "profile_binder";
    public const string TaxCodeBook = "tax_code_book";
    public const string Postcard = "coast_postcard";
    public const string ReviewVoucher = "review_voucher";
    public const string BookmarkSet = "bookmark_set";
    /// <summary>T30: Arthur's Reviewed File (ReviewedFileRules.ItemId).</summary>
    public const string ReviewedFile = "arthur_reviewed_file";

    public List<InventoryItemDefinition> items = new List<InventoryItemDefinition>();
    public List<InventoryJournalFact> journal = new List<InventoryJournalFact>();

    public InventoryItemDefinition Find(string id)
    {
        if (string.IsNullOrWhiteSpace(id) || items == null) return null;
        foreach (InventoryItemDefinition item in items)
            if (item != null && item.id == id) return item;
        return null;
    }

    public bool Knows(string id) => Find(id) != null;
    public bool IsRequired(string id) => Find(id)?.required == true;
    public int StackLimit(string id) => Math.Max(1, Find(id)?.stackLimit ?? 1);
    public string Label(string id) => Find(id)?.label is string label && label.Length > 0 ? label : id ?? "";

    /// <summary>Authoring problems (blank/duplicate ids, unknown categories, bad stacks, unknown journal sources).</summary>
    public List<string> Problems()
    {
        var problems = new List<string>();
        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (InventoryItemDefinition item in items ?? new List<InventoryItemDefinition>())
        {
            if (item == null || string.IsNullOrWhiteSpace(item.id)) { problems.Add("an item is missing its id"); continue; }
            if (!ids.Add(item.id)) problems.Add($"duplicate item id \"{item.id}\"");
            if (string.IsNullOrWhiteSpace(item.label)) problems.Add($"item \"{item.id}\" has no label");
            if (Array.IndexOf(Categories, item.category) < 0) problems.Add($"item \"{item.id}\" has unknown category \"{item.category}\"");
            if (item.stackLimit < 1) problems.Add($"item \"{item.id}\" has stack limit {item.stackLimit} (at least 1)");
            if (item.container && item.stackLimit != 1) problems.Add($"container \"{item.id}\" cannot stack");
            if (item.required && item.stackLimit != 1) problems.Add($"required item \"{item.id}\" cannot stack");
            if (item.carryLimit < 0) problems.Add($"item \"{item.id}\" has carry limit {item.carryLimit}");
            if (item.required && item.carryLimit > 0) problems.Add($"required item \"{item.id}\" cannot have a carry limit (it could be refused)");
            if (item.reference && (!item.required || !string.IsNullOrEmpty(item.scope)))
                problems.Add($"reference \"{item.id}\" must be required (never discarded) and kept for the whole campaign (scope \"\")");
        }
        var factIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (InventoryJournalFact fact in journal ?? new List<InventoryJournalFact>())
        {
            if (fact == null || string.IsNullOrWhiteSpace(fact.id)) { problems.Add("a journal fact is missing its id"); continue; }
            if (!factIds.Add(fact.id)) problems.Add($"duplicate journal fact \"{fact.id}\"");
            if (string.IsNullOrWhiteSpace(fact.text)) problems.Add($"journal fact \"{fact.id}\" has no text");
            if (!string.IsNullOrEmpty(fact.sourceItemId) && !ids.Contains(fact.sourceItemId))
                problems.Add($"journal fact \"{fact.id}\" names unknown source \"{fact.sourceItemId}\"");
        }
        return problems;
    }

    /// <summary>The shipped catalogue (identical to Resources/Campaign/inventory.json), used when the file is missing or broken.</summary>
    public static InventoryCatalogue BuiltIn()
    {
        var catalogue = new InventoryCatalogue();
        catalogue.items.Add(new InventoryItemDefinition
        {
            id = ProfileBinder, label = "Client IDs", category = Profiles, icon = "profiles", required = true, container = true, scope = "Level_01",
            description = "Every taxpayer profile you collect, kept together in one binder."
        });
        catalogue.items.Add(new InventoryItemDefinition
        {
            id = Notes, label = "Notes", category = ToolsAndSupplies, icon = "notes", required = true, scope = "Level_01",
            description = "Today's objectives and every step, ticked off as you go."
        });
        catalogue.items.Add(new InventoryItemDefinition
        {
            id = CaseFolder, label = "Case Folder", category = CaseFiles, icon = "folder", required = true, container = true, scope = "Level_01",
            description = "Your client's case papers. Hand it to the Auditor from your bar."
        });
        catalogue.items.Add(new InventoryItemDefinition
        {
            id = TaxCodeBook, label = "Tax Code Book", category = ToolsAndSupplies, icon = "book", scope = "Level_01", returnsToWorld = true,
            description = "The rules, section by section. Read it anywhere; it can go back on its desk stand."
        });
        catalogue.items.Add(new InventoryItemDefinition
        {
            id = Postcard, label = "Coast Tales Postcard", category = Keepsakes, icon = "postcard",
            description = "A postcard of the island coast with a short note on the back. Display it on the stand at your desk."
        });
        catalogue.items.Add(new InventoryItemDefinition
        {
            id = ReviewVoucher, label = "Review Voucher", category = ToolsAndSupplies, icon = "voucher", stackLimit = 2, carryLimit = 2,
            description = "A folded green ticket with a copper clock seal: +180 REVIEW SECONDS on one case, before the Auditor leaves. One per case; you can carry two."
        });
        catalogue.items.Add(new InventoryItemDefinition
        {
            id = BookmarkSet, label = "Bookmark Set", category = ToolsAndSupplies, icon = "bookmarks",
            description = "Three leather ribbon pins. Pin up to three records to the top of your Bag; every record stays free to read."
        });
        catalogue.items.Add(new InventoryItemDefinition
        {
            id = ReviewedFile, label = "Arthur's Reviewed File", category = CaseFiles, icon = "reviewed", required = true, container = true, reference = true,
            description = "A finished file Arthur reviewed for a former client: completed entries, then the ID and background behind them. Read only; yours to keep."
        });
        catalogue.journal.Add(new InventoryJournalFact { id = "met-npc_marie", text = "You met Marie Bautista in the plaza and took a copy of her ID.", sourceItemId = ProfileBinder });
        catalogue.journal.Add(new InventoryJournalFact { id = "met-npc_jhunjhun", text = "You met Jhun-jhun in the plaza and took a copy of his ID.", sourceItemId = ProfileBinder });
        catalogue.journal.Add(new InventoryJournalFact { id = "met-npc_milo", text = "You met Milo in the plaza and took a copy of his ID.", sourceItemId = ProfileBinder });
        catalogue.journal.Add(new InventoryJournalFact { id = "met-npc_zorbix", text = "You met Zorbix in the plaza and took a copy of his ID.", sourceItemId = ProfileBinder });
        catalogue.journal.Add(new InventoryJournalFact { id = "case-received", text = "The receptionist handed you Marie's case for the day.", sourceItemId = CaseFolder });
        catalogue.journal.Add(new InventoryJournalFact { id = "client-copy", text = "The Auditor gave you Marie's client copy to deliver.", sourceItemId = CaseFolder });
        return catalogue;
    }
}

/// <summary>What an inventory command did.</summary>
public enum InventoryOutcome { Applied, NoChange, AlreadyApplied, NeedsRoom, Rejected, SaveFailed }

/// <summary>The result of one inventory command, with presentation facts for observers.</summary>
public sealed class InventoryResult
{
    public InventoryOutcome Outcome = InventoryOutcome.NoChange;
    /// <summary>Player-facing reason for a refusal ("Needed for your case").</summary>
    public string Message = "";
    public string ItemId = "";
    /// <summary>Where the item ended up.</summary>
    public SlotRef Location = SlotRef.None;
    /// <summary>An item pushed out of its slot by a swap, and where it went.</summary>
    public string DisplacedItemId = "";
    public SlotRef DisplacedTo = SlotRef.None;
    /// <summary>A new or displaced item landed in the bag because the bar had no free slot (the bag lesson).</summary>
    public bool WentToBagBecauseBarFull;
    /// <summary>An item came from the bag into the bar (completes the bag lesson).</summary>
    public bool MovedBagToHotbar;

    public bool Changed => Outcome == InventoryOutcome.Applied || Outcome == InventoryOutcome.NeedsRoom;
    public bool Ok => Outcome == InventoryOutcome.Applied || Outcome == InventoryOutcome.NoChange || Outcome == InventoryOutcome.AlreadyApplied;

    public static InventoryResult Reject(string message) => new InventoryResult { Outcome = InventoryOutcome.Rejected, Message = message ?? "" };
}

/// <summary>
/// PURPOSE:
/// The campaign inventory commands (T10): Acquire, Remove, Move/Swap,
/// QuickMove, TakeOut, the "Make room" hand and
/// UpgradeCapacity, plus catalogue normalization and the required-capacity
/// validator. Pure C#: each command changes the record it is given (the save
/// owner passes a candidate copy and writes it before publishing), refuses an
/// invalid request without changing anything, and treats a repeated action
/// id as already applied.
///
/// RULES:
/// - Auto-placement: first empty hotbar slot, then first empty bag slot, then
///   an existing stack with room (ticket order), else the hand ("Make room").
/// - A drop on an occupied hotbar slot swaps; the previous item goes to the
///   bag, or to the hand when the bag is full.
/// - Required items cannot leave the hand except into a slot. Non-stackable
///   items are owned once. Past an item's carry limit more is refused.
/// - The Desk Drawer was removed from play (2026-10-25). The record keeps
///   its drawer list only as save-repair overflow; Normalize moves anything
///   in it back into free slots.
/// - Locked slots (index at or above capacity) refuse items.
/// </summary>
public static class InventoryRules
{
    public const string NeededForCase = "Needed for your case";
    public const string ShopUpgrade = "Shop upgrade";
    public const string AtYourDesk = "At your desk";
    /// <summary>T14: the carry limit is reached (two vouchers carried).</summary>
    public const string CarryFull = "You can't carry more of these";
    public const string NoFreeSlot = "No free slot";
    public const string BagFull = "Your bar and bag are full";
    public const string HandSourcePickup = "pickup";
    public const string HandSourceDisplaced = "displaced";

    // =========================================================
    // QUERIES
    // =========================================================

    public static InventorySlot SlotAt(CampaignInventoryRecord r, SlotRef slot)
    {
        if (r == null) return null;
        switch (slot.Area)
        {
            case InventoryArea.Hotbar: return slot.Index >= 0 && slot.Index < r.hotbar.Count ? r.hotbar[slot.Index] : null;
            case InventoryArea.Bag: return slot.Index >= 0 && slot.Index < r.bag.Count ? r.bag[slot.Index] : null;
            case InventoryArea.Hand: return r.hand;
            default: return null;
        }
    }

    /// <summary>An unlocked slot (inside its area's capacity) or the hand.</summary>
    public static bool IsUsable(CampaignInventoryRecord r, SlotRef slot) =>
        slot.Area == InventoryArea.Hand ||
        (slot.Area == InventoryArea.Hotbar && slot.Index >= 0 && slot.Index < r.hotbarCapacity && slot.Index < r.hotbar.Count) ||
        (slot.Area == InventoryArea.Bag && slot.Index >= 0 && slot.Index < r.bagCapacity && slot.Index < r.bag.Count);

    /// <summary>The first hotbar/bag/hand slot holding the item (None when it is not carried).</summary>
    public static SlotRef Find(CampaignInventoryRecord r, string itemId)
    {
        if (r == null || string.IsNullOrEmpty(itemId)) return SlotRef.None;
        for (int i = 0; i < r.hotbar.Count; i++) if (r.hotbar[i].itemId == itemId && !r.hotbar[i].IsEmpty) return SlotRef.Hotbar(i);
        for (int i = 0; i < r.bag.Count; i++) if (r.bag[i].itemId == itemId && !r.bag[i].IsEmpty) return SlotRef.Bag(i);
        if (r.hand.itemId == itemId && !r.hand.IsEmpty) return SlotRef.Hand;
        return SlotRef.None;
    }

    public static int DrawerCount(CampaignInventoryRecord r, string itemId)
    {
        InventoryDrawerEntry entry = r?.drawer?.Find(e => e != null && e.itemId == itemId);
        return entry != null ? Math.Max(0, entry.quantity) : 0;
    }

    /// <summary>Carried (hotbar, bag or hand), in the drawer, or on display (T11).</summary>
    public static bool Owns(CampaignInventoryRecord r, string itemId) =>
        !Find(r, itemId).IsNone || DrawerCount(r, itemId) > 0 || IsDisplayed(r, itemId);

    /// <summary>How many are carried: bar, bag and hand together (the carry limit counts these).</summary>
    public static int CarriedCount(CampaignInventoryRecord r, string itemId)
    {
        if (r == null || string.IsNullOrEmpty(itemId)) return 0;
        long total = 0;
        foreach (InventorySlot slot in r.hotbar) if (slot.itemId == itemId) total += Math.Max(0, slot.quantity);
        foreach (InventorySlot slot in r.bag) if (slot.itemId == itemId) total += Math.Max(0, slot.quantity);
        if (r.hand.itemId == itemId) total += Math.Max(0, r.hand.quantity);
        return (int)Math.Min(int.MaxValue, total);
    }

    /// <summary>How many of a new acquisition are past the carry limit and would be refused (0 = all fit the limit).</summary>
    public static int OverCarryLimit(CampaignInventoryRecord r, InventoryCatalogue c, string itemId, int quantity)
    {
        InventoryItemDefinition def = c?.Find(itemId);
        if (def == null || def.carryLimit <= 0 || quantity <= 0) return 0;
        int room = Math.Max(0, def.carryLimit - CarriedCount(r, itemId));
        return Math.Max(0, quantity - room);
    }

    public static bool IsDisplayed(CampaignInventoryRecord r, string itemId) =>
        r?.displays != null && !string.IsNullOrEmpty(itemId) && r.displays.Exists(d => d != null && d.itemId == itemId);

    /// <summary>The keepsake on an anchor ("" when it is empty).</summary>
    public static string DisplayedAt(CampaignInventoryRecord r, string anchorId)
    {
        InventoryDisplay display = r?.displays?.Find(d => d != null && d.anchorId == anchorId);
        return display != null ? display.itemId : "";
    }

    public static bool InHotbar(CampaignInventoryRecord r, string itemId) => Find(r, itemId).Area == InventoryArea.Hotbar;

    public static int FirstEmpty(List<InventorySlot> slots, int capacity)
    {
        for (int i = 0; i < slots.Count && i < capacity; i++) if (slots[i].IsEmpty) return i;
        return -1;
    }

    public static InventoryItemRecord Record(CampaignInventoryRecord r, string itemId) =>
        r?.items?.Find(i => i != null && i.itemId == itemId);

    public static bool Applied(CampaignInventoryRecord r, string actionId) =>
        !string.IsNullOrWhiteSpace(actionId) && r.appliedActionIds.Contains(actionId.Trim());

    // =========================================================
    // ACQUIRE / REMOVE
    // =========================================================

    /// <summary>
    /// A new item arrives (world pickup, handover, purchase). preferred: the
    /// hotbar slot it was dropped on (swap if occupied), or (T30) an empty Bag
    /// slot it should go straight into; else auto-placement. A non-stackable
    /// item already owned is a no-change. Full: it is held in the hand and the
    /// outcome is NeedsRoom.
    /// </summary>
    public static InventoryResult Acquire(CampaignInventoryRecord r, InventoryCatalogue c, string itemId, int quantity, string actionId,
        SlotRef? preferred = null, InventoryItemRecord info = null)
    {
        if (r == null || c == null) return InventoryResult.Reject("No inventory");
        InventoryItemDefinition def = c.Find(itemId);
        if (def == null) return InventoryResult.Reject("Unknown item");
        if (quantity < 1) return InventoryResult.Reject("Nothing to add");
        if (Applied(r, actionId)) return new InventoryResult { Outcome = InventoryOutcome.AlreadyApplied, ItemId = itemId, Location = Find(r, itemId) };
        int limit = Math.Max(1, def.stackLimit);
        if (limit == 1 && Owns(r, itemId))
            return new InventoryResult { Outcome = InventoryOutcome.NoChange, ItemId = itemId, Location = Find(r, itemId) };
        if (limit == 1) quantity = 1;

        var result = new InventoryResult { Outcome = InventoryOutcome.Applied, ItemId = itemId };
        // Past the carry limit nothing more is taken (vouchers: carry two); only what fits the limit is added.
        int over = OverCarryLimit(r, c, itemId, quantity);
        if (over >= quantity) return InventoryResult.Reject(CarryFull);
        quantity -= over;
        bool barHadRoom = FirstEmpty(r.hotbar, r.hotbarCapacity) >= 0;

        if (preferred.HasValue && preferred.Value.Area == InventoryArea.Hotbar && IsUsable(r, preferred.Value))
        {
            InventorySlot target = SlotAt(r, preferred.Value);
            if (target.IsEmpty)
            {
                Put(target, itemId, quantity);
                result.Location = preferred.Value;
            }
            else if (target.itemId == itemId && target.quantity < limit && target.quantity + quantity <= limit)
            {
                target.quantity += quantity;
                result.Location = preferred.Value;
            }
            else
            {
                // Swap: the previous item goes to the bag (or the hand when the bag is full).
                string oldId = target.itemId;
                int oldQuantity = target.quantity;
                if (!r.hand.IsEmpty && FirstEmpty(r.bag, r.bagCapacity) < 0) return InventoryResult.Reject("Make room first");
                Put(target, itemId, quantity);
                result.Location = preferred.Value;
                result.DisplacedItemId = oldId;
                int bagIndex = FirstEmpty(r.bag, r.bagCapacity);
                if (bagIndex >= 0)
                {
                    Put(r.bag[bagIndex], oldId, oldQuantity);
                    result.DisplacedTo = SlotRef.Bag(bagIndex);
                    result.WentToBagBecauseBarFull = true;
                }
                else
                {
                    Put(r.hand, oldId, oldQuantity);
                    r.handSource = HandSourceDisplaced;
                    result.DisplacedTo = SlotRef.Hand;
                    result.Outcome = InventoryOutcome.NeedsRoom;
                }
            }
        }
        else if (preferred.HasValue && preferred.Value.Area == InventoryArea.Bag && IsUsable(r, preferred.Value) && SlotAt(r, preferred.Value).IsEmpty)
        {
            Put(SlotAt(r, preferred.Value), itemId, quantity);
            result.Location = preferred.Value;
        }
        else
        {
            if (!r.hand.IsEmpty && !Fits(r, c, itemId, quantity)) return InventoryResult.Reject("Make room first");
            SlotRef placed = AutoPlace(r, c, itemId, quantity, out int left);
            if (placed.IsNone || left > 0)
            {
                Put(r.hand, itemId, placed.IsNone ? quantity : left);
                r.handSource = HandSourcePickup;
                result.Location = SlotRef.Hand;
                result.Outcome = InventoryOutcome.NeedsRoom;
            }
            else
            {
                result.Location = placed;
                result.WentToBagBecauseBarFull = placed.Area == InventoryArea.Bag && !barHadRoom;
            }
        }

        MergeRecord(r, itemId, info);
        MarkApplied(r, actionId);
        return result;
    }

    /// <summary>
    /// A new item where no hand can hold it (the menu shop, T13): a slot when
    /// one fits (as Acquire), otherwise refused ("Your bar and bag are full").
    /// Never NeedsRoom, so nothing waits for a Make room panel that is not there.
    /// </summary>
    public static InventoryResult AcquireWithoutHand(CampaignInventoryRecord r, InventoryCatalogue c, string itemId, int quantity, string actionId,
        InventoryItemRecord info = null)
    {
        if (r == null || c == null) return InventoryResult.Reject("No inventory");
        InventoryItemDefinition def = c.Find(itemId);
        if (def == null) return InventoryResult.Reject("Unknown item");
        if (quantity < 1) return InventoryResult.Reject("Nothing to add");
        int limit = Math.Max(1, def.stackLimit);
        if (Applied(r, actionId) || (limit == 1 && Owns(r, itemId)) || Fits(r, c, itemId, Math.Min(quantity, limit)))
            return Acquire(r, c, itemId, quantity, actionId, null, info);
        return InventoryResult.Reject(BagFull);
    }

    /// <summary>
    /// T14: one carried unit is used up (a review voucher applied to a case):
    /// from the bag first (the paper leaves the bag), then the bar, then the
    /// hand. Drawer stock is never used directly - it is taken out first.
    /// </summary>
    public static InventoryResult ConsumeCarried(CampaignInventoryRecord r, InventoryCatalogue c, string itemId, string actionId)
    {
        if (r == null || c == null) return InventoryResult.Reject("No inventory");
        if (Applied(r, actionId)) return new InventoryResult { Outcome = InventoryOutcome.AlreadyApplied, ItemId = itemId };
        SlotRef from = SlotRef.None;
        for (int i = r.bag.Count - 1; i >= 0 && from.IsNone; i--) if (r.bag[i].itemId == itemId && !r.bag[i].IsEmpty) from = SlotRef.Bag(i);
        for (int i = r.hotbar.Count - 1; i >= 0 && from.IsNone; i--) if (r.hotbar[i].itemId == itemId && !r.hotbar[i].IsEmpty) from = SlotRef.Hotbar(i);
        if (from.IsNone && r.hand.itemId == itemId && !r.hand.IsEmpty) from = SlotRef.Hand;
        if (from.IsNone) return InventoryResult.Reject("You don't carry one");
        InventorySlot slot = from.Area == InventoryArea.Hand ? r.hand : SlotAt(r, from);
        slot.quantity -= 1;
        if (slot.quantity <= 0)
        {
            slot.Clear();
            if (from.Area == InventoryArea.Hand) r.handSource = "";
        }
        MarkApplied(r, actionId);
        return new InventoryResult { Outcome = InventoryOutcome.Applied, ItemId = itemId, Location = from };
    }

    /// <summary>The item goes back to the world (folder to the desk, book on its stand, copy to its client): every copy leaves.</summary>
    public static InventoryResult Remove(CampaignInventoryRecord r, InventoryCatalogue c, string itemId, string actionId)
    {
        if (r == null) return InventoryResult.Reject("No inventory");
        if (Applied(r, actionId)) return new InventoryResult { Outcome = InventoryOutcome.AlreadyApplied, ItemId = itemId };
        if (!Owns(r, itemId)) return new InventoryResult { Outcome = InventoryOutcome.NoChange, ItemId = itemId };
        SlotRef was = Find(r, itemId);
        foreach (InventorySlot slot in r.hotbar) if (slot.itemId == itemId) slot.Clear();
        foreach (InventorySlot slot in r.bag) if (slot.itemId == itemId) slot.Clear();
        if (r.hand.itemId == itemId) { r.hand.Clear(); r.handSource = ""; }
        r.drawer.RemoveAll(e => e != null && e.itemId == itemId);
        r.displays.RemoveAll(d => d != null && d.itemId == itemId);
        r.items.RemoveAll(i => i != null && i.itemId == itemId);
        MarkApplied(r, actionId);
        return new InventoryResult { Outcome = InventoryOutcome.Applied, ItemId = itemId, Location = was };
    }

    /// <summary>Updates what a container holds (profile ids, case papers, the Client Copy) without moving it.</summary>
    public static InventoryResult SetContents(CampaignInventoryRecord r, string itemId, IEnumerable<string> contents)
    {
        InventoryItemRecord record = Record(r, itemId);
        if (record == null) return new InventoryResult { Outcome = InventoryOutcome.NoChange, ItemId = itemId };
        var next = new List<string>();
        if (contents != null)
            foreach (string id in contents)
                if (!string.IsNullOrWhiteSpace(id) && !next.Contains(id.Trim())) next.Add(id.Trim());
        if (SameList(next, record.contents)) return new InventoryResult { Outcome = InventoryOutcome.NoChange, ItemId = itemId };
        record.contents = next;
        return new InventoryResult { Outcome = InventoryOutcome.Applied, ItemId = itemId, Location = Find(r, itemId) };
    }

    // =========================================================
    // MOVE / SWAP / QUICK MOVE
    // =========================================================

    /// <summary>Moves an item between slots (and the hand): empty target = move, same stack = merge up to the limit, otherwise swap.</summary>
    public static InventoryResult Move(CampaignInventoryRecord r, InventoryCatalogue c, SlotRef from, SlotRef to, string actionId)
    {
        if (r == null || c == null) return InventoryResult.Reject("No inventory");
        if (Applied(r, actionId)) return new InventoryResult { Outcome = InventoryOutcome.AlreadyApplied };
        if (!IsUsable(r, from)) return InventoryResult.Reject("Pick an item");
        if (!IsUsable(r, to)) return InventoryResult.Reject(to.IsSlot ? ShopUpgrade : "Pick a slot");
        if (to.Area == InventoryArea.Hand) return InventoryResult.Reject("Pick a slot");
        InventorySlot source = SlotAt(r, from);
        InventorySlot target = SlotAt(r, to);
        if (source.IsEmpty) return InventoryResult.Reject("Pick an item");
        if (from == to) return new InventoryResult { Outcome = InventoryOutcome.NoChange, ItemId = source.itemId, Location = from };

        var result = new InventoryResult { Outcome = InventoryOutcome.Applied, ItemId = source.itemId, Location = to };
        int limit = c.StackLimit(source.itemId);
        if (target.IsEmpty)
        {
            Put(target, source.itemId, source.quantity);
            source.Clear();
        }
        else if (target.itemId == source.itemId && limit > 1)
        {
            int room = limit - target.quantity;
            if (room <= 0) return InventoryResult.Reject("That stack is full");
            int moved = Math.Min(room, source.quantity);
            target.quantity += moved;
            source.quantity -= moved;
            if (source.quantity <= 0) source.Clear();
        }
        else
        {
            string otherId = target.itemId;
            int otherQuantity = target.quantity;
            Put(target, source.itemId, source.quantity);
            Put(source, otherId, otherQuantity);
            result.DisplacedItemId = otherId;
            result.DisplacedTo = from;
            if (from.Area == InventoryArea.Hand) r.handSource = HandSourceDisplaced;
            if (from.Area == InventoryArea.Hotbar && to.Area == InventoryArea.Bag) result.MovedBagToHotbar = true;
        }
        if (from.Area == InventoryArea.Hand && r.hand.IsEmpty) r.handSource = "";
        if (from.Area == InventoryArea.Bag && to.Area == InventoryArea.Hotbar) result.MovedBagToHotbar = true;
        MarkApplied(r, actionId);
        return result;
    }

    /// <summary>Minecraft's shift-click: bar to the first free bag slot (stacks first), bag or hand to the first free bar slot.</summary>
    public static InventoryResult QuickMove(CampaignInventoryRecord r, InventoryCatalogue c, SlotRef from, string actionId)
    {
        if (r == null || c == null) return InventoryResult.Reject("No inventory");
        if (Applied(r, actionId)) return new InventoryResult { Outcome = InventoryOutcome.AlreadyApplied };
        InventorySlot source = IsUsable(r, from) ? SlotAt(r, from) : null;
        if (source == null || source.IsEmpty) return InventoryResult.Reject("Pick an item");
        SlotRef to = SlotRef.None;
        if (from.Area == InventoryArea.Hotbar)
            to = StackOrEmpty(r, c, r.bag, r.bagCapacity, InventoryArea.Bag, source.itemId);
        else
        {
            to = StackOrEmpty(r, c, r.hotbar, r.hotbarCapacity, InventoryArea.Hotbar, source.itemId);
            if (to.IsNone && from.Area == InventoryArea.Hand) to = StackOrEmpty(r, c, r.bag, r.bagCapacity, InventoryArea.Bag, source.itemId);
        }
        if (to.IsNone) return InventoryResult.Reject(NoFreeSlot);
        return Move(r, c, from, to, actionId);
    }

    /// <summary>"Take it out": a bag item goes into the bar, swapping with a bar item when the bar is full.</summary>
    public static InventoryResult TakeOut(CampaignInventoryRecord r, InventoryCatalogue c, string itemId, string actionId)
    {
        if (r == null || c == null) return InventoryResult.Reject("No inventory");
        if (Applied(r, actionId)) return new InventoryResult { Outcome = InventoryOutcome.AlreadyApplied, ItemId = itemId };
        SlotRef at = Find(r, itemId);
        if (at.Area == InventoryArea.Hotbar) return new InventoryResult { Outcome = InventoryOutcome.NoChange, ItemId = itemId, Location = at };
        if (at.Area != InventoryArea.Bag && at.Area != InventoryArea.Hand) return InventoryResult.Reject("You don't have it");
        int free = FirstEmpty(r.hotbar, r.hotbarCapacity);
        if (free >= 0) return Move(r, c, at, SlotRef.Hotbar(free), actionId);
        // Swap with the last bar item that is not required (else the last slot): a true swap, no room needed.
        return Move(r, c, at, SlotRef.Hotbar(SwapCandidate(r, c, allowRequired: true)), actionId);
    }

    // =========================================================
    // MAKE ROOM (the hand)
    // =========================================================

    /// <summary>
    /// Closing "Make room" with something still in hand. A required item
    /// cannot be left behind (refused). A new pickup that can go back to its
    /// world source (returnToWorld) leaves the inventory; anything else takes
    /// a free slot, and with none free it stays in hand (refused).
    /// </summary>
    public static InventoryResult ReleaseHand(CampaignInventoryRecord r, InventoryCatalogue c, bool returnToWorld, string actionId)
    {
        if (r == null || c == null) return InventoryResult.Reject("No inventory");
        if (r.hand.IsEmpty) return new InventoryResult { Outcome = InventoryOutcome.NoChange };
        if (Applied(r, actionId)) return new InventoryResult { Outcome = InventoryOutcome.AlreadyApplied };
        string id = r.hand.itemId;
        if (c.IsRequired(id)) return InventoryResult.Reject(NeededForCase);
        bool backToWorld = returnToWorld && r.handSource == HandSourcePickup && c.Find(id)?.returnsToWorld == true;
        if (backToWorld)
        {
            InventoryResult removed = Remove(r, c, id, actionId);
            removed.Location = SlotRef.None;
            return removed;
        }
        SlotRef to = StackOrEmpty(r, c, r.hotbar, r.hotbarCapacity, InventoryArea.Hotbar, id);
        if (to.IsNone) to = StackOrEmpty(r, c, r.bag, r.bagCapacity, InventoryArea.Bag, id);
        if (to.IsNone) return InventoryResult.Reject(BagFull);
        return Move(r, c, SlotRef.Hand, to, actionId);
    }

    // =========================================================
    // OFFICE DISPLAY (T11 keepsakes)
    // =========================================================

    /// <summary>
    /// Puts an owned keepsake on an office display anchor (from a slot, the
    /// hand or the drawer). Only Keepsakes; one per anchor. It stays owned and
    /// leaves its slot.
    /// </summary>
    public static InventoryResult Display(CampaignInventoryRecord r, InventoryCatalogue c, string itemId, string anchorId, string actionId)
    {
        if (r == null || c == null) return InventoryResult.Reject("No inventory");
        if (Applied(r, actionId)) return new InventoryResult { Outcome = InventoryOutcome.AlreadyApplied, ItemId = itemId };
        InventoryItemDefinition def = c.Find(itemId);
        if (def == null || def.category != InventoryCatalogue.Keepsakes) return InventoryResult.Reject("Only keepsakes go on display");
        if (string.IsNullOrWhiteSpace(anchorId)) return InventoryResult.Reject("Pick a display spot");
        if (DisplayedAt(r, anchorId) == itemId) return new InventoryResult { Outcome = InventoryOutcome.NoChange, ItemId = itemId, Location = SlotRef.Display };
        if (DisplayedAt(r, anchorId).Length > 0) return InventoryResult.Reject("Something is already on display there");
        if (IsDisplayed(r, itemId)) return InventoryResult.Reject("It is already on display");
        SlotRef at = Find(r, itemId);
        if (!at.IsNone)
        {
            InventorySlot slot = SlotAt(r, at);
            slot.quantity -= 1;
            if (slot.quantity <= 0) slot.Clear();
            if (at.Area == InventoryArea.Hand && r.hand.IsEmpty) r.handSource = "";
        }
        else if (DrawerCount(r, itemId) > 0) TakeFromDrawer(r, itemId, 1);
        else return InventoryResult.Reject("You don't have it");
        r.displays.Add(new InventoryDisplay { anchorId = anchorId.Trim(), itemId = itemId });
        MarkApplied(r, actionId);
        return new InventoryResult { Outcome = InventoryOutcome.Applied, ItemId = itemId, Location = SlotRef.Display };
    }

    /// <summary>Takes a keepsake off its display anchor: first free bar slot, then bag (or a stack). With no room it stays on display (refused).</summary>
    public static InventoryResult TakeDown(CampaignInventoryRecord r, InventoryCatalogue c, string anchorId, string actionId)
    {
        if (r == null || c == null) return InventoryResult.Reject("No inventory");
        if (Applied(r, actionId)) return new InventoryResult { Outcome = InventoryOutcome.AlreadyApplied };
        InventoryDisplay display = r.displays.Find(d => d != null && d.anchorId == anchorId);
        if (display == null) return new InventoryResult { Outcome = InventoryOutcome.NoChange };
        string itemId = display.itemId;
        if (!Fits(r, c, itemId, 1)) return InventoryResult.Reject(BagFull);
        r.displays.Remove(display);
        var result = new InventoryResult { Outcome = InventoryOutcome.Applied, ItemId = itemId };
        result.Location = AutoPlace(r, c, itemId, 1, out _);
        MarkApplied(r, actionId);
        return result;
    }

    // =========================================================
    // BOOKMARK PINS (T12)
    // =========================================================

    public const string PinReplace = "Pick a ribbon to replace";

    /// <summary>A record that can carry a pin: owned, one per slot, not the bookmark set itself.</summary>
    public static bool IsPinnable(CampaignInventoryRecord r, InventoryCatalogue c, string itemId) =>
        !string.IsNullOrEmpty(itemId) && itemId != InventoryCatalogue.BookmarkSet && c?.Find(itemId) != null &&
        c.StackLimit(itemId) == 1 && Owns(r, itemId);

    public static bool HasBookmarks(CampaignInventoryRecord r) => Owns(r, InventoryCatalogue.BookmarkSet);

    /// <summary>
    /// Pins a record (needs the bookmark set). With three pins already,
    /// replaceIndex chooses the ribbon to reuse; without it the request is
    /// refused with "Pick a ribbon to replace". Pins are display preference
    /// only: the record itself never moves or changes.
    /// </summary>
    public static InventoryResult Pin(CampaignInventoryRecord r, InventoryCatalogue c, string itemId, int replaceIndex = -1)
    {
        if (r == null || c == null) return InventoryResult.Reject("No inventory");
        if (!HasBookmarks(r)) return InventoryResult.Reject("Buy the Bookmark Set at the Supply Counter first");
        if (!IsPinnable(r, c, itemId)) return InventoryResult.Reject("That can't be pinned");
        if (r.pins.Contains(itemId)) return new InventoryResult { Outcome = InventoryOutcome.NoChange, ItemId = itemId };
        if (r.pins.Count < CampaignInventoryRecord.MaxPins)
            r.pins.Add(itemId);
        else if (replaceIndex >= 0 && replaceIndex < r.pins.Count)
            r.pins[replaceIndex] = itemId;
        else
            return InventoryResult.Reject(PinReplace);
        return new InventoryResult { Outcome = InventoryOutcome.Applied, ItemId = itemId };
    }

    public static InventoryResult Unpin(CampaignInventoryRecord r, string itemId)
    {
        if (r == null || !r.pins.Remove(itemId)) return new InventoryResult { Outcome = InventoryOutcome.NoChange, ItemId = itemId };
        return new InventoryResult { Outcome = InventoryOutcome.Applied, ItemId = itemId };
    }

    // =========================================================
    // CAPACITY AND PRESENTATION STATE
    // =========================================================

    /// <summary>Raises a capacity (never lowers it, capped at 5 / 13); new slots are empty and unlocked at once.</summary>
    public static InventoryResult UpgradeCapacity(CampaignInventoryRecord r, InventoryArea area, int capacity, string actionId)
    {
        if (r == null) return InventoryResult.Reject("No inventory");
        if (Applied(r, actionId)) return new InventoryResult { Outcome = InventoryOutcome.AlreadyApplied };
        if (area == InventoryArea.Hotbar)
        {
            int next = Math.Min(CampaignInventoryRecord.MaxHotbar, capacity);
            if (next <= r.hotbarCapacity) return new InventoryResult { Outcome = InventoryOutcome.NoChange };
            r.hotbarCapacity = next;
            while (r.hotbar.Count < next) r.hotbar.Add(new InventorySlot());
        }
        else if (area == InventoryArea.Bag)
        {
            int next = Math.Min(CampaignInventoryRecord.MaxBag, capacity);
            if (next <= r.bagCapacity) return new InventoryResult { Outcome = InventoryOutcome.NoChange };
            r.bagCapacity = next;
            while (r.bag.Count < next) r.bag.Add(new InventorySlot());
        }
        else return InventoryResult.Reject("Only the bar and the bag grow");
        MarkApplied(r, actionId);
        return new InventoryResult { Outcome = InventoryOutcome.Applied };
    }

    /// <summary>Remembers the inventory cursor (a usable slot, or none).</summary>
    public static InventoryResult SetSelected(CampaignInventoryRecord r, SlotRef slot)
    {
        string value = slot.IsSlot && IsUsable(r, slot) ? slot.ToString() : "";
        if (r.selectedSlot == value) return new InventoryResult { Outcome = InventoryOutcome.NoChange };
        r.selectedSlot = value;
        return new InventoryResult { Outcome = InventoryOutcome.Applied, Location = slot };
    }

    /// <summary>Marks journal facts as seen (presentation state only; never a fact or reward).</summary>
    public static InventoryResult MarkJournalSeen(CampaignInventoryRecord r, IEnumerable<string> factIds)
    {
        bool changed = false;
        if (factIds != null)
            foreach (string id in factIds)
                if (!string.IsNullOrWhiteSpace(id) && !r.journalSeenFactIds.Contains(id.Trim()))
                {
                    r.journalSeenFactIds.Add(id.Trim());
                    changed = true;
                }
        return new InventoryResult { Outcome = changed ? InventoryOutcome.Applied : InventoryOutcome.NoChange };
    }

    /// <summary>The journal lists only catalogue facts that were learned, in catalogue order; reading it creates nothing.</summary>
    public static List<InventoryJournalFact> Journal(InventoryCatalogue c, ICollection<string> learnedFactIds)
    {
        var shown = new List<InventoryJournalFact>();
        if (c?.journal == null || learnedFactIds == null) return shown;
        foreach (InventoryJournalFact fact in c.journal)
            if (fact != null && !string.IsNullOrEmpty(fact.id) && learnedFactIds.Contains(fact.id)) shown.Add(fact);
        return shown;
    }

    // =========================================================
    // NORMALIZE (catalogue) AND CONTENT VALIDATION
    // =========================================================

    /// <summary>
    /// Applies the catalogue to a loaded record: unknown ids become empty
    /// slots (one warning), stacks above their limit spill into free slots or
    /// the drawer, a non-stackable item kept twice keeps its first place,
    /// required items in the drawer or the hand return to the first free slot
    /// (a non-required item steps into the drawer when nothing is free), any
    /// other hand item goes to a free slot or the drawer, and item records
    /// follow ownership. True when anything changed.
    /// </summary>
    public static bool Normalize(CampaignInventoryRecord r, InventoryCatalogue c, List<string> warnings)
    {
        if (r == null || c == null) return false;
        bool changed = r.Repair();
        var unknown = new List<string>();

        void Unknown(string id) { if (!unknown.Contains(id)) unknown.Add(id); }

        foreach (InventorySlot slot in AllSlots(r))
            if (!slot.IsEmpty && !c.Knows(slot.itemId)) { Unknown(slot.itemId); slot.Clear(); changed = true; }
        if (!r.hand.IsEmpty && !c.Knows(r.hand.itemId)) { Unknown(r.hand.itemId); r.hand.Clear(); r.handSource = ""; changed = true; }
        for (int i = r.drawer.Count - 1; i >= 0; i--)
            if (!c.Knows(r.drawer[i].itemId)) { Unknown(r.drawer[i].itemId); r.drawer.RemoveAt(i); changed = true; }
        for (int i = r.displays.Count - 1; i >= 0; i--)
            if (!c.Knows(r.displays[i].itemId)) { Unknown(r.displays[i].itemId); r.displays.RemoveAt(i); changed = true; }
        if (unknown.Count > 0) warnings?.Add("Unknown inventory items were emptied: " + string.Join(", ", unknown));

        // One place for each non-stackable item; stacks within their limit.
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var spill = new List<InventorySlot>();
        foreach (InventorySlot slot in AllSlots(r))
        {
            if (slot.IsEmpty) continue;
            int limit = c.StackLimit(slot.itemId);
            if (limit == 1)
            {
                if (!seen.Add(slot.itemId)) { slot.Clear(); changed = true; continue; }
                if (slot.quantity != 1) { slot.quantity = 1; changed = true; }
            }
            else if (slot.quantity > limit)
            {
                spill.Add(new InventorySlot { itemId = slot.itemId, quantity = slot.quantity - limit });
                slot.quantity = limit;
                changed = true;
            }
        }
        if (!r.hand.IsEmpty && c.StackLimit(r.hand.itemId) == 1)
        {
            if (!seen.Add(r.hand.itemId)) { r.hand.Clear(); r.handSource = ""; changed = true; }
            else if (r.hand.quantity != 1) { r.hand.quantity = 1; changed = true; }
        }
        foreach (InventoryDrawerEntry entry in r.drawer.ToArray())
        {
            if (c.StackLimit(entry.itemId) != 1) continue;
            if (!seen.Add(entry.itemId)) { r.drawer.Remove(entry); changed = true; }
            else if (entry.quantity != 1) { entry.quantity = 1; changed = true; }
        }
        // A displayed keepsake is that item's one place (a copy elsewhere wins: it is kept, the display entry goes).
        foreach (InventoryDisplay display in r.displays.ToArray())
            if (c.Find(display.itemId)?.category != InventoryCatalogue.Keepsakes || (c.StackLimit(display.itemId) == 1 && !seen.Add(display.itemId)))
            {
                r.displays.Remove(display);
                if (!Owns(r, display.itemId)) r.AddToDrawer(display.itemId, 1);
                changed = true;
            }
        foreach (InventorySlot extra in spill)
        {
            int left = extra.quantity;
            while (left > 0)
            {
                int bagIndex = FirstEmpty(r.bag, r.bagCapacity);
                if (bagIndex < 0) { r.AddToDrawer(extra.itemId, left); break; }
                int put = Math.Min(left, c.StackLimit(extra.itemId));
                Put(r.bag[bagIndex], extra.itemId, put);
                left -= put;
            }
        }

        // Required items are always carried.
        foreach (InventoryDrawerEntry entry in r.drawer.ToArray())
        {
            if (!c.IsRequired(entry.itemId)) continue;
            r.drawer.Remove(entry);
            changed = true;
            if (Owns(r, entry.itemId)) continue;
            PlaceCarried(r, c, entry.itemId, 1);
        }
        // Never stuck: the hand is resolved at load.
        if (!r.hand.IsEmpty)
        {
            string id = r.hand.itemId;
            int quantity = r.hand.quantity;
            r.hand.Clear();
            r.handSource = "";
            changed = true;
            if (c.IsRequired(id)) PlaceCarried(r, c, id, quantity);
            else
            {
                SlotRef placed = AutoPlace(r, c, id, quantity, out int left);
                if (placed.IsNone) r.AddToDrawer(id, quantity);
                else if (left > 0) r.AddToDrawer(id, left);
            }
        }
        // The Desk Drawer was removed from play (2026-10-25): what an older save
        // (or a repair above) kept there comes back into free slots, within the
        // carry limit. Only what cannot fit anywhere stays held in the record.
        foreach (InventoryDrawerEntry entry in r.drawer.ToArray())
        {
            InventoryItemDefinition def = c.Find(entry.itemId);
            int room = def != null && def.carryLimit > 0 ? Math.Max(0, def.carryLimit - CarriedCount(r, entry.itemId)) : entry.quantity;
            int moving = Math.Min(entry.quantity, room);
            while (moving > 0)
            {
                SlotRef placed = AutoPlace(r, c, entry.itemId, moving, out int left);
                if (placed.IsNone) break;
                int put = moving - left;
                entry.quantity -= put;
                moving = left;
                changed = true;
            }
            if (entry.quantity <= 0) r.drawer.Remove(entry);
        }

        // Item records follow ownership.
        int removed = r.items.RemoveAll(i => !Owns(r, i.itemId));
        if (removed > 0) changed = true;
        foreach (InventorySlot slot in AllSlots(r))
            if (!slot.IsEmpty && Record(r, slot.itemId) == null) { r.items.Add(new InventoryItemRecord { itemId = slot.itemId }); changed = true; }
        foreach (InventoryDrawerEntry entry in r.drawer)
            if (Record(r, entry.itemId) == null) { r.items.Add(new InventoryItemRecord { itemId = entry.itemId }); changed = true; }
        foreach (InventoryDisplay display in r.displays)
            if (Record(r, display.itemId) == null) { r.items.Add(new InventoryItemRecord { itemId = display.itemId }); changed = true; }

        // Pins only point at records still owned (a lost record is simply unpinned).
        if (r.pins.RemoveAll(id => !IsPinnable(r, c, id)) > 0) changed = true;

        if (SlotRef.TryParse(r.selectedSlot, out SlotRef selected) ? !IsUsable(r, selected) || !selected.IsSlot : r.selectedSlot.Length > 0)
        {
            r.selectedSlot = "";
            changed = true;
        }
        return changed;
    }

    /// <summary>Content rule: the required items one level can hold at once never exceed the starting capacity (3 + 6).</summary>
    public static List<string> RequiredCapacityProblems(InventoryCatalogue c, string levelId)
    {
        var problems = new List<string>();
        if (c == null) return problems;
        int required = 0;
        foreach (InventoryItemDefinition item in c.items)
            if (item != null && item.required && (string.IsNullOrEmpty(item.scope) || item.scope == levelId)) required++;
        int capacity = CampaignInventoryRecord.StartHotbar + CampaignInventoryRecord.StartBag;
        if (required > capacity)
            problems.Add($"{levelId} can need {required} required items at once, more than the starting {capacity} slots");
        return problems;
    }

    // =========================================================
    // HELPERS
    // =========================================================

    private static IEnumerable<InventorySlot> AllSlots(CampaignInventoryRecord r)
    {
        foreach (InventorySlot slot in r.hotbar) yield return slot;
        foreach (InventorySlot slot in r.bag) yield return slot;
    }

    /// <summary>First empty bar slot, then first empty bag slot, then a stack with room; quantity that did not fit is returned in left.</summary>
    private static SlotRef AutoPlace(CampaignInventoryRecord r, InventoryCatalogue c, string itemId, int quantity, out int left)
    {
        left = quantity;
        int limit = c.StackLimit(itemId);
        int hot = FirstEmpty(r.hotbar, r.hotbarCapacity);
        if (hot >= 0)
        {
            int put = Math.Min(limit, left);
            Put(r.hotbar[hot], itemId, put);
            left -= put;
            return SlotRef.Hotbar(hot);
        }
        int bagIndex = FirstEmpty(r.bag, r.bagCapacity);
        if (bagIndex >= 0)
        {
            int put = Math.Min(limit, left);
            Put(r.bag[bagIndex], itemId, put);
            left -= put;
            return SlotRef.Bag(bagIndex);
        }
        SlotRef stack = StackWithRoom(r, c, itemId);
        if (stack.IsNone) return SlotRef.None;
        InventorySlot slot = SlotAt(r, stack);
        int room = Math.Min(left, limit - slot.quantity);
        slot.quantity += room;
        left -= room;
        return stack;
    }

    /// <summary>Auto-placement would take the whole quantity without the hand.</summary>
    private static bool Fits(CampaignInventoryRecord r, InventoryCatalogue c, string itemId, int quantity)
    {
        int limit = c.StackLimit(itemId);
        if (FirstEmpty(r.hotbar, r.hotbarCapacity) >= 0 || FirstEmpty(r.bag, r.bagCapacity) >= 0) return quantity <= limit;
        SlotRef stack = StackWithRoom(r, c, itemId);
        return !stack.IsNone && limit - SlotAt(r, stack).quantity >= quantity;
    }

    /// <summary>Required items always find a slot: a non-required item steps into the drawer when none is free.</summary>
    private static void PlaceCarried(CampaignInventoryRecord r, InventoryCatalogue c, string itemId, int quantity)
    {
        SlotRef placed = AutoPlace(r, c, itemId, quantity, out _);
        if (!placed.IsNone) return;
        for (int pass = 0; pass < 2; pass++)
        {
            List<InventorySlot> slots = pass == 0 ? r.bag : r.hotbar;
            int capacity = pass == 0 ? r.bagCapacity : r.hotbarCapacity;
            for (int i = capacity - 1; i >= 0; i--)
            {
                if (i >= slots.Count || slots[i].IsEmpty || c.IsRequired(slots[i].itemId)) continue;
                r.AddToDrawer(slots[i].itemId, slots[i].quantity);
                Put(slots[i], itemId, quantity);
                return;
            }
        }
    }

    private static SlotRef StackWithRoom(CampaignInventoryRecord r, InventoryCatalogue c, string itemId)
    {
        int limit = c.StackLimit(itemId);
        if (limit <= 1) return SlotRef.None;
        for (int i = 0; i < r.hotbarCapacity && i < r.hotbar.Count; i++)
            if (r.hotbar[i].itemId == itemId && r.hotbar[i].quantity < limit) return SlotRef.Hotbar(i);
        for (int i = 0; i < r.bagCapacity && i < r.bag.Count; i++)
            if (r.bag[i].itemId == itemId && r.bag[i].quantity < limit) return SlotRef.Bag(i);
        return SlotRef.None;
    }

    private static SlotRef StackOrEmpty(CampaignInventoryRecord r, InventoryCatalogue c, List<InventorySlot> slots, int capacity, InventoryArea area, string itemId)
    {
        int limit = c.StackLimit(itemId);
        if (limit > 1)
            for (int i = 0; i < capacity && i < slots.Count; i++)
                if (slots[i].itemId == itemId && slots[i].quantity < limit) return new SlotRef(area, i);
        int free = FirstEmpty(slots, capacity);
        return free >= 0 ? new SlotRef(area, free) : SlotRef.None;
    }

    /// <summary>The last bar slot whose item is not required; allowRequired falls back to the last slot.</summary>
    private static int SwapCandidate(CampaignInventoryRecord r, InventoryCatalogue c, bool allowRequired)
    {
        int last = Math.Min(r.hotbarCapacity, r.hotbar.Count) - 1;
        for (int i = last; i >= 0; i--)
            if (!r.hotbar[i].IsEmpty && !c.IsRequired(r.hotbar[i].itemId)) return i;
        return allowRequired ? last : -1;
    }

    private static void TakeFromDrawer(CampaignInventoryRecord r, string itemId, int quantity)
    {
        InventoryDrawerEntry entry = r.drawer.Find(e => e != null && e.itemId == itemId);
        if (entry == null) return;
        entry.quantity -= quantity;
        if (entry.quantity <= 0) r.drawer.Remove(entry);
    }

    private static void Put(InventorySlot slot, string itemId, int quantity)
    {
        slot.itemId = itemId;
        slot.quantity = quantity;
    }

    private static void MergeRecord(CampaignInventoryRecord r, string itemId, InventoryItemRecord info)
    {
        InventoryItemRecord record = Record(r, itemId);
        if (record == null)
        {
            record = new InventoryItemRecord { itemId = itemId };
            r.items.Add(record);
        }
        if (info == null) return;
        if (!string.IsNullOrEmpty(info.source)) record.source = info.source;
        if (!string.IsNullOrEmpty(info.originalDocumentId)) record.originalDocumentId = info.originalDocumentId;
        if (!string.IsNullOrEmpty(info.workingDocumentId)) record.workingDocumentId = info.workingDocumentId;
        if (info.revision > record.revision) record.revision = info.revision;
        if (info.contents != null)
            foreach (string id in info.contents)
                if (!string.IsNullOrWhiteSpace(id) && !record.contents.Contains(id)) record.contents.Add(id);
        if (info.factIds != null)
            foreach (string id in info.factIds)
                if (!string.IsNullOrWhiteSpace(id) && !record.factIds.Contains(id)) record.factIds.Add(id);
    }

    private static void MarkApplied(CampaignInventoryRecord r, string actionId)
    {
        if (string.IsNullOrWhiteSpace(actionId)) return;
        r.appliedActionIds.Add(actionId.Trim());
        if (r.appliedActionIds.Count > CampaignInventoryRecord.MaxActionIds)
            r.appliedActionIds.RemoveRange(0, r.appliedActionIds.Count - CampaignInventoryRecord.MaxActionIds);
    }

    private static bool SameList(List<string> a, List<string> b)
    {
        if (a == null || b == null) return a == b;
        if (a.Count != b.Count) return false;
        for (int i = 0; i < a.Count; i++) if (a[i] != b[i]) return false;
        return true;
    }
}
