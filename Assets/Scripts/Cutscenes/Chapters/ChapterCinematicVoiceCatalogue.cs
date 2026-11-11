using System;
using UnityEngine;

/// <summary>One chapter presentation's clip references; recordings remain in AudioManager/Cutscene_Dialogues.</summary>
[CreateAssetMenu(menuName = "TaxVenture/Cutscene Voice Catalogue")]
public sealed class ChapterCinematicVoiceCatalogue : ScriptableObject
{
    [Serializable]
    public sealed class Entry
    {
        public string id;
        public string speaker;
        [TextArea] public string text;
        public string variant;
        public AudioClip clip;
    }

    public Entry[] entries = new Entry[0];

    public AudioClip Find(ChapterCinematicCue cue)
    {
        foreach (Entry entry in entries)
            if (entry != null && ChapterCinematicVoiceRules.Matches(cue, entry.id, entry.speaker, entry.text, entry.variant))
                return entry.clip;
        return null;
    }
}
