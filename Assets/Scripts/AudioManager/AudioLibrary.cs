using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Build-safe references to supplied audio; clips stay in their original folders.</summary>
public sealed class AudioLibrary : ScriptableObject
{
    public AudioClip[] clips;
    public AudioClip[] gameplayMusic;
    public AudioClip[] menuMusic;
    public AudioClip[] officeMusic;
    public AudioClip[] plazaMusic;
    public AudioClip[] completionMusic;
    public AudioClip[] officeAmbience;
    public AudioClip[] outdoorAmbience;

    public AudioClip Find(string clipName)
    {
        if (string.IsNullOrEmpty(clipName)) return null;
        if (clips != null)
            foreach (AudioClip clip in clips)
                if (clip != null && clip.name == clipName) return clip;
        return null;
    }

    /// <summary>Returns numbered variants in name order, deriving the family from any member.</summary>
    public AudioClip[] FindFamily(string clipName)
    {
        if (string.IsNullOrEmpty(clipName)) return new AudioClip[0];
        string family = FamilyName(clipName);
        var matches = new List<AudioClip>();
        if (clips != null)
            foreach (AudioClip clip in clips)
                if (clip != null && FamilyName(clip.name) == family) matches.Add(clip);
        matches.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
        return matches.ToArray();
    }

    public AudioClip[] FindDialogue(bool female)
    {
        string prefix = female ? "femgib_" : "malegib";
        string longName = female ? "female gibberish long" : "male gibberish long";
        var matches = new List<AudioClip>();
        if (clips != null)
            foreach (AudioClip clip in clips)
                if (clip != null && (clip.name.StartsWith(prefix, StringComparison.Ordinal) || clip.name == longName))
                    matches.Add(clip);
        matches.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
        return matches.ToArray();
    }

    private static string FamilyName(string clipName)
    {
        if (string.IsNullOrEmpty(clipName)) return string.Empty;
        int lastUnderscore = clipName.LastIndexOf('_');
        if (lastUnderscore >= 0 && clipName.Length - lastUnderscore == 3 &&
            char.IsDigit(clipName[lastUnderscore + 1]) && char.IsDigit(clipName[lastUnderscore + 2]))
            return clipName.Substring(0, lastUnderscore);
        return clipName;
    }
}
