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
    [SerializeField] private GameObject questionButtonListRoot;
    [SerializeField] private GameObject questionButtonPrefab;

    [Header("Finish")]
    [SerializeField] private GameObject finishInterviewButton;

    [Header("NPC Reference")]
    [SerializeField] private NpcStateMachine clientNpcState;

    private List<InterviewSection> sections;
    private int currentSectionIndex;
    private List<GameObject> spawnedButtons = new List<GameObject>();

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
}