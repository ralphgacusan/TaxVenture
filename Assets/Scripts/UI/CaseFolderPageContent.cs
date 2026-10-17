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

    public CaseFolderPageContent(string heading, string body)
    {
        Heading = heading;
        Body = body;
    }
}