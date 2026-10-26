using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// PURPOSE:
/// Marie's two-page Level 1 case folder (Campaign Foundation T25). Her desk
/// Case Folder keeps its manila look, its editable first page and every
/// existing path (store, hand-in, failed-review return, finale); this adds
/// page two, "Supporting Documents", to THAT folder instance only
/// (CaseFolderUI.InstancePages, found by its role, never a page number):
/// her Valid Government ID (her taxpayer profile on its back) and Employment Records, printed
/// from the case JSON's own values (CaseRecordRules) as attached physical
/// records (AttachedRecordsPage, the same holders, tap/drag, front focus,
/// screen bounds, optional two-second grouping and return as Arthur's file).
///
/// FIRST DESK: the guide asks for both records (her ID and her background,
/// like Arthur's file; Level 1 has no BIR form) to be taken out and put back.
/// Once both were shown and put back the preparation is saved through the
/// existing case checkpoint (Level1CaseRecord.workRecordsPrepared). A case
/// already under way (saved preparation, a filled entry, an audit, approval)
/// never asks again. Reading a record marks its case document read (the
/// existing CaseFolderUI.MarkReviewDocumentRead) and changes no answer,
/// reward, tier, deadline or clock rule.
///
/// ISOLATION: the records belong to this folder (owner = case id): drops on
/// another folder (Arthur's) are refused, a copy of the folder leaves them
/// behind (AttachedRecordsRoot), and record faces are never value sources.
/// Ordinary Tax Code Book transfers into Marie's entries are unchanged.
///
/// LIFETIME: added at runtime to Office_Level1 under the "Campaign
/// Foundation" root. Level 2 and other levels keep their own page structure.
/// </summary>
public sealed class CaseRecordsDesk : MonoBehaviour
{
    public const string ObjectName = "Level 1 Case Records";
    private const string DeskFolderPath = "Environment/OfficeBuilding/Rooms/ConsultantOffice/InteractiveFolderObjects/OpenCaseFolder";
    /// <summary>Guide flags (flag:...): both records shown; both shown and put back (or the case already under way).</summary>
    public const string ShownFlag = "case-records-shown";
    public const string PreparedFlag = "case-records-prepared";
    /// <summary>Her ID card was turned over to her profile (2026-10-27: the profile lives on the back of her ID).</summary>
    public const string FlippedFlag = "case-id-flipped";

    public static CaseRecordsDesk Active { get; private set; }

    /// <summary>The first-desk preparation became true (Level1FieldCheckpointer saves it).</summary>
    public static event Action Changed;

    private CaseFolderUI pages;
    private AttachedRecordsPage records;
    private List<ClientRecord> built = new List<ClientRecord>();
    private List<string> workIds = new List<string>();
    private readonly HashSet<string> shown = new HashSet<string>();
    private readonly HashSet<string> returned = new HashSet<string>();
    private bool prepared, restoredPrepared, idFlipped;

    /// <summary>The first-desk preparation is done (saved with the case).</summary>
    public bool Prepared => prepared;
    public AttachedRecordsPage Records => records;
    public IReadOnlyList<ClientRecord> Built => built;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Install()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (mode != LoadSceneMode.Single || OfficeLevelProfile.LevelIdFor(scene) != OfficeLevelCatalog.Level1Id) return;
        GameObject root = null;
        foreach (GameObject candidate in scene.GetRootGameObjects())
            if (candidate.name == MainMenuCampaignPanel.RootName) { root = candidate; break; }
        if (root == null)
        {
            root = new GameObject(MainMenuCampaignPanel.RootName);
            SceneManager.MoveGameObjectToScene(root, scene);
        }
        if (root.GetComponentInChildren<CaseRecordsDesk>(true) != null) return;
        var host = new GameObject(ObjectName);
        host.transform.SetParent(root.transform, false);
        host.AddComponent<CaseRecordsDesk>();
    }

    private void OnEnable()
    {
        Active = this;
        GuideFlags.Add(Flag);
    }

    private void OnDisable()
    {
        if (Active == this) Active = null;
        GuideFlags.Remove(Flag);
        if (pages != null)
        {
            pages.PageRendered -= HandlePageRendered;
            if (pages.InstancePages == (Func<CaseData, List<CaseFolderPageContent>>)SupportingPages) pages.InstancePages = null;
        }
    }

    // =========================================================
    // SET UP (once the case is loaded; the desk folder is still at rest)
    // =========================================================

    private IEnumerator Start()
    {
        float waited = 0f;
        while (!TryBuild())
        {
            if (waited > 10f) yield break;   // no Level 1 case on this desk (or a Level 2-style review case)
            waited += Time.unscaledDeltaTime;
            yield return null;
        }
    }

    private bool TryBuild()
    {
        CaseManager cases = CaseManager.Instance;
        if (cases == null || cases.CurrentCase == null || cases.CurrentDefinition == null) return false;
        if (cases.CurrentDefinition.HasReview) { enabled = false; return true; }
        Transform folder = Find(gameObject.scene, DeskFolderPath);
        CaseFolderUI ui = folder != null ? folder.GetComponentInChildren<CaseFolderUI>(true) : null;
        if (ui == null || ui.IsReference || ui.PaperRect == null) return false;

        built = CaseRecordRules.Build(Source(cases.CurrentDefinition));
        if (built.Count == 0) return true;
        workIds = CaseRecordRules.WorkRecordIds(built);
        pages = ui;
        pages.InstancePages = SupportingPages;
        // Two records (ID above background) like Arthur's file; side-by-side sheets only when a case has more.
        int sheets = built.FindAll(r => !r.IsCard).Count;
        records = AttachedRecordsPage.Create(gameObject, folder, pages, -1, cases.CurrentDefinition.caseId, built,
            sheets > 1 ? RecordPageLayout.CaseRecords : RecordPageLayout.Default, () => pages != null && pages.IsShowing,
            CaseFolderPageContent.SupportingRecordsRole);
        if (records == null)
        {
            Debug.LogWarning("[CaseRecords] Marie's supporting records could not be attached to the desk folder.");
            pages.InstancePages = null;
            pages = null;
            return true;
        }
        records.Shown += HandleShown;
        records.PutBack += HandlePutBack;
        records.Flipped += (record, backUp) => { if (backUp) idFlipped = true; };
        pages.PageRendered += HandlePageRendered;
        // Already drawn (an early open): rebuild so page two exists now.
        pages.RefreshCurrentPage();
        return true;
    }

    /// <summary>The case's authored documents as the rules read them (documentName, label, shown value, key).</summary>
    private static CaseRecordSource Source(CaseDefinition definition)
    {
        var source = new CaseRecordSource
        {
            caseId = definition.caseId,
            caseNumber = definition.caseNumber,
            taxYear = definition.taxYear,
            birthdate = definition.birthdate
        };
        // Her taxpayer profile (the one she handed over in the plaza) on the back of her ID.
        foreach (TaxpayerProfile profile in TaxpayerProfileDatabase.All)
            if (profile != null && profile.TaxpayerName == definition.fullName)
            {
                ClientRecord id = CaseRecordRules.FromProfile(profile);
                source.profileText = id.profileText;
                source.profileNumber = id.profileNumber;
            }
        if (definition.documents != null)
            foreach (DocumentDefinition document in definition.documents)
            {
                if (document == null) continue;
                var copy = new CaseRecordDocument { documentName = document.documentName };
                if (document.fields != null)
                    foreach (DocumentFieldDefinition field in document.fields)
                        if (field != null) copy.fields.Add(new CaseRecordField { label = field.label, value = field.displayValue, key = field.semanticKey });
                source.documents.Add(copy);
            }
        return source;
    }

    /// <summary>Page two of this folder: the attached records draw themselves (no rows, never a value source).</summary>
    private List<CaseFolderPageContent> SupportingPages(CaseData data) => new List<CaseFolderPageContent>
    {
        new CaseFolderPageContent(CaseFolderPageContent.SupportingRecordsHeading, "")
        {
            Render = page => { },
            Role = CaseFolderPageContent.SupportingRecordsRole
        }
    };

    private void HandlePageRendered(int index)
    {
        if (records != null && index == records.PageIndex) records.SetEventCamera(HeldOnScreen.ActiveCamera(Camera.main));
    }

    // =========================================================
    // READING (progress only: nothing here changes the case's answers)
    // =========================================================

    private void HandleShown(ClientRecord record)
    {
        if (record == null) return;
        // The existing "document read" mark (consultation clues, the checkpoint's reviewed documents).
        CaseFolderUI.MarkReviewDocumentRead(record.source);
        if (shown.Add(record.id) && workIds.Contains(record.id) && CaseRecordRules.AllShown(workIds, shown))
            GameplayEvents.RaiseGuideCue(ShownFlag);
    }

    private void HandlePutBack(ClientRecord record)
    {
        if (record == null || prepared) return;
        returned.Add(record.id);
        if (!CaseRecordRules.Prepared(workIds, shown, returned)) return;
        prepared = true;
        Changed?.Invoke();
    }

    /// <summary>Resume: the saved preparation (never relocked; the records themselves start attached).</summary>
    public void RestorePrepared()
    {
        restoredPrepared = true;
        prepared = true;
    }

    /// <summary>A case already under way never asks for the preparation again (saved, an entry, an audit or approval).</summary>
    private bool UnderWay()
    {
        CaseManager cases = CaseManager.Instance;
        CaseData data = cases != null ? cases.CurrentCase : null;
        int filled = data?.folderTransferredFields != null ? data.folderTransferredFields.Count : 0;
        int audits = CaseProgressionManager.Instance != null ? CaseProgressionManager.Instance.CurrentCaseAuditAttempts : 0;
        bool approved = GameStateMachine.Instance != null && GameStateMachine.Instance.CurrentState is CaseCompleteState;
        return CaseRecordRules.AlreadyUnderWay(prepared || restoredPrepared, filled, audits, approved);
    }

    /// <summary>An entry filled, an audit made or the case approved (the preparation itself does not count).</summary>
    private static bool CaseStarted()
    {
        CaseManager cases = CaseManager.Instance;
        CaseData data = cases != null ? cases.CurrentCase : null;
        int filled = data?.folderTransferredFields != null ? data.folderTransferredFields.Count : 0;
        int audits = CaseProgressionManager.Instance != null ? CaseProgressionManager.Instance.CurrentCaseAuditAttempts : 0;
        bool approved = GameStateMachine.Instance != null && GameStateMachine.Instance.CurrentState is CaseCompleteState;
        return CaseRecordRules.AlreadyUnderWay(false, filled, audits, approved);
    }

    private bool Flag(string name)
    {
        if (name == PreparedFlag) return UnderWay();
        if (name == ShownFlag) return UnderWay() || CaseRecordRules.AllShown(workIds, shown);
        // Not the preparation alone: a fresh desk still shows the flip once (only a resumed or started case skips it).
        if (name == FlippedFlag) return idFlipped || restoredPrepared || CaseStarted();
        return false;
    }

    private static Transform Find(Scene scene, string path)
    {
        int slash = path.IndexOf('/');
        string rootName = path.Substring(0, slash);
        string rest = path.Substring(slash + 1);
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root.name != rootName) continue;
            Transform t = root.transform.Find(rest);
            if (t != null) return t;
        }
        return null;
    }
}
