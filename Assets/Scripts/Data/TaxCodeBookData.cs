using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// PURPOSE:
/// Static, authored reference content for the Tax Code Book, per the design
/// doc's Section 1-12 Simplified Tax Code Book. This is a ScriptableObject
/// (unlike CaseData) because this content is identical every playthrough
/// and edited by the designer, not by gameplay — a proper fit for
/// authored/static data rather than runtime mutable state.
///
/// HOW TO CREATE THE ASSET:
/// Right-click in the Project window -> Create -> TaxGame -> Tax Code Book
/// Data. Only ONE instance should exist for this prototype (Phase 1 scope:
/// one book, one case).
///
/// CONNECTS WITH:
/// - TaxCodeBookUI: reads the Sections list to render pages
/// </summary>
[CreateAssetMenu(fileName = "TaxCodeBookData", menuName = "TaxGame/Tax Code Book Data")]
public class TaxCodeBookData : ScriptableObject
{
    public List<TaxCodeSection> sections = new List<TaxCodeSection>();
}