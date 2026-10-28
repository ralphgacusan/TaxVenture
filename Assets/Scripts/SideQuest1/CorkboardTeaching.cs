using System.Collections.Generic;

/// <summary>
/// PURPOSE:
/// What the guide (Arthur) says about the corkboard, without ever naming a
/// classification: his short introduction (T26: a reminder of the two
/// details, no client described again), his wrap-up, his reaction to every
/// card placed on the board, and the board's static category reference.
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
    // The guide's talks (built-in; town.json may replace)
    // ---------------------------------------------------------

    // Four brief screens explain the vocabulary and the first-check stakes,
    // without assigning any plaza person to a category.
    private static readonly List<KeyValuePair<bool, string>> BuiltInIntro = new List<KeyValuePair<bool, string>>
    {
        Npc("Four IDs, four stories! Resident citizens are Filipinos whose home is here; non-resident citizens are Filipinos whose home is abroad."),
        Npc("Resident aliens are foreign nationals who make their home here; non-resident aliens live abroad. 'Alien' just means foreign national. No spaceship required!"),
        Npc("Flip an ID to reread its story, then drag it to a corner. CATEGORIES is your reminder. Pinning the fourth ID checks the whole board."),
        Npc("First check: four right earns Gold, two earns Silver, zero or one earns Bronze. Fix any mix-ups to finish; that first check sets your medal!")
    };

    /// <summary>The board introduction's longest allowed length.</summary>
    public const int MaxIntroLines = 4;

    private static readonly List<KeyValuePair<bool, string>> BuiltInDone = new List<KeyValuePair<bool, string>>
    {
        Npc("Excellent work! Every ID is in its right spot."),
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

    // ---------------------------------------------------------
    // The board's category reference (T26): what each corner's
    // formal name means, one sentence each, from the board's own two
    // distinctions (Filipino citizen or not; home here or not).
    // Static and client-independent: it never names a client, an
    // assignment or a destination, and never changes with the draft.
    // ---------------------------------------------------------

    public sealed class CategoryMeaning
    {
        public TaxpayerClassification Category;
        public string Name;
        public string Meaning;
    }

    private static readonly CategoryMeaning[] BuiltInCategories =
    {
        new CategoryMeaning { Category = TaxpayerClassification.ResidentCitizen, Name = "Resident Citizen",
            Meaning = "A Filipino citizen whose home is in the Philippines." },
        new CategoryMeaning { Category = TaxpayerClassification.NonResidentCitizen, Name = "Non-Resident Citizen",
            Meaning = "A Filipino citizen whose home is abroad for most of the year." },
        new CategoryMeaning { Category = TaxpayerClassification.ResidentAlien, Name = "Resident Alien",
            Meaning = "A citizen of another country whose home is now in the Philippines." },
        new CategoryMeaning { Category = TaxpayerClassification.NonResidentAlien, Name = "Non-Resident Alien",
            Meaning = "A citizen of another country whose home is not in the Philippines." }
    };

    private static readonly Dictionary<TaxpayerClassification, string> categoryOverrides = new Dictionary<TaxpayerClassification, string>();

    /// <summary>The four categories in a fixed order (town.json "boardCategories" may reword a meaning).</summary>
    public static IReadOnlyList<CategoryMeaning> Categories
    {
        get
        {
            var list = new List<CategoryMeaning>();
            foreach (CategoryMeaning c in BuiltInCategories)
                list.Add(new CategoryMeaning
                {
                    Category = c.Category,
                    Name = c.Name,
                    Meaning = categoryOverrides.TryGetValue(c.Category, out string edited) ? edited : c.Meaning
                });
            return list;
        }
    }

    /// <summary>Rewords one category's meaning (blank keeps the built-in sentence).</summary>
    public static void SetCategoryMeaning(TaxpayerClassification category, string meaning)
    {
        if (string.IsNullOrWhiteSpace(meaning)) categoryOverrides.Remove(category);
        else categoryOverrides[category] = meaning.Trim();
    }

    private static List<KeyValuePair<bool, string>> introOverride;
    private static List<KeyValuePair<bool, string>> doneOverride;
    private static readonly Dictionary<string, Reaction> reactionOverrides = new Dictionary<string, Reaction>();

    private static KeyValuePair<bool, string> Npc(string text) => new KeyValuePair<bool, string>(false, text);
    private static KeyValuePair<bool, string> Player(string text) => new KeyValuePair<bool, string>(true, text);

    public static IReadOnlyList<KeyValuePair<bool, string>> IntroLines => introOverride ?? BuiltInIntro;
    public static IReadOnlyList<KeyValuePair<bool, string>> DoneLines => doneOverride ?? BuiltInDone;

    /// <summary>The guide's corkboard introduction as a conversation.</summary>
    public static List<DialogueLine> BuildIntro(string speakerName) => Build(speakerName, IntroLines);

    /// <summary>The guide's wrap-up once the board is solved.</summary>
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
    /// The guide's reaction to a card placed on a corner: why it fits, or a
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

    /// <summary>town.json's introduction (one to MaxIntroLines lines; anything longer keeps the built-in one).</summary>
    public static void SetIntro(List<KeyValuePair<bool, string>> lines) =>
        introOverride = lines != null && lines.Count > 0 && lines.Count <= MaxIntroLines ? lines : null;

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
        categoryOverrides.Clear();
    }
}
