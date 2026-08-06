using System.Collections.Generic;
using UnityEngine;

public class DialoguePortraitDatabase : MonoBehaviour
{
    public static DialoguePortraitDatabase Instance { get; private set; }

    [System.Serializable]
    public class PortraitEntry
    {
        public string portraitId;
        public Sprite sprite;
    }

    [SerializeField]
    private List<PortraitEntry> portraits = new();

    private Dictionary<string, Sprite> lookup;

    private void Awake()
    {
        Instance = this;

        lookup = new Dictionary<string, Sprite>();

        foreach (var entry in portraits)
        {
            lookup[entry.portraitId] = entry.sprite;
        }
    }

    public Sprite GetPortrait(string id)
    {
        if (lookup.TryGetValue(id, out Sprite sprite))
            return sprite;

        return null;
    }
}