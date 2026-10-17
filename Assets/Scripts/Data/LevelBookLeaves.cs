using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// PURPOSE:
/// Optional extra Tax Code Book leaves for one level, from
/// Assets/Resources/Levels/&lt;levelId&gt;/book.json. TaxCodeBookUI shows them
/// after the shared TaxCodeBookData sections, so a level can teach new rules
/// without changing the shared book asset other levels use. Level 1 has no
/// book.json, so its book is unchanged.
///
/// JSON:
/// { "sections": [ { "heading": "...", "body": "...", "clickableValues": [] } ] }
///
/// Leaves explain concepts only: never a client's amounts, finding IDs or the
/// stamp to choose. Leaves without a heading or body are skipped with a warning.
/// </summary>
public static class LevelBookLeaves
{
#pragma warning disable 0649
    [Serializable]
    private class BookFile
    {
        public List<TaxCodeSection> sections;
    }
#pragma warning restore 0649

    public static string ResourcePath(string levelId) => $"Levels/{levelId}/book";

    /// <summary>The level's extra leaves (empty when it has none).</summary>
    public static List<TaxCodeSection> Load(string levelId)
    {
        var leaves = new List<TaxCodeSection>();
        if (string.IsNullOrEmpty(levelId)) return leaves;

        string path = ResourcePath(levelId);
        TextAsset file = Resources.Load<TextAsset>(path);
        if (file == null) return leaves;

        BookFile parsed;
        try
        {
            parsed = JsonUtility.FromJson<BookFile>(file.text);
        }
        catch (Exception error)
        {
            Debug.LogWarning($"[LevelBookLeaves] Resources/{path}.json is not valid JSON ({error.Message}); no extra leaves.");
            return leaves;
        }

        if (parsed == null || parsed.sections == null) return leaves;
        for (int i = 0; i < parsed.sections.Count; i++)
        {
            TaxCodeSection section = parsed.sections[i];
            if (section == null || string.IsNullOrWhiteSpace(section.heading) || string.IsNullOrWhiteSpace(section.body))
            {
                Debug.LogWarning($"[LevelBookLeaves] Resources/{path}.json leaf {i + 1} needs a heading and a body; skipped.");
                continue;
            }
            if (section.clickableValues == null) section.clickableValues = new List<TaxCodeClickableValue>();
            leaves.Add(section);
        }
        return leaves;
    }
}
