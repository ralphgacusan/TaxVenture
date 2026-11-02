using System;
using System.Collections.Generic;
using System.Globalization;

/// <summary>The keys of the Level 3 desk calculator.</summary>
public enum Level3CalcKey
{
    D0, D1, D2, D3, D4, D5, D6, D7, D8, D9,
    Point, Add, Subtract, Multiply, Divide, Percent, Equals, Clear, ClearEntry, Backspace
}

/// <summary>What one key or source tap did.</summary>
public struct Level3CalcResult
{
    /// <summary>The display, expression or tape changed.</summary>
    public bool Changed;
    /// <summary>Equals printed a new draft tape.</summary>
    public bool Printed;
    /// <summary>Plain input problem shown on the display (never a grade).</summary>
    public string Issue;
}

/// <summary>
/// PURPOSE:
/// The Level 3 desk calculator's arithmetic and input state (T03), pure C#
/// so the regression suite drives it key by key. One shared tool: the
/// protected budget practice now, the worksheet and tax computations later.
///
/// RULES:
/// - Exact decimal arithmetic, never floats. Standard precedence (× ÷ before
///   + −), evaluated left to right, so "22,500 + 0.20 × 200,000" reads as
///   written on a tape.
/// - % turns the number being entered into its fraction (20% = 0.20), so
///   "A × 20%" is A × 0.20. There is no consumer "add a percentage" shortcut.
/// - A number is typed (manual) or taken from a source (sourceId + printed
///   label); the tape keeps that provenance.
/// - Equals prints a draft tape (expression + exact result). The draft stays
///   until a new Equals replaces it or it is attached; Clear only clears the
///   expression being entered.
/// - Divide by zero, overflow and malformed input report an Issue and change
///   nothing else. No score, wallet or trust effect exists here.
/// </summary>
public sealed class Level3Calculator
{
    public const int MaxIntegerDigits = 12;
    public const int MaxFractionDigits = 6;
    public const decimal MaxMagnitude = 999999999999.99m;
    public const string ManualLabel = "typed";
    public const string PreviousResultSource = "tape:previous";

    public const string IssueNoNumber = "Enter a number first";
    public const string IssueUnfinished = "Finish the calculation first";
    public const string IssueDivideByZero = "Can't divide by zero";
    public const string IssueTooLarge = "Too large for this calculator";
    public const string IssueTooLong = "That number is too long";
    public const string IssueNothingToCalculate = "Enter a calculation";

    private readonly List<Level3CalcToken> tokens = new List<Level3CalcToken>();
    private string entry = "";
    private Level3CalcToken entrySource;
    private bool entryPercent;

    /// <summary>The expression entered so far (numbers and operators, without the number being typed).</summary>
    public IReadOnlyList<Level3CalcToken> Tokens => tokens;

    /// <summary>The number being typed ("" when none); "-" while a negative number is started.</summary>
    public string Entry => entry;

    /// <summary>The source the number being entered came from (null when typed).</summary>
    public Level3CalcToken EntrySource => entrySource;

    /// <summary>The last Equals result (exact), or null.</summary>
    public string Result { get; private set; }

    /// <summary>The expression of the last Equals: the draft tape (null when none).</summary>
    public List<Level3CalcToken> DraftExpression { get; private set; }

    /// <summary>The draft tape's exact result (null when none).</summary>
    public string DraftResult { get; private set; }

    public bool HasDraft => DraftExpression != null && DraftResult != null;

    /// <summary>
    /// T07: the draft as one traceable chain. Continuing from a result (an
    /// operator right after =) keeps the calculation that produced it, with a
    /// step separator: 600,000 − 400,000 = × 20% = + 22,500 is one chain.
    /// Null when there is no draft. DraftExpression stays the last step only.
    /// </summary>
    public List<Level3CalcToken> DraftChain { get; private set; }

    /// <summary>The step separator inside a chain (an operator token with this op).</summary>
    public const string ChainStep = "=";

    private List<Level3CalcToken> lastChain;

    /// <summary>The last input problem (null when the last input was fine).</summary>
    public string Issue { get; private set; }

    /// <summary>True right after Equals, before anything else was entered.</summary>
    public bool ShowingResult => Result != null && tokens.Count == 0 && entry.Length == 0;

    // =========================================================
    // INPUT
    // =========================================================

    public Level3CalcResult Press(Level3CalcKey key)
    {
        Issue = null;
        switch (key)
        {
            case Level3CalcKey.Point: return Point();
            case Level3CalcKey.Add: return Operator("+");
            case Level3CalcKey.Subtract: return Operator("-");
            case Level3CalcKey.Multiply: return Operator("*");
            case Level3CalcKey.Divide: return Operator("/");
            case Level3CalcKey.Percent: return Percent();
            case Level3CalcKey.Equals: return EqualsKey();
            case Level3CalcKey.Clear: return ClearAll();
            case Level3CalcKey.ClearEntry: return ClearEntryKey();
            case Level3CalcKey.Backspace: return Backspace();
            default: return Digit((int)key - (int)Level3CalcKey.D0);
        }
    }

    /// <summary>Takes a printed amount from a source paper as the number being entered (replacing a typed one).</summary>
    public Level3CalcResult SelectSource(string sourceId, string label, string amount)
    {
        Issue = null;
        if (string.IsNullOrWhiteSpace(sourceId) || !TryParse(amount, out decimal value)) return Fail(IssueNoNumber);
        if (ShowingResult) Result = null;
        entry = Canonical(value);
        entryPercent = false;
        entrySource = new Level3CalcToken { kind = Level3CalcToken.Number, value = entry, sourceId = sourceId, label = label ?? "" };
        return Changed();
    }

    /// <summary>Clears everything, including the draft tape (a new attempt or a restore).</summary>
    public void Reset()
    {
        tokens.Clear();
        entry = "";
        entrySource = null;
        entryPercent = false;
        Result = null;
        DraftExpression = null;
        DraftResult = null;
        DraftChain = null;
        lastChain = null;
        Issue = null;
    }

    /// <summary>The draft tape was attached: it leaves the calculator (the expression and result stay on the display).</summary>
    public void ConsumeDraft()
    {
        DraftExpression = null;
        DraftResult = null;
        DraftChain = null;
    }

    /// <summary>
    /// T07: takes a printed rate from a source as a percentage (20% enters as
    /// 0.2 and shows as "20%"), keeping the source, so A × rate needs no
    /// typing. A later digit replaces it, as for any source amount.
    /// </summary>
    public Level3CalcResult SelectSourcePercent(string sourceId, string label, string fraction)
    {
        Issue = null;
        if (string.IsNullOrWhiteSpace(sourceId) || !TryParse(fraction, out decimal value)) return Fail(IssueNoNumber);
        if (ShowingResult) Result = null;
        entry = Canonical(value);
        entryPercent = true;
        entrySource = new Level3CalcToken { kind = Level3CalcToken.Number, value = entry, sourceId = sourceId, label = label ?? "", percent = true };
        return Changed();
    }

    private Level3CalcResult Digit(int digit)
    {
        if (digit < 0 || digit > 9) return Fail(IssueNoNumber);
        if (ShowingResult) Result = null;
        if (entrySource != null || entryPercent) { entry = ""; entrySource = null; entryPercent = false; }
        string next = entry == "0" ? digit.ToString(CultureInfo.InvariantCulture)
            : entry == "-0" ? "-" + digit.ToString(CultureInfo.InvariantCulture)
            : entry + digit.ToString(CultureInfo.InvariantCulture);
        if (!WithinLength(next)) return Fail(IssueTooLong);
        entry = next;
        return Changed();
    }

    private Level3CalcResult Point()
    {
        if (ShowingResult) Result = null;
        if (entrySource != null || entryPercent) { entry = ""; entrySource = null; entryPercent = false; }
        if (entry.Contains(".")) return default;
        entry = entry.Length == 0 ? "0." : entry == "-" ? "-0." : entry + ".";
        return Changed();
    }

    private Level3CalcResult Operator(string op)
    {
        if (HasEntryNumber)
        {
            tokens.Add(EntryToken());
            ClearEntryState();
        }
        else if (entry == "-")
        {
            // "-" then another operator: the negative sign is dropped, nothing else changes.
            if (op == "-") return default;
            entry = "";
            if (tokens.Count == 0) return Fail(IssueNoNumber);
        }
        else if (tokens.Count == 0)
        {
            if (Result != null)
            {
                // Continue from the result on the display.
                tokens.Add(new Level3CalcToken { kind = Level3CalcToken.Number, value = Result, sourceId = PreviousResultSource, label = "result" });
                Result = null;
            }
            else if (op == "-")
            {
                entry = "-";
                return Changed();
            }
            else return Fail(IssueNoNumber);
        }
        else if (tokens[tokens.Count - 1].IsOperator)
        {
            // Two operators in a row: the newest one replaces the old one.
            tokens[tokens.Count - 1].op = op;
            return Changed();
        }
        tokens.Add(new Level3CalcToken { kind = Level3CalcToken.Operator, op = op });
        return Changed();
    }

    private Level3CalcResult Percent()
    {
        if (!HasEntryNumber || entryPercent) return Fail(IssueNoNumber);
        if (!TryParse(entry, out decimal value)) return Fail(IssueNoNumber);
        entry = Canonical(value / 100m);
        entryPercent = true;
        return Changed();
    }

    private Level3CalcResult EqualsKey()
    {
        var expression = new List<Level3CalcToken>(tokens.Count + 1);
        foreach (Level3CalcToken token in tokens) expression.Add(token.Copy());
        if (HasEntryNumber) expression.Add(EntryToken());
        if (expression.Count == 0) return Result != null ? default : Fail(IssueNothingToCalculate);
        if (expression[expression.Count - 1].IsOperator) return Fail(IssueUnfinished);

        string problem = Evaluate(expression, out decimal value);
        if (problem != null) return Fail(problem);

        // The chain keeps what produced a continued result (T07); a fresh calculation starts a new chain.
        var chain = new List<Level3CalcToken>();
        if (expression[0].sourceId == PreviousResultSource && lastChain != null)
        {
            foreach (Level3CalcToken token in lastChain) chain.Add(token.Copy());
            chain.Add(new Level3CalcToken { kind = Level3CalcToken.Operator, op = ChainStep });
            for (int i = 1; i < expression.Count; i++) chain.Add(expression[i].Copy());
        }
        else
            foreach (Level3CalcToken token in expression) chain.Add(token.Copy());

        tokens.Clear();
        ClearEntryState();
        Result = Canonical(value);
        DraftExpression = expression;
        DraftResult = Result;
        DraftChain = chain;
        lastChain = chain;
        return new Level3CalcResult { Changed = true, Printed = true };
    }

    private Level3CalcResult ClearAll()
    {
        bool had = tokens.Count > 0 || entry.Length > 0 || Result != null;
        tokens.Clear();
        ClearEntryState();
        Result = null;
        return had ? Changed() : default;
    }

    private Level3CalcResult ClearEntryKey()
    {
        if (entry.Length == 0 && entrySource == null) return default;
        ClearEntryState();
        return Changed();
    }

    private Level3CalcResult Backspace()
    {
        if (entrySource != null || entryPercent)
        {
            ClearEntryState();
            return Changed();
        }
        if (entry.Length > 0)
        {
            entry = entry.Substring(0, entry.Length - 1);
            return Changed();
        }
        if (tokens.Count > 0 && tokens[tokens.Count - 1].IsOperator)
        {
            // Remove the operator and bring the number before it back for editing.
            tokens.RemoveAt(tokens.Count - 1);
            Level3CalcToken back = tokens[tokens.Count - 1];
            tokens.RemoveAt(tokens.Count - 1);
            entry = back.value;
            entryPercent = back.percent;
            entrySource = back.manual || back.sourceId == "" ? null : back;
            return Changed();
        }
        return default;
    }

    private bool HasEntryNumber => entry.Length > 0 && entry != "-" && TryParse(entry, out _);

    private Level3CalcToken EntryToken()
    {
        TryParse(entry, out decimal value);
        if (entrySource != null && !entryPercent)
        {
            Level3CalcToken source = entrySource.Copy();
            source.value = Canonical(value);
            return source;
        }
        return new Level3CalcToken
        {
            kind = Level3CalcToken.Number,
            value = Canonical(value),
            manual = entrySource == null,
            sourceId = entrySource != null ? entrySource.sourceId : "",
            label = entrySource != null ? entrySource.label : ManualLabel,
            percent = entryPercent
        };
    }

    private void ClearEntryState()
    {
        entry = "";
        entrySource = null;
        entryPercent = false;
    }

    private Level3CalcResult Fail(string issue)
    {
        Issue = issue;
        return new Level3CalcResult { Changed = true, Issue = issue };
    }

    private static Level3CalcResult Changed() => new Level3CalcResult { Changed = true };

    private static bool WithinLength(string text)
    {
        string digits = text.StartsWith("-", StringComparison.Ordinal) ? text.Substring(1) : text;
        int point = digits.IndexOf('.');
        int integer = point < 0 ? digits.Length : point;
        int fraction = point < 0 ? 0 : digits.Length - point - 1;
        return integer <= MaxIntegerDigits && fraction <= MaxFractionDigits;
    }

    // =========================================================
    // ARITHMETIC (exact)
    // =========================================================

    /// <summary>
    /// Evaluates number (op number)* with × ÷ before + −. Null on success; otherwise
    /// the input issue (malformed, divide by zero, too large). Exact decimals only.
    /// </summary>
    public static string Evaluate(IReadOnlyList<Level3CalcToken> expression, out decimal value)
    {
        value = 0m;
        if (expression == null || expression.Count == 0 || expression.Count % 2 == 0) return IssueUnfinished;
        try
        {
            var terms = new List<decimal>();
            var signs = new List<string>();
            if (!TryNumber(expression[0], out decimal current)) return IssueUnfinished;
            for (int i = 1; i < expression.Count; i += 2)
            {
                Level3CalcToken op = expression[i];
                if (op == null || !op.IsOperator || !TryNumber(expression[i + 1], out decimal next)) return IssueUnfinished;
                switch (op.op)
                {
                    case "*": current *= next; break;
                    case "/":
                        if (next == 0m) return IssueDivideByZero;
                        current /= next;
                        break;
                    case "+":
                    case "-":
                        terms.Add(current);
                        signs.Add(op.op);
                        current = next;
                        break;
                    default: return IssueUnfinished;
                }
                if (Math.Abs(current) > MaxMagnitude) return IssueTooLarge;
            }
            terms.Add(current);
            decimal total = terms[0];
            for (int i = 0; i < signs.Count; i++)
            {
                total = signs[i] == "+" ? total + terms[i + 1] : total - terms[i + 1];
                if (Math.Abs(total) > MaxMagnitude) return IssueTooLarge;
            }
            value = total;
            return null;
        }
        catch (OverflowException)
        {
            return IssueTooLarge;
        }
    }

    /// <summary>
    /// T07: evaluates a chain: steps split at the ChainStep separator; the
    /// first step is an ordinary expression, every later step starts with an
    /// operator applied to the previous step's exact result (precedence holds
    /// inside a step). A chain without separators is just Evaluate.
    /// </summary>
    public static string EvaluateChain(IReadOnlyList<Level3CalcToken> chain, out decimal value)
    {
        value = 0m;
        List<List<Level3CalcToken>> steps = ChainSteps(chain);
        if (steps.Count == 0) return IssueUnfinished;
        string problem = Evaluate(steps[0], out decimal current);
        if (problem != null) return problem;
        for (int i = 1; i < steps.Count; i++)
        {
            List<Level3CalcToken> step = steps[i];
            if (step.Count < 2 || step[0] == null || !step[0].IsOperator) return IssueUnfinished;
            var withPrevious = new List<Level3CalcToken>(step.Count + 1) { new Level3CalcToken { kind = Level3CalcToken.Number, value = Canonical(current) } };
            withPrevious.AddRange(step);
            problem = Evaluate(withPrevious, out current);
            if (problem != null) return problem;
        }
        value = current;
        return null;
    }

    /// <summary>A chain's steps (split at ChainStep separators; the separators themselves are dropped).</summary>
    public static List<List<Level3CalcToken>> ChainSteps(IReadOnlyList<Level3CalcToken> chain)
    {
        var steps = new List<List<Level3CalcToken>>();
        if (chain == null || chain.Count == 0) return steps;
        var current = new List<Level3CalcToken>();
        foreach (Level3CalcToken token in chain)
        {
            if (token != null && token.IsOperator && token.op == ChainStep)
            {
                steps.Add(current);
                current = new List<Level3CalcToken>();
                continue;
            }
            current.Add(token);
        }
        steps.Add(current);
        return steps;
    }

    /// <summary>A chain as printed tape lines, each step closed by its own "= result" line.</summary>
    public static List<string> ChainLines(IReadOnlyList<Level3CalcToken> chain)
    {
        var lines = new List<string>();
        List<List<Level3CalcToken>> steps = ChainSteps(chain);
        var sofar = new List<Level3CalcToken>();
        for (int i = 0; i < steps.Count; i++)
        {
            if (i > 0) sofar.Add(new Level3CalcToken { kind = Level3CalcToken.Operator, op = ChainStep });
            sofar.AddRange(steps[i]);
            string result = EvaluateChain(sofar, out decimal value) == null ? Canonical(value) : null;
            lines.AddRange(TapeLines(steps[i], result));
        }
        return lines;
    }

    private static bool TryNumber(Level3CalcToken token, out decimal value)
    {
        value = 0m;
        return token != null && token.IsNumber && TryParse(token.value, out value);
    }

    /// <summary>Invariant decimal text ("100", "100.00", "-0.2") -> exact value.</summary>
    public static bool TryParse(string text, out decimal value)
    {
        value = 0m;
        if (string.IsNullOrWhiteSpace(text) || text.EndsWith(".", StringComparison.Ordinal) && text.Length == 1) return false;
        return decimal.TryParse(text, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out value);
    }

    /// <summary>Exact value as invariant text without trailing zeros ("80", "0.2", "-12.5").</summary>
    public static string Canonical(decimal value)
    {
        string text = value.ToString(CultureInfo.InvariantCulture);
        if (text.Contains("."))
        {
            text = text.TrimEnd('0');
            if (text.EndsWith(".", StringComparison.Ordinal)) text = text.Substring(0, text.Length - 1);
        }
        return text == "-0" ? "0" : text;
    }

    // =========================================================
    // DISPLAY AND TAPE TEXT
    // =========================================================

    /// <summary>"80" -> "80.00", "1234.5" -> "1,234.50", 1/3 -> "0.333333…" (display only; the value stays exact).</summary>
    public static string Format(string canonical)
    {
        if (!TryParse(canonical, out decimal value)) return canonical ?? "";
        string text = value.ToString("#,0.00####", CultureInfo.InvariantCulture);
        if (decimal.Round(value, MaxFractionDigits) != value) text += "…";
        return text;
    }

    /// <summary>The operator as printed on keys and tape.</summary>
    public static string Symbol(string op)
    {
        switch (op)
        {
            case "+": return "+";
            case "-": return "−";
            case "*": return "×";
            case "/": return "÷";
            default: return op ?? "";
        }
    }

    /// <summary>What the display shows: the number being typed, the result, or 0.</summary>
    public string DisplayText
    {
        get
        {
            if (entry == "-") return "−";
            if (entry.Length > 0)
            {
                if (entryPercent && TryParse(entry, out decimal fraction)) return Format(Canonical(fraction * 100m)) + "%";
                // A source amount reads as printed on its paper ("100.00").
                if (entrySource != null) return Format(entry);
                return entry.EndsWith(".", StringComparison.Ordinal) ? GroupInteger(entry) : GroupTyped(entry);
            }
            if (Result != null) return Format(Result);
            return "0";
        }
    }

    /// <summary>The small line under the number: an input problem, the source in use, or the expression so far.</summary>
    public string Caption
    {
        get
        {
            if (Issue != null) return Issue;
            if (entrySource != null) return "from " + entrySource.label;
            if (tokens.Count > 0)
            {
                var parts = new List<string>();
                foreach (Level3CalcToken token in tokens) parts.Add(token.IsOperator ? Symbol(token.op) : Format(token.value));
                return string.Join(" ", parts);
            }
            return ShowingResult ? "result" : "";
        }
    }

    /// <summary>The draft tape as printed lines: first number, then "op number  label", then "= result".</summary>
    public static List<string> TapeLines(IReadOnlyList<Level3CalcToken> expression, string result)
    {
        var lines = new List<string>();
        if (expression == null) return lines;
        string pendingOp = null;
        foreach (Level3CalcToken token in expression)
        {
            if (token == null) continue;
            if (token.IsOperator) { pendingOp = token.op; continue; }
            string number = token.percent && TryParse(token.value, out decimal f) ? Format(Canonical(f * 100m)) + "%" : Format(token.value);
            string tag = token.manual || string.IsNullOrEmpty(token.label) ? "" : " " + Tag(token.label);
            lines.Add((pendingOp != null ? Symbol(pendingOp) + " " : "") + number + tag);
            pendingOp = null;
        }
        if (result != null) lines.Add("= " + Format(result));
        return lines;
    }

    /// <summary>A short tape tag from a source label: its first word, upper case, at most 8 letters ("Paper order" -> "PAPER").</summary>
    public static string Tag(string label)
    {
        string word = (label ?? "").Trim();
        int space = word.IndexOf(' ');
        if (space > 0) word = word.Substring(0, space);
        word = word.TrimEnd(':', ',', '.').ToUpperInvariant();
        return word.Length > 8 ? word.Substring(0, 8) : word;
    }

    private static string GroupTyped(string typed)
    {
        int point = typed.IndexOf('.');
        string integer = point < 0 ? typed : typed.Substring(0, point);
        string fraction = point < 0 ? "" : typed.Substring(point);
        return GroupInteger(integer) + fraction;
    }

    private static string GroupInteger(string text)
    {
        bool negative = text.StartsWith("-", StringComparison.Ordinal);
        string digits = negative ? text.Substring(1) : text;
        string suffix = "";
        if (digits.EndsWith(".", StringComparison.Ordinal)) { suffix = "."; digits = digits.Substring(0, digits.Length - 1); }
        if (digits.Length == 0) return (negative ? "−" : "") + suffix;
        decimal.TryParse(digits, NumberStyles.None, CultureInfo.InvariantCulture, out decimal whole);
        return (negative ? "−" : "") + whole.ToString("#,0", CultureInfo.InvariantCulture) + suffix;
    }
}
