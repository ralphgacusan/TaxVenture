using UnityEngine;
using TMPro;

/// <summary>
/// PURPOSE:
/// Displays the Tax Code Book using the same static "paper" pattern as
/// CaseFolderUI — one panel, one paper, Previous/Next swap text in place,
/// no animation. Content comes from a TaxCodeBookData ScriptableObject
/// asset instead of runtime CaseData, since this is static reference
/// material.
///
/// RESPONSIBILITIES:
/// - Show/hide the book panel
/// - Render the current section's heading/body onto the paper
/// - Handle Next/Previous navigation
///
/// DOES NOT:
/// - Write to CaseData or trigger any GameStateMachine transition — the Tax
///   Book is pure reference material, consulted freely, not a progression gate.
///
/// CONNECTS WITH:
/// - TaxCodeBookInteractable: calls Show() when the book is clicked at the desk
/// - TaxCodeBookData: the ScriptableObject asset assigned in the Inspector
/// </summary>
public class TaxCodeBookUI : MonoBehaviour
{
    [Header("Data Source")]
    [Tooltip("Drag the single TaxCodeBookData asset here.")]
    [SerializeField] private TaxCodeBookData bookData;

    [Header("Panel")]
    [SerializeField] private GameObject bookPanelRoot;

    [Header("The Paper (single, static)")]
    [SerializeField] private TextMeshProUGUI headingText;
    [SerializeField] private TextMeshProUGUI bodyText;
    [SerializeField] private TextMeshProUGUI pageIndicatorText;

    [Header("Nav Buttons")]
    [SerializeField] private GameObject previousButton;
    [SerializeField] private GameObject nextButton;

    private int currentSectionIndex = 0;

    private void Awake()
    {
        Hide();
    }

    public void Show()
    {
        bookPanelRoot.SetActive(true);
        RenderSection(currentSectionIndex);
    }

    public void Hide()
    {
        bookPanelRoot.SetActive(false);
    }

    public void NextPage()
    {
        if (currentSectionIndex < bookData.sections.Count - 1)
        {
            currentSectionIndex++;
            RenderSection(currentSectionIndex);
        }
    }

    public void PreviousPage()
    {
        if (currentSectionIndex > 0)
        {
            currentSectionIndex--;
            RenderSection(currentSectionIndex);
        }
    }

    private void RenderSection(int index)
    {
        if (bookData == null || bookData.sections.Count == 0)
        {
            headingText.text = "No Tax Code Data Assigned";
            bodyText.text = "Assign a TaxCodeBookData asset in the Inspector.";
            return;
        }

        TaxCodeSection section = bookData.sections[index];
        headingText.text = section.heading;
        bodyText.text = section.body;
        pageIndicatorText.text = $"Section {index + 1} / {bookData.sections.Count}";

        previousButton.SetActive(index > 0);
        nextButton.SetActive(index < bookData.sections.Count - 1);
    }
}