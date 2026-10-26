using System;
using System.Collections.Generic;

/// <summary>
/// One supporting record in a client's file (Documentation/SupportingDocuments.md;
/// model from T24, now used by Arthur's Reviewed File, T30). The type fixes the
/// layout; the values are that client's facts. Never a classification hidden
/// for the player: a reviewed file shows its own completed entries on page one.
/// </summary>
[Serializable]
public class ClientRecord
{
    public const string Identity = "identity";
    public const string Background = "background";
    /// <summary>Type 3, the compensation certificate (2316): Marie's case, T25.</summary>
    public const string Compensation = "compensation";

    public string id;
    /// <summary>The client it belongs to (a former client for Arthur's file).</summary>
    public string npcId;
    /// <summary>Identity (Type 1, ID card), Background (Type 2, client background sheet) or Compensation (Type 3, 2316).</summary>
    public string type;
    /// <summary>The case document it prints (its documentName), for an active case's records; empty for Arthur's file.</summary>
    public string source;
    /// <summary>The short tab name, e.g. "Identity".</summary>
    public string tab;
    /// <summary>The printed header, e.g. "IDENTITY CARD".</summary>
    public string title;
    public string name;
    // Identity card
    public string citizenship;
    public string born;
    public string address;
    public string reference;
    // Client background sheet
    public string period;
    public string living;
    public string work;
    public string income;
    public string footer;
    // Compensation certificate (2316)
    public string employer;
    public string employerTin;
    public string compensationAmount;
    public string withheldAmount;

    // ID card back (user revision 2026-10-27): the taxpayer profile, read by flipping the card.
    /// <summary>The profile summary printed on the card's back (empty = the back is blank).</summary>
    public string profileText;
    /// <summary>The profile's number on the back header, e.g. "No. 01" (optional).</summary>
    public string profileNumber;

    /// <summary>An ID card; every other type is a sheet.</summary>
    public bool IsCard => type == Identity;

    /// <summary>An ID card with a profile on its back: a tap flips it.</summary>
    public bool HasBack => IsCard && !string.IsNullOrWhiteSpace(profileText);

    public ClientRecord Copy() => (ClientRecord)MemberwiseClone();
}

/// <summary>One completed, read-only entry on the reviewed file's first page (a case folder key and its enum value name).</summary>
[Serializable]
public class ReviewedEntry
{
    /// <summary>ResidencyStatus, TaxpayerType or IncomeSource (the Level 1 case folder's keys).</summary>
    public string key;
    /// <summary>The row label, as Marie's folder shows it ("Taxpayer Classification").</summary>
    public string label;
    /// <summary>The enum value name ("NonResidentCitizen"); shown spaced out like the case folder does.</summary>
    public string value;
}

/// <summary>Everything Arthur's Reviewed File shows and Arthur says around it (town.json "reviewedFile"; built-in copy below).</summary>
[Serializable]
public class ReviewedFileContent
{
    public string clientName;
    /// <summary>A masked reference ("•••• 4180").</summary>
    public string tin;
    public string taxYear;
    public string reviewedBy;
    public ReviewedEntry[] entries;
    public ClientRecord[] records;
    /// <summary>Arthur's opening talk (two short lines at most).</summary>
    public string[] arthurIntro;
    /// <summary>
    /// His four contextual lines while the file is read, in order: the first
    /// record looked at; back on page one (classification); the work and
    /// earnings entries; the send-off once the file was looked over.
    /// </summary>
    public string[] notes;
}

/// <summary>
/// PURPOSE:
/// Arthur's Reviewed File and the opening route (Campaign Foundation T30,
/// replacing T24's Jhun-jhun-first gate): meet Arthur at his start marker; his
/// completed talk presents the file; it is collected once into the Bag; its two
/// records are looked at up close and put back and the completed first page is
/// seen again; then the four plaza clients are open, in any order, profiles
/// only. Pure C#, no Unity: stages, who waits, what a saved checkpoint may
/// hold (quiet migration of older runs plus repair), and the authored content.
///
/// The file is a former client's finished reference. It is never Marie's
/// active case, never edited, submitted or used as a value source.
/// </summary>
public static class ReviewedFileRules
{
    /// <summary>The inventory item (InventoryCatalogue.ReviewedFile).</summary>
    public const string ItemId = "arthur_reviewed_file";
    /// <summary>The once-per-campaign pickup claim (PickupRules.ClaimId).</summary>
    public const string PickupId = "arthur-reviewed-file";
    public const string IdentityId = "reviewed-identity";
    public const string BackgroundId = "reviewed-background";
    /// <summary>The former client (never one of the four plaza clients).</summary>
    public const string ClientId = "former_client_ramon";

    /// <summary>Its records, in page order (stable ids saved in the Level 1 checkpoint).</summary>
    public static readonly string[] RecordIds = { IdentityId, BackgroundId };

    /// <summary>T24's record ids (Jhun-jhun's sleeve, retired by T30): dropped quietly from older runs.</summary>
    public static readonly string[] RetiredRecordIds = { "jhunjhun-identity", "jhunjhun-background" };

    /// <summary>The four plaza clients (each gives only their profile).</summary>
    public static readonly string[] PlazaClients = { "npc_marie", "npc_jhunjhun", "npc_milo", "npc_zorbix" };

    // The route's objectives events (ObjectiveTriggers uses these names; objectives.json "event:<name>").
    public const string MetEvent = "ArthurMet";
    public const string LookedOverEvent = "ReviewedFileLookedOver";

    /// <summary>The route's objectives events, in order.</summary>
    public static readonly string[] Steps = { MetEvent, LookedOverEvent };

    /// <summary>The first-page keys, in order (the Level 1 case folder's three graded entries).</summary>
    public static readonly string[] EntryKeys = { "ResidencyStatus", "TaxpayerType", "IncomeSource" };

    public enum Stage
    {
        /// <summary>Arthur's opening talk is next; the plaza clients wait.</summary>
        Meet,
        /// <summary>His file floats beside him, to collect.</summary>
        Collect,
        /// <summary>The file is owned; its records and first page are still to look over.</summary>
        Review,
        /// <summary>The rounds are open: every plaza client talks and gives a profile.</summary>
        Open
    }

    public static bool AllReturned(ICollection<string> returnedRecordIds)
    {
        if (returnedRecordIds == null) return false;
        foreach (string id in RecordIds)
            if (!returnedRecordIds.Contains(id)) return false;
        return true;
    }

    /// <summary>Both records shown and put back, and the completed page seen again after that.</summary>
    public static bool LookedOver(ICollection<string> returnedRecordIds, bool pageRevisited) => AllReturned(returnedRecordIds) && pageRevisited;

    /// <summary>The rounds had begun before the file (another client met or a profile collected): nothing is locked again.</summary>
    public static bool RoundsBegun(ICollection<string> talkedNpcIds, ICollection<string> collectedProfileIds)
    {
        if (collectedProfileIds != null)
            foreach (string id in collectedProfileIds)
                if (!string.IsNullOrEmpty(id)) return true;
        if (talkedNpcIds != null)
            foreach (string id in talkedNpcIds)
                if (!string.IsNullOrEmpty(id)) return true;
        return false;
    }

    public static Stage StageOf(bool introDone, bool fileOwned, bool lookedOver, bool roundsOpen)
    {
        if (roundsOpen || lookedOver) return Stage.Open;
        if (!introDone) return Stage.Meet;
        return fileOwned ? Stage.Review : Stage.Collect;
    }

    /// <summary>A saved run's stage; fileOwned comes from the campaign inventory (the file is not part of the checkpoint).</summary>
    public static Stage StageOf(Level1FieldCheckpoint checkpoint, bool fileOwned) => checkpoint == null
        ? StageOf(false, fileOwned, false, false)
        : StageOf(checkpoint.arthurIntroDone || fileOwned, fileOwned, LookedOver(checkpoint.returnedRecordIds, checkpoint.reviewedPageRevisited),
            checkpoint.roundsOpen || RoundsBegun(checkpoint.talkedNpcIds, checkpoint.collectedProfileIds));

    /// <summary>A plaza client before the rounds open: no marker, and a tap never starts their talk.</summary>
    public static bool IsWaiting(string npcId, bool roundsOpen) =>
        !roundsOpen && Array.IndexOf(PlazaClients, npcId) >= 0;

    /// <summary>
    /// Arthur's opening talk can start: it was not done yet, and the opening
    /// lessons (controls, Notes, Supply Counter) are behind the player. An
    /// older run whose rounds already began still gets the file, as an option.
    /// </summary>
    public static bool OpeningAvailable(bool introDone, bool openingLessonsDone) => !introDone && openingLessonsDone;

    /// <summary>The tracker steps already behind a stage (a resumed or older run never points back at them).</summary>
    public static List<string> ReachedSteps(Stage stage)
    {
        int count = stage == Stage.Open ? 2 : stage == Stage.Meet ? 0 : 1;
        var keys = new List<string>(count);
        for (int i = 0; i < count; i++) keys.Add("event:" + Steps[i]);
        return keys;
    }

    /// <summary>
    /// Quiet upgrade of a run saved before T30 (not a repair, no notice): T24's
    /// Jhun-jhun record ids go, and a run whose plaza rounds had begun keeps
    /// them open. Nothing is granted for an old flag. True when anything changed.
    /// </summary>
    public static bool Migrate(Level1FieldCheckpoint checkpoint)
    {
        if (checkpoint == null) return false;
        bool changed = false;
        if (checkpoint.presentedRecordIds == null) { checkpoint.presentedRecordIds = new List<string>(); changed = true; }
        if (checkpoint.returnedRecordIds == null) { checkpoint.returnedRecordIds = new List<string>(); changed = true; }
        changed |= checkpoint.presentedRecordIds.RemoveAll(id => Array.IndexOf(RetiredRecordIds, id) >= 0) > 0;
        changed |= checkpoint.returnedRecordIds.RemoveAll(id => Array.IndexOf(RetiredRecordIds, id) >= 0) > 0;
        if (!checkpoint.roundsOpen && RoundsBegun(checkpoint.talkedNpcIds, checkpoint.collectedProfileIds))
        {
            checkpoint.roundsOpen = true;
            changed = true;
        }
        return changed;
    }

    /// <summary>
    /// Content check of the saved route (after the structural repair and
    /// Migrate): only the file's record ids; a record put back was shown; a
    /// revisit needs both records back; shown records mean the talk happened;
    /// a looked-over file means the rounds are open. True when anything changed.
    /// </summary>
    public static bool Repair(Level1FieldCheckpoint checkpoint)
    {
        if (checkpoint == null) return false;
        bool changed = false;
        if (checkpoint.presentedRecordIds == null) { checkpoint.presentedRecordIds = new List<string>(); changed = true; }
        if (checkpoint.returnedRecordIds == null) { checkpoint.returnedRecordIds = new List<string>(); changed = true; }
        changed |= checkpoint.presentedRecordIds.RemoveAll(id => Array.IndexOf(RecordIds, id) < 0) > 0;
        changed |= checkpoint.returnedRecordIds.RemoveAll(id => Array.IndexOf(RecordIds, id) < 0) > 0;
        foreach (string id in checkpoint.returnedRecordIds)
            if (!checkpoint.presentedRecordIds.Contains(id)) { checkpoint.presentedRecordIds.Add(id); changed = true; }
        if (checkpoint.reviewedPageRevisited && !AllReturned(checkpoint.returnedRecordIds))
        {
            checkpoint.reviewedPageRevisited = false;
            changed = true;
        }
        if (!checkpoint.arthurIntroDone && checkpoint.presentedRecordIds.Count > 0)
        {
            checkpoint.arthurIntroDone = true;
            changed = true;
        }
        if (!checkpoint.roundsOpen && LookedOver(checkpoint.returnedRecordIds, checkpoint.reviewedPageRevisited))
        {
            checkpoint.roundsOpen = true;
            changed = true;
        }
        return changed;
    }

    // =========================================================
    // CONTENT
    // =========================================================

    private static ReviewedFileContent overrides;

    /// <summary>The file as shown (town.json "reviewedFile" over the built-in copy; blank parts keep the built-in text).</summary>
    public static ReviewedFileContent Content()
    {
        ReviewedFileContent c = BuiltIn();
        ReviewedFileContent e = overrides;
        if (e == null) return c;
        c.clientName = Pick(e.clientName, c.clientName);
        c.tin = Pick(e.tin, c.tin);
        c.taxYear = Pick(e.taxYear, c.taxYear);
        c.reviewedBy = Pick(e.reviewedBy, c.reviewedBy);
        if (e.entries != null && e.entries.Length == c.entries.Length)
            for (int i = 0; i < c.entries.Length; i++)
                if (e.entries[i] != null && e.entries[i].key == c.entries[i].key)
                {
                    c.entries[i].label = Pick(e.entries[i].label, c.entries[i].label);
                    c.entries[i].value = Pick(e.entries[i].value, c.entries[i].value);
                }
        if (e.records != null)
            for (int i = 0; i < c.records.Length; i++)
            {
                ClientRecord edited = Array.Find(e.records, r => r != null && r.id == c.records[i].id);
                if (edited != null) c.records[i] = Merge(c.records[i], edited);
            }
        if (e.arthurIntro != null && e.arthurIntro.Length >= 1 && e.arthurIntro.Length <= 2 && Array.TrueForAll(e.arthurIntro, l => !string.IsNullOrWhiteSpace(l)))
            c.arthurIntro = Trimmed(e.arthurIntro);
        if (e.notes != null && e.notes.Length == c.notes.Length && Array.TrueForAll(e.notes, l => !string.IsNullOrWhiteSpace(l)))
            c.notes = Trimmed(e.notes);
        return c;
    }

    public static ClientRecord Record(string id) => Array.Find(Content().records, r => r.id == id);

    /// <summary>town.json values (null clears). Problems() reports what is ignored.</summary>
    public static void SetOverrides(ReviewedFileContent content) => overrides = content;

    public static void ClearOverrides() => overrides = null;

    /// <summary>What is wrong with an edited file (empty = fine). Keys, ids and types are fixed; blank text keeps the built-in copy.</summary>
    public static List<string> Problems(ReviewedFileContent content)
    {
        var problems = new List<string>();
        if (content == null) return problems;
        if (content.entries != null && content.entries.Length > 0)
        {
            if (content.entries.Length != EntryKeys.Length) problems.Add($"entries must be the {EntryKeys.Length} keys {string.Join(", ", EntryKeys)}");
            else
                for (int i = 0; i < EntryKeys.Length; i++)
                    if (content.entries[i] == null || content.entries[i].key != EntryKeys[i])
                        problems.Add($"entry {i + 1} must be \"{EntryKeys[i]}\"");
        }
        if (content.records != null)
        {
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (ClientRecord record in content.records)
            {
                if (record == null) { problems.Add("an empty record"); continue; }
                if (Array.IndexOf(RecordIds, record.id) < 0) { problems.Add($"unknown record id \"{record.id}\" (use {string.Join(" or ", RecordIds)})"); continue; }
                if (!seen.Add(record.id)) problems.Add($"duplicate record \"{record.id}\"");
                string type = record.id == IdentityId ? ClientRecord.Identity : ClientRecord.Background;
                if (!string.IsNullOrWhiteSpace(record.type) && record.type.Trim() != type) problems.Add($"\"{record.id}\" keeps its type \"{type}\"");
            }
        }
        if (content.arthurIntro != null && content.arthurIntro.Length > 2)
            problems.Add("arthurIntro needs one or two short lines");
        if (content.notes != null && content.notes.Length > 0 && content.notes.Length != 4)
            problems.Add("notes needs exactly four lines (first record, classification, work and earnings, send-off)");
        return problems;
    }

    private static ClientRecord Merge(ClientRecord builtIn, ClientRecord edited)
    {
        ClientRecord r = builtIn.Copy();
        r.tab = Pick(edited.tab, r.tab);
        r.title = Pick(edited.title, r.title);
        r.name = Pick(edited.name, r.name);
        r.citizenship = Pick(edited.citizenship, r.citizenship);
        r.born = Pick(edited.born, r.born);
        r.address = Pick(edited.address, r.address);
        r.reference = Pick(edited.reference, r.reference);
        r.period = Pick(edited.period, r.period);
        r.living = Pick(edited.living, r.living);
        r.work = Pick(edited.work, r.work);
        r.income = Pick(edited.income, r.income);
        r.footer = Pick(edited.footer, r.footer);
        r.profileText = Pick(edited.profileText, r.profileText);
        return r;
    }

    private static string Pick(string edited, string builtIn) => string.IsNullOrWhiteSpace(edited) ? builtIn : edited.Trim();

    private static string[] Trimmed(string[] lines) => Array.ConvertAll(lines, l => l.Trim());

    /// <summary>Built-in copy of town.json "reviewedFile" (keep both the same).</summary>
    public static ReviewedFileContent BuiltIn() => new ReviewedFileContent
    {
        clientName = "Ramon Ilagan",
        tin = "•••• 4180",
        taxYear = "2025",
        reviewedBy = "Arthur",
        entries = new[]
        {
            new ReviewedEntry { key = "ResidencyStatus", label = "Taxpayer Classification", value = "NonResidentCitizen" },
            new ReviewedEntry { key = "TaxpayerType", label = "Taxpayer Type", value = "SelfEmployed" },
            new ReviewedEntry { key = "IncomeSource", label = "Income Source", value = "BusinessIncome" },
        },
        records = new[]
        {
            new ClientRecord
            {
                id = IdentityId, npcId = ClientId, type = ClientRecord.Identity, tab = "Identity", title = "IDENTITY CARD",
                name = "Ramon Ilagan", citizenship = "Filipino", born = "21 Feb 1986", address = "Coast Tales (family home)", reference = "•••• 4180",
                profileText = "Filipino trader based in Dubai since 2019; home two weeks a year; runs his own shop.",
                profileNumber = "Reviewed"
            },
            new ClientRecord
            {
                id = BackgroundId, npcId = ClientId, type = ClientRecord.Background, tab = "Background", title = "CLIENT BACKGROUND",
                name = "Ramon Ilagan", period = "Year 2025", reference = "BG-0402",
                living = "Lives in Dubai since 2019\nHome visits: 2 weeks/year",
                work = "Own trading shop in Dubai",
                income = "Profit from shop sales",
                footer = "Client interview notes"
            },
        },
        arthurIntro = new[]
        {
            "There you are, Elijah! Before you meet today's clients, I want you to see a case I finished.",
            "It's closed, so it's just for reading. Here, take it!"
        },
        notes = new[]
        {
            "The ID gives his citizenship, the background where he lives.",
            "Filipino, but his home is abroad: that's his classification.",
            "And his own shop explains the other two.",
            "Keep it handy. Now go meet the four clients: each will hand you a copy of their ID."
        }
    };
}
