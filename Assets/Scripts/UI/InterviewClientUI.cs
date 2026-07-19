using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InterviewClientUI : MonoBehaviour
{
    [Header("Panel")]
    [SerializeField] private GameObject interviewPanelRoot;

    [Header("Section Header")]
    [SerializeField] private TextMeshProUGUI sectionTitleText;

    [Header("Client Dialogue Area")]
    [SerializeField] private TextMeshProUGUI clientLineText;

    [Header("Question List")]
    [SerializeField] private GameObject questionButtonPrefab;

    [Header("Finish")]
    [SerializeField] private GameObject finishInterviewButton;

    [Header("NPC Reference")]
    [SerializeField] private NpcStateMachine clientNpcState;

    [Header("Presentation Mode (reuses this same panel — no new dialogue system)")]
    [SerializeField] private GameObject continueButton; // shown only during presentation mode
    [SerializeField] private GameObject questionButtonListRoot; // already exists — hidden during presentation

    private List<InterviewSection> sections;
    private int currentSectionIndex;
    private List<GameObject> spawnedButtons = new List<GameObject>();

    private List<string> presentationLines;
    private int presentationLineIndex;
    private bool isPresentationMode = false;
    private System.Action onPresentationConcluded;

    private void Awake()
    {
        Hide();
    }

    public void Show()
    {
        Debug.Log("Interview UI SHOW");
        sections = InterviewDataProvider.GetSections();
        currentSectionIndex = 0;
        clientLineText.text = "Good morning! Thank you for taking my case. What would you like to know?";
        RebuildForCurrentSection();
        interviewPanelRoot.SetActive(true);
    }

    public void Hide()
    {
        Debug.Log("Interview UI HIDE");
        interviewPanelRoot.SetActive(false);
    }

    private InterviewSection CurrentSection =>
        (sections != null && currentSectionIndex < sections.Count) ? sections[currentSectionIndex] : null;

    private void RebuildForCurrentSection()
    {
        foreach (var btn in spawnedButtons) Destroy(btn);
        spawnedButtons.Clear();

        InterviewSection section = CurrentSection;
        if (section == null)
        {
            sectionTitleText.text = "Interview Complete";
            return;
        }

        sectionTitleText.text = section.SectionTitle;

        foreach (var question in section.Questions)
        {
            GameObject buttonObj = Instantiate(questionButtonPrefab, questionButtonListRoot.transform);
            TextMeshProUGUI label = buttonObj.GetComponentInChildren<TextMeshProUGUI>();
            label.text = (question.IsAnswered ? "[Asked] " : "") + question.QuestionText;

            Button button = buttonObj.GetComponent<Button>();
            InterviewQuestion capturedQuestion = question;
            button.onClick.AddListener(() => OnQuestionSelected(capturedQuestion));

            spawnedButtons.Add(buttonObj);
        }

        // Auto-advance if this section is already fully answered (e.g. re-opening)
        if (section.IsComplete())
        {
            AdvanceToNextSection();
        }
    }

    private void OnQuestionSelected(InterviewQuestion question)
    {
        clientLineText.text = question.ClientAnswerText;
        question.ApplyAnswer(CaseManager.Instance.CurrentCase);

        RefreshButtonLabels();

        if (CurrentSection != null && CurrentSection.IsComplete())
        {
            // small delay-free advance; player sees the answer, then next click opens new set
            AdvanceToNextSection();
        }
    }

    private void RefreshButtonLabels()
    {
        InterviewSection section = CurrentSection;
        if (section == null) return;

        for (int i = 0; i < spawnedButtons.Count; i++)
        {
            TextMeshProUGUI label = spawnedButtons[i].GetComponentInChildren<TextMeshProUGUI>();
            InterviewQuestion q = section.Questions[i];
            label.text = (q.IsAnswered ? "[Asked] " : "") + q.QuestionText;
        }
    }

    private void AdvanceToNextSection()
    {
        currentSectionIndex++;
        RebuildForCurrentSection();
    }

    public void OnFinishInterviewPressed()
    {
        Debug.Log("Finish Interview button pressed!");

        Hide();

        clientNpcState?.ChangeState(new NpcCompletedState());

        CameraController.Instance.UnlockPlayerControls(); // NEW
    }


    /// <summary>
    /// Presentation mode: reuses the exact same panel, clientLineText, and
    /// player-lock flow as the interview, but shows a fixed queue of lines
    /// (no question choices) advanced one at a time via Continue — same
    /// linear pattern as AuditorDialogueUI. Used for "Present Findings to
    /// Client" (Milestone 14) instead of the branching interview questions.
    /// </summary>
    public void ShowPresentation(List<string> lines, System.Action onConcluded)
    {
        isPresentationMode = true;
        presentationLines = lines;
        presentationLineIndex = 0;
        onPresentationConcluded = onConcluded;

        questionButtonListRoot.SetActive(false);
        finishInterviewButton.SetActive(false);
        continueButton.SetActive(true);

        interviewPanelRoot.SetActive(true);
        RenderCurrentPresentationLine();
    }

    private void RenderCurrentPresentationLine()
    {
        if (presentationLineIndex < presentationLines.Count)
        {
            clientLineText.text = presentationLines[presentationLineIndex];
        }
    }

    /// <summary>
    /// Wired to the Continue button, active only during presentation mode.
    /// </summary>
    public void OnPresentationContinuePressed()
    {
        presentationLineIndex++;

        if (presentationLineIndex >= presentationLines.Count)
        {
            // Conversation concluded — close exactly like Finish Interview does,
            // but route through the presentation-specific callback instead.
            Hide();
            isPresentationMode = false;
            continueButton.SetActive(false);
            questionButtonListRoot.SetActive(true); // restore normal interview mode for any future reuse

            CameraController.Instance.UnlockPlayerControls();
            onPresentationConcluded?.Invoke();
            return;
        }

        RenderCurrentPresentationLine();
    }
}