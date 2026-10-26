using System.Collections.Generic;

/// <summary>
/// PURPOSE:
/// Dialogue for the 4 Level 1 town NPCs. Uses the existing
/// DialogueBuilder / DialogueLine data model, so it plugs straight into
/// DialogueUI.StartDialogue() with no changes to that system.
///
/// Each talk is a short life story (6 lines) that holds the clues for the
/// corkboard (where they're from, where they spend their year) and, for
/// Marie, for her office case (how she earns her money), WITHOUT naming any
/// classification. The player has to put it together.
///
/// Player lines are Elijah's (the tax consultant / player character).
/// Built-in lines below; Resources/Levels/Level_01/town.json can replace
/// them per NPC (TownContentLoader calls SetOverride at level load).
/// TaxpayerNpcInteractable just calls GetDialogueFor(npcId, ...).
/// A few lines carry an emote (T22); town.json lines may set "emote"/"burst".
/// Each client has one waiting line for a tap before the plaza rounds open
/// (T24; T30: all four wait until Arthur's reviewed file was looked over).
/// Talks end with the profile only (T30 removed T24's records sleeve line).
/// </summary>
public static class TownNpcDialogueLibrary
{
    // npcId -> lines (Key true = player line)
    private static readonly Dictionary<string, List<KeyValuePair<bool, string>>> overrides =
        new Dictionary<string, List<KeyValuePair<bool, string>>>();
    // npcId -> per-line emote (tag, burst), same order as the override lines
    private static readonly Dictionary<string, List<KeyValuePair<string, bool>>> overrideEmotes =
        new Dictionary<string, List<KeyValuePair<string, bool>>>();
    // npcId -> the line said when tapped before the rounds open (town.json "waiting")
    private static readonly Dictionary<string, string> waitingOverrides = new Dictionary<string, string>();

    /// <summary>
    /// The one line a client says when tapped before the rounds open (T30:
    /// after Arthur's reviewed file). Null for an unknown client.
    /// </summary>
    public static List<DialogueLine> GetWaitingLine(string npcId, string npcDisplayName)
    {
        if (!IsAuthored(npcId)) return null;
        string text = waitingOverrides.TryGetValue(npcId, out string custom) ? custom : BuiltInWaitingLine(npcId);
        return string.IsNullOrWhiteSpace(text) ? null : new DialogueBuilder(npcDisplayName).Npc(text).Build();
    }

    /// <summary>Built-in waiting lines (town.json "waiting" keeps the same text).</summary>
    public static string BuiltInWaitingLine(string npcId)
    {
        switch (npcId)
        {
            case "npc_marie": return "Oh, hi! I'm just finishing a call. Can you come back in a bit?";
            case "npc_jhunjhun": return "Sorry, I'm on the phone with my ride. Come back in a bit?";
            case "npc_milo": return "Welcome! The lunch rush has me running. Come back in a little while?";
            case "npc_zorbix": return "GREETINGS. MY TRANSLATOR IS STILL WARMING UP. RETURN SHORTLY.";
            default: return null;
        }
    }

    /// <summary>Replaces one client's waiting line (null or empty restores the built-in one).</summary>
    public static void SetWaitingOverride(string npcId, string text)
    {
        if (string.IsNullOrEmpty(npcId)) return;
        if (string.IsNullOrWhiteSpace(text)) waitingOverrides.Remove(npcId);
        else waitingOverrides[npcId] = text.Trim();
    }

    public static List<DialogueLine> GetDialogueFor(string npcId, string npcDisplayName)
    {
        return GetDialogueFor(npcId, npcDisplayName, out _);
    }

    /// <summary>
    /// Returns true when this library actually authored lines for
    /// <paramref name="npcId"/>. The placeholder fallback in
    /// GetDialogueFor is itself a valid one-line conversation, so a caller
    /// that advances quest progress on completion must check this instead
    /// of assuming every returned list is real content.
    /// </summary>
    public static bool HasDialogueFor(string npcId)
    {
        GetDialogueFor(npcId, string.Empty, out bool recognized);
        return recognized;
    }

    /// <summary>Replaces one NPC's built-in lines (null or empty restores them).</summary>
    public static void SetOverride(string npcId, List<KeyValuePair<bool, string>> lines) => SetOverride(npcId, lines, null);

    /// <summary>Replaces one NPC's built-in lines with per-line emotes (tag, burst; an empty tag = none).</summary>
    public static void SetOverride(string npcId, List<KeyValuePair<bool, string>> lines,
        List<KeyValuePair<string, bool>> emotes)
    {
        if (string.IsNullOrEmpty(npcId)) return;
        overrideEmotes.Remove(npcId);
        if (lines == null || lines.Count == 0)
        {
            overrides.Remove(npcId);
            return;
        }
        overrides[npcId] = lines;
        if (emotes != null && emotes.Count > 0) overrideEmotes[npcId] = emotes;
    }

    public static void ClearOverrides()
    {
        overrides.Clear();
        overrideEmotes.Clear();
        waitingOverrides.Clear();
    }

    public static List<DialogueLine> GetDialogueFor(string npcId, string npcDisplayName,
        out bool recognized)
    {
        var builder = new DialogueBuilder(npcDisplayName);
        recognized = true;

        // Only authored NPCs may be overridden (unknown ids stay unrecognized).
        if (npcId != null && overrides.TryGetValue(npcId, out List<KeyValuePair<bool, string>> custom) && IsAuthored(npcId))
        {
            overrideEmotes.TryGetValue(npcId, out List<KeyValuePair<string, bool>> emotes);
            for (int i = 0; i < custom.Count; i++)
            {
                if (custom[i].Key) builder.Player(custom[i].Value);
                else builder.Npc(custom[i].Value);
                if (emotes != null && i < emotes.Count) builder.Emote(emotes[i].Key, emotes[i].Value);
            }
            return builder.Build();
        }

        switch (npcId)
        {
            // Marie Bautista: born here, never left; salaried BPO employee.
            case "npc_marie":
                builder
                    .Player("Hi! I'm the new tax consultant in town. Mind telling me a bit about yourself?")
                    .Npc("Not at all! Born and raised right here. My Filipino passport has never even been stamped!").Emote("music")
                    .Player("Ha! So what keeps you busy these days?")
                    .Npc("I work the night shift at the BPO office downtown. Same company for five years, and my pay comes in every payday.")
                    .Player("No side business or anything?")
                    .Npc("Just my salary! Work, sleep, and weekend karaoke with the family. Good luck on your first day!").Emote("sparkles");
                break;

            // Jhun-jhun Templo: Filipino, works and lives in Qatar; home only for visits.
            case "npc_jhunjhun":
                builder
                    .Player("Hi there! Waiting for someone?")
                    .Npc("My ride to the airport. Vacation's over, so it's back to Qatar tomorrow.")
                    .Player("Back to Qatar? What do you do there?")
                    .Npc("I'm an engineer on a five-year contract. My apartment and my whole work life are over there now.")
                    .Player("You must miss home.")
                    .Npc("Every day! I'm Pinoy to the bone, but I only get to come home for a few weeks a year.").Emote("worried");
                break;

            // Milo Zenn: from very far away, settled here for six years; runs his own restaurant.
            case "npc_milo":
                builder
                    .Player("Hello! Is this your restaurant?")
                    .Npc("It is! Six years now. You can probably tell from my accent: I'm from somewhere... very far away.")
                    .Player("And you've stayed all this time?")
                    .Npc("I came for a short visit, fell in love with the food, and never left. My house and my family are all here now.")
                    .Player("So the restaurant is how you make a living?")
                    .Npc("My pride and joy! I run it myself, every single day. Come try the adobo sometime!").Emote("heart");
                break;

            // Zorbix: a visitor from another planet on a 27-day paid project.
            case "npc_zorbix":
                builder
                    .Player("Good afternoon! What brings you to town?")
                    .Npc("BUSINESS. A TEMPORARY COMMERCIAL PROJECT FOR MY HOME PLANET.").Emote("alert")
                    .Player("...Right. How long are you staying?")
                    .Npc("TWENTY-SEVEN DAYS. MY RETURN FLIGHT IS ALREADY BOOKED.")
                    .Player("And you're paid for the work you do here?")
                    .Npc("AFFIRMATIVE. THEN I GO HOME. YOUR PLANET IS... NICE TO VISIT.").Emote("music");
                break;

            default:
                recognized = false;
                builder.Npc("...");
                break;
        }

        return builder.Build();
    }

    private static bool IsAuthored(string npcId) =>
        npcId == "npc_marie" || npcId == "npc_jhunjhun" || npcId == "npc_milo" || npcId == "npc_zorbix";
}
