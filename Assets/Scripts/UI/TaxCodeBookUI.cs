using UnityEngine;
using TMPro;
using System.Collections.Generic;

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

    [Header("Page Edge Navigation (replaces Prev/Next buttons)")]
    [SerializeField] private PageEdgeTapZone leftEdgeZone;
    [SerializeField] private PageEdgeTapZone rightEdgeZone;
    [SerializeField] private GameObject leftEdgeIndicator;
    [SerializeField] private GameObject rightEdgeIndicator;

    [Header("Swipe to Close (replaces Close button)")]
    [SerializeField] private SwipeDownToClose swipeToClose;

    [Header("Clickable Values")]
    [SerializeField] private Transform clickableValueListRoot;
    [SerializeField] private TaxCodeValueChip valueChipPrefab;

    [Header("3D Book Interaction")]
    [SerializeField] private Collider openBookCollider;

    private int currentSectionIndex = 0;

    private List<TaxCodeValueChip> spawnedChips = new List<TaxCodeValueChip>();

    private void Awake()
    {
        if (leftEdgeZone != null)
        {
            leftEdgeZone.OnTapped += PreviousPage;
        }
        else
        {
            Debug.LogError(
                "[TaxCodeBookUI] Left Edge Zone is not assigned."
            );
        }

        if (rightEdgeZone != null)
        {
            rightEdgeZone.OnTapped += NextPage;
        }
        else
        {
            Debug.LogError(
                "[TaxCodeBookUI] Right Edge Zone is not assigned."
            );
        }

        if (swipeToClose != null)
        {
            swipeToClose.OnSwipeClosed += Hide;
        }
        else
        {
            Debug.LogError(
                "[TaxCodeBookUI] Swipe To Close is not assigned."
            );
        }
    }

    public void Show()
    {
        Debug.Log("[TaxCodeBookUI] Show() was called.");

        if (bookPanelRoot == null)
        {
            Debug.LogError("[TaxCodeBookUI] bookPanelRoot is not assigned.");
            return;
        }

        CameraController.Instance?.LockPlayerControls();

        currentSectionIndex = 0;

        RenderSection(currentSectionIndex);

        bookPanelRoot.SetActive(true);

        if (openBookCollider != null)
        {
            openBookCollider.enabled = false;
            Debug.Log("[TaxCodeBookUI] Open book collider disabled.");
        }

        Debug.Log(
            "[TaxCodeBookUI] Panel activated: " +
            bookPanelRoot.name
        );
    }
    public void Hide()
    {
        if (bookPanelRoot != null)
        {
            bookPanelRoot.SetActive(false);
        }

        if (openBookCollider != null)
        {
            openBookCollider.enabled = true;
            Debug.Log("[TaxCodeBookUI] Open book collider enabled.");
        }

        CameraController.Instance?.UnlockPlayerControls();

        Debug.Log("[TaxCodeBookUI] Tax Code Book UI hidden.");
    }

    public void NextPage()
    {
        if (bookData == null || bookData.sections == null || bookData.sections.Count == 0)
            return;

        if (currentSectionIndex < bookData.sections.Count - 1)
        {
            currentSectionIndex++;

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayPaperSFX();
            }

            RenderSection(currentSectionIndex);
        }
    }

    public void PreviousPage()
    {
        if (bookData == null || bookData.sections == null || bookData.sections.Count == 0)
            return;

        if (currentSectionIndex > 0)
        {
            currentSectionIndex--;

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayPaperSFX();
            }

            RenderSection(currentSectionIndex);
        }
    }

    private void RenderSection(int index)
    {
        if (bookData == null || bookData.sections.Count == 0)
        {
            headingText.text = "No Tax Code Data Assigned";
            bodyText.text = "Assign a TaxCodeBookData asset in the Inspector.";
            leftEdgeIndicator.SetActive(false);
            rightEdgeIndicator.SetActive(false);
            return;
        }

        TaxCodeSection section = bookData.sections[index];
        headingText.text = section.heading;
        bodyText.text = section.body;
        pageIndicatorText.text = $"Section {index + 1} / {bookData.sections.Count}";

        leftEdgeIndicator.SetActive(index > 0);
        rightEdgeIndicator.SetActive(index < bookData.sections.Count - 1);

        RenderClickableValues(section);
    }

    // Call this from inside your existing RenderSection(index) method:
    private void RenderClickableValues(TaxCodeSection section)
    {
        foreach (var chip in spawnedChips) Destroy(chip.gameObject);
        spawnedChips.Clear();

        foreach (var clickable in section.clickableValues)
        {
            var chipObj = Instantiate(valueChipPrefab, clickableValueListRoot);
            chipObj.Initialize(clickable);
            spawnedChips.Add(chipObj);
        }
    }
}