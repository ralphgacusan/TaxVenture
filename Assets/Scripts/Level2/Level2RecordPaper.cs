using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Creates the handover records as removable source papers in the supporting stack.
/// T18: each prints its authored supporting-document type (SupportingPaperTypes,
/// case JSON paperType: D1 Type 4, D2 Type 6, D3/D4/D6 Type 5, D5 Type 7); a
/// record without a built type prints as a plain readable sheet and is named
/// once in the Console. A table longer than one face continues on another face.
/// </summary>
public sealed class Level2RecordPaper : MonoBehaviour
{
    private readonly List<Level2FindingsSheet> detachedPapers = new List<Level2FindingsSheet>();
    private readonly HashSet<string> warned = new HashSet<string>();
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
            if (document == null)
            {
                // Never another record's paper in its place: it is left out and named.
                Warn(candidate.id, $"has no case document \"{candidate.documentName}\"; it is not printed");
                continue;
            }

            List<DocumentFieldDefinition> fields = document.fields != null ? document.fields.FindAll(f => f != null) : new List<DocumentFieldDefinition>();
            SupportingPaperFace face = SupportingPaperTypes.Face(candidate, definition.fullName);
            if (face.Fallback) Warn(candidate.id, face.Problem + "; it prints as a plain record");
            foreach (DocumentFieldDefinition field in fields) face.RowKeys.Add(field.semanticKey ?? "");
            int perFace = face.Style.RowsPerFace;
            Level2FindingsSheet created = null;
            created = Level2FindingsSheet.CreateRecord(page, stack, candidate.id, candidate.title,
                (owner, rows) => DrawRecord(owner, rows, document, fields, created != null ? created.FaceIndex : 0, perFace),
                TrackDetached, ForgetDetached, Mathf.Min(fields.Count, perFace), face, SupportingPaperTypes.FaceCount(fields.Count, perFace));
        }
    }

    private void Warn(string id, string problem)
    {
        if (warned.Add(id + problem)) Debug.LogWarning($"[Level2] Record {id} {problem}.");
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

    /// <summary>Prints one face of a record: its rows from start, at most perFace (the rest wait on its next face).</summary>
    private static void DrawRecord(CaseFolderUI.PageBuilder page, RectTransform rows, DocumentDefinition document,
        List<DocumentFieldDefinition> fields, int face, int perFace)
    {
        CaseFolderUI.MarkReviewDocumentRead(document.documentName);
        SupportingPaperTypes.FaceRange(fields.Count, perFace, face, out int start, out int count);
        int index = 0;
        for (int f = start; f < start + count; f++)
        {
            DocumentFieldDefinition field = fields[f];
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
            if (!row.gameObject.activeSelf) row.gameObject.SetActive(true);
            row.InitializeSourceOnly(field.label, value, type, field.semanticKey);
            Level2FindingsSheet.StyleRecordRow(row.gameObject, index);
            index++;
        }
        // A shorter face hides the rows it does not use (they come back when their face shows).
        for (int i = index; i < rows.childCount; i++)
            if (rows.GetChild(i).gameObject.activeSelf) rows.GetChild(i).gameObject.SetActive(false);
    }
}
