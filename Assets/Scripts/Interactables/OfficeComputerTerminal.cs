using UnityEngine;

/// <summary>
/// PURPOSE:
/// The installed office computer (Level 3 T15), added by OfficeDecor to the
/// owned office_computer piece. Interacting opens the read-only client
/// register on the existing Audit Summary card (AuditSummaryPopupUI.ShowScreen:
/// its controls lock, its CLOSE). HER PAPERS closes the card and opens her
/// saved papers in the existing Case Folder reader (HudHotbar.OpenViewer).
///
/// Reads the saved campaign only (Level3RegisterRules); it saves nothing,
/// changes no chapter, claim, wallet or possession, and never offers the
/// legacy prototype calculator, filing, printing or figure transfer (Level 4).
/// </summary>
[RequireComponent(typeof(HighlightEffect))]
public sealed class OfficeComputerTerminal : MonoBehaviour, IInteractable
{
    private HighlightEffect highlight;

    private void Awake() => highlight = GetComponent<HighlightEffect>();

    public void OnFocus() { if (highlight != null) highlight.Highlight(); }

    public void OnUnfocus() { if (highlight != null) highlight.Unhighlight(); }

    public string GetPromptText() => "Use computer";

    public void OnInteract()
    {
        if (DialogueUI.IsAnyDialogueActive || HudRewardScreen.AnyOpen) return;
        AuditSummaryPopupUI popup = FindAnyObjectByType<AuditSummaryPopupUI>(FindObjectsInactive.Include);
        if (popup == null) { Debug.LogWarning("[OfficeComputer] No Audit Summary card in this office; the register cannot open."); return; }
        PlayerProgressData data = PlayerProgressManager.Instance != null ? PlayerProgressManager.Instance.CurrentProgress : null;
        CaseDefinition definition = CaseManager.Instance != null ? CaseManager.Instance.CurrentDefinition : null;
        string over = definition != null && definition.caseId == Level3ChapterRules.BeaCaseId ? Level3ClosingRules.DraftOverstatement(definition.level3) : "";
        Level3RegisterEntry entry = Level3RegisterRules.Bea(data, over);
        AudioManager.Instance?.PlayPaperSFX();
        popup.ShowScreen(Screen(entry, popup), entry.Status, null);
    }

    /// <summary>The register card: one client, her frozen approved lines, CLOSE and HER PAPERS (verified only).</summary>
    private static RewardScreenData Screen(Level3RegisterEntry entry, AuditSummaryPopupUI popup)
    {
        var data = new RewardScreenData
        {
            Title = "Client Register",
            Subtitle = entry.Verified ? entry.Client + "  ·  2026" : null,
            HasStatus = true,
            StatusGood = entry.Verified,
            StatusText = entry.Verified ? "VERIFIED" : "NO CASES YET",
            StatusDetail = entry.Status,
            Accent = entry.Verified ? new Color32(110, 201, 140, 255) : new Color32(150, 160, 175, 255),
            Summary = entry.Verified ? "Read only: exactly as approved and delivered." : null,
            ButtonLabel = "Close"
        };
        foreach (var line in entry.Lines) data.Fact(line.Key, line.Value);
        if (entry.Verified)
        {
            data.SecondaryLabel = "Her papers";
            data.Secondary = () =>
            {
                popup.OnCloseButtonPressed();
                HudHotbar bar = HudHotbar.Instance;
                if (bar == null || !bar.OpenViewer(InventoryCatalogue.CaseFolder))
                    Level3BeaTable.Level3Say("Her papers are in your Case Folder in the Bag.");
            };
        }
        return data;
    }
}
