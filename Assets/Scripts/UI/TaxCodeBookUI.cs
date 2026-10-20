using UnityEngine;
using UnityEngine.UI;
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

    // The shared book's sections plus this level's optional extra leaves
    // (Levels/<levelId>/book.json, see LevelBookLeaves). Built once.
    private List<TaxCodeSection> sections;

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

    [Header("Page Turn")]
    [Tooltip("Rect flipped when the page changes. Defaults to the heading's parent (the paper).")]
    [SerializeField] private RectTransform pageTurnRoot;
    [SerializeField, Min(0f)] private float pageTurnDuration = 0.26f;

    private Coroutine pageTurnRoutine;
    private bool hasPageTurnRest;
    private Quaternion pageTurnRestRotation;
    private Vector3 pageTurnRestScale;

    private int currentSectionIndex = 0;

    // The body's authored type settings, restored before each fit.
    private bool hasBodyRest;
    private float bodyRestSize;
    private bool bodyRestAutoSize;
    private float bodyRestMin, bodyRestMax;

    private List<TaxCodeValueChip> spawnedChips = new List<TaxCodeValueChip>();
    private readonly List<GameObject> spawnedCaptions = new List<GameObject>();
    private VerticalLayoutGroup chipLayout;
    private float originalChipSpacing = -1f;

    [Header("Printed Descriptions")]
    [Tooltip("Starting size for descriptions printed under each value; shrinks only if the section would not fit.")]
    [SerializeField, Min(10f)] private float descriptionFontSize = 36f;
    [SerializeField, Min(10f)] private float minDescriptionFontSize = 26f;
    [Tooltip("Size of the value list area while descriptions are printed under the values (the separate body text is then unused).")]
    [SerializeField] private Vector2 inlineListSize = new Vector2(700f, 860f);

    private RectTransform listArea;
    private bool hasListAreaRest;
    private Vector2 listAreaRestSize;
    private Vector2 listAreaRestPosition;

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

    private void OnDisable()
    {
        StopPageTurn();
    }

    /// <summary>The printed page (for a level's drag grip on a floating book).</summary>
    public Transform PaperRoot => GetPageTurnRoot();

    /// <summary>The book's pages are on screen.</summary>
    public bool IsShowing => bookPanelRoot != null && bookPanelRoot.activeSelf;

    public void Show()
    {
        DebugTrace.Log("[TaxCodeBookUI] Show() was called.");

        if (bookPanelRoot == null)
        {
            Debug.LogError("[TaxCodeBookUI] bookPanelRoot is not assigned.");
            return;
        }

        CameraController.Instance?.LockPlayerControls();

        StopPageTurn();
        // The book always opens on page 1.
        currentSectionIndex = 0;

        RenderSection(currentSectionIndex);

        bookPanelRoot.SetActive(true);
        PrintedPaperStyle.Apply(GetPageTurnRoot(), headingText, bodyText, pageIndicatorText);

        if (openBookCollider != null)
        {
            openBookCollider.enabled = false;
            DebugTrace.Log("[TaxCodeBookUI] Open book collider disabled.");
        }

        DebugTrace.Log(
            "[TaxCodeBookUI] Panel activated: " +
            bookPanelRoot.name
        );
    }
    public void Hide()
    {
        StopPageTurn();

        if (bookPanelRoot != null)
        {
            bookPanelRoot.SetActive(false);
        }

        if (openBookCollider != null)
        {
            openBookCollider.enabled = true;
            DebugTrace.Log("[TaxCodeBookUI] Open book collider enabled.");
        }

        CameraController.Instance?.UnlockPlayerControls();

        DebugTrace.Log("[TaxCodeBookUI] Tax Code Book UI hidden.");
    }

    public void NextPage()
    {
        // Changing page rebuilds the rows/chips; never do it under a dragged value.
        if (ValueDragSource.IsAnyDragActive)
            return;

        if (Sections.Count == 0)
            return;

        if (currentSectionIndex < Sections.Count - 1)
        {
            currentSectionIndex++;

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayPaperSFX();
            }

            TurnToCurrentPage(1);
        }
    }

    public void PreviousPage()
    {
        // Changing page rebuilds the rows/chips; never do it under a dragged value.
        if (ValueDragSource.IsAnyDragActive)
            return;

        if (Sections.Count == 0)
            return;

        if (currentSectionIndex > 0)
        {
            currentSectionIndex--;

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayPaperSFX();
            }

            TurnToCurrentPage(-1);
        }
    }

    // ============================================================
    // PAGE TURN
    // ============================================================

    private void TurnToCurrentPage(int direction)
    {
        RectTransform root = GetPageTurnRoot();
        if (root == null || pageTurnDuration <= 0f || !isActiveAndEnabled)
        {
            RenderSection(currentSectionIndex);
            return;
        }

        StopPageTurn();
        pageTurnRoutine = StartCoroutine(DeskMotion.TurnPage(root, pageTurnRestRotation,
            pageTurnRestScale, direction, pageTurnDuration, () =>
            {
                RenderSection(currentSectionIndex);
            }));
    }

    /// <summary>Stops a page turn and puts the paper back at rest.</summary>
    private void StopPageTurn()
    {
        if (pageTurnRoutine != null)
        {
            StopCoroutine(pageTurnRoutine);
            pageTurnRoutine = null;
        }

        if (hasPageTurnRest && pageTurnRoot != null)
        {
            pageTurnRoot.localRotation = pageTurnRestRotation;
            pageTurnRoot.localScale = pageTurnRestScale;
        }

    }

    private RectTransform GetPageTurnRoot()
    {
        if (pageTurnRoot == null && headingText != null)
            pageTurnRoot = headingText.transform.parent as RectTransform;

        if (pageTurnRoot != null && !hasPageTurnRest)
        {
            pageTurnRestRotation = pageTurnRoot.localRotation;
            pageTurnRestScale = pageTurnRoot.localScale;
            hasPageTurnRest = true;
        }

        return pageTurnRoot;
    }

    private List<TaxCodeSection> Sections
    {
        get
        {
            if (sections == null)
            {
                sections = new List<TaxCodeSection>();
                if (bookData != null && bookData.sections != null) sections.AddRange(bookData.sections);
                sections.AddRange(LevelBookLeaves.Load(OfficeLevelProfile.LevelIdFor(this)));
            }
            return sections;
        }
    }

    private void RenderSection(int index)
    {
        if (Sections.Count == 0)
        {
            headingText.text = "No Tax Code Data Assigned";
            bodyText.text = "Assign a TaxCodeBookData asset in the Inspector.";
            leftEdgeIndicator.SetActive(false);
            rightEdgeIndicator.SetActive(false);
            return;
        }

        TaxCodeSection section = Sections[index];
        headingText.text = section.heading;
        bodyText.text = section.body;
        pageIndicatorText.text = $"Section {index + 1} / {Sections.Count}";

        leftEdgeIndicator.SetActive(index > 0);
        rightEdgeIndicator.SetActive(index < Sections.Count - 1);

        RenderClickableValues(section);
        FitBody();
    }

    /// <summary>
    /// Keeps a long leaf inside its page: when the printed body would run
    /// past its box it shrinks (never below about two thirds of its size).
    /// Text that already fits keeps its authored size.
    /// </summary>
    private void FitBody()
    {
        if (bodyText == null) return;
        if (!hasBodyRest)
        {
            bodyRestSize = bodyText.fontSize;
            bodyRestAutoSize = bodyText.enableAutoSizing;
            bodyRestMin = bodyText.fontSizeMin;
            bodyRestMax = bodyText.fontSizeMax;
            hasBodyRest = true;
        }
        bodyText.enableAutoSizing = bodyRestAutoSize;
        bodyText.fontSizeMin = bodyRestMin;
        bodyText.fontSizeMax = bodyRestMax;
        bodyText.fontSize = bodyRestSize;
        if (bodyRestAutoSize || string.IsNullOrEmpty(bodyText.text)) return;

        Rect box = bodyText.rectTransform.rect;
        if (box.width <= 0f || box.height <= 0f) return;
        if (bodyText.GetPreferredValues(bodyText.text, box.width, Mathf.Infinity).y <= box.height) return;
        bodyText.fontSizeMax = bodyRestSize;
        bodyText.fontSizeMin = Mathf.Max(10f, bodyRestSize * 0.66f);
        bodyText.enableAutoSizing = true;
    }

    /// <summary>
    /// When the section body has one paragraph per value, prints each
    /// paragraph directly under its value so they can never drift apart.
    /// Otherwise the authored body text layout is kept.
    /// </summary>
    private void PrintDescriptionsUnderChips(TaxCodeSection section)
    {
        if (chipLayout == null && clickableValueListRoot != null)
            chipLayout = clickableValueListRoot.GetComponent<VerticalLayoutGroup>();
        if (chipLayout != null && originalChipSpacing < 0f)
            originalChipSpacing = chipLayout.spacing;

        List<string> paragraphs = PaperInkStyle.Paragraphs(section.body);
        bool inline = chipLayout != null && spawnedChips.Count > 0 &&
                      paragraphs.Count == spawnedChips.Count;

        if (chipLayout != null)
            chipLayout.spacing = inline ? 4f : originalChipSpacing;
        SetInlineListArea(inline);
        if (!inline)
            return;

        bodyText.text = string.Empty;
        TMP_FontAsset font = bodyText.font;
        var captions = new List<TextMeshProUGUI>();
        for (int i = 0; i < spawnedChips.Count; i++)
        {
            int index = spawnedChips[i].transform.GetSiblingIndex();
            // One flowing paragraph wraps tighter than the authored line breaks.
            string description = paragraphs[i].Replace('\n', ' ');
            TextMeshProUGUI caption = PaperInkStyle.CreateCaption(clickableValueListRoot, font, description,
                descriptionFontSize);
            caption.transform.SetSiblingIndex(index + 1);
            GameObject spacer = PaperInkStyle.CreateSpacer(clickableValueListRoot, 14f);
            spacer.transform.SetSiblingIndex(index + 2);
            spawnedCaptions.Add(caption.gameObject);
            spawnedCaptions.Add(spacer);
            captions.Add(caption);
        }

        FitDescriptions(captions);
    }

    /// <summary>Shrinks the printed descriptions until the whole section fits its area.</summary>
    private void FitDescriptions(List<TextMeshProUGUI> captions)
    {
        RectTransform content = clickableValueListRoot as RectTransform;
        RectTransform viewport = content != null ? content.parent as RectTransform : null;
        if (content == null || viewport == null || captions.Count == 0)
            return;

        float available = viewport.rect.height;
        // First with the tall (easy to drag) chips, then, if a crowded page still
        // overflows at the smallest captions, with the compact chips.
        foreach (float chipHeight in new[] { PaperInkStyle.ChipHeight, PaperInkStyle.ChipHeightCompact })
        {
            foreach (TaxCodeValueChip chip in spawnedChips)
            {
                LayoutElement element = chip != null ? chip.GetComponent<LayoutElement>() : null;
                if (element == null) continue;
                element.minHeight = chipHeight;
                element.preferredHeight = chipHeight;
            }
            for (float size = descriptionFontSize; size >= minDescriptionFontSize; size -= 2f)
            {
                foreach (TextMeshProUGUI caption in captions)
                    caption.fontSize = size;

                LayoutRebuilder.ForceRebuildLayoutImmediate(content);
                if (LayoutUtility.GetPreferredHeight(content) <= available)
                    return;
            }
        }
    }

    /// <summary>
    /// Uses the page space freed by the unused body text for the value list,
    /// keeping its top edge. Only applied when the list area sits directly on
    /// the paper; restored when the authored body layout is used.
    /// </summary>
    private void SetInlineListArea(bool inline)
    {
        if (listArea == null && clickableValueListRoot != null && clickableValueListRoot.parent != null)
        {
            RectTransform candidate = clickableValueListRoot.parent.parent as RectTransform;
            if (candidate != null && candidate.parent == GetPageTurnRoot())
                listArea = candidate;
        }

        if (listArea == null)
            return;

        if (!hasListAreaRest)
        {
            listAreaRestSize = listArea.sizeDelta;
            listAreaRestPosition = listArea.anchoredPosition;
            hasListAreaRest = true;
        }

        if (!inline)
        {
            listArea.sizeDelta = listAreaRestSize;
            listArea.anchoredPosition = listAreaRestPosition;
            return;
        }

        // Keep the top edge where it was; grow downward and sideways.
        float top = listAreaRestPosition.y + listAreaRestSize.y * (1f - listArea.pivot.y);
        Vector2 size = new Vector2(Mathf.Max(listAreaRestSize.x, inlineListSize.x),
            Mathf.Max(listAreaRestSize.y, inlineListSize.y));
        listArea.sizeDelta = size;
        listArea.anchoredPosition = new Vector2(listAreaRestPosition.x, top - size.y * (1f - listArea.pivot.y));
    }

    // Call this from inside your existing RenderSection(index) method:
    private void RenderClickableValues(TaxCodeSection section)
    {
        // Hide before Destroy (which waits until the end of the frame) so the
        // old values never count in this frame's layout.
        foreach (var chip in spawnedChips)
        {
            chip.gameObject.SetActive(false);
            Destroy(chip.gameObject);
        }
        spawnedChips.Clear();
        foreach (var caption in spawnedCaptions)
        {
            caption.SetActive(false);
            Destroy(caption);
        }
        spawnedCaptions.Clear();

        foreach (var clickable in section.clickableValues)
        {
            var chipObj = Instantiate(valueChipPrefab, clickableValueListRoot);
            chipObj.Initialize(clickable);
            spawnedChips.Add(chipObj);
        }

        PrintDescriptionsUnderChips(section);
    }
}