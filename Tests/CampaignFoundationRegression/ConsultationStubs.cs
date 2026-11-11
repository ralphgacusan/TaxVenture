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

// Level 3 T09 test stub: PlayerProgressData's Level 3 case progress keeps a
// ReviewClockState (Assets/Scripts/Data/ReviewClock.cs, linked in the csproj),
// whose rules read this definition. The real class lives in
// CaseReviewDefinition.cs with Level 1/2 definition dependencies.
// Must match Assets/Scripts/Data/Definitions/CaseReviewDefinition.cs.
[Serializable]
public class ReviewClockDefinition
{
    public float allowanceSeconds = 720f;
    public System.Collections.Generic.List<float> warnSeconds = new System.Collections.Generic.List<float> { 120f, 30f };
    public int lateExp = 3;
    public int lateHearts = 1;
    public string announceLine;
    public string warnLine;
    public string missedLine;
    public string nextMorningLine;
    public string onTimeLine;

    public bool IsSet => allowanceSeconds > 0f;
}
