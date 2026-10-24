using System.Collections.Generic;

/// <summary>
/// PURPOSE:
/// What Arthur says about the corkboard, without ever naming a
/// classification: his introduction (one indirect line per corner), his
/// wrap-up, and his reaction to every card placed on the board.
///
/// THE TWO HIDDEN QUESTIONS:
/// Every corner is the answer to two questions: is the person Filipino?
/// and do they live here most of the year? A wrong placement is judged on
/// which question was missed, so the hint points at the right clue
/// (passport, or where they spend their year) without giving the answer.
///
/// Pure C# (no Unity), so it is checked by Tests/DialogueRegression.
/// Built-in lines below; Resources/Levels/Level_01/town.json can replace
/// them (TownContentLoader calls the Set... methods at level load).
/// </summary>
public static class CorkboardTeaching
{
    public enum Miss { None, Citizenship, Residence, Both }

    public sealed class Reaction
    {
        public string PlacedRight;
        public string HintCitizenship;
        public string HintResidence;
        public string HintBoth;
    }

    // ---------------------------------------------------------
    // The two questions
    // ---------------------------------------------------------

    public static bool IsFilipino(TaxpayerClassification c) =>
        c == TaxpayerClassification.ResidentCitizen || c == TaxpayerClassification.NonResidentCitizen;

    public static bool LivesHere(TaxpayerClassification c) =>
        c == TaxpayerClassification.ResidentCitizen || c == TaxpayerClassification.ResidentAlien;

    /// <summary>Which of the two questions a placement got wrong.</summary>
    public static Miss Compare(TaxpayerClassification correct, TaxpayerClassification placed)
    {
        bool passport = IsFilipino(correct) != IsFilipino(placed);
        bool home = LivesHere(correct) != LivesHere(placed);
        if (passport && home) return Miss.Both;
        if (passport) return Miss.Citizenship;
        if (home) return Miss.Residence;
        return Miss.None;
    }

    // ---------------------------------------------------------
    // Arthur's talks (built-in; town.json may replace)
    // ---------------------------------------------------------

    private static readonly List<KeyValuePair<bool, string>> BuiltInIntro = new List<KeyValuePair<bool, string>>
    {
        Npc("Great work collecting all four profiles! Now let's sort them on my corkboard."),
        Npc("Every taxpayer comes down to two clues: where their passport is from, and where they spend most of their year."),
        Npc("Some were born Filipino and never really left. Their whole life, and their paycheck, is right here."),
        Npc("Some are Filipino to the bone, but work keeps them overseas most of the year. Home is just for visits."),
        Npc("Some came from another country, fell for this place, and stayed. They live and do business here now."),
        Npc("And some are only passing through: foreign visitors on a short job, with a ticket home already booked."),
        Player("So I listen for where they're from, and where they actually live."),
        Npc("Exactly! Pin each profile where you think it belongs. Once all four are up, I'll check the board, and your first check counts.")
    };

    private static readonly List<KeyValuePair<bool, string>> BuiltInDone = new List<KeyValuePair<bool, string>>
    {
        Npc("Excellent work! Every profile is in its right spot."),
        Npc("You listened for the two clues: where they're from and where they live. That instinct will serve you well in the office."),
        Npc("Now, let's head there. Your first real case is waiting!")
    };

    private static readonly Dictionary<string, Reaction> BuiltInReactions = new Dictionary<string, Reaction>
    {
        ["profile_marie"] = new Reaction
        {
            PlacedRight = "Yes! Born here, never left. Her home and her paycheck are both right here.",
            HintCitizenship = "Hmm. Where was Marie born, and whose passport has never been stamped?",
            HintResidence = "Close! But has Marie ever actually lived anywhere else?",
            HintBoth = "Not quite. Think about where Marie is from, and where she spends her days."
        },
        ["profile_jhunjhun"] = new Reaction
        {
            PlacedRight = "That's it! Filipino through and through, but his work keeps him abroad most of the year.",
            HintCitizenship = "Listen again: Jhun-jhun called himself Pinoy to the bone.",
            HintResidence = "Almost! Where does Jhun-jhun spend most of his year?",
            HintBoth = "Not quite. Where is Jhun-jhun from, and where does he actually live?"
        },
        ["profile_milo"] = new Reaction
        {
            PlacedRight = "Exactly! He came from far away, but this town is truly his home now.",
            HintCitizenship = "Did Milo grow up here? Remember where he said he's from.",
            HintResidence = "Close! Six years, a house and a family here. Is Milo just visiting?",
            HintBoth = "Not quite. Think about where Milo is from, and where he has made his home."
        },
        ["profile_zorbix"] = new Reaction
        {
            PlacedRight = "Spot on! A visitor from far, far away, here only for a short paid project.",
            HintCitizenship = "Hmm. Does a visitor from another planet carry a Filipino passport?",
            HintResidence = "Almost! Twenty-seven days and a flight home booked. Does Zorbix live here?",
            HintBoth = "Not quite. Where is Zorbix from, and how long is he staying?"
        }
    };

    private static List<KeyValuePair<bool, string>> introOverride;
    private static List<KeyValuePair<bool, string>> doneOverride;
    private static readonly Dictionary<string, Reaction> reactionOverrides = new Dictionary<string, Reaction>();

    private static KeyValuePair<bool, string> Npc(string text) => new KeyValuePair<bool, string>(false, text);
    private static KeyValuePair<bool, string> Player(string text) => new KeyValuePair<bool, string>(true, text);

    public static IReadOnlyList<KeyValuePair<bool, string>> IntroLines => introOverride ?? BuiltInIntro;
    public static IReadOnlyList<KeyValuePair<bool, string>> DoneLines => doneOverride ?? BuiltInDone;

    /// <summary>Arthur's corkboard introduction as a conversation.</summary>
    public static List<DialogueLine> BuildIntro(string speakerName) => Build(speakerName, IntroLines);

    /// <summary>Arthur's wrap-up once the board is solved.</summary>
    public static List<DialogueLine> BuildDone(string speakerName) => Build(speakerName, DoneLines);

    private static List<DialogueLine> Build(string speakerName, IReadOnlyList<KeyValuePair<bool, string>> lines)
    {
        var builder = new DialogueBuilder(speakerName);
        for (int i = 0; i < lines.Count; i++)
        {
            if (lines[i].Key) builder.Player(lines[i].Value);
            else builder.Npc(lines[i].Value);
        }
        return builder.Build();
    }

    /// <summary>
    /// Arthur's reaction to a card placed on a corner: why it fits, or a
    /// hint toward the clue that was missed. Never names a classification.
    /// </summary>
    public static string ReactionFor(TaxpayerProfile profile, TaxpayerClassification placedOn)
    {
        if (profile == null) return null;
        Reaction r = ReactionById(profile.ProfileId);
        Miss miss = Compare(profile.CorrectClassification, placedOn);
        string name = FirstName(profile.TaxpayerName);
        if (r == null)
        {
            switch (miss)
            {
                case Miss.None: return $"Nicely done! That's exactly where {name} belongs.";
                case Miss.Citizenship: return $"Think about where {name} is from.";
                case Miss.Residence: return $"Think about where {name} spends most of the year.";
                default: return $"Think about where {name} is from, and where they live.";
            }
        }
        switch (miss)
        {
            case Miss.None: return r.PlacedRight;
            case Miss.Citizenship: return r.HintCitizenship;
            case Miss.Residence: return r.HintResidence;
            default: return r.HintBoth;
        }
    }

    public static Reaction ReactionById(string profileId)
    {
        if (string.IsNullOrEmpty(profileId)) return null;
        if (reactionOverrides.TryGetValue(profileId, out Reaction o)) return o;
        return BuiltInReactions.TryGetValue(profileId, out Reaction r) ? r : null;
    }

    private static string FirstName(string full)
    {
        if (string.IsNullOrWhiteSpace(full)) return "them";
        int space = full.IndexOf(' ');
        return space > 0 ? full.Substring(0, space) : full;
    }

    // ---------------------------------------------------------
    // Overrides (town.json)
    // ---------------------------------------------------------

    public static void SetIntro(List<KeyValuePair<bool, string>> lines) => introOverride = lines != null && lines.Count > 0 ? lines : null;

    public static void SetDone(List<KeyValuePair<bool, string>> lines) => doneOverride = lines != null && lines.Count > 0 ? lines : null;

    public static void SetReaction(string profileId, Reaction reaction)
    {
        if (string.IsNullOrEmpty(profileId)) return;
        if (reaction == null) reactionOverrides.Remove(profileId);
        else reactionOverrides[profileId] = reaction;
    }

    /// <summary>Back to the built-in lines.</summary>
    public static void ClearOverrides()
    {
        introOverride = null;
        doneOverride = null;
        reactionOverrides.Clear();
    }
}
