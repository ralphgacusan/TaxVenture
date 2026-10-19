using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Creates the handover records as removable source papers in the supporting stack.</summary>
public sealed class Level2RecordPaper : MonoBehaviour
{
    private readonly List<Level2FindingsSheet> detachedPapers = new List<Level2FindingsSheet>();
    private CaseData boundCase;

    public void Draw(CaseFolderUI.PageBuilder page, Level2SupportingStack stack)
    {
        CaseData data = CaseManager.Instance != null ? CaseManager.Instance.CurrentCase : null;
        CaseReviewState review = data?.review;
        CaseDefinition definition = CaseManager.Instance != null ? CaseManager.Instance.CurrentDefinition : null;
        if (data != boundCase) ResetForCase(data);
        if (data == null || review == null || !review.HandoverDone || definition == null ||
            !definition.HasReview || definition.review.documents == null || definition.documents == null)
            return;

        foreach (ReviewDocumentDefinition candidate in definition.review.documents)
        {
            // Only the records Rosa hands over are papers in the stack. Old
            // checkpoints may still hold retired records (D2 bank statement, D6
            // cash receipt); they stay in the save but are not printed.
            if (candidate == null || candidate.kind != "record" || !review.HasDocument(candidate.id) ||
                !Handed(definition.review, candidate.id) || HasDetached(candidate.id))
                continue;

            DocumentDefinition document = definition.documents.Find(d => d != null &&
                d.documentName == candidate.documentName);
            if (document == null) continue;

            Level2FindingsSheet.CreateRecord(page, stack, candidate.id, candidate.title,
                (owner, rows) => DrawRecord(owner, rows, document), TrackDetached, ForgetDetached,
                document.fields != null ? document.fields.FindAll(f => f != null).Count : 0);
        }
    }

    private static bool Handed(CaseReviewDefinition review, string id) =>
        review.handoverDocumentIds == null || review.handoverDocumentIds.Count == 0 ? id != "D6" : review.handoverDocumentIds.Contains(id);

    public void SyncVisibility()
    {
        for (int i = detachedPapers.Count - 1; i >= 0; i--)
        {
            Level2FindingsSheet paper = detachedPapers[i];
            if (paper == null) detachedPapers.RemoveAt(i);
            else paper.SyncVisibility();
        }
    }

    public void DisposeDetached()
    {
        while (detachedPapers.Count > 0)
        {
            int last = detachedPapers.Count - 1;
            Level2FindingsSheet paper = detachedPapers[last];
            detachedPapers.RemoveAt(last);
            if (paper != null) paper.Dispose();
        }
    }

    private void ResetForCase(CaseData data)
    {
        DisposeDetached();
        boundCase = data;
    }

    private bool HasDetached(string id)
    {
        for (int i = detachedPapers.Count - 1; i >= 0; i--)
        {
            Level2FindingsSheet paper = detachedPapers[i];
            if (paper == null) detachedPapers.RemoveAt(i);
            else if (paper.PaperId == id) return true;
        }
        return false;
    }

    private void TrackDetached(Level2FindingsSheet paper)
    {
        if (paper != null && !detachedPapers.Contains(paper)) detachedPapers.Add(paper);
    }

    private void ForgetDetached(Level2FindingsSheet paper) => detachedPapers.Remove(paper);

    private static void DrawRecord(CaseFolderUI.PageBuilder page, RectTransform rows, DocumentDefinition document)
    {
        CaseFolderUI.MarkReviewDocumentRead(document.documentName);
        if (document.fields == null) return;
        int index = 0;
        foreach (DocumentFieldDefinition field in document.fields)
        {
            if (field == null) continue;
            DataValueType type = System.Enum.TryParse(field.type, out DataValueType parsed)
                ? parsed : DataValueType.Text;
            object value = field.displayValue;
            if (type == DataValueType.Number)
            {
                if (CaseFolderUI.TryParseAmount(field.displayValue, out float amount)) value = amount;
                else type = DataValueType.Text;
            }

            CaseFolderFieldRow row = rows.childCount > index
                ? rows.GetChild(index).GetComponent<CaseFolderFieldRow>()
                : page.CreateSheetRow(rows);
            if (row == null) continue;
            row.InitializeSourceOnly(field.label, value, type, field.semanticKey);
            Level2FindingsSheet.StyleRecordRow(row.gameObject, index);
            index++;
        }
    }
}
