using System;
using System.Collections.Generic;

/// <summary>
/// Converts an immutable CaseDefinition (JSON) into a fresh, mutable
/// CaseData instance — your existing runtime class.
///
/// Also provides conversion helpers for JSON documents and dialogue.
/// </summary>
public static class CaseFactory
{
    public static CaseData CreateCaseData(CaseDefinition def)
    {
        var data = new CaseData
        {
            caseNumber = def.caseNumber,
            taxYear = def.taxYear,
            dateReceived = def.dateReceived,
            assignedConsultant = def.assignedConsultant,
            caseTitle = def.caseTitle,
            caseSummary = def.caseSummary,

            caseAssessment = CaseAssessment.NotReadyForFiling,
            assessmentStamped = false,

            // ---------- Page 2: Taxpayer Information ----------
            fullName = def.fullName,
            tin = def.tin,
            birthdate = def.birthdate,
            address = def.address,
            contactNumber = def.contactNumber,
            civilStatus = ParseEnumOrDefault(
                def.civilStatus,
                CivilStatus.Single
            ),
            spouseName = def.spouseName,
            spouseTin = def.spouseTin,
            citizenship = def.citizenship,

            // ---------- Player-discovered values ----------
            // IMPORTANT:
            // These intentionally do NOT come from answerKey.
            // The player must transfer/discover them during gameplay.
            residencyStatus = null,
            taxpayerType = null,
            incomeSource = null,
            numberOfEmployers = null,
            businessRegistration = null,
            taxOption = null,

            // ---------- Tax Computation ----------
            // These remain empty until the player performs the
            // appropriate gameplay actions.
            grossIncome = 0f,
            allowableExpenses = 0f,
            taxableIncome = 0f,
            taxDue = 0f,
            taxWithheldOrCredits = 0f,
            finalTaxPayable = 0f,

            computationStatus = ComputationStatus.NotComputed,

            folderTransferredFields = new HashSet<string>(),

            // ---------- Filing ----------
            requiredForm = null,
            filingStatus = FilingStatus.NotReady,
            submissionDate = "Date",
            remarks = "Remarks",

            // ---------- Documents ----------
            supportingDocuments = BuildSupportingDocumentList(def),

            // ---------- Findings ----------
            potentialIssuesIdentified =
                new List<string>(def.potentialIssues),

            // ---------- Audit ----------
            auditMistakeCount = 0,
            auditPassed = false,

            // ---------- Tax Return ----------
            hasPrintedReturn = false,
            isCarryingPrintedReturn = false,

            // ---------- Outcome / Archive ----------
            clientPresentationCompleted = false,
            isArchived = false,
            isCarryingCaseFolder = true,

            encodedForm = null,

            // ---------- Final Audit Result ----------
            finalVerdictWasCorrect = false,
            finalMissedIssueCount = 0
        };

        return data;
    }


    /// <summary>
    /// Converts JSON document definitions into the existing
    /// SupportingDocument runtime objects.
    /// </summary>
    private static List<SupportingDocument> BuildSupportingDocumentList(
        CaseDefinition def)
    {
        var list = new List<SupportingDocument>();

        if (def.documents == null)
            return list;

        foreach (var docDef in def.documents)
        {
            if (docDef == null)
                continue;

            list.Add(
                new SupportingDocument(docDef.documentName)
            );
        }

        return list;
    }


    /// <summary>
    /// Converts a JSON DocumentDefinition into the existing
    /// DocumentFieldData structure.
    ///
    /// This replaces the old hardcoded switch that used to live
    /// inside DocumentDataProvider.
    ///
    /// displayValue is what the player sees.
    /// correctValue remains inside the JSON definition and will be
    /// used later by the Phase 4 validation system.
    /// </summary>
    public static DocumentFieldData BuildDocumentFieldData(
        DocumentDefinition docDef)
    {
        if (docDef == null)
        {
            return new DocumentFieldData("Unknown Document")
                .AddField(
                    "(No data available)",
                    "",
                    DataValueType.Text,
                    ""
                );
        }

        var result = new DocumentFieldData(
            docDef.documentName
        );

        if (docDef.fields == null)
            return result;

        foreach (var field in docDef.fields)
        {
            if (field == null)
                continue;

            DataValueType type =
                ParseEnumOrDefault(
                    field.type,
                    DataValueType.Text
                );

            result.AddField(
                field.label,
                field.displayValue,
                type,
                field.semanticKey
            );
        }

        return result;
    }


    /// <summary>
    /// Converts JSON dialogue definitions into the existing
    /// DialogueLine objects using the existing DialogueBuilder.
    ///
    /// The DialogueBuilder itself is unchanged.
    /// Only the source of the dialogue content is now JSON.
    /// </summary>
    public static List<DialogueLine> BuildDialogue(
        List<DialogueLineDefinition> jsonLines,
        string npcName)
    {
        var builder = new DialogueBuilder(npcName);

        if (jsonLines == null || jsonLines.Count == 0)
        {
            return builder.Build();
        }

        foreach (var line in jsonLines)
        {
            if (line == null)
                continue;

            if (line.speaker == "Npc")
            {
                builder.Npc(
                    line.text,
                    line.portraitId
                );
            }
            else
            {
                builder.Player(
                    line.text,
                    line.portraitId
                );
            }
        }

        return builder.Build();
    }


    /// <summary>
    /// Safely converts a JSON string into an enum.
    /// </summary>
    private static T ParseEnumOrDefault<T>(
        string value,
        T fallback
    ) where T : struct
    {
        if (string.IsNullOrEmpty(value))
            return fallback;

        return Enum.TryParse(
            value,
            out T result
        )
            ? result
            : fallback;
    }
}

