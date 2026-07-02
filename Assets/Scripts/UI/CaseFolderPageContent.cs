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

    public CaseFolderPageContent(string heading, string body)
    {
        Heading = heading;
        Body = body;
    }
}