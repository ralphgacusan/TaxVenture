using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// PURPOSE:
/// "Prepare Tax Return" app, Page 1 — the player manually picks a BIR Form.
/// No automatic selection, no correctness hint. Wrong choices are allowed;
/// the Auditor catches mistakes later, per design goal of meaningful
/// human error.
///
/// CONNECTS WITH:
/// - ComputerHomeUI: launches this; Back returns to home
/// - BirFormEncodingUI: launched with the chosen RequiredForm
/// </summary>
public class FormSelectionUI : MonoBehaviour
{
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private Button form1700Button;
    [SerializeField] private Button form1701Button;
    [SerializeField] private Button form1701AButton;

    [SerializeField] private ComputerHomeUI computerHomeUI;
    [SerializeField] private BirFormEncodingUI encodingUI;

    private void Awake()
    {
        Hide();
        form1700Button.onClick.AddListener(() => SelectForm(RequiredForm.BIR1700));
        form1701Button.onClick.AddListener(() => SelectForm(RequiredForm.BIR1701));
        form1701AButton.onClick.AddListener(() => SelectForm(RequiredForm.BIR1701A));
    }

    public void Show()
    {
        panelRoot.SetActive(true);
    }

    public void Hide()
    {
        panelRoot.SetActive(false);
    }

    private void SelectForm(RequiredForm form)
    {
        Hide();
        encodingUI.Show(form);
    }

    public void OnBackPressed()
    {
        Hide();
        computerHomeUI.Show();
    }
}