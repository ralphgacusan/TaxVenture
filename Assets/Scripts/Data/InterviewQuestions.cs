using System;

/// <summary>
/// PURPOSE:
/// Represents one interview question: the question text shown to the player,
/// the client's canned answer text, and an action that writes the answer
/// into CaseData when selected. This directly models the design doc's
/// interview flow: "Questions are based on missing information... client
/// provides answers... missing information becomes available."
///
/// WHY A DELEGATE (Action) FOR THE ANSWER EFFECT:
/// Each question fills a different field with a different type (enum,
/// string, etc.) in CaseData. Rather than building a rigid "field name +
/// value" data structure and a switch statement to apply it, we let each
/// question carry its own small lambda that knows exactly how to write its
/// answer. This keeps InterviewClientUI generic — it doesn't need to know
/// what CaseData fields exist at all.
///
/// CONNECTS WITH:
/// - InterviewDataProvider: builds the list of questions for the case
/// - InterviewClientUI: displays question text, calls ApplyAnswer() on click
/// </summary>
public class InterviewQuestion
{
    public string QuestionText;
    public string ClientAnswerText;
    public bool IsAnswered { get; private set; }

    private readonly Action<CaseData> applyAnswer;

    public InterviewQuestion(string questionText, string clientAnswerText, Action<CaseData> applyAnswer)
    {
        QuestionText = questionText;
        ClientAnswerText = clientAnswerText;
        this.applyAnswer = applyAnswer;
        IsAnswered = false;
    }

    /// <summary>
    /// Writes this question's answer into the given CaseData and marks the
    /// question as answered. Safe to call multiple times (idempotent) in
    /// case the player re-asks an already-answered question.
    /// </summary>
    public void ApplyAnswer(CaseData data)
    {
        applyAnswer?.Invoke(data);
        IsAnswered = true;
    }
}