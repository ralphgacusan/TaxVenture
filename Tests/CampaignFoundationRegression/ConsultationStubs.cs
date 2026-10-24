// T23 test stub: the one field shape ClientConsultationRules reads from the
// case definitions (the real class lives in CaseDefinition.cs with Unity-side
// dependencies). Must match Assets/Scripts/Data/Definitions/CaseDefinition.cs.
using System;

[Serializable]
public class DialogueLineDefinition
{
    public string speaker;
    public string text;
    public string portraitId;
    public string emote;
    public bool burst;
}
