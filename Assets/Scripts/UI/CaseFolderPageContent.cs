/// <summary>
/// PURPOSE:
/// Plain data describing what text to show on the single folder "paper" for
/// one page. Replaces the old per-page GameObject approach — instead of 7
/// separate panels, we have 7 of these lightweight data entries, and the
/// same paper UI just rewrites its text fields to match whichever entry is
/// currently selected.
///
/// CONNECTS WITH:
/// - CaseFolderUI builds a list of these (one per page) and displays the
///   current one's fields on the paper's TextMeshPro components.
/// </summary>
public class CaseFolderPageContent
{
    public string Heading;
    public string Body;

    /// <summary>
    /// Level 2+: the case document this page prints (review page), or null
    /// for the Taxpayer Information page.
    /// </summary>
    public DocumentDefinition Document;

    /// <summary>
    /// Level-supplied page (e.g. Level 2's Findings page): draws its rows
    /// through the builder, in page 1's row area and style.
    /// </summary>
    public System.Action<CaseFolderUI.PageBuilder> Render;

    /// <summary>
    /// The page's stable identity in its folder (e.g. SupportingRecordsRole), found with
    /// CaseFolderUI.FindPageIndexByRole; never a fixed page number. Empty for ordinary pages.
    /// </summary>
    public string Role;

    /// <summary>A folder's attached supporting records (Arthur's file, T30; Marie's case, T25).</summary>
    public const string SupportingRecordsRole = "supporting-records";
    /// <summary>The printed heading of that page, the same in every folder.</summary>
    public const string SupportingRecordsHeading = "Supporting Documents";
    /// <summary>A page that lists the case's documents as buttons (SupportingDocumentsPageLink); no level uses it now.</summary>
    public const string DocumentLinksRole = "document-links";

    public CaseFolderPageContent(string heading, string body)
    {
        Heading = heading;
        Body = body;
    }
}