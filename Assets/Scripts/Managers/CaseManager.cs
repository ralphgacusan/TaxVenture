using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// PURPOSE:
/// Holds the currently active CaseData instance and provides a single access
/// point for any script that needs to read or write case information.
///
/// RESPONSIBILITIES:
/// - Own the "current case" data for the session
/// - Create a fresh CaseData with placeholder values on scene start
///   (Phase 1 scope: only one hardcoded case, per your brief)
///
/// DOES NOT:
/// - Contain any UI logic — CaseFolderUI reads FROM this manager but this
///   manager knows nothing about how data is displayed.
///
/// CONNECTS WITH:
/// - CaseFolderUI: reads CurrentCase to populate folder pages
/// - Future: InterviewClientState, ComputeTaxesState, StampAssessmentState,
///   Save System — all read/write through CaseManager.Instance.CurrentCase
/// </summary>
public class CaseManager : MonoBehaviour
{
    public static CaseManager Instance { get; private set; }

    public CaseData CurrentCase { get; private set; }

    private void Awake()
    {
        Instance = this;
        CurrentCase = CreatePlaceholderCase();
    }

    /// <summary>
    /// Builds the single hardcoded case for this Phase 1 prototype, using the
    /// example values from the design document's folder pages. In a later
    /// milestone (multi-case support), this would instead load case data
    /// from a case database or save file.
    /// </summary>
    private CaseData CreatePlaceholderCase()
    {
        CaseData data = new CaseData();

        // Populate supporting documents list (Page 6), matching the design doc.
        data.supportingDocuments = new List<SupportingDocument>
        {
            new SupportingDocument("BIR Form 2316"),
            new SupportingDocument("BIR Form 2303"),
            new SupportingDocument("Financial Statements"),
            new SupportingDocument("Sales Records"),
            new SupportingDocument("Bank Statements"),
            new SupportingDocument("Property Documents"),
            new SupportingDocument("Previous Year's ITR"),
        };

        return data;
    }
}