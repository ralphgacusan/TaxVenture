using System.Collections.Generic;

public class InterviewSection
{
    public string SectionTitle;
    public List<InterviewQuestion> Questions;

    public InterviewSection(string title, List<InterviewQuestion> questions)
    {
        SectionTitle = title;
        Questions = questions;
    }

    public bool IsComplete()
    {
        foreach (var q in Questions)
            if (!q.IsAnswered) return false;
        return true;
    }
}