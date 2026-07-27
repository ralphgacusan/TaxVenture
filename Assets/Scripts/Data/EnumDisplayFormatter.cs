using System.Text.RegularExpressions;

/// <summary>
/// PURPOSE:
/// Converts PascalCase enum names (e.g. "MixedIncomeEarner") into readable
/// spaced text ("Mixed Income Earner") for display anywhere in the game.
/// Previously duplicated as a private method inside CaseFolderUI — pulled
/// out into a shared static utility so every UI that displays an enum value
/// (Folder pages, Interview answers, Compliance issues, etc.) uses the same
/// formatting, rather than each screen inventing its own.
/// </summary>
public static class EnumDisplayFormatter
{
    public static string Format(string enumName)
    {
        if (string.IsNullOrEmpty(enumName)) return enumName;
        return Regex.Replace(enumName, "(\\B[A-Z])", " $1");
    }

    /// <summary>Convenience overload for nullable enums — returns "?" if null, matching the Folder's existing convention for unknown fields.</summary>
    public static string Format<T>(T? value) where T : struct
    {
        return value.HasValue ? Format(value.Value.ToString()) : "?";
    }
}