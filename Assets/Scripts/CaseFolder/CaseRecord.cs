using System;

[Serializable]
public class CaseRecord
{
    public string caseID;
    public CaseProgressStatus status;

    public bool isCorrect;
    public int attempts;
    public int hintsUsed;

    public CaseRecord(string id)
    {
        caseID = id;
        status = CaseProgressStatus.InStack;

        isCorrect = false;
        attempts = 0;
        hintsUsed = 0;
    }
}